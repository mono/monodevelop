//
// GtkMacInterop.cs
//
// Author:
//       Jérémie Laval <jeremie.laval@xamarin.com>
//
// Copyright (c) 2012 Xamarin, Inc.
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

#if MAC
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using System;

namespace MonoDevelop.Components.Mac
{
	public class GtkMacInterop
	{
		const string LibGdk = "libgdk-quartz-2.0.dylib";
		const string LibGtk = "libgtk-quartz-2.0";
		
		[System.Runtime.InteropServices.DllImport (LibGtk)]
		extern static IntPtr gtk_ns_view_new (IntPtr nsview);
		
		public static Gtk.Widget NSViewToGtkWidget (NSView view)
		{
			return new Gtk.Widget (gtk_ns_view_new ((IntPtr)view.Handle));
		}


		//this may be needed to work around focusing issues in GTK/Cocoa interop
		public static void FocusWindow (Gtk.Window widget)
		{
			if (widget == null) {
				return;
			}
			var window = GetNSWindow (widget);
			if (window != null)
				window.MakeKeyAndOrderFront (window);
		}

		public static Gtk.Window GetGtkWindow (NSWindow window)
		{
			if (window == null)
				return null;

			var toplevels = Gtk.Window.ListToplevels ();
			return toplevels.FirstOrDefault (w => w.IsRealized && gdk_quartz_window_get_nswindow (w.GdkWindow.Handle) == window.Handle);
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

		public static NSWindow GetNSWindow (Gtk.Window window)
		{
			var ptr = gdk_quartz_window_get_nswindow (window.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return ObjCRuntime.Runtime.GetNSObject<NSWindow> (ptr);
		}

		public static NSView GetNSView (Gtk.Widget widget)
		{
			var ptr = gdk_quartz_window_get_nsview (widget.GdkWindow.Handle);
			if (ptr == IntPtr.Zero)
				return null;
			return ObjCRuntime.Runtime.GetNSObject<NSView> (ptr);
		}


		static bool? supportsGtkIntoNSViewEmbedding;

		public static bool SupportsGtkIntoNSViewEmbedding ()
		{
			if (supportsGtkIntoNSViewEmbedding.HasValue)
				return supportsGtkIntoNSViewEmbedding.Value;

			try {
				supportsGtkIntoNSViewEmbedding = gdk_window_supports_nsview_embedding ();
				return supportsGtkIntoNSViewEmbedding.Value;
			} catch (DllNotFoundException) {
			} catch (EntryPointNotFoundException) {
			}
			supportsGtkIntoNSViewEmbedding = false;
			return false;
		}

		public static int GetTitleBarHeight ()
		{
			var frame = new CoreGraphics.CGRect (0, 0, 100, 100);
			var rect = NSWindow.ContentRectFor (frame, NSWindowStyle.Titled);
			return (int)(frame.Height - rect.Height);
		}

		internal static Gdk.EventKey ConvertKeyEvent (AppKit.NSEvent ev)
		{
			var state = Gdk.ModifierType.None;
			if ((ev.ModifierFlags & AppKit.NSEventModifierMask.ControlKeyMask) != 0)
				state |= Gdk.ModifierType.ControlMask;
			if ((ev.ModifierFlags & AppKit.NSEventModifierMask.ShiftKeyMask) != 0)
				state |= Gdk.ModifierType.ShiftMask;
			if ((ev.ModifierFlags & AppKit.NSEventModifierMask.CommandKeyMask) != 0)
				state |= Gdk.ModifierType.MetaMask;
			if ((ev.ModifierFlags & AppKit.NSEventModifierMask.AlternateKeyMask) != 0)
				state |= Gdk.ModifierType.Mod1Mask;

			var w = GetGtkWindow (ev.Window);
			return GtkUtil.CreateKeyEventFromKeyCode (ev.KeyCode, state, Gdk.EventType.KeyPress, w != null ? w.GdkWindow : null, (uint)(ev.Timestamp * 1000));
		}


		[DllImport (LibGtk)]
		static extern IntPtr gdk_quartz_window_get_nsview (IntPtr window);

		[DllImport (LibGtk)]
		static extern IntPtr gdk_quartz_window_get_nswindow (IntPtr window);

		[DllImport (LibGtk, CallingConvention = CallingConvention.Cdecl)]
		static extern bool gdk_window_supports_nsview_embedding ();

		/// <summary>
		/// Render a GTK widget to an AppKit NSImage
		/// </summary>
		public static NSImage RenderGtkWidget (Gtk.Widget widget)
		{
			var nativeView = GetNSView (widget);

			widget.TranslateCoordinates (widget.Toplevel, widget.Allocation.X, widget.Allocation.Y, out int transX, out int transY);
			var rect = new CoreGraphics.CGRect (transX, transY, widget.Allocation.Width, widget.Allocation.Height);

			var imageRep = nativeView.BitmapImageRepForCachingDisplayInRect (rect);
			nativeView.CacheDisplay (rect, imageRep);

			var image  = new NSImage (rect.Size);
			image.AddRepresentation (imageRep);
			return image;
		}
	}
}

#endif
