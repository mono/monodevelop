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
		
		double lineHeight = 16;
		int highlightBracketOffset = -1;

		public double LineHeight {
			get { return lineHeight; }
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

		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get { return this.textEditor.ColorStyle; }
		}

		public Document Document {
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

			textEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs e) {
				if (mouseSelectionMode == MouseSelectionMode.Word && e.Offset < mouseWordStart) {
					int delta = -e.Count;
					if (!string.IsNullOrEmpty (e.Value))
						delta += e.Value.Length;
					mouseWordStart += delta;
					mouseWordEnd += delta;
				}
			};
			base.cursor = xtermCursor;
			textEditor.HighlightSearchPatternChanged += delegate {
				selectedRegions.Clear ();
				RefreshSearchMarker ();
			};
			//			textEditor.SelectionChanged += delegate { DisposeLayoutDict (); };
			textEditor.Document.TextReplaced += delegate(object sender, ReplaceEventArgs e) {
				if (selectedRegions.Count == 0)
					return;
				List<ISegment> newRegions = new List<ISegment> (this.selectedRegions);
				Document.UpdateSegments (newRegions, e);
				this.selectedRegions = newRegions;
				RefreshSearchMarker ();
			};
			textEditor.Document.LineChanged += TextEditorDocumentLineChanged;
			textEditor.GetTextEditorData ().SearchChanged += HandleSearchChanged;
			markerLayout = PangoUtil.CreateLayout (textEditor);

			textEditor.Document.EndUndo += UpdateBracketHighlighting;
			textEditor.SelectionChanged += UpdateBracketHighlighting;
			textEditor.Document.Undone += delegate {
				UpdateBracketHighlighting (this, EventArgs.Empty);
			};
			textEditor.Document.Redone += delegate {
				UpdateBracketHighlighting (this, EventArgs.Empty);
			};
			Caret.PositionChanged += UpdateBracketHighlighting;
			textEditor.VScroll += HandleVAdjustmentValueChanged;
		}

		void TextEditorDocumentLineChanged (object sender, LineEventArgs e)
		{
			RemoveCachedLine (e.Line);
		}

		List<LineSegment> linesToRemove = new List<LineSegment> ();
		void HandleVAdjustmentValueChanged (object sender, EventArgs e)
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
			
			textEditor.RequestResetCaretBlink ();
		}

		public void ClearSearchMaker ()
		{
			selectedRegions.Clear ();
		}

		public class SearchWorkerArguments {
			public int FirstLine { get; set; }
			public int LastLine { get; set; }
			public List<ISegment> OldRegions { get; set; }
			public ISearchEngine Engine { get; set; }
		}

		public void RefreshSearchMarker ()
		{
			if (textEditor.HighlightSearchPattern) {
				DisposeSearchPatternWorker ();

				SearchWorkerArguments args = new SearchWorkerArguments () {
					FirstLine = this.YToLine (textEditor.VAdjustment.Value),
					LastLine = this.YToLine (textEditor.Allocation.Height + textEditor.VAdjustment.Value),
					OldRegions = selectedRegions,
					Engine = this.textEditor.GetTextEditorData ().SearchEngine.Clone ()
				};

				if (string.IsNullOrEmpty (this.textEditor.SearchPattern)) {
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
			List<ISegment> newRegions = new List<ISegment> ();
			int offset = 0;
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
				newRegions.Add (result);
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

		void UpdateRegions (IEnumerable<ISegment> regions, SearchWorkerArguments args)
		{
			HashSet<int> updateLines = new HashSet<int> ();

			foreach (ISegment region in regions) {
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
		Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);

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
			if (Caret.Mode != CaretMode.Insert || (offset >= 0 && offset < Document.Length && !Document.IsBracket (Document.GetCharAt (offset))))
				offset++;
			offset = System.Math.Max (0, offset);
			if (highlightBracketOffset >= 0 && (offset >= Document.Length || !Document.IsBracket (Document.GetCharAt (offset)))) {
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
			if (matchingBracket == caretOffset && offset + 1 < Document.Length)
				matchingBracket = Document.GetMatchingBracketOffset (worker, offset + 1);
			if (matchingBracket == caretOffset)
				matchingBracket = -1;
			if (worker.CancellationPending)
				return;
			if (matchingBracket != oldIndex) {
				highlightBracketOffset = matchingBracket;
				int line1 = oldIndex >= 0 ? Document.OffsetToLineNumber (oldIndex) : -1;
				int line2 = highlightBracketOffset >= 0 ? Document.OffsetToLineNumber (highlightBracketOffset) : -1;
				//DocumentLocation matchingBracketLocation = Document.OffsetToLocation (matchingBracket);
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
			markerLayout.FontDescription.Weight = Pango.Weight.Bold;
			markerLayout.SetText (" ");
			int w, h;
			markerLayout.GetSize (out w, out h);
			this.charWidth = w / Pango.Scale.PangoScale;
			this.lineHeight = System.Math.Ceiling (h / Pango.Scale.PangoScale);
			
			markerLayout.FontDescription.Weight = Pango.Weight.Normal;

			Pango.Font font = textEditor.PangoContext.LoadFont (markerLayout.FontDescription);
			if (font != null) {
				Pango.FontMetrics metrics = font.GetMetrics (null);
				this.charWidth = metrics.ApproximateCharWidth / Pango.Scale.PangoScale;

				font.Dispose ();
			}

			CaretMoveActions.LineHeight = lineHeight = System.Math.Max (1, lineHeight);

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

			EnsureCaretGc ();

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

		void EnsureCaretGc ()
		{
			if (caretGc != null || textEditor.GdkWindow == null)
				return;
			caretGc = new Gdk.GC (textEditor.GdkWindow);
			caretGc.RgbFgColor = new Color (0x80, 0x80, 0x80);
			caretGc.Function = Gdk.Function.Xor;
		}

		void DisposeGCs ()
		{
			ShowTooltip (null, Gdk.Rectangle.Zero);
		}
		

		public override void Dispose ()
		{
			CancelCodeSegmentTooltip ();
			StopCaretThread ();
			DisposeHighightBackgroundWorker ();
			DisposeSearchPatternWorker ();

			textEditor.Document.EndUndo -= UpdateBracketHighlighting;
			Caret.PositionChanged -= UpdateBracketHighlighting;

			textEditor.GetTextEditorData ().SearchChanged -= HandleSearchChanged;

			arrowCursor.Dispose ();
			xtermCursor.Dispose ();

			DisposeGCs ();
			if (caretGc != null)
				caretGc.Dispose ();
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
		object caretLock = new object ();
		
		// constants taken from gtk.
		const int cursorOnMultiplier = 2;
		const int cursorOffMultiplier = 1;
		const int cursorDivider = 3;
		
		public void ResetCaretBlink ()
		{
			lock (caretLock) {
				StopCaretThread ();
				blinkTimeout = GLib.Timeout.Add ((uint)(Gtk.Settings.Default.CursorBlinkTime * cursorOnMultiplier / cursorDivider), UpdateCaret);
				caretBlink = true;
			}
		}

		internal void StopCaretThread ()
		{
			lock (caretLock) {
				if (blinkTimeout == 0)
					return;
				GLib.Source.Remove (blinkTimeout);
				blinkTimeout = 0;
				caretBlink = false;
			}
		}

		bool UpdateCaret ()
		{
			lock (caretLock) {
				caretBlink = !caretBlink;
				int multiplier = caretBlink ? cursorOnMultiplier : cursorOffMultiplier;
				if (Caret.IsVisible)
					DrawCaret (textEditor.GdkWindow);
				blinkTimeout = GLib.Timeout.Add ((uint)(Gtk.Settings.Default.CursorBlinkTime * multiplier / cursorDivider), UpdateCaret);
				return false;
			}
		}
		#endregion

		internal double caretX;
		internal double caretY;
		Gdk.GC caretGc;

		void SetVisibleCaretPosition (double x, double y)
		{
			caretX = x;
			caretY = y;
		}

		public static Gdk.Rectangle EmptyRectangle = new Gdk.Rectangle (0, 0, 0, 0);
		public void DrawCaret (Gdk.Drawable win)
		{
			if (!this.textEditor.IsInDrag && !(this.caretX >= 0 && (!this.textEditor.IsSomethingSelected || this.textEditor.SelectionRange.Length == 0))) 
				return;
			if (win == null || Settings.Default.CursorBlink && !Caret.IsVisible)
				return;

			switch (Caret.Mode) {
			case CaretMode.Insert:
				win.DrawLine (caretGc, (int)caretX, (int)caretY, (int)caretX, (int)(caretY + LineHeight - 1));
				break;
			case CaretMode.Block:
				win.DrawRectangle (caretGc, true, new Gdk.Rectangle ((int)caretX, (int)caretY, (int)this.charWidth, (int)LineHeight));
				break;
			case CaretMode.Underscore:
				double bottom = caretY + lineHeight;
				win.DrawLine (caretGc, (int)caretX, (int)bottom, (int)(caretX + this.charWidth), (int)bottom);
				break;
			}
		}

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
				isInvalid = MarkerLength != line.MarkerCount || line.StartSpan.Equals (Spans);
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
				return MarkerLength == other.MarkerLength && Offset == other.Offset && Length == other.Length && Spans == other.Spans  && SelectionStart == other.SelectionStart && SelectionEnd == other.SelectionEnd;
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
				if (descriptor.Equals (line, offset, length, selectionStart, selectionEnd, out isInvalid)) {
					return descriptor.Layout;
				}
				descriptor.Dispose ();
				layoutDict.Remove (line);
			}

			var wrapper = new LayoutWrapper (PangoUtil.CreateLayout (textEditor));
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
		}

		public void PurgeLayoutCache ()
		{
			DisposeLayoutDict ();
			if (chunkDict != null)
				chunkDict.Clear ();
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

		public LayoutWrapper CreateLinePartLayout (SyntaxMode mode, LineSegment line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return CreateLinePartLayout (mode, line, -1, offset, length, selectionStart, selectionEnd);
		}

		public LayoutWrapper CreateLinePartLayout (SyntaxMode mode, LineSegment line, int logicalRulerColumn, int offset, int length, int selectionStart, int selectionEnd)
		{
			return GetCachedLayout (line, offset, length, selectionStart, selectionEnd, delegate(LayoutWrapper wrapper) {
				if (logicalRulerColumn < 0)
					logicalRulerColumn = line.GetLogicalColumn(textEditor.GetTextEditorData(), textEditor.Options.RulerColumn);
				var atts = new FastPangoAttrList ();
				wrapper.Layout.Alignment = Pango.Alignment.Left;
				wrapper.Layout.FontDescription = textEditor.Options.Font;
				wrapper.Layout.Tabs = tabArray;
				StringBuilder textBuilder = new StringBuilder ();
				Chunk startChunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, offset, length);
				for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
					try {
						textBuilder.Append (chunk.GetText (Document));
					} catch {
						Console.WriteLine (chunk);
					}
				}
				var spanStack = line.StartSpan;
				int lineOffset = line.Offset;
				string lineText = textBuilder.ToString ();
				bool containsPreedit = !string.IsNullOrEmpty (textEditor.preeditString) && offset <= textEditor.preeditOffset && textEditor.preeditOffset <= offset + length;
				uint preeditLength = 0;

				if (containsPreedit) {
					lineText = lineText.Insert (textEditor.preeditOffset - offset, textEditor.preeditString);
					preeditLength = (uint)textEditor.preeditString.Length;
				}
				char[] lineChars = lineText.ToCharArray ();
				int startOffset = offset, endOffset = offset + length;
				uint curIndex = 0, byteIndex = 0;
				uint curChunkIndex = 0, byteChunkIndex = 0;
				
				uint oldEndIndex = 0;
				for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
					ChunkStyle chunkStyle = chunk != null ? chunk.GetChunkStyle (textEditor.ColorStyle) : null;
					spanStack = chunk.SpanStack ?? spanStack;
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

						HandleSelection(lineOffset, logicalRulerColumn, selectionStart, selectionEnd, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {
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
							var si = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
							var ei = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
							atts.AddForegroundAttribute (SelectionColor.Color, si, ei);
							if (!wrapper.StartSet)
								wrapper.SelectionStartIndex = (int)si;
							wrapper.SelectionEndIndex   = (int)ei;
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
					var ei = TranslateToUTF8Index (lineChars, (uint)(si + preeditLength), ref curIndex, ref byteIndex);
					atts.AddUnderlineAttribute (Pango.Underline.Single, si, ei);
				}
				wrapper.LineChars = lineChars;
				wrapper.Layout.SetText (lineText);
				wrapper.EolSpanStack = spanStack;
				atts.AssignTo (wrapper.Layout);
				atts.Dispose ();
				int w, h;
				wrapper.Layout.GetSize (out w, out h);
				wrapper.PangoWidth = w;
			});
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
			for (int i = 0; i < layout.LineChars.Length; i++) {
				if (layout.LineChars[i] == ' ') {
					bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
					if (first || oldSelected != selected) {
						ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
						first = false;
						oldSelected = selected;
					}
					Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
					int xpos = pos.X;
					DrawSpaceMarker (ctx, selected, xPos + xpos / Pango.Scale.PangoScale, y);
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
			for (int i = 0; i < layout.LineChars.Length; i++) {
				if (layout.LineChars[i] == '\t') {
					bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
					if (first || oldSelected != selected) {
						ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
						first = false;
						oldSelected = selected;
					}
					Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
					int xpos = pos.X;
					DrawTabMarker (ctx, selected, xPos + xpos / Pango.Scale.PangoScale, y);
				}
			}
		}

		void DecorateTabsAndSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			for (int i = 0; i < layout.LineChars.Length; i++) {
				char ch = layout.LineChars[i];
				if (ch != ' ' && ch != '\t')
					continue;
				bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
				if (first || oldSelected != selected) {
					ctx.Color = selected ? SelectionColor.CairoColor : ColorStyle.WhitespaceMarker;
					first = false;
					oldSelected = selected;
				}
				Pango.Rectangle pos = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)i, ref curIndex, ref byteIndex));
				int xpos = pos.X;
				if (ch == '\t') {
					DrawTabMarker (ctx, selected, xPos + xpos / Pango.Scale.PangoScale, y);
				} else {
					DrawSpaceMarker (ctx, selected, xPos + xpos / Pango.Scale.PangoScale, y);
				}
			}
		}

		void DecorateMatchingBracket (Cairo.Context ctx, LayoutWrapper layout, int offset, int length, double xPos, double y, int selectionStart, int selectionEnd)
		{
			uint curIndex = 0, byteIndex = 0;
			if (offset <= highlightBracketOffset && highlightBracketOffset <= offset + length) {
				int index = highlightBracketOffset - offset;
				Pango.Rectangle rect = layout.Layout.IndexToPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)index, ref curIndex, ref byteIndex));
				
				var bracketMatch = new Cairo.Rectangle (xPos + rect.X / Pango.Scale.PangoScale, y, (rect.Width / Pango.Scale.PangoScale) - 1, (rect.Height / Pango.Scale.PangoScale) - 1);
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
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			return CreateLinePartLayout (mode, line, line.Offset, line.EditableLength, -1, -1);
		}

		public void DrawCaretLineMarker (Cairo.Context cr, double xPos, double y, double width)
		{
			cr.Rectangle (xPos, y, width, LineHeight);
			var color = ColorStyle.LineMarker;
			cr.Color = new Cairo.Color (color.R, color.G, color.B, 0.5);
			cr.Fill ();
			cr.MoveTo (xPos, y + cr.LineWidth);
			cr.LineTo (xPos + width, y + cr.LineWidth);
			cr.MoveTo (xPos, y + lineHeight - cr.LineWidth);
			cr.LineTo (xPos + width, y + lineHeight - cr.LineWidth);
			cr.Color = color;
			cr.Stroke ();
		}

		void DrawLinePart (Cairo.Context cr, LineSegment line, int lineNumber, int logicalRulerColumn, int offset, int length, ref double pangoPosition, ref bool isSelectionDrawn, double y, double maxX)
		{
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			int selectionStart;
			int selectionEnd;
			if (BackgroundRenderer != null || this.HideSelection) {
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
				DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (xPos + startX, y, endX - startX + 0.5, textEditor.LineHeight), this.SelectionColor.CairoBackgroundColor, true);
			}

			// highlight search results
			ISegment firstSearch;
			int o = offset;
			uint curIndex = 0, byteIndex = 0;

			while ((firstSearch = GetFirstSearchResult (o, offset + length)) != null) {
				double x = pangoPosition;
				HandleSelection (lineOffset, logicalRulerColumn, selectionStart, selectionEnd, firstSearch.Offset, firstSearch.EndOffset, delegate(int start, int end) {
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

						cr.Color = MainSearchResult == null || MainSearchResult.Offset != firstSearch.Offset ? ColorStyle.SearchTextBg : ColorStyle.SearchTextMainBg;
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, x1, y, System.Math.Min (10, width) * textEditor.Options.Zoom, x2 - x1, textEditor.LineHeight);
						cr.Fill ();
					}
				}, null);

				o = System.Math.Max (firstSearch.EndOffset, o + 1);
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
					Pango.Rectangle strong_pos, weak_pos;
					int index = caretOffset- offset;
					if (offset <= textEditor.preeditOffset && textEditor.preeditOffset < offset + length) {
						index += textEditor.preeditString.Length;
					}

					if (Caret.Column > line.EditableLength + 1) {
						string virtualSpace = this.textEditor.GetTextEditorData ().GetVirtualSpaces (Caret.Line, Caret.Column);
						LayoutWrapper wrapper = new LayoutWrapper (PangoUtil.CreateLayout (textEditor));
						wrapper.LineChars = virtualSpace.ToCharArray ();
						wrapper.Layout.SetText (virtualSpace);
						wrapper.Layout.Tabs = tabArray;
						wrapper.Layout.FontDescription = textEditor.Options.Font;
						int vy, vx;
						wrapper.Layout.GetSize (out vx, out vy);
						SetVisibleCaretPosition (((pangoPosition + vx + layout.PangoWidth) / Pango.Scale.PangoScale), y);
						xPos = (pangoPosition + layout.PangoWidth) / Pango.Scale.PangoScale;

						if (!isSelectionDrawn && (selectionEnd == lineOffset + line.EditableLength)) {
							double startX;
							double endX;
							startX = xPos;
							endX = (pangoPosition + vx + layout.PangoWidth) / Pango.Scale.PangoScale;
							DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (startX, y, endX - startX, textEditor.LineHeight), this.SelectionColor.CairoBackgroundColor, true);
						}
						if ((HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber)
							DrawCaretLineMarker (cr, pangoPosition / Pango.Scale.PangoScale, y, vx / Pango.Scale.PangoScale);

						if (DecorateLineBg != null)
							DecorateLineBg (cr, wrapper, offset, length, xPos, y, selectionStart, selectionEnd + virtualSpace.Length);
						if (DecorateLineFg != null)
							DecorateLineFg (cr, wrapper, offset, length, xPos, y, selectionStart, selectionEnd + virtualSpace.Length);
						wrapper.Dispose ();
						pangoPosition += vx;
					} else if (index >= 0 && index < length) {
						curIndex = byteIndex = 0;
						layout.Layout.GetCursorPos ((int)TranslateToUTF8Index (layout.LineChars, (uint)index, ref curIndex, ref byteIndex), out strong_pos, out weak_pos);
						SetVisibleCaretPosition (xPos + (strong_pos.X / Pango.Scale.PangoScale), y);
					} else if (index == length) {
						SetVisibleCaretPosition ((pangoPosition + layout.PangoWidth) / Pango.Scale.PangoScale, y);
					}
				}
			}

			foreach (TextMarker marker in line.Markers) {
				marker.Draw (textEditor, cr, layout.Layout, false, /*selected*/offset, offset + length, y, xPos, xPos + width);
			}

			pangoPosition += layout.PangoWidth;
			if (layout.IsUncached)
				layout.Dispose ();
		}


		ISegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset && this.selectedRegions.Count > 0) {
				ISegment region = new Segment (startOffset, endOffset - startOffset);
				int min = 0;
				int max = selectedRegions.Count - 1;
				do {
					int mid = (min + max) / 2;
					ISegment segment = selectedRegions[mid];
					if (segment.Contains (startOffset) || segment.Contains (endOffset) || region.Contains (segment)) {
						if (mid == 0)
							return segment;
						ISegment prevSegment = selectedRegions[mid - 1];
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
			return null;
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
				if (Document.GetCharAt (line.Offset + line.EditableLength) == '\n') {
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

			if (args.Button == 1) {
				VisualLocationTranslator trans = new VisualLocationTranslator (this);
				clickLocation = trans.PointToLocation (args.X, args.Y);
				if (clickLocation.IsEmpty)
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
				if (line != null && clickLocation.Column >= line.EditableLength + 1 && GetWidth (Document.GetTextAt (line) + "-") < args.X) {
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
			if (args.Button == 2 && this.textEditor.CanEdit (docLocation.Line)) {
				ISegment selectionRange = null;
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
				Document.CommitLineToEndUpdate (docLocation.Line);
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
		ISegment previewSegment = null;
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
			while (curOffset >= 0 && curOffset < previewSegment.EndOffset && curOffset < Document.Length) {
				LineSegment line = Document.GetLineByOffset (curOffset);
				string lineText = Document.GetTextAt (curOffset, line.Offset + line.EditableLength - curOffset);
				textBuilder.Append (lineText);
				textBuilder.AppendLine ();
				curOffset = line.EndOffset + indentLength;
			}

			codeSegmentEditorWindow.Text = textBuilder.ToString ();

			HideCodeSegmentPreviewWindow ();
			codeSegmentEditorWindow.ShowAll ();

			codeSegmentEditorWindow.GrabFocus ();

		}

		uint codeSegmentTooltipTimeoutId = 0;
		void ShowTooltip (ISegment segment, Rectangle hintRectangle)
		{
			if (previewSegment == segment)
				return;
			CancelCodeSegmentTooltip ();
			HideCodeSegmentPreviewWindow ();
			previewSegment = segment;
			if (segment == null)
				return;
			codeSegmentTooltipTimeoutId = GLib.Timeout.Add (650, delegate {
				previewWindow = new CodeSegmentPreviewWindow (this.textEditor, false, segment);
				int ox = 0, oy = 0;
				this.textEditor.GdkWindow.GetOrigin (out ox, out oy);

				int x = hintRectangle.Right;
				int y = hintRectangle.Bottom;
				previewWindow.CalculateSize ();
				int w = previewWindow.SizeRequest ().Width;
				int h = previewWindow.SizeRequest ().Height;

				Gdk.Rectangle geometry = this.textEditor.Screen.GetMonitorGeometry (this.textEditor.Screen.GetMonitorAtPoint (ox + x, oy + y));

				if (x + ox + w > geometry.Right)
					x = hintRectangle.Left - w;
				if (y + oy + h > geometry.Bottom)
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
				DocumentLocation loc = PointToLocation (args.X, args.Y);
				int column = 0;
				for (; chunk != null; chunk = chunk.Next) {
					if (column <= loc.Column && loc.Column < column + chunk.Length) {
						ChunkStyle chunkStyle = chunk.GetChunkStyle (style);

						return chunkStyle != null ? chunkStyle.Link : null;
					}
					column += chunk.Length;
				}
			}
			return null;
		}

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
			if (textEditor.IsSomethingSelected && textEditor.MainSelection.SelectionMode == SelectionMode.Block) {
				Caret.AllowCaretBehindLineEnd = true;
			}

			DocumentLocation loc = PointToLocation (args.X, args.Y);
			if (loc.IsEmpty)
				return;
			LineSegment line = Document.GetLine (loc.Line);
			LineSegment oldHoveredLine = HoveredLine;
			HoveredLine = line;
			OnHoveredLineChanged (new LineEventArgs (oldHoveredLine));

			TextMarkerHoverResult hoverResult = new TextMarkerHoverResult ();
			oldMarkers.ForEach (m => m.MouseHover (this.textEditor, args, hoverResult));

			if (line != null) {
				newMarkers.Clear ();
				newMarkers.AddRange (line.Markers.Where (m => m is IActionTextMarker).Cast <IActionTextMarker> ());
				IActionTextMarker extraMarker = Document.GetExtendingTextMarker (loc.Line) as IActionTextMarker;
				if (extraMarker != null && !oldMarkers.Contains (extraMarker))
					newMarkers.Add (extraMarker);
				foreach (var marker in newMarkers.Where (m => !oldMarkers.Contains (m))) {
					marker.MouseHover (this.textEditor, args, hoverResult);
				}
				oldMarkers.Clear();
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
						ShowTooltip (shownFolding.Value, shownFolding.Key);
						return;
					}
				}

				ShowTooltip (null, Gdk.Rectangle.Zero);
				string link = GetLink (args);

				if (!String.IsNullOrEmpty (link)) {
					base.cursor = arrowCursor;
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
				Caret.Offset = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffset;
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
			if (BackgroundRenderer != null)
				return;
			bool isDefaultColor = color.R == defaultBgColor.R && color.G == defaultBgColor.G && color.B == defaultBgColor.B;
			if (isDefaultColor && !drawDefaultBackground)
				return;
			cr.Color = color;
			double xp = System.Math.Floor (area.X);
			
			if (textEditor.Options.ShowRuler) {
				double divider = System.Math.Max (xp, System.Math.Min (x + TextStartPosition + rulerX + 0.5, xp + area.Width));
				if (divider < area.X + area.Width) {
					cr.Rectangle (xp, area.Y, divider - xp, area.Height);
					cr.Fill ();
					cr.Rectangle (divider, area.Y, xp + area.Width - divider + 1, area.Height);
					cr.Color = DimColor (color);
					cr.Fill ();
					cr.DrawLine (ColorStyle.Ruler, divider, area.Y, divider, area.Y + area.Height);
					return;
				}
			}
			cr.Rectangle (xp, area.Y, System.Math.Ceiling (area.Width), area.Height);
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
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;

				if (folding.IsFolded) {
					markerLayout.SetText (Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.Length - offset))).Replace ("\t", new string (' ', textEditor.Options.TabSize)));
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

		List<ISegment> selectedRegions = new List<ISegment> ();
		public int SearchResultMatchCount {
			get {
				return selectedRegions.Count;
			}
		}
		public IEnumerable<ISegment> SearchResults {
			get {
				return selectedRegions;
			}
		}

		public ISegment MainSearchResult {
			get;
			set;
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
			var lineArea = new Cairo.Rectangle (XOffset - 1, y, textEditor.Allocation.Width - XOffset + 1, textEditor.LineHeight);
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
			int logicalRulerColumn = line.GetLogicalColumn(textEditor.GetTextEditorData(), textEditor.Options.RulerColumn);
			
			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;

				if (folding.IsFolded) {
					
					DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, foldOffset - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width);
					
					offset = folding.EndLine.Offset + folding.EndColumn;
					markerLayout.SetText (folding.Description);
					markerLayout.GetSize (out width, out height);
					
					bool isFoldingSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding);
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

					if (folding.EndLine != line) {
						line = folding.EndLine;
						lineNr = Document.OffsetToLineNumber (line.Offset);
						foldings = Document.GetStartFoldings (line);
						isEolFolded = line.EditableLength <= folding.EndColumn;
						goto restart;
					}
					isEolFolded = line.EditableLength <= folding.EndColumn;
				}
			}
			
			// Draw remaining line - must be called for empty line parts as well because the caret may be at this positon
			// and the caret position is calculated in DrawLinePart.
			if (line.EndOffset - offset >= 0)
				DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, line.Offset + line.EditableLength - offset, ref pangoPosition, ref isSelectionDrawn, y, area.X + area.Width);
			
			bool isEolSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionMode == SelectionMode.Normal && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength);
			lineArea = new Cairo.Rectangle (pangoPosition / Pango.Scale.PangoScale,
				lineArea.Y,
				textEditor.Allocation.Width - pangoPosition / Pango.Scale.PangoScale,
				lineArea.Height);

			if (textEditor.SelectionMode == SelectionMode.Block && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (line.Offset + line.EditableLength)) {
				DocumentLocation start = textEditor.MainSelection.Anchor;
				DocumentLocation end = textEditor.MainSelection.Lead;
				DocumentLocation visStart = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), start);
				DocumentLocation visEnd = Document.LogicalToVisualLocation (this.textEditor.GetTextEditorData (), end);
				
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


		void SetClip (Gdk.Rectangle rect)
		{
			EnsureCaretGc ();
			caretGc.ClipRectangle = rect;
		}

		protected internal override void MouseLeft ()
		{
			base.MouseLeft ();
			ShowTooltip (null, Gdk.Rectangle.Zero);
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
				index = line.EditableLength;
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
				SyntaxMode mode = margin.Document.SyntaxMode != null && margin.textEditor.Options.EnableSyntaxHighlighting ? margin.Document.SyntaxMode : SyntaxMode.Default;
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				bool done = false;
				Pango.Layout measueLayout = null;
				restart:
				int logicalRulerColumn = line.GetLogicalColumn(margin.textEditor.GetTextEditorData(), margin.textEditor.Options.RulerColumn);
				foreach (FoldSegment folding in foldings.Where(f => f.IsFolded))
				{
					int foldOffset = folding.StartLine.Offset + folding.Column;
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
					layoutWrapper = margin.CreateLinePartLayout(mode, line, logicalRulerColumn, offset, line.Offset + line.EditableLength - offset, -1, -1);
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
			if (line == null || line.EditableLength == 0 || column < 0)
				return 0;
			int logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData(), textEditor.Options.RulerColumn);
			int lineOffset = line.Offset;
			StringBuilder textBuilder = new StringBuilder ();
			SyntaxMode mode = Document.SyntaxMode != null && textEditor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : SyntaxMode.Default;
			Chunk startChunk = GetCachedChunks (mode, Document, textEditor.ColorStyle, line, lineOffset, line.EditableLength);
			for (Chunk chunk = startChunk; chunk != null; chunk = chunk != null ? chunk.Next : null) {
				try {
					textBuilder.Append (chunk.GetText (Document));
				} catch (Exception e) {
					Console.WriteLine (e);
					return 0;
				}
			}
			string lineText = textBuilder.ToString ();
			char[] lineChars = lineText.ToCharArray ();
			bool containsPreedit = lineOffset <= textEditor.preeditOffset && textEditor.preeditOffset <= lineOffset + line.EditableLength;
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

			int startOffset = lineOffset, endOffset = lineOffset + line.EditableLength;
			uint curIndex = 0, byteIndex = 0;
			uint curChunkIndex = 0, byteChunkIndex = 0;
			List<Pango.Attribute> attributes = new List<Pango.Attribute> ();
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

					HandleSelection (lineOffset, logicalRulerColumn, - 1, -1, chunk.Offset, chunk.EndOffset, delegate(int start, int end) {
						Pango.AttrForeground foreGround = new Pango.AttrForeground (chunkStyle.Color.Red, chunkStyle.Color.Green, chunkStyle.Color.Blue);
						foreGround.StartIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
						foreGround.EndIndex = TranslateToUTF8Index (lineChars, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
						attributes.Add (foreGround);
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
		
		// TODO: Reminder - put the line heights into the line segment tree - doing it this way is a performance bottlenek!
		public int YToLine (double yPos)
		{
			double delta = 0;
			foreach (LineSegment extendedTextMarkerLine in Document.LinesWithExtendingTextMarkers) {
				int lineNumber = Document.OffsetToLineNumber (extendedTextMarkerLine.Offset);
				double y = LineToY (lineNumber);
				if (y < yPos) {
					double curLineHeight = GetLineHeight (extendedTextMarkerLine);
					delta += curLineHeight - LineHeight;
					if (y <= yPos && yPos < y + curLineHeight)
						return lineNumber;
				}
			}
			return Document.VisualToLogicalLine (1 + (int)((yPos - delta) / LineHeight));
		}
		
		public double LineToY (int logicalLine)
		{
			double delta = 0;
			LineSegment logicalLineSegment = Document.GetLine (logicalLine);
			foreach (LineSegment extendedTextMarkerLine in Document.LinesWithExtendingTextMarkers) {
				if (extendedTextMarkerLine == null)
					continue;
				if (logicalLineSegment != null && extendedTextMarkerLine.Offset >= logicalLineSegment.Offset)
					continue;
				delta += GetLineHeight (extendedTextMarkerLine) - LineHeight;
			}
			
			int visualLine = Document.LogicalToVisualLine (logicalLine) - 1;
			return visualLine * LineHeight + delta;
		}
		
		public double GetLineHeight (LineSegment line)
		{
			if (line == null || line.MarkerCount == 0)
				return LineHeight;
			foreach (var marker in line.Markers) {
				IExtendingTextMarker extendingTextMarker = marker as IExtendingTextMarker;
				if (extendingTextMarker == null)
					continue;
				return extendingTextMarker.GetLineHeight (textEditor);
			}
			return LineHeight;
		}
		
		public double GetLineHeight (int logicalLineNumber)
		{
			return GetLineHeight (Document.GetLine (logicalLineNumber));
		}
		#endregion
	}
}
