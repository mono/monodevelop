
using System;
using System.Collections.Specialized;
using VersionControl.Service;
using VersionControl.AddIn.Dialogs;

namespace VersionControl.AddIn
{
	class CommitCommand
	{
		public static bool Commit (Repository vc, string path, StringCollection filesToCommit, string message, bool test)
		{
			if (vc.CanCommit (path)) {
				if (test) return true;
				
				VersionInfo[] status = vc.GetDirectoryVersionInfo (path, false, true);
				using (CommitDialog dlg = new CommitDialog (status, path, filesToCommit)) {
					dlg.Message = message;
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						if (filesToCommit != null) {
							// Update the list of files to commit
							filesToCommit.Clear ();
							filesToCommit.AddRange (dlg.GetFilesToCommit ());
						}
						new CommitWorker (vc, path, dlg.GetFilesToCommit(), dlg.Message).Start();
						return true;
					}
				}
			}
			return false;
		}

		private class CommitWorker : Task
		{
			Repository vc;
			string path;
			string[] files;
			string message;
						
			public CommitWorker (Repository vc, string path, string[] files, string message)
			{
				this.vc = vc;
				this.path = path;
				this.files = files;
				this.message = message;
			}
			
			protected override string GetDescription()
			{
				return "Committing " + path + "...";
			}
			
			protected override void Run ()
			{
				vc.Commit (files, message, GetProgressMonitor ());
			}
		}
	}
}
