//
// ParameterCompletionTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;
using MonoDevelop.CSharpBinding.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture()]
	public class ParameterCompletionTests
	{
		public static IParameterDataProvider CreateProvider (string text)
		{
			int cursorPosition = text.IndexOf ('$');
			string parsedText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
			DotNetProject project = new DotNetProject ("C#");
			project.FileName = "/tmp/a.csproj";
			
			SimpleProjectDom dom = new SimpleProjectDom ();
			dom.Project = project;
			ProjectDomService.RegisterDom (dom, "Project:" + project.FileName);
			
			sev.Project = project;
			sev.ContentName = "a.cs";
			sev.Text = parsedText;
			sev.CursorPosition = cursorPosition;
			tww.ViewContent = sev;
			Document doc = new Document (tww);
			doc.ParsedDocument = new NRefactoryParser ().Parse (sev.ContentName, sev.Text);
			dom.Add (doc.CompilationUnit);
			CSharpTextEditorCompletion textEditorCompletion = new CSharpTextEditorCompletion (doc);
			
			int triggerWordLength = 1;
			CodeCompletionContext ctx = new CodeCompletionContext ();
			ctx.TriggerOffset = sev.CursorPosition;
			 
			IParameterDataProvider result =  textEditorCompletion.HandleParameterCompletion (ctx, text[cursorPosition - 1]);
			return result;
		}
		
		/// <summary>
		/// Bug 427448 - Code Completion: completion of constructor parameters not working
		/// </summary>
		[Test()]
		public void TestBug427448 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Test
{
	public Test (int a)
	{
	}
	
	public Test (string b)
	{
	}
}

class AClass
{
	void A()
	{
		Test t = new Test ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.OverloadCount);
		}
	}
}
