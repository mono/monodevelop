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
	abstract class BaseRepoUtilsTest
	{
		// [Git] Set user and email.
		protected const string Author = "author";
		protected const string Email = "email@service.domain";

		protected string RepoLocation = "";
		protected FilePath RootUrl = "";
		protected FilePath RootCheckout;
		protected Repository Repo;
		protected Repository Repo2;
		protected string DotDir;
		protected List<string> AddedItems = new List<string> ();
		protected int CommitNumber = 0;

		[SetUp]
		public abstract void Setup ();

		[TearDown]
		public virtual void TearDown ()
		{
			DeleteDirectory (RootUrl);
			DeleteDirectory (RootCheckout);
			AddedItems.Clear ();
			CommitNumber = 0;
		}

		[Test]
		// Tests false positives of repository detection.
		public void IgnoreScatteredDotDir ()
		{
			var working = FileService.CreateTempDirectory ();

			var path = Path.Combine (working, "test");
			var staleGit = Path.Combine (working, ".git");
			var staleSvn = Path.Combine (working, ".svn");
			Directory.CreateDirectory (path);
			Directory.CreateDirectory (staleGit);
			Directory.CreateDirectory (staleSvn);

			Assert.IsNull (VersionControlService.GetRepositoryReference ((path).TrimEnd (Path.DirectorySeparatorChar), null));

			DeleteDirectory (working);
		}

		[Test]
		// Tests VersionControlService.GetRepositoryReference.
		public void RightRepositoryDetection ()
		{
			var path = ((string)RootCheckout).TrimEnd (Path.DirectorySeparatorChar);
			var repo = VersionControlService.GetRepositoryReference (path, null);
			Assert.That (repo, IsCorrectType (), "#1");

			while (!String.IsNullOrEmpty (path)) {
				path = Path.GetDirectoryName (path);
				if (path == null)
					return;
				Assert.IsNull (VersionControlService.GetRepositoryReference (path, null), "#2." + path);
			}

			// Versioned file
			AddFile ("foo", "contents", true, true);
			path = Path.Combine (RootCheckout, "foo");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#2");

			// Versioned directory
			AddDirectory ("bar", true, true);
			path = Path.Combine (RootCheckout, "bar");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#3");

			// Unversioned file
			AddFile ("bip", "contents", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#4");

			// Unversioned directory
			AddDirectory ("bop", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#5");

			// Nonexistent file
			path = Path.Combine (RootCheckout, "do_i_exist");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#6");

			// Nonexistent directory
			path = Path.Combine (RootCheckout, "do", "i", "exist");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#6");
		}

		protected abstract NUnit.Framework.Constraints.IResolveConstraint IsCorrectType ();

		[Test]
		public void UrlIsValid ()
		{
			TestValidUrl ();
		}

		protected abstract void TestValidUrl ();

		[Test]
		// Tests Repository.Checkout.
		public void CheckoutExists ()
		{
			Assert.IsTrue (Directory.Exists (RootCheckout + DotDir));
		}

		[Test]
		// Tests Repository.Add.
		public void FileIsAdded ()
		{
			AddFile ("testfile", null, true, false);

			VersionInfo vi = Repo.GetVersionInfo (RootCheckout + "testfile", VersionInfoQueryFlags.IgnoreCache);

			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
			Assert.IsFalse (vi.CanAdd);
		}

		[Test]
		// Tests Repository.Commit.
		public void FileIsCommitted ()
		{
			AddFile ("testfile", null, true, true);
			PostCommit (Repo);

			VersionInfo vi = Repo.GetVersionInfo (RootCheckout + "testfile", VersionInfoQueryFlags.IncludeRemoteStatus | VersionInfoQueryFlags.IgnoreCache);
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
			PostCommit (Repo);

			// Checkout a second repository.
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second, RepoLocation);
			Repo2 = GetRepo (second, RepoLocation);
			string added = second + "testfile2";
			File.Create (added).Close ();
			Repo2.Add (added, false, new NullProgressMonitor ());
			ChangeSet changes = Repo.CreateChangeSet (Repo.RootPath);
			changes.AddFile (Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test2";
			Repo2.Commit (changes, new NullProgressMonitor ());

			PostCommit (Repo2);

			Repo.Update (Repo.RootPath, true, new NullProgressMonitor ());
			Assert.True (File.Exists (RootCheckout + "testfile2"));

			DeleteDirectory (second);
		}

		[Test]
		// Tests Repository.GetHistory.
		public void LogIsProper ()
		{
			AddFile ("testfile", null, true, true);
			AddFile ("testfile2", null, true, true);
			int index = 0;
			foreach (Revision rev in Repo.GetHistory (RootCheckout + "testfile", null)) {
				Assert.AreEqual (String.Format ("Commit #{0}", index++), rev.Message);
			}
		}

		[Test]
		// Tests Repository.GenerateDiff.
		public void DiffIsProper ()
		{
			AddFile ("testfile", null, true, true);
			File.AppendAllText (RootCheckout + "testfile", "text" + Environment.NewLine);

			TestDiff ();
		}

		protected abstract void TestDiff ();

		[Test]
		// Tests Repository.Revert and Repository.GetBaseText.
		public void Reverts ()
		{
			string content = "text";
			AddFile ("testfile", null, true, true);
			string added = RootCheckout + "testfile";

			// Revert to head.
			File.WriteAllText (added, content);
			Repo.Revert (added, false, new NullProgressMonitor ());
			Assert.AreEqual (Repo.GetBaseText (added), File.ReadAllText (added));
		}

		[Test]
		// Tests Repository.GetRevisionChanges.
		public void CorrectRevisionChanges ()
		{
			AddFile ("testfile", "text", true, true);
			// TODO: Extend and test each member and more types.
			foreach (var rev in Repo.GetRevisionChanges (GetHeadRevision ())) {
				Assert.AreEqual (rev.Action, RevisionAction.Add);
			}
		}

		protected abstract Revision GetHeadRevision ();

		[Test]
		// Tests Repository.RevertRevision.
		public virtual void RevertsRevision ()
		{
			string added = RootCheckout + "testfile2";
			AddFile ("testfile", "text", true, true);
			AddFile ("testfile2", "text2", true, true);
			Repo.RevertRevision (added, GetHeadRevision (), new NullProgressMonitor ());
			Assert.IsFalse (File.Exists (added));
		}

		[Test]
		// Tests Repository.MoveFile.
		public virtual void MovesFile ()
		{
			string src = RootCheckout + "testfile";
			string dst = src + "2";

			AddFile ("testfile", null, true, true);
			Repo.MoveFile (src, dst, false, new NullProgressMonitor ());
			VersionInfo srcVi = Repo.GetVersionInfo (src, VersionInfoQueryFlags.IgnoreCache);
			VersionInfo dstVi = Repo.GetVersionInfo (dst, VersionInfoQueryFlags.IgnoreCache);
			const VersionStatus expectedStatus = VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace;
			Assert.AreNotEqual (VersionStatus.Unversioned, srcVi.Status & expectedStatus);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);
		}

		[Test]
		// Tests Repository.MoveDirectory.
		public virtual void MovesDirectory ()
		{
			string srcDir = RootCheckout + "test";
			string dstDir = RootCheckout + "test2";
			string src = srcDir + Path.DirectorySeparatorChar + "testfile";
			string dst = dstDir + Path.DirectorySeparatorChar + "testfile";

			AddDirectory ("test", true, false);
			AddFile ("test" + Path.DirectorySeparatorChar + "testfile", null, true, true);

			Repo.MoveDirectory (srcDir, dstDir, false, new NullProgressMonitor ());
			VersionInfo srcVi = Repo.GetVersionInfo (src, VersionInfoQueryFlags.IgnoreCache);
			VersionInfo dstVi = Repo.GetVersionInfo (dst, VersionInfoQueryFlags.IgnoreCache);
			const VersionStatus expectedStatus = VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace;
			Assert.AreNotEqual (VersionStatus.Unversioned, srcVi.Status & expectedStatus);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);
		}

		[Test]
		// Tests Repository.DeleteFile.
		public virtual void DeletesFile ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", null, true, true);
			Repo.DeleteFile (added, true, new NullProgressMonitor ());
			VersionInfo vi = Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledDelete, vi.Status & VersionStatus.ScheduledDelete);
		}

		[Test]
		// Tests Repository.DeleteDirectory.
		public virtual void DeletesDirectory ()
		{
			string addedDir = RootCheckout + "test";
			string added = addedDir + Path.DirectorySeparatorChar + "testfile";
			AddDirectory ("test", true, false);
			AddFile ("testfile", null, true, true);

			Repo.DeleteDirectory (addedDir, true, new NullProgressMonitor ());
			VersionInfo vi = Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledDelete, vi.Status & VersionStatus.ScheduledDelete);
		}

		[Test]
		// Tests Repository.Lock.
		public virtual void LocksEntities ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", null, true, true);
			Repo.Lock (new NullProgressMonitor (), added);

			PostLock ();
		}

		protected virtual void PostLock ()
		{
		}

		[Test]
		// Tests Repository.Unlock.
		public virtual void UnlocksEntities ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", null, true, true);
			Repo.Lock (new NullProgressMonitor (), "testfile");
			Repo.Unlock (new NullProgressMonitor (), added);

			PostLock ();
		}

		protected virtual void PostUnlock ()
		{
		}

		[Test]
		// Tests Repository.Ignore
		public virtual void IgnoresEntities ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", null, false, false);
			Repo.Ignore (new FilePath[] { added });
			VersionInfo vi = Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Ignored, vi.Status & VersionStatus.Ignored);
		}

		[Test]
		// Tests Repository.Unignore
		public virtual void UnignoresEntities ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", null, false, false);
			Repo.Ignore (new FilePath[] { added });
			Repo.Unignore (new FilePath[] { added });
			VersionInfo vi = Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
		}

		[Test]
		[Ignore]
		// TODO: Fix Subversion for Unix not returning the correct value.
		// TODO: Fix SvnSharp logic failing to generate correct URL.
		// TODO: Fix Git TreeWalk failing.
		// Tests Repository.GetTextAtRevision.
		public void CorrectTextAtRevision ()
		{
			string added = RootCheckout + "testfile";
			AddFile ("testfile", "text1", true, true);
			File.AppendAllText (added, "text2");
			CommitFile (added);
			string text = Repo.GetTextAtRevision (RootCheckout, GetHeadRevision ());
			Assert.AreEqual ("text1text2", text);
		}

		[Test]
		// Tests Repository.GetAnnotations.
		public void BlameIsCorrect ()
		{
			string added = RootCheckout + "testfile";
			// Initial commit.
			AddFile ("testfile", "blah" + Environment.NewLine, true, true);
			// Second commit.
			File.AppendAllText (added, "wut" + Environment.NewLine);
			CommitFile (added);
			// Working copy.
			File.AppendAllText (added, "wut2" + Environment.NewLine);

			var annotations = Repo.GetAnnotations (added);
			for (int i = 0; i < 2; i++) {
				var annotation = annotations [i];
				Assert.IsTrue (annotation.HasDate);
				Assert.IsNotNull (annotation.Date);
			}

			BlameExtraInternals (annotations);
			
			Assert.False (annotations [2].HasEmail);
			Assert.IsNotNull (annotations [2].Author);
			Assert.IsNull (annotations [2].Email);
			Assert.AreEqual (annotations [2].Revision, GettextCatalog.GetString ("working copy"));
			Assert.AreEqual (annotations [2].Author, "<uncommitted>");
		}

		protected abstract void BlameExtraInternals (Annotation [] annotations);

		#region Util

		protected void Checkout (string path, string url)
		{
			Repository _repo = GetRepo (path, url);
			_repo.Checkout (path, true, new NullProgressMonitor ());
			if (Repo == null)
				Repo = _repo;
			else
				Repo2 = _repo;
		}

		protected void CommitItems ()
		{
			ChangeSet changes = Repo.CreateChangeSet (Repo.RootPath);
			foreach (var item in AddedItems) {
				changes.AddFile (Repo.GetVersionInfo (item, VersionInfoQueryFlags.IgnoreCache));
			}
			changes.GlobalComment = String.Format ("Commit #{0}", CommitNumber);
			Repo.Commit (changes, new NullProgressMonitor ());
			CommitNumber++;
		}

		protected void CommitFile (string path)
		{
			ChangeSet changes = Repo.CreateChangeSet (Repo.RootPath);

			// [Git] Needed by build bots.
			changes.ExtendedProperties.Add ("Git.AuthorName", "author");
			changes.ExtendedProperties.Add ("Git.AuthorEmail", "email@service.domain");

			changes.AddFile (Repo.GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = String.Format ("Commit #{0}", CommitNumber);
			Repo.Commit (changes, new NullProgressMonitor ());
			CommitNumber++;
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
			string added = Path.Combine (RootCheckout, relativePath);
			if (contents == null)
				Directory.CreateDirectory (added);
			else
				File.WriteAllText (added, contents);

			if (toVcs)
				Repo.Add (added, false, new NullProgressMonitor ());

			if (commit)
				CommitFile (added);
			else
				AddedItems.Add (added);
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

