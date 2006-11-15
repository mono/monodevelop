
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
				cset.AddFiles (vc.GetDirectoryVersionInfo (path, false, true));
				Commit (vc, cset, false);
			}
			return false;
		}
		
		public static bool Commit (Repository vc, ChangeSet changeSet, bool test)
		{
			if (vc.CanCommit (changeSet.BaseLocalPath)) {
				if (test) return true;

				using (CommitDialog dlg = new CommitDialog (changeSet)) {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						if (VersionControlProjectService.NotifyBeforeCommit (vc, changeSet))
							new CommitWorker (vc, changeSet).Start();
						return true;
					}
				}
			}
			return false;
		}

		private class CommitWorker : Task
		{
			Repository vc;
			ChangeSet changeSet;
						
			public CommitWorker (Repository vc, ChangeSet changeSet)
			{
				this.vc = vc;
				this.changeSet = changeSet;
			}
			
			protected override string GetDescription()
			{
				return "Committing " + changeSet.BaseLocalPath + "...";
			}
			
			protected override void Run ()
			{
				vc.Commit (changeSet, GetProgressMonitor ());
				Gtk.Application.Invoke (delegate {
					VersionControlProjectService.NotifyAfterCommit (vc, changeSet);
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
