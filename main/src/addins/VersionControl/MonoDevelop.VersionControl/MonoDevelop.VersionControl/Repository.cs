
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
		public VersionInfo GetVersionInfo (FilePath localPath)
		{
			return GetVersionInfo (localPath, false);
		}
		
		// Returns the versioning status of a file or directory
		public VersionInfo GetVersionInfo (FilePath localPath, bool getRemoteStatus)
		{
			VersionInfo vi = OnGetVersionInfo (new FilePath[] { localPath }, getRemoteStatus).Single ();
			vi.Init (this);
			return vi;
		}
		
		/// <summary>
		/// Returns the versioning status of a set of files or directories
		/// </summary>
		/// <param name='paths'>
		/// A list of files or directories
		/// </param>
		public IEnumerable<VersionInfo> GetVersionInfo (IEnumerable<FilePath> paths)
		{
			return GetVersionInfo (paths, false);
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
		public IEnumerable<VersionInfo> GetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			foreach (VersionInfo vi in OnGetVersionInfo (paths, false)) {
				vi.Init (this);
				yield return vi;
			}
		}
		
		public VersionInfo[] GetDirectoryVersionInfo (FilePath localDirectory, bool getRemoteStatus, bool recursive)
		{
			VersionInfo[] infos = OnGetDirectoryVersionInfo (localDirectory, getRemoteStatus, recursive);
			foreach (var vi in infos)
				vi.Init (this);
			return infos;
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
		public abstract Revision[] GetHistory (FilePath localFile, Revision since);
		
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
		public abstract Repository Publish (string serverPath, FilePath localPath, FilePath[] files, string message, IProgressMonitor monitor);
		
		// Updates a local file or directory from the repository
		// Returns a list of updated files
		public void Update (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Update (new FilePath[] { localPath }, recurse, monitor);
		}

		public abstract void Update (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);
		
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
		public abstract void Commit (ChangeSet changeSet, IProgressMonitor monitor);
		
		// Gets the contents of this repositories into the specified local path
		public void Checkout (FilePath targetLocalPath, bool recurse, IProgressMonitor monitor) { Checkout (targetLocalPath, null, recurse, monitor); }
		public abstract void Checkout (FilePath targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor);

		public abstract void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);

		public void Revert (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Revert (new FilePath[] { localPath }, recurse, monitor);
		}

		public abstract void RevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor);

		public abstract void RevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor);
		
		// Adds a file or directory to the repository
		public void Add (FilePath localPath, bool recurse, IProgressMonitor monitor)
		{
			Add (new FilePath[] { localPath }, recurse, monitor);
		}

		public abstract void Add (FilePath[] localPaths, bool recurse, IProgressMonitor monitor);
		
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
		public virtual void MoveFile (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			File.Move (localSrcPath, localDestPath);
		}
		
		// Moves a directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file move.
		public virtual void MoveDirectory (FilePath localSrcPath, FilePath localDestPath, bool force, IProgressMonitor monitor)
		{
			Directory.Move (localSrcPath, localDestPath);
		}
		
		// Deletes a file or directory. This method may be called for versioned and unversioned
		// files. The default implementetions performs a system file delete.
		public void DeleteFile (FilePath localPath, bool force, IProgressMonitor monitor)
		{
			DeleteFiles (new FilePath[] { localPath }, force, monitor);
		}

		public virtual void DeleteFiles (FilePath[] localPaths, bool force, IProgressMonitor monitor)
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

		public virtual void DeleteDirectories (FilePath[] localPaths, bool force, IProgressMonitor monitor)
		{
			foreach (string localPath in localPaths) {
				if (Directory.Exists (localPath))
					Directory.Delete (localPath, true);
				else
					File.Delete (localPath);
			}
		}
		
		// Creates a local directory.
		public virtual void CreateLocalDirectory (FilePath path)
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
		public virtual void NotifyFileChanged (FilePath path)
		{
		}
		
		// Locks a file in the repository so no other users can change it
		public virtual void Lock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
		}
		
		// Unlocks a file in the repository so other users can change it
		public virtual void Unlock (IProgressMonitor monitor, params FilePath[] localPaths)
		{
			throw new System.NotSupportedException ();
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

		public abstract string GetTextAtRevision (FilePath repositoryPath, Revision revision);

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
}
