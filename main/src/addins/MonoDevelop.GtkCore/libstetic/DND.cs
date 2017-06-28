using System;
using System.Collections;

namespace Stetic {

	public static class DND {
		static Gtk.TargetEntry[] targets;
		static Gtk.TargetList targetList;
		static Gdk.Atom steticWidgetType;
		static Gdk.Pixbuf widgetIcon;

		const int SteticType = 0;
		const int GladeType = 1;

		static DND ()
		{
			try {
				widgetIcon = Gdk.Pixbuf.LoadFromResource ("widget.png");
			} catch (Exception e) {
				Console.WriteLine ("Error while loading pixbuf 'widget.png': " + e);
			}
			
			steticWidgetType = Gdk.Atom.Intern ("application/x-stetic-widget", false);

			targets = new Gtk.TargetEntry[2];
			targets[0] = new Gtk.TargetEntry (steticWidgetType, 0, SteticType);
			targets[1] = new Gtk.TargetEntry ((string) GladeUtils.ApplicationXGladeAtom, 0, GladeType);

			targetList = new Gtk.TargetList (targets);
			targets = (Gtk.TargetEntry[]) targetList;
		}

		public static Gtk.TargetEntry[] Targets {
			get { return targets; }
		}

		public static void SourceSet (Gtk.Widget source)
		{
			Gtk.Drag.SourceSet (source, Gdk.ModifierType.Button1Mask,
					    targets, Gdk.DragAction.Move);
		}

		public static void SourceUnset (Gtk.Widget source)
		{
			Gtk.Drag.SourceUnset (source);
		}

		public static void DestSet (Gtk.Widget dest, bool automatic)
		{
			Gtk.Drag.DestSet (dest, automatic ? Gtk.DestDefaults.All : 0,
					  targets, Gdk.DragAction.Move | Gdk.DragAction.Copy);
		}

		public static void DestUnset (Gtk.Widget dest)
		{
			Gtk.Drag.DestUnset (dest);
		}

		static Gtk.Widget dragWidget;
		static WidgetDropCallback dropCallback;
		static int dragHotX;
		static int dragHotY;

		// Drag function for non-automatic sources, called from MotionNotifyEvent
		public static void Drag (Gtk.Widget source, Gdk.Event evt, Gtk.Widget dragWidget)
		{
			Gdk.DragContext ctx;

			ctx = Gtk.Drag.Begin (source, targetList, Gdk.DragAction.Move,
					      1 /* button */, evt);
			Drag (source, ctx, dragWidget);
		}

		// Drag function for automatic sources, called from DragBegin
		public static void Drag (Gtk.Widget source, Gdk.DragContext ctx, WidgetDropCallback dropCallback, string label)
		{
			Gtk.Frame fr = new Gtk.Frame ();
			fr.ShadowType = Gtk.ShadowType.Out;
			Gtk.HBox box = new Gtk.HBox ();
			box.Spacing = 3;
			box.BorderWidth = 3;
			box.PackStart (new Gtk.Image (widgetIcon), false, false, 0);
			Gtk.Label lab = new Gtk.Label (label);
			lab.Xalign = 0;
			box.PackStart (lab, true, true, 0);
			fr.Add (box);
			fr.ShowAll ();
			Drag (source, ctx, dropCallback, fr);
		}
		
		// Drag function for automatic sources, called from DragBegin
		public static void Drag (Gtk.Widget source, Gdk.DragContext ctx, Gtk.Widget dragWidget)
		{
			Drag (source, ctx, null, dragWidget);
		}
		
		// Drag function for automatic sources, called from DragBegin
		static void Drag (Gtk.Widget source, Gdk.DragContext ctx, WidgetDropCallback dropCallback, Gtk.Widget dragWidget)
		{
			if (ctx == null)
				return;

			Gtk.Window dragWin;
			Gtk.Requisition req;

			ShowFaults ();
			DND.dragWidget = dragWidget;
			DND.dropCallback = dropCallback;

			dragWin = new Gtk.Window (Gtk.WindowType.Popup);
			dragWin.Add (dragWidget);

			req = dragWidget.SizeRequest ();
			if (req.Width < 20 && req.Height < 20)
				dragWin.SetSizeRequest (20, 20);
			else if (req.Width < 20)
				dragWin.SetSizeRequest (20, -1);
			else if (req.Height < 20)
				dragWin.SetSizeRequest (-1, 20);

			req = dragWin.SizeRequest ();
			if (ctx.SourceWindow != null) {
				int px, py, rx, ry;
				Gdk.ModifierType pmask;
				ctx.SourceWindow.GetPointer (out px, out py, out pmask);
				ctx.SourceWindow.GetRootOrigin (out rx, out ry);
	
				dragWin.Move (rx + px, ry + py);
				dragWin.Show ();
				
				dragHotX = req.Width / 2;
				dragHotY = -3;
				
				Gtk.Drag.SetIconWidget (ctx, dragWin, dragHotX, dragHotY);
			}

			if (source != null) {
				source.DragDataGet += DragDataGet;
				source.DragEnd += DragEnded;
			}
		}

		public static Gtk.Widget DragWidget {
			get {
				return dragWidget;
			}
		}

		public static int DragHotX {
			get {
				return dragHotX;
			}
		}

		public static int DragHotY {
			get {
				return dragHotY;
			}
		}

		// Call this from a DragDrop event to receive the dragged widget
		public static void Drop (Gdk.DragContext ctx, uint time, ObjectWrapper targetWrapper, string dropData)
		{
			if (dropCallback == null) {
				Gtk.Widget w = Drop (ctx, (Gtk.Widget) targetWrapper.Wrapped, time);
				targetWrapper.DropObject (dropData, w);
				return;
			}
			
			Cancel ();
			Gtk.Drag.Finish (ctx, true, true, time);
			
			Gtk.Application.Invoke ((args, e) => {
				IProject project = targetWrapper.Project;
				string uid = targetWrapper.UndoId;
				string tname = ((Wrapper.Widget)targetWrapper).GetTopLevel ().Wrapped.Name;
				
				// This call may cause the project to be reloaded
				dragWidget = dropCallback ();
				if (dragWidget == null)
					return;
				
				if (targetWrapper.IsDisposed) {
					// The project has been reloaded. Find the wrapper again.
					Gtk.Widget twidget = project.GetTopLevel (tname);
					ObjectWrapper ow = ObjectWrapper.Lookup (twidget);
					if (ow != null)
						targetWrapper = ow.FindObjectByUndoId (uid);
					else
						targetWrapper = null;
					
					if (targetWrapper == null) {
						// Target wrapper not found. Just ignore the drop.
						return;
					}
				}
				
				targetWrapper.DropObject (dropData, dragWidget);
			});
		}
		
		public static Gtk.Widget Drop (Gdk.DragContext ctx, Gtk.Widget target, uint time)
		{
			if (dropCallback != null) {
				dragWidget = dropCallback ();
			}
		
			if (dragWidget == null) {
				Gtk.Drag.GetData (target, ctx, GladeUtils.ApplicationXGladeAtom, time);
				return null;
			}

			Gtk.Widget w = Cancel ();
			Gtk.Drag.Finish (ctx, true, true, time);
			return w;
		}

		// Call this from a DragEnd event to check if the widget wasn't dropped
		public static Gtk.Widget Cancel ()
		{
			if (dragWidget == null)
				return null;

			Gtk.Widget w = dragWidget;
			dragWidget = null;

			// Remove the widget from its dragWindow
			Gtk.Container parent = w.Parent as Gtk.Container;
			if (parent != null) {
				parent.Remove (w);
				parent.Destroy ();
			}
			return w;
		}

		static void DragEnded (object obj, Gtk.DragEndArgs args)
		{
			dragWidget = null;
			HideFaults ();

			((Gtk.Widget)obj).DragEnd -= DragEnded;
			((Gtk.Widget)obj).DragDataGet -= DragDataGet;
		}

		static void DragDataGet (object obj, Gtk.DragDataGetArgs args)
		{
			if (args.Info == GladeType) {
				Gtk.Widget w = Cancel ();
				if (w != null)
					WidgetUtils.Copy (w, args.SelectionData, false);
			}
		}

		class Fault {
			public Stetic.Wrapper.Widget Owner;
			public object Id;
			public Gtk.Orientation Orientation;
			public Gdk.Window Window;

			public Fault (Stetic.Wrapper.Widget owner, object id,
				      Gtk.Orientation orientation, Gdk.Window window)
			{
				Owner = owner;
				Id = id;
				Orientation = orientation;
				Window = window;
			}
		}

		static Hashtable faultGroups = new Hashtable ();
		const int FaultOverlap = 3;

		public static void AddFault (Stetic.Wrapper.Widget owner, object faultId,
					     Gtk.Orientation orientation, Gdk.Rectangle fault)
		{
			AddFault (owner, faultId, orientation,
				  fault.X, fault.Y, fault.Width, fault.Height);
		}

		public static void AddFault (Stetic.Wrapper.Widget owner, object faultId,
					     Gtk.Orientation orientation,
					     int x, int y, int width, int height)
		{
			Gtk.Widget widget = owner.Wrapped;
			if (!widget.IsRealized)
				return;

			Gdk.Window win = NewWindow (widget, Gdk.WindowClass.InputOnly);
			win.MoveResize (x, y, width, height);

			Hashtable widgetFaults = faultGroups[widget] as Hashtable;
			if (widgetFaults == null) {
				faultGroups[widget] = widgetFaults = new Hashtable ();
				widget.Destroyed += FaultWidgetDestroyed;
				widget.DragMotion += FaultDragMotion;
				widget.DragLeave += FaultDragLeave;
				widget.DragDrop += FaultDragDrop;
				widget.DragDataReceived += FaultDragDataReceived;
				DND.DestSet (widget, false);
			}
			widgetFaults[win] = new Fault (owner, faultId, orientation, win);
		}

		public static void AddFault (Stetic.Wrapper.Widget owner, object faultId,
					     Gtk.Orientation orientation,
					     Gtk.Widget before, Gtk.Widget after)
		{
			if (orientation == Gtk.Orientation.Horizontal)
				AddHFault (owner, faultId, before, after);
			else
				AddVFault (owner, faultId, before, after);
		}

		public static void AddHFault (Stetic.Wrapper.Widget owner, object faultId,
					      Gtk.Widget above, Gtk.Widget below)
		{
			Gtk.Widget widget = owner.Wrapped;
			if (!widget.IsRealized)
				return;

			Gdk.Rectangle aboveAlloc, belowAlloc;
			int x1, y1, x2, y2;

			if (above != null && below != null) {
				aboveAlloc = above.Allocation;
				belowAlloc = below.Allocation;

				x1 = Math.Min (aboveAlloc.X, belowAlloc.X);
				x2 = Math.Max (aboveAlloc.X + aboveAlloc.Width, belowAlloc.X + belowAlloc.Width);
				y1 = aboveAlloc.Y + aboveAlloc.Height;
				y2 = belowAlloc.Y;

				while (y2 - y1 < FaultOverlap * 2) {
					y1--;
					y2++;
				}
			} else if (above == null) {
				belowAlloc = below.Allocation;

				x1 = belowAlloc.X;
				x2 = belowAlloc.X + belowAlloc.Width;
				y1 = 0;
				y2 = Math.Max (belowAlloc.Y, FaultOverlap);
			} else {
				aboveAlloc = above.Allocation;

				x1 = aboveAlloc.X;
				x2 = aboveAlloc.X + aboveAlloc.Width;
				y1 = Math.Min (aboveAlloc.Y + aboveAlloc.Height, widget.Allocation.Height - FaultOverlap);
				y2 = widget.Allocation.Height;
			}

			AddFault (owner, faultId, Gtk.Orientation.Horizontal,
				  x1, y1, x2 - x1, y2 - y1);
		}

		public static void AddVFault (Stetic.Wrapper.Widget owner, object faultId,
					      Gtk.Widget left, Gtk.Widget right)
		{
			Gtk.Widget widget = owner.Wrapped;
			if (!widget.IsRealized)
				return;

			Gdk.Rectangle leftAlloc, rightAlloc;
			int x1, y1, x2, y2;

			if (left != null && right != null) {
				leftAlloc = left.Allocation;
				rightAlloc = right.Allocation;

				x1 = leftAlloc.X + leftAlloc.Width;
				x2 = rightAlloc.X;

				y1 = Math.Min (leftAlloc.Y, rightAlloc.Y);
				y2 = Math.Max (leftAlloc.Y + leftAlloc.Height, rightAlloc.Y + rightAlloc.Height);

				while (x2 - x1 < FaultOverlap * 2) {
					x1--;
					x2++;
				}
			} else if (left == null) {
				rightAlloc = right.Allocation;

				x1 = 0;
				x2 = Math.Max (rightAlloc.X, FaultOverlap);

				y1 = rightAlloc.Y;
				y2 = rightAlloc.Y + rightAlloc.Height;
			} else {
				leftAlloc = left.Allocation;

				x1 = Math.Min (leftAlloc.X + leftAlloc.Width, widget.Allocation.Width - FaultOverlap);
				x2 = widget.Allocation.Width;

				y1 = leftAlloc.Y;
				y2 = leftAlloc.Y + leftAlloc.Height;
			}

			AddFault (owner, faultId, Gtk.Orientation.Vertical,
				  x1, y1, x2 - x1, y2 - y1);
		}

		public static void AddFault (Stetic.Wrapper.Widget owner, object faultId,
					     Gtk.SideType side, Gtk.Widget widget)
		{
			Gdk.Rectangle fault;
			Gtk.Orientation orientation;

			if (widget == null) {
				fault = owner.Wrapped.Allocation;
				int border = (int)((Gtk.Container)owner.Wrapped).BorderWidth;
				fault.Inflate (-border, -border);
			} else
				fault = widget.Allocation;

			switch (side) {
			case Gtk.SideType.Top:
				fault.Y -= FaultOverlap;
				fault.Height = 2 * FaultOverlap;
				orientation = Gtk.Orientation.Horizontal;
				break;
			case Gtk.SideType.Bottom:
				fault.Y += fault.Height - FaultOverlap;
				fault.Height = 2 * FaultOverlap;
				orientation = Gtk.Orientation.Horizontal;
				break;
			case Gtk.SideType.Left:
				fault.X -= FaultOverlap;
				fault.Width = 2 * FaultOverlap;
				orientation = Gtk.Orientation.Vertical;
				break;
			case Gtk.SideType.Right:
				fault.X += fault.Width - FaultOverlap;
				fault.Width = 2 *FaultOverlap;
				orientation = Gtk.Orientation.Vertical;
				break;
			default:
				throw new Exception ("not reached");
			}

			AddFault (owner, faultId, orientation, fault);
		}

		static void FaultWidgetDestroyed (object widget, EventArgs args)
		{
			ClearFaults ((Gtk.Widget)widget);
		}

		public static void ClearFaults (Stetic.Wrapper.Widget owner)
		{
			ClearFaults (owner.Wrapped);
		}

		static void ClearFaults (Gtk.Widget widget)
		{
			Hashtable widgetFaults = faultGroups[widget] as Hashtable;
			if (widgetFaults == null)
				return;
			faultGroups.Remove (widget);
			widget.Destroyed -= FaultWidgetDestroyed;
			widget.DragMotion -= FaultDragMotion;
			widget.DragLeave -= FaultDragLeave;
			widget.DragDrop -= FaultDragDrop;
			widget.DragDataReceived -= FaultDragDataReceived;

			foreach (Gdk.Window win in widgetFaults.Keys)
				win.Destroy ();
			widgetFaults.Clear ();
			DND.DestUnset (widget);
		}

		static void ShowFaults ()
		{
			foreach (Hashtable widgetFaults in faultGroups.Values) {
				foreach (Gdk.Window win in widgetFaults.Keys)
					win.Show ();
			}
		}

		static void HideFaults ()
		{
			foreach (Hashtable widgetFaults in faultGroups.Values) {
				foreach (Gdk.Window win in widgetFaults.Keys)
					win.Hide ();
			}
			DestroySplitter ();
			dragFault = null;
		}

		static Fault dragFault;
		static Gdk.Window splitter;

		static void DestroySplitter ()
		{
			if (splitter != null) {
				splitter.Hide ();
				splitter.Destroy ();
				splitter = null;
			}
		}

		static Fault FindFault (int x, int y, Gtk.Widget w)
		{
			int wx, wy, width, height, depth;

			Hashtable widgetFaults  = (Hashtable) faultGroups [w];
			if (widgetFaults == null)
				return null;
				
			foreach (Fault f in widgetFaults.Values) {
				f.Window.GetGeometry (out wx, out wy, out width, out height, out depth);
				if (x >= wx && y >= wy && x <= wx + width && y <= wy + height) {
					return f;
				}
			}
			return null;
		}

		static void FaultDragMotion (object obj, Gtk.DragMotionArgs args)
		{
			int wx, wy, width, height, depth;
			
			Gtk.Widget widget = (Gtk.Widget) obj;
			int px = args.X + widget.Allocation.X;
			int py = args.Y + widget.Allocation.Y;
			
			Fault fault = FindFault (px, py, widget);

			// If there's a splitter visible, and we're not currently dragging
			// in the fault that owns that splitter, hide it
			if (splitter != null && dragFault != fault)
				DestroySplitter ();

			if (dragFault != fault) {
				dragFault = fault;
				if (dragFault == null)
					return;

				splitter = NewWindow (fault.Owner.Wrapped, Gdk.WindowClass.InputOutput);
				fault.Window.GetGeometry (out wx, out wy, out width, out height, out depth);
				if (fault.Orientation == Gtk.Orientation.Horizontal) {
					splitter.MoveResize (wx, wy + height / 2 - FaultOverlap,
							     width, 2 * FaultOverlap);
				} else {
					splitter.MoveResize (wx + width / 2 - FaultOverlap, wy,
							     2 * FaultOverlap, height);
				}
				splitter.ShowUnraised ();
				fault.Window.Lower ();
			} else if (dragFault == null)
				return;

			Gdk.Drag.Status (args.Context, Gdk.DragAction.Move, args.Time);
			args.RetVal = true;
		}

		static void FaultDragLeave (object obj, Gtk.DragLeaveArgs args)
		{
			DestroySplitter ();
			dragFault = null;
		}

		static void FaultDrop (Stetic.Wrapper.Widget wrapper, int x, int y, Gtk.Widget targetWidget)
		{
			Fault fault = FindFault (x, y, targetWidget);
			if (fault != null) {
				fault.Owner.Drop (wrapper.Wrapped, fault.Id);
				wrapper.Select ();
			}
		}

		static void FaultDragDrop (object obj, Gtk.DragDropArgs args)
		{
			Gtk.Widget w = DND.Drop (args.Context, (Gtk.Widget)obj, args.Time);
			Stetic.Wrapper.Widget dropped = Stetic.Wrapper.Widget.Lookup (w);
			if (dropped != null) {
				Gtk.Widget targetWidget = (Gtk.Widget) obj;
				int px = args.X + targetWidget.Allocation.X;
				int py = args.Y + targetWidget.Allocation.Y;
			
				FaultDrop (dropped, px, py, targetWidget);
				args.RetVal = true;
			}
		}

		static void FaultDragDataReceived (object obj, Gtk.DragDataReceivedArgs args)
		{
			Stetic.Wrapper.Widget dropped = null;

			Stetic.Wrapper.Widget faultOwner = Stetic.Wrapper.Widget.Lookup ((Gtk.Widget)obj);
			if (faultOwner != null)
				dropped = WidgetUtils.Paste (faultOwner.Project, args.SelectionData);
			Gtk.Drag.Finish (args.Context, dropped != null,
					 dropped != null, args.Time);
			if (dropped != null) {
				Gtk.Widget targetWidget = (Gtk.Widget) obj;
				int px = args.X + targetWidget.Allocation.X;
				int py = args.Y + targetWidget.Allocation.Y;
				FaultDrop (dropped, px, py, targetWidget);
			}
		}

		static Gdk.Window NewWindow (Gtk.Widget parent, Gdk.WindowClass wclass)
		{
			Gdk.WindowAttr attributes;
			Gdk.WindowAttributesType attributesMask;
			Gdk.Window win;

			attributes = new Gdk.WindowAttr ();
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = wclass ;
			attributes.Visual = parent.Visual;
			attributes.Colormap = parent.Colormap;
			attributes.Mask = (Gdk.EventMask.ButtonPressMask |
					   Gdk.EventMask.ButtonMotionMask |
					   Gdk.EventMask.ButtonReleaseMask |
					   Gdk.EventMask.ExposureMask |
					   Gdk.EventMask.EnterNotifyMask |
					   Gdk.EventMask.LeaveNotifyMask);

			attributesMask =
				Gdk.WindowAttributesType.Visual |
				Gdk.WindowAttributesType.Colormap;

			win = new Gdk.Window (parent.GdkWindow, attributes, attributesMask);
			win.UserData = parent.Handle;

			if (wclass == Gdk.WindowClass.InputOutput)
				parent.Style.Attach (win);

			return win;
		}
	}

	public delegate Gtk.Widget WidgetDropCallback ();
}
