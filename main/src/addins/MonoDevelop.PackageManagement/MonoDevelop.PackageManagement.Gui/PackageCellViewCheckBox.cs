//
// PackageCellViewCheckBox.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.PackageManagement
{
	public class PackageCellViewCheckBox
	{
		static int indicatorSize;
		static int indicatorSpacing;
		double scaleFactor;

		static PackageCellViewCheckBox ()
		{
			var cb = new Gtk.CheckButton ();
			indicatorSize = (int) cb.StyleGetProperty ("indicator-size");
			indicatorSpacing = (int) cb.StyleGetProperty ("indicator-spacing");
		}

		public PackageCellViewCheckBox (double scaleFactor)
		{
			this.scaleFactor = scaleFactor;
			Size = indicatorSize;
			BackgroundColor = Xwt.Drawing.Colors.White;
		}

		public int Size { get; set; }
		public bool Active { get; set; }
		public Widget Container { get; set; }
		public Xwt.Drawing.Color BackgroundColor { get; set; }

		public Xwt.Drawing.Image CreateImage ()
		{
			var bounds = new Gdk.Rectangle (0, 0, (int)(Size * scaleFactor), (int)(Size * scaleFactor));
			return CreatePixBuf (bounds).ToXwtImage ().WithSize (Size, Size);
		}

		Gdk.Pixbuf CreatePixBuf (Gdk.Rectangle bounds)
		{
			using (var pmap = new Gdk.Pixmap (Container.GdkWindow, bounds.Width, bounds.Height)) {
				using (Cairo.Context ctx = Gdk.CairoHelper.Create (pmap)) {
					ctx.Rectangle (0, 0, bounds.Width, bounds.Height);
					ctx.SetSourceRGBA (BackgroundColor.Red, BackgroundColor.Green, BackgroundColor.Blue, BackgroundColor.Alpha);
					ctx.Paint ();

					Render (pmap, bounds, Gtk.StateType.Normal);
					return Gdk.Pixbuf.FromDrawable (pmap, pmap.Colormap, 0, 0, 0, 0, bounds.Width, bounds.Height);
				}
			}
		}

		void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			Gtk.ShadowType sh = (bool) Active ? Gtk.ShadowType.In : Gtk.ShadowType.Out;
			int s = (int)(scaleFactor * Size) - 1;
			if (s > bounds.Height)
				s = bounds.Height;
			if (s > bounds.Width)
				s = bounds.Width;

			Gtk.Style.PaintCheck (Container.Style, window, state, sh, bounds, Container, "checkbutton", bounds.X + indicatorSpacing - 1, bounds.Y + (bounds.Height - s)/2, s, s);
		}
	}
}

