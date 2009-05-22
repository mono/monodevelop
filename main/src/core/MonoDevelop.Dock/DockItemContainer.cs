//
// DockItemContainer.cs
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
using Gtk;
using Mono.Unix;

namespace MonoDevelop.Components.Docking
{
	class DockItemContainer: VBox
	{
		static Gdk.Pixbuf pixClose;
		static Gdk.Pixbuf pixAutoHide;
		static Gdk.Pixbuf pixDock;
		
		Gtk.Label title;
		Gtk.Button btnClose;
		Gtk.Button btnDock;
		string txt;
		Gtk.EventBox header;
		Gtk.Alignment headerAlign;
		DockFrame frame;
		DockItem item;
		Widget widget;
		Container borderFrame;
		bool allowPlaceholderDocking;
		bool pointerHover;
		Gtk.Tooltips tips = new Tooltips ();
		
		static Gdk.Cursor fleurCursor = new Gdk.Cursor (Gdk.CursorType.Fleur);
		static Gdk.Cursor handCursor = new Gdk.Cursor (Gdk.CursorType.Hand2);
		
		static DockItemContainer ()
		{
			pixClose = Gdk.Pixbuf.LoadFromResource ("stock-close-12.png");
			pixAutoHide = Gdk.Pixbuf.LoadFromResource ("stock-auto-hide.png");
			pixDock = Gdk.Pixbuf.LoadFromResource ("stock-dock.png");
		}
		
		public DockItemContainer (DockFrame frame, DockItem item)
		{
			this.frame = frame;
			this.item = item;
			
			ResizeMode = Gtk.ResizeMode.Queue;
			Spacing = 0;
			
			title = new Gtk.Label ();
			title.Xalign = 0;
			title.Xpad = 3;
			title.UseMarkup = true;
			
			btnDock = new Button (new Gtk.Image (pixAutoHide));
			btnDock.Relief = ReliefStyle.None;
			btnDock.CanFocus = false;
			btnDock.WidthRequest = btnDock.HeightRequest = 17;
			btnDock.Clicked += OnClickDock;
			
			btnClose = new Button (new Gtk.Image (pixClose));
			tips.SetTip (btnClose, Catalog.GetString ("Hide"), "");
			btnClose.Relief = ReliefStyle.None;
			btnClose.CanFocus = false;
			btnClose.WidthRequest = btnClose.HeightRequest = 17;
			btnClose.Clicked += delegate {
				item.Visible = false;
			};
			
			HBox box = new HBox (false, 0);
			box.PackStart (title, true, true, 0);
			box.PackEnd (btnClose, false, false, 0);
			box.PackEnd (btnDock, false, false, 0);
			
			headerAlign = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
			headerAlign.TopPadding = headerAlign.BottomPadding = headerAlign.RightPadding = headerAlign.LeftPadding = 1;
			headerAlign.Add (box);
			
			header = new EventBox ();
			header.Events |= Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask;
			header.ButtonPressEvent += HeaderButtonPress;
			header.ButtonReleaseEvent += HeaderButtonRelease;
			header.MotionNotifyEvent += HeaderMotion;
			header.KeyPressEvent += HeaderKeyPress;
			header.KeyReleaseEvent += HeaderKeyRelease;
			header.Add (headerAlign);
			header.ExposeEvent += HeaderExpose;
			header.Realized += delegate {
				header.GdkWindow.Cursor = handCursor;
			};
			
			foreach (Widget w in new Widget [] { header, btnDock, btnClose }) {
				w.EnterNotifyEvent += HeaderEnterNotify;
				w.LeaveNotifyEvent += HeaderLeaveNotify;
			}
			
			PackStart (header, false, false, 0);
			ShowAll ();
			UpdateBehavior ();
		}
		
		void OnClickDock (object s, EventArgs a)
		{
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating)
				item.Status = DockItemStatus.Dockable;
			else
				item.Status = DockItemStatus.AutoHide;
		}
		
		public void UpdateContent ()
		{
			if (widget != null)
				((Gtk.Container)widget.Parent).Remove (widget);
			widget = item.Content;
			
			if (item.DrawFrame) {
				if (borderFrame == null) {
					borderFrame = new CustomFrame (frame, item);
					borderFrame.Show ();
					PackStart (borderFrame, true, true, 0);
				}
				if (widget != null) {
					borderFrame.Add (widget);
					widget.Show ();
				}
			}
			else if (widget != null) {
				if (borderFrame != null) {
					Remove (borderFrame);
					borderFrame = null;
				}
				PackStart (widget, true, true, 0);
				widget.Show ();
			}
		}
		
		public void UpdateBehavior ()
		{
			btnClose.Visible = (item.Behavior & DockItemBehavior.CantClose) == 0;
			header.Visible = (item.Behavior & DockItemBehavior.Locked) == 0;
			btnDock.Visible = (item.Behavior & DockItemBehavior.CantAutoHide) == 0;
			
			if (item.Status == DockItemStatus.AutoHide || item.Status == DockItemStatus.Floating) {
				btnDock.Image = new Gtk.Image (pixDock);
				tips.SetTip (btnDock, Catalog.GetString ("Dock"), "");
			}
			else {
				btnDock.Image = new Gtk.Image (pixAutoHide);
				tips.SetTip (btnDock, Catalog.GetString ("Auto Hide"), "");
			}
		}
		
		void HeaderButtonPress (object ob, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1) {
				frame.ShowPlaceholder ();
				header.GdkWindow.Cursor = fleurCursor;
				frame.Toplevel.KeyPressEvent += HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent += HeaderKeyRelease;
				allowPlaceholderDocking = true;
			}
			else if (args.Event.Button == 3) {
				item.ShowDockPopupMenu (args.Event.Time);
			}
		}
		
		void HeaderButtonRelease (object ob, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 1) {
				frame.DockInPlaceholder (item);
				frame.HidePlaceholder ();
				if (header.GdkWindow != null)
					header.GdkWindow.Cursor = handCursor;
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
			}
		}
		
		void HeaderMotion (object ob, Gtk.MotionNotifyEventArgs args)
		{
			frame.UpdatePlaceholder (item, Allocation.Size, allowPlaceholderDocking);
		}
		
		[GLib.ConnectBeforeAttribute]
		void HeaderKeyPress (object ob, Gtk.KeyPressEventArgs a)
		{
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = false;
				frame.UpdatePlaceholder (item, Allocation.Size, false);
			}
			if (a.Event.Key == Gdk.Key.Escape) {
				frame.HidePlaceholder ();
				frame.Toplevel.KeyPressEvent -= HeaderKeyPress;
				frame.Toplevel.KeyReleaseEvent -= HeaderKeyRelease;
				Gdk.Pointer.Ungrab (0);
			}
		}
				
		[GLib.ConnectBeforeAttribute]
		void HeaderKeyRelease (object ob, Gtk.KeyReleaseEventArgs a)
		{
			if (a.Event.Key == Gdk.Key.Control_L || a.Event.Key == Gdk.Key.Control_R) {
				allowPlaceholderDocking = true;
				frame.UpdatePlaceholder (item, Allocation.Size, true);
			}
		}
		
		private void HeaderExpose (object ob, Gtk.ExposeEventArgs a)
		{
			Gdk.Rectangle rect = new Gdk.Rectangle (0, 0, header.Allocation.Width - 1, header.Allocation.Height);
			Gdk.Color gcol = pointerHover
				? frame.Style.Mid (Gtk.StateType.Active)
				: frame.Style.Mid (Gtk.StateType.Normal);
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (a.Event.Window)) {
				cr.NewPath ();
				cr.MoveTo (0, 0);
				cr.RelLineTo (rect.Width, 0);
				cr.RelLineTo (0, rect.Height);
				cr.RelLineTo (-rect.Width, 0);
				cr.RelLineTo (0, -rect.Height);
				cr.ClosePath ();
				Cairo.Gradient pat = new Cairo.LinearGradient (0, 0, rect.Width, rect.Height);
				Cairo.Color color1 = DockFrame.ToCairoColor (gcol);
				pat.AddColorStop (0, color1);
				color1.A = 0.3;
				pat.AddColorStop (1, color1);
				cr.Pattern = pat;
				cr.FillPreserve ();
			}
			
//			header.GdkWindow.DrawRectangle (gc, true, rect);
			header.GdkWindow.DrawRectangle (frame.Style.DarkGC (Gtk.StateType.Normal), false, rect);
			
			foreach (Widget child in header.Children)
				header.PropagateExpose (child, a.Event);
		}
		
		private void HeaderLeaveNotify (object ob, EventArgs a)
		{
			pointerHover = false;
			header.QueueDraw ();
		}
		
		private void HeaderEnterNotify (object ob, EventArgs a)
		{
			pointerHover = true;
			header.QueueDraw ();
		}
				
		public string Label {
			get { return txt; }
			set {
				title.Markup = "<small>" + value + "</small>";
				txt = value;
			}
		}
	}

	class CustomFrame: Bin
	{
		Gtk.Widget child;
		DockFrame frame;
		DockItem item;

		public CustomFrame (DockFrame frame, DockItem item)
		{
			this.frame = frame;
			this.item = item;
		}

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			requisition = child.SizeRequest ();
			requisition.Width++;
			requisition.Height++;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			allocation.Inflate (-1, -1);
			child.Allocation = allocation;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool res = base.OnExposeEvent (evnt);
			Gdk.Rectangle rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, Allocation.Width - 1, Allocation.Height - 1);
			GdkWindow.DrawRectangle (Style.DarkGC (Gtk.StateType.Normal), false, rect);
/*			DockGroupItem dit = frame.Container.FindDockGroupItem (item.Id);
			if (dit != null && dit.ParentGroup != null && dit.ParentGroup.Type == DockGroupType.Tabbed && dit.ParentGroup.TabStrip != null && dit.ParentGroup.TabStrip.Visible) {
				rect = dit.ParentGroup.TabStrip.GetTabArea (dit.ParentGroup.TabStrip.CurrentTab);
				int sx, sy;
				GdkWindow.GetRootOrigin (out sx, out sy);
				rect.X -= sx;
				rect.Y -= sy;
				GdkWindow.DrawLine (Style.LightGC (Gtk.StateType.Active), rect.X + 1, Allocation.Bottom - 1, rect.Right - 1, Allocation.Bottom - 1);
			}
*/
			return res;
		}
	}
}
