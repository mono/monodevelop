
using System.Collections;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Threading.Tasks;

namespace MonoDevelop.Deployment.Gui
{
	public static class DeployOperations
	{
		public static void Install (SolutionFolderItem entry, ConfigurationSelector configuration)
		{
			using (ProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ()) {
				InstallDialog dlg = new InstallDialog (entry);
				try {
					if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok)
						DeployService.Install (mon, entry, dlg.Prefix, dlg.AppName, configuration);
				} finally {
					dlg.Destroy ();
				}
			}
		}
		
		public static Task BuildPackages (PackagingProject project)
		{
			return BuildPackages (project.Packages);
		}
		
		static async Task BuildPackages (ICollection packages)
		{
			ProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (true);

			// Run the deploy command in a background thread to avoid
			// deadlocks with the gui thread
			
			using (mon) {
				mon.BeginTask ("Creating packages", packages.Count);
				foreach (Package p in packages) {
					await DeployService.BuildPackage (mon, p);
					mon.Step (1);
				}
				mon.EndTask ();
			}
		}
		
		public static Task BuildPackage (Package package)
		{
			return BuildPackages (new object[] { package });
		}
		
		public static void ShowPackageSettings (Package package)
		{
			EditPackageDialog dlg = new EditPackageDialog (package);
			if (MessageService.ShowCustomDialog (dlg) == (int) Gtk.ResponseType.Ok)
				IdeApp.ProjectOperations.SaveAsync (package.ParentProject);
		}
	}
}
