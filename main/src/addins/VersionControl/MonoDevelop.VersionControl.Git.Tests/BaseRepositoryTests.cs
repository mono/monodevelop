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
using System.Collections.Generic;

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
		protected List<string> addedItems = new List<string> ();
		protected int commitNumber = 0;

		[SetUp]
		public abstract void Setup ();

		[TearDown]
		public virtual void TearDown ()
		{
			DeleteDirectory (rootUrl);
			DeleteDirectory (rootCheckout);
			addedItems.Clear ();
			commitNumber = 0;
		}

		[Test]
		// Tests VersionControlService.GetRepositoryReference.
		public abstract void RightRepositoryDetection ();

		[Test]
		// Tests Repository.Checkout.
		public virtual void CheckoutExists ()
		{
			Assert.True (Directory.Exists (rootCheckout + DOT_DIR));
		}

		[Test]
		// Tests Repository.Add.
		public virtual void FileIsAdded ()
		{
			AddFile ("testfile", null, true, false);

			VersionInfo vi = repo.GetVersionInfo (rootCheckout + "testfile", VersionInfoQueryFlags.IgnoreCache);

			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
			Assert.IsFalse (vi.CanAdd);
		}

		[Test]
		// Tests Repository.Commit.
		public virtual void FileIsCommitted ()
		{
			AddFile ("testfile", null, true, true);
			PostCommit (repo);

			VersionInfo vi = repo.GetVersionInfo (rootCheckout + "testfile", VersionInfoQueryFlags.IncludeRemoteStatus | VersionInfoQueryFlags.IgnoreCache);
			// TODO: Fix Win32 Svn Remote status check.
			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
		}

		protected virtual void PostCommit (Repository repo)
		{
		}

		[Test]
		// Tests Repository.Update.
		public virtual void UpdateIsDone ()
		{
			AddFile ("testfile", null, true, true);
			PostCommit (repo);

			// Checkout a second repository.
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second, repoLocation);
			repo2 = GetRepo (second, repoLocation);
			string added = second + "testfile2";
			File.Create (added).Close ();
			repo2.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test2";
			repo2.Commit (changes, new NullProgressMonitor ());

			PostCommit (repo2);

			repo.Update (repo.RootPath, true, new NullProgressMonitor ());
			Assert.True (File.Exists (rootCheckout + "testfile2"));

			DeleteDirectory (second);
		}

		[Test]
		// Tests Repository.GetHistory.
		public virtual void LogIsProper ()
		{
			AddFile ("testfile", null, true, true);
			AddFile ("testfile2", null, true, true);
			int index = 0;
			foreach (Revision rev in repo.GetHistory (rootCheckout + "testfile", null)) {
				Assert.AreEqual (String.Format ("Commit #{0}", index++), rev.Message);
			}
		}

		[Test]
		// Tests Repository.GenerateDiff.
		public virtual void DiffIsProper ()
		{
			AddFile ("testfile", null, true, true);
			File.AppendAllText (rootCheckout + "testfile", "text" + Environment.NewLine);
		}

		[Test]
		// Tests Repository.Revert and Repository.GetBaseText.
		public virtual void Reverts ()
		{
			string content = "text";
			AddFile ("testfile", null, true, true);
			string added = rootCheckout + "testfile";

			// Revert to head.
			File.WriteAllText (added, content);
			repo.Revert (added, false, new NullProgressMonitor ());
			Assert.AreEqual (repo.GetBaseText (added), File.ReadAllText (added));
		}

		// TODO: Remaining tests for Repository class.
		// Tests Repository.GetRevisionChanges.
		public virtual void CorrectRevisionChanges ()
		{
			AddFile ("testfile", "text", true, true);
			// Override and test in specific with GitRevision and SvnRevision.
		}

		// Tests Repository.RevertRevision.
		[Test]
		[Ignore]
		public virtual void RevertsRevision ()
		{
			AddFile ("testfile", "text", true, true);
			// Override and test in specific with GitRevision and SvnRevision.
		}

		// Tests Repository.MoveFile.
		[Test]
		public virtual void MovesFile ()
		{
			string src = rootCheckout + "testfile";
			string dst = src + "2";

			AddFile ("testfile", "text", true, true);
			repo.MoveFile (src, dst, false, new NullProgressMonitor ());
			// Win32 Subversion shows srcVi as Unversioned.
			// Test for Unix Subversion.
			VersionInfo srcVi = repo.GetVersionInfo (src, VersionInfoQueryFlags.IgnoreCache);
			VersionInfo dstVi = repo.GetVersionInfo (dst, VersionInfoQueryFlags.IgnoreCache);
			VersionStatus expectedStatus = VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace;
			Assert.AreNotEqual (VersionStatus.Unversioned, srcVi.Status & expectedStatus);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);
		}

		[Test]
		// Tests Repository.MoveDirectory.
		public virtual void MovesDirectory ()
		{
		}

		[Test]
		// Tests Repository.DeleteFile.
		public virtual void DeletesFile ()
		{
		}

		[Test]
		// Tests Repository.DeleteDirectory.
		public virtual void DeletesDirectory ()
		{
		}

		[Test]
		// Tests Repository.CreateLocalDirectory.
		public virtual void CreatesDirectory ()
		{
		}

		[Test]
		// Tests Repository.Lock.
		public virtual void LocksEntities ()
		{
		}

		[Test]
		// Tests Repository.Unlock.
		public virtual void UnlocksEntities ()
		{
		}

		[Test]
		// Tests Repository.Ignore
		public virtual void IgnoresEntities ()
		{
		}

		[Test]
		// Tests Repository.Unignore
		public virtual void UnignoresEntities ()
		{
		}

		[Test]
		// Tests Repository.GetTextAtRevision.
		public virtual void CorrectTextAtRevision ()
		{
		}

		[Test]
		// Tests Repository.GetAnnotations.
		public virtual void BlameIsCorrect ()
		{
		}

		#region Util

		protected void Checkout (string path, string url)
		{
			Repository _repo = GetRepo (path, url);
			_repo.Checkout (path, true, new NullProgressMonitor ());
			if (repo == null)
				repo = _repo;
			else
				repo2 = _repo;
		}

		protected void CommitItems ()
		{
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			foreach (var item in addedItems) {
				changes.AddFile (repo.GetVersionInfo (item, VersionInfoQueryFlags.IgnoreCache));
			}
			changes.GlobalComment = String.Format ("Commit #{0}", commitNumber);
			repo.Commit (changes, new NullProgressMonitor ());
			commitNumber++;
		}

		protected void CommitFile (string path)
		{
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = String.Format ("Commit #{0}", commitNumber);
			repo.Commit (changes, new NullProgressMonitor ());
			commitNumber++;
		}

		protected void AddFile (string path, string contents, bool toVcs, bool commit)
		{
			AddToRepository (path, contents ?? "", toVcs, commit);
		}

		protected void AddDirectory (string path, bool toVcs, bool commit)
		{
			AddToRepository (path, null, toVcs, commit);
		}

		void AddToRepository (string relativePath, string contents, bool toVcs, bool commit)
		{
			string added = Path.Combine (rootCheckout, relativePath);
			if (contents == null)
				Directory.CreateDirectory (added);
			else
				File.WriteAllText (added, contents);

			if (toVcs)
				repo.Add (added, false, new NullProgressMonitor ());

			if (commit)
				CommitFile (added);
			else
				addedItems.Add (added);
		}

		protected abstract Repository GetRepo (string path, string url);

		protected static void DeleteDirectory (string path)
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

