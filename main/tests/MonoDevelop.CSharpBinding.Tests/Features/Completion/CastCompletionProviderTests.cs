//
// CastCompletionContextHandlerTests.cs
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
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using MonoDevelop.CSharp.Completion.Provider;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Tests.Features.Completion
{
	[TestFixture]
	class CastCompletionProviderTests : AbstractCSharpCompletionProviderTests
	{
		protected override IEnumerable<CompletionProvider> CreateCompletionProvider ()
		{
			yield return new CastCompletionProvider ();
		}

		[Test]
		public void TestSimple ()
		{
			VerifyItemExists (@"
using System;

class FooBar
{
	public event EventHandler Foo;

	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		if (fb is FooBar) {
			fb.$$
		}
	}
}
", "Bar");
		}

		[Ignore]
		[Test]
		public void TestNoUpcastAvailable ()
		{
			VerifyNoItemsExist (@"
class A 
{
	public int Foo;
	public void Bar (){}
	public string FooBar { get ; set; }
	public event FooEvt;
}

class B : A
{

}

class TestClass
{
	public TestClass(A a)
	{
		if (a is B) {
			a.$$
		}
}
}	");
		}

		[Test]
		public void TestReturn ()
		{
			VerifyItemExists (@"
using System;

class FooBar
{
	public event EventHandler Foo;

	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		if (true) {
			if (!(fb is FooBar))
				return;
			fb.$$
		}
	}
}
", "Bar");
		}

		[Test]
		public void TestContinue ()
		{
			VerifyItemExists (@"
using System;

class FooBar
{
	public event EventHandler Foo;

	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		for (int i = 0; i < 10; i++) {
			if (!(fb is FooBar))
				continue;
			fb.$$
		}
	}
}
", "Bar");
		}

		[Test]
		public void TestBreak ()
		{
			VerifyItemExists (@"
using System;

class FooBar
{
	public event EventHandler Foo;

	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		for (int i = 0; i < 10; i++) {
			if (!(fb is FooBar))
				break;
			fb.$$
		}
	}
}
", "Bar");
		}

		/// <summary>
		/// Bug 38957 - Casting code completion(one based on if "is") offers wrong in case of "if else if" 
		/// </summary>
		[Test]
		public void TestBug38957 ()
		{
			VerifyItemsAbsent (@"
using System;

class FooBar
{
	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		if (fb is FooBar) {
		} else if (true) {
			fb.$$
		}
	}
}
", "Bar");
		}

		[Test]
		public void TestExpression ()
		{
			VerifyItemExists (@"
using System;

class FooBar
{
	public event EventHandler Foo;

	public int  Bar { get; set; }

	public static void Test (object fb)
	{	
		if (fb is FooBar && fb.$$
	}
}
", "Bar");
		}

	}
}