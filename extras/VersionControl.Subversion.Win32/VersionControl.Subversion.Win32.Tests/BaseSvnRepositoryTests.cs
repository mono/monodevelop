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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.VersionControl.Subversion.Tests
{
	[TestFixture]
	public abstract class BaseSvnUtilsTest : BaseRepoUtilsTest
	{
		protected SubversionBackend backend;
		protected SubversionRepository repo;
		protected Process svnServe = null;

		[SetUp]
		public override void Setup ()
		{
			Process svnAdmin;
			ProcessStartInfo info;

			// Generate directories and a svn util.
			rootCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);

			// Create repo in "repo".
			svnAdmin = new Process ();
			info = new ProcessStartInfo ();
			info.FileName = "svnadmin";
			info.Arguments = "create " + rootUrl + Path.DirectorySeparatorChar + "repo";
			info.WindowStyle = ProcessWindowStyle.Hidden;
			svnAdmin.StartInfo = info;
			svnAdmin.Start ();
			svnAdmin.WaitForExit ();

			// Create host (Win32)
			// This needs to be done after doing the svnAdmin creation.
			// And before checkout.
			if (svnServe != null) {
				info = new ProcessStartInfo ();
				info.FileName = "svnserve";
				info.Arguments = "-dr " + rootUrl;
				info.WindowStyle = ProcessWindowStyle.Hidden;
				svnServe.StartInfo = info;
				svnServe.Start ();

				// Create user to auth.
				using (var perm = File. CreateText (rootUrl + Path.DirectorySeparatorChar + "repo" +
				                                    Path.DirectorySeparatorChar + "conf" + Path.DirectorySeparatorChar + "svnserve.conf")) {
					perm.WriteLine ("[general]");
					perm.WriteLine ("anon-access = write");
					perm.WriteLine ("[sasl]");
				}
			}

			// Check out the repository.
			Checkout (rootCheckout);
			repo = GetRepo (repoLocation, rootCheckout);
		}

		[TearDown]
		public override void TearDown ()
		{
			DeleteDirectory (rootUrl);
			DeleteDirectory (rootCheckout);
		}

		[Test]
		public override void CheckoutExists ()
		{
			Assert.True (Directory.Exists (rootCheckout + ".svn"));
		}

		[Test]
		public override void FileIsAdded ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());

			foreach (var vi in backend.Status (repo, added, SvnRevision.First))
				Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
		}

		[Test]
		public override void FileIsCommitted ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { rootCheckout }, "File committed", new NullProgressMonitor ());

			foreach (var vi in backend.Status (repo, added, SvnRevision.First))
				Assert.AreEqual (VersionStatus.Versioned, vi.Status);
		}

		[Test]
		public override void UpdateIsDone ()
		{
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second);

			string added = second + "testfile";
			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { second }, "Check text", new NullProgressMonitor ());

			backend.Update (rootCheckout, true, new NullProgressMonitor ());
			Assert.True (File.Exists (rootCheckout + "testfile"));
			DeleteDirectory (second);
		}

		[Test]
		public override void Reverts ()
		{
			string added = rootCheckout + "testfile";
			string content = "text";

			File.Create (added).Close ();
			backend.Add (added, false, new NullProgressMonitor ());
			backend.Commit (new FilePath[] { rootCheckout }, "File committed", new NullProgressMonitor ());

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

		public override void Checkout (string path)
		{
			backend.Checkout (repoLocation,
			                  path, SvnRevision.Head,
			                  true, new NullProgressMonitor ());
		}

		public virtual SubversionRepository GetRepo (string url, string path)
		{
			return null;
		}

		#endregion
	}
}

