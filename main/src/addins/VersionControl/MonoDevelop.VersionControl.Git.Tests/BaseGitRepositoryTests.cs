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

namespace MonoDevelop.VersionControl.Git.Tests
{
	[TestFixture]
	public class BaseGitUtilsTest : BaseRepoUtilsTest
	{
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
			Checkout (rootCheckout, repoLocation);
			repo = GetRepo (rootCheckout, repoLocation);
			DOT_DIR = ".git";
		}

		[Test]
		public override void DiffIsProper ()
		{
			string added = rootCheckout + "testfile";
			File.Create (added).Close ();
			repo.Add (added, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			ChangeSet changes = repo.CreateChangeSet (repo.RootPath);
			changes.AddFile (repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "File committed";
			repo.Commit (changes, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
			File.AppendAllText (added, "text" + Environment.NewLine);

			string difftext = @"@@ -0,0 +1 @@
+text
";
			Assert.AreEqual (difftext, repo.GenerateDiff (added, repo.GetVersionInfo (added, VersionInfoQueryFlags.IgnoreCache)).Content.Replace ("\n", "\r\n"));
		}

		protected override void PostCommit (Repository repo)
		{
			GitRepository repo2 = (GitRepository)repo;
			repo2.Push (new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor (), repo2.GetCurrentRemote (), repo2.GetCurrentBranch ());
		}

		protected override Repository GetRepo (string path, string url)
		{
			return new GitRepository (path, url);
		}
	}
}

