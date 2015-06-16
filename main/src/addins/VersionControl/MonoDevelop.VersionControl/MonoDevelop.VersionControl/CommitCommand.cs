
using System;
using System.Collections;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	class CommitCommand
	{
		public static void Commit (Repository vc, ChangeSet changeSet)
		{
			try {
				VersionControlService.NotifyPrepareCommit (vc, changeSet);

				CommitDialog dlg = new CommitDialog (changeSet);
				try {
					if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
						VersionControlService.NotifyBeforeCommit (vc, changeSet);
							new CommitWorker (vc, changeSet, dlg).Start();
							return;
						}
					dlg.EndCommit (false);
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
				VersionControlService.NotifyAfterCommit (vc, changeSet, false);
			}
			catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
			}
		}

		private class CommitWorker : Task
		{
			Repository vc;
			ChangeSet changeSet;
			CommitDialog dlg;
			bool success;
						
			public CommitWorker (Repository vc, ChangeSet changeSet, CommitDialog dlg)
			{
				this.vc = vc;
				this.changeSet = changeSet;
				this.dlg = dlg;
				OperationType = VersionControlOperationType.Push;
			}
			
			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Committing {0}...", changeSet.BaseLocalPath);
			}
			
			protected override void Run ()
			{
				success = true;
				try {
					// store global comment before commit.
					VersionControlService.SetCommitComment (changeSet.BaseLocalPath, changeSet.GlobalComment, true);
					
					vc.Commit (changeSet, Monitor);
					Monitor.ReportSuccess (GettextCatalog.GetString ("Commit operation completed."));
					
					// Reset the global comment on successful commit.
					VersionControlService.SetCommitComment (changeSet.BaseLocalPath, "", true);
				} catch {
					success = false;
					throw;
				}
			}
			
			protected override void Finished ()
			{
				dlg.EndCommit (success);
				dlg.Destroy ();
				FileUpdateEventArgs args = new FileUpdateEventArgs ();
				foreach (ChangeSetItem it in changeSet.Items)
					args.Add (new FileUpdateEventInfo (vc, it.LocalPath, it.IsDirectory));

				if (args.Count > 0)
					VersionControlService.NotifyFileStatusChanged (args);

				VersionControlService.NotifyAfterCommit (vc, changeSet, success);
			}
		}
	}
}
