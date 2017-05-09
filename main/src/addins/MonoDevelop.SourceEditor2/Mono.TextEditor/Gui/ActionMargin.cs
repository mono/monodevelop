//
// ActionMargin.cs
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
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class ActionMargin : Margin
	{
		readonly MonoTextEditor editor;

		double marginWidth;
		public override double Width {
			get {
				return marginWidth;
			}
		}

		public ActionMargin (MonoTextEditor editor)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			this.editor = editor;
			marginWidth = 20;
			IsVisible = false;
			this.editor.Caret.PositionChanged += HandlePositionChanged;;
		}

		public override void Dispose ()
		{
			editor.Caret.PositionChanged -= HandlePositionChanged;;
			base.Dispose ();
		}

		void HandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}

		void DrawMarginBackground (Cairo.Context cr, int line, double x, double y, double lineHeight)
		{
			if (editor.Caret.Line == line) {
				editor.TextViewMargin.DrawCaretLineMarker (cr, x, y, Width, lineHeight);
				return;
			}
			cr.Rectangle (x, y, Width, lineHeight);

			cr.SetSourceColor (SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbersBackground));
			cr.Fill ();
		}

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			var marker = lineSegment != null ? (MarginMarker)editor.Document.GetMarkers (lineSegment).FirstOrDefault (m => m is MarginMarker && ((MarginMarker)m).CanDraw (this)) : null;
			bool drawBackground = true;
			if (marker != null && marker.CanDrawBackground (this))
				drawBackground = !marker.DrawBackground (editor, cr, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));

			if (drawBackground) 
				DrawMarginBackground (cr, line, x, y, lineHeight);

			if (marker != null && marker.CanDrawForeground (this))
				marker.DrawForeground (editor, cr, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
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

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
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
	}
}

