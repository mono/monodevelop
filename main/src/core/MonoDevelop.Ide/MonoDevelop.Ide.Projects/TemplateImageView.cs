//
// TemplateImageView.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc.
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

using Gdk;
using MonoDevelop.Components;
using System;

namespace MonoDevelop.Ide.Projects
{
	internal class TemplateImageView : ImageView
	{
		public TemplateImageView ()
		{
		}

		public TemplateImageView (Xwt.Drawing.Image image)
			: base (image)
		{
		}

		double IconScale {
			get { return Mono.TextEditor.GtkWorkarounds.GetPixelScale (); }
		}

		/// <summary>
		/// Ensure image x, y positions are rounded to whole numbers to prevent the image from
		/// being blurry
		/// </summary>
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (Image != null) {
				using (var ctx = CairoHelper.Create (evnt.Window)) {
					var x = Math.Round (Allocation.X + (Allocation.Width - Image.Width * IconScale) * Xalign);
					var y = Math.Round (Allocation.Y + (Allocation.Height - Image.Height * IconScale) * Yalign);
					ctx.Save ();
					ctx.Scale (IconScale, IconScale);
					ctx.DrawImage (this, Image, x / IconScale, y / IconScale);
					ctx.Restore ();
				}
			}
			return true;
		}
	}
}
