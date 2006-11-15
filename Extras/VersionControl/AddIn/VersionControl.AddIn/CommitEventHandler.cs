
using System;
using VersionControl.Service;

namespace VersionControl.AddIn
{
	public delegate void CommitEventHandler (object sender, CommitEventArgs args);
	
	public class CommitEventArgs: EventArgs
	{
		ChangeSet cset;
		Repository repo;

		internal CommitEventArgs (Repository repo, ChangeSet cset)
		{
			this.cset = cset;
			this.repo = repo;
		}
		
		public ChangeSet ChangeSet {
			get { return cset; }
		}
		
		public Repository Repository {
			get { return repo; }
		}
	}
}
