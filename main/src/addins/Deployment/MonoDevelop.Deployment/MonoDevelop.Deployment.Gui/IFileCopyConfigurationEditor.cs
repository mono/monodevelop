
using System;
using MonoDevelop.Deployment;

namespace MonoDevelop.Deployment.Gui
{
	public interface IFileCopyConfigurationEditor
	{
		bool CanEdit (FileCopyConfiguration config);
		Gtk.Widget CreateEditor (FileCopyConfiguration config);
	}
}
