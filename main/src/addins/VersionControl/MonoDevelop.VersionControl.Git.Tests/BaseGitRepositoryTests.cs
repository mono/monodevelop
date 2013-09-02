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
using System;
using NGit.Storage.File;
using NGit.Revwalk;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	[Ignore ("These are randomly failing on our build bots so I'm disabling them until they can be robustified")]
	public class BaseGitUtilsTest : BaseRepoUtilsTest
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
			InitCommand ci = new InitCommand ();
			ci.SetDirectory (new Sharpen.FilePath (RootUrl.FullPath + "repo.git"));
			ci.SetBare (true);
			ci.Call ();
			FileRepository bare = new FileRepository (new Sharpen.FilePath (RootUrl.FullPath + "repo.git"));
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
			GitRepository repo2 = (GitRepository)Repo;
			RevWalk rw = new RevWalk (repo2.RootRepository);
			ObjectId headId = repo2.RootRepository.Resolve (Constants.HEAD);
			if (headId == null)
				return null;

			RevCommit commit = rw.ParseCommit (headId);
			GitRevision rev = new GitRevision (Repo, repo2.RootRepository, commit.Id.Name);
			rev.Commit = commit;
			return rev;
		}

		protected override void PostCommit (Repository repo)
		{
			GitRepository repo2 = (GitRepository)repo;
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

		protected override Repository GetRepo (string path, string url)
		{
			return new GitRepository (path, url);
		}
	}
}

