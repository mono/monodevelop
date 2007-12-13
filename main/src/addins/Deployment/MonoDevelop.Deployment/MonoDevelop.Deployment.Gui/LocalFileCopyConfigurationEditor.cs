
using System;
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Targets;

namespace MonoDevelop.Deployment.Gui
{
	internal class LocalFileCopyConfigurationEditor: IFileCopyConfigurationEditor
	{
		public bool CanEdit (FileCopyConfiguration config)
		{
			return config != null && config.GetType () == typeof (LocalFileCopyConfiguration);
		}
		
		public Gtk.Widget CreateEditor (FileCopyConfiguration config)
		{
			return new LocalFileCopyConfigurationEditorWidget ((LocalFileCopyConfiguration) config);
		}
	}
}
