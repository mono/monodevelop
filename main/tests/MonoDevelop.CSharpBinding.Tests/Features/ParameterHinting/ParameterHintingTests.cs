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

using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Host.Mef;

namespace ICSharpCode.NRefactory6.CSharp.ParameterHinting
{
	[Ignore("Fixme")]
	[TestFixture]
	class ParameterHintingTests : TestBase
	{
		internal static MonoDevelop.Ide.CodeCompletion.ParameterHintingResult CreateProvider(string text, bool force = false)
		{
			string parsedText;
			string editorText;
			int cursorPosition = text.IndexOf('$');
			int endPos = text.IndexOf('$', cursorPosition + 1);
			if (endPos == -1) {
				parsedText = editorText = text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1);
			} else {
				parsedText = text.Substring(0, cursorPosition) + new string(' ', endPos - cursorPosition) + text.Substring(endPos + 1);
				editorText = text.Substring(0, cursorPosition) + text.Substring(cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring(endPos + 1);
				cursorPosition = endPos - 1; 
			}
			
			var workspace = new InspectionActionTestBase.TestWorkspace ();

			var projectId  = ProjectId.CreateNewId();
			//var solutionId = SolutionId.CreateNewId();
			var documentId = DocumentId.CreateNewId(projectId);

			workspace.Open(ProjectInfo.Create(
						projectId,
						VersionStamp.Create(),
						"TestProject",
						"TestProject",
						LanguageNames.CSharp,
						null,
						null,
						new CSharpCompilationOptions (
							OutputKind.DynamicallyLinkedLibrary,
							false,
							"",
							"",
							"Script",
							null,
							OptimizationLevel.Debug,
							false,
							false
						),
						new CSharpParseOptions (
							LanguageVersion.CSharp6,
							DocumentationMode.None,
							SourceCodeKind.Regular,
							ImmutableArray.Create("DEBUG", "TEST")
						),
						new [] {
							DocumentInfo.Create(
								documentId,
								"a.cs",
								null,
								SourceCodeKind.Regular,
								TextLoader.From(TextAndVersion.Create(SourceText.From(parsedText), VersionStamp.Create())) 
							)
						},
						null,
						InspectionActionTestBase.DefaultMetadataReferences
					)
			);
			
			var engine = new MonoDevelop.CSharp.Completion.RoslynParameterHintingEngine ();
			
			var compilation = workspace.CurrentSolution.GetProject(projectId).GetCompilationAsync().Result;
			
			if (!workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(editorText)))) {
				Assert.Fail();
			}
			var document = workspace.CurrentSolution.GetDocument(documentId);
			var semanticModel = document.GetSemanticModelAsync().Result;
			if (force) {
				var workspace1 = TypeSystemService.Workspace;
				var mefExporter = (IMefHostExportProvider)workspace1.Services.HostServices;
				var helpProviders = mefExporter.GetExports<ISignatureHelpProvider, LanguageMetadata> ()
					.FilterToSpecificLanguage (LanguageNames.CSharp).ToList ();
				return engine.GetParameterDataProviderAsync (helpProviders, document, cursorPosition, new SignatureHelpTriggerInfo (SignatureHelpTriggerReason.InvokeSignatureHelpCommand)).Result;
			} else
				return engine.GetParameterDataProviderAsync (document, cursorPosition).Result;
		}
		
		/// <summary>
		/// Bug 427448 - Code Completion: completion of constructor parameters not working
		/// </summary>
		[Test]
		public void TestBug427448 ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (2, provider.Count);
		}

		/// <summary>
		/// Bug 432437 - No completion when invoking delegates
		/// </summary>
		[Test]
		public void TestBug432437 ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 432658 - Incorrect completion when calling an extension method from inside another extension method
		/// </summary>
		[Test]
		public void TestBug432658 ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (1, provider.Count, "There should be one overload");
			Assert.AreEqual (1, provider[0].ParameterCount, "Parameter 'start' should exist");
		}

		/// <summary>
		/// Bug 432727 - No completion if no constructor
		/// </summary>
		[Test]
		public void TestBug432727 ()
		{
			var provider = CreateProvider (
@"class A
{
	void Method ()
	{
		$A aTest = new A ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test]
		public void TestBug434705 ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (1, provider.Count);
		}
		
		/// <summary>
		/// Bug 434705 - No autocomplete offered if not assigning result of 'new' to a variable
		/// </summary>
		[Test]
		public void TestBug434705B ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (1, provider.Count);
		}
	
		
		/// <summary>
		/// Bug 434701 - No autocomplete in attributes
		/// </summary>
		[Test]
		public void TestBug434701 ()
		{
			var provider = CreateProvider (
@"namespace Test {
class TestAttribute : System.Attribute
{
	public Test (int a)
	{
	}
}

$[Test ($
class AClass
{
}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// <summary>
		/// Bug 447985 - Exception display tip is inaccurate for derived (custom) exceptions
		/// </summary>
		[Test]
		public void TestBug447985 ()
		{
			var provider = CreateProvider (
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
			Assert.AreEqual (1, provider.Count);
			Assert.AreEqual (1, provider[0].ParameterCount, "Parameter 'test' should exist");
		}
		
		
		/// <summary>
		/// Bug 1760 - [New Resolver] Parameter tooltip not shown for indexers 
		/// </summary>
		[Test]
		public void Test1760 ()
		{
			var provider = CreateProvider (
				@"
class TestClass
{
	public static void Main (string[] args)
	{
		$args[$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		[Test]
		public void TestSecondIndexerParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public int this[int i, int j] { get { return 0; } } 
	public void Test ()
	{
		$this[1,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test]
		public void TestSecondMethodParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public int TestMe (int i, int j) { return 0; } 
	public void Test ()
	{
		$TestMe (1,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		[Test]
		public void TestMethodParameterWithSpacesTabsNewLines ()
		{
			var provider = CreateProvider (@"class TestClass
{
	public int TestMe (int x) { return 0; } 
	public void Test ()
	{
		$TestMe ( 		  	  
 	
 $
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
        
		[Test]
		public void TestMethodParameterNestedArray ()
		{
		    var provider = CreateProvider (@"using System;

class TestClass
{
	TestClass ()
	{
		var str = new string[2,2];
		$Console.WriteLine ( str [1,$
	}
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		
		/// Bug 599 - Regression: No intellisense over Func delegate
		[Test]
		public void TestBug599 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Core;

class TestClass
{
	void A (Func<int, int> f)
	{
		$f ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		/// Bug 3307 - Chained linq methods do not work correctly
		[Test]
		public void TestBug3307 ()
		{
			var provider = CreateProvider (
@"using System;
using System.Linq;

class TestClass
{
	public static void Main (string[] args)
	{
		$args.Select ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsTrue (provider.Count > 0);
		}

		[Test]
		public void TestConstructor ()
		{
			var provider = CreateProvider (
@"class Foo { public Foo (int a) {} }

class A
{
	void Method ()
	{
		$Bar = new Foo ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		
		[Test]
		public void TestConstructorCase2 ()
		{
			var provider = CreateProvider (
@"
namespace Test 
{
	struct TestMe 
	{
		public TestMe (string a)
		{
		}
	}
	
	class A
	{
		void Method ()
		{
			$new TestMe ($
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}
		
		[Test]
		public void TestTypeParameter ()
		{
			var provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void Method ()
		{
			$Action<$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (16, provider.Count);
		}

		[Test]
		public void TestSecondTypeParameter ()
		{
			var provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void Method ()
		{
			$Action<string,$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (16, provider.Count);
		}
		
		[Test]
		public void TestMethodTypeParameter ()
		{
			var provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void TestMethod<T, S>()
		{
		}

		void Method ()
		{
			$TestMethod<$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test]
		public void TestSecondMethodTypeParameter ()
		{
			var provider = CreateProvider (
@"using System;

namespace Test 
{
	class A
	{
		void TestMethod<T, S>()
		{
		}

		void Method ()
		{
			$TestMethod<string,$
		}
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}		
	
		[Test]
		public void TestArrayParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public void Method()
	{
		int[,,,] arr;
		$arr[$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test]
		public void TestSecondArrayParameter ()
		{
			var provider = CreateProvider (
@"
class TestClass
{
	public void Method()
	{
		int[,,,] arr;
		$arr[5,$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}
		
		[Test]
		public void TestTypeParameterInBaseType ()
		{
			var provider = CreateProvider (
@"using System;

namespace Test 
{
	$class A : Tuple<$
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (8, provider.Count);
		}
		
		
		[Test]
		public void TestBaseConstructorCall ()
		{
			var provider = CreateProvider (
@"class Base
{
	public Base (int i)
	{
			
	}
	public Base (int i, string s)
	{
			
	}
}

namespace Test 
{
	class A : Base
	{
		$public A () : base($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}
		
		[Test]
		public void TestThisConstructorCall ()
		{
			var provider = CreateProvider (
@"class Base
{
	public Base (int i)
	{
			
	}
	public Base (int i, string s)
	{
			
	}
}

namespace Test 
{
	class A : Base
	{
		public A (int a, int b) : base(a) {}

		$public A () : this($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}
		
		/// <summary>
		/// Bug 3645 - [New Resolver]Parameter completion shows all static and non-static overloads
		/// </summary>
		[Test]
		public void TestBug3645 ()
		{
			var provider = CreateProvider (
@"class Main
{
	public static void FooBar (string str)
	{
	}
	
	public void FooBar (int i)
	{
		
	}
	
	public static void Main (string[] args)
	{
		$FooBar ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 3991 - constructor argument completion not working for attributes applied to methods or parameters
		/// </summary>
		[Test]
		public void TestBug3991()
		{
			var provider = CreateProvider(
@"using System;
namespace Test
{
	class TestClass
	{
		[Obsolete$($]
		TestClass()
		{
		}
	}
}
");
			Assert.IsNotNull(provider, "provider was not created.");
			Assert.Greater(provider.Count, 0);
		}

		/// <summary>
		/// Bug 4087 - code completion handles object and collection initializers (braces) incorrectly in method calls
		/// </summary>
		[Test]
		public void TestBug4087()
		{
			var provider = CreateProvider(
@"using System;
class TestClass
{
	TestClass()
	{
		$Console.WriteLine (new int[]{ 4, 5,$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}

		/// <summary>
		/// Bug 4927 - [New Resolver] Autocomplete shows non-static methods when using class name
		/// </summary>
		[Test]
		public void TestBug4927 ()
		{
			var provider = CreateProvider (
@"
public class A
{
  // static method
  public static void Method(string someParameter, object anotherParameter)
  {
  }

  // instance method
  public void Method()
  {
  }
}


public class B
{
  public static void Main()
  {
    $A.Method($
  }
}
");
	
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}


		[Test]
		public void TestLambdaCase()
		{
			var provider = CreateProvider(
				@"using System;
class TestClass
{    
	void F (Action i, int foo)
	{
		$F (()=> Something(),$

	}
}
");
			Assert.IsTrue (provider != null && provider.Count == 1);
		}

		[Test]
		public void TestJaggedArrayCreation()
		{
			var provider = CreateProvider(
				@"using System;
class TestClass
{    
	void F (Action i, int foo)
	{
		$new foo[1,2][$

	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}

		[Test]
		public void TestJaggedArrayCreationCase2()
		{
			var provider = CreateProvider(
				@"using System;
class TestClass
{    
	void F (Action i, int foo)
	{
		$new foo[1,2][1,$

	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}

		/// <summary>
		/// Bug 9301 - Inaccessible indexer overload in completion 
		/// </summary>
		[Test]
		public void TestBug9301()
		{
			var provider = CreateProvider(
				@"using System;

public class A
{
	public virtual int this [int i, string s] {
		get {
			return 1;
		}
	}
}

public class B : A
{
	public new bool this [int i, string s2] {
		get {
			return true;
		}
	}
}

public class Test
{
	public static int Main ()
	{
		B p = new B ();
		$p[$
		return 0;
	}
}
");
			Assert.AreEqual (1, provider.Count);
		}

		[Test]
		public void TestBug9301Case2()
		{
			var provider = CreateProvider(
				@"using System;

public class A
{
	public virtual int Test (int i, string s) {
		return 1;
	}
}

public class B : A
{
	public new bool Test (int i, string s2) {
		return true;
	}
}

public class Test
{
	public static int Main ()
	{
		B p = new B ();
		$p.Test($
		return 0;
	}
}
");
			Assert.AreEqual (1, provider.Count);
		}

		[Test]
		public void TestExtensionMethod()
		{
			var provider = CreateProvider(@"static class Ext { public static void Foo(this object o, string str) {} }
class Test
{
	public static void Main (string[] args)
	{
		$args.Foo($
	}
}");
			Assert.AreEqual (1, provider.Count);
			Assert.AreEqual (1, provider[0].ParameterCount);
		}
		
		
		[Test]
		public void TestExtensionMethodStaticInvocation()
		{
			var provider = CreateProvider(@"static class Ext { public static void Foo(this object o, string str) {} }
class Test
{
	public static void Main (string[] args)
	{
		$Ext.Foo($
	}
}");
			Assert.AreEqual (1, provider.Count);
			Assert.AreEqual (2, provider[0].ParameterCount);
		}

		[Ignore("fixme")]
		[Test]
		public void TypeArgumentsInIncompleteMethodCall ()
		{
			var provider = CreateProvider (
				@"using System.Collections.Generic;
using System.Linq;
class NUnitTestClass {
    public ICollection<ITest> NestedTestCollection { get; set; }
    public NUnitTestMethod FindTestMethodWithShortName(string name)
    {
        this.NestedTestCollection$.OfType<$.LastOrDefault(
    }
}");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test]
		public void TestBug474199B ()
		{
			var provider = ParameterHintingTests.CreateProvider (
				@"
public class InnerTest
{
	public class Inner
	{
		public Inner(string test)
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		$new ExtInner.Inner ($
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count, "There should be one overload");
			Assert.AreEqual (1, provider[0].ParameterCount, "Parameter 'test' should exist");
		}

		/// <summary>
		/// Bug 4290 - Parameter completion exception inserting method with arguments before other methods
		/// </summary>
		[Test]
		public void TestBug4290()
		{
			// just test for exception
			ParameterHintingTests.CreateProvider (
				@"using System;
namespace Test
{
    class TestClass  
    {
        $public static void Foo(string bar,$
        public static void Main(string[] args)
        {
        }
    }
}");
		}

		/// <summary>
		/// Bug 4323 - Parameter completion exception while attempting to instantiate unknown class
		/// </summary>
		[Test]
		public void TestBug4323()
		{
			// just test for exception
			ParameterHintingTests.CreateProvider(
				@"namespace Test
{
    class TestClass
    {
        public static void Main(string[] args)
        {
            $object foo = new Foo($
        }
    }
}");
		}

		/// <summary>
		/// Bug 432727 - No completion if no constructor
		/// </summary>
		[Test()]
		public void TestArrayInitializerParameterContext ()
		{
			var provider = ParameterHintingTests.CreateProvider (
				@"using System;

class MyTest
{
	public void Test ()
	{
		$new [] { Tuple.Create($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.Greater (provider.Count, 1);
		}

		[Test]
		public void TestMethodOverloads ()
		{
			var provider = CreateProvider(@"class TestClass
{
	public int TestMe () { return 0; } 
	public int TestMe (int x) { return 0; } 
	public int TestMe (int x, int y) { return 0; } 
	public void Test ()
	{
		$TestMe ($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (3, provider.Count);
		}

		[Test]
		public void TestMethodOverloads2 ()
		{
			var provider = CreateProvider(@"class TestClass
{
	public int TestMe () { return 0; } 
	public int TestMe (int x) { return 0; } 
	public int TestMe (int x, int y) { return 0; } 
	public void Test ()
	{
		$TestMe (1, $
	}
}");
			Assert.IsNotNull(provider, "provider was not created.");
			Assert.AreEqual(3, provider.Count);
		}

		[Test]
		public void TestMethodOverloads3 ()
		{
			var provider = CreateProvider (@"class TestClass
{
	public int TestMe () { return 0; } 
	public int TestMe (int x) { return 0; } 
	public int TestMe (int x, int y) { return 0; } 
	public void Test ()
	{
		$TestMe (1, 2$
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (3, provider.Count);
		}

		[Test]
		public void TestWriteLine ()
		{
			var provider = CreateProvider (
				@"using System;
class TestClass
{
	public static void Main (string[] args)
	{
		Console.WriteLine ($$);
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (19, provider.Count);
		}

		[Test]
		public void TestExtensionMethods ()
		{
			var provider = CreateProvider (
				@"using System;
using System.Linq;
class TestClass
{
	public static void Main (string[] args)
	{
		Console.WriteLine ($args.Any($);
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}

		[Test]
		public void TestHintingTooEager ()
		{
			var provider = CreateProvider (
				@"using System;
class TestClass
{
	public static void Main ()
	{
		$Main $();
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (0, provider.Count);

			provider = CreateProvider (
				@"using System;
class TestClass
{
	public static void Main ()
	{
		Main ()$  $;
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (0, provider.Count);
		}

		/// <summary>
		/// Bug 40018 - Autocomplete shows different list before and after typing "("
		/// </summary>
		[Test]
		public void TestBug40018 ()
		{
			var provider = CreateProvider (
				@"
namespace Test40018
{
    static class ExtMethods
    {
        public static void Foo(this MyClass c, int i)
        {

        }
    }
	
    class MyClass
    {
        public void Foo(string str)
        {
        }

        public void Foo()
        {
        }

        public void Test()
        {
            this.Foo($$);
        }
    }
}				
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (3, provider.Count);
		}

		/// <summary>
		/// Bug 41245 - Attribute code completion not showing all constructors and showing too many things
		/// </summary>
		[Test]
		public void TestBug41245 ()
		{
			var provider = CreateProvider (
				@"
using System;

namespace cp654fz7
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
	public sealed class JsonPropertyAttribute : Attribute
	{
		internal bool? _isReference;
		internal int? _order;
		public bool IsReference
		{
			get { return _isReference ?? default(bool); }
			set { _isReference = value; }
		}
		public int Order
		{
			get { return _order ?? default(int); }
			set { _order = value; }
		}
		public string PropertyName { get; set; }
		public JsonPropertyAttribute()
		{
		}

		public JsonPropertyAttribute(string propertyName)
		{
			PropertyName = propertyName;
		}
	}

	class MainClass
	{
		[JsonProperty($$)]
		public object MyProperty { get; set; }

		public static void Main(string[] args)
		{
		}
	}
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}


		/// <summary>
		/// Bug 41351 - No arguments code completion for methods called via ?. operator
		/// </summary>
		[Test]
		public void TestBug41351 ()
		{
			var provider = CreateProvider (
				@"
using System;

class test
{
	public event EventHandler Handler;

	public test()
	{
		Handler?.Invoke($$);
	}
}

");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}

		/// <summary>
		/// Bug 42952 - Parameter info not working for extension methods in different namespace
		/// </summary>
		[Test]
		public void TestBug42952 ()
		{
			var provider = CreateProvider (
				@"
using System;
using DifferentNamespace.ha;

namespace parametersInfoBug
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            new Class1().Test1($$);
        }
    }
}


namespace DifferentNamespace.ha
{
    public static class Extensions
    {
        public static void Test1(this Class1 class1, string stringParam)
        {

        }
    }

    public class Class1
    {

    }
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (1, provider.Count);
		}


		/// <summary>
		/// Bug 52886 - Base class constructor tooltip doesn't contain all constructor definition.
		/// </summary>
		[Test]
		public void TestBug52886 ()
		{
			var provider = CreateProvider (
				@"
class Foo
{
	public Foo ()
	{
	}
	public Foo (int a)
	{
	}
}
class Bar : Foo
{
	public Bar () : base($$
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (2, provider.Count);
		}

		[Test]
		public void TestConstructorInsideMethodCall()
		{
			var provider = CreateProvider (
				@"
class MainClass
{
	public static void Main(string[] args)
	{
		System.Console.WriteLine(new string($));
	}
}
", true);
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (8, provider.Count);
		}
	}
}