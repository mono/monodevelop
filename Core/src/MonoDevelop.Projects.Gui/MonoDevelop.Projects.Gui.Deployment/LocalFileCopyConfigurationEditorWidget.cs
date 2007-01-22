
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public partial class LocalFileCopyConfigurationEditorWidget : Gtk.Bin
	{
		LocalFileCopyConfiguration config;
		
		public LocalFileCopyConfigurationEditorWidget (LocalFileCopyConfiguration config)
		{
			this.Build();
			this.config = config;
			folderEntry.Path = config.TargetDirectory;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			config.TargetDirectory = folderEntry.Path;
		}
	}
}
