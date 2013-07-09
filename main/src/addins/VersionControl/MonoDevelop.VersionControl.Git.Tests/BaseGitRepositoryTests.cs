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
using NGit;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System;
using NGit.Storage.File;
using NGit.Api;

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class BaseGitUtilsTest : BaseRepoUtilsTest
	{
		protected GitRepository repo;
		protected GitRepository repo2;

		[SetUp]
		public override void Setup ()
		{
			// Generate directories and a svn util.
			rootUrl = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			rootCheckout = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Directory.CreateDirectory (rootUrl.FullPath + "repo.git");
			repoLocation = "file:///" + rootUrl.FullPath + "repo.git";

			// Initialize the bare repo.
			InitCommand ci = new InitCommand ();
			ci.SetDirectory (new Sharpen.FilePath (rootUrl.FullPath + "repo.git"));
			ci.SetBare (true);
			ci.Call ();
			FileRepository bare = new FileRepository (new Sharpen.FilePath (rootUrl.FullPath + "repo.git"));
			string branch = Constants.R_HEADS + "master";

			RefUpdate head = bare.UpdateRef (Constants.HEAD);
			head.DisableRefLog ();
			head.Link (branch);

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
			Assert.True (Directory.Exists (rootCheckout + ".git"));
		}

		[Test]
		public override void FileIsAdded ()
		{
			FilePath added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());

			VersionInfo vi = repo.GetVersionInfo (added);
			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.IsFalse (vi.CanAdd);
			Assert.IsFalse (vi.CanRevert);
		}

		[Test]
		public override void FileIsCommitted ()
		{
			FilePath added = rootCheckout + "testfile";

			File.Create (added).Close ();
			repo.Add (added, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (added);
			changes.GlobalComment = "test";
			repo.Commit (changes, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());

			VersionInfo vi = repo.GetVersionInfo (added);
			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.IsFalse (vi.CanAdd);
			Assert.IsTrue (vi.CanRevert);
		}

		[Test]
		[Ignore]
		// Works but doesn't delete the files.
		public override void UpdateIsDone ()
		{
			// Update fails on repositories with nothing in them.
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (added);
			changes.GlobalComment = "test";
			repo.Commit (changes, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			repo.Push (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), repo.GetCurrentRemote (), repo.GetCurrentBranch ());

			// Checkout a second repository.
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			Checkout (second);
			added = second + "testfile2";
			File.Create (added).Close ();
			repo2.Add (added, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (added);
			changes.GlobalComment = "test2";
			repo2.Commit (changes, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			repo2.Push (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), repo2.GetCurrentRemote (), repo2.GetCurrentBranch ());

			repo.Update (repo.RootPath, true, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			Assert.True (File.Exists (rootCheckout + "testfile2"));

			DeleteDirectory (second);
		}

		[Test]
		// TODO:
		public override void LogIsProper ()
		{
		}

		[Test]
		// TODO:
		public override void DiffIsProper ()
		{
		}

		[Test]
		public override void Reverts ()
		{
			string added = rootCheckout + "testfile";
			string content = "text";

			File.Create (added).Close ();
/*			backend.Add (added, false, new NullProgressMonitor ());
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

			Assert.AreNotEqual (File.ReadAllText (added + "2"), File.ReadAllText (added));*/
		}

		#region Util

		public override void Checkout (string path)
		{
			GitRepository _repo = GetRepo (repoLocation, path);
			_repo.Checkout (path, true, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			if (repo == null)
				repo = _repo;
			else
				repo2 = _repo;
		}

		public GitRepository GetRepo (string url, string path)
		{
			return new GitRepository (path, url);
		}

		#endregion
	}
}

