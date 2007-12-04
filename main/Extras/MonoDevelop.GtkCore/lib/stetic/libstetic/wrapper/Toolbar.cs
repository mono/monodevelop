using System;
using System.Collections;

namespace Stetic.Wrapper {

	public class Toolbar : Container {

		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			toolbar.SizeAllocated += toolbar_SizeAllocated;
		}

		public override void Dispose ()
		{
			toolbar.SizeAllocated -= toolbar_SizeAllocated;
			base.Dispose ();
		}

		public override IEnumerable RealChildren {
			get {
				// Don't return Gtk.ToolItems that are only being used
				// to hold other non-ToolItem widgets. Just return the
				// contained widgets themselves.

				Gtk.Widget[] children = toolbar.Children;
				for (int i = 0; i < children.Length; i++) {
					if (children[i].GetType () == typeof (Gtk.ToolItem))
						children[i] = ((Gtk.ToolItem)children[i]).Child;
				}
				return children;
			}
		}

		Gtk.Toolbar toolbar {
			get {
				return (Gtk.Toolbar)Wrapped;
			}
		}

		public override bool HExpandable {
			get {
				return toolbar.Orientation == Gtk.Orientation.Horizontal;
			}
		}

		public override bool VExpandable {
			get {
				return toolbar.Orientation == Gtk.Orientation.Vertical;
			}
		}

		public Gtk.Orientation Orientation {
			get {
				return toolbar.Orientation;
			}
			set {
				toolbar.Orientation = value;
				EmitContentsChanged ();
			}
		}

		protected override void DoSync ()
		{
			DND.ClearFaults (this);
			Gtk.Orientation faultOrientation =
				Orientation == Gtk.Orientation.Horizontal ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal;
			Gdk.Rectangle tbAlloc = toolbar.Allocation;

			Gtk.Widget[] children = toolbar.Children;
			if (children.Length == 0) {
				DND.AddFault (this, 0, faultOrientation, tbAlloc);
				return;
			}

			if (faultOrientation == Gtk.Orientation.Horizontal) {
				DND.AddHFault (this, 0, null, children[0]);
				DND.AddHFault (this, children.Length, children[children.Length - 1], null);
			} else {
				DND.AddVFault (this, 0, null, children[0]);
				DND.AddVFault (this, children.Length, children[children.Length - 1], null);
			}

			for (int i = 1; i < children.Length; i++) {
				if (faultOrientation == Gtk.Orientation.Horizontal)
					DND.AddHFault (this, i, children[i - 1], children[i]);
				else
					DND.AddVFault (this, i, children[i - 1], children[i]);
			}
		}

		void toolbar_SizeAllocated (object obj, Gtk.SizeAllocatedArgs args)
		{
			Sync ();
		}

		// Insert widget at index, wrapping a ToolItem around it if needed
		void ToolItemize (Gtk.Widget widget, int index)
		{
			Gtk.ToolItem toolItem = widget as Gtk.ToolItem;
			if (toolItem == null) {
				toolItem = new Gtk.ToolItem ();
				toolItem.Show ();
				toolItem.Add (widget);
			}
			toolbar.Insert (toolItem, index);
		}

		// Remove widget (or its ToolItem parent), returning its position
		int ToolDeItemize (Gtk.Widget widget)
		{
			Gtk.ToolItem toolItem = widget as Gtk.ToolItem;
			if (toolItem == null) {
				toolItem = (Gtk.ToolItem)widget.Parent;
				toolItem.Remove (widget);
			}

			int index = toolbar.GetItemIndex (toolItem);

			toolbar.Remove (toolItem);
			if (toolItem != (widget as Gtk.ToolItem))
				toolItem.Destroy ();

			return index;
		}

		protected override void ReplaceChild (Gtk.Widget oldChild, Gtk.Widget newChild)
		{
			ToolItemize (newChild, ToolDeItemize (oldChild));
		}
		
		public override Placeholder AddPlaceholder ()
		{
			Placeholder ph = CreatePlaceholder ();
			ToolItemize (ph, 0);
			return ph;
		}

		int dragIndex;

		protected override Gtk.Widget CreateDragSource (Gtk.Widget dragWidget)
		{
			Gtk.Invisible invis = new Gtk.Invisible ();
			invis.Show ();
			invis.DragEnd += DragEnd;

			dragIndex = ToolDeItemize (dragWidget);
			return invis;
		}

		void DragEnd (object obj, Gtk.DragEndArgs args)
		{
			Gtk.Invisible invis = obj as Gtk.Invisible;
			invis.DragEnd -= DragEnd;
			invis.Destroy ();

			if (DND.DragWidget != null)
				ToolItemize (DND.Cancel (), dragIndex);
			dragIndex = -1;
		}

		public override void Drop (Gtk.Widget w, object faultId)
		{
			ToolItemize (w, (int)faultId);
			EmitContentsChanged ();
			Sync ();
		}

		public class ToolbarChild : Container.ContainerChild {
			public Gtk.ToolItem ToolItem {
				get {
					Gtk.Container.ContainerChild cc = (Gtk.Container.ContainerChild)Wrapped;
					return (Gtk.ToolItem)cc.Child;
				}
			}
		}
	}
}
