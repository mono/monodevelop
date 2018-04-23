// FoldMarkerMargin.cs
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

using System.Linq;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	partial class FoldMarkerMargin
	{
		abstract class FoldMarkerMarginDrawer
		{
			readonly FoldMarkerMargin margin;

			public FoldMarkerMargin Margin => margin;
			public MonoTextEditor Editor => margin.editor;
			public abstract bool AutoHide { get; }
			public double FoldMarkerOcapitiy { get; set; }

			protected FoldMarkerMarginDrawer (FoldMarkerMargin margin)
			{
				this.margin = margin;
			}

			public abstract void OptionsChanged ();

			public abstract void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight);

			protected MarginMarker GetMarginMarker (DocumentLine line)
			{
				if (line != null) {
					foreach (var m in Editor.Document.GetMarkers (line)) {
						var mm = m as MarginMarker;
						if (mm != null && mm.CanDraw (Margin)) {
							return mm;
						}
					}
				}
				return null;
			}

			protected bool IsMouseHover (IEnumerable<FoldSegment> foldings)
			{
				return foldings.Any (s => Margin.lineHover == s.GetStartLine (Editor.Document));
			}
		}

		abstract class LineStateFoldMarkerMarginDrawer : FoldMarkerMarginDrawer
		{
			Color lineStateChangedGC, lineStateDirtyGC, backgroundColor;

			protected LineStateFoldMarkerMarginDrawer (FoldMarkerMargin margin) : base (margin)
			{
			}

			public override void OptionsChanged ()
			{
				lineStateChangedGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.QuickDiffChanged);
				lineStateDirtyGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.QuickDiffDirty);
				backgroundColor = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Background);
			}

			public override void Draw (Context cr, Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
			{
				MarginMarker marker = GetMarginMarker (line);
				bool hasDrawn = false;
				if (marker != null) {
					hasDrawn = marker.DrawBackground (Editor, cr, new MarginDrawMetrics (Margin, area, line, lineNumber, x, y, lineHeight));
				}

				if (!hasDrawn) {
					if (Editor.GetTextEditorData ().HighlightCaretLine && Editor.Caret.Line == lineNumber) {
						Editor.TextViewMargin.DrawCaretLineMarker (cr, x, y, Margin.Width, lineHeight);
					} else {
						cr.Rectangle (x, y, Margin.Width, lineHeight);
						cr.SetSourceColor (backgroundColor);
						cr.Fill ();
					}
				}

				var state = Editor.Document.GetLineState (line);

				if (Editor.Options.EnableQuickDiff && state != TextDocument.LineState.Unchanged) {
					var prevState = line?.PreviousLine != null ? Editor.Document.GetLineState (line.PreviousLine) : TextDocument.LineState.Unchanged;
					var nextState = line?.NextLine != null ? Editor.Document.GetLineState (line.NextLine) : TextDocument.LineState.Unchanged;

					if (state == TextDocument.LineState.Changed) {
						cr.SetSourceColor (lineStateChangedGC);
					} else if (state == TextDocument.LineState.Dirty) {
						cr.SetSourceColor (lineStateDirtyGC);
					}

					if ((prevState == TextDocument.LineState.Unchanged && prevState != state ||
						 nextState == TextDocument.LineState.Unchanged && nextState != state)) {
						FoldingScreenbackgroundRenderer.DrawRoundRectangle (
							cr,
							prevState == TextDocument.LineState.Unchanged,
							nextState == TextDocument.LineState.Unchanged,
							x + 1,
							y,
							lineHeight / 4,
							Margin.Width / 4,
							lineHeight
						);
					} else {
						cr.Rectangle (x + 1, y, Margin.Width / 4, lineHeight);
					}
					cr.Fill ();
				}
			}
		}
	}
}
