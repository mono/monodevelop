
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class VersionInfo
	{
		bool opsLoaded;
		VersionControlOperation operations;
		Repository ownerRepository;

		public VersionInfo (FilePath localPath, string repositoryPath, bool isDirectory, VersionStatus status, Revision revision, VersionStatus remoteStatus, Revision remoteRevision)
		{
			this.LocalPath = localPath;
			this.RepositoryPath = repositoryPath;
			this.IsDirectory = isDirectory;
			this.Status = status;
			this.Revision = revision;
			this.RemoteStatus = remoteStatus;
			this.RemoteRevision = remoteRevision;
		}

		public bool Equals (VersionInfo obj)
		{
			if (obj == null)
				return false;
			return LocalPath == obj.LocalPath &&
				RepositoryPath == obj.RepositoryPath &&
				IsDirectory == obj.IsDirectory &&
				Status == obj.Status &&
				Revision == obj.Revision &&
				RemoteStatus == obj.RemoteStatus &&
				RemoteRevision == obj.RemoteRevision &&
				AllowedOperations == obj.AllowedOperations;
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
			get { return (Status & VersionStatus.Versioned) != 0; }
		}
		
		public bool HasLocalChanges {
			get { return (Status & VersionStatus.LocalChangesMask) != 0; }
		}
		
		public bool HasRemoteChanges {
			get { return (RemoteStatus & VersionStatus.LocalChangesMask) != 0; }
		}
		
		public bool HasLocalChange (VersionStatus changeKind)
		{
			return (Status & changeKind) != 0;
		}
		
		public bool HasRemoteChange (VersionStatus changeKind)
		{
			return (RemoteStatus & changeKind) != 0;
		}
		
		public FilePath LocalPath {
			get;
			private set;
		}
		
		public string RepositoryPath {
			get;
			private set;
		}
		
 		public bool IsDirectory {
			get;
			private set;
		}
 		
		public VersionStatus Status {
			get;
			private set;
		}
		
		public Revision Revision {
			get;
			private set;
		}

		public VersionStatus RemoteStatus {
			get;
			internal set;
		}
		
		public Revision RemoteRevision {
			get;
			internal set;
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
