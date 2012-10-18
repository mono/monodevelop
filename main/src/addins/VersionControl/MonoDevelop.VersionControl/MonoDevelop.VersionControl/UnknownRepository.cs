
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


		protected override Repository OnPublish (string serverPath, FilePath localPath, FilePath[] FilePath, string message, IProgressMonitor monitor)
		{
			return null;
		}

		protected override void OnUpdate (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
		}
		
		protected override void OnCommit (ChangeSet changeSet, IProgressMonitor monitor)
		{
		}

		protected override void OnCheckout (FilePath path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
		}

		protected override void OnRevert (FilePath[] localPaths, bool recurse, IProgressMonitor monitor)
		{
		}

		protected override void OnRevertRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
		}

		protected override void OnRevertToRevision (FilePath localPath, Revision revision, IProgressMonitor monitor)
		{
		}

		protected override void OnAdd (FilePath[] paths, bool recurse, IProgressMonitor monitor)
		{
		}

		protected override void OnMoveFile (FilePath srcPath, FilePath destPath, bool force, IProgressMonitor monitor)
		{
		}

		protected override void OnMoveDirectory (FilePath srcPath, FilePath destPath, bool force, IProgressMonitor monitor)
		{
		}

		protected override void OnDeleteFiles (FilePath[] path, bool force, IProgressMonitor monitor)
		{
		}

		protected override void OnDeleteDirectories (FilePath[] path, bool force, IProgressMonitor monitor)
		{
		}
		
		public override Annotation[] GetAnnotations (MonoDevelop.Core.FilePath repositoryPath)
		{
			return new Annotation[0];
		}
		
		protected override RevisionPath[] OnGetRevisionChanges (Revision revision)
		{
			return new RevisionPath [0];
		}
	}
}
