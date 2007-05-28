
using System;
using System.Collections;
using System.Collections.Specialized;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	class CommitCommand
	{
		public static bool Commit (Repository vc, string path, bool test)
		{
			if (vc.CanCommit (path)) {
				if (test) return true;
				ChangeSet cset = vc.CreateChangeSet (path);
				foreach (VersionInfo vi in vc.GetDirectoryVersionInfo (path, false, true))
					if (vi.NeedsCommit)
						cset.AddFile (vi);
				Commit (vc, cset, false);
			}
			return false;
		}
		
		public static bool Commit (Repository vc, ChangeSet changeSet, bool test)
		{
			try {
				if (vc.CanCommit (changeSet.BaseLocalPath)) {
					if (test) return true;

					if (!VersionControlProjectService.NotifyPrepareCommit (vc, changeSet))
						return false;
					CommitDialog dlg = new CommitDialog (changeSet);
					try {
						if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
							if (VersionControlProjectService.NotifyBeforeCommit (vc, changeSet)) {
								new CommitWorker (vc, changeSet, dlg).Start();
								return true;
							}
						}
						dlg.EndCommit (false);
					} finally {
						dlg.Destroy ();
					}
					VersionControlProjectService.NotifyAfterCommit (vc, changeSet, false);
				}
				return false;
			}
			catch (Exception ex) {
				if (test)
					Runtime.LoggingService.Error (ex);
				else
					IdeApp.Services.MessageService.ShowError (ex, GettextCatalog.GetString ("Version control command failed."));
				return false;
			}
		}

		private class CommitWorker : Task
		{
			Repository vc;
			ChangeSet changeSet;
			CommitDialog dlg;
						
			public CommitWorker (Repository vc, ChangeSet changeSet, CommitDialog dlg)
			{
				this.vc = vc;
				this.changeSet = changeSet;
				this.dlg = dlg;
			}
			
			protected override string GetDescription()
			{
				return "Committing " + changeSet.BaseLocalPath + "...";
			}
			
			protected override void Run ()
			{
				bool success = true;
				try {
					vc.Commit (changeSet, GetProgressMonitor ());
				} catch {
					success = false;
					throw;
				} finally {
					Gtk.Application.Invoke (delegate {
						dlg.EndCommit (success);
						dlg.Dispose ();
						VersionControlProjectService.NotifyAfterCommit (vc, changeSet, success);
						ArrayList dirs = new ArrayList ();
						ArrayList files = new ArrayList ();
						foreach (ChangeSetItem it in changeSet.Items)
							if (it.IsDirectory) dirs.Add (it.LocalPath);
							else files.Add (it.LocalPath);
						foreach (string path in dirs)
							VersionControlProjectService.NotifyFileStatusChanged (vc, path, true);
						foreach (string path in files)
							VersionControlProjectService.NotifyFileStatusChanged (vc, path, false);
					});
				}
			}
		}
	}
}
