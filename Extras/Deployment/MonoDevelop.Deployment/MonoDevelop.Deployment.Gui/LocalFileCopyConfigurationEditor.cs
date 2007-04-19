
using System;
using MonoDevelop.Deployment;

namespace MonoDevelop.Deployment.Gui
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
