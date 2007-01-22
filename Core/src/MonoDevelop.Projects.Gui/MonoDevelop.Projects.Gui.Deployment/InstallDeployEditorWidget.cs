
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public partial class InstallDeployEditorWidget : Gtk.Bin
	{
		InstallDeployTarget target;
		
		public InstallDeployEditorWidget (InstallDeployTarget target)
		{
			this.target = target;
			this.Build();
			folderEntry.Path = target.InstallPrefix;
			nameEntry.Text = target.ApplicationName;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			target.InstallPrefix = folderEntry.Path;
		}

		protected virtual void OnNameEntryChanged(object sender, System.EventArgs e)
		{
			target.ApplicationName = nameEntry.Text;
		}
	}
}
