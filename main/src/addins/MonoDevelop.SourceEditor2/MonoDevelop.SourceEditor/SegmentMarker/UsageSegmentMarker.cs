//
// UsageSegmentMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor
{
	class UsageSegmentMarker : Mono.TextEditor.TextSegmentMarker
	{
		readonly Usage usage;

		public UsageSegmentMarker (Usage usage) : base (usage.Segment.Offset, usage.Segment.Length)
		{
			this.usage = usage;
		}
	}
}

//		public class UsageMarker : TextLineMarker
//		{
//			List<UsageSegment> usages = new List<UsageSegment> ();
//
//			public List<UsageSegment> Usages {
//				get { return usages; }
//			}
//
//			public bool Contains (int offset)
//			{
//				return usages.Any (u => u.TextSegment.Offset <= offset && offset <= u.TextSegment.EndOffset);
//			}
//
//			public override bool DrawBackground (TextEditor editor, Context cr, double y, LineMetrics metrics)
//			{
//				if (metrics.SelectionStart >= 0 || editor.CurrentMode is TextLinkEditMode || editor.TextViewMargin.SearchResultMatchCount > 0)
//					return false;
//				foreach (var usage in Usages) {
//					int markerStart = usage.TextSegment.Offset;
//					int markerEnd = usage.TextSegment.EndOffset;
//
//					if (markerEnd < metrics.TextStartOffset || markerStart > metrics.TextEndOffset) 
//						return false; 
//
//					double @from;
//					double to;
//
//					if (markerStart < metrics.TextStartOffset && metrics.TextEndOffset < markerEnd) {
//						@from = metrics.TextRenderStartPosition;
//						to = metrics.TextRenderEndPosition;
//					} else {
//						int start = metrics.TextStartOffset < markerStart ? markerStart : metrics.TextStartOffset;
//						int end = metrics.TextEndOffset < markerEnd ? metrics.TextEndOffset : markerEnd;
//
//						uint curIndex = 0, byteIndex = 0;
//						TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(start - metrics.TextStartOffset), ref curIndex, ref byteIndex);
//
//						int x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;
//
//						@from = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);
//
//						TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(end - metrics.TextStartOffset), ref curIndex, ref byteIndex);
//						x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;
//
//						to = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);
//					}
//
//					@from = Math.Max (@from, editor.TextViewMargin.XOffset);
//					to = Math.Max (to, editor.TextViewMargin.XOffset);
//					if (@from < to) {
//						Mono.TextEditor.Highlighting.AmbientColor colorStyle;
//						if ((usage.UsageType & ReferenceUsageType.Write) == ReferenceUsageType.Write) {
//							colorStyle = editor.ColorStyle.ChangingUsagesRectangle;
//						} else {
//							colorStyle = editor.ColorStyle.UsagesRectangle;
//						}
//
//						using (var lg = new LinearGradient (@from + 1, y + 1, to , y + editor.LineHeight)) {
//							lg.AddColorStop (0, colorStyle.Color);
//							lg.AddColorStop (1, colorStyle.SecondColor);
//							cr.SetSource (lg);
//							cr.RoundedRectangle (@from + 0.5, y + 1.5, to - @from - 1, editor.LineHeight - 2, editor.LineHeight / 4);
//							cr.FillPreserve ();
//						}
//
//						cr.SetSourceColor (colorStyle.BorderColor);
//						cr.Stroke ();
//					}
//				}
//				return true;
//			}
//		}

