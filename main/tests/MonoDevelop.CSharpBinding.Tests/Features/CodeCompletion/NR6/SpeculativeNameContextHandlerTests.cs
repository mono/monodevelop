//
// SpeculativeNameContextHandlerTests.cs
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

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.NR6
{
	[TestFixture]
	public class SpeculativeNameContextHandlerTests : TestBase
	{
		[Test]
		public void TestField()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	TestClass $
}");
			AssertExists (provider, "testClass");
			AssertExists (provider, "class");
		}


		[Test]
		public void TestGenericField()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	List<string> $
}");

			AssertExists (provider, "list");
		}


		[Test]
		public void TestLocal()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	public static void Main (string[] args)
	{
		TestClass $
	}
}");

			AssertExists (provider, "testClass");
			AssertExists (provider, "class");
		}

		[Test]
		public void TestGenericLocal()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	public static void Main (string[] args)
	{
		List<string> $
	}
}");

			AssertExists (provider, "list");
		}

		[Test]
		public void TestParameter()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	public static void Main (TestClass $)
	{
	}
}");

			AssertExists (provider, "testClass");
			AssertExists (provider, "class");
		}

		[Test]
		public void TestGenericParameter()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	public static void Main (List<string> $)
	{
	}
}");

			AssertExists (provider, "list");
		}

		[Test]
		public void TestStringSpecialType()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
				@"public class TestClass
{
	string $
}");
			Assert.IsNotNull (provider.Find ("str"), "should contain 'str'");
		}

		[Test]
		public void TestCharSpecialType()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
		@"using System.Collections.Generic;

class MainClass
{
	void Bar (object o)
	{
		// MainClass mc = new 

		char $
	}
}");
			AssertExists (provider, "ch");
			AssertExists (provider, "c");
		}



	}
}

