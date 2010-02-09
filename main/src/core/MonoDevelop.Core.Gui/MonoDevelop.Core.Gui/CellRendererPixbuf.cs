// 
// CellRendererPixbuf.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
namespace MonoDevelop.Core.Gui
{
	/// <summary>
	/// Replaces the Gtk.CellRendererPixbuf with a version that loads it's stock icons from the ImageService to
	/// support lazy loading of images.
	/// </summary>
	public class CellRendererPixbuf : CellRenderer
	{
		public CellRendererPixbuf ()
		{
			StockSize = (uint)Gtk.IconSize.Menu;
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			x_offset = 0;
			y_offset = 0;
			
			Pixbuf pixbuf = GetPixbuf ();
			
			if (pixbuf == null) {
				width = height = 0;
			} else {
				width = pixbuf.Width;
				height = pixbuf.Height;
			}
		}
		
		protected override void Render (Drawable window, Widget widget, Rectangle background_area, Rectangle cell_area, Rectangle expose_area, CellRendererState flags)
		{
			Gdk.GC gc = widget.Style.BaseGC (flags == CellRendererState.Selected ? StateType.Selected : StateType.Normal);
			Pixbuf pixbuf = GetPixbuf ();
			
			if (pixbuf != null)
				window.DrawPixbuf (gc, pixbuf, 0, 0, expose_area.X, expose_area.Y, pixbuf.Width, pixbuf.Height, RgbDither.None, 0, 0);
		}
		
		Pixbuf GetPixbuf ()
		{
			if (base.IsExpanded) {
				if (PixbufExpanderOpen != null)
					return PixbufExpanderOpen;
			} else {
				if (PixbufExpanderClosed != null)
					return PixbufExpanderClosed;
			}
			if (Pixbuf != null)
				return Pixbuf;
			
			if (!string.IsNullOrEmpty (StockId))
				return ImageService.GetPixbuf (StockId, (IconSize)StockSize);
			
			return null;
		}
		
		[GLib.Property ("pixbuf-expander-open")]
		public Gdk.Pixbuf PixbufExpanderOpen {
			get;
			set;
		}

		[GLib.Property ("pixbuf")]
		public Gdk.Pixbuf Pixbuf {
			get;
			set;
		}

		[GLib.Property ("stock-id")]
		public string StockId {
			get;
			set;
		}

		[GLib.Property ("icon-name")]
		public string IconName {
			get;
			set;
		}

		[GLib.Property ("pixbuf-expander-closed")]
		public Gdk.Pixbuf PixbufExpanderClosed {
			get;
			set;
		}

		[GLib.Property ("stock-detail")]
		public string StockDetail {
			get;
			set;
		}

		[GLib.Property ("follow-state")]
		public bool FollowState {
			get;
			set;
		}

		[GLib.Property ("stock-size")]
		public uint StockSize {
			get;
			set;
		}
		
	}
}

