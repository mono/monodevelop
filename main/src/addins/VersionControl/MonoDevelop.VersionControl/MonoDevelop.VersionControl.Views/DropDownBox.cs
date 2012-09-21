// 
// DropDownBox.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using System.ComponentModel;
using MonoDevelop.Ide;
using Gtk;
using Mono.TextEditor;

namespace MonoDevelop.VersionControl.Views
{
	//FIXME: re-merge this with MonoDevelop.Components.DropDownBox
	[Category ("Widgets")]
	[ToolboxItem (true)]
	public class DropDownBox : Gtk.Button
	{
		Pango.Layout layout;
		const int pixbufSpacing = 2;
		const int leftSpacing   = 2;
		const int ySpacing      = 4;
		
		public string Text {
			get {
				return layout.Text;
			}
			set {
				layout.SetText (value);
//				QueueResize ();
			}
		}
		
		public Gdk.Pixbuf Pixbuf {
			get;
			set;
		}
		
		public object CurrentItem {
			get;
			set;
		}
		
		int defaultIconHeight, defaultIconWidth;
		
		/// <summary>
		/// This is so that the height doesn't jump around depending whether there's an icon assigned or not.
		/// </summary>
		public int DefaultIconHeight {
			get { return defaultIconHeight; }
			set {
				defaultIconHeight = value;
			}
		}
		
		public int DefaultIconWidth  {
			get { return defaultIconWidth; }
			set {
				defaultIconWidth = value;
			}
		}
		
		public object Tag {
			get;
			set;
		}
		
		Window window = null;
		public Func<DropDownBox, Window> WindowRequestFunc = null;
		
		
		public DropDownBox ()
		{
			layout = new Pango.Layout (this.PangoContext);
			this.Events = Gdk.EventMask.KeyPressMask | Gdk.EventMask.FocusChangeMask | Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
			this.CanFocus = true;
			BorderWidth = 0;
		}
		
		void PositionListWindow ()
		{
			if (window == null)
				return;
			int ox, oy;
			ParentWindow.GetOrigin (out ox, out oy);
			int dx = ox + this.Allocation.X;
			int dy = oy + this.Allocation.Bottom;
			window.WidthRequest = Allocation.Width;
			int width, height;
			window.GetSizeRequest (out width, out height);
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (dx, dy));
			
			if (dy + height > geometry.Bottom)
				dy = oy + this.Allocation.Y - height;
			if (dx + width > geometry.Right)
				dx = geometry.Right - width;
			
			window.Move (dx, dy);
			window.GetSizeRequest (out width, out height);
			window.GrabFocus ();
			window.FocusOutEvent += delegate {
				DestroyWindow ();
			};
			window.ShowAll ();
		}
		
		public void SetItem (string text, Gdk.Pixbuf icon, object currentItem)
		{
			if (currentItem != CurrentItem) {// don't update when the same item is set.
				this.Text = text;
				this.CurrentItem = currentItem;
				this.Pixbuf = icon;
				this.QueueDraw ();
			}
			
			if (ItemSet != null)
				ItemSet (this, EventArgs.Empty);
		}
		
		protected override void OnDestroyed ()
		{
			DestroyWindow ();
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			base.OnDestroyed ();
		}
		
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			int width, height;
			layout.GetPixelSize (out width, out height);
			
			if (Pixbuf != null) {
				width += Pixbuf.Width + pixbufSpacing * 2;
				height = System.Math.Max (height, Pixbuf.Height);
			} else {
				height = System.Math.Max (height, defaultIconHeight);
			}
			
//			if (DrawRightBorder)
//				width += 2;
			int arrowHeight = height / 2; 
			int arrowWidth = arrowHeight + 1;
			
			requisition.Height = height + ySpacing * 2;
			requisition.Width = width + arrowWidth + leftSpacing;
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			//DestroyWindow ();
			return base.OnFocusOutEvent (evnt);
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				DestroyWindow (); 
				return true;
			}
			
//			if (window != null && window.ProcessKey (evnt.Key, evnt.State))
//				return true;
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			if (e.TriggersContextMenu ()) {
				return base.OnButtonPressEvent (e);
			}
			if (e.Type == Gdk.EventType.ButtonPress) {
				if (window != null) {
					DestroyWindow ();
				} else {
					if (WindowRequestFunc != null) {
						window = WindowRequestFunc (this);
						window.Destroyed += delegate {
							window = null;
							QueueDraw ();
						};
						PositionListWindow ();
 					}
				}
			}
			return base.OnButtonPressEvent (e);
		}
		
		void DestroyWindow ()
		{
			if (window != null) 
				window.Destroy ();
		}
		
		protected override void OnStateChanged (StateType previous_state)
		{
			base.OnStateChanged (previous_state);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton e)
		{
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			QueueDraw ();
			return base.OnMotionNotifyEvent (e);
		}
	
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gdk.Drawable win = args.Window;
		
			int width, height;
			layout.GetPixelSize (out width, out height);
			
//			if (DrawRightBorder)
//				arrowXPos -= 2;
			
			var state = window != null? StateType.Active : State;
			
			//HACK: paint the button background as if it were bigger, but it stays clipped to the real area,
			// so we get the content but not the border. This might break with crazy themes.
			//FIXME: we can't use the style's actual internal padding because GTK# hasn't wrapped GtkBorder AFAICT
			// (default-border, inner-border, default-outside-border, etc - see http://git.gnome.org/browse/gtk+/tree/gtk/gtkbutton.c)
			const int padding = 0;
			Style.PaintBox (Style, args.Window, state, ShadowType.None, args.Area, this, "button", 
			                Allocation.X - padding, Allocation.Y - padding, Allocation.Width + padding * 2, Allocation.Height + padding * 2);
			
			int xPos = Allocation.Left + 4;
			if (Pixbuf != null) {
				win.DrawPixbuf (this.Style.BaseGC (StateType.Normal), Pixbuf, 0, 0, xPos + pixbufSpacing, Allocation.Y + (Allocation.Height - Pixbuf.Height) / 2, Pixbuf.Width, Pixbuf.Height, Gdk.RgbDither.None, 0, 0);
				xPos += Pixbuf.Width + pixbufSpacing * 2;
			}
			int arrowHeight = height - 4; 
			int arrowWidth = arrowHeight + 3;
			int arrowXPos = this.Allocation.X + this.Allocation.Width - arrowWidth;
			//constrain the text area so it doesn't get rendered under the arrows
			var textArea = new Gdk.Rectangle (xPos, Allocation.Y + ySpacing, arrowXPos - xPos - 2, Allocation.Height - ySpacing);
			Style.PaintLayout (Style, win, state, true, textArea, this, "", textArea.X, textArea.Y, layout);
			
			state = Sensitive ? StateType.Normal : StateType.Insensitive;
			
			Gtk.Style.PaintVline (this.Style, win, state, args.Area, this, "", Allocation.Y + 3, Allocation.Bottom - 4, arrowXPos - 4);
			Gtk.Style.PaintArrow (this.Style, win, state, ShadowType.None, args.Area, this, "", ArrowType.Down, true, arrowXPos, Allocation.Y, Allocation.Height / 2, Allocation.Height);
//			if (DrawRightBorder)
//				win.DrawLine (this.Style.DarkGC (StateType.Normal), Allocation.X + Allocation.Width - 1, Allocation.Y, Allocation.X + Allocation.Width - 1, Allocation.Y + Allocation.Height);			
			return false;
		}
		
		public EventHandler ItemSet;
	}
}

