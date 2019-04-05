using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	public abstract class Revision
	{
		string shortMessage;
		
		protected Revision (Repository repo)
		{
			Repository = repo;
		}
		
		protected Revision (Repository repo, DateTime time, string author, string message)
		{
			Repository = repo;
			Time = time;
			Author = author;
			Message = message;
		}
		
		public abstract Task<Revision> GetPreviousAsync (CancellationToken cancellationToken = default);
		
		public DateTime Time {
			get;
			protected set;
		}
		
		public string Author {
			get;
			protected set;
		}
		
		public string Email { get; set; }

		const int maxDefaultShortMessageLineLength = 80;

		public string ShortMessage {
			get {
				if (shortMessage != null)
					return shortMessage;
				if (Message.Length > maxDefaultShortMessageLineLength)
					return shortMessage = Message.Substring (0, maxDefaultShortMessageLineLength);
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
		
		public override int GetHashCode() => ToString().GetHashCode();

		public override bool Equals(object obj) => ToString() == obj?.ToString();

		protected Repository Repository { get; }

		public virtual string Name {
			get { return ToString (); }
		}
		
		public virtual string ShortName {
			get { return Name; }
		}

		public static bool operator ==(Revision a, Revision b)
		{
			if (ReferenceEquals (a, b))
			{
				return true;
			}

			if (a is null || b is null)
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
			Path = path;
			OldPath = oldPath;
			Action = action;
			ActionDescription = actionDescription;
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
