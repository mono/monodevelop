//
// TextViewMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

using Mono.TextEditor.Highlighting;

using Gdk;
using Gtk;

namespace Mono.TextEditor
{
	public class TextViewMargin : AbstractMargin
	{
		TextEditor textEditor;
		Pango.Layout tabMarker, spaceMarker, eolMarker, invalidLineMarker;
		Pango.Layout layout;
		bool caretBlink = true;
		
		public int charWidth;
		int caretBlinkStatus;
		uint caretBlinkTimeoutId = 0;
		const int CaretBlinkTime = 800;
		
		Gdk.Cursor defaultCursor;
		Gdk.Cursor textCursor;
		
		int lineHeight = 16;
		public int LineHeight {
			get {
				return lineHeight;
			}
		}

		public override int Width {
			get {
				return -1;
			}
		}
		
		Caret Caret {
			get {
				return textEditor.Caret;
			}
		}
		
		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return this.textEditor.ColorStyle;
			}
		}
		
		public Document Document {
			get {
				return textEditor.Document;
			}
		}
		
		public TextEditorData TextEditorData {
			get {
				return textEditor.TextEditorData;
			}
		}
		
		public TextViewMargin (TextEditor textEditor)
		{
			this.textEditor = textEditor;
			
			layout = new Pango.Layout (textEditor.PangoContext);
			layout.Alignment = Pango.Alignment.Left;
			
			tabMarker = new Pango.Layout (textEditor.PangoContext);
			tabMarker.SetText ("\u00BB");
			
			spaceMarker = new Pango.Layout (textEditor.PangoContext);
			spaceMarker.SetText ("\u00B7");
			
			eolMarker = new Pango.Layout (textEditor.PangoContext);
			eolMarker.SetText ("\u00B6");
			
			invalidLineMarker = new Pango.Layout (textEditor.PangoContext);
			invalidLineMarker.SetText ("~");
			
			ResetCaretBlink ();
			Caret.PositionChanged += delegate (object sender, DocumentLocationEventArgs args) {
				if (Caret.AutoScrollToCaret) {
					textEditor.ScrollToCaret ();
					caretBlink = true;
					if (args.Location.Line != Caret.Line) 
						textEditor.RedrawLine (args.Location.Line);
					textEditor.RedrawLine (Caret.Line);
				}
			};
			
			// Bracket highlighting
			Caret.PositionChanged += delegate {
				int offset = Caret.Offset - 1;
				if (offset >= 0 && offset < Document.Buffer.Length && !TextUtil.IsBracket (Document.Buffer.GetCharAt (offset)))
					offset++;
				if (offset >= Document.Buffer.Length)
					return;
				if (offset < 0)
					offset = 0;
				char ch = Document.Buffer.GetCharAt (offset);
				int bracket = TextUtil.openBrackets.IndexOf (ch);
				int oldIndex = bracketIndex;
				if (bracket >= 0) {
					bracketIndex = TextUtil.SearchMatchingBracketForward (Document, offset + 1, bracket);
				} else {
					bracket = TextUtil.closingBrackets.IndexOf (ch);
					if (bracket >= 0) {
						bracketIndex = TextUtil.SearchMatchingBracketBackward (Document, offset - 1, bracket);
					} else {
						bracketIndex = -1;
					}
				}
				if (bracketIndex != oldIndex) {
					int line1 = oldIndex >= 0 ? Document.Splitter.OffsetToLineNumber (oldIndex) : -1;
					int line2 = bracketIndex >= 0 ? Document.Splitter.OffsetToLineNumber (bracketIndex) : -1;
					if (line1 >= 0)
						textEditor.RedrawLine (line1);
					if (line1 != line2 && line2 >= 0)
						textEditor.RedrawLine (line2);
				}
			};
			
			defaultCursor = null;
			textCursor = new Gdk.Cursor (Gdk.CursorType.Xterm);
		}
		
		public override void OptionsChanged ()
		{
			DisposeGCs ();
			gc = new Gdk.GC (textEditor.GdkWindow);
			layout.FontDescription = TextEditorOptions.Options.Font;
			layout.SetText ("H");
			layout.GetPixelSize (out this.charWidth, out this.lineHeight);
		}
		
		void DisposeGCs ()
		{
			if (gc != null) {
				gc.Dispose ();
				gc = null;
			}
		}
		
		public override void Dispose ()
		{
			if (caretBlinkTimeoutId != 0)
				GLib.Source.Remove (caretBlinkTimeoutId);
			DisposeGCs ();
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			if (tabMarker != null) {
				tabMarker.Dispose ();
				tabMarker = null;
			}
			if (spaceMarker != null) {
				spaceMarker.Dispose ();
				spaceMarker = null;
			}
			if (eolMarker != null) {
				eolMarker.Dispose ();
				eolMarker = null;
			}
			if (invalidLineMarker != null) {
				invalidLineMarker.Dispose ();
				invalidLineMarker = null;
			}
		}
		
		public void ResetCaretBlink ()
		{
			if (caretBlinkTimeoutId != 0)
				GLib.Source.Remove (caretBlinkTimeoutId);
			caretBlinkStatus = 0;
			caretBlinkTimeoutId = GLib.Timeout.Add (CaretBlinkTime / 2, new GLib.TimeoutHandler (CaretThread));
		}
		
		bool CaretThread ()
		{
			bool newCaretBlink = caretBlinkStatus < 4 || (caretBlinkStatus - 4) % 3 != 0;
			if (layout != null && newCaretBlink != caretBlink) {
				caretBlink = newCaretBlink;
				textEditor.RedrawLine (Caret.Line);
			}
			caretBlinkStatus++;
			return true;
		}
		
		void DrawCaret (Gdk.Drawable win, int x, int y)
		{
			if (!caretBlink || !textEditor.IsFocus) 
				return;
			gc.RgbFgColor = ColorStyle.Caret;
			if (Caret.IsInInsertMode) {
				win.DrawLine (gc, x, y, x, y + LineHeight);
			} else {
				win.DrawRectangle (gc, false, new Gdk.Rectangle (x, y, this.charWidth, LineHeight - 1));
			}
		}
		int bracketIndex = -1;
		
		
		void DrawLineText (Gdk.Drawable win, LineSegment line, int offset, int length, ref int xPos, int y)
		{
			SyntaxMode mode = Document.SyntaxMode != null && TextEditorOptions.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			Chunk[] chunks = mode.GetChunks (Document, TextEditorData.ColorStyle, line, offset, length);
//			int start  = offset;
			int xStart = xPos;
			int index = line.Offset + line.EditableLength - offset;
			int selectionStart = TextEditorData.SelectionStart != null ? TextEditorData.SelectionStart.Segment.Offset + TextEditorData.SelectionStart.Column : -1;
			int selectionEnd = TextEditorData.SelectionEnd != null ? TextEditorData.SelectionEnd.Segment.Offset + TextEditorData.SelectionEnd.Column : -1;
			int visibleColumn = 0;
			if (selectionStart > selectionEnd) {
				int tmp = selectionEnd;
				selectionEnd = selectionStart;
				selectionStart = tmp;
			}
			
			if (TextEditorOptions.Options.HighlightMatchingBracket && offset <= this.bracketIndex && this.bracketIndex < offset + length) {
				int bracketMarkerColumn = this.bracketIndex - line.Offset; 
				int width, height;
				layout.SetText (Document.Buffer.GetTextAt (offset, bracketMarkerColumn).Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize)));
				layout.GetPixelSize (out width, out height);
				Gdk.Rectangle bracketMatch = new Gdk.Rectangle (xStart + width, y, charWidth, LineHeight - 1);
				if (this.bracketIndex < selectionStart || this.bracketIndex > selectionEnd) {
					gc.RgbFgColor = this.ColorStyle.BracketHighlightBg;
					win.DrawRectangle (gc, true, bracketMatch);
				}
				gc.RgbFgColor = this.ColorStyle.BracketHighlightRectangle;
				win.DrawRectangle (gc, false, bracketMatch);
			}
			
//				Console.WriteLine ("#" + chunks.Length);
			foreach (Chunk chunk in chunks) {
				if (chunk.Offset >= selectionStart && chunk.EndOffset <= selectionEnd) {
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, chunk.EndOffset);
				} else if (chunk.Offset >= selectionStart && chunk.Offset < selectionEnd && chunk.EndOffset > selectionEnd) {
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionEnd);
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref visibleColumn, ref xPos, y, selectionEnd, chunk.EndOffset);
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionStart && chunk.EndOffset <= selectionEnd) {
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionStart);
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref visibleColumn, ref xPos, y, selectionStart, chunk.EndOffset);
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionEnd) {
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionStart);
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref visibleColumn, ref xPos, y, selectionStart, selectionEnd);
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref visibleColumn, ref xPos, y, selectionEnd, chunk.EndOffset);
				} else 
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, chunk.EndOffset);
			}
			if (line.Markers != null) {
				foreach (TextMarker marker in line.Markers) {
					marker.Draw (textEditor, win, index, index + length, y, xStart, xPos);
				}
			}
			
			int caretOffset = Caret.Offset;
			if (caretOffset == offset + length) 
				DrawCaret (win, xPos, y);
		}
		
		StringBuilder wordBuilder = new StringBuilder ();
		void OutputWordBuilder (Gdk.Drawable win, bool selected, ChunkStyle style, ref int visibleColumn, ref int xPos, int y)
		{
			if (selected) {
				DrawText (win, ColorStyle.SelectedFg, ColorStyle.SelectedBg, ref xPos, y);
			} else {
				DrawText (win, style.Color, ColorStyle.Background, ref xPos, y);
			}
			visibleColumn += wordBuilder.Length;
			wordBuilder.Length = 0;
		}
		
		void DrawTextWithHighlightedWs (Gdk.Drawable win, bool selected, ChunkStyle style, ref int visibleColumn, ref int xPos, int y, int startOffset, int endOffset)
		{
			int caretOffset = Caret.Offset;
			int drawCaretAt = -1;
			wordBuilder.Length = 0;
			if (style.Bold)
				layout.FontDescription.Weight = Pango.Weight.Bold;
			if (style.Italic)
				layout.FontDescription.Style = Pango.Style.Italic;
			for (int offset = startOffset; offset < endOffset; offset++) {
				char ch = Document.Buffer.GetCharAt (offset);
				if (ch == ' ') {
					OutputWordBuilder (win, selected, style, ref visibleColumn, ref xPos, y);
					DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, charWidth, LineHeight), selected ? ColorStyle.SelectedBg : ColorStyle.Background);
					
					if (TextEditorOptions.Options.ShowSpaces) 
						DrawSpaceMarker (win, selected, xPos, y);
					if (offset == caretOffset) 
						DrawCaret (win, xPos, y);
					xPos += this.charWidth;
					visibleColumn++;
				} else if (ch == '\t') {
					OutputWordBuilder (win, selected, style, ref visibleColumn, ref xPos, y);
					
					int newColumn = visibleColumn + TextEditorOptions.Options.TabSize;
					newColumn = (newColumn / TextEditorOptions.Options.TabSize) * TextEditorOptions.Options.TabSize;
					int delta = (newColumn - visibleColumn) * this.charWidth;
					visibleColumn = newColumn;
					
					DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, delta, LineHeight), selected ? ColorStyle.SelectedBg : ColorStyle.Background);
					if (TextEditorOptions.Options.ShowTabs) 
						DrawTabMarker (win, selected, xPos, y);
					if (offset == caretOffset) 
						DrawCaret (win, xPos, y);
					xPos += delta;
				} else {
					if (offset == caretOffset) 
						drawCaretAt = xPos + wordBuilder.Length * this.charWidth;
					wordBuilder.Append (ch);
				}
			}
			OutputWordBuilder (win, selected, style, ref visibleColumn, ref xPos, y);
			
			if (style.Bold)
				layout.FontDescription.Weight = Pango.Weight.Normal;
			if (style.Italic)
				layout.FontDescription.Style = Pango.Style.Normal;
			
			if (drawCaretAt >= 0)
				DrawCaret (win, drawCaretAt , y);
		}
		
		void DrawText (Gdk.Drawable win, Gdk.Color foreColor, Gdk.Color backgroundColor, ref int xPos, int y)
		{
			layout.SetText (wordBuilder.ToString ());
			
			int width, height;
			layout.GetPixelSize (out width, out height);
			DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, width, height), backgroundColor);
			
			gc.RgbFgColor = foreColor;
			win.DrawLayout (gc, xPos, y, layout);
			xPos += width;
		}
		
		void DrawEolMarker (Gdk.Drawable win, bool selected, ref int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, eolMarker);
		}
		
		void DrawSpaceMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, spaceMarker);
		}
		
		void DrawTabMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, tabMarker);
		}
		
		void DrawInvalidLineMarker (Gdk.Drawable win, int x, int y)
		{
			gc.RgbFgColor = ColorStyle.InvalidLineMarker;
			win.DrawLayout (gc, x, y, invalidLineMarker);
		}
		
		public bool inSelectionDrag = false;
		public bool inDrag = false;
		public DocumentLocation clickLocation;
		
		public override void MousePressed (int button, int x, int y, bool doubleClick, Gdk.ModifierType modifierState)
		{
			inSelectionDrag = false;
			inDrag = false;
			if (button == 1 || button == 2) {
				clickLocation = VisualToDocumentLocation (x, y);
				int offset = Document.LocationToOffset (clickLocation);
				if (offset < 0) {
					new CaretMoveToDocumentEnd ().Run (TextEditorData);
					return;
				}
				if (doubleClick) {
					int start = ScanWord (offset, false);
					int end   = ScanWord (offset, true);
					if (TextEditorData.IsSomethingSelected) {
						if (TextEditorData.SelectionRange.Offset == start && TextEditorData.SelectionRange.EndOffset == end) {
							TextEditorData.SelectionRange = Document.Splitter.GetByOffset (offset);
							return;
						}
					}
					TextEditorData.SelectionRange = new Segment (start, end - start);
					return;
				}
 
				if (TextEditorData.IsSomethingSelected && TextEditorData.SelectionRange.Offset <= offset && offset < TextEditorData.SelectionRange.EndOffset) {
					inDrag = true;
				} else {
					inSelectionDrag = true;
					if ((modifierState & Gdk.ModifierType.ShiftMask) == ModifierType.ShiftMask) {
						if (!TextEditorData.IsSomethingSelected) 
							SelectionMoveLeft.StartSelection (TextEditorData);
						Caret.PreserveSelection = true;
						Caret.Location = clickLocation;
						Caret.PreserveSelection = false;
						SelectionMoveLeft.EndSelection (TextEditorData);
					} else {
						Caret.Location = clickLocation; 
					}
					this.caretBlink = false;
				}
			}
			if (button == 2) {
				PasteAction.PasteFromPrimary (TextEditorData);
			}
		}
		
		int ScanWord (int offset, bool forwardDirection)
		{
			LineSegment line = Document.Splitter.GetByOffset (offset);
			while (offset >= line.Offset && offset < line.Offset + line.EditableLength && char.IsWhiteSpace (Document.Buffer.GetCharAt (offset))) {
				offset = forwardDirection ? offset + 1 : offset - 1; 
			}
			while (offset >= line.Offset && offset < line.Offset + line.EditableLength && (char.IsLetterOrDigit (Document.Buffer.GetCharAt (offset)) || Document.Buffer.GetCharAt (offset) == '_')) {
				offset = forwardDirection ? offset + 1 : offset - 1; 
			}
			return offset + (forwardDirection ? 0 : 1);
		}
		
		public override void MouseHover (int x, int y, bool buttonPressed)
		{
			textEditor.GdkWindow.Cursor = textCursor;
			if (!buttonPressed)
				return;
			if (inSelectionDrag) {
				if (!TextEditorData.IsSomethingSelected) {
					SelectionMoveLeft.StartSelection (TextEditorData);
				}
				Caret.PreserveSelection = true;
				Caret.AutoScrollToCaret = false;
//				int oldLine = Caret.Line;
				Caret.Location = VisualToDocumentLocation (x, y);
				Caret.PreserveSelection = false;
				Caret.AutoScrollToCaret = true;
				SelectionMoveLeft.EndSelection (TextEditorData);
				this.textEditor.ScrollToCaret ();
				this.caretBlink = false;
//				textEditor.RedrawLines (System.Math.Min (oldLine, Caret.Line), System.Math.Max (oldLine, Caret.Line));
			}
		}
		
		public int ColumnToVisualX (LineSegment line, int column)
		{
			if (line.EditableLength == 0)
				return 0;
			
			int lineXPos  = 0;
			
			int visibleColumn = 0;
			for (int c = 0; c < column; c++) {
				int delta;
				if (this.Document.Buffer.GetCharAt (line.Offset + column) == '\t') {
					int newColumn = visibleColumn + TextEditorOptions.Options.TabSize;
					newColumn = (newColumn / TextEditorOptions.Options.TabSize) * TextEditorOptions.Options.TabSize;
					delta = (newColumn - visibleColumn) * this.charWidth;
					visibleColumn = newColumn;
				} else {
					delta = this.charWidth;
					visibleColumn++;
				}
				lineXPos += delta;
			}
			return lineXPos;
		}
		int rulerX = 0;
		
		public int GetWidth (string text)
		{
			text = text.Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize));
			layout.SetText (text);
			int width, height;
			layout.GetPixelSize (out width, out height);
			return width;
		}

		static Color DimColor (Color color)
		{
			return new Color ((byte)(((byte)color.Red * 19) / 20),
			                  (byte)(((byte)color.Green * 19) / 20),
			                  (byte)(((byte)color.Blue * 19) / 20));
		}
		Gdk.GC gc;
		void DrawRectangleWithRuler (Gdk.Drawable win, int x, Gdk.Rectangle area, Gdk.Color color)
		{
			gc.RgbFgColor = color;
			if (TextEditorOptions.Options.ShowRuler) {
				int divider = System.Math.Max (area.Left, System.Math.Min (x + rulerX, area.Right));
				if (divider < area.Right) {
					win.DrawRectangle (gc, true, new Rectangle (area.X, area.Y, divider - area.X, area.Height));
					gc.RgbFgColor = DimColor (color);
					win.DrawRectangle (gc, true, new Rectangle (divider, area.Y, area.Right - divider, area.Height));
					return;
				}
			}
			win.DrawRectangle (gc, true, area);
		}
		
		public override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
			layout.Alignment = Pango.Alignment.Left;
			
			LineSegment line = lineNr < Document.Splitter.LineCount ? Document.Splitter.Get (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);
			gc.ClipRectangle = new Gdk.Rectangle (xStart, y, area.Right - xStart, LineHeight);
		
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			
			if (line == null) {
				if (TextEditorOptions.Options.ShowInvalidLines) {
					DrawRectangleWithRuler (win, x, lineArea, this.ColorStyle.Background);
					DrawInvalidLineMarker (win, x, y);
				}
				return;
			}
			
			List<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int xPos   = (int)(x - TextEditorData.HAdjustment.Value);
			int caretOffset = Caret.Offset;
			for (int i = 0; i < foldings.Count; ++i) {
				FoldSegment folding = foldings[i];
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
					layout.SetText (Document.Buffer.GetTextAt (offset, foldOffset - offset));
					gc.RgbFgColor = ColorStyle.FoldLine;
//					win.DrawLayout (gc, xPos, y, layout);
					layout.GetPixelSize (out width, out height);
					
					DrawLineText (win, line, offset, foldOffset - offset, ref xPos, y);
//					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					gc.RgbBgColor = ColorStyle.Background;
					gc.RgbFgColor = ColorStyle.FoldLine;
					win.DrawRectangle (gc, false, new Rectangle (xPos, y, width, this.LineHeight - 1));
					
					gc.RgbFgColor = ColorStyle.FoldLine;
					win.DrawLayout (gc, xPos, y, layout);
					if (caretOffset == foldOffset)
						DrawCaret (win, xPos, y);
					
					xPos += width;
					
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						i = -1;
					}
				
				}
			}
			
			if (textEditor.longestLine == null || line.EditableLength > textEditor.longestLine.EditableLength) {
				textEditor.longestLine = line;
				textEditor.SetAdjustments (textEditor.Allocation);
			}
			
			// Draw remaining line
			if (line.EndOffset - offset > 0)
				DrawLineText (win, line, offset, line.Offset + line.EditableLength - offset, ref xPos, y);
			
			bool isEolSelected = TextEditorData.IsSomethingSelected && TextEditorData.SelectionRange.Contains (line.Offset + line.EditableLength);
			if (TextEditorOptions.Options.ShowEolMarkers) // TODO: EOL selected
				DrawEolMarker (win, isEolSelected, ref xPos, y);
			
			lineArea.X     = xPos;
			lineArea.Width = textEditor.Allocation.Width - xPos;
			DrawRectangleWithRuler (win, x, lineArea, isEolSelected ? this.ColorStyle.SelectedBg : this.ColorStyle.Background);
			
			if (TextEditorOptions.Options.ShowRuler) {
				gc.RgbFgColor = ColorStyle.Ruler;
				win.DrawLine (gc, x + rulerX, y, x + rulerX, y + LineHeight); 
			}
			
			if (caretOffset == line.Offset + line.EditableLength)
				DrawCaret (win, xPos, y);
		}
		
		public override void MouseLeft ()
		{
			textEditor.GdkWindow.Cursor = defaultCursor;
		}
		
		public DocumentLocation VisualToDocumentLocation (int x, int y)
		{
			int lineNumber = Document.VisualToLogicalLine (System.Math.Min ((int)(y + TextEditorData.VAdjustment.Value) / LineHeight, Document.Splitter.LineCount - 1));
			LineSegment line = Document.Splitter.Get (lineNumber);
			int lineXPos  = 0;
			int column;
			int  visibleColumn = 0;
			int visualXPos = x + (int)TextEditorData.HAdjustment.Value;
			for (column = 0; column < line.EditableLength; column++) {
				int delta;
				if (this.Document.Buffer.GetCharAt (line.Offset + column) == '\t') {
					int newColumn = visibleColumn + TextEditorOptions.Options.TabSize;
					newColumn = (newColumn / TextEditorOptions.Options.TabSize) * TextEditorOptions.Options.TabSize;
					delta = (newColumn - visibleColumn) * this.charWidth;
					visibleColumn = newColumn;
				} else {
					delta = this.charWidth;
					visibleColumn++;
				}
				int nextXPosition = lineXPos + delta;
				if (nextXPosition >= visualXPos) {
					if (!IsNearX1 (visualXPos, lineXPos, nextXPosition))
						column++;
					break;
				}
				lineXPos = nextXPosition;
			}
			return new DocumentLocation (lineNumber, column);
		}
		
		static bool IsNearX1 (int pos, int x1, int x2)
		{
			return System.Math.Abs (x1 - pos) < System.Math.Abs (x2 - pos);
		}
	}
}
