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
		
		int charWidth;
		int caretBlinkStatus;
		uint caretBlinkTimeoutId = 0;
		const int CaretBlinkTime = 800;
		
		int lineHeight = 16;
		int highlightBracketOffset = -1;
		
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
		
		public int CharWidth {
			get {
				return charWidth;
			}
		}
		
		const char spaceMarkerChar = '\u00B7'; 
		const char tabMarkerChar = '\u00BB'; 
		const char eolMarkerChar = '\u00B6'; 
		
		public TextViewMargin (TextEditor textEditor)
		{
			this.textEditor = textEditor;
			
			layout = new Pango.Layout (textEditor.PangoContext);
			layout.Alignment = Pango.Alignment.Left;
			
			tabMarker = new Pango.Layout (textEditor.PangoContext);
			tabMarker.SetText ("\u00BB");
			
			spaceMarker = new Pango.Layout (textEditor.PangoContext);
			spaceMarker.SetText (spaceMarkerChar.ToString ());
			
			eolMarker = new Pango.Layout (textEditor.PangoContext);
			eolMarker.SetText ("\u00B6");
			
			invalidLineMarker = new Pango.Layout (textEditor.PangoContext);
			invalidLineMarker.SetText ("~");
			
			ResetCaretBlink ();
			Caret.PositionChanged += CaretPositionChanged;
			textEditor.Document.TextReplaced += UpdateBracketHighlighting;
			Caret.PositionChanged += UpdateBracketHighlighting;
			base.cursor = new Gdk.Cursor (Gdk.CursorType.Xterm);
		}
		
		void CaretPositionChanged (object sender, DocumentLocationEventArgs args) 
		{
			if (Caret.AutoScrollToCaret) {
				textEditor.ScrollToCaret ();
				if (args.Location.Line != Caret.Line) {
					caretBlink = false;
					textEditor.RedrawLine (args.Location.Line);
				}
				caretBlink = true;
				textEditor.RedrawLine (Caret.Line);
			}
		}
		
		void UpdateBracketHighlighting (object sender, EventArgs e)
		{
			int offset = Caret.Offset - 1;
			if (offset >= 0 && offset < Document.Length && !Document.IsBracket (Document.GetCharAt (offset)))
				offset++;
			if (offset >= Document.Length) {
				int old = highlightBracketOffset;
				highlightBracketOffset = -1;
				if (old >= 0)
					textEditor.RedrawLine (Document.OffsetToLineNumber (old));
				return;
			}
			if (offset < 0)
				offset = 0;
			int oldIndex = highlightBracketOffset;
			highlightBracketOffset = Document.GetMatchingBracketOffset (offset);
			if (highlightBracketOffset == Caret.Offset && offset + 1 < Document.Length)
				highlightBracketOffset = Document.GetMatchingBracketOffset (offset + 1);
			if (highlightBracketOffset == Caret.Offset)
				highlightBracketOffset = -1;
			
			if (highlightBracketOffset != oldIndex) {
				int line1 = oldIndex >= 0 ? Document.OffsetToLineNumber (oldIndex) : -1;
				int line2 = highlightBracketOffset >= 0 ? Document.OffsetToLineNumber (highlightBracketOffset) : -1;
				if (line1 >= 0)
					textEditor.RedrawLine (line1);
				if (line1 != line2 && line2 >= 0)
					textEditor.RedrawLine (line2);
			}
		}
		
		public override void OptionsChanged ()
		{
			DisposeGCs ();
			gc = new Gdk.GC (textEditor.GdkWindow);
			
			tabMarker.FontDescription = 
			spaceMarker.FontDescription = 
			eolMarker.FontDescription = 
			invalidLineMarker.FontDescription = 
			layout.FontDescription = TextEditorOptions.Options.Font;
			
			layout.SetText ("H");
			layout.GetPixelSize (out this.charWidth, out this.lineHeight);
		}
		
		void DisposeGCs ()
		{
			ShowTooltip (null, Gdk.Rectangle.Zero);
			if (gc != null) {
				gc.Dispose ();
				gc = null;
			}
		}
		
		public override void Dispose ()
		{
			if (caretBlinkTimeoutId != 0)
				GLib.Source.Remove (caretBlinkTimeoutId);
			
			Caret.PositionChanged -= CaretPositionChanged;
			textEditor.Document.TextReplaced -= UpdateBracketHighlighting;
			Caret.PositionChanged -= UpdateBracketHighlighting;
						
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
			base.Dispose ();
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
		
		char caretChar; 
		int  caretX;
		int  caretY;
		
		void SetVisibleCaretPosition (Gdk.Drawable win, char ch, int x, int y)
		{
			caretChar = ch;
			caretX    = x;
			caretY    = y;
		}
		
		void DrawCaret (Gdk.Drawable win)
		{
			if (!Caret.IsVisible || !caretBlink || !textEditor.IsFocus) 
				return;
			gc.RgbFgColor = ColorStyle.Caret;
			if (Caret.IsInInsertMode) {
				win.DrawLine (gc, caretX, caretY, caretX, caretY + LineHeight);
			} else {
				win.DrawRectangle (gc, true, new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight));
				layout.SetText (caretChar.ToString ());
				gc.RgbFgColor = ColorStyle.CaretForeground;
				win.DrawLayout (gc, caretX, caretY, layout);
			}
		}
		
		void DrawLinePart (Gdk.Drawable win, LineSegment line, int offset, int length, ref int xPos, int y)
		{
			SyntaxMode mode = Document.SyntaxMode != null && TextEditorOptions.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			Chunk[] chunks = mode.GetChunks (Document, textEditor.ColorStyle, line, offset, length);
			int selectionStart = -1;
			int selectionEnd   = -1;
			if (textEditor.IsSomethingSelected) {
				ISegment segment = textEditor.SelectionRange;
				selectionStart = segment.Offset;
				selectionEnd   = segment.EndOffset;
			}
			int visibleColumn = 0;
			
			foreach (Chunk chunk in chunks) {
				if (chunk.Offset >= selectionStart && chunk.EndOffset <= selectionEnd) {
					DrawStyledText (win, line, true, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, chunk.EndOffset);
				} else if (chunk.Offset >= selectionStart && chunk.Offset < selectionEnd && chunk.EndOffset > selectionEnd) {
					DrawStyledText (win, line, true, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionEnd);
					DrawStyledText (win, line, false, chunk.Style, ref visibleColumn, ref xPos, y, selectionEnd, chunk.EndOffset);
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionStart && chunk.EndOffset <= selectionEnd) {
					DrawStyledText (win, line, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionStart);
					DrawStyledText (win, line, true, chunk.Style, ref visibleColumn, ref xPos, y, selectionStart, chunk.EndOffset);
				} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionEnd) {
					DrawStyledText (win, line, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, selectionStart);
					DrawStyledText (win, line, true, chunk.Style, ref visibleColumn, ref xPos, y, selectionStart, selectionEnd);
					DrawStyledText (win, line, false, chunk.Style, ref visibleColumn, ref xPos, y, selectionEnd, chunk.EndOffset);
				} else 
					DrawStyledText (win, line, false, chunk.Style, ref visibleColumn, ref xPos, y, chunk.Offset, chunk.EndOffset);
			}
			
			if (Caret.Offset == offset + length) 
				SetVisibleCaretPosition (win, ' ', xPos, y);
		}
		
		StringBuilder wordBuilder = new StringBuilder ();
		void OutputWordBuilder (Gdk.Drawable win, LineSegment line, bool selected, ChunkStyle style, ref int visibleColumn, ref int xPos, int y, int curOffset)
		{
			bool drawText = true;
			int oldxPos = xPos;
			int startOffset = curOffset - wordBuilder.Length;
				
			if (line.Markers != null) {
				foreach (TextMarker marker in line.Markers) 
					drawText &= marker.DrawBackground (textEditor, win, selected, startOffset, curOffset, y, oldxPos, xPos);
			}
			if (drawText) {
				string text = wordBuilder.ToString ();
				if (selected) {
					DrawText (win, text, ColorStyle.SelectedFg, ColorStyle.SelectedBg, ref xPos, y);
				} else {
					ISegment firstSearch;
					int offset = startOffset;
					int s;
					while ((firstSearch = GetFirstSearchResult (offset, curOffset)) != null) {
						// Draw text before the search result (if any)
						if (firstSearch.Offset > offset) {
							s = offset - startOffset;
							DrawText (win, text.Substring (s, firstSearch.Offset - offset), style.Color, defaultBgColor, ref xPos, y);
							offset += firstSearch.Offset - offset;
						}
						// Draw text within the search result
						s = offset - startOffset;
						int len = System.Math.Min (firstSearch.EndOffset - offset, text.Length - s);
						if (len > 0)
							DrawText (win, text.Substring (s, len), style.Color, ColorStyle.SearchTextBg, ref xPos, y);
						offset = System.Math.Max (firstSearch.EndOffset, offset + 1);
					}
					s = offset - startOffset;
					if (s < wordBuilder.Length) {
						DrawText (win, text.Substring (s, wordBuilder.Length - s), style.Color, defaultBgColor, ref xPos, y);
					}
				}
			}
			if (line.Markers != null) {
				foreach (TextMarker marker in line.Markers) {
					marker.Draw (textEditor, win, selected, startOffset, curOffset, y, oldxPos, xPos);
				}
			}
			visibleColumn += wordBuilder.Length;
			wordBuilder.Length = 0;
		}
		
		ISegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset) {
				ISegment region = new Segment (startOffset, endOffset - startOffset);
				foreach (ISegment segment in this.selectedRegions) {
					if (segment.Contains (startOffset) || segment.Contains (endOffset) || 
					    region.Contains (segment)) {
						return segment;
					}
				}
			}
			return null;
		}
		
		bool IsSearchResultAt (int offset)
		{
			foreach (ISegment segment in this.selectedRegions) {
				if (segment.Contains (offset))
					return true;
			}
			return false;
		}
		
		void DrawStyledText (Gdk.Drawable win, LineSegment line, bool selected, ChunkStyle style, ref int visibleColumn, ref int xPos, int y, int startOffset, int endOffset)
		{
			int caretOffset = Caret.Offset;
			int drawCaretAt = -1;
			wordBuilder.Length = 0;
			if (style.Bold)
				layout.FontDescription.Weight = Pango.Weight.Bold;
			if (style.Italic)
				layout.FontDescription.Style = Pango.Style.Italic;
			
			for (int offset = startOffset; offset < endOffset; offset++) {
				char ch = Document.GetCharAt (offset);
				if (TextEditorOptions.Options.HighlightMatchingBracket && offset == this.highlightBracketOffset && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0)) {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					
					bool drawText = true;
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) 
							drawText &= marker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					if (drawText) {
						Gdk.Rectangle bracketMatch = new Gdk.Rectangle (xPos, y, charWidth - 1, LineHeight - 1);
						gc.RgbFgColor = selected ? this.ColorStyle.SelectedBg : this.ColorStyle.BracketHighlightBg;
						win.DrawRectangle (gc, true, bracketMatch);
						gc.RgbFgColor = this.ColorStyle.BracketHighlightRectangle;
						win.DrawRectangle (gc, false, bracketMatch);
						
						layout.SetText (ch.ToString ());
						gc.RgbFgColor = selected ? ColorStyle.SelectedFg : style.Color;
						win.DrawLayout (gc, xPos, y, layout);
					}
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) {
							marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
						}
					}
					xPos += this.charWidth;
					visibleColumn++;
				} else if (ch == ' ') {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					bool drawText = true;
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) 
							drawText &= marker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					if (drawText) {
						DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, charWidth, LineHeight), selected ? ColorStyle.SelectedBg : (IsSearchResultAt (offset) ? ColorStyle.SearchTextBg : defaultBgColor));
						
						if (TextEditorOptions.Options.ShowSpaces) 
							DrawSpaceMarker (win, selected, xPos, y);
						if (offset == caretOffset) 
							SetVisibleCaretPosition (win, TextEditorOptions.Options.ShowSpaces ? spaceMarkerChar : ' ', xPos, y);
					}
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) {
							marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
						}
					}
					xPos += this.charWidth;
					visibleColumn++;
				} else if (ch == '\t') {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					
					int newColumn = GetNextTabstop (visibleColumn);
					int delta = (newColumn - visibleColumn) * this.charWidth;
					visibleColumn = newColumn;
					bool drawText = true;
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) 
							drawText &= marker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					if (drawText) {
						DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, delta, LineHeight), selected ? ColorStyle.SelectedBg : (IsSearchResultAt (offset) ? ColorStyle.SearchTextBg : defaultBgColor));
						if (TextEditorOptions.Options.ShowTabs) 
							DrawTabMarker (win, selected, xPos, y);
						if (offset == caretOffset) 
							SetVisibleCaretPosition (win, TextEditorOptions.Options.ShowSpaces ? tabMarkerChar : ' ', xPos, y);
					}
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) {
							marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + delta);
						}
					}
					xPos += delta;
				} else {
					if (offset == caretOffset) {
						layout.SetText (wordBuilder.ToString ());
						
						int width, height;
						layout.GetPixelSize (out width, out height);
						
						drawCaretAt = xPos + width;
					}
					wordBuilder.Append (ch);
				}
			}
			
			OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, endOffset);
			
			if (style.Bold)
				layout.FontDescription.Weight = Pango.Weight.Normal;
			if (style.Italic)
				layout.FontDescription.Style = Pango.Style.Normal;
			
			if (drawCaretAt >= 0)
				SetVisibleCaretPosition (win, Document.Contains (caretOffset) ? Document.GetCharAt (caretOffset) : ' ', drawCaretAt, y);
		}
		
		void DrawText (Gdk.Drawable win, string text, Gdk.Color foreColor, Gdk.Color backgroundColor, ref int xPos, int y)
		{
			layout.SetText (text);
			
			int width, height;
			layout.GetPixelSize (out width, out height);
			DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, width, height), backgroundColor);
			
			gc.RgbFgColor = foreColor;
			win.DrawLayout (gc, xPos, y, layout);
			xPos += width;
		}
		
		void DrawEolMarker (Gdk.Drawable win, bool selected, int xPos, int y)
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
		enum MouseSelectionMode {
			SingleChar,
			Word,
			WholeLine
		};
		MouseSelectionMode mouseSelectionMode = MouseSelectionMode.SingleChar;
		
		public override void MousePressed (int button, int x, int y, Gdk.EventType type, Gdk.ModifierType modifierState)
		{
			inSelectionDrag = false;
			inDrag = false;
			ISegment selection = textEditor.SelectionRange;
			int anchor         = textEditor.SelectionAnchor;
			int oldOffset      = textEditor.Caret.Offset;
			if (button == 1 || button == 2) {
				clickLocation = VisualToDocumentLocation (x, y);
				if (!textEditor.IsSomethingSelected) {
					textEditor.SelectionAnchorLocation = clickLocation;
				}
				
				int offset = Document.LocationToOffset (clickLocation);
				if (offset < 0) {
					textEditor.RunAction (new CaretMoveToDocumentEnd ());
					return;
				}
				if (button == 2 && selection != null && selection.Contains (offset)) {
					textEditor.ClearSelection ();
					return;
				}
					
				if (type == EventType.TwoButtonPress) {
					int start = ScanWord (offset, false);
					int end   = ScanWord (offset, true);
					Caret.Offset = end;
					textEditor.SelectionAnchor = start;
					textEditor.SelectionRange = new Segment (start, end - start);
					inSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.Word;
					return;
				} else if (type == EventType.ThreeButtonPress) {
					textEditor.SelectionRange = Document.GetLineByOffset (offset);
					inSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.WholeLine;
					return;
				}
				mouseSelectionMode = MouseSelectionMode.SingleChar;
				
				if (textEditor.IsSomethingSelected && textEditor.SelectionRange.Offset <= offset && offset < textEditor.SelectionRange.EndOffset && clickLocation != textEditor.Caret.Location) {
					inDrag = true;
				} else {
					inSelectionDrag = true;
					if ((modifierState & Gdk.ModifierType.ShiftMask) == ModifierType.ShiftMask) {
						Caret.PreserveSelection = true;
						if (!textEditor.IsSomethingSelected)
							textEditor.SelectionAnchor = Caret.Offset;
						Caret.Location = clickLocation;
						Caret.PreserveSelection = false;
						textEditor.ExtendSelectionTo (clickLocation);
					} else {
						textEditor.ClearSelection ();
						Caret.Location = clickLocation; 
					}
					this.caretBlink = false;
				}
			}
			if (button == 2)  {
				int length = PasteAction.PasteFromPrimary (textEditor.GetTextEditorData ());
				int newOffset = textEditor.Caret.Offset;
				if (selection != null) {
					if (newOffset < selection.EndOffset) {
						oldOffset += length;
						anchor   += length;
						selection = new Segment (selection.Offset + length, selection.Length);
					}
					textEditor.Caret.Offset = oldOffset;
					textEditor.SelectionAnchor = anchor;
					textEditor.SelectionRange  = selection;
				} else {
					textEditor.Caret.Offset = oldOffset;
				}
			}
		}
		
		public override void MouseReleased (int button, int x, int y, ModifierType modifierState)
		{
			if (inDrag) 
				Caret.Location = clickLocation;
			if (!inSelectionDrag)
				textEditor.ClearSelection ();
			inSelectionDrag = false;
			base.MouseReleased (button, x, y, modifierState);
		}
		
		
		int ScanWord (int offset, bool forwardDirection)
		{
			if (offset < 0 || offset >= Document.Length)
				return offset;
			LineSegment line = Document.GetLineByOffset (offset);
			char first = Document.GetCharAt (offset);
			while (offset >= line.Offset && offset < line.Offset + line.EditableLength) {
				char ch = Document.GetCharAt (offset);
				if (char.IsWhiteSpace (first) && !char.IsWhiteSpace (ch) ||
				    char.IsPunctuation (first) && !char.IsPunctuation (ch) ||
				    (char.IsLetterOrDigit (first) || first == '_') && !(char.IsLetterOrDigit (ch) || ch == '_'))
				    break;
				
				offset = forwardDirection ? offset + 1 : offset - 1; 
			}
//			while (offset >= line.Offset && offset < line.Offset + line.EditableLength && (char.IsLetterOrDigit (Document.GetCharAt (offset)) || Document.GetCharAt (offset) == '_')) {
//				offset = forwardDirection ? offset + 1 : offset - 1; 
//			}
			return System.Math.Min (line.EndOffset - 1, System.Math.Max (line.Offset, offset + (forwardDirection ? 0 : 1)));
		}
		
		CodeSegmentPreviewWindow previewWindow = null;
		ISegment previewSegment = null;
		void ShowTooltip (ISegment segment, Rectangle hintRectangle)
		{
			if (previewSegment == segment)
				return;
			if (previewWindow != null) {
				previewWindow.Destroy ();
				previewWindow = null;
			}
			previewSegment = segment;
			if (segment == null) {
				return;
			}
			previewWindow = new CodeSegmentPreviewWindow (this.textEditor, segment);
			int ox = 0, oy = 0;
			this.textEditor.GdkWindow.GetOrigin (out ox, out oy);
			
			int x = hintRectangle.Right;
			int y = hintRectangle.Bottom;
			int w = previewWindow.SizeRequest ().Width;
			int h = previewWindow.SizeRequest ().Height;
			if (x + ox + w > this.textEditor.GdkWindow.Screen.Width) 
				x = hintRectangle.Left - w;
			if (y + oy + h > this.textEditor.GdkWindow.Screen.Height) 
				y = hintRectangle.Top - h;
			previewWindow.Move (ox + x, oy + y);
			previewWindow.ShowAll ();
		}
		
		public override void MouseHover (int x, int y, bool buttonPressed)
		{
			if (!buttonPressed) {
				int lineNr = Document.VisualToLogicalLine ((int)((y + textEditor.VAdjustment.Value) / this.LineHeight));
				foreach (KeyValuePair<Rectangle, FoldSegment> shownFolding in GetFoldRectangles (lineNr)) {
					if (shownFolding.Key.Contains (x + this.XOffset, y)) {
						ShowTooltip (shownFolding.Value, shownFolding.Key);
						return;
					}
				}
				ShowTooltip (null, Gdk.Rectangle.Zero);
				return;
			}
			if (inSelectionDrag) {
				DocumentLocation loc = VisualToDocumentLocation (x, y);
				Caret.PreserveSelection = true;
				switch (this.mouseSelectionMode) {
				case MouseSelectionMode.SingleChar:
					textEditor.ExtendSelectionTo (loc);
					Caret.Location = loc;
					break;
				case MouseSelectionMode.Word:
					int offset = textEditor.Document.LocationToOffset (loc);
					int start;
					int end;
					if (offset < textEditor.SelectionAnchor) {
						start = ScanWord (offset, false);
						end   = ScanWord (textEditor.SelectionAnchor, true);
						Caret.Offset = start;
					} else {
						start = ScanWord (textEditor.SelectionAnchor, false);
						end   = ScanWord (offset, true);
						Caret.Offset = end;
					}
					textEditor.SelectionRange = new Segment (start, end - start);
					break;
				case MouseSelectionMode.WholeLine:
					textEditor.SetSelectLines (loc.Line, textEditor.SelectionAnchorLocation.Line);
					LineSegment line1 = textEditor.Document.GetLine (loc.Line);
					LineSegment line2 = textEditor.Document.GetLineByOffset (textEditor.SelectionAnchor);
					Caret.Offset = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffset;
					break;
				}
				Caret.PreserveSelection = false;
//				textEditor.RedrawLines (System.Math.Min (oldLine, Caret.Line), System.Math.Max (oldLine, Caret.Line));
			}
		}
		
		public Gdk.Point LocationToDisplayCoordinates (DocumentLocation loc)
		{
			LineSegment line = Document.GetLine (loc.Line);
			if (line == null)
				return Gdk.Point.Zero;
			int x = ColumnToVisualX (line, loc.Column) + this.XOffset;
			int y = Document.LogicalToVisualLine (loc.Line) * this.LineHeight;
			return new Gdk.Point (x - (int)this.textEditor.HAdjustment.Value, 
			                      y - (int)this.textEditor.VAdjustment.Value);
		}
		
		public int ColumnToVisualX (LineSegment line, int column)
		{
			if (line == null || line.EditableLength == 0)
				return 0;
			
			int lineXPos  = 0;
			
			int visibleColumn = 0;
			for (int curColumn = 0; curColumn < column && curColumn < line.EditableLength; curColumn++) {
				int delta;
				if (this.Document.GetCharAt (line.Offset + curColumn) == '\t') {
					int newColumn = GetNextTabstop (visibleColumn);
					delta = (newColumn - visibleColumn) * this.charWidth;
					visibleColumn = newColumn;
				} else {
					delta = this.charWidth;
					visibleColumn++;
				}
				lineXPos += delta;
			}
			if (column >= line.EditableLength)
				lineXPos += (line.EditableLength - column + 1) * this.charWidth;
			return lineXPos;
		}
		
		public static int GetNextTabstop (int currentColumn)
		{
			int result = currentColumn + TextEditorOptions.Options.TabSize;
			return (result / TextEditorOptions.Options.TabSize) * TextEditorOptions.Options.TabSize;
		}
		
		internal int rulerX = 0;
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
		
		List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> GetFoldRectangles (int lineNr)
		{
			List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> result = new List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> ();
			layout.Alignment = Pango.Alignment.Left;
			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
			int xStart = XOffset;
			int y      = (int)(Document.LogicalToVisualLine (lineNr) * LineHeight - textEditor.VAdjustment.Value);
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = (int)(XOffset - textEditor.HAdjustment.Value);
			
			if (line == null) {
				return result;
			}
			
			List<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			for (int i = 0; i < foldings.Count; ++i) {
				FoldSegment folding = foldings[i];
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
					layout.SetText (Document.GetTextAt (offset, foldOffset - offset).Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize)));
					layout.GetPixelSize (out width, out height);
					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, this.LineHeight - 1);
					result.Add (new KeyValuePair<Rectangle, FoldSegment> (foldingRectangle, folding));
					xPos += width;
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						i = -1;
					}
				}
			}
			return result;
		}
		
		List<ISegment> selectedRegions = new List<ISegment> ();
		Gdk.Color      defaultBgColor;
		
		public override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
			int visibleLine = y / this.LineHeight;
			this.caretX = -1;
			layout.Alignment = Pango.Alignment.Left;
			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);
			gc.ClipRectangle = new Gdk.Rectangle (xStart, y, area.Right - xStart, LineHeight);
			
			if (TextEditorOptions.Options.HighlightCaretLine && Caret.Line == lineNr) {
				defaultBgColor = ColorStyle.LineMarker;
			} else {
				defaultBgColor = ColorStyle.Background;
			}
				
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = (int)(x - textEditor.HAdjustment.Value);
			
			if (line == null) {
				DrawRectangleWithRuler (win, x, lineArea, defaultBgColor);
				if (TextEditorOptions.Options.ShowInvalidLines) {
					DrawInvalidLineMarker (win, xPos, y);
				}
				if (TextEditorOptions.Options.ShowRuler) { // warning: code duplication, look at the method end.
					gc.RgbFgColor = ColorStyle.Ruler;
					win.DrawLine (gc, x + rulerX, y, x + rulerX, y + LineHeight); 
				}
				return;
			}
			selectedRegions.Clear ();
			if (textEditor.HighlightSearchPattern) {
				for (int i = line.Offset; i < line.EndOffset; i++) {
					if (this.textEditor.GetTextEditorData ().IsMatchAt (i))
						selectedRegions.Add (new Segment (i, textEditor.SearchPattern.Length));
				}
			}
			
			List<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			for (int i = 0; i < foldings.Count; ++i) {
				FoldSegment folding = foldings[i];
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
//					layout.SetText (Document.GetTextAt (offset, foldOffset - offset));
//					gc.RgbFgColor = ColorStyle.FoldLine;
//					win.DrawLayout (gc, xPos, y, layout);
//					layout.GetPixelSize (out width, out height);
					
					DrawLinePart (win, line, offset, foldOffset - offset, ref xPos, y);
//					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					bool isFoldingSelected = textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding);
					gc.RgbFgColor = isFoldingSelected ? ColorStyle.SelectedBg : defaultBgColor;
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, this.LineHeight - 1);
					win.DrawRectangle (gc, true, foldingRectangle);
					gc.RgbFgColor = isFoldingSelected ? ColorStyle.SelectedFg : ColorStyle.FoldLine;
					win.DrawRectangle (gc, false, foldingRectangle);
					
					gc.RgbFgColor = isFoldingSelected ? ColorStyle.SelectedFg : ColorStyle.FoldLine;
					win.DrawLayout (gc, xPos, y, layout);
					if (caretOffset == foldOffset)
						SetVisibleCaretPosition (win, folding.Description[0], xPos, y);
					
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
				DrawLinePart (win, line, offset, line.Offset + line.EditableLength - offset, ref xPos, y);
			
			bool isEolSelected = textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength);
			
			lineArea.X     = xPos;
			lineArea.Width = textEditor.Allocation.Width - xPos;
			DrawRectangleWithRuler (win, x, lineArea, isEolSelected ? this.ColorStyle.SelectedBg : defaultBgColor);
			
			if (TextEditorOptions.Options.ShowEolMarkers)
				DrawEolMarker (win, isEolSelected, xPos, y);
			
			if (TextEditorOptions.Options.ShowRuler) { // warning: code duplication, scroll up.
				gc.RgbFgColor = ColorStyle.Ruler;
				win.DrawLine (gc, x + rulerX, y, x + rulerX, y + LineHeight); 
			}
			
			if (caretOffset == line.Offset + line.EditableLength)
				SetVisibleCaretPosition (win, TextEditorOptions.Options.ShowEolMarkers ? eolMarkerChar : ' ', xPos, y);
			
			if (this.caretX >= 0 && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0))
				this.DrawCaret (win);
		}
		
		public override void MouseLeft ()
		{
			ShowTooltip (null, Gdk.Rectangle.Zero);
		}
		
		public DocumentLocation VisualToDocumentLocation (int xp, int yp)
		{
			int lineNumber = System.Math.Min (Document.VisualToLogicalLine ((int)(yp + textEditor.VAdjustment.Value) / LineHeight), Document.LineCount - 1);
			
			layout.Alignment = Pango.Alignment.Left;
			LineSegment line = lineNumber < Document.LineCount ? Document.GetLine (lineNumber) : null;
			int xStart = XOffset;
			int y      = (int)(Document.LogicalToVisualLine (lineNumber) * LineHeight - textEditor.VAdjustment.Value);
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = 0;
			int column = 0;
			int visibleColumn = 0;
			if (line == null) 
				return DocumentLocation.Empty;
			
			List<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			int index, trailing;
			int visualXPos = xp + (int)textEditor.HAdjustment.Value;
			for (int i = 0; i < foldings.Count; ++i) {
				FoldSegment folding = foldings[i];
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
					for (int o = offset; o < foldOffset; o++) {
						char ch = Document.GetCharAt (o);
						int delta;
						System.Console.WriteLine(ch);
						if (ch == '\t') {
							int newColumn = GetNextTabstop (visibleColumn);
							delta = (newColumn - visibleColumn) * this.charWidth;
							visibleColumn = newColumn;
						} else {
							layout.SetText (ch.ToString ());
							layout.GetPixelSize (out delta, out height);
							visibleColumn++;
						}
						int nextXPosition = xPos + delta;
						if (nextXPosition >= visualXPos) {
							if (!IsNearX1 (visualXPos, xPos, nextXPosition))
								column++;
							break;
						}
						column++;
						xPos = nextXPosition;
					}
					
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					xPos += width;
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						i = -1;
					}
				}
				
//				i1 (!IsNearX1 (xp, xPos, nextXPosition))
//					column++;
			}

			if (line.EndOffset - offset > 0) {
				for (int o = offset; o < line.Offset + line.EditableLength; o++) {
					char ch = Document.GetCharAt (o);
					int delta;
					if (ch == '\t') {
						int newColumn = GetNextTabstop (visibleColumn);
						delta = (newColumn - visibleColumn) * this.charWidth;
						visibleColumn = newColumn;
					} else {
						layout.SetText (ch.ToString ());
						layout.GetPixelSize (out delta, out height);
						visibleColumn++;
					}
					int nextXPosition = xPos + delta;
					if (nextXPosition >= visualXPos) {
						if (!IsNearX1 (visualXPos, xPos, nextXPosition))
							column++;
						break;
					}
					column++;
					xPos = nextXPosition;
				}
			}
			
			return new DocumentLocation (lineNumber, column);
//			int lineNumber = System.Math.Min (Document.VisualToLogicalLine ((int)(y + textEditor.VAdjustment.Value) / LineHeight), Document.LineCount - 1);
//			LineSegment line = Document.GetLine (lineNumber);
//			int lineXPos  = 0;
//			int column;
//			int visibleColumn = 0;
//			int visualXPos = x + (int)textEditor.HAdjustment.Value;
//			for (column = 0; column < line.EditableLength; column++) {
//				int delta;
//				if (this.Document.GetCharAt (line.Offset + column) == '\t') {
//					int newColumn = GetNextTabstop (visibleColumn);
//					delta = (newColumn - visibleColumn) * this.charWidth;
//					visibleColumn = newColumn;
//				} else {
//					delta = this.charWidth;
//					visibleColumn++;
//				}
//				int nextXPosition = lineXPos + delta;
//				if (nextXPosition >= visualXPos) {
//					if (!IsNearX1 (visualXPos, lineXPos, nextXPosition))
//						column++;
//					break;
//				}
//				lineXPos = nextXPosition;
//			}			
//			return new DocumentLocation (lineNumber, column);
		}
		
		static bool IsNearX1 (int pos, int x1, int x2)
		{
			return System.Math.Abs (x1 - pos) < System.Math.Abs (x2 - pos);
		}
	}
}
