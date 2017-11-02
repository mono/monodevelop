//
// Window.cs
//
// Author:
//       therzok <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 therzok
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

namespace MonoDevelop.Components
{
	public class Window : Control
	{
		protected Window ()
		{
		}

		internal protected Window (object window)
		{
			if (window == null)
				throw new ArgumentNullException (nameof (window));
			
			this.nativeWidget = window;
			cache.Add (window, new WeakReference<Control> (this));
		}

		public static implicit operator Gtk.Window (Window d)
		{
			if (d is XwtWindowControl)
				return (XwtWindowControl)d;
			return d?.GetNativeWidget<Gtk.Window> ();
		}

		public static implicit operator Window (Gtk.Window d)
		{
			if (d == null)
				return null;

			var window = GetImplicit<Window, Gtk.Window>(d);
			if (window == null) {
				window = new Window (d);
				d.Destroyed += delegate {
					GC.SuppressFinalize (window);
					window.Dispose (true);
				};
			}
			return window;
		}

#if MAC
		public static implicit operator AppKit.NSWindow (Window d)
		{
			if (d is XwtWindowControl)
				return (XwtWindowControl)d;
			if (d?.nativeWidget is Gtk.Window)
				return Mac.GtkMacInterop.GetNSWindow (d.GetNativeWidget<Gtk.Window> ());
			
			return d?.GetNativeWidget<AppKit.NSWindow> ();
		}

		public static implicit operator Window (AppKit.NSWindow d)
		{
			if (d == null)
				return null;
			
			return GetImplicit<Window, AppKit.NSWindow> (d) ?? new Window (d);
		}
#endif

		public static implicit operator Window (Xwt.WindowFrame d)
		{
			if (d == null)
				return null;

			var window = GetImplicit<Window, Xwt.WindowFrame> (d);
			if (window == null) {
				window = new XwtWindowControl (d);
				d.Disposed += delegate {
					GC.SuppressFinalize (window);
					window.Dispose (true);
				};
			}
			return window;
		}

		public static implicit operator Xwt.WindowFrame (Window d)
		{
			if (d == null)
				return null;

			if (d is XwtWindowControl)
				return d.GetNativeWidget<Xwt.WindowFrame> ();

			if (d.nativeWidget is Gtk.Window)
				return Xwt.Toolkit.Load (Xwt.ToolkitType.Gtk).WrapWindow ((Gtk.Window)d);
#if MAC
			if (d.nativeWidget is AppKit.NSWindow)
				return Xwt.Toolkit.Load (Xwt.ToolkitType.XamMac).WrapWindow ((AppKit.NSWindow)d);
#endif

			throw new NotSupportedException ();
		}
	}
}

