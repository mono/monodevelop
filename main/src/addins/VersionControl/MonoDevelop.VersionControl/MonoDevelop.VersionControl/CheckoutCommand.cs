using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.VersionControl.Dialogs;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	internal class CheckoutCommand : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = VersionControlService.CheckVersionControlInstalled ();
		}

		protected override void Run()
		{
			SelectRepositoryDialog del = new SelectRepositoryDialog (SelectRepositoryMode.Checkout);
			try {
				if (MessageService.RunCustomDialog (del) == (int) Gtk.ResponseType.Ok && del.Repository != null) {
					CheckoutWorker w = new CheckoutWorker (del.Repository, del.TargetPath);
					w.Start ();
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
			OperationType = VersionControlOperationType.Pull;
		}
		
		protected override string GetDescription ()
		{
			return GettextCatalog.GetString ("Checking out {0}...", path);
		}
		
		protected override IProgressMonitor CreateProgressMonitor ()
		{
			return new MonoDevelop.Core.ProgressMonitoring.AggregatedProgressMonitor (
				base.CreateProgressMonitor (),
				new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor (true, true, true, true)
			);
		}
		
		protected override void Run () 
		{
			vc.Checkout (path, null, true, Monitor);
			if (Monitor.IsCancelRequested) {
				Monitor.ReportSuccess (GettextCatalog.GetString ("Checkout operation cancelled"));
				return;
			}

			if (!System.IO.Directory.Exists (path)) {
				Monitor.ReportError (GettextCatalog.GetString ("Checkout folder does not exist"), null);
				return;
			}

			string projectFn = null;
			
			string[] list = System.IO.Directory.GetFiles(path);
			foreach (string str in list ) {
				if (str.EndsWith (".mds", System.StringComparison.Ordinal)) {
					projectFn = str;
					break;
				}
			}
			if ( projectFn == null ) {
				foreach ( string str in list ) {
					if (str.EndsWith (".mdp", System.StringComparison.Ordinal)) {
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
			
			if (projectFn != null) {
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workspace.OpenWorkspaceItem (projectFn);
				});
			}
			
			Monitor.ReportSuccess (GettextCatalog.GetString ("Solution checked out"));
		}
	}
}
