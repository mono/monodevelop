// 
// TinyDropDownBox.cs
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
	public class DropDownBox  : Gtk.DrawingArea
	{
		Pango.Layout layout;
		const int pixbufSpacing = 4;
		const int leftSpacing   = 2;
		const int ySpacing   = 1;
		
		public string Text {
			get {
				return layout.Text;
			}
			set {
				layout.SetText (value);
				QueueResize ();
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
			
			if (dy + height > Screen.Height)
				dy = oy + this.Allocation.Y - height;
			if (dx + width > Screen.Width)
				dx = Screen.Width - width;
			
			window.Move (dx, dy);
			window.Show ();
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
				width += Pixbuf.Width + pixbufSpacing;
				height = System.Math.Max (height, Pixbuf.Height);
			}
			
			if (DrawRightBorder)
				width += 2;
			int arrowHeight = height / 2; 
			int arrowWidth = arrowHeight;
			
			requisition.Width = width + arrowWidth + leftSpacing;
			requisition.Height = height + ySpacing * 2;
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			DestroyWindow ();
			return base.OnFocusOutEvent (evnt);
		}

		
		bool isMouseOver = false;
		DropDownBoxListWindow window = null;
		void DestroyWindow ()
		{
			if (window != null) {
				window.Destroy ();
				window = null;
			}
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (window != null && window.ProcessKey (evnt.Key, evnt.State))
				return true;
			return base.OnKeyPressEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			isMouseOver = false;
			QueueDraw ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
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
							window.ShowAll ();
						}
					}
				}
			}
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton e)
		{
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			isMouseOver = true;
			QueueDraw ();
			return base.OnMotionNotifyEvent (e);
		}
	
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			Gdk.Drawable win = args.Window;
			using (var g = Gdk.CairoHelper.Create (win)) {
				int width, height;
				layout.GetPixelSize (out width, out height);
				
				int arrowHeight = height / 2; 
				int arrowWidth = arrowHeight;
				int arrowXPos = this.Allocation.Width - arrowWidth;
				if (DrawRightBorder)
					arrowXPos -= 2;
				
				DrawGradientBackground (g, new Gdk.Rectangle (0, 0, arrowXPos - leftSpacing , Allocation.Height));
				
				int xPos = leftSpacing;
				if (Pixbuf != null) {
					
					win.DrawPixbuf (this.Style.BaseGC (StateType.Normal), Pixbuf, 0, 0, xPos, (this.Allocation.Height - Pixbuf.Height) / 2, Pixbuf.Width, Pixbuf.Height, Gdk.RgbDither.None, 0, 0);
					xPos += Pixbuf.Width + pixbufSpacing;
				}
				win.DrawLayout (this.Style.TextGC (StateType.Normal), xPos, ySpacing, layout);
				
				
				
				DrawGradientBackground (g, new Gdk.Rectangle (arrowXPos - leftSpacing, 0, Allocation.Width - (arrowXPos - leftSpacing), Allocation.Height));
				
				Gtk.Style.PaintArrow (this.Style, win, StateType.Normal, ShadowType.None, args.Area, this, "", ArrowType.Up, true, arrowXPos, (Allocation.Height) / 2 - arrowHeight, arrowWidth, arrowHeight);
				Gtk.Style.PaintArrow (this.Style, win, StateType.Normal, ShadowType.None, args.Area, this, "", ArrowType.Down, true, arrowXPos, (Allocation.Height) / 2, arrowWidth, arrowHeight);
				if (DrawRightBorder)
					win.DrawLine (this.Style.DarkGC (StateType.Normal), Allocation.Width - 1, 0, Allocation.Width - 1, Allocation.Height);
				xPos += arrowWidth;
			}
			return true;
		}
		
		void DrawGradientBackground (Cairo.Context g, Gdk.Rectangle area)
		{
			DrawRectangle (g, area.X, area.Y, area.Right, area.Bottom);
			
			Cairo.Gradient pat = new Cairo.LinearGradient (area.X, area.Y, area.X, area.Y + area.Height);
			Cairo.Color lightBg = ToCairoColor (Style.Background (isMouseOver ? StateType.Prelight : StateType.Normal));
			Cairo.Color darkBg = ToCairoColor (Style.Base (isMouseOver ? StateType.Prelight : StateType.Normal));
	
			pat.AddColorStop (0, lightBg);
			pat.AddColorStop (0.5, darkBg);
			pat.AddColorStop (1, lightBg);
			g.Pattern = pat;
			g.Fill ();
				
		}
		
		public static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		static void DrawRectangle (Cairo.Context g, int x, int y, int width, int height)
		{
			int right = x + width;
			int bottom = y + height;
			g.MoveTo (new Cairo.PointD (x, y));
			g.LineTo (new Cairo.PointD (right, y));
			g.LineTo (new Cairo.PointD (right, bottom));
			g.LineTo (new Cairo.PointD (x, bottom));
			g.LineTo (new Cairo.PointD (x, y));
			g.ClosePath ();
		}
		
		public EventHandler ItemSet;
	}
}
