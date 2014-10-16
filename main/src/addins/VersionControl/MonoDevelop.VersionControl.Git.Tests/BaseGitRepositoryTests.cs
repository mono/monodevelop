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
			ModifyPath (Repo, ref LocalPath);
			DotDir = ".git";
		}

		protected override NUnit.Framework.Constraints.IResolveConstraint IsCorrectType ()
		{
			return Is.InstanceOf<GitRepository> ();
		}

		protected override int RepoItemsCount {
			get { return 1; }
		}

		protected override int RepoItemsCountRecursive {
			get { return 17; }
		}

		protected override void TestDiff ()
		{
			string difftext = @"diff --git a/testfile b/testfile
index e69de29..f3a3485 100644
--- a/testfile
+++ b/testfile
@@ -0,0 +1 @@
+text
\ No newline at end of file
";
			Assert.AreEqual (difftext, Repo.GenerateDiff (LocalPath + "testfile", Repo.GetVersionInfo (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content.Replace ("\n", "\r\n"));
		}

		protected override void ModifyPath (Repository repo, ref FilePath old)
		{
			var repo2 = (GitRepository)repo;
			old = repo2.RootRepository.Info.WorkingDirectory;
			repo2.RootRepository.Config.Set<string> ("user.name", Author);
			repo2.RootRepository.Config.Set<string> ("user.email", Email);
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

		[Test]
		[Ignore ("NGit sees added directories as unversioned.")]
		public override void MovesDirectory ()
		{
			base.MovesDirectory ();
		}

		protected override Revision GetHeadRevision ()
		{
			var repo2 = (GitRepository)Repo;
			return new GitRevision (Repo, repo2.RootRepository, repo2.RootRepository.Head.Tip.Sha) {
				Commit = repo2.RootRepository.Head.Tip
			};
		}

		protected override void PostCommit (Repository repo)
		{
			var repo2 = (GitRepository)repo;
			repo2.Push (new NullProgressMonitor (), repo2.GetCurrentRemote (), repo2.GetCurrentBranch ());
		}

		protected override void BlameExtraInternals (Annotation [] annotations)
		{
			for (int i = 0; i < 2; i++) {
				Assert.IsTrue (annotations [i].HasEmail);
				Assert.AreEqual (Author, annotations [i].Author);
				Assert.AreEqual (String.Format ("<{0}>", Email), annotations [i].Email);
			}
			Assert.IsTrue (annotations [2].HasDate);
		}

		[Test]
		[Ignore]
		public void TestGitStash ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file2", "nothing", true, true);
			AddFile ("file1", "text", true, false);
			repo2.CreateStash (new NullProgressMonitor (), "meh");
			Assert.IsTrue (!File.Exists (LocalPath + "file1"), "Stash creation failure");
			repo2.PopStash (new NullProgressMonitor ());

			VersionInfo vi = repo2.GetVersionInfo (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledAdd, vi.Status & VersionStatus.ScheduledAdd, "Stash pop failure");
		}

		[Test]
		public void TestGitBranchCreation ()
		{
			var repo2 = (GitRepository)Repo;

			AddFile ("file1", "text", true, true);
			repo2.CreateBranch ("branch1", null);

			repo2.SwitchToBranch (new NullProgressMonitor (), "branch1");
			Assert.AreEqual ("branch1", repo2.GetCurrentBranch ());
			Assert.IsTrue (File.Exists (LocalPath + "file1"), "Branch not inheriting from current.");

			AddFile ("file2", "text", true, false);
			repo2.CreateBranch ("branch2", null);
			repo2.SwitchToBranch (new NullProgressMonitor (), "branch2");
			Assert.IsTrue (!File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed");
			repo2.PopStash (new NullProgressMonitor ());

			Assert.IsTrue (File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed correctly");

			repo2.SwitchToBranch (new NullProgressMonitor (), "master");
			repo2.RemoveBranch ("branch1");
			Assert.IsFalse (repo2.GetBranches ().Any (b => b.Name == "branch1"), "Failed to delete branch");

			repo2.RenameBranch ("branch2", "branch3");
			Assert.IsTrue (repo2.GetBranches ().Any (b => b.Name == "branch3") && repo2.GetBranches ().All (b => b.Name != "branch2"), "Failed to rename branch");

			// TODO: Add CreateBranchFromCommit tests.
		}

		[Test]
		[Ignore ("this is a new test which doesn't pass on Mac yet")]
		public void TestGitSyncBranches ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", "text", true, true);
			PostCommit (repo2);

			repo2.CreateBranch ("branch3", null);
			repo2.SwitchToBranch (new NullProgressMonitor (), "branch3");
			AddFile ("file2", "asdf", true, true);
			repo2.Push (new NullProgressMonitor (), "origin", "branch3");

			repo2.SwitchToBranch (new NullProgressMonitor (), "master");

			repo2.CreateBranch ("branch4", "origin/branch3");
			repo2.SwitchToBranch (new NullProgressMonitor (), "branch4");
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
			string text = @"diff --git a/file1 b/file1
new file mode 100644
index 0000000..f3a3485
--- /dev/null
+++ b/file1
@@ -0,0 +1 @@
+text
\ No newline at end of file
";
			Assert.AreEqual (text, item.Content.Replace("\n", "\r\n"));

			item = diff [1];
			Assert.IsNotNull (item);
			Assert.AreEqual ("file2", item.FileName.FileName);
			text = @"diff --git a/file2 b/file2
new file mode 100644
index 0000000..009b64b
--- /dev/null
+++ b/file2
@@ -0,0 +1 @@
+text2
\ No newline at end of file
";
			Assert.AreEqual (text, item.Content.Replace("\n", "\r\n"));
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
			//Assert.IsTrue (repo2.IsUrlValid ("rsync://github.com/mono/monodevelpo.git"));
		}

		[Test]
		public void TestRemote ()
		{
			var repo2 = (GitRepository)Repo;

			Assert.AreEqual ("origin", repo2.GetCurrentRemote ());

			AddFile ("file1", "text", true, true);
			PostCommit (repo2);
			repo2.CreateBranch ("branch1", null);
			repo2.SwitchToBranch (new NullProgressMonitor (), "branch1");
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

			repo2.CreateBranch ("branch1", null);
			repo2.SwitchToBranch (new NullProgressMonitor (), "branch1");
			AddFile ("file2", "text", true, true);

			repo2.SwitchToBranch (new NullProgressMonitor (), "master");
			Assert.IsFalse (repo2.IsBranchMerged ("branch1"));
			repo2.Merge ("branch1", GitUpdateOptions.NormalUpdate, new NullProgressMonitor ());
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
			return new GitRepository (path, url);
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
	}
}

