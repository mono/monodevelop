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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Unix;
using MonoDevelop.Components;

namespace Mono.TextEditor.PopupWindow
{
	abstract class ModeHelpWindow : Gtk.EventBox
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

		public ModeHelpWindow ()
		{
			VisibleWindow = false;
			Items = new List<KeyValuePair<string, string>> ();
			CheckScreenColormap ();
		}

		void CheckScreenColormap ()
		{
			Colormap = Screen.RgbaColormap;
			if (Colormap == null) {
				Colormap = Screen.RgbColormap;
				SupportsAlpha = false;
			} else
				SupportsAlpha = true;
		}
		
		protected override void OnScreenChanged (Gdk.Screen previous_screen)
		{
			base.OnScreenChanged (previous_screen);
			CheckScreenColormap ();
		}

	}

	class TableLayoutModeHelpWindow : ModeHelpWindow
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
				g.Translate (Allocation.X, Allocation.Y);
				g.LineWidth = 1;
				
				Gdk.GC gc = new Gdk.GC (args.Window);
				layout.SetMarkup (TitleText);
				int width, height;
				layout.GetPixelSize (out width, out height);
				width += xBorder * 2;
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, true, false, 0.5, 0.5, height + yBorder * 2 + 1.5, width, height + yBorder * 2);
				g.SetSourceColor (Styles.TableLayoutModeTitleBackgroundColor.ToCairoColor ());
				g.FillPreserve ();
				g.SetSourceColor (Styles.TableLayoutModeBorderColor.ToCairoColor ());
				g.Stroke ();

				g.Save ();
				g.SetSourceColor (Styles.TableLayoutModeTextColor.ToCairoColor ());
				g.Translate (xBorder, yBorder);
				g.ShowLayout (layout);
				g.Restore ();

				FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, false, true, 0.5, height * 2 + yBorder * 2 + 0.5, height, Allocation.Width - 1, Allocation.Height - height * 2 - yBorder * 2 - 1);
				g.SetSourceColor (Styles.TableLayoutModeBackgroundColor.ToCairoColor ());
				g.FillPreserve ();
				g.SetSourceColor (Styles.TableLayoutModeBorderColor.ToCairoColor ());
				g.Stroke ();
				
				g.MoveTo (xSpacer + 0.5, height * 2 + yBorder * 2);
				g.LineTo (xSpacer + 0.5, Allocation.Height - 1);
				g.SetSourceColor (Styles.TableLayoutModeGridColor.ToCairoColor ());
				g.Stroke ();
				
				int y = height + yBorder * 2;
				
				for (int i = 0; i < Items.Count; i++) {
					KeyValuePair<string, string> pair = Items[i];
					
					layout.SetMarkup (pair.Key);
					layout.GetPixelSize (out width, out height);
					
					if (i == 0) {
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, false, true, false, false, 0, y + 0.5, height + 1.5, Allocation.Width, height);
						g.SetSourceColor (Styles.TableLayoutModeCategoryBackgroundColor.ToCairoColor ());
						g.FillPreserve ();
						g.SetSourceColor (Styles.TableLayoutModeBorderColor.ToCairoColor ());
						g.Stroke ();
						
						g.MoveTo (xSpacer + 0.5, height + yBorder * 2 + 1);
						g.LineTo (xSpacer + 0.5, height * 2 + yBorder * 2 + 1);
						g.SetSourceColor (Styles.TableLayoutModeGridColor.ToCairoColor ());
						g.Stroke ();
					}
					
					gc.RgbFgColor = (HslColor)(i == 0 ? Styles.TableLayoutModeBackgroundColor : Styles.TableLayoutModeTextColor).ToCairoColor ();
					g.Save ();
					g.SetSourceColor (Styles.TableLayoutModeTextColor.ToCairoColor ());
					g.Translate (xBorder, y);
					g.ShowLayout (layout);
					g.Restore ();

					g.Save ();
					g.SetSourceColor (Styles.TableLayoutModeTextColor.ToCairoColor ());
					g.Translate (xSpacer + xBorder, y);
					layout.SetMarkup (pair.Value);
					g.ShowLayout (layout);
					g.Restore ();

					// draw top line
					if (i > 0) {
						g.MoveTo (1, y + 0.5);
						g.LineTo (Allocation.Width - 1, y + 0.5);
						g.SetSourceColor (Styles.TableLayoutModeGridColor.ToCairoColor ());
						g.Stroke ();
					}
					y += height;
				}
				gc.Dispose ();
			}

			return base.OnExposeEvent (args);
		}
	}

	enum SymbolTokenType {
		Up,
		Down,
		None
	}

	class TokenRenderer : IDisposable
	{
		const int normalFontSize = 11;
		const int outlinedFontSize = 8;
		const int outlinePadding = 1;
		const int textInnerPadding = 1;

		Pango.Layout layout;
		SymbolTokenType Symbol;

		public int Width { get; private set; }
		public int Height { get; private set; }
		public bool Outlined { get; private set; }
		public int Spacing { get; private set; }

		public TokenRenderer (Pango.Context ctx, string str, bool outlined)
		{
			Outlined = outlined;
			Spacing = 6;

			if (str == "%UP%") {
				Symbol = SymbolTokenType.Up;
				Width = 14;
				Height = 10;
			} else if (str == "%DOWN%") {
				Symbol = SymbolTokenType.Down;
				Width = 14;
				Height = 10;
			} else {
				Symbol = SymbolTokenType.None;

				var desc = ctx.FontDescription.Copy ();
				desc.AbsoluteSize = Pango.Units.FromPixels (Outlined ? outlinedFontSize : normalFontSize);
				if (Outlined) {
					desc.Weight = Pango.Weight.Bold;
					Spacing += outlinePadding;
				}

				layout = new Pango.Layout (ctx);
				layout.FontDescription = desc;
				layout.Spacing = (int)(Spacing * Pango.Scale.PangoScale);
				layout.SetMarkup (str);

				int w, h;
				layout.GetPixelSize (out w, out h);
				Width = w;
				Height = h - 1;
			}
		}

		public void Render (Cairo.Context cr, double _x, double _y, int max_height)
		{
			double x = _x;
			double y = _y;
			int w = Width;
			int h = max_height;
			int inner_padding = 0;

			if (Outlined) {
				x -= outlinePadding;
				y -= outlinePadding;
				w += outlinePadding * 2;
				h += outlinePadding * 2;

				cr.MoveTo (x, y);
				cr.LineWidth = 1;
				cr.SetSourceColor (Styles.ModeHelpWindowTokenOutlineColor.ToCairoColor());

				if (Symbol == SymbolTokenType.None)
					inner_padding = textInnerPadding;

				// -0.5f to fix the @1x stroke problem:
				// 1px stroke is rendered on the center of the shape edge, resulting in
				// two semitransparent pixels. Even worse the rounded rectangle renders
				// transparency artifacts on edge overlaps. See http://vncr.in/atks
				FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, x - inner_padding - 0.5f, y - 0.5f, 8, w + inner_padding * 2 + 1, h + 1);

				cr.Stroke ();

				if (Symbol == SymbolTokenType.Down) {
					RenderTriangleDown (cr, x + 4, y + 3, 8, 6);
				} else if (Symbol == SymbolTokenType.Up) {
					RenderTriangleUp (cr, x + 4, y + 3, 8, 6);
				} else {
					cr.MoveTo (x + outlinePadding, y + (max_height - Height - 0.5));
					cr.ShowLayout (layout);
				}
			} else {
				cr.MoveTo (x, y);
				cr.SetSourceColor (Styles.ModeHelpWindowTokenTextColor.ToCairoColor());
				cr.ShowLayout (layout);
			}
		}

		void RenderTriangleDown (Cairo.Context cr, double x, double y, double w, double h)
		{
			cr.MoveTo (x + w / 2.0, y + h);
			cr.LineTo (x, y);
			cr.LineTo (x + w, y);
			cr.ClosePath ();
			cr.Fill ();
		}

		void RenderTriangleUp (Cairo.Context cr, double x, double y, double w, double h)
		{
			cr.MoveTo (x + w / 2.0, y);
			cr.LineTo (x, y + h);
			cr.LineTo (x + w, y + h);
			cr.ClosePath ();
			cr.Fill ();
		}

		public void Dispose ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
	}

	class LineRenderer : IDisposable
	{
		TokenRenderer[] tokens;

		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Spacing { get; private set; }
		static Regex rx = new Regex (@"^\[.+\]$", RegexOptions.Compiled);

		public LineRenderer (Pango.Context ctx, string str)
		{
			var pieces = str.Split (null as string[], StringSplitOptions.RemoveEmptyEntries);
			List<TokenRenderer> line = new List<TokenRenderer> ();
			var currentLine = "";

			var desc = ctx.FontDescription.Copy ();
			desc.AbsoluteSize = Pango.Units.FromPixels (14);
			desc.Weight = Pango.Weight.Bold;
			var layout = new Pango.Layout (ctx);
			layout.SetMarkup (" ");
			int w, h;
			layout.GetPixelSize (out w, out h);
			Spacing = w;

			foreach (var token in pieces) {
				if (rx.IsMatch (token)) {
					if (!String.IsNullOrEmpty (currentLine))
						line.Add (new TokenRenderer (ctx, currentLine, false));
					line.Add (new TokenRenderer (ctx, token.Substring (1, token.Length - 2), true));
					currentLine = "";
				} else {
					if (!String.IsNullOrEmpty (currentLine))
						currentLine += " ";
					currentLine += token;
				}
			}

			if (!String.IsNullOrEmpty (currentLine))
				line.Add (new TokenRenderer (ctx, currentLine, false));

			tokens = line.ToArray ();

			Width = tokens.Sum (t => t.Width + t.Spacing);
			Height = tokens.Max (t => t.Height);
		}

		public void Render (Cairo.Context cr, double x, double y)
		{
			foreach (var token in tokens) {
				token.Render (cr, x, y, Height);
				x += token.Width + token.Spacing;
			}
		}

		public void Dispose ()
		{
			foreach (var token in tokens) {
				token.Dispose ();
			}
		}
	}

	class InsertionCursorLayoutModeHelpWindow : ModeHelpWindow
	{
		Pango.Layout titleLayout;

		LineRenderer[] descTexts;

		public InsertionCursorLayoutModeHelpWindow ()
		{
			// %UP% and %DOWN% should not be translated, those will be rendered as up- or down-facing triangles.
			// Words surrounded by brackets will be rendered with a rounded-rectangle outline.
			descTexts = new LineRenderer[] {
				new LineRenderer (PangoContext, Catalog.GetString ("Use [%UP%] [%DOWN%] to move to another location.")),
				new LineRenderer (PangoContext, Catalog.GetString ("Press [ENTER] to select the location.")),
				new LineRenderer (PangoContext, Catalog.GetString ("Press [ESC] to cancel this operation."))
			};

			titleLayout = new Pango.Layout (PangoContext);
			var desc = PangoContext.FontDescription.Copy ();
			desc.AbsoluteSize = Pango.Units.FromPixels (12);
			desc.Weight = Pango.Weight.Bold;
			titleLayout.FontDescription = desc;
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

			int h2 = descTexts.Sum (x => x.Height + x.Spacing);
			int w2 = descTexts.Max (x => x.Width + x.Spacing * 2);
			totalHeight += h2 + 4;
			xSpacer = System.Math.Max (width, w2);

			xSpacer += xDescriptionBorder * 2 + 1;
			
			requisition.Width = triangleWidth + descriptionWidth + xSpacer;
			requisition.Height = totalHeight;
		}
		
		int xSpacer = 0;
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			if (descTexts != null) {
				foreach (var item in descTexts) {
					item.Dispose ();
				}
				descTexts = null;
			}
			
			if (titleLayout != null) {
				titleLayout.Dispose ();
				titleLayout = null;
			}
		}
		
		const int triangleHeight = 16;
		const int triangleWidth = 8;

		const int xDescriptionBorder = 12;
		const int yDescriptionBorder = 8;
		const int yTitleBorder = 8;

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			using (var g = Gdk.CairoHelper.Create (args.Window)) {
				g.Translate (Allocation.X, Allocation.Y);
				g.LineWidth = 1;
				titleLayout.SetMarkup (TitleText);
				int width, height;
				titleLayout.GetPixelSize (out width, out height);
				var tw = SupportsAlpha ? triangleWidth : 0;
				var th = SupportsAlpha ? triangleHeight : 0;
				width += xDescriptionBorder * 2;

				if (SupportsAlpha) {
					FoldingScreenbackgroundRenderer.DrawRoundRectangle (g, true, true, tw + 0.5, 0.5, 12, Allocation.Width - 1 - tw, Allocation.Height);
				} else {
					g.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				}
				g.SetSourceColor (Styles.InsertionCursorBackgroundColor.ToCairoColor ());
				g.FillPreserve ();
				g.SetSourceColor (Styles.InsertionCursorBorderColor.ToCairoColor ());
				g.Stroke ();
				

				g.MoveTo (tw + xDescriptionBorder, yTitleBorder);
				g.SetSourceColor (Styles.InsertionCursorTitleTextColor.ToCairoColor ());
				g.ShowLayout (titleLayout);

				if (SupportsAlpha) {
					g.MoveTo (tw, Allocation.Height / 2 - th / 2);
					g.LineTo (0, Allocation.Height / 2);
					g.LineTo (tw, Allocation.Height / 2 + th / 2);
					g.LineTo (tw + 5, Allocation.Height / 2 + th / 2);
					g.LineTo (tw + 5, Allocation.Height / 2 - th / 2);
					g.ClosePath ();
					g.SetSourceColor (Styles.InsertionCursorBackgroundColor.ToCairoColor ());
					g.Fill ();

					g.MoveTo (tw, Allocation.Height / 2 - th / 2);
					g.LineTo (0, Allocation.Height / 2);
					g.LineTo (tw, Allocation.Height / 2 + th / 2);
					g.SetSourceColor (Styles.InsertionCursorBorderColor.ToCairoColor ());
					g.Stroke ();
				}

				int y = height + yTitleBorder + yDescriptionBorder;
				int x = tw + xDescriptionBorder;
				g.SetSourceColor (Styles.InsertionCursorTextColor.ToCairoColor ());

				foreach (var desc in descTexts) {
					desc.Render (g, x, y + 4);
					y += desc.Height + 8;
				}
			}
			return base.OnExposeEvent (args);
		}
	}
}
