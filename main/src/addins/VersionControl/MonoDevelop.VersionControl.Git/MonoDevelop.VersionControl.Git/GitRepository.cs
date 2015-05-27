//
// GitRepository.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

//#define DEBUG_GIT

using System;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide;
using ProgressMonitor = MonoDevelop.Core.ProgressMonitor;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	[Flags]
	public enum GitUpdateOptions
	{
		None = 0x0,
		SaveLocalChanges = 0x1,
		UpdateSubmodules = 0x2,
		NormalUpdate = SaveLocalChanges | UpdateSubmodules,
	}

	public sealed class GitRepository : UrlBasedRepository
	{
		public LibGit2Sharp.Repository RootRepository {
			get; set;
		}

		public static event EventHandler BranchSelectionChanged;

		public GitRepository ()
		{
			Url = "git://";
		}

		public GitRepository (VersionControlSystem vcs, FilePath path, string url) : base (vcs)
		{
			RootPath = path;
			string discovered = LibGit2Sharp.Repository.Discover (path);
			if (!string.IsNullOrEmpty (discovered))
				RootRepository = new LibGit2Sharp.Repository (discovered);
			Url = url;
		}

		public override void Dispose ()
		{
			if (VersionControlSystem != null)
				((GitVersionControl)VersionControlSystem).UnregisterRepo (this);

			if (RootRepository != null)
				RootRepository.Dispose ();
			foreach (var rep in cachedSubmodules)
				rep.Item2.Dispose ();
			base.Dispose ();
		}

		public override string[] SupportedProtocols {
			get {
				return new [] {"git", "ssh", "http", "https", /*"ftp", "ftps", "rsync",*/ "file"};
			}
		}

		public override bool IsUrlValid (string url)
		{
			if (url.Contains (':')) {
				var tokens = url.Split (new[] { ':' }, 2);
				if (Uri.IsWellFormedUriString (tokens [0], UriKind.RelativeOrAbsolute) ||
					Uri.IsWellFormedUriString (tokens [1], UriKind.RelativeOrAbsolute))
					return true;
			}

			return base.IsUrlValid (url);
		}

		/*public override string[] SupportedNonUrlProtocols {
			get {
				return new string[] {"ssh/scp"};
			}
		}

		public override string Protocol {
			get {
				string p = base.Protocol;
				if (p != null)
					return p;
				return IsUrlValid (Url) ? "ssh/scp" : null;
			}
		}*/

		public override void CopyConfigurationFrom (Repository other)
		{
			base.CopyConfigurationFrom (other);

			var r = (GitRepository)other;
			RootPath = r.RootPath;
			if (!RootPath.IsNullOrEmpty)
				RootRepository = new LibGit2Sharp.Repository (RootPath);
		}

		public override string LocationDescription {
			get { return Url ?? RootPath; }
		}

		public override bool AllowLocking {
			get { return false; }
		}

		public override string GetBaseText (FilePath localFile)
		{
			Commit c = GetHeadCommit (GetRepository (localFile));
			return c == null ? string.Empty : GetCommitTextContent (c, localFile);
		}

		static Commit GetHeadCommit (LibGit2Sharp.Repository repository)
		{
			return repository.Head.Tip;
		}

		public StashCollection GetStashes ()
		{
			return RootRepository.Stashes;
		}

		const CheckoutNotifyFlags refreshFlags = CheckoutNotifyFlags.Updated | CheckoutNotifyFlags.Conflict | CheckoutNotifyFlags.Untracked | CheckoutNotifyFlags.Dirty;
		bool RefreshFile (string path, CheckoutNotifyFlags flags)
		{
			FilePath fp = RootRepository.FromGitPath (path);
			if (IdeApp.IsInitialized) {
				MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.GetDocument (fp);
				if (doc != null)
					doc.Reload ();
			}
			FileService.NotifyFileChanged (fp);
			VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (this, fp, false));
			return true;
		}

		static bool OnTransferProgress (TransferProgress tp, ProgressMonitor monitor, ref int progress)
		{
			if (progress == 0 && tp.ReceivedObjects == 0)
				monitor.BeginTask ("Receiving and indexing objects", 2 * tp.TotalObjects);

			int currentProgress = tp.ReceivedObjects + tp.IndexedObjects;
			int steps = currentProgress - progress;
			monitor.Step (steps);
			progress = currentProgress;

			if (tp.IndexedObjects >= tp.TotalObjects)
				monitor.EndTask ();

			return !monitor.CancellationToken.IsCancellationRequested;
		}

		static void OnCheckoutProgress (int completedSteps, int totalSteps, ProgressMonitor monitor, ref int progress)
		{
			if (progress == 0 && completedSteps == 0)
				monitor.BeginTask ("Checking out files", totalSteps);

			int steps = completedSteps - progress;
			monitor.Step (steps);
			progress = completedSteps;

			if (completedSteps >= totalSteps)
				monitor.EndTask ();
		}

		void NotifyFilesChangedForStash (Stash stash)
		{
			// HACK: Notify file changes.
			foreach (var entry in RootRepository.Diff.Compare<TreeChanges> (stash.WorkTree.Tree, stash.Base.Tree)) {
				if (entry.Status == ChangeKind.Deleted || entry.Status == ChangeKind.Renamed) {
					FileService.NotifyFileRemoved (RootRepository.FromGitPath (entry.OldPath));
				} else {
					FileService.NotifyFileChanged (RootRepository.FromGitPath (entry.Path));
				}
			}
		}

		public StashApplyStatus ApplyStash (ProgressMonitor monitor, int stashIndex)
		{
			if (monitor != null)
				monitor.BeginTask ("Applying stash", 1);

			int progress = 0;
			var res = RootRepository.Stashes.Apply (stashIndex, StashApplyModifiers.Default, new CheckoutOptions {
				OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
				OnCheckoutNotify = RefreshFile,
				CheckoutNotifyFlags = refreshFlags,
			});

			NotifyFilesChangedForStash (RootRepository.Stashes [stashIndex]);
			if (monitor != null)
				monitor.EndTask ();

			return res;
		}

		public StashApplyStatus PopStash (ProgressMonitor monitor, int stashIndex)
		{
			if (monitor != null)
				monitor.BeginTask ("Applying stash", 1);

			var stash = RootRepository.Stashes [stashIndex];
			int progress = 0;
			var res = RootRepository.Stashes.Pop (stashIndex, StashApplyModifiers.Default, new CheckoutOptions {
				OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
				OnCheckoutNotify = RefreshFile,
				CheckoutNotifyFlags = refreshFlags,
			});
			NotifyFilesChangedForStash (stash);
			if (monitor != null)
				monitor.EndTask ();

			return res;
		}

		public bool TryCreateStash (ProgressMonitor monitor, string message, out Stash stash)
		{
			Signature sig = GetSignature ();
			stash = null;
			if (sig == null)
				return false;

			if (monitor != null)
				monitor.BeginTask ("Stashing changes", 1);

			stash = RootRepository.Stashes.Add (sig, message, StashModifiers.Default | StashModifiers.IncludeUntracked);

			if (monitor != null)
				monitor.EndTask ();
			return true;
		}

		internal Signature GetSignature()
		{
			// TODO: Investigate Configuration.BuildSignature.
			string name;
			string email;

			GetUserInfo (out name, out email);
			if (name == null || email == null)
				return null;

			return new Signature (name, email, DateTimeOffset.Now);
		}

		DateTime cachedSubmoduleTime = DateTime.MinValue;
		Tuple<FilePath, LibGit2Sharp.Repository>[] cachedSubmodules = new Tuple<FilePath, LibGit2Sharp.Repository>[0];
		Tuple<FilePath, LibGit2Sharp.Repository>[] CachedSubmodules {
			get {
				var submoduleWriteTime = File.GetLastWriteTimeUtc(RootPath.Combine(".gitmodules"));
				if (cachedSubmoduleTime != submoduleWriteTime) {
					cachedSubmoduleTime = submoduleWriteTime;
					cachedSubmodules = RootRepository.Submodules.Select (s => {
						var fp = new FilePath (Path.Combine (RootRepository.Info.WorkingDirectory, s.Path.Replace ('/', Path.DirectorySeparatorChar))).CanonicalPath;
						return new Tuple<FilePath, LibGit2Sharp.Repository> (fp, new LibGit2Sharp.Repository (fp));
					}).ToArray ();
				}
				return cachedSubmodules;
			}
		}

		LibGit2Sharp.Repository GetRepository (FilePath localPath)
		{
			return GroupByRepository (new [] { localPath }).First ().Key;
		}

		IEnumerable<IGrouping<LibGit2Sharp.Repository, FilePath>> GroupByRepository (IEnumerable<FilePath> files)
		{
			var cache = CachedSubmodules;
			return files.GroupBy (f => {
				var res = cache.FirstOrDefault (s => f.IsChildPathOf (s.Item1) || f.FullPath == s.Item1);
				return res != null ? res.Item2 : RootRepository;
			});
		}

		protected override Revision[] OnGetHistory (FilePath localFile, Revision since)
		{
			var revs = new List<Revision> ();

			var repository = GetRepository (localFile);
			var sinceRev = (GitRevision)since;
			var hc = GetHeadCommit (repository);
			if (hc == null)
				return new GitRevision [0];

			IEnumerable<Commit> commits = repository.Commits;
			if (localFile.CanonicalPath != RootPath.CanonicalPath) {
				var localPath = repository.ToGitPath (localFile);
				commits = commits.Where (c => c.Parents.Count () == 1 && c.Tree [localPath] != null &&
					(c.Parents.FirstOrDefault ().Tree [localPath] == null ||
					c.Tree [localPath].Target.Id != c.Parents.FirstOrDefault ().Tree [localPath].Target.Id));
			}

			foreach (var commit in commits) {
				var author = commit.Author;
				var rev = new GitRevision (this, repository, commit, author.When.LocalDateTime, author.Name, commit.Message) {
					Email = author.Email,
					ShortMessage = commit.MessageShort,
					FileForChanges = localFile,
				};
				revs.Add (rev);
			}

			return revs.ToArray ();
		}

		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			var rev = (GitRevision) revision;
			if (rev.Commit == null)
				return new RevisionPath [0];

			var paths = new List<RevisionPath> ();
			var parent = rev.Commit.Parents.FirstOrDefault ();
			var changes = rev.GitRepository.Diff.Compare <TreeChanges>(parent != null ? parent.Tree : null, rev.Commit.Tree);

			foreach (var entry in changes.Added)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
			foreach (var entry in changes.Copied)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
			foreach (var entry in changes.Deleted)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.OldPath), RevisionAction.Delete, null));
			foreach (var entry in changes.Renamed)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Replace, null));
			foreach (var entry in changes.Modified)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
			foreach (var entry in changes.TypeChanged)
				paths.Add (new RevisionPath (rev.GitRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));

			return paths.ToArray ();
		}

		protected override IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			return GetDirectoryVersionInfo (FilePath.Null, paths, getRemoteStatus, false);
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
		}

		// Used for checking if we will dupe data.
		// This way we reduce the number of GitRevisions created and RevWalks done.
		Dictionary<LibGit2Sharp.Repository, GitRevision> versionInfoCacheRevision = new Dictionary<LibGit2Sharp.Repository, GitRevision> ();
		Dictionary<LibGit2Sharp.Repository, GitRevision> versionInfoCacheEmptyRevision = new Dictionary<LibGit2Sharp.Repository, GitRevision> ();
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive)
		{
			var versions = new List<VersionInfo> ();

			if (localFileNames != null) {
				var localFiles = new List<FilePath> ();
				foreach (var group in GroupByRepository (localFileNames)) {
					var repository = group.Key;
					GitRevision arev;
					if (!versionInfoCacheEmptyRevision.TryGetValue (repository, out arev)) {
						arev = new GitRevision (this, repository, null);
						versionInfoCacheEmptyRevision.Add (repository, arev);
					}
					foreach (var p in group) {
						if (Directory.Exists (p)) {
							if (recursive)
								versions.AddRange (GetDirectoryVersionInfo (p, getRemoteStatus, true));
							versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
						} else
							localFiles.Add (p);
					}
				}
				// No files to check, we are done
				if (localFiles.Count != 0) {
					foreach (var group in GroupByRepository (localFileNames)) {
						var repository = group.Key;

						GitRevision rev = null;
						Commit headCommit = GetHeadCommit (repository);
						if (headCommit != null) {
							if (!versionInfoCacheRevision.TryGetValue (repository, out rev)) {
								rev = new GitRevision (this, repository, headCommit);
								versionInfoCacheRevision.Add (repository, rev);
							} else if (rev.Commit != headCommit) {
								rev = new GitRevision (this, repository, headCommit);
								versionInfoCacheRevision [repository] = rev;
							}
						}

						GetFilesVersionInfoCore (repository, rev, group.ToList (), versions);
					}
				}
			} else {
				var directories = new List<FilePath> ();
				CollectFiles (directories, localDirectory, recursive);

				// Set directory items as Versioned.
				GitRevision arev = null;
				foreach (var group in GroupByRepository (directories)) {
					var repository = group.Key;
					if (!versionInfoCacheEmptyRevision.TryGetValue (repository, out arev)) {
						arev = new GitRevision (this, repository, null);
						versionInfoCacheEmptyRevision.Add (repository, arev);
					}
					foreach (var p in group)
						versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
				}

				Commit headCommit = GetHeadCommit (RootRepository);
				if (headCommit != null) {
					if (!versionInfoCacheRevision.TryGetValue (RootRepository, out arev)) {
						arev = new GitRevision (this, RootRepository, headCommit);
						versionInfoCacheRevision.Add (RootRepository, arev);
					} else if (arev.Commit != headCommit) {
						arev = new GitRevision (this, RootRepository, headCommit);
						versionInfoCacheRevision [RootRepository] = arev;
					}
				}

				GetDirectoryVersionInfoCore (RootRepository, arev, localDirectory.CanonicalPath, versions, recursive);
			}

			return versions.ToArray ();
		}

		static void GetFilesVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, List<FilePath> localPaths, List<VersionInfo> versions)
		{
			foreach (var file in repo.ToGitPath (localPaths)) {
				var status = repo.RetrieveStatus (file);
				VersionStatus fstatus = VersionStatus.Versioned;

				if (status != FileStatus.Unaltered) {
					if ((status & FileStatus.Added) != 0)
						fstatus |= VersionStatus.ScheduledAdd;
					else if ((status & (FileStatus.Removed | FileStatus.Missing)) != 0)
						fstatus |= VersionStatus.ScheduledDelete;
					else if ((status & (FileStatus.TypeChanged | FileStatus.Modified)) != 0)
						fstatus |= VersionStatus.Modified;
					else if ((status & (FileStatus.RenamedInIndex | FileStatus.RenamedInWorkDir)) != 0)
						fstatus |= VersionStatus.ScheduledReplace;
					else if ((status & (FileStatus.Nonexistent | FileStatus.Untracked)) != 0)
						fstatus = VersionStatus.Unversioned;
					else if ((status & FileStatus.Ignored) != 0)
						fstatus = VersionStatus.Ignored;
				}

				if (repo.Index.Conflicts [file] != null)
					fstatus = VersionStatus.Versioned | VersionStatus.Conflicted;

				versions.Add (new VersionInfo (repo.FromGitPath (file), "", false, fstatus, rev, fstatus == VersionStatus.Ignored ? VersionStatus.Unversioned : VersionStatus.Versioned, null));
			}
		}

		static void GetDirectoryVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, FilePath directory, List<VersionInfo> versions, bool recursive)
		{
			var relativePath = repo.ToGitPath (directory);
			var status = repo.RetrieveStatus (new StatusOptions {
				PathSpec = relativePath != "." ? new [] { relativePath } : null,
				IncludeUnaltered = true,
			});

			versions.AddRange (status.Added.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.ScheduledAdd, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.Ignored.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Ignored, rev, VersionStatus.Unversioned, null)));
			versions.AddRange (status.Missing.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.ScheduledDelete, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.Modified.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.Modified, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.Removed.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.ScheduledDelete, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.RenamedInIndex.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.ScheduledReplace, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.RenamedInWorkDir.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned | VersionStatus.ScheduledReplace, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.Unaltered.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Versioned, rev, VersionStatus.Versioned, null)));
			versions.AddRange (status.Untracked.Select (s => new VersionInfo (
				repo.FromGitPath (s.FilePath), "", false, VersionStatus.Unversioned, rev, VersionStatus.Unversioned, null)));

			foreach (var conflict in repo.Index.Conflicts) {
				FilePath path = conflict.Ours.Path;
				if (path.IsChildPathOf (directory))
					versions.Add (new VersionInfo (
						repo.FromGitPath (path), "", false, VersionStatus.Versioned | VersionStatus.Conflicted, rev, VersionStatus.Versioned, null));
			}

			if (!recursive)
				versions.RemoveAll (v => v.LocalPath.ParentDirectory != directory);
		}

		protected override VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
		{
			VersionControlOperation ops = base.GetSupportedOperations (vinfo);
			if (GetCurrentRemote () == null)
				ops &= ~VersionControlOperation.Update;
			if (vinfo.IsVersioned && !vinfo.IsDirectory)
				ops |= VersionControlOperation.Annotate;
			if (!vinfo.IsVersioned && vinfo.IsDirectory)
				ops &= ~VersionControlOperation.Add;
			return ops;
		}

		static void CollectFiles (List<FilePath> directories, FilePath dir, bool recursive)
		{
			if (!Directory.Exists (dir))
				return;

			directories.AddRange (Directory.GetDirectories (dir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
				.Select (f => new FilePath (f)));
		}

		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
		{
			// Initialize the repository
			RootRepository = new LibGit2Sharp.Repository (LibGit2Sharp.Repository.Init (localPath));
			RootPath = localPath;
			RootRepository.Network.Remotes.Add ("origin", Url);

			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			foreach (FilePath fp in files) {
				RootRepository.Stage (RootRepository.ToGitPath (fp));
				cs.AddFile (fp);
			}

			// Create the initial commit
			cs.GlobalComment = message;
			Commit (cs, monitor);

			RootRepository.Branches.Update (RootRepository.Branches ["master"], branch => branch.TrackedBranch = "refs/remotes/origin/master");

			RetryUntilSuccess (monitor, delegate {
				RootRepository.Network.Push (RootRepository.Head, new PushOptions {
					OnPushStatusError = delegate (PushStatusError e) {
						RootRepository.Dispose ();
						RootRepository = null;
						if (RootPath.Combine (".git").IsDirectory)
							Directory.Delete (RootPath.Combine (".git"), true);
						throw new VersionControlException (e.Message);
					},
					CredentialsProvider = GitCredentials.TryGet
				});
			});

			return this;
		}

		protected override void OnUpdate (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			// TODO: Make it work differently for submodules.
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);

			if (RootRepository.Head.IsTracking) {
				Fetch (monitor, RootRepository.Head.Remote.Name);

				GitUpdateOptions options = GitService.StashUnstashWhenUpdating ? GitUpdateOptions.NormalUpdate : GitUpdateOptions.UpdateSubmodules;
				if (GitService.UseRebaseOptionWhenPulling)
					Rebase (RootRepository.Head.TrackedBranch.Name, options, monitor);
				else
					Merge (RootRepository.Head.TrackedBranch.Name, options, monitor);

				monitor.Step (1);
			}

			monitor.EndTask ();
		}

		static void RetryUntilSuccess (ProgressMonitor monitor, Action func)
		{
			bool retry;
			do {
				try {
					func ();
					GitCredentials.StoreCredentials ();
					retry = false;
				} catch (AuthenticationException) {
					GitCredentials.InvalidateCredentials ();
					retry = true;
				} catch (VersionControlException e) {
					GitCredentials.InvalidateCredentials ();
					if (monitor != null)
						monitor.ReportError (e.Message, null);
					retry = false;
				} catch (UserCancelledException) {
					GitCredentials.StoreCredentials ();
					retry = false;
				} catch (LibGit2SharpException e) {
					GitCredentials.InvalidateCredentials ();

					string message;
					// TODO: Remove me once https://github.com/libgit2/libgit2/pull/3137 goes in.
					if (string.Equals (e.Message, "early EOF", StringComparison.OrdinalIgnoreCase))
						message = "Unable to authorize credentials for the repository.";
					else if (string.Equals (e.Message, "Received unexpected content-type", StringComparison.OrdinalIgnoreCase))
						message = "Not a valid git repository.";
					else
						message = e.Message;

					if (monitor != null)
						monitor.ReportError (message, null);
					retry = false;
				}
			} while (retry);
		}

		public void Fetch (ProgressMonitor monitor, string remote)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
			int progress = 0;
			RetryUntilSuccess (monitor, delegate {
				RootRepository.Fetch (remote, new FetchOptions {
					CredentialsProvider = GitCredentials.TryGet,
					OnTransferProgress = (tp) => OnTransferProgress (tp, monitor, ref progress),
				});
			});
			monitor.Step (1);
		}

		bool CommonPreMergeRebase (GitUpdateOptions options, ProgressMonitor monitor, out int stashIndex)
		{
			stashIndex = -1;
			monitor.Step (1);

			if ((options & GitUpdateOptions.SaveLocalChanges) != GitUpdateOptions.SaveLocalChanges) {
				const VersionStatus unclean = VersionStatus.Modified | VersionStatus.ScheduledAdd | VersionStatus.ScheduledDelete;
				bool modified = false;
				if (GetDirectoryVersionInfo (RootPath, false, true).Any (v => (v.Status & unclean) != VersionStatus.Unversioned))
					modified = true;

				if (modified) {
					if (MessageService.GenericAlert (
						    MonoDevelop.Ide.Gui.Stock.Question,
						    GettextCatalog.GetString ("You have uncommitted changes"),
						    GettextCatalog.GetString ("What do you want to do?"),
						    AlertButton.Cancel,
						    new AlertButton ("Stash")) == AlertButton.Cancel)
						return false;

					options |= GitUpdateOptions.SaveLocalChanges;
				}
			}
			if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
				Stash stash;
				if (!TryCreateStash (monitor, GetStashName ("_tmp_"), out stash))
					return false;

				if (stash != null)
					stashIndex = 0;
				monitor.Step (1);
			}
			return true;
		}

		void ConflictResolver(ProgressMonitor monitor, Commit resetToIfFail, string message)
		{
			bool commit = true;
			foreach (var conflictFile in RootRepository.Index.Conflicts) {
				ConflictResult res = ResolveConflict (RootRepository.FromGitPath (conflictFile.Ancestor.Path));
				if (res == ConflictResult.Abort) {
					RootRepository.Reset (ResetMode.Hard, resetToIfFail);
					commit = false;
					break;
				}
				if (res == ConflictResult.Skip) {
					Revert (RootRepository.FromGitPath (conflictFile.Ancestor.Path), false, monitor);
					break;
				}
			}
			if (commit)
				RootRepository.Commit (message);
		}

		void CommonPostMergeRebase(int stashIndex, GitUpdateOptions options, ProgressMonitor monitor)
		{
			if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
				monitor.Step (1);

				// Restore local changes
				if (stashIndex != -1) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					PopStash (monitor, stashIndex);
					monitor.Step (1);
				}
			}
			monitor.EndTask ();
		}

		public void Rebase (string branch, GitUpdateOptions options, ProgressMonitor monitor)
		{
			int stashIndex = -1;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 5);
				if (!CommonPreMergeRebase (options, monitor, out stashIndex))
					return;

				// Do a rebase.
				var divergence = RootRepository.ObjectDatabase.CalculateHistoryDivergence (RootRepository.Head.Tip, RootRepository.Branches [branch].Tip);
				var toApply = RootRepository.Commits.QueryBy (new CommitFilter {
					Since = RootRepository.Head.Tip,
					Until = divergence.CommonAncestor,
					SortBy = CommitSortStrategies.Topological
				});

				RootRepository.Reset (ResetMode.Hard, divergence.Another);

				int count = toApply.Count ();
				int i = 1;
				foreach (var com in toApply) {
					monitor.Log.WriteLine ("Cherry-picking {0} - {1}/{2}", com.Id, i, count);
					CherryPickResult cherryRes = RootRepository.CherryPick (com, com.Author, new CherryPickOptions {
						CheckoutNotifyFlags = refreshFlags,
						OnCheckoutNotify = RefreshFile,
					});
					if (cherryRes.Status == CherryPickStatus.Conflicts)
						ConflictResolver(monitor, toApply.Last(), RootRepository.Info.Message ?? com.Message);
					++i;
				}
			} finally {
				CommonPostMergeRebase (stashIndex, options, monitor);
			}
		}

		public void Merge (string branch, GitUpdateOptions options, ProgressMonitor monitor)
		{
			int stashIndex = -1;

			Signature sig = GetSignature ();
			if (sig == null)
				return;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				CommonPreMergeRebase (options, monitor, out stashIndex);

				// Do a merge.
				MergeResult mergeResult = RootRepository.Merge (branch, sig, new MergeOptions {
					CheckoutNotifyFlags = refreshFlags,
					OnCheckoutNotify = RefreshFile,
				});

				if (mergeResult.Status == MergeStatus.Conflicts)
					ConflictResolver (monitor, RootRepository.Head.Tip, RootRepository.Info.Message);
			} finally {
				CommonPostMergeRebase (stashIndex, GitUpdateOptions.SaveLocalChanges, monitor);
			}
		}

		static ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			DispatchService.GuiSyncDispatch (delegate {
				var dlg = new ConflictResolutionDialog ();
				try {
					dlg.Load (file);
					var dres = (Gtk.ResponseType) MessageService.RunCustomDialog (dlg);
					dlg.Hide ();
					switch (dres) {
					case Gtk.ResponseType.Cancel:
						res = ConflictResult.Abort;
						break;
					case Gtk.ResponseType.Close:
						res = ConflictResult.Skip;
						break;
					case Gtk.ResponseType.Ok:
						res = ConflictResult.Continue;
						dlg.Save (file);
						break;
					}
				} finally {
					dlg.Destroy ();
				}
			});
			return res;
		}

		protected override void OnCommit (ChangeSet changeSet, ProgressMonitor monitor)
		{
			string message = changeSet.GlobalComment;
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Commit message must not be null or empty!", "message");

			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var repo = (GitRepository)changeSet.Repository;
			repo.RootRepository.Stage (changeSet.Items.Select (i => i.LocalPath).ToPathStrings ());

			if (changeSet.ExtendedProperties.Contains ("Git.AuthorName"))
				repo.RootRepository.Commit (message, new Signature (
					(string)changeSet.ExtendedProperties ["Git.AuthorName"],
					(string)changeSet.ExtendedProperties ["Git.AuthorEmail"],
					DateTimeOffset.Now), sig);
			else
				repo.RootRepository.Commit (message, sig);
		}

		public bool IsUserInfoDefault ()
		{
			string name;
			string email;
			try {
				name = RootRepository.Config.Get<string> ("user.name").Value;
				email = RootRepository.Config.Get<string> ("user.email").Value;
			} catch {
				name = email = null;
			}
			return name == null && email == null;
		}

		public void GetUserInfo (out string name, out string email)
		{
			try {
				name = RootRepository.Config.Get<string> ("user.name").Value;
				email = RootRepository.Config.Get<string> ("user.email").Value;
			} catch {
				string dlgName = null, dlgEmail = null;

				DispatchService.GuiSyncDispatch (() => {
					var dlg = new UserGitConfigDialog ();
					try {
						if ((Gtk.ResponseType)MessageService.RunCustomDialog (dlg) == Gtk.ResponseType.Ok) {
							dlgName = dlg.UserText;
							dlgEmail = dlg.EmailText;
							SetUserInfo (dlgName, dlgEmail);
						}
					} finally {
						dlg.Destroy ();
					}
				});

				name = dlgName;
				email = dlgEmail;
			}
		}

		public void SetUserInfo (string name, string email)
		{
			RootRepository.Config.Set ("user.name", name);
			RootRepository.Config.Set ("user.email", email);
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			int transferProgress = 0;
			int checkoutProgress = 0;
			RetryUntilSuccess (monitor, delegate {
				RootPath = LibGit2Sharp.Repository.Clone (Url, targetLocalPath, new CloneOptions {
					CredentialsProvider = GitCredentials.TryGet,

					OnTransferProgress = (tp) => OnTransferProgress (tp, monitor, ref transferProgress),
					OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref checkoutProgress),
				});
			});

			if (monitor.CancellationToken.IsCancellationRequested || RootPath.IsNull)
				return;
			
			RootPath = RootPath.ParentDirectory;
			RootRepository = new LibGit2Sharp.Repository (RootPath);
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				HashSet<FilePath> files = new HashSet<FilePath> ();

				foreach (var item in group)
					if (item.IsDirectory) {
						foreach (var vi in GetDirectoryVersionInfo (item, false, recurse))
							if (!vi.IsDirectory)
								files.Add (vi.LocalPath);
					} else
						files.Add (item);

				monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 1);

				var repoFiles = repository.ToGitPath (files).ToArray ();
				int progress = 0;
				repository.CheckoutPaths ("HEAD", repoFiles, new CheckoutOptions {
					OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
					CheckoutModifiers = CheckoutModifiers.Force,
					CheckoutNotifyFlags = refreshFlags,
					OnCheckoutNotify = delegate (string path, CheckoutNotifyFlags notifyFlags) {
						if ((notifyFlags & CheckoutNotifyFlags.Untracked) != 0)
							FileService.NotifyFileRemoved (repository.FromGitPath (path));
						else
							RefreshFile (path, notifyFlags);
						return true;
					}
				});
				repository.Stage (repoFiles);
				monitor.EndTask ();
			}
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected override void OnAdd (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var repository = group.Key;
				var files = group.Where (f => !f.IsDirectory);
				if (files.Any ())
					repository.Stage (repository.ToGitPath (files));
			}
		}

		protected override void OnDeleteFiles (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
			DeleteCore (localPaths, keepLocal);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of files.
					VersionInfo info = GetVersionInfo (path, VersionInfoQueryFlags.IgnoreCache);
					if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
						// Revert addition.
						Revert (path, false, monitor);
					}
				} else {
					// Untracked files are not deleted by the rm command, so delete them now
					if (File.Exists (path))
						File.Delete (path);
				}
			}
		}

		protected override void OnDeleteDirectories (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
			DeleteCore (localPaths, keepLocal);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of directories and files.
					foreach (var info in GetDirectoryVersionInfo (path, false, true)) {
						if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
							// Revert addition.
							Revert (path, true, monitor);
						}
					}
				} else {
					// Untracked files are not deleted by the rm command, so delete them now
					foreach (var f in localPaths)
						if (Directory.Exists (f))
							Directory.Delete (f, true);
				}
			}
		}

		void DeleteCore (FilePath[] localPaths, bool keepLocal)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				if (!keepLocal)
					foreach (var f in localPaths) {
						if (File.Exists (f))
							File.Delete (f);
						else if (Directory.Exists (f))
							Directory.Delete (f, true);
					}

				var repository = group.Key;
				var files = repository.ToGitPath (group);

				repository.Remove (files, !keepLocal);
			}
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			var gitRev = (GitRevision)revision;
			var blob = (Blob)gitRev.Commit [gitRev.GitRepository.ToGitPath (repositoryPath)].Target;
			return blob.GetContentText ();
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			try {
				var repository = GetRepository (versionInfo.LocalPath);
				var patch = repository.Diff.Compare<Patch> (new [] { repository.ToGitPath (versionInfo.LocalPath) });
				// Trim the header by taking out the first 2 lines.
				int diffStart = patch.Content.IndexOf ('\n', patch.Content.IndexOf ('\n') + 1);
				return new DiffInfo (baseLocalPath, versionInfo.LocalPath, patch.Content.Substring (diffStart + 1));
			} catch (Exception ex) {
				LoggingService.LogError ("Could not get diff for file '" + versionInfo.LocalPath + "'", ex);
			}
			return null;
		}

		public override DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			var diffs = new List<DiffInfo> ();
			VersionInfo[] vinfos = GetDirectoryVersionInfo (baseLocalPath, localPaths, false, true);
			foreach (VersionInfo vi in vinfos) {
				var diff = GenerateDiff (baseLocalPath, vi);
				if (diff != null)
					diffs.Add (diff);
			}
			return diffs.ToArray ();
		}

		byte[] GetCommitContent (Commit c, FilePath file)
		{
			var repository = GetRepository (file);
			var blob = (Blob)c [repository.ToGitPath (file)].Target;
			using (var stream = blob.GetContentStream ())
			using (var ms = new MemoryStream ()) {
				stream.CopyTo (ms);
				return ms.ToArray ();
			}
		}

		string GetCommitTextContent (Commit c, FilePath file)
		{
			var repository = GetRepository (file);
			var blob = (Blob)c [repository.ToGitPath (file)].Target;
			return blob.IsBinary ? String.Empty : blob.GetContentText ();
		}

		public string GetCurrentRemote ()
		{
			var remotes = new List<string> (GetRemotes ().Select (r => r.Name));
			if (remotes.Count == 0)
				return null;

			return remotes.Contains ("origin") ? "origin" : remotes [0];
		}

		public void Push (ProgressMonitor monitor, string remote, string remoteBranch)
		{
			bool success = true;

			var branch = RootRepository.Head;
			if (branch.TrackedBranch == null) {
				RootRepository.Branches.Update (branch, b => b.TrackedBranch = "refs/remotes/" + remote + "/" + remoteBranch);
			}

			RetryUntilSuccess (monitor, () =>
				RootRepository.Network.Push (RootRepository.Network.Remotes [remote], "refs/heads/" + remoteBranch, new PushOptions {
					OnPushStatusError = delegate (PushStatusError pushStatusErrors) {
						success = false;
					},
					CredentialsProvider = GitCredentials.TryGet
				})
			);

			if (!success)
				return;

			monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
		}

		public void CreateBranchFromCommit (string name, Commit id)
		{
			RootRepository.Branches.Add (name, id);
		}

		public void CreateBranch (string name, string trackSource)
		{
			RootRepository.Branches.Update (RootRepository.Branches.Add (name, RootRepository.Head.Tip),
				bu => bu.TrackedBranch = trackSource);
		}

		public void SetBranchTrackSource (string name, string trackSource)
		{
			var branch = RootRepository.Branches [name];
			if (branch != null) {
				RootRepository.Branches.Update (branch,	bu => bu.TrackedBranch = trackSource);
			} else
				RootRepository.Branches.Add (name, trackSource);
		}

		public void RemoveBranch (string name)
		{
			RootRepository.Branches.Remove (name);
		}

		public void RenameBranch (string name, string newName)
		{
			RootRepository.Branches.Rename (name, newName, true);
		}

		public IEnumerable<Remote> GetRemotes ()
		{
			return RootRepository.Network.Remotes;
		}

		public bool IsBranchMerged (string branchName)
		{
			// check if a branch is merged into HEAD
			var tip = RootRepository.Branches [branchName].Tip.Sha;
			return RootRepository.Commits.Any (c => c.Sha == tip);
		}

		public void RenameRemote (string name, string newName)
		{
			RootRepository.Network.Remotes.Rename (name, newName);
		}

		public void ChangeRemoteUrl (string name, string url)
		{
			RootRepository.Network.Remotes.Update (
				RootRepository.Network.Remotes [name],
				r => r.Url = url
			);
		}

		public void ChangeRemotePushUrl (string name, string url)
		{
			RootRepository.Network.Remotes.Update (
				RootRepository.Network.Remotes [name],
				r => r.PushUrl = url
			);
		}

		public void AddRemote (string name, string url, bool importTags)
		{
			if (string.IsNullOrEmpty (name))
				throw new InvalidOperationException ("Name not set");

			RootRepository.Network.Remotes.Update (RootRepository.Network.Remotes.Add (name, url),
				r => r.TagFetchMode = importTags ? TagFetchMode.All : TagFetchMode.Auto);
		}

		public void RemoveRemote (string name)
		{
			RootRepository.Network.Remotes.Remove (name);
		}

		public IEnumerable<Branch> GetBranches ()
		{
			return RootRepository.Branches.Where (b => !b.IsRemote);
		}

		public IEnumerable<string> GetTags ()
		{
			return RootRepository.Tags.Select (t => t.Name);
		}

		public void AddTag (string name, Revision rev, string message)
		{
			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var gitRev = (GitRevision)rev;
			RootRepository.Tags.Add (name, gitRev.Commit, sig, message);
		}

		public void RemoveTag (string name)
		{
			RootRepository.Tags.Remove (name);
		}

		public void PushTag (string name)
		{
			RetryUntilSuccess (null, delegate {
				RootRepository.Network.Push (RootRepository.Network.Remotes [GetCurrentRemote ()], "refs/tags/" + name + ":refs/tags/" + name, new PushOptions {
					CredentialsProvider = GitCredentials.TryGet,
				});
			});
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			return RootRepository.Branches.Where (b => b.IsRemote && b.Remote.Name == remoteName).Select (b => b.Name.Substring (b.Name.IndexOf ('/') + 1));
		}

		public string GetCurrentBranch ()
		{
			return RootRepository.Head.Name;
		}

		public void SwitchToBranch (ProgressMonitor monitor, string branch)
		{
			Signature sig = GetSignature ();
			Stash stash;
			int stashIndex = -1;
			if (sig == null)
				return;

			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), GitService.StashUnstashWhenSwitchingBranches ? 4 : 2);

			// Get a list of files that are different in the target branch
			var statusList = GitUtil.GetChangedFiles (RootRepository, branch);

			if (GitService.StashUnstashWhenSwitchingBranches) {
				// Remove the stash for this branch, if exists
				string currentBranch = GetCurrentBranch ();
				stashIndex = GetStashForBranch (RootRepository.Stashes, currentBranch);
				if (stashIndex != -1)
					RootRepository.Stashes.Remove (stashIndex);

				if (!TryCreateStash (monitor, GetStashName (currentBranch), out stash))
					return;
				
				monitor.Step (1);
			}

			try {
				int progress = 0;
				RootRepository.Checkout (branch, new CheckoutOptions {
					OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
					OnCheckoutNotify = RefreshFile,
					CheckoutNotifyFlags = refreshFlags,
				});
			} finally {
				// Restore the branch stash
				if (GitService.StashUnstashWhenSwitchingBranches) {
					stashIndex = GetStashForBranch (RootRepository.Stashes, branch);
					if (stashIndex != -1)
						PopStash (monitor, stashIndex);
					monitor.Step (1);
				}
			}
			// Notify file changes
			NotifyFileChanges (monitor, statusList);

			if (BranchSelectionChanged != null)
				BranchSelectionChanged (this, EventArgs.Empty);

			monitor.EndTask ();
		}

		void NotifyFileChanges (ProgressMonitor monitor, TreeChanges statusList)
		{
			// Files added to source branch not present to target branch.
			var removed = statusList.Where (c => c.Status == ChangeKind.Added).Select (c => GetRepository (c.Path).FromGitPath (c.Path)).ToList ();
			var modified = statusList.Where (c => c.Status != ChangeKind.Added).Select (c => GetRepository (c.Path).FromGitPath (c.Path)).ToList ();

			monitor.BeginTask (GettextCatalog.GetString ("Updating solution"), removed.Count + modified.Count);

			FileService.NotifyFilesChanged (modified, true);
			monitor.Step (modified.Count);

			FileService.NotifyFilesRemoved (removed);
			monitor.Step (removed.Count);

			monitor.EndTask ();
		}

		static string GetStashName (string branchName)
		{
			return "__MD_" + branchName;
		}

		public static string GetStashBranchName (string stashName)
		{
			return stashName.StartsWith ("__MD_", StringComparison.Ordinal) ? stashName.Substring (5) : null;
		}

		static int GetStashForBranch (StashCollection stashes, string branchName)
		{
			string sn = GetStashName (branchName);
			int count = stashes.Count ();
			for (int i = 0; i < count; ++i) {
				if (stashes[i].Message.IndexOf (sn, StringComparison.InvariantCulture) != -1)
					return i;
			}
			return -1;
		}

		public ChangeSet GetPushChangeSet (string remote, string branch)
		{
			ChangeSet cset = CreateChangeSet (RootPath);

			Commit reference = RootRepository.Branches [remote + "/" + branch].Tip;
			Commit compared = RootRepository.Head.Tip;

			foreach (var change in GitUtil.CompareCommits (RootRepository, reference, compared)) {
				VersionStatus status;
				switch (change.Status) {
				case ChangeKind.Added:
				case ChangeKind.Copied:
					status = VersionStatus.ScheduledAdd;
					break;
				case ChangeKind.Deleted:
					status = VersionStatus.ScheduledDelete;
					break;
				case ChangeKind.Renamed:
					status = VersionStatus.ScheduledReplace;
					break;
				default:
					status = VersionStatus.Modified;
					break;
				}
				var vi = new VersionInfo (RootRepository.FromGitPath (change.Path), "", false, status | VersionStatus.Versioned, null, VersionStatus.Versioned, null);
				cset.AddFile (vi);
			}
			return cset;
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			Commit reference = RootRepository.Branches[remote + "/" + branch].Tip;
			Commit compared = RootRepository.Head.Tip;

			var diffs = new List<DiffInfo> ();
			var patch = RootRepository.Diff.Compare<Patch> (reference.Tree, compared.Tree);
			foreach (var change in GitUtil.CompareCommits (RootRepository, reference, compared)) {
				string path;
				switch (change.Status) {
				case ChangeKind.Deleted:
				case ChangeKind.Renamed:
					path = change.OldPath;
					break;
				default:
					path = change.Path;
					break;
				}
				diffs.Add (new DiffInfo (RootPath, RootRepository.FromGitPath (path), patch[path].Patch));
			}
			return diffs.ToArray ();
		}

		protected override void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo vi = GetVersionInfo (localSrcPath, VersionInfoQueryFlags.IgnoreCache);
			if (vi == null || !vi.IsVersioned) {
				base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}

			vi = GetVersionInfo (localDestPath, VersionInfoQueryFlags.IgnoreCache);
			if (vi != null && ((vi.Status & (VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace)) != VersionStatus.Unversioned))
				RootRepository.Unstage (localDestPath);

			RootRepository.Move (localSrcPath, localDestPath);
			ClearCachedVersionInfo (localSrcPath, localDestPath);
		}

		protected override void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo[] versionedFiles = GetDirectoryVersionInfo (localSrcPath, false, true);
			base.OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
			monitor.BeginTask ("Moving files", versionedFiles.Length);
			foreach (VersionInfo vif in versionedFiles) {
				if (vif.IsDirectory)
					continue;
				FilePath newDestPath = vif.LocalPath.ToRelative (localSrcPath).ToAbsolute (localDestPath);
				Add (newDestPath, false, monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		public override Annotation[] GetAnnotations (FilePath repositoryPath)
		{
			var repository = GetRepository (repositoryPath);
			Commit hc = GetHeadCommit (repository);
			if (hc == null)
				return new Annotation [0];

			int lines = File.ReadAllLines (repositoryPath).Length;
			var list = new List<Annotation> (lines);
			var working = new Annotation (GettextCatalog.GetString ("working copy"), "<uncommitted>", DateTime.Now);
			for (int i = 0; i < lines; ++i)
				list.Add (working);

			foreach (var hunk in repository.Blame (repository.ToGitPath (repositoryPath))) {
				var commit = hunk.FinalCommit;
				var author = hunk.FinalSignature;
				working = new Annotation (commit.Sha, author.Name, author.When.LocalDateTime, String.Format ("<{0}>", author.Email));
				for (int i = 0; i < hunk.LineCount; ++i)
					list [hunk.FinalStartLineNumber + i] = working;
			}

			return list.ToArray ();
		}

		protected override void OnIgnore (FilePath[] localPath)
		{
			var ignored = new List<FilePath> ();
			string gitignore = RootPath + Path.DirectorySeparatorChar + ".gitignore";
			string txt;
			if (File.Exists (gitignore)) {
				using (var br = new StreamReader (gitignore)) {
					while ((txt = br.ReadLine ()) != null) {
						ignored.Add (txt);
					}
				}
			}

			var sb = new StringBuilder ();
			foreach (var path in localPath.Except (ignored))
				sb.AppendLine (RootRepository.ToGitPath (path));

			File.AppendAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
			RootRepository.Stage (".gitignore");
		}

		protected override void OnUnignore (FilePath[] localPath)
		{
			var ignored = new List<string> ();
			string gitignore = RootPath + Path.DirectorySeparatorChar + ".gitignore";
			string txt;
			if (File.Exists (gitignore)) {
				using (var br = new StreamReader (RootPath + Path.DirectorySeparatorChar + ".gitignore")) {
					while ((txt = br.ReadLine ()) != null) {
						ignored.Add (txt);
					}
				}
			}

			var sb = new StringBuilder ();
			foreach (var path in ignored.Except (RootRepository.ToGitPath (localPath)))
				sb.AppendLine (path);

			File.WriteAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
			RootRepository.Stage (".gitignore");
		}
	}

	public class GitRevision: Revision
	{
		readonly string rev;
		internal Commit Commit { get; set; }
		internal FilePath FileForChanges { get; set; }

		public LibGit2Sharp.Repository GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, LibGit2Sharp.Repository gitRepository, Commit commit) : base(repo)
		{
			GitRepository = gitRepository;
			Commit = commit;
			rev = Commit != null ? Commit.Id.Sha : "";
		}

		public GitRevision (Repository repo, LibGit2Sharp.Repository gitRepository, Commit commit, DateTime time, string author, string message) : base(repo, time, author, message)
		{
			GitRepository = gitRepository;
			Commit = commit;
			rev = Commit != null ? Commit.Id.Sha : "";
		}

		public override string ToString ()
		{
			return rev;
		}

		public override string ShortName {
			get { return rev.Length > 10 ? rev.Substring (0, 10) : rev; }
		}

		public override Revision GetPrevious ()
		{
			var id = Commit.Parents.FirstOrDefault ();
			return id == null ? null : new GitRevision (Repository, GitRepository, id);
		}
	}
}
