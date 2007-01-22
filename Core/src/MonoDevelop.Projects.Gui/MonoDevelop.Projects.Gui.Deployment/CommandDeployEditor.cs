
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public class CommandDeployEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is CommandDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new CommandDeployEditorWidget ((CommandDeployTarget) target);
		}
	}
}
