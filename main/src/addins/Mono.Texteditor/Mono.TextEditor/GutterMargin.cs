// GutterMargin.cs
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
	public class GutterMargin : Margin
	{
		TextEditor editor;
		Pango.Layout layout;
		int width;
		int oldLineCountLog10 = 1;
		
		public GutterMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = new Pango.Layout (editor.PangoContext);
			base.cursor = new Gdk.Cursor (Gdk.CursorType.RightPtr);
			this.editor.Document.LineChanged += UpdateWidth;
		}
		
		void CalculateWidth ()
		{
			layout.SetText (editor.Document.LineCount.ToString () + "_");
			int height;
			layout.GetPixelSize (out this.width, out height);
		}
		
		void UpdateWidth (object sender, LineEventArgs args)
		{
			int currentLineCountLog10 = (int)System.Math.Log10 (editor.Document.LineCount);
			if (oldLineCountLog10 != currentLineCountLog10) {
				CalculateWidth ();
				oldLineCountLog10 = currentLineCountLog10;
				editor.Document.CommitUpdateAll ();
			}
		}
		
		public override int Width {
			get {
				return width;
			}
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.Button != 1)
				return;
			int lineNumber       = args.LineNumber;
			bool extendSelection = (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
			if (lineNumber < editor.Document.LineCount) {
				DocumentLocation loc = new DocumentLocation (lineNumber, 0);
				LineSegment line = args.LineSegment;
				if (args.Type == EventType.TwoButtonPress) {
					editor.SelectionRange = line;
					editor.SelectionAnchor = editor.Document.LocationToOffset (loc);
				} else if (extendSelection) {
					if (!editor.IsSomethingSelected) {
						editor.SelectionAnchor = editor.Caret.Offset;
					} 
					editor.SetSelectLines (editor.SelectionAnchorLocation.Line, lineNumber);
				} else {
					editor.ClearSelection ();
				}
				editor.Caret.PreserveSelection = true;
				editor.Caret.Location = loc;
				editor.Caret.PreserveSelection = false;
				
				
//				if (loc != editor.Caret.Location) {
//					
//				} else if (editor.IsSomethingSelected) {
//					editor.ClearSelection ();
//					editor.QueueDraw ();
//				} else {
//					LineSegment line = editor.Document.GetLine (lineNumber);
//					editor.SelectionRange = new Segment (line.Offset, line.EditableLength); 
//					editor.QueueDraw ();
//				}
			}
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			
			if (args.Button == 1) {
				if (!editor.IsSomethingSelected) {
					editor.SelectionAnchor = editor.Caret.Offset;
				} 
				int lineNumber = args.LineNumber != -1 ? args.LineNumber : editor.Document.LineCount - 1;
				editor.SetSelectLines (editor.SelectionAnchorLocation.Line, lineNumber);
				editor.Caret.PreserveSelection = true;
				editor.Caret.Location = new DocumentLocation (lineNumber, 0);
				editor.Caret.PreserveSelection = false;
			}
		}
		
		
		public override void Dispose ()
		{
			this.editor.Document.LineChanged -= UpdateWidth;
			layout = layout.Kill ();
			DisposeGCs ();
			base.Dispose ();
		}
		
		void DisposeGCs ()
		{
			lineNumberBgGC = lineNumberBgGC.Kill ();
			lineNumberGC = lineNumberGC.Kill ();
			lineNumberHighlightGC = lineNumberHighlightGC.Kill ();
		}
		
		Gdk.GC lineNumberBgGC, lineNumberGC, lineNumberHighlightGC;
		internal protected override void OptionsChanged ()
		{
			layout.FontDescription = editor.Options.Font;
			CalculateWidth ();
			
			DisposeGCs ();
			lineNumberBgGC = new Gdk.GC (editor.GdkWindow);
			lineNumberBgGC.RgbFgColor = editor.ColorStyle.LineNumberBg;
			
			lineNumberGC = new Gdk.GC (editor.GdkWindow);
			lineNumberGC.RgbFgColor = editor.ColorStyle.LineNumberFg;
			
			lineNumberHighlightGC = new Gdk.GC (editor.GdkWindow);
			lineNumberHighlightGC.RgbFgColor = editor.ColorStyle.LineNumberFgHighlighted;
		}
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int line, int x, int y)
		{
			
			Gdk.Rectangle drawArea = new Rectangle (x, y, Width, editor.LineHeight);
			win.DrawRectangle (lineNumberBgGC, true, drawArea);
			if (line < editor.Document.LineCount) {
				layout.SetText ((line + 1).ToString ());
				win.DrawLayout (editor.Caret.Line == line ? lineNumberHighlightGC : lineNumberGC, x + 1, y, layout);
			}
		}
		
	}
}