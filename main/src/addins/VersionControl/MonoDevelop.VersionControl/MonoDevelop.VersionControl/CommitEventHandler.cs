
using System;

namespace MonoDevelop.VersionControl
{
	public delegate void CommitEventHandler (object sender, CommitEventArgs args);
	
	public class CommitEventArgs: EventArgs
	{
		ChangeSet cset;
		Repository repo;
		bool success;

		internal CommitEventArgs (Repository repo, ChangeSet cset, bool success)
		{
			this.cset = cset;
			this.repo = repo;
			this.success = success;
		}
		
		public ChangeSet ChangeSet {
			get { return cset; }
		}
		
		public Repository Repository {
			get { return repo; }
		}
		
		public bool Success {
			get { return success; }
		}
	}
}
