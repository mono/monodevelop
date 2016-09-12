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
	[System.ComponentModel.ToolboxItem (true)]
	public class ImageView: Gtk.Misc
	{
		Xwt.Drawing.Image image;
		string iconId;
		Gtk.IconSize? size;

		public ImageView ()
		{
			Accessible.Role = Atk.Role.Image;
			WidgetFlags |= Gtk.WidgetFlags.AppPaintable | Gtk.WidgetFlags.NoWindow;
		}

		public ImageView (Xwt.Drawing.Image image): this ()
		{
			this.image = image;
		}

		public ImageView (string iconId, Gtk.IconSize size): this ()
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

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			requisition.Width = Xpad * 2;
			requisition.Height = Ypad * 2;
			if (image != null) {
				requisition.Width += (int)(image.Width);
				requisition.Height += (int)(image.Height);
			}
		}

		bool IsParentDisabled ()
		{
			var parent = Parent;
			if (parent != null) {
				if (!parent.Sensitive)
					return true;
				// special case: Buttons with image and label align children with HBox and Alignment
				//               Button -> Alignment -> HBox -> [ImageView|Label]
				parent = parent.Parent.Parent as Gtk.Button;
				if (parent != null && !parent.Sensitive)
					return true;
			}
			return false;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (image != null) {
				var alloc = Allocation;
				alloc.Inflate (-Xpad, -Ypad);
				using (var ctx = CairoHelper.Create (evnt.Window)) {
					var x = Math.Round (alloc.X + (alloc.Width - image.Width) * Xalign);
					var y = Math.Round (alloc.Y + (alloc.Height - image.Height) * Yalign);
					ctx.Save ();
					ctx.DrawImage (this, IsParentDisabled () ? image.WithAlpha (0.4) : image, x, y);
					ctx.Restore ();
				}
			}
			return true;
		}
	}
}

