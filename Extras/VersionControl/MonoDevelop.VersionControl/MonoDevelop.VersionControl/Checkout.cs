using System;
using System.IO;
using System.Collections;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

using MonoDevelop.VersionControl.Dialogs;

namespace MonoDevelop.VersionControl
{
	public class CheckoutCommand : CommandHandler
	{
		protected override void Run()
		{
			SelectRepositoryDialog del = new SelectRepositoryDialog (SelectRepositoryMode.Checkout);
			using (del) {
				if (del.Run () == (int) Gtk.ResponseType.Ok) {
					CheckoutWorker w = new CheckoutWorker (del.Repository, del.TargetPath);
					w.Start ();
				}
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			bool enabled = false;
			
			foreach (VersionControlSystem vcs in VersionControlService.GetVersionControlSystems ()) {
				if (vcs.IsInstalled) {
					enabled = true;
					break;
				}
			}
			
			info.Enabled = enabled;
			info.Visible = enabled;
		}
	}
	
	class CheckoutWorker : Task
	{
		Repository vc;
		string path;
					
		public CheckoutWorker (Repository vc, string path)
		{
			this.vc = vc;
			this.path = path;
		}
		
		protected override string GetDescription ()
		{
			return "Checkout " + path + "...";
		}
		
		protected override void Run () 
		{
			vc.Checkout (path, null, true, GetProgressMonitor ());
			string projectFn = null;
			
			string[] list = System.IO.Directory.GetFiles(path);
			foreach (string str in list ) {
				if (str.EndsWith(".mds")) {
					projectFn = str;
					break;
				}
			}
			if ( projectFn == null ) {
				foreach ( string str in list ) {
					if (str.EndsWith(".mdp")) {
						projectFn = str;
						break;
					}
				}	
			}
			if ( projectFn == null ) {
				foreach (string str in list ) {
					if (MonoDevelop.Projects.Services.ProjectService.IsCombineEntryFile (str)) {
						projectFn = str;
						break;
					}
				}	
			}
			
			if (projectFn != null)
				IdeApp.ProjectOperations.OpenCombine (projectFn);
		}
	}
}
