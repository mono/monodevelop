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
		RevisionPath[] changedFiles; // only set by GetHistory; informative, not necessarily a file path
		
		protected Revision (Repository repo)
		{
			this.repo = repo;
		}
		
		protected Revision (Repository repo, DateTime time, string author, string message, RevisionPath[] changedFiles)
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
		public RevisionPath[] ChangedFiles {
			get { return changedFiles; }
			protected set { changedFiles = value; }
		}
		
		public override int GetHashCode() { return ToString().GetHashCode(); }
		public override bool Equals(object other) { return ToString() == other.ToString(); }
		
		protected Repository Repository {
			get { return repo; }
		}
	}
	
	public class RevisionPath
	{
		string path;
		RevisionAction action;
		string actionDescription;
		
		public RevisionPath (string path, RevisionAction action, string actionDescription)
		{
			this.path = path;
			this.action = action;
			this.actionDescription = actionDescription;
		}
		
		public string Path {
			get { return path; }
			set { path = value; }
		}
		
		public RevisionAction Action {
			get { return action; }
			set { action = value; }
		}
		
		// To use when Action == RevisionAction.Other
		public string ActionDescription {
			get { return actionDescription; }
			set { actionDescription = value; }
		}
	}
	
	public enum RevisionAction
	{
		Add,
		Delete,
		Replace,
		Modify,
		Other
	}
}
