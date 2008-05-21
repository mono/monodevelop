
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Deployment.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment
{
	public enum Commands
	{
		CreatePackage,
		Install,
		AddPackage
	}
	
	internal class CreatePackageHandler: CommandHandler
	{
		protected override void Run ()
		{
			SolutionItem entry = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
			DeployDialog dlg = new DeployDialog (entry, false);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					if (dlg.SaveToProject) {
						Package p = new Package ();
						p.Name = dlg.NewPackageName;
						p.PackageBuilder = dlg.PackageBuilder;
						
						if (dlg.CreateNewProject) {
							PackagingProject project = new PackagingProject ();
							project.Name = dlg.NewProjectName;
							project.FileName = Path.Combine (dlg.NewProjectSolution.BaseDirectory, project.Name + ".mdse");
							project.Packages.Add (p);
							dlg.NewProjectSolution.Items.Add (project);
							IdeApp.ProjectOperations.Save (dlg.NewProjectSolution.ParentSolution);
						}
						else {
							dlg.ExistingPackagingProject.Packages.Add (p);
							IdeApp.ProjectOperations.Save (dlg.ExistingPackagingProject);
						}
					}
					Package pkg = new Package (dlg.PackageBuilder);
					DeployOperations.BuildPackage (pkg);
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null;
		}
	}
	
	internal class AddPackageHandler: CommandHandler
	{
		protected override void Run ()
		{
			PackagingProject project = IdeApp.ProjectOperations.CurrentSelectedSolutionItem as PackagingProject;
			DeployDialog dlg = new DeployDialog (project.ParentFolder, true);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					project.AddPackage (dlg.NewPackageName, dlg.PackageBuilder);
					IdeApp.ProjectOperations.Save (project);
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem is PackagingProject;
		}
	}
	
	internal class InstallHandler: CommandHandler
	{
		protected override void Run ()
		{
			SolutionItem entry = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
			DeployOperations.Install (entry, IdeApp.Workspace.ActiveConfiguration);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedSolutionItem != null;
		}
	}
}
