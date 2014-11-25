
using System;

namespace MonoDevelop.VersionControl
{
	public delegate void CommitEventHandler (object sender, CommitEventArgs args);
	
	public class CommitEventArgs: EventArgs
	{
		internal CommitEventArgs (Repository repo, ChangeSet cset, bool success)
		{
			ChangeSet = cset;
			Repository = repo;
			Success = success;
		}
		
		public ChangeSet ChangeSet {
			get;
			private set;
		}
		
		public Repository Repository {
			get;
			private set;
		}
		
		public bool Success {
			get;
			private set;
		}
	}
}
