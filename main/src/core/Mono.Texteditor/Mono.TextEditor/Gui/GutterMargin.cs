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

namespace Mono.TextEditor
{
	public class GutterMargin : Margin
	{
		TextEditor editor;
		int width;
		int oldLineCountLog10 = -1;

		double fontHeight;
		
		public GutterMargin (TextEditor editor)
		{
			this.editor = editor;
			
			this.editor.Document.LineChanged += UpdateWidth;
			this.editor.Document.TextSet += HandleEditorDocumenthandleTextSet;
			this.editor.Caret.PositionChanged += EditorCarethandlePositionChanged;
		}

		void HandleEditorDocumenthandleTextSet (object sender, EventArgs e)
		{
			UpdateWidth (null, null);
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
				return System.Math.Max (1000, editor.Document.LineCount);
			}
		}

		void CalculateWidth ()
		{
			using (var layout = PangoUtil.CreateLayout (editor)) {
				layout.FontDescription = gutterFont;
				layout.SetText (LineCountMax.ToString ());
				layout.Alignment = Pango.Alignment.Left;
				layout.Width = -1;
				int height;
				layout.GetPixelSize (out this.width, out height);
				this.width += 4;
				if (!editor.Options.ShowFoldMargin)
					this.width += 2;

				using (var metrics = editor.PangoContext.GetMetrics (layout.FontDescription, editor.PangoContext.Language)) {
					fontHeight = System.Math.Ceiling (0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
				}

			}
		}
		
		void UpdateWidth (object sender, LineEventArgs args)
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
			if (hoverSegment != null) {
				hoverSegment.IsFolded = !hoverSegment.IsFolded;
				editor.SetAdjustments ();
				editor.Caret.MoveCaretBeforeFoldings ();
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
				if (folding.IsFolded && folding.Contains (data.Document.LocationToOffset (result))) {
					segment = folding;
					break;
				}
			}
			if (segment != null) 
				result = data.Document.OffsetToLocation (segment.EndLine.Offset + segment.EndColumn - 1); 
			return result;
		}

		DocumentLine lineHover;
		FoldSegment hoverSegment;

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);

			DocumentLine lineSegment = null;
			if (args.LineSegment != null) {
				lineSegment = args.LineSegment;
				if (lineHover != lineSegment) {
					lineHover = lineSegment;
					editor.RedrawMargin (this);
				}
			} 
			lineHover = lineSegment;
			hoverSegment = null;
			foreach (var seg in editor.Document.GetFoldingContaining (lineSegment)) {
				if (hoverSegment == null || hoverSegment.StartLine.LineNumber < seg.StartLine.LineNumber)
					hoverSegment = seg;
			}
			editor.RedrawMargin (this);
		}

		internal protected override void MouseLeft ()
		{
			base.MouseLeft ();
			hoverSegment = null;
			editor.RedrawMargin (this);
		}

		
		public override void Dispose ()
		{
			if (base.cursor == null)
				return;
			
			base.cursor.Dispose ();
			base.cursor = null;
			
			this.editor.Document.TextSet -= HandleEditorDocumenthandleTextSet;
			this.editor.Document.LineChanged -= UpdateWidth;
//			layout = layout.Kill ();
			base.Dispose ();
		}
		
		Cairo.Color lineNumberBgGC, lineNumberGC, lineNumberHighlightGC;

		Pango.FontDescription gutterFont;

		internal protected override void OptionsChanged ()
		{
			lineNumberBgGC = editor.ColorStyle.LineNumber.CairoBackgroundColor;
			lineNumberGC = editor.ColorStyle.LineNumber.CairoColor;
			gutterFont = Gtk.Widget.DefaultStyle.FontDescription.Copy ();
			gutterFont.Size = (int)(Pango.Scale.PangoScale * 11.0 * editor.Options.Zoom);

			CalculateWidth ();
		}

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			var gutterMarker = lineSegment != null ? (IGutterMarker)lineSegment.Markers.FirstOrDefault (marker => marker is IGutterMarker) : null;
			if (gutterMarker != null) {
				gutterMarker.DrawLineNumber (editor, Width, cr, area, lineSegment, line, x, y, lineHeight);
				return;
			}
			cr.Rectangle (x, y, Width, lineHeight);
			cr.Color = lineNumberBgGC;

			if (hoverSegment != null && hoverSegment.StartLine.LineNumber <= line && line <= hoverSegment.EndLine.LineNumber) {
				cr.Fill ();
				double a = 6 * editor.Options.Zoom;
				var startX = x + width - a - 2 * editor.Options.Zoom;
				if (hoverSegment.IsFolded) {
					cr.Rectangle (startX - a / 2,
					              y + lineHeight * 0.4, 
					              a,
					              lineHeight * 0.2);
					cr.Color = editor.ColorStyle.FoldMarkerSpace;
					cr.Fill ();

					cr.MoveTo (startX , y);
					cr.LineTo (startX  + a * 0.7, y + lineHeight * 0.4);
					cr.LineTo (startX  - a * 0.7 , y + lineHeight * 0.4);
					cr.ClosePath ();
					cr.Color = editor.ColorStyle.FoldMarkerArrow;
					cr.Fill ();
					
					cr.MoveTo (startX , y + lineHeight);
					cr.LineTo (startX  + a * 0.7, y + lineHeight * 0.6);
					cr.LineTo (startX  - a * 0.7 , y + lineHeight * 0.6);
					cr.ClosePath ();
					cr.Fill ();
				} else if (hoverSegment.StartLine.LineNumber == line && hoverSegment.EndLine.LineNumber == line) {
					cr.Rectangle (startX - a / 2,
					              y, 
					              a,
					              lineHeight);
					cr.Color = editor.ColorStyle.FoldMarkerSpace;
					cr.Fill ();

					cr.MoveTo (startX , y+ lineHeight * 0.4);
					cr.LineTo (startX  + a * 0.7, y );
					cr.LineTo (startX  - a * 0.7 , y );
					cr.ClosePath ();
					cr.Color = editor.ColorStyle.FoldMarkerArrow;
					cr.Fill ();
					
					cr.MoveTo (startX , y + lineHeight * 0.6);
					cr.LineTo (startX  + a * 0.7, y + lineHeight);
					cr.LineTo (startX  - a * 0.7 , y + lineHeight);
					cr.ClosePath ();
					cr.Fill ();


				} else if (hoverSegment.StartLine.LineNumber == line) {
					
					cr.Rectangle (startX - a / 2,
					              y + lineHeight / 2, 
					              a,
					              lineHeight / 2);
					cr.Color = editor.ColorStyle.FoldMarkerSpace;
					cr.Fill ();
					
					cr.MoveTo (startX , y + lineHeight);
					cr.LineTo (startX  + a * 0.7, y + lineHeight / 2);
					cr.LineTo (startX  - a * 0.7 , y + lineHeight / 2);
					cr.ClosePath ();
					cr.Color = editor.ColorStyle.FoldMarkerArrow;
					cr.Fill ();

					
				} else if (hoverSegment.EndLine.LineNumber == line) {
					
					cr.Rectangle (startX - a / 2,
					              y, 
					              a,
					              lineHeight / 2);
					cr.Color = editor.ColorStyle.FoldMarkerSpace;
					cr.Fill ();

					cr.MoveTo (startX , y);
					cr.LineTo (startX  + a * 0.7, y + lineHeight / 2);
					cr.LineTo (startX  - a * 0.7 , y + lineHeight / 2);
					cr.ClosePath ();
					cr.Color = editor.ColorStyle.FoldMarkerArrow;
					cr.Fill ();


				} else {

					cr.Rectangle (startX - a / 2,
					              y, 
					              a,
					              lineHeight);
					cr.Color = editor.ColorStyle.FoldMarkerSpace;
					cr.Fill ();

				}
				return;
			}

			bool containsFoldedFolding = editor.Document.GetFoldingContaining (lineSegment).Any (seg => seg.IsFolded && seg.StartLine == lineSegment);

			if (editor.Caret.Line == line) {
				cr.FillPreserve ();

				var color = editor.ColorStyle.LineMarker;
				cr.Color = new Cairo.Color (color.R, color.G, color.B, 0.5);
				cr.Fill ();

				var realTopY = System.Math.Floor (y + cr.LineWidth / 2) + 0.5;
				cr.MoveTo (x, realTopY);
				cr.LineTo (x + Width, realTopY);

				var realBottomY = System.Math.Floor (y + lineHeight - cr.LineWidth / 2) + 0.5;
				cr.MoveTo (x, realBottomY);
				cr.LineTo (x + Width, realBottomY);

				cr.Color = color;
				cr.Stroke ();
			} else {
				cr.Fill ();
			}

			if (line <= editor.Document.LineCount) {
				if (editor.Options.EnableQuickDiff) {
					var state = editor.Document.GetLineState (lineSegment);
					double len = editor.Options.Zoom;
					if (state == TextDocument.LineState.Changed) {
						cr.Color = editor.ColorStyle.LineChangedBg;
						cr.Rectangle (x + Width - len, y, len, lineHeight);
						cr.Fill ();
					} else if (state == TextDocument.LineState.Dirty) {
						cr.Color = editor.ColorStyle.LineDirtyBg;
						cr.Rectangle (x + Width - len, y, len, lineHeight);
						cr.Fill ();
					}
				}
				if (containsFoldedFolding) {
					cr.Color = editor.ColorStyle.FoldMarkerCollapsedots;
					var rad = 2 * editor.Options.Zoom;
					for (int i = 0; i < 3; i++) {
						cr.Arc (x + Width - rad * 2, y  + rad + i * lineHeight / 3, rad, 0, System.Math.PI * 2);
						cr.Fill ();
					}
					return;
				}

				// Due to a mac? gtk bug I need to re-create the layout here
				// otherwise I get pango exceptions.
				using (var layout = PangoUtil.CreateLayout (editor)) {
					layout.FontDescription = gutterFont;

					layout.Width = (int)Width;
					layout.Alignment = Pango.Alignment.Right;
					layout.SetText (line.ToString ());
					cr.Save ();
					cr.Translate (x + (int)Width + (editor.Options.ShowFoldMargin ? 0 : -2), y + (lineHeight - fontHeight) / 2);
					cr.Color = lineNumberGC;
					cr.ShowLayout (layout);
					cr.Restore ();
				}
			}
		}
	}
}
