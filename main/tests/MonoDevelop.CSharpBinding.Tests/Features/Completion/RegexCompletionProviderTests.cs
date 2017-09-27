//
// RegexContextHandlerTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
	class RegexCompletionProviderTests : AbstractCSharpCompletionProviderTests
	{
		protected override IEnumerable<CompletionProvider> CreateCompletionProvider ()
		{
			yield return new RegexCompletionProvider ();
		}

		[Test]
		public void Constructor_SimpleStringTest ()
		{
			var text = @"using System;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Class1
    {
        void Test ()
        {
			var reg = new Regex(""test\$$me"");
        }

    }
}
";
			VerifyItemExists (text, "W", usePreviousCharAsTrigger: true);
		}

		[Test]
		public void Constructor_VerbatimStringTest ()
		{
			var text = @"using System;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Class1
    {
        void Test ()
        {
			var reg = new Regex(@""test\$$me"");
        }

    }
}
";
			VerifyItemExists (text, "\\W", usePreviousCharAsTrigger: true);
		}

		[Test]
		public void RegexMatch_SimpleStringTest ()
		{
			var text = @"using System;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Class1
    {
        void Test ()
        {
			var match = Regex.Match(""input"", ""test\$$me"");
        }

    }
}
";
			VerifyItemExists (text, "W", usePreviousCharAsTrigger: true);
		}

		[Test]
		public void TestGroupCompletion_DirectMatch ()
		{
			var text = @"using System;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Class1
    {
        void Test ()
        {
			var match = Regex.Match(""input"", ""(?<testgroup>test)me"");
			match.$$
        }

    }
}
";
			VerifyItemExists (text, "Groups[\"testgroup\"]", usePreviousCharAsTrigger: true);
		}

		[Test]
		public void TestGroupCompletion_InDirectMatch ()
		{
			var text = @"using System;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    class Class1
    {
        void Test ()
        {
			var regex = new Regex (""(?<testgroup>test)me"");
			var match = regex.Match(""input"");
			match.$$
        }

    }
}
";
			VerifyItemExists (text, "Groups[\"testgroup\"]", usePreviousCharAsTrigger: true);
		}
	}
}