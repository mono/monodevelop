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
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	sealed class BaseGitUtilsTest : BaseRepoUtilsTest
	{
		[SetUp]
		public override async Task Setup ()
		{
			// Generate directories and a svn util.
			RemotePath = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			LocalPath = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Directory.CreateDirectory (RemotePath.FullPath + "repo.git");
			RemoteUrl = "file://" + (Platform.IsWindows ? "/" : "") + RemotePath.FullPath + "repo.git";

			LibGit2Sharp.Repository.Init (RemotePath.FullPath + "repo.git", true);

			// Check out the repository.
			await CheckoutAsync (LocalPath, RemoteUrl);
			Repo = GetRepo (LocalPath, RemoteUrl);
			await ((GitRepository)Repo).RunOperationAsync ((token) => ((GitRepository)Repo).RootRepository.Config.Set ("core.ignorecase", false));
			ModifyPath (Repo, ref LocalPath);
			DotDir = ".git";

			await base.Setup ();
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

		protected override async Task TestDiff ()
		{
			string difftext = @"--- a/testfile
+++ b/testfile
@@ -0,0 +1 @@
+text
\ No newline at end of file
";
			if (Platform.IsWindows)
				difftext = difftext.Replace ("\r\n", "\n");
			Assert.AreEqual (difftext, (await Repo.GenerateDiffAsync (LocalPath + "testfile", await Repo.GetVersionInfoAsync (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache))).Content);
		}

		protected override void ModifyPath (Repository repo, ref FilePath old)
		{
			var repo2 = (GitRepository)repo;
			old = repo2.RootRepository.Info.WorkingDirectory;
			repo2.SetUserInfo (Author, Email);
		}

		protected override async Task CheckLog (Repository repo)
		{
			int index = 2;
			var history = await Repo.GetHistoryAsync (LocalPath, null);
			Assert.AreEqual (index + 1, history.Length);
			foreach (Revision rev in history)
				Assert.AreEqual (String.Format ("Commit #{0}\n", index--), rev.Message);
		}

		[Test]
		[Ignore ("Not implemented in GitRepository.")]
		public override Task LocksEntities ()
		{
			return base.LocksEntities ();
		}

		[Test]
		[Ignore ("Not implemented in GitRepository.")]
		public override Task UnlocksEntities ()
		{
			return base.UnlocksEntities ();
		}

		protected override Revision GetHeadRevision ()
		{
			var repo2 = (GitRepository)Repo;
			return new GitRevision (Repo, repo2.RootPath, repo2.RootRepository.Head.Tip);
		}

		protected override async Task PostCommit (Repository repo)
		{
			var repo2 = (GitRepository)repo;
			var monitor = new ProgressMonitor ();
			await repo2.PushAsync (monitor, await repo2.GetCurrentRemoteAsync (), repo2.GetCurrentBranch ());
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
		public async Task TestGitStash ()
		{
			var monitor = new ProgressMonitor ();
			LibGit2Sharp.Stash stash;
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file2", "nothing", true, true);
			await AddFileAsync ("file1", "text", true, false);
			await AddFileAsync ("file3", "unstaged", false, false);
			await AddFileAsync ("file4", "noconflict", true, false);
			repo2.TryCreateStash (monitor, "meh", out stash);
			Assert.IsTrue (!File.Exists (LocalPath + "file1"), "Stash creation failure");
			await AddFileAsync ("file4", "conflict", true, true);
			await Task.Run (() => repo2.PopStash (monitor, 0));

			VersionInfo vi = await repo2.GetVersionInfoAsync (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file1"), "Stash pop staged failure");
			Assert.AreEqual (VersionStatus.ScheduledAdd, vi.Status & VersionStatus.ScheduledAdd, "Stash pop failure");

			vi = await repo2.GetVersionInfoAsync (LocalPath + "file3", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file3"), "Stash pop untracked failure");
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status, "Stash pop failure");

			vi = await repo2.GetVersionInfoAsync (LocalPath + "file4", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (File.Exists (LocalPath + "file4"), "Stash pop conflict failure");
			Assert.AreEqual (VersionStatus.Conflicted, vi.Status & VersionStatus.Conflicted, "Stash pop failure");
		}

		[Test]
		public async Task TestStashNoExtraCommit ()
		{
			var monitor = new ProgressMonitor ();
			LibGit2Sharp.Stash stash;
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file1", null, true, true);
			await AddFileAsync ("file2", null, true, false);
			await AddFileAsync ("file3", null, false, false);

			int commitCount = (await repo2.GetHistoryAsync (repo2.RootPath, null)).Length;
			await Task.Run (() => repo2.TryCreateStash (monitor, "stash1", out stash));
			await Task.Run (() => repo2.PopStash (monitor, 0));
			Assert.AreEqual (commitCount, (await repo2.GetHistoryAsync (repo2.RootPath, null)).Length, "stash1 added extra commit.");

			await Task.Run (() => repo2.TryCreateStash (monitor, "stash2", out stash));

			await AddFileAsync ("file4", null, true, true);
			commitCount = (await repo2.GetHistoryAsync (repo2.RootPath, null)).Length;

			await Task.Run (() => repo2.PopStash (monitor, 0));
			Assert.AreEqual (commitCount, (await repo2.GetHistoryAsync (repo2.RootPath, null)).Length, "stash2 added extra commit.");
		}

		[TestCase (true)]
		[TestCase (false)]
		public async Task TestGitStagedModifiedStatus (bool withDiff)
		{
			var repo2 = (GitRepository)Repo;
			var testFile = "file1";
			var testPath = LocalPath.Combine ("file1");
			string difftext = string.Empty;
			await AddFileAsync (testFile, "test 1\n", true, true);

			// new file added and committed
			var status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsFalse (status.HasLocalChanges);
			Assert.AreEqual (VersionStatus.Versioned, status.Status);
			if (withDiff)
				Assert.IsTrue (string.IsNullOrEmpty ((await repo2.GenerateDiffAsync (testPath, status)).Content));

			// modify the file without staging
			File.AppendAllText (testPath, "test 2\n");
			status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (status.HasLocalChanges);
			Assert.AreEqual (VersionStatus.Modified | VersionStatus.Versioned, status.Status);

			if (withDiff) {
				difftext = @"--- a/file1
+++ b/file1
@@ -1 +1,2 @@
 test 1
+test 2
".Replace ("file1", testFile);
				var diff = await repo2.GenerateDiffAsync (testPath, status);
				Assert.AreEqual (difftext, diff.Content);
			}

			// stage changes
			LibGit2Sharp.Commands.Stage (repo2.RootRepository, testPath);
			Assert.IsTrue (status.Equals (await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache)));

			if (withDiff) {
				var diff = await repo2.GenerateDiffAsync (testPath, status);
				Assert.AreEqual (difftext, diff.Content);
			}

			// modify the file again
			File.AppendAllText (testPath, "test 3\n");
			Assert.IsTrue (status.Equals (await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache)));


			if (withDiff) {
				difftext = @"--- a/file1
+++ b/file1
@@ -1 +1,3 @@
 test 1
+test 2
+test 3
".Replace ("file1", testFile);
				var diff = await repo2.GenerateDiffAsync (testPath, status);
				Assert.AreEqual (difftext, diff.Content);
			}

		}

		[TestCase (false)]
		[TestCase (true, Ignore = true, IgnoreReason = "Needs to be fixed")]
		public async Task TestGitStagedNewFileStatus (bool testUnstagedRemove)
		{
			var repo2 = (GitRepository)Repo;
			var testFile = "file1";
			var testPath = LocalPath.Combine ("file1");
			await AddFileAsync ("file", null, true, true);
			await AddFileAsync (testFile, "test 1\n", false, false);

			// new file added but not staged
			var status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsFalse (status.HasLocalChanges);
			Assert.AreEqual (VersionStatus.Unversioned, status.Status);

			// stage added file
			LibGit2Sharp.Commands.Stage (repo2.RootRepository, testPath);
			status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (status.HasLocalChanges);
			Assert.AreEqual (VersionStatus.ScheduledAdd | VersionStatus.Versioned, status.Status);

			// modify the file without staging
			File.AppendAllText (testPath, "test 2\n");
			status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (status.Equals (await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache)));

			// remove the staged file
			File.Delete (testPath);
			// TODO: at this point the file still exists in the index, but should be detected as Unversioned
			if (testUnstagedRemove) {
				status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
				Assert.IsFalse (status.HasLocalChanges);
				Assert.AreEqual (VersionStatus.Unversioned, status.Status);
			}

			// stage removed file
			LibGit2Sharp.Commands.Stage (repo2.RootRepository, testPath);
			status = await Repo.GetVersionInfoAsync (testPath, VersionInfoQueryFlags.IgnoreCache);
			Assert.IsFalse (status.HasLocalChanges);
			Assert.AreEqual (VersionStatus.Unversioned, status.Status);
		}

		static string GetStashMessageForBranch (string branch)
		{
			return string.Format ("On {0}: __MD_{0}\n\n", branch);
		}

		[TestCase (false)]
		[TestCase (true, Ignore = true, IgnoreReason = "We stash now only if there are conflicts, this needs to be updated")]
		public async Task TestGitBranchCreation (bool automaticStashCreation)
		{
			var autoStashDefault = GitService.StashUnstashWhenSwitchingBranches.Value;
			GitService.StashUnstashWhenSwitchingBranches.Value = automaticStashCreation;
			try {
				var repo2 = (GitRepository)Repo;
				var monitor = new ProgressMonitor ();

				await AddFileAsync ("file1", "text", true, true);

				await repo2.CreateBranchAsync ("branch1", null, null);

				await repo2.SwitchToBranchAsync (monitor, "branch1");
				// Nothing could be stashed for master. Branch1 should be popped in any case if it exists.
				Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("master")));
				Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));

				Assert.AreEqual ("branch1", repo2.GetCurrentBranch ());
				Assert.IsTrue (File.Exists (LocalPath + "file1"), "Branch not inheriting from current.");

				await AddFileAsync ("file2", "text", true, false);
				await repo2.CreateBranchAsync ("branch2", null, null);

				await repo2.SwitchToBranchAsync (monitor, "branch2");
				if (automaticStashCreation) {
					// Branch1 has a stash created and assert clean workdir. Branch2 should be popped in any case.
					Assert.IsTrue (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch2")));
					Assert.IsTrue (!File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed");
				} else {
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("master")));
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
					Assert.IsTrue (File.Exists (LocalPath + "file2"), "Uncommitted changes were stashed");
				}

				await AddFileAsync ("file2", "text", true, false);
				await repo2.SwitchToBranchAsync (monitor, "branch1");
				// Branch2 has a stash created. Branch1 should be popped with file2 reinstated.

				if (automaticStashCreation) {
					Assert.True (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch2")));
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
					Assert.IsTrue (File.Exists (LocalPath + "file2"), "Uncommitted changes were not stashed correctly");
				} else {
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch2")));
					Assert.IsFalse (repo2.GetStashes ().Any (s => s.Message == GetStashMessageForBranch ("branch1")));
					Assert.IsTrue (File.Exists (LocalPath + "file2"), "Uncommitted changes were stashed");
				}

				await repo2.SwitchToBranchAsync (monitor, "master");
				await repo2.RemoveBranchAsync ("branch1");
				Assert.IsFalse ((await repo2.GetBranchesAsync ()).Any (b => b.FriendlyName == "branch1"), "Failed to delete branch");

				await repo2.RenameBranchAsync ("branch2", "branch3");
				var branches = await repo2.GetBranchesAsync ();
				Assert.IsTrue (branches.Any (b => b.FriendlyName == "branch3") && branches.All (b => b.FriendlyName != "branch2"), "Failed to rename branch");
			} finally {
				GitService.StashUnstashWhenSwitchingBranches.Value = autoStashDefault;
			}

			// TODO: Add CreateBranchFromCommit tests.
		}

		[Test]
		public async Task TestGitSyncBranches ()
		{
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file1", "text", true, true);
			await PostCommit (repo2);

			var monitor = new ProgressMonitor ();

			await repo2.CreateBranchAsync ("branch3", null, null);
			await repo2.SwitchToBranchAsync (monitor, "branch3");
			await AddFileAsync ("file2", "asdf", true, true);
			await repo2.PushAsync (monitor, "origin", "branch3");

			await repo2.SwitchToBranchAsync (monitor, "master");

			await repo2.CreateBranchAsync ("branch4", "origin/branch3", "refs/remotes/origin/branch3");
			await repo2.SwitchToBranchAsync (monitor, "branch4");
			Assert.IsTrue (File.Exists (LocalPath + "file2"), "Tracking remote is not grabbing correct commits");
		}

		[Test]
		public async Task TestPushChangeset ()
		{
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file", "meh", true, true);
			await PostCommit (repo2);

			await AddFileAsync ("file1", "text", true, true);
			await AddFileAsync ("file2", "text2", true, true);

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
		public async Task TestPushDiff ()
		{
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file", "meh", true, true);
			await PostCommit (repo2);

			await AddFileAsync ("file1", "text", true, true);
			await AddFileAsync ("file2", "text2", true, true);

			DiffInfo [] diff = repo2.GetPushDiff ("origin", "master");
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
		public async Task TestRemote ()
		{
			var repo2 = (GitRepository)Repo;
			var monitor = new ProgressMonitor ();

			Assert.AreEqual ("origin", await repo2.GetCurrentRemoteAsync ());

			await AddFileAsync ("file1", "text", true, true);
			await PostCommit (repo2);
			await repo2.CreateBranchAsync ("branch1", null, null);
			await repo2.SwitchToBranchAsync (monitor, "branch1");
			await AddFileAsync ("file2", "text", true, true);
			await PostCommit (repo2);
			Assert.AreEqual (2, (await repo2.GetBranchesAsync ()).Count ());
			Assert.AreEqual (1, (await repo2.GetRemotesAsync ()).Count ());

			await repo2.RenameRemoteAsync ("origin", "other");
			Assert.AreEqual ("other", await repo2.GetCurrentRemoteAsync ());

			await repo2.RemoveRemoteAsync ("other");
			Assert.IsFalse ((await repo2.GetRemotesAsync ()).Any ());
		}

		[Test]
		public async Task TestIsMerged ()
		{
			var repo2 = (GitRepository)Repo;
			var monitor = new ProgressMonitor ();
			await AddFileAsync ("file1", "text", true, true);

			Assert.IsTrue (await repo2.IsBranchMergedAsync ("master"));

			await repo2.CreateBranchAsync ("branch1", null, null);
			await repo2.SwitchToBranchAsync (monitor, "branch1");
			await AddFileAsync ("file2", "text", true, true);

			await repo2.SwitchToBranchAsync (monitor, "master");
			Assert.IsFalse (await repo2.IsBranchMergedAsync ("branch1"));
			await repo2.MergeAsync ("branch1", GitUpdateOptions.NormalUpdate, monitor);
			Assert.IsTrue (await repo2.IsBranchMergedAsync ("branch1"));
		}

		[Test]
		public async Task TestTags ()
		{
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("file1", "text", true, true);
			await repo2.AddTagAsync ("tag1", GetHeadRevision (), "my-tag");
			var tags = await repo2.GetTagsAsync ();
			Assert.AreEqual (1, tags.Count);
			Assert.AreEqual ("tag1", tags.First ());
			await repo2.RemoveTagAsync ("tag1");
			Assert.AreEqual (0, (await repo2.GetTagsAsync ()).Count);
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
		public async Task TestSameGitRevision ()
		{
			// Test that we have the same Revision on subsequent calls when HEAD doesn't change.
			await AddFileAsync ("file1", null, true, true);
			var info1 = await Repo.GetVersionInfoAsync (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			var info2 = await Repo.GetVersionInfoAsync (LocalPath + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (object.ReferenceEquals (info1.Revision, info2.Revision));

			// Test that we have the same Revision between two files on a HEAD bump.
			await AddFileAsync ("file2", null, true, true);
			var infos = (await Repo.GetVersionInfoAsync (
				new FilePath [] { LocalPath + "file1", LocalPath + "file2" },
				VersionInfoQueryFlags.IgnoreCache
			)).ToArray ();
			Assert.IsTrue (object.ReferenceEquals (infos [0].Revision, infos [1].Revision));

			// Test that the new Revision and the old one are different instances.
			Assert.IsFalse (object.ReferenceEquals (info1.Revision, infos [0].Revision));

			// Write the first test so it also covers directories.
			await AddDirectoryAsync ("meh", true, false);
			await AddFileAsync ("meh/file3", null, true, true);
			var info4 = await Repo.GetVersionInfoAsync (LocalPath + "meh", VersionInfoQueryFlags.IgnoreCache);
			var info3 = await Repo.GetVersionInfoAsync (LocalPath + "meh", VersionInfoQueryFlags.IgnoreCache);
			Assert.IsTrue (object.ReferenceEquals (info3.Revision, info4.Revision));
		}

		[Test]
		// Tests bug #23274
		public async Task DeleteWithStagedChanges ()
		{
			var monitor = new ProgressMonitor ();
			string added = LocalPath.Combine ("testfile");
			await AddFileAsync ("testfile", "test", true, true);

			// Test with VCS Remove.
			File.WriteAllText ("testfile", "t");
			await Repo.AddAsync (added, false, monitor);
			await Repo.DeleteFileAsync (added, false, monitor, true);
			Assert.AreEqual (VersionStatus.Versioned | VersionStatus.ScheduledDelete, (await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache)).Status);

			// Reset state.
			await Repo.RevertAsync (added, false, monitor);

			// Test with Project Remove.
			File.WriteAllText ("testfile", "t");
			await Repo.AddAsync (added, false, monitor);
			await Repo.DeleteFileAsync (added, false, monitor, false);
			Assert.AreEqual (VersionStatus.Versioned | VersionStatus.ScheduledDelete, (await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache)).Status);
		}

		[TestCase(null, "origin/master", "refs/remotes/origin/master")]
		[TestCase(null, "master", "refs/heads/master")]
		[TestCase(typeof(LibGit2Sharp.NotFoundException), "noremote/master", "refs/remotes/noremote/master")]
		[TestCase(typeof(ArgumentException), "origin/master", "refs/remote/origin/master")]
		// Tests bug #30347
		public async Task CreateBranchWithRemoteSource (Type exceptionType, string trackSource, string trackRef)
		{
			var monitor = new ProgressMonitor ();
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("init", "init", true, true);
			await repo2.PushAsync (new ProgressMonitor (), "origin", "master");
			await repo2.CreateBranchAsync ("testBranch", "origin/master", "refs/remotes/origin/master");

			if (exceptionType != null) {
				try {
					await repo2.CreateBranchAsync ("testBranch2", trackSource, trackRef);
				} catch (Exception ex) {
					Assert.IsInstanceOfType (exceptionType, ex);
				}
			} else {
				await repo2.CreateBranchAsync ("testBranch2", trackSource, trackRef);
				Assert.True ((await repo2.GetBranchesAsync ()).Any (b => b.FriendlyName == "testBranch2" && b.TrackedBranch.FriendlyName == trackSource));
			}
			repo2.Dispose ();
		}

		public async Task CanSetBranchTrackRef (string trackSource, string trackRef)
		{
			var monitor = new ProgressMonitor ();
			var repo2 = (GitRepository)Repo;
			await AddFileAsync ("init", "init", true, true);
			await repo2.PushAsync (new ProgressMonitor (), "origin", "master");

			await repo2.SetBranchTrackRefAsync ("testBranch", "origin/master", "refs/remotes/origin/master");
			var branches = await repo2.GetBranchesAsync ();
			Assert.True (branches.Any (
				b => b.FriendlyName == "testBranch" &&
				b.TrackedBranch == branches.Single (rb => rb.FriendlyName == "origin/master")
			));

			await repo2.SetBranchTrackRefAsync ("testBranch", null, null);
			Assert.True ((await repo2.GetBranchesAsync ()).Any (
				b => b.FriendlyName == "testBranch" &&
				b.TrackedBranch == null)
			);
		}

		// teests bug #30415
		[TestCase(false, false)]
		[TestCase(true, false)]
		[TestCase(true, true)]
		public async Task BlameDiffWithNotCommitedItem (bool toVcs, bool commit)
		{
			string added = LocalPath.Combine ("init");

			var contents = string.Join (Environment.NewLine, Enumerable.Repeat ("string", 10));
			await AddFileAsync ("init", contents, toVcs, commit);

			var expectedBaseText = commit ? contents : string.Empty;
			Assert.AreEqual (expectedBaseText, await Repo.GetBaseTextAsync (added));
			var revisions = (await Repo.GetAnnotationsAsync (added, null)).Select (a => a.Revision);

			string expectedRevision = commit
				? "Commit #0\n"
				: GettextCatalog.GetString ("working copy");

			foreach (var rev in revisions)
				Assert.AreEqual (expectedRevision, rev.Message);
		}

		[Test]
		public async Task BlameWithWorkingChanges ()
		{
			string added = LocalPath.Combine ("init");

			/*
			 * string1 - #0
			 * string1 - #0
			 * string1 - #0
			 * string2 - #0
			 * string2 - #0
			 * string2 - #0
			 * string3 - #0
			 * string3 - #0
			 * string3 - #0
			 */
			var contents = string.Join (
				Environment.NewLine,
				Enumerable.Repeat ("string1", 3)
					.Concat (Enumerable.Repeat ("string2", 3))
					.Concat (Enumerable.Repeat ("string3", 3))
			);
			await AddFileAsync ("init", contents, true, true);

			contents = string.Join (
				Environment.NewLine,
				new [] {
					"string0", // working copy <- deleted 1, added 1
					"string1", // #0
					"string4", // working copy <- deleted 1, added 3
					"string4", // working copy
					"string4", // working copy
					"string1", // #0
					"string2", // #0           <- deleted 1
					"string2", // #0
					"string3", // #0
					"string4", // working copy
				});
			await AddFileAsync ("init", contents, true, false);
			var annotations = await Repo.GetAnnotationsAsync (added, null);

			string [] expected = {
				GettextCatalog.GetString ("working copy"),
				"Commit #0\n",
				GettextCatalog.GetString ("working copy"),
				GettextCatalog.GetString ("working copy"),
				GettextCatalog.GetString ("working copy"),
				"Commit #0\n",
				"Commit #0\n",
				"Commit #0\n",
				"Commit #0\n",
				GettextCatalog.GetString ("working copy"),
			};

			Assert.That (annotations.Select (x => x.Revision?.Message ?? x.Text), Is.EquivalentTo (expected));
		}

		[Test]
		public async Task TestGitRebaseCommitOrdering ()
		{
			var monitor = new ProgressMonitor ();
			var gitRepo = (GitRepository)Repo;

			await AddFileAsync ("init", "init", toVcs: true, commit: true);

			// Create a branch from initial commit.
			await gitRepo.CreateBranchAsync ("test", null, null);

			// Create two commits in master.
			await AddFileAsync ("init2", "init", toVcs: true, commit: true);
			await AddFileAsync ("init3", "init", toVcs: true, commit: true);

			// Create two commits in test.
			await gitRepo.SwitchToBranchAsync (monitor, "test");
			await AddFileAsync ("init4", "init", toVcs: true, commit: true);
			await AddFileAsync ("init5", "init", toVcs: true, commit: true);

			await gitRepo.RebaseAsync ("master", GitUpdateOptions.None, monitor);

			// Commits come in reverse (recent to old).
			var history = (await gitRepo.GetHistoryAsync (LocalPath, null)).Reverse ().ToArray ();
			Assert.AreEqual (5, history.Length);
			for (int i = 0; i < 5; ++i) {
				Assert.AreEqual (string.Format ("Commit #{0}\n", i), history [i].Message);
			}
		}

		// If this starts passing, either the git backend was fixed or the repository has changed
		// a submodule whose loose object cannot be found.
		[Test]
		[Ignore("Fixed in another PR.")]
		public async Task TestGitRecursiveCloneFailsAndDoesntCrash ()
		{
			var toCheckout = new GitRepository {
				Url = "git://github.com/xamarin/xamarin-android.git",
			};
			var monitor = new ProgressMonitor ();

			var directory = FileService.CreateTempDirectory ();
			try {
				await toCheckout.CheckoutAsync (directory, true, monitor);
			} catch (Exception e) {
				var exception = e.InnerException;
				// libgit2 < 0.26 will throw NotFoundException (result -3)
				// libgit2 >= 0.26 will throw generic LibGit2SharpException (result -1), assert the expected message
				Assert.That (exception, Is.InstanceOf<LibGit2Sharp.NotFoundException> ().Or.InstanceOf<LibGit2Sharp.NameConflictException> ().Or.Message.EqualTo ("reference 'refs/heads/master' not found"));
				return;
			} finally {
				toCheckout.Dispose ();
				Directory.Delete (directory, true);
			}

			Assert.Fail ("Repository is not reporting loose object cannot be found. Consider removing test.");
		}
	}
}
