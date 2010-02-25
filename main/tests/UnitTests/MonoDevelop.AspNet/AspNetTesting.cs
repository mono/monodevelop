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
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.AspNet.Tests
{

	//largely copied from MonoDevelop.CSharpBinding.Tests.CodeCompletionBugTests
	public static class AspNetTesting
	{
		static int pcount = 0;
		
		public static CompletionDataList CreateAspxCtrlSpaceProvider (string text)
		{
			return CreateProvider (text, ".aspx", true);
		}
		
		public static CompletionDataList CreateProvider (string text, string extension, bool isCtrlSpace)
		{
			string parsedText;
			string editorText;
			int cursorPosition = text.IndexOf ('$');
			int endPos = text.IndexOf ('$', cursorPosition + 1);
			if (endPos == -1)
				parsedText = editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			else {
				parsedText = text.Substring (0, cursorPosition) + new string (' ', endPos - cursorPosition) + text.Substring (endPos + 1);
				editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring (endPos + 1);
				cursorPosition = endPos - 1; 
			}
			var tww = new MonoDevelop.CSharpBinding.Tests.TestWorkbenchWindow ();
			var sev = new MonoDevelop.CSharpBinding.Tests.TestViewContent ();
			var project = new AspNetAppProject ("C#");
			project.FileName = UnitTests.TestBase.GetTempFile (".csproj");
			
			string file = UnitTests.TestBase.GetTempFile (extension);
			project.AddFile (file);
			
			ProjectDomService.Load (project);
			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			dom.ForceUpdate (true);
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;
			tww.ViewContent = sev;
			var doc = new MonoDevelop.Ide.Gui.Document (tww);
			doc.ParsedDocument = new MonoDevelop.AspNet.Parser.AspNetParser ().Parse (null, sev.ContentName, parsedText);
			foreach (var e in doc.ParsedDocument.Errors)
				Console.WriteLine (e);
			
			var textEditorCompletion = new MonoDevelop.AspNet.Gui.AspNetEditorExtension ();
			Initialize (textEditorCompletion, doc);
			
			int triggerWordLength = 1;
			CodeCompletionContext ctx = new CodeCompletionContext ();
			ctx.TriggerOffset = sev.CursorPosition;
			int line, column;
			sev.GetLineColumnFromPosition (sev.CursorPosition, out line, out column);
			ctx.TriggerLine = line;
			ctx.TriggerLineOffset = column;
			
			if (isCtrlSpace)
				return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
			else
				return textEditorCompletion.HandleCodeCompletion (ctx, editorText[cursorPosition - 1] , ref triggerWordLength) as CompletionDataList;
		}
		
		static void Initialize (MonoDevelop.Ide.Gui.Content.TextEditorExtension extension, MonoDevelop.Ide.Gui.Document doc)
		{
			var meth = typeof (MonoDevelop.Ide.Gui.Content.TextEditorExtension)
				.GetMethod ("Initialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
				            null, new Type [] { typeof (MonoDevelop.Ide.Gui.Document) }, null);
			meth.Invoke (extension, new object [] { doc });
		}
	}
}
