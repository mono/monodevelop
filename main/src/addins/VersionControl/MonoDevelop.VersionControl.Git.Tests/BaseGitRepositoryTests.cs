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
using MonoDevelop.VersionControl.Git;
using MonoDevelop.VersionControl.Tests;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	sealed class BaseGitUtilsTest : BaseRepoUtilsTest
	{
		[SetUp]
		public override void Setup ()
		{
			// Generate directories and a svn util.
			RemotePath = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			LocalPath = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Directory.CreateDirectory (RemotePath.FullPath + "repo.git");
			RemoteUrl = "file://" + (Platform.IsWindows ? "/" : "") + RemotePath.FullPath + "repo.git";

			LibGit2Sharp.Repository.Init (RemotePath.FullPath + "repo.git", true);

			// Check out the repository.
			Checkout (LocalPath, RemoteUrl);
			Repo = GetRepo (LocalPath, RemoteUrl);
			((GitRepository)Repo).RootRepository.Config.Set ("core.ignorecase", false);
			ModifyPath (Repo, ref LocalPath);
			DotDir = ".git";

			base.Setup ();
		}

		protected override NUnit.Framework.Constraints.IResolveConstraint IsCorrectType ()
		{
			return Is.InstanceOf<GitRepository> ();
		}

		protected override int RepoItemsCount {
			get { return 1; }
		}

		protected override int RepoItemsCountRecursive {
			get { return 10; }
		}

		protected override void TestDiff ()
		{
			string difftext = @"--- a/testfile
+++ b/testfile
@@ -0,0 +1 @@
+text
\ No newline at end of file
";
			if (Platform.IsWindows)
				difftext = difftext.Replace ("\r\n", "\n");
			Assert.AreEqual (difftext, Repo.GenerateDiff (LocalPath + "testfile", Repo.GetVersionInfo (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content);
		}

		protected override void ModifyPath (Repository repo, ref FilePath old)
		{
			var repo2 = (GitRepository)repo;
			old = repo2.RootRepository.Info.WorkingDirectory;
			repo2.RootRepository.Config.Set<string> ("user.name", Author);
			repo2.RootRepository.Config.Set<string> ("user.email", Email);
		}

		protected override void CheckLog (Repository repo)
		{
			int index = 2;
			foreach (Revision rev in Repo.GetHistory (LocalPath, null))
				Assert.AreEqual (String.Format ("Commit #{0}\n", index--), rev.Message);
		}

		[Test]
		[Ignore ("Not implemented in GitRepository.")]
		public override void LocksEntities ()
		{
			base.LocksEntities ();
		}

		[Test]
		[Ignore ("Not implemented in GitRepository.")]
		public override void UnlocksEntities ()
		{
			base.UnlocksEntities ();
		}

		protected override Revision GetHeadRevision ()
		{
			var repo2 = (GitRepository)Repo;
			return new GitRevision (Repo, repo2.RootRepository, repo2.RootRepository.Head.Tip);
		}

		protected override void PostCommit (Repository repo)
		{
			var repo2 = (GitRepository)repo;
			repo2.Push (new ProgressMonitor (), repo2.GetCurrentRemote (), repo2.GetCurrentBranch ());
		}

		protected override void BlameExtraInternals (Annotation [] annotations)
		{
			for (int i = 0; i < 2; i++) {
				Assert.IsTrue (annotations [i].HasEmail);
				Assert.AreEqual (Author, annotations [i].Author);
				Assert.AreEqual (String.Format ("<{0}>", Email), annotations [i].Email);
			}
			Assert.IsFalse (annotations [2].HasDate);
		}

		[Test]
		public void TestGitStash ()
		{
			LibGit2Sharp.Stash stash;
			var repo2 = (GitRepository)Repo;
			AddFile ("file2", "nothing", true, true);
			AddFile ("file1", "text", true, false);
			AddFile ("file3", "unstaged", false, false);
			AddFile ("file4", "noconflict", true, false);
			repo2.TryCreateStash (new ProgressMonitor (), "meh", out stash);
			Assert.IsTrue (!File.Exists (LocalPath + "file1"), "Stash creation failure");
			AddFile ("file4", "conflict", true, true);
			repo2.PopStash (new ProgressMonitor (), 0);

			VersionInfo vi = repo2.GetVersionInfo (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file1"), "Stash pop staged failure");
			Assert.AreEqual (VersionStatus.ScheduledAdd, vi.Status & VersionStatus.ScheduledAdd, "Stash pop failure");

			vi = repo2.GetVersionInfo (LocalPath + "file3", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file3"), "Stash pop untracked failure");
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status, "Stash pop failure");

			vi = repo2.GetVersionInfo (LocalPath + "file4", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file4"), "Stash pop conflict failure");
			Assert.AreEqual (VersionStatus.Conflicted, vi.Status & VersionStatus.Conflicted, "Stash pop failure");
		}

		[Test]
		public void TestStashNoExtraCommit ()
		{
			LibGit2Sharp.Stash stash;
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", null, true, true);
			AddFile ("file2", null, true, false);
			AddFile ("file3", null, false, false);

			int commitCount = repo2.GetHistory (repo2.RootPath, null).Length;
			repo2.TryCreateStash (new ProgressMonitor (), "stash1", out stash);
			repo2.PopStash (new ProgressMonitor (), 0);
			Assert.AreEqual (commitCount, repo2.GetHistory (repo2.RootPath, null).Length, "stash1 added extra commit.");

			repo2.TryCreateStash (new ProgressMonitor (), "stash2", out stash);

			AddFile ("file4", null, true, true);
			commitCount = repo2.GetHistory (repo2.RootPath, null).Length;

			repo2.PopStash (new ProgressMonitor (), 0);
			Assert.AreEqual (commitCount, repo2.GetHistory (repo2.RootPath, null).Length, "stash2 added extra commit.");
		}

		static string GetStashMessageForBranch (string branch)
		{
			return string.Format ("On {0}: __MD_{0}\n\n", branch);
		}
		[Test]
		public void TestGitBranchCreation ()
		{
			var repo2 = (GitRepository)Repo;

			AddFile ("file1", "text", true, true);
			repo2.CreateBranch ("branch1", null, null);

			repo2.SwitchToBranch (new ProgressMonitor (), "branch1");
			// Nothing could be stashed for master. Branch1 should be popped in any case if it exists.
			Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("master")));
			Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));

			Assert.AreEqual ("branch1", repo2.GetCurrentBranch ());
			Assert.IsTrue (File.Exists (LocalPath + "file1"), "Branch not inheriting from current.");

			AddFile ("file2", "text", true, false);
			repo2.CreateBranch ("branch2", null, null);

			repo2.SwitchToBranch (new ProgressMonitor (), "branch2");
			// Branch1 has a stash created and assert clean workdir. Branch2 should be popped in any case.
			Assert.IsTrue (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
			Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch2")));
			Assert.IsTrue (!File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed");

			AddFile ("file2", "text", true, false);
			repo2.SwitchToBranch (new ProgressMonitor (), "branch1");
			// Branch2 has a stash created. Branch1 should be popped with file2 reinstated.
			Assert.True (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch2")));
			Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
			Assert.IsTrue (File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed correctly");

			repo2.SwitchToBranch (new ProgressMonitor (), "master");
			repo2.RemoveBranch ("branch1");
			Assert.IsFalse (repo2.GetBranches ().Any (b => b.FriendlyName == "branch1"), "Failed to delete branch");

			repo2.RenameBranch ("branch2", "branch3");
			Assert.IsTrue (repo2.GetBranches ().Any (b => b.FriendlyName == "branch3") && repo2.GetBranches ().All (b => b.FriendlyName != "branch2"), "Failed to rename branch");

			// TODO: Add CreateBranchFromCommit tests.
		}

		[Test]
		public void TestGitSyncBranches ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", "text", true, true);
			PostCommit (repo2);

			repo2.CreateBranch ("branch3", null, null);
			repo2.SwitchToBranch (new ProgressMonitor (), "branch3");
			AddFile ("file2", "asdf", true, true);
			repo2.Push (new ProgressMonitor (), "origin", "branch3");

			repo2.SwitchToBranch (new ProgressMonitor (), "master");

			repo2.CreateBranch ("branch4", "origin/branch3", "refs/remotes/origin/branch3");
			repo2.SwitchToBranch (new ProgressMonitor (), "branch4");
			Assert.IsTrue (File.Exists (LocalPath + "file2"), "Tracking remote is not grabbing correct commits");
		}

		[Test]
		public void TestPushChangeset ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file", "meh", true, true);
			PostCommit (repo2);

			AddFile ("file1", "text", true, true);
			AddFile ("file2", "text2", true, true);

			ChangeSet diff = repo2.GetPushChangeSet ("origin", "master");
			Assert.AreEqual (2, diff.Items.Count ());

			ChangeSetItem item = diff.GetFileItem (LocalPath + "file1");
			Assert.IsNotNull (item);
			Assert.AreEqual (VersionStatus.ScheduledAdd, item.Status & VersionStatus.ScheduledAdd);

			item = diff.GetFileItem (LocalPath + "file1");
			Assert.IsNotNull (item);
			Assert.AreEqual (VersionStatus.ScheduledAdd, item.Status & VersionStatus.ScheduledAdd);
		}

		[Test]
		public void TestPushDiff ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file", "meh", true, true);
			PostCommit (repo2);

			AddFile ("file1", "text", true, true);
			AddFile ("file2", "text2", true, true);

			DiffInfo[] diff = repo2.GetPushDiff ("origin", "master");
			Assert.AreEqual (2, diff.Length);

			DiffInfo item = diff [0];
			Assert.IsNotNull (item);
			Assert.AreEqual ("file1", item.FileName.FileName);
			string text = @"index 0000000..f3a3485
--- /dev/null
+++ b/file1
@@ -0,0 +1 @@
+text
\ No newline at end of file
";

			if (Platform.IsWindows)
				text = text.Replace ("\r\n", "\n");
			Assert.AreEqual (text, item.Content);

			item = diff [1];
			Assert.IsNotNull (item);
			Assert.AreEqual ("file2", item.FileName.FileName);
			text = @"index 0000000..009b64b
--- /dev/null
+++ b/file2
@@ -0,0 +1 @@
+text2
\ No newline at end of file
";
			if (Platform.IsWindows)
				text = text.Replace ("\r\n", "\n");
			Assert.AreEqual (text, item.Content);
		}

		protected override void TestValidUrl ()
		{
			var repo2 = (GitRepository)Repo;
			Assert.IsTrue (repo2.IsUrlValid ("git@github.com:mono/monodevelop"));
			Assert.IsTrue (repo2.IsUrlValid ("git://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("ssh://user@host.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("http://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("https://github.com:80/mono/monodevelop.git"));
			//Assert.IsTrue (repo2.IsUrlValid ("ftp://github.com:80/mono/monodevelop.git"));
			//Assert.IsTrue (repo2.IsUrlValid ("ftps://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("file:///mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("git@172.31.118.100:/home/git/my-app.git"));
			Assert.IsTrue (repo2.IsUrlValid ("git@172.31.118.100:home/git/my-app.git"));
			//Assert.IsTrue (repo2.IsUrlValid ("rsync://github.com/mono/monodevelpo.git"));
		}

		[Test]
		public void TestRemote ()
		{
			var repo2 = (GitRepository)Repo;

			Assert.AreEqual ("origin", repo2.GetCurrentRemote ());

			AddFile ("file1", "text", true, true);
			PostCommit (repo2);
			repo2.CreateBranch ("branch1", null, null);
			repo2.SwitchToBranch (new ProgressMonitor (), "branch1");
			AddFile ("file2", "text", true, true);
			PostCommit (repo2);
			Assert.AreEqual (2, repo2.GetBranches ().Count ());
			Assert.AreEqual (1, repo2.GetRemotes ().Count ());

			repo2.RenameRemote ("origin", "other");
			Assert.AreEqual ("other", repo2.GetCurrentRemote ());

			repo2.RemoveRemote ("other");
			Assert.IsFalse (repo2.GetRemotes ().Any ());
		}

		[Test]
		public void TestIsMerged ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", "text", true, true);

			Assert.IsTrue (repo2.IsBranchMerged ("master"));

			repo2.CreateBranch ("branch1", null, null);
			repo2.SwitchToBranch (new ProgressMonitor (), "branch1");
			AddFile ("file2", "text", true, true);

			repo2.SwitchToBranch (new ProgressMonitor (), "master");
			Assert.IsFalse (repo2.IsBranchMerged ("branch1"));
			repo2.Merge ("branch1", GitUpdateOptions.NormalUpdate, new ProgressMonitor ());
			Assert.IsTrue (repo2.IsBranchMerged ("branch1"));
		}

		[Test]
		public void TestTags ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", "text", true, true);
			repo2.AddTag ("tag1", GetHeadRevision (), "my-tag");
			Assert.AreEqual (1, repo2.GetTags ().Count ());
			Assert.AreEqual ("tag1", repo2.GetTags ().First ());
			repo2.RemoveTag ("tag1");
			Assert.AreEqual (0, repo2.GetTags ().Count ());
		}

		// TODO: Test rebase and merge - This is broken on Windows

		protected override Repository GetRepo (string path, string url)
		{
			return new GitRepository (VersionControlService.GetVersionControlSystems ().First (id => id.Name == "Git"), path, url);
		}

		protected override Repository GetRepo ()
		{
			return new GitRepository ();
		}

		// This test is for a memory usage improvement on status.
		[Test]
		public void TestSameGitRevision ()
		{
			// Test that we have the same Revision on subsequent calls when HEAD doesn't change.
			AddFile ("file1", null, true, true);
			var info1 = Repo.GetVersionInfo (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			var info2 = Repo.GetVersionInfo (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (object.ReferenceEquals (info1.Revision, info2.Revision));

			// Test that we have the same Revision between two files on a HEAD bump.
			AddFile ("file2", null, true, true);
			var infos = Repo.GetVersionInfo (
				new FilePath[] { LocalPath + "file1", LocalPath + "file2" },
				VersionInfoQueryFlags.IgnoreCache
			).ToArray ();
			Assert.IsTrue (object.ReferenceEquals (infos [0].Revision, infos [1].Revision));

			// Test that the new Revision and the old one are different instances.
			Assert.IsFalse (object.ReferenceEquals (info1.Revision, infos [0].Revision));

			// Write the first test so it also covers directories.
			AddDirectory ("meh", true, false);
			AddFile ("meh/file3", null, true, true);
			var info3 = Repo.GetVersionInfo (LocalPath + "meh", VersionInfoQueryFlags.IgnoreCache);
			var info4 = Repo.GetVersionInfo (LocalPath + "meh", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (object.ReferenceEquals (info3.Revision, info4.Revision));
		}

		[Test]
		// Tests bug #23274
		public void DeleteWithStagedChanges ()
		{
			string added = LocalPath.Combine ("testfile");
			AddFile ("testfile", "test", true, true);

			// Test with VCS Remove.
			File.WriteAllText ("testfile", "t");
			Repo.Add (added, false, new ProgressMonitor ());
			Repo.DeleteFile (added, false, new ProgressMonitor (), true);
			Assert.AreEqual (VersionStatus.Versioned | VersionStatus.ScheduledDelete, Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache).Status);

			// Reset state.
			Repo.Revert (added, false, new ProgressMonitor ());

			// Test with Project Remove.
			File.WriteAllText ("testfile", "t");
			Repo.Add (added, false, new ProgressMonitor ());
			Repo.DeleteFile (added, false, new ProgressMonitor (), false);
			Assert.AreEqual (VersionStatus.Versioned | VersionStatus.ScheduledDelete, Repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache).Status);
		}

		[TestCase(null, "origin/master", "refs/remotes/origin/master")]
		[TestCase(null, "master", "refs/heads/master")]
		[TestCase(typeof(LibGit2Sharp.NotFoundException), "noremote/master", "refs/remotes/noremote/master")]
		[TestCase(typeof(ArgumentException), "origin/master", "refs/remote/origin/master")]
		// Tests bug #30347
		public void CreateBranchWithRemoteSource (Type exceptionType, string trackSource, string trackRef)
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("init", "init", true, true);
			repo2.Push (new ProgressMonitor (), "origin", "master");
			repo2.CreateBranch ("testBranch", "origin/master", "refs/remotes/origin/master");

			if (exceptionType != null)
				Assert.Throws (exceptionType, () => repo2.CreateBranch ("testBranch2", trackSource, trackRef));
			else {
				repo2.CreateBranch ("testBranch2", trackSource, trackRef);
				Assert.True (repo2.GetBranches ().Any (b => b.FriendlyName == "testBranch2" && b.TrackedBranch.FriendlyName == trackSource));
			}
		}

		public void CanSetBranchTrackRef (string trackSource, string trackRef)
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("init", "init", true, true);
			repo2.Push (new ProgressMonitor (), "origin", "master");

			repo2.SetBranchTrackRef ("testBranch", "origin/master", "refs/remotes/origin/master");
			Assert.True (repo2.GetBranches ().Any (
				b => b.FriendlyName == "testBranch" &&
				b.TrackedBranch == repo2.GetBranches ().Single (rb => rb.FriendlyName == "origin/master")
			));

			repo2.SetBranchTrackRef ("testBranch", null, null);
			Assert.True (repo2.GetBranches ().Any (
				b => b.FriendlyName == "testBranch" &&
				b.TrackedBranch == null)
			);
		}

		// teests bug #30415
		[TestCase(false, false)]
		[TestCase(true, false)]
		public void BlameDiffWithNotCommitedItem (bool toVcs, bool commit)
		{
			string added = LocalPath.Combine ("init");

			AddFile ("init", "init", toVcs, commit);

			Assert.AreEqual (string.Empty, Repo.GetBaseText (added));
			var revisions = Repo.GetAnnotations (added, null).Select (a => a.Revision);
			foreach (var rev in revisions)
				Assert.AreEqual (GettextCatalog.GetString ("working copy"), rev);
		}

		[Test]
		public void TestGitRebaseCommitOrdering ()
		{
			var gitRepo = (GitRepository)Repo;

			AddFile ("init", "init", toVcs: true, commit: true);

			// Create a branch from initial commit.
			gitRepo.CreateBranch ("test", null, null);

			// Create two commits in master.
			AddFile ("init2", "init", toVcs: true, commit: true);
			AddFile ("init3", "init", toVcs: true, commit: true);

			// Create two commits in test.
			gitRepo.SwitchToBranch (new ProgressMonitor (), "test");
			AddFile ("init4", "init", toVcs: true, commit: true);
			AddFile ("init5", "init", toVcs: true, commit: true);

			gitRepo.Rebase ("master", GitUpdateOptions.None, new ProgressMonitor ());

			// Commits come in reverse (recent to old).
			var history = gitRepo.GetHistory (LocalPath, null).Reverse ().ToArray ();
			Assert.AreEqual (5, history.Length);
			for (int i = 0; i < 5; ++i) {
				Assert.AreEqual (string.Format ("Commit #{0}\n", i), history [i].Message);
			}
		}

		// If this starts passing, either the git backend was fixed or the repository has changed
		// a submodule whose loose object cannot be found.
		[Test]
		public void TestGitRecursiveCloneFailsAndDoesntCrash ()
		{
			var toCheckout = new GitRepository {
				Url = "git://github.com/xamarin/xamarin-android.git",
			};

			var directory = FileService.CreateTempDirectory ();
			try {
				toCheckout.Checkout (directory, true, new ProgressMonitor ());
			} catch (VersionControlException e) {
				Assert.That (e.InnerException, Is.InstanceOf<LibGit2Sharp.NotFoundException> ());
				return;
			} finally {
				Directory.Delete (directory, true);
			}

			Assert.Fail ("Repository is not reporting loose object cannot be found. Consider removing test.");
		}
	}
}
