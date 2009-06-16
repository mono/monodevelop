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
		TextRenderer tabMarker, spaceMarker, eolMarker, invalidLineMarker;
		TextRenderer textRenderer;
		IEnumerable<TextRenderer> allTextRenderers;
		
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
			
			textRenderer = CreateTextRenderer ();
			
			tabMarker = CreateTextRenderer ();
			tabMarker.SetText ("\u00BB");
			
			spaceMarker = CreateTextRenderer ();
			spaceMarker.SetText (spaceMarkerChar.ToString ());
			
			eolMarker = CreateTextRenderer ();
			eolMarker.SetText ("\u00B6");
			
			invalidLineMarker = CreateTextRenderer ();
			invalidLineMarker.SetText ("~");
			
			allTextRenderers = new TextRenderer [] {
				textRenderer,
				tabMarker,
				spaceMarker,
				eolMarker,
				invalidLineMarker
			};
			
			ResetCaretBlink ();
			Caret.PositionChanged += CaretPositionChanged;
			textEditor.Document.TextReplaced += UpdateBracketHighlighting;
			Caret.PositionChanged += UpdateBracketHighlighting;
			base.cursor = xtermCursor;
			Document.LineChanged += CheckLongestLine;
			textEditor.HighlightSearchPatternChanged += delegate {
				selectedRegions.Clear ();
			};
			textEditor.SelectionChanged += delegate {
				DisposeLayoutDict ();
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
		
		List<LineSegment> linesToRemove = new List<LineSegment> ();
		internal void VAdjustmentValueChanged ()
		{
			int startLine = (int)(textEditor.GetTextEditorData ().VAdjustment.Value / lineHeight);
			int endLine   = (int)(startLine + textEditor.GetTextEditorData ().VAdjustment.PageSize / lineHeight) + 1;
			
			foreach (LineSegment line in layoutDict.Keys) {
				int curLine = Document.OffsetToLineNumber (line.Offset);
				if (startLine - 5 >= curLine || endLine + 5 <= curLine) 
					linesToRemove.Add (line);
			}
			linesToRemove.ForEach (line => RemoveLayoutLine (line) );
			linesToRemove.Clear ();
			
			foreach (LineSegment line in chunkDict.Keys) {
				int curLine = Document.OffsetToLineNumber (line.Offset);
				if (startLine - 5 >= curLine || endLine + 5 <= curLine) 
					linesToRemove.Add (line);
			}
			linesToRemove.ForEach (line => chunkDict.Remove (line));
			linesToRemove.Clear ();
		}
		
		TextRenderer CreateTextRenderer ()
		{
			return TextRenderer.Create (this, textEditor);
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
			
			chunkDict.Clear ();
			if (layoutDict.ContainsKey (args.Line)) {
				RemoveLayoutLine (args.Line);
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
			
			tabMarker.SentFont (textEditor.Options.Font);
			spaceMarker.SentFont (textEditor.Options.Font);
			eolMarker.SentFont (textEditor.Options.Font);
			invalidLineMarker.SentFont (textEditor.Options.Font);
			textRenderer.SentFont (textEditor.Options.Font);
			
			textRenderer.GetCharSize (out this.charWidth, out this.lineHeight);
			lineHeight = System.Math.Max (1, lineHeight);

			DecorateLineFg -= DecorateTabs;
			if (textEditor.Options.ShowTabs) 
				DecorateLineFg += DecorateTabs;
			
			DecorateLineFg -= DecorateSpaces;
			if (textEditor.Options.ShowSpaces) 
				DecorateLineFg += DecorateSpaces;
			
			DecorateLineBg -= DecorateMatchingBracket;
			if (textEditor.Options.HighlightMatchingBracket) 
				DecorateLineBg += DecorateMatchingBracket;
			DisposeLayoutDict ();
			chunkDict.Clear ();
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
		internal Gdk.GC GetGC (Color color)
		{
			/*if (gc == null)
				gc = new Gdk.GC (textEditor.GdkWindow);
			gc.RgbFgColor = color;
			return gc;*/
			Gdk.GC result = null;
			// color.Pixel doesn't work
			ulong colorId = (ulong)color.Red * (1 << 32) + (ulong)color.Blue * (1 << 16) + (ulong)color.Green;
			if (gcDictionary.TryGetValue (colorId, out result)) {
				// GCs are clipped when starting to draw the line
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
			
			textEditor.GetTextEditorData ().SearchChanged -= HandleSearchChanged;
			
			arrowCursor.Dispose ();
			xtermCursor.Dispose ();
			arrowCursor = xtermCursor = null;
			
			DisposeGCs ();
			textRenderer.Dispose ();
			tabMarker.Dispose ();
			spaceMarker.Dispose ();
			eolMarker.Dispose ();
			invalidLineMarker.Dispose ();
			DisposeLayoutDict ();
			
			layoutDict = null;
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
			if (textRenderer != null && newCaretBlink != caretBlink) {
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
			
			switch (Caret.Mode) {
			case CaretMode.Insert:
				if (caretX < this.XOffset)
					return;
				SetClip (new Gdk.Rectangle (caretX, caretY, 1, LineHeight));
				win.DrawLine (GetGC (ColorStyle.Caret.Color), caretX, caretY, caretX, caretY + LineHeight);
				break;
			case CaretMode.Block:
				if (caretX + this.charWidth < this.XOffset)
					return;
				SetClip (new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight));
				win.DrawRectangle (GetGC (ColorStyle.Caret.Color), true, new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight));
				textRenderer.BeginDraw (win);
				textRenderer.SetClip (clipRectangle);
				textRenderer.Color = ColorStyle.Caret.BackgroundColor;
				textRenderer.SetText (caretChar.ToString ());
				textRenderer.DrawText (win, caretX, caretY);
				textRenderer.EndDraw ();
				break;
			case CaretMode.Underscore:
				if (caretX + this.charWidth < this.XOffset)
					return;
				int bottom = caretY + lineHeight;
				SetClip (new Gdk.Rectangle (caretX, bottom, this.charWidth, 1));
				win.DrawLine (GetGC (ColorStyle.Caret.Color), caretX, bottom, caretX+this.charWidth, bottom);
				break;
			}
		}
		/*
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
			DrawRectangleWithRuler (win, xPos, new Gdk.Rectangle (xPos, y, cWidth, cHeight), GetBackgroundColor (textEditor.preeditOffset, false, chunkStyle), false);
			win.DrawLayout (GetGC (chunkStyle.Color), xPos, y, preEditLayout);
			
			xPos += cWidth;
			preEditLayout.Dispose ();
		}
		*/
		
		void GetSelectionOffsets (LineSegment line, out int selectionStart, out int selectionEnd)
		{
			selectionStart = -1;
			selectionEnd   = -1;
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
		}
		#region Layout cache
		class LineDescriptor
		{
			int Offset { get; set; }
			int Length { get; set; }
			int MarkerLength { get; set; }
			int SpanLength    { get; set; }
			
			protected LineDescriptor (LineSegment line, int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
				this.MarkerLength = line.MarkerCount;
				if (line.StartSpan != null)
					this.SpanLength  = line.StartSpan.Length;
			}
			
			public bool Equals (LineSegment line, int offset, int length, out bool isInvalid)
			{
				isInvalid = MarkerLength != line.MarkerCount || 
				            SpanLength != (line.StartSpan == null ? 0 : line.StartSpan.Length);
				return /*offset == Offset &&*/ Length == length && !isInvalid;
			}
		}
		
		class LayoutDescriptor : LineDescriptor
		{
			public Pango.Layout Layout { get; set; }
			
			public LayoutDescriptor (LineSegment line, int offset, int length, Pango.Layout layout) : base (line, offset, length)
			{
				this.Layout = layout;
			}
		}
		
		Dictionary<LineSegment, List<LayoutDescriptor>> layoutDict = new Dictionary<LineSegment, List<LayoutDescriptor>> ();
		Pango.Layout GetCachedLayout (LineSegment line, int offset, int length, Action<Pango.Layout> createNew)
		{
			List<LayoutDescriptor> list;
			if (!layoutDict.ContainsKey (line)) {
				list = new List<LayoutDescriptor> ();
				layoutDict[line] = list;
			} else {
				list = layoutDict[line];
			}
			for (int i = 0; i < list.Count; i++) {
				LayoutDescriptor descriptor = list[i];
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, out isInvalid))
					return descriptor.Layout;
				if (isInvalid) {
					descriptor.Layout.Dispose ();
					list.RemoveAt (i);
					i--;
				}
			}
			Pango.Layout layout = new Pango.Layout (textEditor.PangoContext);
			createNew (layout);
			
			LayoutDescriptor newDesrc = new LayoutDescriptor (line, offset, length, layout);
			list.Add (newDesrc);
			return newDesrc.Layout;
		}
		
		public void RemoveLayoutLine (LineSegment line)
		{
			List<LayoutDescriptor> list;
			if (!layoutDict.TryGetValue (line, out list)) 
				return;
			foreach (LayoutDescriptor desrc in list)
				desrc.Layout.Dispose ();
			list.Clear ();
			layoutDict.Remove (line);
		}
		
		void DisposeLayoutDict ()
		{
			foreach (List<LayoutDescriptor> list in layoutDict.Values) {
				foreach (LayoutDescriptor desrc in list)
					desrc.Layout.Dispose ();
			}
			layoutDict.Clear ();
		}
		
		class ChunkDescriptor : LineDescriptor
		{
			public Chunk Chunk { get; set; }
			
			public ChunkDescriptor (LineSegment line, int offset, int length, Chunk chunk) : base (line, offset, length)
			{
				this.Chunk = chunk;
			}
		}
		
		Dictionary<LineSegment, List<ChunkDescriptor>> chunkDict = new Dictionary<LineSegment, List<ChunkDescriptor>> ();
		Chunk GetCachedChunks (SyntaxMode mode, Document doc, Mono.TextEditor.Highlighting.Style style, LineSegment line, int offset, int length)
		{
			List<ChunkDescriptor> list;
			if (!chunkDict.ContainsKey (line)) {
				list = new List<ChunkDescriptor> ();
				chunkDict[line] = list;
			} else {
				list = chunkDict[line];
			}
			for (int i = 0; i < list.Count; i++) {
				ChunkDescriptor descriptor = list[i];
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, out isInvalid))
					return descriptor.Chunk;
				if (isInvalid) {
					list.RemoveAt (i);
					i--;
				}
			}
			
			ChunkDescriptor newDesrc = new ChunkDescriptor (line, offset, length, mode.GetChunks (doc, style, line, offset, length));
			list.Add (newDesrc);
			return newDesrc.Chunk;
		}
		
		public void ForceInvalidateLine (int lineNr)
		{
			LineSegment line = Document.GetLine (lineNr);
			if (line != null && layoutDict.ContainsKey (line))
				layoutDict.Remove (line);
		}
		
		delegate void HandleSelectionDelegate (int start, int end);
		static void HandleSelection (int selectionStart, int selectionEnd, int startOffset, int endOffset, HandleSelectionDelegate handleNotSelected, HandleSelectionDelegate handleSelected)
		{
			if (startOffset >= selectionStart && endOffset <= selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, endOffset);
			} else if (startOffset >= selectionStart && startOffset < selectionEnd && endOffset > selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, selectionEnd);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionStart && endOffset <= selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart);
				if (handleSelected != null)
					handleSelected (selectionStart, endOffset);
			} else if (startOffset < selectionStart && endOffset > selectionEnd) {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, selectionStart);
				if (handleSelected != null)
					handleSelected (selectionStart, selectionEnd);
				if (handleNotSelected != null)
					handleNotSelected (selectionEnd, endOffset);
			} else {
				if (handleNotSelected != null)
					handleNotSelected (startOffset, endOffset);
			}
		}
		
		static uint TranslateToUTF8Index (string text, uint textIndex, ref uint curIndex, ref uint byteIndex)
		{
			if (textIndex < curIndex) {
				byteIndex = (uint)Encoding.UTF8.GetByteCount (text.ToCharArray (), 0, (int)textIndex);
			} else {
				byteIndex += (uint)Encoding.UTF8.GetByteCount (text.ToCharArray (), (int)curIndex, (int)(textIndex - curIndex));
			}
			curIndex = textIndex;
			return byteIndex;
		}
		
		Pango.Layout CreateLinePartLayout (SyntaxMode mode, LineSegment line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return GetCachedLayout (line, offset, length, delegate (Pango.Layout layout) {
				layout.Alignment = Pango.Alignment.Left;
				layout.FontDescription = textEditor.Options.Font;
				string lineText        = Document.GetTextAt (offset, length);
				bool   containsPreedit = offset <= textEditor.preeditOffset && textEditor.preeditOffset <= offset + length;
				uint   preeditLength   = 0;
				
				if (containsPreedit) {
					lineText = lineText.Insert (textEditor.preeditOffset - offset, textEditor.preeditString);
					preeditLength = (uint)textEditor.preeditString.Length;
				}
				layout.SetText (lineText);
				
				Pango.TabArray tabArray = new Pango.TabArray (10, true);
				for (int i = 0; i < 10; i++)
					tabArray.SetTab (i, Pango.TabAlign.Left, (i + 1) * CharWidth * textEditor.Options.TabSize);
				
				layout.Tabs = tabArray;
				Pango.AttrList attributes = new Pango.AttrList ();
				
				int startOffset = offset, endOffset = offset + length;
				uint curIndex = 0, byteIndex = 0;
				HandleSelection (selectionStart, selectionEnd, startOffset, endOffset, null, delegate (int start, int end) {
					Pango.AttrBackground attrBackground = new Pango.AttrBackground (this.ColorStyle.Selection.BackgroundColor.Red,
					                                                                this.ColorStyle.Selection.BackgroundColor.Green,
					                                                                this.ColorStyle.Selection.BackgroundColor.Blue);
					attrBackground.StartIndex = TranslateToUTF8Index (lineText, (uint)(start - startOffset), ref curIndex, ref byteIndex);
					attrBackground.EndIndex   = TranslateToUTF8Index (lineText, (uint)(end - startOffset), ref curIndex, ref byteIndex);
					attributes.Insert (attrBackground);
				});
				
				ISegment firstSearch;
				int o = offset;
				int s;
				while ((firstSearch = GetFirstSearchResult (o, offset + length)) != null) {
					
					HandleSelection (selectionStart, selectionEnd, firstSearch.Offset, firstSearch.EndOffset, delegate (int start, int end) {
						Pango.AttrBackground backGround = new Pango.AttrBackground (ColorStyle.SearchTextBg.Red,
						                                                            ColorStyle.SearchTextBg.Green,
						                                                            ColorStyle.SearchTextBg.Blue);
						backGround.StartIndex = TranslateToUTF8Index (lineText, (uint)(start - offset), ref curIndex, ref byteIndex);
						backGround.EndIndex   = TranslateToUTF8Index (lineText, (uint)(end - offset), ref curIndex, ref byteIndex);
						attributes.Insert (backGround);
					},
					null);
					
					o = System.Math.Max (firstSearch.EndOffset, o + 1);
				}
				
				for (Chunk chunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, offset, length); chunk != null; chunk = chunk != null ? chunk.Next : null) {
					ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (textEditor.ColorStyle) : null;
					
					foreach (TextMarker marker in line.Markers)
						chunkStyle = marker.GetStyle (chunkStyle);
					
					if (chunkStyle != null) {
						startOffset = chunk.Offset;
						endOffset   = chunk.EndOffset;
						uint startIndex = (uint)(chunk.Offset - line.Offset);
						uint endIndex   = (uint)(startIndex + chunk.Length);
						if (containsPreedit) {
							if (textEditor.preeditOffset < startOffset)
								startIndex += preeditLength;
							if (textEditor.preeditOffset < endOffset)
								endIndex += preeditLength;
						}
						
						HandleSelection (selectionStart, selectionEnd, startOffset, endOffset, delegate (int start, int end) {
							Pango.AttrForeground foreGround = new Pango.AttrForeground  (chunkStyle.Color.Red,
							                                                             chunkStyle.Color.Green,
							                                                             chunkStyle.Color.Blue);
							foreGround.StartIndex = TranslateToUTF8Index (lineText, (uint)(start - offset), ref curIndex, ref byteIndex);
							foreGround.EndIndex   = TranslateToUTF8Index (lineText, (uint)(end - offset), ref curIndex, ref byteIndex);
							
							attributes.Insert (foreGround);
							
							if (!chunkStyle.TransparentBackround) {
								Pango.AttrBackground backGround = new Pango.AttrBackground (chunkStyle.BackgroundColor.Red,
								                                                            chunkStyle.BackgroundColor.Green,
								                                                            chunkStyle.BackgroundColor.Blue);
								backGround.StartIndex = foreGround.StartIndex;
								backGround.EndIndex   = foreGround.EndIndex;
								attributes.Insert (backGround);
							}
						}, 
						delegate (int start, int end) {
							Pango.AttrForeground selectedForeground = new Pango.AttrForeground (ColorStyle.Selection.Color.Red,
							                                                                    ColorStyle.Selection.Color.Green,
							                                                                    ColorStyle.Selection.Color.Blue);
							selectedForeground.StartIndex = TranslateToUTF8Index (lineText, (uint)(start - offset), ref curIndex, ref byteIndex);
							selectedForeground.EndIndex   = TranslateToUTF8Index (lineText, (uint)(end - offset), ref curIndex, ref byteIndex);
							attributes.Insert (selectedForeground);
						});
						
						if (chunkStyle.Bold) {
							Pango.AttrWeight attrWeight = new Pango.AttrWeight (Pango.Weight.Bold);
							attrWeight.StartIndex = startIndex;
							attrWeight.EndIndex   = endIndex;
							attributes.Insert (attrWeight);
						}
						
						if (chunkStyle.Italic) {
							Pango.AttrStyle attrStyle = new Pango.AttrStyle (Pango.Style.Italic);
							attrStyle.StartIndex = startIndex;
							attrStyle.EndIndex   = endIndex;
							attributes.Insert (attrStyle);
						}
						
						if (chunkStyle.Underline) {
							Pango.AttrUnderline attrUnderline = new Pango.AttrUnderline (Pango.Underline.Single);
							attrUnderline.StartIndex = startIndex;
							attrUnderline.EndIndex   = endIndex;
							attributes.Insert (attrUnderline);
						}
					}
				}
				if (containsPreedit) {
					Pango.AttrUnderline underline = new Pango.AttrUnderline (Pango.Underline.Single);
					underline.StartIndex = TranslateToUTF8Index (lineText, (uint)(textEditor.preeditOffset - offset), ref curIndex, ref byteIndex);
					underline.EndIndex   = TranslateToUTF8Index (lineText, (uint)(underline.StartIndex + preeditLength), ref curIndex, ref byteIndex);
					attributes.Insert (underline);
					// doesn't work:
					/*
					Pango.AttrIterator iter = textEditor.preeditAttrs.Iterator;
					int start, end;
					iter.Range (out start, out end);
					do {
						Pango.FontDescription desc;
						Pango.Language lang;
						Pango.Attribute[] attrs;
						iter.GetFont (out desc, out lang, out attrs);
						foreach (Pango.Attribute attr in attrs) {
							Console.WriteLine (attr);
							attr.StartIndex = (uint)(textEditor.preeditOffset - offset);
							attr.EndIndex   = attr.StartIndex + preeditLength;
							attributes.Insert (attr);
						}
					} while (iter.Next ());
					iter.Dispose ();*/
				}
				layout.Attributes = attributes;
			});
		}
		#endregion
		
		public delegate void LineDecorator (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd);
		public event LineDecorator DecorateLineBg;
		public event LineDecorator DecorateLineFg;
		
		void DecorateSpaces (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			string lineText = layout.Text;
			uint curIndex = 0, byteIndex = 0;
			for (int i = 0; i < lineText.Length; i++) {
				if (lineText[i] == ' ') {
					int line2, xpos;
					
					layout.IndexToLineX ((int)TranslateToUTF8Index (lineText, (uint)i, ref curIndex, ref byteIndex), false, out line2, out xpos);
					DrawSpaceMarker (win, selectionStart <= offset + i && offset + i <= selectionEnd, xPos + xpos  / 1024, y);
				}
			}
		}
		
		void DecorateTabs (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			string lineText = layout.Text;
			uint curIndex = 0, byteIndex = 0;
			for (int i = 0; i < lineText.Length; i++) {
				if (lineText[i] == '\t') {
					int line2, xpos;
					layout.IndexToLineX ((int)TranslateToUTF8Index (lineText, (uint)i, ref curIndex, ref byteIndex), false, out line2, out xpos);
					DrawTabMarker (win, selectionStart <= offset + i && offset + i <= selectionEnd, xPos + xpos  / 1024, y);
				}
			}
		}
		
		void DecorateMatchingBracket (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			string lineText = layout.Text;
			if (offset <= highlightBracketOffset && highlightBracketOffset <= offset + length) {
				int index = highlightBracketOffset - offset;
				Pango.Rectangle rect = layout.IndexToPos ((int)TranslateToUTF8Index (lineText, (uint)index, ref curIndex, ref byteIndex));
				
				Gdk.Rectangle bracketMatch = new Gdk.Rectangle (xPos + (int)(rect.X / Pango.Scale.PangoScale), y, (int)(rect.Width / Pango.Scale.PangoScale) - 1, (int)(rect.Height / Pango.Scale.PangoScale) - 1);
				win.DrawRectangle (GetGC (this.ColorStyle.BracketHighlightRectangle.BackgroundColor), true, bracketMatch);
				win.DrawRectangle (GetGC (this.ColorStyle.BracketHighlightRectangle.Color), false, bracketMatch);
			}
		}
		
		
		void DrawLinePart (Gdk.Drawable win, LineSegment line, int offset, int length, ref int xPos, int y, int maxX)
		{
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			int selectionStart;
			int selectionEnd;
			GetSelectionOffsets (line, out selectionStart, out selectionEnd);
			
			// ---- new renderer
			Pango.Layout layout = CreateLinePartLayout (mode, line, offset, length, selectionStart, selectionEnd);
			
			Pango.Rectangle ink_rect, logical_rect;
			layout.GetExtents (out ink_rect, out logical_rect);
			int width = (int)((logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale);
			
			bool drawBg = true;
			bool drawText = true;
			foreach (TextMarker marker in line.Markers)  {
				IBackgroundMarker bgMarker = marker as IBackgroundMarker;
				if (bgMarker == null) 
					continue;
				drawText &= bgMarker.DrawBackground (textEditor, win, layout, false /*selected*/, offset, offset + length, y, xPos, xPos + width, ref drawBg);
			}
			
			if (DecorateLineBg != null)
				DecorateLineBg (win, layout, offset, length, xPos, y, selectionStart, selectionEnd);
			
//			if (drawText)
				win.DrawLayout (GetGC (ColorStyle.Default.Color), xPos, y, layout);
			
			if (DecorateLineFg != null)
				DecorateLineFg (win, layout, offset, length, xPos, y, selectionStart, selectionEnd);
			
			
			if (Document.GetLine (Caret.Line) == line) {
				Pango.Rectangle strong_pos, weak_pos;
				int index = Caret.Offset - offset;
				if (offset <= textEditor.preeditOffset && textEditor.preeditOffset < offset + length) {
					index += textEditor.preeditString.Length;
				}
				if (index >= 0 && index < length) {
					uint curIndex = 0, byteIndex = 0;
					layout.GetCursorPos ((int)TranslateToUTF8Index (layout.Text, (uint)index, ref curIndex, ref byteIndex), out strong_pos, out weak_pos);
					char caretChar = Document.GetCharAt (Caret.Offset);
					if (textEditor.Options.ShowSpaces && caretChar == ' ') 
						caretChar = spaceMarkerChar;
					if (textEditor.Options.ShowTabs && caretChar == '\t')
						caretChar = tabMarkerChar;
					SetVisibleCaretPosition (win, caretChar, (int)(xPos + strong_pos.X / Pango.Scale.PangoScale), y);
				} else if (index == length) {
					SetVisibleCaretPosition (win, textEditor.Options.ShowEolMarkers ? eolMarkerChar : ' ', xPos + width, y);
				}
			}
			
			foreach (TextMarker marker in line.Markers) {
				marker.Draw (textEditor, win, layout, false /*selected*/, offset, offset + length, y, xPos, xPos + width);
			}
			
			xPos += width;
			
			// --- new renderer end
			
			// -- old renderer
			/*
			int visibleColumn = 0;
			Chunk lastChunk = null;
			ChunkStyle lastChunkStyle = null;
			for (Chunk chunk = mode.GetChunks (Document, textEditor.ColorStyle, line, offset, length); (lastChunk != null || chunk != null); chunk = chunk != null ? chunk.Next : null) {
				if (xPos >= maxX)
					break;

				ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (textEditor.ColorStyle) : null;
				if (lastChunk == null) {
					lastChunk = chunk;
					lastChunkStyle = chunkStyle;
					continue;
				}
				if (chunk != null && lastChunk.EndOffset == chunk.Offset && lastChunkStyle.Equals (chunkStyle) && chunk.Style == lastChunk.Style) {
					// Merge together chunks with the same style
					lastChunk.Length += chunk.Length;
					continue;
				}
				
				if (lastChunk.Contains (textEditor.preeditOffset)) {
					DrawChunkPart (win, line, lastChunk, ref visibleColumn, ref xPos, y, lastChunk.Offset, textEditor.preeditOffset, selectionStart, selectionEnd);
					DrawPreeditString (win, lastChunk.Style, ref xPos, y);
					DrawChunkPart (win, line, lastChunk, ref visibleColumn, ref xPos, y, textEditor.preeditOffset, lastChunk.EndOffset, selectionStart, selectionEnd);
				} else {
					DrawChunkPart (win, line, lastChunk, ref visibleColumn, ref xPos, y, lastChunk.Offset, lastChunk.EndOffset, selectionStart, selectionEnd);
				}
				lastChunk = chunk;
				lastChunkStyle = chunkStyle;
			}
			
			if (textEditor.preeditOffset == offset + length) 
				DrawPreeditString (win, lastChunk.Style, ref xPos, y);
			*/
			// --- old renderer end
			
		//	if (Caret.Offset == offset + length) 
		//		SetVisibleCaretPosition (win, ' ', xPos, y);
		
		}
		/*
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
		*/
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
		/*
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
			
			textRenderer.Weight = style.GetWeight (DefaultWeight);
			textRenderer.Style = style.GetStyle (DefaultStyle);
			
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
						int cWidth, cHeight, cAdv;
						textRenderer.SetText (ch.ToString ());
						textRenderer.GetPixelSize (out cWidth, out cHeight, out cAdv);
						width = cWidth;
						if (drawBg) {
							Gdk.Rectangle bracketMatch = new Gdk.Rectangle (xPos, y, cWidth - 1, cHeight - 1);
							win.DrawRectangle (GetGC (selected ? this.ColorStyle.Selection.BackgroundColor : this.ColorStyle.BracketHighlightRectangle.BackgroundColor), true, bracketMatch);
							win.DrawRectangle (GetGC (this.ColorStyle.BracketHighlightRectangle.Color), false, bracketMatch);
						}
						textRenderer.Color = selected ? ColorStyle.Selection.Color : style.Color;
						textRenderer.DrawText (win, xPos, y);
						width = cAdv;
					}
					foreach (TextMarker marker in line.Markers) {
						marker.Draw (textEditor, win, selected, offset, offset + 1, y, xPos, xPos + charWidth);
					}
					xPos += width;
					visibleColumn++;
				} else if (ch == ' ' && textEditor.Options.ShowSpaces) {
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
							DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, charWidth, LineHeight), bgc, false);
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
							DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, delta, LineHeight), GetBackgroundColor (offset, selected, style), false);
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
						textRenderer.SetText (wordBuilder.ToString ());
						
						int width, height, xadv;
						textRenderer.GetPixelSize (out width, out height, out xadv);
						
						drawCaretAt = xPos + xadv;
					}
					wordBuilder.Append (ch);
				}
			}
			
			OutputWordBuilder (win, line, selected, style, ref visibleColumn, ref xPos, y, endOffset);
			
			textRenderer.Weight = DefaultWeight;
			textRenderer.Style = DefaultStyle;
			if (style.Underline) 
				line.RemoveMarker (underlineMarker);
						
			if (drawCaretAt >= 0)
				SetVisibleCaretPosition (win, Document.Contains (caretOffset) ? Document.GetCharAt (caretOffset) : ' ', drawCaretAt, y);
		}*/
		
		void DrawText (Gdk.Drawable win, string text, Gdk.Color foreColor, bool drawBg, Gdk.Color backgroundColor, ref int xPos, int y)
		{
			textRenderer.SetText (text);
			
			int width, height, xadv;
			textRenderer.GetPixelSize (out width, out height, out xadv);
			if (drawBg) 
				DrawRectangleWithRuler (win, this.XOffset, new Gdk.Rectangle (xPos, y, width, height), backgroundColor, false);
			
			textRenderer.Color = foreColor;
			textRenderer.DrawText (win, xPos, y);
			xPos += xadv;
		}
		
		void DrawEolMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			eolMarker.Color = selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker;
			eolMarker.DrawText (win, xPos, y);
		}
		
		void DrawSpaceMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			spaceMarker.Color = selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker;
			spaceMarker.DrawText (win, xPos, y);
		}
		
		void DrawTabMarker (Gdk.Drawable win, bool selected, int xPos, int y)
		{
			tabMarker.Color = selected ? ColorStyle.Selection.Color : ColorStyle.WhitespaceMarker;
			tabMarker.DrawText (win, xPos, y);
		}
		
		void DrawInvalidLineMarker (Gdk.Drawable win, int x, int y)
		{
			invalidLineMarker.Color = ColorStyle.InvalidLineMarker;
			invalidLineMarker.DrawText (win, x, y);
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
			
			Chunk chunk = GetCachedChunks (mode, Document, style, line, line.Offset, line.EditableLength);
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
			if (line == null || line.EditableLength == 0)
				return 0;
			
			SyntaxMode mode = Document.SyntaxMode != null && this.textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			
			/*
			Pango.Layout lineLayout = CreateLinePartLayout (mode, line, line.Offset, line.EditableLength, -1, -1);
			column = line.GetVisualColumn (this.textEditor.GetTextEditorData (), Document, column);
			int lineNr, xpos;
			lineLayout.IndexToLineX (column, false, out lineNr, out xpos);
			Console.WriteLine (line);
			Console.WriteLine (column + " - " + xpos);
			Console.WriteLine (lineLayout.Text);
			return (int)(xpos / Pango.Scale.PangoScale);*/
			
			int lineXPos  = 0;
			int curColumn = 0;
			int visibleColumn = 0;
			
			TextRenderer measureLayout = CreateTextRenderer ();
			
			measureLayout.SentFont (textEditor.Options.Font);
			
			for (Chunk chunk = GetCachedChunks (mode, Document, this.ColorStyle, line, line.Offset, line.EditableLength); chunk != null; chunk = chunk.Next) {
				for (int i = 0; i < chunk.Length && chunk.Offset + i < Document.Length; i++) {
					int delta;
					char ch = chunk.GetCharAt (Document, chunk.Offset + i);
					if (ch == '\t') {
						int newColumn = GetNextTabstop (this.textEditor.GetTextEditorData (), visibleColumn);
						delta = (newColumn - visibleColumn) * CharWidth;
						visibleColumn = newColumn;
					} else {
						ChunkStyle style = chunk.GetChunkStyle (ColorStyle);
						measureLayout.Weight = style.GetWeight (DefaultWeight);
						measureLayout.Style = style.GetStyle (DefaultStyle);
						measureLayout.SetText (ch.ToString ());
						int height, width;
						measureLayout.GetPixelSize (out width, out height, out delta);
						
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
			textRenderer.SetText (text);
			int width, height, xadv;
			textRenderer.GetPixelSize (out width, out height, out xadv);
			return width;
		}

		static Color DimColor (Color color)
		{
			return new Color ((byte)(((byte)color.Red * 19) / 20), 
			                  (byte)(((byte)color.Green * 19) / 20), 
			                  (byte)(((byte)color.Blue * 19) / 20));
		}
		
		void DrawRectangleWithRuler (Gdk.Drawable win, int x, Gdk.Rectangle area, Gdk.Color color, bool drawDefaultBackground)
		{
			bool isDefaultColor = (color.Red == defaultBgColor.Red && color.Green == defaultBgColor.Green && color.Blue == defaultBgColor.Blue);
			if (isDefaultColor && !drawDefaultBackground)
				return;
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

			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
//			int xStart = XOffset;
			int y      = (int)(Document.LogicalToVisualLine (lineNr) * LineHeight - textEditor.VAdjustment.Value);
//			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height, xadv;
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
					textRenderer.SetText (Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.Length - offset))).Replace ("\t", new string (' ', textEditor.Options.TabSize)));
					textRenderer.GetPixelSize (out width, out height, out xadv);
					xPos += xadv;
					offset = folding.EndLine.Offset + folding.EndColumn;
					
					textRenderer.SetText (folding.Description);
					textRenderer.GetPixelSize (out width, out height, out xadv);
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, this.LineHeight - 1);
					result.Add (new KeyValuePair<Rectangle, FoldSegment> (foldingRectangle, folding));
					xPos += xadv;
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						goto restart;
					}
				}
			}
			return result;
		}

		protected internal override void BeginRender (Drawable drawable, Rectangle area, int x)
		{
			InitializeTextRenderers (drawable);
		}

		protected internal override void EndRender (Drawable drawable, Rectangle area, int x)
		{
			FinalizeTextRenderers ();
		}

		List<ISegment> selectedRegions = new List<ISegment> ();
		Gdk.Color defaultBgColor;
		Gdk.Rectangle clipRectangle;

		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
//			int visibleLine = y / this.LineHeight;
//			this.caretX = -1;
			
			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);
			
			SetClip (new Gdk.Rectangle (xStart, y, area.Right - xStart, LineHeight));
			ClipRenderers ();
			
			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height, xadv;
			int xPos = (int)(x - textEditor.HAdjustment.Value);
			
			// Draw the default back color for the whole line. Colors other than the default
			// background will be drawn when rendering the text chunks.
			
			if (textEditor.Options.HighlightCaretLine && Caret.Line == lineNr)
				defaultBgColor = ColorStyle.LineMarker;
			else
				defaultBgColor = ColorStyle.Default.BackgroundColor;
			DrawRectangleWithRuler (win, x, lineArea, defaultBgColor, true);
			
			// Check if line is beyond the document length
			
			if (line == null) {
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
					
					textRenderer.SetText (folding.Description);
					textRenderer.GetPixelSize (out width, out height, out xadv);
					bool isFoldingSelected = textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding);
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, this.LineHeight - 1);
					win.DrawRectangle (GetGC (isFoldingSelected ? ColorStyle.Selection.BackgroundColor : defaultBgColor), true, foldingRectangle);
					win.DrawRectangle (GetGC (isFoldingSelected ? ColorStyle.Selection.Color : ColorStyle.FoldLine.Color), false, foldingRectangle);
					textRenderer.Color = isFoldingSelected ? ColorStyle.Selection.Color : ColorStyle.FoldLine.Color;
					textRenderer.DrawText (win, xPos, y);
					if (caretOffset == foldOffset && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition (win, folding.Description[0], xPos, y);
					
					xPos += xadv;
					
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
						DrawRectangleWithRuler (win, x, new Gdk.Rectangle (lineArea.X, lineArea.Y, x1 - lineArea.X, lineArea.Height), defaultBgColor, false);
						lineArea.X = x1;
					}
					
					DrawRectangleWithRuler (win, x, new Gdk.Rectangle (lineArea.X, lineArea.Y, x2 - lineArea.X, lineArea.Height), this.ColorStyle.Selection.BackgroundColor, false);
					
					lineArea.X = x2;
					lineArea.Width = textEditor.Allocation.Width - lineArea.X;
					
				}
			}
			DrawRectangleWithRuler (win, x, lineArea, isEolSelected ? this.ColorStyle.Selection.BackgroundColor : defaultBgColor, false);
			
			
			
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
						textRenderer.SetText (virtualText[i].ToString ());
						textRenderer.GetPixelSize (out width, out height, out xadv);
						if (textEditor.Options.ShowSpaces) 
							DrawSpaceMarker (win, isEolSelected, xPos, y);
						xPos += xadv;
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
		
		void InitializeTextRenderers (Gdk.Drawable win)
		{
			foreach (TextRenderer tr in allTextRenderers) {
				tr.BeginDraw (win);
				tr.Size = textEditor.Options.Font.Size;
			}
		}
		
		void FinalizeTextRenderers ()
		{
			foreach (TextRenderer tr in allTextRenderers)
				tr.EndDraw ();
		}

		void ClipRenderers ( )
		{
			foreach (TextRenderer tr in allTextRenderers)
				tr.SetClip (clipRectangle);
		}

		void SetClip (Gdk.Rectangle rect)
		{
			clipRectangle = rect;
			foreach (Gdk.GC gc in gcDictionary.Values)
				gc.ClipRectangle = rect;
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
			int xPos = 0;
			int column = 0;
			int visibleColumn = 0;
			int visualXPos;
			SyntaxMode mode;
			TextRenderer measureLayout;
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
					for (int o = chunks.Offset; o < chunks.EndOffset && o < margin.Document.Length; o++) {
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
							measureLayout.Weight = style.GetWeight (margin.DefaultWeight);
							measureLayout.Style = style.GetStyle (margin.DefaultStyle);
							measureLayout.SetText (ch.ToString ());
							int height, width;
							measureLayout.GetPixelSize (out width, out height, out delta);
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
				measureLayout = margin.CreateTextRenderer ();
				measureLayout.SentFont (margin.textEditor.Options.Font);
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
					chunks = margin.GetCachedChunks (mode, margin.Document, margin.textEditor.ColorStyle, line, offset, foldOffset - offset);
					ConsumeChunks ();
					if (done)
						break;
					
					if (folding.IsFolded) {
						offset = folding.EndLine.Offset + folding.EndColumn;
						DocumentLocation loc = margin.Document.OffsetToLocation (offset);
						lineNumber = loc.Line;
						column     = loc.Column;
						measureLayout.SetText (folding.Description);
						int height, width, delta;
						measureLayout.GetPixelSize (out width, out height, out delta);
						xPos += delta;
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
						chunks = margin.GetCachedChunks (mode, margin.Document, margin.textEditor.ColorStyle, line, foldOffset, folding.EndLine.Offset + folding.EndColumn - offset);
						ConsumeChunks ();
					}
				}
				
				if (!done && line.EndOffset - offset > 0) {
					chunks = margin.GetCachedChunks (mode, margin.Document, margin.textEditor.ColorStyle, line, offset, line.Offset + line.EditableLength - offset);
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
