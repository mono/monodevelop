
using System;
using System.Collections;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace VersionControl.Service
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
		
		public override bool IsModified (string sourcefile)
		{
			return false;
		}
		
		public override bool IsVersioned (string sourcefile)
		{
			return false;
		}
		
		public override bool CanAdd (string sourcepath)
		{
			return false;
		}
		
		public override string GetPathToBaseText (string sourcefile)
		{
			return null;
		}
		
		public override string GetTextAtRevision (string repositoryPath, Revision revision)
		{
			return null;
		}
		
		public override Revision[] GetHistory (string sourcefile, Revision since)
		{
			return null;
		}
		
		public override VersionInfo GetVersionInfo (string sourcefile, bool getRemoteStatus)
		{
			return null;
		}
		
		public override VersionInfo[] GetDirectoryVersionInfo (string sourcepath, bool getRemoteStatus, bool recursive)
		{
			return null;
		}
		
		
		public override Repository Publish (string serverPath, string localPath, string[] files, string message, IProgressMonitor monitor)
		{
			return null;
		}

		public override void Update (string path, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Commit (string[] paths, string message, IProgressMonitor monitor)
		{
		}
		
		public override void Checkout (string path, Revision rev, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Add (string path, bool recurse, IProgressMonitor monitor)
		{
		}
		
		public override void Move (string srcPath, string destPath, Revision revision, bool force, IProgressMonitor monitor)
		{
		}
		
		public override void Delete (string path, bool force, IProgressMonitor monitor)
		{
		}
	}
}
