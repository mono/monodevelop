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
using MonoDevelop.Components.AtkCocoaHelper;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.SourceEditor;

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
						var metrics = new MarginDrawMetrics(this, area, lineSegment, line, x, y, lineHeight);
						marginMarker.DrawForeground (editor, ctx, metrics);
						marginMarker.MarginDrawMetrics = metrics;
					}
				}
				if (DrawEvent != null) 
					DrawEvent (this, new BookmarkMarginDrawEventArgs (editor, ctx, lineSegment, line, x, y));
			}
			UpdateAccessibility();
		}

		void UpdateAccessibility()
		{
			var children = GetAccessibleChildren();
			Accessible.ResetAccessibilityChildren();
			Accessible.AddAccessibleChildren(children);
		}

		Button fakeParent = new Button(); //margin can't be widget, and gtkparent can't be null  
		IEnumerable<AccessibilityElementProxy> GetAccessibleChildren()
		{
			var result = editor.Document.Lines
							   .SelectMany(line => editor.Document.GetMarkers(line))
							   .OfType<MarginMarker>()
							   .Where(mr => !(mr is HoverDebugIconMarker))
			                   .Select(mark => new MarkerAccessible(fakeParent, mark).Accessible);
			return result;
		}

		class MarkerAccessible
		{
			AccessibilityElementProxy accessible;
			public AccessibilityElementProxy Accessible => accessible ?? (accessible = AccessibilityElementProxy.ButtonElementProxy());

			public MarkerAccessible(Widget parent, MarginMarker marker)
			{
				Accessible.GtkParent = parent;
				Accessible.Help = GettextCatalog.GetString("Marker");
				var metrics = marker.MarginDrawMetrics;
				if (metrics == null)
					return;

				var markerX = (int)metrics.X;
				var markerY = (int)metrics.Y;
				var markerWidth = (int)metrics.Width;
				var markerHeight = (int)metrics.Height;
				var marginHeight = metrics.Margin.RectInParent.Height;
				var mirroredY = marginHeight - (int)metrics.Height - (int)metrics.Y;

				Accessible.Title = GettextCatalog.GetString("Marker at line {0}", metrics.LineNumber);
				Accessible.FrameInParent = new Rectangle(markerX, mirroredY, markerWidth, markerHeight);
				Accessible.FrameInGtkParent = new Rectangle(markerX, markerY, markerWidth, markerHeight);
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
