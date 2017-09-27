//
// DelegateCreationContextHandlerTests.cs
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
	class DelegateCompletionProviderTests : AbstractCSharpCompletionProviderTests
	{
		protected override IEnumerable<CompletionProvider> CreateCompletionProvider ()
		{
			yield return new DelegateCompletionProvider ();
		}

		[Test]
		public void TestNameGenerationMember ()
		{
			VerifyItemExists (@"
using System;

public class A
{
    public event EventHandler FooBar;
}

public class B
{
	A test;

	void TestMe()
	{
		test.FooBar += $$
	}
}", "Test_FooBar");
		}

		[Test]
		public void TestNameGeneration ()
		{
			VerifyItemExists (@"
using System;

public class A
{
    public event EventHandler FooBar;

	void TestMe()
	{
		FooBar += $$
	}
}", "A_FooBar");
		}

		[Test]
		public void TestNameClash ()
		{
			VerifyItemExists (@"
using System;

public class A
{
    public event EventHandler FooBar;
}

public class B
{
	A test;

	void TestMe()
	{
		test.FooBar += $$
	}

	void Test_FooBar()
	{
	}
}", "Test_FooBar1");
		}
	}
}
