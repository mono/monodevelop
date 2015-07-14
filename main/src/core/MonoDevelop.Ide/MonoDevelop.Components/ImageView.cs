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
		string iconId;
		Gtk.IconSize? size;
		int xpad, ypad;

		public ImageView ()
		{
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable | Gtk.WidgetFlags.NoWindow;
		}

		public ImageView (Xwt.Drawing.Image image): this ()
		{
			this.image = image;
		}

		public ImageView (string iconId, Gtk.IconSize size)
		{
			this.iconId = iconId;
			this.size = size;
			image = MonoDevelop.Ide.ImageService.GetIcon (iconId, size);
		}

		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				image = value;
				QueueDraw ();
				QueueResize ();
			}
		}

		public void SetIcon (string iconId, Gtk.IconSize size)
		{
			this.iconId = iconId;
			this.size = size;
			Image = MonoDevelop.Ide.ImageService.GetIcon (iconId, size);
		}

		public Gtk.IconSize IconSize {
			get {
				return size.HasValue ? size.Value : Gtk.IconSize.Invalid;
			}
			set {
				size = value;
				if (iconId != null)
					Image = MonoDevelop.Ide.ImageService.GetIcon (iconId, size.Value);
			}
		}

		public string IconId {
			get {
				return iconId;
			}
			set {
				iconId = value;
				if (size.HasValue)
					Image = MonoDevelop.Ide.ImageService.GetIcon (iconId, size.Value);
			}
		}

		public int Xpad {
			get {
				return xpad;
			}
			set {
				xpad = value;
				QueueResize ();
			}
		}

		public int Ypad {
			get {
				return ypad;
			}
			set {
				ypad = value;
				QueueResize ();
			}
		}

		public void SetAlignment (float xalign, float yalign)
		{
			Xalign = xalign;
			Yalign = yalign;
			QueueDraw ();
		}

		float xalign = 0.5f;
		public float Xalign {
			get { return xalign; }
			set {
				xalign = (float)(value * IconScale);
				QueueDraw ();
			}
		}

		float yalign = 0.5f;
		public float Yalign {
			get { return yalign; }
			set {
				yalign = (float)(value * IconScale);
				QueueDraw ();
			}
		}

		double IconScale {
			get { return GtkWorkarounds.GetPixelScale (); }
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = xpad * 2;
			requisition.Height = ypad * 2;
			if (image != null) {
				requisition.Width = (int)(image.Width * IconScale);
				requisition.Height = (int)(image.Height * IconScale);
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (image != null) {
				var alloc = Allocation;
				alloc.Inflate (-xpad, -ypad);
				using (var ctx = CairoHelper.Create (evnt.Window)) {
					var x = Math.Round (alloc.X + (alloc.Width - image.Width * IconScale) * Xalign);
					var y = Math.Round (alloc.Y + (alloc.Height - image.Height * IconScale) * Yalign);
					ctx.Save ();
					ctx.Scale (IconScale, IconScale);
					ctx.DrawImage (this, image, x / IconScale, y / IconScale);
					ctx.Restore ();
				}
			}
			return true;
		}
	}
}

