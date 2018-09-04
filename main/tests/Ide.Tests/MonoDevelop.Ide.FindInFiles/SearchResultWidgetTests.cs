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

	}
}
