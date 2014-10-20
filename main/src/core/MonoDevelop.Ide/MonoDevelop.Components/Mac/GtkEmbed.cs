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
using CoreGraphics;

using System;
using Foundation;
using AppKit;
using ObjCRuntime;
using Gtk;
using System.Collections.Generic;

namespace MonoDevelop.Components.Mac
{
	public class GtkEmbed : NSView
	{
		WidgetWithNativeWindow cw;
		NSViewContainer container;
		Gtk.Widget embeddedWidget;

		public GtkEmbed (Gtk.Widget w)
		{
			if (!GtkMacInterop.SupportsGtkIntoNSViewEmbedding ())
				throw new NotSupportedException ("GTK/NSView embedding is not supported by the installed GTK");

			embeddedWidget = w;
			var s = w.SizeRequest ();
			SetFrameSize (new CGSize (s.Width, s.Height));
			WatchForFocus (w);
		}

		internal void Connect (NSViewContainer container)
		{
			this.container = container;
			cw = new WidgetWithNativeWindow (this);
			cw.Add (embeddedWidget);
			container.Add (cw);
			cw.Show ();
		}

		void WatchForFocus (Widget widget)
		{
			widget.FocusInEvent += (o, args) => {
				var view = GtkMacInterop.GetNSView (widget);
				if (view != null)
					view.Window.MakeFirstResponder (view);
			};

			if (widget is Container) {
				Container c = (Container)widget;
				foreach (Widget w in c.Children)
					WatchForFocus (w);
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
			gw.Frame = new CGRect (0, 0, Frame.Width, Frame.Height);
			var rect = GetRelativeAllocation (GtkMacInterop.GetNSView (container), gw);

			var allocation = new Gdk.Rectangle {
				X = (int)rect.Left,
				Y = (int)rect.Top,
				Width = (int)rect.Width,
				Height = (int)rect.Height
			};

			cw.SizeAllocate (allocation);
		}

		CGRect GetRelativeAllocation (NSView ancestor, NSView child)
		{
			if (child == null)
				return CGRect.Empty;
			if (child.Superview == ancestor)
				return child.Frame;
			var f = GetRelativeAllocation (ancestor, child.Superview);
			var cframe = child.Frame;
			return new CGRect (cframe.X + f.X, cframe.Y + f.Y, cframe.Width, cframe.Height);
		}

		public override CGRect Frame {
			get {
				return base.Frame;
			}
			set {
				base.Frame = value;
				UpdateAllocation ();
			}
		}

		public override void ViewDidMoveToSuperview ()
		{
			base.ViewDidMoveToSuperview ();
			var c = NSViewContainer.GetContainer (Superview);
			if (c != null)
				Connect (c);
		}

		public override void RemoveFromSuperview ()
		{
			base.RemoveFromSuperview ();
			if (container != null) {
				container.Remove (cw);
				container = null;
			}
		}
	}
}

#endif