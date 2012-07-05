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
				new MonoDevelop.Ide.ProgressMonitoring.MessageDialogProgressMonitor (true, true, true, true),
				base.CreateProgressMonitor ()
			);
		}
		
		protected override void Run () 
		{
			vc.Checkout (path, null, true, Monitor);
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
			
			if (projectFn != null) {
				DispatchService.GuiDispatch (delegate {
					IdeApp.Workspace.OpenWorkspaceItem (projectFn);
				});
			}
			
			Monitor.ReportSuccess (GettextCatalog.GetString ("Solution checked out"));
		}
	}
}
