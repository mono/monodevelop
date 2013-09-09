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

using System;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.AspNet.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CSharpBinding;
using MonoDevelop.Ide.Gui;
using MonoDevelop.AspNet.Parser;
using System.IO;

namespace MonoDevelop.AspNet.Tests
{
	//largely copied from RazorCompletionTesting
	public static class AspNetTesting
	{
		public static CompletionDataList CreateProvider (string text, string extension, bool isCtrlSpace = false)
		{
			string editorText;
			TestViewContent sev;

			var textEditorCompletion = CreateEditor (text, extension, out editorText, out sev);
			int cursorPosition = text.IndexOf ('$');

			int triggerWordLength = 1;
			var ctx = textEditorCompletion.GetCodeCompletionContext (sev);

			if (isCtrlSpace)
				return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
			else
				return textEditorCompletion.HandleCodeCompletion (ctx, editorText[cursorPosition - 1], ref triggerWordLength) as CompletionDataList;
		}


		static AspNetTestingEditorExtension CreateEditor (string text, string extension, out string editorText, out TestViewContent sev)
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

			var project = new AspNetAppProject ("C#");
			project.References.Add (new ProjectReference (ReferenceType.Package, "System"));
			project.References.Add (new ProjectReference (ReferenceType.Package, "System.Web"));
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

			var doc = new TestDocument (tww);
			doc.Editor.Document.FileName = sev.ContentName;
			var parser = new AspNetParser ();
			var parsedDoc = (AspNetParsedDocument) parser.Parse (false, sev.ContentName, new StringReader (parsedText), project);
			doc.HiddenParsedDocument = parsedDoc;

			return new AspNetTestingEditorExtension (doc);
		}

		public class AspNetTestingEditorExtension : AspNetEditorExtension
		{
			public AspNetTestingEditorExtension (Document doc)
			{
				Initialize (doc);
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
