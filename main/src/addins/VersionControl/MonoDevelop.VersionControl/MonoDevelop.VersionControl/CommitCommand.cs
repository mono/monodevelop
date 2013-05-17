
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
		// TODO: Split algorithm by repository to commit to?
		public static bool Commit (VersionControlItemList items, bool test)
		{
			int filesToCommit = 0;

			// Generate base folder path.
			var MatchingChars =
				from len in Enumerable.Range(0, items.Min (item => item.Path.ToString ().Length)).Reverse ()
				let possibleMatch = items.First ().Path.ToString ().Substring (0, len)
				where items.All (item => item.Path.ToString ().StartsWith (possibleMatch))
				select possibleMatch;

			string basePath = System.IO.Path.GetDirectoryName (MatchingChars.First ());
			Repository repo = items.First ().Repository;

			ChangeSet cset = repo.CreateChangeSet (basePath);
			cset.GlobalComment = VersionControlService.GetCommitComment (cset.BaseLocalPath);

			foreach (var item in items) {
				if (!item.VersionInfo.CanCommit)
					continue;
				
				LoggingService.LogError("{0} {1}", test ? "test" : "", item.VersionInfo.CanCommit ? " can commit" : " can't");
				filesToCommit++;
				if (test)
					continue;

				foreach (VersionInfo vi in item.Repository.GetDirectoryVersionInfo (item.Path, false, true))
					if (vi.HasLocalChanges)
						cset.AddFile (vi);
			}

			if (!cset.IsEmpty) {
				Commit (items.First ().Repository, cset, false);
			} else if (!test) {
				MessageService.ShowMessage (GettextCatalog.GetString ("There are no changes to be committed."));
				return false;
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
					if (test) return true;

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
