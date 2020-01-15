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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.TemplateEngine.Core.Util;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using ProgressMonitor = MonoDevelop.Core.ProgressMonitor;

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
				if (this.watchGitLockfiles)
					InitFileWatcher (false);
			}
		}

		public static event EventHandler BranchSelectionChanged;

		FileSystemWatcher watcher;

		readonly bool watchGitLockfiles;

		DedicatedThreadScheduler gitScheduler;
		TaskFactory dedicatedOperationFactory;

		DedicatedThreadScheduler GitScheduler {
			get {
				if (gitScheduler == null)
					gitScheduler = new DedicatedThreadScheduler ("Libgit2 Thread");
				return gitScheduler;
			}
		}

		TaskFactory DedicatedOperationFactory {
			get {
				if (dedicatedOperationFactory == null)
					dedicatedOperationFactory = new TaskFactory (GitScheduler);
				return dedicatedOperationFactory;
			}
		}

		public GitRepository ()
		{
			Url = "git://";
		}

		internal GitRepository (VersionControlSystem vcs, FilePath path, string url, bool watchGitLockfiles) : base (vcs)
		{
			RootRepository = CreateSafeRepositoryAsync (path).Result;
			RootPath = RootRepository.Info.WorkingDirectory;
			Url = url;
			this.watchGitLockfiles = watchGitLockfiles;

			if (this.watchGitLockfiles && watcher == null)
				InitFileWatcher ();
		}

		public GitRepository (VersionControlSystem vcs, FilePath path, string url) : this (vcs, path, url, true)
		{

		}

		Task<LibGit2Sharp.Repository> CreateSafeRepositoryAsync (string path)
		{
			//if (Thread.CurrentThread == GitScheduler.DedicatedThread)
				return Task.FromResult (new LibGit2Sharp.Repository (path));
			//return RunOperationAsync ((token) => new LibGit2Sharp.Repository (path));
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

			if (watcher?.Path == dotGitPath.CanonicalPath)
				return;

			ShutdownFileWatcher ();

			watcher = new FileSystemWatcher (dotGitPath.CanonicalPath, "*");
			watcher.Created += HandleGitLockCreated;
			watcher.Deleted += HandleGitLockDeleted;
			watcher.Renamed += HandleGitLockRenamed;
			watcher.Changed += HandleGitChanged;
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

		const string rebaseApply = "rebase-apply";
		const string rebaseMerge = "rebase-merge";
		const string cherryPickHead = "CHERRY_PICK_HEAD";
		const string revertHead = "REVERT_HEAD";

		static bool ShouldLock (string fileName)
			=> fileName.EndsWith (".lock", FilePath.PathComparison)
			|| fileName == rebaseApply
			|| fileName == rebaseMerge
			|| fileName == cherryPickHead
			|| fileName == revertHead;

		void HandleGitLockCreated (object sender, FileSystemEventArgs e)
		{
			if (ShouldLock (e.Name))
				OnGitLocked (e.FullPath);
		}

		void HandleGitLockRenamed (object sender, RenamedEventArgs e)
		{
			if (ShouldLock (e.OldName)) {
				if (!ShouldLock (e.Name)) {
					OnGitUnlocked (e.OldFullPath);
				} else {
					lock (lockedPathes) {
						lockedPathes.Remove (e.OldFullPath);
						lockedPathes.Add (e.FullPath);
					}
				}
			}
			if (e.Name == "HEAD" && e.OldName == "HEAD.lock")
				Runtime.RunInMainThread (() => BranchSelectionChanged?.Invoke (this, EventArgs.Empty));
		}

		void HandleGitLockDeleted (object sender, FileSystemEventArgs e)
		{
			if (ShouldLock (e.Name))
				OnGitUnlocked (e.FullPath);
		}

		void HandleGitChanged (object sender, FileSystemEventArgs e)
		{
			if (e.Name == "HEAD")
				Runtime.RunInMainThread (() => BranchSelectionChanged?.Invoke (this, EventArgs.Empty));
		}

		readonly ManualResetEventSlim gitLock = new ManualResetEventSlim (true);
		readonly HashSet<FilePath> lockedPathes = new HashSet<FilePath> ();

		void OnGitLocked (string path)
		{
			lock (lockedPathes) {
				if (File.Exists (path) && lockedPathes.Add (path) && lockedPathes.Count == 1 && gitLock.IsSet) {
					gitLock.Reset ();
					FileService.FreezeEvents ();
				}
			}
		}

		void OnGitUnlocked (string file)
		{
			lock (lockedPathes) {
				lockedPathes.Remove (file);
				lockedPathes.RemoveWhere (path => !File.Exists (path));

				if (!gitLock.IsSet && lockedPathes.Count == 0) {
					gitLock.Set ();
					ThawEvents ();
				}
			}
		}

		/// <summary>
		/// Checks if the lock-files still exist, or if FSW has skipped an event
		/// </summary>
		/// <returns><c>true</c> if any locked file exists. Resets the event, cleans the list and returns <c>false</c> otherwise</returns>
		bool GetHasValidLocks ()
		{
			lock (lockedPathes) {
				if (lockedPathes.Count > 0) {
					lockedPathes.RemoveWhere (path => {
						try {
							// we don't care about files or folders, so just remove if this fails
							File.GetAttributes (path);
							return false;
						} catch (FileNotFoundException) {
							return true;
						}
					});
				}
				var result = lockedPathes.Count > 0;
				if (!result && !gitLock.IsSet) {
					gitLock.Set ();
				}
				return result;
			}
		}

		readonly int recheckLocksTimeout = 250;

		bool WaitAndFreezeEvents (CancellationToken cancellationToken)
		{
			// checking locks is expensive, rely on FSW to be right first and check only after timeout
			// this will block until all locks are released
			bool recheck = false;
			while (!gitLock.IsSet && (!recheck || GetHasValidLocks ())) {
				if (gitLock.Wait (recheckLocksTimeout, cancellationToken)) {
					break;
				}
				recheck = true;
			}
			if (cancellationToken.IsCancellationRequested)
				return false;

			FileService.FreezeEvents ();
			return true;
		}

		void ThawEvents ()
		{
			FileService.ThawEvents ();
		}
		object disposeLock = new object ();
		protected override void Dispose (bool disposing)
		{
			lock (disposeLock) {
				if (IsDisposed)
					return;
				IsDisposed = true;
				if (disposing) {
					ShutdownFileWatcher ();
					gitLock?.Dispose ();

					if (rootRepository != null) {
						// disposeTokenSource.Cancel is called so all operations on DedicatedOperationFactory should be canceled
						DedicatedOperationFactory.StartNew (() => {
							try {
								rootRepository?.Dispose ();
							} catch (Exception e) {
								LoggingService.LogInternalError ("Disposing LibGit2Sharp.Repository failed", e);
							}
							if (cachedSubmodules != null) {
								foreach (var submodule in cachedSubmodules) {
									try {
										var submoduleRepository = submodule?.Item2;
										submoduleRepository?.Dispose ();
									} catch (Exception e) {
										LoggingService.LogInternalError ("Disposing LibGit2Sharp.Repository failed", e);
									}
								}
							}
						}).Ignore ();
						gitScheduler.Dispose ();
					}
				}

				// now it's safe to dispose the base and release all information caches
				// this will also wait for the scheduler to finish all operations and shutdown
				base.Dispose (disposing);

				rootRepository = null;
				cachedSubmodules = null;
			}
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
				RootRepository = CreateSafeRepositoryAsync (RootPath).Result;
		}

		public override string LocationDescription {
			get { return Url ?? RootPath; }
		}

		public override bool AllowLocking {
			get { return false; }
		}

		public override Task<string> GetBaseTextAsync (FilePath localFile, CancellationToken cancellationToken)
		{
			return RunOperationAsync (localFile, (repository, token) => {
				var c = GetHeadCommit (repository);
				return c == null ? string.Empty : GetCommitTextContent (c, localFile, repository);
			}, cancellationToken: cancellationToken);
		}

		Task<Commit> GetHeadCommitAsync (LibGit2Sharp.Repository repository)
		{
			return RunOperationAsync ((token) => GetHeadCommit (repository));
		}

		static Commit GetHeadCommit (LibGit2Sharp.Repository repository)
		{
			return repository.Head.Tip;
		}

		internal StashCollection GetStashes ()
		{
			return RunOperation (() => RootRepository.Stashes);
		}

		public Task<StashCollection> GetStashesAsync (CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => RootRepository.Stashes, cancellationToken: cancellationToken);
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
			return RunBlockingOperation ((token) => ApplyStash (RootRepository, monitor, stashIndex), true, monitor.CancellationToken);
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
		public override bool TryGetFileUpdateEventInfo (Repository rep, FilePath file, out FileUpdateEventInfo eventInfo)
		{
			if (file.FileName == "index" && file.ParentDirectory.FileName == ".git") {
				eventInfo = FileUpdateEventInfo.UpdateRepository (rep);
				return true;
			}
			return base.TryGetFileUpdateEventInfo (rep, file, out eventInfo);
		}

		public StashApplyStatus PopStash (ProgressMonitor monitor, int stashIndex)
		{
			if (monitor != null)
				monitor.BeginTask (GettextCatalog.GetString ("Popping stash"), 1);

			var res = RunBlockingOperation ((token) => {
				var stash = RootRepository.Stashes [stashIndex];
				int progress = 0;
				return RootRepository.Stashes.Pop (stashIndex, new StashApplyOptions {
					CheckoutOptions = new CheckoutOptions {
						OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
						OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
						CheckoutNotifyFlags = refreshFlags,
					},
				});
			}, true, monitor.CancellationToken);

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

			stash = RunBlockingOperation ((token) => RootRepository.Stashes.Add (sig, message, StashModifiers.Default | StashModifiers.IncludeUntracked), cancellationToken: monitor.CancellationToken);

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
				lock (this) {
					cachedSubmodules = RootRepository.Submodules.Select (s => {
						var fp = new FilePath (Path.Combine (RootRepository.Info.WorkingDirectory, s.Path.Replace ('/', Path.DirectorySeparatorChar))).CanonicalPath;
						return new Tuple<FilePath, LibGit2Sharp.Repository> (fp, CreateSafeRepositoryAsync (fp).Result);
					}).ToArray ();
				}
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
				throw new ObjectDisposedException (typeof (GitRepository).Name);
			if (RootRepository != null)
				InitFileWatcher ();
		}

		bool IsGitThread {
			get { return Thread.CurrentThread == GitScheduler.DedicatedThread; }
		}

		internal void RunOperation (Action action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			if (IsGitThread)
				action ();
			else
				DedicatedOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal Task RunOperationAsync (Action<CancellationToken> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();
			if (IsGitThread) {
				action (cancellationToken);
				return Task.CompletedTask;
			}
			var src = LinkTokenToDispose (ref cancellationToken);
			return DedicatedOperationFactory.StartNew (() => {
				try {
					action (cancellationToken);
				} finally {
					src.Dispose ();
				}
			}, cancellationToken);
		}

		internal T RunOperation<T> (Func<T> action, bool hasUICallbacks = false)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			if (IsGitThread)
				return action ();
			return DedicatedOperationFactory.StartNew (action).RunWaitAndCapture ();
		}

		internal Task<T> RunOperationAsync<T> (Func<CancellationToken, T> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();
			if (IsGitThread)
				return Task.FromResult (action (cancellationToken));
			var src = LinkTokenToDispose (ref cancellationToken);
			return DedicatedOperationFactory.StartNew (() => {
				try {
					return action (cancellationToken);
				} finally {
					src.Dispose ();
				}
			}, cancellationToken);
		}

		internal Task<T> RunOperationAsync<T> (FilePath localPath, Func<LibGit2Sharp.Repository, CancellationToken, T> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();
			if (IsGitThread)
				return Task.FromResult (action (GetRepository (localPath), cancellationToken));
			var src = LinkTokenToDispose (ref cancellationToken);
			return DedicatedOperationFactory.StartNew (() => {
				try {
					return action (GetRepository (localPath), cancellationToken);
				} finally {
					src.Dispose ();
				}
			}, cancellationToken);
		}

		internal T RunBlockingOperation<T> (Func<CancellationToken, T> action, bool hasUICallbacks = false, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();
			if (hasUICallbacks)
				EnsureBackgroundThread ();
			if (IsGitThread) {
				return RunBlockingOperationInternal (action, cancellationToken);
			}

			using (LinkTokenToDispose (ref cancellationToken)) {
				return DedicatedOperationFactory.StartNew (() => RunBlockingOperationInternal (action, cancellationToken))
					.RunWaitAndCapture ();
			}

			T RunBlockingOperationInternal (Func<CancellationToken, T> action, CancellationToken cancellationToken)
			{
				if (!WaitAndFreezeEvents (cancellationToken))
					return default;

				try {
					return action (cancellationToken);
				} finally {
					ThawEvents ();
				}
			}
		}

		internal Task RunBlockingOperationAsync (Action<CancellationToken> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();

			if (IsGitThread) {
				RunBlockingOperationInternal (action, cancellationToken);
				return Task.CompletedTask;
			}

			var src = LinkTokenToDispose (ref cancellationToken);
			return DedicatedOperationFactory.StartNew (() => {
				try {
					RunBlockingOperationInternal (action, cancellationToken);
				} finally {
					src.Dispose ();
				}
			});

			void RunBlockingOperationInternal (Action<CancellationToken> action, CancellationToken cancellationToken)
			{
				if (!WaitAndFreezeEvents (cancellationToken))
					cancellationToken.ThrowIfCancellationRequested ();

				try {
					action (cancellationToken);
				} finally {
					ThawEvents ();
				}
			}
		}

		internal Task RunBlockingOperationAsync (FilePath localPath, Action<LibGit2Sharp.Repository, CancellationToken> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();

			if (IsGitThread) {
				RunBlockingOperationInternal (localPath, action, cancellationToken);
				return Task.CompletedTask;
			}

			var src = LinkTokenToDispose (ref cancellationToken);

			return DedicatedOperationFactory.StartNew (() => {
				try {
					RunBlockingOperationInternal (localPath, action, cancellationToken);
				} finally {
					src.Dispose ();
				}
			});

			void RunBlockingOperationInternal (FilePath localPath, Action<LibGit2Sharp.Repository, CancellationToken> action, CancellationToken cancellationToken)
			{
				if (!WaitAndFreezeEvents (cancellationToken))
					cancellationToken.ThrowIfCancellationRequested ();

				try {
					action (GetRepository (localPath), cancellationToken);
				} finally {
					ThawEvents ();
				}
			}
		}

		internal Task<T> RunBlockingOperationAsync<T> (Func<CancellationToken, T> action, CancellationToken cancellationToken = default)
		{
			EnsureInitialized ();
			if (IsGitThread) {
				return Task.FromResult (RunBlockingOperationInternal (action, cancellationToken));
			}

			var src = LinkTokenToDispose (ref cancellationToken);
			return DedicatedOperationFactory.StartNew<T> (() => {
				try {
					return action (cancellationToken);
				} finally {
					src.Dispose ();
				}
			}, cancellationToken);
			
			T RunBlockingOperationInternal (Func<CancellationToken, T> action, CancellationToken cancellationToken)
			{
				if (!WaitAndFreezeEvents (cancellationToken))
					cancellationToken.ThrowIfCancellationRequested ();

				try {
					return action (cancellationToken);
				} finally {
					ThawEvents ();
				}
			}
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

		protected override Task<Revision []> OnGetHistoryAsync (FilePath localFile, Revision since, CancellationToken cancellationToken)
		{
			return RunOperationAsync ((token) => {
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
				}).Cast<Revision> ().ToArray();
			}, cancellationToken: cancellationToken);
		}

		protected override Task<RevisionPath []> OnGetRevisionChangesAsync (Revision revision, CancellationToken cancellationToken = default)
		{
			var rev = (GitRevision)revision;
			return RunOperationAsync ((token) => {
				var commit = rev.GetCommit (RootRepository);
				if (commit == null)
					return new RevisionPath [0];

				var paths = new List<RevisionPath> ();
				var parent = commit.Parents.FirstOrDefault ();
				var changes = RootRepository.Diff.Compare<TreeChanges> (parent?.Tree, commit.Tree);

				foreach (var entry in changes.Added) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
				}
				foreach (var entry in changes.Copied) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Add, null));
				}
				foreach (var entry in changes.Deleted) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.OldPath), RevisionAction.Delete, null));
				}
				foreach (var entry in changes.Renamed) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RootRepository.FromGitPath (entry.OldPath), RevisionAction.Replace, null));
				}
				foreach (var entry in changes.Modified) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
				}
				foreach (var entry in changes.TypeChanged) {
					token.ThrowIfCancellationRequested ();
					paths.Add (new RevisionPath (RootRepository.FromGitPath (entry.Path), RevisionAction.Modify, null));
				}
				return paths.ToArray ();
			}, cancellationToken: cancellationToken);
		}

		protected override async Task<IReadOnlyList<VersionInfo>> OnGetVersionInfoAsync (IEnumerable<FilePath> paths, bool getRemoteStatus, CancellationToken cancellationToken)
		{
			try {
				return await GetDirectoryVersionInfoAsync (FilePath.Null, paths, getRemoteStatus, false, cancellationToken).ConfigureAwait (false);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to query git status", e);
				return paths.Select (x => VersionInfo.CreateUnversioned (x, false)).ToList ();
			}
		}

		protected override async Task<VersionInfo []> OnGetDirectoryVersionInfoAsync (FilePath localDirectory, bool getRemoteStatus, bool recursive, CancellationToken cancellationToken)
		{
			try {
				return await GetDirectoryVersionInfoAsync (localDirectory, null, getRemoteStatus, recursive, cancellationToken).ConfigureAwait (false);
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
		async Task<VersionInfo[]> GetDirectoryVersionInfoAsync (FilePath localDirectory, IEnumerable<FilePath> localFileNames, bool getRemoteStatus, bool recursive, CancellationToken cancellationToken)
		{
			var versions = new List<VersionInfo> ();

			if (localFileNames != null) {
				var localFiles = new List<FilePath> ();
				var groups = GroupByRepository (localFileNames);
				foreach (var group in groups) {
					var repositoryRoot = group.Key.Info.WorkingDirectory;
					GitRevision arev;
					lock (versionInfoCacheEmptyRevision) {
						if (!versionInfoCacheEmptyRevision.TryGetValue (repositoryRoot, out arev)) {
							arev = new GitRevision (this, repositoryRoot, null);
							versionInfoCacheEmptyRevision.Add (repositoryRoot, arev);
						}
					}
					foreach (var p in group) {
						if (Directory.Exists (p)) {
							if (recursive)
								versions.AddRange (await GetDirectoryVersionInfoAsync (p, getRemoteStatus, true, cancellationToken).ConfigureAwait (false));
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
						Commit headCommit = await GetHeadCommitAsync (repository).ConfigureAwait (false);
						if (headCommit != null) {
							bool runAsync = false;
							lock (versionInfoCacheRevision) {
								if (!versionInfoCacheRevision.TryGetValue (repositoryRoot, out rev)) {
									rev = new GitRevision (this, repositoryRoot, headCommit);
									versionInfoCacheRevision.Add (repositoryRoot, rev);
								} else
									runAsync = true;
							}

							if (runAsync && await RunOperationAsync ((token) => rev.GetCommit (repository)).ConfigureAwait (false) != headCommit) {
								rev = new GitRevision (this, repositoryRoot, headCommit);
								versionInfoCacheRevision [repositoryRoot] = rev;
							}
						}

						await RunOperationAsync ((token) => GetFilesVersionInfoCore (repository, rev, group.ToList (), versions)).ConfigureAwait (false);
					}
				}
			} else {
				var directories = new List<FilePath> ();
				CollectFiles (directories, localDirectory, recursive);

				// Set directory items as Versioned.
				GitRevision arev = null;
				foreach (var group in GroupByRepositoryRoot (directories)) {
					lock (versionInfoCacheEmptyRevision) {
						if (!versionInfoCacheEmptyRevision.TryGetValue (group.Key, out arev)) {
							arev = new GitRevision (this, group.Key, null);
							versionInfoCacheEmptyRevision.Add (group.Key, arev);
						}
					}
					foreach (var p in group)
						versions.Add (new VersionInfo (p, "", true, VersionStatus.Versioned, arev, VersionStatus.Versioned, null));
				}

				var rootRepository = GetRepository (RootPath);
				Commit headCommit = await GetHeadCommitAsync (rootRepository).ConfigureAwait (false);
				if (headCommit != null) {
					bool runAsync = false;

					lock (versionInfoCacheRevision) {
						if (!versionInfoCacheRevision.TryGetValue (RootPath, out arev)) {
							arev = new GitRevision (this, RootPath, headCommit);
							versionInfoCacheRevision.Add (RootPath, arev);
						} else {
							runAsync = true;
						}

					}

					if (runAsync && await RunOperationAsync ((token) => arev.GetCommit (rootRepository)).ConfigureAwait (false) != headCommit) {
						arev = new GitRevision (this, RootPath, headCommit);
						lock (versionInfoCacheRevision) {
							versionInfoCacheRevision [RootPath] = arev;
						}
					}
				}

				await RunOperationAsync ((token) => GetDirectoryVersionInfoCore (rootRepository, arev, localDirectory.CanonicalPath, versions, recursive)).ConfigureAwait (false);
			}

			return versions.ToArray ();
		}

		void GetFilesVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, List<FilePath> localPaths, List<VersionInfo> versions)
		{
			if (IsDisposed)
				return;
			AssertIsGitThread ();
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
				else if ((status & (FileStatus.TypeChangeInWorkdir | FileStatus.TypeChangeInIndex | FileStatus.ModifiedInWorkdir | FileStatus.ModifiedInIndex)) != 0)
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

		void AssertIsGitThread ()
		{
			if (Thread.CurrentThread.ManagedThreadId != GitScheduler.DedicatedThread.ManagedThreadId)
				throw new InvalidOperationException ();
		}

		void GetDirectoryVersionInfoCore (LibGit2Sharp.Repository repo, GitRevision rev, FilePath directory, List<VersionInfo> versions, bool recursive)
		{
			if (IsDisposed)
				return;
			AssertIsGitThread ();
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

		protected internal override async Task<VersionControlOperation> GetSupportedOperationsAsync (VersionInfo vinfo, CancellationToken cancellationToken)
		{
			VersionControlOperation ops = await base.GetSupportedOperationsAsync (vinfo, cancellationToken).ConfigureAwait (false);
			if (await GetCurrentRemoteAsync (cancellationToken).ConfigureAwait (false) == null)
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

		protected override async Task<Repository> OnPublishAsync (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
		{
			// Initialize the repository
			RootPath = localPath;
			RootRepository = CreateSafeRepositoryAsync (LibGit2Sharp.Repository.Init (localPath)).Result;
			RootRepository.Network.Remotes.Add ("origin", Url);

			// Add the project files
			ChangeSet cs = CreateChangeSet (localPath);
			foreach (FilePath fp in files) {
				LibGit2Sharp.Commands.Stage (RootRepository, RootRepository.ToGitPath (fp));
				await cs.AddFileAsync (fp).ConfigureAwait (false);
			}

			// Create the initial commit
			cs.GlobalComment = message;
			await CommitAsync (cs, monitor).ConfigureAwait (false);

			RootRepository.Branches.Update (RootRepository.Branches ["master"], branch => branch.TrackedBranch = "refs/remotes/origin/master");

			await RetryUntilSuccessAsync (monitor, credType => {

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
					return Task.FromException (vcex);
				}
				return Task.CompletedTask;
			});

			return this;
		}

		protected override async Task OnUpdateAsync (FilePath [] localPaths, bool recurse, ProgressMonitor monitor)
		{
			// TODO: Make it work differently for submodules.
			monitor.BeginTask (GettextCatalog.GetString ("Updating"), 5);

			if (RootRepository.Head.IsTracking) {
				await FetchAsync (monitor, RootRepository.Head.RemoteName).ConfigureAwait (false);

				GitUpdateOptions options = GitService.StashUnstashWhenUpdating ? GitUpdateOptions.NormalUpdate : GitUpdateOptions.UpdateSubmodules;
				if (GitService.UseRebaseOptionWhenPulling)
					await RebaseAsync (RootRepository.Head.TrackedBranch.FriendlyName, options, monitor, true).ConfigureAwait (false);
				else
					await MergeAsync (RootRepository.Head.TrackedBranch.FriendlyName, options, monitor, true).ConfigureAwait (false);

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

		static async Task RetryUntilSuccessAsync (ProgressMonitor monitor, Func<GitCredentialsType, Task> func, Action onRetry = null)
		{
			bool retry;
			using (var tfsSession = new TfsSmartSession ()) {
				do {
					var credType = tfsSession.Disposed ? GitCredentialsType.Normal : GitCredentialsType.Tfs;
					try {
						await func (credType).ConfigureAwait (false);
						GitCredentials.StoreCredentials (credType);
						retry = false;
					} catch (AuthenticationException e) {
						GitCredentials.InvalidateCredentials (credType);
						retry = await Runtime.RunInMainThread (() => HandleAuthenticationException (e)).ConfigureAwait (false);
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
						if (e.Message.Contains ("remote: Public key authentication failed.")) {
							// if key auth fails, retry until the user selects the proper key or cancels
							retry = true;
							continue;
						}
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

		public async Task FetchAsync (ProgressMonitor monitor, string remote)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Fetching"), 1);
			monitor.Log.WriteLine (GettextCatalog.GetString ("Fetching from '{0}'", remote));
			int progress = 0;

			var innerTask = await RunBlockingOperationAsync ((token) => {
				var refSpec = RootRepository.Network.Remotes [remote]?.FetchRefSpecs.Select (spec => spec.Specification);
				return RetryUntilSuccessAsync (monitor, credType => {
					LibGit2Sharp.Commands.Fetch (RootRepository, remote, refSpec, new FetchOptions {
						CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType),
						OnTransferProgress = tp => OnTransferProgress (tp, monitor, ref progress),
					}, string.Empty);
					return Task.CompletedTask;
				});
			}).ConfigureAwait (false);
			await innerTask.ConfigureAwait (false);

			monitor.Step (1);
			monitor.EndTask ();
		}

		async Task<(bool, int, GitUpdateOptions)> CommonPreMergeRebase (GitUpdateOptions options, ProgressMonitor monitor, int stashIndex, string branch, string actionButtonTitle, bool isUpdate)
		{
			if (!WaitAndFreezeEvents (monitor.CancellationToken))
				return (false, -1, options);
			monitor.Step (1);

			if ((options & GitUpdateOptions.SaveLocalChanges) != GitUpdateOptions.SaveLocalChanges) {
				const VersionStatus unclean = VersionStatus.Modified | VersionStatus.ScheduledAdd | VersionStatus.ScheduledDelete;
				bool modified = false;
				if ((await GetDirectoryVersionInfoAsync (RootPath, false, true, monitor.CancellationToken).ConfigureAwait (false)).Any (v => (v.Status & unclean) != VersionStatus.Unversioned))
					modified = true;

				if (modified) {
					if (!PromptToStash (
						GettextCatalog.GetString ("There are local changes that conflict with changes committed in the <b>{0}</b> branch. Would you like to stash the changes and continue?", branch),
						actionButtonTitle,
						isUpdate ? GettextCatalog.GetString ("Automatically stash/unstash changes when merging/rebasing") : null,
						isUpdate ? GitService.StashUnstashWhenUpdating : null))
						return (false, -1, options);

					options |= GitUpdateOptions.SaveLocalChanges;
				}
			}
			if ((options & GitUpdateOptions.SaveLocalChanges) == GitUpdateOptions.SaveLocalChanges) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Saving local changes"));
				Stash stash;
				if (!TryCreateStash (monitor, GetStashName ("_tmp_"), out stash))
					return (false, - 1, options);

				if (stash != null)
					stashIndex = 0;
				monitor.Step (1);
			}
			return (true, stashIndex, options);
		}

		bool PromptToStash (string messageText, string actionButtonTitle, string dontAskLabel = null, ConfigurationProperty<bool> dontAskProperty = null)
		{
			bool showDontAsk = !string.IsNullOrEmpty (dontAskLabel) && dontAskProperty != null;
			var message = new GenericMessage {
				Text = GettextCatalog.GetString ("Conflicting local changes found"),
				SecondaryText = messageText,
				Icon = Ide.Gui.Stock.Question
			};
			if (showDontAsk) {
				message.AddOption (nameof (dontAskLabel), dontAskLabel, dontAskProperty.Value);
			}
			message.Buttons.Add (AlertButton.Cancel);
			message.Buttons.Add (new AlertButton (actionButtonTitle));
			message.DefaultButton = 1;

			var result = MessageService.GenericAlert (message) != AlertButton.Cancel;
			if (result && showDontAsk)
				dontAskProperty.Value = message.GetOptionValue (nameof (dontAskLabel));
			return result;
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
					RevertAsync (repository.FromGitPath (conflictFile.Ancestor.Path), false, monitor);
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

		async Task CommonPostMergeRebase (int stashIndex, GitUpdateOptions options, ProgressMonitor monitor, Commit oldHead)
		{
			try {
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
							await RunBlockingOperationAsync ((token) => RootRepository.Stashes.Remove (stashIndex), cancellationToken: monitor.CancellationToken).ConfigureAwait (false);
						monitor.Step (1);
					}
				}
			} finally {
				ThawEvents ();
				monitor.EndTask ();
			}
		}

		public Task RebaseAsync (string branch, GitUpdateOptions options, ProgressMonitor monitor)
		{
			return RebaseAsync (branch, options, monitor, false);
		}

		async Task RebaseAsync (string branch, GitUpdateOptions options, ProgressMonitor monitor, bool isUpdate)
		{
			int stashIndex = -1;
			var oldHead = RootRepository.Head.Tip;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Rebasing"), 5);
				var (success, newStashIndex, newOptions) = await CommonPreMergeRebase (options, monitor, stashIndex, branch, GettextCatalog.GetString ("Stash and Rebase"), isUpdate).ConfigureAwait (false);
				if (!success)
					return;
				stashIndex = newStashIndex;
				options = newOptions;

				await RunBlockingOperationAsync ((token) => {

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
				}, monitor.CancellationToken).ConfigureAwait (false);
			} finally {
				await CommonPostMergeRebase (stashIndex, options, monitor, oldHead).ConfigureAwait (false);
			}
		}

		public Task MergeAsync (string branch, GitUpdateOptions options, ProgressMonitor monitor, FastForwardStrategy strategy = FastForwardStrategy.Default)
		{
			return MergeAsync (branch, options, monitor, false, strategy);
		}

		async Task MergeAsync (string branch, GitUpdateOptions options, ProgressMonitor monitor, bool isUpdate, FastForwardStrategy strategy = FastForwardStrategy.Default)
		{
			int stashIndex = -1;

			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var oldHead = RootRepository.Head.Tip;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Merging"), 5);
				var (success, newStashIndex, newOptions) = await CommonPreMergeRebase (options, monitor, stashIndex, branch, GettextCatalog.GetString ("Stash and Merge"), isUpdate).ConfigureAwait (false);
				if (!success)
					return;
				stashIndex = newStashIndex;
				options = newOptions;

				// Do a merge.
				MergeResult mergeResult = await RunBlockingOperationAsync ((token) =>
					RootRepository.Merge (branch, sig, new MergeOptions {
						CheckoutNotifyFlags = refreshFlags,
						OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
					}), monitor.CancellationToken).ConfigureAwait (false);
				if (mergeResult.Status == MergeStatus.Conflicts)
						ConflictResolver (RootRepository, monitor, RootRepository.Head.Tip, RootRepository.Info.Message);
			} finally {
				await CommonPostMergeRebase (stashIndex, GitUpdateOptions.SaveLocalChanges, monitor, oldHead).ConfigureAwait (false);
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

		protected override async Task OnCommitAsync (ChangeSet changeSet, ProgressMonitor monitor)
		{
			string message = changeSet.GlobalComment;
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Commit message must not be null or empty!", "message");

			Signature sig = GetSignature ();
			if (sig == null)
				return;

			var repo = (GitRepository)changeSet.Repository;
			var addedFiles = await GetAddedLocalPathItems (changeSet, monitor.CancellationToken).ConfigureAwait (false);

			await RunBlockingOperationAsync ((token) => {
				try {
					// Unstage added files not included in the changeSet
					if (addedFiles.Any ())
						LibGit2Sharp.Commands.Unstage (RootRepository, addedFiles.ToPathStrings ());
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Failed to commit.", ex);
					return;
				}
				try {
					// Commit
					LibGit2Sharp.Commands.Stage (RootRepository, changeSet.Items.Select (i => i.LocalPath).ToPathStrings ());

					if (changeSet.ExtendedProperties.Contains ("Git.AuthorName"))
						RootRepository.Commit (message, new Signature (
							(string)changeSet.ExtendedProperties ["Git.AuthorName"],
							(string)changeSet.ExtendedProperties ["Git.AuthorEmail"],
							DateTimeOffset.Now), sig);
					else
						RootRepository.Commit (message, sig, sig);
				} catch (Exception ex) {
					LoggingService.LogInternalError ("Failed to commit.", ex);
				} finally {
					// Always at the end, stage again the unstage added files not included in the changeSet
					if (addedFiles.Any ())
						LibGit2Sharp.Commands.Stage (RootRepository, addedFiles.ToPathStrings ());
				}
			}, cancellationToken: monitor.CancellationToken).ConfigureAwait (false);
		}

		async Task<HashSet<FilePath>> GetAddedLocalPathItems (ChangeSet changeSet, CancellationToken cancellationToken)
		{
			var addedLocalPathItems = new HashSet<FilePath> ();
			try {
				var directoryVersionInfo = await GetDirectoryVersionInfoAsync (changeSet.BaseLocalPath, false, true, cancellationToken).ConfigureAwait (false);
				const VersionStatus addedStatus = VersionStatus.Versioned | VersionStatus.ScheduledAdd;
				var directoryVersionInfoItems = directoryVersionInfo.Where (vi => vi.Status == addedStatus);

				foreach (var item in directoryVersionInfoItems)
					foreach (var changeSetItem in changeSet.Items)
						if (item.LocalPath != changeSetItem.LocalPath)
							addedLocalPathItems.Add (item.LocalPath);
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Could not get added VersionInfo items.", ex);
			}
			return addedLocalPathItems;
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

		public void GetUserInfo (out string name, out string email, Components.Window parent = null)
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
						if ((Gtk.ResponseType)MessageService.RunCustomDialog (dlg, parent) == Gtk.ResponseType.Ok) {
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

		protected override async Task OnCheckoutAsync (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			int transferProgress = 0;
			int checkoutProgress = 0;

			try {
				monitor.BeginTask (GettextCatalog.GetString ("Cloning…"), 2);
				bool skipSubmodules = false;
				var innerTask = await RunOperationAsync ((token) => RetryUntilSuccessAsync (monitor, credType => {
					var options = new CloneOptions {
						CredentialsProvider = (url, userFromUrl, types) => {
							transferProgress = checkoutProgress = 0;
							return GitCredentials.TryGet (url, userFromUrl, types, credType);
						},
						RepositoryOperationStarting = ctx => {
							Runtime.RunInMainThread (() => {
								monitor.Log.WriteLine (GettextCatalog.GetString ("Checking out repository at '{0}'"), ctx.RepositoryPath);
							});
							return true;
						},
						OnTransferProgress = (tp) => OnTransferProgress (tp, monitor, ref transferProgress),
						OnCheckoutProgress = (path, completedSteps, totalSteps) => {
							OnCheckoutProgress (completedSteps, totalSteps, monitor, ref checkoutProgress);
							Runtime.RunInMainThread (() => {
								monitor.Log.WriteLine (GettextCatalog.GetString ("Checking out file '{0}'"), path);
							});
						}
					};

					try {
						RootPath = LibGit2Sharp.Repository.Clone (Url, targetLocalPath, options);
					} catch (UserCancelledException) {
						return Task.CompletedTask;
					}
					var updateOptions = new SubmoduleUpdateOptions {
						Init = true,
						CredentialsProvider = options.CredentialsProvider,
						OnTransferProgress = options.OnTransferProgress,
						OnCheckoutProgress = options.OnCheckoutProgress,
					};
					monitor.Step (1);
					try {
						if (!skipSubmodules)
							RecursivelyCloneSubmodules (RootPath, updateOptions, monitor);
					} catch (Exception e) {
						LoggingService.LogError ("Cloning submodules failed", e);
						FileService.DeleteDirectory (RootPath);
						skipSubmodules = true;
						return Task.FromException (e);
					}
					return Task.CompletedTask;
				}), monitor.CancellationToken).ConfigureAwait (false);

				await innerTask.ConfigureAwait (false);

				if (monitor.CancellationToken.IsCancellationRequested || RootPath.IsNull)
					return;

				RootPath = RootPath.CanonicalPath.ParentDirectory;

				RootRepository = await CreateSafeRepositoryAsync (RootPath).ConfigureAwait (false);
				InitFileWatcher ();
				if (skipSubmodules) {
					MessageService.ShowError (GettextCatalog.GetString("Cloning submodules failed"), GettextCatalog.GetString ("Please use the command line client to init the submodules manually."));
				}
			} catch (Exception e) {
				LoggingService.LogInternalError ("Error while cloning repository " + rev + " recuse: " + recurse, e);
				throw e;
			} finally {
				monitor.EndTask ();
			}
		}

		static void RecursivelyCloneSubmodules (string repoPath, SubmoduleUpdateOptions updateOptions, ProgressMonitor monitor)
		{
			var submodules = new List<string> ();
			using (var repo = new LibGit2Sharp.Repository (repoPath)) {
				// Iterate through the submodules (where the submodule is in the index),
				// and clone them.
				var submoduleArray = repo.Submodules.Where (sm => sm.RetrieveStatus ().HasFlag (SubmoduleStatus.InIndex)).ToArray ();
				monitor.BeginTask (GettextCatalog.GetString ("Cloning submodules…"), submoduleArray.Length);
				try {
					foreach (var sm in submoduleArray) {
						if (monitor.CancellationToken.IsCancellationRequested) {
							throw new UserCancelledException ("Recursive clone of submodules was cancelled.");
						}

						Runtime.RunInMainThread (() => {
							monitor.Log.WriteLine (GettextCatalog.GetString ("Checking out submodule at '{0}'…", sm.Path));
							monitor.Step (1);
						});
						repo.Submodules.Update (sm.Name, updateOptions);

						submodules.Add (Path.Combine (repo.Info.WorkingDirectory, sm.Path));
					}
				} finally {
					monitor.EndTask ();
				}
			}

			// If we are continuing the recursive operation, then
			// recurse into nested submodules.
			// Check submodules to see if they have their own submodules.
			foreach (string path in submodules) {
				RecursivelyCloneSubmodules (path, updateOptions, monitor);
			}
		}

		protected override async Task OnRevertAsync (FilePath [] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepositoryRoot (localPaths)) {
				var toCheckout = new HashSet<FilePath> ();
				var toUnstage = new HashSet<FilePath> ();

				foreach (var item in group)
					if (item.IsDirectory) {
						foreach (var vi in await GetDirectoryVersionInfoAsync (item, false, recurse, monitor.CancellationToken).ConfigureAwait (false))
							if (!vi.IsDirectory) {
								if (vi.Status == VersionStatus.Unversioned)
									continue;

								if ((vi.Status & VersionStatus.ScheduledAdd) == VersionStatus.ScheduledAdd)
									toUnstage.Add (vi.LocalPath);
								else
									toCheckout.Add (vi.LocalPath);
							}
					} else {
						if (!TryGetVersionInfo (item, out var vi))
							continue;
						if (vi.Status == VersionStatus.Unversioned)
							continue;

						if ((vi.Status & VersionStatus.ScheduledAdd) == VersionStatus.ScheduledAdd)
							toUnstage.Add (vi.LocalPath);
						else
							toCheckout.Add (vi.LocalPath);
					}

				monitor.BeginTask (GettextCatalog.GetString ("Reverting files"), 1);

				await RunBlockingOperationAsync (group.Key, (repository, token) => {
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
				}, monitor.CancellationToken).ConfigureAwait (false);
				monitor.EndTask ();
			}
		}

		protected override Task OnRevertRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected override Task OnRevertToRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			throw new NotSupportedException ();
		}

		protected override async Task OnAddAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				var files = group.Where (f => !f.IsDirectory);
				if (files.Any ())
					await RunBlockingOperationAsync ((token) => LibGit2Sharp.Commands.Stage (group.Key, group.Key.ToGitPath (files)), monitor.CancellationToken).ConfigureAwait (false);
			}
		}

		protected override async Task OnDeleteFilesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
			await DeleteCore (localPaths, keepLocal, monitor).ConfigureAwait (false);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of files.
					VersionInfo info = await GetVersionInfoAsync (path, VersionInfoQueryFlags.IgnoreCache, monitor.CancellationToken).ConfigureAwait (false);
					if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
						// Revert addition.
						await RevertAsync (path, false, monitor);
					}
				} else {
					// Untracked files are not deleted by the rm command, so delete them now
					if (File.Exists (path))
						File.Delete (path);
				}
			}
		}

		protected override async Task OnDeleteDirectoriesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal)
		{
 			await DeleteCore (localPaths, keepLocal, monitor).ConfigureAwait (false);

			foreach (var path in localPaths) {
				if (keepLocal) {
					// Undo addition of directories and files.
					foreach (var info in await GetDirectoryVersionInfoAsync (path, false, true, monitor.CancellationToken).ConfigureAwait (false)) {
						if (info != null && info.HasLocalChange (VersionStatus.ScheduledAdd)) {
							// Revert addition.
							await RevertAsync (path, true, monitor).ConfigureAwait (false);
						}
					}
				}
			}

			if (!keepLocal) {
				// Untracked files are not deleted by the rm command, so delete them now
				foreach (var f in localPaths) {
					if (Directory.Exists (f)) {
						FileService.AssertCanDeleteDirectory (f, this.RootPath);
						Directory.Delete (f, true);
					}
				}
			}
		}

		async Task DeleteCore (FilePath[] localPaths, bool keepLocal, ProgressMonitor monitor)
		{
			foreach (var group in GroupByRepository (localPaths)) {
				if (!keepLocal)
					foreach (var f in localPaths) {
						if (File.Exists (f))
							File.Delete (f);
						else if (Directory.Exists (f)) {
							FileService.AssertCanDeleteDirectory (f, RootPath);
							Directory.Delete (f, true);
						}
					}

				await RunBlockingOperationAsync ((token) => {
					var files = group.Key.ToGitPath (group);
					LibGit2Sharp.Commands.Remove (group.Key, files, !keepLocal, null);
				}, monitor.CancellationToken).ConfigureAwait (false);
			}
		}

		protected override Task<string> OnGetTextAtRevisionAsync (FilePath repositoryPath, Revision revision, CancellationToken cancellationToken)
		{
			var gitRev = (GitRevision)revision;
			return RunOperationAsync (repositoryPath, (repository, token) => GetCommitTextContent (gitRev.GetCommit (repository), repositoryPath, repository));
		}

		public override Task<DiffInfo> GenerateDiffAsync (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			return RunOperationAsync (versionInfo.LocalPath, (repository, token) => {
				try {
					var patch = repository.Diff.Compare<Patch> (repository.Head?.Tip?.Tree, DiffTargets.WorkingDirectory | DiffTargets.Index, new [] { repository.ToGitPath (versionInfo.LocalPath) });
					// Trim the header by taking out the first 2 lines.
					int diffStart = patch.Content.IndexOf ('\n', patch.Content.IndexOf ('\n') + 1);
					return new DiffInfo (baseLocalPath, versionInfo.LocalPath, patch.Content.Substring (diffStart + 1));
				} catch (Exception ex) {
					LoggingService.LogError ("Could not get diff for file '" + versionInfo.LocalPath + "'", ex);
					return null;
				}
			});
		}

		public override async Task<DiffInfo []> PathDiffAsync (FilePath baseLocalPath, FilePath [] localPaths, bool remoteDiff, CancellationToken cancellationToken)
		{
			var diffs = new List<DiffInfo> ();
			VersionInfo[] vinfos = await GetDirectoryVersionInfoAsync (baseLocalPath, localPaths, false, true, cancellationToken).ConfigureAwait (false);
			foreach (VersionInfo vi in vinfos) {
				var diff = await GenerateDiffAsync (baseLocalPath, vi).ConfigureAwait (false);
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

		public async Task<string> GetCurrentRemoteAsync (CancellationToken cancellationToken = default)
		{
			using (LinkTokenToDispose (ref cancellationToken)) {
				var headRemote = await RunOperationAsync ((token) => RootRepository.Head?.RemoteName).ConfigureAwait (false);
				if (!string.IsNullOrEmpty (headRemote))
					return headRemote;

				var remotes = await GetRemoteNamesAsync (cancellationToken).ConfigureAwait (false);
				if (remotes.Count == 0)
					return null;

				return remotes.Contains ("origin") ? "origin" : remotes [0];
			}
		}

		public async Task PushAsync (ProgressMonitor monitor, string remote, string remoteBranch)
		{
			bool success = true;

			await RunOperationAsync ((token) => {
				var branch = RootRepository.Head;
				if (branch.TrackedBranch == null) {
					RootRepository.Branches.Update (branch, b => b.TrackedBranch = "refs/remotes/" + remote + "/" + remoteBranch);
				}
			}, monitor.CancellationToken).ConfigureAwait (false);

			var innerTask = await RunOperationAsync ((token) => {
				return RetryUntilSuccessAsync (monitor, credType => {
					RootRepository.Network.Push (RootRepository.Network.Remotes [remote], "refs/heads/" + remoteBranch, new PushOptions {
						OnPushStatusError = pushStatusErrors => success = false,
						CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType)
					});
					return Task.CompletedTask;
				});
			}, monitor.CancellationToken).ConfigureAwait (false);

			await innerTask.ConfigureAwait (false);

			if (!success)
				return;

			monitor.ReportSuccess (GettextCatalog.GetString ("Push operation successfully completed."));
		}

		public Task CreateBranchFromCommitAsync (string name, Commit id)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.CreateBranch (name, id));
		}

		public Task CreateBranchAsync (string name, string trackSource, string targetRef)
		{
			return RunBlockingOperationAsync ((token) => CreateBranch (name, trackSource, targetRef, RootRepository));
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

		public Task SetBranchTrackRefAsync (string name, string trackSource, string trackRef)
		{
			return RunBlockingOperationAsync ((token) => {
				var branch = RootRepository.Branches [name];
				if (branch != null) {
					RootRepository.Branches.Update (branch, bu => bu.TrackedBranch = trackRef);
				} else
					CreateBranch (name, trackSource, trackRef, RootRepository);
			});
		}

		public Task RemoveBranchAsync (string name)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.Branches.Remove (name));
		}

		public Task RenameBranchAsync (string name, string newName)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.Branches.Rename (name, newName, true));
		}

		public Task<List<string>> GetRemoteNamesAsync (CancellationToken cancellationToken = default)
		{
			// TODO: access to Remote props is not under our control
			return RunOperationAsync ((token) => RootRepository.Network.Remotes.Select (r => r.Name).ToList (), cancellationToken: cancellationToken);
		}

		public Task<IEnumerable<Remote>> GetRemotesAsync (CancellationToken cancellationToken = default)
		{
			// TODO: access to Remote props is not under our control
			return RunOperationAsync ((token) => RootRepository.Network.Remotes.Cast<Remote> (), cancellationToken:cancellationToken);
		}

		public Task<bool> IsBranchMergedAsync (string branchName)
		{
			// check if a branch is merged into HEAD
			return RunOperationAsync ((token) => {
				var tip = RootRepository.Branches [branchName].Tip.Sha;
				return RootRepository.Commits.Any (c => c.Sha == tip);
			});
		}

		public Task RenameRemoteAsync (string name, string newName)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.Network.Remotes.Rename (name, newName));
		}

		public Task ChangeRemoteUrlAsync (string name, string url)
		{
			return RunBlockingOperationAsync ((token) =>
				RootRepository.Network.Remotes.Update (
					name,
					r => r.Url = url
				));
		}

		public Task ChangeRemotePushUrlAsync (string name, string url)
		{
			return RunBlockingOperationAsync ((token) =>
				RootRepository.Network.Remotes.Update (
					name,
					r => r.PushUrl = url
				));
		}

		public Task AddRemoteAsync (string name, string url, bool importTags)
		{
			if (string.IsNullOrEmpty (name))
				throw new InvalidOperationException ("Name not set");

			return RunBlockingOperationAsync ((token) =>
				RootRepository.Network.Remotes.Update (RootRepository.Network.Remotes.Add (name, url).Name,
					r => r.TagFetchMode = importTags ? TagFetchMode.All : TagFetchMode.Auto));
		}

		public Task RemoveRemoteAsync (string name)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.Network.Remotes.Remove (name));
		}

		public Task<List<string>> GetLocalBranchNamesAsync (CancellationToken cancellationToken = default)
		{
			// TODO: access to Remote props is not under our control
			return RunOperationAsync ((token) => RootRepository.Branches.Where (b => !b.IsRemote).Select (b => b.FriendlyName).ToList (), cancellationToken: cancellationToken);
		}

		public Task<List<Branch>> GetBranchesAsync (CancellationToken cancellationToken = default)
		{
			// TODO: access to Remote props is not under our control
			return RunOperationAsync ((token) => RootRepository.Branches.Where (b => !b.IsRemote).ToList (), cancellationToken: cancellationToken);
		}

		public Task<List<string>> GetTagsAsync (CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => RootRepository.Tags.Select (t => t.FriendlyName).ToList (), cancellationToken: cancellationToken);
		}

		public Task AddTagAsync (string name, Revision rev, string message, CancellationToken cancellationToken = default)
		{
			Signature sig = GetSignature ();
			if (sig == null)
				return Task.CompletedTask;

			var gitRev = (GitRevision)rev;
			return RunBlockingOperationAsync ((token) => RootRepository.Tags.Add (name, gitRev.GetCommit (RootRepository), sig, message), cancellationToken: cancellationToken);
		}

		public Task RemoveTagAsync (string name, CancellationToken cancellationToken = default)
		{
			return RunBlockingOperationAsync ((token) => RootRepository.Tags.Remove (name), cancellationToken: cancellationToken);
		}

		public Task PushTagAsync (string name, CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => {
				return RetryUntilSuccessAsync (null, async credType => RootRepository.Network.Push (RootRepository.Network.Remotes [await GetCurrentRemoteAsync ().ConfigureAwait (false)], "refs/tags/" + name + ":refs/tags/" + name, new PushOptions {
					CredentialsProvider = (url, userFromUrl, types) => GitCredentials.TryGet (url, userFromUrl, types, credType),
				}));
			}, cancellationToken).Unwrap ();
		}

		public Task<List<string>> GetRemoteBranchesAsync (string remoteName, CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => RootRepository.Branches
				.Where (b => b.IsRemote && b.RemoteName == remoteName)
				.Select (b => b.FriendlyName.Substring (b.FriendlyName.IndexOf ('/') + 1)).ToList (), cancellationToken: cancellationToken);
		}

		public Task<List<string>> GetRemoteBranchFullNamesAsync (CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => RootRepository.Branches
				.Where (b => b.IsRemote).Select (b => b.FriendlyName).OrderBy (n => n).ToList (), cancellationToken: cancellationToken);
		}

		internal string GetCurrentBranch ()
		{
			return RunOperation (() => RootRepository.Head.FriendlyName);
		}

		internal string CachedCurrentBranch { get; private set; } = DefaultNoBranchName; //"(no branch)" is the default libgit string

		internal static string DefaultNoBranchName = "(no branch)"; //"(no branch)" is the default libgit string

		public Task<string> GetCurrentBranchAsync (CancellationToken cancellationToken = default)
		{
			return RunOperationAsync ((token) => CachedCurrentBranch = RootRepository.Head.FriendlyName, cancellationToken: cancellationToken);
		}

		async Task SwitchBranchInternalAsync (ProgressMonitor monitor, string branch)
		{
			int progress = 0;
			await RunOperationAsync ((token) => LibGit2Sharp.Commands.Checkout (RootRepository, branch, new CheckoutOptions {
				OnCheckoutProgress = (path, completedSteps, totalSteps) => OnCheckoutProgress (completedSteps, totalSteps, monitor, ref progress),
				OnCheckoutNotify = (string path, CheckoutNotifyFlags flags) => RefreshFile (path, flags),
				CheckoutNotifyFlags = refreshFlags,
			}), monitor.CancellationToken).ConfigureAwait (false);

			if (GitService.StashUnstashWhenSwitchingBranches) {
				try {
					// Restore the branch stash
					var stashIndex = RunOperation (() => GetStashForBranch (RootRepository.Stashes, branch));
					if (stashIndex != -1)
						PopStash (monitor, stashIndex);
				} catch (Exception e) {
					monitor.ReportError (GettextCatalog.GetString ("Restoring stash for branch {0} failed", branch), e);
				}
			}

			monitor.Step (1);
		}

		public async Task<bool> SwitchToBranchAsync (ProgressMonitor monitor, string branch)
		{
			Signature sig = GetSignature ();
			Stash stash;
			int stashIndex = -1;
			if (sig == null)
				return false;

			if (!WaitAndFreezeEvents (monitor.CancellationToken))
				return false;
			try {
				// try to switch without stashing
				monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), 2);
				await SwitchBranchInternalAsync (monitor, branch).ConfigureAwait (false);
				return true;
			} catch (CheckoutConflictException ex) {
				// retry with stashing
				monitor.EndTask ();
				if (!GitService.StashUnstashWhenSwitchingBranches) {
					if (!PromptToStash (
						GettextCatalog.GetString ("There are local changes that conflict with changes committed in the <b>{0}</b> branch. Would you like to stash the changes and continue with the checkout?", branch),
						GettextCatalog.GetString ("Stash and Switch"),
						GettextCatalog.GetString ("Automatically stash/unstash changes when switching branches"),
						GitService.StashUnstashWhenSwitchingBranches)) {
						// if canceled, report the error and return
						monitor.ReportError (GettextCatalog.GetString ("Switching to branch {0} failed", branch), ex);
						return false;
					}
				}

				// stash automatically is selected or user requested a stash

				monitor.BeginTask (GettextCatalog.GetString ("Switching to branch {0}", branch), 4);
				// Remove the stash for this branch, if exists
				// TODO: why do with do this?
				string currentBranch = RootRepository.Head.FriendlyName;
				stashIndex = await RunBlockingOperationAsync ((token) => GetStashForBranch (RootRepository.Stashes, currentBranch), cancellationToken: monitor.CancellationToken);
				if (stashIndex != -1)
					await RunBlockingOperationAsync ((token) => RootRepository.Stashes.Remove (stashIndex), cancellationToken: monitor.CancellationToken).ConfigureAwait (false);

				if (!TryCreateStash (monitor, GetStashName (currentBranch), out stash))
					return false;

				monitor.Step (1);

				try {
					await SwitchBranchInternalAsync (monitor, branch).ConfigureAwait (false);
					return true;
				} catch (Exception e) {
					monitor.ReportError (GettextCatalog.GetString ("Switching to branch {0} failed", branch), e);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Switching to branch {0} failed", branch), ex);
			} finally {
				monitor.EndTask ();
				ThawEvents ();
			}
			return false;
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

		protected override async Task OnMoveFileAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo vi = await GetVersionInfoAsync (localSrcPath, VersionInfoQueryFlags.IgnoreCache, monitor.CancellationToken).ConfigureAwait (false);
			if (vi == null || !vi.IsVersioned) {
				await base.OnMoveFileAsync (localSrcPath, localDestPath, force, monitor).ConfigureAwait (false);
				return;
			}

			var srcRepo = GetRepository (localSrcPath);
			var dstRepo = GetRepository (localDestPath);

			vi = await GetVersionInfoAsync (localDestPath, VersionInfoQueryFlags.IgnoreCache, monitor.CancellationToken).ConfigureAwait (false);
			await RunBlockingOperationAsync ((token) => {
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
			}, cancellationToken: monitor.CancellationToken).ConfigureAwait (false);
		}

		protected override async Task OnMoveDirectoryAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			VersionInfo[] versionedFiles = await GetDirectoryVersionInfoAsync (localSrcPath, false, true, monitor.CancellationToken).ConfigureAwait (false);
			await base.OnMoveDirectoryAsync (localSrcPath, localDestPath, force, monitor).ConfigureAwait (false);
			monitor.BeginTask (GettextCatalog.GetString ("Moving files"), versionedFiles.Length);
			foreach (VersionInfo vif in versionedFiles) {
				if (vif.IsDirectory)
					continue;
				FilePath newDestPath = vif.LocalPath.ToRelative (localSrcPath).ToAbsolute (localDestPath);
				await AddAsync (newDestPath, false, monitor).ConfigureAwait (false);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}

		object blameLock = new object ();

		public override Task<Annotation []> GetAnnotationsAsync (FilePath repositoryPath, Revision since, CancellationToken cancellationToken)
		{
			return RunOperationAsync (repositoryPath, async (repository, token) => {
				Commit hc = GetHeadCommit (repository);
				Commit sinceCommit = since != null ? ((GitRevision)since).GetCommit (repository) : null;
				if (hc == null)
					return Array.Empty<Annotation> ();

				var list = new List<Annotation> ();

				var gitPath = repository.ToGitPath (repositoryPath);
				var status = repository.RetrieveStatus (gitPath);
				if (status != FileStatus.NewInIndex && status != FileStatus.NewInWorkdir) {
					lock (blameLock) {
						foreach (var hunk in repository.Blame (gitPath, new BlameOptions { FindExactRenames = true, StartingAt = sinceCommit })) {
							var commit = hunk.FinalCommit;
							var author = hunk.FinalSignature;
							var working = new Annotation (new GitRevision (this, gitPath, commit), author.Name, author.When.LocalDateTime, String.Format ("<{0}>", author.Email));
							for (int i = 0; i < hunk.LineCount; ++i)
								list.Add (working);
						}
					}
				}

				if (sinceCommit == null) {
					var baseText = await GetBaseTextAsync (repositoryPath, token).ConfigureAwait (false);
					await Runtime.RunInMainThread (delegate {
						var baseDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (baseText);
						var workingDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (TextFileUtility.GetText (repositoryPath));
						var nextRev = new Annotation (null, GettextCatalog.GetString ("<uncommitted>"), DateTime.MinValue, null, GettextCatalog.GetString ("working copy"));
						int offsetStart = 0;
						foreach (var hunk in baseDocument.Diff (workingDocument, includeEol: false)) {
							list.RemoveRange (offsetStart + hunk.RemoveStart - 1, hunk.Removed);
							offsetStart -= hunk.Removed;
							for (int i = 0; i < hunk.Inserted; ++i) {
								if (hunk.InsertStart + i >= list.Count)
									list.Add (nextRev);
								else
									list.Insert (offsetStart + hunk.InsertStart - 1, nextRev);
								offsetStart++;
							}
						}
					}).ConfigureAwait (false);
				}

				return list.ToArray ();
			}, cancellationToken).Unwrap ();
		}

		protected override async Task OnIgnoreAsync (FilePath[] localPath, CancellationToken cancellationToken)
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

			var sb = StringBuilderCache.Allocate ();
			await RunBlockingOperationAsync ((token) => {
				foreach (var path in localPath.Except (ignored))
					sb.AppendLine (RootRepository.ToGitPath (path));

				File.AppendAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", StringBuilderCache.ReturnAndFree (sb));
				LibGit2Sharp.Commands.Stage (RootRepository, ".gitignore");
			}, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task OnUnignoreAsync (FilePath[] localPath, CancellationToken cancellationToken)
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
			await RunBlockingOperationAsync ((token) => {
				foreach (var path in ignored.Except (RootRepository.ToGitPath (localPath)))
					sb.AppendLine (path);

				File.WriteAllText (RootPath + Path.DirectorySeparatorChar + ".gitignore", sb.ToString ());
				LibGit2Sharp.Commands.Stage (RootRepository, ".gitignore");
			}, cancellationToken).ConfigureAwait (false);
		}
	}

	public class GitRevision: Revision
	{
		readonly string rev;

		internal FilePath FileForChanges { get; set; }

		public string GitRepository {
			get; private set;
		}

		public GitRevision (Repository repo, string gitRepository, Commit commit) : base(repo, commit?.Author.When.DateTime ?? DateTime.Now, commit?.Author.Name, commit?.Message)
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

		string shortName;
		public override string ShortName {
			get {
				if (shortName != null)
					return shortName;
				return shortName = rev.Length > 10 ? rev.Substring (0, 10) : rev;
			}
		}

		internal Commit GetCommit (LibGit2Sharp.Repository repository)
		{
			if (repository.Info.WorkingDirectory != GitRepository)
				throw new ArgumentException ("Commit does not belog to the repository", nameof (repository));
			return repository.Lookup<Commit> (rev);
		}

		public override Task<Revision> GetPreviousAsync (CancellationToken cancellationToken)
		{
			var repo = (GitRepository)Repository;
			return repo.RunOperationAsync (GitRepository, (repository, token) => GetPrevious (repository), cancellationToken: cancellationToken);
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
