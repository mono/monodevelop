
using System;

namespace MonoDevelop.VersionControl
{
	public class CommitDialogExtension: Gtk.EventBox
	{
		public virtual void Initialize (ChangeSet changeSet)
		{
		}
		
		public virtual bool OnBeginCommit (ChangeSet changeSet)
		{
			return true;
		}
		
		public virtual void OnEndCommit (ChangeSet changeSet, bool success)
		{
		}
		
		internal event EventHandler AllowCommitChanged;
		
		bool allowCommit = true;
		
		protected internal bool AllowCommit {
			get { return allowCommit; }
			protected set {
				if (value == allowCommit)
					return;
				allowCommit = value;
				if (AllowCommitChanged != null)
					AllowCommitChanged (this, EventArgs.Empty);
			}
		}
	}
}
