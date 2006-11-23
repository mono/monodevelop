using System;
using System.Collections;
using System.IO;

namespace MonoDevelop.VersionControl
{
	public abstract class Revision
	{
		Repository repo;
		DateTime time;
		string author;
		string message;
		string[] changedFiles; // only set by GetHistory; informative, not necessarily a file path
		
		protected Revision (Repository repo)
		{
			this.repo = repo;
		}
		
		protected Revision (Repository repo, DateTime time, string author, string message, string[] changedFiles)
		{
			this.repo = repo;
			this.time = time;
			this.author = author;
			this.message = message;
			this.changedFiles = changedFiles;
		}
		
		public abstract Revision GetPrevious ();
		
		public DateTime Time {
			get { return time; }
			protected set { time = value; }
		}
		
		public string Author {
			get { return author; }
			protected set { author = value; }
		}
		
		public string Message {
			get { return message; }
			protected set { message = value; }
		}
		
		// only set by GetHistory; informative, not necessarily a file path
		public string[] ChangedFiles {
			get { return changedFiles; }
			protected set { changedFiles = value; }
		}
		
		public override int GetHashCode() { return ToString().GetHashCode(); }
		public override bool Equals(object other) { return ToString() == other.ToString(); }
		
		protected Repository Repository {
			get { return repo; }
		}
	}
}
