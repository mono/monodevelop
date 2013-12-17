
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
				if (vc.GetVersionInfo (changeSet.BaseLocalPath).CanCommit) {
					if (!VersionControlService.NotifyPrepareCommit (vc, changeSet))
						return;

					CommitDialog dlg = new CommitDialog (changeSet);
					try {
						if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
							if (VersionControlService.NotifyBeforeCommit (vc, changeSet)) {
								new CommitWorker (vc, changeSet, dlg).Start();
								return;
							}
						}
						dlg.EndCommit (false);
					} finally {
						dlg.Destroy ();
					}
					VersionControlService.NotifyAfterCommit (vc, changeSet, false);
				}
			}
			catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
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
				dlg.Dispose ();
				VersionControlService.NotifyAfterCommit (vc, changeSet, success);
				ArrayList dirs = new ArrayList ();
				ArrayList files = new ArrayList ();
				foreach (ChangeSetItem it in changeSet.Items)
					if (it.IsDirectory) dirs.Add (it.LocalPath);
					else files.Add (it.LocalPath);
				
				FileUpdateEventArgs args = new FileUpdateEventArgs ();
				
				foreach (FilePath path in dirs)
					args.Add (new FileUpdateEventInfo (vc, path, true));
				foreach (FilePath path in files)
					args.Add (new FileUpdateEventInfo (vc, path, false));
				
				if (args.Count > 0)
					VersionControlService.NotifyFileStatusChanged (args);
			}
		}
	}
}
