//
// CodeCompletionTests.cs
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
	public class CodeCompletionTests : UnitTests.TestBase
	{
		public static CompletionDataList CreateProvider (string text)
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
			
			return textEditorCompletion.HandleCodeCompletion (ctx, text[cursorPosition - 1] , ref triggerWordLength) as CompletionDataList;
		}

		public static CompletionDataList CreateCtrlSpaceProvider (string text)
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
			
			return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
		}
		
		[Test()]
		public void TestSimpleCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (3, provider.Count);
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}
		[Test()]
		public void TestSimpleInterfaceCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"interface ITest { void TM1 (); void TM2 (); int TF1 { get; } }
class CCTest {
void TestMethod ()
{
	ITest t;
	t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (3, provider.Count);
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		/// <summary>
		/// Bug 399695 - Code completion not working with an enum in a different namespace
		/// </summary>
		[Test()]
		public void TestBug399695 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace Other { enum TheEnum { One, Two } }
namespace ThisOne { 
        public class Test {
                public Other.TheEnum TheEnum {
                        set { }
                }

                public void TestMethod () {
                        TheEnum = $
                }
        }
}");
			Assert.IsNotNull (provider);
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "Other.TheEnum not found.");
		}

		/// <summary>
		/// Bug 318834 - autocompletion kicks in when inputting decimals
		/// </summary>
		[Test()]
		public void TestBug318834 ()
		{
			CompletionDataList provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                decimal foo = 0.$
        }
}

");
			Assert.IsNull (provider);
		}

		/// <summary>
		/// Bug 321306 - Code completion doesn't recognize child namespaces
		/// </summary>
		[Test()]
		public void TestBug321306 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace a
{
	namespace b
	{
		public class c
		{
			public static int hi;
		}
	}
	
	public class d
	{
		public d ()
		{
			b.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("c"), "class 'c' not found.");
		}

		/// <summary>
		/// Bug 322089 - Code completion for indexer
		/// </summary>
		[Test()]
		public void TestBug322089 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		AClass[] list = new AClass[0];
		list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 323283 - Code completion for indexers offered by generic types (generics)
		/// </summary>
		[Test()]
		public void TestBug323283 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class MyClass<T>
{
	public T this[int i] {
		get {
			return default (T);
		}
	}
}

class Test
{
	public void TestMethod ()
	{
		MyClass<AClass> list = new MyClass<AClass> ();
		list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}

		/// <summary>
		/// Bug 323317 - Code completion not working just after a constructor
		/// </summary>
		[Test()]
		public void TestBug323317 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		new AClass().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 325509 - Inaccessible methods displayed in autocomplete
		/// </summary>
		[Test()]
		public void TestBug325509 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int A;
	public int B;
	
	protected int C;
	int D;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("A"), "field 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "field 'B' not found.");
		}

		/// <summary>
		/// Bug 338392 - MD tries to use types when declaring namespace
		/// </summary>
		[Test()]
		public void TestBug338392 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A
{
        class C
        {
        }
}

namespace A.$
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (0, provider.Count);
		}

		/// <summary>
		/// Bug 427284 - Code Completion: class list shows the full name of classes
		/// </summary>
		[Test()]
		public void TestBug427284 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace TestNamespace
{
        class Test
        {
        }
}
class TestClass
{
	void Method ()
	{
		TestNamespace.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}

		/// <summary>
		/// Bug 427294 - Code Completion: completion on values returned by methods doesn't work
		/// </summary>
		[Test()]
		public void TestBug427294 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass GetTestClass ()
	{
	}
}

class Test
{
	public void TestMethod ()
	{
		TestClass a;
		a.GetTestClass ().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("GetTestClass"), "method 'GetTestClass' not found.");
		}
		
		/// <summary>
		/// Bug 405000 - Namespace alias qualifier operator (::) does not trigger code completion
		/// </summary>
		[Test()]
		public void TestBug405000 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A {
	class Test
	{
	}
}

namespace B {
	using foo = A;
	class C
	{
		public static void Main ()
		{
			foo::$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 427649 - Code Completion: protected methods shown in code completion
		/// </summary>
		[Test()]
		public void TestBug427649 ()
		{
			CompletionDataList provider = CreateProvider (
@"class BaseClass
{
	protected void ProtecedMember ()
	{
	}
}

class C : BaseClass
{
	public static void Main ()
	{
		BaseClass bc;
		bc.$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734A ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		Test.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("SomeEnum"), "enum 'SomeEnum' not found.");
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		SomeEnum.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("a"), "enum member 'a' not found.");
			Assert.IsNotNull (provider.Find ("b"), "enum member 'b' not found.");
		}
		
		/// <summary>
		/// Bug 431764 - Completion doesn't work in properties
		/// </summary>
		[Test()]
		public void TestBug431764 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	int number;
	public int Number {
		set { this.number = $ }
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("value"), "Should contain 'value'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797A ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	private List<string> strings;
	public $
}");
		
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("strings"), "should not contain 'strings'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public delegate string [] AutoCompleteHandler (string text, int pos);
	public void Method ()
	{
		Test t = new Test ();
		t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("AutoCompleteHandler"), "should not contain 'AutoCompleteHandler' delegate");
		}
		
		/// <summary>
		/// Bug 432681 - Incorrect completion in nested classes
		/// </summary>
		[Test()]
		public void TestBug432681 ()
		{
			CompletionDataList provider = CreateProvider (
@"

class C {
        public class D
        {
        }

        public void Method ()
        {
                C.D c = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("C.D", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		[Test()]
		public void TestGenericObjectCreation ()
		{
			CompletionDataList provider = CreateProvider (
@"
class List<T>
{
}
class Test{
	public void Method ()
	{
		List<int> i = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Find ("List<int>") != null, "List<int> not found");
		}
		
		/// <summary>
		/// Bug 431803 - Autocomplete not giving any options
		/// </summary>
		[Test()]
		public void TestBug431803 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public string[] GetStrings ()
	{
		return new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("string[]"), "type string not found.");
		}

		/// <summary>
		/// Bug 434770 - No autocomplete on array types
		/// </summary>
		[Test()]
		public void TestBug434770 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace System {
	public class Array 
	{
		public int Length {
			get {}
			set {}
		}
		public int MyField;
	}
}
public class Test
{
	public void AMethod ()
	{
		byte[] buffer = new byte[1024];
		buffer.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Length"), "property 'Length' not found.");
			Assert.IsNotNull (provider.Find ("MyField"), "field 'MyField' not found.");
		}
		
		
		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
		
	}
}
