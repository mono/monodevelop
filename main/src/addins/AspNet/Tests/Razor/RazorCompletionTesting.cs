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

		public static CompletionDataList CreateRazorCtrlSpaceProvider (string text, bool isInCSharpContext)
		{
			return CreateProvider (text, isInCSharpContext, true);
		}

		public static CompletionDataList CreateProvider (string text, bool isInCSharpContext = false, bool isCtrlSpace = false)
		{
			string editorText;
			TestViewContent sev;

			var textEditorCompletion = CreateEditor (text, isInCSharpContext, out editorText, out sev);
			int cursorPosition = text.IndexOf ('$');

			var ctx = GetCodeCompletionContext (isInCSharpContext, sev, textEditorCompletion.hiddenInfo.UnderlyingDocument);

			if (isCtrlSpace) {
				var result = textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
				TypeSystemServiceTestExtensions.UnloadSolution (solution);
				return result;
			} else {
				var task = textEditorCompletion.HandleCodeCompletionAsync (ctx, editorText [cursorPosition - 1], default(CancellationToken));
				TypeSystemServiceTestExtensions.UnloadSolution (solution);
				if (task != null) {
					return task.Result as CompletionDataList;
				}
				return null;
			}
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

		public static ParameterHintingResult CreateParameterProvider (string text)
		{
			string editorText;
			TestViewContent sev;

			var textEditorCompletion = CreateEditor (text, true, out editorText, out sev);
			int cursorPosition = text.IndexOf ('$');

			var ctx = GetCodeCompletionContext (true, sev, textEditorCompletion.hiddenInfo.UnderlyingDocument);
			var task = textEditorCompletion.HandleParameterCompletionAsync (ctx, editorText[cursorPosition - 1], default(CancellationToken));
			if (task != null) {
				return task.Result;
			}
			return null;
		}

		static Solution solution;

		static RazorCSharpEditorExtension CreateEditor (string text, bool isInCSharpContext, out string editorText,
			out TestViewContent sev)
		{
			string parsedText;
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

			sev = new TestViewContent ();
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
			TypeSystemServiceTestExtensions.LoadSolution (solution);

			var parser = new RazorTestingParser {
				Doc = doc
			};
			var options = new ParseOptions {
				Project = project,
				FileName = sev.ContentName,
				Content = new StringTextSource (parsedText)
			};
			var parsedDoc = (RazorCSharpParsedDocument)parser.Parse (options, default(CancellationToken)).Result;
			doc.HiddenParsedDocument = parsedDoc;

			var editorExtension = new RazorCSharpEditorExtension (doc, parsedDoc as RazorCSharpParsedDocument, isInCSharpContext);
			return editorExtension;
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
