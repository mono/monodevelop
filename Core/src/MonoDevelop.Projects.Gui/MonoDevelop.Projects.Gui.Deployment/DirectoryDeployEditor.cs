
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public class DirectoryDeployEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is DirectoryDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new DirectoryDeployEditorWidget ((DirectoryDeployTarget) target);
		}
	}
}
