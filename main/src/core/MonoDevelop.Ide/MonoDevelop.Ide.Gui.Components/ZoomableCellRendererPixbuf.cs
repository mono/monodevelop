//
// ZoomableCellRendererPixbuf.cs
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

//#define TREE_VERIFY_INTEGRITY

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Extensions;
using System.Linq;
using MonoDevelop.Ide.Gui.Components.Internal;

namespace MonoDevelop.Ide.Gui.Components
{

	class ZoomableCellRendererPixbuf: CellRendererImage
	{
		double zoom = 1f;

		Dictionary<Xwt.Drawing.Image,Xwt.Drawing.Image> resizedCache = new Dictionary<Xwt.Drawing.Image, Xwt.Drawing.Image> ();

		Xwt.Drawing.Image overlayBottomLeft;
		Xwt.Drawing.Image overlayBottomRight;
		Xwt.Drawing.Image overlayTopLeft;
		Xwt.Drawing.Image overlayTopRight;

		public double Zoom {
			get { return zoom; }
			set {
				if (zoom != value) {
					zoom = value;
					resizedCache.Clear ();
					Notify ("image");
				}
			}
		}

		public override Xwt.Drawing.Image Image {
			get {
				return base.Image;
			}
			set {
				base.Image = GetResized (value);
			}
		}

		public override Xwt.Drawing.Image ImageExpanderOpen {
			get {
				return base.ImageExpanderOpen;
			}
			set {
				base.ImageExpanderOpen = GetResized (value);
			}
		}

		public override Xwt.Drawing.Image ImageExpanderClosed {
			get {
				return base.ImageExpanderClosed;
			}
			set {
				base.ImageExpanderClosed = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-top-left")]
		public Xwt.Drawing.Image OverlayTopLeft {
			get {
				return overlayTopLeft;
			}
			set {
				overlayTopLeft = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-top-right")]
		public Xwt.Drawing.Image OverlayTopRight {
			get {
				return overlayTopRight;
			}
			set {
				overlayTopRight = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-bottom-left")]
		public Xwt.Drawing.Image OverlayBottomLeft {
			get {
				return overlayBottomLeft;
			}
			set {
				overlayBottomLeft = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-bottom-right")]
		public Xwt.Drawing.Image OverlayBottomRight {
			get {
				return overlayBottomRight;
			}
			set {
				overlayBottomRight = GetResized (value);
			}
		}

		Xwt.Drawing.Image GetResized (Xwt.Drawing.Image value)
		{
			//this can happen during solution deserialization if the project is unrecognized
			//because a line is added into the treeview with no icon
			if (value == null || value == CellRendererImage.NullImage)
				return null;

			if (zoom == 1)
				return value;

			Xwt.Drawing.Image resized;
			if (resizedCache.TryGetValue (value, out resized))
				return resized;

			int w = (int) (zoom * (double) value.Width);
			int h = (int) (zoom * (double) value.Height);
			if (w == 0) w = 1;
			if (h == 0) h = 1;
			resized = value.WithSize (w, h);
			resizedCache [value] = resized;
			return resized;
		}

		public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			/*			if (overlayBottomLeft != null || overlayBottomRight != null)
				height += overlayOverflow;
			if (overlayTopLeft != null || overlayTopRight != null)
				height += overlayOverflow;
			if (overlayBottomRight != null || overlayTopRight != null)
				width += overlayOverflow;*/
		}

		const int overlayOverflow = 2;

		protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
		{
			base.Render (window, widget, background_area, cell_area, expose_area, flags);

			if (overlayBottomLeft != null || overlayBottomRight != null || overlayTopLeft != null || overlayTopRight != null) {
				int x, y;
				Xwt.Drawing.Image image;
				GetImageInfo (cell_area, out image, out x, out y);

				if (image == null)
					return;

				using (var ctx = Gdk.CairoHelper.Create (window)) {
					if (overlayBottomLeft != null && overlayBottomLeft != NullImage)
						ctx.DrawImage (widget, overlayBottomLeft, x - overlayOverflow, y + image.Height - overlayBottomLeft.Height + overlayOverflow);
					if (overlayBottomRight != null && overlayBottomRight != NullImage)
						ctx.DrawImage (widget, overlayBottomRight, x + image.Width - overlayBottomRight.Width + overlayOverflow, y + image.Height - overlayBottomRight.Height + overlayOverflow);
					if (overlayTopLeft != null && overlayTopLeft != NullImage)
						ctx.DrawImage (widget, overlayTopLeft, x - overlayOverflow, y - overlayOverflow);
					if (overlayTopRight != null && overlayTopRight != NullImage)
						ctx.DrawImage (widget, overlayTopRight, x + image.Width - overlayTopRight.Width + overlayOverflow, y - overlayOverflow);
				}
			}
		}
	}

}
