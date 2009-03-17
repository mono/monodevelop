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
	public class ParameterCompletionTests : UnitTests.TestBase
	{
		static int pcount = 0;
		internal static IParameterDataProvider CreateProvider (string text)
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
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
			DotNetProject project = new DotNetProject ("C#");
			project.FileName = "/tmp/ap" + pcount + ".csproj";
			
			string file = "/tmp/test-pfile-" + (pcount++) + ".cs";
			project.AddFile (file);
			
			ProjectDomService.Load (project);
//			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;
			tww.ViewContent = sev;
			Document doc = new Document (tww);
			doc.ParsedDocument = new NRefactoryParser ().Parse (null, sev.ContentName, parsedText);
			CSharpTextEditorCompletion textEditorCompletion = new CSharpTextEditorCompletion (doc);
			
			int triggerWordLength = 1;
			CodeCompletionContext ctx = new CodeCompletionContext ();
			ctx.TriggerOffset = sev.CursorPosition;
			int line, column;
			sev.GetLineColumnFromPosition (sev.CursorPosition, out line, out column);
			ctx.TriggerLine = line;
			ctx.TriggerLineOffset = column;
			
			return textEditorCompletion.HandleParameterCompletion (ctx, editorText[cursorPosition - 1]);
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
	protected Test ()
	{
	}
	Test (double d, float m)
	{
	}
}

class AClass
{
	void A()
	{
		$Test t = new Test ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.OverloadCount);
		}

		/// <summary>
		/// Bug 432437 - No completion when invoking delegates
		/// </summary>
		[Test()]
		public void TestBug432437 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"public delegate void MyDel (int value);

class Test
{
	MyDel d;

	void A()
	{
		$d ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
		}

		/// <summary>
		/// Bug 432658 - Incorrect completion when calling an extension method from inside another extension method
		/// </summary>
		[Test()]
		public void TestBug432658 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"static class Extensions
{
	public static void Ext1 (this int start)
	{
	}
	public static void Ext2 (this int end)
	{
		$Ext1($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount, "There should be one overload");
			Assert.AreEqual (1, provider.GetParameterCount(0), "Parameter 'start' should exist");
		}

		/// <summary>
		/// Bug 432727 - No completion if no constructor
		/// </summary>
		[Test()]
		public void TestBug432727 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class A
{
	void Method ()
	{
		$A aTest = new A ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
		}

		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test()]
		public void TestBug434705 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class Test
{
	public Test (int a)
	{
	}
}

class AClass
{
	Test A()
	{
		$return new Test ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
		}
		
		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test()]
		public void TestBug434705B ()
		{
			IParameterDataProvider provider = CreateProvider (
@"
class Test<T>
{
	public Test (T t)
	{
	}
}
class TestClass
{
	void TestMethod ()
	{
		$Test<int> l = new Test<int> ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
		}
	
		
		/// <summary>
		/// Bug 434701 - No autocomplete in attributes
		/// </summary>
		[Test()]
		public void TestBug434701 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"class TestAttribute : System.Attribute
{
	public Test (int a)
	{
	}
}

$[Test ($
class AClass
{
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
		}
		
		/// <summary>
		/// Bug 447985 - Exception display tip is inaccurate for derived (custom) exceptions
		/// </summary>
		[Test()]
		public void TestBug447985 ()
		{
			IParameterDataProvider provider = CreateProvider (
@"
namespace System {
	public class Exception
	{
		public Exception () {}
	}
}

class MyException : System.Exception
{
	public MyException (int test)
	{}
}

class AClass
{
	public void Test ()
	{
		$throw new MyException($
	}

}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.OverloadCount);
			Assert.AreEqual (1, provider.GetParameterCount(0), "Parameter 'test' should exist");
		}
		
		
	}
}