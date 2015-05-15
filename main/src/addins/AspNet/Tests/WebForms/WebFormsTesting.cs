//
// AspNetTesting.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.AspNet.Projects;
using MonoDevelop.AspNet.WebForms;
using MonoDevelop.Core.Text;
using MonoDevelop.CSharpBinding;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNet.Tests.WebForms
{
	//largely copied from RazorCompletionTesting
	static class WebFormsTesting
	{
		public static CompletionDataList CreateProvider (string text, string extension, bool isCtrlSpace = false)
		{
			string editorText;
			TestViewContent sev;

			var textEditorCompletion = CreateEditor (text, extension, out editorText, out sev);
			int cursorPosition = text.IndexOf ('$');

			var ctx = textEditorCompletion.GetCodeCompletionContext (sev);

			if (isCtrlSpace)
				return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
			else {
				var task = textEditorCompletion.HandleCodeCompletionAsync (ctx, editorText [cursorPosition - 1]);
				if (task != null) {
					return task.Result as CompletionDataList;
				}
				return null;
			}
		}

		static WebFormsTestingEditorExtension CreateEditor (string text, string extension, out string editorText, out TestViewContent sev)
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

			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.References.Add (ProjectReference.CreateAssemblyReference ("System"));
			project.References.Add (ProjectReference.CreateAssemblyReference ("System.Web"));
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
			var parser = new WebFormsParser ();
			var options = new ParseOptions {
				Project = project,
				FileName = sev.ContentName,
				Content = new StringTextSource (parsedText)
			};
			var parsedDoc = (WebFormsParsedDocument)parser.Parse (options, default(CancellationToken)).Result;
			doc.HiddenParsedDocument = parsedDoc;

			return new WebFormsTestingEditorExtension (doc);
		}

		public class WebFormsTestingEditorExtension : WebFormsEditorExtension
		{
			public WebFormsTestingEditorExtension (Document doc)
			{
				Initialize (doc.Editor, doc);
			}

			public CodeCompletionContext GetCodeCompletionContext (TestViewContent sev)
			{
				var ctx = new CodeCompletionContext ();
				ctx.TriggerOffset = sev.CursorPosition;

				int line, column;
				sev.GetLineColumnFromPosition (ctx.TriggerOffset, out line, out column);
				ctx.TriggerLine = line;
				ctx.TriggerLineOffset = column - 1;

				return ctx;
			}
		}
	}
}
