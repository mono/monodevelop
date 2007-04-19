
using System;
using MonoDevelop.Deployment;

namespace MonoDevelop.Deployment.Gui
{
	public class CommandDeployEditor: IPackageBuilderEditor
	{
		public bool CanEdit (PackageBuilder target)
		{
			return target is CommandPackageBuilder;
		}
		
		public Gtk.Widget CreateEditor (PackageBuilder target)
		{
			return new CommandDeployEditorWidget ((CommandPackageBuilder) target);
		}
	}
}
