//
// RepositoryTests.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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

using MonoDevelop.Core;
using MonoDevelop.VersionControl.Subversion;
using MonoDevelop.VersionControl.Subversion.Unix;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System;
using MonoDevelop.VersionControl;

namespace VersionControl.Subversion.Unix.Tests
{
	[TestFixture]
	public class UnixSvnUtilsTest : MonoDevelop.VersionControl.Subversion.Tests.BaseSvnUtilsTest
	{
		SubversionBackend SvnClient {
			get { return repo.Svn; }
		}

		new SubversionRepository repo {
			get { return (SubversionRepository) base.repo; }
		}

		[SetUp]
		public override void Setup ()
		{
			rootUrl = new FilePath (FileService.CreateTempDirectory ());
			repoLocation = "file://" + rootUrl + "/repo";
			base.Setup ();
		}

		[Test]
		public void ListUrls ()
		{
			AddFile ("test", "data", true, true);
			AddDirectory ("foo", true, true);
			var items = SvnClient.ListUrl (repo.Url, false).ToArray ();
			Assert.AreEqual (2, items.Length, "#1");
			Assert.IsTrue (items.Any (item => item.Name == "test" && !item.IsDirectory), "#2a");
			Assert.IsTrue (items.Any (item => item.Name == "foo" && item.IsDirectory), "#2b");
		}

		protected override void TestDiff ()
		{
			string difftext = @"--- testfile	(revision 1)
+++ testfile	(working copy)
@@ -0,0 +1 @@
+text
";
			Assert.AreEqual (difftext, repo.GenerateDiff (rootCheckout + "testfile", repo.GetVersionInfo (rootCheckout + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content);
		}

		protected override Repository GetRepo (string path, string url)
		{
			return new SubversionRepository (new SvnClient (), url, path);
		}
	}
}

