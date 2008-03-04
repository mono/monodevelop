using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	internal class RevertCommand
	{
		public static bool Revert (Repository vc, string path, bool test)
		{
			return Revert (vc, new string[] {path}, test);
		}
		
		public static bool Revert (Repository vc, string[] path, bool test)
		{
			try {
				if (test) {
					foreach (string s in path)
						if (!vc.CanRevert (s))
							return false;
					return true;
				}

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to revert the changes done in the selected files?"), 
				                                GettextCatalog.GetString ("All changes made to the selected files will be permanently lost."),
				                                AlertButton.Cancel, AlertButton.Revert) != AlertButton.Revert)
					return false;

				new RevertWorker(vc, path).Start();
				return true;
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
				return false;
			}
		}

		private class RevertWorker : Task {
			Repository vc;
			string[] paths;
						
			public RevertWorker(Repository vc, string[] paths) {
				this.vc = vc;
				this.paths = paths;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Reverting ...");
			}
			
			protected override void Run ()
			{
				// A revert operation can create or remove a directory, so the directory
				// check must be done before and after the revert.
				
				ArrayList files = new ArrayList ();
				ArrayList dirs = new ArrayList ();
				foreach (string s in paths) {
					bool isDir = Directory.Exists (s);
					vc.Revert (s, true, GetProgressMonitor ());
					if (isDir || Directory.Exists (s))
						dirs.Add (s);
					else
						files.Add (s);
				}
				
				Gtk.Application.Invoke (delegate {
					foreach (string s in files) {
						// Reload reverted files
						Document doc = IdeApp.Workbench.GetDocument (s);
						if (doc != null)
							doc.Reload ();
						VersionControlService.NotifyFileStatusChanged (vc, s, false);
						FileService.NotifyFileChanged (s);
					}
					foreach (string s in dirs)
						VersionControlService.NotifyFileStatusChanged (vc, s, true);
				});
			}
		}
		
	}
}
