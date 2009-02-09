//
// CodeCompletionBugTests.cs
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
	public class CodeCompletionBugTests : UnitTests.TestBase
	{
		static int pcount = 0;
		
		public static CompletionDataList CreateProvider (string text)
		{
			return CreateProvider (text, false);
		}
		
		public static CompletionDataList CreateCtrlSpaceProvider (string text)
		{
			return CreateProvider (text, true);
		}
		
		static CompletionDataList CreateProvider (string text, bool isCtrlSpace)
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
			project.FileName = "/tmp/a" + pcount + ".csproj";
			
			string file = "/tmp/test-file-" + (pcount++) + ".cs";
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
			doc.ParsedDocument = new NRefactoryParser ().Parse (sev.ContentName, parsedText);
			CSharpTextEditorCompletion textEditorCompletion = new CSharpTextEditorCompletion (doc);
			
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
		
		public static void CheckObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "Method 'System.Object.GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("GetType"), "Method 'System.Object.GetType' not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "Method 'System.Object.ToString' not found.");
		}
		
		public static void CheckProtectedObjectMembers (CompletionDataList provider)
		{
			CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("MemberwiseClone"), "Method 'System.Object.MemberwiseClone' not found.");
		}
		
		public static void CheckStaticObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("ReferenceEquals"), "Method 'System.Object.ReferenceEquals' not found.");
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
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
                        $TheEnum = $
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
                $decimal foo = 0.$
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
			$b.$
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
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
		$new AClass().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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

$namespace A.$
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
		$TestNamespace.$
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
		$a.GetTestClass ().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
			$foo::$
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
		$bc.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (4, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
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
		$Test.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (3, provider.Count);
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 from System.Object
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
		$SomeEnum.$
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
		set { $this.number = $ }
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
	$public $
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
		$t.$
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
                $C.D c = new $
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
		$List<int> i = new $
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
		$return new $
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
		$buffer.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Length"), "property 'Length' not found.");
			Assert.IsNotNull (provider.Find ("MyField"), "field 'MyField' not found.");
		}
		
		/// <summary>
		/// Bug 439601 - Intellisense Broken For Partial Classes
		/// </summary>
		[Test()]
		public void TestBug439601 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		private void Foo()
		{
			Bar();
		}
		
		private void Blah()
		{
			Foo();
		}
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		public FormMain()
		{
		}
		
		private void Bar()
		{
			$this.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Blah"), "method 'Blah' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 432434 - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434 ()
		{
			CompletionDataList provider = CreateProvider (

@"public class Test
{
	public class Inner
	{
		public void Inner1 ()
		{
		}
		
		public void Inner2 ()
		{
		}
	}
	
	public void Run ()
	{
		Inner inner = new Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner1"), "Method inner1 not found.");
			Assert.IsNotNull (provider.Find ("Inner2"), "Method inner2 not found.");
		}

		/// <summary>
		/// Bug 432434A - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434A ()
		{
			CompletionDataList provider = CreateProvider (

@"    public class E
        {
                public class Inner
                {
                        public void Method ()
                        {
                                Inner inner = new Inner();
                                $inner.$
                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Method"), "Method 'Method' not found.");
		}
		
		/// <summary>
		/// Bug 432434B - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434B ()
		{
			CompletionDataList provider = CreateProvider (

@"  public class E
        {
                public class Inner
                {
                        public class ReallyInner $: $
                        {

                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("E"), "Class 'E' not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "Class 'Inner' not found.");
			Assert.IsNull (provider.Find ("ReallyInner"), "Class 'ReallyInner' found, but shouldn't.");
		}
		

		/// <summary>
		/// Bug 436705 - code completion for constructors does not handle class name collisions properly
		/// </summary>
		[Test()]
		public void TestBug436705 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Point
{
}

class C {

        public void Method ()
        {
                $System.Drawing.Point p = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("System.Drawing.Point", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		/// <summary>
		/// Bug 439963 - Lacking members in code completion
		/// </summary>
		[Test()]
		public void TestBug439963 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class StaticTest
{
	public void Test1()
	{}
	public void Test2()
	{}
	
	public static StaticTest GetObject ()
	{
	}
}

public class Test
{
	public void TestMethod ()
	{
		$StaticTest.GetObject ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test1"), "Method 'Test1' not found.");
			Assert.IsNotNull (provider.Find ("Test2"), "Method 'Test2' not found.");
			Assert.IsNull (provider.Find ("GetObject"), "Method 'GetObject' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 441671 - Finalisers show up in code completion
		/// </summary>
		[Test()]
		public void TestBug441671 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass (int i)
	{
	}
	public void TestMethod ()
	{
	}
	public ~TestClass ()
	{
	}
}

class AClass
{
	void AMethod ()
	{
		TestClass c;
		$c.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNull (provider.Find (".dtor"), "destructor found - but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		/// <summary>
		/// Bug 444110 - Code completion doesn't activate
		/// </summary>
		[Test()]
		public void TestBug444110 ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;
using System.Collections.Generic;

namespace System.Collections.Generic {
	
	public class TemplateClass<T>
	{
		public T TestField;
	}
}

namespace CCTests
{
	
	public class Test
	{
		public TemplateClass<int> TemplateClass { get; set; }
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			Test t = new Test();
			$t.TemplateClass.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TestField"), "field 'TestField' not found.");
		}
		
		/// <summary>
		/// Bug 460234 - Invalid options shown when typing 'override'
		/// </summary>
		[Test()]
		public void TestBug460234 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace System {
	public class Object
	{
		public virtual int GetHashCode ()
		{
		}
		protected virtual void Finalize ()
		{
		}
	}
}
public class TestMe : System.Object
{
	$override $
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNull (provider.Find ("Finalize"), "method 'Finalize' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "method 'GetHashCode' not found.");
		}

		/// <summary>
		/// Bug 457003 - code completion shows variables out of scope
		/// </summary>
		[Test()]
		public void TestBug457003 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class A
{
	public void Test ()
	{
		if (true) {
			A st = null;
		}
		
		if (true) {
			int i = 0;
			$st.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Count == 0, "variable 'st' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 457237 - code completion doesn't show static methods when setting global variable
		/// </summary>
		[Test()]
		public void TestBug457237 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class Test
{
	public static double Val = 0.5;
}

class Test2
{
	$double dd = Test.$
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Val"), "field 'Val' not found.");
		}

		/// <summary>
		/// Bug 459682 - Static methods/properties don't show up in subclasses
		/// </summary>
		[Test()]
		public void TestBug459682 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class BaseC
{
	public static int TESTER;
}
public class Child : BaseC
{
	public Child()
	{
		$Child.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TESTER"), "field 'TESTER' not found.");
		}
		
		/// <summary>
		/// Bug 466692 - Missing completion for return/break keywords after yield
		/// </summary>
		[Test()]
		public void TestBug466692 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class TestMe 
{
	public int Test ()
	{
		$yield $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("break"), "keyword 'break' not found");
			Assert.IsNotNull (provider.Find ("return"), "keyword 'return' not found");
		}
		
		/// <summary>
		/// Bug 467507 - No completion of base members inside explicit events
		/// </summary>
		[Test()]
		public void TestBug467507 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;

class Test
{
	public void TestMe ()
	{
	}
	
	public event EventHandler TestEvent {
		add {
			$
		}
		remove {
			
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TestMe"), "method 'TestMe' not found");
			Assert.IsNotNull (provider.Find ("value"), "keyword 'value' not found");
		}
		
		/// <summary>
		/// Bug 444643 - Extension methods don't show up on array types
		/// </summary>
		[Test()]
		public void TestBug444643 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

	static class ExtensionTest
	{
		public static bool TestExt<T> (this IList<T> list, T val)
		{
			return true;
		}
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			$args.$
		}
	}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestExt"), "method 'TestExt' not found");
		}
		
		/// <summary>
		/// Bug 471935 - Code completion window not showing in MD1CustomDataItem.cs
		/// </summary>
		[Test()]
		public void TestBug471935 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public class AClass
{
	public AClass Test ()
	{
		if (true) {
			AClass data;
			$data.$
			return data;
		} else if (false) {
			AClass data;
			return data;
		}
		return null;
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 471937 - Code completion of 'new' showing invorrect entries 
		/// </summary>
		[Test()]
		public void TestBug471937 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class B
{
}

class A
{
	public void Test()
	{
		int i = 5;
		i += 5;
		$A a = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn'tj.");
		}

	}
}
