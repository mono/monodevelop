
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Gui
{
	public static class DeployOperations
	{
		public static void Install (SolutionItem entry, ConfigurationSelector configuration)
		{
			using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ()) {
				InstallDialog dlg = new InstallDialog (entry);
				try {
					if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok)
						DeployService.Install (mon, entry, dlg.Prefix, dlg.AppName, configuration);
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
			}
		}
		
		public static IAsyncOperation BuildPackages (PackagingProject project)
		{
			return BuildPackages (project.Packages);
		}
		
		static IAsyncOperation BuildPackages (ICollection packages)
		{
			IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (true);

			// Run the deploy command in a background thread to avoid
			// deadlocks with the gui thread
			
			System.Threading.Thread t = new System.Threading.Thread (
				delegate () {
				using (mon) {
					mon.BeginTask ("Creating packages", packages.Count);
					foreach (Package p in packages) {
						DeployService.BuildPackage (mon, p);
						mon.Step (1);
					}
					mon.EndTask ();
				}
			});
			t.IsBackground = true;
			t.Start ();

			return mon.AsyncOperation;
		}
		
		public static IAsyncOperation BuildPackage (Package package)
		{
			return BuildPackages (new object[] { package });
		}
		
		public static void ShowPackageSettings (Package package)
		{
			using (EditPackageDialog dlg = new EditPackageDialog (package)) {
				if (MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok)
					IdeApp.ProjectOperations.Save (package.ParentProject);
			}
		}
	}
}
