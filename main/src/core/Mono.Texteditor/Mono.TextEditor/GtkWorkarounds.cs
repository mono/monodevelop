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
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
		static extern IntPtr sel_registerName (string selector);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
		static extern IntPtr objc_getClass (string klass);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern IntPtr objc_msgSend_IntPtr (IntPtr klass, IntPtr selector);
		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
		static extern void objc_msgSend_RectangleF (out RectangleF rect, IntPtr klass, IntPtr selector);
		
		static IntPtr cls_NSScreen, sel_screens, sel_objectEnumerator, sel_nextObject, sel_frame, sel_visibleFrame;
		static bool mac_initialized = false;

		static void InitMac ()
		{
			if (mac_initialized)
				return;
			
			cls_NSScreen = objc_getClass ("NSScreen");
			sel_screens = sel_registerName ("screens");
			sel_objectEnumerator = sel_registerName ("objectEnumerator");
			sel_nextObject = sel_registerName ("nextObject");
			sel_visibleFrame = sel_registerName ("visibleFrame");
			sel_frame = sel_registerName ("frame");
			mac_initialized = true;
		}
		
		static Gdk.Rectangle MacGetUsableMonitorGeometry (Gdk.Screen screen, int monitor)
		{
			InitMac ();
			
			IntPtr array = objc_msgSend_IntPtr (cls_NSScreen, sel_screens);
			IntPtr iter = objc_msgSend_IntPtr (array, sel_objectEnumerator);
			RectangleF visible, frame;
			IntPtr scrn;
			int i = 0;
			
			while ((scrn = objc_msgSend_IntPtr (iter, sel_nextObject)) != IntPtr.Zero && i < monitor)
				i++;
			
			if (scrn == IntPtr.Zero)
				return screen.GetMonitorGeometry (monitor);
			
			objc_msgSend_RectangleF (out visible, scrn, sel_visibleFrame);
			objc_msgSend_RectangleF (out frame, scrn, sel_frame);
			
			// VisibleFrame.Y is the height of the Dock if it is at the bottom of the screen, so in order
			// to get the menu height, we just figure out the difference between the visibleFrame height
			// and the actual frame height, then subtract the Dock height.
			//
			// We need to swap the Y offset with the menu height because our callers expect the Y offset
			// to be from the top of the screen, not from the bottom of the screen.
			float menubar = (frame.Height - visible.Height) - visible.Y;
			visible.Y = menubar;
			
			return new Gdk.Rectangle ((int) visible.X, (int) visible.Y, (int) visible.Width, (int) visible.Height);
		}
		
		public static Gdk.Rectangle GetUsableMonitorGeometry (this Gdk.Screen screen, int monitor)
		{
			if (Platform.IsMac)
				return MacGetUsableMonitorGeometry (screen, monitor);
			
			return screen.GetMonitorGeometry (monitor);
		}
	}
}
