//
// Control.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Components.Commands;

#if MAC
using AppKit;
using MonoDevelop.Components.Mac;
#endif

namespace MonoDevelop.Components
{
	public class Control: IDisposable
	{
		object nativeWidget;

		protected Control ()
		{
		}

		public Control (object widget)
		{
			this.nativeWidget = widget;
		}

		~Control ()
		{
			Dispose (false);
		}

		protected virtual object CreateNativeWidget ()
		{
			throw new NotSupportedException ();
		}

		public T GetNativeWidget<T> ()
		{
			if (nativeWidget == null) {
				var w = CreateNativeWidget ();
				if (!(w is T))
					w = ConvertToType (typeof(T), w);
				if (w is Gtk.Widget) {
					var c = new CommandRouterContainer ((Gtk.Widget)w, this, true);
					c.Show ();
					nativeWidget = c;
					c.Destroyed += delegate {
						GC.SuppressFinalize (this);
						Dispose (true);
					};
				}
				else
					nativeWidget = w;
			}
			if (nativeWidget is T)
				return (T)nativeWidget;
			else
				throw new NotSupportedException ();
		}

		static object ConvertToType (Type t, object w)
		{
			if (t.IsInstanceOfType (w))
				return w;

			#if MAC
			if (w is NSView && t == typeof(Gtk.Widget)) {
				var ww = GtkMacInterop.NSViewToGtkWidget ((NSView)w);
				ww.Show ();
				return ww;
			}
			if (w is Gtk.Widget && t == typeof(NSView)) {
				return new GtkEmbed ((Gtk.Widget)w);
			}
			#endif
			throw new NotSupportedException ();
		}

		public static implicit operator Gtk.Widget (Control d)
		{
			return d.GetNativeWidget<Gtk.Widget> ();
		}

		public static implicit operator Control (Gtk.Widget d)
		{
			return new Control (d);
		}

		public void GrabFocus ()
		{
			// TODO
		}

		public void Dispose ()
		{
			if (nativeWidget is Gtk.Widget) {
				((Gtk.Widget)nativeWidget).Destroy ();
				return;
			}
			#if MAC
			else if (nativeWidget is NSView)
				((NSView)nativeWidget).Dispose ();
			#endif

			GC.SuppressFinalize (this);
			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
		}
	}
}

