//
// EmbedWindow.cs
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

using Gtk;
using System;
using System.Collections;

namespace GladeAddIn.Gui {

	public class EmbedWindow {

		public static Widget Wrap (Window window)
		{
			if (window.IsRealized)
				throw new ApplicationException ("Cannot make a realized window embeddable");

			window.WidgetFlags &= ~(WidgetFlags.Toplevel);
			window.SizeAllocated += new SizeAllocatedHandler (OnSizeAllocated);
			window.Realized += new EventHandler (OnRealized);

			PreviewBox prev = new PreviewBox (window);
			prev.Title = window.Title;
			prev.Theme = Theme;
			return prev;
		}

		static private void OnSizeAllocated (object obj, SizeAllocatedArgs args)
		{
			Window window = obj as Window;
			Gdk.Rectangle allocation = args.Allocation;

			if (window.IsRealized) {
				window.GdkWindow.MoveResize (allocation.X,
							     allocation.Y,
							     allocation.Width,
							     allocation.Height);
			}
		}

		static private void OnRealized (object obj, EventArgs args)
		{
			Window window = obj as Window;

			window.WidgetFlags |= WidgetFlags.Realized;

			Gdk.WindowAttr attrs = new Gdk.WindowAttr ();
			attrs.Mask = window.Events |
				(Gdk.EventMask.ExposureMask |
				 Gdk.EventMask.KeyPressMask |
				 Gdk.EventMask.KeyReleaseMask |
				 Gdk.EventMask.EnterNotifyMask |
				 Gdk.EventMask.LeaveNotifyMask |
				 Gdk.EventMask.StructureMask);
			attrs.X = window.Allocation.X;
			attrs.Y = window.Allocation.Y;
			attrs.Width = window.Allocation.Width;
			attrs.Height = window.Allocation.Height;
			attrs.Wclass = Gdk.WindowClass.InputOutput;
			attrs.Visual = window.Visual;
			attrs.Colormap = window.Colormap;
			attrs.WindowType = Gdk.WindowType.Child;

			Gdk.WindowAttributesType mask =
				Gdk.WindowAttributesType.X |
				Gdk.WindowAttributesType.Y |
				Gdk.WindowAttributesType.Colormap |
				Gdk.WindowAttributesType.Visual;

			window.GdkWindow = new Gdk.Window (window.ParentWindow, attrs, mask);
			window.GdkWindow.UserData = window.Handle;

			window.Style = window.Style.Attach (window.GdkWindow);
			window.Style.SetBackground (window.GdkWindow, StateType.Normal);

			// FIXME: gtk-sharp 2.6
			// window.GdkWindow.EnableSynchronizedConfigure ();
		}

		static Metacity.Theme theme;
		static Metacity.Theme Theme {
			get {
				if (theme == null) {
					GConf.Client client = new GConf.Client ();
//					client.AddNotify ("/apps/metacity/general", GConfNotify);
					string themeName = (string)client.Get ("/apps/metacity/general/theme");
					theme = Metacity.Theme.Load (themeName);
				}
				return theme;
			}
		}

/*		static void GConfNotify (object obj, GConf.NotifyEventArgs args)
		{
			if (args.Key == "/apps/metacity/general/theme") {
				theme = Metacity.Theme.Load ((string)args.Value);
				foreach (Metacity.Preview prev in wrappers.Values)
					prev.Theme = Theme;
			}
		}
*/	}
}
