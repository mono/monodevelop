
using System;
using MonoDevelop.Core;
using System.IO;

namespace MonoDevelop.VersionControl
{
	public class VersionInfo
	{
		FilePath localPath;
		string repositoryPath;
 		bool isDirectory;
		VersionStatus status = VersionStatus.Unversioned;
		Revision revision;
		VersionStatus remoteStatus = VersionStatus.Versioned;
		Revision remoteRevision;
		bool opsLoaded;
		VersionControlOperation operations;
		Repository ownerRepository;

		public VersionInfo (FilePath localPath, string repositoryPath, bool isDirectory, VersionStatus status, Revision revision, VersionStatus remoteStatus, Revision remoteRevision)
		{
			this.localPath = localPath;
			this.repositoryPath = repositoryPath;
			this.isDirectory = isDirectory;
			this.status = status;
			this.revision = revision;
			this.remoteStatus = remoteStatus;
			this.remoteRevision = remoteRevision;
		}

		public bool Equals (VersionInfo obj)
		{
			VersionInfo other = obj as VersionInfo;
			if (other == null)
				return false;
			return localPath == other.localPath &&
				repositoryPath == other.repositoryPath &&
				isDirectory == other.isDirectory &&
				status == other.status &&
				revision == other.revision &&
				remoteStatus == other.remoteStatus &&
				remoteRevision == other.remoteRevision;
		}
		
		internal void Init (Repository repo)
		{
			ownerRepository = repo;
			RequiresRefresh = false;
		}
		
		public static VersionInfo CreateUnversioned (FilePath path, bool isDirectory)
		{
			return new VersionInfo (path, "", isDirectory, VersionStatus.Unversioned, null, VersionStatus.Unversioned, null);
		}

		internal bool RequiresRefresh { get; set; }
		
		public bool IsVersioned {
			get { return (status & VersionStatus.Versioned) != 0; }
		}
		
		public bool HasLocalChanges {
			get { return (status & VersionStatus.LocalChangesMask) != 0; }
		}
		
		public bool HasRemoteChanges {
			get { return (remoteStatus & VersionStatus.LocalChangesMask) != 0; }
		}
		
		public bool HasLocalChange (VersionStatus changeKind)
		{
			return (status & changeKind) != 0;
		}
		
		public bool HasRemoteChange (VersionStatus changeKind)
		{
			return (remoteStatus & changeKind) != 0;
		}
		
		public FilePath LocalPath {
			get { return localPath; }
		}
		
		public string RepositoryPath {
			get { return repositoryPath; }
		}
		
 		public bool IsDirectory {
			get { return isDirectory; }
		}
 		
		public VersionStatus Status {
			get { return status; }
		}
		
		public Revision Revision {
			get { return revision; }
		}

		public VersionStatus RemoteStatus {
			get { return remoteStatus; }
			internal set { remoteStatus = value; }
		}
		
		public Revision RemoteRevision {
			get { return remoteRevision; }
			internal set { remoteRevision = value; }
		}
		
		public VersionControlOperation AllowedOperations {
			get {
				if (!opsLoaded && ownerRepository != null) {
					opsLoaded = true;
					operations = ownerRepository.GetSupportedOperations (this);
				}
				return operations;
			}
		}
		
		public bool SupportsOperation (VersionControlOperation op)
		{
			return (AllowedOperations & op) != 0;
		}
		
		public bool CanAdd { get { return SupportsOperation (VersionControlOperation.Add); } }
		
		public bool CanAnnotate { get { return SupportsOperation (VersionControlOperation.Annotate); } }
		
		public bool CanCommit { get { return SupportsOperation (VersionControlOperation.Commit); } }
		
		public bool CanLock { get { return SupportsOperation (VersionControlOperation.Lock); } }
		
		public bool CanLog { get { return SupportsOperation (VersionControlOperation.Log); } }
		
		public bool CanRemove { get { return SupportsOperation (VersionControlOperation.Remove); } }
		
		public bool CanRevert { get { return SupportsOperation (VersionControlOperation.Revert); } }
		
		public bool CanUnlock { get { return SupportsOperation (VersionControlOperation.Unlock); } }
		
		public bool CanUpdate { get { return SupportsOperation (VersionControlOperation.Update); } }
	}
}
