// IconMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Gtk;
using Gdk;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Components;

namespace Mono.TextEditor
{
	class IconMargin : Margin
	{
		MonoTextEditor editor;
		Cairo.Color backgroundColor, separatorColor;
		const int marginWidth = 22;
		
		public IconMargin (MonoTextEditor editor)
		{
			this.editor = editor;
		}
		
		public override double Width {
			get {
				return marginWidth;
			}
		}
		
		internal protected override void OptionsChanged ()
		{
			backgroundColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.IndicatorMargin);
			separatorColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.IndicatorMarginSeparator);
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null) 
						marginMarker.InformMousePress (editor, this, args);
				}
			}
		}
		
		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			base.MouseReleased (args);
			
			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null) 
						marginMarker.InformMouseRelease (editor, this, args);
				}
			}
		}

		Gdk.Cursor textLinkCursor = new Gdk.Cursor (Gdk.CursorType.Hand1);

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			cursor = textLinkCursor;
			args.Editor.TooltipText = null;
			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null) 
						marginMarker.InformMouseHover (editor, this, args);
				}
			}
		}

		internal protected override void MouseLeft ()
		{
			if (!string.IsNullOrEmpty (editor.TooltipText))
				editor.TooltipText = null;
			base.MouseLeft ();
		}

		internal protected override void Draw (Cairo.Context ctx, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			bool backgroundIsDrawn = false;
			if (lineSegment != null) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null && marginMarker.CanDrawBackground (this)) {
						backgroundIsDrawn = marginMarker.DrawBackground (editor, ctx, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
					}
				}
			}

			if (!backgroundIsDrawn) {
				ctx.Rectangle (x, y, Width, lineHeight);
				ctx.SetSourceColor (backgroundColor);
				ctx.Fill ();
				
				ctx.MoveTo (x + Width - 0.5, y);
				ctx.LineTo (x + Width - 0.5, y + lineHeight);
				ctx.SetSourceColor (separatorColor);
				ctx.Stroke ();
			}

			if (lineSegment != null && line <= editor.Document.LineCount) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null && marginMarker.CanDrawForeground (this)) {
						marginMarker.DrawForeground (editor, ctx, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
					}
				}
				if (DrawEvent != null) 
					DrawEvent (this, new BookmarkMarginDrawEventArgs (editor, ctx, lineSegment, line, x, y));
			}
		}
		
		public EventHandler<BookmarkMarginDrawEventArgs> DrawEvent;
	}
	
	class BookmarkMarginDrawEventArgs : EventArgs
	{
		public MonoTextEditor Editor {
			get;
			private set;
		}

		public Cairo.Context Context {
			get;
			private set;
		}

		public int Line {
			get;
			private set;
		}

		public double X {
			get;
			private set;
		}

		public double Y {
			get;
			private set;
		}

		public DocumentLine LineSegment {
			get;
			private set;
		}
		
		public BookmarkMarginDrawEventArgs (MonoTextEditor editor, Cairo.Context context, DocumentLine line, int lineNumber, double xPos, double yPos)
		{
			this.Editor = editor;
			this.Context    = context;
			this.LineSegment = line;
			this.Line   = lineNumber;
			this.X      = xPos;
			this.Y      = yPos;
		}
	}
	
}
