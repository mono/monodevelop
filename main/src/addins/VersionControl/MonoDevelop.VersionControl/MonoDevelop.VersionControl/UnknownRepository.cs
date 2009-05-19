
using System;
using System.Collections;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	public class UnknownRepository: Repository, IExtendedDataItem
	{
		Hashtable properties;
		
		public IDictionary ExtendedProperties {
			get {
				if (properties == null) properties = new Hashtable ();
				return properties;
			}
		}

		public override string GetPathToBaseText (FilePath sourcefile)
		{
			return null;
		}

		public override string GetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			return null;
		}

		public override Revision[] GetHistory (FilePath sourcefile, Revision since)
		{
			return null;
		}

		public override VersionInfo GetVersionInfo (FilePath localPath, bool getRemoteStatus)
		{
			return null;
		}

		public override VersionInfo[] GetDirectoryVersionInfo (FilePath sourcepath, bool getRemoteStatus, bool recursive)
		{
			return null;
		}


		public override Repository Publish (string serverPath, FilePath localPath, FilePath[] FilePath, string message, IProgressMonitor monitor)
		{
			return null;
		}

		public override void Update (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Commit (ChangeSet changeSet, IProgressMonitor monitor)
		{
		}

		public override void Checkout (FilePath path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
		}

		public override void Revert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
		}

		public override void RevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
		}

		public override void RevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
		}

		public override void Add (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
		}

		public override void MoveFile (FilePath srcPath, FilePath destPath, bool force, IProgressMonitor monitor)
		{
		}

		public override void MoveDirectory (FilePath srcPath, FilePath destPath, bool force, IProgressMonitor monitor)
		{
		}

		public override void DeleteFiles (FilePath[] path, bool force, IProgressMonitor monitor)
		{
		}

		public override void DeleteDirectories (FilePath[] path, bool force, IProgressMonitor monitor)
		{
		}
	}
}
