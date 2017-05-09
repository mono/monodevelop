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
	interface IExtendingTextLineMarker
	{
		bool IsSpaceAbove { get; }

		double GetLineHeight (MonoTextEditor editor);
		void Draw (MonoTextEditor editor, Cairo.Context cr, int lineNr, Cairo.Rectangle lineArea);
	}

	interface IActionTextLineMarker
	{
		/// <returns>
		/// true, if the mouse press was handled - false otherwise.
		/// </returns>
		bool MousePressed (MonoTextEditor editor, MarginMouseEventArgs args);
		bool MouseReleased (MonoTextEditor editor, MarginMouseEventArgs args);

		void MouseHover (MonoTextEditor editor, MarginMouseEventArgs args, TextLineMarkerHoverResult result);
	}

	class TextLineMarkerHoverResult
	{
		bool isCursorSet;

		public bool HasCursor {
			get { return isCursorSet; }
		}

		Gdk.Cursor cursor;
		public Gdk.Cursor Cursor {
			get {
				return cursor;
			}
			set {
				cursor = value;
				isCursorSet = true;
			}
		}
		public string TooltipMarkup { get; set; }
	}

	[Flags]
	public enum TextLineMarkerFlags
	{
		None = 0,
		DrawsSelection = 1
	}

	class LineMetrics
	{
		public DocumentLine LineSegment { get; internal set; }
		public TextViewMargin.LayoutWrapper Layout { get; internal set; }

		public int SelectionStart { get; internal set; }
		public int SelectionEnd { get; internal set; }

		public int TextStartOffset { get; internal set; }
		public int TextEndOffset { get; internal set; }

		public double TextRenderStartPosition { get; internal set; }
		public double TextRenderEndPosition { get; internal set; }

		public double LineHeight { get; internal set; }

		public double WholeLineWidth { get; internal set; }

		public double LineYRenderStartPosition { get; internal set; }
	}

	class EndOfLineMetrics
	{
		public DocumentLine LineSegment { get; internal set; }
		public double TextRenderEndPosition { get; internal set; }
		public double LineHeight { get; internal set; }
		public double LineYRenderStartPosition { get; internal set; }
	}

	class TextLineMarker
	{
		internal TextDocument.DocumentLineTextSegmentMarker parent { get; set; }

		public DocumentLine LineSegment {
			get {
				return parent?.LineSegment;
			}
		}

		public virtual TextLineMarkerFlags Flags {
			get;
			set;
		}

		public object Tag {
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

		public virtual void Draw (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
		}

		internal virtual MonoDevelop.Ide.Editor.Highlighting.ChunkStyle GetStyle (MonoDevelop.Ide.Editor.Highlighting.ChunkStyle baseStyle)
		{
			return baseStyle;
		}

		/// <summary>
		/// Draws the background of the text.
		/// </summary>
		/// <returns><c>true</c>, if background was drawn, <c>false</c> otherwise.</returns>
		/// <param name="editor">The editor.</param>
		/// <param name="cr">The cairo context.</param>
		/// <param name="metrics">The line metrics.</param>
		public virtual bool DrawBackground (MonoTextEditor editor, Cairo.Context cr, LineMetrics metrics)
		{
			return false;
		}

		/// <summary>
		/// Is used to draw in the area after the visible text.
		/// </summary>
		public virtual void DrawAfterEol (MonoTextEditor textEditor, Cairo.Context cr, EndOfLineMetrics metrics)
		{
		}
	}

}
