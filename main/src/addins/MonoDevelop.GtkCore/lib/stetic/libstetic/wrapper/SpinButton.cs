using System;

namespace Stetic.Wrapper {

	public class SpinButton : Widget {

		public static new Gtk.SpinButton CreateInstance ()
		{
			return new Gtk.SpinButton (0.0, 100.0, 1.0);
		}
	}
}
