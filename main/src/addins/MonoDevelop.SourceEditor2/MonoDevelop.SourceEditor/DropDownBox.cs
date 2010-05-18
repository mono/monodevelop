// 
// DropDownBox.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;

namespace MonoDevelop.SourceEditor
{
	[Category ("Widgets")]
	[ToolboxItem (true)]
	public class DropDownBox : Gtk.Button
	{
		Pango.Layout layout;
		const int pixbufSpacing = 2;
		const int leftSpacing   = 2;
		const int ySpacing   = 1;
		
		public string Text {
			get {
				return layout.Text;
			}
			set {
				layout.SetText (value);
//				QueueResize ();
			}
		}
		
		public DropDownBoxListWindow.IListDataProvider DataProvider {
			get;
			set;
		}
		
		public bool DrawRightBorder {
			get;
			set;
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
			Gdk.Rectangle geometry = Screen.GetMonitorGeometry (Screen.GetMonitorAtPoint (dx, dy));
			
			if (dy + height > geometry.Bottom)
				dy = oy + this.Allocation.Y - height;
			if (dx + width > geometry.Right)
				dx = geometry.Right - width;
			
			window.Move (dx, dy);
			window.GetSizeRequest (out width, out height);
			window.GrabFocus ();
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
		
		public void SetItem (int i)
		{
			SetItem (DataProvider.GetText (i), DataProvider.GetIcon (i), DataProvider.GetTag (i));
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
			
			if (DrawRightBorder)
				width += 2;
			int arrowHeight = height / 2; 
			int arrowWidth = arrowHeight + 1;
			
			requisition.Width = width + arrowWidth + leftSpacing;
			requisition.Height = height + ySpacing * 2;
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			DestroyWindow ();
			return base.OnFocusOutEvent (evnt);
		}

		
		DropDownBoxListWindow window = null;
		internal void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
				QueueDraw ();
			}
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				DestroyWindow (); 
				return true;
			}
			if (window != null && window.ProcessKey (evnt.Key, evnt.State))
				return true;
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			if (e.Button == 3) {
				StatusBox.ShowNavigationBarContextMenu ();
				return true;
			}
			if (e.Type == Gdk.EventType.ButtonPress) {
				if (window != null) {
					DestroyWindow ();
				} else {
					this.GrabFocus ();
					if (DataProvider != null) {
						DataProvider.Reset ();
						if (DataProvider.IconCount > 0) {
							window = new DropDownBoxListWindow (this);
							PositionListWindow ();
							window.SelectItem (CurrentItem);
						}
					}
				}
			}
			return base.OnButtonPressEvent (e);
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
			
			int arrowHeight = height / 2; 
			int arrowWidth = arrowHeight + 1;
			int arrowXPos = this.Allocation.X + this.Allocation.Width - arrowWidth;
			if (DrawRightBorder)
				arrowXPos -= 2;
			
			//HACK: don't ever draw insensitive, only active/prelight/normal, because insensitive generally looks really ugly
			//this *might* cause some theme issues with the state of the text/arrows rendering on top of it
			var state = window != null? StateType.Active
				: State == StateType.Insensitive? StateType.Normal : State;
			
			//HACK: paint the button background as if it were bigger, but it stays clipped to the real area,
			// so we get the content but not the border. This might break with crazy themes.
			//FIXME: we can't use the style's actual internal padding because GTK# hasn't wrapped GtkBorder AFAICT
			// (default-border, inner-border, default-outside-border, etc - see http://git.gnome.org/browse/gtk+/tree/gtk/gtkbutton.c)
			const int padding = 4;
			Style.PaintBox (Style, args.Window, state, ShadowType.None, args.Area, this, "button", 
			                Allocation.X - padding, Allocation.Y - padding, Allocation.Width + padding * 2, Allocation.Height + padding * 2);
			
			int xPos = Allocation.Left;
			if (Pixbuf != null) {
				win.DrawPixbuf (this.Style.BaseGC (StateType.Normal), Pixbuf, 0, 0, xPos + pixbufSpacing, Allocation.Y + (Allocation.Height - Pixbuf.Height) / 2, Pixbuf.Width, Pixbuf.Height, Gdk.RgbDither.None, 0, 0);
				xPos += Pixbuf.Width + pixbufSpacing * 2;
			}
			
			//constrain the text area so it doesn't get rendered under the arrows
			var textArea = new Gdk.Rectangle (xPos, Allocation.Y + ySpacing, arrowXPos - xPos - 2, Allocation.Height - ySpacing);
			Style.PaintLayout (Style, win, state, true, textArea, this, "", textArea.X, textArea.Y, layout);
			
			state = Sensitive ? StateType.Normal : StateType.Insensitive;
			Gtk.Style.PaintArrow (this.Style, win, state, ShadowType.None, args.Area, this, "", ArrowType.Up, true, arrowXPos, Allocation.Y + (Allocation.Height) / 2 - arrowHeight, arrowWidth, arrowHeight);
			Gtk.Style.PaintArrow (this.Style, win, state, ShadowType.None, args.Area, this, "", ArrowType.Down, true, arrowXPos, Allocation.Y + (Allocation.Height) / 2, arrowWidth, arrowHeight);
			if (DrawRightBorder)
				win.DrawLine (this.Style.DarkGC (StateType.Normal), Allocation.X + Allocation.Width - 1, Allocation.Y, Allocation.X + Allocation.Width - 1, Allocation.Y + Allocation.Height);			
			return false;
		}
		
		public EventHandler ItemSet;
	}
}
