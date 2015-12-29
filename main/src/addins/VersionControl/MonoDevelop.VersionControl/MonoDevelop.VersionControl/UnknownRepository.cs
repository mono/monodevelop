
using System;
using System.Collections;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Collections.Generic;

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

		public override string GetBaseText (FilePath sourcefile)
		{
			return null;
		}

		protected override string OnGetTextAtRevision (FilePath repositoryPath, Revision revision)
		{
			return null;
		}

		protected override Revision[] OnGetHistory (FilePath sourcefile, Revision since)
		{
			return null;
		}

		protected override IEnumerable<VersionInfo> OnGetVersionInfo (IEnumerable<FilePath> paths, bool getRemoteStatus)
		{
			foreach (var p in paths)
				yield return VersionInfo.CreateUnversioned (p, System.IO.Directory.Exists (p));
		}

		protected override VersionInfo[] OnGetDirectoryVersionInfo (FilePath sourcepath, bool getRemoteStatus, bool recursive)
		{
			return new VersionInfo [0];
		}


		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] FilePath, string message, ProgressMonitor monitor)
		{
			return null;
		}

		protected override void OnUpdate (FilePath[] paths, bool recurse, ProgressMonitor monitor)
		{
		}
		
		protected override void OnCommit (ChangeSet changeSet, ProgressMonitor monitor)
		{
		}

		protected override void OnCheckout (FilePath path, Revision rev, bool recurse, ProgressMonitor monitor)
		{
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, ProgressMonitor monitor)
		{
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, ProgressMonitor monitor)
		{
		}

		protected override void OnAdd (FilePath[] paths, bool recurse, ProgressMonitor monitor)
		{
		}

		protected override void OnMoveFile (FilePath srcPath, FilePath destPath, bool force, ProgressMonitor monitor)
		{
		}

		protected override void OnMoveDirectory (FilePath srcPath, FilePath destPath, bool force, ProgressMonitor monitor)
		{
		}

		protected override void OnDeleteFiles (FilePath[] path, bool force, ProgressMonitor monitor, bool keepLocal)
		{
		}

		protected override void OnDeleteDirectories (FilePath[] path, bool force, ProgressMonitor monitor, bool keepLocal)
		{
		}

		protected override void OnIgnore (FilePath[] path)
		{
		}

		protected override void OnUnignore (FilePath[] path)
		{
		}
		
		public override Annotation[] GetAnnotations (FilePath repositoryPath, Revision since)
		{
			return new Annotation[0];
		}
		
		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			return new RevisionPath [0];
		}
	}
}
