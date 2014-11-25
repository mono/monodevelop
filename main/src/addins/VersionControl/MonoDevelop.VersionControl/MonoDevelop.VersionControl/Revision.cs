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
		public RevisionPath (string path, RevisionAction action, string actionDescription)
		{
			this.Path = path;
			this.Action = action;
			this.ActionDescription = actionDescription;
		}
		
		public string Path {
			get;
			set;
		}
		
		public RevisionAction Action {
			get;
			set;
		}
		
		// To use when Action == RevisionAction.Other
		public string ActionDescription {
			get;
			set;
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
