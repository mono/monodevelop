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
using System.Runtime.InteropServices;

using Mono.TextEditor.Highlighting;

using Gdk; 
using Gtk;
using System.Timers;

namespace Mono.TextEditor
{
	public class TextViewMargin : Margin
	{
		readonly TextEditor textEditor;
		Pango.TabArray tabArray;
		
		Pango.Layout markerLayout;
		
		Pango.Layout tabMarkerLayout, spaceMarkerLayout, invalidLineLayout;
		Pango.Layout macEolLayout, unixEolLayout, windowEolLayout, eofEolLayout;
		
		internal double charWidth;
		
		int highlightBracketOffset = -1;
		
		
		double LineHeight {
			get {
				return textEditor.LineHeight;
			}
		}

		public override double Width { 
			get { return -1; }
		}

		double xOffset;
		public override double XOffset {
			get {
				return xOffset;
			}
			internal set {
				if (xOffset != value) {
					xOffset = value;
				}
			}
		}

		public bool AlphaBlendSearchResults {
			get;
			set;
		}

		/// <summary>
		/// Set to true to highlight the caret line temporarly. It's
		/// the same as the option, but is unset when the caret moves.
		/// </summary>
		bool highlightCaretLine;
		public bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				if (highlightCaretLine != value) {
					highlightCaretLine = value;
					RemoveCachedLine (Document.GetLine (Caret.Line));
					Document.CommitLineUpdate (Caret.Line);
				}
			}
		}

		public bool HideSelection {
			get;
			set;
		}

		Caret Caret {
			get { return textEditor.Caret; }
		}

		public Mono.TextEditor.Highlighting.ColorScheme ColorStyle {
			get { return this.textEditor.ColorStyle; }
		}

		public TextDocument Document {
			get { return textEditor.Document; }
		}

		public double CharWidth {
			get { return charWidth; }
		}


		public TextViewMargin (TextEditor textEditor)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");
			this.textEditor = textEditor;

			textEditor.Document.TextReplaced += HandleTextReplaced;
			base.cursor = xtermCursor;
			textEditor.HighlightSearchPatternChanged += delegate {
				selectedRegions.Clear ();
				RefreshSearchMarker ();
			};
			textEditor.Document.LineChanged += TextEditorDocumentLineChanged;
			textEditor.GetTextEditorData ().SearchChanged += HandleSearchChanged;
			markerLayout = PangoUtil.CreateLayout (textEditor);

			textEditor.Document.EndUndo += HandleEndUndo;
			textEditor.SelectionChanged += UpdateBracketHighlighting;
			textEditor.Document.Undone += HandleUndone; 
			textEditor.Document.Redone += HandleUndone;
			
			Caret.PositionChanged += UpdateBracketHighlighting;
			textEditor.VScroll += HandleVAdjustmentValueChanged;
		}

		void HandleUndone (object sender, EventArgs e)
		{
			UpdateBracketHighlighting (this, EventArgs.Empty);
		}

		void HandleEndUndo (object sender, EventArgs e)
		{
			if (!textEditor.Document.IsInAtomicUndo)
				UpdateBracketHighlighting (this, EventArgs.Empty);
		}

		void HandleTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			RemoveCachedLine (Document.GetLineByOffset (e.Offset));
			if (mouseSelectionMode == MouseSelectionMode.Word && e.Offset < mouseWordStart) {
				int delta = e.ChangeDelta;
				mouseWordStart += delta;
				mouseWordEnd += delta;
			}
			
			if (selectedRegions.Count > 0) {
				this.selectedRegions = new List<TextSegment> (this.selectedRegions.AdjustSegments (e));
				RefreshSearchMarker ();
			}
		}

		void TextEditorDocumentLineChanged (object sender, LineEventArgs e)
		{
			RemoveCachedLine (e.Line);
		}

		List<LineSegment> linesToRemove = new List<LineSegment> ();
		void HandleVAdjustmentValueChanged (object sender, EventArgs e)
		{
			int startLine = (int)(textEditor.GetTextEditorData ().VAdjustment.Value / LineHeight);
			int endLine = (int)(startLine + textEditor.GetTextEditorData ().VAdjustment.PageSize / LineHeight) + 1;
			foreach (LineSegment line in layoutDict.Keys) {
				int curLine = Document.OffsetToLineNumber (line.Offset);
				if (startLine - 5 >= curLine || endLine + 5 <= curLine) {
					linesToRemove.Add (line);
				}
			}
			linesToRemove.ForEach (line => RemoveCachedLine (line));
			linesToRemove.Clear ();
			
			textEditor.RequestResetCaretBlink ();
		}

		public void ClearSearchMaker ()
		{
			selectedRegions.Clear ();
		}

		public class SearchWorkerArguments {
			public int FirstLine { get; set; }
			public int LastLine { get; set; }
			public List<TextSegment> OldRegions { get; set; }
			public ISearchEngine Engine { get; set; }
		}

		public void RefreshSearchMarker ()
		{
			if (textEditor.HighlightSearchPattern) {
				DisposeSearchPatternWorker ();
				SearchWorkerArguments args = new SearchWorkerArguments () {
					FirstLine = YToLine (textEditor.VAdjustment.Value),
					LastLine = YToLine (textEditor.Allocation.Height + textEditor.VAdjustment.Value),
					OldRegions = selectedRegions,
					Engine = textEditor.GetTextEditorData ().SearchEngine.Clone ()
				};

				if (string.IsNullOrEmpty (textEditor.SearchPattern)) {
					if (selectedRegions.Count > 0) {
						UpdateRegions (selectedRegions, args);
						selectedRegions.Clear ();
					}
					return;
				}

				searchPatternWorker = new System.ComponentModel.BackgroundWorker ();
				searchPatternWorker.WorkerSupportsCancellation = true;
				searchPatternWorker.DoWork += SearchPatternWorkerDoWork;
				searchPatternWorker.RunWorkerAsync (args);
			}
		}

		void SearchPatternWorkerDoWork (object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			SearchWorkerArguments args = (SearchWorkerArguments)e.Argument;
			System.ComponentModel.BackgroundWorker worker = (System.ComponentModel.BackgroundWorker)sender;
			List<TextSegment> newRegions = new List<TextSegment> ();
			int offset = args.Engine.SearchRequest.SearchRegion.IsInvalid ? 0 : args.Engine.SearchRequest.SearchRegion.Offset;
			do {
				if (worker.CancellationPending)
					return;
				SearchResult result = null;
				try {
					result = args.Engine.SearchForward (worker, offset);
				} catch (Exception ex) {
					Console.WriteLine ("Got exception while search forward:" + ex);
					break;
				}
				if (worker.CancellationPending)
					return;
				if (result == null || result.SearchWrapped)
					break;
				offset = result.EndOffset;
				newRegions.Add (result.Segment);
			} while (true);
			HashSet<int> updateLines = null;
			if (args.OldRegions.Count == newRegions.Count) {
				updateLines = new HashSet<int> ();
				for (int i = 0; i < newRegions.Count; i++) {
					if (worker.CancellationPending)
						return;
					if (args.OldRegions[i].Offset != newRegions[i].Offset || args.OldRegions[i].Length != newRegions[i].Length) {
						int lineNumber = Document.OffsetToLineNumber (args.OldRegions[i].Offset);
						if (lineNumber > args.LastLine)
							break;
						if (lineNumber >= args.FirstLine)
							updateLines.Add (lineNumber);
					}
				}
			}
			Application.Invoke (delegate {
				this.selectedRegions = newRegions;
				if (updateLines != null) {
					foreach (int lineNumber in updateLines) {
//						RemoveCachedLine (Document.GetLine (lineNumber));
						textEditor.Document.RequestUpdate (new LineUpdate (lineNumber));
					}
					textEditor.Document.CommitDocumentUpdate ();
				} else {
					UpdateRegions (args.OldRegions.Concat (newRegions), args);
				}
				OnSearchRegionsUpdated (EventArgs.Empty);
			});
		}

		void UpdateRegions (IEnumerable<TextSegment> regions, SearchWorkerArguments args)
		{
			HashSet<int> updateLines = new HashSet<int> ();

			foreach (TextSegment region in regions) {
				int lineNumber = Document.OffsetToLineNumber (region.Offset);
				if (lineNumber > args.LastLine || lineNumber < args.FirstLine)
					continue;
				updateLines.Add (lineNumber);
			}
			foreach (int lineNumber in updateLines) {
//				RemoveCachedLine (Document.GetLine (lineNumber));
				textEditor.Document.RequestUpdate (new LineUpdate (lineNumber));
			}
			if (updateLines.Count > 0)
				textEditor.Document.CommitDocumentUpdate ();
		}

		void HandleSearchChanged (object sender, EventArgs args)
		{
			RefreshSearchMarker ();
		}



		protected virtual void OnSearchRegionsUpdated (EventArgs e)
		{
			EventHandler handler = this.SearchRegionsUpdated;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler SearchRegionsUpdated;


		void DisposeSearchPatternWorker ()
		{
			if (searchPatternWorker == null)
				return;
			if (searchPatternWorker.IsBusy)
				searchPatternWorker.CancelAsync ();
			searchPatternWorker.DoWork -= SearchPatternWorkerDoWork;
			searchPatternWorker.Dispose ();
			searchPatternWorker = null;
		}


		System.ComponentModel.BackgroundWorker searchPatternWorker;
		System.ComponentModel.BackgroundWorker highlightBracketWorker;
		Gdk.Cursor xtermCursor = new Gdk.Cursor (Gdk.CursorType.Xterm);
		Gdk.Cursor textLinkCursor = new Gdk.Cursor (Gdk.CursorType.Hand1);

		void UpdateBracketHighlighting (object sender, EventArgs e)
		{
			HighlightCaretLine = false;
			
			if (!textEditor.Options.HighlightMatchingBracket || textEditor.IsSomethingSelected) {
				if (highlightBracketOffset >= 0) {
					textEditor.RedrawLine (Document.OffsetToLineNumber (highlightBracketOffset));
					highlightBracketOffset = -1;
				}
				return;
			}

			int offset = Caret.Offset - 1;
			if (Caret.Mode != CaretMode.Insert || (offset >= 0 && offset < Document.TextLength && !TextDocument.IsBracket (Document.GetCharAt (offset))))
				offset++;
			offset = System.Math.Max (0, offset);
			if (highlightBracketOffset >= 0 && (offset >= Document.TextLength || !TextDocument.IsBracket (Document.GetCharAt (offset)))) {
				int old = highlightBracketOffset;
				highlightBracketOffset = -1;
				if (old >= 0)
					textEditor.RedrawLine (Document.OffsetToLineNumber (old));
				return;
			}
			if (offset < 0)
				offset = 0;

			DisposeHighightBackgroundWorker ();

			highlightBracketWorker = new System.ComponentModel.BackgroundWorker ();
			highlightBracketWorker.WorkerSupportsCancellation = true;
			highlightBracketWorker.DoWork += HighlightBracketWorkerDoWork;
			highlightBracketWorker.RunWorkerAsync (offset);
		}

		void HighlightBracketWorkerDoWork (object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			System.ComponentModel.BackgroundWorker worker = (System.ComponentModel.BackgroundWorker)sender;
			int offset = (int)e.Argument;
			int oldIndex = highlightBracketOffset;
			int caretOffset = Caret.Offset;
			int matchingBracket;
			matchingBracket = Document.GetMatchingBracketOffset (worker, offset);
			if (worker.CancellationPending)
				return;
			if (matchingBracket == caretOffset && offset + 1 < Document.TextLength)
				matchingBracket = Document.GetMatchingBracketOffset (worker, offset + 1);
			if (worker.CancellationPending)
				return;
			if (matchingBracket == caretOffset)
				matchingBracket = -1;
			if (matchingBracket != oldIndex) {
				int line1 = oldIndex >= 0 ? Document.OffsetToLineNumber (oldIndex) : -1;
				int line2 = matchingBracket >= 0 ? Document.OffsetToLineNumber (matchingBracket) : -1;
				//DocumentLocation matchingBracketLocation = Document.OffsetToLocation (matchingBracket);
				if (worker.CancellationPending)
					return;
				highlightBracketOffset = matchingBracket;
				Application.Invoke (delegate {
					if (line1 >= 0)
						textEditor.RedrawLine (line1);
					if (line1 != line2 && line2 >= 0)
						textEditor.RedrawLine (line2);
				});
			}
		}

		void DisposeHighightBackgroundWorker ()
		{
			if (highlightBracketWorker == null)
				return;
			if (highlightBracketWorker.IsBusy)
				highlightBracketWorker.CancelAsync ();
			highlightBracketWorker.DoWork -= HighlightBracketWorkerDoWork;
			highlightBracketWorker.Dispose ();
			highlightBracketWorker = null;
		}

		protected internal override void OptionsChanged ()
		{
			DisposeGCs ();

			markerLayout.FontDescription = textEditor.Options.Font;
			markerLayout.FontDescription.Weight = Pango.Weight.Normal;
			markerLayout.FontDescription.Style = Pango.Style.Italic;
			markerLayout.SetText ("_");
			int w, h;
			markerLayout.GetSize (out w, out h);
			
			this.charWidth = w / Pango.Scale.PangoScale;
			if (textEditor.preeditString != null && textEditor.preeditAttrs != null) {
				using (var preeditLayout = PangoUtil.CreateLayout (textEditor)) {
					preeditLayout.SetText (textEditor.preeditString);
					preeditLayout.Attributes = textEditor.preeditAttrs;
					preeditLayout.GetSize (out w, out h);
				}
			}
			this.textEditor.GetTextEditorData ().LineHeight = System.Math.Ceiling (h / Pango.Scale.PangoScale);
			
			markerLayout.FontDescription.Weight = Pango.Weight.Normal;

			Pango.Font font = textEditor.PangoContext.LoadFont (markerLayout.FontDescription);
			if (font != null) {
				Pango.FontMetrics metrics = font.GetMetrics (null);
				this.charWidth = metrics.ApproximateCharWidth / Pango.Scale.PangoScale;

				font.Dispose ();
			}

			textEditor.LineHeight = System.Math.Max (1, LineHeight);

			if (textEditor.Options.ShowInvalidLines && invalidLineLayout == null) {
				invalidLineLayout = PangoUtil.CreateLayout (textEditor);
				invalidLineLayout.SetText ("~");
			}
			
			if (invalidLineLayout != null)
				invalidLineLayout.FontDescription = textEditor.Options.Font;
			
			if (textEditor.Options.ShowEolMarkers && unixEolLayout == null) {
				unixEolLayout = PangoUtil.CreateLayout (textEditor);
				unixEolLayout.SetText ("\\n");
				macEolLayout = PangoUtil.CreateLayout (textEditor);
				macEolLayout.SetText ("\\r");
				windowEolLayout = PangoUtil.CreateLayout (textEditor);
				windowEolLayout.SetText ("\\r\\n");
				eofEolLayout = PangoUtil.CreateLayout (textEditor);
				eofEolLayout.SetText ("<EOF>");
			}
			
			if (unixEolLayout != null)
				unixEolLayout.FontDescription = macEolLayout.FontDescription = windowEolLayout.FontDescription = eofEolLayout.FontDescription = textEditor.Options.Font;
			
			if (textEditor.Options.ShowTabs && tabMarkerLayout == null) {
				tabMarkerLayout = PangoUtil.CreateLayout (textEditor);
				tabMarkerLayout.SetText ("»");
			}
			if (tabMarkerLayout != null)
				tabMarkerLayout.FontDescription = textEditor.Options.Font;

			if (textEditor.Options.ShowSpaces && spaceMarkerLayout == null) {
				spaceMarkerLayout = PangoUtil.CreateLayout (textEditor);
				spaceMarkerLayout.SetText ("·");
			}
			if (spaceMarkerLayout != null)
				spaceMarkerLayout.FontDescription = textEditor.Options.Font;
			
			DecorateLineFg -= DecorateTabs;
			DecorateLineFg -= DecorateSpaces;
			DecorateLineFg -= DecorateTabsAndSpaces;
			
			if (textEditor.Options.ShowTabs && textEditor.Options.ShowSpaces) {
				DecorateLineFg += DecorateTabsAndSpaces;
			} else if (textEditor.Options.ShowTabs) {
				DecorateLineFg += DecorateTabs;
			} else if (textEditor.Options.ShowSpaces) {
				DecorateLineFg += DecorateSpaces;
			} 
			
			DecorateLineBg -= DecorateMatchingBracket;
			if (textEditor.Options.HighlightMatchingBracket && !Document.ReadOnly)
				DecorateLineBg += DecorateMatchingBracket;

			if (tabArray != null) {
				tabArray.Dispose ();
				tabArray = null;
			}

			var tabWidthLayout = PangoUtil.CreateLayout (textEditor, (new string (' ', textEditor.Options.TabSize)));
			tabWidthLayout.Alignment = Pango.Alignment.Left;
			tabWidthLayout.FontDescription = textEditor.Options.Font;
			int tabWidth;
			tabWidthLayout.GetSize (out tabWidth, out h);
			tabWidthLayout.Dispose ();
			tabArray = new Pango.TabArray (1, false);
			tabArray.SetTab (0, Pango.TabAlign.Left, tabWidth);

			DisposeLayoutDict ();
			chunkDict.Clear ();
		}

		void DisposeGCs ()
		{
			ShowTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
		}
		

		public override void Dispose ()
		{
			CancelCodeSegmentTooltip ();
			StopCaretThread ();
			DisposeHighightBackgroundWorker ();
			DisposeSearchPatternWorker ();
			
			textEditor.Document.TextReplaced -= HandleTextReplaced;
			textEditor.Document.LineChanged -= TextEditorDocumentLineChanged;
			textEditor.Document.EndUndo -= HandleEndUndo;
			textEditor.Document.Undone -= HandleUndone; 
			textEditor.Document.Redone -= HandleUndone;
			
			textEditor.Document.EndUndo -= UpdateBracketHighlighting;
			Caret.PositionChanged -= UpdateBracketHighlighting;

			textEditor.GetTextEditorData ().SearchChanged -= HandleSearchChanged;

			textLinkCursor.Dispose ();
			xtermCursor.Dispose ();

			DisposeGCs ();
			if (markerLayout != null)
				markerLayout.Dispose ();
			if (tabMarkerLayout != null)
				tabMarkerLayout.Dispose ();
			if (spaceMarkerLayout != null)
				spaceMarkerLayout.Dispose ();
			if (invalidLineLayout != null)
				invalidLineLayout.Dispose ();
			if (unixEolLayout != null) {
				macEolLayout.Dispose ();
				unixEolLayout.Dispose ();
				windowEolLayout.Dispose ();
				eofEolLayout.Dispose ();
			}
			
			DisposeLayoutDict ();
			if (tabArray != null)
				tabArray.Dispose ();
			base.Dispose ();
		}

		#region Caret blinking
		bool caretBlink = true;
		uint blinkTimeout = 0;
		
		// constants taken from gtk.
		const int cursorOnMultiplier = 2;
		const int cursorOffMultiplier = 1;
		const int cursorDivider = 3;
		
		public void ResetCaretBlink ()
		{
			StopCaretThread ();
			blinkTimeout = GLib.Timeout.Add ((uint)(Gtk.Settings.Default.CursorBlinkTime * cursorOnMultiplier / cursorDivider), UpdateCaret);
			caretBlink = true;
		}

		internal void StopCaretThread ()
		{
			if (blinkTimeout == 0)
				return;
			GLib.Source.Remove (blinkTimeout);
			blinkTimeout = 0;
			caretBlink = false;
		}

		bool UpdateCaret ()
		{
			caretBlink = !caretBlink;
			//			int multiplier = caretBlink ? cursorOnMultiplier : cursorOffMultiplier;
			if (caretBlink) {
				if (Caret.IsVisible)
					DrawCaret (textEditor.GdkWindow, textEditor.Allocation);
			} else {
				textEditor.QueueDrawArea (caretRectangle.X,
				                          (int)(caretRectangle.Y + (textEditor.VAdjustment.Value - caretVAdjustmentValue)),
				                          caretRectangle.Width,
				                          caretRectangle.Height);
			}
			return true;
		}
		#endregion
		
		internal double caretX;
		internal double caretY;

		void SetVisibleCaretPosition (double x, double y)
		{
			if (x == caretX && y == caretY)
				return;
			caretX = x;
			caretY = y;
			
			textEditor.ResetIMContext ();
			
			GtkWorkarounds.SetImCursorLocation (
				textEditor.IMContext,
				textEditor.GdkWindow,
				new Rectangle ((int)caretX, (int)caretY, 0, (int)(LineHeight - 1)));
		}

		public static Gdk.Rectangle EmptyRectangle = new Gdk.Rectangle (0, 0, 0, 0);
		Gdk.Rectangle caretRectangle;
		double caretVAdjustmentValue;
		char GetCaretChar ()
		{
			var offset = Caret.Offset;
			char caretChar;
			if (offset >= 0 && offset < Document.TextLength) {
				caretChar = Document.GetCharAt (offset);
			} else {
				if (textEditor.Options.ShowEolMarkers) {
					// <EOF>
					return '<';
				}
				caretChar = '\0';
			}

			switch (caretChar) {
			case ' ':
				if (textEditor.Options.ShowSpaces)
					return '·';
				break;
			case '\t':
				if (textEditor.Options.ShowTabs)
					return '»';
				break;
			case '\n':
			case '\r':
				if (textEditor.Options.ShowEolMarkers)
					return '\\';
				break;
			}
			return caretChar;
		}

		public void DrawCaret (Gdk.Drawable win, Gdk.Rectangle rect)
		{
			if (!this.textEditor.IsInDrag && !(this.caretX >= 0 && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0))) 
				return;
			if (win == null || Settings.Default.CursorBlink && !Caret.IsVisible || !caretBlink)
				return;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (win)) {
				cr.LineWidth = textEditor.Options.Zoom;
				cr.Antialias = Cairo.Antialias.None;
				var curRect = new Gdk.Rectangle ((int)caretX, (int)caretY, (int)this.charWidth, (int)LineHeight - 1);
				if (curRect != caretRectangle) {
					caretRectangle = curRect;
					textEditor.QueueDrawArea (caretRectangle.X,
					               (int)(caretRectangle.Y + (-textEditor.VAdjustment.Value + caretVAdjustmentValue)),
					               caretRectangle.Width + 1,
					               caretRectangle.Height + 1);
					caretVAdjustmentValue = textEditor.VAdjustment.Value;
				}
				cr.Color = textEditor.ColorStyle.Default.CairoColor;
				switch (Caret.Mode) {
				case CaretMode.Insert:
					cr.DrawLine (textEditor.ColorStyle.Default.CairoColor,
					             caretRectangle.X + 0.5, 
					             caretRectangle.Y + 0.5,
					             caretRectangle.X + 0.5,
					             caretRectangle.Y + caretRectangle.Height);
					break;
				case CaretMode.Block:
					cr.Rectangle (caretRectangle.X + 0.5, caretRectangle.Y + 0.5, caretRectangle.Width, caretRectangle.Height);
					cr.Fill ();
					char caretChar = GetCaretChar ();
					if (!char.IsWhiteSpace (caretChar) && caretChar != '\0') {
						using (var layout = PangoUtil.CreateLayout (textEditor)) {
							layout.FontDescription = textEditor.Options.Font;
							layout.SetText (caretChar.ToString ());
							cr.MoveTo (caretRectangle.X, caretRectangle.Y);
							cr.Color = textEditor.ColorStyle.Default.CairoBackgroundColor;
							cr.ShowLayout (layout);
						}
					}
					break;
				case CaretMode.Underscore:
					cr.DrawLine (textEditor.ColorStyle.Default.CairoColor,
					             caretRectangle.X + 0.5, 
					             caretRectangle.Y + caretRectangle.Height + 0.5,
					             caretRectangle.X + caretRectangle.Width,
					             caretRectangle.Y + caretRectangle.Height + 0.5);
					break;
				}
			}
		}

		void GetSelectionOffsets (LineSegment line, out int selectionStart, out int selectionEnd)
		{
			selectionStart = -1;
			selectionEnd = -1;
			if (textEditor.IsSomethingSelected) {
				TextSegment segment = textEditor.SelectionRange;
				selectionStart = segment.Offset;
				selectionEnd = segment.EndOffset;

				if (textEditor.SelectionMode == SelectionMode.Block) {
					DocumentLocation start = textEditor.MainSelection.Anchor;
					DocumentLocation end = textEditor.MainSelection.Lead;

					DocumentLocation visStart = textEditor.LogicalToVisualLocation (start);
					DocumentLocation visEnd = textEditor.LogicalToVisualLocation (end);
					int lineOffset = line.Offset;
					int lineNumber = Document.OffsetToLineNumber (lineOffset);
					if (textEditor.MainSelection.MinLine <= lineNumber && lineNumber <= textEditor.MainSelection.MaxLine) {
						selectionStart = lineOffset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), System.Math.Min (visStart.Column, visEnd.Column)) - 1;
						selectionEnd = lineOffset + line.GetLogicalColumn (this.textEditor.GetTextEditorData (), System.Math.Max (visStart.Column, visEnd.Column)) - 1;
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
			public Mono.TextEditor.Highlighting.CloneableStack<Mono.TextEditor.Highlighting.Span> Spans {
				get;
				private set;
			}

			protected LineDescriptor (LineSegment line, int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
				this.MarkerLength = line.MarkerCount;
				this.Spans = line.StartSpan;
			}

			public bool Equals (LineSegment line, int offset, int length, out bool isInvalid)
			{
				isInvalid = MarkerLength != line.MarkerCount || !line.StartSpan.Equals (Spans);
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
				if (selectionEnd >= 0) {
					this.SelectionStart = selectionStart;
					this.SelectionEnd = selectionEnd;
				}
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
				int selStart = 0, selEnd = 0;
				if (selectionEnd >= 0) {
					selStart = selectionStart;
					selEnd = selectionEnd;
				}
				return base.Equals (line, offset, length, out isInvalid) && selStart == this.SelectionStart && selEnd == this.SelectionEnd;
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
				return MarkerLength == other.MarkerLength && Offset == other.Offset && Length == other.Length && Spans.Equals (other.Spans) && SelectionStart == other.SelectionStart && SelectionEnd == other.SelectionEnd;
			}

			public override int GetHashCode ()
			{
				unchecked {
					return SelectionStart.GetHashCode () ^ SelectionEnd.GetHashCode ();
				}
			}
		}

		Dictionary<LineSegment, LayoutDescriptor> layoutDict = new Dictionary<LineSegment, LayoutDescriptor> ();
		
		public LayoutWrapper CreateLinePartLayout (ISyntaxMode mode, LineSegment line, int logicalRulerColumn, int offset, int length, int selectionStart, int selectionEnd)
		{
			bool containsPreedit = textEditor.ContainsPreedit (offset, length);
			LayoutDescriptor descriptor;
			if (!containsPreedit && layoutDict.TryGetValue (line, out descriptor)) {
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, selectionStart, selectionEnd, out isInvalid) && descriptor.Layout != null) {
					return descriptor.Layout;
				}
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}
			var wrapper = new LayoutWrapper (PangoUtil.CreateLayout (textEditor));
			wrapper.IsUncached = containsPreedit;

			if (logicalRulerColumn < 0)
				logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData (), textEditor.Options.RulerColumn);
			var atts = new FastPangoAttrList ();
			wrapper.Layout.Alignment = Pango.Alignment.Left;
			wrapper.Layout.FontDescription = textEditor.Options.Font;
			wrapper.Layout.Tabs = tabArray;
			StringBuilder textBuilder = new StringBuilder ();
			var chunks = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, offset, length);
			foreach (var chunk in chunks) {
				try {
					textBuilder.Append (Document.GetTextAt (chunk));
				} catch {
					Console.WriteLine (chunk);
				}
			}
			var spanStack = line.StartSpan;
			int lineOffset = line.Offset;
			string lineText = textBuilder.ToString ();
			uint preeditLength = 0;
			
			if (containsPreedit) {
				lineText = lineText.Insert (textEditor.preeditOffset - offset, textEditor.preeditString);
				preeditLength = (uint)textEditor.preeditString.Length;
			}
			char[] lineChars = lineText.ToCharArray ();
			//int startOffset = offset, endOffset = offset + length;
			uint curIndex = 0, byteIndex = 0;
			uint curChunkIndex = 0, byteChunkIndex = 0;
			
			uint oldEndIndex = 0;
			foreach (Chunk chunk in chunks) {
				ChunkStyle chunkStyle = chunk != null ? textEditor.ColorStyle.GetChunkStyle (chunk) : null;
				spanStack = chunk.SpanStack ?? spanStack;
				foreach (TextMarker marker in line.Markers)
					chunkStyle = marker.GetStyle (chunkStyle);

				if (chunkStyle != null) {
					//startOffset = chunk.Offset;
					//endOffset = chunk.EndOffset;

					uint startIndex = (uint)(oldEndIndex);
					uint endIndex = (uint)(startIndex + chunk.Length);
					oldEndIndex = endIndex;

					HandleSelection (lineOffset, logicalRulerColumn, selectionStart, selectionEnd, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {
						if (containsPreedit) {
							if (textEditor.preeditOffset < start)
								start += (int)preeditLength;
							if (textEditor.preeditOffset < end)
								end += (int)preeditLength;
						}
						var si = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						var ei = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						atts.AddForegroundAttribute (chunkStyle.Color, si, ei);
						
						if (!chunkStyle.TransparentBackround && GetPixel (ColorStyle.Default.BackgroundColor) != GetPixel (chunkStyle.BackgroundColor)) {
							wrapper.AddBackground (chunkStyle.CairoBackgroundColor, (int)si, (int)ei);
						} else if (chunk.SpanStack != null && ColorStyle != null) {
							foreach (var span in chunk.SpanStack) {
								if (span == null)
									continue;
								var spanStyle = ColorStyle.GetChunkStyle (span.Color);
								if (!spanStyle.TransparentBackround && GetPixel (ColorStyle.Default.BackgroundColor) != GetPixel (spanStyle.BackgroundColor)) {
									wrapper.AddBackground (spanStyle.CairoBackgroundColor, (int)si, (int)ei);
									break;
								}
							}
						}
					}, delegate(int start, int end) {
						if (containsPreedit) {
							if (textEditor.preeditOffset < start)
								start += (int)preeditLength;
							if (textEditor.preeditOffset < end)
								end += (int)preeditLength;
						}
						var si = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						var ei = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						atts.AddForegroundAttribute (SelectionColor.Color, si, ei);
						if (!wrapper.StartSet)
							wrapper.SelectionStartIndex = (int)si;
						wrapper.SelectionEndIndex = (int)ei;
					});

					var translatedStartIndex = TranslateToUTF8Index (lineChars, (uint)startIndex, ref curChunkIndex, ref byteChunkIndex);
					var translatedEndIndex = TranslateToUTF8Index (lineChars, (uint)endIndex, ref curChunkIndex, ref byteChunkIndex);

					if (chunkStyle.Bold)
						atts.AddWeightAttribute (Pango.Weight.Bold, translatedStartIndex, translatedEndIndex);

					if (chunkStyle.Italic)
						atts.AddStyleAttribute (Pango.Style.Italic, translatedStartIndex, translatedEndIndex);

					if (chunkStyle.Underline)
						atts.AddUnderlineAttribute (Pango.Underline.Single, translatedStartIndex, translatedEndIndex);
				}
			}
			if (containsPreedit) {
				var si = TranslateToUTF8Index (lineChars, (uint)(textEditor.preeditOffset - offset), ref curIndex, ref byteIndex);
				var ei = TranslateToUTF8Index (lineChars, (uint)(textEditor.preeditOffset - offset + preeditLength), ref curIndex, ref byteIndex);
				atts.Splice (textEditor.preeditAttrs, (int)si, (int)(ei - si));
			}
			wrapper.LineChars = lineChars;
			wrapper.Layout.SetText (lineText);
			wrapper.EolSpanStack = spanStack;
			atts.AssignTo (wrapper.Layout);
			atts.Dispose ();
			int w, h;
			wrapper.Layout.GetSize (out w, out h);
			wrapper.PangoWidth = w;

			selectionStart = System.Math.Max (line.Offset - 1, selectionStart);
			selectionEnd = System.Math.Min (line.EndOffsetIncludingDelimiter + 1, selectionEnd);
			descriptor = new LayoutDescriptor (line, offset, length, wrapper, selectionStart, selectionEnd);
			if (!containsPreedit)
				layoutDict[line] = descriptor;
			return wrapper;
		}

		public void RemoveCachedLine (LineSegment line)
		{
			if (line == null)
				return;
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

		public void PurgeLayoutCache ()
		{
			DisposeLayoutDict ();
			if (chunkDict != null)
				chunkDict.Clear ();
		}

		class ChunkDescriptor : LineDescriptor
		{
			public Chunk[] Chunk {
				get;
				private set;
			}
			public ChunkDescriptor (LineSegment line, int offset, int length, Chunk[] chunk) : base(line, offset, length)
			{
				this.Chunk = chunk;
			}
		}

		Dictionary<LineSegment, ChunkDescriptor> chunkDict = new Dictionary<LineSegment, ChunkDescriptor> ();
		IEnumerable<Chunk> GetCachedChunks (ISyntaxMode mode, TextDocument doc, Mono.TextEditor.Highlighting.ColorScheme style, LineSegment line, int offset, int length)
		{
			ChunkDescriptor descriptor;
			if (chunkDict.TryGetValue (line, out descriptor)) {
				bool isInvalid;
				if (descriptor.Equals (line, offset, length, out isInvalid))
					return descriptor.Chunk;
				chunkDict.Remove (line);
			}

			Chunk[] chunks = mode.GetChunks (style, line, offset, length).ToArray ();
			descriptor = new ChunkDescriptor (line, offset, length, chunks);
			chunkDict[line] = descriptor;
			return chunks;
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
		static void InternalHandleSelection (int selectionStart, int selectionEnd, int startOffset, int endOffset, HandleSelectionDelegate handleNotSelected, HandleSelectionDelegate handleSelected)
		{
			if (startOffset >= selectionStart && endOffset <= selectionEnd) {
				if (handleSelected != null)
					handleSelected (startOffset, endOffset);
			} else if (startOffset >= selectionStart && startOffset <= selectionEnd && endOffset >= selectionEnd) {
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

		void HandleSelection (int lineOffset, int logicalRulerColumn, int selectionStart, int selectionEnd, int startOffset, int endOffset, HandleSelectionDelegate handleNotSelected, HandleSelectionDelegate handleSelected)
		{
			int selectionStartColumn = selectionStart - lineOffset;
			int selectionEndColumn = selectionEnd - lineOffset;
			int rulerOffset = lineOffset + logicalRulerColumn;
			if (textEditor.Options.ShowRuler && selectionStartColumn < logicalRulerColumn && logicalRulerColumn < selectionEndColumn && startOffset < rulerOffset && rulerOffset < endOffset) {
				InternalHandleSelection (selectionStart, selectionEnd, startOffset, rulerOffset, handleNotSelected, handleSelected);
				InternalHandleSelection (selectionStart, selectionEnd, rulerOffset, endOffset, handleNotSelected, handleSelected);
			} else {
				InternalHandleSelection (selectionStart, selectionEnd, startOffset, endOffset, handleNotSelected, handleSelected);
			}
		}

		public static uint TranslateToUTF8Index (char[] charArray, uint textIndex, ref uint curIndex, ref uint byteIndex)
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

		public static int TranslateIndexToUTF8 (string text, int index)
		{
			byte[] bytes = Encoding.UTF8.GetBytes (text);
			return Encoding.UTF8.GetString (bytes, 0, index).Length;
		}

		public class LayoutWrapper : IDisposable
		{
			public Pango.Layout Layout {
				get;
				private set;
			}

			public bool IsUncached {
				get;
				set;
			}
			public bool StartSet {
				get;
				set;
			}

			public char[] LineChars {
				get;
				set;
			}

			public CloneableStack<Mono.TextEditor.Highlighting.Span> EolSpanStack {
				get;
				set;
			}

			int selectionStartIndex;
			public int SelectionStartIndex {
				get {
					return selectionStartIndex;
				}
				set {
					selectionStartIndex = value;
					StartSet = true;
				}
			}

			public int SelectionEndIndex {
				get;
				set;
			}

			public int PangoWidth {
				get;
				set;
			}

			public LayoutWrapper (Pango.Layout layout)
			{
				this.Layout = layout;
				this.IsUncached = false;
			}

			public void Dispose ()
			{
				if (Layout != null) {
					Layout.Dispose ();
					Layout = null;
				}
			}

			public class BackgroundColor
			{
				public readonly Cairo.Color Color;
				public readonly int FromIdx;
				public readonly int ToIdx;

				public BackgroundColor (Cairo.Color color, int fromIdx, int toIdx)
				{
					this.Color = color;
					this.FromIdx = fromIdx;
					this.ToIdx = toIdx;
				}
			}

			List<BackgroundColor> backgroundColors = null;
			public List<BackgroundColor> BackgroundColors {
				get {
					return backgroundColors ?? new List<BackgroundColor> ();
				}
			}

			public void AddBackground (Cairo.Color color, int fromIdx, int toIdx)
			{
				if (backgroundColors == null)
					backgroundColors = new List<BackgroundColor> ();
				BackgroundColors.Add (new BackgroundColor (color, fromIdx, toIdx));
			}
		}

		ChunkStyle SelectionColor {
			get {
				return textEditor.HasFocus ? ColorStyle.Selection : ColorStyle.InactiveSelection;
			}
		}

		public LayoutWrapper CreateLinePartLayout (ISyntaxMode mode, LineSegment line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return CreateLinePartLayout (mode, line, -1, offset, length, selectionStart, selectionEnd);
		}

		#endregion

		public delegate void LineDecorator (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd);
		public event LineDecorator DecorateLineBg;
		public event LineDecorator DecorateLineFg;

		void DrawSpaceMarker (Cairo.Context cr, bool selected, double x, double y)
		{
			cr.Save ();
			cr.Translate (x, y);
			cr.ShowLayout (spaceMarkerLayout);
			cr.Restore ();
		}

		void DecorateSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			int index, trailing;
			layout.Layout.XyToIndex ((int)textEditor.HAdjustment.Value, 0, out index, out trailing);

			for (int i = index; i < layout.LineChars.Length; i++) {
				if (layout.LineChars [i] == ' ') {
					bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
					if (first || oldSelected != selected) {
						ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
						first = false;
						oldSelected = selected;
					}
					Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
					double xpos = xPos + pos.X / Pango.Scale.PangoScale;
					if (xpos > textEditor.Allocation.Width)
						break;
					DrawSpaceMarker (ctx, selected, xpos, y);
				}
			}
		}

		void DrawTabMarker (Cairo.Context cr, bool selected, double x, double y)
		{
			cr.Save ();
			cr.Translate (x, y);
			cr.ShowLayout (tabMarkerLayout);
			cr.Restore ();
		}
		
		void DecorateTabs (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			int index, trailing;
			layout.Layout.XyToIndex ((int)textEditor.HAdjustment.Value, 0, out index, out trailing);

			for (int i = index; i < layout.LineChars.Length; i++) {
				if (layout.LineChars[i] == '\t') {
					bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
					if (first || oldSelected != selected) {
						ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
						first = false;
						oldSelected = selected;
					}
					Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
					double xpos = xPos + pos.X / Pango.Scale.PangoScale;
					if (xpos > textEditor.Allocation.Width)
						break;
					DrawTabMarker (ctx, selected, xpos, y);
				}
			}
		}

		void DecorateTabsAndSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			int index, trailing;
			layout.Layout.XyToIndex ((int)textEditor.HAdjustment.Value, 0, out index, out trailing);

			for (int i = index; i < layout.LineChars.Length; i++) {
				char ch = layout.LineChars [i];
				if (ch != ' ' && ch != '\t')
					continue;
				bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
				if (first || oldSelected != selected) {
					ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
					first = false;
					oldSelected = selected;
				}
				Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
				double xpos = xPos + pos.X / Pango.Scale.PangoScale;
				if (xpos > textEditor.Allocation.Width)
					break;
				if (ch == '\t') {
					DrawTabMarker (ctx, selected, xpos, y);
				} else {
					DrawSpaceMarker (ctx, selected, xpos, y);
				}
			}
		}

		void DecorateMatchingBracket (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			if (offset <= highlightBracketOffset && highlightBracketOffset <= offset + length) {
				int index = highlightBracketOffset - offset;
				Pango.Rectangle rect = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)index, ref curIndex, ref byteIndex));
				
				var bracketMatch = new Cairo.Rectangle (xPos + rect.X / Pango.Scale.PangoScale + 0.5, y + 0.5, (rect.Width / Pango.Scale.PangoScale) - 1, (rect.Height / Pango.Scale.PangoScale) - 1);
				if (BackgroundRenderer == null) {
					ctx.Color = this.ColorStyle.BracketHighlightRectangle.CairoBackgroundColor;
					ctx.Rectangle (bracketMatch);
					ctx.FillPreserve ();
					ctx.Color = this.ColorStyle.BracketHighlightRectangle.CairoColor;
					ctx.Stroke ();
				}
			}
		}

		public LayoutWrapper GetLayout (LineSegment line)
		{
			ISyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);
			return CreateLinePartLayout (mode, line, line.Offset, line.Length, -1, -1);
		}

		public void DrawCaretLineMarker (Cairo.Context cr, double xPos, double y, double width)
		{
			cr.Rectangle (xPos, y, width, LineHeight);
			var color = ColorStyle.LineMarker;
			cr.Color = new Cairo.Color (color.R, color.G, color.B, 0.5);
			cr.Fill ();
			double halfLine = (cr.LineWidth / 2.0);
			cr.MoveTo (xPos, y + halfLine);
			cr.LineTo (xPos + width, y + halfLine);
			cr.MoveTo (xPos, y + LineHeight - halfLine);
			cr.LineTo (xPos + width, y + LineHeight - halfLine);
			cr.Color = color;
			cr.Stroke ();
		}

		void DrawLinePart (Cairo.Context cr, LineSegment line, int lineNumber, int logicalRulerColumn, int offset, int length, ref double pangoPosition, ref bool isSelectionDrawn, double y, double maxX)
		{
			ISyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);
			int selectionStart;
			int selectionEnd;
			if (this.HideSelection) {
				selectionStart = selectionEnd = -1;
			} else {
				GetSelectionOffsets (line, out selectionStart, out selectionEnd);
			}

			// ---- new renderer
			LayoutWrapper layout = CreateLinePartLayout (mode, line, logicalRulerColumn, offset, length, selectionStart, selectionEnd);
			int lineOffset = line.Offset;
			double width = layout.PangoWidth / Pango.Scale.PangoScale;
			double xPos = pangoPosition / Pango.Scale.PangoScale;

			//		if (!(HighlightCaretLine || textEditor.Options.HighlightCaretLine) || Document.GetLine(Caret.Line) != line) {
			foreach (var bg in layout.BackgroundColors) {
				int x1, x2;
				x1 = layout.Layout.IndexToPos (bg.FromIdx).X;
				x2 = layout.Layout.IndexToPos (bg.ToIdx).X;
				DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition,
						new Cairo.Rectangle ((x1 + pangoPosition) / Pango.Scale.PangoScale, y, (x2 - x1) / Pango.Scale.PangoScale + 1, LineHeight),
						bg.Color, true);
			}
			//		}

			bool drawBg = true;
			bool drawText = true;
			foreach (TextMarker marker in line.Markers) {
				IBackgroundMarker bgMarker = marker as IBackgroundMarker;
				if (bgMarker == null || !marker.IsVisible)
					continue;
				isSelectionDrawn |= (marker.Flags & TextMarkerFlags.DrawsSelection) == TextMarkerFlags.DrawsSelection;
				drawText &= bgMarker.DrawBackground (textEditor, cr, layout, selectionStart, selectionEnd, offset, offset + length, y, xPos, xPos + width, ref drawBg);
			}

			if (DecorateLineBg != null)
				DecorateLineBg (cr, layout, offset, length, xPos, y, selectionStart, selectionEnd);
			
			if ((HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber)
				DrawCaretLineMarker (cr, xPos, y, layout.PangoWidth / Pango.Scale.PangoScale);

			if (!isSelectionDrawn && (layout.StartSet || selectionStart == offset + length)) {
				double startX;
				double endX;

				if (selectionStart != offset + length) {
					var start = layout.Layout.IndexToPos ((int)layout.SelectionStartIndex);
					startX = start.X / Pango.Scale.PangoScale;
					var end = layout.Layout.IndexToPos ((int)layout.SelectionEndIndex);
					endX = end.X / Pango.Scale.PangoScale;
				} else {
					startX = width;
					endX = startX;
				}

				if (textEditor.MainSelection.SelectionMode == SelectionMode.Block && startX == endX) {
					endX = startX + 2;
				}
				DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (xPos + startX, y, endX - startX, LineHeight), this.SelectionColor.CairoBackgroundColor, true);
			}

			// highlight search results
			TextSegment firstSearch;
			int o = offset;
			uint curIndex = 0, byteIndex = 0;
			if (textEditor.HighlightSearchPattern) {
				while (!(firstSearch = GetFirstSearchResult (o, offset + length)).IsInvalid) {
					double x = pangoPosition;
					HandleSelection (lineOffset, logicalRulerColumn, selectionStart, selectionEnd, System.Math.Max (lineOffset, firstSearch.Offset), System.Math.Min (lineOffset + line.Length, firstSearch.EndOffset), delegate(int start, int end) {
						uint startIndex = (uint)(start - offset);
						uint endIndex = (uint)(end - offset);
						if (startIndex < endIndex && endIndex <= layout.LineChars.Length) {
							uint startTranslated = TranslateToUTF8Index (layout.LineChars, startIndex, ref curIndex, ref byteIndex);
							uint endTranslated = TranslateToUTF8Index (layout.LineChars, endIndex, ref curIndex, ref byteIndex);
							
							int l, x1, x2;
							layout.Layout.IndexToLineX ((int)startTranslated, false, out l, out x1);
							layout.Layout.IndexToLineX ((int)endTranslated, false, out l, out x2);
							x1 += (int)x;
							x2 += (int)x;
							x1 /= (int)Pango.Scale.PangoScale;
							x2 /= (int)Pango.Scale.PangoScale;
	
							cr.Color = MainSearchResult.IsInvalid || MainSearchResult.Offset != firstSearch.Offset ? ColorStyle.SearchTextBg : ColorStyle.SearchTextMainBg;
							FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, x1, y, System.Math.Min (10, width) * textEditor.Options.Zoom, x2 - x1, LineHeight);
							cr.Fill ();
						}
					}, null);
	
					o = System.Math.Max (firstSearch.EndOffset, o + 1);
				}
			}
			
			cr.Save ();
			cr.Translate (xPos, y);
			cr.ShowLayout (layout.Layout);
			cr.Restore ();
			
			if (DecorateLineFg != null)
				DecorateLineFg (cr, layout, offset, length, xPos, y, selectionStart, selectionEnd);

			if (lineNumber == Caret.Line) {
				int caretOffset = Caret.Offset;
				if (offset <= caretOffset && caretOffset <= offset + length) {
					int index = caretOffset - offset;

					if (Caret.Column > line.Length + 1) {
						string virtualSpace = "";
						var data = textEditor.GetTextEditorData ();
						if (data.HasIndentationTracker && line.Length == 0) {
							virtualSpace = this.textEditor.GetTextEditorData ().GetIndentationString (Caret.Location);
						}
						if (Caret.Column > line.Length + 1 + virtualSpace.Length) 
							virtualSpace += new string (' ', Caret.Column - line.Length - 1 - virtualSpace.Length);
						LayoutWrapper wrapper = new LayoutWrapper (PangoUtil.CreateLayout (textEditor));
						wrapper.LineChars = virtualSpace.ToCharArray ();
						wrapper.Layout.SetText (virtualSpace);
						wrapper.Layout.Tabs = tabArray;
						wrapper.Layout.FontDescription = textEditor.Options.Font;
						int vy, vx;
						wrapper.Layout.GetSize (out vx, out vy);
						
						SetVisibleCaretPosition (((pangoPosition + vx + layout.PangoWidth) / Pango.Scale.PangoScale), y);
						xPos = (pangoPosition + layout.PangoWidth) / Pango.Scale.PangoScale;

						if (!isSelectionDrawn && (selectionEnd == lineOffset + line.Length)) {
							double startX;
							double endX;
							startX = xPos;
							endX = (pangoPosition + vx + layout.PangoWidth) / Pango.Scale.PangoScale;
							DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (startX, y, endX - startX, LineHeight), this.SelectionColor.CairoBackgroundColor, true);
						}
						if ((HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber)
							DrawCaretLineMarker (cr, pangoPosition / Pango.Scale.PangoScale, y, vx / Pango.Scale.PangoScale);

						// When drawing virtual space before the selection start paint it as unselected.
						var virtualSpaceMod = selectionStart < caretOffset ? 0 : virtualSpace.Length;

						if (DecorateLineBg != null)
							DecorateLineBg (cr, wrapper, offset, length, xPos, y, selectionStart + virtualSpaceMod, selectionEnd + virtualSpace.Length);
						if (DecorateLineFg != null)
							DecorateLineFg (cr, wrapper, offset, length, xPos, y, selectionStart + virtualSpaceMod, selectionEnd + virtualSpace.Length);
						wrapper.Dispose ();
						pangoPosition += vx;
					} else if (index == length && textEditor.preeditString == null) {
						SetVisibleCaretPosition ((pangoPosition + layout.PangoWidth) / Pango.Scale.PangoScale, y);
					} else if (index >= 0 && index <= length) {
						Pango.Rectangle strong_pos, weak_pos;
						curIndex = byteIndex = 0;
						int utf8ByteIndex = (int)TranslateToUTF8Index (layout.LineChars, (uint)index, ref curIndex, ref byteIndex);
						if (textEditor.preeditString != null && textEditor.preeditCursorCharIndex > 0) {
							curIndex = byteIndex = 0;
							int preeditUtf8ByteIndex = (int)TranslateToUTF8Index (textEditor.preeditString.ToCharArray (),
								(uint)textEditor.preeditCursorCharIndex,
								ref curIndex, ref byteIndex);
							utf8ByteIndex += preeditUtf8ByteIndex;
						}
						layout.Layout.GetCursorPos (utf8ByteIndex, out strong_pos, out weak_pos);
						SetVisibleCaretPosition (xPos + (strong_pos.X / Pango.Scale.PangoScale), y);
					}
				}
			}

			foreach (TextMarker marker in line.Markers.Where (m => m.IsVisible)) {
				if (layout.Layout != null)
					marker.Draw (textEditor, cr, layout.Layout, false, /*selected*/offset, offset + length, y, xPos, xPos + width);
			}

			pangoPosition += layout.PangoWidth;
			if (layout.IsUncached)
				layout.Dispose ();
		}


		TextSegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset && this.selectedRegions.Count > 0) {
				var region = new TextSegment (startOffset, endOffset - startOffset);
				int min = 0;
				int max = selectedRegions.Count - 1;
				do {
					int mid = (min + max) / 2;
					TextSegment segment = selectedRegions [mid];
					if (segment.Contains (startOffset) || segment.Contains (endOffset) || region.Contains (segment)) {
						if (mid == 0)
							return segment;
						TextSegment prevSegment = selectedRegions [mid - 1];
						if (!(prevSegment.Contains (startOffset) || prevSegment.Contains (endOffset) || region.Contains (prevSegment)))
							return segment;
						max = mid - 1;
						continue;
					}

					if (segment.Offset < endOffset) {
						min = mid + 1;
					} else {
						max = mid - 1;
					}

				} while (min <= max);
			}
			return TextSegment.Invalid;
		}

		void DrawEolMarker (Cairo.Context cr, LineSegment line, bool selected, double x, double y)
		{
			Pango.Layout layout;
			switch (line.DelimiterLength) {
			case 0:
				// an emty line end should only happen at eof
				layout = eofEolLayout;
				break;
			case 1:
				if (Document.GetCharAt (line.Offset + line.Length) == '\n') {
					layout = unixEolLayout;
				} else {
					layout = macEolLayout;
				}
				break;
			case 2:
				layout = windowEolLayout;
				break;
			default:
				throw new InvalidOperationException (); // other line endings are not known.
			}
			cr.Save ();
			cr.Translate (x, y);
			cr.Color = selected ? SelectionColor.CairoColor : ColorStyle.EolWhitespaceMarker;
			cr.ShowLayout (layout);
			cr.Restore ();
		}
		

		void DrawInvalidLineMarker (Cairo.Context cr, double x, double y)
		{
			cr.Save ();
			cr.Translate (x, y);
			cr.Color = ColorStyle.InvalidLineMarker;
			cr.ShowLayout (invalidLineLayout);
			cr.Restore ();
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
			
			if (args.TriggersContextMenu ())
				return;
			
			inSelectionDrag = false;
			inDrag = false;
			Selection selection = textEditor.MainSelection;
			int anchor = selection != null ? selection.GetAnchorOffset (this.textEditor.GetTextEditorData ()) : -1;
			int oldOffset = textEditor.Caret.Offset;

			string link = GetLink != null ? GetLink (args) : null;
			if (!String.IsNullOrEmpty (link)) {
				textEditor.FireLinkEvent (link, args.Button, args.ModifierState);
				return;
			}

			if (args.Button == 1) {
				VisualLocationTranslator trans = new VisualLocationTranslator (this);
				clickLocation = trans.PointToLocation (args.X, args.Y);
				if (clickLocation.Line < DocumentLocation.MinLine || clickLocation.Column < DocumentLocation.MinColumn)
					return;
				LineSegment line = Document.GetLine (clickLocation.Line);
				bool isHandled = false;
				if (line != null) {
					foreach (TextMarker marker in line.Markers) {
						if (marker is IActionTextMarker) {
							isHandled |= ((IActionTextMarker)marker).MousePressed (this.textEditor, args);
							if (isHandled)
								break;
						}
					}
				}
				if (isHandled)
					return;
				if (line != null && clickLocation.Column >= line.Length + 1 && GetWidth (Document.GetTextAt (line.SegmentIncludingDelimiter) + "-") < args.X) {
					clickLocation = new DocumentLocation (clickLocation.Line, line.Length + 1);
					if (textEditor.GetTextEditorData ().HasIndentationTracker && textEditor.Options.IndentStyle == IndentStyle.Virtual) {
						int indentationColumn = this.textEditor.GetTextEditorData ().GetVirtualIndentationColumn (clickLocation);
						if (indentationColumn > clickLocation.Column)
							clickLocation = new DocumentLocation (clickLocation.Line, indentationColumn);
					}
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
					var data = textEditor.GetTextEditorData ();
					mouseWordStart = data.FindCurrentWordStart (offset);
					mouseWordEnd = data.FindCurrentWordEnd (offset);
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
					if ((args.ModifierState & Gdk.ModifierType.ShiftMask) == ModifierType.ShiftMask) {
						inSelectionDrag = true;
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
						inSelectionDrag = false;
						textEditor.ClearSelection ();
						Caret.Location = clickLocation;
					}
					textEditor.RequestResetCaretBlink ();
				}
			}

			DocumentLocation docLocation = PointToLocation (args.X, args.Y);
			if (docLocation.Line < DocumentLocation.MinLine || docLocation.Column < DocumentLocation.MinColumn)
				return;
			
			// disable middle click on windows.
			if (!Platform.IsWindows && args.Button == 2 && this.textEditor.CanEdit (docLocation.Line)) {
				TextSegment selectionRange = TextSegment.Invalid;
				int offset = Document.LocationToOffset (docLocation);
				if (selection != null)
					selectionRange = selection.GetSelectionRange (this.textEditor.GetTextEditorData ());

				bool autoScroll = textEditor.Caret.AutoScrollToCaret;
				textEditor.Caret.AutoScrollToCaret = false;
				int length = ClipboardActions.PasteFromPrimary (textEditor.GetTextEditorData (), offset);
				textEditor.Caret.Offset = oldOffset;
				if (selection != null) {
					if (offset < selectionRange.EndOffset) {
						oldOffset += length;
						anchor += length;
						selection = new Selection (Document.OffsetToLocation (selectionRange.Offset + length),
						                           Document.OffsetToLocation (selectionRange.Offset + length + selectionRange.Length));
					}
					textEditor.MainSelection = selection;
				}

				if (autoScroll)
					textEditor.Caret.ActivateAutoScrollWithoutMove ();
			}
		}

		protected internal override void MouseReleased (MarginMouseEventArgs args)
		{
			if (args.Button != 2 && !inSelectionDrag)
				textEditor.ClearSelection ();
			inSelectionDrag = false;
			if (inDrag)
				Caret.Location = clickLocation;
			base.MouseReleased (args);
		}

		CodeSegmentPreviewWindow previewWindow = null;
		public bool IsCodeSegmentPreviewWindowShown {
			get {
				return previewWindow != null;
			}
		}

		public void HideCodeSegmentPreviewWindow ()
		{
			if (previewWindow != null) {
				previewWindow.Destroy ();
				previewWindow = null;
			}
		}

		internal void OpenCodeSegmentEditor ()
		{
			if (!IsCodeSegmentPreviewWindowShown)
				throw new InvalidOperationException ("CodeSegment preview window isn't shown.");
			var previewSegment = previewWindow.Segment;

			int x = 0, y = 0;
			this.previewWindow.GdkWindow.GetOrigin (out x, out y);
			int w = previewWindow.Allocation.Width;
			int h = previewWindow.Allocation.Height;
			if (!previewWindow.HideCodeSegmentPreviewInformString)
				h -= previewWindow.PreviewInformStringHeight;
			CodeSegmentEditorWindow codeSegmentEditorWindow = new CodeSegmentEditorWindow (textEditor);
			codeSegmentEditorWindow.Move (x, y);
			codeSegmentEditorWindow.Resize (w, h);
			codeSegmentEditorWindow.SyntaxMode = Document.SyntaxMode;
			int indentLength = SyntaxMode.GetIndentLength (Document, previewSegment.Offset, previewSegment.Length, false);

			StringBuilder textBuilder = new StringBuilder ();
			int curOffset = previewSegment.Offset;
			while (curOffset >= 0 && curOffset < previewSegment.EndOffset && curOffset < Document.TextLength) {
				LineSegment line = Document.GetLineByOffset (curOffset);
				string lineText = Document.GetTextAt (curOffset, line.Offset + line.Length - curOffset);
				textBuilder.Append (lineText);
				textBuilder.AppendLine ();
				curOffset = line.EndOffsetIncludingDelimiter + indentLength;
			}

			codeSegmentEditorWindow.Text = textBuilder.ToString ();

			HideCodeSegmentPreviewWindow ();
			codeSegmentEditorWindow.ShowAll ();

			codeSegmentEditorWindow.GrabFocus ();

		}

		uint codeSegmentTooltipTimeoutId = 0;
		void ShowTooltip (TextSegment segment, Rectangle hintRectangle)
		{
			if (previewWindow != null && previewWindow.Segment == segment)
				return;
			CancelCodeSegmentTooltip ();
			HideCodeSegmentPreviewWindow ();
			if (segment.IsInvalid || segment.Length == 0)
				return;
			codeSegmentTooltipTimeoutId = GLib.Timeout.Add (650, delegate {
				previewWindow = new CodeSegmentPreviewWindow (this.textEditor, false, segment);
				if (previewWindow.IsEmptyText) {
					previewWindow.Destroy ();
					previewWindow = null;
					return false;
				}
					
				int ox = 0, oy = 0;
				this.textEditor.GdkWindow.GetOrigin (out ox, out oy);

				int x = hintRectangle.X + hintRectangle.Width;
				int y = hintRectangle.Y + hintRectangle.Height;
				previewWindow.CalculateSize ();
				int w = previewWindow.SizeRequest ().Width;
				int h = previewWindow.SizeRequest ().Height;

				Gdk.Rectangle geometry = this.textEditor.Screen.GetUsableMonitorGeometry (this.textEditor.Screen.GetMonitorAtPoint (ox + x, oy + y));

				if (x + ox + w > geometry.X + geometry.Width)
					x = hintRectangle.Left - w;
				if (y + oy + h > geometry.Y + geometry.Height)
					y = hintRectangle.Top - h;
				int destX = System.Math.Max (0, ox + x);
				int destY = System.Math.Max (0, oy + y);
				previewWindow.Move (destX, destY);
				previewWindow.ShowAll ();
				return false;
			});
		}

		void CancelCodeSegmentTooltip ()
		{
			if (codeSegmentTooltipTimeoutId != 0) {
				GLib.Source.Remove (codeSegmentTooltipTimeoutId);
				codeSegmentTooltipTimeoutId = 0;
			}
		}
		
		public Func<MarginMouseEventArgs, string> GetLink;
//		= new delegate (MarginMouseEventArgs args) {
//			LineSegment line = args.LineSegment;
//			Mono.TextEditor.Highlighting.ColorSheme style = ColorStyle;
//			Document doc = Document;
//			if (doc == null)
//				return null;
//			SyntaxMode mode = doc.SyntaxMode;
//			if (line == null || style == null || mode == null)
//				return null;
//
//			Chunk chunk = GetCachedChunks (mode, Document, style, line, line.Offset, line.EditableLength);
//			if (chunk != null) {
//				DocumentLocation loc = PointToLocation (args.X, args.Y);
//				int column = 0;
//				for (; chunk != null; chunk = chunk.Next) {
//					if (column <= loc.Column && loc.Column < column + chunk.Length) {
//						ChunkStyle chunkStyle = chunk.GetChunkStyle (style);
//
//						return chunkStyle != null ? chunkStyle.Link : null;
//					}
//					column += chunk.Length;
//				}
//			}
//			return null;
//		};

		public LineSegment HoveredLine {
			get;
			set;
		}
		public event EventHandler<LineEventArgs> HoveredLineChanged;
		protected virtual void OnHoveredLineChanged (LineEventArgs e)
		{
			EventHandler<LineEventArgs> handler = this.HoveredLineChanged;
			if (handler != null)
				handler (this, e);
		}

		List<IActionTextMarker> oldMarkers = new List<IActionTextMarker> ();
		List<IActionTextMarker> newMarkers = new List<IActionTextMarker> ();
		protected internal override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);

			var loc = PointToLocation (args.X, args.Y);
			if (loc.Line < DocumentLocation.MinLine || loc.Column < DocumentLocation.MinColumn)
				return;
			var line = Document.GetLine (loc.Line);
			var oldHoveredLine = HoveredLine;
			HoveredLine = line;
			OnHoveredLineChanged (new LineEventArgs (oldHoveredLine));

			var hoverResult = new TextMarkerHoverResult ();
			oldMarkers.ForEach (m => m.MouseHover (this.textEditor, args, hoverResult));

			if (line != null) {
				newMarkers.Clear ();
				newMarkers.AddRange (line.Markers.Where (m => m is IActionTextMarker).Cast <IActionTextMarker> ());
				var extraMarker = Document.GetExtendingTextMarker (loc.Line) as IActionTextMarker;
				if (extraMarker != null && !oldMarkers.Contains (extraMarker))
					newMarkers.Add (extraMarker);
				foreach (var marker in newMarkers.Where (m => !oldMarkers.Contains (m))) {
					marker.MouseHover (this.textEditor, args, hoverResult);
				}
				oldMarkers.Clear ();
				var tmp = oldMarkers;
				oldMarkers = newMarkers;
				newMarkers = tmp;
			} else {
				oldMarkers.Clear ();
			}
			base.cursor = hoverResult.Cursor ?? xtermCursor;
			if (textEditor.TooltipMarkup != hoverResult.TooltipMarkup) {
				textEditor.TooltipMarkup = null;
				textEditor.TriggerTooltipQuery ();
			}
			textEditor.TooltipMarkup = hoverResult.TooltipMarkup;

			if (args.Button != 1 && args.Y >= 0 && args.Y <= this.textEditor.Allocation.Height) {
				// folding marker
				int lineNr = args.LineNumber;
				foreach (KeyValuePair<Rectangle, FoldSegment> shownFolding in GetFoldRectangles (lineNr)) {
					if (shownFolding.Key.Contains ((int)(args.X + this.XOffset), (int)args.Y)) {
						ShowTooltip (shownFolding.Value.Segment, shownFolding.Key);
						return;
					}
				}

				ShowTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
				string link = GetLink != null ? GetLink (args) : null;

				if (!String.IsNullOrEmpty (link)) {
					base.cursor = textLinkCursor;
				} else {
					base.cursor = hoverResult.Cursor ?? xtermCursor;
				}
				return;
			}

			if (inDrag)
				return;
			Caret.PreserveSelection = true;

			switch (this.mouseSelectionMode) {
			case MouseSelectionMode.SingleChar:
				if (!inSelectionDrag) {
					textEditor.SetSelection (loc, loc);
				} else {
					textEditor.ExtendSelectionTo (loc);
				}
				Caret.Location = loc;
				break;
			case MouseSelectionMode.Word:
				int offset = textEditor.Document.LocationToOffset (loc);
				int start;
				int end;
				var data = textEditor.GetTextEditorData ();
				if (offset < textEditor.SelectionAnchor) {
					start = data.FindCurrentWordStart (offset);
					end = data.FindCurrentWordEnd (textEditor.SelectionAnchor);
					Caret.Offset = start;
				} else {
					start = data.FindCurrentWordStart (textEditor.SelectionAnchor);
					end = data.FindCurrentWordEnd (offset);
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
				Caret.Offset = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffsetIncludingDelimiter;
				if (textEditor.MainSelection != null)
					textEditor.MainSelection.Lead = Caret.Location;
				break;
			}
			Caret.PreserveSelection = false;

			//HACK: use cmd as Mac block select modifier because GTK currently makes it impossible to access alt/mod1
			//NOTE: Mac cmd seems to be mapped as ControlMask from mouse events on older GTK, mod1 on newer
			var blockSelModifier = !Platform.IsMac? ModifierType.Mod1Mask
				: (ModifierType.ControlMask | ModifierType.Mod1Mask);

			//NOTE: also allow super for block select on X11 because most window managers use the alt modifier already
			if (Platform.IsX11)
				blockSelModifier |= (ModifierType.SuperMask | ModifierType.Mod4Mask);

			if ((args.ModifierState & blockSelModifier) != 0) {
				textEditor.SelectionMode = SelectionMode.Block;
			} else {
				if (textEditor.SelectionMode == SelectionMode.Block)
					Document.CommitMultipleLineUpdate (textEditor.MainSelection.MinLine, textEditor.MainSelection.MaxLine);
				textEditor.SelectionMode = SelectionMode.Normal;
			}
			inSelectionDrag = true;
		}

		public static int GetNextTabstop (TextEditorData textEditor, int currentColumn)
		{
			int tabSize = textEditor != null && textEditor.Options != null ? textEditor.Options.TabSize : 4;
			int result = currentColumn - 1 + tabSize;
			return 1 + (result / tabSize) * tabSize;
		}

		internal double rulerX = 0;

		public double RulerX {
			get { return this.rulerX; }
		}

		public int GetWidth (string text)
		{
			text = text.Replace ("\t", new string (' ', textEditor.Options.TabSize));
			markerLayout.SetText (text);
			int width, height;
			markerLayout.GetPixelSize (out width, out height);
			return width;
		}

		internal static Cairo.Color DimColor (Cairo.Color color, double dimFactor)
		{
			var result = new Cairo.Color (color.R * dimFactor,
			                              color.G * dimFactor,
			                              color.B * dimFactor);
			return result;
		}
		internal static Cairo.Color DimColor (Cairo.Color color)
		{
			return DimColor (color, 0.95);
		}

		public void DrawRectangleWithRuler (Cairo.Context cr, double x, Cairo.Rectangle area, Cairo.Color color, bool drawDefaultBackground)
		{
			bool isDefaultColor = color.R == defaultBgColor.R && color.G == defaultBgColor.G && color.B == defaultBgColor.B;
			if (isDefaultColor && !drawDefaultBackground)
				return;
			cr.Color = color;
			double xp = /*System.Math.Floor*/ (area.X);
			
			if (textEditor.Options.ShowRuler) {
				double divider = System.Math.Max (area.X, System.Math.Min (x + TextStartPosition + rulerX, area.X + area.Width));
				if (divider < area.X + area.Width) {
					cr.Rectangle (xp, area.Y, divider - area.X, area.Height);
					cr.Fill ();
					
					cr.Rectangle (divider, area.Y, area.X + area.Width - divider, area.Height);
					cr.Color = DimColor (color);
					cr.Fill ();
					cr.DrawLine (ColorStyle.Ruler, divider, area.Y, divider, area.Y + area.Height);
					return;
				}
			}
			cr.Rectangle (xp, area.Y, area.Width, area.Height);
			cr.Fill ();
		}

		List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> GetFoldRectangles (int lineNr)
		{
			List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> result = new List<System.Collections.Generic.KeyValuePair<Gdk.Rectangle, FoldSegment>> ();
			if (lineNr < 0)
				return result;

			LineSegment line = lineNr <= Document.LineCount ? Document.GetLine (lineNr) : null;
			//			int xStart = XOffset;
			int y = (int)(LineToY (lineNr) - textEditor.VAdjustment.Value);
			//			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			int xPos = (int)(XOffset - textEditor.HAdjustment.Value);

			if (line == null) {
				return result;
			}

			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			restart:
			//			int caretOffset = Caret.Offset;
			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column - 1;
				if (foldOffset < offset)
					continue;

				if (folding.IsFolded) {
					markerLayout.SetText (Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.TextLength - offset))).Replace ("\t", new string (' ', textEditor.Options.TabSize)));
					markerLayout.GetPixelSize (out width, out height);
					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;

					markerLayout.SetText (folding.Description);
					markerLayout.GetPixelSize (out width, out height);
					Rectangle foldingRectangle = new Rectangle (xPos, y, width - 1, (int)this.LineHeight - 1);
					result.Add (new KeyValuePair<Rectangle, FoldSegment> (foldingRectangle, folding));
					xPos += width;
					if (folding.EndLine != line) {
						line = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						goto restart;
					}
				}
			}
			return result;
		}

		List<TextSegment> selectedRegions = new List<TextSegment> ();
		public int SearchResultMatchCount {
			get {
				return selectedRegions.Count;
			}
		}
		public IEnumerable<TextSegment> SearchResults {
			get {
				return selectedRegions;
			}
		}

		TextSegment mainSearchResult;
		public TextSegment MainSearchResult {
			get {
				return mainSearchResult;
			}
			set {
				mainSearchResult = value;
				OnMainSearchResultChanged (EventArgs.Empty);
			}
		}
		
		public event EventHandler MainSearchResultChanged;
		protected virtual void OnMainSearchResultChanged (EventArgs e)
		{
			EventHandler handler = this.MainSearchResultChanged;
			if (handler != null)
				handler (this, e);
		}
		
		Cairo.Color defaultBgColor;
		
		public int TextStartPosition {
			get {
				return 4;
			}
		}

		protected internal override void Draw (Cairo.Context cr, Cairo.Rectangle area, LineSegment line, int lineNr, double x, double y, double _lineHeight)
		{
//			double xStart = System.Math.Max (area.X, XOffset);
//			xStart = System.Math.Max (0, xStart);
			var lineArea = new Cairo.Rectangle (XOffset - 1, y, textEditor.Allocation.Width - XOffset + 1, _lineHeight);
			int width, height;
			double pangoPosition = (x - textEditor.HAdjustment.Value + TextStartPosition) * Pango.Scale.PangoScale;

			defaultBgColor = Document.ReadOnly ? ColorStyle.ReadOnlyTextBg : ColorStyle.Default.CairoBackgroundColor;

			// Draw the default back color for the whole line. Colors other than the default
			// background will be drawn when rendering the text chunks.
			DrawRectangleWithRuler (cr, x, lineArea, defaultBgColor, true);
			bool isSelectionDrawn = false;

			if (BackgroundRenderer != null)
				BackgroundRenderer.Draw (cr, area, line, x, y, _lineHeight);

			// Check if line is beyond the document length
			if (line == null) {
				if (textEditor.Options.ShowInvalidLines)
					DrawInvalidLineMarker (cr, pangoPosition / Pango.Scale.PangoScale, y);
				var marker = Document.GetExtendingTextMarker (lineNr);
				if (marker != null)
					marker.Draw (textEditor, cr, lineNr, lineArea);
				return;
			}
			
			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			bool isEolFolded = false;
			restart:
			int logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData (), textEditor.Options.RulerColumn);
			
			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column - 1;
				if (foldOffset < offset)
					continue;

				if (folding.IsFolded) {
					
					DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, foldOffset - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width);
					
					offset = folding.EndLine.Offset + folding.EndColumn;
					markerLayout.SetText (folding.Description);
					markerLayout.GetSize (out width, out height);
					
					bool isFoldingSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding.Segment);
					double pixelX = pangoPosition / Pango.Scale.PangoScale;
					double pixelWidth = (pangoPosition + width) / Pango.Scale.PangoScale - pixelX;
					var foldingRectangle = new Cairo.Rectangle (pixelX + 0.5, y + 0.5, pixelWidth - cr.LineWidth, this.LineHeight - cr.LineWidth);
					if (BackgroundRenderer == null) {
						cr.Color = isFoldingSelected ? SelectionColor.CairoBackgroundColor : defaultBgColor;
						cr.Rectangle (foldingRectangle);
						cr.Fill ();
					}
					
					cr.Color = isFoldingSelected ? SelectionColor.CairoColor : ColorStyle.FoldLine.CairoColor;
					cr.Rectangle (foldingRectangle);
					cr.Stroke ();
					
					cr.Save ();
					cr.Translate (pangoPosition / Pango.Scale.PangoScale, y);
					cr.Color = isFoldingSelected ? SelectionColor.CairoColor : ColorStyle.FoldLine.CairoColor;
					cr.ShowLayout (markerLayout);
					cr.Restore ();
					

					if (caretOffset == foldOffset && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition ((int)(pangoPosition / Pango.Scale.PangoScale), y);
					pangoPosition += width;
					if (caretOffset == foldOffset + folding.Length && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition ((int)(pangoPosition / Pango.Scale.PangoScale), y);

					if (folding.EndLine != line) {
						line = folding.EndLine;
						lineNr = Document.OffsetToLineNumber (line.Offset);
						foldings = Document.GetStartFoldings (line);
						isEolFolded = line.Length <= folding.EndColumn;
						goto restart;
					}
					isEolFolded = line.Length <= folding.EndColumn;
				}
			}
			
			// Draw remaining line - must be called for empty line parts as well because the caret may be at this positon
			// and the caret position is calculated in DrawLinePart.
			if (line.EndOffsetIncludingDelimiter - offset >= 0)
				DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, line.Offset + line.Length - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width);
			
			bool isEolSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionMode == SelectionMode.Normal && textEditor.SelectionRange.Contains (line.Offset + line.Length);
			lineArea = new Cairo.Rectangle (pangoPosition / Pango.Scale.PangoScale,
				lineArea.Y,
				textEditor.Allocation.Width - pangoPosition / Pango.Scale.PangoScale,
				lineArea.Height);

			if (textEditor.SelectionMode == SelectionMode.Block && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (line.Offset + line.Length)) {
				DocumentLocation start = textEditor.MainSelection.Anchor;
				DocumentLocation end = textEditor.MainSelection.Lead;
				DocumentLocation visStart = textEditor.LogicalToVisualLocation (start);
				DocumentLocation visEnd = textEditor.LogicalToVisualLocation (end);
				
				double x1 = this.ColumnToX (line, visStart.Column);
				double x2 = this.ColumnToX (line, visEnd.Column);
				if (x1 > x2) {
					var tmp = x1;
					x1 = x2;
					x2 = tmp;
				}
				x1 += XOffset - textEditor.HAdjustment.Value;
				x2 += XOffset - textEditor.HAdjustment.Value;

				if (x2 > lineArea.X) {
					if (x1 - lineArea.X > 0) {
						DrawRectangleWithRuler (cr, x, new Cairo.Rectangle (lineArea.X, lineArea.Y, x1 - lineArea.X, lineArea.Height), defaultBgColor, false);
						lineArea = new Cairo.Rectangle (x1, lineArea.Y, lineArea.Width, lineArea.Height);
					}
					DrawRectangleWithRuler (cr, x, new Cairo.Rectangle (lineArea.X, lineArea.Y, x2 - lineArea.X, lineArea.Height), this.SelectionColor.CairoBackgroundColor, false);
					lineArea = new Cairo.Rectangle (x2, lineArea.Y, textEditor.Allocation.Width - lineArea.X, lineArea.Height);
				}
			}

			if (!isSelectionDrawn) {
				if (isEolSelected) {
					if (!Platform.IsMac) {
						// prevent "gaps" in the selection drawing ('fuzzy' lines problem)
						lineArea = new Cairo.Rectangle (pangoPosition / Pango.Scale.PangoScale,
						lineArea.Y,
						textEditor.Allocation.Width - pangoPosition / Pango.Scale.PangoScale + 1,
						lineArea.Height);
					} else {
						// prevent "gaps" in the selection drawing ('fuzzy' lines problem)
						lineArea = new Cairo.Rectangle (pangoPosition / Pango.Scale.PangoScale - 1,
						lineArea.Y,
						textEditor.Allocation.Width - pangoPosition / Pango.Scale.PangoScale + 1,
						lineArea.Height);
					}
					
					DrawRectangleWithRuler (cr, x, lineArea, this.SelectionColor.CairoBackgroundColor, false);
				} else if (!(HighlightCaretLine || textEditor.Options.HighlightCaretLine) || Caret.Line != lineNr) {
					LayoutWrapper wrapper = GetLayout (line);
					if (wrapper.EolSpanStack != null) {
						foreach (var span in wrapper.EolSpanStack) {
							var spanStyle = textEditor.ColorStyle.GetChunkStyle (span.Color);
							if (!spanStyle.TransparentBackround && GetPixel (ColorStyle.Default.BackgroundColor) != GetPixel (spanStyle.BackgroundColor)) {
								DrawRectangleWithRuler (cr, x, lineArea, spanStyle.CairoBackgroundColor, false);
								break;
							}
						}
					}
				} else {
					double xPos = pangoPosition / Pango.Scale.PangoScale;
					DrawCaretLineMarker (cr, xPos, y, lineArea.X + lineArea.Width - xPos);
				}
			}
			
			if (!isEolFolded && textEditor.Options.ShowEolMarkers)
				DrawEolMarker (cr, line, isEolSelected, pangoPosition / Pango.Scale.PangoScale, y);
			var extendingMarker = Document.GetExtendingTextMarker (lineNr);
			if (extendingMarker != null)
				extendingMarker.Draw (textEditor, cr, lineNr, lineArea);
			
			lastLineRenderWidth = pangoPosition / Pango.Scale.PangoScale;
		}

		internal IBackgroundRenderer BackgroundRenderer {
			get;
			set;
		}

		internal double lastLineRenderWidth = 0;

		protected internal override void MouseLeft ()
		{
			base.MouseLeft ();
			ShowTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
		}
		
		#region Coordinate transformation
		class VisualLocationTranslator
		{
			TextViewMargin margin;
			int lineNumber;
			LineSegment line;
			int xPos = 0;
			
			public bool WasInLine {
				get;
				set;
			}

			public VisualLocationTranslator (TextViewMargin margin)
			{
				this.margin = margin;
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
					index = TranslateIndexToUTF8 (layoutWrapper.Layout.Text, index);

					if (!IsNearX1 (xp, xp1, xp2))
						index++;
					return true;
				}
				index = line.Length;
				return false;
			}

			public DocumentLocation PointToLocation (double xp, double yp)
			{
				lineNumber = System.Math.Min (margin.YToLine (yp + margin.textEditor.VAdjustment.Value), margin.Document.LineCount);
				line = lineNumber <= margin.Document.LineCount ? margin.Document.GetLine (lineNumber) : null;
				if (line == null)
					return DocumentLocation.Empty;
				
				int offset = line.Offset;
				
				yp = ((int)yp % margin.LineHeight);
				xp -= margin.TextStartPosition;
				xp += margin.textEditor.HAdjustment.Value;
				xp *= Pango.Scale.PangoScale;
				yp *= Pango.Scale.PangoScale;
				yp = System.Math.Max (0, yp);
				if (xp < 0)
					return new DocumentLocation (lineNumber, DocumentLocation.MinColumn);
				int column = DocumentLocation.MinColumn;
				ISyntaxMode mode = margin.Document.SyntaxMode != null && margin.textEditor.Options.EnableSyntaxHighlighting ? margin.Document.SyntaxMode : new SyntaxMode (margin.Document);
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				bool done = false;
				Pango.Layout measueLayout = null;
				restart:
				int logicalRulerColumn = line.GetLogicalColumn(margin.textEditor.GetTextEditorData(), margin.textEditor.Options.RulerColumn);
				foreach (FoldSegment folding in foldings.Where(f => f.IsFolded))
				{
					int foldOffset = folding.StartLine.Offset + folding.Column - 1;
					if (foldOffset < offset)
						continue;
					layoutWrapper = margin.CreateLinePartLayout(mode, line, logicalRulerColumn, line.Offset, foldOffset - offset, -1, -1);
					done |= ConsumeLayout ((int)(xp - xPos), (int)yp);
					if (done)
						break;
					int height, width;
					layoutWrapper.Layout.GetPixelSize (out width, out height);
					xPos += width * (int)Pango.Scale.PangoScale;
					if (measueLayout == null) {
						measueLayout = PangoUtil.CreateLayout (margin.textEditor, folding.Description);
						measueLayout.FontDescription = margin.textEditor.Options.Font;
					}

					int delta;
					measueLayout.GetPixelSize (out delta, out height);
					delta *= (int)Pango.Scale.PangoScale;
					xPos += delta;
					if (xPos - delta / 2 >= xp) {
						index = foldOffset - offset;
						done = true;
						break;
					}

					offset = folding.EndLine.Offset + folding.EndColumn;
					DocumentLocation foldingEndLocation = margin.Document.OffsetToLocation(offset);
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
					layoutWrapper = margin.CreateLinePartLayout(mode, line, logicalRulerColumn, offset, line.Offset + line.Length - offset, -1, -1);
					ConsumeLayout ((int)(xp - xPos), (int)yp);
				}
				if (measueLayout != null)
					measueLayout.Dispose ();
				return new DocumentLocation (lineNumber, column + index);
			}
		}

		public DocumentLocation PointToLocation (double xp, double yp)
		{
			return new VisualLocationTranslator (this).PointToLocation (xp, yp);
		}
		
		public DocumentLocation PointToLocation (Cairo.Point p)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y);
		}
		
		public DocumentLocation PointToLocation (Cairo.PointD p)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y);
		}
		
		static bool IsNearX1 (int pos, int x1, int x2)
		{
			return System.Math.Abs (x1 - pos) < System.Math.Abs (x2 - pos);
		}
		
		public Cairo.Point LocationToPoint (int line, int column)
		{
			return LocationToPoint (line, column, false);
		}
		
		public Cairo.Point LocationToPoint (DocumentLocation loc)
		{
			return LocationToPoint (loc, false);
		}
		
		public Cairo.Point LocationToPoint (int line, int column, bool useAbsoluteCoordinates)
		{
			return LocationToPoint (new DocumentLocation (line, column), useAbsoluteCoordinates);
		}
		
		public Cairo.Point LocationToPoint (DocumentLocation loc, bool useAbsoluteCoordinates)
		{
			LineSegment line = Document.GetLine (loc.Line);
			if (line == null)
				return new Cairo.Point (-1, -1);
			int x = (int)(ColumnToX (line, loc.Column) + this.XOffset + this.TextStartPosition);
			int y = (int)LineToY (loc.Line);
			return useAbsoluteCoordinates ? new Cairo.Point (x, y) : new Cairo.Point (x - (int)this.textEditor.HAdjustment.Value, y - (int)this.textEditor.VAdjustment.Value);
		}
		
		public double ColumnToX (LineSegment line, int column)
		{
			column--;
			if (line == null || line.Length == 0 || column < 0)
				return 0;
			int logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData (), textEditor.Options.RulerColumn);
			int lineOffset = line.Offset;
			StringBuilder textBuilder = new StringBuilder ();
			ISyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);
			var startChunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, lineOffset, line.Length);
			foreach (Chunk chunk in startChunk) {
				try {
					textBuilder.Append (Document.GetTextAt (chunk));
				} catch (Exception e) {
					Console.WriteLine (e);
					return 0;
				}
			}
			string lineText = textBuilder.ToString ();
			char[] lineChars = lineText.ToCharArray ();
			
			bool containsPreedit = textEditor.ContainsPreedit (lineOffset, line.Length);
			uint preeditLength = 0;

			if (containsPreedit) {
				lineText = lineText.Insert (textEditor.preeditOffset - lineOffset, textEditor.preeditString);
				preeditLength = (uint)textEditor.preeditString.Length;
			}
			if (column < lineText.Length)
				lineText = lineText.Substring (0, column);

			var layout = PangoUtil.CreateLayout (textEditor, lineText);
			layout.Alignment = Pango.Alignment.Left;
			layout.FontDescription = textEditor.Options.Font;
			layout.Tabs = tabArray;

			int startOffset = lineOffset, endOffset = lineOffset + line.Length;
			uint curIndex = 0, byteIndex = 0;
			uint curChunkIndex = 0, byteChunkIndex = 0;
			List<Pango.Attribute> attributes = new List<Pango.Attribute> ();
			uint oldEndIndex = 0;
			foreach (Chunk chunk in startChunk) {
				ChunkStyle chunkStyle = chunk != null ? textEditor.ColorStyle.GetChunkStyle (chunk) : null;

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

					HandleSelection (lineOffset, logicalRulerColumn, - 1, -1, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {
						Pango.AttrForeground foreGround = new Pango.AttrForeground (chunkStyle.Color.Red, chunkStyle.Color.Green, chunkStyle.Color.Blue);
						foreGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						foreGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Add (foreGround);
						if (!chunkStyle.TransparentBackround) {
							var background = new Pango.AttrBackground (chunkStyle.BackgroundColor.Red, chunkStyle.BackgroundColor.Green, chunkStyle.BackgroundColor.Blue);
							background.StartIndex = foreGround.StartIndex;
							background.EndIndex = foreGround.EndIndex;
							attributes.Add (background);
						}
					}, delegate(int start, int end) {
						Pango.AttrForeground selectedForeground = new Pango.AttrForeground (SelectionColor.Color.Red, SelectionColor.Color.Green, SelectionColor.Color.Blue);
						selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Add (selectedForeground);
					});

					var translatedStartIndex = TranslateToUTF8Index (lineChars, (uint)startIndex, ref curChunkIndex, ref byteChunkIndex);
					var translatedEndIndex = TranslateToUTF8Index (lineChars, (uint)endIndex, ref curChunkIndex, ref byteChunkIndex);

					if (chunkStyle.Bold) {
						Pango.AttrWeight attrWeight = new Pango.AttrWeight (Pango.Weight.Bold);
						attrWeight.StartIndex = translatedStartIndex;
						attrWeight.EndIndex = translatedEndIndex;
						attributes.Add (attrWeight);
					}

					if (chunkStyle.Italic) {
						Pango.AttrStyle attrStyle = new Pango.AttrStyle (Pango.Style.Italic);
						attrStyle.StartIndex = translatedStartIndex;
						attrStyle.EndIndex = translatedEndIndex;
						attributes.Add (attrStyle);
					}

					if (chunkStyle.Underline) {
						Pango.AttrUnderline attrUnderline = new Pango.AttrUnderline (Pango.Underline.Single);
						attrUnderline.StartIndex = translatedStartIndex;
						attrUnderline.EndIndex = translatedEndIndex;
						attributes.Add (attrUnderline);
					}
				}
			}
			Pango.AttrList attributeList = new Pango.AttrList ();
			attributes.ForEach (attr => attributeList.Insert (attr));
			layout.Attributes = attributeList;
			Pango.Rectangle ink_rect, logical_rect;
			layout.GetExtents (out ink_rect, out logical_rect);
			attributes.ForEach (attr => attr.Dispose ());
			attributeList.Dispose ();
			layout.Dispose ();
			return (logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale;
		}
		
		public int YToLine (double yPos)
		{			
			var result = textEditor.GetTextEditorData ().HeightTree.YToLineNumber (yPos);
			return result;
		}
		
		public double LineToY (int logicalLine)
		{
			return textEditor.GetTextEditorData ().HeightTree.LineNumberToY (logicalLine);
			
			/*		double delta = 0;
			var doc = Document;
			if (doc == null)
				return 0;
			LineSegment logicalLineSegment = doc.GetLine (logicalLine);
			foreach (LineSegment extendedTextMarkerLine in doc.LinesWithExtendingTextMarkers) {
				if (extendedTextMarkerLine == null)
					continue;
				if (logicalLineSegment != null && extendedTextMarkerLine.Offset >= logicalLineSegment.Offset)
					continue;
				delta += GetLineHeight (extendedTextMarkerLine) - LineHeight;
			}
			
			int visualLine = doc.LogicalToVisualLine (logicalLine) - 1;
			return visualLine * LineHeight + delta;*/
		}
		
		public double GetLineHeight (LineSegment line)
		{
			if (line == null)
				return LineHeight;
			foreach (var marker in line.Markers) {
				IExtendingTextMarker extendingTextMarker = marker as IExtendingTextMarker;
				if (extendingTextMarker == null)
					continue;
				return extendingTextMarker.GetLineHeight (textEditor);
			}
			int lineNumber = textEditor.OffsetToLineNumber (line.Offset); 
			var node = textEditor.GetTextEditorData ().HeightTree.GetNodeByLine (lineNumber);
			if (node == null)
				return LineHeight;
			return node.height / node.count;
		}
		
		public double GetLineHeight (int logicalLineNumber)
		{
			var doc = Document;
			if (doc == null)
				return LineHeight;
			return GetLineHeight (doc.GetLine (logicalLineNumber));
		}
		#endregion
	}
}
