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
		}
		
		public virtual ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			return baseStyle;
		}
	}
	
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
		void DrawIcon (TextEditor editor, Gdk.Drawable win, LineSegment line, int lineNumber, int xPos, int yPos, int width, int height);
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
	
	public class LineBackgroundMarker: TextMarker, IBackgroundMarker
	{
		Gdk.Color color;
		
		public LineBackgroundMarker (Gdk.Color color)
		{
			this.color = color;
		}
		
		public bool DrawBackground (TextEditor editor, Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos)
		{
			if (selected)
				return true;
			using (Gdk.GC gc = new Gdk.GC (win)) {
				gc.RgbFgColor = color;
				win.DrawRectangle (gc, true, startXPos, y, endXPos - startXPos, editor.LineHeight);
			}
			return true;
		}
	}
	
	public class UnderlineMarker: TextMarker
	{
		Gdk.Color   color = new Color (255, 0, 0);

		public Color Color {
			get {
				return color;
			}
			set {
				color = value;
			}
		}
		
		public override void Draw (TextEditor editor, Gdk.Drawable win, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos)
		{
			using (Gdk.GC gc = new Gdk.GC (win)) {
				gc.RgbFgColor = color;
				int drawY    = y + editor.LineHeight - 1;
				const int length = 6;
				const int height = 2;
				startXPos = System.Math.Max (startXPos, editor.TextViewMargin.XOffset);
				for (int i = startXPos; i < endXPos; i += length) {
					win.DrawLine (gc, i, drawY, i + length / 2, drawY - height);
					win.DrawLine (gc, i + length / 2, drawY - height, i + length, drawY);
				}
			}
		}
	}
	
	public class StyleTextMarker: TextMarker
	{
		[Flags]
		public enum StyleFlag {
			None = 0,
			Color = 1,
			BackroundColor = 2,
			Bold = 4,
			Italic = 8
		}
		
		StyleFlag includedStyles;
		Gdk.Color color;
		Gdk.Color backColor;
		bool bold;
		bool italic;
		
		public bool Italic {
			get {
				return italic;
			}
			set {
				italic = value;
				includedStyles |= StyleFlag.Italic;
			}
		}
		
		public StyleFlag IncludedStyles {
			get {
				return includedStyles;
			}
			set {
				includedStyles = value;
			}
		}
		
		public virtual Color Color {
			get {
				return color;
			}
			set {
				color = value;
				includedStyles |= StyleFlag.Color;
			}
		}
		
		public bool Bold {
			get {
				return bold;
			}
			set {
				bold = value;
				includedStyles |= StyleFlag.Bold;
			}
		}
		
		public virtual Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value;
				includedStyles |= StyleFlag.BackroundColor;
			}
		}
		
		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			if (includedStyles == StyleFlag.None)
				return baseStyle;
			
			ChunkStyle style = new ChunkStyle (baseStyle);
			if ((includedStyles & StyleFlag.Color) != 0)
				style.Color = Color;
			if ((includedStyles & StyleFlag.BackroundColor) != 0)
				style.BackgroundColor = BackgroundColor;
			if ((includedStyles & StyleFlag.Bold) != 0)
				style.Bold = bold;
			if ((includedStyles & StyleFlag.Italic) != 0)
				style.Italic = italic;
			return style;
		}
	}
}
