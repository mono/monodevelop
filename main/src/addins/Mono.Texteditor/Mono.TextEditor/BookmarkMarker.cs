// BookmarkMarker.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	
	
	public class BookmarkMarker: TextMarker, IIconBarMarker
	{
		internal static BookmarkMarker Instance = new BookmarkMarker ();
		
		public BookmarkMarker()
		{
		}

		public void DrawIcon (TextEditor editor, Gdk.Drawable win, LineSegment lineSegment, int lineNumber, int x, int y, int width, int height)
		{
			if (lineSegment.IsBookmarked) {
				Cairo.Color color1 = Style.ToCairoColor (editor.ColorStyle.BookmarkColor1);
				Cairo.Color color2 = Style.ToCairoColor (editor.ColorStyle.BookmarkColor2);
				
				Cairo.Context cr = Gdk.CairoHelper.Create (win);
				DrawRoundRectangle (cr, x + 1, y + 1, 8, width - 4, height - 4);
				Cairo.Gradient pat = new Cairo.LinearGradient (x + width / 4, y, x + width / 2, y + height - 4);
				pat.AddColorStop (0, color1);
				pat.AddColorStop (1, color2);
				cr.Pattern = pat;
				cr.FillPreserve ();
				
				pat = new Cairo.LinearGradient (x, y + height, x + width, y);
				pat.AddColorStop (0, color2);
				//pat.AddColorStop (1, color1);
				cr.Pattern = pat;
				cr.Stroke ();
				((IDisposable)cr).Dispose();
			}
		}
		
		public static void DrawRoundRectangle (Cairo.Context cr, int x, int y, int r, int w, int h)
		{
			const double ARC_TO_BEZIER = 0.55228475;
			int radius_x = r;
			int radius_y = r / 4;
			
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
		
		public void MousePress (MarginMouseEventArgs args)
		{
		}
		
		public void MouseRelease (MarginMouseEventArgs args)
		{
		}
	}
}
