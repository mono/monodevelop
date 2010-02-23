// 
// PathBar.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;

using Gtk;
using Gdk;

namespace MonoDevelop.Components
{
	
	public class PathBar : Gtk.DrawingArea
	{
		string[] path = new string[0];
		Pango.Layout layout;
		Pango.AttrList boldAtts = new Pango.AttrList ();
		
		int[] widths;
		int height;
		
		bool pressed, hovering, menuVisible;
		int hoverIndex = -1;
		int activeIndex = -1;
		
		const int padding = 2;
		const int spacing = 10;
		
		Func<int,Menu> createMenuForItem;
		
		public PathBar (Func<int,Menu> createMenuForItem)
		{
			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
				           EventMask.KeyPressMask | 
					       EventMask.PointerMotionMask;
			boldAtts.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
			this.createMenuForItem = createMenuForItem;
		}
		
		public string[] Path { get { return path; } }
		public int ActiveIndex { get { return activeIndex; } }
		
		public void SetPath (string[] path)
		{
			if (ArrSame (this.path, path))
				return;
			
			this.path = path ?? new string[0];
			activeIndex = -1;
			widths = null;
			QueueResize ();
		}
		
		bool ArrSame (string[] a, string[] b)
		{
			if ((a == null || b == null) && a != b)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
		
		public void SetActive (int index)
		{
			if (index >= path.Length)
				throw new IndexOutOfRangeException ();
			
			if (activeIndex != index) {
				activeIndex = index;
				widths = null;
				QueueResize ();
			}
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			EnsureWidths ();
			requisition.Width = Math.Max (WidthRequest, 0);
			requisition.Height = height + padding * 2;
		}
		
		protected override bool OnExposeEvent (EventExpose evnt)
		{
			if (widths == null)
				return true;
			
			//FIXME use event area
			//FIXME: why don't we need to use the allocation.x and allocation.y here?
			int xpos = padding, ypos = padding;
			for (int i = 0; i < path.Length; i++) {
				bool last = i == path.Length - 1;
				
				if (hoverIndex == i && (menuVisible || pressed || hovering)) {
					Style.PaintBox (Style, GdkWindow,
					                (pressed || menuVisible)? StateType.Active : StateType.Prelight,
					                (pressed || menuVisible)? ShadowType.In : ShadowType.Out,
					                evnt.Area, this, "button",
					                xpos - padding, ypos - padding, widths[i] + padding + (last? padding : spacing),
					                height + padding * 2);
				}
				
				layout.Attributes = (i == activeIndex)? boldAtts : null;
				layout.SetText (path[i]);
				Style.PaintLayout (Style, GdkWindow, State, false, evnt.Area, this, "", xpos, ypos, layout);
				
				xpos += widths[i];
				if (!last) {
					int arrowH = Math.Min (height, spacing);
					int arrowY = ypos + (height - arrowH) / 2;
					Style.PaintArrow (Style, GdkWindow, State, ShadowType.None, evnt.Area, this, "", ArrowType.Right,
					                  true, xpos, arrowY, spacing, arrowH);
				}
				xpos += spacing;
			}
			
			return true;
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (hovering) {
				pressed = true;
				QueueDraw ();
			}
			return true;
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			pressed = false;
			if (hovering) {
				QueueDraw ();
				ShowMenu ();
			}
			return true;
		}
		
		void ShowMenu ()
		{
			if (hoverIndex < 0)
				return;
			
			Menu menu = createMenuForItem (hoverIndex);
			menu.Hidden += delegate {
				
				menuVisible = false;
				QueueDraw ();
				
				//FIXME: for some reason the menu's children don't get activated if we destroy 
				//directly here, so use a timeout to delay it
				GLib.Timeout.Add (100, delegate {
					menu.Destroy ();
					return false;
				});
			};
			menuVisible = true;
			menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
		}
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			var rect = this.Allocation;
			y += rect.Height;
			x += widths.Take (hoverIndex).Sum () + hoverIndex * spacing;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			pressed = false;
			SetHover (-1);
			return true;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		void SetHover (int i)
		{
			bool oldHovering = hovering;
			hovering = i > -1;
			
			if (hoverIndex != i || oldHovering != hovering) {
				if (hovering)
					hoverIndex = i;
				QueueDraw ();
			}
		}
		
		int GetItemAt (int x, int y)
		{
			int xpos = padding;
			if (widths == null || x < xpos)
				return -1;
			//could do a binary search, but probably not worth it
			for (int i = 0; i < path.Length; i++) {
				xpos += widths[i] + spacing;
				if (x < xpos)
					return i;
			}
			return -1;
		}
		
		void EnsureLayout ()
		{
			if (layout != null)
				layout.Dispose ();
			layout = new Pango.Layout (PangoContext);
		}
		
		void EnsureWidths ()
		{
			if (widths != null)
				return;
			EnsureLayout ();
			
			widths = new int[path.Length];
			for (int i = 0; i < widths.Length; i++) {
				layout.Attributes = (i == activeIndex)? boldAtts : null;
				layout.SetText (path[i]);
				int w, h;
				layout.GetPixelSize (out w, out h);
				height = Math.Max (h, height);
				widths[i] = w;
			}
		}
		
		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			KillLayout ();
		}
		
		void KillLayout ()
		{
			if (layout == null)
				return;
			layout.Dispose ();
			layout = null;
			boldAtts.Dispose ();
			
			widths = null;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			KillLayout ();
		}
	}
}
