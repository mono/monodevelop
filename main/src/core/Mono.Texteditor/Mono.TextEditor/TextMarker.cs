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
	public interface IExtendingTextMarker 
	{
		double GetLineHeight (TextEditor editor);
		void Draw (TextEditor editor, Cairo.Context cr, int lineNr, Cairo.Rectangle lineArea);
	}
	
	public interface IActionTextMarker
	{
		/// <returns>
		/// true, if the mouse press was handled - false otherwise.
		/// </returns>
		bool MousePressed (TextEditor editor, MarginMouseEventArgs args);
		
		void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextMarkerHoverResult result);
	}
	
	public class TextMarkerHoverResult 
	{
		public Gdk.Cursor Cursor { get; set; }
		public string TooltipMarkup { get; set; }
	}
	
	[Flags]
	public enum TextMarkerFlags
	{
		None           = 0,
		DrawsSelection = 1
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
		
		public virtual TextMarkerFlags Flags {
			get;
			set;
		}
		
		
		bool isVisible = true;
		public virtual bool IsVisible {
			get { return isVisible; }
			set { isVisible = value; }
		}

		public TextMarker ()
		{
		}
		
		public virtual void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
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
		
		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			int markerStart = line.Offset + startColumn;
			int markerEnd = line.Offset + endColumn;
	
			if (markerEnd < startOffset || markerStart > endOffset) 
				return; 
	
			double @from;
			double to;
	
			if (markerStart < startOffset && endOffset < markerEnd) {
				@from = startXPos;
				to = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;
				int x_pos = layout.IndexToPos (start - startOffset).X;
	
				@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
	
				x_pos = layout.IndexToPos (end - startOffset).X;
	
				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}
	
			@from = System.Math.Max (@from, editor.TextViewMargin.XOffset);
			to = System.Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from < to) {
				cr.DrawLine (selected ? editor.ColorStyle.Selection.CairoColor : editor.ColorStyle.GetChunkStyle (style).CairoColor, @from + 0.5, y + editor.LineHeight - 1.5, to + 0.5, y + editor.LineHeight - 1.5);
			}
		}
	}
	
	/// <summary>
	/// A specialized text marker interface to draw icons in the bookmark margin.
	/// </summary>
	public interface IIconBarMarker
	{
		void DrawIcon (TextEditor editor, Cairo.Context cr, LineSegment line, int lineNumber, double xPos, double yPos, double width, double height);
		void MousePress (MarginMouseEventArgs args);
		void MouseRelease (MarginMouseEventArgs args);
		void MouseHover (MarginMouseEventArgs args);
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
		bool DrawBackground (TextEditor Editor, Cairo.Context cr, TextViewMargin.LayoutWrapper layout, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg);
	}
	
	public class LineBackgroundMarker: TextMarker, IBackgroundMarker
	{
		Cairo.Color color;
		
		public LineBackgroundMarker (Cairo.Color color)
		{
			this.color = color;
		}
		
		public bool DrawBackground (TextEditor editor, Cairo.Context cr, TextViewMargin.LayoutWrapper layout, int selectionStart, int selectionEnd, int startOffset, int endOffset, double y, double startXPos, double endXPos, ref bool drawBg)
		{
			drawBg = false;
			if (selectionStart > 0)
				return true;
			cr.Color = color;
			cr.Rectangle (startXPos, y, endXPos - startXPos, editor.LineHeight);
			cr.Fill ();
			return true;
		}
	}
	
	public class UnderlineMarker: TextMarker
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
		
		public override void Draw (TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
		{
			int markerStart = LineSegment.Offset + System.Math.Max (StartCol - 1, 0);
			int markerEnd = LineSegment.Offset + (EndCol < 1 ? LineSegment.EditableLength : EndCol - 1);
			if (markerEnd < startOffset || markerStart > endOffset) 
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
			cr.Color = ColorName == null ? Color : editor.ColorStyle.GetColorFromDefinition (ColorName);
			Pango.CairoHelper.ShowErrorUnderline (cr, @from, y + editor.LineHeight - height, to - @from, height);
		}
	}

	public class StyleTextMarker: TextMarker
	{
		[Flags]
		public enum StyleFlag {
			None = 0,
			Color = 1,
			BackgroundColor = 2,
			Bold = 4,
			Italic = 8
		}
		
		StyleFlag includedStyles;
		Cairo.Color color;
		Cairo.Color backColor;
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
		
		public virtual Cairo.Color Color {
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
		
		public virtual Cairo.Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value;
				includedStyles |= StyleFlag.BackgroundColor;
			}
		}
		
		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			if (includedStyles == StyleFlag.None)
				return baseStyle;
			
			ChunkStyle style = new ChunkStyle (baseStyle);
			if ((includedStyles & StyleFlag.Color) != 0)
				style.Color = Mono.TextEditor.Highlighting.Style.ToGdkColor (Color);
		
			if ((includedStyles & StyleFlag.BackgroundColor) != 0) {
				style.ChunkProperties &= ~ChunkProperties.TransparentBackground;
				style.BackgroundColor = Mono.TextEditor.Highlighting.Style.ToGdkColor (BackgroundColor);
			}
			
			if ((includedStyles & StyleFlag.Bold) != 0)
				style.ChunkProperties |= ChunkProperties.Bold;
			
			if ((includedStyles & StyleFlag.Italic) != 0)
				style.ChunkProperties |= ChunkProperties.Italic;
			return style;
		}
	}
}
