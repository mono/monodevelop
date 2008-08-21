
using System;

namespace MonoDevelop.VersionControl
{
	public class VersionInfo
	{
		string localPath;
		string repositoryPath;
 		bool isDirectory;
		VersionStatus status = VersionStatus.Unversioned;
		Revision revision;
		VersionStatus remoteStatus = VersionStatus.Versioned;
		Revision remoteRevision;
		
		public VersionInfo (string localPath, string repositoryPath, bool isDirectory, VersionStatus status, Revision revision, VersionStatus remoteStatus, Revision remoteRevision)
		{
			this.localPath = localPath;
			this.repositoryPath = repositoryPath;
			this.isDirectory = isDirectory;
			this.status = status;
			this.revision = revision;
			this.remoteStatus = remoteStatus;
			this.remoteRevision = remoteRevision;
		}
		
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
		
		public string LocalPath {
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
	}
}
