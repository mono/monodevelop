using System;
using System.Collections;
using System.IO;

namespace VersionControl {
	public abstract class VersionControlSystem {
		public abstract bool IsDiffAvailable(string sourcefile);
		public abstract bool IsHistoryAvailable(string sourcefile);
		public abstract bool IsFileStatusAvailable(string sourcefile);
		public abstract bool IsDirectoryStatusAvailable(string sourcepath);
		public abstract bool CanUpdate(string sourcepath);
		public abstract bool CanCommit(string sourcepath);
		
		public abstract string GetPathToBaseText(string sourcefile);
		public abstract string GetTextAtRevision(object repositoryPath, RevisionPtr revision);
		public abstract RevisionDescription[] GetHistory(string sourcefile, RevisionPtr since);
		public abstract Node GetFileStatus(string sourcefile, bool getRemoteStatus);
		public abstract Node[] GetDirectoryStatus(string sourcepath, bool getRemoteStatus, bool recursive);
		public abstract void Update(string path, bool recurse, UpdateCallback callback);
		public abstract void Commit(string[] paths, string message, UpdateCallback callback);
	}
	
	public delegate void UpdateCallback(string path, string action);
	
	public class RevisionDescription {
		public object RepositoryPath;
		public RevisionPtr Revision;
		public DateTime Time;
		public string Author;
		public string Message;
	}
	
	public abstract class RevisionPtr {
		public abstract RevisionPtr GetPrevious();
	}
	
	public class Node {
		public string LocalPath;
		public object RepositoryPath;
 		public bool IsDirectory;
 		
		public NodeStatus Status = NodeStatus.Unknown;
		public RevisionPtr BaseRevision;

		public NodeStatus RemoteStatus = NodeStatus.Unknown;
		public RevisionDescription RemoteUpdate;
	}
	
	public enum NodeStatus {
		Unknown,
		Unversioned,
		UnversionedIgnored,
		Missing,
		Obstructed,
		Unchanged,
		Modified,
		ScheduledAdd,
		ScheduledDelete,
		ScheduledReplace,
		Conflicted
	}
	
}
