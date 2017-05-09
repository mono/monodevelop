//
// RazorCompletionTesting.cs
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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.Razor;
using MonoDevelop.AspNet.Razor.Parser;
using MonoDevelop.Core.Text;
using MonoDevelop.CSharpBinding;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet.Tests.Razor
{
	//largely copied from MonoDevelop.AspNet.Tests.AspNetTesting

	static class RazorCompletionTesting
	{
// TODO: Roslyn port
		static readonly string extension = ".cshtml";

		public static Task<CompletionDataList> CreateRazorCtrlSpaceProvider (string text, bool isInCSharpContext)
		{
			return CreateProvider (text, isInCSharpContext, true);
		}

		public static async Task<CompletionDataList> CreateProvider (string text, bool isInCSharpContext = false, bool isCtrlSpace = false)
		{
			var ed = await CreateEditor (text, isInCSharpContext);
			int cursorPosition = text.IndexOf ('$');

			var ctx = GetCodeCompletionContext (isInCSharpContext, ed.View, ed.Extension.hiddenInfo.UnderlyingDocument);

			Task<ICompletionDataList> task;
			if (isCtrlSpace) {
				task = ed.Extension.HandleCodeCompletionAsync (ctx, CompletionTriggerInfo.CodeCompletionCommand, default (CancellationToken));
			} else {
				task = ed.Extension.HandleCodeCompletionAsync (ctx, new CompletionTriggerInfo (CompletionTriggerReason.CharTyped, ed.EditorText [cursorPosition - 1]), default (CancellationToken));
			}

			TypeSystemServiceTestExtensions.UnloadSolution (solution);
			if (task != null) {
				return await task as CompletionDataList;
			}
			return null;
		}

		static CodeCompletionContext GetCodeCompletionContext (bool cSharpContext, TestViewContent sev, UnderlyingDocument underlyingDocument)
		{
			var ctx = new CodeCompletionContext ();
			if (!cSharpContext)
				ctx.TriggerOffset = sev.CursorPosition;
			else
				ctx.TriggerOffset = underlyingDocument.Editor.CaretOffset;

			int line, column;
			sev.GetLineColumnFromPosition (ctx.TriggerOffset, out line, out column);
			ctx.TriggerLine = line;
			ctx.TriggerLineOffset = column - 1;

			return ctx;
		}

		public static async Task<ParameterHintingResult> CreateParameterProvider (string text)
		{
			var ed = await CreateEditor (text, true);

			int cursorPosition = text.IndexOf ('$');

			var ctx = GetCodeCompletionContext (true, ed.View, ed.Extension.hiddenInfo.UnderlyingDocument);
			var task = ed.Extension.HandleParameterCompletionAsync (ctx, ed.EditorText[cursorPosition - 1], default(CancellationToken));
			if (task != null) {
				return await task;
			}
			return null;
		}

		static Solution solution;

		class EditorInfo
		{
			public RazorCSharpEditorExtension Extension;
			public string EditorText;
			public TestViewContent View;
		}

		static async Task<EditorInfo> CreateEditor (string text, bool isInCSharpContext)
		{
			string parsedText, editorText;
			int cursorPosition = text.IndexOf ('$');
			int endPos = text.IndexOf ('$', cursorPosition + 1);
			if (endPos == -1)
				parsedText = editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			else {
				parsedText = text.Substring (0, cursorPosition) + new string (' ', endPos - cursorPosition) + text.Substring (endPos + 1);
				editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring (endPos + 1);
				cursorPosition = endPos - 1;
			}

			var project = Services.ProjectService.CreateProject ("C#", "AspNetApp");

			project.FileName = UnitTests.TestBase.GetTempFile (".csproj");
			string file = UnitTests.TestBase.GetTempFile (extension);
			project.AddFile (file);

			var sev = new TestViewContent ();
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;

			var tww = new TestWorkbenchWindow ();
			tww.ViewContent = sev;

			var doc = new TestDocument (tww);
			doc.Editor.FileName = sev.ContentName;
			doc.UpdateProject (project);

			solution = new MonoDevelop.Projects.Solution ();
			solution.DefaultSolutionFolder.AddItem (project);
			solution.AddConfiguration ("", true);
			await TypeSystemServiceTestExtensions.LoadSolution (solution);

			var parser = new RazorTestingParser {
				Doc = doc
			};
			var options = new ParseOptions {
				Project = project,
				FileName = sev.ContentName,
				Content = new StringTextSource (parsedText)
			};
			var parsedDoc = await parser.Parse (options, default(CancellationToken)) as RazorCSharpParsedDocument;
			doc.HiddenParsedDocument = parsedDoc;

			var editorExtension = new RazorCSharpEditorExtension (doc, parsedDoc as RazorCSharpParsedDocument, isInCSharpContext);
			return new EditorInfo {
				Extension = editorExtension,
				EditorText = editorText,
				View = sev
			};
		}
	}

	public class RazorTestingParser : RazorCSharpParser
	{
		public Document	Doc { get; set; }

		public override System.Threading.Tasks.Task<ParsedDocument> Parse (ParseOptions parseOptions, System.Threading.CancellationToken cancellationToken)
		{
			Doc.Editor.FileName = parseOptions.FileName;
			OpenDocuments.Add (new OpenRazorDocument (Doc.Editor));
			return base.Parse (parseOptions, cancellationToken);
		}
	}
}
