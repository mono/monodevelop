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

namespace Mono.TextEditor
{
	public class BookmarkMargin : AbstractMargin
	{
		TextEditor editor;
		
		static Gdk.Pixbuf bookmarkPixbuf;
		
		static BookmarkMargin ()
		{
			bookmarkPixbuf = Gdk.Pixbuf.LoadFromResource ("bookmark.png");
		}
		
		public BookmarkMargin (TextEditor editor)
		{
			this.editor = editor;
		}
		
		public override int Width {
			get {
				return 18;
			}
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
			Gdk.GC gc = new Gdk.GC (win);
			gc.RgbFgColor = editor.ColorStyle.IconBarBg;
			win.DrawRectangle (gc, true, drawArea);
			gc.RgbFgColor = editor.ColorStyle.IconBarSeperator;
			win.DrawLine (gc, x + Width - 1, drawArea.Top, x + Width - 1, drawArea.Bottom);
			if (line < editor.Splitter.LineCount) {
				LineSegment lineSegment = editor.Document.GetLine (line);
				if (lineSegment.IsBookmarked) {
					win.DrawPixbuf (editor.Style.BackgroundGC (StateType.Normal), bookmarkPixbuf, 0, 0, x + 1, y, bookmarkPixbuf.Width, bookmarkPixbuf.Height, Gdk.RgbDither.None, 0, 0);
				}
			}
		}
	}
}