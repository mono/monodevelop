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

using NUnit.Framework;
using System.IO;
using System;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.VersionControl;

namespace MonoDevelop.VersionControl.Tests
{
	[TestFixture]
	public abstract class BaseRepoUtilsTest
	{
		protected string repoLocation = "";
		protected FilePath rootUrl = "";
		protected FilePath rootCheckout;
		protected Repository repo;
		protected Repository repo2;
		protected string DOT_DIR;

		[SetUp]
		public abstract void Setup ();

		[TearDown]
		public virtual void TearDown ()
		{
			DeleteDirectory (rootUrl);
			DeleteDirectory (rootCheckout);
		}

		[Test]
		public abstract void RightRepositoryDetection ();

		[Test]
		public virtual void CheckoutExists ()
		{
			Assert.True (Directory.Exists (rootCheckout + DOT_DIR));
		}

		[Test]
		public virtual void FileIsAdded ()
		{
			FilePath added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new NullProgressMonitor ());

			VersionInfo vi = repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);

			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
			Assert.IsFalse (vi.CanAdd);
		}

		[Test]
		public virtual void FileIsCommitted ()
		{
			FilePath added = rootCheckout + "testfile";

			File.Create (added).Close ();
			repo.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test";
			repo.Commit (changes, new NullProgressMonitor ());

			PostCommit (repo);

			// TODO: Fix remote status for Win32 Svn.
			VersionInfo vi = repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
		}

		protected virtual void PostCommit (Repository repo)
		{
		}

		[Test]
		public virtual void UpdateIsDone ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test";
			repo.Commit (changes, new NullProgressMonitor ());

			PostCommit (repo);

			// Checkout a second repository.
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second, repoLocation);
			repo2 = GetRepo (second, repoLocation);
			added = second + "testfile2";
			File.Create (added).Close ();
			repo2.Add (added, false, new NullProgressMonitor ());
			changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test2";
			repo2.Commit (changes, new NullProgressMonitor ());

			PostCommit (repo2);

			repo.Update (repo.RootPath, true, new NullProgressMonitor ());
			Assert.True (File.Exists (rootCheckout + "testfile2"));

			DeleteDirectory (second);
		}

		[Test]
		public virtual void LogIsProper ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "File committed";
			repo.Commit (changes, new NullProgressMonitor ());
			foreach (Revision rev in repo.GetHistory (added, null)) {
				Assert.AreEqual ("File committed", rev.Message);
			}
		}

		[Test]
		public abstract void DiffIsProper ();

		[Test]
		public virtual void Reverts ()
		{
			string added = rootCheckout + "testfile";
			string content = "text";

			File.Create (added).Close ();
			repo.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "File committed";
			repo.Commit (changes, new NullProgressMonitor ());

			// Revert to head.
			File.WriteAllText (added, content);
			repo.Revert (added, false, new NullProgressMonitor ());
			Assert.AreEqual (repo.GetBaseText (added), File.ReadAllText (added));
		}

		#region Util

		public virtual void Checkout (string path, string url)
		{
			Repository _repo = GetRepo (path, url);
			_repo.Checkout (path, true, new NullProgressMonitor ());
			if (repo == null)
				repo = _repo;
			else
				repo2 = _repo;
		}

		protected abstract Repository GetRepo (string path, string url);

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

			Directory.Delete (path, true);
		}

		#endregion
	}
}

