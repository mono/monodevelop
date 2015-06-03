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
	sealed class UnixSvnUtilsTest : MonoDevelop.VersionControl.Subversion.Tests.BaseSvnUtilsTest
	{
		SubversionBackend SvnClient {
			get { return Repo.Svn; }
		}

		new SubversionRepository Repo {
			get { return (SubversionRepository) base.Repo; }
		}

		[SetUp]
		public override void Setup ()
		{
			RemotePath = new FilePath (FileService.CreateTempDirectory ());
			RemoteUrl = "file://" + RemotePath + "/repo";
			base.Setup ();
		}

		[Test]
		public void ListUrls ()
		{
			AddFile ("test", "data", true, true);
			AddDirectory ("foo", true, true);
			var items = SvnClient.ListUrl (Repo.Url, false).ToArray ();
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
\ No newline at end of file
";
			Assert.AreEqual (difftext, Repo.GenerateDiff (LocalPath + "testfile", Repo.GetVersionInfo (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content);
		}

		// Tests that fail due to Subversion giving wrong data.
		[Test]
		[Ignore ("Fix Subversion")]
		public override void MovesDirectory ()
		{
			base.MovesDirectory ();
		}

		[Test]
		[Ignore ("Fix Subversion")]
		public override void DeletesDirectory ()
		{
			base.DeletesDirectory ();
		}

		[Test]
		[Ignore ("Test fails on Lock")]
		public override void LocksEntities ()
		{
			base.UnlocksEntities ();
		}

		protected override void PostLock ()
		{
			string added = LocalPath + "testfile";
			Assert.Throws<Exception> (delegate {
				File.WriteAllText (added, "text");
			});
		}

		[Test]
		[Ignore ("Test fails on Unlock")]
		public override void UnlocksEntities ()
		{
			base.UnlocksEntities ();
		}

		protected override void PostUnlock ()
		{
			string added = LocalPath + "testfile";
			Assert.DoesNotThrow (delegate {
				File.WriteAllText (added, "text");
			});
		}

		[Test]
		public void DoesntDieInReposWithNoRevisions ()
		{
			Repo.GetHistory (LocalPath, null);
		}

		protected override void TestValidUrl ()
		{
			base.TestValidUrl ();
			Assert.IsTrue (Repo.IsUrlValid ("file:///dir/repo"));
		}

		protected override Repository GetRepo (string path, string url)
		{
			return new SubversionRepository (new SvnClient (), url, path);
		}
	}
}

