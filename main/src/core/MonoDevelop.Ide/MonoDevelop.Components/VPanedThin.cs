//
// VPanedThin.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components
{
	public class VPanedThin: Gtk.VPaned
	{
		CustomPanedHandle handle;

		public VPanedThin ()
		{
			Mono.TextEditor.GtkWorkarounds.FixContainerLeak (this);
			handle = new CustomPanedHandle (this);
			handle.Parent = this;
		}

		public int GrabAreaSize {
			get { return handle.GrabAreaSize; }
			set {
				handle.GrabAreaSize = value;
				QueueResize ();
			}
		}

		public Gtk.Widget HandleWidget {
			get { return handle.HandleWidget; }
			set { handle.HandleWidget = value; }
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			if (handle != null)
				callback (handle);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			base.OnExposeEvent (evnt);
			
			if (Child1 != null && Child1.Visible && Child2 != null && Child2.Visible) {
				var gc = new Gdk.GC (evnt.Window);
				gc.RgbFgColor = (HslColor) Styles.ThinSplitterColor;
				var y = Child1.Allocation.Y + Child1.Allocation.Height;
				evnt.Window.DrawLine (gc, Allocation.X, y, Allocation.X + Allocation.Width, y);
				gc.Dispose ();
			}
			
			return true;
		}
	}
}

