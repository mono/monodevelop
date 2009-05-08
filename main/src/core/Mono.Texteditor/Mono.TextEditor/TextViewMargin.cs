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
using System.Timers;


namespace Mono.TextEditor
{
	public class TextViewMargin : Margin
	{
		TextEditor textEditor;
		Pango.Layout tabMarker, spaceMarker, eolMarker, invalidLineMarker;
		Pango.Layout layout;
		
		int charWidth;
		
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
			base.cursor = xtermCursor;
			Document.LineChanged += CheckLongestLine;
			textEditor.HighlightSearchPatternChanged += delegate {
				selectedRegions.Clear ();
			};
			textEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs e) {
				if (selectedRegions.Count == 0)
					return;
				List<ISegment> newRegions = new List<ISegment> (this.selectedRegions);
				Document.UpdateSegments (newRegions, e);
				
				if (searchPatternWorker == null || !searchPatternWorker.IsBusy) {
					this.selectedRegions = newRegions;
					HandleSearchChanged (this, EventArgs.Empty);
				}
			};
			
			textEditor.GetTextEditorData ().SearchChanged += HandleSearchChanged;
		}
		
		void HandleSearchChanged (object sender, EventArgs args)
		{
			if (textEditor.HighlightSearchPattern) {
				if (searchPatternWorker != null && searchPatternWorker.IsBusy) 
					searchPatternWorker.CancelAsync ();
				if (string.IsNullOrEmpty (this.textEditor.SearchPattern)) {
					selectedRegions.Clear ();
					return;
				}
				searchPatternWorker = new System.ComponentModel.BackgroundWorker ();
				searchPatternWorker.WorkerSupportsCancellation = true;
				searchPatternWorker.DoWork += delegate(object s, System.ComponentModel.DoWorkEventArgs e) {
					System.ComponentModel.BackgroundWorker worker = (System.ComponentModel.BackgroundWorker)s;
					List<ISegment> newRegions = new List<ISegment> ();
					int offset = 0;
					do {
						if (worker.CancellationPending)
							return;
						SearchResult result = null;
						try {
							result = this.textEditor.GetTextEditorData ().SearchEngine.SearchForward (offset);
						} catch (Exception ex) {
							Console.WriteLine ("Got exception while search forward:" + ex);
							break;
						}
						if (result == null || result.SearchWrapped)
							break;
						offset = result.EndOffset;
						newRegions.Add (result);
					} while (true);
					this.selectedRegions = newRegions;
				};
				searchPatternWorker.RunWorkerAsync ();
			}
		}
		
		System.ComponentModel.BackgroundWorker searchPatternWorker;
		
		Gdk.Cursor xtermCursor = new Gdk.Cursor (Gdk.CursorType.Xterm);
		Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
		internal void Initialize ()
		{
			foreach (LineSegment line in Document.Lines) 
				CheckLongestLine (this, new LineEventArgs (line));
		}
		
		void CheckLongestLine (object sender, LineEventArgs args)
		{
			if (textEditor.longestLine == null || args.Line.EditableLength > textEditor.longestLine.EditableLength) {
				textEditor.longestLine = args.Line;
				textEditor.SetAdjustments (textEditor.Allocation);
			}
		}
		
		void CaretPositionChanged (object sender, DocumentLocationEventArgs args) 
		{
			if (Caret.AutoScrollToCaret) {
				textEditor.ScrollToCaret ();
				if (args.Location.Line != Caret.Line) {
					caretBlink = false;
					if (!textEditor.IsSomethingSelected)
						textEditor.RedrawLine (args.Location.Line);
				}
				caretBlink = true;
				if (!textEditor.IsSomethingSelected)
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
		
		internal protected override void OptionsChanged ()
		{
			DisposeGCs ();
			
			tabMarker.FontDescription = 
			spaceMarker.FontDescription = 
			eolMarker.FontDescription = 
			invalidLineMarker.FontDescription = 
			layout.FontDescription = textEditor.Options.Font;
			
			layout.SetText (" ");
			layout.GetPixelSize (out this.charWidth, out this.lineHeight);
			lineHeight = System.Math.Max (1, lineHeight);
		}
		
		void DisposeGCs ()
		{
			ShowTooltip (null, Gdk.Rectangle.Zero);
			//gc = gc.Kill ();
			foreach (Gdk.GC gc in gcDictionary.Values) {
				gc.Kill ();
			}
			gcDictionary.Clear ();
		}
		//Gdk.GC gc = null;
		Dictionary<ulong, Gdk.GC> gcDictionary = new Dictionary<ulong, Gdk.GC> ();
		Gdk.GC GetGC (Color color)
		{
			/*if (gc == null)
				gc = new Gdk.GC (textEditor.GdkWindow);
			gc.RgbFgColor = color;
			return gc;*/
			Gdk.GC result = null;
			// color.Pixel doesn't work
			ulong colorId = (ulong)color.Red * (1 << 32) + (ulong)color.Blue * (1 << 16) + (ulong)color.Green;
			if (gcDictionary.TryGetValue (colorId, out result)) {
				result.ClipRectangle = clipRectangle;
				return result;
			}
			result = new Gdk.GC (textEditor.GdkWindow);
			result.RgbFgColor = color;
			result.ClipRectangle = clipRectangle;
			gcDictionary.Add (colorId, result);
			return result;
		}
		
		public override void Dispose ()
		{
			if (arrowCursor == null)
				return;
			
			caretTimer.Stop ();
			caretTimer.Dispose ();
			
			Caret.PositionChanged -= CaretPositionChanged;
			textEditor.Document.TextReplaced -= UpdateBracketHighlighting;
			Caret.PositionChanged -= UpdateBracketHighlighting;
			Document.LineChanged -= CheckLongestLine;
	//		Document.LineInserted -= CheckLongestLine;
			
			arrowCursor.Dispose ();
			xtermCursor.Dispose ();
			arrowCursor = xtermCursor = null;
			
			DisposeGCs ();
			layout = layout.Kill ();
			tabMarker = tabMarker.Kill ();
			spaceMarker = spaceMarker.Kill ();
			eolMarker = eolMarker.Kill ();
			invalidLineMarker = invalidLineMarker.Kill ();
			base.Dispose ();
		}
		
		#region Caret blinking
		Timer caretTimer = null;
		bool caretBlink = true;
		int caretBlinkStatus;
		
		public void ResetCaretBlink ()
		{
			if (caretTimer == null) {
				caretTimer = new Timer ();
				caretTimer.Elapsed += UpdateCaret;
				caretTimer.Interval = (uint)Gtk.Settings.Default.CursorBlinkTime / 2;
			} else {
				caretTimer.Stop ();
			}
			caretTimer.Start();
		}
		
		void UpdateCaret (object sender, EventArgs args)
		{
			bool newCaretBlink = caretBlinkStatus < 4 || (caretBlinkStatus - 4) % 3 != 0;
			if (layout != null && newCaretBlink != caretBlink) {
				caretBlink = newCaretBlink;
				Application.Invoke (delegate {
					try {
						Document.CommitLineUpdate (Caret.Line);
					} catch (Exception) {
					}
				});
			}
			caretBlinkStatus++;
		}
		#endregion
		
		char caretChar;
		internal int  caretX;
		int  caretY;
		
		void SetVisibleCaretPosition (Gdk.Drawable win, char ch, int x, int y)
		{
			caretChar = ch;
			caretX    = x;
			caretY    = y;
		}
		
		public void DrawCaret (Gdk.Drawable win)
		{
			if (!this.textEditor.IsInDrag) {
				if (!(this.caretX >= 0 && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0)))
					return;
				if (!textEditor.HasFocus)
					return;
			}
			if (Settings.Default.CursorBlink && (!Caret.IsVisible || !caretBlink)) 
				return;
			
			if (Caret.IsInInsertMode) {
				if (caretX < this.XOffset)
					return;
				clipRectangle = new Gdk.Rectangle (caretX, caretY, 1, LineHeight);
				win.DrawLine (GetGC (ColorStyle.Caret.Color), caretX, caretY, caretX, caretY + LineHeight);
			} else {
				if (caretX + this.charWidth < this.XOffset)
					return;
				clipRectangle = new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight);
				win.DrawRectangle (GetGC (ColorStyle.Caret.Color), true, new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight));
				layout.SetText (caretChar.ToString ());
				win.DrawLayout (GetGC (ColorStyle.Caret.BackgroundColor), caretX, caretY, layout);
			}
		}
		
		void DrawChunkPart (Gdk.Drawable win, LineSegment line, Chunk chunk, ref int visibleColumn, ref int xPos, int y, int startOffset, int endOffset, int selectionStart, int selectionEnd)
		{
			if (startOffset >= selectionStart && endOffset <= selectionEnd) {
				DrawStyledText (win, line, true, chunk, ref visibleColumn, ref xPos, y, startOffset, endOffset);
			} else if (startOffset >= selectionStart && startOffset < selectionEnd && endOffset > selectionEnd) {
				DrawStyledText (win, line, true, chunk, ref visibleColumn, ref xPos, y, startOffset, selectionEnd);
				DrawStyledText (win, line, false, chunk, ref visibleColumn, ref xPos, y, selectionEnd, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionStart && endOffset <= selectionEnd) {
				DrawStyledText (win, line, false, chunk, ref visibleColumn, ref xPos, y, startOffset, selectionStart);
				DrawStyledText (win, line, true, chunk, ref visibleColumn, ref xPos, y, selectionStart, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionEnd) {
				DrawStyledText (win, line, false, chunk, ref visibleColumn, ref xPos, y, startOffset, selectionStart);
				DrawStyledText (win, line, true, chunk, ref visibleColumn, ref xPos, y, selectionStart, selectionEnd);
				DrawStyledText (win, line, false, chunk, ref visibleColumn, ref xPos, y, selectionEnd, endOffset);
			} else {
				DrawStyledText (win, line, false, chunk, ref visibleColumn, ref xPos, y, startOffset, endOffset);
			}
		}
		
		void DrawPreeditString (Gdk.Drawable win, string style, ref int xPos, int y)
		{
			Pango.Layout preEditLayout = new Pango.Layout (textEditor.PangoContext);
			preEditLayout.Attributes = textEditor.preeditAttrs;
			preEditLayout.SetText (textEditor.preeditString);
			ChunkStyle chunkStyle = ColorStyle.GetChunkStyle (style);
			int cWidth, cHeight;
			preEditLayout.GetPixelSize (out cWidth, out cHeight);
			DrawRectangleWithRuler (win, xPos, new Gdk.Rectangle (xPos, y, cWidth, cHeight), GetBackgroundColor (textEditor.preeditOffset, false, chunkStyle));
			win.DrawLayout (GetGC (chunkStyle.Color), xPos, y, preEditLayout);
			
			xPos += cWidth;
			preEditLayout.Dispose ();
		}
		
		void DrawLinePart (Gdk.Drawable win, LineSegment line, int offset, int length, ref int xPos, int y, int maxX)
		{
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			
			int selectionStart = -1;
			int selectionEnd   = -1;
			if (textEditor.IsSomethingSelected) {
				ISegment segment = textEditor.SelectionRange;
				selectionStart = segment.Offset;
				selectionEnd   = segment.EndOffset;
				
				if (textEditor.SelectionMode == SelectionMode.Block) {
					DocumentLocation start = textEditor.MainSelection.Anchor;
					DocumentLocation end   = textEditor.MainSelection.Lead;
					
					DocumentLocation visStart = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), start);
					DocumentLocation visEnd   = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), end);
					
					if (segment.Contains (line.Offset) || segment.Contains (line.EndOffset)) {
						
						selectionStart = line.Offset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), Document, System.Math.Min (visStart.Column, visEnd.Column));
						selectionEnd   = line.Offset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), Document, System.Math.Max (visStart.Column, visEnd.Column));
					}
				} 
			}
			int visibleColumn = 0;
			string curStyle = "text";
			for (Chunk chunk = mode.GetChunks (Document, textEditor.ColorStyle, line, offset, length); chunk != null; chunk = chunk.Next) {
				if (xPos >= maxX)
					break;
				curStyle = chunk.Style;
				if (chunk.Contains (textEditor.preeditOffset)) {
					DrawChunkPart (win, line, chunk, ref visibleColumn, ref xPos, y, chunk.Offset, textEditor.preeditOffset, selectionStart, selectionEnd);
					DrawPreeditString (win, curStyle, ref xPos, y);
					DrawChunkPart (win, line, chunk, ref visibleColumn, ref xPos, y, textEditor.preeditOffset, chunk.EndOffset, selectionStart, selectionEnd);
				} else {
					DrawChunkPart (win, line, chunk, ref visibleColumn, ref xPos, y, chunk.Offset, chunk.EndOffset, selectionStart, selectionEnd);
				}
			}
			if (textEditor.preeditOffset == offset + length) 
				DrawPreeditString (win, curStyle, ref xPos, y);
		//	if (Caret.Offset == offset + length) 
		//		SetVisibleCaretPosition (win, ' ', xPos, y);
		}
		
		StringBuilder wordBuilder = new StringBuilder ();
		void OutputWordBuilder (Gdk.Drawable win, LineSegment line, bool selected, ChunkStyle style, ref int visibleColumn, ref int xPos, int y, int curOffset)
		{
			bool drawText = true;
			bool drawBg   = true;
			int oldxPos = xPos;
			int startOffset = curOffset - wordBuilder.Length;
			foreach (TextMarker marker in line.Markers)  {
				IBackgroundMarker bgMarker = marker as IBackgroundMarker;
				if (bgMarker == null) 
					continue;
				drawText &= bgMarker.DrawBackground (textEditor, win, selected, startOffset, curOffset, y, oldxPos, oldxPos + xPos, ref drawBg);
			}
			if (drawText) {
				string text = wordBuilder.ToString ();
				if (selected) {
					DrawText (win, text, ColorStyle.Selection.Color, drawBg, ColorStyle.Selection.BackgroundColor, ref xPos, y);
				} else {
					ISegment firstSearch;
					int offset = startOffset;
					int s;
					while ((firstSearch = GetFirstSearchResult (offset, curOffset)) != null) {
						// Draw text before the search result (if any)
						if (firstSearch.Offset > offset) {
							s = offset - startOffset;
							Gdk.Color bgc = UseDefaultBackgroundColor (style) ? defaultBgColor : style.BackgroundColor;
							DrawText (win, text.Substring (s, firstSearch.Offset - offset), style.Color, drawBg, bgc, ref xPos, y);
							offset += firstSearch.Offset - offset;
						}
						// Draw text within the search result
						s = offset - startOffset;
						int len = System.Math.Min (firstSearch.EndOffset - offset, text.Length - s);
						if (len > 0)
							DrawText (win, text.Substring (s, len), style.Color, drawBg, ColorStyle.SearchTextBg, ref xPos, y);
						offset = System.Math.Max (firstSearch.EndOffset, offset + 1);
					}
					s = offset - startOffset;
					if (s < wordBuilder.Length) {
						Gdk.Color bgc = UseDefaultBackgroundColor (style) ? defaultBgColor : style.BackgroundColor;
						DrawText (win, s > 0 ? text.Substring (s, wordBuilder.Length - s) : text, style.Color, drawBg, bgc, ref xPos, y);
					}
				}
			}
			foreach (TextMarker marker in line.Markers) {
				marker.Draw (textEditor, win, selected, startOffset, curOffset, y, oldxPos, xPos);
			}
			visibleColumn += wordBuilder.Length;
			wordBuilder.Length = 0;
		}
		
		ISegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset && this.selectedRegions.Count > 0) {
				ISegment region = new Segment (startOffset, endOffset - startOffset);
				foreach (ISegment segment in this.selectedRegions) {
					if (segment.Contains (startOffset) || 
					    segment.Contains (endOffset) ||
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
		Pango.Weight DefaultWeight {
			get {
				return textEditor.Options.Font.Weight;
			}
		}
		Pango.Style DefaultStyle {
			get {
				return textEditor.Options.Font.Style;
			}
		}

		void DrawStyledText (Gdk.Drawable win, LineSegment line, bool selected, Chunk chunk, ref int visibleColumn, ref int xPos, int y, int startOffset, int endOffset)
		{
			int caretOffset = Caret.Offset;
			int drawCaretAt = -1;
			wordBuilder.Length = 0;
			ChunkStyle style = chunk.GetChunkStyle (ColorStyle);
			
			foreach (TextMarker marker in line.Markers)
				style = marker.GetStyle (style);
			UnderlineMarker underlineMarker = null;
			if (style.Underline) {
				underlineMarker = new UnderlineMarker (selected ? this.ColorStyle.Selection.Color : style.Color, 
				                                       chunk.Offset - line.Offset, 
				                                       chunk.EndOffset - line.Offset);
				line.AddMarker (underlineMarker);
			}
			
			Pango.Weight requestedWeight = style.GetWeight (DefaultWeight);
			if (layout.FontDescription.Weight != requestedWeight)
				layout.FontDescription.Weight = requestedWeight;
			
			Pango.Style requestedStyle = style.GetStyle (DefaultStyle);
			if (layout.FontDescription.Style != requestedStyle)
				layout.FontDescription.Style = requestedStyle;
				
			for (int offset = startOffset; offset < endOffset; offset++) {
				char ch = chunk.GetCharAt (Document, offset);
				
				if (textEditor.Options.HighlightMatchingBracket && offset == this.highlightBracketOffset && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0)) {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					
					bool drawText = true;
					bool drawBg   = true;
					foreach (TextMarker marker in line.Markers)  {
						IBackgroundMarker bgMarker = marker as IBackgroundMarker;
						if (bgMarker == null) 
							continue;
						drawText &= bgMarker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth, ref drawBg);
					}
					int width = this.charWidth;
					if (drawText) {
						layout.SetText (ch.ToString ());
						int cWidth, cHeight;
						layout.GetPixelSize (out cWidth, out cHeight);
						width = cWidth;
						if (drawBg) {
							Gdk.Rectangle bracketMatch = new Gdk.Rectangle (xPos, y, cWidth - 1, cHeight - 1);
							win.DrawRectangle (GetGC (selected ? this.ColorStyle.Selection.BackgroundColor : this.ColorStyle.BracketHighlightRectangle.BackgroundColor), true, bracketMatch);
							win.DrawRectangle (GetGC (this.ColorStyle.BracketHighlightRectangle.Color), false, bracketMatch);
						}
						win.DrawLayout (GetGC (selected ? ColorStyle.Selection.Color : style.Color), xPos, y, layout);
					}
					foreach (TextMarker marker in line.Markers) {
						marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					xPos += width;
					visibleColumn++;
				} else if (ch == ' ') {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					bool drawText = true;
					bool drawBg   = true;
					foreach (TextMarker marker in line.Markers)  {
						IBackgroundMarker bgMarker = marker as IBackgroundMarker;
						if (bgMarker == null) 
							continue;
						drawText &= bgMarker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth, ref drawBg);
					}
					if (drawText) {
						if (drawBg) {
							Gdk.Color bgc = GetBackgroundColor (offset, selected, style);
							DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, charWidth, LineHeight), bgc);
						}
						
						if (textEditor.Options.ShowSpaces) 
							DrawSpaceMarker (win, selected, xPos, y);
						if (offset == caretOffset) 
							SetVisibleCaretPosition (win, textEditor.Options.ShowSpaces ? spaceMarkerChar : ' ', xPos, y);
					}
					foreach (TextMarker marker in line.Markers) {
						marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					xPos += this.charWidth;
					visibleColumn++;
				} else if (ch == '\t') {
					OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, offset);
					int newColumn = GetNextTabstop (this.textEditor.GetTextEditorData (), visibleColumn);
					int delta = (newColumn - visibleColumn) * CharWidth;
					visibleColumn = newColumn;
					bool drawText = true;
					bool drawBg   = true;
					foreach (TextMarker marker in line.Markers)  {
						IBackgroundMarker bgMarker = marker as IBackgroundMarker;
						if (bgMarker == null) 
							continue;
						drawText &= bgMarker.DrawBackground (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth, ref drawBg);
					}
					if (drawText) {
						if (drawBg)
							DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, delta, LineHeight), GetBackgroundColor (offset, selected, style));
						if (textEditor.Options.ShowTabs) 
							DrawTabMarker (win, selected, xPos, y);
						if (offset == caretOffset) 
							SetVisibleCaretPosition (win, textEditor.Options.ShowSpaces ? tabMarkerChar : ' ', xPos, y);
					}
					foreach (TextMarker marker in line.Markers) {
						marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + delta);
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
			
			if (DefaultWeight != requestedWeight)
				layout.FontDescription.Weight = DefaultWeight;
			if (DefaultStyle != requestedStyle)
				layout.FontDescription.Style = DefaultStyle;
			if (style.Underline) 
				line.RemoveMarker (underlineMarker);
						
			if (drawCaretAt >= 0)
				SetVisibleCaretPosition (win, Document.Contains (caretOffset) ? Document.GetCharAt (caretOffset) : ' ', drawCaretAt, y);
		}
		
		void DrawText (Gdk.Drawable win, string text, Gdk.Color foreColor, bool drawBg, Gdk.Color backgroundColor, ref int xPos, int y)
		{
			layout.SetText (text);
			
			int width, height;
			layout.GetPixelSize (out width, out height);
			if (drawBg) 
				DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, width, height), backgroundColor);
			
			win.DrawLayout (GetGC (foreColor), xPos, y, layout);
			xPos += width;
		}
		
		void DrawEolMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			win.DrawLayout (GetGC (selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker), xPos, y, eolMarker);
		}
		
		void DrawSpaceMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			win.DrawLayout (GetGC (selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker), xPos, y, spaceMarker);
		}
		
		void DrawTabMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			win.DrawLayout (GetGC (selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker), xPos, y, tabMarker);
		}
		
		void DrawInvalidLineMarker (Gdk.Drawable win, int x, int y)
		{
			win.DrawLayout (GetGC (ColorStyle.InvalidLineMarker), x, y, invalidLineMarker);
		}
		internal static ulong GetPixel (Color color)
		{
			return (((ulong)color.Red) << 32) |
					(((ulong)color.Green) << 16) |
					((ulong)color.Blue);
		}
		bool UseDefaultBackgroundColor (ChunkStyle style)
		{
			return style.TransparentBackround || GetPixel (style.BackgroundColor) == GetPixel (ColorStyle.Default.BackgroundColor);
		}
		Gdk.Color GetBackgroundColor (int offset, bool selected, ChunkStyle style)
		{
			if (selected)
				return ColorStyle.Selection.BackgroundColor;
			if (IsSearchResultAt (offset))
				return ColorStyle.SearchTextBg;
			if (UseDefaultBackgroundColor (style))
				return defaultBgColor;
			return style.BackgroundColor;
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
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			inSelectionDrag = false;
			inDrag = false;
			Selection selection = textEditor.MainSelection;
			int anchor         = selection != null ?  selection.GetAnchorOffset (this.textEditor.GetTextEditorData ()) : -1;
			int oldOffset      = textEditor.Caret.Offset;
			
			string link = GetLink (args);
			if (!String.IsNullOrEmpty (link)) {
				textEditor.FireLinkEvent (link, args.Button, args.ModifierState);
				return;
			}

			if (args.Button == 1 || args.Button == 2) {
				VisualLocationTranslator trans = new VisualLocationTranslator (this, args.X, args.Y);
				clickLocation = trans.VisualToDocumentLocation (args.X, args.Y);
				LineSegment line = Document.GetLine (clickLocation.Line);
				if (line != null && clickLocation.Column >= line.EditableLength && GetWidth (Document.GetTextAt (line)+"-") < args.X) {
					int nextColumn = this.textEditor.GetTextEditorData ().GetNextVirtualColumn (clickLocation.Line, clickLocation.Column);
					clickLocation.Column = nextColumn;
				}

				if (!textEditor.IsSomethingSelected) {
					textEditor.MainSelection  = new Selection (clickLocation, clickLocation);
				}

				int offset = Document.LocationToOffset (clickLocation);
				if (offset < 0) {
					textEditor.RunAction (CaretMoveActions.ToDocumentEnd);
					return;
				}
				if (args.Button == 2 && selection != null && selection.Contains (Document.OffsetToLocation (offset))) {
					textEditor.ClearSelection ();
					return;
				}
					
				if (args.Type == EventType.TwoButtonPress) {
					int start = ScanWord (offset, false);
					int end   = ScanWord (offset, true);
					Caret.Offset = end;
					textEditor.MainSelection = new Selection (textEditor.Document.OffsetToLocation (start),
					                                          textEditor.Document.OffsetToLocation (end));
					inSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.Word;
					return;
				} else if (args.Type == EventType.ThreeButtonPress) {
					int lineNr = Document.OffsetToLineNumber (offset);
					textEditor.SetSelectLines (lineNr, lineNr);
					
					inSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.WholeLine;
					return;
				}
				mouseSelectionMode = MouseSelectionMode.SingleChar;
				
				if (textEditor.IsSomethingSelected && textEditor.SelectionRange.Offset <= offset && offset < textEditor.SelectionRange.EndOffset && clickLocation != textEditor.Caret.Location) {
					inDrag = true;
				} else {
					inSelectionDrag = true;
					if ((args.ModifierState & Gdk.ModifierType.ShiftMask) == ModifierType.ShiftMask) {
						Caret.PreserveSelection = true;
						if (!textEditor.IsSomethingSelected) {
							textEditor.MainSelection  = new Selection (Caret.Location, clickLocation);
							Caret.Location = clickLocation;
						} else {
							Caret.Location = clickLocation;
							textEditor.ExtendSelectionTo (clickLocation);
						}
						Caret.PreserveSelection = false;
						
					} else {
						textEditor.ClearSelection ();
						Caret.Location = clickLocation; 
					}
					this.caretBlink = false;
				}
			}
			DocumentLocation docLocation = VisualToDocumentLocation (args.X, args.Y);
			if (args.Button == 2 && this.textEditor.CanEdit (docLocation.Line))  {
				int offset = Document.LocationToOffset (docLocation);
				int length = ClipboardActions.PasteFromPrimary (textEditor.GetTextEditorData (), offset);
				int newOffset = textEditor.Caret.Offset;
				if (selection != null) {
					ISegment selectionRange = selection.GetSelectionRange (this.textEditor.GetTextEditorData ());
					if (newOffset < selectionRange.EndOffset) {
						oldOffset += length;
						anchor   += length;
						selection = new Selection (Document.OffsetToLocation (selectionRange.Offset + length + 1), Document.OffsetToLocation (selectionRange.Offset + length + 1 + selectionRange.Length));
					}
					bool autoScroll = textEditor.Caret.AutoScrollToCaret;
					textEditor.Caret.AutoScrollToCaret = false;
					try {
						textEditor.Caret.Offset = oldOffset;
					} finally {
						textEditor.Caret.AutoScrollToCaret = autoScroll;
					}
					//textEditor.SelectionAnchor = anchor;
					textEditor.MainSelection  = selection;
				} else {
					textEditor.Caret.Offset = oldOffset;
				}
			}
		}
		
		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			if (inDrag) 
				Caret.Location = clickLocation;
			if (!inSelectionDrag)
				textEditor.ClearSelection ();
			inSelectionDrag = false;
			base.MouseReleased (args);
		}
		
		static bool IsNoLetterOrDigit (char ch)
		{
			return !char.IsWhiteSpace (ch) && !char.IsLetterOrDigit (ch);
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
				    IsNoLetterOrDigit (first) && !IsNoLetterOrDigit (ch) ||
				    (char.IsLetterOrDigit (first) || first == '_') && !(char.IsLetterOrDigit (ch) || ch == '_'))
				    break;
				
				offset = forwardDirection ? offset + 1 : offset - 1; 
			}
			return System.Math.Min (line.EndOffset - 1, System.Math.Max (line.Offset, offset + (forwardDirection ? 0 : 1)));
		}
		
		CodeSegmentPreviewWindow previewWindow = null;
		ISegment previewSegment = null;
		
		public void HideCodeSegmentPreviewWindow ()
		{
			if (previewWindow != null) {
				previewWindow.Destroy ();
				previewWindow = null;
			}
		}
		
		void ShowTooltip (ISegment segment, Rectangle hintRectangle)
		{
			if (previewSegment == segment)
				return;
			HideCodeSegmentPreviewWindow ();
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
		
		string GetLink (MarginMouseEventArgs args)
		{
			LineSegment                        line  = args.LineSegment;
			Mono.TextEditor.Highlighting.Style style = ColorStyle;
			Document                           doc   = Document;
			if (doc == null)
				return null;
			SyntaxMode                         mode  = doc.SyntaxMode;
			if (line == null || style == null || mode == null)
				return null;
			
			Chunk chunk = mode.GetChunks (Document, style, line, line.Offset, line.EditableLength);
			if (chunk != null) {
				int offset = Document.LocationToOffset (VisualToDocumentLocation (args.X, args.Y));
				for (; chunk != null; chunk = chunk.Next) {
					if (chunk.Offset <= offset && offset < chunk.EndOffset) {
						ChunkStyle chunkStyle = chunk.GetChunkStyle (style);
						return chunkStyle != null ? chunkStyle.Link : null;
					}
				}
			}
			return null;
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			
			if (args.Button != 1 && args.Y >= 0 && args.Y <= this.textEditor.Allocation.Height) {
				// folding marker 
				int lineNr = args.LineNumber;
				foreach (KeyValuePair<Rectangle, FoldSegment> shownFolding in GetFoldRectangles (lineNr)) {
					if (shownFolding.Key.Contains (args.X + this.XOffset, args.Y)) {
						ShowTooltip (shownFolding.Value, shownFolding.Key);
						return;
					}
				}
				ShowTooltip (null, Gdk.Rectangle.Zero);
				string link = GetLink (args);
				
				if (!String.IsNullOrEmpty (link)) {
					base.cursor = arrowCursor;
				} else {
					base.cursor = xtermCursor;
				}
				return;
			}
			
			if (inSelectionDrag) {
				DocumentLocation loc = VisualToDocumentLocation (args.X, args.Y);
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
					textEditor.MainSelection.Lead = Caret.Location;
					break;
				case MouseSelectionMode.WholeLine:
					//textEditor.SetSelectLines (loc.Line, textEditor.MainSelection.Anchor.Line);
					LineSegment line1 = textEditor.Document.GetLine (loc.Line);
					LineSegment line2 = textEditor.Document.GetLineByOffset (textEditor.SelectionAnchor);
					Caret.Offset = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffset;
					textEditor.MainSelection.Lead = Caret.Location;
					break;
				}
				Caret.PreserveSelection = false;
				if ((args.ModifierState & Gdk.ModifierType.Mod1Mask) == ModifierType.Mod1Mask) {
					textEditor.SelectionMode = SelectionMode.Block;
				} else {
					textEditor.SelectionMode = SelectionMode.Normal;
				}
				
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
			Pango.Layout measureLayout = new Pango.Layout (textEditor.PangoContext);
			int lineXPos  = 0;
			int curColumn = 0;
			int visibleColumn = 0;
			if (line == null || line.EditableLength == 0)
				goto exit;
			measureLayout.Alignment = Pango.Alignment.Left;
			measureLayout.FontDescription = textEditor.Options.Font;
			
			SyntaxMode mode = Document.SyntaxMode != null && this.textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			for (Chunk chunk = mode.GetChunks (Document, this.ColorStyle, line, line.Offset, line.EditableLength); chunk != null; chunk = chunk.Next) {
				for (int i = 0; i < chunk.Length; i++) {
					int delta;
					char ch = chunk.GetCharAt (Document, chunk.Offset + i);
					if (ch == '\t') {
						int newColumn = GetNextTabstop (this.textEditor.GetTextEditorData (), visibleColumn);
						delta = (newColumn - visibleColumn) * CharWidth;
						visibleColumn = newColumn;
					} else {
						ChunkStyle style = chunk.GetChunkStyle (ColorStyle);
						Pango.Weight requestedWeight = style.GetWeight (DefaultWeight);
						if (measureLayout.FontDescription.Weight != requestedWeight)
							measureLayout.FontDescription.Weight = requestedWeight;
						Pango.Style requestedStyle = style.GetStyle (DefaultStyle);
						if (measureLayout.FontDescription.Style != requestedStyle)
							measureLayout.FontDescription.Style = requestedStyle;
						measureLayout.SetText (ch.ToString ());
						int height;
						measureLayout.GetPixelSize (out delta, out height);
						
						visibleColumn++;
					}
					curColumn++;
					if (curColumn > column)
						goto exit;
					lineXPos += delta;
				}
			}
		 exit:
			measureLayout.Dispose ();
			if (column > visibleColumn) {
				lineXPos += (column - visibleColumn) * this.charWidth;
			}
			return lineXPos;
		}
		
		public static int GetNextTabstop (TextEditorData textEditor, int currentColumn)
		{
			int tabSize = textEditor != null && textEditor.Options != null ? textEditor.Options.TabSize : 4;
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}
		
		internal int rulerX = 0;
		public int GetWidth (string text)
		{
			text = text.Replace ("\t", new string (' ', textEditor.Options.TabSize));
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
		
		void DrawRectangleWithRuler (Gdk.Drawable win, int x, Gdk.Rectangle area, Gdk.Color color)
		{
			Gdk.GC gc = GetGC (color);
			if (textEditor.Options.ShowRuler) {
				int divider = System.Math.Max (area.Left, System.Math.Min (x + rulerX, area.Right));
				if (divider < area.Right) {
					win.DrawRectangle (gc, true, new Rectangle (area.X, area.Y, divider - area.X, area.Height));
					gc = GetGC (DimColor (color));
					win.DrawRectangle (gc, true, new Rectangle (divider, area.Y, area.Right - divider, area.Height));
					return;
				}
			}
			win.DrawRectangle (gc, true, area);
		}
		
		List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> GetFoldRectangles (int lineNr)
		{
			List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> result = new List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle,FoldSegment>> ();
			if (lineNr < 0)
				return result;
			layout.Alignment = Pango.Alignment.Left;
			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
//			int xStart = XOffset;
			int y      = (int)(Document.LogicalToVisualLine (lineNr) * LineHeight - textEditor.VAdjustment.Value);
//			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = (int)(XOffset - textEditor.HAdjustment.Value);
			
			if (line == null) {
				return result;
			}
			
			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
//			int caretOffset = Caret.Offset;
		 restart:
			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
					layout.SetText (Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.Length - offset))).Replace ("\t", new string (' ', textEditor.Options.TabSize)));
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
						goto restart;
					}
				}
			}
			return result;
		}
		
		List<ISegment> selectedRegions = new List<ISegment> ();
		Gdk.Color      defaultBgColor;
		Gdk.Rectangle  clipRectangle;
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
//			int visibleLine = y / this.LineHeight;
//			this.caretX = -1;
			layout.Alignment = Pango.Alignment.Left;
			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);
			clipRectangle = new Gdk.Rectangle (xStart, y, area.Right - xStart, LineHeight);
			
			if (textEditor.Options.HighlightCaretLine && Caret.Line == lineNr) {
				defaultBgColor = ColorStyle.LineMarker;
			} else {
				defaultBgColor = ColorStyle.Default.BackgroundColor;
			}
				
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = (int)(x - textEditor.HAdjustment.Value);
			
			if (line == null) {
				DrawRectangleWithRuler (win, x, lineArea, defaultBgColor);
				if (textEditor.Options.ShowInvalidLines) {
					DrawInvalidLineMarker (win, xPos, y);
				}
				if (textEditor.Options.ShowRuler) { // warning: code duplication, look at the method end.
					win.DrawLine (GetGC (ColorStyle.Ruler), x + rulerX, y, x + rulerX, y + LineHeight); 
				}
				return;
			}
			//selectedRegions.Clear ();
			
			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
		 restart:
			foreach (FoldSegment folding  in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
//					layout.SetText (Document.GetTextAt (offset, foldOffset - offset));
//					gc.RgbFgColor = ColorStyle.FoldLine;
//					win.DrawLayout (gc, xPos, y, layout);
//					layout.GetPixelSize (out width, out height);
					
					DrawLinePart (win, line, offset, foldOffset - offset, ref xPos, y, area.Right);
//					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					bool isFoldingSelected = textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding);
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, this.LineHeight - 1);
					win.DrawRectangle (GetGC (isFoldingSelected ? ColorStyle.Selection.BackgroundColor : defaultBgColor), true, foldingRectangle);
					win.DrawRectangle (GetGC (isFoldingSelected ? ColorStyle.Selection.Color : ColorStyle.FoldLine.Color), false, foldingRectangle);
					win.DrawLayout (GetGC (isFoldingSelected ? ColorStyle.Selection.Color : ColorStyle.FoldLine.Color), xPos, y, layout);
					if (caretOffset == foldOffset && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition (win, folding.Description[0], xPos, y);
					
					xPos += width;
					
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						goto restart;
					}
				}
			}
			
			if (textEditor.longestLine == null || line.EditableLength > textEditor.longestLine.EditableLength) {
				textEditor.longestLine = line;
				textEditor.SetAdjustments (textEditor.Allocation);
			}
			
			// Draw remaining line
			if (line.EndOffset - offset > 0)
				DrawLinePart (win, line, offset, line.Offset + line.EditableLength - offset, ref xPos, y, area.Right);
			
			bool isEolSelected = textEditor.IsSomethingSelected && textEditor.SelectionMode == SelectionMode.Normal && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength);
			
			lineArea.X     = xPos;
			lineArea.Width = textEditor.Allocation.Width - xPos;
			
			if (textEditor.SelectionMode == SelectionMode.Block && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength)) {
				DocumentLocation start = textEditor.MainSelection.Anchor;
				DocumentLocation end   = textEditor.MainSelection.Lead;
				
				DocumentLocation visStart = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), start);
				DocumentLocation visEnd   = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), end);
				
				int x1 = this.ColumnToVisualX (line, visStart.Column);
				int x2 = this.ColumnToVisualX (line, visEnd.Column);
				if (x1 > x2) {
					int tmp = x1;
					x1 = x2;
					x2 = tmp;
				}
				x1 += (int)(XOffset - textEditor.HAdjustment.Value);
				x2 += (int)(XOffset - textEditor.HAdjustment.Value);
				
				if (x2 > lineArea.X) {
					if (x1 - lineArea.X > 0) {
						DrawRectangleWithRuler (win, x, new Gdk.Rectangle (lineArea.X, lineArea.Y, x1 - lineArea.X, lineArea.Height), defaultBgColor);
						lineArea.X = x1;
					}
					
					DrawRectangleWithRuler (win, x, new Gdk.Rectangle (lineArea.X, lineArea.Y, x2 - lineArea.X, lineArea.Height), this.ColorStyle.Selection.BackgroundColor);
					
					lineArea.X = x2;
					lineArea.Width = textEditor.Allocation.Width - lineArea.X;
					
				}
			}
			DrawRectangleWithRuler (win, x, lineArea, isEolSelected ? this.ColorStyle.Selection.BackgroundColor : defaultBgColor);
			
			
			
			if (textEditor.Options.ShowEolMarkers)
				DrawEolMarker (win, isEolSelected, xPos, y);
			
			if (textEditor.Options.ShowRuler) { // warning: code duplication, scroll up.
				win.DrawLine (GetGC (ColorStyle.Ruler), x + rulerX, y, x + rulerX, y + LineHeight); 
			}
			
			if (Caret.Line == lineNr && Caret.Column > line.EditableLength) {
				string virtualText = textEditor.GetTextEditorData ().GetVirtualSpaces (Caret.Line, Caret.Column);
				int visibleColumn = line.EditableLength;
				for (int i = 0; i < virtualText.Length; i++) {
					if (virtualText[i] != '\t') {
						layout.SetText (virtualText[i].ToString ());
						layout.GetPixelSize (out width, out height);
						if (textEditor.Options.ShowSpaces) 
							DrawSpaceMarker (win, isEolSelected, xPos, y);
						xPos += width;
						visibleColumn++;
					} else {
						int newColumn = GetNextTabstop (this.textEditor.GetTextEditorData (), visibleColumn);
						int delta = (newColumn - visibleColumn) * CharWidth;
						if (textEditor.Options.ShowTabs) 
							DrawTabMarker (win, isEolSelected, xPos, y);
						xPos += delta;
						visibleColumn = newColumn;
					}
				}
				SetVisibleCaretPosition (win, ' ', xPos, y);
			} else {
				if (caretOffset == line.Offset + line.EditableLength)
					SetVisibleCaretPosition (win, textEditor.Options.ShowEolMarkers ? eolMarkerChar : ' ', xPos, y);
			}
		}
		
		internal protected override void MouseLeft ()
		{
			base.MouseLeft ();
			ShowTooltip (null, Gdk.Rectangle.Zero);
		}
		
		class VisualLocationTranslator
		{
			TextViewMargin margin;
			int lineNumber;
			LineSegment line;
			int width;
			int xPos = 0;
			int column = 0;
			int visibleColumn = 0;
			int visualXPos;
			SyntaxMode mode;
			Pango.Layout measureLayout;
			bool done = false;
			
			public bool WasInLine {
				get;
				set;
			}
			
			public VisualLocationTranslator (TextViewMargin margin, int xp, int yp)
			{
				this.margin = margin;
				lineNumber = System.Math.Min (margin.Document.VisualToLogicalLine ((int)(yp + margin.textEditor.VAdjustment.Value) / margin.LineHeight), margin.Document.LineCount - 1);
				line = lineNumber < margin.Document.LineCount ? margin.Document.GetLine (lineNumber) : null;
			}
			
			Chunk chunks;
			void ConsumeChunks ()
			{
				for (;chunks != null; chunks = chunks.Next) {
					for (int o = chunks.Offset; o < chunks.EndOffset; o++) {
						char ch = chunks.GetCharAt (margin.Document, o);
						
						int delta = 0;
						if (ch == '\t') {
							int newColumn = GetNextTabstop (margin.textEditor.GetTextEditorData (), visibleColumn);
							delta = (newColumn - visibleColumn) * margin.CharWidth;
							visibleColumn = newColumn;
						} else if (ch == ' ') {
							delta = margin.charWidth;
							visibleColumn++;
						} else {
							ChunkStyle style = chunks.GetChunkStyle (margin.ColorStyle);
							Pango.Weight requestedWeight = style.GetWeight (margin.DefaultWeight);
							if (measureLayout.FontDescription.Weight != requestedWeight)
								measureLayout.FontDescription.Weight = requestedWeight;
							Pango.Style requestedStyle = style.GetStyle (margin.DefaultStyle);
							if (measureLayout.FontDescription.Style != requestedStyle)
								measureLayout.FontDescription.Style = requestedStyle;
							measureLayout.SetText (ch.ToString ());
							int height;
							measureLayout.GetPixelSize (out delta, out height);
							visibleColumn++;
						}
						int nextXPosition = xPos + delta;
						if (nextXPosition >= visualXPos) {
							if (!IsNearX1 (visualXPos, xPos, nextXPosition)) 
								column = o - line.Offset + 1;
							done = true;
							return;
						}
						column = o - line.Offset + 1;
						xPos = nextXPosition;
					}
				}
			}
			
			public DocumentLocation VisualToDocumentLocation (int xp, int yp)
			{
				if (line == null) 
					return DocumentLocation.Empty;
				mode = margin.Document.SyntaxMode != null && margin.textEditor.Options.EnableSyntaxHighlighting ? margin.Document.SyntaxMode : SyntaxMode.Default;
				measureLayout = new Pango.Layout (margin.textEditor.PangoContext);
				measureLayout.Alignment = Pango.Alignment.Left;
				measureLayout.FontDescription = margin.textEditor.Options.Font;
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				int offset = line.Offset;
//				int caretOffset = margin.Caret.Offset;
//				int index, trailing;
				column = 0;
				visualXPos = xp + (int)margin.textEditor.HAdjustment.Value;
			 restart:
				foreach (FoldSegment folding in foldings) {
					if (!folding.IsFolded)
						continue;
					int foldOffset = folding.StartLine.Offset + folding.Column;
					if (foldOffset < offset)
						continue;
					chunks = mode.GetChunks (margin.Document, margin.textEditor.ColorStyle, line, offset, foldOffset - offset);
					ConsumeChunks ();
					if (done)
						break;
					
					if (folding.IsFolded) {
						offset = folding.EndLine.Offset + folding.EndColumn;
						DocumentLocation loc = margin.Document.OffsetToLocation (offset);
						lineNumber = loc.Line;
						column     = loc.Column;
						measureLayout.SetText (folding.Description);
						int height;
						measureLayout.GetPixelSize (out width, out height);
						xPos += width;
						if (xPos >= visualXPos) {
							done = true;
							break;
						}
						if (folding.EndLine != line) {
							line   = folding.EndLine;
							foldings = margin.Document.GetStartFoldings (line);
							goto restart;
						}
					} else {
						chunks = mode.GetChunks (margin.Document, margin.textEditor.ColorStyle, line, foldOffset, folding.EndLine.Offset + folding.EndColumn - offset);
						ConsumeChunks ();
					}
				}
				
				if (!done && line.EndOffset - offset > 0) {
					chunks = mode.GetChunks (margin.Document, margin.textEditor.ColorStyle, line, offset, line.Offset + line.EditableLength - offset);
					ConsumeChunks ();
				}
				WasInLine = xPos >= visualXPos;
				measureLayout.Dispose ();
				return new DocumentLocation (lineNumber, column);
			}
		}
		
		public DocumentLocation VisualToDocumentLocation (int xp, int yp)
		{
			return new VisualLocationTranslator (this, xp, yp).VisualToDocumentLocation (xp, yp);
		}
		
		static bool IsNearX1 (int pos, int x1, int x2)
		{
			return System.Math.Abs (x1 - pos) < System.Math.Abs (x2 - pos);
		}
	}
}
