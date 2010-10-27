using System;
using System.Collections;

namespace Stetic.Wrapper {

	public class Viewport : Container {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			Unselectable = true;
		}
		
		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			Widget ww = Widget.Lookup (oldChild);
			if ((oldChild is Placeholder) && (ParentWrapper is ScrolledWindow) && newChild.SetScrollAdjustments (null, null)) {
				Widget wrapper = Widget.Lookup (newChild);
				wrapper.ShowScrollbars = false;
				ParentWrapper.ReplaceChild (Wrapped, newChild, false);
			} else if (ww != null && ww.ShowScrollbars && (ParentWrapper is ScrolledWindow) && ParentWrapper.ParentWrapper != null) {
				// The viewport is bound to the child widget. Remove it together with the child
				ParentWrapper.ParentWrapper.ReplaceChild (ParentWrapper.Wrapped, newChild, false);
			} else
				base.ReplaceChild (oldChild, newChild);
		}
	}
}
