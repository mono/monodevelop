
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public class InstallDeployEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is InstallDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new InstallDeployEditorWidget ((InstallDeployTarget) target);
		}
	}
}
