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
		Pango.Layout markerLayout, defaultLayout;
		Pango.Layout macEolLayout, unixEolLayout, windowsEolLayout, eofEolLayout;
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
			defaultLayout = PangoUtil.CreateLayout (textEditor);

			textEditor.Document.EndUndo += HandleEndUndo;
			textEditor.SelectionChanged += UpdateBracketHighlighting;
			textEditor.Document.Undone += HandleUndone; 
			textEditor.Document.Redone += HandleUndone;
			textEditor.TextArea.FocusInEvent += HandleFocusInEvent;
			textEditor.TextArea.FocusOutEvent += HandleFocusOutEvent;
			Caret.PositionChanged += UpdateBracketHighlighting;
			textEditor.VScroll += HandleVAdjustmentValueChanged;
		}

		void HandleFocusInEvent (object o, FocusInEventArgs args)
		{
			selectionColor = ColorStyle.SelectedText;
			currentLineColor = ColorStyle.LineMarker;
		}

		void HandleFocusOutEvent (object o, FocusOutEventArgs args)
		{
			selectionColor = ColorStyle.SelectedInactiveText;
			currentLineColor = ColorStyle.LineMarkerInactive;
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

		List<DocumentLine> linesToRemove = new List<DocumentLine> ();

		void HandleVAdjustmentValueChanged (object sender, EventArgs e)
		{
			int startLine = (int)(textEditor.GetTextEditorData ().VAdjustment.Value / LineHeight);
			int endLine = (int)(startLine + textEditor.GetTextEditorData ().VAdjustment.PageSize / LineHeight) + 1;
			foreach (DocumentLine line in layoutDict.Keys) {
				int curLine = line.LineNumber;
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

		public class SearchWorkerArguments
		{
			public int FirstLine { get; set; }

			public int LastLine { get; set; }

			public List<TextSegment> OldRegions { get; set; }

			public ISearchEngine Engine { get; set; }

			public string Text { get; set; }
		}

		public void RefreshSearchMarker ()
		{
			if (textEditor.HighlightSearchPattern) {
				DisposeSearchPatternWorker ();
				SearchWorkerArguments args = new SearchWorkerArguments () {
					FirstLine = YToLine (textEditor.VAdjustment.Value),
					LastLine = YToLine (textEditor.Allocation.Height + textEditor.VAdjustment.Value),
					OldRegions = selectedRegions,
					Engine = textEditor.GetTextEditorData ().SearchEngine.Clone (),
					Text = textEditor.Text
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
					result = args.Engine.SearchForward (worker, args, offset);
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
					if (args.OldRegions [i].Offset != newRegions [i].Offset || args.OldRegions [i].Length != newRegions [i].Length) {
						int lineNumber = Document.OffsetToLineNumber (args.OldRegions [i].Offset);
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

		Pango.Rectangle unixEolLayoutRect;
		Pango.Rectangle macEolLayoutRect;
		Pango.Rectangle windowsEolLayoutRect;
		Pango.Rectangle eofEolLayoutRect;

		protected internal override void OptionsChanged ()
		{
			DisposeGCs ();

			var markerFont = textEditor.Options.Font.Copy ();
			markerFont.Size = markerFont.Size * 8 / 10;
			markerLayout.FontDescription = markerFont;
			markerLayout.FontDescription.Weight = Pango.Weight.Normal;
			markerLayout.FontDescription.Style = Pango.Style.Normal;

			if (textEditor.preeditString != null && textEditor.preeditAttrs != null) {
				using (var preeditLayout = PangoUtil.CreateLayout (textEditor)) {
					preeditLayout.SetText (textEditor.preeditString);
					preeditLayout.Attributes = textEditor.preeditAttrs;
				}
			}

			defaultLayout.FontDescription = textEditor.Options.Font;
			using (var metrics = textEditor.PangoContext.GetMetrics (defaultLayout.FontDescription, textEditor.PangoContext.Language)) {
				this.textEditor.GetTextEditorData ().LineHeight = System.Math.Ceiling (0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
				this.charWidth = metrics.ApproximateCharWidth / Pango.Scale.PangoScale;
			}

			textEditor.LineHeight = System.Math.Max (1, LineHeight);

			if (unixEolLayout == null) {
				unixEolLayout = PangoUtil.CreateLayout (textEditor);
				macEolLayout = PangoUtil.CreateLayout (textEditor);
				windowsEolLayout = PangoUtil.CreateLayout (textEditor);
				eofEolLayout = PangoUtil.CreateLayout (textEditor);
			}

			var font = textEditor.Options.Font.Copy ();
			font.Size = font.Size * 3 / 4;
			unixEolLayout.FontDescription = macEolLayout.FontDescription = windowsEolLayout.FontDescription = eofEolLayout.FontDescription = font;

			unixEolLayout.SetText ("\\n");
			Pango.Rectangle logRect;
			unixEolLayout.GetPixelExtents (out logRect, out unixEolLayoutRect);

			macEolLayout.SetText ("\\r");
			macEolLayout.GetPixelExtents (out logRect, out macEolLayoutRect);

			windowsEolLayout.SetText ("\\r\\n");
			windowsEolLayout.GetPixelExtents (out logRect, out windowsEolLayoutRect);

			eofEolLayout.SetText ("<EOF>");
			eofEolLayout.GetPixelExtents (out logRect, out eofEolLayoutRect);

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
			int tabWidth, h;
			tabWidthLayout.GetSize (out tabWidth, out h);
			tabWidthLayout.Dispose ();
			tabArray = new Pango.TabArray (1, false);
			tabArray.SetTab (0, Pango.TabAlign.Left, tabWidth);

			DisposeLayoutDict ();
			chunkDict.Clear ();
			caretX = caretY = -LineHeight;
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
			textEditor.TextArea.FocusInEvent -= HandleFocusInEvent;
			textEditor.TextArea.FocusOutEvent -= HandleFocusOutEvent;
			Caret.PositionChanged -= UpdateBracketHighlighting;

			textEditor.GetTextEditorData ().SearchChanged -= HandleSearchChanged;

			textLinkCursor.Dispose ();
			xtermCursor.Dispose ();

			DisposeGCs ();
			if (markerLayout != null)
				markerLayout.Dispose ();

			if (defaultLayout!= null) 
				defaultLayout.Dispose ();
			if (unixEolLayout != null) {
				macEolLayout.Dispose ();
				unixEolLayout.Dispose ();
				windowsEolLayout.Dispose ();
				eofEolLayout.Dispose ();
			}
			
			DisposeLayoutDict ();
			if (tabArray != null)
				tabArray.Dispose ();
			base.Dispose ();
		}

		#region Caret blinking
		internal bool caretBlink = true;
		uint blinkTimeout = 0, startBlinkTimeout = 0;

		// constants taken from gtk.
		const int cursorOnMultiplier = 2;
		const int cursorOffMultiplier = 1;
		const int cursorDivider = 3;
		
		public void ResetCaretBlink (uint delay = 0)
		{
			StopCaretThread ();
			blinkTimeout = GLib.Timeout.Add ((uint)(Gtk.Settings.Default.CursorBlinkTime * cursorOnMultiplier / cursorDivider), UpdateCaret);
			caretBlink = true;
		}

		internal void StopCaretThread ()
		{
			if (startBlinkTimeout != 0) {
				GLib.Source.Remove (startBlinkTimeout);
				startBlinkTimeout = 0;
			}

			if (blinkTimeout == 0)
				return;
			GLib.Source.Remove (blinkTimeout);
			blinkTimeout = 0;
			caretBlink = false;
		}

		bool UpdateCaret ()
		{
			caretBlink = !caretBlink;
			textEditor.TextArea.QueueDrawArea (caretRectangle.X - (int)textEditor.Options.Zoom,
			                          (int)(caretRectangle.Y + (textEditor.VAdjustment.Value - caretVAdjustmentValue)),
			                          caretRectangle.Width + 2 * (int)textEditor.Options.Zoom,
			                          caretRectangle.Height);
			OnCaretBlink (EventArgs.Empty);
			return true;
		}

		internal event EventHandler CaretBlink;
		void OnCaretBlink (EventArgs e)
		{
			var handler = CaretBlink;
			if (handler != null)
				handler (this, e);
		}
		#endregion
		
		internal double caretX;
		internal double caretY;

		public Cairo.PointD CaretVisualLocation {
			get {
				return new Cairo.PointD (caretX, caretY);
			}
		}

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
				caretChar = '\0';
			}

			switch (caretChar) {
			case ' ':
				break;
			case '\t':
				break;
			case '\n':
			case '\r':
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
				cr.Rectangle (XOffset, 0, textEditor.Allocation.Width - XOffset, textEditor.Allocation.Height);
				cr.Clip ();
				cr.LineWidth = System.Math.Max (1, System.Math.Floor (textEditor.Options.Zoom));
				cr.Antialias = Cairo.Antialias.None;
				var curRect = new Gdk.Rectangle ((int)caretX, (int)caretY, (int)this.charWidth, (int)LineHeight - 1);
				if (curRect != caretRectangle) {
					caretRectangle = curRect;
					textEditor.TextArea.QueueDrawArea (caretRectangle.X - (int)textEditor.Options.Zoom,
					               (int)(caretRectangle.Y + (-textEditor.VAdjustment.Value + caretVAdjustmentValue)),
				                    caretRectangle.Width + (int)textEditor.Options.Zoom,
					               caretRectangle.Height + 1);
					caretVAdjustmentValue = textEditor.VAdjustment.Value;
				}


				var fgColor = textEditor.ColorStyle.PlainText.Foreground;
//				var bgColor = textEditor.ColorStyle.Default.CairoBackgroundColor;
				var line = Document.GetLine (Caret.Line);
				if (line != null) {
					foreach (var marker in line.Markers) {
						var style = marker as StyleTextLineMarker;
						if (style == null)
							continue;
	//					if (style.IncludedStyles.HasFlag (StyleTextLineMarker.StyleFlag.BackgroundColor))
	//						bgColor = style.BackgroundColor;
						if (style.IncludedStyles.HasFlag (StyleTextLineMarker.StyleFlag.Color))
							fgColor = style.Color;
					}
				}
				/*
				var foreground = ((HslColor)fgColor).ToPixel ();
				var background = ((HslColor)color).ToPixel ();
				var caretColor = (foreground ^ background) & 0xFFFFFF;
				color = HslColor.FromPixel (caretColor);*/
				var color = fgColor;

				switch (Caret.Mode) {
				case CaretMode.Insert:
					cr.DrawLine (color,
					             caretRectangle.X + 0.5, 
					             caretRectangle.Y + 0.5,
					             caretRectangle.X + 0.5,
					             caretRectangle.Y + caretRectangle.Height);
					break;
				case CaretMode.Block:
					cr.Color = color;
					cr.Rectangle (caretRectangle.X + 0.5, caretRectangle.Y + 0.5, caretRectangle.Width, caretRectangle.Height);
					cr.Fill ();
					char caretChar = GetCaretChar ();
					if (!char.IsWhiteSpace (caretChar) && caretChar != '\0') {
						using (var layout = PangoUtil.CreateLayout (textEditor)) {
							layout.FontDescription = textEditor.Options.Font;
							layout.SetText (caretChar.ToString ());
							cr.MoveTo (caretRectangle.X, caretRectangle.Y);
							cr.Color = textEditor.ColorStyle.PlainText.Background;
							cr.ShowLayout (layout);
						}
					}
					break;
				case CaretMode.Underscore:
					cr.DrawLine (color,
					             caretRectangle.X + 0.5, 
					             caretRectangle.Y + caretRectangle.Height + 0.5,
					             caretRectangle.X + caretRectangle.Width,
					             caretRectangle.Y + caretRectangle.Height + 0.5);
					break;
				}
			}
		}

		void GetSelectionOffsets (DocumentLine line, out int selectionStart, out int selectionEnd)
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
					int lineNumber = line.LineNumber;
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

			protected LineDescriptor (DocumentLine line, int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
				this.MarkerLength = line.MarkerCount;
				this.Spans = line.StartSpan;
			}

			public bool Equals (DocumentLine line, int offset, int length, out bool isInvalid)
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

			public LayoutDescriptor (DocumentLine line, int offset, int length, LayoutWrapper layout, int selectionStart, int selectionEnd) : base(line, offset, length)
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

			public bool Equals (DocumentLine line, int offset, int length, int selectionStart, int selectionEnd, out bool isInvalid)
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

		Dictionary<DocumentLine, LayoutDescriptor> layoutDict = new Dictionary<DocumentLine, LayoutDescriptor> ();
		
		public LayoutWrapper CreateLinePartLayout (ISyntaxMode mode, DocumentLine line, int logicalRulerColumn, int offset, int length, int selectionStart, int selectionEnd)
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
			if (textEditor.Options.WrapLines) {
				wrapper.Layout.Wrap = Pango.WrapMode.WordChar;
				wrapper.Layout.Width = (int)((textEditor.Allocation.Width - XOffset - TextStartPosition) * Pango.Scale.PangoScale);
			}
			StringBuilder textBuilder = new StringBuilder ();
			var chunks = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, offset, length);
			wrapper.Chunks = chunks;
			foreach (var chunk in chunks) {
				try {
					textBuilder.Append (Document.GetTextAt (chunk));
				} catch {
					Console.WriteLine (chunk);
				}
			}
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
				foreach (TextLineMarker marker in line.Markers)
					chunkStyle = marker.GetStyle (chunkStyle);

				if (chunkStyle != null) {
					//startOffset = chunk.Offset;
					//endOffset = chunk.EndOffset;

					uint startIndex = (uint)(oldEndIndex);
					uint endIndex = (uint)(startIndex + chunk.Length);
					oldEndIndex = endIndex;
					var markers = Document.GetTextSegmentMarkersAt (line).Where (m => m.IsVisible).ToArray ();
					HandleSelection (lineOffset, logicalRulerColumn, selectionStart, selectionEnd, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {
						if (containsPreedit) {
							if (textEditor.preeditOffset < start)
								start += (int)preeditLength;
							if (textEditor.preeditOffset < end)
								end += (int)preeditLength;
						}
						var si = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						var ei = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						var color = ColorStyle.GetForeground (chunkStyle);
						foreach (var marker in markers) {
							var chunkMarker = marker as IChunkMarker;
							if (chunkMarker == null)
								continue;
							chunkMarker.ChangeForeColor (textEditor, chunk, ref color);
						}
						atts.AddForegroundAttribute ((HslColor)color, si, ei);
						
						if (!chunkStyle.TransparentBackground && GetPixel (ColorStyle.PlainText.Background) != GetPixel (chunkStyle.Background)) {
							wrapper.AddBackground (chunkStyle.Background, (int)si, (int)ei);
						} else if (chunk.SpanStack != null && ColorStyle != null) {
							foreach (var span in chunk.SpanStack) {
								if (span == null || string.IsNullOrEmpty (span.Color))
									continue;
								var spanStyle = ColorStyle.GetChunkStyle (span.Color);
								if (spanStyle != null && !spanStyle.TransparentBackground && GetPixel (ColorStyle.PlainText.Background) != GetPixel (spanStyle.Background)) {
									wrapper.AddBackground (spanStyle.Background, (int)si, (int)ei);
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
						var color = !SelectionColor.TransparentForeground ? SelectionColor.Foreground : ColorStyle.GetForeground (chunkStyle);
						foreach (var marker in markers) {
							var chunkMarker = marker as IChunkMarker;
							if (chunkMarker == null)
								continue;
							chunkMarker.ChangeForeColor (textEditor, chunk, ref color);
						}
						atts.AddForegroundAttribute ((HslColor)color, si, ei);
						if (!wrapper.StartSet)
							wrapper.SelectionStartIndex = (int)si;
						wrapper.SelectionEndIndex = (int)ei;
					});

					var translatedStartIndex = TranslateToUTF8Index (lineChars, (uint)startIndex, ref curChunkIndex, ref byteChunkIndex);
					var translatedEndIndex = TranslateToUTF8Index (lineChars, (uint)endIndex, ref curChunkIndex, ref byteChunkIndex);

					if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
						atts.AddWeightAttribute ((Pango.Weight)chunkStyle.FontWeight, translatedStartIndex, translatedEndIndex);

					if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
						atts.AddStyleAttribute ((Pango.Style)chunkStyle.FontStyle, translatedStartIndex, translatedEndIndex);

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
			wrapper.IndentSize = 0;
			for (int i = 0; i < lineChars.Length; i++) {
				char ch = lineChars [i];
				if (ch == ' ') {
					wrapper.IndentSize ++;
				} else if (ch == '\t') {
					wrapper.IndentSize = GetNextTabstop (textEditor.GetTextEditorData (), wrapper.IndentSize);
				} else {
					break;
				}
			}

			var nextLine = line.NextLine;
			wrapper.EolSpanStack = nextLine != null ? nextLine.StartSpan : null;
			atts.AssignTo (wrapper.Layout);
			atts.Dispose ();
			int w, h;
			wrapper.Layout.GetSize (out w, out h);
			wrapper.PangoWidth = w;

			selectionStart = System.Math.Max (line.Offset - 1, selectionStart);
			selectionEnd = System.Math.Min (line.EndOffsetIncludingDelimiter + 1, selectionEnd);
			descriptor = new LayoutDescriptor (line, offset, length, wrapper, selectionStart, selectionEnd);
			if (!containsPreedit)
				layoutDict [line] = descriptor;
			//textEditor.GetTextEditorData ().HeightTree.SetLineHeight (line.LineNumber, System.Math.Max (LineHeight, System.Math.Floor (h / Pango.Scale.PangoScale)));
			return wrapper;
		}

		public void RemoveCachedLine (DocumentLine line)
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

			public ChunkDescriptor (DocumentLine line, int offset, int length, Chunk[] chunk) : base(line, offset, length)
			{
				this.Chunk = chunk;
			}
		}

		Dictionary<DocumentLine, ChunkDescriptor> chunkDict = new Dictionary<DocumentLine, ChunkDescriptor> ();

		IEnumerable<Chunk> GetCachedChunks (ISyntaxMode mode, TextDocument doc, Mono.TextEditor.Highlighting.ColorScheme style, DocumentLine line, int offset, int length)
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
			chunkDict [line] = descriptor;
			return chunks;
		}

		public void ForceInvalidateLine (int lineNr)
		{
			DocumentLine line = Document.GetLine (lineNr);
			LayoutDescriptor descriptor;
			if (line != null && layoutDict.TryGetValue (line, out descriptor)) {
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}
		}

		delegate void HandleSelectionDelegate (int start,int end);

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
			public int IndentSize {
				get;
				set;
			}

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

			public IEnumerable<Chunk> Chunks {
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

		ChunkStyle selectionColor;
		ChunkStyle SelectionColor {
			get {
				if (selectionColor == null)
					selectionColor = textEditor.HasFocus ? ColorStyle.SelectedText : ColorStyle.SelectedInactiveText;
				return selectionColor;
			}
		}

		AmbientColor currentLineColor;
		AmbientColor CurrentLineColor {
			get {
				if (currentLineColor == null)
					currentLineColor = textEditor.HasFocus ? ColorStyle.LineMarker : ColorStyle.LineMarkerInactive;
				return currentLineColor;
			}
		}

		public LayoutWrapper CreateLinePartLayout (ISyntaxMode mode, DocumentLine line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return CreateLinePartLayout (mode, line, -1, offset, length, selectionStart, selectionEnd);
		}

		#endregion

		public delegate void LineDecorator (Cairo.Context ctx,LayoutWrapper layout,int offset,int length,double xPos,double y,int selectionStart,int selectionEnd);

		public event LineDecorator DecorateLineBg;

		void DrawSpaceMarker (Cairo.Context cr, bool selected, double x, double x2, double y)
		{
			var d = textEditor.Options.Zoom * 2;
			var py = (int)(y + (LineHeight - d) / 2);
			cr.Rectangle (x + (x2 - x - d) / 2, py, d, d);
			cr.Fill ();
		}

		void DrawTabMarker (Cairo.Context cr, bool selected, double x, double x2, double y)
		{
			var py = (int)(y + LineHeight / 2);
			cr.MoveTo (0.5 + x, 0.5 + py);
			cr.LineTo (0.5 + x2 - charWidth / 2, 0.5 + py);
			cr.Stroke ();
		}

		const double whitespaceMarkerAlpha = 0.3;

		void DecorateTabsAndSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			int index, trailing;
			layout.Layout.XyToIndex ((int)textEditor.HAdjustment.Value, 0, out index, out trailing);
			var curchunk = layout.Chunks != null ? layout.Chunks.FirstOrDefault () : null;
			for (int i = index; i < layout.LineChars.Length; i++) {
				char ch = layout.LineChars [i];
				if (ch != ' ' && ch != '\t')
					continue;
				if (ch == ' ' && !textEditor.Options.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Space))
					continue;
				if (ch == '\t' && !textEditor.Options.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Tab))
					continue;
				bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
				if (first || oldSelected != selected) {
					first = false;
					oldSelected = selected;
				}
				if (!selected && textEditor.Options.ShowWhitespaces != ShowWhitespaces.Always)
					continue;
				Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
				double xpos = xPos + pos.X / Pango.Scale.PangoScale;
				if (xpos > textEditor.Allocation.Width)
					break;
				Pango.Rectangle pos2 = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i + 1, ref curIndex, ref byteIndex));
				double xpos2 = xPos + pos2.X / Pango.Scale.PangoScale;
				Cairo.Color col = new Cairo.Color (0, 0, 0);
				if (SelectionColor.TransparentForeground) {
					while (curchunk != null && curchunk.EndOffset < offset + i)
						curchunk = curchunk.Next;
					if (curchunk != null && curchunk.SpanStack.Count > 0 && curchunk.SpanStack.Peek ().Color != "Plain Text") {
						var chunkStyle = ColorStyle.GetChunkStyle (curchunk.SpanStack.Peek ().Color);
						if (chunkStyle != null)
							col = ColorStyle.GetForeground (chunkStyle);
					} else {
						col = ColorStyle.PlainText.Foreground;
					}
				} else {
					col = selected ? SelectionColor.Foreground : col = ColorStyle.PlainText.Foreground;
				}
				ctx.Color = new Cairo.Color (col.R, col.G, col.B, whitespaceMarkerAlpha);

				if (ch == '\t') {
					DrawTabMarker (ctx, selected, xpos, xpos2, y);
				} else {
					DrawSpaceMarker (ctx, selected, xpos, xpos2, y);
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
					ctx.Color = this.ColorStyle.BraceMatchingRectangle.GetColor ("color");
					ctx.Rectangle (bracketMatch);
					ctx.FillPreserve ();
					ctx.Color = this.ColorStyle.BraceMatchingRectangle.GetColor ("secondcolor");
					ctx.Stroke ();
				}
			}
		}

		public LayoutWrapper GetLayout (DocumentLine line)
		{
			ISyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);
			return CreateLinePartLayout (mode, line, line.Offset, line.Length, -1, -1);
		}

		public void DrawCaretLineMarker (Cairo.Context cr, double xPos, double y, double width, double lineHeight)
		{
			xPos = System.Math.Floor (xPos);
			cr.Rectangle (xPos, y, width, lineHeight);
			var color = CurrentLineColor;
			cr.Color = color.GetColor ("color");
			cr.Fill ();
			double halfLine = (cr.LineWidth / 2.0);
			cr.MoveTo (xPos, y + halfLine);
			cr.LineTo (xPos + width, y + halfLine);
			cr.MoveTo (xPos, y + lineHeight - halfLine);
			cr.LineTo (xPos + width, y + lineHeight - halfLine);
			cr.Color = color.GetColor ("secondcolor");
			cr.Stroke ();
		}

		void DrawIndent (Cairo.Context cr, LayoutWrapper layout, DocumentLine line, double xPos, double y)
		{
			if (!textEditor.Options.DrawIndentationMarkers)
				return;
			if (line.Length == 0) {
				var nextLine = line.NextLine;
				while (nextLine != null && nextLine.Length == 0)
					nextLine = nextLine.NextLine;
				if (nextLine != null)
					layout = GetLayout (nextLine);
			}
			if (layout.IndentSize == 0)
				return;
			cr.Save ();
			var dotted = new [] { textEditor.Options.Zoom };
			cr.SetDash (dotted, (int)y + textEditor.VAdjustment.Value);
			var top = y;
			var bottom = y + LineHeight;
			if (Caret.Line == line.LineNumber && textEditor.Options.HighlightCaretLine) {
				top += textEditor.Options.Zoom;
				bottom -= textEditor.Options.Zoom;
			}
			for (int i = 0; i < layout.IndentSize; i += textEditor.Options.IndentationSize) {
				var x = System.Math.Floor (xPos + i * charWidth);
				cr.MoveTo (x + 0.5, top);
				cr.LineTo (x + 0.5, bottom);

				cr.Color = ColorStyle.IndentationGuide.GetColor ("color");
				cr.Stroke ();
			}
			cr.Restore ();
		}

		void DrawLinePart (Cairo.Context cr, DocumentLine line, int lineNumber, int logicalRulerColumn, int offset, int length, ref double pangoPosition, ref bool isSelectionDrawn, double y, double maxX, double _lineHeight)
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

			// The caret line marker must be drawn below the text markers otherwise the're invisible
			if ((HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber)
				DrawCaretLineMarker (cr, xPos, y, layout.PangoWidth / Pango.Scale.PangoScale, _lineHeight);

			//		if (!(HighlightCaretLine || textEditor.Options.HighlightCaretLine) || Document.GetLine(Caret.Line) != line) {
			if (BackgroundRenderer == null) {
				foreach (var bg in layout.BackgroundColors) {
					int x1, x2;
					x1 = layout.Layout.IndexToPos (bg.FromIdx).X;
					x2 = layout.Layout.IndexToPos (bg.ToIdx).X;
					DrawRectangleWithRuler (
						cr, xPos + textEditor.HAdjustment.Value - TextStartPosition,
						new Cairo.Rectangle ((x1 + pangoPosition) / Pango.Scale.PangoScale, y, (x2 - x1) / Pango.Scale.PangoScale + 1, _lineHeight),
						bg.Color, true);
				}
			}


			bool drawBg = true;
			bool drawText = true;
			foreach (TextLineMarker marker in line.Markers) {
				IBackgroundMarker bgMarker = marker as IBackgroundMarker;
				if (bgMarker == null || !marker.IsVisible)
					continue;
				isSelectionDrawn |= (marker.Flags & TextLineMarkerFlags.DrawsSelection) == TextLineMarkerFlags.DrawsSelection;
				drawText &= bgMarker.DrawBackground (textEditor, cr, layout, selectionStart, selectionEnd, offset, offset + length, y, xPos, xPos + width, ref drawBg);
			}

			if (DecorateLineBg != null)
				DecorateLineBg (cr, layout, offset, length, xPos, y, selectionStart, selectionEnd);
			

			if (!isSelectionDrawn && (layout.StartSet || selectionStart == offset + length) && BackgroundRenderer == null) {
				double startX;
				double endX;

				if (selectionStart != offset + length) {
					var start = layout.Layout.IndexToPos ((int)layout.SelectionStartIndex);
					startX = System.Math.Floor (start.X / Pango.Scale.PangoScale);
					var end = layout.Layout.IndexToPos ((int)layout.SelectionEndIndex);
					endX = System.Math.Ceiling (end.X / Pango.Scale.PangoScale);
				} else {
					startX = width;
					endX = startX;
				}

				if (textEditor.MainSelection.SelectionMode == SelectionMode.Block && startX == endX) {
					endX = startX + 2;
				}
				DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (xPos + startX, y, endX - startX, _lineHeight), this.SelectionColor.Background, true);
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
							int w = (int) System.Math.Ceiling ((x2 - x1) / Pango.Scale.PangoScale);
							int s = (int) System.Math.Floor ((x1 + x) / Pango.Scale.PangoScale);
							double corner = System.Math.Min (4, width) * textEditor.Options.Zoom;

							cr.Color = MainSearchResult.IsInvalid || MainSearchResult.Offset != firstSearch.Offset ? ColorStyle.SearchResult.GetColor ("color") : ColorStyle.SearchResultMain.GetColor ("color");
							FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, s, y, corner, w + 1, LineHeight);
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
			if (offset == line.Offset) {
				DrawIndent (cr, layout, line, xPos, y);
			}

			if (textEditor.Options.ShowWhitespaces != ShowWhitespaces.Never && !(BackgroundRenderer != null && textEditor.Options.ShowWhitespaces == ShowWhitespaces.Selection))
				DecorateTabsAndSpaces (cr, layout, offset, length, xPos, y, selectionStart, selectionEnd);

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

						if (!isSelectionDrawn && (selectionEnd == lineOffset + line.Length) && BackgroundRenderer == null) {
							double startX;
							double endX;
							startX = xPos;
							endX = (pangoPosition + vx + layout.PangoWidth) / Pango.Scale.PangoScale;
							DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (startX, y, endX - startX, _lineHeight), this.SelectionColor.Background, true);
						}

						// When drawing virtual space before the selection start paint it as unselected.
						var virtualSpaceMod = selectionStart < caretOffset ? 0 : virtualSpace.Length;

						if ((!textEditor.IsSomethingSelected || (selectionStart >= offset && selectionStart != selectionEnd)) && (HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber)
							DrawCaretLineMarker (cr, pangoPosition / Pango.Scale.PangoScale, y, vx / Pango.Scale.PangoScale, _lineHeight);

						if (DecorateLineBg != null)
							DecorateLineBg (cr, wrapper, offset, length, xPos, y, selectionStart + virtualSpaceMod, selectionEnd + virtualSpace.Length);

						switch (textEditor.Options.ShowWhitespaces) {
						case ShowWhitespaces.Selection:
							if (textEditor.IsSomethingSelected && (selectionStart < offset || selectionStart == selectionEnd) && BackgroundRenderer == null)
								DecorateTabsAndSpaces (cr, wrapper, offset, length, xPos, y, selectionStart, selectionEnd + virtualSpace.Length);
							break;
						case ShowWhitespaces.Always:
							DecorateTabsAndSpaces (cr, wrapper, offset, length, xPos, y, selectionStart, selectionEnd + virtualSpace.Length);
							break;
						}

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
						SetVisibleCaretPosition (xPos + (strong_pos.X / Pango.Scale.PangoScale), y + (strong_pos.Y / Pango.Scale.PangoScale));
					}
				}
			}

			foreach (TextLineMarker marker in line.Markers.Where (m => m.IsVisible)) {
				if (layout.Layout != null)
					marker.Draw (textEditor, cr, layout.Layout, false, /*selected*/offset, offset + length, y, xPos, xPos + width);
			}

			foreach (var marker in Document.GetTextSegmentMarkersAt (line).Where (m => m.IsVisible)) {
				if (layout.Layout != null)
					marker.Draw (textEditor, cr, layout.Layout, false, /*selected*/offset, offset + length, y, xPos, xPos + width);
			}

			pangoPosition += layout.PangoWidth;
			int scaledDown = (int)(pangoPosition / Pango.Scale.PangoScale);
			pangoPosition = scaledDown * Pango.Scale.PangoScale;

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

		void DrawEolMarker (Cairo.Context cr, DocumentLine line, bool selected, double x, double y)
		{
			if (!textEditor.Options.IncludeWhitespaces.HasFlag (IncludeWhitespaces.LineEndings))
				return;

			Pango.Layout layout;
			Pango.Rectangle rect;
			switch (line.DelimiterLength) {
			case 0:
				// an emty line end should only happen at eof
				layout = eofEolLayout;
				rect = eofEolLayoutRect;
				break;
			case 1:
				if (Document.GetCharAt (line.Offset + line.Length) == '\n') {
					layout = unixEolLayout;
					rect = unixEolLayoutRect;
				} else {
					layout = macEolLayout;
					rect = macEolLayoutRect;
				}
				break;
			case 2:
				layout = windowsEolLayout;
				rect = windowsEolLayoutRect;
				break;
			default:
				throw new InvalidOperationException (); // other line endings are not known.
			}
			cr.Save ();
			cr.Translate (x, y + System.Math.Max (0, LineHeight - rect.Height - 1));
			var col = ColorStyle.PlainText.Foreground;

			if (selected && !SelectionColor.TransparentForeground) {
				col = SelectionColor.Foreground;
			} else {
				if (line != null && line.NextLine != null && line.NextLine.StartSpan != null && line.NextLine.StartSpan.Count > 0) {
					var span = line.NextLine.StartSpan.Peek ();
					var chunkStyle = ColorStyle.GetChunkStyle (span.Color);
					if (chunkStyle != null)
						col = ColorStyle.GetForeground (chunkStyle);
				}
			}

			cr.Color = new Cairo.Color (col.R, col.G, col.B, whitespaceMarkerAlpha);
			cr.ShowLayout (layout);
			cr.Restore ();
		}

		static internal ulong GetPixel (Color color)
		{
			return (((ulong)color.Red) << 32) | (((ulong)color.Green) << 16) | ((ulong)color.Blue);
		}

		static internal ulong GetPixel (Cairo.Color color)
		{
			return GetPixel ((Gdk.Color) ((HslColor)color));
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

		internal bool CalculateClickLocation (double x, double y, out DocumentLocation clickLocation)
		{
			VisualLocationTranslator trans = new VisualLocationTranslator (this);

			clickLocation = trans.PointToLocation (x, y);
			if (clickLocation.Line < DocumentLocation.MinLine || clickLocation.Column < DocumentLocation.MinColumn)
				return false;
			DocumentLine line = Document.GetLine (clickLocation.Line);
			if (line != null && clickLocation.Column >= line.Length + 1 && GetWidth (Document.GetTextAt (line.SegmentIncludingDelimiter) + "-") < x) {
				clickLocation = new DocumentLocation (clickLocation.Line, line.Length + 1);
				if (textEditor.GetTextEditorData ().HasIndentationTracker && textEditor.Options.IndentStyle == IndentStyle.Virtual) {
					int indentationColumn = this.textEditor.GetTextEditorData ().GetVirtualIndentationColumn (clickLocation);
					if (indentationColumn > clickLocation.Column)
						clickLocation = new DocumentLocation (clickLocation.Line, indentationColumn);
				}
			}
			return true;
		}

		protected internal override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (args.TriggersContextMenu ())
				return;
			
			inSelectionDrag = false;
			inDrag = false;
			Selection selection = textEditor.MainSelection;
			int oldOffset = textEditor.Caret.Offset;

			string link = GetLink != null ? GetLink (args) : null;
			if (!String.IsNullOrEmpty (link)) {
				textEditor.FireLinkEvent (link, args.Button, args.ModifierState);
				return;
			}

			if (args.Button == 1) {
				if (!CalculateClickLocation (args.X, args.Y, out clickLocation))
					return;

				DocumentLine line = Document.GetLine (clickLocation.Line);
				bool isHandled = false;
				if (line != null) {
					foreach (TextLineMarker marker in line.Markers) {
						if (marker is IActionTextLineMarker) {
							isHandled |= ((IActionTextLineMarker)marker).MousePressed (textEditor, args);
							if (isHandled)
								break;
						}
					}
				}
				if (isHandled)
					return;

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

					// folding marker
					int lineNr = args.LineNumber;
					foreach (var shownFolding in GetFoldRectangles (lineNr)) {
						if (shownFolding.Item1.Contains ((int)(args.X + this.XOffset), (int)args.Y)) {
							shownFolding.Item2.IsFolded = false;
							return;
						}
					}
					return;
				} else if (args.Type == EventType.ThreeButtonPress) {
					int lineNr = Document.OffsetToLineNumber (offset);
					textEditor.SetSelectLines (lineNr, lineNr);

					var range = textEditor.SelectionRange;
					mouseWordStart = range.Offset;
					mouseWordEnd = range.EndOffset;

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
						textEditor.ClearSelection ();
						Caret.Location = clickLocation;
						inSelectionDrag = true;
						textEditor.SetSelection (clickLocation, clickLocation);
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
				var oldVersion = textEditor.Document.Version;

				bool autoScroll = textEditor.Caret.AutoScrollToCaret;
				textEditor.Caret.AutoScrollToCaret = false;
				if (selection != null && selectionRange.Contains (offset)) {
					textEditor.ClearSelection ();
					textEditor.Caret.Offset = selectionRange.EndOffset;
					return;
				}

				ClipboardActions.PasteFromPrimary (textEditor.GetTextEditorData (), offset);
				textEditor.Caret.Offset = oldOffset;
				if (!selectionRange.IsInvalid)
					textEditor.SelectionRange = new TextSegment (oldVersion.MoveOffsetTo (Document.Version, selectionRange.Offset), selectionRange.Length);

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
			int indentLength = SyntaxMode.GetIndentLength (Document, previewSegment.Offset, previewSegment.Length, false);

			StringBuilder textBuilder = new StringBuilder ();
			int curOffset = previewSegment.Offset;
			while (curOffset >= 0 && curOffset < previewSegment.EndOffset && curOffset < Document.TextLength) {
				DocumentLine line = Document.GetLineByOffset (curOffset);
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
				previewWindow = new CodeSegmentPreviewWindow (textEditor, false, segment);
				if (previewWindow.IsEmptyText) {
					previewWindow.Destroy ();
					previewWindow = null;
					return false;
				}
					
				int ox = 0, oy = 0;
				this.textEditor.GdkWindow.GetOrigin (out ox, out oy);
				ox += textEditor.Allocation.X;
				oy += textEditor.Allocation.Y;

				int x = hintRectangle.Right;
				int y = hintRectangle.Bottom;
				previewWindow.CalculateSize ();
				var req = previewWindow.SizeRequest ();
				int w = req.Width;
				int h = req.Height;

				var geometry = this.textEditor.Screen.GetUsableMonitorGeometry (this.textEditor.Screen.GetMonitorAtPoint (ox + x, oy + y));

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

		public DocumentLine HoveredLine {
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

		List<IActionTextLineMarker> oldMarkers = new List<IActionTextLineMarker> ();
		List<IActionTextLineMarker> newMarkers = new List<IActionTextLineMarker> ();

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

			var hoverResult = new TextLineMarkerHoverResult ();
			oldMarkers.ForEach (m => m.MouseHover (textEditor, args, hoverResult));

			if (line != null) {
				newMarkers.Clear ();
				newMarkers.AddRange (line.Markers.Where (m => m is IActionTextLineMarker).Cast <IActionTextLineMarker> ());
				var extraMarker = Document.GetExtendingTextMarker (loc.Line) as IActionTextLineMarker;
				if (extraMarker != null && !oldMarkers.Contains (extraMarker))
					newMarkers.Add (extraMarker);
				foreach (var marker in newMarkers.Where (m => !oldMarkers.Contains (m))) {
					marker.MouseHover (textEditor, args, hoverResult);
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
				foreach (var shownFolding in GetFoldRectangles (lineNr)) {
					if (shownFolding.Item1.Contains ((int)(args.X + this.XOffset), (int)args.Y)) {
						ShowTooltip (shownFolding.Item2.Segment, shownFolding.Item1);
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
				DocumentLine line1 = textEditor.Document.GetLine (loc.Line);
				DocumentLine line2 = textEditor.Document.GetLineByOffset (textEditor.SelectionAnchor);
				var o2 = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffsetIncludingDelimiter;
				Caret.Offset = o2;
				if (textEditor.MainSelection != null) {
					textEditor.MainSelection.Lead = Caret.Location;
					if (mouseWordStart < o2) {
						textEditor.MainSelection.Anchor = textEditor.OffsetToLocation (mouseWordStart);
					} else {
						textEditor.MainSelection.Anchor = textEditor.OffsetToLocation (mouseWordEnd);

					}
				}

				break;
			}
			Caret.PreserveSelection = false;

			//HACK: use cmd as Mac block select modifier because GTK currently makes it impossible to access alt/mod1
			//NOTE: Mac cmd seems to be mapped as ControlMask from mouse events on older GTK, mod1 on newer
			var blockSelModifier = !Platform.IsMac ? ModifierType.Mod1Mask
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
			defaultLayout.SetText (text);
			int width, height;
			defaultLayout.GetPixelSize (out width, out height);
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
			var left = (int)(area.X);
			var width = (int)area.Width + 1;
			if (textEditor.Options.ShowRuler) {
				var right = left + width;

				var divider = (int) (System.Math.Max (left, System.Math.Min (x + TextStartPosition + rulerX, right)));
				if (divider < right) {
					var beforeDividerWidth = divider - left;
					if (beforeDividerWidth > 0) {
						cr.Rectangle (left, area.Y, beforeDividerWidth, area.Height);
						cr.Fill ();
					}
					cr.Rectangle (divider, area.Y, right - divider, area.Height);
					cr.Color = DimColor (color);
					cr.Fill ();

					if (beforeDividerWidth > 0) {
						cr.DrawLine (
							ColorStyle.Ruler.GetColor ("color"),
							divider + 0.5, area.Y,
							divider + 0.5, area.Y + area.Height);
					}
					return;
				}
			}

			cr.Rectangle (left, area.Y, System.Math.Ceiling (area.Width), area.Height);
			cr.Fill ();
		}

		IEnumerable<Tuple<Gdk.Rectangle, FoldSegment>> GetFoldRectangles (int lineNr)
		{
			if (lineNr < 0)
				yield break;

			var line = lineNr <= Document.LineCount ? Document.GetLine (lineNr) : null;
			//			int xStart = XOffset;
			int y = (int)(LineToY (lineNr) - textEditor.VAdjustment.Value);
			//			Gdk.Rectangle lineArea = new Gdk.Rectangle (XOffset, y, textEditor.Allocation.Width - XOffset, LineHeight);
			int width, height;
			var xPos = this.XOffset + this.TextStartPosition - textEditor.HAdjustment.Value;

			if (line == null)
				yield break;

			var foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			double foldXMargin = foldMarkerXMargin * textEditor.Options.Zoom;
			restart:
			using (var calcTextLayout = PangoUtil.CreateLayout (textEditor))
			using (var calcFoldingLayout = PangoUtil.CreateLayout (textEditor)) {
				calcTextLayout.FontDescription = textEditor.Options.Font;
				calcTextLayout.Tabs = this.tabArray;

				calcFoldingLayout.FontDescription = markerLayout.FontDescription;
				calcFoldingLayout.Tabs = this.tabArray;
				foreach (var folding in foldings) {
					int foldOffset = folding.StartLine.Offset + folding.Column - 1;
					if (foldOffset < offset)
						continue;

					if (folding.IsFolded) {
						var txt = Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.TextLength - offset)));
						calcTextLayout.SetText (txt);
						calcTextLayout.GetSize (out width, out height);
						xPos += width / Pango.Scale.PangoScale;
						offset = folding.EndLine.Offset + folding.EndColumn;

						calcFoldingLayout.SetText (folding.Description);

						calcFoldingLayout.GetSize (out width, out height);

						var pixelWidth = width / Pango.Scale.PangoScale + foldXMargin * 2;

						var foldingRectangle = new Rectangle ((int)xPos, y, (int)pixelWidth, (int)LineHeight - 1);
						yield return Tuple.Create (foldingRectangle, folding);
						xPos += pixelWidth;
						if (folding.EndLine != line) {
							line = folding.EndLine;
							foldings = Document.GetStartFoldings (line);
							goto restart;
						}
					}
				}
			}
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

		[Flags]
		public enum CairoCorners
		{
			None = 0,
			TopLeft = 1,
			TopRight = 2,
			BottomLeft = 4,
			BottomRight = 8,
			All = 15
		}

		public static void RoundedRectangle(Cairo.Context cr, double x, double y, double w, double h,
		                                    double r, CairoCorners corners, bool topBottomFallsThrough)
		{
			if(topBottomFallsThrough && corners == CairoCorners.None) {
				cr.MoveTo(x, y - r);
				cr.LineTo(x, y + h + r);
				cr.MoveTo(x + w, y - r);
				cr.LineTo(x + w, y + h + r);
				return;
			} else if(r < 0.0001 || corners == CairoCorners.None) {
				cr.Rectangle(x, y, w, h);
				return;
			}
			
			if((corners & (CairoCorners.TopLeft | CairoCorners.TopRight)) == 0 && topBottomFallsThrough) {
				y -= r;
				h += r;
				cr.MoveTo(x + w, y);
			} else {
				if((corners & CairoCorners.TopLeft) != 0) {
					cr.MoveTo(x + r, y);
				} else {
					cr.MoveTo(x, y);
				}
				if((corners & CairoCorners.TopRight) != 0) {
					cr.Arc(x + w - r, y + r, r, System.Math.PI * 1.5, System.Math.PI * 2);
				} else {
					cr.LineTo(x + w, y);
				}
			}
			
			if((corners & (CairoCorners.BottomLeft | CairoCorners.BottomRight)) == 0 && topBottomFallsThrough) {
				h += r;
				cr.LineTo(x + w, y + h);
				cr.MoveTo(x, y + h);
				cr.LineTo(x, y + r);
				cr.Arc(x + r, y + r, r, System.Math.PI, System.Math.PI * 1.5);
			} else {
				if((corners & CairoCorners.BottomRight) != 0) {
					cr.Arc(x + w - r, y + h - r, r, 0, System.Math.PI * 0.5);
				} else {
					cr.LineTo(x + w, y + h);
				}
				
				if((corners & CairoCorners.BottomLeft) != 0) {
					cr.Arc(x + r, y + h - r, r, System.Math.PI * 0.5, System.Math.PI);
				} else {
					cr.LineTo(x, y + h);
				}
				
				if((corners & CairoCorners.TopLeft) != 0) {
					cr.Arc(x + r, y + r, r, System.Math.PI, System.Math.PI * 1.5);
				} else {
					cr.LineTo(x, y);
				}
			}
		}

		const double foldMarkerXMargin = 4.0;

		protected internal override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNr, double x, double y, double _lineHeight)
		{
//			double xStart = System.Math.Max (area.X, XOffset);
//			xStart = System.Math.Max (0, xStart);
			var correctedXOffset = System.Math.Floor (XOffset) - 1;
			var lineArea = new Cairo.Rectangle (correctedXOffset, y, textEditor.Allocation.Width - correctedXOffset, _lineHeight);
			int width, height;
			double pangoPosition = (x - textEditor.HAdjustment.Value + TextStartPosition) * Pango.Scale.PangoScale;

			defaultBgColor = Document.ReadOnly ? ColorStyle.BackgroundReadOnly.GetColor ("color") : ColorStyle.PlainText.Background;

			// Draw the default back color for the whole line. Colors other than the default
			// background will be drawn when rendering the text chunks.
			if (BackgroundRenderer == null)
				DrawRectangleWithRuler (cr, x, lineArea, defaultBgColor, true);
			bool isSelectionDrawn = false;

			// Check if line is beyond the document length
			if (line == null) {
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

			if ((HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNr)
				DrawCaretLineMarker (cr, x, y, TextStartPosition, _lineHeight);

			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column - 1;
				if (foldOffset < offset)
					continue;

				if (folding.IsFolded) {
					
					DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, foldOffset - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width, _lineHeight);
					
					offset = folding.EndLine.Offset + folding.EndColumn;
					markerLayout.SetText (folding.Description);
					markerLayout.GetSize (out width, out height);
					
					bool isFoldingSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding.Segment);
					double pixelX = 0.5 + System.Math.Floor (pangoPosition / Pango.Scale.PangoScale);
					double foldXMargin = foldMarkerXMargin * textEditor.Options.Zoom;
					double pixelWidth = System.Math.Floor ((pangoPosition + width) / Pango.Scale.PangoScale - pixelX + foldXMargin * 2);
					var foldingRectangle = new Cairo.Rectangle (
						pixelX, 
						y, 
						pixelWidth, 
						this.LineHeight);

					if (BackgroundRenderer == null && isFoldingSelected) {
						cr.Color = SelectionColor.Background;
						cr.Rectangle (foldingRectangle);
						cr.Fill ();
					}

					if (isFoldingSelected && SelectionColor.TransparentForeground) {
						cr.Color = ColorStyle.CollapsedText.Foreground;
					} else {
						cr.Color = isFoldingSelected ? SelectionColor.Foreground : ColorStyle.CollapsedText.Foreground;
					}
					var boundingRectangleHeight = foldingRectangle.Height - 1;
					var boundingRectangleY = System.Math.Floor (foldingRectangle.Y + (foldingRectangle.Height - boundingRectangleHeight) / 2);
					RoundedRectangle (cr,
					                 System.Math.Floor (foldingRectangle.X) + 0.5,
					                 boundingRectangleY + 0.5,
					                 System.Math.Floor (foldingRectangle.Width - cr.LineWidth),
					                 System.Math.Floor (boundingRectangleHeight - cr.LineWidth),
					                 LineHeight / 8, CairoCorners.All, false);
					cr.Stroke ();
					
					cr.Save ();
					cr.Translate (
						pangoPosition / Pango.Scale.PangoScale + foldXMargin,
						System.Math.Floor (boundingRectangleY + (boundingRectangleHeight - System.Math.Floor (height / Pango.Scale.PangoScale)) / 2));
					cr.ShowLayout (markerLayout);
					cr.Restore ();

					if (caretOffset == foldOffset && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition ((int)(pangoPosition / Pango.Scale.PangoScale), y);
					pangoPosition += foldingRectangle.Width * Pango.Scale.PangoScale;
					if (caretOffset == foldOffset + folding.Length && !string.IsNullOrEmpty (folding.Description))
						SetVisibleCaretPosition ((int)(pangoPosition / Pango.Scale.PangoScale), y);

					if (folding.EndLine != line) {
						line = folding.EndLine;
						lineNr = line.LineNumber;
						foldings = Document.GetStartFoldings (line);
						isEolFolded = line.Length <= folding.EndColumn;
						goto restart;
					}
					isEolFolded = line.Length <= folding.EndColumn;
				}
			}
			
			// Draw remaining line - must be called for empty line parts as well because the caret may be at this positon
			// and the caret position is calculated in DrawLinePart.
			if (line.EndOffsetIncludingDelimiter - offset >= 0) {
				DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, line.Offset + line.Length - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width, _lineHeight);
			}

			bool isEolSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionMode == SelectionMode.Normal && textEditor.SelectionRange.Contains (line.Offset + line.Length);
			var lx = (int)(pangoPosition / Pango.Scale.PangoScale);
			lineArea = new Cairo.Rectangle (lx,
				lineArea.Y,
				textEditor.Allocation.Width - lx,
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
				x1 += correctedXOffset - textEditor.HAdjustment.Value;
				x2 += correctedXOffset - textEditor.HAdjustment.Value;

				if (x2 > lineArea.X && BackgroundRenderer == null)  {
					if (x1 - lineArea.X > 0) {
						DrawRectangleWithRuler (cr, x, new Cairo.Rectangle (lineArea.X, lineArea.Y, x1 - lineArea.X, lineArea.Height), defaultBgColor, false);
						lineArea = new Cairo.Rectangle (x1, lineArea.Y, lineArea.Width, lineArea.Height);
					}
					DrawRectangleWithRuler (cr, x, new Cairo.Rectangle (lineArea.X, lineArea.Y, x2 - lineArea.X, lineArea.Height), this.SelectionColor.Background, false);
					lineArea = new Cairo.Rectangle (x2, lineArea.Y, textEditor.Allocation.Width - lineArea.X, lineArea.Height);
				}
			}

			if (!isSelectionDrawn && BackgroundRenderer == null) {
				if (isEolSelected) {
					// prevent "gaps" in the selection drawing ('fuzzy' lines problem)
					var eolStartX = System.Math.Floor (pangoPosition / Pango.Scale.PangoScale);
					lineArea = new Cairo.Rectangle (
						eolStartX,
						lineArea.Y,
						textEditor.Allocation.Width - eolStartX,
						lineArea.Height);
					DrawRectangleWithRuler (cr, x, lineArea, this.SelectionColor.Background, false);
					if (line.Length == 0)
						DrawIndent (cr, GetLayout (line), line, lx, y);
				} else if (!(HighlightCaretLine || textEditor.Options.HighlightCaretLine) || Caret.Line != lineNr) {
					LayoutWrapper wrapper = GetLayout (line);
					if (wrapper.EolSpanStack != null) {
						foreach (var span in wrapper.EolSpanStack) {
							var spanStyle = textEditor.ColorStyle.GetChunkStyle (span.Color);
							if (spanStyle == null)
								continue;
							if (!spanStyle.TransparentBackground && GetPixel (ColorStyle.PlainText.Background) != GetPixel (spanStyle.Background)) {
								DrawRectangleWithRuler (cr, x, lineArea, spanStyle.Background, false);
								break;
							}
						}
					}
				} else {
					double xPos = pangoPosition / Pango.Scale.PangoScale;
					DrawCaretLineMarker (cr, xPos, y, lineArea.X + lineArea.Width - xPos, _lineHeight);
				}
			}

			if (textEditor.Options.ShowWhitespaces != ShowWhitespaces.Never) {
				if (!isEolFolded && isEolSelected || textEditor.Options.ShowWhitespaces == ShowWhitespaces.Always)
					if (!(BackgroundRenderer != null && textEditor.Options.ShowWhitespaces == ShowWhitespaces.Selection))
						DrawEolMarker (cr, line, isEolSelected, pangoPosition / Pango.Scale.PangoScale, y);
			}

			var extendingMarker = Document.GetExtendingTextMarker (lineNr);
			if (extendingMarker != null)
				extendingMarker.Draw (textEditor, cr, lineNr, lineArea);
			
			lastLineRenderWidth = pangoPosition / Pango.Scale.PangoScale;
			if (textEditor.HAdjustment.Value > 0) {
				cr.LineWidth = textEditor.Options.Zoom;
				for (int i = 0; i < verticalShadowAlphaTable.Length; i++) {
					cr.Color = new Cairo.Color (0, 0, 0, 1 - verticalShadowAlphaTable[i]);
					cr.MoveTo (x + i * cr.LineWidth + 0.5, y);
					cr.LineTo (x + i * cr.LineWidth + 0.5, y + 1 + _lineHeight);
					cr.Stroke ();
				}
			}
		}

		static double[] verticalShadowAlphaTable = new [] { 0.71, 0.84, 0.95 };

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
			DocumentLine line;
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

			public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false)
			{
				lineNumber = System.Math.Min (margin.YToLine (yp + margin.textEditor.VAdjustment.Value), margin.Document.LineCount);
				line = lineNumber <= margin.Document.LineCount ? margin.Document.GetLine (lineNumber) : null;
				if (line == null)
					return DocumentLocation.Empty;
				
				int offset = line.Offset;
				
				xp -= margin.TextStartPosition;
				xp += margin.textEditor.HAdjustment.Value;
				xp *= Pango.Scale.PangoScale;
				if (xp < 0)
					return new DocumentLocation (lineNumber, DocumentLocation.MinColumn);
				int column = DocumentLocation.MinColumn;
				ISyntaxMode mode = margin.Document.SyntaxMode != null && margin.textEditor.Options.EnableSyntaxHighlighting ? margin.Document.SyntaxMode : new SyntaxMode (margin.Document);
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				bool done = false;
				Pango.Layout measueLayout = null;
				try {
					restart:
					int logicalRulerColumn = line.GetLogicalColumn (margin.textEditor.GetTextEditorData (), margin.textEditor.Options.RulerColumn);
					foreach (FoldSegment folding in foldings.Where(f => f.IsFolded)) {
						int foldOffset = folding.StartLine.Offset + folding.Column - 1;
						if (foldOffset < offset)
							continue;
						layoutWrapper = margin.CreateLinePartLayout (mode, line, logicalRulerColumn, line.Offset, foldOffset - offset, -1, -1);
						done |= ConsumeLayout ((int)(xp - xPos), 0);
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

						offset = folding.EndLine.Offset + folding.EndColumn - 1;
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
						layoutWrapper = margin.CreateLinePartLayout (mode, line, logicalRulerColumn, offset, line.Offset + line.Length - offset, -1, -1);
						if (!ConsumeLayout ((int)(xp - xPos), 0)) {
							if (endAtEol)
								return DocumentLocation.Empty; 
						}
					}
				} finally {
					if (measueLayout != null)
						measueLayout.Dispose ();
				}
				return new DocumentLocation (lineNumber, column + index);
			}
		}

		public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (xp, yp, endAtEol);
		}
		
		public DocumentLocation PointToLocation (Cairo.Point p, bool endAtEol = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y, endAtEol);
		}
		
		public DocumentLocation PointToLocation (Cairo.PointD p, bool endAtEol = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y, endAtEol);
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
			DocumentLine line = Document.GetLine (loc.Line);
			if (line == null)
				return new Cairo.Point (-1, -1);
			int x = (int)(ColumnToX (line, loc.Column) + this.XOffset + this.TextStartPosition);
			int y = (int)LineToY (loc.Line);
			return useAbsoluteCoordinates ? new Cairo.Point (x, y) : new Cairo.Point (x - (int)this.textEditor.HAdjustment.Value, y - (int)this.textEditor.VAdjustment.Value);
		}
		
		public double ColumnToX (DocumentLine line, int column)
		{
			column--;
			// calculate virtual indentation
			if (column > 0 && line.Length == 0 && textEditor.GetTextEditorData ().HasIndentationTracker) {
				using (var l = PangoUtil.CreateLayout (textEditor, textEditor.GetTextEditorData ().IndentationTracker.GetIndentationString (line.Offset))) {
					l.Alignment = Pango.Alignment.Left;
					l.FontDescription = textEditor.Options.Font;
					l.Tabs = tabArray;

					Pango.Rectangle ink_rect, logical_rect;
					l.GetExtents (out ink_rect, out logical_rect);
					return (logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale;
				}
			}
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

				foreach (TextLineMarker marker in line.Markers)
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
						var color = textEditor.ColorStyle.GetForeground (chunkStyle);
						Pango.AttrForeground foreGround = new Pango.AttrForeground (
							(ushort)(color.R * ushort.MaxValue),
							(ushort)(color.G * ushort.MaxValue),
							(ushort)(color.B * ushort.MaxValue));
						foreGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						foreGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Add (foreGround);
						if (!chunkStyle.TransparentBackground) {
							color = chunkStyle.Background;
							var background = new Pango.AttrBackground (
								(ushort)(color.R * ushort.MaxValue),
								(ushort)(color.G * ushort.MaxValue),
								(ushort)(color.B * ushort.MaxValue));
							background.StartIndex = foreGround.StartIndex;
							background.EndIndex = foreGround.EndIndex;
							attributes.Add (background);
						}
					}, delegate(int start, int end) {
						Pango.AttrForeground selectedForeground;
						if (!SelectionColor.TransparentForeground) {
							var color = SelectionColor.Foreground;
							selectedForeground = new Pango.AttrForeground (
								(ushort)(color.R * ushort.MaxValue),
								(ushort)(color.G * ushort.MaxValue),
								(ushort)(color.B * ushort.MaxValue));
						} else {
							var color = ColorStyle.GetForeground (chunkStyle);
							selectedForeground = new Pango.AttrForeground (
								(ushort)(color.R * ushort.MaxValue),
								(ushort)(color.G * ushort.MaxValue),
								(ushort)(color.B * ushort.MaxValue));
						} 
						selectedForeground.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						selectedForeground.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Add (selectedForeground);
					});

					var translatedStartIndex = TranslateToUTF8Index (lineChars, (uint)startIndex, ref curChunkIndex, ref byteChunkIndex);
					var translatedEndIndex = TranslateToUTF8Index (lineChars, (uint)endIndex, ref curChunkIndex, ref byteChunkIndex);

					if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal) {
						Pango.AttrWeight attrWeight = new Pango.AttrWeight ((Pango.Weight)chunkStyle.FontWeight);
						attrWeight.StartIndex = translatedStartIndex;
						attrWeight.EndIndex = translatedEndIndex;
						attributes.Add (attrWeight);
					}

					if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal) {
						Pango.AttrStyle attrStyle = new Pango.AttrStyle ((Pango.Style)chunkStyle.FontStyle);
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
			Pango.Rectangle inkrect, logicalrect;
			layout.GetExtents (out inkrect, out logicalrect);
			attributes.ForEach (attr => attr.Dispose ());
			attributeList.Dispose ();
			layout.Dispose ();
			return (logicalrect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale;
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
		
		public double GetLineHeight (DocumentLine line)
		{
			if (line == null)
				return LineHeight;
			foreach (var marker in line.Markers) {
				IExtendingTextLineMarker extendingTextMarker = marker as IExtendingTextLineMarker;
				if (extendingTextMarker == null)
					continue;
				return extendingTextMarker.GetLineHeight (textEditor);
			}
			int lineNumber = line.LineNumber; 
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
