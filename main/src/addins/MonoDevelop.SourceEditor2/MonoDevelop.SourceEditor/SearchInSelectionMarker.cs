using Mono.TextEditor;
using System;

namespace MonoDevelop.SourceEditor
{
	class SearchInSelectionMarker : TextSegmentMarker
	{

		public SearchInSelectionMarker (TextSegment textSegment) : base (textSegment)
		{
		}

		public override void DrawBackground (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			int markerStart = Offset;
			int markerEnd = EndOffset;

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
				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(start - startOffset), ref curIndex, ref byteIndex);

				int x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;

				@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);

				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(end - startOffset), ref curIndex, ref byteIndex);
				x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;

				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}

			@from = Math.Max (@from, editor.TextViewMargin.XOffset);
			to = Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from <= to) {
				if (metrics.TextEndOffset < markerEnd)
					to = metrics.WholeLineWidth + metrics.TextRenderStartPosition;
				var c1 = editor.Options.GetColorStyle ().PlainText.Background;
				var c2 = editor.Options.GetColorStyle ().SelectedText.Background;
				cr.SetSourceColor (new Cairo.Color ((c1.R + c2.R) / 2, (c1.G + c2.G) / 2, (c1.B + c2.B) / 2));
				cr.Rectangle (@from, y, to - @from, metrics.LineHeight);
				cr.Fill ();
			}
		}
	}
}