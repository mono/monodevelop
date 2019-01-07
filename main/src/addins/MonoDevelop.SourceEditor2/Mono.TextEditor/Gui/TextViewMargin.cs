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

using MonoDevelop.Components.AtkCocoaHelper;

using Gdk;
using Gtk;
using System.Timers;
using System.Diagnostics;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Collections.Immutable;
using System.Threading;
using MonoDevelop.Ide;
using System.Threading.Tasks;

namespace Mono.TextEditor
{
	partial class TextViewMargin : Margin
	{
		readonly MonoTextEditor textEditor;

		Pango.Layout markerLayout, defaultLayout;
		Pango.FontDescription markerLayoutFont;
		Pango.Layout[] eolMarkerLayout;
		Pango.Rectangle[] eolMarkerLayoutRect;

		internal double charWidth;
		bool isMonospacedFont;

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
					RemoveCachedLine (Caret.Line);
					Document.CommitLineUpdate (Caret.Line);
				}
			}
		}

		public bool HideSelection {
			get;
			set;
		}

		CaretImpl Caret {
			get { return textEditor.Caret; }
		}

		internal MonoDevelop.Ide.Editor.Highlighting.EditorTheme EditorTheme {
			get { return this.textEditor.EditorTheme; }
		}

		public TextDocument Document {
			get { return textEditor.Document; }
		}

		public double CharWidth {
			get { return charWidth; }
		}

		class TextViewMarginAccessibilityProxy : IDisposable
		{
			public AccessibilityElementProxy Accessible { get; private set; }
			public TextViewMargin Margin { get; set; }

			public TextViewMarginAccessibilityProxy ()
			{
				Accessible = AccessibilityElementProxy.TextElementProxy ();
				Accessible.Contents = GetContents;
				Accessible.InsertionPointLineNumber = GetInsertionPointLineNumber;
				Accessible.NumberOfCharacters = GetNumberOfCharacters;
				Accessible.FrameForRange = GetFrameForRange;
				Accessible.LineForIndex = GetLineForIndex;
				Accessible.RangeForLine = GetRangeForLine;
				Accessible.StringForRange = GetStringForRange;
				Accessible.RangeForIndex = GetRangeForIndex;
				Accessible.StyleRangeForIndex = GetStyleRangeForIndex;
				Accessible.RangeForPosition = GetRangeForPosition;
				Accessible.GetVisibleCharacterRange = GetVisibleCharacterRange;
			}

			public void Dispose ()
			{
				Accessible.Contents = null;
				Accessible.InsertionPointLineNumber = null;
				Accessible.NumberOfCharacters = null;
				Accessible.FrameForRange = null;
				Accessible.LineForIndex = null;
				Accessible.RangeForLine = null;
				Accessible.StringForRange = null;
				Accessible.RangeForIndex = null;
				Accessible.StyleRangeForIndex = null;
				Accessible.RangeForPosition = null;
				Accessible.GetVisibleCharacterRange = null;
				Accessible = null;
				Margin = null;
			}

			int GetInsertionPointLineNumber ()
			{
				return Margin.Caret.Line;
			}

			int GetNumberOfCharacters ()
			{
				return Margin.Document.Length;
			}

			string GetContents ()
			{
				return Margin.Document.Text;
			}

			Rectangle GetFrameForRange (AtkCocoa.Range range)
			{
				//ISyntaxHighlighting mode = Margin.Document.SyntaxMode != null && Margin.textEditor.Options.EnableSyntaxHighlighting ? Margin.Document.SyntaxMode : new SyntaxHighlighting(Margin.Document);

				var startLine = Margin.Document.GetLineByOffset (range.Location);
				var finishLine = Margin.Document.GetLineByOffset (range.Location + range.Length);

				double xPos = Margin.XOffset;
				double rectangleWidth = 0, rectangleHeight = 0;

				var layout = Margin.CreateLinePartLayout (startLine, startLine.Offset, startLine.Length, -1, -1);
				xPos = layout.Layout.IndexToPos (range.Location - startLine.Offset).X;

				var pos = layout.Layout.IndexToPos ((range.Location - startLine.Offset) + range.Length);
				var lXPos = pos.X + pos.Width;
				rectangleWidth = (lXPos - xPos) / Pango.Scale.PangoScale;

				var y = Margin.textEditor.LineToY (startLine.LineNumber);
				var yEnd = Margin.textEditor.LineToY (finishLine.LineNumber + 1) + (finishLine.LineNumber == Margin.textEditor.LineCount ? Margin.textEditor.LineHeight : 0);
				if ((int)yEnd == 0) {
					yEnd = Margin.textEditor.VAdjustment.Upper;
				}
				rectangleHeight = yEnd - y;

				// FIXME: Need to take scroll offset into consideration
				return new Rectangle ((int)((xPos / Pango.Scale.PangoScale) + Margin.XOffset), (int)y, (int)rectangleWidth, (int)rectangleHeight);
			}

			int GetLineForIndex (int index)
			{
				if (Margin?.Document == null) {
					return 0;
				}

				var line = Margin.Document.GetLineByOffset (index);
				if (line == null) {
					return 0;
				}

				return line.LineNumber;
			}

			AtkCocoa.Range GetRangeForIndex (int index)
			{
				// Check if the glyph at offset really is just 1 char wide
				var c = Margin.Document.GetCharAt (index);
				int length = 1;

				return new AtkCocoa.Range { Location = index, Length = length };
			}

			AtkCocoa.Range GetRangeForLine (int lineNo)
			{
				var line = Margin.Document.GetLine (lineNo);

				int length = line.Length > 0 ? line.Length : line.LengthIncludingDelimiter;
				return new AtkCocoa.Range { Location = line.Offset, Length = length };
			}

			AtkCocoa.Range GetRangeForPosition (Gdk.Point position)
			{
				var location = Margin.PointToLocation (position.X, position.Y);

				// FIXME: Check if the glyph at offset really is just 1 char wide
				return new AtkCocoa.Range { Location = Margin.Document.LocationToOffset (location), Length = 1 };
			}

			string GetStringForRange (AtkCocoa.Range range)
			{
				string orig = Margin.Document.GetTextAt (range.Location, range.Length);

				return orig;
			}

			AtkCocoa.Range GetStyleRangeForIndex (int index)
			{
				// FIXME: this should be the range of text with the same style as index
				return GetRangeForIndex (index);
			}

			AtkCocoa.Range GetVisibleCharacterRange ()
			{
				return new AtkCocoa.Range { Location = 0, Length = Margin.Document.Length };
			}
		}

		TextViewMarginAccessibilityProxy accessible;
		public override AccessibilityElementProxy Accessible {
			get {
				if (accessible == null && AccessibilityElementProxy.Enabled) {
					accessible = new TextViewMarginAccessibilityProxy ();
				}
				return accessible == null ? null : accessible.Accessible;
			}
		}

		public TextViewMargin (MonoTextEditor textEditor)
		{
			if (textEditor == null)
				throw new ArgumentNullException ("textEditor");

			// Overwrite the default margin role
			if (Accessible != null) {
				Accessible.SetRole (AtkCocoa.Roles.AXTextArea);
			}

			if (accessible != null) {
				accessible.Margin = this;
			}

			this.textEditor = textEditor;
			textEditor.Document.TextChanged += HandleTextReplaced;
			textEditor.HighlightSearchPatternChanged += TextEditor_HighlightSearchPatternChanged;
			textEditor.GetTextEditorData ().SearchChanged += HandleSearchChanged;
			markerLayout = PangoUtil.CreateLayout (textEditor);
			defaultLayout = PangoUtil.CreateLayout (textEditor);

			textEditor.TextArea.FocusInEvent += HandleFocusInEvent;
			textEditor.TextArea.FocusOutEvent += HandleFocusOutEvent;
			textEditor.VScroll += HandleVAdjustmentValueChanged;

			textEditor.Document.SyntaxModeChanged += HandleSyntaxModeChanged;
		}

		void HandleSyntaxModeChanged(object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				PurgeLayoutCache ();
				textEditor.Document.CommitUpdateAll ();
			});
		}

		void TextEditor_HighlightSearchPatternChanged (object sender, EventArgs e)
		{
			selectedRegions.Clear ();
			RefreshSearchMarker ();
		}

		void HandleFocusInEvent (object o, FocusInEventArgs args)
		{
			selectionColor = SyntaxHighlightingService.GetColor (EditorTheme, EditorThemeColors.Selection);
			currentLineColor = SyntaxHighlightingService.GetColor (EditorTheme, EditorThemeColors.LineHighlight);
		}

		void HandleFocusOutEvent (object o, FocusOutEventArgs args)
		{
			selectionColor = SyntaxHighlightingService.GetColor (EditorTheme, EditorThemeColors.InactiveSelection);
			currentLineColor = SyntaxHighlightingService.GetColor (EditorTheme, EditorThemeColors.InactiveLineHighlight);
		}

		void HandleTextReplaced (object sender, TextChangeEventArgs e)
		{
			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				// It's required to remove the cached line here, otherwise the layout cache could chose to wrongly dipslay an older version of the line.
				RemoveCachedLine (Document.OffsetToLineNumber (change.NewOffset));
				if (mouseSelectionMode == MouseSelectionMode.Word && change.Offset < mouseWordStart) {
					int delta = change.ChangeDelta;
					mouseWordStart += delta;
					mouseWordEnd += delta;
				}
			}

			if (selectedRegions.Count > 0) {
				this.selectedRegions = new List<ISegment> (this.selectedRegions.AdjustSegments (e));
				RefreshSearchMarker ();
			}
		}

		void HandleVAdjustmentValueChanged (object sender, EventArgs e)
		{
			//We don't want to invalidate 5 lines before start
			int startLine = YToLine (textEditor.GetTextEditorData ().VAdjustment.Value) - 5;
			//We don't want to invalidate 5 lines after end(+10 because start is already -5)
			int endLine = (int)(startLine + textEditor.GetTextEditorData ().VAdjustment.PageSize / LineHeight) + 10;
			var linesToRemove = new List<int> ();
			foreach (var curLine in layoutDict.Keys) {
				if (startLine >= curLine || endLine <= curLine) {
					linesToRemove.Add (curLine);
				}
			}
			linesToRemove.ForEach (RemoveCachedLine);

			textEditor.RequestResetCaretBlink ();
		}

		public void ClearSearchMaker ()
		{
			selectedRegions.Clear ();
		}

		internal class SearchWorkerArguments
		{
			public int FirstLine { get; set; }

			public int LastLine { get; set; }

			public List<ISegment> OldRegions { get; set; }

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
			var newRegions = new List<ISegment> ();
			int offset = args.Engine.SearchRequest.SearchRegion.IsInvalid () ? 0 : args.Engine.SearchRequest.SearchRegion.Offset;
			do {
				if (worker.CancellationPending)
					return;
				SearchResult result = null;
				try {
					result = args.Engine.SearchForward (worker, args, offset);
				} catch (Exception ex) {
					LoggingService.LogError ("Got exception while search forward",  ex);
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
			Application.Invoke ((o2, a2) => {
				this.selectedRegions = newRegions;
				if (updateLines != null) {
					var document = textEditor.Document;
					if (document == null)
						return;
					foreach (int lineNumber in updateLines) {
//						RemoveCachedLine (Document.GetLine (lineNumber));
						document.RequestUpdate (new LineUpdate (lineNumber));
					}
					document.CommitDocumentUpdate ();
				} else {
					UpdateRegions (args.OldRegions.Concat (newRegions), args);
				}
				OnSearchRegionsUpdated (EventArgs.Empty);
			});
		}

		void UpdateRegions (IEnumerable<ISegment> regions, SearchWorkerArguments args)
		{
			HashSet<int> updateLines = new HashSet<int> ();

			foreach (var region in regions) {
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
		Lazy<Cursor> xtermCursor = new Lazy<Cursor> (() => new Cursor (CursorType.Xterm));
		Lazy<Cursor> xtermCursorInverted = new Lazy<Cursor> (() => {
#if MAC
			var img = OSXEditor.IBeamCursorImage;
			if (img != null) {
				return new Cursor(Display.Default, (InvertCursorPixbuf (img.ToPixbuf())), (int)img.Width / 2, (int)img.Height / 2);
			} else {
				return new Cursor (CursorType.Xterm);
			}
#else
			return new Cursor (CursorType.Xterm);
#endif
		});

		Lazy<Cursor> textLinkCursor = new Lazy<Cursor> (() => new Cursor (CursorType.Hand1));


		unsafe static Pixbuf InvertCursorPixbuf(Pixbuf src)
		{
			var dest = new Pixbuf(src.Colorspace, src.HasAlpha, src.BitsPerSample, src.Width, src.Height);

			var src_start = (byte*)src.Pixels;
			var dst_start = (byte*)dest.Pixels;

			for (int i = 0; i < src.Height; i++) {
				var sp = src_start + i * src.Rowstride;
				var dp = dst_start + i * dest.Rowstride;

				for (int j = 0; j < src.Width; j++) {
					var r = *(dp++) = (byte)(*(sp++) ^ 0xFF);
					var g = *(dp++) = (byte)(*(sp++) ^ 0xFF);
					var b = *(dp++) = (byte)(*(sp++) ^ 0xFF);

					if (src.HasAlpha) {
						if (r + g + b < 750) {
							*(dp++) = 0;
							sp++;
						} else {
							*(dp++) = *(sp++);
						}
					}
				}
			}

			return dest;
		}

		static readonly string[] markerTexts = {
			"<EOF>",
			"\\n",
			"\\r\\n",
			"\\r",
			"<NEL>",
			"<VT>",
			"<FF>",
			"<LS>",
			"<PS>"
		};

		static int GetEolMarkerIndex (UnicodeNewline ch)
		{
			switch (ch) {
			case UnicodeNewline.Unknown:
				return 0;
			case UnicodeNewline.LF:
				return 1;
			case UnicodeNewline.CRLF:
				return 2;
			case UnicodeNewline.CR:
				return 3;
			case UnicodeNewline.NEL:
				return 4;
			//case UnicodeNewline.VT:
			//	return 5;
			//case UnicodeNewline.FF:
			//	return 6;
			case UnicodeNewline.LS:
				return 5;
			case UnicodeNewline.PS:
				return 6;
			}
			return 0;
		}

		protected internal override void OptionsChanged ()
		{
			DisposeGCs ();
			selectionColor = null;
			currentLineColor = null;

			markerLayoutFont = textEditor.Options.Font.Copy ();
			markerLayoutFont.Size = markerLayoutFont.Size * 8 / 10;
			markerLayoutFont.Style = Pango.Style.Normal;
			markerLayoutFont.Weight = Pango.Weight.Normal;
			markerLayout.FontDescription = markerLayoutFont;

			// Gutter font may be bigger
			GetFontMetrics (textEditor.Options.GutterFont, out double gutterFontLineHeight, out double fontCharWidth, out underlinePosition, out underLineThickness);
			GetFontMetrics (textEditor.Options.Font, out double fontLineHeight, out fontCharWidth, out underlinePosition, out underLineThickness);
			this.textEditor.GetTextEditorData ().LineHeight = fontLineHeight;
			this.charWidth = fontCharWidth;

			var family = textEditor.PangoContext.Families.FirstOrDefault (f => f.Name == textEditor.Options.Font.Family);
			if (family != null) {
				isMonospacedFont = family.IsMonospace;
			} else {
				isMonospacedFont = false;
			}

			textEditor.LineHeight = System.Math.Max (1, LineHeight);

			if (eolMarkerLayout == null) {
				eolMarkerLayout = new Pango.Layout [markerTexts.Length];
				eolMarkerLayoutRect = new Pango.Rectangle [markerTexts.Length];
				for (int i = 0; i < eolMarkerLayout.Length; i++)
					eolMarkerLayout [i] = PangoUtil.CreateLayout (textEditor);
			}

			var font = textEditor.Options.Font.Copy ();
			font.Size = font.Size * 3 / 4;

			Pango.Rectangle logRect;
			for (int i = 0; i < eolMarkerLayout.Length; i++) {
				var layout = eolMarkerLayout [i];
				layout.FontDescription = font;

				layout.SetText (markerTexts [i]);

				Pango.Rectangle tRect;
				layout.GetPixelExtents (out logRect, out tRect);
				eolMarkerLayoutRect [i] = tRect;
			}

			textEditor.UpdatePreeditLineHeight ();

			DisposeLayoutDict ();
			caretX = caretY = -LineHeight;
			base.cursor = GetDefaultTextCursor ();
		}

		void DisposeGCs ()
		{
			ShowCodeSegmentPreviewTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
		}

		int underlinePosition, underLineThickness;
		public int UnderlinePosition => underlinePosition;
		public int UnderLineThickness => underLineThickness;

		void GetFontMetrics (Pango.FontDescription font, out double lineHeight, out double charWidth, out int underlinePosition, out int underLineThickness)
		{
			using (var metrics = textEditor.PangoContext.GetMetrics(font, textEditor.PangoContext.Language)) {
#if MAC
				try {
					lineHeight = System.Math.Ceiling (0.5 + OSXEditor.GetLineHeight(font.ToString ()));
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting the macOS font metrics for " + font, e);
					lineHeight = System.Math.Ceiling (0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
				}
#else
				lineHeight = System.Math.Ceiling(0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
#endif
				underlinePosition = metrics.UnderlinePosition;
				underLineThickness = metrics.UnderlineThickness;
				charWidth = metrics.ApproximateCharWidth / Pango.Scale.PangoScale;
			}
		}
		public override void Dispose ()
		{
			CancelCodeSegmentTooltip ();
			StopCaretThread ();
			DisposeSearchPatternWorker ();
			HideCodeSegmentPreviewWindow ();
			textEditor.Document.SyntaxModeChanged -= HandleSyntaxModeChanged;
			textEditor.VScroll -= HandleVAdjustmentValueChanged;
			textEditor.HighlightSearchPatternChanged -= TextEditor_HighlightSearchPatternChanged;

			textEditor.Document.TextChanged -= HandleTextReplaced;
			textEditor.TextArea.FocusInEvent -= HandleFocusInEvent;
			textEditor.TextArea.FocusOutEvent -= HandleFocusOutEvent;

			textEditor.GetTextEditorData ().SearchChanged -= HandleSearchChanged;

			DisposeGCs ();
			if (markerLayout != null)
				markerLayout.Dispose ();
			markerLayoutFont = null;

			if (defaultLayout!= null)
				defaultLayout.Dispose ();
			if (eolMarkerLayout != null) {
				foreach (var marker in eolMarkerLayout)
					marker.Dispose ();
				eolMarkerLayout = null;
			}
			DisposeLayoutDict ();
			tabArray?.Dispose ();
			accessible?.Dispose ();
			accessible = null;
			if (xtermCursor.IsValueCreated)
				xtermCursor.Value.Dispose ();
			if (xtermCursorInverted.IsValueCreated)
				xtermCursorInverted.Value.Dispose ();
			if (textLinkCursor.IsValueCreated)
				textLinkCursor.Value.Dispose ();
			base.Dispose ();
		}

		int tabLayoutTabSize = -1;
		Pango.TabArray tabArray;
		Pango.TabArray TabArray {
			get {
				if (tabArray == null || tabLayoutTabSize != textEditor.Options.TabSize) {
					CreateTabArray ();
					tabLayoutTabSize = textEditor.Options.TabSize;
				}
				return tabArray;
			}
		}

		void CreateTabArray ()
		{
			tabArray?.Dispose ();
			using (var tabWidthLayout = PangoUtil.CreateLayout (textEditor, (new string (' ', textEditor.Options.TabSize)))) {
				tabWidthLayout.Alignment = Pango.Alignment.Left;
				tabWidthLayout.FontDescription = textEditor.Options.Font;
				tabWidthLayout.GetSize (out int tabWidth, out int h);

				tabArray = new Pango.TabArray (1, false);
				tabArray.SetTab (0, Pango.TabAlign.Left, tabWidth);
			}
		}

		#region Caret blinking
		internal bool caretBlink = true;
		uint blinkTimeout = 0;

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
			if (blinkTimeout == 0)
				return;
			GLib.Source.Remove (blinkTimeout);
			blinkTimeout = 0;
			caretBlink = false;
		}

		bool UpdateCaret ()
		{
			if (!Settings.Default.CursorBlink) {
				caretBlink = true;
				return true;
			}
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

		internal double caretX, caretY, nonPreeditX, nonPreeditY;

		public Cairo.PointD CaretVisualLocation {
			get {
				return new Cairo.PointD (caretX, caretY);
			}
		}

		void SetVisibleCaretPosition (double x, double y, double nonPreeditX, double nonPreeditY)
		{
			if (x == caretX && y == caretY && this.nonPreeditX == nonPreeditX && this.nonPreeditY == nonPreeditY)
				return;
			caretX = x;
			caretY = y;
			this.nonPreeditX = nonPreeditX;
			this.nonPreeditY = nonPreeditY;

			GtkWorkarounds.SetImCursorLocation (
				textEditor.IMContext,
				textEditor.GdkWindow,
				new Rectangle ((int)nonPreeditX, (int)nonPreeditY, 0, (int)(LineHeight - 1)));
		}

		public static Gdk.Rectangle EmptyRectangle = new Gdk.Rectangle (0, 0, 0, 0);
		Gdk.Rectangle caretRectangle;
		double caretVAdjustmentValue;

		char GetCaretChar ()
		{
			var offset = Caret.Offset;
			char caretChar;
			if (offset >= 0 && offset < Document.Length) {
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
//					textEditor.TextArea.QueueDrawArea (caretRectangle.X - (int)textEditor.Options.Zoom,
//					               (int)(caretRectangle.Y + (-textEditor.VAdjustment.Value + caretVAdjustmentValue)),
//				                    caretRectangle.Width + (int)textEditor.Options.Zoom,
//					               caretRectangle.Height + 1);
					caretVAdjustmentValue = textEditor.VAdjustment.Value;
				}

				var fgColor = SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Foreground);
//				var bgColor = textEditor.ColorStyle.Default.CairoBackgroundColor;
				var line = Document.GetLine (Caret.Line);
				if (line != null) {
					foreach (var marker in Document.GetMarkers (line)) {
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
					cr.SetSourceColor (color);
					cr.Rectangle (caretRectangle.X + 0.5, caretRectangle.Y + 0.5, caretRectangle.Width, caretRectangle.Height);
					cr.Fill ();
					char caretChar = GetCaretChar ();
					if (!char.IsWhiteSpace (caretChar) && caretChar != '\0') {
						using (var layout = textEditor.LayoutCache.RequestLayout ()) {
							layout.FontDescription = textEditor.Options.Font;
							layout.SetText (caretChar.ToString ());
							cr.MoveTo (caretRectangle.X, caretRectangle.Y);
							cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background));
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
				var segment = textEditor.SelectionRange;
				selectionStart = segment.Offset;
				selectionEnd = segment.EndOffset;

				if (textEditor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block) {
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
			protected readonly TextDocument doc;
			protected LineDescriptor (TextDocument doc, DocumentLine line, int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
				this.doc = doc;
                this.MarkerLength = doc.GetMarkers (line).Count ();
			}

			public bool Equals (DocumentLine line, int offset, int length)
			{
				return offset == Offset && Length == length && MarkerLength == doc.GetMarkers (line).Count ();
			}
		}

		class LayoutDescriptor : LineDescriptor, IDisposable
		{
			readonly ITextSourceVersion version;

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

			public LayoutDescriptor (TextDocument doc, DocumentLine line, int offset, int length, LayoutWrapper layout, int selectionStart, int selectionEnd) : base(doc, line, offset, length)
			{
				this.version = doc.Version;
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

			public bool Equals (DocumentLine line, int offset, int length, int selectionStart, int selectionEnd)
			{
				int selStart = 0, selEnd = 0;
				if (selectionEnd >= 0) {
					selStart = selectionStart;
					selEnd = selectionEnd;
				}
				if (selStart != this.SelectionStart || selEnd != this.SelectionEnd || Length != length || MarkerLength != doc.GetMarkers (line).Count ())
					return false;
				return doc.Version.MoveOffsetTo (version, offset) == Offset;
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
				return MarkerLength == other.MarkerLength && Offset == other.Offset && Length == other.Length && SelectionStart == other.SelectionStart && SelectionEnd == other.SelectionEnd;
			}

			public override int GetHashCode ()
			{
				unchecked {
					return SelectionStart.GetHashCode () ^ SelectionEnd.GetHashCode ();
				}
			}
		}

		Dictionary<int, LayoutDescriptor> layoutDict = new Dictionary<int, LayoutDescriptor> ();

		internal LayoutWrapper CreateLinePartLayout (DocumentLine line, int logicalRulerColumn, int offset, int length, int selectionStart, int selectionEnd)
		{
			textEditor.CheckUIThread ();
			bool containsPreedit = textEditor.ContainsPreedit (offset, length);
			LayoutDescriptor descriptor;
			int lineNumber = line.LineNumber;
			if (!containsPreedit && layoutDict.TryGetValue (lineNumber, out descriptor)) {
				if (descriptor.Equals (line, offset, length, selectionStart, selectionEnd) && descriptor?.Layout?.Layout != null) {
					return descriptor.Layout;
				}
				descriptor.Dispose ();
				layoutDict.Remove (lineNumber);
			}

			OnLineShowing (line);

			var wrapper = new LayoutWrapper (this, textEditor.LayoutCache.RequestLayout ());
			wrapper.IsUncached = containsPreedit;
			if (logicalRulerColumn < 0)
				logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData (), textEditor.Options.RulerColumn);
			var atts = new FastPangoAttrList ();
			wrapper.Layout.Alignment = Pango.Alignment.Left;
			wrapper.Layout.FontDescription = textEditor.Options.Font;
			wrapper.Layout.Tabs = TabArray;
			if (textEditor.Options.WrapLines) {
				wrapper.Layout.Wrap = Pango.WrapMode.WordChar;
				wrapper.Layout.Width = (int)((textEditor.Allocation.Width - XOffset - TextStartPosition) * Pango.Scale.PangoScale);
			}
			StringBuilder textBuilder = StringBuilderCache.Allocate ();
			var cachedChunks = GetCachedChunks (Document, line, offset, length);
			var lineOffset = line.Offset;
			var chunks = new List<ColoredSegment> (cachedChunks.Item1.Select (c => new ColoredSegment (c.Offset + lineOffset, c.Length, c.ScopeStack)));;
			var markers = TextDocument.OrderTextSegmentMarkersByInsertion (Document.GetVisibleTextSegmentMarkersAt (line)).ToList ();
			foreach (var marker in markers) {
				var chunkMarker = marker as IChunkMarker;
				if (chunkMarker == null)
					continue;
				chunkMarker.TransformChunks (chunks);
			}
			wrapper.HighlightedLine = cachedChunks.Item3;
			wrapper.Chunks = chunks;
			foreach (var chunk in chunks) {
				try {
					textBuilder.Append (Document.GetTextAt (chunk));
				} catch {
					Console.WriteLine (chunk);
				}
			}
			string lineText = StringBuilderCache.ReturnAndFree (textBuilder);
			uint preeditLength = 0;

			if (containsPreedit) {
				if (textEditor.GetTextEditorData ().IsCaretInVirtualLocation) {
					lineText = textEditor.GetTextEditorData ().GetIndentationString (textEditor.Caret.Location) + textEditor.preeditString;
					wrapper.IsVirtualLineText = true;
				} else {
					lineText = lineText.Insert (textEditor.preeditOffset - offset, textEditor.preeditString);
				}
				preeditLength = (uint)textEditor.preeditString.Length;
			}
			//int startOffset = offset, endOffset = offset + length;
			uint curIndex = 0, byteIndex = 0;
			uint curChunkIndex = 0, byteChunkIndex = 0;

			uint oldEndIndex = 0;
			bool disableHighlighting = false;
			var sw = new Stopwatch ();
			sw.Start ();
			try {
				restart:
				foreach (var chunk in chunks) {
					if (!disableHighlighting && sw.ElapsedMilliseconds > 50) {
						chunks.Clear ();
						chunks.Add (new MonoDevelop.Ide.Editor.Highlighting.ColoredSegment (line.Offset, line.Length, new ScopeStack ("")));
						disableHighlighting = true;
						atts.Dispose ();
						atts = new FastPangoAttrList ();
						curIndex = byteIndex = 0;
						curChunkIndex = byteChunkIndex = 0;
						oldEndIndex = 0;
						goto restart;
					}
					var theme = textEditor.GetTextEditorData ().EditorTheme;
					var chunkStyle = theme.GetChunkStyle (chunk.ScopeStack);
					foreach (TextLineMarker marker in textEditor.Document.GetMarkers (line))
						chunkStyle = marker.GetStyle (chunkStyle);

					if (chunkStyle != null) {
						//startOffset = chunk.Offset;
						//endOffset = chunk.EndOffset;

						uint startIndex = (uint)(oldEndIndex);
						uint endIndex = (uint)(startIndex + chunk.Length);
						oldEndIndex = endIndex;
						HandleSelection (lineOffset, logicalRulerColumn, selectionStart, selectionEnd, chunk.Offset, chunk.EndOffset, delegate (int start, int end) {
							if (containsPreedit) {
								if (textEditor.preeditOffset < start)
									start += (int)preeditLength;
								if (textEditor.preeditOffset < end)
									end += (int)preeditLength;
							}
							var si = TranslateToUTF8Index (lineText, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
							var ei = TranslateToUTF8Index (lineText, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
							var color = (Cairo.Color)EditorTheme.GetForeground (chunkStyle);
							foreach (var marker in markers) {
								var chunkMarker = marker as IChunkMarker;
								if (chunkMarker == null)
									continue;
								chunkMarker.ChangeForeColor (textEditor, chunk, ref color);
							}
							color = StripAlphaValue (color);
							atts.AddForegroundAttribute ((HslColor)color, si, ei);

							if (!chunkStyle.TransparentBackground && GetPixel (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background)) != GetPixel (chunkStyle.Background)) {
								wrapper.AddBackground (chunkStyle.Background, (int)si, (int)ei);
							} /*else if (chunk.SpanStack != null && ColorStyle != null) {
								foreach (var span in chunk.SpanStack) {
									if (span == null || string.IsNullOrEmpty (span.Color))
										continue;
									var spanStyle = ColorStyle.GetChunkStyle (span.Color);
									if (spanStyle != null && !spanStyle.TransparentBackground && GetPixel (ColorStyle.PlainText.Background) != GetPixel (spanStyle.Background)) {
										wrapper.AddBackground (spanStyle.Background, (int)si, (int)ei);
										break;
									}
								}
							}*/
						}, delegate (int start, int end) {
							if (containsPreedit) {
								if (textEditor.preeditOffset < start)
									start += (int)preeditLength;
								if (textEditor.preeditOffset < end)
									end += (int)preeditLength;
							}
							var si = TranslateToUTF8Index (lineText, (uint)(startIndex + start - chunk.Offset), ref curIndex, ref byteIndex);
							var ei = TranslateToUTF8Index (lineText, (uint)(startIndex + end - chunk.Offset), ref curIndex, ref byteIndex);
							var color = (Cairo.Color)EditorTheme.GetForeground (chunkStyle);
							foreach (var marker in markers) {
								var chunkMarker = marker as IChunkMarker;
								if (chunkMarker == null)
									continue;
								chunkMarker.ChangeForeColor (textEditor, chunk, ref color);
							}
							color = StripAlphaValue (color);
							atts.AddForegroundAttribute ((HslColor)color, si, ei);
							if (!wrapper.StartSet)
								wrapper.SelectionStartIndex = (int)si;
							wrapper.SelectionEndIndex = (int)ei;
						});

						var translatedStartIndex = TranslateToUTF8Index (lineText, (uint)startIndex, ref curChunkIndex, ref byteChunkIndex);
						var translatedEndIndex = TranslateToUTF8Index (lineText, (uint)endIndex, ref curChunkIndex, ref byteChunkIndex);

						if (chunkStyle.FontWeight != Xwt.Drawing.FontWeight.Normal)
							atts.AddWeightAttribute ((Pango.Weight)chunkStyle.FontWeight, translatedStartIndex, translatedEndIndex);

						if (chunkStyle.FontStyle != Xwt.Drawing.FontStyle.Normal)
							atts.AddStyleAttribute ((Pango.Style)chunkStyle.FontStyle, translatedStartIndex, translatedEndIndex);

						if (chunkStyle.Underline)
							atts.AddUnderlineAttribute (Pango.Underline.Single, translatedStartIndex, translatedEndIndex);
					}
				}
				if (containsPreedit) {
					var byteLength = Encoding.UTF8.GetByteCount (textEditor.preeditString);
					var si = TranslateToUTF8Index (lineText, (uint)(textEditor.preeditOffset - offset), ref curIndex, ref byteIndex);
					var ei = TranslateToUTF8Index (lineText, (uint)(textEditor.preeditOffset - offset + byteLength), ref curIndex, ref byteIndex);

					if (textEditor.GetTextEditorData ().IsCaretInVirtualLocation) {
						uint len = (uint)textEditor.GetTextEditorData ().GetIndentationString (textEditor.Caret.Location).Length;
						si += len;
						ei += len;
					}

					atts.AddForegroundAttribute (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Foreground), si, ei);
					var hasBackground = wrapper.BackgroundColors.Any (bg => bg.FromIdx <= si && bg.ToIdx >= ei);
					if (hasBackground)
						atts.AddBackgroundAttribute (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background), si, ei);
					atts.InsertOffsetList (textEditor.preeditAttrs, si, ei);
				}
				wrapper.Text = lineText;
				wrapper.IndentSize = 0;
				var tabSize = textEditor.Options != null ? textEditor.Options.TabSize : 4;
				int i = 0, lineWidth;
				for (; i < lineText.Length; i++) {
					char ch = lineText [i];
					if (ch == ' ') {
						wrapper.IndentSize++;
					} else if (ch == '\t') {
						wrapper.IndentSize = GetNextTabstop (textEditor.GetTextEditorData (), wrapper.IndentSize + 1, tabSize) - 1;
					} else {
						break;
					}
				}
				lineWidth = wrapper.IndentSize;
				bool isFastPathPossible = isMonospacedFont;
				if (isFastPathPossible) {
					for (; i < lineText.Length; i++) {
						char ch = lineText [i];
						if (ch == '\t') {
							lineWidth = GetNextTabstop (textEditor.GetTextEditorData (), lineWidth + 1, tabSize) - 1;
						} else {
							if (ch > 255) {
								// for non ASCII chars always fall back to pango.
								isFastPathPossible = false;
								break;
							}
							lineWidth++;
						}
					}
					isFastPathPossible &= ((int)wrapper.Width) == (int)(lineWidth * charWidth);
				}

				var nextLine = line.NextLine;
				atts.AssignTo (wrapper.Layout);
				atts.Dispose ();
				int w, h;
				wrapper.GetSize (out w, out h);
				wrapper.Width = System.Math.Floor (w / Pango.Scale.PangoScale);
				wrapper.Height = System.Math.Floor (h / Pango.Scale.PangoScale);
    			wrapper.FastPath = isFastPathPossible;
				var lines = wrapper.Layout.LineCount;

				if (lines == 1) {
					wrapper.LastLineWidth = wrapper.Width;
				} else {
					var layoutLine = wrapper.Layout.GetLine (lines - 1);
					Pango.Rectangle iR = Pango.Rectangle.Zero;
					Pango.Rectangle lR = Pango.Rectangle.Zero;
					layoutLine.GetExtents (ref iR, ref lR);
					wrapper.LastLineWidth = System.Math.Floor (lR.Width / Pango.Scale.PangoScale);
				}


				selectionStart = System.Math.Max (line.Offset - 1, selectionStart);
				selectionEnd = System.Math.Min (line.EndOffsetIncludingDelimiter + 1, selectionEnd);
				descriptor = new LayoutDescriptor (textEditor.Document, line, offset, length, wrapper, selectionStart, selectionEnd);
				if (!containsPreedit && cachedChunks.Item2) {
					layoutDict [lineNumber] = descriptor;
				}
				//			textEditor.GetTextEditorData ().HeightTree.SetLineHeight (line.LineNumber, System.Math.Max (LineHeight, wrapper.Height));
				return wrapper;
			} finally {
				sw.Stop ();
			}
		}

		/// <summary>
		/// Strips the alpha value. Gtk doesn't support alpha colors.
		/// </summary>
		Cairo.Color StripAlphaValue (Cairo.Color color)
		{
			if (color.A < 1.0) {
				var bgc = (Cairo.Color)SyntaxHighlightingService.GetColor (EditorTheme, EditorThemeColors.Background);
				return new Cairo.Color (
					color.R * color.A + bgc.R * (1.0 - color.A),
					color.G * color.A + bgc.G * (1.0 - color.A),
					color.B * color.A + bgc.B * (1.0 - color.A)
				);
			}
			return color;
		}

		internal void GetSize (out int w, out int h)
			{
				throw new NotImplementedException ();
			}

		void OnLineShowing (DocumentLine line)
		{
			LineShowing?.Invoke (this, new LineEventArgs (line));
		}

		public event EventHandler<LineEventArgs> LineShowing;

		public IEnumerable<DocumentLine> CachedLine {
			get {
				return layoutDict.Keys.Select (ln => this.textEditor.GetLine (ln));
			}
		}

		public void RemoveCachedLine (int lineNumber)
		{
			if (lineNumber <= 0)
				return;
			textEditor.CheckUIThread ();
			LayoutDescriptor descriptor;
			if (layoutDict.TryGetValue (lineNumber, out descriptor)) {
				descriptor.Dispose ();
				layoutDict.Remove (lineNumber);
			}
		}

		internal void DisposeLayoutDict ()
		{
			textEditor.CheckUIThread ();
			foreach (LayoutDescriptor descr in layoutDict.Values) {
				descr.Dispose ();
			}
			layoutDict.Clear ();
			cacheSrc.Cancel ();
			cacheSrc = new CancellationTokenSource ();
		}
		public void PurgeLayoutCache ()
		{
			DisposeLayoutDict ();
		}

		void PurgeLayoutCacheAfter (int lineNumber)
		{
			foreach (var descr in layoutDict.ToArray()) {
				if (descr.Key >= lineNumber) {
					descr.Value.Dispose ();
					layoutDict.Remove (descr.Key);
				}
			}
		}

		class ChunkDescriptor : LineDescriptor
		{
			public List<MonoDevelop.Ide.Editor.Highlighting.ColoredSegment> Chunk {
				get;
				private set;
			}

			public ChunkDescriptor (TextDocument doc, DocumentLine line, int offset, int length, List<MonoDevelop.Ide.Editor.Highlighting.ColoredSegment> chunk) : base(doc, line, offset, length)
			{
				this.Chunk = chunk;
			}
		}
		CancellationTokenSource cacheSrc = new CancellationTokenSource ();
		Tuple<List<ColoredSegment>, bool, HighlightedLine> GetCachedChunks (TextDocument doc, DocumentLine line, int offset, int length)
		{
			var lineNumber = line.LineNumber;
			var token = cacheSrc.Token;
			var task = doc.SyntaxMode.GetHighlightedLineAsync (line, token);
			switch (task.Status) {
			case TaskStatus.Faulted:
				LoggingService.LogError ("Error while highlighting line " + lineNumber, task.Exception);
				break;
			case TaskStatus.RanToCompletion:
				if (task.Result != null) {
					return Tuple.Create (TrimChunks (task.Result.Segments, offset - line.Offset, length), true, task.Result);
				}
				break;
			}
			try {
				var taskResult = task.WaitAndGetResult (default (CancellationToken));
				return Tuple.Create (TrimChunks (taskResult.Segments, offset - line.Offset, length), true, taskResult);
			} catch (AggregateException e) {
				e.Flatten().Handle (x => x is OperationCanceledException);
			} catch (OperationCanceledException) {
			} catch (Exception e) {
				LoggingService.LogError ("Error while highlighting", e);
			}
			return Tuple.Create (new List<ColoredSegment> (new [] { new ColoredSegment (0, line.Length, ScopeStack.Empty) }), false, (HighlightedLine)null);
		}


		internal static List<ColoredSegment> TrimChunks (IReadOnlyList<ColoredSegment> segments, int offset, int length)
		{
			var result = new List<ColoredSegment> ();
			int i = 0;
			while (i < segments.Count && segments [i].EndOffset <= offset)
				i++;
			var endOffset = offset + length;

			if (i < segments.Count && segments [i].Offset < offset) {
				if (segments [i].EndOffset <= endOffset) {
					result.Add (segments [i].WithOffsetAndLength (offset, segments [i].EndOffset - offset));
				} else {
					result.Add (segments [i].WithOffsetAndLength (offset, endOffset - offset));
					return result;
				}
				i++;
			}

			while (i < segments.Count && segments [i].EndOffset <= endOffset) {
				result.Add (segments [i]);
				i++;
			}

			if (i < segments.Count && segments [i].Offset < endOffset) {
				result.Add (segments [i].WithOffsetAndLength (segments [i].Offset, endOffset - segments [i].Offset));
				i++;
			}
			return result;
		}

		public void ForceInvalidateLine (int lineNr)
		{
			LayoutDescriptor descriptor;
			if (lineNr > 0 && layoutDict.TryGetValue (lineNr, out descriptor)) {
				descriptor.Dispose ();
				layoutDict.Remove (lineNr);
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
			if (textEditor.GetTextEditorData ().ShowRuler && selectionStartColumn < logicalRulerColumn && logicalRulerColumn < selectionEndColumn && startOffset < rulerOffset && rulerOffset < endOffset) {
				InternalHandleSelection (selectionStart, selectionEnd, startOffset, rulerOffset, handleNotSelected, handleSelected);
				InternalHandleSelection (selectionStart, selectionEnd, rulerOffset, endOffset, handleNotSelected, handleSelected);
			} else {
				InternalHandleSelection (selectionStart, selectionEnd, startOffset, endOffset, handleNotSelected, handleSelected);
			}
		}

		public static uint TranslateToUTF8Index (string text, uint textIndex, ref uint curIndex, ref uint byteIndex)
		{
			if (text == null)
				throw new ArgumentNullException (nameof (text));

			if (textIndex < 0)
				throw new ArgumentOutOfRangeException (nameof (textIndex));

			if (textIndex < curIndex) {
				if (textIndex > text.Length)
					throw new ArgumentOutOfRangeException (nameof (textIndex));

				unsafe {
					fixed (char *p = text)
						byteIndex = (uint)Encoding.UTF8.GetByteCount (p, (int)textIndex);
				}
			} else {
				int count = System.Math.Min ((int)(textIndex - curIndex), text.Length - (int)curIndex);

				if (curIndex < 0)
					throw new ArgumentOutOfRangeException (nameof (curIndex));

				if (count - curIndex > text.Length)
					throw new ArgumentOutOfRangeException (nameof (curIndex));

				if (count > 0) {
					unsafe {
						fixed (char* p = text)
							byteIndex += (uint)Encoding.UTF8.GetByteCount (p + curIndex, count);
					}
				}
			}
			curIndex = textIndex;
			return byteIndex;
		}

		public static int TranslateIndexToUTF8 (string text, int index)
		{
			byte[] bytes = Encoding.UTF8.GetBytes (text);
			return Encoding.UTF8.GetCharCount (bytes, 0, index);
		}

		internal class LayoutWrapper : IDisposable
		{
			readonly TextViewMargin parent;

			public int IndentSize {
				get;
				set;
			}

			public LayoutCache.LayoutProxy Layout {
				get;
				set;
			}

			public bool IsUncached {
				get;
				set;
			}

			public bool StartSet {
				get;
				set;
			}

			internal List<MonoDevelop.Ide.Editor.Highlighting.ColoredSegment> Chunks {
				get;
				set;
			}

			internal HighlightedLine HighlightedLine { get; set; }

			public string Text {
				get {
					return Layout.Text;
				}
				set {
					Layout.SetText (value);
				}
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

			public double Width {
				get;
				set;
			}

			public double Height {
				get;
				set;
			}

			public double LastLineWidth {
				get;
				set;
			}

			public LayoutWrapper (TextViewMargin parent, LayoutCache.LayoutProxy layout)
			{
				this.parent = parent;
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

			internal class BackgroundColor
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

			public uint TranslateToUTF8Index (uint textIndex, ref uint curIndex, ref uint byteIndex)
			{
				return TextViewMargin.TranslateToUTF8Index (Text, textIndex, ref curIndex, ref byteIndex);
			}

			public bool FastPath { get; internal set; }
			public bool IsVirtualLineText { get; internal set; }

			public Pango.Rectangle IndexToPos (int index)
			{
				if (Layout == null)
					return Pango.Rectangle.Zero;
				return Layout.IndexToPos (index);
			}

			public void GetSize (out int width, out int height)
			{
				if (FastPath) {
					width = (int)(Pango.Scale.PangoScale * Text.Length * parent.charWidth);
					height = (int)(Pango.Scale.PangoScale * parent.LineHeight);
					return;
				}
				Layout.GetSize (out width, out height);
			}

			public void GetPixelSize (out int width, out int height)
			{
				if (FastPath) {
					width = (int)(Text.Length * parent.charWidth);
					height = (int)(parent.LineHeight);
					return;
				}
				Layout.GetPixelSize (out width, out height);
			}

			public void IndexToLineX (int index, bool trailing, out int line, out int xPos)
			{
				Layout.IndexToLineX (index, trailing, out line, out xPos);
			}

			public bool XyToIndex (int x, int y, out int index, out int trailing)
			{
				return Layout.XyToIndex (x, y, out index, out trailing);
			}

			public void GetCursorPos (int index, out Pango.Rectangle strong_pos, out Pango.Rectangle weak_pos)
			{
				Layout.GetCursorPos (index, out strong_pos, out weak_pos);
			}
		}

		HslColor? selectionColor;
		HslColor SelectionColor {
			get {
				if (selectionColor == null)
					selectionColor = MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (EditorTheme, textEditor.HasFocus ? EditorThemeColors.Selection : EditorThemeColors.InactiveSelection);
				return selectionColor.Value;
			}
		}

		HslColor? currentLineColor;
		HslColor CurrentLineColor {
			get {
				if (currentLineColor == null)
					currentLineColor = MonoDevelop.Ide.Editor.Highlighting.SyntaxHighlightingService.GetColor (EditorTheme, textEditor.HasFocus ? EditorThemeColors.LineHighlight : EditorThemeColors.InactiveLineHighlight);
				return currentLineColor.Value;
			}
		}

		internal LayoutWrapper CreateLinePartLayout (DocumentLine line, int offset, int length, int selectionStart, int selectionEnd)
		{
			return CreateLinePartLayout (line, -1, offset, length, selectionStart, selectionEnd);
		}

		#endregion

		public delegate void LineDecorator (Cairo.Context ctx,LayoutWrapper layout,int offset,int length,double xPos,double y,int selectionStart,int selectionEnd);

		public event LineDecorator DecorateLineBg;

		const double whitespaceMarkerAlpha = 0.12;

		void InnerDecorateTabsAndSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, double x, double y, int selectionStart, int selectionEnd, char spaceOrTab)
		{
			var text = layout.Text;
			if (text.IndexOf (spaceOrTab) == -1)
				return;
			var chunks = layout.Chunks;

			uint curIndex = 0, byteIndex = 0;
			bool first = true, oldSelected = false;
			var curchunk = 0;

			var dotThickness = textEditor.Options.Zoom * 2;
			var textEditorWidth = textEditor.Allocation.Width;

			//Get 1st visible character index from left based on HAdjustment
			int index, trailing;
			layout.XyToIndex ((int)textEditor.HAdjustment.Value, 0, out index, out trailing);

			double ypos;
			if (spaceOrTab == ' ') {
				ypos = System.Math.Floor (y + (LineHeight - dotThickness) / 2);
			} else {
				ypos = 0.5 + System.Math.Floor (y + LineHeight / 2);
			}

			var showOnlySelected = textEditor.Options.ShowWhitespaces != ShowWhitespaces.Always;
			var foregroundColor = SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Foreground);

			int lastIndex = -1;
			int lastPosX = 0;

			for (int i = index; i < text.Length; i++) {
				if (spaceOrTab != text [i])
					continue;

				bool selected = selectionStart <= offset + i && offset + i < selectionEnd;
				if (first || oldSelected != selected) {
					first = false;
					oldSelected = selected;
				}

				if (showOnlySelected && !selected)
					continue;
				int line, posX;
				if (lastIndex == i) {
					posX = lastPosX;
				} else {
					layout.IndexToLineX ((int)TranslateToUTF8Index (text, (uint)i, ref curIndex, ref byteIndex), false, out line, out posX);
				}
				double xpos = x + posX / Pango.Scale.PangoScale;
				if (xpos > textEditorWidth)
					break;
				layout.IndexToLineX ((int)TranslateToUTF8Index (text, (uint)i + 1, ref curIndex, ref byteIndex), false, out line, out posX);
				lastPosX = posX;
				lastIndex = i + 1;
				double xpos2 = x + posX / Pango.Scale.PangoScale;
				var col = (Cairo.Color)foregroundColor;
				if (chunks != null) {
					while (curchunk + 1 < chunks.Count) {
						if (offset + i < chunks [curchunk].EndOffset)
							break;
						curchunk++;
					}
					if (curchunk < chunks.Count) {
						var chunkStyle = EditorTheme.GetChunkStyle (chunks [curchunk].ScopeStack);
						col = chunkStyle.Foreground;
					}
				}
				ctx.SetSourceRGBA (col.R, col.G, col.B, whitespaceMarkerAlpha);

				if (spaceOrTab == ' ') {
					ctx.Rectangle (xpos + (xpos2 - xpos - dotThickness) / 2, ypos, dotThickness, dotThickness);
					ctx.Fill ();
				} else {
					ctx.MoveTo (0.5 + xpos, ypos);
					ctx.LineTo (0.5 + xpos2 - charWidth / 2, ypos);
					ctx.Stroke ();
				}
			}
		}

		void DecorateTabsAndSpaces (Cairo.Context ctx, LayoutWrapper layout, int offset, double x, double y, int selectionStart, int selectionEnd)
		{
			if (layout.IsVirtualLineText)
				return;
			if (textEditor.Options.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Space)) {
				InnerDecorateTabsAndSpaces (ctx, layout, offset, x, y, selectionStart, selectionEnd, ' ');
			}
			if (textEditor.Options.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Tab)) {
				InnerDecorateTabsAndSpaces (ctx, layout, offset, x, y, selectionStart, selectionEnd, '\t');
			}
		}

		public LayoutWrapper GetLayout (DocumentLine line)
		{
			return CreateLinePartLayout (line, line.Offset, line.Length, -1, -1);
		}

		public void DrawCaretLineMarker (Cairo.Context cr, double xPos, double y, double width, double lineHeight)
		{
			if (BackgroundRenderer != null)
				return;
			xPos = System.Math.Floor (xPos);
			cr.Rectangle (xPos, y, width, lineHeight);
			cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background));
			cr.FillPreserve ();
			cr.SetSourceColor (CurrentLineColor);
			cr.Fill ();

			//double halfLine = (cr.LineWidth / 2.0);
			//cr.MoveTo (xPos, y + halfLine);
			//cr.LineTo (xPos + width, y + halfLine);
			//cr.MoveTo (xPos, y + lineHeight - halfLine);
			//cr.LineTo (xPos + width, y + lineHeight - halfLine);
			//cr.SetSourceColor (color.SecondColor);
			//cr.Stroke ();
		}

		void DrawIndent (Cairo.Context cr, LayoutWrapper layout, DocumentLine line, double xPos, double y)
		{
			if (!textEditor.Options.DrawIndentationMarkers)
				return;
			bool dispose = false;

			if (line.Length == 0) {
				var nextLine = line.NextLine;
				while (nextLine != null && nextLine.Length == 0)
					nextLine = nextLine.NextLine;
				if (nextLine != null) {
					layout = GetLayout (nextLine);
					dispose = true;
				}
			}
			if (layout.IndentSize == 0) {
				if (dispose && layout.IsUncached)
					layout.Dispose ();
				return;
			}
			cr.Save ();
			var dotted = new [] { textEditor.Options.Zoom };
			cr.SetDash (dotted, 0);
			var top = y;
			var bottom = y + LineHeight + spaceBelow;
			if (isSpaceAbove) {
				top -= spaceAbove;
				bottom += spaceAbove;
			}
			for (int i = 0; i < layout.IndentSize; i += textEditor.Options.IndentationSize) {
				var x = System.Math.Floor (xPos + i * charWidth);
				cr.MoveTo (x + 0.5, top);
				cr.LineTo (x + 0.5, bottom);

				cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.IndentationGuide));
				cr.Stroke ();
			}
			cr.Restore ();
			if (dispose && layout.IsUncached)
				layout.Dispose ();
		}

		LayoutWrapper GetVirtualSpaceLayout (DocumentLine line, DocumentLocation location)
		{
			string virtualSpace = "";
			var data = textEditor.GetTextEditorData ();
			if (data.HasIndentationTracker && line.Length == 0) {
				virtualSpace = this.textEditor.GetTextEditorData ().GetIndentationString (location);
			}
			if (location.Column > line.Length + 1 + virtualSpace.Length)
				virtualSpace += new string (' ', location.Column - line.Length - 1 - virtualSpace.Length);
			// predit layout already contains virtual space.
			if (!string.IsNullOrEmpty (textEditor.preeditString))
				virtualSpace = "";
			LayoutWrapper wrapper = new LayoutWrapper (this, textEditor.LayoutCache.RequestLayout ());
			wrapper.IsVirtualLineText = true;
			wrapper.Text = virtualSpace;
			wrapper.Layout.Tabs = TabArray;
			wrapper.Layout.FontDescription = textEditor.Options.Font;
			int vy, vx;
			wrapper.GetSize (out vx, out vy);
			wrapper.Width = wrapper.LastLineWidth = vx / Pango.Scale.PangoScale;
			return wrapper;
		}

		void DrawLinePart (Cairo.Context cr, DocumentLine line, int lineNumber, int logicalRulerColumn, int offset, int length, ref double position, ref bool isSelectionDrawn, double y, double maxX, double _lineHeight)
		{
			int selectionStartOffset;
			int selectionEndOffset;
			if (this.HideSelection) {
				selectionStartOffset = selectionEndOffset = -1;
			} else {
				GetSelectionOffsets (line, out selectionStartOffset, out selectionEndOffset);
			}

			// ---- new renderer
			LayoutWrapper layout = CreateLinePartLayout (line, logicalRulerColumn, offset, length, selectionStartOffset, selectionEndOffset);
			int lineOffset = line.Offset;
			double width = layout.Width;
			double xPos = position;

			// The caret line marker must be drawn below the text markers otherwise the're invisible
			if ((HighlightCaretLine || textEditor.GetTextEditorData ().HighlightCaretLine) && Caret.Line == lineNumber)
				DrawCaretLineMarker (cr, xPos, y, layout.Width, _lineHeight);

			//		if (!(HighlightCaretLine || textEditor.Options.HighlightCaretLine) || Document.GetLine(Caret.Line) != line) {
			if (BackgroundRenderer == null) {
				foreach (var bg in layout.BackgroundColors) {
					int x1, x2;
					x1 = layout.IndexToPos (bg.FromIdx).X;
					x2 = layout.IndexToPos (bg.ToIdx).X;
					DrawRectangleWithRuler (
						cr, xPos + textEditor.HAdjustment.Value - TextStartPosition,
						new Cairo.Rectangle (x1 / Pango.Scale.PangoScale + position, y, (x2 - x1) / Pango.Scale.PangoScale + 1, _lineHeight),
						bg.Color, true);
				}
			}

			var metrics  = new LineMetrics {
				LineSegment = line,
				Layout = layout,

				SelectionStart = selectionStartOffset,
				SelectionEnd = selectionEndOffset,

				TextStartOffset = offset,
				TextEndOffset = offset + length,

				TextRenderStartPosition = xPos,
				TextRenderEndPosition = xPos + width,

				LineHeight = _lineHeight,
				WholeLineWidth = textEditor.Allocation.Width - xPos,

				LineYRenderStartPosition = y
			};

			foreach (TextLineMarker marker in textEditor.Document.GetMarkers (line)) {
				if (!marker.IsVisible)
					continue;

				if (marker.DrawBackground (textEditor, cr, metrics)) {
					isSelectionDrawn |= (marker.Flags & TextLineMarkerFlags.DrawsSelection) == TextLineMarkerFlags.DrawsSelection;
				}
			}

			var textSegmentMarkers = TextDocument.OrderTextSegmentMarkersByInsertion (Document.GetVisibleTextSegmentMarkersAt (line)).ToList ();
			foreach (var marker in textSegmentMarkers) {
				if (layout.Layout != null)
					marker.DrawBackground (textEditor, cr, metrics, offset, offset + length);
			}


			if (DecorateLineBg != null)
				DecorateLineBg (cr, layout, offset, length, xPos, y, selectionStartOffset, selectionEndOffset);


			if (!isSelectionDrawn && (layout.StartSet || selectionStartOffset == offset + length) && BackgroundRenderer == null) {
				double startX;
				int startY;
				double endX;
				int endY;
				if (selectionStartOffset != offset + length) {
					var start = layout.IndexToPos (layout.SelectionStartIndex);
					startX = System.Math.Floor (start.X / Pango.Scale.PangoScale);
					startY = (int)(y + System.Math.Floor (start.Y / Pango.Scale.PangoScale));

					var end = layout.IndexToPos (layout.SelectionEndIndex);
					endX = System.Math.Ceiling (end.X / Pango.Scale.PangoScale);
					endY = (int)(y + System.Math.Ceiling (end.Y / Pango.Scale.PangoScale));
				} else {
					startY = endY = (int)y;
					startX = width;
					endX = startX;
				}

				if (textEditor.MainSelection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block && startX == endX) {
					endX = startX + 2;
				}
				if (startY == endY) {
					DrawRectangleWithRuler (
						cr,
						xPos + textEditor.HAdjustment.Value - TextStartPosition,
						new Cairo.Rectangle (xPos + startX, startY, endX - startX, LineHeight),
						SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection),
						true
						);
				} else {
					DrawRectangleWithRuler (
						cr,
						xPos + textEditor.HAdjustment.Value - TextStartPosition,
						new Cairo.Rectangle (xPos + startX, startY, textEditor.Allocation.Width - xPos - startX, LineHeight),
						SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection),
						true
					);

					if (endY - startY > LineHeight) {
						DrawRectangleWithRuler (
							cr,
							xPos,
							new Cairo.Rectangle (xPos, startY + LineHeight, textEditor.Allocation.Width - xPos, endY - startY - LineHeight),
							SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection),
							true
						);
					}

					DrawRectangleWithRuler (
						cr,
						xPos,
						new Cairo.Rectangle (xPos, endY, endX, LineHeight),
						SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection),
						true
						);
				}
			}

			// highlight search results
			ISegment firstSearch;
			int o = offset;
			uint curIndex = 0, byteIndex = 0;
			if (textEditor.HighlightSearchPattern) {
				while (!(firstSearch = GetFirstSearchResult (o, offset + length)).IsInvalid ()) {
					double x = position;
					HandleSelection (lineOffset, logicalRulerColumn, selectionStartOffset, selectionEndOffset, System.Math.Max (lineOffset, firstSearch.Offset), System.Math.Min (lineOffset + line.Length, firstSearch.EndOffset), delegate(int start, int end) {
						uint startIndex = (uint)(start - offset);
						uint endIndex = (uint)(end - offset);
						if (startIndex < endIndex && endIndex <= layout.Text.Length) {
							uint startTranslated = TranslateToUTF8Index (layout.Text, startIndex, ref curIndex, ref byteIndex);
							uint endTranslated = TranslateToUTF8Index (layout.Text, endIndex, ref curIndex, ref byteIndex);

							int l, x1, x2;
							layout.IndexToLineX ((int)startTranslated, false, out l, out x1);
							layout.IndexToLineX ((int)endTranslated, false, out l, out x2);
							int w = (int)System.Math.Ceiling ((x2 - x1) / Pango.Scale.PangoScale);
							int s = (int)System.Math.Floor (x1 / Pango.Scale.PangoScale + x);
							double corner = System.Math.Min (4, width) * textEditor.Options.Zoom;

							// TODO : EditorTheme
							// var color = MainSearchResult.IsInvalid () || MainSearchResult.Offset != firstSearch.Offset ? EditorTheme.SearchResult.Color : EditorTheme.SearchResultMain.Color;
							var color = SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.FindHighlight);
							cr.SetSourceColor (color);
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
				DecorateTabsAndSpaces (cr, layout, offset, xPos, y, selectionStartOffset, selectionEndOffset);


			if (textEditor.IsSomethingSelected && !isSelectionDrawn && BackgroundRenderer == null) {
				if (lineNumber == textEditor.MainSelection.End.Line && textEditor.MainSelection.End.Column > line.Length + 1) {
					using (var wrapper = GetVirtualSpaceLayout (line, textEditor.MainSelection.End)) {
						double startX;
						double endX;
						startX = xPos;
						endX = position + wrapper.Width + layout.Width;

						DrawRectangleWithRuler (cr, xPos + textEditor.HAdjustment.Value - TextStartPosition, new Cairo.Rectangle (startX, y, endX - startX, _lineHeight), SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection), true);

						if (lineNumber == Caret.Line &&
						    textEditor.Options.ShowWhitespaces == ShowWhitespaces.Selection &&
						    textEditor.IsSomethingSelected &&
						    (selectionStartOffset < offset || selectionStartOffset == selectionEndOffset) &&
						    BackgroundRenderer == null) {
							DecorateTabsAndSpaces (cr, wrapper, offset, xPos, y, selectionStartOffset, selectionEndOffset + wrapper.Text.Length);
						}

						DrawIndent (cr, wrapper, line, position, y);
					}
				}
			}
			if (lineNumber == Caret.Line) {
				int caretOffset = Caret.Offset;
				if (offset <= caretOffset && caretOffset <= offset + length) {
					int index = caretOffset - offset;
					//This if means we have temporary indent
					if (Caret.Column > line.Length + 1) {
						using (var wrapper = GetVirtualSpaceLayout (line, Caret.Location)) {
							var x = (position + wrapper.Width) + layout.Width;
							SetVisibleCaretPosition (x, y, x, y);
							xPos = position + layout.Width;


							// When drawing virtual space before the selection start paint it as unselected.
							var virtualSpaceMod = selectionStartOffset < caretOffset ? 0 : wrapper.Text.Length;

							if ((!textEditor.IsSomethingSelected || (selectionStartOffset >= offset && selectionStartOffset != selectionEndOffset)) && (HighlightCaretLine || textEditor.Options.HighlightCaretLine) && Caret.Line == lineNumber) {
								DrawCaretLineMarker (cr, position, y, wrapper.Width, _lineHeight);
								DrawIndent (cr, wrapper, line, position, y); // caret line marker overdrawn that
							}

							if (DecorateLineBg != null)
								DecorateLineBg (cr, wrapper, offset, length, xPos, y, selectionStartOffset + virtualSpaceMod, selectionEndOffset + wrapper.Text.Length);

							if (textEditor.Options.ShowWhitespaces == ShowWhitespaces.Always) {
								DecorateTabsAndSpaces (cr, wrapper, offset, xPos, y, selectionStartOffset, selectionEndOffset + wrapper.Text.Length);
							}

							position += System.Math.Floor (wrapper.Width);
						}
					} else if (index == length && string.IsNullOrEmpty (textEditor.preeditString)) {
						var x = position + layout.Width;
						SetVisibleCaretPosition (x, y, x, y);
					} else if (index >= 0 && index <= length) {
						Pango.Rectangle strong_pos, weak_pos;
						curIndex = byteIndex = 0;
						int utf8ByteIndex = (int)TranslateToUTF8Index (layout.Text, (uint)index, ref curIndex, ref byteIndex);
						layout.GetCursorPos (utf8ByteIndex, out strong_pos, out weak_pos);
						var cx = xPos + (strong_pos.X / Pango.Scale.PangoScale);
						var cy = y + (strong_pos.Y / Pango.Scale.PangoScale);
						if (textEditor.preeditCursorCharIndex == 0) {
							SetVisibleCaretPosition (cx, cy, cx, cy);
						} else {
							var preeditIndex = (uint)(index + textEditor.preeditCursorCharIndex);
							utf8ByteIndex = (int)TranslateToUTF8Index (layout.Text, preeditIndex, ref curIndex, ref byteIndex);
							layout.GetCursorPos (utf8ByteIndex, out strong_pos, out weak_pos);
							var pcx = xPos + (strong_pos.X / Pango.Scale.PangoScale);
							var pcy = y + (strong_pos.Y / Pango.Scale.PangoScale);
							SetVisibleCaretPosition (pcx, pcy, cx, cy);
						}
					}
				}
			}
			foreach (TextLineMarker marker in textEditor.Document.GetMarkers (line)) {
				if (!marker.IsVisible)
					continue;

				if (layout.Layout != null)
					marker.Draw (textEditor, cr, metrics);
			}

			foreach (var marker in textSegmentMarkers) {
				if (layout.Layout != null)
					marker.Draw (textEditor, cr, metrics, offset, offset + length);
			}
			position += System.Math.Floor (layout.LastLineWidth);

			if (layout.IsUncached)
				layout.Dispose ();
		}

		ISegment GetFirstSearchResult (int startOffset, int endOffset)
		{
			if (startOffset < endOffset && this.selectedRegions.Count > 0) {
				var region = new TextSegment (startOffset, endOffset - startOffset);
				int min = 0;
				int max = selectedRegions.Count - 1;
				do {
					int mid = (min + max) / 2;
					var segment = selectedRegions [mid];
					if (segment.Contains (startOffset) || segment.Contains (endOffset) || region.Contains (segment)) {
						if (mid == 0)
							return segment;
						var prevSegment = selectedRegions [mid - 1];
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

			var index = GetEolMarkerIndex (line.UnicodeNewline);
			layout = eolMarkerLayout [index];
			rect = eolMarkerLayoutRect [index];
			cr.Save ();
			cr.Translate (x, y + System.Math.Max (0, LineHeight - rect.Height - 1));
			var col = (Cairo.Color)SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Foreground);

/*			if (selected && !SelectionColor.TransparentForeground) {
				col = SelectionColor.Foreground;
			} else {*/
				if (line != null && line.NextLine != null) {
					var span = textEditor.Document.SyntaxMode.GetScopeStackAsync (line.NextLine.Offset, CancellationToken.None).WaitAndGetResult (CancellationToken.None);
					var chunkStyle = EditorTheme.GetChunkStyle (span);
					if (chunkStyle != null)
						col = EditorTheme.GetForeground (chunkStyle);
				}
			//}

			cr.SetSourceRGBA (col.R, col.G, col.B, whitespaceMarkerAlpha * 1.4); // needs to more opaque due to font rendering
			cr.ShowLayout (layout);
			cr.Restore ();
		}

		static internal ulong GetPixel (Color color)
		{
			return (((ulong)color.Red) << 32) | (((ulong)color.Green) << 16) | ((ulong)color.Blue);
		}

		static internal ulong GetPixel (HslColor color)
		{
			return GetPixel ((Color)color);
		}

		static internal ulong GetPixel (Cairo.Color color)
		{
			return GetPixel ((Gdk.Color) ((HslColor)color));
		}

		internal bool InSelectionDrag = false;
		internal bool inDrag = false;
		internal DocumentLocation clickLocation;
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

			clickLocation = trans.PointToLocation (x, y, snapCharacters: true);
			if (clickLocation.Line < DocumentLocation.MinLine || clickLocation.Column < DocumentLocation.MinColumn)
				return false;
			DocumentLine line = Document.GetLine (clickLocation.Line);
			if (line != null && clickLocation.Column >= line.Length + 1 && GetWidth (Document.GetTextAt (line.SegmentIncludingDelimiter) + "-") < x) {
				clickLocation = new DocumentLocation (clickLocation.Line, line.Length + 1);
				if (textEditor.GetTextEditorData ().HasIndentationTracker && textEditor.Options.IndentStyle == IndentStyle.Virtual && clickLocation.Column == 1) {
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

			InSelectionDrag = false;
			inDrag = false;
			var selection = textEditor.MainSelection;
			int oldOffset = textEditor.Caret.Offset;

			string link = GetLink != null ? GetLink (args) : null;
			if (!String.IsNullOrEmpty (link)) {
				textEditor.ClearSelection ();
				textEditor.FireLinkEvent (link, args.Button, args.ModifierState);
				return;
			}

			if (args.Button == 1) {
				if (!CalculateClickLocation (args.X, args.Y, out clickLocation))
					return;

				DocumentLine line = Document.GetLine (clickLocation.Line);
				bool isHandled = false;
				if (line != null) {
					foreach (TextLineMarker marker in textEditor.Document.GetMarkers (line)) {
						if (marker is IActionTextLineMarker) {
							isHandled |= ((IActionTextLineMarker)marker).MousePressed (textEditor, args);
							if (isHandled)
								break;
						}
					}
					var locNotSnapped = PointToLocation (args.X, args.Y, snapCharacters: false);
					foreach (var marker in Document.GetTextSegmentMarkersAt (Document.LocationToOffset (locNotSnapped))) {
						if (!marker.IsVisible)
							continue;
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
				if (args.Button == 2 && !selection.IsEmpty && selection.Contains (Document.OffsetToLocation (offset))) {
					textEditor.ClearSelection ();
					return;
				}

				if (args.Type == EventType.TwoButtonPress) {
					var data = textEditor.GetTextEditorData ();
					mouseWordStart = data.FindCurrentWordStart (offset);
					mouseWordEnd = data.FindCurrentWordEnd (offset);
					Caret.Offset = mouseWordEnd;
					textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (textEditor.Document.OffsetToLocation (mouseWordStart), textEditor.Document.OffsetToLocation (mouseWordEnd));
					InSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.Word;

					// folding marker
					int lineNr = args.LineNumber;
					foreach (var shownFolding in GetFoldRectangles (lineNr)) {
						if (shownFolding.Key.Contains ((int)(args.X + this.XOffset), (int)args.Y)) {
							shownFolding.Value.IsCollapsed = false;
							textEditor.Document.InformFoldChanged (new FoldSegmentEventArgs (shownFolding.Value));
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

					InSelectionDrag = true;
					mouseSelectionMode = MouseSelectionMode.WholeLine;
					return;
				}
				mouseSelectionMode = MouseSelectionMode.SingleChar;

				if (textEditor.IsSomethingSelected && IsInsideSelection (clickLocation) && clickLocation != textEditor.Caret.Location) {
					inDrag = true;
				} else {
					if ((args.ModifierState & Gdk.ModifierType.ShiftMask) == ModifierType.ShiftMask) {
						InSelectionDrag = true;
						Caret.PreserveSelection = true;
						if (!textEditor.IsSomethingSelected) {
							textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (Caret.Location, clickLocation);
							Caret.Location = clickLocation;
						} else {
							Caret.Location = clickLocation;
							textEditor.ExtendSelectionTo (clickLocation);
						}
						Caret.PreserveSelection = false;
					} else {
						textEditor.ClearSelection ();
						if (Caret.Location.Line != clickLocation.Line && !String.IsNullOrEmpty (textEditor.preeditString)) {
							textEditor.CommitPreedit ();
						}
						Caret.Location = clickLocation;
						InSelectionDrag = true;
						textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (clickLocation, clickLocation);
					}
					textEditor.RequestResetCaretBlink ();
				}
			}

			DocumentLocation docLocation = PointToLocation (args.X, args.Y, snapCharacters: true);
			if (docLocation.Line < DocumentLocation.MinLine || docLocation.Column < DocumentLocation.MinColumn)
				return;

			// disable middle click on windows.
			if (!Platform.IsWindows && args.Button == 2 && this.textEditor.CanEdit (docLocation.Line)) {
				ISegment selectionRange = TextSegment.Invalid;
				int offset = Document.LocationToOffset (docLocation);
				if (!selection.IsEmpty)
					selectionRange = selection.GetSelectionRange (this.textEditor.GetTextEditorData ());
				var oldVersion = textEditor.Document.Version;

				bool autoScroll = textEditor.Caret.AutoScrollToCaret;
				textEditor.Caret.AutoScrollToCaret = false;
				if (!selection.IsEmpty && selectionRange.Contains (offset)) {
					textEditor.ClearSelection ();
					textEditor.Caret.Offset = selectionRange.EndOffset;
					return;
				}

				ClipboardActions.PasteFromPrimary (textEditor.GetTextEditorData (), offset);
				textEditor.Caret.Offset = oldOffset;
				if (!selectionRange.IsInvalid ())
					textEditor.SelectionRange = new TextSegment (oldVersion.MoveOffsetTo (Document.Version, selectionRange.Offset), selectionRange.Length);

				if (autoScroll)
					textEditor.Caret.ActivateAutoScrollWithoutMove ();
			}
		}

		bool IsInsideSelection (DocumentLocation clickLocation)
		{
			var selection = textEditor.MainSelection;
			if (selection.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block) {
				int minColumn = System.Math.Min (selection.Anchor.Column, selection.Lead.Column);
				int maxColumn = System.Math.Max (selection.Anchor.Column, selection.Lead.Column);

				return selection.MinLine <= clickLocation.Line && clickLocation.Line <= selection.MaxLine &&
					minColumn <= clickLocation.Column && clickLocation.Column <= maxColumn;
			}
			return selection.Start <= clickLocation && clickLocation < selection.End;
		}

		protected internal override void MouseReleased (MarginMouseEventArgs args)
		{
			if (args.Button != 2 && !InSelectionDrag)
				textEditor.ClearSelection ();

			DocumentLine line = Document.GetLine (clickLocation.Line);
			bool isHandled = false;
			if (line != null) {
				foreach (TextLineMarker marker in textEditor.Document.GetMarkers (line)) {
					if (marker is IActionTextLineMarker) {
						isHandled |= ((IActionTextLineMarker)marker).MouseReleased(textEditor, args);
						if (isHandled)
							break;
					}
				}
				var locNotSnapped = PointToLocation (args.X, args.Y, snapCharacters: false);
				foreach (var marker in Document.GetTextSegmentMarkersAt (Document.LocationToOffset (locNotSnapped))) {
					if (!marker.IsVisible)
						continue;
					if (marker is IActionTextLineMarker) {
						isHandled |= ((IActionTextLineMarker)marker).MouseReleased (textEditor, args);
						if (isHandled)
							break;
					}
				}
			}


			InSelectionDrag = false;
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
			if (previewWindow.HasFooterText)
				h -= previewWindow.FooterTextHeight;
			CodeSegmentEditorWindow codeSegmentEditorWindow = new CodeSegmentEditorWindow (textEditor);
			codeSegmentEditorWindow.Move (x, y);
			codeSegmentEditorWindow.Resize (w, h);
			int indentLength = -1;

			StringBuilder textBuilder = StringBuilderCache.Allocate ();
			int curOffset = previewSegment.Offset;
			while (curOffset >= 0 && curOffset < previewSegment.EndOffset && curOffset < Document.Length) {
				DocumentLine line = Document.GetLineByOffset (curOffset);
				var indentString = line.GetIndentation (Document);
				var curIndent = TextEditorData.CalcIndentLength (indentString);
				if (indentLength < 0) {
					indentLength = curIndent;
				} else {
					curOffset += TextEditorData.CalcOffset (indentString, System.Math.Min (curIndent, indentLength));
				}

				string lineText = Document.GetTextAt (curOffset, line.Offset + line.Length - curOffset);
				textBuilder.Append (lineText);
				textBuilder.AppendLine ();
				curOffset = line.EndOffsetIncludingDelimiter;
			}

			codeSegmentEditorWindow.Text = StringBuilderCache.ReturnAndFree (textBuilder);

			HideCodeSegmentPreviewWindow ();
			codeSegmentEditorWindow.ShowAll ();

			codeSegmentEditorWindow.GrabFocus ();

		}

		uint codeSegmentTooltipTimeoutId = 0;

		internal void ShowCodeSegmentPreviewTooltip (ISegment segment, Rectangle hintRectangle, uint timeout = 650)
		{
			if (previewWindow != null && previewWindow.Segment.Equals (segment))
				return;
			CancelCodeSegmentTooltip ();
			HideCodeSegmentPreviewWindow ();
			if (segment.IsInvalid () || segment.Length == 0)
				return;
			codeSegmentTooltipTimeoutId = GLib.Timeout.Add (timeout, delegate {
				codeSegmentTooltipTimeoutId = 0;
				previewWindow = new CodeSegmentPreviewWindow (textEditor, false, segment);
				if (previewWindow.IsEmptyText) {
					previewWindow.Destroy ();
					previewWindow = null;
					return false;
				}
				if (textEditor == null || textEditor.GdkWindow == null) {
					return false;
				}
				int ox = 0, oy = 0;
				textEditor.GdkWindow.GetOrigin (out ox, out oy);
				ox += textEditor.Allocation.X;
				oy += textEditor.Allocation.Y;

				int x = hintRectangle.Right;
				int y = hintRectangle.Bottom;
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


		static int ScanWord (TextDocument doc, int offset, bool forwardDirection)
		{
			if (offset < 0 || offset >= doc.Length)
				return offset;
			var line = doc.GetLineByOffset (offset);
			char first = doc.GetCharAt (offset);
			if (char.IsPunctuation (first))
				return forwardDirection ? System.Math.Min (line.Offset + line.Length, offset + 1) : System.Math.Max (line.Offset, offset);
			while (offset >= line.Offset && offset < line.Offset + line.Length) {
				char ch = doc.GetCharAt (offset);
				if (char.IsWhiteSpace (first) && !char.IsWhiteSpace (ch)
				    || WordFindStrategy.IsNoIdentifierPart (first) && !WordFindStrategy.IsNoIdentifierPart (ch)
				    || (char.IsLetterOrDigit (first) || first == '_') && !(char.IsLetterOrDigit (ch) || ch == '_'))
					break;
				offset = forwardDirection ? offset + 1 : offset - 1;
			}
			return System.Math.Min (line.Offset + line.Length,
			                        System.Math.Max (line.Offset, offset + (forwardDirection ? 0 : 1)));
		}

		List<IActionTextLineMarker> oldMarkers = new List<IActionTextLineMarker> ();
		List<IActionTextLineMarker> newMarkers = new List<IActionTextLineMarker> ();

		Cursor GetDefaultTextCursor()
		{
			var baseColor = textEditor.Style.Background(StateType.Normal);
			return  HslColor.Brightness(baseColor) < 0.5 ? xtermCursorInverted.Value : xtermCursor.Value;
		}

		protected internal override void MouseHover (MarginMouseEventArgs args)
		{
			var loc = args.Location;
			if (loc.Line < DocumentLocation.MinLine || loc.Column < DocumentLocation.MinColumn)
				return;
			var line = Document.GetLine (loc.Line);
			var oldHoveredLine = HoveredLine;
			HoveredLine = line;
			HoveredLocation = loc;
			OnHoveredLineChanged (new LineEventArgs (oldHoveredLine));

			var hoverResult = new TextLineMarkerHoverResult ();
			foreach (var marker in oldMarkers)
				marker.MouseHover (textEditor, args, hoverResult);

			if (line != null) {
				newMarkers.Clear ();
				newMarkers.AddRange (textEditor.Document.GetMarkers (line).OfType<IActionTextLineMarker> ());
				foreach (var marker in newMarkers) {
					if (oldMarkers.Contains (marker))
						continue;

					marker.MouseHover (textEditor, args, hoverResult);
				}
				oldMarkers.Clear ();
				var tmp = oldMarkers;
				oldMarkers = newMarkers;
				newMarkers = tmp;
				var locNotSnapped = PointToLocation (args.X, args.Y, snapCharacters: false);
				foreach (var marker in Document.GetTextSegmentMarkersAt (Document.LocationToOffset (locNotSnapped))) {
					if (!marker.IsVisible)
						continue;
					if (marker is IActionTextLineMarker) {
						((IActionTextLineMarker)marker).MouseHover (textEditor, args, hoverResult);
					}
				}
			} else {
				oldMarkers.Clear ();
			}
			base.cursor = hoverResult.HasCursor ? hoverResult.Cursor : GetDefaultTextCursor ();
			if (textEditor.TooltipMarkup != hoverResult.TooltipMarkup) {
				textEditor.TooltipMarkup = null;
				textEditor.TriggerTooltipQuery ();
			}
			if (!textEditor.GetTextEditorData ().SuppressTooltips)
				textEditor.TooltipMarkup = hoverResult.TooltipMarkup;
			if (args.Button != 1 && args.Y >= 0 && args.Y <= this.textEditor.Allocation.Height) {
				// folding marker
				int lineNr = args.LineNumber;
				foreach (var shownFolding in GetFoldRectangles (lineNr)) {
					if (shownFolding.Key.Contains ((int)(args.X + this.XOffset), (int)args.Y)) {
						ShowCodeSegmentPreviewTooltip (shownFolding.Value.Segment, shownFolding.Key);
						return;
					}
				}

				ShowCodeSegmentPreviewTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
				string link = GetLink != null ? GetLink (args) : null;

				if (!String.IsNullOrEmpty (link)) {
					base.cursor = textLinkCursor.Value;
				} else {
					base.cursor = hoverResult.HasCursor ? hoverResult.Cursor : GetDefaultTextCursor ();
				}
				return;
			}

			if (inDrag)
				return;
			Caret.PreserveSelection = true;

			switch (this.mouseSelectionMode) {
			case MouseSelectionMode.SingleChar:
				if (loc.Line != Caret.Line || !textEditor.GetTextEditorData ().IsCaretInVirtualLocation) {
					if (!InSelectionDrag) {
						textEditor.SetSelection (loc, loc);
					} else {
						textEditor.ExtendSelectionTo (loc);
					}
					Caret.Location = loc;
				}
				break;
			case MouseSelectionMode.Word:
				if (loc.Line != Caret.Line || !textEditor.GetTextEditorData ().IsCaretInVirtualLocation) {
					int offset = textEditor.Document.LocationToOffset (loc);
					int start;
					int end;
//					var data = textEditor.GetTextEditorData ();
					if (offset < textEditor.SelectionAnchor) {
						start = ScanWord (Document, offset, false);
						end = ScanWord (Document,  textEditor.SelectionAnchor, true);
						Caret.Offset = start;
					} else {
						start = ScanWord (Document, textEditor.SelectionAnchor, false);
						end = ScanWord (Document, offset, true);
						Caret.Offset = end;
					}
					if (!textEditor.MainSelection.IsEmpty) {
						if (Caret.Offset < mouseWordStart) {
							textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (Document.OffsetToLocation (mouseWordEnd), Caret.Location, textEditor.MainSelection.SelectionMode);
						} else {
							textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (Document.OffsetToLocation (mouseWordStart), Caret.Location, textEditor.MainSelection.SelectionMode);
						}
					}
				}
				break;
			case MouseSelectionMode.WholeLine:
				//textEditor.SetSelectLines (loc.Line, textEditor.MainSelection.Anchor.Line);
				DocumentLine line1 = textEditor.Document.GetLine (loc.Line);
				DocumentLine line2 = textEditor.Document.GetLineByOffset (textEditor.SelectionAnchor);
				var o2 = line1.Offset < line2.Offset ? line1.Offset : line1.EndOffsetIncludingDelimiter;
				Caret.Offset = o2;
				if (!textEditor.MainSelection.IsEmpty) {
					if (mouseWordStart < o2) {
						textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (textEditor.OffsetToLocation (mouseWordStart), Caret.Location, textEditor.MainSelection.SelectionMode);
					} else {
						textEditor.MainSelection = new MonoDevelop.Ide.Editor.Selection (textEditor.OffsetToLocation (mouseWordEnd), Caret.Location, textEditor.MainSelection.SelectionMode);
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
			if (Platform.IsLinux)
				blockSelModifier |= (ModifierType.SuperMask | ModifierType.Mod4Mask);

			if ((args.ModifierState & blockSelModifier) != 0) {
				textEditor.SelectionMode = MonoDevelop.Ide.Editor.SelectionMode.Block;
			} else {
				if (textEditor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Block)
					Document.CommitMultipleLineUpdate (textEditor.MainSelection.MinLine, textEditor.MainSelection.MaxLine);
				textEditor.SelectionMode = MonoDevelop.Ide.Editor.SelectionMode.Normal;
			}
			InSelectionDrag = true;
			base.MouseHover (args);

		}

		public static int GetNextTabstop (TextEditorData textEditor, int currentColumn)
		{
			int tabSize = textEditor != null && textEditor.Options != null ? textEditor.Options.TabSize : 4;
			return GetNextTabstop (textEditor, currentColumn, tabSize);
		}

		public static int GetNextTabstop (TextEditorData textEditor, int currentColumn, int tabSize)
		{
			if (tabSize == 0)
				return currentColumn;
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
			cr.SetSourceColor (color);
			var left = (int)(area.X);
			var width = (int)area.Width + 1;
			if (textEditor.GetTextEditorData ().ShowRuler) {
				var right = left + width;

				var divider = (int) (System.Math.Max (left, System.Math.Min (x + TextStartPosition + rulerX, right)));
				if (divider < right) {
					var beforeDividerWidth = divider - left;
					if (beforeDividerWidth > 0) {
						cr.Rectangle (left, area.Y, beforeDividerWidth, area.Height);
						cr.Fill ();
					}
					cr.Rectangle (divider, area.Y, right - divider, area.Height);
					cr.SetSourceColor (color);
					cr.Fill ();

					if (beforeDividerWidth > 0) {
						cr.DrawLine (
							SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Ruler),
							divider + 0.5, area.Y,
							divider + 0.5, area.Y + area.Height);
					}
					return;
				}
			}

			cr.Rectangle (left, area.Y, System.Math.Ceiling (area.Width), area.Height);
			cr.Fill ();
		}

		internal IEnumerable<KeyValuePair<Gdk.Rectangle, FoldSegment>> GetFoldRectangles (int lineNr)
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
			using (var calcTextLayout = textEditor.LayoutCache.RequestLayout ())
			using (var calcFoldingLayout = textEditor.LayoutCache.RequestLayout ()) {
				calcTextLayout.FontDescription = textEditor.Options.Font;
				calcTextLayout.Tabs = this.TabArray;

				calcFoldingLayout.FontDescription = markerLayoutFont;
				calcFoldingLayout.Tabs = this.TabArray;
				foreach (var folding in foldings) {
					int foldOffset = folding.Offset;
					if (foldOffset < offset)
						continue;

					if (folding.IsCollapsed) {
						var txt = Document.GetTextAt (offset, System.Math.Max (0, System.Math.Min (foldOffset - offset, Document.Length - offset)));
						calcTextLayout.SetText (txt);
						calcTextLayout.GetSize (out width, out height);
						xPos += width / Pango.Scale.PangoScale;
						offset = folding.EndOffset;

						calcFoldingLayout.SetText (folding.CollapsedText);

						calcFoldingLayout.GetSize (out width, out height);

						var pixelWidth = width / Pango.Scale.PangoScale + foldXMargin * 2;

						var foldingRectangle = new Rectangle ((int)xPos, y, (int)pixelWidth, (int)LineHeight - 1);
						yield return new KeyValuePair<Rectangle, FoldSegment> (foldingRectangle, folding);
						xPos += pixelWidth;
						if (folding.GetEndLine (textEditor.Document) != line) {
							line = folding.GetEndLine (textEditor.Document);
							foldings = Document.GetStartFoldings (line);
							goto restart;
						}
					}
				}
			}
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

		ISegment mainSearchResult;

		public ISegment MainSearchResult {
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

		public DocumentLocation HoveredLocation { get; private set; }

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

		bool isSpaceAbove;
		int spaceAbove, spaceBelow;

		protected internal override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNr, double x, double y, double _lineHeight)
		{
//			double xStart = System.Math.Max (area.X, XOffset);
//			xStart = System.Math.Max (0, xStart);
			cr.Rectangle (XOffset, 0, textEditor.Allocation.Width - XOffset, textEditor.Allocation.Height);
			cr.Clip ();

			var correctedXOffset = System.Math.Floor (XOffset) - 1;
			var extendingMarker = line != null ? (IExtendingTextLineMarker)textEditor.Document.GetMarkers (line).FirstOrDefault (l => l is IExtendingTextLineMarker) : null;
			isSpaceAbove = extendingMarker != null ? extendingMarker.IsSpaceAbove : false;
			spaceAbove = 0;
			spaceBelow = 0;
			var originalY = y;
			if (isSpaceAbove) {
				spaceAbove = (int)(_lineHeight - LineHeight);
				y += spaceAbove;
			} else {
				if (extendingMarker != null)
					spaceBelow = (int)(_lineHeight - LineHeight);
			}
			var lineArea = new Cairo.Rectangle (correctedXOffset, y, textEditor.Allocation.Width - correctedXOffset, LineHeight);
			var originalLineArea = lineArea;
			double position = x - textEditor.HAdjustment.Value + TextStartPosition;
			var bgColor = SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background);
			// TODO : EditorTheme does that look good ?
			if (Document.IsReadOnly) {
				if (HslColor.Brightness (bgColor) < 0.5)
					bgColor = bgColor.AddLight (0.1);
				else
					bgColor = bgColor.AddLight (-0.1);
			}
			defaultBgColor = bgColor;
			var startLineNr = lineNr;
			// Draw the default back color for the whole line. Colors other than the default
			// background will be drawn when rendering the text chunks.
			if (BackgroundRenderer == null) {
				if (LineHeight < _lineHeight) {
					var extendedLineArea = new Cairo.Rectangle (lineArea.X, originalY, lineArea.Width, _lineHeight);
					DrawRectangleWithRuler (cr, x, extendedLineArea, defaultBgColor, true);
				} else {
					DrawRectangleWithRuler (cr, x, lineArea, defaultBgColor, true);
				}
			}
			bool isSelectionDrawn = false;

			// Check if line is beyond the document length
			if (line == null) {
				DrawScrollShadow (cr, x, y, _lineHeight);
				return;
			}

			IEnumerable<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int caretOffset = Caret.Offset;
			bool isEolFolded = false;
			restart:
			int logicalRulerColumn = line.GetLogicalColumn (textEditor.GetTextEditorData (), textEditor.Options.RulerColumn);

			if ((HighlightCaretLine || textEditor.GetTextEditorData ().HighlightCaretLine) && Caret.Line == lineNr)
				DrawCaretLineMarker (cr, x, y, TextStartPosition, lineArea.Height);

			foreach (FoldSegment folding in foldings) {
				int foldOffset = folding.Offset;
				if (foldOffset < offset)
					continue;

				if (folding.IsCollapsed) {
					DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, foldOffset - offset, ref position, ref isSelectionDrawn, y, area.X + area.Width, lineArea.Height);

					offset = folding.EndOffset;
					markerLayout.SetText (folding.CollapsedText);
					int width, height;
					markerLayout.GetPixelSize (out width, out height);

					bool isFoldingSelected = !this.HideSelection && textEditor.IsSomethingSelected && textEditor.SelectionRange.Contains (folding.Segment);
					double pixelX = 0.5 + System.Math.Floor (position);
					double foldXMargin = foldMarkerXMargin * textEditor.Options.Zoom;
					double pixelWidth = System.Math.Floor (position + width - pixelX + foldXMargin * 2);
					var foldingRectangle = new Cairo.Rectangle (
						pixelX,
						y,
						pixelWidth,
						this.LineHeight);

					if (BackgroundRenderer == null && isFoldingSelected) {
						cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection));
						cr.Rectangle (foldingRectangle);
						cr.Fill ();
					}

					// if (isFoldingSelected && SelectionColor.TransparentForeground) {
					cr.SetSourceColor (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.CollapsedText));
					// } else {
					//	cr.SetSourceColor (isFoldingSelected ? SelectionColor.Foreground : EditorTheme.CollapsedText.Foreground);
					//}
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
						position + foldXMargin,
						System.Math.Floor (boundingRectangleY + System.Math.Max (0, boundingRectangleHeight - height) / 2));
					cr.ShowLayout (markerLayout);
					cr.Restore ();

					if (caretOffset == foldOffset && !string.IsNullOrEmpty (folding.CollapsedText)) {
						var cx = (int)position;
						SetVisibleCaretPosition (cx, y, cx, y);
					}
					position += foldingRectangle.Width;
					if (caretOffset == foldOffset + folding.Length && !string.IsNullOrEmpty (folding.CollapsedText)) {
						var cx = (int)position;
						SetVisibleCaretPosition (cx, y, cx, y);
					}

					if (folding.GetEndLine (textEditor.Document) != line) {
						line = folding.GetEndLine (textEditor.Document);
						lineNr = line.LineNumber;
						foldings = Document.GetStartFoldings (line);
						isEolFolded = line.EndOffset <= folding.EndOffset;
						goto restart;
					}
					isEolFolded = line.EndOffset <= folding.EndOffset;
				}
			}


			bool isEolSelected =
				!this.HideSelection &&
				textEditor.IsSomethingSelected &&
				textEditor.SelectionMode == MonoDevelop.Ide.Editor.SelectionMode.Normal &&
				textEditor.MainSelection.ContainsLine (lineNr) &&
				textEditor.MainSelection.Contains (lineNr + 1, 1);

			var lx = (int)position;
			lineArea = new Cairo.Rectangle (lx,
				lineArea.Y,
				textEditor.Allocation.Width - lx,
				lineArea.Height);


			LayoutWrapper wrapper = null;
			if (!isSelectionDrawn && BackgroundRenderer == null) {
				if (isEolSelected) {
					// prevent "gaps" in the selection drawing ('fuzzy' lines problem)
					// Need to get the layout for the remaning line which is drawn after the last fold marker
					wrapper = CreateLinePartLayout (line, logicalRulerColumn, offset, line.Offset + line.Length - offset, 0, 0);

					int remainingLineWidth, ph;
					wrapper.GetPixelSize (out remainingLineWidth, out ph);

					if (lineNr == textEditor.MainSelection.Start.Line && line.Length == 0 && textEditor.MainSelection.Start.Column > 1) {
						// position already skipped virtual space layout
					} else  {
						var eolStartX = System.Math.Floor (position + remainingLineWidth);
						lineArea = new Cairo.Rectangle (
							eolStartX,
							lineArea.Y + System.Math.Max (0, wrapper.Height - LineHeight),
							textEditor.Allocation.Width - eolStartX,
							LineHeight);
					}
					if (lineNr != textEditor.MainSelection.End.Line)
						DrawRectangleWithRuler (cr, x, lineArea, SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Selection), false);
					if (line.Length == 0)
						DrawIndent (cr, wrapper, line, lx, y);
				} else if (!(HighlightCaretLine || textEditor.GetTextEditorData ().HighlightCaretLine) || Caret.Line != lineNr && Caret.Line != startLineNr) {
					wrapper = GetLayout (line);
					//if (wrapper.EolSpanStack != null) {
					//	foreach (var span in wrapper.EolSpanStack) {
					//		var spanStyle = textEditor.EditorTheme.GetChunkStyle (span.Color);
					//		if (spanStyle == null)
					//			continue;
					//		if (!spanStyle.TransparentBackground && GetPixel (SyntaxHighlightingService.GetColor (textEditor.EditorTheme, EditorThemeColors.Background)) != GetPixel (spanStyle.Background)) {
					//			DrawRectangleWithRuler (cr, x, lineArea, spanStyle.Background, false);
					//			break;
					//		}
					//	}
					//}
				} else {
					double xPos = position;
					if (line.Length == 0 && Caret.Column > 1) {
						wrapper = GetLayout (line);
						DrawIndent (cr, wrapper, line, lx, y);
					}
					DrawCaretLineMarker (cr, xPos, y, lineArea.X + lineArea.Width - xPos, lineArea.Height);
				}
			}

			// Draw remaining line - must be called for empty line parts as well because the caret may be at this positon
			// and the caret position is calculated in DrawLinePart.
			if (line.EndOffsetIncludingDelimiter - offset >= 0) {
				DrawLinePart (cr, line, lineNr, logicalRulerColumn, offset, line.Offset + line.Length - offset, ref position, ref isSelectionDrawn, y, area.X + area.Width, lineArea.Height);
			}

			if (textEditor.Options.ShowWhitespaces != ShowWhitespaces.Never) {
				switch (textEditor.Options.ShowWhitespaces) {
				case ShowWhitespaces.Selection:
					if (!isEolFolded && isEolSelected)
					if (!(BackgroundRenderer != null && textEditor.Options.ShowWhitespaces == ShowWhitespaces.Selection))
					if (textEditor.MainSelection.Contains (lineNr, 2 + line.Length) &&
					    !(lineNr == Caret.Line && Caret.Column > 1 && textEditor.MainSelection.Anchor.Line < textEditor.MainSelection.Lead.Line) &&
					    textEditor.MainSelection.Anchor.Line != textEditor.MainSelection.Lead.Line)
						goto case ShowWhitespaces.Always;
					break;
				case ShowWhitespaces.Always:
					if (wrapper == null)
						wrapper = GetLayout (line);
					DrawEolMarker (cr, line, isEolSelected, position, y + System.Math.Max (0, wrapper.Height - LineHeight));
					break;
				}
			}

			if (extendingMarker != null)
				extendingMarker.Draw (textEditor, cr, lineNr, originalLineArea);

			if (BackgroundRenderer == null) {
				var metrics = new EndOfLineMetrics {
					LineSegment = line,
					TextRenderEndPosition = TextStartPosition + position,
					LineHeight = _lineHeight,
					LineYRenderStartPosition = y
				};
				foreach (var marker in textEditor.Document.GetMarkers (line)) {
					marker.DrawAfterEol (textEditor, cr, metrics);
				}
			}

			lastLineRenderWidth = position;
			DrawScrollShadow (cr, x, y, _lineHeight);
			if (wrapper != null && wrapper.IsUncached)
				wrapper.Dispose ();
		}

		void DrawScrollShadow (Cairo.Context cr, double x, double y, double _lineHeight)
		{
			if (textEditor.HAdjustment.Value > 0) {
				cr.LineWidth = textEditor.Options.Zoom;
				for (int i = 0; i < verticalShadowAlphaTable.Length; i++) {
					cr.SetSourceRGBA (0, 0, 0, 1 - verticalShadowAlphaTable [i]);
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
			ShowCodeSegmentPreviewTooltip (TextSegment.Invalid, Gdk.Rectangle.Zero);
		}

		#region Coordinate transformation
		class VisualLocationTranslator
		{
			TextViewMargin margin;
			int lineNumber;
			DocumentLine line;
			int xPos = 0;
			int yPos = 0;

			public bool WasInLine {
				get;
				set;
			}

			public VisualLocationTranslator (TextViewMargin margin)
			{
				this.margin = margin;
			}

			int index;
			bool snapCharacters;

			bool ConsumeLayout (LayoutWrapper layoutWrapper, int xp, int yp)
			{
				int trailing;
				if (layoutWrapper.Layout != null) {
					bool isInside = layoutWrapper.XyToIndex (xp, yp, out index, out trailing);

					if (isInside) {
						int lineNr;
						int xp1, xp2;
						layoutWrapper.IndexToLineX (index, false, out lineNr, out xp1);
						layoutWrapper.IndexToLineX (index + 1, false, out lineNr, out xp2);
						index = TranslateIndexToUTF8 (layoutWrapper.Text, index);

						if (snapCharacters && !IsNearX1 (xp, xp1, xp2)) {
							index++;
							if (index < layoutWrapper.Text.Length  && CaretMoveActions.IsLowSurrogateMarkerSet (layoutWrapper.Text [index]))
								index++;
						}
						return true;
					}
				}
				index = line.Length;
				return false;
			}

			static bool IsNearX1 (int pos, int x1, int x2)
			{
				return System.Math.Abs (x1 - pos) < System.Math.Abs (x2 - pos);
			}

			public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false, bool snapCharacters = false)
			{
				lineNumber = System.Math.Min (margin.YToLine (yp + margin.textEditor.VAdjustment.Value), margin.Document.LineCount);
				line = lineNumber <= margin.Document.LineCount ? margin.Document.GetLine (lineNumber) : null;
				this.snapCharacters = snapCharacters;
				if (line == null)
					return DocumentLocation.Empty;

				int offset = line.Offset;

				xp -= margin.TextStartPosition;
				xp += margin.textEditor.HAdjustment.Value;
				xp *= Pango.Scale.PangoScale;
				if (xp < 0)
					return new DocumentLocation (lineNumber, DocumentLocation.MinColumn);
				yp = 0;
//				yp -= margin.LineToY (lineNumber);
//				yp *= Pango.Scale.PangoScale;
				int column = DocumentLocation.MinColumn;
				IEnumerable<FoldSegment> foldings = margin.Document.GetStartFoldings (line);
				bool done = false;
				Pango.Layout measueLayout = null;
				try {
					restart:
					int logicalRulerColumn = line.GetLogicalColumn (margin.textEditor.GetTextEditorData (), margin.textEditor.Options.RulerColumn);
					foreach (FoldSegment folding in foldings) {
						if (!folding.IsCollapsed)
							continue;
						int foldOffset = folding.Offset;
						if (foldOffset < offset)
							continue;
						var layoutWrapper = margin.CreateLinePartLayout (line, logicalRulerColumn, line.Offset, foldOffset - offset, -1, -1);
						int height, width;
						try {
							done |= ConsumeLayout (layoutWrapper, (int)(xp - xPos), (int)(yp - yPos));
							if (done)
								break;
							layoutWrapper.GetPixelSize (out width, out height);
						} finally {
							if (layoutWrapper.IsUncached)
								layoutWrapper.Dispose ();
						}
						xPos += width * (int)Pango.Scale.PangoScale;
						if (measueLayout == null) {
							measueLayout = margin.textEditor.LayoutCache.RequestLayout ();
							measueLayout.SetText (folding.CollapsedText);
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

						offset = folding.EndOffset;
						DocumentLocation foldingEndLocation = margin.Document.OffsetToLocation (offset);
						lineNumber = foldingEndLocation.Line;
						column = foldingEndLocation.Column;
						if (xPos >= xp) {
							index = 0;
							done = true;
							break;
						}

						if (folding.GetEndLine (margin.Document)!= line) {
							line = folding.GetEndLine (margin.Document);
							foldings = margin.Document.GetStartFoldings (line);
							goto restart;
						}
					}
					if (!done) {
						LayoutWrapper layoutWrapper = margin.CreateLinePartLayout (line, logicalRulerColumn, offset, line.Offset + line.Length - offset, -1, -1);
						try {
							if (!ConsumeLayout (layoutWrapper, (int)(xp - xPos), (int)(yp - yPos))) {
								if (endAtEol)
									return DocumentLocation.Empty;
							}
						} finally {
							if (layoutWrapper.IsUncached)
								layoutWrapper.Dispose ();
						}
					}
				} finally {
					if (measueLayout != null)
						measueLayout.Dispose ();
				}
				return new DocumentLocation (lineNumber, column + index);
			}
		}

		public DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false, bool snapCharacters = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (xp, yp, endAtEol, snapCharacters);
		}

		public DocumentLocation PointToLocation (Cairo.Point p, bool endAtEol = false, bool snapCharacters = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y, endAtEol, snapCharacters);
		}

		public DocumentLocation PointToLocation (Cairo.PointD p, bool endAtEol = false, bool snapCharacters = false)
		{
			return new VisualLocationTranslator (this).PointToLocation (p.X, p.Y, endAtEol, snapCharacters);
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
				using (var l = textEditor.LayoutCache.RequestLayout ()) {
					l.SetText (textEditor.GetTextEditorData ().IndentationTracker.GetIndentationString (line.LineNumber));
					l.Alignment = Pango.Alignment.Left;
					l.FontDescription = textEditor.Options.Font;
					l.Tabs = TabArray;

					Pango.Rectangle ink_rect, logical_rect;
					l.GetExtents (out ink_rect, out logical_rect);
					return (logical_rect.Width + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale;
				}
			}
			if (line == null || line.Length == 0 || column < 0)
				return 0;

			var wrapper = GetLayout (line);
			uint curIndex = 0;
			uint byteIndex = 0;
			int index;
			Pango.Rectangle pos;
			try {
				index = (int)TranslateToUTF8Index (wrapper.Text, (uint)System.Math.Min (System.Math.Max (0, column), wrapper.Text.Length), ref curIndex, ref byteIndex);
				pos = wrapper.IndexToPos (index);
			} catch (Exception ex) {
				LoggingService.LogError ($"Error calculating X position for {line}@{column}", ex);
				return 0;
			} finally {
				if (wrapper.IsUncached)
					wrapper.Dispose ();
			}

			return (pos.X + Pango.Scale.PangoScale - 1) / Pango.Scale.PangoScale;
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
			foreach (var marker in textEditor.Document.GetMarkers (line)) {
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
