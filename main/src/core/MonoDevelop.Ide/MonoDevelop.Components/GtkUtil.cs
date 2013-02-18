// GtkUtil.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using Gtk;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components
{
	public static class GtkUtil
	{
		static Dictionary<TreeView, TreeViewTooltipsData> treeData = new Dictionary<TreeView, TreeViewTooltipsData> ();
		
		public static Cairo.Color ToCairoColor (this Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		public static Gdk.Color ToGdkColor (this Cairo.Color color)
		{
			return new Gdk.Color ((byte)(color.R * 255d), (byte)(color.G * 255d), (byte)(color.B * 255d));
		}
		
		/// <summary>
		/// Makes a color lighter or darker
		/// </summary>
		/// <param name='lightAmount'>
		/// Amount of lightness to add. If the value is positive, the color will be lighter,
		/// if negative it will be darker. Value must be between 0 and 1.
		/// </param>
		public static Gdk.Color AddLight (this Gdk.Color color, double lightAmount)
		{
			HslColor c = color;
			c.L += lightAmount;
			return c;
		}

		public static Cairo.Color AddLight (this Cairo.Color color, double lightAmount)
		{
			HslColor c = color;
			c.L += lightAmount;
			return c;
		}

		public static Gtk.Widget ToGtkWidget (this Xwt.Widget widget)
		{
			return (Gtk.Widget) Xwt.Engine.WidgetRegistry.GetNativeWidget (widget);
		}
		
		public static void EnableAutoTooltips (this Gtk.TreeView tree)
		{
			TreeViewTooltipsData data = new TreeViewTooltipsData ();
			treeData [tree] = data;
			tree.MotionNotifyEvent += HandleMotionNotifyEvent;
			tree.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			tree.ButtonPressEvent += HandleButtonPressEvent;
			tree.ScrollEvent += HandleTreeScrollEvent;
			tree.Hidden += HandleTreeHidden;
			tree.Unrealized += HandleTreeHidden;
			tree.Destroyed += delegate {
				ResetTooltip (tree);
				treeData.Remove (tree);
			};
		}
		
		static void ResetTooltip (Gtk.TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			if (data.ShowTimer != 0)
				GLib.Source.Remove (data.ShowTimer);
			if (data.LeaveTimer != 0)
				GLib.Source.Remove (data.LeaveTimer);
			if (data.Tooltip != null)
				data.Tooltip.Destroy ();
		}

		static void HandleTreeHidden (object sender, EventArgs e)
		{
			ResetTooltip ((Gtk.TreeView) sender);
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleTreeScrollEvent (object o, ScrollEventArgs args)
		{
			HideTooltip ((Gtk.TreeView)o);
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
		{
			TreeView tree = (TreeView) o;
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			data.LeaveTimer = GLib.Timeout.Add (50, delegate {
				data.LeaveTimer = 0;
				if (data != null && data.Tooltip != null && data.Tooltip.MouseIsOver)
					return false;
				HideTooltip (tree);
				return false;
			});
		}

		internal static void HideTooltip (TreeView tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;
			if (data.ShowTimer != 0) {
				GLib.Source.Remove (data.ShowTimer);
				data.ShowTimer = 0;
				return;
			}
			if (data.Tooltip != null) {
				data.Tooltip.Destroy ();
				data.Tooltip = null;
			}
		}

		[GLib.ConnectBeforeAttribute]
		static void HandleMotionNotifyEvent(object o, MotionNotifyEventArgs args)
		{
			TreeView tree = (TreeView) o;
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue (tree, out data))
				return;

			HideTooltip (tree);

			int cx, cy;
			TreePath path;
			TreeViewColumn col;
			if (!tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy))
				return;
			data.Path = path;
			data.ShowTimer = GLib.Timeout.Add (300, delegate {
				data.ShowTimer = 0;
				int ox, oy;
				tree.BinWindow.GetOrigin (out ox, out oy);
				Gdk.Rectangle rect = tree.GetCellArea (path, col);
				data.Tooltip = new CellTooltipWindow (tree, col, path);
				if (rect.X + data.Tooltip.SizeRequest ().Width > tree.Allocation.Width) {
					data.Tooltip.Move (ox + rect.X - 1, oy + rect.Y);
					data.Tooltip.ShowAll ();
				} else {
					data.Tooltip.Destroy ();
					data.Tooltip = null;
				}
				return false;
			});
		}
		
		[GLib.ConnectBeforeAttribute]
		static void HandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs ev)
		{
			UnscheduleTooltipShow (o);
		}
		
		static void UnscheduleTooltipShow (object tree)
		{
			TreeViewTooltipsData data;
			if (!treeData.TryGetValue ((Gtk.TreeView)tree, out data))
				return;
			if (data.ShowTimer != 0) {
				GLib.Source.Remove (data.ShowTimer);
				data.ShowTimer = 0;
			}
		}

		public static Gdk.Rectangle ToScreenCoordinates (Gtk.Widget widget, Gdk.Window w, Gdk.Rectangle rect)
		{
			return new Gdk.Rectangle (ToScreenCoordinates (widget, w, rect.X, rect.Y), rect.Size);
		}

		public static Gdk.Point ToScreenCoordinates (Gtk.Widget widget, Gdk.Window w, int x, int y)
		{
			int ox, oy;
			w.GetOrigin (out ox, out oy);
			ox += widget.Allocation.X;
			oy += widget.Allocation.Y;
			return new Gdk.Point (ox + x, oy + y);
		}

		public static Gdk.Rectangle ToWindowCoordinates (Gtk.Widget widget, Gdk.Window w, Gdk.Rectangle rect)
		{
			return new Gdk.Rectangle (ToWindowCoordinates (widget, w, rect.X, rect.Y), rect.Size);
		}
		
		public static Gdk.Point ToWindowCoordinates (Gtk.Widget widget, Gdk.Window w, int x, int y)
		{
			int ox, oy;
			w.GetOrigin (out ox, out oy);
			ox += widget.Allocation.X;
			oy += widget.Allocation.Y;
			return new Gdk.Point (x - ox, y - oy);
		}

		public static T ReplaceWithWidget<T> (this Gtk.Widget oldWidget, T newWidget, bool transferChildren = false) where T:Gtk.Widget
		{
			Gtk.Container parent = (Gtk.Container) oldWidget.Parent;
			if (parent == null)
				throw new InvalidOperationException ();

			if (parent is Box) {
				var box = (Box) parent;
				var bc = (Gtk.Box.BoxChild) parent [oldWidget];
				box.Add (newWidget);
				var nc = (Gtk.Box.BoxChild) parent [newWidget];
				nc.Expand = bc.Expand;
				nc.Fill = bc.Fill;
				nc.PackType = bc.PackType;
				nc.Padding = bc.Padding;
				nc.Position = bc.Position;
				box.Remove (oldWidget);
			}
			else if (parent is Table) {
				var table = (Table) parent;
				var bc = (Gtk.Table.TableChild) parent [oldWidget];
				table.Add (newWidget);
				var nc = (Gtk.Table.TableChild) parent [newWidget];
				nc.BottomAttach = bc.BottomAttach;
				nc.LeftAttach = bc.LeftAttach;
				nc.RightAttach = bc.RightAttach;
				nc.TopAttach = bc.TopAttach;
				nc.XOptions = bc.XOptions;
				nc.XPadding = bc.XPadding;
				nc.YOptions = bc.YOptions;
				nc.YPadding = bc.YPadding;
				table.Remove (oldWidget);
			}
			else if (parent is Paned) {
				var paned = (Paned) parent;
				var bc = (Gtk.Paned.PanedChild) parent [oldWidget];
				var resize = bc.Resize;
				var shrink = bc.Shrink;
				if (oldWidget == paned.Child1) {
					paned.Remove (oldWidget);
					paned.Add1 (newWidget);
				} else {
					paned.Remove (oldWidget);
					paned.Add2 (newWidget);
				}
				var nc = (Gtk.Paned.PanedChild) parent [newWidget];
				nc.Resize = resize;
				nc.Shrink = shrink;
			}
			else
				throw new NotSupportedException ();

			if (transferChildren) {
				if (newWidget is Paned && oldWidget is Paned) {
					var panedOld = (Paned) oldWidget;
					var panedNew = (Paned) (object) newWidget;
					if (panedOld.Child1 != null) {
						var c = panedOld.Child1;
						var bc = (Gtk.Paned.PanedChild) panedOld [c];
						var resize = bc.Resize;
						var shrink = bc.Shrink;
						panedOld.Remove (c);
						panedNew.Add1 (c);
						var nc = (Gtk.Paned.PanedChild) panedNew [c];
						nc.Resize = resize;
						nc.Shrink = shrink;
					}
					if (panedOld.Child2 != null) {
						var c = panedOld.Child2;
						var bc = (Gtk.Paned.PanedChild) panedOld [c];
						var resize = bc.Resize;
						var shrink = bc.Shrink;
						panedOld.Remove (c);
						panedNew.Add2 (c);
						var nc = (Gtk.Paned.PanedChild) panedNew [c];
						nc.Resize = resize;
						nc.Shrink = shrink;
					}
				}
				else
					throw new NotSupportedException ();
			}

			newWidget.Visible = oldWidget.Visible;
			return newWidget;
		}

		public static bool ScreenSupportsARGB ()
		{
			return Gdk.Screen.Default.IsComposited;
		}

		/// <summary>
		/// This method can be used to get a reliave Leave event for a widget, which
		/// is not fired if the pointer leaves the widget to enter a child widget.
		/// To ubsubscribe the event, dispose the object returned by the method.
		/// </summary>
		public static IDisposable SubscribeLeaveEvent (this Gtk.Widget w, System.Action leaveHandler)
		{
			return new LeaveEventData (w, leaveHandler);
		}
	}

	class LeaveEventData: IDisposable
	{
		public System.Action LeaveHandler;
		public Gtk.Widget RootWidget;
		public bool Inside;

		public LeaveEventData (Gtk.Widget w, System.Action leaveHandler)
		{
			RootWidget = w;
			LeaveHandler = leaveHandler;
			if (w.IsRealized) {
				RootWidget.Unrealized += HandleUnrealized;
				TrackLeaveEvent (w);
			}
			else
				w.Realized += HandleRealized;
		}

		void HandleRealized (object sender, EventArgs e)
		{
			RootWidget.Realized -= HandleRealized;
			RootWidget.Unrealized += HandleUnrealized;
			TrackLeaveEvent (RootWidget);
		}

		void HandleUnrealized (object sender, EventArgs e)
		{
			RootWidget.Unrealized -= HandleUnrealized;
			UntrackLeaveEvent (RootWidget);
			RootWidget.Realized += HandleRealized;
			if (Inside) {
				Inside = false;
				LeaveHandler ();
			}
		}

		public void Dispose ()
		{
			if (RootWidget.IsRealized) {
				UntrackLeaveEvent (RootWidget);
				RootWidget.Unrealized -= HandleUnrealized;
			} else {
				RootWidget.Realized -= HandleRealized;
			}
		}

		public void TrackLeaveEvent (Gtk.Widget w)
		{
			w.LeaveNotifyEvent += HandleLeaveNotifyEvent;
			w.EnterNotifyEvent += HandleEnterNotifyEvent;
			if (w is Gtk.Container) {
				((Gtk.Container)w).Added += HandleAdded;
				((Gtk.Container)w).Removed += HandleRemoved;
				foreach (var c in ((Gtk.Container)w).Children)
					TrackLeaveEvent (c);
			}
		}

		void UntrackLeaveEvent (Gtk.Widget w)
		{
			w.LeaveNotifyEvent -= HandleLeaveNotifyEvent;
			w.EnterNotifyEvent -= HandleEnterNotifyEvent;
			if (w is Gtk.Container) {
				((Gtk.Container)w).Added -= HandleAdded;
				((Gtk.Container)w).Removed -= HandleRemoved;
				foreach (var c in ((Gtk.Container)w).Children)
					UntrackLeaveEvent (c);
			}
		}

		void HandleRemoved (object o, RemovedArgs args)
		{
			UntrackLeaveEvent (args.Widget);
		}

		void HandleAdded (object o, AddedArgs args)
		{
			TrackLeaveEvent (args.Widget);
		}

		void HandleEnterNotifyEvent (object o, EnterNotifyEventArgs args)
		{
			Inside = true;
		}

		void HandleLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
		{
			Inside = false;

			// Delay the call to the leave handler since the pointer may be
			// entering a child widget, in which case the event doesn't have to be fired
			Gtk.Application.Invoke (delegate {
				if (!Inside)
					LeaveHandler ();
			});
		}
	}

	class TreeViewTooltipsData
	{
		public uint ShowTimer;
		public uint LeaveTimer;
		public TreePath Path;
		public CellTooltipWindow Tooltip;
	}

	class CellTooltipWindow: TooltipWindow
	{
		TreeViewColumn col;
		TreeView tree;
		TreeIter iter;

		public bool MouseIsOver;
		
		public CellTooltipWindow (TreeView tree, TreeViewColumn col, TreePath path)
		{
			this.tree = tree;
			this.col = col;
			
			NudgeHorizontal = true;
			
			Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask;
			
			Gdk.Rectangle rect = tree.GetCellArea (path, col);
			rect.Inflate (2, 2);
			
			if (!tree.Model.GetIter (out iter, path)) {
				Hide ();
				return;
			}

			col.CellSetCellData (tree.Model, iter, false, false);
			int x = 0;
			int th = 0;
			CellRenderer[] renderers = col.CellRenderers;
			foreach (CellRenderer cr in renderers) {
				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle crect = new Gdk.Rectangle (x, rect.Y, wi, rect.Height);
				cr.GetSize (tree, ref crect, out xo, out yo, out wi, out he);
				if (cr != renderers [renderers.Length - 1])
					x += crect.Width + col.Spacing + 1;
				else
					x += wi + 1;
				if (he > th) th = he;
			}
			SetSizeRequest (x, th + 2);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			base.OnExposeEvent (evnt);

			Gdk.Rectangle expose = Allocation;
			Gdk.Color save = Gdk.Color.Zero;
			int x = 1;

			col.CellSetCellData (tree.Model, iter, false, false);

			foreach (CellRenderer cr in col.CellRenderers) {
				if (!cr.Visible)
					continue;

				if (cr is CellRendererText) {
					save = ((CellRendererText)cr).ForegroundGdk;
					((CellRendererText)cr).ForegroundGdk = Style.Foreground (State);
				}

				int sp, wi, he, xo, yo;
				col.CellGetPosition (cr, out sp, out wi);
				Gdk.Rectangle bgrect = new Gdk.Rectangle (x, expose.Y, wi, expose.Height - 2);
				cr.GetSize (tree, ref bgrect, out xo, out yo, out wi, out he);
				int leftMargin = (int) ((bgrect.Width - wi) * cr.Xalign);
				int topMargin = (int) ((bgrect.Height - he) * cr.Yalign);
				Gdk.Rectangle cellrect = new Gdk.Rectangle (bgrect.X + leftMargin, bgrect.Y + topMargin + 1, wi, he);
				cr.Render (this.GdkWindow, this, bgrect, cellrect, expose, CellRendererState.Focused);
				x += bgrect.Width + col.Spacing + 1;

				if (cr is CellRendererText) {
					((CellRendererText)cr).ForegroundGdk = save;
				}
			}

			return true;
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonPressEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			bool res = base.OnButtonReleaseEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			bool res = base.OnScrollEvent (evnt);
			CreateEvent (evnt);
			return res;
		}
		
		void CreateEvent (Gdk.EventButton refEvent)
		{
			int rx, ry;
			tree.BinWindow.GetOrigin (out rx, out ry);
			NativeEventButtonStruct nativeEvent = new NativeEventButtonStruct (); 
			nativeEvent.type = refEvent.Type;
			nativeEvent.send_event = 1;
			nativeEvent.window = tree.BinWindow.Handle;
			nativeEvent.x = refEvent.XRoot - rx;
			nativeEvent.y = refEvent.YRoot - ry;
			nativeEvent.x_root = refEvent.XRoot;
			nativeEvent.y_root = refEvent.YRoot;
			nativeEvent.time = refEvent.Time;
			nativeEvent.state = (uint) refEvent.State;
			nativeEvent.button = refEvent.Button;
			nativeEvent.device = refEvent.Device.Handle;

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventButton evnt = new Gdk.EventButton (ptr); 
				Gdk.EventHelper.Put (evnt); 
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}
		
		void CreateEvent (Gdk.EventScroll refEvent)
		{
			int rx, ry;
			tree.BinWindow.GetOrigin (out rx, out ry);
			NativeEventScrollStruct nativeEvent = new NativeEventScrollStruct (); 
			nativeEvent.type = refEvent.Type;
			nativeEvent.send_event = 1;
			nativeEvent.window = tree.BinWindow.Handle;
			nativeEvent.x = refEvent.XRoot - rx;
			nativeEvent.y = refEvent.YRoot - ry;
			nativeEvent.x_root = refEvent.XRoot;
			nativeEvent.y_root = refEvent.YRoot;
			nativeEvent.time = refEvent.Time;
			nativeEvent.direction = refEvent.Direction;
			nativeEvent.state = (uint) refEvent.State;
			nativeEvent.device = refEvent.Device.Handle;

			IntPtr ptr = GLib.Marshaller.StructureToPtrAlloc (nativeEvent); 
			try {
				Gdk.EventScroll evnt = new Gdk.EventScroll (ptr); 
				Gdk.EventHelper.Put (evnt); 
			} finally {
				Marshal.FreeHGlobal (ptr);
			}
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			MouseIsOver = false;
			GtkUtil.HideTooltip (tree);
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			MouseIsOver = true;
			return base.OnEnterNotifyEvent (evnt);
		}
	}
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventButtonStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public IntPtr axes; 
		public uint state; 
		public uint button; 
		public IntPtr device; 
		public double x_root; 
		public double y_root; 
	} 
	
	[StructLayout (LayoutKind.Sequential)] 
	struct NativeEventScrollStruct { 
		public Gdk.EventType type; 
		public IntPtr window; 
		public sbyte send_event; 
		public uint time; 
		public double x; 
		public double y; 
		public uint state; 
		public Gdk.ScrollDirection direction;
		public IntPtr device; 
		public double x_root; 
		public double y_root;
		//FIXME: scroll deltas
	} 
}
