//
// UrlMarker.cs
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
using Gtk;
using System.Collections.Immutable;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	
	enum UrlType {
		Unknown,
		Url,
		Email
	}


	class UrlMarker : TextLineMarker, IDisposable
	{
		string url;
		string style;
		int startColumn;
		int endColumn;
		UrlType urlType;
		TextDocument doc;
		Cairo.Color? color;
		
		public string Url {
			get {
				return url;
			}
		}

		public int StartColumn {
			get {
				return startColumn;
			}
		}

		public int EndColumn {
			get {
				return endColumn;
			}
		}

		public UrlType UrlType {
			get {
				return urlType;
			}
		}
		
		public UrlMarker (TextDocument doc, string url, UrlType urlType, string style, int startColumn, int endColumn)
		{
			this.doc = doc;
			this.url = url;
			this.urlType = urlType;
			this.style = style;
			this.startColumn = startColumn;
			this.endColumn = endColumn;
		}

		public void Dispose ()
		{
			doc = null;
		}
		
		public override void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			if (editor is null)
				throw new ArgumentNullException (nameof (editor));
			if (cr is null)
				throw new ArgumentNullException (nameof (cr));
			if (metrics is null)
				throw new ArgumentNullException (nameof (metrics));

			var startOffset = metrics.TextStartOffset;
			int endOffset = metrics.TextEndOffset;
			double startXPos = metrics.TextRenderStartPosition;
			double endXPos = metrics.TextRenderEndPosition;
			double y = metrics.LineYRenderStartPosition;
			var layoutWrapper = metrics.Layout;
			var layout = layoutWrapper?.Layout;
			if (layout == null || LineSegment == null)
				return;
			var lineOffset = LineSegment.Offset;
			int markerStart = lineOffset + startColumn;
			int markerEnd = lineOffset + endColumn;

			if (markerEnd < startOffset || markerStart > endOffset)
				return;

			var (x1, x2) = CalculateXPositions (startOffset, endOffset, startXPos, endXPos, layoutWrapper, layout, markerStart, markerEnd);
			DrawUnderline (editor, cr, y, layoutWrapper, markerStart, x1, x2);
		}

		static (double x1, double x2) CalculateXPositions (int startOffset, int endOffset, double startXPos, double endXPos, TextViewMargin.LayoutWrapper layoutWrapper, LayoutCache.LayoutProxy layout, int markerStart, int markerEnd)
		{
			if (layoutWrapper is null)
				throw new ArgumentNullException (nameof (layoutWrapper));
			if (layout is null)
				throw new ArgumentNullException (nameof (layout));

			double x1, x2;
			if (markerStart < startOffset && endOffset < markerEnd) {
				x1 = startXPos;
				x2 = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;

				uint curIndex = 0, byteIndex = 0;
				int x_pos = layout.IndexToPos ((int)layoutWrapper.TranslateToUTF8Index ((uint)(start - startOffset), ref curIndex, ref byteIndex)).X;

				x1 = startXPos + (int)(x_pos / Pango.Scale.PangoScale);

				x_pos = layout.IndexToPos ((int)layoutWrapper.TranslateToUTF8Index ((uint)(end - startOffset), ref curIndex, ref byteIndex)).X;

				x2 = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
			return (x1, x2);
		}

		void DrawUnderline (MonoTextEditor editor, Cairo.Context cr, double y, TextViewMargin.LayoutWrapper layoutWrapper, int markerStart, double x1, double x2)
		{
			if (editor is null)
				throw new ArgumentNullException (nameof (editor));
			if (cr is null) 
				throw new ArgumentNullException (nameof (cr));
			if (layoutWrapper is null)
				throw new ArgumentNullException (nameof (layoutWrapper));

			x1 = System.Math.Max (x1, editor.TextViewMargin.XOffset);
			x2 = System.Math.Max (x2, editor.TextViewMargin.XOffset);
			if (x1 < x2) {
				if (color == null) {
					foreach (var chunk in layoutWrapper.Chunks) {
						if (chunk.Contains (markerStart)) {
							color = editor.EditorTheme.GetForeground (editor.EditorTheme.GetChunkStyle (chunk.ScopeStack));
							break;
						}
					}
					if (color == null)
						color = editor.EditorTheme.GetForeground (editor.EditorTheme.GetChunkStyle (new ScopeStack (style)));
				}
				cr.DrawLine (color.Value, x1 + 0.5, y + editor.LineHeight - 1.5, x2 + 0.5, y + editor.LineHeight - 1.5);
			}
		}
	}
}
