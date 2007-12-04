
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
	}
}
