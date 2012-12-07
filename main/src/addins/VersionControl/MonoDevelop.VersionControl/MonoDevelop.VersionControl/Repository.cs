
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Linq;

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
		
		public event EventHandler NameChanged;
		
		public Repository ()
		{
			infoCache = new VersionInfoCache (this);
		}
		
		public Repository (VersionControlSystem vcs): this ()
		{
			VersionControlSystem = vcs;
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
		
		public virtual void Dispose ()
		{
		}
		
		// Display name of the repository
		[ItemProperty]
		public string Name	{
			get { return name ?? string.Empty; }
			set {
				name = value;
				if (NameChanged != null)
					NameChanged (this, EventArgs.Empty);
			}		
		}
		
		// Description of the repository location (for example, the url)
		public virtual string LocationDescription {
			get { return name; }
		}
		
		// Version control system that manages this repository
		public VersionControlSystem VersionControlSystem {
			get {
				if (vcs == null && vcsName != null) {
					foreach (VersionControlSystem v in VersionControlService.GetVersionControlSystems ()) {
						if (v.Id == vcsName) {
							vcs = v;
							break;
						}
					}
				}
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
		
		internal protected virtual VersionControlOperation GetSupportedOperations (VersionInfo vinfo)
		{
			VersionControlOperation operations = VersionControlOperation.None;
			bool exists = !vinfo.LocalPath.IsNullOrEmpty && (File.Exists (vinfo.LocalPath) || Directory.Exists (vinfo.LocalPath));
			if (vinfo.IsVersioned) {
				operations = VersionControlOperation.Commit | VersionControlOperation.Update | VersionControlOperation.Log;
				if (exists) {
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
			return infos.Single ();
		}
		
		/// <summary>
		/// Returns the versioning status of a set of files or directories
		/// </summary>
		/// <param name='paths'>
		/// A list of files or directories
		/// </param>
		/// <param name='getRemoteStatus'>
		/// True if remote status information has to be included
		/// </param>
		public IEnumerable<VersionInfo> GetVersionInfo (IEnumerable<FilePath> paths, VersionInfoQueryFlags queryFlags = VersionInfoQueryFlags.None)
		{
			if ((queryFlags & VersionInfoQueryFlags.IgnoreCache) != 0) {
				var res = OnGetVersionInfo (paths, (queryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0);
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
					vi.Init (this);
					result.Add (vi);
					pathsToQuery.Add (p);
				}
//				Console.WriteLine ("GetVersionInfo " + string.Join (", ", paths.Select (p => p.FullPath)));
			}
			if (pathsToQuery.Count > 0)
				AddQuery (new VersionInfoQuery () { Paths = pathsToQuery, QueryFlags = queryFlags });
			return result;
		}

		public VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			try {
				if (recursive)
					return OnGetDirectoryVersionInfo (localDirectory, getRemoteStatus, recursive);

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
				else
					return new VersionInfo[0];
			} finally {
				//Console.WriteLine ("GetDirectoryVersionInfo " + localDirectory + " - " + (DateTime.Now - now).TotalMilliseconds);
			}
		}

		public void ClearCachedVersionInfo (FilePath rootPath)
		{
			infoCache.ClearCachedVersionInfo (rootPath);
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

		Queue<object> queryQueue = new Queue<object> ();
		bool queryRunning;
		VersionInfoCache infoCache;
		HashSet<FilePath> filesInQueryQueue = new HashSet<FilePath> ();
		HashSet<FilePath> directoriesInQueryQueue = new HashSet<FilePath> ();

		void QueueVersionInfoQuery (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
		}

		void AddQuery (object query)
		{
			lock (queryQueue) {
				if (query is VersionInfoQuery) {
					VersionInfoQuery vi = (VersionInfoQuery) query;
					vi.Paths.RemoveAll (p => filesInQueryQueue.Contains (p) || directoriesInQueryQueue.Contains (p.ParentDirectory));
					if (vi.Paths.Count == 0)
						return;
					filesInQueryQueue.UnionWith (vi.Paths);
				//	Console.WriteLine ("GetVersionInfo AddQuery " + string.Join (", ", vi.Paths.Select (p => p.FullPath)));
				}
				else if (query is DirectoryInfoQuery) {
					if (!directoriesInQueryQueue.Add (((DirectoryInfoQuery)query).Directory))
						return;
				//	Console.WriteLine ("GetDirectoryVersionInfo AddQuery " + ((DirectoryInfoQuery)query).Directory);
				}
				queryQueue.Enqueue (query);
				if (!queryRunning) {
					queryRunning = true;
					System.Threading.ThreadPool.QueueUserWorkItem (RunQueries);
				}
			}
		}

		void RunQueries (object ob)
		{
		//	DateTime t = DateTime.Now;
		//	Console.WriteLine ("RunQueries started");
			do {
				object query = null;
				lock (queryQueue) {
					if (queryQueue.Count == 0) {
						queryRunning = false;
						break;
					}
					query = queryQueue.Dequeue ();
					if (query is VersionInfoQuery) {
						VersionInfoQuery q = (VersionInfoQuery) query;
						filesInQueryQueue.ExceptWith (q.Paths);
					}
					else if (query is DirectoryInfoQuery) {
						var q = (DirectoryInfoQuery) query;
						directoriesInQueryQueue.Remove (q.Directory);
					}
				}
				try {
					if (query is VersionInfoQuery) {
						VersionInfoQuery q = (VersionInfoQuery) query;
						var status = OnGetVersionInfo (q.Paths, (q.QueryFlags & VersionInfoQueryFlags.IncludeRemoteStatus) != 0);
						infoCache.SetStatus (status);
					}
					else if (query is DirectoryInfoQuery) {
						var q = (DirectoryInfoQuery) query;
						var status = OnGetDirectoryVersionInfo (q.Directory, q.GetRemoteStatus, false);
						infoCache.SetDirectoryStatus (q.Directory, status, q.GetRemoteStatus);
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Version control status query failed", ex);
				}
			} while (true);
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
		
		
		// Returns a path to the last version of the file updated from the repository
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
		public Repository Publish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor)
		{
			var res = OnPublish (serverPath, localPath, files, message, monitor);
			ClearCachedVersionInfo (localPath);
			return res;
		}
		
		protected abstract Repository OnPublish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor);

		// Updates a local file or directory from the repository
		// Returns a list of updated files
		public void Update (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Update (new FilePath[] { localPath }, recurse, monitor);
		}

		public void Update (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			OnUpdate (localPaths, recurse, monitor);
			ClearCachedVersionInfo (localPaths);
		}
		
		protected abstract void OnUpdate (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);
		
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
					patch.AppendLine (diff.Content);
				}
			}
			
			return patch.ToString ();
		}
		
		// Commits changes in a set of files or directories into the repository
		public void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (changeSet.BaseLocalPath);
			OnCommit (changeSet, monitor);
		}
		
		protected abstract void OnCommit (ChangeSet changeSet, IProgressMonitor monitor);

		// Gets the contents of this repositories into the specified local path
		public void Checkout (FilePath targetLocalPath, bool recurse, IProgressMonitor monitor)
		{
			Checkout (targetLocalPath, null, recurse, monitor); 
		}

		public void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (targetLocalPath);
			OnCheckout (targetLocalPath, rev, recurse, monitor);
		}

		protected abstract void OnCheckout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor);

		public void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			OnRevert (localPaths, recurse, monitor);
			ClearCachedVersionInfo (localPaths);
		}

		public void Revert (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Revert (new FilePath[] { localPath }, recurse, monitor);
		}

		protected abstract void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);

		public void RevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			OnRevertRevision (localPath, revision, monitor);
			ClearCachedVersionInfo (localPath);
		}

		protected abstract void OnRevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor);

		public void RevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
			OnRevertToRevision (localPath, revision, monitor);
			ClearCachedVersionInfo (localPath);
		}
		
		protected abstract void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor);
		
		// Adds a file or directory to the repository
		public void Add (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Add (new FilePath[] { localPath }, recurse, monitor);
		}

		public void Add (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
			OnAdd (localPaths, recurse, monitor);
			ClearCachedVersionInfo (localPaths);
		}

		protected abstract void OnAdd (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);
		
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
		public void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			OnMoveFile (localSrcPath, localDestPath, force, monitor);
		}
		
		protected virtual void OnMoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			File.Move (localSrcPath, localDestPath);
		}
		
		// Moves a directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file move.
		public void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localSrcPath, localDestPath);
			OnMoveDirectory (localSrcPath, localDestPath, force, monitor);
		}
		
		protected virtual void OnMoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			Directory.Move (localSrcPath, localDestPath);
		}
		
		// Deletes a file or directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file delete.
		public void DeleteFile (FilePath localPath, bool force, IProgressMonitor monitor)
		{
			DeleteFiles (new FilePath[] { localPath }, force, monitor);
		}

		public void DeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localPaths);
			OnDeleteFiles (localPaths, force, monitor);
		}

		protected virtual void OnDeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			foreach (string localPath in localPaths) {
				if (Directory.Exists (localPath))
					Directory.Delete (localPath, true);
				else
					File.Delete (localPath);
			}
		}

		public void DeleteDirectory (FilePath localPath, bool force, IProgressMonitor monitor)
		{
			DeleteDirectories (new FilePath[] { localPath }, force, monitor);
		}

		public void DeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			ClearCachedVersionInfo (localPaths);
			OnDeleteDirectories (localPaths, force, monitor);
		}
		
		protected virtual void OnDeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			foreach (string localPath in localPaths) {
				if (Directory.Exists (localPath))
					Directory.Delete (localPath, true);
				else
					File.Delete (localPath);
			}
		}
		
		// Creates a local directory.
		public void CreateLocalDirectory (FilePath path)
		{
			ClearCachedVersionInfo (path);
			OnCreateLocalDirectory (path);
		}
		
		protected virtual void OnCreateLocalDirectory (FilePath path)
		{
			Directory.CreateDirectory (path);
		}
		
		// Called to request write permission for a file. The file may not yet exist.
		// After the file is modified or created, NotifyFileChanged is called.
		// This method is allways called for versioned and unversioned files.
		public virtual bool RequestFileWritePermission (FilePath path)
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
		public void Lock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			ClearCachedVersionInfo (localPaths);
			OnLock (monitor, localPaths);
		}
		
		// Locks a file in the repository so no other users can change it
		protected virtual void OnLock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}
		
		// Unlocks a file in the repository so other users can change it
		public void Unlock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			ClearCachedVersionInfo (localPaths);
			OnUnlock (monitor, localPaths);
		}
		
		protected virtual void OnUnlock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}
		
		public virtual DiffInfo GenerateDiff (FilePath baseLocalPath, VersionInfo versionInfo)
		{
			return null;
		}
		
		// Returns a dif description between local files and the remote files.
		// baseLocalPath is the root path of the diff. localPaths is optional and
		// it can be a list of files to compare.
		public DiffInfo[] PathDiff (ChangeSet cset, bool remoteDiff)
		{
			List<FilePath> paths = new List<FilePath> ();
			foreach (ChangeSetItem item in cset.Items)
				paths.Add (item.LocalPath);
			return PathDiff (cset.BaseLocalPath, paths.ToArray (), remoteDiff);
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
			ArrayList list = new ArrayList ();
			using (StringReader sr = new StringReader (diffContent)) {
				string line;
				StringBuilder content = new StringBuilder ();
				string fileName = null;
				string pathRoot = null;
				
				while ((line = sr.ReadLine ()) != null) {
					if (pathRoot != null && fileName != null && (line.StartsWith ("+++ " + pathRoot) || line.StartsWith ("--- " + pathRoot))) {
						line = line.Substring (0, 4) + line.Substring (4 + pathRoot.Length);
						content.Append (line).Append ('\n');
					}
					else if (!line.StartsWith ("Index:")) {
						content.Append (line).Append ('\n');
					} else {
						if (fileName != null) {
							list.Add (new DiffInfo (basePath, fileName, content.ToString ()));
							fileName = null;
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
			return (DiffInfo[]) list.ToArray (typeof(DiffInfo));
		}
		
		/// <summary>
		/// Retrieves annotations for a given path in the repository.
		/// </summary>
		/// <param name="repositoryPath">
		/// A <see cref="FilePath"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> corresponding to each line 
		/// of the file to which repositoryPath points.
		/// </returns>
		public virtual Annotation[] GetAnnotations (FilePath repositoryPath)
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
	}
	
	public class Annotation
	{
		public string Revision {
			get;
			private set;
		}

		public string Author {
			get;
			private set;
		}

		public DateTime Date {
			get;
			private set;
		}
		
		public bool HasDate {
			get { return Date != DateTime.MinValue; }
		}

		public Annotation (string revision, string author, DateTime date)
		{
			this.Revision = revision;
			this.Author = author;
			this.Date = date;
		}
		
		public override string ToString ()
		{
			return string.Format ("[Annotation: Revision={0}, Author={1}, Date={2}, HasDate={3}]", Revision, Author, Date, HasDate);
		}
	}
	
	public class DiffInfo
	{
		FilePath fileName;
		FilePath basePath;
		string content;
		
		public DiffInfo (FilePath basePath, FilePath fileName, string content)
		{
			this.basePath = basePath;
			this.fileName = fileName;
			this.content = content.Replace ("\r","");
		}
		
		public FilePath FileName {
			get { return fileName; }
		}
		
		public string Content {
			get { return content; }
		}
		
		public FilePath BasePath {
			get { return basePath; }
		}
	}

	public enum VersionInfoQueryFlags
	{
		None = 0,
		IgnoreCache = 1,
		IncludeRemoteStatus = 2
	}
}
