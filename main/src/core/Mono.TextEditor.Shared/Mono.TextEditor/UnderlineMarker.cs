//
// UnderlineMarker.cs
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

using System;
using Gdk;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	class UnderlineMarker: TextLineMarker
	{
		protected UnderlineMarker ()
		{}
		
		public UnderlineMarker (string colorName, int start, int end)
		{
			this.ColorName = colorName;
			this.StartCol = start;
			this.EndCol = end;
			this.Wave = true;
		}
		public UnderlineMarker (Cairo.Color color, int start, int end)
		{
			this.Color = color;
			this.StartCol = start;
			this.EndCol = end;
			this.Wave = false;
		}
		
		public string ColorName { get; set; }
		public Cairo.Color Color { get; set; }
		public int StartCol { get; set; }
		public int EndCol { get; set; }
		public bool Wave { get; set; }
		
		public override void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			var startOffset = metrics.TextStartOffset;
			int endOffset = metrics.TextEndOffset;
			double startXPos = metrics.TextRenderStartPosition;
			double endXPos = metrics.TextRenderEndPosition;
			double y = metrics.LineYRenderStartPosition;
			var layout = metrics.Layout.Layout;

			int markerStart = LineSegment.Offset + System.Math.Max (StartCol - 1, 0);
			int markerEnd = LineSegment.Offset + (EndCol < 1 ? LineSegment.Length : EndCol - 1);
			if (markerEnd < startOffset || markerStart > endOffset) 
				return; 
			
			if (editor.IsSomethingSelected) {
				var range = editor.SelectionRange;
				if (range.Contains (markerStart)) {
					int end = System.Math.Min (markerEnd, range.EndOffset);
					InternalDraw (markerStart, end, editor, cr, layout, true, startOffset, endOffset, y, startXPos, endXPos);
					InternalDraw (range.EndOffset, markerEnd, editor, cr, layout, false, startOffset, endOffset, y, startXPos, endXPos);
					return;
				}
				if (range.Contains (markerEnd)) {
					InternalDraw (markerStart, range.Offset, editor, cr, layout, false, startOffset, endOffset, y, startXPos, endXPos);
					InternalDraw (range.Offset, markerEnd, editor, cr, layout, true, startOffset, endOffset, y, startXPos, endXPos);
					return;
				}
				if (markerStart <= range.Offset && range.EndOffset <= markerEnd) {
					InternalDraw (markerStart, range.Offset, editor, cr, layout, false, startOffset, endOffset, y, startXPos, endXPos);
					InternalDraw (range.Offset, range.EndOffset, editor, cr, layout, true, startOffset, endOffset, y, startXPos, endXPos);
					InternalDraw (range.EndOffset, markerEnd, editor, cr, layout, false, startOffset, endOffset, y, startXPos, endXPos);
					return;
				}
				
			}
			
			InternalDraw (markerStart, markerEnd, editor, cr, layout, false, startOffset, endOffset, y, startXPos, endXPos);
		}
		
		void InternalDraw (int markerStart, int markerEnd, MonoTextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			if (markerStart >= markerEnd)
				return;
			double @from;
			double to;
			if (markerStart < startOffset && endOffset < markerEnd) {
				@from = startXPos;
				to = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;
				int /*lineNr,*/ x_pos;
				
				x_pos = layout.IndexToPos (start - startOffset).X;
				@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
	
				x_pos = layout.IndexToPos (end - startOffset).X;
	
				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
			@from = System.Math.Max (@from, editor.TextViewMargin.XOffset);
			to = System.Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from >= to) {
				return;
			}
			double height = editor.LineHeight / 5;
			// TODO : EditorTheme : Does that look ok ?
			// if (selected) {
			//	cr.SetSourceColor (editor.EditorTheme.SelectedText.Foreground);
			//} else {
			if (ColorName == null) {
				cr.SetSourceColor (Color);

			} else {
				HslColor color;
				editor.EditorTheme.TryGetColor (ColorName, out color);
				cr.SetSourceColor (color);
			}
			//}
			if (Wave) {	
				Pango.CairoHelper.ShowErrorUnderline (cr, @from, y + editor.TextViewMargin.UnderlinePosition, to - @from, height);
			} else {
				cr.LineWidth = editor.TextViewMargin.UnderLineThickness;

				cr.MoveTo (@from, y + editor.TextViewMargin.UnderlinePosition - 0.5);
				cr.LineTo (to, y + editor.TextViewMargin.UnderlinePosition - 0.5);
				cr.Stroke ();
			}
		}
	}
	
}
