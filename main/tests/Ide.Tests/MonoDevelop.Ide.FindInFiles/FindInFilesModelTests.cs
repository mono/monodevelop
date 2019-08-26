//
// FindInFilesModelTests.cs
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
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Mono.TextEditor.Utils;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	public class FindInFilesModelTests : IdeTestBase
	{
		[Test]
		public void TestIsFileNameMatching ()
		{
			var model = new FindInFilesModel ();
			model.FileMask = "*.cs";

			Assert.IsTrue (model.IsFileNameMatching ("a.cs"));
			Assert.IsFalse (model.IsFileNameMatching ("a.txt"));

			model.FileMask = "*.cs;*.txt";

			Assert.IsTrue (model.IsFileNameMatching ("a.cs"));
			Assert.IsTrue (model.IsFileNameMatching ("a.txt"));
			Assert.IsFalse (model.IsFileNameMatching ("a.vb"));
		}

		[Test]
		public void TestEmptyFileMask ()
		{
			var model = new FindInFilesModel ();

			model.FileMask = "";
			Assert.IsTrue (model.IsFileNameMatching ("a.txt"));
			Assert.IsTrue (model.IsFileNameMatching ("a.vb"));

			model.FileMask = null;
			Assert.IsTrue (model.IsFileNameMatching ("a.txt"));
			Assert.IsTrue (model.IsFileNameMatching ("a.vb"));
		}

		[Test]
		public void TestPatternMatcherPattern ()
		{
			var model = new FindInFilesModel ();

			model.FindPattern = "foo";
			string text = "foo foo foo foo";

			Assert.AreEqual (4, model.PatternSearcher.FindAll (null, text).Length);
		}


		[Test]
		public void TestEmptyFindPattern ()
		{
			var model = new FindInFilesModel ();

			model.FindPattern = null;
			Assert.AreEqual (-1, model.PatternSearcher.Find ("foo", 0, 3));
		}

		[Test]
		public void TestCaseSensitivity ()
		{
			var model = new FindInFilesModel ();

			model.FindPattern = "fOo";
			string text = "foo fOo foo fOo";
			model.CaseSensitive = false;
			Assert.AreEqual (4, model.PatternSearcher.FindAll (null, text).Length);

			model.CaseSensitive = true;
			Assert.AreEqual (2, model.PatternSearcher.FindAll (null, text).Length);
		}

		[Test]
		public void TestWholeWordsOnly ()
		{
			var model = new FindInFilesModel ();

			model.FindPattern = "foo";
			string text = "foo foofoo foo foo";
			model.WholeWordsOnly = false;
			Assert.AreEqual (5, model.PatternSearcher.FindAll (null, text).Length);

			model.WholeWordsOnly = true;
			Assert.AreEqual (3, model.PatternSearcher.FindAll (null, text).Length);
		}

	}
}
