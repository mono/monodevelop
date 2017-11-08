// GutterMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using Gtk;
using Gdk;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	class GutterMargin : Margin
	{
		MonoTextEditor editor;
		int width;
		int oldLineCountLog10 = -1;

		public GutterMargin (MonoTextEditor editor)
		{
			this.editor = editor;

			this.editor.Document.TextChanged += UpdateWidth;
			this.editor.Caret.PositionChanged += EditorCarethandlePositionChanged;
		}

		void EditorCarethandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}

		int LineCountMax {
			get {
				return editor.Document.LineCount;
			}
		}

		void CalculateWidth ()
		{
			using (var layout = editor.LayoutCache.RequestLayout ()) {
				layout.FontDescription = gutterFont;
				layout.SetText (LineCountMax.ToString ());
				layout.Alignment = Pango.Alignment.Left;
				layout.Width = -1;
				int height;
				layout.GetPixelSize (out this.width, out height);
				this.width += 4;
				this.width += (int)MonoDevelop.SourceEditor.CurrentDebugLineTextMarker.currentLine.Width;
				if (!editor.Options.ShowFoldMargin)
					this.width += 2;
			}
		}

		void UpdateWidth (object sender, TextChangeEventArgs args)
		{
			int currentLineCountLog10 = (int)System.Math.Log10 (LineCountMax);
			if (oldLineCountLog10 != currentLineCountLog10) {
				CalculateWidth ();
				oldLineCountLog10 = currentLineCountLog10;
				editor.Document.CommitUpdateAll ();
			}
		}

		public override double Width {
			get {
				return width;
			}
		}

		DocumentLocation anchorLocation = new DocumentLocation (DocumentLocation.MinLine, DocumentLocation.MinColumn);
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);

			if (args.Button != 1 || args.LineNumber < DocumentLocation.MinLine)
				return;
			editor.LockedMargin = this;
			int lineNumber = args.LineNumber;
			bool extendSelection = (args.ModifierState & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask;
			if (lineNumber <= editor.Document.LineCount) {
				DocumentLocation loc = new DocumentLocation (lineNumber, DocumentLocation.MinColumn);
				DocumentLine line = args.LineSegment;
				if (args.Type == EventType.TwoButtonPress) {
					if (line != null)
						editor.MainSelection = new MonoDevelop.Ide.Editor.Selection (loc, GetLineEndLocation (editor.GetTextEditorData (), lineNumber));
				} else if (extendSelection) {
					if (!editor.IsSomethingSelected) {
						editor.MainSelection = new MonoDevelop.Ide.Editor.Selection (loc, loc);
					} else {
						editor.MainSelection = editor.MainSelection.WithLead (loc);
					}
				} else {
					anchorLocation = loc;
					editor.ClearSelection ();
				}
				editor.Caret.PreserveSelection = true;
				editor.Caret.Location = loc;
				editor.Caret.PreserveSelection = false;
			}
		}

		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			editor.LockedMargin = null;
			base.MouseReleased (args);
		}

		public static DocumentLocation GetLineEndLocation (TextEditorData data, int lineNumber)
		{
			DocumentLine line = data.Document.GetLine (lineNumber);

			DocumentLocation result = new DocumentLocation (lineNumber, line.Length + 1);

			FoldSegment segment = null;
			foreach (FoldSegment folding in data.Document.GetStartFoldings (line)) {
				if (folding.IsCollapsed && folding.Contains (data.Document.LocationToOffset (result))) {
					segment = folding;
					break;
				}
			}
			if (segment != null) {
				result = data.Document.OffsetToLocation (segment.EndOffset);
			}
			return result;
		}

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);

			if (!args.TriggersContextMenu () && args.Button == 1) {
				//	DocumentLocation loc = editor.Document.LogicalToVisualLocation (editor.GetTextEditorData (), editor.Caret.Location);

				int lineNumber = args.LineNumber >= DocumentLocation.MinLine ? args.LineNumber : editor.Document.LineCount;
				editor.Caret.PreserveSelection = true;
				editor.Caret.Location = new DocumentLocation (lineNumber, DocumentLocation.MinColumn);
				editor.MainSelection = new MonoDevelop.Ide.Editor.Selection (anchorLocation, editor.Caret.Location);
				editor.Caret.PreserveSelection = false;
			}
		}

		public override void Dispose ()
		{
			if (base.cursor == null)
				return;

			base.cursor.Dispose ();
			base.cursor = null;

			this.editor.Caret.PositionChanged -= EditorCarethandlePositionChanged;
			this.editor.Document.TextChanged -= UpdateWidth;
			//			layout = layout.Kill ();
			base.Dispose ();
		}

		Cairo.Color lineNumberBgGC, lineNumberGC/*, lineNumberHighlightGC*/;

		Pango.FontDescription gutterFont;

		internal protected override void OptionsChanged ()
		{

			lineNumberBgGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbersBackground);
			lineNumberGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbers);
			gutterFont = editor.Options.GutterFont;
			//			gutterFont.Weight = (Pango.Weight)editor.ColorStyle.LineNumbers.FontWeight;
			//			gutterFont.Style = (Pango.Style)editor.ColorStyle.LineNumbers.FontStyle;

			/*			if (Platform.IsWindows) {
							gutterFont.Size = (int)(Pango.Scale.PangoScale * 8.0 * editor.Options.Zoom);
						} else {
							gutterFont.Size = (int)(Pango.Scale.PangoScale * 11.0 * editor.Options.Zoom);
						}*/
			CalculateWidth ();
		}

		void DrawGutterBackground (Cairo.Context cr, int line, double x, double y, double lineHeight)
		{
			if (editor.Caret.Line == line) {
				editor.TextViewMargin.DrawCaretLineMarker (cr, x, y, Width, lineHeight);
				return;
			}
			cr.Rectangle (x, y, Width, lineHeight);
			cr.SetSourceColor (lineNumberBgGC);
			cr.Fill ();
		}

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			var extendingMarker = lineSegment != null ? (IExtendingTextLineMarker)editor.Document.GetMarkers (lineSegment).FirstOrDefault (l => l is IExtendingTextLineMarker) : null;
			bool isSpaceAbove = extendingMarker != null ? extendingMarker.IsSpaceAbove : false;

			var gutterMarker = lineSegment != null ? (MarginMarker)editor.Document.GetMarkers (lineSegment).FirstOrDefault (marker => marker is MarginMarker && ((MarginMarker)marker).CanDraw (this)) : null;
			if (gutterMarker != null && gutterMarker.CanDrawBackground (this)) {
				bool hasDrawn = gutterMarker.DrawBackground (editor, cr, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
				if (!hasDrawn)
					DrawGutterBackground (cr, line, x, y, lineHeight);
			} else {
				DrawGutterBackground (cr, line, x, y, lineHeight);
			}

			if (gutterMarker != null && gutterMarker.CanDrawForeground (this)) {
				gutterMarker.DrawForeground (editor, cr, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
				return;
			}

			if (line <= editor.Document.LineCount) {
				// Due to a mac? gtk bug I need to re-create the layout here
				// otherwise I get pango exceptions.
				DrawForeground (cr, line, x, y, lineHeight, isSpaceAbove);
			}
		}

		internal void DrawForeground (Cairo.Context cr, int line, double x, double y, double lineHeight, bool isSpaceAbove)
		{
			using (var layout = editor.LayoutCache.RequestLayout ()) {
				layout.FontDescription = gutterFont;
				layout.Width = (int)Width;
				layout.Alignment = Pango.Alignment.Right;
				layout.SetText (line.ToString ());
				cr.Save ();
				cr.Translate (x + (int)Width + (editor.Options.ShowFoldMargin ? 0 : -2), y + (isSpaceAbove ? lineHeight - editor.LineHeight : 0));
				cr.SetSourceColor (lineNumberGC);
				cr.ShowLayout (layout);
				cr.Restore ();
			}
		}
	}
}
