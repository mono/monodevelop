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
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Tests
{
	[TestFixture]
	public abstract class BaseRepoUtilsTest
	{
		// [Git] Set user and email.
		protected const string Author = "author";
		protected const string Email = "email@service.domain";

		protected string RemoteUrl = "";
		protected FilePath RemotePath = "";
		protected FilePath LocalPath;
		protected Repository Repo;
		protected Repository Repo2;
		protected string DotDir;
		protected List<string> AddedItems = new List<string> ();
		protected int CommitNumber = 0;

		[SetUp]
		public virtual Task Setup ()
		{
			var vcs = Repo.VersionControlSystem;
			Console.WriteLine ("Running {0} for {1} (v{2})", TestContext.CurrentContext.Test.FullName, vcs.Name, vcs.Version);
			return Task.CompletedTask;
		}

		[TearDown]
		public virtual void TearDown ()
		{
			if (Repo != null) {
				Repo.Dispose ();
				Repo = null;
			}
			if (Repo2 != null) {
				Repo2.Dispose ();
				Repo2 = null;
			}
			DeleteDirectory (RemotePath);
			DeleteDirectory (LocalPath);
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
		public async Task RightRepositoryDetection ()
		{
			var path = ((string)LocalPath).TrimEnd (Path.DirectorySeparatorChar);
			var repo = VersionControlService.GetRepositoryReference (path, null);
			Assert.That (repo, IsCorrectType (), "#1");

			while (!String.IsNullOrEmpty (path)) {
				path = Path.GetDirectoryName (path);
				if (path == null)
					return;
				Assert.IsNull (VersionControlService.GetRepositoryReference (path, null), "#2." + path);
			}

			// Versioned file
			await AddFileAsync ("foo", "contents", true, true);
			path = Path.Combine (LocalPath, "foo");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#2");

			// Versioned directory
			await AddDirectoryAsync ("bar", true, true);
			path = Path.Combine (LocalPath, "bar");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#3");

			// Unversioned file
			await AddFileAsync ("bip", "contents", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#4");

			// Unversioned directory
			await AddDirectoryAsync ("bop", false, false);
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#5");

			// Nonexistent file
			path = Path.Combine (LocalPath, "do_i_exist");
			Assert.AreSame (VersionControlService.GetRepositoryReference (path, null), repo, "#6");

			// Nonexistent directory
			path = Path.Combine (LocalPath, "do", "i", "exist");
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
			Assert.IsTrue (Directory.Exists (LocalPath + DotDir));
		}

		// In main directory, ".git".
		protected virtual int RepoItemsCount {
			get { return 0; }
		}

		// All contents of ".git".
		protected virtual int RepoItemsCountRecursive {
			get { return 0; }
		}

		// Subversion does an initial query.
		protected virtual VersionStatus InitialValue {
			get { return VersionStatus.Versioned; }
		}

		protected int QueryTimer {
			get { return 1000; }
		}

		[Test]
		// Tests Repository.GetVersionInfo with query thread.
		public async Task QueryThreadWorks ()
		{
			// Cache is initially empty.
			await AddFileAsync ("testfile", null, true, false);

			// Query two queries.
			VersionInfo vi = await Repo.GetVersionInfoAsync (LocalPath + "testfile");
			VersionInfo[] vis = await Repo.GetDirectoryVersionInfoAsync (LocalPath, false, false);

			// No cache, query.
			Assert.AreEqual (InitialValue, vi.Status);
			Assert.AreEqual (0, vis.Length);
			System.Threading.Thread.Sleep (QueryTimer);

			// Cached.
			vi = await Repo.GetVersionInfoAsync (LocalPath + "testfile");
			Assert.AreEqual (VersionStatus.ScheduledAdd, vi.Status & VersionStatus.ScheduledAdd);

			await AddDirectoryAsync ("testdir", true, false);
			await AddFileAsync (Path.Combine ("testdir", "testfile2"), null, true, false);

			// Old cache.
			vis = await Repo.GetDirectoryVersionInfoAsync (LocalPath, false, false);
			Assert.AreEqual (1 + RepoItemsCount, vis.Length, "Old DirectoryVersionInfo.");

			// Query.
			Repo.ClearCachedVersionInfo (LocalPath);
			await Repo.GetDirectoryVersionInfoAsync (LocalPath, false, false);
			System.Threading.Thread.Sleep (QueryTimer);

			// Cached.
			vis = await Repo.GetDirectoryVersionInfoAsync (LocalPath, false, false);
			Assert.AreEqual (2 + RepoItemsCount, vis.Length, "New DirectoryVersionInfo.");

			// Wait for result.
			await AddFileAsync ("testfile3", null, true, false);
			vis = await Repo.GetDirectoryVersionInfoAsync (LocalPath, false, true);
			Assert.AreEqual (4 + RepoItemsCountRecursive, vis.Length, "Recursive DirectoryVersionInfo.");
		}

		[Test]
		// Tests Repository.Add.
		public async Task FileIsAdded ()
		{
			await AddFileAsync ("testfile", null, true, false);

			VersionInfo vi = await Repo.GetVersionInfoAsync (LocalPath + "testfile", VersionInfoQueryFlags.IgnoreCache);

			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
			Assert.AreEqual (VersionStatus.ScheduledAdd, (VersionStatus.ScheduledAdd & vi.Status));
			Assert.IsFalse (vi.CanAdd);
		}

		[Test]
		// Tests Repository.Commit.
		public async Task FileIsCommitted ()
		{
			await AddFileAsync ("testfile", null, true, true);
			await PostCommit (Repo);

			VersionInfo vi = await Repo.GetVersionInfoAsync (LocalPath + "testfile", VersionInfoQueryFlags.IncludeRemoteStatus | VersionInfoQueryFlags.IgnoreCache);
			// TODO: Fix Win32 Svn Remote status check.
			Assert.AreEqual (VersionStatus.Versioned, (VersionStatus.Versioned & vi.Status));
		}

		protected virtual Task PostCommit (Repository repo)
		{
			return Task.CompletedTask;
		}

		[Test]
		// Tests Repository.Update.
		public virtual async Task UpdateIsDone ()
		{
			var monitor = new ProgressMonitor ();

			await AddFileAsync ("testfile", null, true, true);
			await PostCommit (Repo);

			// Checkout a second repository.
			FilePath second = new FilePath (FileService.CreateTempDirectory () + Path.DirectorySeparatorChar);
			await CheckoutAsync (second, RemoteUrl);
			Repo2 = GetRepo (second, RemoteUrl);
			ModifyPath (Repo2, ref second);
			string added = second + "testfile2";
			File.Create (added).Close ();
			await Repo2.AddAsync (new FilePath [] { added }, false, monitor);
			ChangeSet changes = Repo2.CreateChangeSet (Repo2.RootPath);
			changes.AddFile (await Repo2.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = "test2";
			await Repo2.CommitAsync (changes, monitor);

			await PostCommit (Repo2);

			await Task.Run(async () => await Repo.UpdateAsync (Repo.RootPath, true, monitor));
			Assert.True (File.Exists (LocalPath + "testfile2"));

			Repo2.Dispose ();
			DeleteDirectory (second);
		}

		protected virtual void ModifyPath (Repository repo, ref FilePath old)
		{
		}

		[Test]
		// Tests Repository.GetHistory.
		public virtual async Task LogIsProper ()
		{
			await AddFileAsync ("testfile", null, true, true);
			await AddFileAsync ("testfile2", null, true, true);
			await AddFileAsync ("testfile3", null, true, true);

			await CheckLog (Repo);
		}

		protected abstract Task CheckLog (Repository repo);

		[Ignore ("This is failing on Wrench (Windows), and it seems to be choking on symlinks on Mac.")]
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		// Tests Repository.GetHistory with slices.
		public async Task LogSinceWorksAsync (int historyId)
		{
			await AddFileAsync ("testfile", null, true, true);
			await AddFileAsync ("testfile2", null, true, true);
			await AddFileAsync ("testfile3", null, true, true);

			var history = await Repo.GetHistoryAsync (LocalPath, null);
			foreach (var rev in await Repo.GetHistoryAsync (LocalPath, history[historyId]))
				Assert.AreNotEqual (await history [historyId].GetPreviousAsync (), rev, "The revision was found in slice, yet should not be in it.");

			Assert.True (history.Any (r => r == history[historyId]));
		}

		[Test]
		// Tests Repository.GenerateDiff.
		public async Task DiffIsProper ()
		{
			await AddFileAsync ("testfile", null, true, true);
			File.AppendAllText (LocalPath + "testfile", "text");

			await TestDiff ();
		}

		protected abstract Task TestDiff ();

		[Test]
		// Tests Repository.Revert and Repository.GetBaseText.
		public async Task Reverts ()
		{
			var monitor = new ProgressMonitor ();
			string content = "text";
			await AddFileAsync ("testfile", null, true, true);
			string added = LocalPath + "testfile";

			// Force cache update.
			await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);

			// Revert to head.
			File.WriteAllText (added, content);
			await Task.Run (async () => await Repo.RevertAsync (added, false, monitor));
			Assert.AreEqual (await Repo.GetBaseTextAsync (added), File.ReadAllText (added));
		}

		[TestCase (true)]
		[TestCase (false)]
		// Tests Repository.Revert
		public async Task Reverts2 (bool stage)
		{
			var monitor = new ProgressMonitor ();
			await AddFileAsync ("init", null, true, true);

			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", "test", stage, false);

			// Force cache evaluation.
			await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);

			await Task.Run (() => Repo.RevertAsync (added, false, monitor));
			Assert.AreEqual (VersionStatus.Unversioned, (await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache)).Status);
		}

		[Test]
		// Tests Repository.GetRevisionChanges.
		public async Task CorrectRevisionChanges ()
		{
			await AddFileAsync ("testfile", "text", true, true);
			// TODO: Extend and test each member and more types.
			foreach (var rev in await Repo.GetRevisionChangesAsync (GetHeadRevision ())) {
				Assert.AreEqual (RevisionAction.Add, rev.Action);
			}
		}

		protected abstract Revision GetHeadRevision ();

		[Test]
		// Tests Repository.RevertRevision.
		public virtual async Task RevertsRevision ()
		{
			if (!Repo.SupportsRevertRevision)
				Assert.Ignore ("No support for reverting a specific revision.");

			var monitor = new ProgressMonitor ();
			string added = LocalPath + "testfile2";
			await AddFileAsync ("testfile", "text", true, true);
			await AddFileAsync ("testfile2", "text2", true, true);
			Task.Run (async () => await Repo.RevertRevisionAsync (added, GetHeadRevision (), monitor)).Wait ();
			Assert.IsFalse (File.Exists (added));
		}

		[Test]
		// Tests Repository.MoveFile.
		public virtual async Task MovesFile ()
		{
			string src;
			string dst;
			VersionInfo srcVi;
			VersionInfo dstVi;
			var monitor = new ProgressMonitor ();

			// Versioned file.
			await AddFileAsync ("testfile", null, true, true);
			src = LocalPath + "testfile";
			dst = src + "2";
			await Repo.MoveFileAsync (src, dst, false, monitor);
			srcVi = await Repo.GetVersionInfoAsync (src, VersionInfoQueryFlags.IgnoreCache);
			dstVi = await Repo.GetVersionInfoAsync (dst, VersionInfoQueryFlags.IgnoreCache);
			const VersionStatus versionedStatus = VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace;
			Assert.AreNotEqual (VersionStatus.Unversioned, srcVi.Status & versionedStatus);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);

			// Just added file.
			await AddFileAsync ("addedfile", null, true, false);
			src = LocalPath + "addedfile";
			dst = src + "2";
			await Repo.MoveFileAsync (src, dst, false, monitor);
			srcVi = await Repo.GetVersionInfoAsync (src, VersionInfoQueryFlags.IgnoreCache);
			dstVi = await Repo.GetVersionInfoAsync (dst, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, srcVi.Status);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);

			// Non versioned file.
			await AddFileAsync ("unversionedfile", null, false, false);
			src = LocalPath + "unversionedfile";
			dst = src + "2";
			await Repo.MoveFileAsync (src, dst, false, monitor);
			srcVi = await Repo.GetVersionInfoAsync (src, VersionInfoQueryFlags.IgnoreCache);
			dstVi = await Repo.GetVersionInfoAsync (dst, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, srcVi.Status);
			Assert.AreEqual (VersionStatus.Unversioned, dstVi.Status);
		}

		[Test]
		// Tests Repository.MoveDirectory.
		public virtual async Task MovesDirectory ()
		{
			string srcDir = LocalPath.Combine ("test");
			string dstDir = LocalPath.Combine ("test2");
			string src = Path.Combine (srcDir, "testfile");
			string dst = Path.Combine (dstDir, "testfile");
			var monitor = new ProgressMonitor ();

			await AddDirectoryAsync ("test", true, false);
			await AddFileAsync (Path.Combine ("test", "testfile"), null, true, true);

			await Repo.MoveDirectoryAsync (srcDir, dstDir, false, monitor);
			VersionInfo srcVi = await Repo.GetVersionInfoAsync (src, VersionInfoQueryFlags.IgnoreCache);
			VersionInfo dstVi = await Repo.GetVersionInfoAsync (dst, VersionInfoQueryFlags.IgnoreCache);
			const VersionStatus expectedStatus = VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace;
			Assert.AreNotEqual (VersionStatus.Unversioned, srcVi.Status & expectedStatus);
			Assert.AreEqual (VersionStatus.ScheduledAdd, dstVi.Status & VersionStatus.ScheduledAdd);
		}

		async Task DeleteFileTestHelper (bool keepLocal)
		{
			VersionInfo vi;
			string added;
			string postFix = keepLocal ? "2" : "";
			var monitor = new ProgressMonitor ();
			// Versioned file.
			added = LocalPath.Combine ("testfile1") + postFix;
			await AddFileAsync ("testfile1" + postFix, null, true, true);
			await Repo.DeleteFileAsync (added, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledDelete, vi.Status & VersionStatus.ScheduledDelete);
			Assert.AreEqual (keepLocal, File.Exists (added));

			// Just added file.
			added = LocalPath.Combine ("testfile2") + postFix;
			await AddFileAsync ("testfile2" + postFix, null, true, false);
			await Repo.DeleteFileAsync (added, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
			Assert.AreEqual (keepLocal, File.Exists (added));

			// Non versioned file.
			added = LocalPath.Combine ("testfile3") + postFix;
			await AddFileAsync ("testfile3" + postFix, null, false, false);
			await Repo.DeleteFileAsync (added, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
			Assert.AreEqual (keepLocal, File.Exists (added));
		}

		[TestCase(false)]
		[TestCase(true)]
		// Tests Repository.DeleteFile.
		public virtual async Task DeletesFile (bool keepLocal)
		{
			await DeleteFileTestHelper (keepLocal);
		}

		async Task DeleteTestDirectoryHelper (bool keepLocal)
		{
			VersionInfo vi;
			string addedDir;
			string added;
			string postFix = keepLocal ? "2" : "";
			var monitor = new ProgressMonitor ();

			// Versioned directory.
			addedDir = LocalPath.Combine ("test1") + postFix;
			added = Path.Combine (addedDir, "testfile");
			await AddDirectoryAsync ("test1" + postFix, true, false);
			await AddFileAsync (Path.Combine ("test1" + postFix, "testfile"), null, true, true);

			Repo.DeleteDirectory (addedDir, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.ScheduledDelete, vi.Status & VersionStatus.ScheduledDelete);
			Assert.AreEqual (keepLocal, File.Exists (added));

			// Just added directory.
			addedDir = LocalPath.Combine ("test2") + postFix;
			added = Path.Combine (addedDir, "testfile");
			await AddDirectoryAsync ("test2" + postFix, true, false);
			await AddFileAsync (Path.Combine ("test2" + postFix, "testfile"), null, true, false);

			Repo.DeleteDirectory (addedDir, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
			Assert.AreEqual (keepLocal, File.Exists (added));

			// Non versioned file.
			addedDir = LocalPath.Combine ("test3") + postFix;
			added = Path.Combine (addedDir, "testfile");
			await AddDirectoryAsync ("test3" + postFix, true, false);
			await AddFileAsync (Path.Combine ("test3" + postFix, "testfile"), null, false, false);

			Repo.DeleteDirectory (addedDir, true, monitor, keepLocal);
			vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
			Assert.AreEqual (keepLocal, File.Exists (added));
		}

		[Test]
		// Tests Repository.DeleteDirectory.
		public virtual async Task DeletesDirectory ()
		{
			await DeleteTestDirectoryHelper (false);
			await DeleteTestDirectoryHelper (true);
		}

		[Test]
		// Tests Repository.Lock.
		public virtual async Task LocksEntities ()
		{
			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", null, true, true);
			var monitor = new ProgressMonitor ();
			Repo.Lock (monitor, added);

			PostLock ();
		}

		protected virtual void PostLock ()
		{
		}

		[Test]
		// Tests Repository.Unlock.
		public virtual async Task UnlocksEntities ()
		{
			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", null, true, true);
			var monitor = new ProgressMonitor ();
			Repo.Lock (monitor, "testfile");
			Repo.Unlock (monitor, added);

			PostLock ();
		}

		protected virtual void PostUnlock ()
		{
		}

		[Test]
		// Tests Repository.Ignore
		public virtual async Task IgnoresEntities ()
		{
			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", null, false, false);
			await Repo.IgnoreAsync (new FilePath[] { added });
			VersionInfo vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Ignored, vi.Status & VersionStatus.Ignored);
		}

		[Test]
		// Tests Repository.Unignore
		public virtual async Task UnignoresEntities ()
		{
			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", null, false, false);
			await Repo.IgnoreAsync (new FilePath[] { added });
			await Repo.UnignoreAsync (new FilePath[] { added });
			VersionInfo vi = await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);
			Assert.AreEqual (VersionStatus.Unversioned, vi.Status);
		}

		[Test]
		// TODO: Fix SvnSharp logic failing to generate correct URL.
		// Tests Repository.GetTextAtRevision.
		public virtual async Task CorrectTextAtRevision ()
		{
			string added = LocalPath + "testfile";
			await AddFileAsync ("testfile", "text1", true, true);
			File.AppendAllText (added, "text2");
			await CommitFileAsync (added);
			string text = await Repo.GetTextAtRevisionAsync (added, GetHeadRevision ());
			Assert.AreEqual ("text1text2", text);
		}

		[Test]
		// Tests Repository.GetAnnotations.
		public async Task BlameIsCorrect ()
		{
			string added = LocalPath.Combine ("testfile");
			// Initial commit.
			await AddFileAsync ("testfile", "blah" + Environment.NewLine, true, true);
			// Second commit.
			File.AppendAllText (added, "wut" + Environment.NewLine);
			await CommitFileAsync (added);
			// Working copy.
			File.AppendAllText (added, "wut2" + Environment.NewLine);

			var annotations = await Repo.GetAnnotationsAsync (added, null);
			for (int i = 0; i < 2; i++) {
				var annotation = annotations [i];
				Assert.IsTrue (annotation.HasDate);
				Assert.IsNotNull (annotation.Date);
			}

			BlameExtraInternals (annotations);

			Assert.False (annotations [2].HasEmail);
			Assert.IsNotNull (annotations [2].Author);
			Assert.IsNull (annotations [2].Email);
			Assert.IsNull (annotations [2].Revision);
			Assert.AreEqual (annotations [2].Text, GettextCatalog.GetString ("working copy"));
			Assert.AreEqual (annotations [2].Author, "<uncommitted>");
		}

		protected abstract void BlameExtraInternals (Annotation [] annotations);

		[Test]
		// Tests bug #23275
		public async Task MoveAndMoveBack ()
		{
			var monitor = new ProgressMonitor ();
			string added = LocalPath.Combine ("testfile");
			string dir = LocalPath.Combine ("testdir");
			string dirFile = Path.Combine (dir, "testfile");
			await AddFileAsync ("testfile", "test", true, true);
			await AddDirectoryAsync ("testdir", true, false);
			await Task.Run (() => Repo.MoveFileAsync (added, dirFile, true, monitor));
			await Task.Run (() => Repo.MoveFileAsync (dirFile, added, true, monitor));

			Assert.AreEqual (VersionStatus.Unversioned, (await Repo.GetVersionInfoAsync (dirFile, VersionInfoQueryFlags.IgnoreCache)).Status);
			Assert.AreEqual (VersionStatus.Versioned, (await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache)).Status);
		}

		[Test]
		public async Task RevertingADeleteMakesTheFileVersioned ()
		{
			var monitor = new ProgressMonitor ();
			var added = LocalPath.Combine ("testfile");
			await AddFileAsync ("testfile", "test", true, true);

			// Force cache update.
			await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache);

			await Task.Run (() => Repo.DeleteFileAsync (added, true, monitor, false));
			await Task.Run (() => Repo.RevertAsync (added, false, monitor));

			Assert.AreEqual (VersionStatus.Versioned, (await Repo.GetVersionInfoAsync (added, VersionInfoQueryFlags.IgnoreCache)).Status);
		}

		[Test]
		public virtual async Task MoveAndMoveBackCaseOnly ()
		{
			var monitor = new ProgressMonitor ();
			string srcFile = LocalPath.Combine ("testfile");
			string dstFile = LocalPath.Combine ("TESTFILE");
			await AddFileAsync ("testfile", "test", true, true);

			await Task.Run (() => Repo.MoveFileAsync (srcFile, dstFile, true, monitor));
			Assert.AreEqual (VersionStatus.ScheduledAdd, (await Repo.GetVersionInfoAsync (dstFile, VersionInfoQueryFlags.IgnoreCache)).Status & VersionStatus.ScheduledAdd);
			Assert.AreEqual (VersionStatus.ScheduledDelete, (await Repo.GetVersionInfoAsync (srcFile, VersionInfoQueryFlags.IgnoreCache)).Status & VersionStatus.ScheduledDelete);

			await Task.Run (() => Repo.MoveFileAsync (dstFile, srcFile, true, monitor));
			Assert.AreEqual (VersionStatus.Unversioned, (await Repo.GetVersionInfoAsync (dstFile, VersionInfoQueryFlags.IgnoreCache)).Status);
			Assert.AreEqual (VersionStatus.Versioned, (await Repo.GetVersionInfoAsync (srcFile, VersionInfoQueryFlags.IgnoreCache)).Status);
			
		}

		[Test]
		public void RevisionFormatMessageChangelogStyle()
		{
			var res = RevisionHelpers.FormatMessage(
@"2009-11-23 Test Author

	* MonoDevelop.CSharp/CSharpBindingCompilerManager.cs: Emit the
	  target platform option. Fixes bug #557146.");

			var expected =
@"Emit the target platform option. Fixes bug #557146.";
			
			Assert.AreEqual (expected, res);
		}

		[Test]
		public void RevisionFormatMessageChangelogStyleMultipleLines ()
		{
			var res = RevisionHelpers.FormatMessage (
@"2005-09-22 Test Author
	* Services/NUnitService.cs:
	* Services/CombineTestGroup.cs:
	* Services/NUnitProjectTestSuite.cs:
	* Services/SystemTestProvider.cs: Only generate a test suite for
	projects that reference the nunit.framework assembly.");

			var expected =
@"* Services/CombineTestGroup.cs:
 * Services/NUnitProjectTestSuite.cs:
 * Services/SystemTestProvider.cs: Only generate a test suite for
 projects that reference the nunit.framework assembly.";
//			var expected =
//@"Only generate a test suite for projects that reference the nunit.framework assembly.";

			Assert.AreEqual (expected, res);
		}

		[Test]
		public void RevisionFormatMessageChangelogStyleMultipleMessages ()
		{
			var res = RevisionHelpers.FormatMessage (
@"2005-08-22 Test Author
	* Commands/ViewCommands.cs: Implemented delete layout command.
	* Gui/Workbench/Layouts/SdiWorkspaceLayout.cs: Properly load saved
	layouts. Added DeleteLayout method.
	* Gui/IWorkbenchLayout.cs: Added DeleteLayout method.
	* MonoDevelopCore.addin.xml: Added Delete Layout command.");

//			var expected =
//@"Implemented delete layout command.
// Properly load saved layouts. Added DeleteLayout method.
// Added DeleteLayout method.
// Added Delete Layout command.

			var expected =
@"Implemented delete layout command. * Gui/Workbench/Layouts/SdiWorkspaceLayout.cs: Properly load saved
 layouts. Added DeleteLayout method.
 * Gui/IWorkbenchLayout.cs: Added DeleteLayout method.
 * MonoDevelopCore.addin.xml: Added Delete Layout command.";

			Assert.AreEqual (expected, res);
		}



		#region Util

		protected async Task CheckoutAsync (string path, string url)
		{
			var monitor = new ProgressMonitor ();
			using (var mockRepo = (UrlBasedRepository)GetRepo ()) {
				mockRepo.Url = url;
				await Task.Run (async () => {
					await mockRepo.CheckoutAsync (path, true, monitor);
				});
			}

			var _repo = GetRepo (path, url);
			if (Repo == null)
				Repo = _repo;
			else
				Repo2 = _repo;
		}

		protected async Task CommitItems ()
		{
			var monitor = new ProgressMonitor ();
			ChangeSet changes = Repo.CreateChangeSet (Repo.RootPath);
			foreach (var item in AddedItems) {
				changes.AddFile (await Repo.GetVersionInfoAsync (item, VersionInfoQueryFlags.IgnoreCache));
			}
			changes.GlobalComment = String.Format ("Commit #{0}", CommitNumber);
			await Repo.CommitAsync (changes, monitor);
			CommitNumber++;
		}

		protected async Task CommitFileAsync (string path)
		{
			var monitor = new ProgressMonitor ();
			ChangeSet changes = Repo.CreateChangeSet (Repo.RootPath);

			// [Git] Needed by build bots.
			changes.ExtendedProperties.Add ("Git.AuthorName", "author");
			changes.ExtendedProperties.Add ("Git.AuthorEmail", "email@service.domain");

			changes.AddFile (await Repo.GetVersionInfoAsync (path, VersionInfoQueryFlags.IgnoreCache));
			changes.GlobalComment = String.Format ("Commit #{0}", CommitNumber);
			await Repo.CommitAsync (changes, monitor);
			CommitNumber++;
		}

		protected Task AddFileAsync (string path, string contents, bool toVcs, bool commit)
		{
			return AddToRepository (path, contents ?? "", toVcs, commit);
		}

		protected Task AddDirectoryAsync (string path, bool toVcs, bool commit)
		{
			return AddToRepository (path, null, toVcs, commit);
		}

		async Task AddToRepository (string relativePath, string contents, bool toVcs, bool commit)
		{
			var monitor = new ProgressMonitor ();
			string added = Path.Combine (LocalPath, relativePath);
			if (contents == null)
				Directory.CreateDirectory (added);
			else
				File.WriteAllText (added, contents);

			if (toVcs)
				await Repo.AddAsync (new FilePath [] { added }, false, monitor);

			if (commit)
				await CommitFileAsync (added);
			else
				AddedItems.Add (added);
		}

		protected abstract Repository GetRepo ();
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
