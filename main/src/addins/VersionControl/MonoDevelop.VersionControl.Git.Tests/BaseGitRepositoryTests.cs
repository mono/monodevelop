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
using MonoDevelop.VersionControl.Git;
using MonoDevelop.VersionControl.Tests;
using NGit;
using NGit.Api;
using NUnit.Framework;
using System.IO;
using NGit.Storage.File;
using NGit.Revwalk;
using System.Linq;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	sealed class BaseGitUtilsTest : BaseRepoUtilsTest
	{
		[SetUp]
		public override void Setup ()
		{
			// Generate directories and a svn util.
			RootUrl = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			RootCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Directory.CreateDirectory (RootUrl.FullPath + "repo.git");
			RepoLocation = "file:///" + RootUrl.FullPath + "repo.git";

			// Initialize the bare repo.
			var ci = new InitCommand ();
			ci.SetDirectory (new Sharpen.FilePath (RootUrl.FullPath + "repo.git"));
			ci.SetBare (true);
			ci.Call ();
			var bare = new FileRepository (new Sharpen.FilePath (RootUrl.FullPath + "repo.git"));
			string branch = Constants.R_HEADS + "master";

			RefUpdate head = bare.UpdateRef (Constants.HEAD);
			head.DisableRefLog ();
			head.Link (branch);

			// Check out the repository.
			Checkout (RootCheckout, RepoLocation);
			Repo = GetRepo (RootCheckout, RepoLocation);
			DotDir= ".git";
		}

		protected override NUnit.Framework.Constraints.IResolveConstraint IsCorrectType ()
		{
			return Is.InstanceOf<GitRepository> ();
		}

		protected override void TestDiff ()
		{
			string difftext = @"@@ -0,0 +1 @@
+text
";
			Assert.AreEqual (difftext, Repo.GenerateDiff (RootCheckout + "testfile", Repo.GetVersionInfo (RootCheckout + "testfile", VersionInfoQueryFlags.IgnoreCache)).Content.Replace ("\n", "\r\n"));
		}

		[Test]
		[Platform (Exclude = "Win")]
		public override void UpdateIsDone ()
		{
			base.UpdateIsDone ();
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
		[Ignore ("This test is failing because the file is showing up as Unversioned, not ScheduledForDelete")]
		public override void DeletesFile ()
		{
			base.DeletesFile ();
		}

		[Test]
		[Ignore ("Apparently, we have issues with this")]
		public override void DeletesDirectory ()
		{
			base.DeletesDirectory ();
		}
		
		[Test]
		[Ignore ("NGit sees added directories as unversioned on Unix.")]
		public override void MovesFile ()
		{
			base.MovesFile ();
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
			var rw = new RevWalk (repo2.RootRepository);
			ObjectId headId = repo2.RootRepository.Resolve (Constants.HEAD);
			if (headId == null)
				return null;

			RevCommit commit = rw.ParseCommit (headId);
			var rev = new GitRevision (Repo, repo2.RootRepository, commit.Id.Name);
			rev.Commit = commit;
			return rev;
		}

		protected override void PostCommit (Repository repo)
		{
			var repo2 = (GitRepository)repo;
			repo2.Push (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), repo2.GetCurrentRemote (), repo2.GetCurrentBranch ());
		}

		protected override void BlameExtraInternals (Annotation [] annotations)
		{
			// TODO: Fix this for bots.
//			for (int i = 0; i < 2; i++) {
//				Assert.IsTrue (annotations [i].HasEmail);
//				Assert.AreEqual (Author, annotations [i].Author);
//				Assert.AreEqual (String.Format ("<{0}>", Email), annotations [i].Email);
//			}
			Assert.IsTrue (annotations [2].HasDate);
		}

		[Test]
		[Ignore ("this is a new test which doesn't pass on Mac yet")]
		public void TestGitStash ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file2", "nothing", true, true);
			AddFile ("file1", "text", true, false);
			repo2.GetStashes ().Create (new NullProgressMonitor ());
			Assert.IsTrue (!File.Exists (RootCheckout + "file1"), "Stash creation failure");
			repo2.GetStashes ().Pop (new NullProgressMonitor ());

			VersionInfo vi = repo2.GetVersionInfo (RootCheckout + "file1", VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledAdd, vi.Status & VersionStatus.ScheduledAdd, "Stash pop failure");
		}

		[Test]
		[Ignore ("this is a new test which doesn't pass on Mac yet")]
		public void TestGitBranchCreation ()
		{
			var repo2 = (GitRepository)Repo;

			AddFile ("file1", "text", true, true);
			repo2.CreateBranch ("branch1", null);

			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch1");
			Assert.AreEqual ("branch1", repo2.GetCurrentBranch ());
			Assert.IsTrue (File.Exists (RootCheckout + "file1"), "Branch not inheriting from current.");

			AddFile ("file2", "text", true, false);
			repo2.CreateBranch ("branch2", null);
			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch2");
			Assert.IsTrue (!File.Exists (RootCheckout + "file2"), "Uncommitted changes were not stashed");
			repo2.GetStashes ().Pop (new NullProgressMonitor ());

			Assert.IsTrue (File.Exists (RootCheckout + "file2"), "Uncommitted changes were not stashed correctly");

			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "master");
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
			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch3");
			AddFile ("file2", "asdf", true, true);
			repo2.Push (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "origin", "branch3");

			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "master");

			repo2.CreateBranch ("branch4", "origin/branch3");
			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch4");
			Assert.IsTrue (File.Exists (RootCheckout + "file2"), "Tracking remote is not grabbing correct commits");
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

			ChangeSetItem item = diff.GetFileItem (RootCheckout + "file1");
			Assert.IsNotNull (item);
			Assert.AreEqual (VersionStatus.ScheduledAdd, item.Status & VersionStatus.ScheduledAdd);

			item = diff.GetFileItem (RootCheckout + "file1");
			Assert.IsNotNull (item);
			Assert.AreEqual (VersionStatus.ScheduledAdd, item.Status & VersionStatus.ScheduledAdd);
		}

		[Test]
		[Ignore ("GetPushDiff content is always empty")]
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
			//Assert.AreEqual ("text", item.Content);

			item = diff [1];
			Assert.IsNotNull (item);
			Assert.AreEqual ("file2", item.FileName.FileName);
			//Assert.AreEqual ("text2", item.Content);
		}

		protected override void TestValidUrl ()
		{
			var repo2 = (GitRepository)Repo;
			Assert.IsTrue (repo2.IsUrlValid ("git@github.com:mono/monodevelop"));
			Assert.IsTrue (repo2.IsUrlValid ("git://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("ssh://user@host.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("http://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("https://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("ftp://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("ftps://github.com:80/mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("file:///mono/monodevelop.git"));
			Assert.IsTrue (repo2.IsUrlValid ("rsync://github.com/mono/monodevelpo.git"));
		}

		[Test]
		public void TestRemote ()
		{
			var repo2 = (GitRepository)Repo;

			Assert.AreEqual ("origin", repo2.GetCurrentRemote ());

			AddFile ("file1", "text", true, true);
			PostCommit (repo2);
			repo2.CreateBranch ("branch1", null);
			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch1");
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
		[Ignore ("this is a new test which doesn't pass on Mac yet")]
		public void TestIsMerged ()
		{
			var repo2 = (GitRepository)Repo;
			AddFile ("file1", "text", true, true);

			Assert.IsTrue (repo2.IsBranchMerged ("master"));

			repo2.CreateBranch ("branch1", null);
			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "branch1");
			AddFile ("file2", "text", true, true);

			repo2.SwitchToBranch (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), "master");
			Assert.IsFalse (repo2.IsBranchMerged ("branch1"));
			repo2.Merge ("branch1", GitUpdateOptions.NormalUpdate, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
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
	}
}

