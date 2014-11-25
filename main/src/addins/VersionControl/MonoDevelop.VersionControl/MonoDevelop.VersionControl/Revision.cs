using System;
using System.Text;
using Mono.TextEditor;

namespace MonoDevelop.VersionControl
{
	public abstract class Revision
	{
		Repository repo;
		DateTime time;
		string author;
		string message;
		string shortMessage;
		
		protected Revision (Repository repo)
		{
			this.repo = repo;
		}
		
		protected Revision (Repository repo, DateTime time, string author, string message)
		{
			this.repo = repo;
			this.time = time;
			this.author = author;
			this.message = message;
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
			get { return message; }
			protected set { message = value; }
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

		internal static string FormatMessage (string msg)
		{
			StringBuilder sb = new StringBuilder ();
			bool wasWs = false;
			foreach (char ch in msg) {
				if (ch == ' ' || ch == '\t') {
					if (!wasWs)
						sb.Append (' ');
					wasWs = true;
					continue;
				}
				wasWs = false;
				sb.Append (ch);
			}

			var doc = TextDocument.CreateImmutableDocument (sb.ToString());
			foreach (var line in doc.Lines) {
				string text = doc.GetTextAt (line.Offset, line.Length).Trim ();
				int idx = text.IndexOf (':');
				if (text.StartsWith ("*", StringComparison.Ordinal) && idx >= 0 && idx < text.Length - 1) {
					int offset = line.EndOffsetIncludingDelimiter;
					msg = text.Substring (idx + 1) + doc.GetTextAt (offset, doc.TextLength - offset);
					break;
				}
			}
			return msg.TrimStart (' ', '\t');
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
