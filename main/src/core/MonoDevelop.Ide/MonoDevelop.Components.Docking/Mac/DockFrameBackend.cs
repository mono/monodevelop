//
// DockFrameBackend.cs
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
using MonoDevelop.Components.Docking.Internal;
using AppKit;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Components.Docking.Mac
{
	class DockFrameBackend: NSViewContainer, IDockFrameBackend
	{
		IDockFrameController controller;
		NSView overlay;

		public DockFrameBackend ()
		{
		}

		#region IDockFrameBackend implementation

		public void Initialize (IDockFrameController controller)
		{
			this.controller = controller;
		}

		public void LoadLayout (IDockLayout dl)
		{
		}

		public void Refresh (IDockObject obj)
		{
		}

		public IDockItemBackend CreateItemBackend (DockItem item)
		{
			return new DockItemBackend (item);
		}

		public Xwt.Rectangle GetAllocation ()
		{
			var frame = NSView.Frame;
			return new Xwt.Rectangle (frame.X, frame.Y, frame.Width, frame.Height);
		}

		public void AddOverlayWidget (Control widget, bool animate)
		{
			overlay = widget.GetNativeWidget<NSView> ();
			NSView.AddSubview (overlay);
			overlay.Frame = NSView.Frame;
		}

		public void RemoveOverlayWidget (bool animate)
		{
			overlay.RemoveFromSuperview ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (overlay != null)
				overlay.Frame = NSView.Frame;
		}

		#endregion
	}
}

