
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public class CombinedDeployEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is CombinedDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new CombinedDeployEditorWidget ((CombinedDeployTarget) target);
		}
	}
}
