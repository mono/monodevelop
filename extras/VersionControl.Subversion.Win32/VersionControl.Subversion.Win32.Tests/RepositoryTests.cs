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
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.VersionControl;
using MonoDevelop.VersionControl.Subversion;
using NUnit.Framework;
using SubversionAddinWindows;
using System.Diagnostics;
using System.IO;
using System;

namespace VersionControl.Subversion.Win32.Tests
{
	[TestFixture]
	public class SharpSvnUtilsTest : MonoDevelop.VersionControl.Subversion.Tests.BaseSvnUtilsTest
	{
		[SetUp]
		public override void Setup ()
		{
			rootUrl = new FilePath (FileService.CreateTempDirectory ());
			repoLocation = "svn://localhost:3690/repo";
			backend = new SvnSharpBackend ();
			svnServe = new Process ();
			base.Setup ();
		}

		[TearDown]
		public override void TearDown ()
		{
			svnServe.Kill ();

			base.TearDown ();
		}

		[Test]
		public override void LogIsProper ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { rootCheckout }, "File committed", new NullProgressMonitor ());
			foreach (var rev in backend.Log (repo, added, SvnRevision.First, SvnRevision.Working)) {
				Assert.AreEqual ("File committed", rev.Message);
				foreach (var change in rev.ChangedFiles) {
					Assert.AreEqual (RevisionAction.Add, change.Action);
					Assert.AreEqual ("/testfile", change.Path);
				}
			}
		}

		[Test]
		public override void DiffIsProper ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { rootCheckout }, "File committed", new NullProgressMonitor ());
			File.AppendAllText (added, "text" + Environment.NewLine);

			string difftext = @"Index: " + added.Replace ('\\', '/') + @"
===================================================================
--- " + added.Replace ('\\', '/') + @"	(revision 1)
+++ " + added.Replace ('\\', '/') + @"	(working copy)
@@ -0,0 +1 @@
+text
";
			Assert.AreEqual (difftext, backend.GetUnifiedDiff (added, false, false));
		}

		#region Util

		public override SubversionRepository GetRepo (string url, string path)
		{
			return new SubversionRepository (new SvnSharpClient (), url, path);
		}

		#endregion
	}
}

