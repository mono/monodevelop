
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public partial class DirectoryDeployEditorWidget : Gtk.Bin
	{
		DirectoryDeployTarget target;
		
		public DirectoryDeployEditorWidget (DirectoryDeployTarget target)
		{
			this.Build();
			this.target = target;
			selector.Configuration = target.CopierConfiguration;
		}
		
		protected virtual void OnSelectorConfigurationChanged (object sender, System.EventArgs e)
		{
			if (target != null)
				target.CopierConfiguration = selector.Configuration;
		}
	}
}
