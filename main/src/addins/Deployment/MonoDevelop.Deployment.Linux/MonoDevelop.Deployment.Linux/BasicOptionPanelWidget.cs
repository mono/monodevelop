
using System;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Deployment.Linux
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class BasicOptionPanelWidget : Gtk.Bin
	{
		SolutionItem entry;
		
		public BasicOptionPanelWidget (SolutionItem entry, bool creatingProject)
		{
			this.Build();
			
			WidgetFlags |= Gtk.WidgetFlags.NoShowAll;
			
			this.entry = entry;
			if (entry is DotNetProject) {
				DotNetProject project = (DotNetProject) entry;
				boxExe.Visible = (project.CompileTarget == CompileTarget.Exe || project.CompileTarget == CompileTarget.WinExe);
				boxLibrary.Visible = (project.CompileTarget == CompileTarget.Library || project.GetOutputFileName (ProjectService.DefaultConfiguration).FileName.EndsWith (".dll"));
			} else {
				boxExe.Visible = boxLibrary.Visible = false;
			}
			
			LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (entry);
			checkScript.Active = data.GenerateScript;
			entryScript.Text = data.ScriptName;
			checkPcFile.Active = data.GeneratePcFile;
			entryScript.Sensitive = checkScript.Active;
			
			if (!creatingProject)
				checkDesktop.Visible = false;
		}
		
		public string Validate ()
		{
			if (checkScript.Active && entryScript.Text.Length == 0)
				return GettextCatalog.GetString ("Script name not provided");
			return null;
		}
		
		public void Store ()
		{
			DotNetProject project = entry as DotNetProject;
			if (project == null)
				return;
			
			LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (project);
			data.GenerateScript = checkScript.Active;
			data.ScriptName = entryScript.Text;
			data.GeneratePcFile = checkPcFile.Active;
			
			if (checkDesktop.Active) {
				DesktopEntry de = new DesktopEntry ();
				de.SetEntry ("Encoding", "UTF-8");
				de.Type = DesktopEntryType.Application;
				de.Name = entry.Name;
				de.Exec = entryScript.Text;
				de.Terminal = false;
				string file = System.IO.Path.Combine (entry.BaseDirectory, "app.desktop");
				de.Save (file);
				ProjectFile pfile = project.AddFile (file, BuildAction.Content);
				DeployProperties props = DeployService.GetDeployProperties (pfile);
				props.TargetDirectory = LinuxTargetDirectory.DesktopApplications;
			}
		}

		protected virtual void OnCheckScriptClicked(object sender, System.EventArgs e)
		{
			entryScript.Sensitive = checkScript.Active;
		}
	}
}
