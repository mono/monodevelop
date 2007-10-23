using System;

namespace Stetic.Wrapper {

	public class ToggleToolButton : ToolButton {

		public static new Gtk.ToolButton CreateInstance ()
		{
			return new Gtk.ToggleToolButton (Gtk.Stock.Bold);
		}
	}
}
