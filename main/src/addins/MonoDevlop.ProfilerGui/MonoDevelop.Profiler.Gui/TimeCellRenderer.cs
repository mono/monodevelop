// 
// TimeCellRenderer.cs
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
using Gtk;
using Cairo;
using Mono.TextEditor;

namespace MonoDevelop.Profiler
{
	class TimeCellRenderer : CellRenderer
	{
		[GLib.Property ("time")]
		public double Time {
			get;
			set;
		}
		
		public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			x_offset = y_offset = 0;
			width = cell_area.Width;
			height = cell_area.Height;
		}

		protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			using (Cairo.Context gr = Gdk.CairoHelper.Create (window)) {
				gr.Rectangle (cell_area.X, cell_area.Y, cell_area.Width, cell_area.Height);
				gr.Color = (HslColor)widget.Style.Base ((flags & CellRendererState.Selected) == CellRendererState.Selected ? StateType.Selected : StateType.Normal);
				gr.Fill ();
				var size = Math.Max (0, cell_area.Width - cell_area.Width * Time / 100.0);
				var linearGradient = new LinearGradient (cell_area.X, cell_area.Y, cell_area.Right, cell_area.Bottom);
				linearGradient.AddColorStop (0, new Cairo.Color (1, 0, 0));
				linearGradient.AddColorStop (1, new Cairo.Color (1, 1, 1));
				gr.Pattern = linearGradient;
				
				gr.Rectangle (cell_area.X + size, cell_area.Y + 2, cell_area.Width - size, cell_area.Height - 4);
				gr.Fill ();
				
				var layout = gr.CreateLayout ();
				layout.FontDescription = widget.PangoContext.FontDescription;
				layout.SetText (string.Format ("{0:0.0}", Time));
				int w, h;
				layout.GetPixelSize (out w, out h);
				gr.MoveTo (cell_area.X + cell_area.Width - 2 - w, cell_area.Y + (cell_area.Height - h) / 2);
				gr.Color = new Cairo.Color (0, 0, 0);
				gr.ShowLayout (layout);
				layout.Dispose ();
			}
		}
	}
}

