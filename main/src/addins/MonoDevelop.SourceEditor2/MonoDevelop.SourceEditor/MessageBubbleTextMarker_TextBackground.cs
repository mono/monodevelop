//
// MessageBubbleTextMarker_TextBackground.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

using Mono.TextEditor;
using System;
using System.Linq;
using MonoDevelop.Components;

namespace MonoDevelop.SourceEditor
{
	partial class MessageBubbleTextMarker : IBackgroundMarker
	{
		bool IBackgroundMarker.DrawBackground (TextEditor editor, Cairo.Context g, double y, LineMetrics metrics, ref bool drawBg)
		{
			if (!IsVisible)
				return true;
			bool markerShouldDrawnAsHidden = cache.CurrentSelectedTextMarker != null && cache.CurrentSelectedTextMarker != this;


			EnsureLayoutCreated (editor);
			double x = editor.TextViewMargin.XOffset;
			int right = editor.Allocation.Width;
			bool isCaretInLine = metrics.TextStartOffset <= editor.Caret.Offset && editor.Caret.Offset <= metrics.TextEndOffset;
			int errorCounterWidth = GetErrorCountBounds (metrics.Layout).Item1;

			double x2 = System.Math.Max (right - LayoutWidth - border - (ShowIconsInBubble ? cache.errorPixbuf.Width : 0) - errorCounterWidth, editor.TextViewMargin.XOffset + editor.LineHeight / 2);

			bool isEolSelected = editor.IsSomethingSelected && editor.SelectionMode != SelectionMode.Block ? editor.SelectionRange.Contains (lineSegment.Offset + lineSegment.Length) : false;

			int active = editor.Document.GetTextAt (lineSegment) == initialText ? 0 : 1;
			bool highlighted = active == 0 && isCaretInLine;

			// draw background
			if (!markerShouldDrawnAsHidden) {

				DrawRectangle (g, x, y, right, editor.LineHeight);
				g.Color = LineColor.Color;
				g.Fill ();

				if (metrics.Layout.StartSet || metrics.SelectionStart == metrics.TextEndOffset) {
					double startX;
					double endX;

					if (metrics.SelectionStart != metrics.TextEndOffset) {
						var start = metrics.Layout.Layout.IndexToPos ((int)metrics.Layout.SelectionStartIndex);
						startX = (int)(start.X / Pango.Scale.PangoScale);
						var end = metrics.Layout.Layout.IndexToPos ((int)metrics.Layout.SelectionEndIndex);
						endX = (int)(end.X / Pango.Scale.PangoScale);
					} else {
						startX = x2;
						endX = startX;
					}

					if (editor.MainSelection.SelectionMode == SelectionMode.Block && startX == endX)
						endX = startX + 2;
					startX += metrics.TextRenderStartPosition;
					endX += metrics.TextRenderStartPosition;
					startX = Math.Max (editor.TextViewMargin.XOffset, startX);
					// clip region to textviewmargin start
					if (isEolSelected)
						endX = editor.Allocation.Width + (int)editor.HAdjustment.Value;
					if (startX < endX) {
						DrawRectangle (g, startX, y, endX - startX, editor.LineHeight);
						g.Color = GetLineColor (highlighted, true);
						g.Fill ();
					}
				}
				DrawErrorMarkers (editor, g, metrics, y);
			}

			double y2 = y + 0.5;
			double y2Bottom = y2 + editor.LineHeight - 1;
			var selected = isEolSelected;
			var lineTextPx = editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition + metrics.Layout.PangoWidth / Pango.Scale.PangoScale;
			if (x2 < lineTextPx) 
				x2 = lineTextPx;

			if (editor.Options.ShowRuler) {
				double divider = Math.Max (editor.TextViewMargin.XOffset, x + editor.TextViewMargin.RulerX);
				if (divider >= x2) {
					g.MoveTo (new Cairo.PointD (divider + 0.5, y2));
					g.LineTo (new Cairo.PointD (divider + 0.5, y2Bottom));
					g.Color = GetLineColorBorder (highlighted, selected);
					g.Stroke ();
				}
			}

			g.RoundedRectangle (metrics.TextRenderEndPosition, y + 1, LayoutWidth + errorCounterWidth + editor.LineHeight, editor.LineHeight - 2, editor.LineHeight / 2 - 1);
			g.Color = TagColor.Color;
			g.Fill ();
		
			if (errorCounterWidth > 0 && errorCountLayout != null) {
				g.RoundedRectangle (metrics.TextRenderEndPosition + LayoutWidth + editor.LineHeight / 2, y + 2, errorCounterWidth, editor.LineHeight - 4, editor.LineHeight / 2 - 3);
				g.Color = TextColor.Color;
				g.Fill ();

				g.Save ();
				int ew, eh;
				errorCountLayout.GetPixelSize (out ew, out eh);
				g.Translate (metrics.TextRenderEndPosition + LayoutWidth +  + editor.LineHeight / 2 + (errorCounterWidth - ew) / 2, y + 1);
				g.Color = TagColor.Color;
				g.ShowLayout (errorCountLayout);
				g.Restore ();
			}

			var layout = layouts [0];
			g.Save ();
			g.Translate (metrics.TextRenderEndPosition + editor.LineHeight / 2, y + (editor.LineHeight - layout.Height) / 2 + layout.Height % 2);
			g.Color = TextColor.Color;
			g.ShowLayout (layout.Layout);
			g.Restore ();
			return true;
		}

		void DrawErrorMarkers (TextEditor editor, Cairo.Context g, LineMetrics metrics, double y)
		{
			uint curIndex = 0, byteIndex = 0;

			var o = metrics.LineSegment.Offset;

			foreach (var task in errors.Select (t => t.Task)) {
				int index = (int)metrics.Layout.TranslateToUTF8Index ((uint)(task.Column - 1), ref curIndex, ref byteIndex);
				var pos = metrics.Layout.Layout.IndexToPos (index);
				var co = o + task.Column - 1;
				g.Color = GetMarkerColor (false, metrics.SelectionStart <= co && co < metrics.SelectionEnd);
				g.MoveTo (
					metrics.TextRenderStartPosition + editor.TextViewMargin.TextStartPosition + pos.X / Pango.Scale.PangoScale,
					y + editor.LineHeight - 3
				);
				g.RelLineTo (3, 3);
				g.RelLineTo (-6, 0);
				g.ClosePath ();

				g.Fill ();
			}
		}
	}
}

