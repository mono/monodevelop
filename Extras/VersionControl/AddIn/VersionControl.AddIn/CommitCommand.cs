
using System;
using System.Collections;
using System.Collections.Specialized;
using VersionControl.Service;
using VersionControl.AddIn.Dialogs;

namespace VersionControl.AddIn
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
			if (vc.CanCommit (changeSet.BaseLocalPath)) {
				if (test) return true;

				if (!VersionControlProjectService.NotifyPrepareCommit (vc, changeSet))
					return false;
				CommitDialog dlg = new CommitDialog (changeSet);
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					if (VersionControlProjectService.NotifyBeforeCommit (vc, changeSet)) {
						new CommitWorker (vc, changeSet, dlg).Start();
						return true;
					}
				}
				dlg.EndCommit (false);
				dlg.Dispose ();
				VersionControlProjectService.NotifyAfterCommit (vc, changeSet, false);
			}
			return false;
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
