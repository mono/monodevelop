
using System;
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment.Linux
{
	
	
	public partial class BasicOptionPanelWidget : Gtk.Bin
	{
		CombineEntry entry;
		
		public BasicOptionPanelWidget (CombineEntry entry)
		{
			this.Build();
			
			WidgetFlags |= Gtk.WidgetFlags.NoShowAll;
			
			this.entry = entry;
			if (entry is DotNetProject) {
				DotNetProjectConfiguration conf = entry.ActiveConfiguration as DotNetProjectConfiguration;
				boxExe.Visible = (conf.CompileTarget == CompileTarget.Exe);
				boxLibrary.Visible = (conf.CompileTarget == CompileTarget.Library);
			} else {
				boxExe.Visible = boxLibrary.Visible = false;
			}
			
			LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (entry);
			checkScript.Active = data.GenerateScript;
			entryScript.Text = data.ScriptName;
			checkPcFile.Active = data.GeneratePcFile;
			checkDesktop.Active = data.GenerateDesktopEntry;
			entryScript.Sensitive = checkScript.Active;
		}
		
		public bool Store ()
		{
			LinuxDeployData data = LinuxDeployData.GetLinuxDeployData (entry);
			data.GenerateScript = checkScript.Active;
			data.ScriptName = entryScript.Text;
			data.GeneratePcFile = checkPcFile.Active;
			data.GenerateDesktopEntry = checkDesktop.Active;
			
			return true;
		}

		protected virtual void OnCheckScriptClicked(object sender, System.EventArgs e)
		{
			entryScript.Sensitive = checkScript.Active;
		}
	}
}
