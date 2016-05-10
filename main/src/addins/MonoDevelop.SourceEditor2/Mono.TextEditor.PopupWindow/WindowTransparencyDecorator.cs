//
// WindowTransparencyDecorator.cs
//
// Author:
//   Michael Hutchinson <mhutch@xamarin.com>
//
// Based on code derived from Banshee.Widgets.EllipsizeLabel
// by Aaron Bockover (aaron@aaronbock.net)
//
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
// Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
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
using System.Reflection;
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

namespace Mono.TextEditor.PopupWindow
{

	public class WindowTransparencyDecorator
	{
		Gtk.Window window;
		bool semiTransparent;
		bool snooperInstalled;
		uint snooperID;
		const double opacity = 0.2;

		WindowTransparencyDecorator (Gtk.Window window)
		{
			this.window = window;

			window.Shown += ShownHandler;
			window.Hidden += HiddenHandler;
			window.Destroyed += DestroyedHandler;
		}

		public static WindowTransparencyDecorator Attach (Gtk.Window window)
		{
			return new WindowTransparencyDecorator (window);
		}

		public void Detach ()
		{
			if (window == null)
				return;

			//remove the snooper
			HiddenHandler (null,  null);

			//annul allreferences between this and the window
			window.Shown -= ShownHandler;
			window.Hidden -= HiddenHandler;
			window.Destroyed -= DestroyedHandler;
			window = null;
		}

		void ShownHandler (object sender, EventArgs args)
		{
			if (!snooperInstalled)
				snooperID = Gtk.Key.SnooperInstall (TransparencyKeySnooper);
			snooperInstalled = true;

			//NOTE: we unset transparency when showing, instead of when hiding
			//because the latter case triggers a metacity+compositing bug that shows the window again
			SemiTransparent = false;
		}

		void HiddenHandler (object sender, EventArgs args)
		{
			if (snooperInstalled)
				Gtk.Key.SnooperRemove (snooperID);
			snooperInstalled = false;
		}

		void DestroyedHandler (object sender, EventArgs args)
		{
			Detach ();
		}

		int TransparencyKeySnooper (Gtk.Widget widget, EventKey evnt)
		{
			if (evnt != null && evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)
				SemiTransparent = (evnt.Type == Gdk.EventType.KeyPress);
			return 0; //FALSE
		}

		bool SemiTransparent {
			set {
				if (semiTransparent != value) {
					semiTransparent = value;
					window.Opacity = semiTransparent? opacity : 1.0;
				}
			}
		}
	}
}
