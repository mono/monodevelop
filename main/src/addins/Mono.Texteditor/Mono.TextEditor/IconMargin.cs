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
	public class IconMargin : Margin
	{
		TextEditor editor;
		Gdk.GC backgroundGC, separatorGC;
		Pango.Layout layout;
		int marginWidth = 18;
		
		public IconMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = new Pango.Layout (editor.PangoContext);
		}
		
		public override int Width {
			get {
				return marginWidth;
			}
		}
		
		public override void Dispose ()
		{
			layout = layout.Kill ();
			DisposeGCs ();
		}
		
		void DisposeGCs ()
		{
			backgroundGC = backgroundGC.Kill ();
			separatorGC  = separatorGC.Kill ();
		}
		
		internal protected override void OptionsChanged ()
		{
			DisposeGCs ();
			backgroundGC = new Gdk.GC (editor.GdkWindow);
			backgroundGC.RgbFgColor = editor.ColorStyle.IconBarBg;
			
			separatorGC = new Gdk.GC (editor.GdkWindow);
			separatorGC.RgbFgColor = editor.ColorStyle.IconBarSeperator;
			
			layout.FontDescription = editor.Options.Font;
			layout.SetText ("!");
			int tmp;
			layout.GetPixelSize (out tmp, out this.marginWidth);
			marginWidth *= 12;
			marginWidth /= 10;
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			LineSegment lineSegment = args.LineSegment;
			if (lineSegment != null && lineSegment.Markers != null) {
				foreach (TextMarker marker in lineSegment.Markers) {
					if (marker is IIconBarMarker) 
						((IIconBarMarker)marker).MousePress (args);
				}
			}
		}
		
		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			base.MouseReleased (args);
			
			LineSegment lineSegment = args.LineSegment;
			if (lineSegment != null && lineSegment.Markers != null) {
				foreach (TextMarker marker in lineSegment.Markers) {
					if (marker is IIconBarMarker) 
						((IIconBarMarker)marker).MouseRelease (args);
				}
			}
		}
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int line, int x, int y)
		{
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, Width, editor.LineHeight);
			
			win.DrawRectangle (backgroundGC, true, drawArea);
			win.DrawLine (separatorGC, x + Width - 1, drawArea.Top, x + Width - 1, drawArea.Bottom);
			
			if (line < editor.Document.LineCount) {
				LineSegment lineSegment = editor.Document.GetLine (line);
				
				if (lineSegment.Markers != null) {
					foreach (TextMarker marker in lineSegment.Markers) {
						if (marker is IIconBarMarker) 
							((IIconBarMarker)marker).DrawIcon (editor, win, lineSegment, line, x, y, Width, editor.LineHeight);
					}
				}
				if (DrawEvent != null) 
					DrawEvent (this, new BookmarkMarginDrawEventArgs (editor, win, lineSegment, line, x, y));
				
			}
			
		}
		
		public EventHandler<BookmarkMarginDrawEventArgs> DrawEvent;
	}
	
	public class BookmarkMarginDrawEventArgs : EventArgs
	{
		TextEditor editor;
		Gdk.Drawable win;
		LineSegment lineSegment;
		int line;
		int x;
		int y;
		
		public TextEditor Editor {
			get {
				return editor;
			}
		}

		public Drawable Win {
			get {
				return win;
			}
		}

		public int Line {
			get {
				return line;
			}
		}

		public int X {
			get {
				return x;
			}
		}

		public int Y {
			get {
				return y;
			}
		}

		public LineSegment LineSegment {
			get {
				return lineSegment;
			}
		}
		
		public BookmarkMarginDrawEventArgs (TextEditor editor, Gdk.Drawable win, LineSegment line, int lineNumber, int xPos, int yPos)
		{
			this.editor = editor;
			this.win    = win;
			this.lineSegment = line;
			this.line   = lineNumber;
			this.x      = xPos;
			this.y      = yPos;
		}
	}
	
}
