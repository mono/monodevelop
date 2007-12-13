using System;
using System.Collections;
using System.IO;

namespace MonoDevelop.VersionControl
{
	public abstract class VersionControlSystem
	{
		public Repository CreateRepositoryInstance ()
		{
			Repository rep = OnCreateRepositoryInstance ();
			rep.VersionControlSystem = this;
			return rep;
		}
		
		public virtual string Id {
			get { return GetType().ToString(); }
		}
		
		public abstract string Name { get; }
		
		public virtual bool IsInstalled {
			get { return false; }
		}
		
		protected abstract Repository OnCreateRepositoryInstance ();
		public abstract Gtk.Widget CreateRepositoryEditor (Repository repo);
		
		public virtual Repository GetRepositoryReference (string path, string id)
		{
			return VersionControlService.InternalGetRepositoryReference (path, id);
		}
		
		public virtual void StoreRepositoryReference (Repository repo, string path, string id)
		{
			VersionControlService.InternalStoreRepositoryReference (repo, path, id);
		}
	}
	
	public delegate void UpdateCallback(string path, string action);
}
