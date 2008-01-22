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
		
		public GutterMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = new Pango.Layout (editor.PangoContext);
			layout.FontDescription = TextEditorOptions.Options.Font;
		}
		
		public override int Width {
			get {
				return 5 + (int)(System.Math.Max (2, System.Math.Log10 (editor.Splitter.LineCount)) * 10);
			}
		}
		
		public override void MousePressed (int button, int x, int y)
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
		
		public override void Draw (Gdk.Window win, Gdk.Rectangle area, int line, int x, int y)
		{
			Gdk.Rectangle drawArea = new Rectangle (x, y, Width, editor.LineHeight);
			
			Gdk.GC gc = new Gdk.GC (win);
			gc.RgbFgColor = editor.ColorStyle.LineNumberBg;
			win.DrawRectangle (gc, true, drawArea);
			if (line < editor.Splitter.LineCount) {
				layout.SetText ((line + 1).ToString ());
				gc.RgbFgColor = editor.Caret.Line == line ? editor.ColorStyle.LineNumberFgHighlighted : editor.ColorStyle.LineNumberFg;
				win.DrawLayout (gc, x + 1, y, layout);
			}
		}
	}
}