// BookmarkMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Gdk;

namespace Mono.TextEditor
{
	public class BookmarkMargin : AbstractMargin
	{
		TextEditor editor;
		Gdk.GC backgroundGC, seperatorGC;
		Cairo.Color color1, color2;
		
		public BookmarkMargin (TextEditor editor)
		{
			this.editor = editor;
		}
		
		public override int Width {
			get {
				return 18;
			}
		}
		
		public override void Dispose ()
		{
			if (backgroundGC != null) {
				backgroundGC.Dispose ();
				backgroundGC = null;
			}
			if (seperatorGC != null) {
				seperatorGC.Dispose ();
				seperatorGC = null;
			}
		}
		
		public override void OptionsChanged ()
		{
			backgroundGC = new Gdk.GC (editor.GdkWindow);
			backgroundGC.RgbFgColor = editor.ColorStyle.IconBarBg;
			
			seperatorGC = new Gdk.GC (editor.GdkWindow);
			seperatorGC.RgbFgColor = editor.ColorStyle.IconBarSeperator;
			
			color1 = Convert (editor.ColorStyle.BookmarkColor1);
			color2 = Convert (editor.ColorStyle.BookmarkColor2);
		}
		
		static Cairo.Color Convert (Gdk.Color color)
		{
			return new Cairo.Color (color.Red / (double)ushort.MaxValue, 
			                        color.Green / (double)ushort.MaxValue, 
			                        color.Blue / (double)ushort.MaxValue);
		}
		
		public static void DrawRoundRectangle (Cairo.Context cr, int x, int y, int r, int w, int h)
		{
			cr.MoveTo (x + r, y);
			cr.LineTo (x + w - r, y);
			cr.CurveTo (x + w, y, x + w, y, x + w, y + r);
			cr.LineTo (x + w, y + h - r);
			cr.CurveTo (x + w, y + h, x + w, y + h, x + w - r, y + h);
			cr.LineTo (x + r, y + h);
			cr.CurveTo (x, y + h, x, y + h, x, y + h - r);
			cr.LineTo (x, y + r);
			cr.CurveTo (x, y, x, y, x + r, y);
		}

		public override void MousePressed (int button, int x, int y, bool doubleClick)
		{
			if (button != 1 || doubleClick)
				return;
			int lineNumber = editor.VisualToDocumentLocation (x, y).Line;
			if (lineNumber < editor.Splitter.LineCount) {
				LineSegment lineSegment = editor.Document.GetLine (lineNumber);
				lineSegment.IsBookmarked = !lineSegment.IsBookmarked;
				editor.Document.RequestUpdate (new LineUpdate (lineNumber));
				editor.Document.CommitDocumentUpdate ();
			}
		}
		
		public override void Draw (Gdk.Window win, Gdk.Rectangle area, int line, int x, int y)
		{
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, Width, editor.LineHeight);
			
			win.DrawRectangle (backgroundGC, true, drawArea);
			win.DrawLine (seperatorGC, x + Width - 1, drawArea.Top, x + Width - 1, drawArea.Bottom);
			if (line < editor.Splitter.LineCount) {
				LineSegment lineSegment = editor.Document.GetLine (line);
				if (lineSegment.IsBookmarked) {
					Cairo.Context cr = Gdk.CairoHelper.Create (win);
					DrawRoundRectangle (cr, x + 1, y + 1, 8, Width - 4, editor.LineHeight - 4);
					Cairo.Gradient pat = new Cairo.LinearGradient (x, y + editor.LineHeight, x + Width, y);
					pat.AddColorStop (0, color1);
					pat.AddColorStop (1, color2);
					cr.Pattern = pat;
					cr.FillPreserve ();
					
					pat = new Cairo.LinearGradient (x, y + editor.LineHeight, x + Width, y);
					pat.AddColorStop (0, color2);
					pat.AddColorStop (1, color1);
					cr.Pattern = pat;
					cr.Stroke ();
					((IDisposable)cr).Dispose();
				}
			}
		}
	}
}