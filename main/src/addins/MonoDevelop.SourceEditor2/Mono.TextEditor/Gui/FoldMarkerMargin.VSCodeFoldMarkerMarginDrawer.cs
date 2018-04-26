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
using System.Runtime.InteropServices.ComTypes;
using Mono.TextEditor.Theatrics;

namespace Mono.TextEditor
{
	partial class FoldMarkerMargin
	{
		sealed class VSCodeFoldMarkerMarginDrawer : LineStateFoldMarkerMarginDrawer
		{
			double foldSegmentSize = 8;
			Color background, foreground, foldLine;

			List<FoldSegment> startFoldings = new List<FoldSegment> ();

			public override bool AutoHide { get => true; }

			internal VSCodeFoldMarkerMarginDrawer (FoldMarkerMargin margin) : base (margin)
			{
			}

			public override void OptionsChanged ()
			{
				base.OptionsChanged ();
				background = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Background);
				foreground = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.Foreground);
				foldLine = SyntaxHighlightingService.GetColor (Editor.EditorTheme, EditorThemeColors.FoldLine);
			}

			public override void Draw (Context cr, Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
			{
				base.Draw (cr, area, line, lineNumber, x, y, lineHeight);
				if (!Editor.Options.ShowFoldMargin || lineNumber > Editor.Document.LineCount || line == null)
					return;

				foldSegmentSize = Margin.Width * 4 / 6;
				foldSegmentSize -= (foldSegmentSize) % 2;

				startFoldings.Clear ();

				foreach (var segment in Editor.Document.GetFoldingContaining (line)) {
					if (segment.GetStartLine (Editor.Document)?.Offset == line.Offset)
						startFoldings.Add (segment);
				}
				if (startFoldings.Count == 0)
					return;

				bool isCollapsed = false;

				foreach (var foldSegment in startFoldings) {
					if (foldSegment.IsCollapsed) {
						isCollapsed = true;
						break;
					} 
				}

				if (!isCollapsed && FoldMarkerOcapitiy <= 0)
					return;
				DrawFoldSegment (cr, x, y, isCollapsed);
			}

			void DrawFoldSegment (Cairo.Context ctx, double x, double y, bool isCollapsed)
			{
				var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Margin.Width - foldSegmentSize) / 2) + 0.5,
													System.Math.Floor (y + (Editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);
				ctx.Rectangle (drawArea);
				var useSolidColor = isCollapsed || FoldMarkerOcapitiy >= 1;
				var drawColor = background;
				if (!useSolidColor)
					drawColor.A = FoldMarkerOcapitiy;

				ctx.SetSourceColor (drawColor);
				ctx.FillPreserve ();

				drawColor = foldLine;
				if (!useSolidColor)
					drawColor.A = FoldMarkerOcapitiy;

				ctx.SetSourceColor (drawColor);
				ctx.Stroke ();

				drawColor = foreground;
				if (!useSolidColor)
					drawColor.A = FoldMarkerOcapitiy;

				ctx.DrawLine (drawColor,
							  drawArea.X + drawArea.Width * 2 / 10,
							  drawArea.Y + drawArea.Height / 2,
							  drawArea.X + drawArea.Width - drawArea.Width * 2 / 10,
							  drawArea.Y + drawArea.Height / 2);

				if (isCollapsed)
					ctx.DrawLine (drawColor,
								  drawArea.X + drawArea.Width / 2,
								  drawArea.Y + drawArea.Height * 2 / 10,
								  drawArea.X + drawArea.Width / 2,
								  drawArea.Y + drawArea.Height - drawArea.Height * 2 / 10);
			}
		}

	}
}
