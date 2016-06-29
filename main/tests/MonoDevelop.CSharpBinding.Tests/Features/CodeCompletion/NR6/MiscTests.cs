//
// MiscTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory6.CSharp.Completion;
using ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn;
using System.Reflection.Metadata.Ecma335.Blobs;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.NR6
{
	[TestFixture]
	class MiscTests : CompletionTestBase
	{
		[Test]
		public void TestSimple ()
		{
			VerifyItemsAbsent (@"
class FooBar
{
	public static void Test (TestEnum e)
	{	
		foobar$$
	}
}
", "foobar");
		}


		/// <summary>
		/// Bug 37124 - [Roslyn] Autocompletion of method with enum parameter and no-parameter overloads
		/// </summary>
		[Test]
		public void TestBug37124 ()
		{
			var provider = CreateProvider (
				@"
enum FooBar { Foo, Bar }

public class MyClass 
{

	void Test ()
	{
	}

	void Test (FooBar fooBar)
	{

	}

	public void Bug()
	{
		$Test ($
	}
}				
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (false, provider.AutoSelect);
		}

		[Test]
		public void TestBug37124_Case2 ()
		{
			var provider = CreateProvider (
				@"
enum FooBar { Foo, Bar }

public class MyClass 
{

	public MyClass ()
	{
	}

	public MyClass (FooBar fooBar)
	{

	}

	public void Bug()
	{
		$new MyClass ($
	}
}				
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.AreEqual (false, provider.AutoSelect);
		}

		/// <summary>
		/// Bug 37573 - [roslyn] Excessive namespace prefix inserted by code completion
		/// </summary>
		[Test]
		public void TestBug37573 ()
		{
			var provider = CreateProvider (
				@"
using System;

namespace TestProject
{
	class TestClass
	{
		public enum FooBar { Foo, Bar }
	}

	class MainClass
	{
		void Test ()
		{
			TestClass.FooBar fb;
			$if (fb == $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsNull (provider.Find ("TestProject.TestClass.FooBar.Bar"));
			Assert.IsNotNull (provider.Find ("TestClass.FooBar.Bar"));
		}

		/// <summary>
		/// Bug 39015 - New keyword missing from completion
		/// </summary>
		[Test]
		public void TestBug39015 ()
		{
			var provider = CreateProvider (
				@"
using System;

namespace Foo
{
	enum TestEnum { A, B }
	public class Test
	{
		void FooBar (int i, ConsoleKey t)
		{
		}
		void FooBar (int i, object o)
		{
		}

		void TestMe ()
		{
			$this.FooBar (12, $
			
		}
	}
}", usePreviousCharAsTrigger: true);
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsNotNull (provider.Find ("new"));
		}

		//Bug 39589 - No code completion in assembly attribute arguments
		[Test]
		public void TestBug39589 ()
		{
			VerifyItemExists (@"
using System.Reflection;
[assembly: AssemblyTitle(S$$)]
", "System");
		}

		/// <summary>
		/// Bug 40413 - Incorrect number of method overloads
		/// </summary>
		[Test]
		public void TestBug40413 ()
		{
			var provider = CreateProvider (
				@"
using System;

class Test
{
    static object Foo(int arg, int arg2) { return null; }
    static object Foo(object arg, object arg2) { return null; }

    public static void Main(string[] args)
    {
        Func<int, int, object> o = $F$
    }
}", usePreviousCharAsTrigger: true);
			Assert.IsNotNull (provider, "provider was not created.");
			var data = provider.Find ("Foo");
			Assert.IsNotNull (data);
			Assert.AreEqual (2, data.OverloadedData.Count);
		}

		/// <summary>
		/// Bug 41245 - Attribute code completion not showing all constructors and showing too many things
		/// </summary>
		[Ignore("Need to think about/discuss it - would maybe kill implement by usage oportunities.")]
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
		[JsonProperty(""Hello"", $$)]
		public object MyProperty { get; set; }

		public static void Main(string[] args)
		{
		}
	}
}
");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.IsNull (provider.Find ("MainClass"));
		}


		/// <summary>
		/// Bug 41388 - Code completion is incorrect for array types
		/// </summary>
		[Test]
		public void TestBug41388 ()
		{
			var provider = CreateProvider (
				@"
using System;

class Test
{
	public event EventHandler FooBar;

	public Test[] test { get; private set; }


	public Test()
	{
		test = new $$
		FooBar += Test_FooBar;
	}

	void Test_FooBar(object sender, EventArgs e)
	{

	}
}

");
			Assert.IsNotNull (provider, "provider was not created.");

			Assert.IsNull (provider.Find ("System.Action<object, System.EventArgs>"));
		}
	}
}