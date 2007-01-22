
using System;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	public class LocalFileCopyConfigurationEditor: IFileCopyConfigurationEditor
	{
		public bool CanEdit (FileCopyConfiguration config)
		{
			return config is LocalFileCopyConfiguration;
		}
		
		public Gtk.Widget CreateEditor (FileCopyConfiguration config)
		{
			return new LocalFileCopyConfigurationEditorWidget ((LocalFileCopyConfiguration) config);
		}
	}
}
