//
// GtkWorkarounds.cs
//
// Authors: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (C) 2011 Xamarin Inc.
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

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Mono.TextEditor
{
	public static class GtkWorkarounds
	{
		const string LIBOBJC ="/usr/lib/libobjc.dylib";
		
		[DllImport (LIBOBJC, EntryPoint = "sel_registerName")]
		static extern IntPtr sel_registerName (string selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_getClass")]
		static extern IntPtr objc_getClass (string klass);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern IntPtr objc_msgSend_IntPtr (IntPtr klass, IntPtr selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern void objc_msgSend_void_bool (IntPtr klass, IntPtr selector, bool arg);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern bool objc_msgSend_bool (IntPtr klass, IntPtr selector);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend")]
		static extern bool objc_msgSend_int_int (IntPtr klass, IntPtr selector, int arg);
		
		[DllImport (LIBOBJC, EntryPoint = "objc_msgSend_stret")]
		static extern void objc_msgSend_RectangleF (out RectangleF rect, IntPtr klass, IntPtr selector);
		
		static IntPtr cls_NSScreen;
		static IntPtr sel_screens, sel_objectEnumerator, sel_nextObject, sel_frame, sel_visibleFrame,
			sel_activateIgnoringOtherApps, sel_isActive, sel_requestUserAttention;
		static IntPtr sharedApp;
		
		const int NSCriticalRequest = 0;
		const int NSInformationalRequest = 10;
		
		static System.Reflection.MethodInfo glibObjectGetProp, glibObjectSetProp;
		
		public static int GtkMinorVersion = 12;
		
		static GtkWorkarounds ()
		{
			if (Platform.IsMac) {
				InitMac ();
			}
			
			var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
			glibObjectSetProp = typeof (GLib.Object).GetMethod ("SetProperty", flags);
			glibObjectGetProp = typeof (GLib.Object).GetMethod ("GetProperty", flags);
			
			foreach (int i in new [] { 24, 22, 20, 18, 16, 14 }) {
				if (Gtk.Global.CheckVersion (2, (uint)i, 0) == null) {
					GtkMinorVersion = i;
					break;
				}
			}
		}

		static void InitMac ()
		{
			cls_NSScreen = objc_getClass ("NSScreen");
			sel_screens = sel_registerName ("screens");
			sel_objectEnumerator = sel_registerName ("objectEnumerator");
			sel_nextObject = sel_registerName ("nextObject");
			sel_visibleFrame = sel_registerName ("visibleFrame");
			sel_frame = sel_registerName ("frame");
			sel_activateIgnoringOtherApps = sel_registerName ("activateIgnoringOtherApps:");
			sel_isActive = sel_registerName ("isActive");
			sel_requestUserAttention = sel_registerName ("requestUserAttention:");
			sharedApp = objc_msgSend_IntPtr (objc_getClass ("NSApplication"), sel_registerName ("sharedApplication"));
		}
		
		static Gdk.Rectangle MacGetUsableMonitorGeometry (Gdk.Screen screen, int monitor)
		{
			IntPtr array = objc_msgSend_IntPtr (cls_NSScreen, sel_screens);
			IntPtr iter = objc_msgSend_IntPtr (array, sel_objectEnumerator);
			Gdk.Rectangle geometry = screen.GetMonitorGeometry (0);
			RectangleF visible, frame;
			IntPtr scrn;
			int i = 0;
			
			while ((scrn = objc_msgSend_IntPtr (iter, sel_nextObject)) != IntPtr.Zero && i < monitor)
				i++;
			
			if (scrn == IntPtr.Zero)
				return screen.GetMonitorGeometry (monitor);
			
			objc_msgSend_RectangleF (out visible, scrn, sel_visibleFrame);
			objc_msgSend_RectangleF (out frame, scrn, sel_frame);
			
			// Note: Frame and VisibleFrame rectangles are relative to monitor 0, but we need absolute
			// coordinates.
			visible.X += geometry.X;
			visible.Y += geometry.Y;
			frame.X += geometry.X;
			frame.Y += geometry.Y;
			
			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			float x, y, width, height;
			
			if (visible.Height < frame.Height) {
				float dockHeight = visible.Y;
				float menubarHeight = (frame.Height - visible.Height) - dockHeight;
				
				height = frame.Height - menubarHeight - dockHeight;
				y = menubarHeight;
			} else {
				height = frame.Height;
				y = frame.Y;
			}
			
			// Takes care of the possibility of the Dock being positioned on the left or right edge of the screen.
			width = System.Math.Min (visible.Width, frame.Width);
			x = System.Math.Max (visible.X, frame.X);
			
			return new Gdk.Rectangle ((int) x, (int) y, (int) width, (int) height);
		}
		
		static void MacRequestAttention (bool critical)
		{
			int kind = critical?  NSCriticalRequest : NSInformationalRequest;
			objc_msgSend_int_int (sharedApp, sel_requestUserAttention, kind);
		}
		
		public static Gdk.Rectangle GetUsableMonitorGeometry (this Gdk.Screen screen, int monitor)
		{
			if (Platform.IsMac)
				return MacGetUsableMonitorGeometry (screen, monitor);
			
			return screen.GetMonitorGeometry (monitor);
		}
		
		public static int RunDialogWithNotification (Gtk.Dialog dialog)
		{
			if (Platform.IsMac)
				MacRequestAttention (dialog.Modal);
			
			return dialog.Run ();
		}
		
		public static void PresentWindowWithNotification (this Gtk.Window window)
		{
			window.Present ();
			
			if (Platform.IsMac) {
				var dialog = window as Gtk.Dialog;
				MacRequestAttention (dialog == null? false : dialog.Modal);
			}
		}
		
		public static GLib.Value GetProperty (this GLib.Object obj, string name)
		{
			return (GLib.Value) glibObjectGetProp.Invoke (obj, new object[] { name });
		}
		
		public static void SetProperty (this GLib.Object obj, string name, GLib.Value value)
		{
			glibObjectSetProp.Invoke (obj, new object[] { name, value });
		}
		
		public static bool ButtonEventTriggersContextMenu (this Gdk.EventButton evt)
		{
			if (evt.Type == Gdk.EventType.ButtonPress) {
				if (evt.Button == 3 &&
						(evt.State & (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button2Mask)) == 0)
					return true;
				
				if (Platform.IsMac) {
					if (evt.Button == 1 &&
						(evt.State & Gdk.ModifierType.ControlMask) != 0 &&
						(evt.State & (Gdk.ModifierType.Button2Mask | Gdk.ModifierType.Button3Mask)) == 0)
					return true;
				}
			}
			return false;
		}
		
		public static void GetPageScrollPixelDeltas (this Gdk.EventScroll evt, double pageSizeX, double pageSizeY,
			out double deltaX, out double deltaY)
		{
			if (!GetEventScrollDeltas (evt, out deltaX, out deltaY)) {
				var direction = evt.Direction;
				deltaX = deltaY = 0;
				if (pageSizeY != 0 && (direction == Gdk.ScrollDirection.Down || direction == Gdk.ScrollDirection.Up)) {
					deltaY = System.Math.Pow (pageSizeY, 2.0 / 3.0);
					deltaX = 0.0;
					if (direction == Gdk.ScrollDirection.Up)
						deltaY = -deltaY;
				} else if (pageSizeX != 0) {
					deltaX = System.Math.Pow (pageSizeX, 2.0 / 3.0);
					deltaY = 0.0;
					if (direction == Gdk.ScrollDirection.Left)
						deltaX = -deltaX;
				}
			}
		}
		
		public static void AddValueClamped (this Gtk.Adjustment adj, double value)
		{
			adj.Value = System.Math.Max (adj.Lower, System.Math.Min (adj.Value + value, adj.Upper - adj.PageSize));
		}
		
		[DllImport (PangoUtil.LIBGTK)]
		extern static bool gdk_event_get_scroll_deltas (IntPtr eventScroll, out double deltaX, out double deltaY);
		static bool scrollDeltasNotSupported;
		
		public static bool GetEventScrollDeltas (Gdk.EventScroll evt, out double deltaX, out double deltaY)
		{
			if (!scrollDeltasNotSupported) {
				try {
					return gdk_event_get_scroll_deltas (evt.Handle, out deltaX, out deltaY);
				} catch (EntryPointNotFoundException) {
					scrollDeltasNotSupported = true;
				}
			}
			deltaX = deltaY = 0;
			return false;
		}
		
		/// <summary>Shows a context menu.</summary>
		/// <param name='menu'>The menu.</param>
		/// <param name='parent'>The parent widget.</param>
		/// <param name='evt'>The mouse event. May be null if triggered by keyboard.</param>
		/// <param name='caret'>The caret/selection position within the parent, if the EventButton is null.</param>
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.EventButton evt, Gdk.Rectangle caret)
		{
			Gtk.MenuPositionFunc posFunc = null;
			
			if (parent != null) {
				menu.AttachToWidget (parent, null);
				posFunc = delegate (Gtk.Menu m, out int x, out int y, out bool pushIn) {
					Gdk.Window window = evt != null? evt.Window : parent.GdkWindow;
					window.GetOrigin (out x, out y);
					var alloc = parent.Allocation;
					if (evt != null) {
						x += (int) evt.X;
						y += (int) evt.Y;
					} else if (caret.X >= alloc.X && caret.Y >= alloc.Y) {
						x += caret.X;
						y += caret.Y + caret.Height;
					} else {
						x += alloc.X;
						y += alloc.Y;
					}
					Gtk.Requisition request = m.SizeRequest ();
					var screen = parent.Screen;
					Gdk.Rectangle geometry = GetUsableMonitorGeometry (screen, screen.GetMonitorAtPoint (x, y));
					
					y = System.Math.Max (geometry.Top, System.Math.Min (y, geometry.Bottom - request.Height));
					x = System.Math.Max (geometry.Left, System.Math.Min (x, geometry.Right - request.Width));
					
					//TODO: more complicated PushIn logic using caret.Height
					pushIn = true;
				};
			}
			
			if (evt == null) {
				menu.Popup (null, null, posFunc, 0, Gtk.Global.CurrentEventTime);
			} else {
				menu.Popup (null, null, posFunc, evt.Button, evt.Time);
			}
		}
		
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.EventButton evt)
		{
			ShowContextMenu (menu, parent, evt, Gdk.Rectangle.Zero);
		}
		
		public static void ShowContextMenu (Gtk.Menu menu, Gtk.Widget parent, Gdk.Rectangle caret)
		{
			ShowContextMenu (menu, parent, null, caret);
		}
	}
}