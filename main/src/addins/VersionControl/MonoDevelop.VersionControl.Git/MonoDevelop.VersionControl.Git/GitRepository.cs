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
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

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
		LibGit2Sharp.Repository rootRepository;
		internal LibGit2Sharp.Repository RootRepository {
			get { return rootRepository; }
			private set {
				if (rootRepository == value)
					return;

				ShutdownFileWatcher ();
				ShutdownScheduler ();

				if (rootRepository != null)
					rootRepository.Dispose ();

				rootRepository = value;

				InitScheduler ();
				InitFileWatcher (false);
			}
		}

		public static event EventHandler BranchSelectionChanged;


		FileSystemWatcher watcher;
		ConcurrentExclusiveSchedulerPair scheduler;
		TaskFactory blockingOperationFactory;
		TaskFactory readingOperationFactory;

		public GitRepository ()
		{
			Url = "git://";
		}

		public GitRepository (VersionControlSystem vcs, FilePath path, string url) : base (vcs)
		{
			RootRepository = new LibGit2Sharp.Repository (path);
			RootPath = RootRepository.Info.WorkingDirectory;
			Url = url;

			InitFileWatcher ();
		}

		void InitScheduler ()
		{
			if (scheduler == null) {
				scheduler = new ConcurrentExclusiveSchedulerPair ();
				blockingOperationFactory = new TaskFactory (scheduler.ExclusiveScheduler);
				readingOperationFactory = new TaskFactory (scheduler.ConcurrentScheduler);
			}
		}

		void ShutdownScheduler ()
		{
			if (scheduler != null) {
				scheduler.Complete ();
				scheduler.Completion.Wait ();
				scheduler = null;
				readingOperationFactory = null;
				blockingOperationFactory = null;
			}
		}

		void InitFileWatcher (bool throwIfIndexMissing = true)
		{
			if (RootRepository == null)
				throw new InvalidOperationException ($"{nameof (RootRepository)} not initialized, FileSystemWantcher can not be initialized");
			if (throwIfIndexMissing && RootPath.IsNullOrEmpty)
				throw new InvalidOperationException ($"{nameof (RootPath)} not set, FileSystemWantcher can not be initialized");
			FilePath dotGitPath = RootRepository.Info.Path;
			if (!dotGitPath.IsDirectory || !Directory.Exists (dotGitPath)) {
				if (!throwIfIndexMissing)
					return;
				throw new InvalidOperationException ($"{nameof (RootPath)} is not a valid Git repository, FileSystemWantcher can not be initialized");
			}

			if (watcher?.Path == dotGitPath.CanonicalPath.ParentDirectory)
				return;

			ShutdownFileWatcher ();

			watcher = new FileSystemWatcher (dotGitPath.CanonicalPath.ParentDirectory, Path.Combine (dotGitPath.FileName, "*.lock"));
			watcher.Created += HandleGitLockCreated;
			watcher.Deleted += HandleGitLockDeleted;
			watcher.Renamed += HandleGitLockRenamed;
			watcher.EnableRaisingEvents = true;
		}

		void ShutdownFileWatcher ()
		{
			if (watcher != null) {
				watcher.EnableRaisingEvents = false;
				watcher.Dispose ();
				watcher = null;
			}
		}

		void HandleGitLockCreated (object sender, FileSystemEventArgs e)
		{
			OnGitLocked ();
		}

		void HandleGitLockRenamed (object sender, RenamedEventArgs e)
		{
			if (e.OldName.EndsWith (".lock", StringComparison.Ordinal) && !e.Name.EndsWith (".lock", StringComparison.Ordinal))
				OnGitUnlocked ();
		}

		void HandleGitLockDeleted (object sender, FileSystemEventArgs e)
		{
			OnGitUnlocked ();
		}

		void OnGitLocked ()
		{
			// TODO: freeze all git operations
			FileService.FreezeEvents ();
		}

		void OnGitUnlocked ()
		{
			FileService.ThawEvents ();
		}
		
		protected override void Dispose (bool disposing)
		{
			// ensure that no new operations can be started while we wait for the scheduler to shutdown
			base.IsDisposed = true;

			if (disposing) {
				ShutdownFileWatcher ();
				ShutdownScheduler ();
			}

			// now it's safe to dispose the base and release all information caches
			base.Dispose (disposing);

			if (disposing) {
				rootRepository?.Dispose ();
				if (cachedSubmodules != null)
					foreach (var rep in cachedSubmodules)
						rep.Item2.Dispose ();
			}

			scheduler = null;
			readingOperationFactory = null;
			blockingOperationFactory = null;
			watcher = null;
			rootRepository = null;
			cachedSubmodules = null;
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
			return RunOperation (localFile, repository => {
				Commit c = GetHeadCommit (repository);
				return c == null ? string.Empty : GetCommitTextContent (c, localFile, repository);
			});
		}

		static Commit GetHeadCommit (LibGit2Sharp.Repository repository)
		{
			return repository.Head.Tip;
		}

		public StashCollection GetStashes ()
		{
			return RunOperation (() => RootRepository.Stashes);
		}

		const CheckoutNotifyFlags refreshFlags = CheckoutNotifyFlags.Updated | CheckoutNotifyFlags.Conflict | CheckoutNotifyFlags.Untracked | CheckoutNotifyFlags.Dirty;
		bool RefreshFile (string path, CheckoutNotifyFlags flags)
		{
			return RefreshFile (RootRepository, path, flags);
		}

		bool RefreshFile (LibGit2Sharp.Repository repository, string path, CheckoutNotifyFlags flags)
		{
			FilePath fp = repository.FromGitPath (path);
			Gtk.Application.Invoke ((o, args) => {
				if (IdeApp.IsInitialized) {
					MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.GetDocument (fp);
					if (doc != null)
						doc.Reload ();
				}
				VersionControlService.NotifyFileStatusChanged (new FileUpdateEventArgs (this, fp, false));
			});
			return true;
		}

		const int progressThrottle = 200;
		static System.Diagnostics.Stopwatch throttleWatch = new System.Diagnostics.Stopwatch ();
		static bool OnTransferProgress (TransferProgress tp, ProgressMonitor monitor, ref int progress)
		{
			if (progress == 0 && tp.ReceivedObjects == 0) {
				progress = 1;
				monitor.Log.WriteLine (GettextCatalog.GetString ("Receiving and indexing objects"), 2 * tp.TotalObjects);
				throttleWatch.Restart ();
			}

			int currentProgress = tp.ReceivedObjects + tp.IndexedObjects;
			int steps = currentProgress - progress;
			if (throttleWatch.ElapsedMilliseconds > progressThrottle) {
				monitor.Step (steps);
				throttleWatch.Restart ();
				progress = currentProgress;
			}

			if (tp.IndexedObjects >= tp.TotalObjects) {
				throttleWatch.Stop ();
			}

			return !monitor.CancellationToken.IsCancellationRequested;
		}

		static void OnCheckoutProgress (int completedSteps, int totalSteps, ProgressMonitor monitor, ref int progress)
		{
			if (progress == 0 && completedSteps == 0) {
				progress = 1;
				monitor.Log.WriteLine (GettextCatalog.GetString ("Checking out files"), 2 * totalSteps);
				throttleWatch.Restart ();
			}

			int steps = completedSteps - progress;
			if (throttleWatch.ElapsedMilliseconds > progressThrottle) {
				monitor.Step (steps);
				throttleWatch.Restart ();
				progress = completedSteps;
			}

			if (completedSteps >= totalSteps) {
				throttleWatch.Stop ();
			}
		}

		public StashApplyStatus ApplyStash (ProgressMonitor monitor, int stashIndex)
		{
			return RunBlockingOperation (() => ApplyStash (RootRepository, monitor, stashIndex), true);
		}

		StashApplyStatus ApplyStash (LibGit2Sharp.Repository repository, ProgressMonitor monitor, int stashIndex)
		{
			if (monitor != null)
				monitor.BeginTask (GettextCatalog.GetString ("Applying stash"), 1);

			int progress = 0;
			StashApplyStatus res = repository.Stashes.Apply (stashIndex, new StashApplyOptions {
				CheckoutOptions = new CheckoutOptions {
					OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
					OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (repository, path, flags),
					CheckoutNotifyFlags = refreshFlags,
				},
			});

			if (monitor != null)
				monitor.EndTask ();

			return res;
		}

		public StashApplyStatus PopStash (ProgressMonitor monitor, int stashIndex)
		{
			if (monitor != null)
				monitor.BeginTask (GettextCatalog.GetString ("Popping stash"), 1);

			var res = RunBlockingOperation (() => {
				var stash = RootRepository.Stashes [stashIndex];
				int progress = 0;
				return RootRepository.Stashes.Pop (stashIndex, new StashApplyOptions {
					CheckoutOptions = new CheckoutOptions {
						OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
						OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
						CheckoutNotifyFlags = refreshFlags,
					},
				});
			}, true);

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
				monitor.BeginTask (GettextCatalog.GetString ("Stashing changes"), 1);

			stash = RunBlockingOperation (() => RootRepository.Stashes.Add (sig, message, StashModifiers.Default | StashModifiers.IncludeUntracked));

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
		Tuple<FilePath, LibGit2Sharp.Repository> [] cachedSubmodules = new Tuple<FilePath, LibGit2Sharp.Repository> [0];
		Tuple<FilePath, LibGit2Sharp.Repository> [] GetCachedSubmodules ()
		{
			var submoduleWriteTime = File.GetLastWriteTimeUtc (RootPath.Combine (".gitmodules"));
			if (cachedSubmoduleTime != submoduleWriteTime) {
				cachedSubmoduleTime = submoduleWriteTime;
				cachedSubmodules = RootRepository.Submodules.Select (s => {
						var fp = new FilePath (Path.Combine (RootRepository.Info.WorkingDirectory, s.Path.Replace ('/', Path.DirectorySeparatorChar))).CanonicalPath;
						return new Tuple<FilePath, LibGit2Sharp.Repository> (fp, new LibGit2Sharp.Repository (fp));
					}).ToArray ();
			}
			return cachedSubmodules;
		}

		void EnsureBackgroundThread ()
		{
			if (Runtime.IsMainThread)
				throw new InvalidOperationException ("Deadlock prevention: this shall not run on the UI thread");
		}

		void EnsureInitialized ()
		{
			if (IsDisposed)
				throw new ObjectDisposedException (typeof(GitRepository).Name);
			InitScheduler ();
			if (RootRepository != null)
				InitFileWatcher ();
		}

		internal void RunSafeOperation (Action action)
		{
			EnsureInitialized ();
			action ();
		}

		internal T RunSafeOperation<T> (Func<T> action)
		{
			EnsureInitialized ();
			return action ();
		}

		internal void RunOperation (FilePath localPath, Action<LibGit2Sharp.Repository> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			readingOperationFactory.StartNew (() => action (GetRepository (localPath))).RunWaitAndCapture ();
		}

		internal void RunOperation (Action action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			readingOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal T RunOperation<T> (Func<T> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			return readingOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal T RunOperation<T> (FilePath localPath, Func<LibGit2Sharp.Repository, T> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			return readingOperationFactory.StartNew (() => action (GetRepository (localPath))).RunWaitAndCapture ();
		}

		internal void RunBlockingOperation (Action action, bool hasUICallbacks = false)
		{
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			blockingOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal void RunBlockingOperation (FilePath localPath, Action<LibGit2Sharp.Repository> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			blockingOperationFactory.StartNew (() => action (GetRepository (localPath))).RunWaitAndCapture ();
		}

		internal T RunBlockingOperation<T> (Func<T> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			return blockingOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal T RunBlockingOperation<T> (FilePath localPath, Func<LibGit2Sharp.Repository, T> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			return blockingOperationFactory.StartNew (() => action (GetRepository (localPath))).RunWaitAndCapture ();
		}

		LibGit2Sharp.Repository GetRepository (FilePath localPath)
		{
			return GroupByRepository (new [] { localPath }).First ().Key;
		}

		FilePath GetRepositoryRoot (FilePath localPath)
		{
			return GroupByRepositoryRoot (new [] { localPath }).First ().Key;
		}

		IEnumerable<IGrouping<FilePath, FilePath>> GroupByRepositoryRoot (IEnumerable<FilePath> files)
		{
			var cache = GetCachedSubmodules ();
			return files.GroupBy (f => {
				var res = cache.FirstOrDefault (s => f.IsChildPathOf (s.Item1) || f.FullPath == s.Item1);
				return res != null ? res.Item1 : RootPath;
			});
		}

		IEnumerable<IGrouping<LibGit2Sharp.Repository, FilePath>> GroupByRepository (IEnumerable<FilePath> files)
		{
			var cache = GetCachedSubmodules ();
			return files.GroupBy (f => {
				var res = cache.FirstOrDefault (s => f.IsChildPathOf (s.Item1) || f.FullPath == s.Item1);
				return res != null ? res.Item2 : RootRepository;
			});
		}

		protected override Revision[] OnGetHistory (FilePath localFile, Revision since)
		{
			return RunOperation (() => {
				var hc = GetHeadCommit (RootRepository);
				if (hc == null)
					return new GitRevision [0];

				var sinceRev = since != null ? ((GitRevision)since).GetCommit (RootRepository) : null;
				IEnumerable<Commit> commits = RootRepository.Commits.QueryBy (new CommitFilter { SortBy = CommitSortStrategies.Topological });
				if (localFile.CanonicalPath != RootPath.CanonicalPath.ResolveLinks ()) {
					var localPath = RootRepository.ToGitPath (localFile);
					commits = commits.Where (c => {
						int count = c.Parents.Count ();
						if (count > 1)
							return false;

						var localTreeEntry = c.Tree [localPath];
						if (localTreeEntry == null)
							return false;

						if (count == 0)
							return true;

						var parentTreeEntry = c.Parents.Single ().Tree [localPath];
						return parentTreeEntry == null || localTreeEntry.Target.Id != parentTreeEntry.Target.Id;
					});
				}

				return commits.TakeWhile (c => c != sinceRev).Select (commit => {
					var author = commit.Author;
					var shortMessage = commit.MessageShort;
					if (shortMessage.Length > 50) {
						shortMessage = shortMessage.Substring (0, 50) + "…";
					}

					var rev = new GitRevision (this, RootRepository.Info.WorkingDirectory, commit, author.When.LocalDateTime, author.Name, commit.Message) {
						Email = author.Email,
						ShortMessage = shortMessage,
						FileForChanges = localFile,
					};
					return rev;
				}).ToArray ();
			});
		}

		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			var rev = (GitRevision) revision;
			return RunOperation (() => {
				var commit = rev.GetCommit (RootRepository);
				if (commit == null)
					return new RevisionPath [0];

				var paths = new List<RevisionPath> ();
				var parent = commit.Parents.FirstOrDefault ();
				var changes = RootRepository.Diff.Compare<TreeChanges> (parent != null ? parent.Tree : null, commit.Tree);

				foreach (var entry in changes.Added)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
				foreach (var entry in changes.Copied)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
				foreach (var entry in changes.Deleted)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.OldPath), RevisionAction.Delete, null));
				foreach (var entry in changes.Renamed)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RootRepository.FromGitPath (entry.OldPath), RevisionAction.Replace, null));
				foreach (var entry in changes.Modified)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
				foreach (var entry in changes.TypeChanged)
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
				return paths.ToArray ();
			});
		}

		protected override IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			try {
				return GetDirectoryVersionInfo (FilePath.Null, paths, getRemoteStatus, false);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to query git status", e);
				return paths.Select (x => VersionInfo.CreateUnversioned (x, false));
			}
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			try {
				return GetDirectoryVersionInfo (localDirectory, null, getRemoteStatus, recursive);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to get git directory status", e);
				return new VersionInfo [0];
			}
		}

		class RepositoryContainer : IDisposable
		{
			Dictionary<FilePath, LibGit2Sharp.Repository> repositories = new Dictionary<FilePath, LibGit2Sharp.Repository> ();

			public LibGit2Sharp.Repository GetRepository (FilePath root)
			{
				if (!repositories.TryGetValue (root, out var repo) || repo == null) {
					repo = repositories [root] = new LibGit2Sharp.Repository (root);
				}
				return repo;
			}

			bool disposed;
			public void Dispose ()
			{
				if (disposed)
					return;
				foreach (var repo in repositories)
					repo.Value.Dispose ();
				repositories.Clear ();
				repositories = null;
				disposed = true;
			}
		}

		// Used for checking if we will dupe data.
		// This way we reduce the number of GitRevisions created and RevWalks done.
		Dictionary<FilePath, GitRevision> versionInfoCacheRevision = new Dictionary<FilePath, GitRevision> ();
		Dictionary<FilePath, GitRevision> versionInfoCacheEmptyRevision = new Dictionary<FilePath, GitRevision> ();
		VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive)
		{
			var versions = new List<VersionInfo> ();

			return readingOperationFactory.StartNew (() => {
				if (localFileNames != null) {
					var localFiles = new List<FilePath> ();
					var groups = GroupByRepository (localFileNames);
					foreach (var group in groups) {
						var repositoryRoot = group.Key.Info.WorkingDirectory;
						GitRevision arev;
						if (!versionInfoCacheEmptyRevision.TryGetValue (repositoryRoot, out arev)) {
							arev = new GitRevision (this, repositoryRoot, null);
							versionInfoCacheEmptyRevision.Add (repositoryRoot, arev);
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
						foreach (var group in groups) {
							var repository = group.Key;
							var repositoryRoot = repository.Info.WorkingDirectory;

							GitRevision rev = null;
							Commit headCommit = GetHeadCommit (repository);
							if (headCommit != null) {
								if (!versionInfoCacheRevision.TryGetValue (repositoryRoot, out rev)) {
									rev = new GitRevision (this, repositoryRoot, headCommit);
									versionInfoCacheRevision.Add (repositoryRoot, rev);
								} else if (rev.GetCommit (repository) != headCommit) {
									rev = new GitRevision (this, repositoryRoot, headCommit);
									versionInfoCacheRevision [repositoryRoot] = rev;
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
					foreach (var group in GroupByRepositoryRoot (directories)) {
						if (!versionInfoCacheEmptyRevision.TryGetValue (group.Key, out arev)) {
							arev = new GitRevision (this, group.Key, null);
							versionInfoCacheEmptyRevision.Add (group.Key, arev);
						}
						foreach (var p in group)
							versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
					}

					var rootRepository = GetRepository (RootPath);
					Commit headCommit = GetHeadCommit (rootRepository);
					if (headCommit != null) {
						if (!versionInfoCacheRevision.TryGetValue (RootPath, out arev)) {
							arev = new GitRevision (this, RootPath, headCommit);
							versionInfoCacheRevision.Add (RootPath, arev);
						} else if (arev.GetCommit (rootRepository) != headCommit) {
							arev = new GitRevision (this, RootPath, headCommit);
							versionInfoCacheRevision [RootPath] = arev;
						}
					}

					GetDirectoryVersionInfoCore (rootRepository, arev, localDirectory.CanonicalPath, versions, recursive);
				}

				return versions.ToArray ();
			}).Result;
		}

		static void GetFilesVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, List<FilePath> localPaths, List<VersionInfo> versions)
		{
			foreach (var localPath in localPaths) {
				if (!localPath.IsDirectory) {
					var file = repo.ToGitPath (localPath);
					var status = repo.RetrieveStatus (file);
					AddStatus (repo, rev, file, versions, status, null);
				}
			}
		}

		static void AddStatus (LibGit2Sharp.Repository repo, GitRevision rev, string file, List<VersionInfo> versions, FileStatus status, string directoryPath)
		{
			VersionStatus fstatus = VersionStatus.Versioned;

			if (status != FileStatus.Unaltered) {
				if ((status & FileStatus.NewInIndex) != 0)
					fstatus |= VersionStatus.ScheduledAdd;
				else if ((status & (FileStatus.DeletedFromIndex | FileStatus.DeletedFromWorkdir)) != 0)
					fstatus |= VersionStatus.ScheduledDelete;
				else if ((status & (FileStatus.TypeChangeInWorkdir | FileStatus.ModifiedInWorkdir)) != 0)
					fstatus |= VersionStatus.Modified;
				else if ((status & (FileStatus.RenamedInIndex | FileStatus.RenamedInWorkdir)) != 0)
					fstatus |= VersionStatus.ScheduledReplace;
				else if ((status & (FileStatus.Nonexistent | FileStatus.NewInWorkdir)) != 0)
					fstatus = VersionStatus.Unversioned;
				else if ((status & FileStatus.Ignored) != 0)
					fstatus = VersionStatus.Ignored;
			}

			if (repo.Index.Conflicts [file] != null)
				fstatus = VersionStatus.Versioned | VersionStatus.Conflicted;

			var versionPath = repo.FromGitPath (file);
			if (directoryPath != null && versionPath.ParentDirectory != directoryPath) {
				return;
			}

			versions.Add (new VersionInfo (versionPath, "", false, fstatus, rev, fstatus == VersionStatus.Ignored ? VersionStatus.Unversioned : VersionStatus.Versioned, null));
		}

		static void GetDirectoryVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, FilePath directory, List<VersionInfo> versions, bool recursive)
		{
			var relativePath = repo.ToGitPath (directory);
			var status = repo.RetrieveStatus (new StatusOptions {
				DisablePathSpecMatch = true,
				PathSpec = relativePath != "." ? new [] { relativePath } : new string[0],
				IncludeUnaltered = true,
			});

			foreach (var statusEntry in status) {
				AddStatus (repo, rev, statusEntry.FilePath, versions, statusEntry.State, recursive ? null : directory);
			}
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
			RootPath = localPath;
			RootRepository = new LibGit2Sharp.Repository (LibGit2Sharp.Repository.Init (localPath));
			RootRepository.Network.Remotes.Add ("origin", Url);

			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			foreach (FilePath fp in files) {
				LibGit2Sharp.Commands.Stage (RootRepository, RootRepository.ToGitPath (fp));
				cs.AddFile (fp);
			}

			// Create the initial commit
			cs.GlobalComment = message;
			Commit (cs, monitor);

			RootRepository.Branches.Update (RootRepository.Branches ["master"], branch => branch.TrackedBranch = "refs/remotes/origin/master");

			RetryUntilSuccess (monitor, credType => {

				try {
					RootRepository.Network.Push (RootRepository.Head, new PushOptions {
						OnPushStatusError = delegate (PushStatusError e) {
							throw new VersionControlException (e.Message);
						},
						CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType)
					});
				} catch(VersionControlException vcex) {
					RootRepository.Dispose ();
					RootRepository = null;
					if (RootPath.Combine (".git").IsDirectory)
						Directory.Delete (RootPath.Combine (".git"), true);
					LoggingService.LogError ("Failed to publish to the repository", vcex);
					throw;
				}
			});

			return this;
		}

		protected override void OnUpdate (FilePath [] localPaths, bool recurse, ProgressMonitor monitor)
		{
			// TODO: Make it work differently for submodules.
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);

			if (RootRepository.Head.IsTracking) {
				Fetch (monitor, RootRepository.Head.RemoteName);

				GitUpdateOptions options = GitService.StashUnstashWhenUpdating ? GitUpdateOptions.NormalUpdate : GitUpdateOptions.UpdateSubmodules;
				if (GitService.UseRebaseOptionWhenPulling)
					Rebase (RootRepository.Head.TrackedBranch.FriendlyName, options, monitor);
				else
					Merge (RootRepository.Head.TrackedBranch.FriendlyName, options, monitor);

				monitor.Step (1);
			}

			monitor.EndTask ();
		}

		static bool HandleAuthenticationException (AuthenticationException e)
		{
			var ret = MessageService.AskQuestion (
								GettextCatalog.GetString ("Remote server error: {0}", e.Message),
								GettextCatalog.GetString ("Retry authentication?"),
								AlertButton.Yes, AlertButton.No);
			return ret == AlertButton.Yes;
		}

		static void RetryUntilSuccess (ProgressMonitor monitor, Action<GitCredentialsType> func, Action onRetry = null)
		{
			bool retry;
			using (var tfsSession = new TfsSmartSession ()) {
				do {
					var credType = tfsSession.Disposed ? GitCredentialsType.Normal : GitCredentialsType.Tfs;
					try {
						func (credType);
						GitCredentials.StoreCredentials (credType);
						retry = false;
					} catch (AuthenticationException e) {
						GitCredentials.InvalidateCredentials (credType);
						retry = Runtime.RunInMainThread (() => HandleAuthenticationException (e)).Result;
						if (!retry)
							monitor?.ReportError (e.Message, null);
					} catch (VersionControlException e) {
						GitCredentials.InvalidateCredentials (credType);
						monitor?.ReportError (e.Message, null);
						retry = false;
					} catch (UserCancelledException e) {
						GitCredentials.StoreCredentials (credType);
						retry = false;
						throw new VersionControlException (e.Message, e);
					} catch (LibGit2SharpException e) {
						GitCredentials.InvalidateCredentials (credType);

						if (e.Message == GettextCatalog.GetString (GitCredentials.UserCancelledExceptionMessage))
							throw new VersionControlException (e.Message, e);

						if (credType == GitCredentialsType.Tfs) {
							retry = true;
							tfsSession.Dispose ();
							onRetry?.Invoke ();
							continue;
						}

						string message;
						// TODO: Remove me once https://github.com/libgit2/libgit2/pull/3137 goes in.
						if (string.Equals (e.Message, "early EOF", StringComparison.OrdinalIgnoreCase))
							message = GettextCatalog.GetString ("Unable to authorize credentials for the repository.");
						else if (e.Message.StartsWith ("Invalid Content-Type", StringComparison.OrdinalIgnoreCase))
							message = GettextCatalog.GetString ("Not a valid git repository.");
						else if (string.Equals (e.Message, "Received unexpected content-type", StringComparison.OrdinalIgnoreCase))
							message = GettextCatalog.GetString ("Not a valid git repository.");
						else
							message = e.Message;

						throw new VersionControlException (message, e);
					}
				} while (retry);
			}
		}

		public void Fetch (ProgressMonitor monitor, string remote)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Fetching"), 1);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
			int progress = 0;

			RunOperation (() => {
				var refSpec = RootRepository.Network.Remotes [remote]?.FetchRefSpecs.Select (spec => spec.Specification);
				RetryUntilSuccess (monitor, credType => LibGit2Sharp.Commands.Fetch (RootRepository, remote, refSpec, new FetchOptions {
					CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType),
					OnTransferProgress = tp => OnTransferProgress (tp, monitor, ref progress),
				}, string.Empty));
			}, true);
			monitor.Step (1);
			monitor.EndTask ();
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
						    new AlertButton (GettextCatalog.GetString ("Stash"))) == AlertButton.Cancel)
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

		bool ConflictResolver(LibGit2Sharp.Repository repository, ProgressMonitor monitor, Commit resetToIfFail, string message)
		{
			foreach (var conflictFile in repository.Index.Conflicts) {
				ConflictResult res = ResolveConflict (repository.FromGitPath (conflictFile.Ancestor.Path));
				if (res == ConflictResult.Abort) {
					repository.Reset (ResetMode.Hard, resetToIfFail);
					return false;
				}
				if (res == ConflictResult.Skip) {
					Revert (repository.FromGitPath (conflictFile.Ancestor.Path), false, monitor);
					break;
				}
				if (res == Git.ConflictResult.Continue) {
					Add (repository.FromGitPath (conflictFile.Ancestor.Path), false, monitor);
				}
			}
			if (!string.IsNullOrEmpty (message)) {
				var sig = GetSignature ();
				repository.Commit (message, sig, sig);
			}
			return true;
		}

		void CommonPostMergeRebase(int stashIndex, GitUpdateOptions options, ProgressMonitor monitor, Commit oldHead)
		{
			if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
				monitor.Step (1);

				// Restore local changes
				if (stashIndex != -1) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Restoring local changes"));
					ApplyStash (monitor, stashIndex);
					// FIXME: No StashApplyStatus.Conflicts here.
					if (RootRepository.Index.Conflicts.Any () && !ConflictResolver (RootRepository, monitor, oldHead, string.Empty))
						PopStash (monitor, stashIndex);
					else
						RunBlockingOperation (() => RootRepository.Stashes.Remove (stashIndex));
					monitor.Step (1);
				}
			}
			monitor.EndTask ();
		}

		public void Rebase (string branch, GitUpdateOptions options, ProgressMonitor monitor)
		{
			int stashIndex = -1;
			var oldHead = RootRepository.Head.Tip;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 5);
				if (!CommonPreMergeRebase (options, monitor, out stashIndex))
					return;

				RunBlockingOperation (() => {

					// Do a rebase.
					var divergence = RootRepository.ObjectDatabase.CalculateHistoryDivergence (RootRepository.Head.Tip, RootRepository.Branches [branch].Tip);
					var toApply = RootRepository.Commits.QueryBy (new CommitFilter {
						IncludeReachableFrom = RootRepository.Head.Tip,
						ExcludeReachableFrom = divergence.CommonAncestor,
						SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
					}).ToArray ();

					RootRepository.Reset (ResetMode.Hard, divergence.Another);

					int count = toApply.Length;
					int i = 1;
					foreach (var com in toApply) {
						monitor.Log.WriteLine (GettextCatalog.GetString ("Cherry-picking {0} - {1}/{2}", com.Id, i, count));
						CherryPickResult cherryRes = RootRepository.CherryPick (com, com.Author, new CherryPickOptions {
							CheckoutNotifyFlags = refreshFlags,
							OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
						});
						if (cherryRes.Status == CherryPickStatus.Conflicts)
							ConflictResolver (RootRepository, monitor, toApply.Last (), RootRepository.Info.Message ?? com.Message);
						++i;
					}
				}, true);
			} finally {
				CommonPostMergeRebase (stashIndex, options, monitor, oldHead);
			}
		}

		public void Merge (string branch, GitUpdateOptions options, ProgressMonitor monitor, FastForwardStrategy strategy = FastForwardStrategy.Default)
		{
			int stashIndex = -1;

			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var oldHead = RootRepository.Head.Tip;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				CommonPreMergeRebase (options, monitor, out stashIndex);
				// Do a merge.
				MergeResult mergeResult = RunBlockingOperation (() =>
					RootRepository.Merge (branch, sig, new MergeOptions {
						CheckoutNotifyFlags = refreshFlags,
						OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
					}), true);
				if (mergeResult.Status == MergeStatus.Conflicts)
						ConflictResolver (RootRepository, monitor, RootRepository.Head.Tip, RootRepository.Info.Message);
			} finally {
				CommonPostMergeRebase (stashIndex, GitUpdateOptions.SaveLocalChanges, monitor, oldHead);
			}
		}

		static ConflictResult ResolveConflict (string file)
		{
			ConflictResult res = ConflictResult.Abort;
			Runtime.RunInMainThread (delegate {
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
					dlg.Dispose ();
				}
			}).Wait ();
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
			RunBlockingOperation (() => {
				LibGit2Sharp.Commands.Stage (RootRepository, changeSet.Items.Select (i => i.LocalPath).ToPathStrings ());

				if (changeSet.ExtendedProperties.Contains ("Git.AuthorName"))
					RootRepository.Commit (message, new Signature (
						(string)changeSet.ExtendedProperties ["Git.AuthorName"],
						(string)changeSet.ExtendedProperties ["Git.AuthorEmail"],
						DateTimeOffset.Now), sig);
				else
					RootRepository.Commit (message, sig, sig);
			});
		}

		public bool IsUserInfoDefault ()
		{
			string name = null;
			string email = null;
			try {
				RunOperation (() => {
					name = RootRepository.Config.Get<string> ("user.name").Value;
					email = RootRepository.Config.Get<string> ("user.email").Value;
				});
			} catch {
				name = email = null;
			}
			return name == null && email == null;
		}

		public void GetUserInfo (out string name, out string email)
		{
			try {
				string lname = null, lemail = null;
				RunOperation (() => {
					lname = RootRepository.Config.Get<string> ("user.name").Value;
					lemail = RootRepository.Config.Get<string> ("user.email").Value;
				});
				name = lname;
				email = lemail;
			} catch {
				string dlgName = null, dlgEmail = null;

				Runtime.RunInMainThread (() => {
					var dlg = new UserGitConfigDialog ();
					try {
						if ((Gtk.ResponseType)MessageService.RunCustomDialog (dlg) == Gtk.ResponseType.Ok) {
							dlgName = dlg.UserText;
							dlgEmail = dlg.EmailText;
							SetUserInfo (dlgName, dlgEmail);
						}
					} finally {
						dlg.Destroy ();
						dlg.Dispose ();
					}
				}).Wait ();

				name = dlgName;
				email = dlgEmail;
			}
		}

		public void SetUserInfo (string name, string email)
		{
			RunOperation (() => {
				RootRepository.Config.Set ("user.name", name);
				RootRepository.Config.Set ("user.email", email);
			});
		}

		protected override void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			int transferProgress = 0;
			int checkoutProgress = 0;

			try {
				monitor.BeginTask ("Cloning...", 2);

				RunOperation (() => RetryUntilSuccess (monitor, credType => {
					RootPath = LibGit2Sharp.Repository.Clone (Url, targetLocalPath, new CloneOptions {
						CredentialsProvider = (url, userFromUrl, types) => {
							transferProgress = checkoutProgress = 0;
							return GitCredentials.TryGet (url, userFromUrl, types, credType);
						},
						RepositoryOperationStarting = ctx => {
							Runtime.RunInMainThread (() => {
								monitor.Log.WriteLine ("Checking out repository at '{0}'", ctx.RepositoryPath);
							});
							return true;
						},
						OnTransferProgress = (tp) => OnTransferProgress (tp, monitor, ref transferProgress),
						OnCheckoutProgress = (path, completedSteps, totalSteps) => {
							OnCheckoutProgress (completedSteps, totalSteps, monitor, ref checkoutProgress);
							Runtime.RunInMainThread (() => {
								monitor.Log.WriteLine ("Checking out file '{0}'", path);
							});
						},
					});
				}), true);

				if (monitor.CancellationToken.IsCancellationRequested || RootPath.IsNull)
					return;

				monitor.Step (1);

				RootPath = RootPath.CanonicalPath.ParentDirectory;
				RootRepository = new LibGit2Sharp.Repository (RootPath);
				InitFileWatcher ();

				RunOperation (() => RecursivelyCloneSubmodules (RootRepository, monitor), true);
			} finally {
				monitor.EndTask ();
			}
		}

		static void RecursivelyCloneSubmodules (LibGit2Sharp.Repository rootRepository, ProgressMonitor monitor)
		{
			var submodules = new List<string> ();
			RetryUntilSuccess (monitor, credType => {
				int transferProgress = 0, checkoutProgress = 0;
				SubmoduleUpdateOptions updateOptions = new SubmoduleUpdateOptions () {
					Init = true,
					CredentialsProvider = (url, userFromUrl, types) => {
						transferProgress = checkoutProgress = 0;
						return GitCredentials.TryGet (url, userFromUrl, types, credType);
					},
					OnTransferProgress = (tp) => OnTransferProgress (tp, monitor, ref transferProgress),
					OnCheckoutProgress = (file, completedSteps, totalSteps) => {
						OnCheckoutProgress (completedSteps, totalSteps, monitor, ref checkoutProgress);
						Runtime.RunInMainThread (() => {
							monitor.Log.WriteLine ("Checking out file '{0}'", file);
						});
					},
				};

				// Iterate through the submodules (where the submodule is in the index),
				// and clone them.
				var submoduleArray = rootRepository.Submodules.Where (sm => sm.RetrieveStatus ().HasFlag (SubmoduleStatus.InIndex)).ToArray ();
				monitor.BeginTask (submoduleArray.Length);
				foreach (var sm in submoduleArray) {
					if (monitor.CancellationToken.IsCancellationRequested) {
						throw new UserCancelledException ("Recursive clone of submodules was cancelled.");
					}

					Runtime.RunInMainThread (() => {
						monitor.Log.WriteLine ("Checking out submodule at '{0}'", sm.Path);
					});
					rootRepository.Submodules.Update (sm.Name, updateOptions);
					monitor.Step (1);

					submodules.Add (Path.Combine (rootRepository.Info.WorkingDirectory, sm.Path));
				}
				monitor.EndTask ();
			});

			// If we are continuing the recursive operation, then
			// recurse into nested submodules.
			// Check submodules to see if they have their own submodules.
			foreach (string path in submodules) {
				using (var submodule = new LibGit2Sharp.Repository (path))
					RecursivelyCloneSubmodules (submodule, monitor);
			}
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepositoryRoot (localPaths)) {
				var toCheckout = new HashSet<FilePath> ();
				var toUnstage = new HashSet<FilePath> ();

				foreach (var item in group)
					if (item.IsDirectory) {
						foreach (var vi in GetDirectoryVersionInfo (item, false, recurse))
							if (!vi.IsDirectory) {
								if (vi.Status == VersionStatus.Unversioned)
									continue;

								if ((vi.Status & VersionStatus.ScheduledAdd) == VersionStatus.ScheduledAdd)
									toUnstage.Add (vi.LocalPath);
								else
									toCheckout.Add (vi.LocalPath);
							}
					} else {
						var vi = GetVersionInfo (item);
						if (vi.Status == VersionStatus.Unversioned)
							continue;

						if ((vi.Status & VersionStatus.ScheduledAdd) == VersionStatus.ScheduledAdd)
							toUnstage.Add (vi.LocalPath);
						else
							toCheckout.Add (vi.LocalPath);
					}

				monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 1);

				RunBlockingOperation (group.Key, repository => {
					var repoFiles = repository.ToGitPath (toCheckout);
					int progress = 0;
					if (toCheckout.Any ()) {
						repository.CheckoutPaths ("HEAD", repoFiles, new CheckoutOptions {
							OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
							CheckoutModifiers = CheckoutModifiers.Force,
							CheckoutNotifyFlags = refreshFlags,
							OnCheckoutNotify = delegate (string path, CheckoutNotifyFlags notifyFlags) {
								if ((notifyFlags & CheckoutNotifyFlags.Untracked) == 0)
									return RefreshFile (repository, path, notifyFlags);
								return true;
							}
						});
						LibGit2Sharp.Commands.Stage (repository, repoFiles);
					}

					if (toUnstage.Any ())
						LibGit2Sharp.Commands.Unstage (repository, repository.ToGitPath (toUnstage).ToArray ());
				}, true);
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
				var files = group.Where (f => !f.IsDirectory);
				if (files.Any ())
					RunBlockingOperation (() => LibGit2Sharp.Commands.Stage (group.Key, group.Key.ToGitPath (files)));
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

				RunBlockingOperation (() => {
					var files = group.Key.ToGitPath (group);
					LibGit2Sharp.Commands.Remove (group.Key, files, !keepLocal, null);
				});
			}
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			var gitRev = (GitRevision)revision;
			return RunOperation (repositoryPath, repository => GetCommitTextContent (gitRev.GetCommit (repository), repositoryPath, repository));
		}

		public override DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			try {
				return RunOperation (versionInfo.LocalPath, repository => {
					var patch = repository.Diff.Compare<Patch> (new [] { repository.ToGitPath (versionInfo.LocalPath) });
					// Trim the header by taking out the first 2 lines.
					int diffStart = patch.Content.IndexOf ('\n', patch.Content.IndexOf ('\n') + 1);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, patch.Content.Substring (diffStart + 1));
				});
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

		Blob GetBlob (Commit c, FilePath file, LibGit2Sharp.Repository repo)
		{
			TreeEntry entry = c [repo.ToGitPath (file)];
			return entry != null ? (Blob)entry.Target : null;
		}

		string GetCommitTextContent (Commit c, FilePath file, LibGit2Sharp.Repository repo)
		{
			Blob blob = GetBlob (c, file, repo);
			if (blob == null)
				return string.Empty;

			return blob.IsBinary ? String.Empty : blob.GetContentText ();
		}

		public string GetCurrentRemote ()
		{
			var headRemote = RunSafeOperation (() => RootRepository.Head?.RemoteName);
			if (!string.IsNullOrEmpty (headRemote))
				return headRemote;

			var remotes = new List<string> (GetRemotes ().Select (r => r.Name));
			if (remotes.Count == 0)
				return null;

			return remotes.Contains ("origin") ? "origin" : remotes [0];
		}

		public void Push (ProgressMonitor monitor, string remote, string remoteBranch)
		{
			bool success = true;

			RunBlockingOperation (() => {
				var branch = RootRepository.Head;
				if (branch.TrackedBranch == null) {
					RootRepository.Branches.Update (branch, b => b.TrackedBranch = "refs/remotes/" + remote + "/" + remoteBranch);
				}
			}, true);

			RunOperation (() => {
				RetryUntilSuccess (monitor, credType =>
					RootRepository.Network.Push (RootRepository.Network.Remotes [remote], "refs/heads/" + remoteBranch, new PushOptions {
						OnPushStatusError = pushStatusErrors => success = false,
						CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType)
					})
				);
			}, true);

			if (!success)
				return;

			monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
		}

		public void CreateBranchFromCommit (string name, Commit id)
		{
			RunBlockingOperation (() => RootRepository.CreateBranch (name, id));
		}

		public void CreateBranch (string name, string trackSource, string targetRef)
		{
			RunBlockingOperation (() => CreateBranch (name, trackSource, targetRef, RootRepository));
		}

		void CreateBranch (string name, string trackSource, string targetRef, LibGit2Sharp.Repository repo)
		{
			Commit c = null;
			if (!string.IsNullOrEmpty (trackSource))
				c = repo.Lookup<Commit> (trackSource);

			repo.Branches.Update (
				repo.CreateBranch (name, c ?? repo.Head.Tip),
				bu => bu.TrackedBranch = targetRef);
		}

		public void SetBranchTrackRef (string name, string trackSource, string trackRef)
		{
			RunBlockingOperation (() => {
				var branch = RootRepository.Branches [name];
				if (branch != null) {
					RootRepository.Branches.Update (branch, bu => bu.TrackedBranch = trackRef);
				} else
					CreateBranch (name, trackSource, trackRef, RootRepository);
			});
		}

		public void RemoveBranch (string name)
		{
			RunBlockingOperation (() => RootRepository.Branches.Remove (name));
		}

		public void RenameBranch (string name, string newName)
		{
			RunBlockingOperation (() => RootRepository.Branches.Rename (name, newName, true));
		}

		public IEnumerable<Remote> GetRemotes ()
		{
			// TODO: access to Remote props is not under our control
			return RunOperation (() => RootRepository.Network.Remotes);
		}

		public bool IsBranchMerged (string branchName)
		{
			// check if a branch is merged into HEAD
			return RunOperation (() => {
				var tip = RootRepository.Branches [branchName].Tip.Sha;
				return RootRepository.Commits.Any (c => c.Sha == tip);
			});
		}

		public void RenameRemote (string name, string newName)
		{
			RunBlockingOperation (() => RootRepository.Network.Remotes.Rename (name, newName));
		}

		public void ChangeRemoteUrl (string name, string url)
		{
			RunBlockingOperation (() =>
				RootRepository.Network.Remotes.Update (
					name,
					r => r.Url = url
				));
		}

		public void ChangeRemotePushUrl (string name, string url)
		{
			RunBlockingOperation (() =>
				RootRepository.Network.Remotes.Update (
					name,
					r => r.PushUrl = url
				));
		}

		public void AddRemote (string name, string url, bool importTags)
		{
			if (string.IsNullOrEmpty (name))
				throw new InvalidOperationException ("Name not set");

			RunBlockingOperation (() =>
				RootRepository.Network.Remotes.Update (RootRepository.Network.Remotes.Add (name, url).Name,
					r => r.TagFetchMode = importTags ? TagFetchMode.All : TagFetchMode.Auto));
		}

		public void RemoveRemote (string name)
		{
			RunBlockingOperation (() => RootRepository.Network.Remotes.Remove (name));
		}

		public IEnumerable<Branch> GetBranches ()
		{
			// TODO: access to Remote props is not under our control
			return RunOperation (() => RootRepository.Branches.Where (b => !b.IsRemote));
		}

		public IEnumerable<string> GetTags ()
		{
			return RunOperation (() => RootRepository.Tags.Select (t => t.FriendlyName)).ToArray ();
		}

		public void AddTag (string name, Revision rev, string message)
		{
			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var gitRev = (GitRevision)rev;
			RunBlockingOperation (() => RootRepository.Tags.Add (name, gitRev.GetCommit (RootRepository), sig, message));
		}

		public void RemoveTag (string name)
		{
			RunBlockingOperation (() => RootRepository.Tags.Remove (name));
		}

		public void PushTag (string name)
		{
			RunOperation (() => {
				RetryUntilSuccess (null, credType => RootRepository.Network.Push (RootRepository.Network.Remotes [GetCurrentRemote ()], "refs/tags/" + name + ":refs/tags/" + name, new PushOptions {
					CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType),
				}));
			}, true);
		}

		public IEnumerable<string> GetRemoteBranches (string remoteName)
		{
			return RunOperation (() => RootRepository.Branches
				.Where (b => b.IsRemote && b.RemoteName == remoteName)
				.Select (b => b.FriendlyName.Substring (b.FriendlyName.IndexOf ('/') + 1)))
				.ToArray ();
		}

		public string GetCurrentBranch ()
		{
			return RunOperation (() => RootRepository.Head.FriendlyName);
		}

		public bool SwitchToBranch (ProgressMonitor monitor, string branch)
		{
			Signature sig = GetSignature ();
			Stash stash;
			int stashIndex = -1;
			if (sig == null)
				return false;

			monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), GitService.StashUnstashWhenSwitchingBranches ? 4 : 2);

			if (GitService.StashUnstashWhenSwitchingBranches) {
				// Remove the stash for this branch, if exists
				string currentBranch = RootRepository.Head.FriendlyName;
				stashIndex = RunOperation (() => GetStashForBranch (RootRepository.Stashes, currentBranch));
				if (stashIndex != -1)
					RunBlockingOperation (() => RootRepository.Stashes.Remove (stashIndex));

				if (!TryCreateStash (monitor, GetStashName (currentBranch), out stash))
					return false;

				monitor.Step (1);
			}

			try {
				int progress = 0;
				RunBlockingOperation (() => LibGit2Sharp.Commands.Checkout (RootRepository, branch, new CheckoutOptions {
					OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
					OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
					CheckoutNotifyFlags = refreshFlags,
				}), true);
			} finally {
				// Restore the branch stash
				if (GitService.StashUnstashWhenSwitchingBranches) {
					stashIndex = RunOperation (() => GetStashForBranch (RootRepository.Stashes, branch));
					if (stashIndex != -1)
						PopStash (monitor, stashIndex);
					monitor.Step (1);
				}
			}

			Runtime.RunInMainThread (() => {
				BranchSelectionChanged?.Invoke (this, EventArgs.Empty);
			}).Ignore ();

			monitor.EndTask ();
			return true;
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

			RunOperation (() => {
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
			});
			return cset;
		}

		public DiffInfo[] GetPushDiff (string remote, string branch)
		{
			return RunOperation (() => {
				Commit reference = RootRepository.Branches [remote + "/" + branch].Tip;
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

					// Trim the header by taking out the first 2 lines.
					int diffStart = patch [path].Patch.IndexOf ('\n', patch [path].Patch.IndexOf ('\n') + 1);
					diffs.Add (new DiffInfo (RootPath, RootRepository.FromGitPath (path), patch [path].Patch.Substring (diffStart + 1)));
				}
				return diffs.ToArray ();
			});
		}

		protected override void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo vi = GetVersionInfo (localSrcPath, VersionInfoQueryFlags.IgnoreCache);
			if (vi == null || !vi.IsVersioned) {
				base.OnMoveFile (localSrcPath, localDestPath, force, monitor);
				return;
			}

			var srcRepo = GetRepository (localSrcPath);
			var dstRepo = GetRepository (localDestPath);

			vi = GetVersionInfo (localDestPath, VersionInfoQueryFlags.IgnoreCache);
			RunBlockingOperation (() => {
				if (vi != null && ((vi.Status & (VersionStatus.ScheduledDelete | VersionStatus.ScheduledReplace)) != VersionStatus.Unversioned))
					LibGit2Sharp.Commands.Unstage (dstRepo, localDestPath);

				if (srcRepo == dstRepo) {
					LibGit2Sharp.Commands.Move (srcRepo, localSrcPath, localDestPath);
					ClearCachedVersionInfo (localSrcPath, localDestPath);
				} else {
					File.Copy (localSrcPath, localDestPath);
					LibGit2Sharp.Commands.Remove (srcRepo, localSrcPath, true);
					LibGit2Sharp.Commands.Stage (dstRepo, localDestPath);
				}
			});
		}

		protected override void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo[] versionedFiles = GetDirectoryVersionInfo (localSrcPath, false, true);
			base.OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
			monitor.BeginTask (GettextCatalog.GetString ("Moving files"), versionedFiles.Length);
			foreach (VersionInfo vif in versionedFiles) {
				if (vif.IsDirectory)
					continue;
				FilePath newDestPath = vif.LocalPath.ToRelative (localSrcPath).ToAbsolute (localDestPath);
				Add (newDestPath, false, monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		public override Annotation [] GetAnnotations (FilePath repositoryPath, Revision since)
		{
			return RunOperation (repositoryPath, repository => {
				Commit hc = GetHeadCommit (repository);
				Commit sinceCommit = since != null ? ((GitRevision)since).GetCommit (repository) : null;
				if (hc == null)
					return new Annotation [0];

				var list = new List<Annotation> ();

				var baseDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (GetBaseText (repositoryPath));
				var workingDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (File.ReadAllText (repositoryPath));

				repositoryPath = repository.ToGitPath (repositoryPath);
				var status = repository.RetrieveStatus (repositoryPath);
				if (status != FileStatus.NewInIndex && status != FileStatus.NewInWorkdir) {
					foreach (var hunk in repository.Blame (repositoryPath, new BlameOptions { FindExactRenames = true, StartingAt = sinceCommit })) {
						var commit = hunk.FinalCommit;
						var author = hunk.FinalSignature;
						var working = new Annotation (new GitRevision (this, repositoryPath, commit), author.Name, author.When.LocalDateTime, String.Format ("<{0}>", author.Email));
						for (int i = 0; i < hunk.LineCount; ++i)
							list.Add (working);
					}
				}

				if (sinceCommit == null) {
					Annotation nextRev = new Annotation (null, GettextCatalog.GetString ("<uncommitted>"), DateTime.MinValue, null, GettextCatalog.GetString ("working copy"));
					foreach (var hunk in baseDocument.Diff (workingDocument, includeEol: false)) {
						list.RemoveRange (hunk.RemoveStart - 1, hunk.Removed);
						for (int i = 0; i < hunk.Inserted; ++i) {
							if (hunk.InsertStart + i >= list.Count)
								list.Add (nextRev);
							else
								list.Insert (hunk.InsertStart - 1, nextRev);
						}
					}
				}

				return list.ToArray ();
			});
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
			RunBlockingOperation (() => {
				foreach (var path in localPath.Except (ignored))
					sb.AppendLine (RootRepository.ToGitPath (path));

				File.AppendAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
				LibGit2Sharp.Commands.Stage (RootRepository, ".gitignore");
			});
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
			RunBlockingOperation (() => {
				foreach (var path in ignored.Except (RootRepository.ToGitPath (localPath)))
					sb.AppendLine (path);

				File.WriteAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
				LibGit2Sharp.Commands.Stage (RootRepository, ".gitignore");
			});
		}

		public override bool GetFileIsText (FilePath path)
		{
			return RunOperation (path, repo => {
				Commit c = GetHeadCommit (repo);
				if (c == null)
					return base.GetFileIsText (path);

				var blob = GetBlob (c, path, repo);
				if (blob == null)
					return base.GetFileIsText (path);

				return !blob.IsBinary;
			});
		}
	}

	public class GitRevision: Revision
	{
		readonly string rev;

		internal FilePath FileForChanges { get; set; }

		public string GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, string gitRepository, Commit commit) : base(repo)
		{
			GitRepository = gitRepository;
			rev = commit != null ? commit.Id.Sha : "";
		}

		public GitRevision (Repository repo, string gitRepository, Commit commit, DateTime time, string author, string message) : base(repo, time, author, message)
		{
			GitRepository = gitRepository;
			rev = commit != null ? commit.Id.Sha : "";
		}

		public override string ToString ()
		{
			return rev;
		}

		public override string ShortName {
			get { return rev.Length > 10 ? rev.Substring (0, 10) : rev; }
		}

		internal Commit GetCommit (LibGit2Sharp.Repository repository)
		{
			if (repository.Info.WorkingDirectory != GitRepository)
				throw new ArgumentException ("Commit does not belog to the repository", nameof (repository));
			return repository.Lookup<Commit> (rev);
		}

		public override Revision GetPrevious ()
		{
			var repo = (GitRepository)Repository;
			return repo.RunOperation (GitRepository, repository => GetPrevious (repository));
		}

		internal Revision GetPrevious (LibGit2Sharp.Repository repository)
		{
			if (repository.Info.WorkingDirectory != GitRepository)
				throw new ArgumentException ("Commit does not belog to the repository", nameof (repository));
			var id = repository.Lookup<Commit> (rev)?.Parents.FirstOrDefault ();
			return id == null ? null : new GitRevision (Repository, GitRepository, id);
		}
	}

	static class TaskFailureExtensions
	{
		public static void RunWaitAndCapture (this Task task)
		{
			try {
				task.Wait ();
			} catch (AggregateException ex) {
				var exception = ex.FlattenAggregate ().InnerException;
				ExceptionDispatchInfo.Capture (exception).Throw ();
			}
		}

		public static T RunWaitAndCapture<T> (this Task<T> task)
		{
			try {
				return task.Result;
			} catch (AggregateException ex) {
				var exception = ex.FlattenAggregate ().InnerException;
				ExceptionDispatchInfo.Capture (exception).Throw ();
				throw;
			}
		}
	}
}
