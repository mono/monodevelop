
using System;

namespace VersionControl.Service
{
	public class VersionInfo
	{
		string localPath;
		string repositoryPath;
 		bool isDirectory;
		VersionStatus status = VersionStatus.Unversioned;
		Revision revision;
		VersionStatus remoteStatus = VersionStatus.Unchanged;
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
		}
		
		public Revision RemoteRevision {
			get { return remoteRevision; }
		}
	}
}
