
using System;

namespace Stetic.Wrapper
{
	public class Misc: Widget
	{
		public void AlignLeft ()
		{
			((Gtk.Misc)Wrapped).Xalign = 0;
		}
		
		public void AlignRight ()
		{
			((Gtk.Misc)Wrapped).Xalign = 1;
		}
		
		public void AlignCenter ()
		{
			((Gtk.Misc)Wrapped).Xalign = 0.5f;
		}
	}
}
