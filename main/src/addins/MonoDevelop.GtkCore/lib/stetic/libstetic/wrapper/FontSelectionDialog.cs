using System;

namespace Stetic.Wrapper {

	public class FontSelectionDialog : Dialog {

		public Gtk.FontSelection FontSelection {
			get {
				Gtk.Dialog dialog = (Gtk.Dialog)Wrapped;

				foreach (Gtk.Widget w in dialog.VBox) {
					if (w is Gtk.FontSelection)
						return (Gtk.FontSelection)w;
				}
				return null;
			}
		}
	}
}
