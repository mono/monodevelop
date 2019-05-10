//
// PatternSearcherTests.cs
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
using System.Text;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class PatternSearcherTests
	{
		[Test]
		public void TestFind ()
		{
			var searcher = new PatternSearcher ("foo", true, false);

			var idx = searcher.Find ("testfoo", 0, "testfoo".Length);

			Assert.AreEqual ("test".Length, idx);
		}

		[Test]
		public void TestFindAll ()
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < 100; i++)
				sb.Append ("foo");

			var searcher = new PatternSearcher ("foo", true, false);

			var indices = searcher.FindAll (sb.ToString ());

			Assert.AreEqual (100, indices.Length);
		}

		[Test]
		public void TestCaseSensitive ()
		{
			var searcher = new PatternSearcher ("foo", true, false);

			string text = "fooFoofoo";
			var indices = searcher.FindAll (text);

			Assert.AreEqual (2, indices.Length);
		}

		[Test]
		public void TestCaseInsensitive ()
		{
			var searcher = new PatternSearcher ("fOO", false, false);

			string text = "fooFooFOO";
			var indices = searcher.FindAll (text);

			Assert.AreEqual (3, indices.Length);
		}



		[Test]
		public void TestWholeWordsOnly ()
		{
			var searcher = new PatternSearcher ("foo", true, true);

			string text = "foo fooFoofoo foo foo";
			var indices = searcher.FindAll (text);

			Assert.AreEqual (3, indices.Length);
		}

	}
}