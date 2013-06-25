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
	public class SvnUtilsTest
	{
		SvnSharpBackend backend;
		SubversionRepository repo;
		Process svnServe;
		FilePath svnRoot;
		FilePath svnCheckout;

		[SetUp]
		public void Setup ()
		{
			Process svnAdmin;
			ProcessStartInfo info;

			// Generate directories and a svn util.
			svnRoot = new FilePath (FileService.CreateTempDirectory ());
			svnCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			backend = new SvnSharpBackend ();

			// Create repo in "repo".
			svnAdmin = new Process ();
			info = new ProcessStartInfo ();
			info.FileName = "svnadmin";
			info.Arguments = "create " + svnRoot + Path.DirectorySeparatorChar + "repo";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			svnAdmin.StartInfo = info;
			svnAdmin.Start ();
			svnAdmin.WaitForExit ();

			// Create user to auth.
			using (var perm = File. CreateText (svnRoot + Path.DirectorySeparatorChar + "repo" +
			                                  Path.DirectorySeparatorChar + "conf" + Path.DirectorySeparatorChar + "svnserve.conf")) {
				perm.WriteLine ("[general]");
				perm.WriteLine ("anon-access = write");
				perm.WriteLine ("[sasl]");
			}

			// Create host.
			svnServe = new Process ();
			info = new ProcessStartInfo ();
			info.FileName = "svnserve";
			info.Arguments = "-dr " + svnRoot;
			info.WindowStyle = ProcessWindowStyle.Hidden;
			svnServe.StartInfo = info;
			svnServe.Start ();

			// Check out the repository.
			Checkout (svnCheckout);
			repo = GetRepo ("svn://localhost:3690/repo", svnCheckout);
		}

		[TearDown]
		public void TearDown ()
		{
			svnServe.Kill ();

			DeleteDirectory (svnRoot);
			DeleteDirectory (svnCheckout);
		}

		[Test]
		public void CheckoutExists ()
		{
			Assert.True (Directory.Exists (svnCheckout + ".svn"));
		}

		[Test]
		public void FileIsAdded ()
		{
			string added = svnCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());

			foreach (var vi in backend.Status (repo, added, SvnRevision.First))
				Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
		}

		[Test]
		public void FileIsCommitted ()
		{
			string added = svnCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { svnCheckout }, "File committed", new NullProgressMonitor ());

			foreach (var vi in backend.Status (repo, added, SvnRevision.First))
				Assert.AreEqual (VersionStatus.Versioned, vi.Status);
		}

		[Test]
		public void UpdateIsDone ()
		{
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second);

			string added = second + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { second }, "Check text", new NullProgressMonitor ());

			backend.Update (svnCheckout, true, new NullProgressMonitor ());
			Assert.True (File.Exists (svnCheckout + "testfile"));
			DeleteDirectory (second);
		}

		[Test]
		public void LogIsProper ()
		{
			string added = svnCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { svnCheckout }, "File committed", new NullProgressMonitor ());
			foreach (var rev in backend.Log (repo, added, SvnRevision.First, SvnRevision.Working)) {
				Assert.AreEqual ("File committed", rev.Message);
				foreach (var change in rev.ChangedFiles) {
					Assert.AreEqual (RevisionAction.Add, change.Action);
					Assert.AreEqual ("/testfile", change.Path);
				}
			}
		}

		[Test]
		public void DiffIsProper ()
		{
			string added = svnCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { svnCheckout }, "File committed", new NullProgressMonitor ());
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

		[Test]
		public void Reverts ()
		{
			string added = svnCheckout + "testfile";
			string content = "text";

			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { svnCheckout }, "File committed", new NullProgressMonitor ());

			// Revert to head.
			File.WriteAllText (added, content);

			backend.Revert (new FilePath[] { added }, false, new NullProgressMonitor ());
			Assert.AreEqual (backend.GetTextBase (added), File.ReadAllText (added));

			// Revert revision.
			File.AppendAllText (added, content);
			File.Copy (added, added + "2");

			backend.Commit (new FilePath[] { added }, "File modified", new NullProgressMonitor ());
			backend.RevertRevision (added, new SvnRevision (repo, 2), new NullProgressMonitor ());
			backend.Commit (new FilePath[] { added }, "File reverted", new NullProgressMonitor ());

			Assert.AreNotEqual (File.ReadAllText (added + "2"), File.ReadAllText (added));
		}

		#region Util

		public void Checkout (string path)
		{
			backend.Checkout ("svn://localhost:3690/repo",
			                  path, SvnRevision.Head,
			                  true, new NullProgressMonitor ());
		}

		public static SubversionRepository GetRepo (string url, string path)
		{
			return new SubversionRepository (new SvnSharpClient (), url, path);
		}

		public static void DeleteDirectory (string path)
		{
			string[] files = Directory.GetFiles (path);
			string[] dirs = Directory.GetDirectories (path);

			foreach (var file in files) {
				File.SetAttributes (file, FileAttributes.Normal);
				File.Delete (file);
			}

			foreach (var dir in dirs) {
				DeleteDirectory (dir);
			}

			Directory.Delete (path);
		}

		#endregion
	}
}

