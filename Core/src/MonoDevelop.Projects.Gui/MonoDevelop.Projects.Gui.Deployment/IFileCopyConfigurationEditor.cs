
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public interface IFileCopyConfigurationEditor
	{
		bool CanEdit (FileCopyConfiguration config);
		Gtk.Widget CreateEditor (FileCopyConfiguration config);
	}
}
