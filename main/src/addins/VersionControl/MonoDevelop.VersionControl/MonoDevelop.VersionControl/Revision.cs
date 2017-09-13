using System;
using System.Text;
using Mono.TextEditor;

namespace MonoDevelop.VersionControl
{
	public abstract class Revision
	{
		Repository repo;
		string shortMessage;
		
		protected Revision (Repository repo)
		{
			this.repo = repo;
		}
		
		protected Revision (Repository repo, DateTime time, string author, string message)
		{
			this.repo = repo;
			this.Time = time;
			this.Author = author;
			this.Message = message;
		}
		
		public abstract Revision GetPrevious ();
		
		public DateTime Time {
			get;
			protected set;
		}
		
		public string Author {
			get;
			protected set;
		}
		
		public string Email { get; set; }
		
		public string ShortMessage {
			get {
				if (shortMessage != null)
					return shortMessage;
				if (Message.Length > 80)
					return Message.Substring (0, 80);
				else
					return Message;
			}
			set {
				shortMessage = value;
			}
		}
		
		public string Message {
			get;
			protected set;
		}
		
		public override int GetHashCode() { return ToString().GetHashCode(); }
		public override bool Equals(object other) { return ToString() == other.ToString(); }
		
		protected Repository Repository {
			get { return repo; }
		}
		
		public virtual string Name {
			get { return ToString (); }
		}
		
		public virtual string ShortName {
			get { return Name; }
		}

		public static bool operator ==(Revision a, Revision b)
		{
			if (System.Object.ReferenceEquals(a, b))
			{
				return true;
			}

			if (((object)a == null) || ((object)b == null))
			{
				return false;
			}
			return a.Equals(b);
		}

		public static bool operator !=(Revision a, Revision b)
		{
			return !(a == b);
		}
	}
	
	public class RevisionPath
	{
		public RevisionPath (string path, string oldPath, RevisionAction action, string actionDescription)
		{
			this.Path = path;
			this.OldPath = oldPath;
			this.Action = action;
			this.ActionDescription = actionDescription;
		}

		public RevisionPath (string path, RevisionAction action, string actionDescription) : this (path, path, action, actionDescription)
		{
		}

		public string OldPath {
			get;
			private set;
		}
		
		public string Path {
			get;
			private set;
		}
		
		public RevisionAction Action {
			get;
			private set;
		}
		
		// To use when Action == RevisionAction.Other
		public string ActionDescription {
			get;
			private set;
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
