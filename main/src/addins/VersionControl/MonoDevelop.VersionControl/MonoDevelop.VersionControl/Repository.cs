
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

		internal bool Disposed { get; private set; }
		public virtual void Dispose ()
		{
			Disposed = true;
			if (!queryRunning)
				return;

			lock (queryLock) {
				fileQueryQueue.Clear ();
				directoryQueryQueue.Clear ();
				recursiveDirectoryQueryQueue.Clear ();
			}
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

		public virtual bool SupportsRevertToRevision {
			get { return false; }
		}
		
		internal protected virtual VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
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
			return operations;
		}

		// Returns the versioning status of a file or directory
		public VersionInfo GetVersionInfo (FilePath localPath, VersionInfoQueryFlags queryFlags = VersionInfoQueryFlags.None)
		{
			VersionInfo[] infos = GetVersionInfo (new FilePath[] { localPath }, queryFlags).ToArray ();
			if (infos.Length != 1) {
				LoggingService.LogError ("VersionControl returned {0} items for {1}", infos.Length, localPath);
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
		
		/// <summary>
		/// Returns the versioning status of a set of files or directories
		/// </summary>
		/// <param name='paths'>
		/// A list of files or directories
		/// </param>
		/// <param name='queryFlags'>
		/// Use VersionInfoQueryFlags enum for options.
		/// </param>
		public IEnumerable<VersionInfo> GetVersionInfo (IEnumerable<FilePath> paths, VersionInfoQueryFlags queryFlags = VersionInfoQueryFlags.None)
		{
			if ((queryFlags & VersionInfoQueryFlags.IgnoreCache) != 0) {
				// We shouldn't use IEnumerable because elements don't save property modifications.
				var res = OnGetVersionInfo (paths, (queryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0).ToList ();
				infoCache.SetStatus (res);
				return res;
			}
			List<FilePath> pathsToQuery = new List<FilePath> ();
			var result = new List<VersionInfo> ();
			foreach (var p in paths) {
				var vi = infoCache.GetStatus (p);
				if (vi != null) {
					result.Add (vi);
					// This status has been invalidated, query it asynchronously
					if (vi.RequiresRefresh)
						pathsToQuery.Add (p);
				}
				else {
					// If there is no cached status, query it asynchronously
					vi = new VersionInfo (p, "", Directory.Exists (p), VersionStatus.Versioned, null, VersionStatus.Versioned, null);
					infoCache.SetStatus (vi, false);
					result.Add (vi);
					pathsToQuery.Add (p);
				}
//				Console.WriteLine ("GetVersionInfo " + string.Join (", ", paths.Select (p => p.FullPath)));
			}
			if (pathsToQuery.Count > 0)
				AddQuery (new VersionInfoQuery () { Paths = pathsToQuery, QueryFlags = queryFlags });
			return result;
		}

		RecursiveDirectoryInfoQuery AcquireLockForQuery (FilePath path, bool getRemoteStatus)
		{
			RecursiveDirectoryInfoQuery rq;
			bool query = false;
			lock (queryLock) {
				rq = recursiveDirectoryQueryQueue.FirstOrDefault (q => q.Directory == path);
				if (rq == null) {
					query = true;
					var mre = new ManualResetEvent (false);
					rq = new RecursiveDirectoryInfoQuery {
						Directory = path,
						GetRemoteStatus = getRemoteStatus,
						ResetEvent = mre,
						Count = 1,
					};
				} else
					Interlocked.Increment (ref rq.Count);
			}
			if (query)
				AddQuery (rq);
			return rq;
		}

		public VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			try {
				if (recursive) {
					RecursiveDirectoryInfoQuery rq = AcquireLockForQuery (localDirectory, getRemoteStatus);
					rq.ResetEvent.WaitOne ();

					lock (queryLock)
						if (Interlocked.Decrement (ref rq.Count) == 0)
							rq.ResetEvent.Dispose ();
					return rq.Result;
				}

				var status = infoCache.GetDirectoryStatus (localDirectory);
				if (status != null && !status.RequiresRefresh && (!getRemoteStatus || status.HasRemoteStatus))
					return status.FileInfo;

				// If there is no cached status, query it asynchronously
				DirectoryInfoQuery q = new DirectoryInfoQuery () {
					Directory = localDirectory,
					GetRemoteStatus = getRemoteStatus
				};
				AddQuery (q);

				// If we have a status value but the value was invalidated (RequiresRefresh == true)
				// then we return the invalidated value while we start an async query to get the new one

				if (status != null && status.RequiresRefresh && (!getRemoteStatus || status.HasRemoteStatus))
					return status.FileInfo;
				return new VersionInfo[0];
			} finally {
				//Console.WriteLine ("GetDirectoryVersionInfo " + localDirectory + " - " + (DateTime.Now - now).TotalMilliseconds);
			}
		}

		public void ClearCachedVersionInfo (params FilePath[] paths)
		{
			foreach (var p in paths)
				infoCache.ClearCachedVersionInfo (p);
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

		class RecursiveDirectoryInfoQuery : DirectoryInfoQuery
		{
			public VersionInfo[] Result = new VersionInfo[0];
			public ManualResetEvent ResetEvent;
			public int Count;
		}

		Queue<VersionInfoQuery> fileQueryQueue = new Queue<VersionInfoQuery> ();
		Queue<DirectoryInfoQuery> directoryQueryQueue = new Queue<DirectoryInfoQuery> ();
		Queue<RecursiveDirectoryInfoQuery> recursiveDirectoryQueryQueue = new Queue<RecursiveDirectoryInfoQuery> ();
		object queryLock = new object ();
		bool queryRunning;
		VersionInfoCache infoCache;
		HashSet<FilePath> filesInQueryQueue = new HashSet<FilePath> ();
		HashSet<FilePath> directoriesInQueryQueue = new HashSet<FilePath> ();
		HashSet<FilePath> recursiveDirectoriesInQueryQueue = new HashSet<FilePath> ();

		void AddQuery (object query)
		{
			lock (queryLock) {
				if (query is VersionInfoQuery) {
					VersionInfoQuery vi = (VersionInfoQuery)query;
					vi.Paths.RemoveAll (p => filesInQueryQueue.Contains (p) || directoriesInQueryQueue.Contains (p.ParentDirectory));
					if (vi.Paths.Count == 0)
						return;
					filesInQueryQueue.UnionWith (vi.Paths);
					fileQueryQueue.Enqueue (vi);
					//	Console.WriteLine ("GetVersionInfo AddQuery " + string.Join (", ", vi.Paths.Select (p => p.FullPath)));
				} else if (query is RecursiveDirectoryInfoQuery) {
					var di = (RecursiveDirectoryInfoQuery)query;
					if (!recursiveDirectoriesInQueryQueue.Add (di.Directory))
						return;
					recursiveDirectoryQueryQueue.Enqueue (di);
				} else if (query is DirectoryInfoQuery) {
					DirectoryInfoQuery di = (DirectoryInfoQuery)query;
					if (!directoriesInQueryQueue.Add (di.Directory))
						return;
					directoryQueryQueue.Enqueue (di);
					//	Console.WriteLine ("GetDirectoryVersionInfo AddQuery " + ((DirectoryInfoQuery)query).Directory);
				}
				if (!queryRunning) {
					queryRunning = true;
					ThreadPool.QueueUserWorkItem (RunQueries);
				}
			}
		}

		void RunQueries (object ob)
		{
		//	DateTime t = DateTime.Now;
			//	Console.WriteLine ("RunQueries started");
			VersionInfoQuery [] fileQueryQueueClone;
			DirectoryInfoQuery [] directoryQueryQueueClone;
			RecursiveDirectoryInfoQuery [] recursiveDirectoryQueryQueueClone = new RecursiveDirectoryInfoQuery[0];
			try {
				while (true) {
					lock (queryLock) {
						if (fileQueryQueue.Count == 0 &&
							directoryQueryQueue.Count == 0 &&
							recursiveDirectoryQueryQueue.Count == 0) {
							queryRunning = false;
							return;
						}

						fileQueryQueueClone = fileQueryQueue.ToArray ();
						fileQueryQueue.Clear ();
						filesInQueryQueue.Clear ();

						directoryQueryQueueClone = directoryQueryQueue.ToArray ();
						directoriesInQueryQueue.Clear ();
						directoryQueryQueue.Clear ();

						recursiveDirectoryQueryQueueClone = recursiveDirectoryQueryQueue.ToArray ();
						recursiveDirectoriesInQueryQueue.Clear ();
						recursiveDirectoryQueryQueue.Clear ();
					}

					// Ensure we do not execute this with the query lock held, otherwise the IDE can hang while trying to add
					// new queries to the queue while long-running VCS operations are being performed
					var groups = fileQueryQueueClone.GroupBy (q => (q.QueryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0);
					foreach (var group in groups) {
						if (Disposed)
							break;
						var status = OnGetVersionInfo (group.SelectMany (q => q.Paths), group.Key);
						infoCache.SetStatus (status);
					}

					foreach (var item in directoryQueryQueueClone) {
						if (Disposed)
							break;
						var status = OnGetDirectoryVersionInfo (item.Directory, item.GetRemoteStatus, false);
						infoCache.SetDirectoryStatus (item.Directory, status, item.GetRemoteStatus);
					}

					foreach (var item in recursiveDirectoryQueryQueueClone) {
						try {
							if (Disposed)
								continue;
							item.Result = OnGetDirectoryVersionInfo (item.Directory, item.GetRemoteStatus, true);
						} finally {
							item.ResetEvent.Set ();
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Version control status query failed", ex);

				//Release all items in current batch
				foreach (var item in recursiveDirectoryQueryQueueClone)
					item.ResetEvent.Set ();

				lock (queryLock) {
					queryRunning = false;
						
					fileQueryQueue.Clear ();
					filesInQueryQueue.Clear ();

					directoriesInQueryQueue.Clear ();
					directoryQueryQueue.Clear ();

					recursiveDirectoryQueryQueueClone = recursiveDirectoryQueryQueue.ToArray ();
					recursiveDirectoriesInQueryQueue.Clear ();
					recursiveDirectoryQueryQueue.Clear ();
				}

				//Release newly pending
				foreach (var item in recursiveDirectoryQueryQueueClone)
					item.ResetEvent.Set ();
			}
			//Console.WriteLine ("RunQueries finished - " + (DateTime.Now - t).TotalMilliseconds);
		}

		/// <summary>
		/// Returns the list of changes done in the given revision
		/// </summary>
		/// <param name='revision'>
		/// A revision
		/// </param>
		public RevisionPath[] GetRevisionChanges (Revision revision)
		{
			return OnGetRevisionChanges (revision);
		}
		
		
		// Returns the content of the file in the base revision of the working copy.
		public abstract string GetBaseText (FilePath localFile);
		
		// Returns the revision history of a file
		public Revision[] GetHistory (FilePath localFile, Revision since)
		{
			return OnGetHistory (localFile, since);
		}

		protected abstract Revision[] OnGetHistory (FilePath localFile, Revision since);

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
		protected abstract IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus);
		
		// Returns the versioning status of all files in a directory
		protected abstract VersionInfo[] OnGetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive);
		
		// Imports a directory into the repository. 'serverPath' is the relative path in the repository.
		// 'localPath' is the local directory to publish. 'files' is the list of files to add to the new
		// repository directory (must use absolute local paths).
		public Repository Publish (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor)
		{
			var res = OnPublish (serverPath, localPath, files, message, monitor);
			ClearCachedVersionInfo (localPath);
			return res;
		}
		
		protected abstract Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, ProgressMonitor monitor);

		// Updates a local file or directory from the repository
		// Returns a list of updated files
		public void Update (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			Update (new FilePath[] { localPath }, recurse, monitor);
		}

		public void Update (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			OnUpdate (localPaths, recurse, monitor);
			ClearCachedVersionInfo (localPaths);
		}
		
		protected abstract void OnUpdate (FilePath[] localPaths, bool recurse, ProgressMonitor monitor);
		
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
		public void Commit (ChangeSet changeSet, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (changeSet.BaseLocalPath);
			OnCommit (changeSet, monitor);
		}
		
		protected abstract void OnCommit (ChangeSet changeSet, ProgressMonitor monitor);

		// Gets the contents of this repositories into the specified local path
		public void Checkout (FilePath targetLocalPath, bool recurse, ProgressMonitor monitor)
		{
			Checkout (targetLocalPath, null, recurse, monitor); 
		}

		public void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (targetLocalPath);
			OnCheckout (targetLocalPath, rev, recurse, monitor);
		}

		protected abstract void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, ProgressMonitor monitor);

		public void Revert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localPaths);
			OnRevert (localPaths, recurse, monitor);
		}

		public void Revert (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			Revert (new FilePath[] { localPath }, recurse, monitor);
		}

		protected abstract void OnRevert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor);

		public void RevertRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localPath);
			OnRevertRevision (localPath, revision, monitor);
		}

		protected abstract void OnRevertRevision (FilePath localPath, Revision revision, ProgressMonitor monitor);

		public void RevertToRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localPath);
			OnRevertToRevision (localPath, revision, monitor);
		}
		
		protected abstract void OnRevertToRevision (FilePath localPath, Revision revision, ProgressMonitor monitor);
		
		// Adds a file or directory to the repository
		public void Add (FilePath localPath, bool recurse, ProgressMonitor monitor)
		{
			Add (new FilePath[] { localPath }, recurse, monitor);
		}

		public void Add (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
			try {
				OnAdd (localPaths, recurse, monitor);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to add file", e);
			}
			ClearCachedVersionInfo (localPaths);
		}

		protected abstract void OnAdd (FilePath[] localPaths, bool recurse, ProgressMonitor monitor);
		
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
		public void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			try {
				OnMoveFile (localSrcPath, localDestPath, force, monitor);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to move file", e);
				File.Move (localSrcPath, localDestPath);
			}
		}
		
		protected virtual void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			File.Move (localSrcPath, localDestPath);
		}
		
		// Moves a directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file move.
		public void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			try {
				OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to move directory", e);
				FileService.SystemDirectoryRename (localSrcPath, localDestPath);
			}
		}
		
		protected virtual void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, ProgressMonitor monitor)
		{
			FileService.SystemDirectoryRename (localSrcPath, localDestPath);
		}
		
		// Deletes a file or directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file delete.
		public void DeleteFile (FilePath localPath, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			DeleteFiles (new FilePath[] { localPath }, force, monitor, keepLocal);
		}

		public void DeleteFiles (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			try {
				OnDeleteFiles (localPaths, force, monitor, keepLocal);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to delete file", e);
				if (!keepLocal)
					foreach (var path in localPaths)
						File.Delete (path);
			}
			ClearCachedVersionInfo (localPaths);
		}

		protected abstract void OnDeleteFiles (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal);

		public void DeleteDirectory (FilePath localPath, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			DeleteDirectories (new FilePath[] { localPath }, force, monitor, keepLocal);
		}

		public void DeleteDirectories (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal = true)
		{
			try {
				OnDeleteDirectories (localPaths, force, monitor, keepLocal);
			} catch (Exception e) {
				LoggingService.LogError ("Failed to delete directory", e);
				if (!keepLocal)
					foreach (var path in localPaths)
						Directory.Delete (path, true);
			}
			ClearCachedVersionInfo (localPaths);
		}

		protected abstract void OnDeleteDirectories (FilePath[] localPaths, bool force, ProgressMonitor monitor, bool keepLocal);
		
		// Called to request write permission for a file. The file may not yet exist.
		// After the file is modified or created, NotifyFileChanged is called.
		// This method is allways called for versioned and unversioned files.
		public virtual bool RequestFileWritePermission (params FilePath[] paths)
		{
			return true;
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
			ClearCachedVersionInfo (localPaths);
			OnLock (monitor, localPaths);
		}
		
		// Locks a file in the repository so no other users can change it
		protected virtual void OnLock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}
		
		// Unlocks a file in the repository so other users can change it
		public void Unlock (ProgressMonitor monitor, params FilePath[] localPaths)
		{
			ClearCachedVersionInfo (localPaths);
			OnUnlock (monitor, localPaths);
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

		public string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			return OnGetTextAtRevision (repositoryPath, revision);
		}

		protected abstract string OnGetTextAtRevision (FilePath repositoryPath, Revision revision);

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
		public virtual Annotation [] GetAnnotations (FilePath repositoryPath, Revision since)
		{
			return new Annotation[0];
		}

		/// <summary>
		/// Returns the list of changes done in the given revision
		/// </summary>
		/// <param name='revision'>
		/// A revision
		/// </param>
		protected abstract RevisionPath[] OnGetRevisionChanges (Revision revision);

		// Ignores a file for version control operations.
		public void Ignore (FilePath[] localPath)
		{
			ClearCachedVersionInfo (localPath);
			OnIgnore (localPath);
		}

		protected abstract void OnIgnore (FilePath[] localPath);

		// Unignores a file for version control operations.
		public void Unignore (FilePath[] localPath)
		{
			ClearCachedVersionInfo (localPath);
			OnUnignore (localPath);
		}

		protected abstract void OnUnignore (FilePath[] localPath);

		public virtual bool GetFileIsText (FilePath path)
		{
			return DesktopService.GetFileIsText (path);
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
