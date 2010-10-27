
using System;

namespace Stetic.Wrapper
{
	public class Misc: Widget
	{
		public bool AlignLeft {
			get {
				return ((Gtk.Misc)Wrapped).Xalign == 0;
			}
			set {
				((Gtk.Misc)Wrapped).Xalign = 0;
			}
		}
		
		public bool AlignRight {
			get {
				return ((Gtk.Misc)Wrapped).Xalign == 1;
			}
			set {
				((Gtk.Misc)Wrapped).Xalign = 1;
			}
		}
		
		public bool AlignCenter {
			get {
				return ((Gtk.Misc)Wrapped).Xalign == 0.5f;
			}
			set {
				((Gtk.Misc)Wrapped).Xalign = 0.5f;
			}
		}
	}
}
