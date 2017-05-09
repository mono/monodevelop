// 
// NameContextTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using NUnit.Framework;


namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	[TestFixture]
	class NameContextTests : TestBase
	{
		[Test]
		public void TestNamespaceName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace n$");
			AssertExists (provider, "System");
			AssertExists (provider, "Microsoft");
			Assert.AreEqual (2, provider.Count);
		}

		[Test]
		public void TestNamespaceNameCase2 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace $");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test]
		public void TestNamespaceNameCase3 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace Foo.b$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test]
		public void TestNamespaceNameCase4 ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$namespace Foo.$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test]
		public void TestClassName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$class n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}


		[Test]
		public void TestStructName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$struct n$");
			AssertEmpty(provider);

		}
		
		[Test]
		public void TestInterfaceName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$interface n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestEnumName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$enum n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestDelegateName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$delegate void n$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestClassTypeParameter ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"$class MyClass<T$");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestFieldName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$int f$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestParameterName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$void SomeMethod(int f$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestLocalVariableName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
		[Test]
		public void TestForeachLocalVariableName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$foreach (int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test]
		public void TestForLoopLocalVariableName ()
		{

			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$for (int f$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		[Test]
		public void TestCatchExceptionName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	void Test() 
	{
		$try {
		} catch (Exception e$
	}
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}
		
			/// <summary>
		/// Bug 2198 - Typing generic argument to a class/method pops up type completion window
		/// </summary>
		[Test]
		public void TestBug2198 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$class Klass <T$", provider => {
				Assert.AreEqual (0, provider.Count, "provider needs to be empty");
			});
		}
		
		[Test]
		public void TestBug2198Case2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"$class Klass { void Test<T$", provider => {
				Assert.AreEqual (0, provider.Count, "provider needs to be empty");
			});
		}

		[Test]
		public void TestIndexerParameterName ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$public int this [int f$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
			provider = CodeCompletionBugTests.CreateProvider (@"class MyClass {
	$public int this [int f, string x$
}");
			Assert.IsTrue (provider == null || provider.Count == 0, "provider should be empty.");
		}

		/// <summary>
		/// Bug 11609 - Completion engine offers namespaces when user types anonymous method parameter name
		/// </summary>
		[Ignore("there may be keywords there as/is")]
		[Test]
		public void TestBug11609 ()
		{
			CodeCompletionBugTests.CombinedProviderTest(@"using System;

namespace MyApplication
{
   class MyClass
   {
        void MyMethod ()
        {
            $SomeMethod (configurator: (Type s$
        }

        void SomeMethod (Action <Type> configurator)
        {}
    }
}
", AssertEmpty);
		}

		/// <summary>
		/// Bug 13365 - Suggestion context lost after ( in lambda args
		/// </summary>
		[Test]
		public void TestBug13365 ()
		{

			CodeCompletionBugTests.CombinedProviderTest (@"using System;
using System.Threading.Tasks;

namespace MyApplication
{
   class MyClass
   {
        void MyMethod ()
        {
            $Task.Factory.StartNew ((a$
        }
    }
}
", provider => {

				Assert.IsFalse (provider.AutoSelect);
			});
		}

		[Test]
		public void TestBug13365_pt2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"using System;
using System.Threading.Tasks;

namespace MyApplication
{
   class MyClass
   {
        void MyMethod (int a)
        {
            $MyMethod (a$
        }
    }
}
", provider => {

				Assert.IsTrue (provider.AutoSelect);
			});
		}
		[Ignore("there may be keywords there as/is")]
		[Test]
		public void TestLambda ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"using System;
using System.IO;

class Foo
{
	static void Foo (Action<File> act) {}

	public static void Main (string[] args)
	{
		$Foo((File f$
	}
}
", AssertEmpty);
		}

		/// <summary>
		/// Bug 16491 - Wrong completion on multiple parameter lambdas
		/// </summary>
		[Test]
		[Ignore ("https://github.com/dotnet/roslyn/issues/17697")]
		public void TestBug16491 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"
using System;
using System.IO;

class Foo
{
	public static void Main (string[] args)
	{
		$new Action<int, int> ((x, y$
	}
}
", AssertEmpty);

		}

		[Test]
		[Ignore ("https://github.com/dotnet/roslyn/issues/17697")]
		public void TestBug16491Case2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (@"
using System;
using System.IO;

class Foo
{
	public static void Main (string[] args)
	{
		new Action<int, int> ((x$, y$)
	}
}
", AssertEmpty);

		}


	}
}

