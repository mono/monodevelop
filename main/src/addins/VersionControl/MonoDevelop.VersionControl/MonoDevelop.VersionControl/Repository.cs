
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Linq;
using System.Threading;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	[DataItem (FallbackType=typeof(UnknownRepository))]
	public abstract class Repository: IDisposable
	{
		string name;
		VersionControlSystem vcs;

		[ItemProperty ("VcsType")]
		string vcsName;

		int references;

		public FilePath RootPath
		{
			get;
			protected set;
		}

		internal FilePath RepositoryPath { get; set; }

		public event EventHandler NameChanged;

		protected Repository ()
		{
			infoCache = new VersionInfoCache (this);
		}

		protected Repository (VersionControlSystem vcs): this ()
		{
			VersionControlSystem = vcs;
		}

		public override bool Equals (object obj)
		{
			var other = obj as Repository;
			return other != null &&
				other.RootPath == RootPath &&
				other.VersionControlSystem == VersionControlSystem &&
				other.LocationDescription == LocationDescription &&
				other.Name == Name;
		}

		public override int GetHashCode ()
		{
			int result = 0;
			result ^= RootPath.GetHashCode ();
			if (VersionControlSystem != null)
				result ^= VersionControlSystem.GetHashCode ();
			if (LocationDescription != null)
				result ^= LocationDescription.GetHashCode ();
			result ^= Name.GetHashCode ();
			return result;
		}

		public virtual void CopyConfigurationFrom (Repository other)
		{
			name = other.name;
			vcsName = other.vcsName;
			vcs = other.vcs;
		}

		public Repository Clone ()
		{
			Repository res = VersionControlSystem.CreateRepositoryInstance ();
			res.CopyConfigurationFrom (this);
			return res;
		}

		internal void AddRef ()
		{
			references++;
		}

		internal void Unref ()
		{
			if (--references == 0)
				Dispose ();
		}

		public bool IsDisposed { get; protected set; }

		protected virtual void Dispose (bool disposing)
		{
			IsDisposed = true;
			if (disposing) {
				ShutdownScheduler ();
			}

			lock (VersionControlService.repositoryCacheLock) {
				VersionControlService.repositoryCache.Remove (RepositoryPath.CanonicalPath);
			}

			infoCache?.Dispose ();
			infoCache = null;
		}

		ConcurrentExclusiveSchedulerPair scheduler;

		TaskFactory exclusiveOperationFactory;
		protected TaskFactory ExclusiveOperationFactory { get { InitScheduler (); return exclusiveOperationFactory; } }

		TaskFactory concurrentOperationFactory;
		protected TaskFactory ConcurrentOperationFactory { get { InitScheduler (); return concurrentOperationFactory; } }

		protected void InitScheduler ()
		{
			if (IsDisposed)
				throw new ObjectDisposedException (nameof (Repository));

			if (scheduler == null) {
				scheduler = new ConcurrentExclusiveSchedulerPair ();
				exclusiveOperationFactory = new TaskFactory (scheduler.ExclusiveScheduler);
				concurrentOperationFactory = new TaskFactory (scheduler.ConcurrentScheduler);
			}
		}

		protected void ShutdownScheduler ()
		{
			if (scheduler != null) {
				scheduler.Complete ();
				scheduler.Completion.Wait ();
				scheduler = null;
				concurrentOperationFactory = null;
				exclusiveOperationFactory = null;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		~Repository ()
		{
			Dispose (false);
		}

		// Display name of the repository
		[ItemProperty]
		public string Name	{
			get { return name ?? string.Empty; }
			set {
				name = value;
				NameChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		// Description of the repository location (for example, the url)
		public virtual string LocationDescription {
			get { return name; }
		}

		// Version control system that manages this repository
		public VersionControlSystem VersionControlSystem {
			get {
				if (vcs == null && vcsName != null)
					vcs = VersionControlService.GetVersionControlSystems ().FirstOrDefault (v => v.Id == vcsName);
				return vcs;
			}

			internal set {
				vcs = value;
				vcsName = vcs.Id;
			}
		}

		// Return true if this repository is a root repository that contains other repositories
		public virtual bool HasChildRepositories {
			get { return false; }
		}

		// Returns the list of child repositories
		public virtual IEnumerable<Repository> ChildRepositories {
			get { return new Repository [0]; }
		}

		// Returns true if the user is allowed to modify files which have not been
		// explicitely locked
		public virtual bool AllowModifyUnlockedFiles {
			get { return true; }
		}

		// Returns true if this repository supports file locking
		public virtual bool AllowLocking {
			get { return true; }
		}

		public virtual bool SupportsRemoteStatus {
			get { return false; }
		}

		public virtual bool SupportsRevertRevision {
			get { return false; }
		}

		public virtual bool TryGetFileUpdateEventInfo (Repository rep, FilePath file, out FileUpdateEventInfo eventInfo)
		{
			eventInfo = new FileUpdateEventInfo (rep, file, false);
			return true;
		}

		public virtual bool SupportsRevertToRevision {
			get { return false; }
		}

		internal protected virtual Task<VersionControlOperation> GetSupportedOperationsAsync (VersionInfo vinfo, CancellationToken cancellationToken = default)
		{
			VersionControlOperation operations = VersionControlOperation.None;
			bool exists = !vinfo.LocalPath.IsNullOrEmpty && (File.Exists (vinfo.LocalPath) || Directory.Exists (vinfo.LocalPath));
			if (vinfo.IsVersioned) {
				operations = VersionControlOperation.Commit | VersionControlOperation.Update;
				if (!vinfo.HasLocalChange (VersionStatus.ScheduledAdd))
					operations |= VersionControlOperation.Log;

				if (exists) {
					if (!vinfo.HasLocalChange (VersionStatus.ScheduledDelete))
						operations |= VersionControlOperation.Remove;
					if (vinfo.HasLocalChanges || vinfo.IsDirectory)
						operations |= VersionControlOperation.Revert;
				}
				if (AllowLocking && !vinfo.IsDirectory) {
					if (!vinfo.HasLocalChanges && (vinfo.Status & VersionStatus.LockOwned) == 0)
						operations |= VersionControlOperation.Lock;
					if ((vinfo.Status & VersionStatus.LockOwned) != 0)
						operations |= VersionControlOperation.Unlock;
				}
			}
			else if (exists) {
				operations = VersionControlOperation.Add;
			}
			return Task.FromResult (operations);
		}

		// Returns the versioning status of a file or directory
		public async Task<VersionInfo> GetVersionInfoAsync (FilePath localPath, VersionInfoQueryFlags queryFlags = VersionInfoQueryFlags.None, CancellationToken cancellationToken = default)
		{
			var infos = await GetVersionInfoAsync (new FilePath [] { localPath }, queryFlags, cancellationToken);
			if (infos.Count != 1) {
				LoggingService.LogError ("VersionControl returned {0} items for {1}", infos.Count, localPath);
				LoggingService.LogError ("The infos were: {0}", string.Join (" ::: ", infos.Select (i => i.LocalPath)));
			}
			// HACK: This was slowing down the IDE a lot in case in the eventuality of submodules.
			return infos [0];
			/*try {
                return infos.Single ();
            } catch (InvalidOperationException) {
                // Workaround for #17216.
                return infos [0];
            }*/
		}


		public bool TryGetVersionInfo (FilePath localPath, out VersionInfo info)
		{
			return TryGetVersionInfo (localPath, VersionInfoQueryFlags.None, out info);
		}

		public bool TryGetVersionInfo (FilePath localPath, VersionInfoQueryFlags queryFlags, out VersionInfo info)
		{
			var result = TryGetVersionInfo (new FilePath [] { localPath }, queryFlags, out var infos);
			if (!result || infos == null) {
				info = null;
				return false;
			}
			if (infos.Count != 1) {
				LoggingService.LogError ("VersionControl returned {0} items for {1}", infos.Count, localPath);
				LoggingService.LogError ("The infos were: {0}", string.Join (" ::: ", infos.Select (i => i.LocalPath)));
			}
			info = infos [0];
			return result;
		}

		/// <summary>
		/// Returns the versioning status of a set of files or directories
		/// </summary>
		/// <param name='paths'>
		/// A list of files or directories
		/// </param>
		/// <param name='queryFlags'>
		/// Use VersionInfoQueryFlags enum for options.
		/// </param>
		public async Task<IReadOnlyList<VersionInfo>> GetVersionInfoAsync (IEnumerable<FilePath> paths, VersionInfoQueryFlags queryFlags = VersionInfoQueryFlags.None, CancellationToken cancellationToken = default)
		{
			if ((queryFlags & VersionInfoQueryFlags.IgnoreCache) != 0) {
				// We shouldn't use IEnumerable because elements don't save property modifications.
				var res = await OnGetVersionInfoAsync (paths, (queryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0, cancellationToken);
				foreach (var vi in res)
					if (!vi.IsInitialized) await vi.InitAsync (this, cancellationToken);
				await infoCache.SetStatusAsync (res, cancellationToken: cancellationToken);
				return res;
			}
			var pathsToQuery = new List<FilePath> ();
			var result = new List<VersionInfo> ();
			foreach (var path in paths) {
				var vi = infoCache.GetStatus (path);
				if (vi != null) {
					result.Add (vi);
					// This status has been invalidated, query it asynchronously
					if (vi.RequiresRefresh)
						pathsToQuery.Add (path);
				}
				else {
					// If there is no cached status, query it asynchronously
					vi = new VersionInfo (path, "", Directory.Exists (path), VersionStatus.Versioned, null, VersionStatus.Versioned, null);
					await infoCache.SetStatusAsync (vi, false);
					result.Add (vi);
					pathsToQuery.Add (path);
				}
//				Console.WriteLine ("GetVersionInfo " + string.Join (", ", paths.Select (p => p.FullPath)));
			}
			if (pathsToQuery.Count > 0) {
				ConcurrentOperationFactory.StartNew (async delegate {
					var status = await OnGetVersionInfoAsync (pathsToQuery, (queryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0, cancellationToken);
					foreach (var vi in status) {
						if (!vi.IsInitialized) {
							await vi.InitAsync (this, cancellationToken);
						}
					}
					await infoCache.SetStatusAsync (status, cancellationToken);
				}).Ignore ();
			}
			return result;
		}

		public bool TryGetVersionInfo (IEnumerable<FilePath> paths, out IReadOnlyList<VersionInfo> infos)
		{
			return TryGetVersionInfo (paths, VersionInfoQueryFlags.None, out infos);
		}

		public bool TryGetVersionInfo (IEnumerable<FilePath> paths, VersionInfoQueryFlags queryFlags, out IReadOnlyList<VersionInfo> infos)
		{
			var result = new List<VersionInfo> ();
			var pathsToQuery = new List<FilePath> ();
			bool getVersionInfoFailed = false;
			foreach (var path in paths) {
				var vi = infoCache.GetStatus (path);
				if (vi != null) {
					result.Add (vi);
					// This status has been invalidated, query it asynchronously
					if (vi.RequiresRefresh)
						pathsToQuery.Add (path);
				} else {
					pathsToQuery.Add (path);
					getVersionInfoFailed = true;
				}
			}
			if (pathsToQuery.Count > 0) {
				ConcurrentOperationFactory.StartNew (async delegate {
					var status = await OnGetVersionInfoAsync (paths, (queryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0);
					foreach (var vi in status) {
						if (!vi.IsInitialized) {
							await vi.InitAsync (this);
						}
					}
					await infoCache.SetStatusAsync (status);
				}).Ignore ();
			}
			if (getVersionInfoFailed) {
				infos = null;
				return false;
			}
			infos = result;
			return true;
		}

		public async Task<VersionInfo[]> GetDirectoryVersionInfoAsync (FilePath localDirectory, bool getRemoteStatus, bool recursive, CancellationToken cancellationToken = default)
		{
			try {
				if (recursive) {
					return await OnGetDirectoryVersionInfoAsync (localDirectory, getRemoteStatus, true, cancellationToken);
				}

				var status = infoCache.GetDirectoryStatus (localDirectory);
				if (status != null && !status.RequiresRefresh && (!getRemoteStatus || status.HasRemoteStatus))
					return status.FileInfo;

				var directoryVersionInfo = await OnGetDirectoryVersionInfoAsync (localDirectory, getRemoteStatus, false, cancellationToken).ConfigureAwait (false);
				foreach (var vi in directoryVersionInfo)
					if (!vi.IsInitialized) await vi.InitAsync (this, cancellationToken);
				await infoCache.SetDirectoryStatusAsync (localDirectory, directoryVersionInfo, getRemoteStatus, cancellationToken);

				// If we have a status value but the value was invalidated (RequiresRefresh == true)
				// then we return the invalidated value while we start an async query to get the new one

				if (status != null && status.RequiresRefresh && (!getRemoteStatus || status.HasRemoteStatus))
					return status.FileInfo;
				return Array.Empty<VersionInfo> ();
			} finally {
				//Console.WriteLine ("GetDirectoryVersionInfo " + localDirectory + " - " + (DateTime.Now - now).TotalMilliseconds);
			}
		}

		public void ClearCachedVersionInfo (FilePath path)
		{
			infoCache?.ClearCachedVersionInfo (path);
		}

		public void ClearCachedVersionInfo (params FilePath [] paths)
		{
			foreach (var p in paths)
				infoCache?.ClearCachedVersionInfo (p);
		}

		class VersionInfoQuery
		{
			public List<FilePath> Paths;
			public VersionInfoQueryFlags QueryFlags;
		}

		class DirectoryInfoQuery
		{
			public FilePath Directory;
			public bool GetRemoteStatus;
		}

		bool queryRunning;
		VersionInfoCache infoCache;

		/// <summary>
		/// Returns the list of changes done in the given revision
		/// </summary>
		/// <param name='revision'>
		/// A revision
		/// </param>
		public async Task<RevisionPath []> GetRevisionChangesAsync (Revision revision, CancellationToken cancellationToken = default)
		{
			using (var tracker = Instrumentation.GetRevisionChangesCounter.BeginTiming (new RepositoryMetadata (VersionControlSystem))) {
				try {
					return await OnGetRevisionChangesAsync (revision, cancellationToken);
				} catch {
					tracker.Metadata.SetFailure ();
					throw;
				}
			}
		}


		// Returns the content of the file in the base revision of the working copy.
		public abstract Task<string> GetBaseTextAsync (FilePath localFile, CancellationToken cancellationToken = default);

		// Returns the revision history of a file
		public Task<Revision[]> GetHistoryAsync (FilePath localFile, Revision since, CancellationToken cancellationToken = default)
		{
			using (var tracker = Instrumentation.GetHistoryCounter.BeginTiming (new RepositoryMetadata (VersionControlSystem))) {
				try {
					return OnGetHistoryAsync (localFile, since, cancellationToken);
				} catch {
					tracker.Metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task<Revision []> OnGetHistoryAsync (FilePath localFile, Revision since, CancellationToken cancellationToken);

		/// <summary>
		/// Returns the versioning status of a set of files or directories
		/// </summary>
		/// <param name='paths'>
		/// A list of files or directories
		/// </param>
		/// <param name='getRemoteStatus'>
		/// True if remote status information has to be included
		/// </param>
		/// <remarks>
		/// This method must return a VersionInfo object for every path in the 'paths' argument.
		/// </remarks>
		protected abstract Task<IReadOnlyList<VersionInfo>> OnGetVersionInfoAsync (IEnumerable<FilePath> paths, bool getRemoteStatus, CancellationToken cancellationToken = default);

		// Returns the versioning status of all files in a directory
		protected abstract Task<VersionInfo []> OnGetDirectoryVersionInfoAsync (FilePath localDirectory, bool getRemoteStatus, bool recursive, CancellationToken cancellationToken);

		// Imports a directory into the repository. 'serverPath' is the relative path in the repository.
		// 'localPath' is the local directory to publish. 'files' is the list of files to add to the new
		// repository directory (must use absolute local paths).
		public async Task<Repository> PublishAsync (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
		{
			var metadata = new PublishMetadata (VersionControlSystem) { PathsCount = files.Length, SubFolder = !RootPath.IsNullOrEmpty && localPath.IsChildPathOf(RootPath) };
			using (var tracker = Instrumentation.PublishCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					var res = await OnPublishAsync (serverPath, localPath, files, message, monitor);
					ClearCachedVersionInfo (localPath);
					return res;
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task<Repository> OnPublishAsync (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor);

		// Updates a local file or directory from the repository
		// Returns a list of updated files
		public Task UpdateAsync (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			return UpdateAsync (new FilePath[] { localPath }, recurse, monitor);
		}

		public async Task UpdateAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPaths.Length, Recursive = recurse };
			using (var tracker = Instrumentation.UpdateCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnUpdateAsync (localPaths, recurse, monitor);
					ClearCachedVersionInfo (localPaths);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnUpdateAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor);

		// Called to create a ChangeSet to be used for a commit operation
		public virtual ChangeSet CreateChangeSet (FilePath basePath)
		{
			return new ChangeSet (this, basePath);
		}

		/// <summary>
		/// Creates a patch from a set of DiffInfos.
		/// </summary>
		public virtual string CreatePatch (IEnumerable<DiffInfo> diffs)
		{
			StringBuilder patch = new StringBuilder ();

			if (null != diffs) {
				foreach (DiffInfo diff in diffs) {
					if (!string.IsNullOrWhiteSpace (diff.Content))
						patch.AppendLine (diff.Content);
				}
			}

			return patch.ToString ();
		}

		// Commits changes in a set of files or directories into the repository
		public async Task CommitAsync (ChangeSet changeSet, ProgressMonitor monitor)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = changeSet.Count };
			using (var tracker = Instrumentation.CommitCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (changeSet.BaseLocalPath);
					await OnCommitAsync (changeSet, monitor);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnCommitAsync (ChangeSet changeSet, ProgressMonitor monitor);

		// Gets the contents of this repositories into the specified local path
		public Task CheckoutAsync (FilePath targetLocalPath, bool recurse, ProgressMonitor monitor)
		{
			return CheckoutAsync (targetLocalPath, null, recurse, monitor);
		}

		public async Task CheckoutAsync (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { Recursive = recurse };
			using (var tracker = Instrumentation.CheckoutCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (targetLocalPath);
					await OnCheckoutAsync (targetLocalPath, rev, recurse, monitor);

					if (!Directory.Exists (targetLocalPath))
						metadata.SetFailure ();
					else
						metadata.SetSuccess ();
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnCheckoutAsync (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor);

		public async Task RevertAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			var metadata = new RevertMetadata (VersionControlSystem) { PathsCount = localPaths.Length, Recursive = recurse, OperationType = RevertMetadata.RevertType.LocalChanges };
			using (var tracker = Instrumentation.RevertCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnRevertAsync (localPaths, recurse, monitor);
					ClearCachedVersionInfo (localPaths);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		public Task RevertAsync (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			return RevertAsync (new FilePath[] { localPath }, recurse, monitor);
		}

		protected abstract Task OnRevertAsync (FilePath [] localPaths, bool recurse, ProgressMonitor monitor);

		public async Task RevertRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			var metadata = new RevertMetadata (VersionControlSystem) { PathsCount = 1, Recursive = true, OperationType = RevertMetadata.RevertType.SpecificRevision };
			using (var tracker = Instrumentation.RevertCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (localPath);
					await OnRevertRevisionAsync (localPath, revision, monitor);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnRevertRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor);

		public async Task RevertToRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			var metadata = new RevertMetadata (VersionControlSystem) { PathsCount = 1, Recursive = true, OperationType = RevertMetadata.RevertType.ToRevision };
			using (var tracker = Instrumentation.RevertCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (localPath);
					await OnRevertToRevisionAsync (localPath, revision, monitor);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnRevertToRevisionAsync (FilePath localPath, Revision revision, ProgressMonitor monitor);

		// Adds a file or directory to the repository
		public void Add (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			AddAsync (new FilePath [] { localPath }, recurse, monitor).Ignore ();
		}

		public Task AddAsync (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			return AddAsync (new FilePath [] { localPath }, recurse, monitor);
		}

		public async Task AddAsync (FilePath [] localPaths, bool recurse, ProgressMonitor monitor)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPaths.Length, Recursive = recurse };
			using (var tracker = Instrumentation.AddCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnAddAsync (localPaths, recurse, monitor);
				} catch (Exception e) {
					LoggingService.LogError ("Failed to add file", e);
					metadata.SetFailure ();
				} finally {
					ClearCachedVersionInfo (localPaths);
				}
			}
		}

		protected abstract Task OnAddAsync (FilePath[] localPaths, bool recurse, ProgressMonitor monitor);

		// Returns true if the file can be moved from source location (and repository) to this repository
		public virtual bool CanMoveFilesFrom (Repository srcRepository, FilePath localSrcPath, FilePath localDestPath)
		{
			return srcRepository == this;
		}

		// Moves a file. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file move.
		// It's up to the implementation to decide how smart the MoveFile method is.
		// For example, when moving a file to an unversioned directory, the implementation
		// might just throw an exception, or it could version the directory, or it could
		// ask the user what to do.
		public async Task MoveFileAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			var metadata = new MoveMetadata (VersionControlSystem) { Force = force, OperationType = MoveMetadata.MoveType.File };
			using (var tracker = Instrumentation.MoveCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnMoveFileAsync (localSrcPath, localDestPath, force, monitor);
				} catch (Exception e) {
					LoggingService.LogError ("Failed to move file", e);
					metadata.SetFailure ();
					File.Move (localSrcPath, localDestPath);
				}
			}
		}

		protected virtual Task OnMoveFileAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			File.Move (localSrcPath, localDestPath);
			return Task.CompletedTask;
		}

		// Moves a directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file move.
		public async Task MoveDirectoryAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			var metadata = new MoveMetadata (VersionControlSystem) { Force = force, OperationType = MoveMetadata.MoveType.Directory };
			using (var tracker = Instrumentation.MoveCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnMoveDirectoryAsync (localSrcPath, localDestPath, force, monitor);
				} catch (Exception e) {
					LoggingService.LogError ("Failed to move directory", e);
					metadata.SetFailure ();
					FileService.SystemDirectoryRename (localSrcPath, localDestPath);
				}
			}
		}

		protected virtual Task OnMoveDirectoryAsync (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			FileService.SystemDirectoryRename (localSrcPath, localDestPath);
			return Task.CompletedTask;
		}

		// Deletes a file or directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file delete.
		public Task DeleteFileAsync (FilePath localPath, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			return DeleteFilesAsync (new FilePath[] { localPath }, force, monitor, keepLocal);
		}

		public async Task DeleteFilesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			FileUpdateEventArgs args = new FileUpdateEventArgs ();
			var metadata = new DeleteMetadata (VersionControlSystem) { PathsCount = localPaths.Length, Force = force, KeepLocal = keepLocal };
			using (var tracker = Instrumentation.DeleteCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnDeleteFilesAsync (localPaths, force, monitor, keepLocal);
					foreach (var path in localPaths)
						args.Add (new FileUpdateEventInfo (this, path, false));
				} catch (Exception e) {
					LoggingService.LogError ("Failed to delete file", e);
					metadata.SetFailure ();
					if (!keepLocal)
						foreach (var path in localPaths)
							File.Delete (path);
				}
			}
			ClearCachedVersionInfo (localPaths);
			if (args.Any ())
				VersionControlService.NotifyFileStatusChanged (args);
		}

		protected abstract Task OnDeleteFilesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal);

		public void DeleteDirectory (FilePath localPath, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			DeleteDirectoriesAsync (new FilePath[] { localPath }, force, monitor, keepLocal).Ignore ();
		}

		public async Task DeleteDirectoriesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			FileUpdateEventArgs args = new FileUpdateEventArgs ();
			var metadata = new DeleteMetadata (VersionControlSystem) { PathsCount = localPaths.Length, Force = force, KeepLocal = keepLocal };
			using (var tracker = Instrumentation.DeleteCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					await OnDeleteDirectoriesAsync (localPaths, force, monitor, keepLocal);
					foreach (var path in localPaths)
						args.Add (new FileUpdateEventInfo (this, path, true));
				} catch (Exception e) {
					LoggingService.LogError ("Failed to delete directory", e);
					metadata.SetFailure ();
					if (!keepLocal)
						foreach (var path in localPaths)
							Directory.Delete (path, true);
				}
			}
			ClearCachedVersionInfo (localPaths);
			if (args.Any ())
				VersionControlService.NotifyFileStatusChanged (args);
		}

		protected abstract Task OnDeleteDirectoriesAsync (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal);

		// Called to request write permission for a file. The file may not yet exist.
		// After the file is modified or created, NotifyFileChanged is called.
		// This method is allways called for versioned and unversioned files.
		public virtual Task<bool> RequestFileWritePermissionAsync (params FilePath [] paths)
		{
			return Task.FromResult (true);
		}

		// Called after a file has been modified.
		// This method is always called for versioned and unversioned files.
		public void NotifyFileChanged (FilePath path)
		{
			ClearCachedVersionInfo (path);
			OnNotifyFileChanged (path);
		}

		// Called after a file has been modified.
		// This method is always called for versioned and unversioned files.
		protected virtual void OnNotifyFileChanged (FilePath path)
		{
		}

		// Locks a file in the repository so no other users can change it
		public void Lock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPaths.Length };
			using (var tracker = Instrumentation.LockCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (localPaths);
					OnLock (monitor, localPaths);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		// Locks a file in the repository so no other users can change it
		protected virtual void OnLock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}

		// Unlocks a file in the repository so other users can change it
		public void Unlock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPaths.Length };
			using (var tracker = Instrumentation.UnlockCounter.BeginTiming (metadata, monitor.CancellationToken)) {
				try {
					ClearCachedVersionInfo (localPaths);
					OnUnlock (monitor, localPaths);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected virtual void OnUnlock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}

		public virtual DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			return null;
		}

		// Returns a diff description between local files and the remote files.
		// baseLocalPath is the root path of the diff. localPaths is optional and
		// it can be a list of files to compare.
		public DiffInfo[] PathDiff (ChangeSet cset, bool remoteDiff)
		{
			return PathDiff (cset.BaseLocalPath, cset.Items.Select (i => i.LocalPath).ToArray (), remoteDiff);
		}

		/// <summary>
		/// Returns a recursive diff set for a revision range.
		/// </summary>
		/// <param name="localPath">
		/// A <see cref="FilePath"/>: A local file path to diff;
		/// directories will be diffed recursively.
		/// </param>
		/// <param name="fromRevision">
		/// A <see cref="Revision"/>: The beginning revision
		/// </param>
		/// <param name="toRevision">
		/// A <see cref="Revision"/>: The ending revision
		/// </param>
		public virtual DiffInfo[] PathDiff (FilePath localPath, Revision fromRevision, Revision toRevision)
		{
			return new DiffInfo [0];
		}

		public virtual DiffInfo[] PathDiff (FilePath baseLocalPath, FilePath[] localPaths, bool remoteDiff)
		{
			return new DiffInfo [0];
		}

		public Task<string> GetTextAtRevisionAsync (FilePath repositoryPath, Revision revision, CancellationToken cancellationToken = default)
		{
			return OnGetTextAtRevisionAsync (repositoryPath, revision, cancellationToken);
		}

		protected abstract Task<string> OnGetTextAtRevisionAsync (FilePath repositoryPath, Revision revision, CancellationToken cancellationToken);

		static protected DiffInfo[] GenerateUnifiedDiffInfo (string diffContent, FilePath basePath, FilePath[] localPaths)
		{
			basePath = basePath.FullPath;
			var list = new List<DiffInfo> ();
			using (StringReader sr = new StringReader (diffContent)) {
				string line;
				StringBuilder content = new StringBuilder ();
				string fileName = null;
				string pathRoot = null;

				while ((line = sr.ReadLine ()) != null) {
					if (pathRoot != null && fileName != null &&
						(line.StartsWith ("+++ " + pathRoot, StringComparison.Ordinal) || line.StartsWith ("--- " + pathRoot, StringComparison.Ordinal))) {
						line = line.Substring (0, 4) + line.Substring (4 + pathRoot.Length);
						content.Append (line).Append ('\n');
					}
					else if (!line.StartsWith ("Index:", StringComparison.Ordinal)) {
						content.Append (line).Append ('\n');
					} else {
						if (fileName != null) {
							list.Add (new DiffInfo (basePath, fileName, content.ToString ()));
						}
						fileName = line.Substring (6).Trim ();
						fileName = fileName.Replace ('/', Path.DirectorySeparatorChar); // svn returns paths using unix separators
						FilePath fp = fileName;
						pathRoot = null;
						if (fp.IsAbsolute) {
							if (fp == basePath)
								pathRoot = fp.ParentDirectory;
							else if (fp.IsChildPathOf (basePath))
								pathRoot = basePath;
							if (pathRoot != null) {
								pathRoot = pathRoot.Replace (Path.DirectorySeparatorChar, '/').TrimEnd ('/');
								pathRoot += '/';
							}
						}
						else {
							fp = fp.ToAbsolute (basePath);
						}
						fileName = fp;
						content = new StringBuilder ();
						line = sr.ReadLine ();	// "===" Separator

						// Filter out files not in the provided path list
						if (localPaths != null && Array.IndexOf (localPaths, (FilePath) fileName) == -1)
							fileName = null;
					}
				}
				if (fileName != null) {
					list.Add (new DiffInfo (basePath, fileName, content.ToString ()));
				}
			}
			return list.ToArray ();
		}

		/// <summary>
		/// Retrieves annotations for a given path in the repository.
		/// </summary>
		/// <param name="repositoryPath">
		/// A <see cref="FilePath"/>
		/// </param>
		/// <param name="since">
		/// A <see cref="Revision"/>
		/// </param>
		/// <returns>
		/// A <see cref="Annotation"/> corresponding to each line
		/// of the file to which repositoryPath points.
		/// </returns>
		public virtual Task<Annotation []> GetAnnotationsAsync (FilePath repositoryPath, Revision since, CancellationToken cancellationToken = default)
		{
			return Task.FromResult (new Annotation[0]);
		}

		/// <summary>
		/// Returns the list of changes done in the given revision
		/// </summary>
		/// <param name='revision'>
		/// A revision
		/// </param>
		protected abstract Task<RevisionPath []> OnGetRevisionChangesAsync (Revision revision, CancellationToken cancellationToken);

		// Ignores a file for version control operations.
		public async Task IgnoreAsync (FilePath[] localPath, CancellationToken cancellationToken = default)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPath.Length };
			using (var tracker = Instrumentation.IgnoreCounter.BeginTiming (metadata)) {
				try {
					ClearCachedVersionInfo (localPath);
					await OnIgnoreAsync (localPath, cancellationToken);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnIgnoreAsync (FilePath[] localPath, CancellationToken cancellationToken);

		// Unignores a file for version control operations.
		public async Task UnignoreAsync (FilePath[] localPath, CancellationToken cancellationToken = default)
		{
			var metadata = new MultipathOperationMetadata (VersionControlSystem) { PathsCount = localPath.Length };
			using (var tracker = Instrumentation.UnignoreCounter.BeginTiming (metadata)) {
				try {
					ClearCachedVersionInfo (localPath);
					await OnUnignoreAsync (localPath, cancellationToken);
				} catch {
					metadata.SetFailure ();
					throw;
				}
			}
		}

		protected abstract Task OnUnignoreAsync (FilePath[] localPath, CancellationToken cancellationToken);

		public virtual bool GetFileIsText (FilePath path)
		{
			return IdeServices.DesktopService.GetFileIsText (path);
		}
	}

	public class Annotation
	{
		public Revision Revision {
			get;
			private set;
		}

		string text;
		public string Text {
			get { return text ?? Revision?.ToString (); }
		}

		public string Author {
			get;
			private set;
		}

		public DateTime Date {
			get;
			private set;
		}

		public string Email {
			get;
			private set;
		}

		public bool HasEmail {
			get { return Email != null; }
		}

		public bool HasDate {
			get { return Date != DateTime.MinValue; }
		}

		public Annotation (Revision revision, string author, DateTime date) : this (revision, author, date, null)
		{
		}

		public Annotation (Revision revision, string author, DateTime date, string email) : this (revision, author, date, email, null)
		{
		}

		public Annotation (Revision revision, string author, DateTime date, string email, string text)
		{
			Revision = revision;
			Author = author;
			Date = date;
			Email = email;
			this.text = text;
		}

		public override string ToString ()
		{
			return string.Format ("[Annotation: Revision={0}, Author={1}, Date={2}, HasDate={3}, Email={4}, HasEmail={5}]",
									Revision, Author, Date, HasDate, Email, HasEmail);
		}
	}

	public class DiffInfo
	{
		public DiffInfo (FilePath basePath, FilePath fileName, string content)
		{
			BasePath = basePath;
			FileName = fileName;
			Content = content.Replace ("\r","");
		}

		public FilePath FileName {
			get;
			private set;
		}

		public string Content {
			get;
			private set;
		}

		public FilePath BasePath {
			get;
			private set;
		}
	}

	[Flags]
	public enum VersionInfoQueryFlags
	{
		None = 0,
		IgnoreCache = 1,
		IncludeRemoteStatus = 2
	}
}
