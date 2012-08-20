//
// RazorCSharpParser.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using System.Web.Razor;
using System.Web.Mvc.Razor;
using System.Threading;
using System.Web.Razor.Text;
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.TypeSystem;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.IO;
using MonoDevelop.AspNet.Parser.Dom;
using MonoDevelop.Core;
using MonoDevelop.AspNet.Parser;
using System.Web.Configuration;
using System.Web.WebPages.Razor.Configuration;
using System.Web.WebPages.Razor;
using System.Configuration;

namespace MonoDevelop.AspNet.Mvc.Parser
{
	public class RazorCSharpParser : AbstractTypeSystemParser
	{
		RazorEditorParser editorParser;
		DocumentParseCompleteEventArgs capturedArgs;
		AutoResetEvent parseComplete;
		ChangeInfo lastChange;
		string lastParsedFile;
		TextDocument currentDocument;
		AspMvcProject project;
		IList<TextDocument> openDocuments;

		public IList<TextDocument> OpenDocuments { get { return openDocuments; } }

		public RazorCSharpParser ()
		{
			openDocuments = new List<TextDocument> ();
		}

		public override ParsedDocument Parse (bool storeAst, string fileName, System.IO.TextReader content, Projects.Project project = null)
		{
			currentDocument = openDocuments.FirstOrDefault (d => d.FileName == fileName);
			// We need document and project to be loaded to correctly initialize Razor Host.
			if (project == null || (currentDocument == null && !TryAddDocument (fileName)))
				return new RazorCSharpParsedDocument (fileName, new RazorCSharpPageInfo ());

			this.project = project as AspMvcProject;

			EnsureParserInitializedFor (fileName);

			var errors = new List<Error> ();

			using (var source = new SeekableTextReader (content)) {
				var textChange = CreateTextChange (source);
				var parseResult = editorParser.CheckForStructureChanges (textChange);
				if (parseResult == PartialParseResult.Rejected) {
					parseComplete.WaitOne ();
					if (!capturedArgs.GeneratorResults.Success)
						GetRazorErrors (errors);
				}
			}

			CreateHtmlDocument ();
			GetHtmlErrors (errors);
			CreateCSharpParsedDocument ();
			ClearLastChange ();

			var pageInfo = new RazorCSharpPageInfo () {
				HtmlRoot = htmlParsedDocument,
				GeneratorResults = capturedArgs.GeneratorResults,
				Spans = editorParser.CurrentParseTree.Flatten (),
				CSharpParsedFile = parsedCodeFile,
				CSharpCode = csharpCode,
				Errors = errors,
				FoldingRegions = GetFoldingRegions (),
				Comments = comments,
				Compilation = CreateCompilation ()
			};

			return new RazorCSharpParsedDocument (fileName, pageInfo);
		}

		bool TryAddDocument (string fileName)
		{
			var guiDoc = IdeApp.Workbench.GetDocument (fileName);
			if (guiDoc != null && guiDoc.Editor != null) {
				currentDocument = guiDoc.Editor.Document;
				currentDocument.TextReplacing += OnTextReplacing;
				openDocuments.Add (currentDocument);
				guiDoc.Closed += (sender, args) =>
				{
					var doc = sender as Document;
					if (doc.Editor != null && doc.Editor.Document != null)
						openDocuments.Remove (doc.Editor.Document);
				};
				return true;
			}
			return false;
		}

		void EnsureParserInitializedFor (string fileName)
		{
			if (lastParsedFile == fileName && editorParser != null)
				return;

			if (editorParser != null)
				DisposeCurrentParser ();

			CreateParserFor (fileName);
		}

		void CreateParserFor (string fileName)
		{
			editorParser = new RazorEditorParser (CreateRazorHost (fileName), fileName);

			parseComplete = new AutoResetEvent (false);
			editorParser.DocumentParseComplete += (sender, args) =>
			{
				capturedArgs = args;
				parseComplete.Set ();
			};

			lastParsedFile = fileName;
		}

		RazorEngineHost CreateRazorHost (string fileName)
		{
			string virtualPath = "~/Views/Default.cshtml";
			if (project != null)
				virtualPath = project.LocalToVirtualPath (fileName);

			WebPageRazorHost host = null;

			// Try to create host using web.config file
			var webConfigMap = new WebConfigurationFileMap ();
			var vdm = new VirtualDirectoryMapping (project.BaseDirectory.Combine ("Views"), true, "web.config");
			webConfigMap.VirtualDirectories.Add ("/", vdm);
			Configuration configuration;
			try {
				configuration = WebConfigurationManager.OpenMappedWebConfiguration (webConfigMap, "/");
			} catch {
				configuration = null;
			}
			if (configuration != null) {
				var rws = configuration.GetSectionGroup (RazorWebSectionGroup.GroupName) as RazorWebSectionGroup;
				if (rws != null) {
					host = WebRazorHostFactory.CreateHostFromConfig (rws, virtualPath, fileName);
					host.DesignTimeMode = true;
				}
			}

			if (host == null) {
				host = new MvcWebPageRazorHost (virtualPath, fileName) { DesignTimeMode = true };
				// Add default namespaces from Razor section
				host.NamespaceImports.Add ("System.Web.Mvc");
				host.NamespaceImports.Add ("System.Web.Mvc.Ajax");
				host.NamespaceImports.Add ("System.Web.Mvc.Html");
				host.NamespaceImports.Add ("System.Web.Routing");
			}

			return host;
		}

		void DisposeCurrentParser ()
		{
			editorParser.Dispose ();
			editorParser = null;
			parseComplete.Dispose ();
			parseComplete = null;
			ClearLastChange ();
		}

		void ClearLastChange ()
		{
			lastChange = null;
		}

		TextChange CreateTextChange (SeekableTextReader source)
		{
			if (lastChange == null)
				return new TextChange (0, 0, new SeekableTextReader (String.Empty), 0, source.Length, source);
			if (lastChange.DeleteChange)
				return new TextChange (lastChange.StartOffset, lastChange.AbsoluteLength, lastChange.Buffer,
					lastChange.StartOffset,	0, source);
			return new TextChange (lastChange.StartOffset, 0, lastChange.Buffer, lastChange.StartOffset,
				lastChange.AbsoluteLength, source);
		}

		void GetRazorErrors (List<Error> errors)
		{
			foreach (var error in capturedArgs.GeneratorResults.ParserErrors) {
				int off = error.Location.AbsoluteIndex;
				if (error.Location.CharacterIndex > 0 && error.Length == 1)
					off--;
				errors.Add (new Error (ErrorType.Error, error.Message, currentDocument.OffsetToLocation (off)));
			}
		}

		RootNode htmlParsedDocument;
		IList<Comment> comments;

		void CreateHtmlDocument ()
		{
			var sb = new StringBuilder ();
			var spanList = new List<Span> ();
			comments = new List<Comment> ();

			Action<Span> action = (Span span) =>
			{
				if (span.Kind == SpanKind.Markup) {
					sb.Append (span.Content);
					spanList.Add (span);
				} else {
					for (int i = 0; i < span.Content.Length; i++) {
						char ch = span.Content[i];
						if (ch != '\r' && ch != '\n')
							sb.Append (' ');
						else
							sb.Append (ch);
					}
					if (span.Kind == SpanKind.Comment) {
						var comment = new Comment (span.Content)
						{
							OpenTag = "@*",
							ClosingTag = "*@",
							CommentType = CommentType.Block,
						};
						comment.Region = new DomRegion (
							currentDocument.OffsetToLocation (span.Start.AbsoluteIndex - comment.OpenTag.Length),
							currentDocument.OffsetToLocation (span.Start.AbsoluteIndex + span.Length + comment.ClosingTag.Length));
						comments.Add (comment);
					}
				}
			};

			editorParser.CurrentParseTree.Accept (new CallbackVisitor (action));
			var root = new RootNode ();

			try {
				root.Parse (lastParsedFile, new StringReader (sb.ToString ()));
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error parsing html in Razor document '" + (lastParsedFile ?? "") + "'", ex);
			}

			htmlParsedDocument = root;
		}

		void GetHtmlErrors (List<Error> errors)
		{
			foreach (var error in htmlParsedDocument.ParseErrors)
				errors.Add (new Error (ErrorType.Error, error.Message, error.Location.BeginLine, error.Location.BeginColumn));
		}

		IEnumerable<FoldingRegion> GetFoldingRegions ()
		{
			var foldingRegions = new List<FoldingRegion> ();
			GetHtmlFoldingRegions (foldingRegions);
			GetRazorFoldingRegions (foldingRegions);
			return foldingRegions;
		}

		void GetHtmlFoldingRegions (List<FoldingRegion> foldingRegions)
		{
			if (htmlParsedDocument != null) {
				var cuVisitor = new CompilationUnitVisitor (foldingRegions);
				htmlParsedDocument.AcceptVisit (cuVisitor);
			}
		}

		void GetRazorFoldingRegions (List<FoldingRegion> foldingRegions)
		{
			var blocks = new List<Block> ();
			GetBlocks (editorParser.CurrentParseTree, blocks);
			foreach (var block in blocks) {
				var beginLine = currentDocument.GetLineByOffset (block.Start.AbsoluteIndex);
				var endLine = currentDocument.GetLineByOffset (block.Start.AbsoluteIndex + block.Length);
				if (beginLine != endLine)
					foldingRegions.Add (new FoldingRegion (RazorUtils.GetShortName (block),
						new DomRegion (currentDocument.OffsetToLocation (block.Start.AbsoluteIndex),
							currentDocument.OffsetToLocation (block.Start.AbsoluteIndex + block.Length))));
			}
		}

		void GetBlocks (Block root, IList<Block> blocks)
		{
			foreach (var block in root.Children.Where (n => n.IsBlock).Select (n => n as Block)) {
				if (block.Type != BlockType.Comment && block.Type != BlockType.Markup)
					blocks.Add (block);
				if (block.Type != BlockType.Helper)
					GetBlocks (block, blocks);
			}
		}

		ParsedDocumentDecorator parsedCodeFile;
		string csharpCode;

		void CreateCSharpParsedDocument ()
		{
			var parser = new ICSharpCode.NRefactory.CSharp.CSharpParser ();
			ICSharpCode.NRefactory.CSharp.SyntaxTree unit;
			csharpCode = CreateCodeFile ();
			using (var sr = new StringReader (csharpCode)) {
				unit = parser.Parse (sr, "Generated.cs");
			}
			unit.Freeze ();
			var parsedDoc = unit.ToTypeSystem ();
			parsedCodeFile = new ParsedDocumentDecorator (parsedDoc) { Ast = unit };
		}

		string CreateCodeFile ()
		{
			var unit = capturedArgs.GeneratorResults.GeneratedCode;
			var provider = project.LanguageBinding.GetCodeDomProvider ();
			string code;
			using (var sw = new StringWriter ()) {
				provider.GenerateCodeFromCompileUnit (unit, sw, new System.CodeDom.Compiler.CodeGeneratorOptions ()	{
					// Values adjusted to Razor code generator
					BlankLinesBetweenMembers = false,
					IndentString = String.Empty,
				});
				sw.Flush ();
				code = sw.ToString ();
			}
			//HACK: Add a newline between first line pragma and constructor declaration in the generated code file
			//to correctly determine code segments and get code completion working properly on Mono.
			//The missing newline is present only when Mono is used as a runtime.
			code = code.Replace ("#line hiddenpublic", "#line hidden" + Environment.NewLine + "public");
			return code;
		}

		// Creates compilation that includes underlying C# file for Razor view
		ICompilation CreateCompilation ()
		{
			return TypeSystemService.GetProjectContext (project).AddOrUpdateFiles (parsedCodeFile.ParsedFile).CreateCompilation ();
		}

		void OnTextReplacing (object sender, DocumentChangeEventArgs e)
		{
			if (lastChange == null)
				lastChange = new ChangeInfo (e.Offset, new SeekableTextReader((sender as TextDocument).Text));
			if (e.ChangeDelta > 0) {
				lastChange.Length += e.InsertionLength;
			} else {
				lastChange.Length -= e.RemovalLength;
			}
		}
	}

	class ChangeInfo
	{
		int offset;

		public ChangeInfo (int off, SeekableTextReader buffer)
		{
			offset = off;
			Length = 0;
			Buffer = buffer;
		}

		public int StartOffset {
			get	{ return offset; }
			private set { }
		}

		public int Length { get; set; }
		public int AbsoluteLength {
			get { return Math.Abs (Length); }
			private set { }
		}

		public SeekableTextReader Buffer { get; set; }
		public bool DeleteChange { get { return Length < 0; } }
	}
}
