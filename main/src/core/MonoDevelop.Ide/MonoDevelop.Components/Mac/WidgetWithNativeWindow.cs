//
// WidgetWithNativeWindow.cs
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

#if MAC

using System;
using System.Drawing;
using Gtk;

namespace MonoDevelop.Components.Mac
{
	public class WidgetWithNativeWindow: Gtk.EventBox
	{
		GtkEmbed embedParent;

		public WidgetWithNativeWindow (GtkEmbed embed)
		{
			embedParent = embed;
		}

		protected override void OnRealized ()
		{
			WidgetFlags |= WidgetFlags.Realized;

			Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Height = Allocation.Height;
			attributes.Width = Allocation.Width;
			attributes.WindowType = Gdk.WindowType.Child;
			attributes.Wclass = Gdk.WindowClass.InputOutput;
			attributes.Visual = Visual;
			attributes.TypeHint = (Gdk.WindowTypeHint)100; // Custom value which means the this gdk window has to use a native window
			attributes.Colormap = Colormap;
			attributes.EventMask = (int)(Events |
				Gdk.EventMask.ExposureMask |
				Gdk.EventMask.Button1MotionMask |
				Gdk.EventMask.ButtonPressMask |
				Gdk.EventMask.ButtonReleaseMask |
				Gdk.EventMask.KeyPressMask |
				Gdk.EventMask.KeyReleaseMask);

			Gdk.WindowAttributesType attributes_mask =
				Gdk.WindowAttributesType.X |
				Gdk.WindowAttributesType.Y |
				Gdk.WindowAttributesType.Colormap |
				Gdk.WindowAttributesType.Visual;
			GdkWindow = new Gdk.Window (ParentWindow, attributes, (int)attributes_mask);
			GdkWindow.UserData = Handle;

			Style = Style.Attach (GdkWindow);
			Style.SetBackground (GdkWindow, State);
			this.WidgetFlags &= ~WidgetFlags.NoWindow;

			// Remove the gdk window from the original location and move it to the GtkEmbed view that contains it
			var gw = GtkMacInterop.GetNSView (this);
			gw.RemoveFromSuperview ();
			embedParent.AddSubview (gw);
			gw.Frame = new CoreGraphics.CGRect (0, 0, embedParent.Frame.Width, embedParent.Frame.Height);
		}
	}
}

#endif