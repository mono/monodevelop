using System;
using System.IO;
using System.Collections;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Dialogs;

namespace MonoDevelop.VersionControl
{
	internal class CheckoutCommand : CommandHandler
	{
		protected override void Run()
		{
			if (!VersionControlService.CheckVersionControlInstalled ())
				return;
			
			SelectRepositoryDialog del = new SelectRepositoryDialog (SelectRepositoryMode.Checkout);
			del.TransientFor = IdeApp.Workbench.RootWindow;
			try {
				if (del.Run () == (int) Gtk.ResponseType.Ok && del.Repository != null) {
					CheckoutWorker w = new CheckoutWorker (del.Repository, del.TargetPath);
					w.Run ();
				}
			} finally {
				del.Destroy ();
			}
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
			Description = GettextCatalog.GetString ("Checkout from repository '{0}', path '{1}'", vc.Name, path);
		}
		
		protected override string GetDescription ()
		{
			return GettextCatalog.GetString ("Checkout {0}...", path);
		}
		
		protected override void RunTask () 
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
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (str)) {
						projectFn = str;
						break;
					}
				}	
			}
			
			if (projectFn != null)
				IdeApp.Workspace.OpenWorkspaceItem (projectFn);
		}
	}
}
