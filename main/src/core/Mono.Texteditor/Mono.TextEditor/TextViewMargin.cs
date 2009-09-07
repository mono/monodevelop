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
using System.Linq;
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
		Pango.TabArray tabArray = null;
		int charWidth;

		int lineHeight = 16;
		int highlightBracketOffset = -1;

		public int LineHeight {
			get { return lineHeight; }
		}

		public override int Width {
			get { return -1; }
		}

		Caret Caret {
			get { return textEditor.Caret; }
		}

		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get { return this.textEditor.ColorStyle; }
		}

		public Document Document {
			get { return textEditor.Document; }
		}

		public int CharWidth {
			get { return charWidth; }
		}

		const char spaceMarkerChar = '·';
		const char tabMarkerChar = '»';
		const char eolMarkerChar = '¶';

		public TextViewMargin (TextEditor textEditor)
		{
			this.textEditor = textEditor;

			textRenderer = CreateTextRenderer ();

			tabMarker = CreateTextRenderer ();
			tabMarker.SetText ("»");

			spaceMarker = CreateTextRenderer ();
			spaceMarker.SetText (spaceMarkerChar.ToString ());

			eolMarker = CreateTextRenderer ();
			eolMarker.SetText ("¶");

			invalidLineMarker = CreateTextRenderer ();
			invalidLineMarker.SetText ("~");

			allTextRenderers = new TextRenderer[] {
				textRenderer,
				tabMarker,
				spaceMarker,
				eolMarker,
				invalidLineMarker
			};

			ResetCaretBlink ();
			Caret.PositionChanged += CaretPositionChanged;
			textEditor.Document.EndUndo += UpdateBracketHighlighting;
			textEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs e) {
				if (mouseSelectionMode == MouseSelectionMode.Word && e.Offset < mouseWordStart) {
					int delta = -e.Count;
					if (!string.IsNullOrEmpty (e.Value))
						delta += e.Value.Length;
					mouseWordStart += delta;
					mouseWordEnd += delta;
				}
			};
			Caret.PositionChanged += UpdateBracketHighlighting;
			base.cursor = xtermCursor;
			Document.LineChanged += CheckLongestLine;
			textEditor.HighlightSearchPatternChanged += delegate { selectedRegions.Clear (); };
//			textEditor.SelectionChanged += delegate { DisposeLayoutDict (); };
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
			int endLine = (int)(startLine + textEditor.GetTextEditorData ().VAdjustment.PageSize / lineHeight) + 1;
			foreach (LineSegment line in layoutDict.Keys) {
				int curLine = Document.OffsetToLineNumber (line.Offset);
				if (startLine - 5 >= curLine || endLine + 5 <= curLine) {
					linesToRemove.Add (line);
				}
			}
			linesToRemove.ForEach (line => RemoveCachedLine (line));
			linesToRemove.Clear ();

			ResetCaretBlink ();
			caretBlink = true;
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
			if (textEditor.longestLine == null || args.Line.EditableLength > textEditor.longestLine.EditableLength) {
				textEditor.longestLine = args.Line;
				textEditor.SetAdjustments (textEditor.Allocation);
			}

			if (layoutDict.ContainsKey (args.Line)) {
				RemoveCachedLine (args.Line);
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
			if (Document.IsInUndo)
				return;
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

		protected internal override void OptionsChanged ()
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
			if (textEditor.Options.HighlightMatchingBracket && !Document.ReadOnly)
				DecorateLineBg += DecorateMatchingBracket;
			
			if (tabArray != null) {
				tabArray.Dispose ();
				tabArray = null;
			}
			tabArray = new Pango.TabArray (10, true);
			for (int i = 0; i < 10; i++)
				tabArray.SetTab (i, Pango.TabAlign.Left, (i + 1) * CharWidth * textEditor.Options.TabSize);
			
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
			textEditor.Document.EndUndo -= UpdateBracketHighlighting;
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
			if (tabArray != null) {
				tabArray.Dispose ();
				tabArray = null;
			}
			
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
			caretTimer.Start ();
		}

		void UpdateCaret (object sender, EventArgs args)
		{
			bool newCaretBlink = caretBlinkStatus < 4 || (caretBlinkStatus - 4) % 3 != 0;
			if (textRenderer != null && newCaretBlink != caretBlink) {
				caretBlink = newCaretBlink;
				Application.Invoke (delegate {
					try {
						textEditor.RedrawMarginLine (this, Caret.Line);
					} catch (Exception) {
					}
				});
			}
			caretBlinkStatus++;
		}
		#endregion

		char caretChar;
		internal int caretX;
		int caretY;

		void SetVisibleCaretPosition (Gdk.Drawable win, char ch, int x, int y)
		{
			caretChar = ch;
			caretX = x;
			caretY = y;
		}

		public static Gdk.Rectangle EmptyRectangle = new Gdk.Rectangle (0, 0, 0, 0);
		public Gdk.Rectangle DrawCaret (Gdk.Drawable win)
		{
			if (!this.textEditor.IsInDrag) {
				if (!(this.caretX >= 0 && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0)))
					return EmptyRectangle;
				if (!textEditor.HasFocus)
					return EmptyRectangle;
			}
			if (Settings.Default.CursorBlink && (!Caret.IsVisible || !caretBlink))
				return EmptyRectangle;
			
			// Unlike the clip region for the backing store buffer, this needs
			// to be clipped to the range of the visible area of the widget to
			// prevent drawing outside the widget on some Gdk backends.
			Gdk.Rectangle caretClipRectangle = GetClipRectangle (Caret.Mode);
			int width, height;
			win.GetSize (out width, out height);
			caretClipRectangle.Intersect (new Gdk.Rectangle (0, 0, width, height));
			SetClip (caretClipRectangle);

			switch (Caret.Mode) {
			case CaretMode.Insert:
				if (caretX < this.XOffset)
					return EmptyRectangle;
				else if (caretClipRectangle == Gdk.Rectangle.Zero)
					return EmptyRectangle;
				win.DrawLine (GetGC (ColorStyle.Caret.Color), caretX, caretY, caretX, caretY + LineHeight);
				break;
			case CaretMode.Block:
				if (caretX + this.charWidth < this.XOffset)
					return EmptyRectangle;
				win.DrawRectangle (GetGC (ColorStyle.Caret.Color), true, new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight));
				textRenderer.BeginDraw (win);
//				textRenderer.SetClip (clipRectangle);
				textRenderer.Color = ColorStyle.Caret.BackgroundColor;
				textRenderer.SetText (caretChar.ToString ());
				textRenderer.DrawText (win, caretX, caretY);
				textRenderer.EndDraw ();
				break;
			case CaretMode.Underscore:
				if (caretX + this.charWidth < this.XOffset)
					return EmptyRectangle;
				int bottom = caretY + lineHeight;
				win.DrawLine (GetGC (ColorStyle.Caret.Color), caretX, bottom, caretX + this.charWidth, bottom);
				break;
			}
			return caretClipRectangle;
		}

		Gdk.Rectangle GetClipRectangle (Mono.TextEditor.CaretMode mode)
		{
			switch (mode) {
			case CaretMode.Insert:
				return new Gdk.Rectangle (caretX, caretY, 1, LineHeight);
			case CaretMode.Block:
				return new Gdk.Rectangle (caretX, caretY, this.charWidth, LineHeight);
			case CaretMode.Underscore:
				return new Gdk.Rectangle (caretX, caretY + LineHeight, this.charWidth, 1);
			}
			throw new NotImplementedException ("Unknown caret mode :" + mode);
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
			selectionEnd = -1;
			if (textEditor.IsSomethingSelected) {
				ISegment segment = textEditor.SelectionRange;
				selectionStart = segment.Offset;
				selectionEnd = segment.EndOffset;

				if (textEditor.SelectionMode == SelectionMode.Block) {
					DocumentLocation start = textEditor.MainSelection.Anchor;
					DocumentLocation end = textEditor.MainSelection.Lead;

					DocumentLocation visStart = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), start);
					DocumentLocation visEnd = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), end);

					if (segment.Contains (line.Offset) || segment.Contains (line.EndOffset)) {
						selectionStart = line.Offset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), Document, System.Math.Min (visStart.Column, visEnd.Column));
						selectionEnd = line.Offset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), Document, System.Math.Max (visStart.Column, visEnd.Column));
					}
				}
			}
		}
		#region Layout cache
		class LineDescriptor
		{
			public int Offset {
				get;
				private set;
			}
			public int Length {
				get;
				private set;
			}
			public int MarkerLength {
				get;
				private set;
			}
			public int SpanLength {
				get;
				private set;
			}

			protected LineDescriptor (LineSegment line, int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
				this.MarkerLength = line.MarkerCount;
				if (line.StartSpan != null)
					this.SpanLength = line.StartSpan.Length;
			}

			public bool Equals (LineSegment line, int offset, int length, out bool isInvalid)
			{
				isInvalid = MarkerLength != line.MarkerCount || SpanLength != (line.StartSpan == null ? 0 : line.StartSpan.Length);
				return offset == Offset && Length == length && !isInvalid;
			}
		}

		class LayoutDescriptor : LineDescriptor, IDisposable
		{
			public LayoutWrapper Layout {
				get;
				private set;
			}
			public int SelectionStart {
				get;
				private set;
			}
			public int SelectionEnd {
				get;
				private set;
			}

			public LayoutDescriptor (LineSegment line, int offset, int length, LayoutWrapper layout, int selectionStart, int selectionEnd) : base(line, offset, length)
			{
				this.Layout = layout;
				this.SelectionStart = selectionStart;
				this.SelectionEnd = selectionEnd;
			}
			
			public void Dispose ()
			{
				if (Layout != null) {
					Layout.Dispose ();
					Layout = null;
				}
			}
			
			public bool Equals (LineSegment line, int offset, int length, int selectionStart, int selectionEnd, out bool isInvalid)
			{
				return base.Equals (line, offset, length, out isInvalid) && selectionStart == this.SelectionStart && selectionEnd == this.SelectionEnd;
			}
			
			public override bool Equals (object obj)
			{
				if (obj == null)
					return false;
				if (ReferenceEquals (this, obj))
					return true;
				if (obj.GetType () != typeof(LayoutDescriptor))
					return false;
				Mono.TextEditor.TextViewMargin.LayoutDescriptor other = (Mono.TextEditor.TextViewMargin.LayoutDescriptor)obj;
				return MarkerLength == other.MarkerLength && Offset == other.Offset && Length == other.Length && SpanLength == other.SpanLength  && SelectionStart == other.SelectionStart && SelectionEnd == other.SelectionEnd;
			}

			public override int GetHashCode ()
			{
				unchecked {
					return SelectionStart.GetHashCode () ^ SelectionEnd.GetHashCode ();
				}
			}
			
		}

		Dictionary<LineSegment, LayoutDescriptor> layoutDict = new Dictionary<LineSegment, LayoutDescriptor> ();
		LayoutWrapper GetCachedLayout (LineSegment line, int offset, int length, int selectionStart, int selectionEnd, Action<LayoutWrapper> createNew)
		{
			bool containsPreedit = offset <= textEditor.preeditOffset && textEditor.preeditOffset <= offset + length;
			LayoutDescriptor descriptor;
			if (!containsPreedit && layoutDict.TryGetValue (line, out descriptor)) {
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, selectionStart, selectionEnd, out isInvalid))
					return descriptor.Layout;
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}

			LayoutWrapper wrapper = new LayoutWrapper (new Pango.Layout (textEditor.PangoContext));
			wrapper.IsUncached = containsPreedit;
			createNew (wrapper);
			selectionStart = System.Math.Max (line.Offset - 1, selectionStart);
			selectionEnd = System.Math.Min (line.EndOffset + 1, selectionEnd);
			descriptor = new LayoutDescriptor (line, offset, length, wrapper, selectionStart, selectionEnd);
			if (!containsPreedit)
				layoutDict[line] = descriptor;
			return wrapper;
		}

		public void RemoveCachedLine (LineSegment line)
		{
			LayoutDescriptor descriptor;
			if (layoutDict.TryGetValue (line, out descriptor)) {
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}

			ChunkDescriptor chunkDesriptor;
			if (chunkDict.TryGetValue (line, out chunkDesriptor)) {
				chunkDict.Remove (line);
			}
		}

		internal void DisposeLayoutDict ()
		{
			foreach (LayoutDescriptor descr in layoutDict.Values) {
				descr.Dispose ();
			}
			layoutDict.Clear ();
		}

		class ChunkDescriptor : LineDescriptor
		{
			public Chunk Chunk {
				get;
				private set;
			}

			public ChunkDescriptor (LineSegment line, int offset, int length, Chunk chunk) : base(line, offset, length)
			{
				this.Chunk = chunk;
			}
		}

		Dictionary<LineSegment, ChunkDescriptor> chunkDict = new Dictionary<LineSegment, ChunkDescriptor> ();
		Chunk GetCachedChunks (SyntaxMode mode, Document doc, Mono.TextEditor.Highlighting.Style style, LineSegment line, int offset, int length)
		{
//			return mode.GetChunks (doc, style, line, offset, length);
			ChunkDescriptor descriptor;
			if (chunkDict.TryGetValue (line, out descriptor)) {
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, out isInvalid))
					return descriptor.Chunk;
				chunkDict.Remove (line);
			}

			Chunk chunk = mode.GetChunks (doc, style, line, offset, length);
			descriptor = new ChunkDescriptor (line, offset, length, chunk);
			chunkDict.Add (line, descriptor);
			return chunk;
		}

		public void ForceInvalidateLine (int lineNr)
		{
			LineSegment line = Document.GetLine (lineNr);
			LayoutDescriptor descriptor;
			if (line != null && layoutDict.TryGetValue (line, out descriptor)) {
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}
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

		static uint TranslateToUTF8Index (char[] charArray, uint textIndex, ref uint curIndex, ref uint byteIndex)
		{
			if (textIndex < curIndex) {
				byteIndex = (uint)Encoding.UTF8.GetByteCount (charArray, 0, (int)textIndex);
			} else {
				int count = System.Math.Min ((int)(textIndex - curIndex), charArray.Length - (int)curIndex);

				if (count > 0)
					byteIndex += (uint)Encoding.UTF8.GetByteCount (charArray, (int)curIndex, count);
			}
			curIndex = textIndex;
			return byteIndex;
		}
		
		class LayoutWrapper : IDisposable
		{
			public Pango.Layout Layout {
				get;
				private set;
			}
			
			public bool IsUncached {
				get;
				set;
			}
			
			Pango.AttrList attrList;
			List<Pango.Attribute> attributes = new List<Pango.Attribute> ();
			
			public void Add (Pango.Attribute attribute)
			{
				attributes.Add (attribute);
			}
			
			public LayoutWrapper (Pango.Layout layout)
			{
				this.Layout = layout;
				this.IsUncached = false;
			}
			
			public void SetAttributes ()
			{
				this.attrList = new Pango.AttrList ();
				attributes.ForEach (attr => attrList.Insert (attr));
				Layout.Attributes = attrList;
			}
			
			public void Dispose ()
			{
				if (attributes != null) {
					attributes.ForEach (attr => attr.Dispose ());
					attributes.Clear ();
					attributes = null;
				}
				if (attrList != null) {
					attrList.Dispose ();
					attrList = null;
				}

				if (Layout != null) {
					Layout.Dispose ();
					Layout = null;
				}
			}
		}
		
		LayoutWrapper CreateLinePartLayout (SyntaxMode mode, LineSegment line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return GetCachedLayout (line, offset, length, selectionStart, selectionEnd, delegate(LayoutWrapper wrapper) {
				wrapper.Layout.Alignment = Pango.Alignment.Left;
				wrapper.Layout.FontDescription = textEditor.Options.Font;
				wrapper.Layout.Tabs = tabArray;

				StringBuilder textBuilder = new StringBuilder ();
				Chunk startChunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, offset, length);
				for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
					textBuilder.Append (chunk.GetText (Document));
				}
				string lineText = textBuilder.ToString ();
				bool containsPreedit = offset <= textEditor.preeditOffset && textEditor.preeditOffset <= offset + length;
				uint preeditLength = 0;

				if (containsPreedit) {
					lineText = lineText.Insert (textEditor.preeditOffset - offset, textEditor.preeditString);
					preeditLength = (uint)textEditor.preeditString.Length;
				}
				char[] lineChars = lineText.ToCharArray ();
				int startOffset = offset, endOffset = offset + length;
				uint curIndex = 0, byteIndex = 0;

				ISegment firstSearch;
				int o = offset;
				//	int s;
				while ((firstSearch = GetFirstSearchResult (o, offset + length)) != null) {

					HandleSelection (selectionStart, selectionEnd, firstSearch.Offset, firstSearch.EndOffset, delegate(int start, int end) {
						Pango.AttrBackground backGround = new Pango.AttrBackground (ColorStyle.SearchTextBg.Red, ColorStyle.SearchTextBg.Green, ColorStyle.SearchTextBg.Blue);
						backGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(start - offset), ref curIndex, ref byteIndex);
						backGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(end - offset), ref curIndex, ref byteIndex);
						wrapper.Add (backGround);
					}, null);

					o = System.Math.Max (firstSearch.EndOffset, o + 1);
				}

				uint oldEndIndex = 0;
				for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
					ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (textEditor.ColorStyle) : null;

					foreach (TextMarker marker in line.Markers)
						chunkStyle = marker.GetStyle (chunkStyle);

					if (chunkStyle != null) {
						startOffset = chunk.Offset;
						endOffset = chunk.EndOffset;

						uint startIndex = (uint)(oldEndIndex);
						uint endIndex = (uint)(startIndex + chunk.Length);
						oldEndIndex = endIndex;

						if (containsPreedit) {
							if (textEditor.preeditOffset < startOffset)
								startIndex += preeditLength;
							if (textEditor.preeditOffset < endOffset)
								endIndex += preeditLength;
						}

						HandleSelection (selectionStart, selectionEnd, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {

							Pango.AttrForeground foreGround = new Pango.AttrForeground (chunkStyle.Color.Red, chunkStyle.Color.Green, chunkStyle.Color.Blue);
							foreGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
							foreGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);

							wrapper.Add (foreGround);

							if (!chunkStyle.TransparentBackround) {
								Pango.AttrBackground backGround = new Pango.AttrBackground (chunkStyle.BackgroundColor.Red, chunkStyle.BackgroundColor.Green, chunkStyle.BackgroundColor.Blue);
								backGround.StartIndex = foreGround.StartIndex;
								backGround.EndIndex = foreGround.EndIndex;
								wrapper.Add (backGround);
							}
						}, delegate(int start, int end) {
							Pango.AttrForeground selectedForeground = new Pango.AttrForeground (ColorStyle.Selection.Color.Red, ColorStyle.Selection.Color.Green, ColorStyle.Selection.Color.Blue);
							selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
							selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
							wrapper.Add (selectedForeground);

							Pango.AttrBackground attrBackground = new Pango.AttrBackground (this.ColorStyle.Selection.BackgroundColor.Red, this.ColorStyle.Selection.BackgroundColor.Green, this.ColorStyle.Selection.BackgroundColor.Blue);
							attrBackground.StartIndex = selectedForeground.StartIndex;
							attrBackground.EndIndex = selectedForeground.EndIndex;
							wrapper.Add (attrBackground);

						});

						if (chunkStyle.Bold) {
							Pango.AttrWeight attrWeight = new Pango.AttrWeight (Pango.Weight.Bold);
							attrWeight.StartIndex = startIndex;
							attrWeight.EndIndex = endIndex;
							wrapper.Add (attrWeight);
						}

						if (chunkStyle.Italic) {
							Pango.AttrStyle attrStyle = new Pango.AttrStyle (Pango.Style.Italic);
							attrStyle.StartIndex = startIndex;
							attrStyle.EndIndex = endIndex;
							wrapper.Add (attrStyle);
						}

						if (chunkStyle.Underline) {
							Pango.AttrUnderline attrUnderline = new Pango.AttrUnderline (Pango.Underline.Single);
							attrUnderline.StartIndex = startIndex;
							attrUnderline.EndIndex = endIndex;
							wrapper.Add (attrUnderline);
						}
					}
				}
				if (containsPreedit) {
					Pango.AttrUnderline underline = new Pango.AttrUnderline (Pango.Underline.Single);
					underline.StartIndex = TranslateToUTF8Index (lineChars, (uint)(textEditor.preeditOffset - offset), ref curIndex, ref byteIndex);
					underline.EndIndex = TranslateToUTF8Index (lineChars, (uint)(underline.StartIndex + preeditLength), ref curIndex, ref byteIndex);
					wrapper.Add (underline);
				}

				wrapper.Layout.SetText (lineText);
				wrapper.SetAttributes ();
			});
		}
		#endregion

		public delegate void LineDecorator (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd);
		public event LineDecorator DecorateLineBg;
		public event LineDecorator DecorateLineFg;

		void DecorateSpaces (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			char[] lineChars = layout.Text.ToCharArray ();
			uint curIndex = 0, byteIndex = 0;
			for (int i = 0; i < lineChars.Length; i++) {
				if (lineChars[i] == ' ') {
					Pango.Rectangle pos = layout.IndexToPos ((int)TranslateToUTF8Index (lineChars, (uint)i, ref curIndex, ref byteIndex));
					int xpos = pos.X;
					DrawSpaceMarker (win, selectionStart <= offset + i && offset + i <= selectionEnd, xPos + xpos / 1024, y);
				}
			}
		}

		void DecorateTabs (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			char[] lineChars = layout.Text.ToCharArray ();
			uint curIndex = 0, byteIndex = 0;
			for (int i = 0; i < lineChars.Length; i++) {
				if (lineChars[i] == '\t') {
					Pango.Rectangle pos = layout.IndexToPos ((int)TranslateToUTF8Index (lineChars, (uint)i, ref curIndex, ref byteIndex));
					int xpos = pos.X;
					DrawTabMarker (win, selectionStart <= offset + i && offset + i <= selectionEnd, xPos + xpos / 1024, y);
				}
			}
		}

		void DecorateMatchingBracket (Gdk.Drawable win, Pango.Layout layout, int offset, int length, int xPos, int y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			string lineText = layout.Text;
			if (offset <= highlightBracketOffset && highlightBracketOffset <= offset + length) {
				int index = highlightBracketOffset - offset;
				Pango.Rectangle rect = layout.IndexToPos ((int)TranslateToUTF8Index (lineText.ToCharArray (), (uint)index, ref curIndex, ref byteIndex));

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
			LayoutWrapper layout = CreateLinePartLayout (mode, line, offset, length, selectionStart, selectionEnd);

			Pango.Rectangle ink_rect, logical_rect;
			layout.Layout.GetExtents (out ink_rect, out logical_rect);
			int width = (int)((logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale);

			bool drawBg = true;
			bool drawText = true;
			foreach (TextMarker marker in line.Markers) {
				IBackgroundMarker bgMarker = marker as IBackgroundMarker;
				if (bgMarker == null)
					continue;
				drawText &= bgMarker.DrawBackground (textEditor, win, layout.Layout, false, 				/*selected*/offset, offset + length, y, xPos, xPos + width, ref drawBg);
			}

			if (DecorateLineBg != null)
				DecorateLineBg (win, layout.Layout, offset, length, xPos, y, selectionStart, selectionEnd);

			//			if (drawText)
			win.DrawLayout (GetGC (ColorStyle.Default.Color), xPos, y, layout.Layout);

			if (DecorateLineFg != null)
				DecorateLineFg (win, layout.Layout, offset, length, xPos, y, selectionStart, selectionEnd);


			if (Document.GetLine (Caret.Line) == line) {
				Pango.Rectangle strong_pos, weak_pos;
				int index = Caret.Offset - offset;
				if (offset <= textEditor.preeditOffset && textEditor.preeditOffset < offset + length) {
					index += textEditor.preeditString.Length;
				}
				if (index >= 0 && index < length) {
					uint curIndex = 0, byteIndex = 0;
					layout.Layout.GetCursorPos ((int)TranslateToUTF8Index (layout.Layout.Text.ToCharArray (), (uint)index, ref curIndex, ref byteIndex), out strong_pos, out weak_pos);
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
				marker.Draw (textEditor, win, layout.Layout, false, 				/*selected*/offset, offset + length, y, xPos, xPos + width);
			}

			xPos += width;
			if (layout.IsUncached)
				layout.Dispose ();
		}

		
		ISegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset && this.selectedRegions.Count > 0) {
				ISegment region = new Segment (startOffset, endOffset - startOffset);
				foreach (ISegment segment in this.selectedRegions) {
					if (segment.Contains (startOffset) || segment.Contains (endOffset) || region.Contains (segment)) {
						return segment;
					}
				}
			}
			return null;
		}

		Pango.Weight DefaultWeight {
			get { return textEditor.Options.Font.Weight; }
		}
		Pango.Style DefaultStyle {
			get { return textEditor.Options.Font.Style; }
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

		static internal ulong GetPixel (Color color)
		{
			return (((ulong)color.Red) << 32) | (((ulong)color.Green) << 16) | ((ulong)color.Blue);
		}
		
		public bool inSelectionDrag = false;
		public bool inDrag = false;
		public DocumentLocation clickLocation;
		int mouseWordStart, mouseWordEnd;
		enum MouseSelectionMode
		{
			SingleChar,
			Word,
			WholeLine
		}
		MouseSelectionMode mouseSelectionMode = MouseSelectionMode.SingleChar;

		protected internal override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			inSelectionDrag = false;
			inDrag = false;
			Selection selection = textEditor.MainSelection;
			int anchor = selection != null ? selection.GetAnchorOffset (this.textEditor.GetTextEditorData ()) : -1;
			int oldOffset = textEditor.Caret.Offset;

			string link = GetLink (args);
			if (!String.IsNullOrEmpty (link)) {
				textEditor.FireLinkEvent (link, args.Button, args.ModifierState);
				return;
			}

			if (args.Button == 1 || args.Button == 2) {
				VisualLocationTranslator trans = new VisualLocationTranslator (this, args.X, args.Y);
				clickLocation = trans.VisualToDocumentLocation (args.X, args.Y);
				LineSegment line = Document.GetLine (clickLocation.Line);
				if (line != null && clickLocation.Column >= line.EditableLength && GetWidth (Document.GetTextAt (line) + "-") < args.X) {
					int nextColumn = this.textEditor.GetTextEditorData ().GetNextVirtualColumn (clickLocation.Line, clickLocation.Column);
					clickLocation.Column = nextColumn;
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
					mouseWordStart = ScanWord (offset, false);
					mouseWordEnd = ScanWord (offset, true);
					Caret.Offset = mouseWordEnd;
					textEditor.MainSelection = new Selection (textEditor.Document.OffsetToLocation (mouseWordStart), textEditor.Document.OffsetToLocation (mouseWordEnd));
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
							textEditor.MainSelection = new Selection (Caret.Location, clickLocation);
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
					ResetCaretBlink ();
				}
			}

			DocumentLocation docLocation = VisualToDocumentLocation (args.X, args.Y);
			if (args.Button == 2 && this.textEditor.CanEdit (docLocation.Line)) {
				int offset = Document.LocationToOffset (docLocation);
				int length = ClipboardActions.PasteFromPrimary (textEditor.GetTextEditorData (), offset);
				int newOffset = textEditor.Caret.Offset;
				if (selection != null) {
					ISegment selectionRange = selection.GetSelectionRange (this.textEditor.GetTextEditorData ());
					if (newOffset < selectionRange.EndOffset) {
						oldOffset += length;
						anchor += length;
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
					textEditor.MainSelection = selection;
				} else {
					textEditor.Caret.Offset = oldOffset;
				}
			}
		}

		protected internal override void MouseReleased (MarginMouseEventArgs args)
		{
			if (!inSelectionDrag)
				textEditor.ClearSelection ();
			inSelectionDrag = false;
			if (inDrag)
				Caret.Location = clickLocation;
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
				if (char.IsWhiteSpace (first) && !char.IsWhiteSpace (ch) || IsNoLetterOrDigit (first) && !IsNoLetterOrDigit (ch) || (char.IsLetterOrDigit (first) || first == '_') && !(char.IsLetterOrDigit (ch) || ch == '_'))
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
			LineSegment line = args.LineSegment;
			Mono.TextEditor.Highlighting.Style style = ColorStyle;
			Document doc = Document;
			if (doc == null)
				return null;
			SyntaxMode mode = doc.SyntaxMode;
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

		protected internal override void MouseHover (MarginMouseEventArgs args)
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
						end = ScanWord (textEditor.SelectionAnchor, true);
						Caret.Offset = start;
					} else {
						start = ScanWord (textEditor.SelectionAnchor, false);
						end = ScanWord (offset, true);
						Caret.Offset = end;
					}
					if (textEditor.MainSelection != null) {
						textEditor.MainSelection.Lead = Caret.Location;
						if (Caret.Offset < mouseWordStart) {
							textEditor.MainSelection.Anchor = Document.OffsetToLocation (mouseWordEnd);
						} else {
							textEditor.MainSelection.Anchor = Document.OffsetToLocation (mouseWordStart);
						}
					}
					break;
				case MouseSelectionMode.WholeLine:
					//textEditor.SetSelectLines (loc.Line, textEditor.MainSelection.Anchor.Line);
					LineSegment line1 = textEditor.Document.GetLine (loc.Line);
					LineSegment line2 = textEditor.Document.GetLineByOffset (textEditor.SelectionAnchor);
					Caret.Offset = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffset;
					if (textEditor.MainSelection != null)
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
			return new Gdk.Point (x - (int)this.textEditor.HAdjustment.Value, y - (int)this.textEditor.VAdjustment.Value);
		}

		public int ColumnToVisualX (LineSegment line, int column)
		{
			if (line == null || line.EditableLength == 0)
				return 0;

			Pango.Layout layout = new Pango.Layout (textEditor.PangoContext);
			layout.Alignment = Pango.Alignment.Left;
			layout.FontDescription = textEditor.Options.Font;
			StringBuilder textBuilder = new StringBuilder ();
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			Chunk startChunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, line.Offset, line.EditableLength);
			for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
				textBuilder.Append (chunk.GetText (Document));
			}
			Pango.TabArray tabArray = new Pango.TabArray (10, true);
			for (int i = 0; i < 10; i++)
				tabArray.SetTab (i, Pango.TabAlign.Left, (i + 1) * CharWidth * textEditor.Options.TabSize);

			layout.Tabs = tabArray;

			string lineText = textBuilder.ToString ();
			char[] lineChars = lineText.ToCharArray ();
			bool containsPreedit = line.Offset <= textEditor.preeditOffset && textEditor.preeditOffset <= line.Offset + line.EditableLength;
			uint preeditLength = 0;

			if (containsPreedit) {
				lineText = lineText.Insert (textEditor.preeditOffset - line.Offset, textEditor.preeditString);
				preeditLength = (uint)textEditor.preeditString.Length;
			}
			if (column < lineText.Length)
				lineText = lineText.Substring (0, column);
			layout.SetText (lineText);
			Pango.AttrList attributes = new Pango.AttrList ();

			int startOffset = line.Offset, endOffset = line.Offset + line.EditableLength;
			uint curIndex = 0, byteIndex = 0;

			uint oldEndIndex = 0;
			for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
				ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (textEditor.ColorStyle) : null;

				foreach (TextMarker marker in line.Markers)
					chunkStyle = marker.GetStyle (chunkStyle);

				if (chunkStyle != null) {
					startOffset = chunk.Offset;
					endOffset = chunk.EndOffset;

					uint startIndex = (uint)(oldEndIndex);
					uint endIndex = (uint)(startIndex + chunk.Length);
					oldEndIndex = endIndex;

					if (containsPreedit) {
						if (textEditor.preeditOffset < startOffset)
							startIndex += preeditLength;
						if (textEditor.preeditOffset < endOffset)
							endIndex += preeditLength;
					}

					HandleSelection (-1, -1, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {

						Pango.AttrForeground foreGround = new Pango.AttrForeground (chunkStyle.Color.Red, chunkStyle.Color.Green, chunkStyle.Color.Blue);
						foreGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						foreGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);

						attributes.Insert (foreGround);

					}, delegate(int start, int end) {
						Pango.AttrForeground selectedForeground = new Pango.AttrForeground (ColorStyle.Selection.Color.Red, ColorStyle.Selection.Color.Green, ColorStyle.Selection.Color.Blue);
						selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Insert (selectedForeground);

					});

					if (chunkStyle.Bold) {
						Pango.AttrWeight attrWeight = new Pango.AttrWeight (Pango.Weight.Bold);
						attrWeight.StartIndex = startIndex;
						attrWeight.EndIndex = endIndex;
						attributes.Insert (attrWeight);
					}

					if (chunkStyle.Italic) {
						Pango.AttrStyle attrStyle = new Pango.AttrStyle (Pango.Style.Italic);
						attrStyle.StartIndex = startIndex;
						attrStyle.EndIndex = endIndex;
						attributes.Insert (attrStyle);
					}

					if (chunkStyle.Underline) {
						Pango.AttrUnderline attrUnderline = new Pango.AttrUnderline (Pango.Underline.Single);
						attrUnderline.StartIndex = startIndex;
						attrUnderline.EndIndex = endIndex;
						attributes.Insert (attrUnderline);
					}
				}
			}
			layout.Attributes = attributes;
			Pango.Rectangle ink_rect, logical_rect;
			layout.GetExtents (out ink_rect, out logical_rect);
			layout.Dispose ();
			return (int)((logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale);
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
			return new Color ((byte)(((byte)color.Red * 19) / 20), (byte)(((byte)color.Green * 19) / 20), (byte)(((byte)color.Blue * 19) / 20));
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

		List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> GetFoldRectangles (int lineNr)
		{
			List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> result = new List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> ();
			if (lineNr < 0)
				return result;

			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
//			int xStart = XOffset;
			int y = (int)(Document.LogicalToVisualLine (lineNr) * LineHeight - textEditor.VAdjustment.Value);
//			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height, xadv;
			int xPos = (int)(XOffset - textEditor.HAdjustment.Value);

			if (line == null) {
				return result;
			}

			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			restart:
//			int caretOffset = Caret.Offset;
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
						line = folding.EndLine;
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

		protected internal override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
//			int visibleLine = y / this.LineHeight;
//			this.caretX = -1;

			LineSegment line = lineNr < Document.LineCount ? Document.GetLine (lineNr) : null;
			int xStart = System.Math.Max (area.X, XOffset);

			int clipWidth = area.Right - xStart;
			int clipHeight = LineHeight;

			xStart = System.Math.Max (0, xStart);

			if (clipWidth <= 0)
				clipWidth = win.VisibleRegion.Clipbox.Width - xStart;

			if (clipHeight <= 0)
				clipHeight = win.VisibleRegion.Clipbox.Height;

			SetClip (new Gdk.Rectangle (xStart, y, clipWidth, clipHeight));
			ClipRenderers ();

			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height, xadv;
			int xPos = (int)(x - textEditor.HAdjustment.Value);

			// Draw the default back color for the whole line. Colors other than the default
			// background will be drawn when rendering the text chunks.

			if (textEditor.Options.HighlightCaretLine && Caret.Line == lineNr)
				defaultBgColor = ColorStyle.LineMarker; else
				defaultBgColor = ColorStyle.Default.BackgroundColor;
			DrawRectangleWithRuler (win, x, lineArea, defaultBgColor, true);

			// Check if line is beyond the document length

			if (line == null) {
				if (textEditor.Options.ShowInvalidLines) {
					DrawInvalidLineMarker (win, xPos, y);
				}
				if (textEditor.Options.ShowRuler) {
					// warning: code duplication, look at the method end.
					win.DrawLine (GetGC (ColorStyle.Ruler), x + rulerX, y, x + rulerX, y + LineHeight);
				}
				return;
			}
			//selectedRegions.Clear ();

			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			restart:
			foreach (FoldSegment folding in foldings) {
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
						line = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						goto restart;
					}
				}
			}

			if (textEditor.longestLine == null || line.EditableLength > textEditor.longestLine.EditableLength) {
				textEditor.longestLine = line;
				textEditor.SetAdjustments (textEditor.Allocation);
			}

			// Draw remaining line
			if (line.EndOffset - offset > 0)
				DrawLinePart (win, line, offset, line.Offset + line.EditableLength - offset, ref xPos, y, area.Right);

			bool isEolSelected = textEditor.IsSomethingSelected && textEditor.SelectionMode == SelectionMode.Normal && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength);

			lineArea.X = xPos;
			lineArea.Width = textEditor.Allocation.Width - xPos;

			if (textEditor.SelectionMode == SelectionMode.Block && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength)) {
				DocumentLocation start = textEditor.MainSelection.Anchor;
				DocumentLocation end = textEditor.MainSelection.Lead;

				DocumentLocation visStart = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), start);
				DocumentLocation visEnd = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), end);

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

			if (textEditor.Options.ShowRuler) {
				// warning: code duplication, scroll up.
				win.DrawLine (GetGC (ColorStyle.Ruler), x + rulerX, y, x + rulerX, y + LineHeight);
			}

			if (Caret.Line == lineNr && Caret.Column > line.EditableLength) {
		/*		string virtualText = textEditor.GetTextEditorData ().GetVirtualSpaces (Caret.Line, Caret.Column);
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
				SetVisibleCaretPosition (win, ' ', xPos, y);*/
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

		void ClipRenderers ()
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

		protected internal override void MouseLeft ()
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
			
			TextViewMargin.LayoutWrapper layoutWrapper;
			int index;
			bool ConsumeLayout (int xp, int yp)
			{
				int trailing;
				bool isInside = layoutWrapper.Layout.XyToIndex (xp, yp, out index, out trailing);

				if (isInside) {
					int lineNr;
					int xp1, xp2;
					layoutWrapper.Layout.IndexToLineX (index, false, out lineNr, out xp1);
					layoutWrapper.Layout.IndexToLineX (index + 1, false, out lineNr, out xp2);
					if (System.Math.Abs (xp2 - xp) < System.Math.Abs (xp1 - xp))
						index++;
					return true;
				}
				index = line.EditableLength;
				return false;
			}
			
			public DocumentLocation VisualToDocumentLocation (int xp, int yp)
			{
				if (line == null)
					return DocumentLocation.Empty;

				int offset = line.Offset;
				yp %= margin.LineHeight;
				xp *= (int)Pango.Scale.PangoScale;
				yp *= (int)Pango.Scale.PangoScale;
				int column = 0;
				SyntaxMode mode = margin.Document.SyntaxMode != null && margin.textEditor.Options.EnableSyntaxHighlighting ? margin.Document.SyntaxMode : SyntaxMode.Default;
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				bool done = false;
				restart:
				foreach (FoldSegment folding in foldings.Where (f => f.IsFolded)) {
					int foldOffset = folding.StartLine.Offset + folding.Column;
					if (foldOffset < offset)
						continue;
					layoutWrapper = margin.CreateLinePartLayout (mode, line, line.Offset, foldOffset - offset, -1, -1);
					done |= ConsumeLayout (xp - xPos, yp);
					if (done)
						break;
					int height, width, delta;
					layoutWrapper.Layout.GetPixelSize (out width, out height);
					xPos += width * (int)Pango.Scale.PangoScale;
					if (measureLayout == null) {
						measureLayout = margin.CreateTextRenderer ();
						measureLayout.SentFont (margin.textEditor.Options.Font);
					}

					measureLayout.SetText (folding.Description);
					measureLayout.GetPixelSize (out width, out height, out delta);
					delta *= (int)Pango.Scale.PangoScale;
					xPos += delta;
					if (xPos - delta / 2 >= xp) {
						index = foldOffset - offset;
						done = true;
						break;
					}

					offset = folding.EndLine.Offset + folding.EndColumn;
					DocumentLocation foldingEndLocation = margin.Document.OffsetToLocation (offset);
					lineNumber = foldingEndLocation.Line;
					column = foldingEndLocation.Column;
					if (xPos >= xp) {
						index = 0;
						done = true;
						break;
					}
					if (folding.EndLine != line) {
						line = folding.EndLine;
						foldings = margin.Document.GetStartFoldings (line);
						goto restart;
					}
				}
				if (!done) {
					layoutWrapper = margin.CreateLinePartLayout (mode, line, offset, line.Offset + line.EditableLength - offset, -1, -1);
					ConsumeLayout (xp - xPos, yp);
				}
				measureLayout = measureLayout.Kill ();
				return new DocumentLocation (lineNumber, column + index);
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
