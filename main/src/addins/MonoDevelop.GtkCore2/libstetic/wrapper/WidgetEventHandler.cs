using System;

namespace Stetic.Wrapper
{
	public delegate void WidgetEventHandler (object sender, WidgetEventArgs args);
	
	public class WidgetEventArgs: EventArgs
	{
		Stetic.Wrapper.Widget wrapper;
		Gtk.Widget widget;
		
		public WidgetEventArgs (Gtk.Widget widget)
		{
			this.widget = widget;
			wrapper = Stetic.Wrapper.Widget.Lookup (widget);
		}
		
		public WidgetEventArgs (Stetic.Wrapper.Widget wrapper)
		{
			this.wrapper = wrapper;
			if (wrapper != null)
				this.widget = wrapper.Wrapped;
		}
		
		public Gtk.Widget Widget {
			get { return widget; }
		}
		
		public Widget WidgetWrapper {
			get { return wrapper; }
		}
	}	
}
