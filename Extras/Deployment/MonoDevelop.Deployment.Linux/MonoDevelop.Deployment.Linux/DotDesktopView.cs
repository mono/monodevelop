
using System;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment.Linux
{
	public class DotDesktopView: AbstractViewContent
	{
		DotDesktopViewWidget widget;
		DesktopEntry entry;
		
		public DotDesktopView ()
		{
			widget = new DotDesktopViewWidget ();
			widget.Changed += delegate {
				IsDirty = true;
			};
		}
		
		public override void Load (string fileName)
		{
			ContentName = fileName;
			entry = new DesktopEntry ();
			entry.Load (fileName);
			widget.DesktopEntry = entry;
		}
		
		public override void Save (string fileName)
		{
			entry.Save (fileName);
			IsDirty = false;
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}

	}
}
