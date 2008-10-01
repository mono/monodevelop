using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	public class LinuxDeploymentSetup: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workspace.FileAddedToProject += OnFileAdded;
		}
		
		void OnFileAdded (object o, ProjectFileEventArgs a)
		{
			if (a.ProjectFile.Name.EndsWith (".desktop")) {
				DesktopEntry de = new DesktopEntry ();
				try {
					de.Load (a.ProjectFile.Name);
					a.ProjectFile.BuildAction = BuildAction.Content;
					DeployProperties props = DeployService.GetDeployProperties (a.ProjectFile);
					props.TargetDirectory = LinuxTargetDirectory.DesktopApplications;
					if (string.IsNullOrEmpty (de.Exec)) {
						LinuxDeployData dd = LinuxDeployData.GetLinuxDeployData (a.Project);
						if (dd.GenerateScript && !string.IsNullOrEmpty (dd.ScriptName)) {
							de.Exec = dd.ScriptName;
							de.Save (a.ProjectFile.Name);
						}
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Could not read .desktop file", ex);
				}
			}
		}
	}
}
