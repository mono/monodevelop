
using System;

namespace Stetic.Editor
{
	public class IconSelectorMenuItem: Gtk.MenuItem
	{
		IconSelectorItem selector;
		
		public event IconEventHandler IconSelected;
		
		public IconSelectorMenuItem (IconSelectorItem item)
		{
			selector = item;
			Add (selector);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			if (IconSelected != null)
				IconSelected (this, new IconEventArgs (selector.SelectedIcon));
			selector.Destroy ();
			return base.OnButtonPressEvent (ev);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing ev)
		{
			return true;
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			selector.ProcessLeaveNotifyEvent (ev);
			return true;
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
			selector.ProcessMotionEvent ((int)ev.X - selector.Allocation.X + Allocation.X, (int)ev.Y - selector.Allocation.Y + Allocation.Y);
			return true;
		}
	}
	
	public delegate void IconEventHandler (object s, IconEventArgs args);
	
	public class IconEventArgs: EventArgs
	{
		string iconId;
		
		public IconEventArgs (string iconId)
		{
			this.iconId = iconId;
		}
		
		public string IconId {
			get { return iconId; }
		}
	}
}
