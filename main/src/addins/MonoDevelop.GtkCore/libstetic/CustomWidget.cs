
using System;

namespace Stetic
{
	// This widget is used at design-time to represent a Gtk.Bin container.
	// Gtk.Bin is the base class for custom widgets.
	
	public class CustomWidget: Gtk.EventBox
	{
		public CustomWidget (IntPtr ptr): base (ptr)
		{
		}
		
		public CustomWidget ()
		{
			this.VisibleWindow = false;
			this.Events |= Gdk.EventMask.ButtonPressMask;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			// Avoid forwarding event to parent widget
			return true;
		}
	}
}
