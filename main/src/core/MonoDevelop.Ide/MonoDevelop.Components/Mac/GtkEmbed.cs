//
// GtkEmbed.cs
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
using Foundation;
using AppKit;
using ObjCRuntime;
using Gtk;

namespace MonoDevelop.Components.Mac
{
	public class GtkEmbed : NSView
	{
		WidgetWithNativeWindow cw;
		NSViewContainer container;

		public GtkEmbed (NSViewContainer container, Gtk.Widget w)
		{
			this.container = container;
			cw = new WidgetWithNativeWindow (this);
			cw.Add (w);
			container.Add (cw);
			cw.Show ();
		}

		public override CoreGraphics.CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				UpdateAllocation ();
			}
		}

		[Export ("isGtkView")]
		public bool isGtkView ()
		{
			return true;
		}

		void UpdateAllocation ()
		{
			if (container.GdkWindow == null || cw.GdkWindow == null)
				return;

			var gw = GtkMacInterop.GetNSView (cw);
			gw.Frame = new CoreGraphics.CGRect (0, 0, Frame.Width, Frame.Height);
			var rect = GetRelativeAllocation (GtkMacInterop.GetNSView (container), gw);

			var allocation = new Gdk.Rectangle {
				X = (int)rect.Left,
				Y = (int)rect.Top,
				Width = (int)rect.Width,
				Height = (int)rect.Height
			};

			cw.SizeAllocate (allocation);
		}

		CoreGraphics.CGRect GetRelativeAllocation (NSView ancestor, NSView child)
		{
			if (child == null)
				return RectangleF.Empty;
			if (child.Superview == ancestor)
				return child.Frame;
			var f = GetRelativeAllocation (ancestor, child.Superview);
			var cframe = child.Frame;
			return new CoreGraphics.CGRect (cframe.X + f.X, cframe.Y + f.Y, cframe.Width, cframe.Height);
		}
	}
}

#endif