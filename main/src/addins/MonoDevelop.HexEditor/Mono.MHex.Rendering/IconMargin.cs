// 
// IconMargin.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Xwt;

namespace Mono.MHex.Rendering
{
	class IconMargin : Margin
	{
		double marginWidth;
		public override double Width {
			get {
				return marginWidth;
			}
		}
		
		public override double CalculateWidth (int bytesInRow)
		{
			return Width;
		}
		
		public IconMargin  (HexEditor hexEditor) : base (hexEditor)
		{
		}
		
		internal protected override void OptionsChanged ()
		{
			var layout = new TextLayout (Editor);
			layout.Font = Editor.Options.Font;
			layout.Text = "!";
//			int tmp;
			this.marginWidth = layout.GetSize ().Height;
			marginWidth *= 12;
			marginWidth /= 10;
			layout.Dispose ();
		}
		
		internal protected override void Draw (Context win, Rectangle area, long line, double x, double y)
		{
			win.Rectangle (x, y, Width, Editor.LineHeight);
			win.SetColor (Style.IconBarBg);
			win.Fill ();
			win.MoveTo (x + Width - 1, y);
			win.LineTo (x + Width - 1, y + Editor.LineHeight);
			win.SetColor (Style.IconBarSeperator);
			win.Stroke ();

			foreach (long bookmark in Data.Bookmarks) {
				if (line * Editor.BytesInRow <= bookmark && bookmark < line * Editor.BytesInRow + Editor.BytesInRow) {
					DrawBookmark (win, x, y);
					return;
				}
			}
		}

		void DrawBookmark (Context cr, double x, double y)
		{
			var color1 = Style.BookmarkColor1;
			var color2 = Style.BookmarkColor2;
			
			DrawRoundRectangle (cr, x + 1, y + 1, 8, Width - 4, Editor.LineHeight - 4);
			using (var pat = new LinearGradient (x + Width / 4, y, x + Width / 2, y + Editor.LineHeight - 4)) {
				pat.AddColorStop (0, color1);
				pat.AddColorStop (1, color2);
				cr.Pattern = pat;
				cr.FillPreserve ();
			}
			
			using (var pat = new LinearGradient (x, y + Editor.LineHeight, x + Width, y)) {
				pat.AddColorStop (0, color2);
				//pat.AddColorStop (1, color1);
				cr.Pattern = pat;
				cr.Stroke ();
			}
		}
		
		public static void DrawRoundRectangle (Context cr, double x, double y, double r, double w, double h)
		{
			const double ARC_TO_BEZIER = 0.55228475;
			double radius_x = r;
			double radius_y = r / 4;
			
			if (radius_x > w - radius_x)
				radius_x = w / 2;
					
			if (radius_y > h - radius_y)
				radius_y = h / 2;
			
			double c1 = ARC_TO_BEZIER * radius_x;
			double c2 = ARC_TO_BEZIER * radius_y;
			
			cr.NewPath ();
			cr.MoveTo (x + radius_x, y);
			cr.RelLineTo (w - 2 * radius_x, 0.0);
			cr.RelCurveTo (c1, 0.0, radius_x, c2, radius_x, radius_y);
			cr.RelLineTo (0, h - 2 * radius_y);
			cr.RelCurveTo (0.0, c2, c1 - radius_x, radius_y, -radius_x, radius_y);
			cr.RelLineTo (-w + 2 * radius_x, 0);
			cr.RelCurveTo (-c1, 0, -radius_x, -c2, -radius_x, -radius_y);
			cr.RelLineTo (0, -h + 2 * radius_y);
			cr.RelCurveTo (0.0, -c2, radius_x - c1, -radius_y, radius_x, -radius_y);
			cr.ClosePath ();
		}
	}
}
