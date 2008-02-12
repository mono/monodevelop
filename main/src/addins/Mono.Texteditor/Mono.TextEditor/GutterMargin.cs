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
	public class GutterMargin : AbstractMargin
	{
		TextEditor editor;
		Pango.Layout layout;
		int width;
		
		public GutterMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = new Pango.Layout (editor.PangoContext);
			editor.TextEditorData.Document.Splitter.LinesInserted += LineCountChanged;
			editor.TextEditorData.Document.Splitter.LinesRemoved += LineCountChanged;
		}
		
		
		int oldWidth = 1;
		void LineCountChanged (object sender, LineEventArgs args)
		{
			int newWidth = (int)System.Math.Log10 (editor.TextEditorData.Document.Splitter.LineCount);
			if (oldWidth != newWidth) {
				editor.TextEditorData.Document.RequestUpdate (new UpdateAll ());
				editor.TextEditorData.Document.CommitDocumentUpdate ();
				oldWidth = newWidth;
			}
			
		}
		
		public override int Width {
			get {
				layout.SetText (editor.Splitter.LineCount.ToString ());
				int height;
				layout.GetPixelSize (out width, out height);
				return 5 + width;
			}
		}
		
		public override void MousePressed (int button, int x, int y, bool doubleClick)
		{
			int lineNumber = editor.Document.VisualToLogicalLine ((int)(y + editor.TextEditorData.VAdjustment.Value) / editor.LineHeight);
			if (lineNumber < editor.Splitter.LineCount) {
				DocumentLocation loc = new DocumentLocation (lineNumber, 0);
				if (loc != editor.Caret.Location) {
					editor.Caret.Location = loc;
				} else if (editor.TextEditorData.IsSomethingSelected) {
					editor.TextEditorData.ClearSelection ();
					editor.QueueDraw ();
				} else {
					LineSegment line = editor.Document.GetLine (lineNumber);
					editor.TextEditorData.SelectionRange = new Segment (line.Offset, line.EditableLength); 
					editor.QueueDraw ();
				}
			}
		}
		public override void Dispose ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			DisposeGCs ();
		}
		
		void DisposeGCs ()
		{
			if (lineNumberBgGC != null) {
				lineNumberBgGC.Dispose ();
				lineNumberBgGC = null;
			}
			if (lineNumberGC != null) {
				lineNumberGC.Dispose ();
				lineNumberGC = null;
			}
			if (lineNumberHighlightGC != null) {
				lineNumberHighlightGC.Dispose ();
				lineNumberHighlightGC = null;
			}
		}
		
		Gdk.GC lineNumberBgGC, lineNumberGC, lineNumberHighlightGC;
		public override void OptionsChanged ()
		{
			layout.FontDescription = TextEditorOptions.Options.Font;
			DisposeGCs ();
			lineNumberBgGC = new Gdk.GC (editor.GdkWindow);
			lineNumberBgGC.RgbFgColor = editor.ColorStyle.LineNumberBg;
			
			lineNumberGC = new Gdk.GC (editor.GdkWindow);
			lineNumberGC.RgbFgColor = editor.ColorStyle.LineNumberFg;
			
			lineNumberHighlightGC = new Gdk.GC (editor.GdkWindow);
			lineNumberHighlightGC.RgbFgColor = editor.ColorStyle.LineNumberFgHighlighted;
		}
		
		public override void Draw (Gdk.Window win, Gdk.Rectangle area, int line, int x, int y)
		{
			Gdk.Rectangle drawArea = new Rectangle (x, y, Width, editor.LineHeight);
			win.DrawRectangle (lineNumberBgGC, true, drawArea);
			if (line < editor.Splitter.LineCount) {
				layout.SetText ((line + 1).ToString ());
				win.DrawLayout (editor.Caret.Line == line ? lineNumberHighlightGC : lineNumberGC, x + 1, y, layout);
			}
		}
		
	}
}