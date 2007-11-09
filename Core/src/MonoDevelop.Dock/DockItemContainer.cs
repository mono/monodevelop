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
		DockFrame frame;
		DockItem item;
		Widget widget;
		Frame borderFrame;
		bool allowPlaceholderDocking;
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
			
			header = new EventBox ();
			header.Events |= Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask;
			header.ButtonPressEvent += HeaderButtonPress;
			header.ButtonReleaseEvent += HeaderButtonRelease;
			header.MotionNotifyEvent += HeaderMotion;
			header.KeyPressEvent += HeaderKeyPress;
			header.KeyReleaseEvent += HeaderKeyRelease;
			header.Add (box);
			header.Realized += delegate {
				header.GdkWindow.Cursor = handCursor;
				header.ModifyBg (StateType.Normal, frame.Style.Mid (Gtk.StateType.Normal));
			};
			
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
					borderFrame = new Frame ();
					borderFrame.ShadowType = ShadowType.In;
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
				
		public string Label {
			get { return txt; }
			set {
				title.Markup = "<small>" + value + "</small>";
				txt = value;
			}
		}
	}
}
