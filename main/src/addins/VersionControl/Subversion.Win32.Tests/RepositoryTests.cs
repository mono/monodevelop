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
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion;
using NUnit.Framework;
using SubversionAddinWindows;
using System.Diagnostics;
using System.IO;
using System;

namespace MonoDevelop.VersionControl.Subversion.Tests
{
	[TestFixture]
	sealed class SharpSvnUtilsTest : BaseSvnUtilsTest
	{
		[SetUp]
		public override void Setup ()
		{
			RemotePath = new FilePath (FileService.CreateTempDirectory ());
			RemoteUrl = "svn://localhost:3690/repo";
			SvnServe = new Process ();
			base.Setup ();
		}

		[TearDown]
		public override void TearDown ()
		{
			SvnServe.Kill ();

			base.TearDown ();
		}

		protected override void TestDiff ()
		{
			string difftext = @"--- testfile	(revision 1)
+++ testfile	(working copy)
@@ -0,0 +1 @@
+text
\ No newline at end of file
";
			Assert.AreEqual (difftext, Repo.GenerateDiff (LocalPath + "testfile", Repo.GetVersionInfo (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content.Replace ("\n", "\r\n"));
		}

		// Tests that fail due to SvnSharp giving wrong data.
		[Test]
		[Ignore ("Fix SvnSharp")]
		public override void MovesDirectory ()
		{
			base.MovesDirectory ();
		}

		[Test]
		[Ignore ("Fix SvnSharp")]
		public override void DeletesDirectory ()
		{
			base.DeletesDirectory ();
		}

		[Test]
		[Ignore ("Throws SvnAuthorizationException on Lock")]
		public override void LocksEntities ()
		{
			base.LocksEntities ();
		}

		protected override void PostLock ()
		{
			string added = LocalPath + "testfile";
			Assert.Throws<Exception> (delegate {
				File.WriteAllText (added, "text");
			});
		}

		[Test]
		[Ignore ("Throws SvnAuthorizationException on Lock")]
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

		protected override void TestValidUrl ()
		{
			base.TestValidUrl ();

			Assert.Ignore ("File scheme is broken on Windows");
			var repo2 = (SubversionRepository)Repo;
			Assert.IsTrue (repo2.IsUrlValid ("file:///c:/dir/repo"));
		}

		[Test]
		[Ignore ("Url gets broken. ")]
		public override void CorrectTextAtRevision ()
		{
			base.CorrectTextAtRevision ();
		}

		protected override Repository GetRepo (string path, string url)
		{
			return new SubversionRepository (new SvnSharpClient (), url, path);
		}
	}
}

