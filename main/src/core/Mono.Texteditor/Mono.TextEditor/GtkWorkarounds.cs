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
	}
}
