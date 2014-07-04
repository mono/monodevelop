//
// NSViewContainer.cs
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
using System.Collections.Generic;
using Foundation;
using AppKit;
using ObjCRuntime;
using Gtk;

namespace MonoDevelop.Components.Mac
{
	public class NSViewContainer : Bin
	{
		public NSViewContainer ()
		{
			WidgetFlags |= Gtk.WidgetFlags.NoWindow;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (nsviewChild != null)
				nsviewChild.SetFrameSize (new System.Drawing.SizeF (allocation.Width, allocation.Height));
		}

		public void AddNSChild (NSView nsview)
		{
			nsviewChild = nsview;
			NSView.AddSubview (nsview);
		}

		/// <summary>
		/// Creates an NSView that embeds the provided GTK widget.
		/// This view can be added to any NSView inside this container
		/// </summary>
		public NSView CreateEmbedView (Gtk.Widget widget)
		{
			widget.SizeRequest ();
			embedView = new GtkEmbed (this, widget);

			WatchForFocus (widget);

			return embedView;
		}

		private void WatchForFocus (Widget widget)
		{
			widget.FocusInEvent += (o, args) => {
				NSView.Window.MakeFirstResponder (embedView);
			};

			if (widget is Container) {
				Container c = (Container)widget;

				foreach (Widget w in c.Children) {
					WatchForFocus (w);
				}
			}
		}

		/// <summary>
		/// The root NSView for this widget
		/// </summary>
		public NSView NSView {
			get { return GtkMacInterop.GetNSView (this); }
		}

		public NSView NSChild {
			get { return nsviewChild; }
		}

		private NSView embedView;
		private NSView nsviewChild;
	}
}

#endif