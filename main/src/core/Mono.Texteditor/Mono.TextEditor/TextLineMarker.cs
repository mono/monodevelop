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
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public interface IExtendingTextLineMarker 
	{
		double GetLineHeight (TextEditor editor);
		void Draw (TextEditor editor, Cairo.Context cr, int lineNr, Cairo.Rectangle lineArea);
	}
	
	public interface IActionTextLineMarker
	{
		/// <returns>
		/// true, if the mouse press was handled - false otherwise.
		/// </returns>
		bool MousePressed (TextEditor editor, MarginMouseEventArgs args);
		
		void MouseHover (TextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result);
	}
	
	public class TextLineMarkerHoverResult 
	{
		public Gdk.Cursor Cursor { get; set; }
		public string TooltipMarkup { get; set; }
	}
	
	[Flags]
	public enum TextLineMarkerFlags
	{
		None           = 0,
		DrawsSelection = 1
	}
	
	public class TextLineMarker
	{
		DocumentLine lineSegment;
		
		public DocumentLine LineSegment {
			get {
				return lineSegment;
			}
			set {
				lineSegment = value;
			}
		}
		
		public virtual TextLineMarkerFlags Flags {
			get;
			set;
		}
		
		
		bool isVisible = true;
		public virtual bool IsVisible {
			get { return isVisible; }
			set { isVisible = value; }
		}

		public TextLineMarker ()
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
	
	public class UrlMarker : TextLineMarker, IDisposable
	{
		string url;
		string style;
		int startColumn;
		int endColumn;
		DocumentLine line;
		UrlType urlType;
		TextDocument doc;
		
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
		
		public UrlMarker (TextDocument doc, DocumentLine line, string url, UrlType urlType, string style, int startColumn, int endColumn)
		{
			this.doc = doc;
			this.line = line;
			this.url = url;
			this.urlType = urlType;
			this.style = style;
			this.startColumn = startColumn;
			this.endColumn = endColumn;
			doc.LineChanged += HandleDocLineChanged;
		}
		
		void HandleDocLineChanged (object sender, LineEventArgs e)
		{
			if (line == e.Line)
				doc.RemoveMarker (this);
		}
		
		public void Dispose ()
		{
			doc.LineChanged -= HandleDocLineChanged;
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
				cr.DrawLine (selected ? editor.ColorStyle.SelectedText.Foreground : editor.ColorStyle.GetForeground (editor.ColorStyle.GetChunkStyle (style)), @from + 0.5, y + editor.LineHeight - 1.5, to + 0.5, y + editor.LineHeight - 1.5);
			}
		}
	}
	
	/// <summary>
	/// A specialized text marker interface to draw icons in the bookmark margin.
	/// </summary>
	public interface IIconBarMarker
	{
		void DrawIcon (TextEditor editor, Cairo.Context cr, DocumentLine line, int lineNumber, double xPos, double yPos, double width, double height);
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

	public interface IGutterMarker
	{
		void DrawLineNumber (TextEditor editor, double marginWidth, Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight);
	}
	
	public class LineBackgroundMarker: TextLineMarker, IBackgroundMarker
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
	
	public class UnderlineMarker: TextLineMarker
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
		
		void InternalDraw (int markerStart, int markerEnd, TextEditor editor, Cairo.Context cr, Pango.Layout layout, bool selected, int startOffset, int endOffset, double y, double startXPos, double endXPos)
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
			if (selected) {
				cr.Color = editor.ColorStyle.SelectedText.Foreground;
			} else {
				cr.Color = ColorName == null ? Color : editor.ColorStyle.GetChunkStyle (ColorName).Foreground;
			}
			if (Wave) {	
				Pango.CairoHelper.ShowErrorUnderline (cr, @from, y + editor.LineHeight - height, to - @from, height);
			} else {
				cr.LineWidth = 1;
				cr.MoveTo (@from, y + editor.LineHeight - 1.5);
				cr.LineTo (to, y + editor.LineHeight - 1.5);
				cr.Stroke ();
			}
		}
	}

	public class StyleTextLineMarker: TextLineMarker
	{
		[Flags]
		public enum StyleFlag {
			None = 0,
			Color = 1,
			BackgroundColor = 2,
			Bold = 4,
			Italic = 8
		}
		
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
				IncludedStyles |= StyleFlag.Italic;
			}
		}
		
		public virtual StyleFlag IncludedStyles {
			get;
			set;
		}
		
		public virtual Cairo.Color Color {
			get {
				return color;
			}
			set {
				color = value;
				IncludedStyles |= StyleFlag.Color;
			}
		}
		
		public bool Bold {
			get {
				return bold;
			}
			set {
				bold = value;
				IncludedStyles |= StyleFlag.Bold;
			}
		}
		
		public virtual Cairo.Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value;
				IncludedStyles |= StyleFlag.BackgroundColor;
			}
		}
		
		protected virtual ChunkStyle CreateStyle (ChunkStyle baseStyle, Cairo.Color color, Cairo.Color bgColor)
		{
			var style = new ChunkStyle (baseStyle);
			if ((IncludedStyles & StyleFlag.Color) != 0)
				style.Foreground = color;
			
			if ((IncludedStyles & StyleFlag.BackgroundColor) != 0) {
				style.Background = bgColor;
			}
			
			if ((IncludedStyles & StyleFlag.Bold) != 0)
				style.FontWeight = Xwt.Drawing.FontWeight.Bold;
			
			if ((IncludedStyles & StyleFlag.Italic) != 0)
				style.FontStyle = Xwt.Drawing.FontStyle.Italic;
			return style;
		}
		
		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			if (baseStyle == null || IncludedStyles == StyleFlag.None)
				return baseStyle;
			
			return CreateStyle (baseStyle, Color, BackgroundColor);
		}
	}
}
