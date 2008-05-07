// TextMarker.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor
{
	public enum UrlType {
		Unknown,
		Url,
		Email
	}
	
	public class UrlMarker : TextMarker
	{
		string url;
		string style;
		int startColumn;
		int endColumn;
		LineSegment line;
		UrlType urlType;
		
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
		
		public UrlMarker (LineSegment line, string url, UrlType urlType, string style, int startColumn, int endColumn)
		{
			this.line        = line;
			this.url         = url;
			this.urlType     = urlType;
			this.style       = style;
			this.startColumn = startColumn;
			this.endColumn   = endColumn;
		}
		
		public override void Draw (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos)
		{
			int markerStart = line.Offset + startColumn;
			int markerEnd   = line.Offset + endColumn;
			if (markerEnd < startOffset || markerStart > endOffset)
				return;
			    
			int from;
			int to;
			
			if (markerStart < startOffset && endOffset < markerEnd) {
				from = startXPos;
				to   = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end   = endOffset < markerEnd ? endOffset : markerEnd;
				from = startXPos + editor.GetWidth (editor.Document.GetTextAt (startOffset, start - startOffset));
				to   = startXPos + editor.GetWidth (editor.Document.GetTextAt (startOffset, end - startOffset));
			}
 			from = System.Math.Max (from, editor.TextViewMargin.XOffset);
 			to   = System.Math.Max (to, editor.TextViewMargin.XOffset);
			if (from < to) {
				using (Gdk.GC gc = new Gdk.GC (win)) {
					gc.RgbFgColor = selected ? editor.ColorStyle.SelectedFg : editor.ColorStyle.GetChunkStyle (style).Color;
					win.DrawLine (gc, from, y + editor.LineHeight - 1, to, y + editor.LineHeight - 1);
				}
			}
		}
	}
	
	/// <summary>
	/// A specialized text marker interface to draw icons in the bookmark margin.
	/// </summary>
	public interface IIconBarMarker
	{
		void DrawIcon (TextEditor editor, Gdk.Drawable win, LineSegment line, int lineNumber, int xPos, int yPos);
	}
	
	/// <summary>
	/// A specialized interface to draw text backgrounds.
	/// </summary>
	public interface IBackgroundMarker
	{
		/// <summary>
		/// Draws the backround of a line part.
		/// </summary>
		/// <returns>
		/// true, when the text view should draw the text, false when the text view should not draw the text.
		/// </returns>
		bool DrawBackground (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos);
	}
	
	public class TextMarker
	{
		LineSegment lineSegment;
		
		public LineSegment LineSegment {
			get {
				return lineSegment;
			}
			set {
				lineSegment = value;
			}
		}
		
		public virtual void Draw (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos)
		{
			/*using (Gdk.GC gc = new Gdk.GC (win)) {
				gc.RgbFgColor = new Color (255, 0, 0);
				int drawY    = y + editor.LineHeight - 1;
				const int length = 6;
				const int height = 2;
				for (int i = startXPos; i < endXPos; i += length) {
					win.DrawLine (gc, i, drawY, i + length / 2, drawY - height);
					win.DrawLine (gc, i + length / 2, drawY - height, i + length, drawY);
				}
			}*/
		}
	}
}
