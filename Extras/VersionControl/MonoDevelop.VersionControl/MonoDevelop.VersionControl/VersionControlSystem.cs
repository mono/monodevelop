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
	
	public class VCdir
	{
		public string Name;
		public string RealPath;
		public bool Loaded;
		public IList Nodes = new ArrayList();
		public VCdir(string path) {
			Loaded = false;
			RealPath = path;
			Name = DirList( path.Remove(0, path.LastIndexOf('/')) );
		}
		
		string DirList(string bla) {
			int c = bla.IndexOf('/');
			if (c >= 0)
				return bla.Remove(0,c+1);
			else return "";
		}
	}
}
