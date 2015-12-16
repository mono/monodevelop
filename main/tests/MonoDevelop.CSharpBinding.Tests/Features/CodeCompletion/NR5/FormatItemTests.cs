//
// FormatItemTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
	public class FormatItemTests : TestBase
	{
		[Test]
		public void TestFormatItem ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"
class TestClass
{
	public void Test ()
	{
		$string.Format("" {0:$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}


		[Test]
		public void TestFalsePositive ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test ()
	{
		$Console.WriteLine (""Hello :$
	}
}");
			Assert.IsTrue(provider == null || provider.Count == 0); 
		}

		[Test]
		public void TestFormatItemRecognition ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (Guid i)
	{
		string.Format("" ${0:$"", i);
	}
}");
			Assert.IsNotNull (provider);
			Assert.AreEqual(4, provider.Count); 
		}


		[Test]
		public void TestDontShowupCase ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (string i)
	{
		string.Format("" ${1:$"", 12, i);
	}
}");
			Assert.IsTrue(provider == null || provider.Count == 0); 
		}

		[Test]
		public void TestIntToString ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"
class TestClass
{
	public void Test (int i)
	{
		$i.ToString(""$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}
		
		[Test]
		public void TestDateTimeToString ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (DateTime i)
	{
		$i.ToString(""$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}

		
		[Test]
		public void TestGuidToString ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (Guid i)
	{
		$i.ToString(""$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}
		
		[Test]
		public void TestTimeSpanToString ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (TimeSpan i)
	{
		$i.ToString(""$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}

		
		[Test]
		public void TestEnumToString ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
				@"using System;
class TestClass
{
	public void Test (ConsoleKey i)
	{
		$i.ToString(""$
	}
}");
			Assert.IsNotNull (provider);
			Assert.Greater(provider.Count, 0); 
		}
	}
}

