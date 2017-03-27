//
// CellRendererImage.cs
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
using Xwt.Drawing;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components
{
	public class CellRendererImage: Gtk.CellRenderer
	{
		Image image;
		Image imageOpen;
		Image imageClosed;
		IconId icon;
		Gtk.IconSize stockSize = Gtk.IconSize.Menu;

		/// <summary>
		/// Image to be used to represent "no image". This is necessary since GLib.Value can't hold
		/// null values for object that are not of subclasses of GLib.Object
		/// </summary>
		public static readonly Xwt.Drawing.Image NullImage = ImageService.GetIcon ("md-empty", Gtk.IconSize.Menu);

		public CellRendererImage ()
		{
		}

		[GLib.Property ("stock-size")]
		int StockSizeInt {
			get {
				return (int)stockSize;
			}
			set {
				stockSize = (Gtk.IconSize) value;
			}
		}

		public Gtk.IconSize StockSize {
			get {
				return stockSize;
			}
			set {
				stockSize = value;
			}
		}

		[GLib.Property ("icon-id")]
		public IconId IconId {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}

		[GLib.Property ("stock_id")]
		string StockIdInt {
			get {
				return StockId;
			}
			set {
				StockId = value;
			}
		}

		[GLib.Property ("stock-id")]
		public string StockId {
			get {
				return icon;
			}
			set {
				icon = value;
			}
		}

		[GLib.Property ("image")]
		public virtual Image Image {
			get {
				return image;
			}
			set {
				image = value;
			}
		}
		
		[GLib.Property ("image-expander-open")]
		public virtual Image ImageExpanderOpen {
			get {
				return imageOpen;
			}
			set {
				imageOpen = value;
			}
		}

		[GLib.Property ("image-expander-closed")]
		public virtual Image ImageExpanderClosed {
			get {
				return imageClosed;
			}
			set {
				imageClosed = value;
			}
		}

		protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
		{
			var img = GetImage ();
			if (img == null)
				return;

			if ((flags & Gtk.CellRendererState.Selected) != 0)
				img = img.WithStyles ("sel");
			if (!img.HasFixedSize)
				img = img.WithSize (Gtk.IconSize.Menu);
			
			using (var ctx = Gdk.CairoHelper.Create (window)) {
				var x = cell_area.X + cell_area.Width / 2 - (int)(img.Width / 2);
				var y = cell_area.Y + cell_area.Height / 2 - (int)(img.Height / 2);
				ctx.DrawImage (widget, img, x, y);
			}
		}

		protected void GetImageInfo (Gdk.Rectangle cell_area, out Image img, out int x, out int y)
		{
			img = GetImage ();
			if (img == null) {
				x = (int)(cell_area.X + cell_area.Width / 2);
				y = (int)(cell_area.Y + cell_area.Height / 2);
			} else {
				x = (int)(cell_area.X + cell_area.Width / 2 - (int)(img.Width / 2));
				y = (int)(cell_area.Y + cell_area.Height / 2 - (int)(img.Height / 2));
			}
		}

		public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			var img = GetImage ();
			if (img != null) {
				if (img.HasFixedSize) {
					width = (int)img.Width;
					height = (int)img.Height;
				} else
					Gtk.IconSize.Menu.GetSize(out width, out height);
			} else
				width = height = 0;

			width += (int)Xpad * 2;
			height += (int)Ypad * 2;
			x_offset = (int)(cell_area.Width / 2 - (width / 2));
			y_offset = (int)(cell_area.Height / 2 - (height / 2));
		}

		Image GetImage ()
		{
			Image img;
			if (icon.IsNull)
				img = IsExpanded ? (imageOpen ?? image) : (imageClosed ?? image);
			else
				img = ImageService.GetIcon (icon, stockSize);

			return img != NullImage ? img : null;
		}
	}
}

