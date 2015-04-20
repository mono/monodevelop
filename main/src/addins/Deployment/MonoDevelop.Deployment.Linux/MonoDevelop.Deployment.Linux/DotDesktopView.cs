
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
			entry = new DesktopEntry ();
			widget.DesktopEntry = entry;
		}
		
		public override string TabPageLabel {
			get {
				return MonoDevelop.Core.GettextCatalog.GetString ("Desktop Entry");
			}
		}
		
		public override void Load (FileOpenInformation fileOpenInformation)
		{
			ContentName = fileOpenInformation.FileName;
			entry.Load (fileOpenInformation.FileName);
			widget.DesktopEntry = entry;
		}
		
		public override void Save (FileSaveInformation fileSaveInformation)
		{
			entry.Save (fileSaveInformation.FileName);
			IsDirty = false;
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
	}
}
