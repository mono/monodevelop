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

	public class NSViewContainer : Container
	{
		static Dictionary<NSView,NSViewContainer> containers = new Dictionary<NSView, NSViewContainer> ();

		List<Widget> children = new List<Widget> ();
		NSView nsview;

		public NSViewContainer ()
		{
			WidgetFlags |= Gtk.WidgetFlags.NoWindow;
		}

		internal static NSViewContainer GetContainer (NSView v)
		{
			while (v != null) {
				NSViewContainer c;
				if (containers.TryGetValue (v, out c))
					return c;
				v = v.Superview;
			}
			return null;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			nsview = GtkMacInterop.GetNSView (this);
			containers [nsview] = this;
			ConnectSubviews (nsview);
		}

		void ConnectSubviews (NSView v)
		{
			if (v is GtkEmbed) {
				((GtkEmbed)v).Connect (this);
			} else {
				foreach (var sv in v.Subviews)
					ConnectSubviews (v);
			}
		}

		protected override void OnDestroyed ()
		{
			if (nsview != null)
				containers.Remove (nsview);
			base.OnDestroyed ();
		}

		protected override void OnAdded (Widget widget)
		{
			widget.Parent = this;
			children.Add (widget);
		}

		protected override void OnRemoved (Widget widget)
		{
			children.Remove (widget);
		}

		protected override void ForAll (bool include_internals, Callback cb)
		{
			foreach (Widget w in children.ToArray ())
				cb (w);
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
		}

		public NSView NSView {
			get {
				if (nsview == null)
					Realize ();
				return nsview; 
			}
		}
	}

}

#endif
