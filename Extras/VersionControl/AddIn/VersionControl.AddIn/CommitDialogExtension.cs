
using System;
using VersionControl.Service;

namespace VersionControl.AddIn
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
