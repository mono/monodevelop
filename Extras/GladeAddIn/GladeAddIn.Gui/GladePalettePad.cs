
using System;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace GladeAddIn.Gui
{
	public class GladePalettePad: AbstractPadContent
	{
		Gtk.Widget widget;
		
		public GladePalettePad (): base ("")
		{
			try {
				widget = GladeService.App.Palette;
				widget.ShowAll ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
	}
}
