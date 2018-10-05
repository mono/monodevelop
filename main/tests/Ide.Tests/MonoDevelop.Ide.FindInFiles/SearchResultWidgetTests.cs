//
// SearchResultWidgetTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.IO;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.FindInFiles
{
	[TestFixture]
	class SearchResultWidgetTests : IdeTestBase
	{
		[SetUp]
		public void SetUp ()
		{
			//Initialize IdeApp so IdeApp.Workspace is not null, comment tasks listen to root workspace events.
			if (!IdeApp.IsInitialized)
				IdeApp.Initialize (new ProgressMonitor ());
		}

		[Test]
		public void TestSimple ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();
			var sb = new StringBuilder ();
			sb.AppendLine (new string ('a', 5) + "test" + new string ('b', 5));
			File.WriteAllText (fileName, sb.ToString ());
			try {
				var provider = new FileProvider (fileName);
				var sr = new SearchResult (provider, 5, "test".Length);
				var markup = sr.GetMarkup (widget, true);
				Assert.AreEqual ("aaaaa<span background=\"#E6EA00\">test</span>bbbbb", markup);
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public void TestCropEnd ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();
			var sb = new StringBuilder ();
			sb.AppendLine (new string ('a', 5) + "test" + new string ('b', 100));
			File.WriteAllText (fileName, sb.ToString ());
			try {
				var provider = new FileProvider (fileName);
				var sr = new SearchResult (provider, 5, "test".Length);
				var markup = sr.GetMarkup (widget, true);
				Assert.AreEqual ("aaaaa<span background=\"#E6EA00\">test</span>bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb…", markup);
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public void TestCropStart ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();
			var sb = new StringBuilder ();
			sb.AppendLine (new string ('a', 100) + "test" + new string ('b', 5));
			File.WriteAllText (fileName, sb.ToString ());
			try {
				var provider = new FileProvider (fileName);
				var sr = new SearchResult (provider, 100, "test".Length);
				var markup = sr.GetMarkup (widget, true);
				Assert.AreEqual ("…aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa<span background=\"#E6EA00\">test</span>bbbbb", markup);
			} finally {
				File.Delete (fileName);
			}
		}

		[Test]
		public void TestCropBoth ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();
			var sb = new StringBuilder ();
			sb.AppendLine (new string ('a', 100) + "test" + new string ('b', 100));
			File.WriteAllText (fileName, sb.ToString ());
			try {
				var provider = new FileProvider (fileName);
				var sr = new SearchResult (provider, 100, "test".Length);
				var markup = sr.GetMarkup (widget, true);
				Assert.AreEqual ("…aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa<span background=\"#E6EA00\">test</span>bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb…", markup);
			} finally {
				File.Delete (fileName);
			}
		}

		/// <summary>
		/// Search result text height differences #5949
		/// </summary>
		[Test]
		public void TestIssue5949 ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();
			var sb = new StringBuilder ();
			sb.AppendLine ("test");
			sb.AppendLine ("test");
			sb.AppendLine ("test");
			File.WriteAllText (fileName, sb.ToString ());
			try {
				var provider = new FileProvider (fileName);
				var sr = new SearchResult (provider, 0, "test".Length +1);
				var markup = sr.GetMarkup (widget, true);
				Assert.AreEqual ("<span background=\"#E6EA00\">test</span>", markup);
			} finally {
				File.Delete (fileName);
			}
		}



		/// <summary>
		/// Find references shows invalid search results #6015
		/// </summary>
		[Test]
		public void TestIssue6015 ()
		{
			var widget = new SearchResultWidget ();
			var fileName = Path.GetTempFileName ();

			string file = @"test test test";
			File.WriteAllText (fileName, file);
			var provider = new FileProvider (fileName);

			var idx1 = file.IndexOf ("test", StringComparison.Ordinal);
			var sr1 = new SearchResult (provider, idx1, "test".Length);
			Assert.AreEqual ("<span background=\"#E6EA00\">test</span> test test", sr1.GetMarkup (widget, true));

			file = @"using System;
using System.Collections.Generic;

namespace MyLibrary
{
	class FooBar
	{int test;

		public int Test {
			get {
				return test;
			}
		}

		public FooBar ()
		{
			Console.WriteLine (test);
		}
	}
}";
			File.WriteAllText (fileName, file);
			try {
				provider = new FileProvider (fileName);

				idx1 = file.IndexOf ("test", StringComparison.Ordinal);
				sr1 = new SearchResult (provider, idx1, "test".Length);

				var idx2 = file.IndexOf ("test", idx1 + 1, StringComparison.Ordinal);
				var sr2 = new SearchResult (provider, idx2, "test".Length);

				var idx3 = file.IndexOf ("test", idx2 + 1, StringComparison.Ordinal);
				var sr3 = new SearchResult (provider, idx3, "test".Length);

				Assert.AreEqual ("{int <span background=\"#E6EA00\">test</span>;", sr1.GetMarkup (widget, true));
				Assert.AreEqual ("return <span background=\"#E6EA00\">test</span>;", sr2.GetMarkup (widget, true));
				Assert.AreEqual ("Console.WriteLine (<span background=\"#E6EA00\">test</span>);", sr3.GetMarkup (widget, true));
			} finally {
				File.Delete (fileName);
			}
		}

	}
}
