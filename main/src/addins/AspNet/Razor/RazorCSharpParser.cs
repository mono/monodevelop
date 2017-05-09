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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using System.Web.Mvc.Razor;
using System.Web.Razor;
using System.Web.Razor.Parser;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Text;
using System.Web.WebPages.Razor;
using System.Web.WebPages.Razor.Configuration;


using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms.Parser;
using MonoDevelop.AspNet.Razor.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Projection;
using System.Runtime.Remoting.Messaging;

namespace MonoDevelop.AspNet.Razor
{
	public class RazorCSharpParser : TypeSystemParser
	{
		IList<OpenRazorDocument> openDocuments;
		IList<OpenRazorDocument> documentsPendingDispose;

		internal IList<OpenRazorDocument> OpenDocuments { get { return openDocuments; } }

		public RazorCSharpParser ()
		{
			openDocuments = new List<OpenRazorDocument> ();
			documentsPendingDispose = new List<OpenRazorDocument> ();

			IdeApp.Exited += delegate {
				//HACK: workaround for Mono's not shutting downs IsBackground threads in WaitAny calls
				DisposeDocuments (documentsPendingDispose);
				DisposeDocuments (openDocuments);
			};
		}

		public override System.Threading.Tasks.Task<ParsedDocument> Parse (MonoDevelop.Ide.TypeSystem.ParseOptions parseOptions, CancellationToken cancellationToken)
		{
			OpenRazorDocument currentDocument = GetDocument (parseOptions.FileName);
			if (currentDocument == null)
				return System.Threading.Tasks.Task.FromResult ((ParsedDocument)new RazorCSharpParsedDocument (parseOptions.FileName, new RazorCSharpPageInfo ()));

			var context = new RazorCSharpParserContext (parseOptions, currentDocument);

			lock (currentDocument) {
				return Parse (context, cancellationToken);
			}
		}

		public override bool CanGenerateProjection (string mimeType, string buildAction, string [] supportedLanguages)
		{
			return mimeType == "text/x-cshtml";
		}

		public override async System.Threading.Tasks.Task<IReadOnlyList<Projection>> GenerateProjections (Ide.TypeSystem.ParseOptions options, CancellationToken cancellationToken = default (CancellationToken))
		{
			var razorDocument = (RazorCSharpParsedDocument)await Parse (options, cancellationToken);
			return await GenerateProjections (razorDocument, options, cancellationToken);
		}

		async System.Threading.Tasks.Task<IReadOnlyList<Projection>> GenerateProjections (RazorCSharpParsedDocument razorDocument, Ide.TypeSystem.ParseOptions options, CancellationToken cancellationToken = default (CancellationToken))
		{
			var code = razorDocument.PageInfo.CSharpCode;
			if (string.IsNullOrEmpty (code))
				return new List<Projection> ();
			var doc = TextEditorFactory.CreateNewDocument (new StringTextSource (code), razorDocument.PageInfo.ParsedDocument.FileName, "text/x-csharp");
			var currentMappings = razorDocument.PageInfo.GeneratorResults.DesignTimeLineMappings;
			var segments = new List<ProjectedSegment> ();

			foreach (var map in currentMappings) {

				string pattern = "#line " + map.Key + " ";
				var idx = razorDocument.PageInfo.CSharpCode.IndexOf (pattern, StringComparison.Ordinal);
				if (idx < 0)
					continue;
				var line = doc.GetLineByOffset (idx);
				var offset = line.NextLine.Offset + map.Value.StartGeneratedColumn - 1;

				var seg = new ProjectedSegment (map.Value.StartOffset.Value, offset, map.Value.CodeLength);
				segments.Add (seg);
			}

			var projections = new List<Projection> ();
			projections.Add (new Projection (doc, segments));
			return projections;
		}

		public override async System.Threading.Tasks.Task<ParsedDocumentProjection> GenerateParsedDocumentProjection (Ide.TypeSystem.ParseOptions options, CancellationToken cancellationToken = default (CancellationToken))
		{
			var razorDocument = (RazorCSharpParsedDocument)await Parse (options, cancellationToken);
			var projections = await GenerateProjections (razorDocument, options, cancellationToken);
			return new ParsedDocumentProjection (razorDocument, projections);
		}

		OpenRazorDocument GetDocument (string fileName)
		{
			lock (this) {
				DisposeDocuments (documentsPendingDispose);

				OpenRazorDocument currentDocument = openDocuments.FirstOrDefault (d => d != null && d.FileName == fileName);
				// We need document and project to be loaded to correctly initialize Razor Host.
				if (currentDocument == null && !TryAddDocument (fileName, out currentDocument))
					return null;

				return currentDocument;
			}
		}

		void DisposeDocuments (IEnumerable<OpenRazorDocument> documents)
		{
			try {
				foreach (OpenRazorDocument document in documents.Reverse ()) {
					document.Dispose ();
					documentsPendingDispose.Remove (document);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Dispose pending Razor document error.", ex);
			}
		}

		System.Threading.Tasks.Task<ParsedDocument> Parse (RazorCSharpParserContext context, CancellationToken cancellationToken)
		{
			EnsureParserInitializedFor (context);

			var errors = new List<Error> ();

			using (var source = new SeekableTextReader (context.Content.CreateReader ())) {
				var textChange = CreateTextChange (context, source);
				var parseResult = context.EditorParser.CheckForStructureChanges (textChange);
				if (parseResult == PartialParseResult.Rejected) {
					context.RazorDocument.ParseComplete.WaitOne ();
					if (!context.CapturedArgs.GeneratorResults.Success)
						GetRazorErrors (context, errors);
				}
			}

			ParseHtmlDocument (context, errors);
			CreateCSharpParsedDocument (context);
			context.ClearLastTextChange ();

			RazorHostKind kind = RazorHostKind.WebPage;
			if (context.EditorParser.Host is WebCodeRazorHost) {
				kind = RazorHostKind.WebCode;
			} else if (context.EditorParser.Host is MonoDevelop.AspNet.Razor.Generator.PreprocessedRazorHost) {
				kind = RazorHostKind.Template;
			}

			// var model = context.AnalysisDocument.GetSemanticModelAsync (cancellationToken).Result;
			var pageInfo = new RazorCSharpPageInfo () {
				HtmlRoot = context.HtmlParsedDocument,
				GeneratorResults = context.CapturedArgs.GeneratorResults,
				Spans = context.EditorParser.CurrentParseTree.Flatten (),
				CSharpSyntaxTree = context.ParsedSyntaxTree,
				ParsedDocument = new DefaultParsedDocument ("generated.cs") { /* Ast = model */},
				AnalysisDocument = context.AnalysisDocument,
				CSharpCode = context.CSharpCode,
				Errors = errors,
				FoldingRegions = GetFoldingRegions (context),
				Comments = context.Comments,
				HostKind = kind,
			};

			return System.Threading.Tasks.Task.FromResult((ParsedDocument)new RazorCSharpParsedDocument (context.FileName, pageInfo));
		}

		bool TryAddDocument (string fileName, out OpenRazorDocument currentDocument)
		{
			currentDocument = null;
			if (string.IsNullOrEmpty (fileName))
				return false;

			var guiDoc = IdeApp.Workbench.GetDocument (fileName);
			if (guiDoc != null && guiDoc.Editor != null) {
				currentDocument = new OpenRazorDocument (guiDoc.Editor);
				lock (this) {
					var newDocs = new List<OpenRazorDocument> (openDocuments);
					newDocs.Add (currentDocument);
					openDocuments = newDocs;
				}
				var closedDocument = currentDocument;
				guiDoc.Closed += (sender, args) =>
				{
					var doc = (Ide.Gui.Document)sender;
					if (doc.Editor != null) {
						lock (this) {
							openDocuments = new List<OpenRazorDocument> (openDocuments.Where (d => d.FileName != doc.Editor.FileName));
						}
					}

					TryDisposingDocument (closedDocument);
					closedDocument = null;
				};
				return true;
			}
			return false;
		}

		void TryDisposingDocument (OpenRazorDocument document)
		{
			if (Monitor.TryEnter (document)) {
				try {
					document.Dispose ();
				} finally {
					Monitor.Exit (document);
				}
			} else {
				lock (this) {
					documentsPendingDispose.Add (document);
				}
			}
		}

		void EnsureParserInitializedFor (RazorCSharpParserContext context)
		{
			if (context.EditorParser != null)
				return;

			CreateParserFor (context);
		}

		void CreateParserFor (RazorCSharpParserContext context)
		{
			context.EditorParser = new MonoDevelop.Web.Razor.EditorParserFixed.RazorEditorParser (CreateRazorHost (context), context.FileName);

			context.RazorDocument.ParseComplete = new AutoResetEvent (false);
			context.EditorParser.DocumentParseComplete += (sender, args) =>
			{
				context.RazorDocument.CapturedArgs = args;
				context.RazorDocument.ParseComplete.Set ();
			};
		}

		static RazorEngineHost CreateRazorHost (RazorCSharpParserContext context)
		{
			if (context.Project != null) {
				var projectFile = context.Project.GetProjectFile (context.FileName);
				if (projectFile != null && projectFile.Generator == "RazorTemplatePreprocessor") {
					return new MonoDevelop.AspNet.Razor.Generator.PreprocessedRazorHost (context.FileName) {
						DesignTimeMode = true,
						EnableLinePragmas = false,
					};
				}
			}

			string virtualPath = "~/Views/Default.cshtml";
			if (context.AspProject != null)
				virtualPath = context.AspProject.LocalToVirtualPath (context.FileName);

			WebPageRazorHost host = null;

			// Try to create host using web.config file
			var webConfigMap = new WebConfigurationFileMap ();
			if (context.AspProject != null) {
				var vdm = new VirtualDirectoryMapping (context.AspProject.Project.BaseDirectory.Combine ("Views"), true, "web.config");
			webConfigMap.VirtualDirectories.Add ("/", vdm);
			}
			Configuration configuration;
			try {
				configuration = WebConfigurationManager.OpenMappedWebConfiguration (webConfigMap, "/");
			} catch {
				configuration = null;
			}
			if (configuration != null) {
				//TODO: use our assemblies, not the project's
				var rws = configuration.GetSectionGroup (RazorWebSectionGroup.GroupName) as RazorWebSectionGroup;
				if (rws != null) {
					host = WebRazorHostFactory.CreateHostFromConfig (rws, virtualPath, context.FileName);
					host.DesignTimeMode = true;
				}
			}

			if (host == null) {
				host = new MvcWebPageRazorHost (virtualPath, context.FileName) { DesignTimeMode = true };
				// Add default namespaces from Razor section
				host.NamespaceImports.Add ("System.Web.Mvc");
				host.NamespaceImports.Add ("System.Web.Mvc.Ajax");
				host.NamespaceImports.Add ("System.Web.Mvc.Html");
				host.NamespaceImports.Add ("System.Web.Routing");
			}

			return host;
		}

		static System.Web.Razor.Text.TextChange CreateTextChange (RazorCSharpParserContext context, SeekableTextReader source)
		{
			ChangeInfo lastChange = context.GetLastTextChange ();
			if (lastChange == null)
				return new System.Web.Razor.Text.TextChange (0, 0, new SeekableTextReader (String.Empty), 0, source.Length, source);
			if (lastChange.DeleteChange)
				return new System.Web.Razor.Text.TextChange (lastChange.StartOffset, lastChange.AbsoluteLength, lastChange.Buffer,
					lastChange.StartOffset,	0, source);
			return new System.Web.Razor.Text.TextChange (lastChange.StartOffset, 0, lastChange.Buffer, lastChange.StartOffset,
				lastChange.AbsoluteLength, source);
		}

		static void GetRazorErrors (RazorCSharpParserContext context, List<Error> errors)
		{
			foreach (var error in context.CapturedArgs.GeneratorResults.ParserErrors) {
				int off = error.Location.AbsoluteIndex;
				if (error.Location.CharacterIndex > 0 && error.Length == 1)
					off--;
				errors.Add (new Error (ErrorType.Error, error.Message, context.Document.OffsetToLocation (off)));
			}
		}

		static void ParseHtmlDocument (RazorCSharpParserContext context, List<Error> errors)
		{
			var sb = new StringBuilder ();
			var spanList = new List<Span> ();
			context.Comments = new List<Comment> ();

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
						comment.Region = new MonoDevelop.Ide.Editor.DocumentRegion (
							context.Document.OffsetToLocation (span.Start.AbsoluteIndex - comment.OpenTag.Length),
							context.Document.OffsetToLocation (span.Start.AbsoluteIndex + span.Length + comment.ClosingTag.Length));
						context.Comments.Add (comment);
					}
				}
			};

			context.EditorParser.CurrentParseTree.Accept (new CallbackVisitor (action));

			var parser = new MonoDevelop.Xml.Parser.XmlParser (new WebFormsRootState (), true);

			try {
				parser.Parse (new StringReader (sb.ToString ()));
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error parsing html in Razor document '" + (context.FileName ?? "") + "'", ex);
			}

			context.HtmlParsedDocument = parser.Nodes.GetRoot ();
			errors.AddRange (parser.Errors);
		}

		static IEnumerable<FoldingRegion> GetFoldingRegions (RazorCSharpParserContext context)
		{
			var foldingRegions = new List<FoldingRegion> ();
			GetHtmlFoldingRegions (context, foldingRegions);
			GetRazorFoldingRegions (context, foldingRegions);
			return foldingRegions;
		}

		static void GetHtmlFoldingRegions (RazorCSharpParserContext context, List<FoldingRegion> foldingRegions)
		{
			if (context.HtmlParsedDocument != null) {
				var d = new MonoDevelop.AspNet.WebForms.WebFormsParsedDocument (null, WebSubtype.Html, null, context.HtmlParsedDocument);
				foldingRegions.AddRange (d.Foldings);
			}
		}

		static void GetRazorFoldingRegions (RazorCSharpParserContext context, List<FoldingRegion> foldingRegions)
		{
			var blocks = new List<Block> ();
			GetBlocks (context.EditorParser.CurrentParseTree, blocks);
			foreach (var block in blocks) {
				var beginLine = context.Document.GetLineByOffset (block.Start.AbsoluteIndex);
				var endLine = context.Document.GetLineByOffset (block.Start.AbsoluteIndex + block.Length);
				if (beginLine != endLine)
					foldingRegions.Add (new FoldingRegion (RazorUtils.GetShortName (block),
						new DocumentRegion (context.Document.OffsetToLocation (block.Start.AbsoluteIndex),
							context.Document.OffsetToLocation (block.Start.AbsoluteIndex + block.Length))));
			}
		}

		static void GetBlocks (Block root, IList<Block> blocks)
		{
			foreach (var block in root.Children.Where (n => n.IsBlock).Select (n => n as Block)) {
				if (block.Type != BlockType.Comment && block.Type != BlockType.Markup)
					blocks.Add (block);
				if (block.Type != BlockType.Helper)
					GetBlocks (block, blocks);
			}
		}

		static void CreateCSharpParsedDocument (RazorCSharpParserContext context)
		{
			if (context.Project == null)
				return;

			context.CSharpCode = CreateCodeFile (context);
			context.ParsedSyntaxTree = CSharpSyntaxTree.ParseText (Microsoft.CodeAnalysis.Text.SourceText.From (context.CSharpCode));

			var originalProject = TypeSystemService.GetCodeAnalysisProject (context.Project);
			if (originalProject != null) {
				string fileName = context.FileName + ".g.cs";
				var documentId = TypeSystemService.GetDocumentId (originalProject.Id, fileName);
				if (documentId == null) {
					context.AnalysisDocument = originalProject.AddDocument (
						fileName,
						context.ParsedSyntaxTree?.GetRoot ());
				} else {
					context.AnalysisDocument = TypeSystemService.GetCodeAnalysisDocument (documentId);
				}
			}
		}

		static string CreateCodeFile (RazorCSharpParserContext context)
		{
			var unit = context.CapturedArgs.GeneratorResults.GeneratedCode;
			System.CodeDom.Compiler.CodeDomProvider provider = context.Project != null
				? context.Project.LanguageBinding.GetCodeDomProvider ()
				: new Microsoft.CSharp.CSharpCodeProvider ();
			using (var sw = new StringWriter ()) {
				provider.GenerateCodeFromCompileUnit (unit, sw, new System.CodeDom.Compiler.CodeGeneratorOptions ()	{
					// HACK: we use true, even though razor uses false, to work around a mono bug where it omits the 
					// line ending after "#line hidden", resulting in the unparseable "#line hiddenpublic"
					BlankLinesBetweenMembers = true,
					// matches Razor built-in settings
					IndentString = String.Empty,
				});
				return sw.ToString ();
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
