//
// PatternMatcherTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using MonoDevelop.Components;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class PatternMatcherTests
	{
		[Test]
		public void TestPattern ()
		{
			var matcher = new PatternMatcher ("a.cs");
			Assert.IsTrue (matcher.Match ("a.cs"));
			Assert.IsFalse (matcher.Match ("abcd.cs"));
			Assert.IsFalse (matcher.Match ("1234.cs"));
		}

		[Test]
		public void TestAnyChar ()
		{
			var matcher = new PatternMatcher ("???.cs");
			Assert.IsTrue (matcher.Match ("abc.cs"));
			Assert.IsTrue (matcher.Match ("123.cs"));
			Assert.IsFalse (matcher.Match ("abcd.cs"));
			Assert.IsFalse (matcher.Match ("1234.cs"));
		}

		[Test]
		public void TestZeroOrMoreChar ()
		{
			var matcher = new PatternMatcher ("*");
			Assert.IsTrue (matcher.Match ("abc.cs"));
			Assert.IsTrue (matcher.Match ("abcd.cs"));
		}

		[Test]
		public void TestExtension ()
		{
			var matcher = new PatternMatcher ("*.cs");
			Assert.IsTrue (matcher.Match ("foo.cs"));
			Assert.IsFalse (matcher.Match ("foo.cs.other"));
		}


		/// <summary>
		/// Bug 844872: [Feedback] Find in files file mask not correct
		/// </summary>
		[Test]
		public void TestVSTS844872 ()
		{
			var matcher = new PatternMatcher ("*.*");
			Assert.IsTrue (matcher.Match ("foo.cs"));
			Assert.IsTrue (matcher.Match ("foo.cs.other"));
		}
	}
}