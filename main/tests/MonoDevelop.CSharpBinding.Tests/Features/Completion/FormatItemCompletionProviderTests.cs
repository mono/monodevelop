//
// FormatItemCompletionProviderTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
	class FormatItemCompletionProviderTests : AbstractCSharpCompletionProviderTests
	{
		protected override IEnumerable<CompletionProvider> CreateCompletionProvider ()
		{
			yield return new FormatItemCompletionProvider ();
		}

		[Test]
		public void TestFormatItem ()
		{
			VerifyItemExists (
				@"
class TestClass
{
	public void Test ()
	{
		string.Format("" {0:$$
	}
}", "D");
		}


		[Test]
		public void TestFalsePositive ()
		{
			VerifyNoItemsExist (
				@"using System;
class TestClass
{
	public void Test ()
	{
		Console.WriteLine (""Hello :$$
	}
}");
		}

		[Test]
		public void TestFormatItemRecognition ()
		{
			VerifyItemsExist (
				@"using System;
class TestClass
{
	public void Test (Guid i)
	{
		string.Format("" ${0:$$"", i);
	}
}", "D");
		}


		[Test]
		public void TestDontShowupCase ()
		{
			VerifyNoItemsExist (
				@"using System;
class TestClass
{
	public void Test (string i)
	{
		string.Format("" ${1:$$"", 12, i);
	}
}");
		}

		[Test]
		public void TestIntToString ()
		{
			VerifyItemsExist (
				@"
class TestClass
{
	public void Test (int i)
	{
		i.ToString(""$$
	}
}", "D");
		}

		[Test]
		public void TestDateTimeToString ()
		{
			VerifyItemsExist (
				@"using System;
class TestClass
{
	public void Test (DateTime i)
	{
		i.ToString(""$$
	}
}", "d");
		}


		[Test]
		public void TestGuidToString ()
		{
			VerifyItemsExist (
				@"using System;
class TestClass
{
	public void Test (Guid i)
	{
		i.ToString(""$$
	}
}", "D");
		}

		[Test]
		public void TestTimeSpanToString ()
		{
			VerifyItemsExist (
				@"using System;
class TestClass
{
	public void Test (TimeSpan i)
	{
		i.ToString(""$$
	}
}", "c", "G", "g");
		}


		[Test]
		public void TestEnumToString ()
		{
			VerifyItemsExist (
				@"using System;
class TestClass
{
	public void Test (ConsoleKey i)
	{
		i.ToString(""$$
	}
}", "D", "F", "G", "X");
		}
	}
}