
using System;

namespace Stetic.Wrapper
{
	public class Fixed: Container
	{
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			
			DND.DestSet (gtkfixed, true);
			gtkfixed.DragDrop += FixedDragDrop;
			gtkfixed.DragDataReceived += FixedDragDataReceived;
		}
		
		public override void Dispose ()
		{
			gtkfixed.DragDrop -= FixedDragDrop;
			gtkfixed.DragDataReceived -= FixedDragDataReceived;
		}
		
		Gtk.Fixed gtkfixed {
			get {
				return (Gtk.Fixed)Wrapped;
			}
		}
		
		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
		
		void FixedDragDrop (object obj, Gtk.DragDropArgs args)
		{
			Gtk.Widget w = DND.Drop (args.Context, gtkfixed, args.Time);
			Widget ww = Widget.Lookup (w);
			if (ww != null) {
				gtkfixed.Put (w, args.X - DND.DragHotX, args.Y - DND.DragHotY);
				NotifyChildAdded (w);
				args.RetVal = true;
				ww.Select ();
			}
		}

		void FixedDragDataReceived (object obj, Gtk.DragDataReceivedArgs args)
		{
			Widget dropped = WidgetUtils.Paste (proj, args.SelectionData);
			Gtk.Drag.Finish (args.Context, dropped != null, dropped != null, args.Time);
			if (dropped != null) {
				gtkfixed.Put (dropped.Wrapped, 0, 0);
				NotifyChildAdded (dropped.Wrapped);
				dropped.Select ();
			}
		}
		
		int dragX, dragY;

		protected override Gtk.Widget CreateDragSource (Gtk.Widget dragWidget)
		{
			Gtk.Fixed.FixedChild fc = (Gtk.Fixed.FixedChild) gtkfixed [dragWidget];
			if (fc == null)
				return null;
				
			dragX = fc.X;
			dragY = fc.Y;

			gtkfixed.Remove (dragWidget);
			gtkfixed.DragEnd += DragEnd;
			return gtkfixed;
		}
		
		void DragEnd (object obj, Gtk.DragEndArgs args)
		{
			using (UndoManager.AtomicChange) {
				gtkfixed.DragEnd -= DragEnd;
				if (DND.DragWidget != null) {
					DND.DragWidget.Unparent ();
					gtkfixed.Put (DND.DragWidget, dragX, dragY);
					NotifyChildAdded (DND.DragWidget);
					Widget ww = Widget.Lookup (DND.DragWidget);
					ww.Select ();
				}
			}
		}

		public class FixedChild : Container.ContainerChild {
		}
	}
}
