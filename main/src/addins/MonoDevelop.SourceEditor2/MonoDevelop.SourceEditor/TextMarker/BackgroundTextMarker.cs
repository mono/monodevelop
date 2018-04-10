//
// BackgroundTextMarker.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using Cairo;

namespace MonoDevelop.SourceEditor
{
	class BackgroundTextMarker : TextSegmentMarker, IGenericTextSegmentMarker
	{
		HslColor? color;

		TextSegmentMarkerEffect IGenericTextSegmentMarker.Effect {
			get => TextSegmentMarkerEffect.Background;
		}

		public HslColor Color { get => color.HasValue ? color.Value : default (HslColor); set => color = value; }

#pragma warning disable 0067
		public event EventHandler<TextMarkerMouseEventArgs> MousePressed;

		public event EventHandler<TextMarkerMouseEventArgs> MouseHover;
#pragma warning restore 0067

		object ITextSegmentMarker.Tag { get; set; }

		public BackgroundTextMarker (ISegment segment, HslColor? color = null) : base (segment)
		{
			this.color = color;
		}

		public override void DrawBackground (MonoTextEditor editor, Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			if (!color.HasValue)
				return;
			int markerStart = Segment.Offset;
			int markerEnd = Segment.EndOffset;

			if (markerEnd < startOffset || markerStart > endOffset)
				return;

			double @from;
			double to;
			var startXPos = metrics.TextRenderStartPosition;
			var endXPos = metrics.TextRenderEndPosition;
			var y = metrics.LineYRenderStartPosition;
			if (markerStart < startOffset && endOffset < markerEnd) {
				@from = startXPos;
				to = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;

				uint curIndex = 0, byteIndex = 0;
				TextViewMargin.TranslateToUTF8Index (metrics.Layout.Text, (uint)(start - startOffset), ref curIndex, ref byteIndex);

				int x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);

				TextViewMargin.TranslateToUTF8Index (metrics.Layout.Text, (uint)(end - startOffset), ref curIndex, ref byteIndex);
				x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}

			@from = Math.Max (@from, editor.TextViewMargin.XOffset);
			to = Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from < to) {
				cr.SetSourceColor (Color);
				cr.RoundedRectangle (@from - 0.5, y + 0.5, to - @from + 1, editor.LineHeight - 1, 2);
				cr.FillPreserve ();
			}
		}
	}
}