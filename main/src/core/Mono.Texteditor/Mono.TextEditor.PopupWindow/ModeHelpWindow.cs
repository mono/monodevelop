// 
// ModeHelpWindow.cs
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
using System.Collections.Generic;
namespace Mono.TextEditor.PopupWindow
{
	public class ModeHelpWindow : Gtk.Window
	{
		public string TitleText {
			get;
			set;
		}
		
		public List<KeyValuePair<string, string>> Items {
			get;
			set;
		}
		
		Pango.Layout layout;
		
		public ModeHelpWindow () :  base (Gtk.WindowType.Popup)
		{
			this.SkipPagerHint = this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 0;
//			this.TypeHint = Gdk.WindowTypeHint.Normal;
			this.AllowShrink = this.AllowGrow = false;
			this.DestroyWithParent = true;
			
			layout = new Pango.Layout (PangoContext);
			Items = new List<KeyValuePair<string, string>> ();
			
			var rgbaColormap = Screen.RgbaColormap;
			if (rgbaColormap != null)
				Colormap = rgbaColormap;
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			int totalWidth = 1;
			int totalHeight = yBorder * 2 + 1;
			
			int width, height;
			layout.SetText (TitleText);
			layout.GetPixelSize (out width, out height);
			totalHeight += height;
			xSpacer = 0;
			foreach (var pair in Items) {
				int w1, w2;
				layout.SetMarkup (pair.Key);
				layout.GetPixelSize (out w1, out height);

				layout.SetMarkup (pair.Value);
				layout.GetPixelSize (out w2, out height);
				totalWidth = System.Math.Max (totalWidth, w1 + w2 + xBorder * 4 + 1);
				xSpacer = System.Math.Max (xSpacer, w1 + xBorder * 2 + 1);
				
				totalHeight += height;
			}
			
			requisition.Width = totalWidth;
			requisition.Height = totalHeight;
		}
		
		int xSpacer = 0;
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		
		const int xBorder = 4;
		const int yBorder = 2;
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			using (var g = Gdk.CairoHelper.Create (args.Window)) {
				g.SetSourceRGBA (1, 1, 1, 0);
				g.Operator = Cairo.Operator.Source;
				g.Paint ();
			}
			Cairo.Color bgColor = new Cairo.Color (1, 1, 1);
			Cairo.Color titleBgColor = new Cairo.Color (0.88, 0.88, 0.98);
			Cairo.Color categoryBgColor = new Cairo.Color (0.58, 0.58, 0.98);
			Cairo.Color borderColor = new Cairo.Color (0.4, 0.4, 0.6);
			Cairo.Color textColor = new Cairo.Color (0.3, 0.3, 1);
			Cairo.Color gridColor = new Cairo.Color (0.8, 0.8, 0.8);
			
			using (var g = Gdk.CairoHelper.Create (args.Window)) {
				g.LineWidth = 1;
				
				Gdk.GC gc = new Gdk.GC (args.Window);
				layout.SetMarkup (TitleText);
				int width, height;
				layout.GetPixelSize (out width, out height);
				width += xBorder * 2;
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, true, false, 0.5, 0.5, height + yBorder * 2 + 1.5, width, height + yBorder * 2);
				g.Color = titleBgColor;
				g.FillPreserve ();
				g.Color = borderColor;
				g.Stroke ();
				gc.RgbFgColor = (HslColor)textColor;
				args.Window.DrawLayout (gc, xBorder, yBorder, layout);
				
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, false, true, 0.5, height * 2 + yBorder * 2 + 0.5, height, Allocation.Width - 1, Allocation.Height - height * 2 - yBorder * 2 - 1);
				g.Color = bgColor;
				g.FillPreserve ();
				g.Color = borderColor;
				g.Stroke ();
				
				
				
				g.MoveTo (xSpacer + 0.5, height * 2 + yBorder * 2);
				g.LineTo (xSpacer + 0.5, Allocation.Height - 1);
				g.Color = gridColor;
				g.Stroke ();
				
				int y = height + yBorder * 2;
				
				for (int i = 0; i < Items.Count; i++) {
					KeyValuePair<string, string> pair = Items[i];
					
					layout.SetMarkup (pair.Key);
					layout.GetPixelSize (out width, out height);
					
					if (i == 0) {
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, false, true, false, false, 0, y + 0.5, height + 1.5, Allocation.Width, height);
						g.Color = categoryBgColor;
						g.FillPreserve ();
						g.Color = borderColor;
						g.Stroke ();
						
						g.MoveTo (xSpacer + 0.5, height + yBorder * 2 + 1);
						g.LineTo (xSpacer + 0.5, height * 2 + yBorder * 2 + 1);
						g.Color = gridColor;
						g.Stroke ();
					}
					
					gc.RgbFgColor = (HslColor)(i == 0 ? bgColor : textColor);
						
					args.Window.DrawLayout (gc, xBorder, y, layout);
					layout.SetMarkup (pair.Value);
					args.Window.DrawLayout (gc, xSpacer + xBorder, y, layout);
					
					// draw top line
					if (i > 0) {
						g.MoveTo (1, y + 0.5);
						g.LineTo (Allocation.Width - 1, y + 0.5);
						g.Color = gridColor;
						g.Stroke ();
					}
					y += height;
				}
				gc.Dispose ();
			}
			return false;
		}
	}
}
