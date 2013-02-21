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

using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharpBinding;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.AspNet.Mvc;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.AspNet.Mvc.Parser;
using System.IO;
using MonoDevelop.AspNet.Mvc.Gui;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.Completion;

namespace UnitTests.MonoDevelop.AspNet.Mvc.Completion
{
	//largely copied from MonoDevelop.AspNet.Tests.AspNetTesting

	public static class RazorCompletionTesting
	{
		static string extension = ".cshtml";

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

			int triggerWordLength = 1;
			var ctx = textEditorCompletion.GetCodeCompletionContext (isInCSharpContext, sev);

			if (isCtrlSpace)
				return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
			else
				return textEditorCompletion.HandleCodeCompletion (ctx, editorText[cursorPosition - 1], ref triggerWordLength) as CompletionDataList;
		}

		public static IParameterDataProvider CreateProvider (string text)
		{
			string editorText;
			TestViewContent sev;

			var textEditorCompletion = CreateEditor (text, true, out editorText, out sev);
			int cursorPosition = text.IndexOf ('$');

			var ctx = textEditorCompletion.GetCodeCompletionContext (true, sev);
			return textEditorCompletion.HandleParameterCompletion (ctx, editorText[cursorPosition - 1]);
		}

		static RazorTestingEditorExtension CreateEditor (string text, bool isInCSharpContext, out string editorText,
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

			var project = new AspMvc3Project ("C#");
			project.FileName = UnitTests.TestBase.GetTempFile (".csproj");
			string file = UnitTests.TestBase.GetTempFile (extension);
			project.AddFile (file);

			var pcw = TypeSystemService.LoadProject (project);
			TypeSystemService.ForceUpdate (pcw);
			pcw.ReconnectAssemblyReferences ();

			sev = new TestViewContent ();
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;

			var tww = new TestWorkbenchWindow ();
			tww.ViewContent = sev;

			var doc = new Document (tww);
			var parser = new RazorTestingParser ()	{
				Doc = doc
			};
			var parsedDoc = parser.Parse (false, sev.ContentName, new StringReader (parsedText), project);

			return new RazorTestingEditorExtension (doc, parsedDoc as RazorCSharpParsedDocument, isInCSharpContext);
		}
	}

	public class RazorTestingParser : RazorCSharpParser
	{
		public Document	Doc { get; set; }

		public override ParsedDocument Parse (bool storeAst, string fileName, System.IO.TextReader content, Project project = null)
		{
			Doc.Editor.Document.FileName = fileName;
			OpenDocuments.Add (Doc.Editor.Document);
			return base.Parse (storeAst, fileName, content, project);
		}
	}

	public class RazorTestingEditorExtension : RazorCSharpEditorExtension
	{
		public RazorTestingEditorExtension (Document doc, RazorCSharpParsedDocument parsedDoc, bool cSharpContext)
		{
			razorDocument = parsedDoc;
			Initialize (doc);
			if (cSharpContext) {
				InitializeCodeCompletion ();
				SwitchToHidden ();
			}
		}

		public CodeCompletionContext GetCodeCompletionContext (bool cSharpContext, TestViewContent sev)
		{
			var ctx = new CodeCompletionContext ();
			if (!cSharpContext)
				ctx.TriggerOffset = sev.CursorPosition;
			else
				ctx.TriggerOffset = hiddenInfo.UnderlyingDocument.Editor.Caret.Offset;

			int line, column;
			sev.GetLineColumnFromPosition (ctx.TriggerOffset, out line, out column);
			ctx.TriggerLine = line;
			ctx.TriggerLineOffset = column - 1;

			return ctx;
		}
	}
}
