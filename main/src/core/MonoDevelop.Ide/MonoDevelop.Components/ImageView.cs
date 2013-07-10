//
// ImageView.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using Gdk;

namespace MonoDevelop.Components
{
	public class ImageView: Gtk.DrawingArea
	{
		Xwt.Drawing.Image image;

		public ImageView ()
		{
			AppPaintable = true;
		}

		public ImageView (Xwt.Drawing.Image image): this ()
		{
			this.image = image;
		}

		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				QueueDraw ();
			}
		}

		float xalign = 0.5f;
		public float Xalign {
			get { return xalign; }
			set {
				xalign = value;
				QueueDraw ();
			}
		}

		float yalign = 0.5f;
		public float Yalign {
			get { return yalign; }
			set {
 				yalign = value;
				QueueDraw ();
			}
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = (int)image.Width;
			requisition.Height = (int)image.Height;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (image != null) {
				using (var ctx = CairoHelper.Create (evnt.Window)) {
					var x = Allocation.X + (Allocation.Width - image.Width) * xalign;
					var y = Allocation.Y + (Allocation.Height - image.Height) * yalign;
					ctx.DrawImage (this, image, x, y);
				}
			}
			return true;
		}
	}
}

