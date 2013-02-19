// 
// FoldingScreenbackgroundRenderer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class FoldingScreenbackgroundRenderer : IBackgroundRenderer, IDisposable
	{
		TextEditor editor;
		List<FoldSegment> foldSegments;
		[Flags]
		enum Roles
		{
			Between = 0,
			Start = 1,
			End = 2
		}
		TextDocument Document {
			get { return editor.Document; }
		}

		readonly DateTime startTime;
		uint timeout;
		const uint animationLength = 250;
		public bool AnimationFinished {
			get {
				var age = (DateTime.Now - startTime).TotalMilliseconds;
				return age >= animationLength;
			}
		}

		public FoldingScreenbackgroundRenderer (TextEditor editor, IEnumerable<FoldSegment> foldSegments)
		{
			this.editor = editor;
			this.foldSegments = new List<FoldSegment> (foldSegments);
			startTime = DateTime.Now;
			timeout = GLib.Timeout.Add (30, delegate {
				editor.QueueDraw ();
				var cont = (DateTime.Now - startTime).TotalMilliseconds < animationLength;
				if (!cont)
					timeout = 0;
				return cont;
			});
		}

		HslColor GetColor (int i, double brightness, int colorCount)
		{
			HslColor hslColor = new HslColor (editor.ColorStyle.PlainText.Background);
			int colorPosition = i + 1;
			if (i == foldSegments.Count - 1)
				return hslColor;
			if (brightness < 0.5) {
				hslColor.L = hslColor.L * 0.81 + hslColor.L * 0.25 * (colorCount - colorPosition) / colorCount;
			} else {
				hslColor.L = hslColor.L * 0.86 + hslColor.L * 0.1 * colorPosition / colorCount;
			}
			return hslColor;
		}
		
		public void Draw (Cairo.Context cr, Cairo.Rectangle area)
		{
			TextViewMargin textViewMargin = editor.TextViewMargin;
			ISyntaxMode mode = Document.SyntaxMode != null && editor.Options.EnableSyntaxHighlighting ? Document.SyntaxMode : new SyntaxMode (Document);

			TextViewMargin.LayoutWrapper lineLayout = null;
			double brightness = HslColor.Brightness (editor.ColorStyle.PlainText.Background);

			int colorCount = foldSegments.Count + 2;
			cr.Color = GetColor (-1, brightness, colorCount);
			cr.Rectangle (area);
			cr.Fill ();
			var rectangles = new Cairo.Rectangle[foldSegments.Count];
			const int xPadding = 4;
			const int yPadding = 2;
			const int rightMarginPadding = 16;
			for (int i = foldSegments.Count - 1; i >= 0 ; i--) {
				var segment = foldSegments [i];
				var segmentStartLine = segment.StartLine;
				var segmentEndLine = segment.EndLine;

				int curWidth = 0;
				var endLine = segmentEndLine.NextLine;
				var y = editor.LineToY (segmentStartLine.LineNumber);
				for (var curLine = segmentStartLine; curLine != endLine && y < editor.VAdjustment.Value + editor.Allocation.Height; curLine = curLine.NextLine) {
					var curLayout = textViewMargin.CreateLinePartLayout (mode, curLine, curLine.Offset, curLine.Length, -1, -1);
					var width = (int)(curLayout.PangoWidth / Pango.Scale.PangoScale);
					curWidth = System.Math.Max (curWidth, width);
					y += editor.GetLineHeight (curLine);
				}

				double xPos = textViewMargin.XOffset;
				double rectangleWidth = 0, rectangleHeight = 0;
				
				lineLayout = textViewMargin.CreateLinePartLayout (mode, segmentStartLine, segmentStartLine.Offset, segmentStartLine.Length, -1, -1);
				var rectangleStart = lineLayout.Layout.IndexToPos (GetFirstNonWsIdx (lineLayout.Layout.Text));
				xPos = System.Math.Max (textViewMargin.XOffset, (textViewMargin.XOffset + textViewMargin.TextStartPosition + rectangleStart.X / Pango.Scale.PangoScale) - xPadding);

				lineLayout = textViewMargin.CreateLinePartLayout (mode, segmentEndLine, segmentEndLine.Offset, segmentEndLine.Length, -1, -1);
				
				var rectangleEnd = lineLayout.Layout.IndexToPos (GetFirstNonWsIdx (lineLayout.Layout.Text));
				xPos = System.Math.Min (xPos, System.Math.Max (textViewMargin.XOffset, (textViewMargin.XOffset + textViewMargin.TextStartPosition + rectangleEnd.X / Pango.Scale.PangoScale) - xPadding));

				rectangleWidth = textViewMargin.XOffset + textViewMargin.TextStartPosition + curWidth - xPos + xPadding * 2;

				if (i < foldSegments.Count - 1) {
					rectangleWidth = System.Math.Max ((rectangles [i + 1].X + rectangles[i + 1].Width + rightMarginPadding) - xPos, rectangleWidth);
				}

				y = editor.LineToY (segment.StartLine.LineNumber);
				var yEnd = editor.LineToY (segment.EndLine.LineNumber + 1);
				if (yEnd == 0)
					yEnd = editor.VAdjustment.Upper;
				rectangleHeight = yEnd - y;

				rectangles[i] = new Cairo.Rectangle (xPos, y - yPadding, rectangleWidth, rectangleHeight + yPadding * 2);
			}

			for (int i = 0; i < foldSegments.Count; i++) {
				var rect = rectangles[i];

				if (i == foldSegments.Count - 1) {
/*					var radius = (int)(editor.Options.Zoom * 2);
					int w = 2 * radius;
					using (var shadow = new Blur (
						System.Math.Min ((int)rect.Width + w * 2, editor.Allocation.Width),
						System.Math.Min ((int)rect.Height + w * 2, editor.Allocation.Height), 
						radius)) {
						using (var gctx = shadow.GetContext ()) {
							gctx.Color = new Cairo.Color (0, 0, 0, 0);
							gctx.Fill ();

							var a = 0;
							var b = 0;
							DrawRoundRectangle (gctx, true, true, w - a, w - b, editor.LineHeight / 4, rect.Width + a * 2, rect.Height + a * 2);
							var bg = editor.ColorStyle.Default.CairoColor;
							gctx.Color = new Cairo.Color (bg.R, bg.G, bg.B, 0.6);
							gctx.Fill ();
						}

						cr.Save ();
						cr.Translate (rect.X - w - editor.HAdjustment.Value, rect.Y - editor.VAdjustment.Value - w);
						shadow.Draw (cr);
						cr.Restore ();
					}*/

					var curPadSize = 1;

					var age = (DateTime.Now - startTime).TotalMilliseconds;
					var alpha = 0.1;
					if (age < animationLength) {
						var animationState = age / (double)animationLength;
						curPadSize = (int)(3 + System.Math.Sin (System.Math.PI * animationState) * 3);
						alpha = 0.1 + (1.0 - animationState) / 5;
					}

					var bg = editor.ColorStyle.PlainText.Foreground;
					cr.Color = new Cairo.Color (bg.R, bg.G, bg.B, alpha);
					DrawRoundRectangle (cr, true, true, rect.X - editor.HAdjustment.Value - curPadSize , rect.Y - editor.VAdjustment.Value - curPadSize, editor.LineHeight / 2, rect.Width + curPadSize * 2, rect.Height + curPadSize * 2);
					cr.Fill ();

					if (age < animationLength) {
						var animationState = age / (double)animationLength;
						curPadSize = (int)(2 + System.Math.Sin (System.Math.PI * animationState) * 2);
						DrawRoundRectangle (cr, true, true, rect.X - editor.HAdjustment.Value - curPadSize, rect.Y - editor.VAdjustment.Value - curPadSize, editor.LineHeight / 2, rect.Width + curPadSize * 2, rect.Height + curPadSize * 2);
						cr.Color = GetColor (i, brightness, colorCount);
						cr.Fill ();

						continue;
					}
				}

				DrawRoundRectangle (cr, true, true, rect.X - editor.HAdjustment.Value, rect.Y - editor.VAdjustment.Value, editor.LineHeight / 2, rect.Width, rect.Height);
				
				cr.Color = GetColor (i, brightness, colorCount);
				cr.Fill ();
			}
		}

		public static void DrawRoundRectangle (Cairo.Context cr, bool upperRound, bool lowerRound, double x, double y, double r, double w, double h)
		{
			DrawRoundRectangle (cr, upperRound, upperRound, lowerRound, lowerRound, x, y, r, w, h);
		}
		
		public static void DrawRoundRectangle (Cairo.Context cr, bool topLeftRound, bool topRightRound, bool bottomLeftRound, bool bottomRightRound, double x, double y, double r, double w, double h)
		{
			//  UA****BQ
			//  H      C
			//  *      *
			//  G      D
			//  TF****ES
			
			cr.NewPath ();
			
			if (topLeftRound) {
				cr.MoveTo (x + r, y);                 // Move to A
			} else {
				cr.MoveTo (x, y);             // Move to U
			}
			
			if (topRightRound) {
				cr.LineTo (x + w - r, y);             // Straight line to B
				
				cr.CurveTo (x + w, y, 
				            x + w, y,
				            x + w, y + r); // Curve to C, Control points are both at Q
			} else {
				cr.LineTo (x + w, y);         // Straight line to Q
			}
			
			if (bottomRightRound) {
				cr.LineTo (x + w, y + h - r);                              // Move to D

				cr.CurveTo (x + w, y + h, 
				            x + w, y + h, 
				            x + w - r, y + h); // Curve to E
			} else {
				cr.LineTo (x + w, y + h); // Move to S
			}
			
			if (bottomLeftRound) {
				cr.LineTo (x + r, y + h);                      // Line to F
				cr.CurveTo (x, y + h, 
				            x, y + h, 
				            x, y + h - r); // Curve to G
			} else {
				cr.LineTo (x, y + h); // Line to T
			}
			
			if (topLeftRound) {
				cr.LineTo (x, y + r);              // Line to H
				cr.CurveTo (x, y, 
				            x, y, 
				            x + r, y); // Curve to A
			} else {
				cr.LineTo (x, y); // Line to U
			}
			cr.ClosePath ();
		}
		
		int GetFirstNonWsIdx (string text)
		{
			for (int i = 0; i < text.Length; i++) {
				if (!Char.IsWhiteSpace (text [i]))
					return i;
			}
			return 0;
		}
		
		#region IDisposable implementation
		void IDisposable.Dispose ()
		{
			if (timeout != 0) {
				GLib.Source.Remove (timeout);
				timeout = 0;
			}
		}
		#endregion
	}
}
