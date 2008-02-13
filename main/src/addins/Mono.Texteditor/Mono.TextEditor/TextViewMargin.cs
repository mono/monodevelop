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
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (chunk));
				} else if (chunk.Offset >= selectionStart && chunk.Offset < selectionEnd && chunk.EndOffset > selectionEnd) {
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionEnd - chunk.Offset));
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (selectionEnd, chunk.EndOffset - selectionEnd));
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionStart && chunk.EndOffset <= selectionEnd) {
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionStart - chunk.Offset));
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (selectionStart, chunk.EndOffset - selectionStart));
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionEnd) {
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionStart - chunk.Offset));
					DrawTextWithHighlightedWs (win, true, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (selectionStart, selectionEnd - selectionStart));
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (selectionEnd, chunk.EndOffset - selectionEnd));
				} else 
					DrawTextWithHighlightedWs (win, false, chunk.Style, ref xPos, y, Document.Buffer.GetTextAt (chunk));
			}
			if (line.Markers != null) {
				foreach (TextMarker marker in line.Markers) {
					marker.Draw (textEditor, win, index, index + length, y, xStart, xPos);
				}
			}
			
			int caretOffset = Caret.Offset;
			if (offset <= caretOffset && caretOffset < offset + length) {
				int caretX = GetWidth (Document.Buffer.GetTextAt (offset, caretOffset - offset));
				DrawCaret (win, xStart + caretX, y);
			}
		}
		
		void DrawTextWithHighlightedWs (Gdk.Drawable win, bool selected, ChunkStyle style, ref int xPos, int y, string text)
		{
			string[] spaces = text.Split (' ');
			for (int i = 0; i < spaces.Length; i++) {
				string[] tabs = spaces[i].Split ('\t');
				
				for (int j = 0; j < tabs.Length; j++) {
					gc.RgbFgColor = selected ? ColorStyle.SelectedFg : style.Color;
					if (style.Bold)
						layout.FontDescription.Weight = Pango.Weight.Bold; 
					if (style.Italic)
						layout.FontDescription.Style  = Pango.Style.Italic; 
					DrawText (win, ref xPos, y, tabs[j]);
					if (style.Bold)
						layout.FontDescription.Weight = Pango.Weight.Normal; 
					if (style.Italic)
						layout.FontDescription.Style  = Pango.Style.Normal; 
					if (j + 1 < tabs.Length) {
						if (TextEditorOptions.Options.ShowTabs) { 
							DrawTabMarker (win, selected, ref xPos, y);
						} else {
							DrawText (win, ref xPos, y, new string (' ', TextEditorOptions.Options.TabSize));
						}
					}
				}
				
				if (i + 1 < spaces.Length) {
					if (TextEditorOptions.Options.ShowSpaces) { 
						DrawSpaceMarker (win, selected, ref xPos, y);
					} else {
						DrawText (win, ref xPos, y, " ");
					}
				}
			}
		}
		
		void DrawText (Gdk.Drawable win, ref int xPos, int y, string text)
		{
			layout.SetText (text);
			win.DrawLayout (gc, xPos, y, layout);
			int width, height;
			layout.GetPixelSize (out width, out height);
			xPos += width;
		}
		
		void DrawEolMarker (Gdk.Drawable win, bool selected, ref int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, eolMarker);
		}
		
		void DrawSpaceMarker (Gdk.Drawable win, bool selected, ref int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, spaceMarker);
			xPos += charWidth;
		}
		
		void DrawTabMarker (Gdk.Drawable win, bool selected, ref int xPos, int y)
		{
			gc.RgbFgColor = selected ? ColorStyle.SelectedFg : ColorStyle.WhitespaceMarker;
			win.DrawLayout (gc, xPos, y, tabMarker);
			xPos += charWidth * TextEditorOptions.Options.TabSize;
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
//				int oldLine = Caret.Line;
				Caret.Location = VisualToDocumentLocation (x, y);
				Caret.PreserveSelection = false;
				SelectionMoveLeft.EndSelection (TextEditorData);
				this.caretBlink = false;
//				textEditor.RedrawLines (System.Math.Min (oldLine, Caret.Line), System.Math.Max (oldLine, Caret.Line));
			}
		}
		
		public int ColumnToVisualX (LineSegment line, int column)
		{
			if (line.EditableLength == 0)
				return 0;
			string text = this.Document.Buffer.GetTextAt (line.Offset, System.Math.Min (column, line.EditableLength));
			text = text.Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize));
			layout.SetText (text);
			int width, height;
			layout.GetPixelSize (out width, out height);
			return width;
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
				win.DrawRectangle (gc, true, new Rectangle (area.X, area.Y, divider - area.X, area.Height));
				gc.RgbFgColor = DimColor (color);
				win.DrawRectangle (gc, true, new Rectangle (divider, area.Y, area.Right - divider, area.Height));
			} else {
				win.DrawRectangle (gc, true, area);
			}
		}
		
		public override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
			layout.Alignment = Pango.Alignment.Left;
			
			LineSegment line = lineNr < Document.Splitter.LineCount ? Document.Splitter.Get (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);
			gc.ClipRectangle = new Gdk.Rectangle (xStart, 
			                                      y, 
			                                      area.Right - xStart,
			                                      LineHeight);
		
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			bool isSelected = false;
			bool drawDefaultBg = true;
			bool eolSelected = false;
			Gdk.Color defaultStateType = lineNr == Caret.Line && TextEditorOptions.Options.HighlightCaretLine ? this.ColorStyle.LineMarker : this.ColorStyle.Background;
			
			if (line != null && TextEditorData.SelectionStart != null && TextEditorData.SelectionEnd != null) {
				SelectionMarker start;
				SelectionMarker end;
				
				if (TextEditorData.SelectionStart.Segment.Offset < TextEditorData.SelectionEnd.Segment.EndOffset) {
					start = TextEditorData.SelectionStart;
					end   = TextEditorData.SelectionEnd;
				} else {
					start = TextEditorData.SelectionEnd;
					end   = TextEditorData.SelectionStart;
				}
				isSelected = start.Segment.Offset < line.Offset && line.Offset + line.EditableLength < end.Segment.EndOffset;
				int selectionColumnStart = -1;
				int selectionColumnEnd   = -1;
				if (line == end.Segment) {
					selectionColumnStart = 0;
					selectionColumnEnd   = end.Column; 
				} 
				if (line == start.Segment) {
					selectionColumnStart = start.Column; 
				} 
				if (selectionColumnStart >= 0) {
					if (selectionColumnStart >= 0 && selectionColumnEnd >= 0 && selectionColumnEnd < selectionColumnStart) {
						int tmp = selectionColumnStart;
						selectionColumnStart = selectionColumnEnd;
						selectionColumnEnd = tmp;
					}
					
					// draw space before selection
					int visualXStart = ColumnToVisualX (line, selectionColumnStart) - (int)TextEditorData.HAdjustment.Value;
					lineArea = new Gdk.Rectangle (x, y, visualXStart, LineHeight);
					DrawRectangleWithRuler (win, x, lineArea, defaultStateType);
					
					// draw selection (if selection is in the middle)
					if (selectionColumnEnd >= 0) {
						int visualXEnd = ColumnToVisualX (line, selectionColumnEnd) - (int)TextEditorData.HAdjustment.Value;
						int reminder =  System.Math.Max (0, -visualXStart); 
						if (visualXEnd - visualXStart > reminder) {
							lineArea = new Gdk.Rectangle (x + visualXStart + reminder, y, visualXEnd - visualXStart - reminder, LineHeight);
							DrawRectangleWithRuler (win, x, lineArea, ColorStyle.SelectedBg);
						}
					}
					
					// draw remaining line (unselected, if in middle, otherwise rest of line is selected)
					lineArea = new Gdk.Rectangle (System.Math.Max (x, lineArea.Right), y, area.Width - System.Math.Max (x, lineArea.Right), LineHeight);
					DrawRectangleWithRuler (win, x, lineArea, selectionColumnEnd >= 0 ? defaultStateType : this.ColorStyle.SelectedBg);
					eolSelected = true;
					drawDefaultBg = false;
				}
			}
			int width, height;
			
			if (drawDefaultBg) {
				eolSelected = isSelected;
				DrawRectangleWithRuler (win, x, lineArea, isSelected ? this.ColorStyle.SelectedBg : defaultStateType);
			}
			
			if (TextEditorOptions.Options.ShowRuler) {
				gc.RgbFgColor = ColorStyle.Ruler;
				win.DrawLine (gc, x + rulerX, y, x + rulerX, y + LineHeight); 
			}
			
			if (line == null) {
				if (TextEditorOptions.Options.ShowInvalidLines)
					DrawInvalidLineMarker (win, x, y);
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
			if (TextEditorOptions.Options.ShowEolMarkers) 
				DrawEolMarker (win, eolSelected, ref xPos, y);
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
			int lineXPos = 0;
			int column;
			for (column = 0; column < line.EditableLength; column++) {
				if (this.Document.Buffer.GetCharAt (line.Offset + column) == '\t') {
					lineXPos += TextEditorOptions.Options.TabSize * this.charWidth;
				} else {
					lineXPos += this.charWidth;
				}
				if (lineXPos >= x + TextEditorData.HAdjustment.Value) {
					break;
				}
			}
			return new DocumentLocation (lineNumber, column);
		}
		
	}
}
