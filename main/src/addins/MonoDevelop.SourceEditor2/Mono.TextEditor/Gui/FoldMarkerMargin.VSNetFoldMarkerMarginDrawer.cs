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
		sealed class VSNetFoldMarkerMarginDrawer : LineStateFoldMarkerMarginDrawer
		{
			double foldSegmentSize = 8;

			Color foldBgGC, foldLineGC, foldLineHighlightedGC, foldLineHighlightedGCBg, foldToggleMarkerGC, foldToggleMarkerBackground;
			List<FoldSegment> startFoldings = new List<FoldSegment> ();
			List<FoldSegment> containingFoldings = new List<FoldSegment> ();
			List<FoldSegment> endFoldings        = new List<FoldSegment> ();

			public override bool AutoHide { get => false; }

			internal VSNetFoldMarkerMarginDrawer (FoldMarkerMargin margin) : base (margin)
			{
			}

			public override void OptionsChanged ()
			{
				base.OptionsChanged ();
				foldBgGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Background);
				foldLineGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.FoldLine);
				foldLineHighlightedGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Foreground);

				HslColor hslColor = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Background);
				double brightness = HslColor.Brightness (hslColor);
				if (brightness < 0.5) {
					hslColor.L = hslColor.L * 0.85 + hslColor.L * 0.25;
				} else {
					hslColor.L = hslColor.L * 0.9;
				}

				foldLineHighlightedGCBg = hslColor;
				foldToggleMarkerGC = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.FoldCross);
				foldToggleMarkerBackground = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.FoldCrossBackground);
			}

			void DrawFoldSegment (Cairo.Context ctx, double x, double y, bool isOpen, bool isSelected)
			{
				var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Margin.Width - foldSegmentSize) / 2) + 0.5,
													System.Math.Floor (y + (Editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);
				ctx.Rectangle (drawArea);
				ctx.SetSourceColor (isOpen ? foldBgGC : foldToggleMarkerBackground);
				ctx.FillPreserve ();
				ctx.SetSourceColor (isSelected ? foldLineHighlightedGC : foldLineGC);
				ctx.Stroke ();

				ctx.DrawLine (isSelected ? foldLineHighlightedGC : foldToggleMarkerGC,
							  drawArea.X + drawArea.Width * 2 / 10,
							  drawArea.Y + drawArea.Height / 2,
							  drawArea.X + drawArea.Width - drawArea.Width * 2 / 10,
							  drawArea.Y + drawArea.Height / 2);

				if (!isOpen)
					ctx.DrawLine (isSelected ? foldLineHighlightedGC : foldToggleMarkerGC,
								  drawArea.X + drawArea.Width / 2,
								  drawArea.Y + drawArea.Height * 2 / 10,
								  drawArea.X + drawArea.Width / 2,
								  drawArea.Y + drawArea.Height - drawArea.Height * 2 / 10);
			}

			public override void Draw (Context cr, Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
			{
				base.Draw (cr, area, line, lineNumber, x, y, lineHeight);
				if (!Editor.Options.ShowFoldMargin || lineNumber > Editor.Document.LineCount || line == null)
					return;
				foldSegmentSize = Margin.Width * 4 / 6;
				foldSegmentSize -= (foldSegmentSize) % 2;

				Cairo.Rectangle drawArea = new Cairo.Rectangle (x, y, Margin.Width, lineHeight);

				bool isFoldStart = false;
				bool isContaining = false;
				bool isFoldEnd = false;

				bool isStartSelected = false;
				bool isContainingSelected = false;
				bool isEndSelected = false;

				startFoldings.Clear ();
				containingFoldings.Clear ();
				endFoldings.Clear ();
				foreach (FoldSegment segment in Editor.Document.GetFoldingContaining (line)) {
					if (segment.GetStartLine (Editor.Document)?.Offset == line.Offset) {
						startFoldings.Add (segment);
					} else if (segment.GetEndLine (Editor.Document)?.Offset == line.Offset) {
						endFoldings.Add (segment);
					} else {
						containingFoldings.Add (segment);
					}
				}

				isFoldStart = startFoldings.Count > 0;
				isContaining = containingFoldings.Count > 0;
				isFoldEnd = endFoldings.Count > 0;

				isStartSelected = Margin.lineHover != null && IsMouseHover (startFoldings);
				isContainingSelected = Margin.lineHover != null && IsMouseHover (containingFoldings);
				isEndSelected = Margin.lineHover != null && IsMouseHover (endFoldings);

				double foldSegmentYPos = y + System.Math.Floor (Editor.LineHeight - foldSegmentSize) / 2;
				double xPos = x + System.Math.Floor (Margin.Width / 2) + 0.5;

				if (isFoldStart) {
					bool isVisible = true;
					bool moreLinedOpenFold = false;
					foreach (FoldSegment foldSegment in startFoldings) {
						if (foldSegment.IsCollapsed) {
							isVisible = false;
						} else {
							moreLinedOpenFold = foldSegment.GetEndLine (Editor.Document).Offset > foldSegment.GetStartLine (Editor.Document).Offset;
						}
					}
					bool isFoldEndFromUpperFold = false;
					foreach (FoldSegment foldSegment in endFoldings) {
						if (foldSegment.GetEndLine (Editor.Document).Offset > foldSegment.GetStartLine (Editor.Document).Offset && !foldSegment.IsCollapsed)
							isFoldEndFromUpperFold = true;
					}
					DrawFoldSegment (cr, x, y, isVisible, isStartSelected);

					if (isContaining || isFoldEndFromUpperFold)
						cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, foldSegmentYPos - 2);
					if (isContaining || moreLinedOpenFold)
						cr.DrawLine (isEndSelected || (isStartSelected && isVisible) || isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, foldSegmentYPos + foldSegmentSize + 2, xPos, drawArea.Y + drawArea.Height);
				} else {
					if (isFoldEnd) {
						double yMid = System.Math.Floor (drawArea.Y + drawArea.Height / 2) + 0.5;
						cr.DrawLine (isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid, x + Margin.Width - 2, yMid);
						cr.DrawLine (isContainingSelected || isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, yMid);

						if (isContaining)
							cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid, xPos, drawArea.Y + drawArea.Height);
					} else if (isContaining) {
						cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, drawArea.Y + drawArea.Height);
					}
				}
			}
		}
	}
}
