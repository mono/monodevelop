
using System;
using System.Collections;
using System.Linq;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	class CommitCommand
	{
		public static bool Commit (VersionControlItemList items, bool test)
		{
			int filesToCommit = 0;
			VersionControlItemList[] itemListsByRepo = items.SplitByRepository ();

			foreach (VersionControlItemList itemList in itemListsByRepo) {
				// Generate base folder path.
				FilePath basePath = itemList.FindMostSpecificParent ();
				Repository repo = itemList.First ().Repository;

				ChangeSet cset = repo.CreateChangeSet (basePath);
				cset.GlobalComment = VersionControlService.GetCommitComment (cset.BaseLocalPath);

				foreach (var item in itemList) {
					if (!item.VersionInfo.CanCommit)
						continue;

					if (item.Path.IsDirectory) {
						// We don't run checks for directories, we throw dialog if there are no changes.
						if (test)
							return true;

						foreach (VersionInfo vi in repo.GetDirectoryVersionInfo (item.Path, false, true))
							if (vi.HasLocalChanges) {
								filesToCommit++;
								cset.AddFile (vi);
							}
					} else {
						VersionInfo vi = repo.GetVersionInfo (item.Path);
						if (vi.HasLocalChanges) {
							if (test)
								return true;

							filesToCommit++;
							cset.AddFile (vi);
						}
					}
				}

				// In case of no local changes.
				if (test)
					return false;

				if (!cset.IsEmpty) {
					Commit (repo, cset, false);
				} else {
					MessageService.ShowMessage (GettextCatalog.GetString ("There are no changes to be committed."));
					continue;
				}
			}
			return filesToCommit != 0;
		}

		public static bool Commit (Repository vc, ChangeSet changeSet, bool test)
		{
			try {
				if (changeSet.IsEmpty) {
					if (!test)
						MessageService.ShowMessage (GettextCatalog.GetString ("There are no changes to be committed."));
					return false;
				}
				
				if (vc.GetVersionInfo (changeSet.BaseLocalPath).CanCommit) {
					if (test)
						return true;

					if (!VersionControlService.NotifyPrepareCommit (vc, changeSet))
						return false;
					CommitDialog dlg = new CommitDialog (changeSet);
					try {
						if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
							if (VersionControlService.NotifyBeforeCommit (vc, changeSet)) {
								new CommitWorker (vc, changeSet, dlg).Start();
								return true;
							}
						}
						dlg.EndCommit (false);
					} finally {
						dlg.Destroy ();
					}
					VersionControlService.NotifyAfterCommit (vc, changeSet, false);
				}
				return false;
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
				return false;
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
