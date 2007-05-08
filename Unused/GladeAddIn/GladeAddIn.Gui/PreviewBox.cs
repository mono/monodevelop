//
// PreviewBox.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using Gdk;

namespace GladeAddIn.Gui
{	
	class PreviewBox: ScrolledWindow
	{
		Metacity.Preview preview;
		
		public Metacity.Theme Theme {
			set { preview.Theme = value; }
		}
		
		public string Title {
			set { preview.Title = value; }
		}
		
		public PreviewBox (Gtk.Window window)
		{
			ShadowType = ShadowType.None;
			HscrollbarPolicy = PolicyType.Automatic;
			VscrollbarPolicy = PolicyType.Automatic;
			
			preview = new Metacity.Preview ();
			
			switch (window.TypeHint) {
			case Gdk.WindowTypeHint.Normal:
				preview.FrameType = Metacity.FrameType.Normal;
				break;
			case Gdk.WindowTypeHint.Dialog:
				preview.FrameType = window.Modal ? Metacity.FrameType.ModalDialog : Metacity.FrameType.Dialog;	
				break;
			case Gdk.WindowTypeHint.Menu:
				preview.FrameType = Metacity.FrameType.Menu;
				break;
			case Gdk.WindowTypeHint.Splashscreen:
				preview.FrameType = Metacity.FrameType.Border;
				break;
			case Gdk.WindowTypeHint.Utility:
				preview.FrameType = Metacity.FrameType.Utility;
				break;
			default:
				preview.FrameType = Metacity.FrameType.Normal;
				break;
			}

			Metacity.FrameFlags flags =
				Metacity.FrameFlags.AllowsDelete |
				Metacity.FrameFlags.AllowsVerticalResize |
				Metacity.FrameFlags.AllowsHorizontalResize |
				Metacity.FrameFlags.AllowsMove |
				Metacity.FrameFlags.AllowsShade |
				Metacity.FrameFlags.HasFocus;
			if (window.Resizable)
				flags = flags | Metacity.FrameFlags.AllowsMaximize;
			preview.FrameFlags = flags;

			preview.Add (window);
			ResizableFixed fixd = new ResizableFixed ();
			fixd.Put (preview, window);
			AddWithViewport (fixd);
		}
	}
	
	class ResizableFixed: EventBox
	{
		Gtk.Widget child;
		int difX, difY;
		bool resizingX;
		bool resizingY;
		Fixed fixd;
		Gtk.Window window;
		
		Cursor cursorX = new Cursor (CursorType.RightSide);
		Cursor cursorY = new Cursor (CursorType.BottomSide);
		Cursor cursorXY = new Cursor (CursorType.BottomRightCorner);
		
		const int padding = 6;
		const int selectionBorder = 6;
		
		Requisition currentSizeRequest;
		
		public ResizableFixed ()
		{
			fixd = new Fixed ();
			Add (fixd);
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask; 
		}
		
		public void Put (Gtk.Widget child, Gtk.Window window)
		{
			this.child = child;
			this.window = window;
			fixd.Put (child, selectionBorder + padding, selectionBorder + padding);
			child.SizeRequested += new SizeRequestedHandler (OnSizeReq);
		}
		
		void OnSizeReq (object o, SizeRequestedArgs a)
		{
			currentSizeRequest = a.Requisition;
			
			Rectangle alloc = child.Allocation;
			int nw = alloc.Width;
			int nh = alloc.Height;
			
			if (a.Requisition.Width > nw) nw = a.Requisition.Width;
			if (a.Requisition.Height > nh) nh = a.Requisition.Height;
			
			if (nw != alloc.Width || nh != alloc.Height) {
				child.SetSizeRequest (nw, nh);
				QueueDraw ();
			}
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
			if (resizingX || resizingY) {
				if (resizingX) {
					int nw = (int)(ev.X - difX - padding - selectionBorder);
					if (nw < currentSizeRequest.Width) nw = currentSizeRequest.Width;
					child.WidthRequest = nw;
				}
				
				if (resizingY) {
					int nh = (int)(ev.Y - difY - padding - selectionBorder);
					if (nh < currentSizeRequest.Height) nh = currentSizeRequest.Height;
					child.HeightRequest = nh;
				}
				QueueDraw ();
			} else {
				if (GetAreaResizeXY ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorXY;
				else if (GetAreaResizeX ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorX;
				else if (GetAreaResizeY ().Contains ((int) ev.X, (int) ev.Y))
					GdkWindow.Cursor = cursorY;
				else
					GdkWindow.Cursor = null;
			}
			
			return base.OnMotionNotifyEvent (ev);
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			Gdk.Rectangle rectArea = child.Allocation;
			rectArea.Inflate (selectionBorder, selectionBorder);
			
			if (rectArea.Contains ((int) ev.X, (int) ev.Y)) {
				Gladeui.Widget gw = Gladeui.Widget.FromObject (window);
				gw.Project.SelectionSet (window, true);
				
				Rectangle rect = GetAreaResizeXY ();
				if (rect.Contains ((int) ev.X, (int) ev.Y)) {
					resizingX = resizingY = true;
					difX = (int) (ev.X - rect.X);
					difY = (int) (ev.Y - rect.Y);
					GdkWindow.Cursor = cursorXY;
				}
				
				rect = GetAreaResizeY ();
				if (rect.Contains ((int) ev.X, (int) ev.Y)) {
					resizingY = true;
					difY = (int) (ev.Y - rect.Y);
					GdkWindow.Cursor = cursorY;
				}
				
				rect = GetAreaResizeX ();
				if (rect.Contains ((int) ev.X, (int) ev.Y)) {
					resizingX = true;
					difX = (int) (ev.X - rect.X);
					GdkWindow.Cursor = cursorX;
				}
			}
			
			return base.OnButtonPressEvent (ev);
		}
		
		Rectangle GetAreaResizeY ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X - selectionBorder, rect.Y + rect.Height, rect.Width + selectionBorder, selectionBorder);
		}
		
		Rectangle GetAreaResizeX ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X + rect.Width, rect.Y - selectionBorder, selectionBorder, rect.Height + selectionBorder);
		}
		
		Rectangle GetAreaResizeXY ()
		{
			Gdk.Rectangle rect = child.Allocation;
			return new Gdk.Rectangle (rect.X + rect.Width, rect.Y + rect.Height, selectionBorder, selectionBorder);
		}
		
		protected override bool OnButtonReleaseEvent (Gdk.EventButton ev)
		{
			resizingX = resizingY = false;
			GdkWindow.Cursor = null;
			return base.OnButtonReleaseEvent (ev);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			bool r = base.OnExposeEvent (ev);
			Gdk.Rectangle rect = child.Allocation;
			rect.Inflate (selectionBorder, selectionBorder);
			GdkWindow.DrawRectangle (Style.BlackGC, false, rect.X, rect.Y, rect.Width, rect.Height);
			
			int w, h;
			GdkWindow.GetSize (out w, out h);
			
			return r;
		}
	}
}
