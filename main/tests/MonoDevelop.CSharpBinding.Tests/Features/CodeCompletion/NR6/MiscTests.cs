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
	}
}