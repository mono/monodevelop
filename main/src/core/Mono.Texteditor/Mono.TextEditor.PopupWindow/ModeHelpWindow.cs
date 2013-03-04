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
	public abstract class ModeHelpWindow : Gtk.Window
	{
		public string TitleText {
			get;
			set;
		}
		
		public List<KeyValuePair<string, string>> Items {
			get;
			set;
		}

		public bool SupportsAlpha {
			get;
			private set;
		}

		public ModeHelpWindow () : base (Gtk.WindowType.Popup)
		{
			this.SkipPagerHint = this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 0;
//			this.TypeHint = Gdk.WindowTypeHint.Normal;
			this.AllowShrink = this.AllowGrow = false;
			this.DestroyWithParent = true;
			
			Items = new List<KeyValuePair<string, string>> ();
			CheckScreenColormap ();
		}

		void CheckScreenColormap ()
		{
			SupportsAlpha = Screen.IsComposited;
			if (SupportsAlpha) {
				Colormap = Screen.RgbaColormap;
			} else {
				Colormap = Screen.RgbColormap;
			}
		}
		
		protected override void OnScreenChanged (Gdk.Screen previous_screen)
		{
			base.OnScreenChanged (previous_screen);
			CheckScreenColormap ();
		}

	}

	public class TableLayoutModeHelpWindow : ModeHelpWindow
	{
		Pango.Layout layout;
		
		public TableLayoutModeHelpWindow ()
		{
			layout = new Pango.Layout (PangoContext);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			int descriptionWidth = 1;
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
				descriptionWidth = System.Math.Max (descriptionWidth, w2);
				xSpacer = System.Math.Max (xSpacer, w1);
				
				totalHeight += height;
			}
			xSpacer += xBorder * 2 + 1;
			
			requisition.Width = descriptionWidth + xSpacer + xBorder * 2 + 1;
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

			GtkWorkarounds.UpdateNativeShadow (this);
			return false;
		}
	}

	public class InsertionCursorLayoutModeHelpWindow : ModeHelpWindow
	{
		Pango.Layout titleLayout;
		Pango.Layout descriptionLayout;

		public InsertionCursorLayoutModeHelpWindow ()
		{
			titleLayout = new Pango.Layout (PangoContext);
			descriptionLayout = new Pango.Layout (PangoContext);
			descriptionLayout.SetMarkup ("<small>Use Up/Down to move to another location.\nPress Enter to select the location\nPress Esc to cancel this operation</small>");
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			int descriptionWidth = 1;
			int totalHeight = yTitleBorder * 2 + yDescriptionBorder * 2 + 1;
			
			int width, height;
			titleLayout.SetText (TitleText);
			titleLayout.GetPixelSize (out width, out height);
			totalHeight += height;
			xSpacer = 0;

			int h2;
			int w2;
			descriptionLayout.GetPixelSize (out w2, out h2);
			totalHeight += h2;
			xSpacer = System.Math.Max (width, w2);

			xSpacer += xDescriptionBorder * 2 + 1;
			
			requisition.Width = triangleWidth + descriptionWidth + xSpacer;
			requisition.Height = totalHeight;
		}
		
		int xSpacer = 0;
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			if (titleLayout != null) {
				titleLayout.Dispose ();
				titleLayout = null;
			}

			if (descriptionLayout != null) {
				descriptionLayout.Dispose ();
				descriptionLayout = null;
			}
		}
		
		const int triangleHeight = 16;
		const int triangleWidth = 8;

		const int xDescriptionBorder = 6;
		const int yDescriptionBorder = 6;
		const int yTitleBorder = 2;
		static readonly Cairo.Color bgColor = HslColor.Parse ("#ffe97f");
		static readonly Cairo.Color titleBgColor = HslColor.Parse ("#cfb94f");
		static readonly Cairo.Color titleTextColor = HslColor.Parse ("#000000");
		static readonly Cairo.Color borderColor = HslColor.Parse ("#7f6a00");
		static readonly Cairo.Color textColor = HslColor.Parse ("#555753");

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			if (SupportsAlpha) {
				using (var g = Gdk.CairoHelper.Create (args.Window)) {
					g.SetSourceRGBA (1, 1, 1, 0);
					g.Operator = Cairo.Operator.Source;
					g.Paint ();
				}	
			}
			using (var g = Gdk.CairoHelper.Create (args.Window)) {
				g.LineWidth = 1.5;
				titleLayout.SetMarkup (TitleText);
				int width, height;
				titleLayout.GetPixelSize (out width, out height);
				var tw = SupportsAlpha ? triangleWidth : 0;
				var th = SupportsAlpha ? triangleHeight : 0;
				width += xDescriptionBorder * 2;
				if (SupportsAlpha) {
					FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, true, false, tw + 0.5, 0.5, height + yTitleBorder * 2 + 1.5, Allocation.Width - 1 - tw, height + yTitleBorder * 2);
				} else {
					g.Rectangle (0, 0, Allocation.Width, height + yTitleBorder * 2);
				}
				g.Color = titleBgColor;
				g.FillPreserve ();
				g.Color = borderColor;
				g.Stroke ();
				

				g.MoveTo (tw + xDescriptionBorder, yTitleBorder);
				g.Color = titleTextColor;
				g.ShowLayout (titleLayout);

				if (SupportsAlpha) {
					FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, false, true, tw + 0.5, height + yTitleBorder * 2 + 0.5, height, Allocation.Width - 1 - tw, Allocation.Height - height - yTitleBorder * 2 - 1);
				} else {
					g.Rectangle (0, height + yTitleBorder * 2, Allocation.Width, Allocation.Height - height - yTitleBorder * 2);
				}
				g.Color = bgColor;
				g.FillPreserve ();
				g.Color = borderColor;
				g.Stroke ();

				if (SupportsAlpha) {

					g.MoveTo (tw, Allocation.Height / 2 - th / 2);
					g.LineTo (0, Allocation.Height / 2);
					g.LineTo (tw, Allocation.Height / 2 + th / 2);
					g.LineTo (tw + 5, Allocation.Height / 2 + th / 2);
					g.LineTo (tw + 5, Allocation.Height / 2 - th / 2);
					g.ClosePath ();
					g.Color = bgColor;
					g.Fill ();
					
					g.MoveTo (tw, Allocation.Height / 2 - th / 2);
					g.LineTo (0, Allocation.Height / 2);
					g.LineTo (tw, Allocation.Height / 2 + th / 2);
					g.Color = borderColor;
					g.Stroke ();
				}

				int y = height + yTitleBorder * 2 + yDescriptionBorder;
				g.MoveTo (tw + xDescriptionBorder, y);
				g.Color = textColor;
				g.ShowLayout (descriptionLayout);
			}
			return false;
		}
	}

}
