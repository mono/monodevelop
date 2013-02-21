// 
// GtkQuartz.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Runtime.InteropServices;
using MonoMac.AppKit;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.MacInterop
{
	public static class GtkQuartz
	{
		//this may be needed to work around focusing issues in GTK/Cocoa interop
		public static void FocusWindow (Gtk.Window widget)
		{
			if (widget == null) {
				return;
			}
			var window = GetWindow (widget);
			if (window != null)
				window.MakeKeyAndOrderFront (window);
		}

		public static Gtk.Window GetGtkWindow (NSWindow window)
		{
			var toplevels = Gtk.Window.ListToplevels ();
			return toplevels.FirstOrDefault (w => gdk_quartz_window_get_nswindow (w.GdkWindow.Handle) == window.Handle);
		}

		public static IEnumerable<KeyValuePair<NSWindow,Gtk.Window>> GetToplevels ()
		{
			var nsWindows = NSApplication.SharedApplication.Windows;
			var gtkWindows = Gtk.Window.ListToplevels ();
			foreach (var n in nsWindows) {
				var g = gtkWindows.FirstOrDefault (w => {
					return w.GdkWindow != null && gdk_quartz_window_get_nswindow (w.GdkWindow.Handle) == n.Handle;
				});
				yield return new KeyValuePair<NSWindow, Gtk.Window> (n, g);
			}
		}
		
		public static NSWindow GetWindow (Gtk.Window window)
		{
			var ptr = gdk_quartz_window_get_nswindow (window.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr) as NSWindow;
		}
		
		public static NSView GetView (Gtk.Widget widget)
		{
			var ptr = gdk_quartz_window_get_nsview (widget.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return MonoMac.ObjCRuntime.Runtime.GetNSObject (ptr) as NSView;
		}
		
		[DllImport ("libgtk-quartz-2.0.dylib")]
		static extern IntPtr gdk_quartz_window_get_nsview (IntPtr window);
		
		[DllImport ("libgtk-quartz-2.0.dylib")]
		static extern IntPtr gdk_quartz_window_get_nswindow (IntPtr window);
	}
}
