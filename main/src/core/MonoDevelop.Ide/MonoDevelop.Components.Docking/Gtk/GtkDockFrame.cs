//
// MonoDevelop.Components.Docking.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;
using Xwt.Motion;
using MonoDevelop.Components.Docking.Internal;
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Components.Docking
{
	class GtkDockFrame: HBox, IAnimatable, IDockFrameBackend
	{
		internal const double ItemDockCenterArea = 0.4;
		internal const int GroupDockSeparatorSize = 40;
		
		internal bool ShadedSeparators = true;

		IDockFrameController controller;
		DockContainer container;
		DockLayout layout;

		int handleSize = 1;
		int handlePadding = 0;

		List<DockFrameTopLevel> topLevels = new List<DockFrameTopLevel> ();

		DockBar dockBarTop, dockBarBottom, dockBarLeft, dockBarRight;
		VBox mainBox;
		Gtk.Widget overlayWidget;

		Dictionary<DockGroupItem,GtkDockGroupItem> items = new Dictionary<DockGroupItem, GtkDockGroupItem> ();

		public GtkDockFrame ()
		{
		}

		public void Initialize (IDockFrameController controller)
		{
			this.controller = controller;

			GtkWorkarounds.FixContainerLeak (this);

			dockBarTop = new DockBar (this, Gtk.PositionType.Top);
			dockBarBottom = new DockBar (this, Gtk.PositionType.Bottom);
			dockBarLeft = new DockBar (this, Gtk.PositionType.Left);
			dockBarRight = new DockBar (this, Gtk.PositionType.Right);

			container = new DockContainer (this);
			HBox hbox = new HBox ();
			hbox.PackStart (dockBarLeft, false, false, 0);
			hbox.PackStart (container, true, true, 0);
			hbox.PackStart (dockBarRight, false, false, 0);
			mainBox = new VBox ();
			mainBox.PackStart (dockBarTop, false, false, 0);
			mainBox.PackStart (hbox, true, true, 0);
			mainBox.PackStart (dockBarBottom, false, false, 0);
			Add (mainBox);
			mainBox.ShowAll ();
			mainBox.NoShowAll = true;
			UpdateDockbarsVisibility ();
		}

		internal bool UseWindowsForTopLevelFrames {
			get { return Platform.IsMac; }
		}

		public IDockFrameController Frontend {
			get { return controller; }
		}

		void IDockFrameBackend.Refresh (DockObject obj)
		{
			if (container.Layout != null) {
				container.Layout.Sync (layout);
				container.RelayoutWidgets ();
			}
		}

		IDockItemBackend IDockFrameBackend.CreateItemBackend (DockItem item)
		{
			return new DockItemBackend (this, item);
		}

		Xwt.Rectangle IDockFrameBackend.GetAllocation ()
		{
			throw new NotImplementedException ();
		}

		public bool DockbarsVisible {
			get {
				return !OverlayWidgetVisible;
			}
		}
		
		internal bool OverlayWidgetVisible { get; set; }

		public void AddOverlayWidget (Control control, bool animate = false)
		{
			var widget = control.GetNativeWidget<Gtk.Widget> ();

			RemoveOverlayWidget (false);

			this.overlayWidget = widget;
			widget.Parent = this;
			OverlayWidgetVisible = true;
			if (animate) {
				currentOverlayPosition = Math.Max (0, Allocation.Y + Allocation.Height);
				this.Animate (
					"ShowOverlayWidget", 
					ShowOverlayWidgetAnimation,
					easing: Easing.CubicOut);
			} else {
				currentOverlayPosition = Math.Max (0, Allocation.Y);
				QueueResize ();
			}

			UpdateDockbarsVisibility ();
		}

		public void RemoveOverlayWidget (bool animate = false)
		{
			this.AbortAnimation ("ShowOverlayWidget");
			this.AbortAnimation ("HideOverlayWidget");
			OverlayWidgetVisible = false;

			if (overlayWidget != null) {
				if (animate) {
					currentOverlayPosition = Allocation.Y;
					this.Animate (
						"HideOverlayWidget", 
						HideOverlayWidgetAnimation,
						finished: (a,b) => { 
							if (overlayWidget != null) {
								overlayWidget.Unparent ();
								overlayWidget = null;
							}
						},
						easing: Easing.SinOut);
				} else {
					overlayWidget.Unparent ();
					overlayWidget = null;
					QueueResize ();
				}
			}

			UpdateDockbarsVisibility ();
		}

		int currentOverlayPosition;

		void UpdateDockbarsVisibility ()
		{
			dockBarTop.UpdateVisibility ();
			dockBarBottom.UpdateVisibility ();
			dockBarLeft.UpdateVisibility ();
			dockBarRight.UpdateVisibility ();
		}

		void ShowOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * (1f - value));
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void HideOverlayWidgetAnimation (double value)
		{
			currentOverlayPosition = Allocation.Y + (int)((double)Allocation.Height * value);
			overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, Allocation.Width, Allocation.Height));
		}

		void IAnimatable.BatchBegin ()
		{
		}

		void IAnimatable.BatchCommit ()
		{
		}

		internal GtkDockGroupItem GetGroupItem (DockGroupItem di)
		{
			GtkDockGroupItem item;
			if (!items.TryGetValue (di, out item))
				item = items[di] = new GtkDockGroupItem (this, di);
			return item;
		}

		public DockBar ExtractDockBar (PositionType pos)
		{
			DockBar db = new DockBar (this, pos);
			switch (pos) {
				case PositionType.Left: db.OriginalBar = dockBarLeft; dockBarLeft = db; break;
				case PositionType.Top: db.OriginalBar = dockBarTop; dockBarTop = db; break;
				case PositionType.Right: db.OriginalBar = dockBarRight; dockBarRight = db; break;
				case PositionType.Bottom: db.OriginalBar = dockBarBottom; dockBarBottom = db; break;
			}
			return db;
		}
		
		internal DockBar GetDockBar (PositionType pos)
		{
			switch (pos) {
				case Gtk.PositionType.Top: return dockBarTop;
				case Gtk.PositionType.Bottom: return dockBarBottom;
				case Gtk.PositionType.Left: return dockBarLeft;
				case Gtk.PositionType.Right: return dockBarRight;
			}
			return null;
		}
		
		internal DockContainer Container {
			get { return container; }
		}

		public int HandleSize {
			get {
				return handleSize;
			}
			set {
				handleSize = value;
			}
		}
		
		public int HandlePadding {
			get {
				return handlePadding;
			}
			set {
				handlePadding = value;
			}
		}

		public int DefaultItemWidth {
			get {
				return controller.DefaultItemWidth;
			}
		}

		public int DefaultItemHeight {
			get {
				return controller.DefaultItemHeight;
			}
		}
		
		internal int TotalHandleSize {
			get { return handleSize + handlePadding*2; }
		}

		internal int TotalSensitiveHandleSize {
			get { return 6; }
		}
		
		public DockItemBackend GetItem (string id)
		{
			foreach (DockItemBackend it in container.Items) {
				if (it.Id == id) {
					if (!it.IsPositionMarker)
					    return it;
					else
					    return null;
				}
			}
			return null;
		}
		
		public IEnumerable<DockItemBackend> GetItems ()
		{
			return container.Items;
		}

		public void LoadLayout (DockLayout layout)
		{
			this.layout = layout;

			var focus = GetActiveWidget ();

			var dl = new GtkDockLayout (this, layout);
			dl.Sync (layout);
			container.LoadLayout (dl);

			// Keep the currently focused widget when switching layouts
			if (focus != null && focus.IsRealized && focus.Visible)
				DockItemBackend.SetFocus (focus);
		}

		Gtk.Widget GetActiveWidget ()
		{
			Gtk.Widget widget = this;
			while (widget is Gtk.Container) {
				Gtk.Widget child = ((Gtk.Container)widget).FocusChild;
				if (child != null)
					widget = child;
				else
					break;
			}
			return widget;
		}
		
		public uint AutoShowDelay {
			get {
				return controller.AutoShowDelay;
			}
		}

		public uint AutoHideDelay {
			get {
				return controller.AutoHideDelay;
			}
		}
		
		public void UpdateTitle (DockItem item)
		{
			GtkDockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.UpdateTitle (gitem.Item);
			dockBarTop.UpdateTitle (gitem.Item);
			dockBarBottom.UpdateTitle (gitem.Item);
			dockBarLeft.UpdateTitle (gitem.Item);
			dockBarRight.UpdateTitle (gitem.Item);
		}
		
		public void UpdateStyle (DockItem item)
		{
			GtkDockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.UpdateStyle (gitem.Item);
			dockBarTop.UpdateStyle (gitem.Item);
			dockBarBottom.UpdateStyle (gitem.Item);
			dockBarLeft.UpdateStyle (gitem.Item);
			dockBarRight.UpdateStyle (gitem.Item);
		}
		
		internal void Present (DockItemBackend item, bool giveFocus)
		{
			GtkDockGroupItem gitem = container.FindDockGroupItem (item.Id);
			if (gitem == null)
				return;
			
			gitem.ParentGroup.Present (item, giveFocus);
		}
		
		internal void AddTopLevel (DockFrameTopLevel w, int x, int y, int width, int height)
		{
			w.X = x;
			w.Y = y;

			if (UseWindowsForTopLevelFrames) {
				var win = new Gtk.Window (Gtk.WindowType.Toplevel);
				win.SkipTaskbarHint = true;
				win.Decorated = false;
				win.TypeHint = Gdk.WindowTypeHint.Toolbar;
				w.ContainerWindow = win;
				w.Size = new Size (width, height);
				win.Add (w);
				w.Show ();
				var p = this.GetScreenCoordinates (new Gdk.Point (x, y));
				win.Opacity = 0.0;
				win.Move (p.X, p.Y);
				win.Resize (width, height);
				win.Show ();
				Ide.DesktopService.AddChildWindow ((Gtk.Window)Toplevel, win);
				win.AcceptFocus = true;
				win.Opacity = 1.0;

				/* When we use real windows for frames, it's possible for pads to be over other
				 * windows. For some reason simply presenting or raising those dialogs doesn't
				 * seem to work, so we hide/show them in order to force them above the pad. */
				var toplevels = Gtk.Window.ListToplevels ().Where (t => t.IsRealized && t.Visible && t.TypeHint == WindowTypeHint.Dialog); // && t.TransientFor != null);
				foreach (var t in toplevels) {
					t.Hide ();
					t.Show ();
				}
			} else {
				w.Parent = this;
				w.Size = new Size (width, height);
				Requisition r = w.SizeRequest ();
				w.Allocation = new Gdk.Rectangle (Allocation.X + x, Allocation.Y + y, r.Width, r.Height);
				topLevels.Add (w);
			}
		}

		internal void RemoveTopLevel (DockFrameTopLevel w)
		{
			w.Unparent ();
			topLevels.Remove (w);
			QueueResize ();
		}
		
		public Gdk.Rectangle GetCoordinates (Gtk.Widget w)
		{
			int px, py;
			if (!w.TranslateCoordinates (this, 0, 0, out px, out py))
				return new Gdk.Rectangle (0,0,0,0);

			Gdk.Rectangle rect = w.Allocation;
			rect.X = px - Allocation.X;
			rect.Y = py - Allocation.Y;
			return rect;
		}
		
		internal void ShowPlaceholder (DockItemBackend draggedItem)
		{
			container.ShowPlaceholder (draggedItem);
		}
		
		internal void DockInPlaceholder (DockItemBackend item)
		{
			container.DockInPlaceholder (item);
		}
		
		internal void HidePlaceholder ()
		{
			container.HidePlaceholder ();
		}
		
		internal void UpdatePlaceholder (DockItemBackend item, Gdk.Size size, bool allowDocking)
		{
			container.UpdatePlaceholder (item, size, allowDocking);
		}
		
		internal DockBarItem BarDock (Gtk.PositionType pos, DockItemBackend item, int size)
		{
			return GetDockBar (pos).AddItem (item, size);
		}
		
		internal AutoHideBox AutoShow (DockItem item, DockBar bar, int size)
		{
			AutoHideBox aframe = new AutoHideBox (this, (DockItemBackend)item.Backend, bar.Position, size);
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);

			int x,y,w,h;
			if (bar == dockBarLeft || bar == dockBarRight) {
				h = Allocation.Height - sTop.Height - sBot.Height;
				w = size;
				y = sTop.Height;
				if (bar == dockBarLeft)
					x = sLeft.Width;
				else
					x = Allocation.Width - size - sRgt.Width;
			} else {
				w = Allocation.Width - sLeft.Width - sRgt.Width;
				h = size;
				x = sLeft.Width;
				if (bar == dockBarTop)
					y = sTop.Height;
				else
					y = Allocation.Height - size - sBot.Height;
			}

			AddTopLevel (aframe, x, y, w, h);
			aframe.AnimateShow ();

			return aframe;
		}

		internal void UpdateSize (DockBar bar, AutoHideBox aframe)
		{
			Gdk.Size sTop = GetBarFrameSize (dockBarTop);
			Gdk.Size sBot = GetBarFrameSize (dockBarBottom);
			Gdk.Size sLeft = GetBarFrameSize (dockBarLeft);
			Gdk.Size sRgt = GetBarFrameSize (dockBarRight);

			if (bar == dockBarLeft || bar == dockBarRight) {
				aframe.HeightRequest = Allocation.Height - sTop.Height - sBot.Height;
				if (bar == dockBarRight)
					aframe.X = Allocation.Width - aframe.Allocation.Width - sRgt.Width;
			} else {
				aframe.WidthRequest = Allocation.Width - sLeft.Width - sRgt.Width;
				if (bar == dockBarBottom)
					aframe.Y = Allocation.Height - aframe.Allocation.Height - sBot.Height;
			}
		}

		
		Gdk.Size GetBarFrameSize (DockBar bar)
		{
			if (bar.OriginalBar != null)
				bar = bar.OriginalBar;
			if (!bar.Visible)
				return new Gdk.Size (0,0);
			Gtk.Requisition req = bar.SizeRequest ();
			return new Gdk.Size (req.Width, req.Height);
		}
		
		internal void AutoHide (DockItemBackend item, AutoHideBox widget, bool animate)
		{
			if (animate) {
				widget.Hidden += delegate {
					if (!widget.Disposed)
						AutoHide (item, widget, false);
				};
				widget.AnimateHide ();
			}
			else {
				// The widget may already be removed from the parent
				// so 'parent' can be null
				Gtk.Container parent = (Gtk.Container) item.Widget.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (item.Widget.IsMapped) {
						item.Widget.Unmap ();
					}
					parent.Remove (item.Widget);
				}
				parent = (Gtk.Container) item.TitleTab.Parent;
				if (parent != null) {
					//removing the widget from its parent causes it to unrealize without unmapping
					//so make sure it's unmapped
					if (item.TitleTab.IsMapped) {
						item.TitleTab.Unmap ();
					}
					parent.Remove (item.TitleTab);
				}
				if (widget.ContainerWindow != null) {
					widget.ContainerWindow.Destroy ();
				} else
					RemoveTopLevel (widget);

				widget.Disposed = true;
				widget.Destroy ();
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (overlayWidget != null)
				overlayWidget.SizeRequest ();
			base.OnSizeRequested (ref requisition);
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			foreach (DockFrameTopLevel tl in topLevels) {
				Requisition r = tl.SizeRequest ();
				tl.SizeAllocate (new Gdk.Rectangle (allocation.X + tl.X, allocation.Y + tl.Y, r.Width, r.Height));
			}
			if (overlayWidget != null)
				overlayWidget.SizeAllocate (new Rectangle (Allocation.X, currentOverlayPosition, allocation.Width, allocation.Height));
		}
		
		protected override void ForAll (bool include_internals, Callback callback)
		{
			base.ForAll (include_internals, callback);
			List<DockFrameTopLevel> clone = new List<DockFrameTopLevel> (topLevels);
			foreach (DockFrameTopLevel child in clone)
				callback (child);
			if (overlayWidget != null)
				callback (overlayWidget);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			MinimizeAllAutohidden ();
			return base.OnButtonPressEvent (evnt);
		}

		void MinimizeAllAutohidden ()
		{
			foreach (var it in GetItems ()) {
				if (it.Frontend.Visible && it.Frontend.Status == DockItemStatus.AutoHide)
					it.Frontend.Minimize ();
			}
		}

		static internal bool IsWindows {
			get { return System.IO.Path.DirectorySeparatorChar == '\\'; }
		}

		internal static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double) ushort.MaxValue, color.Green / (double) ushort.MaxValue, color.Blue / (double) ushort.MaxValue);
		}
	}

	internal delegate void DockDelegate (DockItemBackend item);
	
}
