
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace VersionControl.Service
{
	[DataItem (FallbackType=typeof(UnknownRepository))]
	public abstract class Repository
	{
		string name;
		VersionControlSystem vcs;
		
		[ItemProperty ("VcsType")] 
		string vcsName;
		
		public Repository ()
		{
		}
		
		public Repository (VersionControlSystem vcs)
		{
			VersionControlSystem = vcs;
		}
		
		// Display name of the repository
		[ItemProperty]
		public string Name	{
			get { return name; }
			set { name = value; }		
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
		
		// Returns true if the specified local file or directory is under version control
		public virtual bool IsVersioned (string localPath)
		{
			VersionInfo vinfo = GetVersionInfo (localPath, false);
			if (vinfo == null)
				return false;
			return vinfo.Status != VersionStatus.Unversioned && vinfo.Status != VersionStatus.UnversionedIgnored;
		}
		
		// Returns true if the specified file has been modified since the last commit
		public virtual bool IsModified (string localFile)
		{
			if (!File.Exists (localFile))
				return false;
			VersionInfo vinfo = GetVersionInfo (localFile, false);
			if (vinfo == null)
				return false;
			return vinfo.Status == VersionStatus.Modified;
		}
		
		// Returns true if the specified file or directory can be added to the repository
		public virtual bool CanAdd (string localPath)
		{
			if (!File.Exists (localPath) && !Directory.Exists (localPath))
				return false;
			VersionInfo vinfo = GetVersionInfo (localPath, false);
			if (vinfo == null)
				return false;
			return vinfo.Status == VersionStatus.Unversioned;
		}
		
		// Returns true if the repository has history for the specified local file
		public virtual bool IsHistoryAvailable (string localFile)
		{
			return IsVersioned (localFile);
		}
		
		// Returns true if the specified path can be updated from the repository
		public virtual bool CanUpdate (string localPath)
		{
			return IsVersioned (localPath);
		}
		
		// Returns true if the specified path can be committed to the repository
		public virtual bool CanCommit (string localPath)
		{
			return GetVersionInfo (localPath, false) != null;
		}
		
		// Returns true if the specified path can be removed from the repository
		public virtual bool CanRemove (string localPath)
		{
			if (!File.Exists (localPath) && !Directory.Exists (localPath))
				return false;
			VersionInfo vinfo = GetVersionInfo (localPath, false);
			if (vinfo == null)
				return false;
			return vinfo.Status == VersionStatus.Unchanged || vinfo.Status == VersionStatus.Modified;
		}
		
		// Returns true if the specified path can be reverted
		public virtual bool CanRevert (string localPath)
		{
			VersionInfo vinfo = GetVersionInfo (localPath, false);
			return vinfo != null && 
					vinfo.Status != VersionStatus.Unchanged && 
					vinfo.Status != VersionStatus.Unversioned &&
					vinfo.Status != VersionStatus.UnversionedIgnored &&
					vinfo.Status != VersionStatus.Protected;
		}
		
		public virtual bool CanLock (string localPath)
		{
			return false;
		}
		
		public virtual bool CanUnlock (string localPath)
		{
			return false;
		}
		
		// Returns a path to the last version of the file updated from the repository
		public abstract string GetPathToBaseText (string localFile);
		
		// Returns the revision history of a file
		public abstract Revision[] GetHistory (string localFile, Revision since);
		
		// Returns the versioning status of a file or directory
		// Returns null if the file or directory is not versioned.
		public abstract VersionInfo GetVersionInfo (string localPath, bool getRemoteStatus);
		
		// Returns the versioning status of all files in a directory
		public abstract VersionInfo[] GetDirectoryVersionInfo (string localDirectory, bool getRemoteStatus, bool recursive);
		
		// Imports a directory into the repository. 'serverPath' is the relative path in the repository.
		// 'localPath' is the local directory to publish. 'files' is the list of files to add to the new
		// repository directory (must use absolute local paths).
		public abstract Repository Publish (string serverPath, string localPath, string[] files, string message, IProgressMonitor monitor);
		
		// Updates a local file or directory from the repository
		// Returns a list of updated files
		public abstract void Update (string localPath, bool recurse, IProgressMonitor monitor);
		
		// Called to create a ChangeSet to be used for a commit operation
		public virtual ChangeSet CreateChangeSet (string basePath)
		{
			return new ChangeSet (this, basePath);
		}
		
		// Commits changes in a set of files or directories into the repository
		public abstract void Commit (ChangeSet changeSet, IProgressMonitor monitor);
		
		// Gets the contents of this repositories into the specified local path
		public void Checkout (string targetLocalPath, bool recurse, IProgressMonitor monitor) { Checkout (targetLocalPath, null, recurse, monitor); }
		public abstract void Checkout (string targetLocalPath, Revision rev, bool recurse, IProgressMonitor monitor);
		
		public abstract void Revert (string localPath, bool recurse, IProgressMonitor monitor);
		
		// Adds a file or directory to the repository
		public abstract void Add (string localPath, bool recurse, IProgressMonitor monitor);
		
		// Moves a file or directory
		public abstract void Move (string localSrcPath, string localDestPath, Revision revision, bool force, IProgressMonitor monitor);
		
		// Deletes a file or directory
		public abstract void Delete (string localPath, bool force, IProgressMonitor monitor);
		
		// Locks a file in the repository so no other users can change it
		public virtual void Lock (string localPath)
		{
			throw new System.NotSupportedException ();
		}
		
		// Unlocks a file in the repository so other users can change it
		public virtual void Unlock (string localPath)
		{
			throw new System.NotSupportedException ();
		}
		
		// Returns a dif description between local files and the remote files.
		// baseLocalPath is the root path of the diff. localPaths is optional and
		// it can be a list of files to compare.
		public virtual DiffInfo[] PathDiff (string baseLocalPath, string[] localPaths)
		{
			return new DiffInfo [0];
		}
		
		public abstract string GetTextAtRevision (string repositoryPath, Revision revision);
		
		static protected DiffInfo[] GenerateUnifiedDiffInfo (string diffFile, string basePath, string[] localPaths)
		{
			ArrayList list = new ArrayList ();
			using (StreamReader sr = new StreamReader (diffFile)) {
				string line;
				StringBuilder content = new StringBuilder ();
				string fileName = null;
				
				while ((line = sr.ReadLine ()) != null) {
					if (!line.StartsWith ("Index:")) {
						content.Append (line).Append ('\n');
					} else {
						if (fileName != null) {
							list.Add (new DiffInfo (fileName, content.ToString ()));
							fileName = null;
						}
						fileName = line.Substring (6).Trim ();
						content = new StringBuilder ();
						line = sr.ReadLine ();	// "===" Separator
						
						// Filter out files not in the provided path list
						if (localPaths != null && Array.IndexOf (localPaths, fileName) == -1)
							fileName = null;
					}
				}
				if (fileName != null) {
					list.Add (new DiffInfo (fileName, content.ToString ()));
				}
			}
			return (DiffInfo[]) list.ToArray (typeof(DiffInfo));
		}
	}
	
	public class DiffInfo
	{
		string fileName;
		string content;
		
		public DiffInfo (string fileName, string content)
		{
			this.fileName = fileName;
			this.content = content;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public string Content {
			get { return content; }
		}
	}
}
