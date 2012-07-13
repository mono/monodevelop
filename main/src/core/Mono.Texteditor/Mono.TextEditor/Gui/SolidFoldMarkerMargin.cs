// SolidFoldMarkerMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using Gtk;
using System.Timers;

namespace Mono.TextEditor
{
	public class SolidFoldMarkerMargin : Margin
	{
		TextEditor editor;
		DocumentLine lineHover;
		Pango.Layout layout;
		
		double foldSegmentSize = 8;
		double marginWidth;
		public override double Width {
			get {
				return marginWidth;
			}
		}
		
		bool isInCodeFocusMode;
		public bool IsInCodeFocusMode {
			get { 
				return isInCodeFocusMode; 
			}
			set {
				isInCodeFocusMode = value; 
				if (!isInCodeFocusMode) {
					RemoveBackgroundRenderer ();
				} else {
					foldings = null;
					HandleEditorCaretPositionChanged (null, null);
				}
			}
		}
		
		public SolidFoldMarkerMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = PangoUtil.CreateLayout (editor);
			editor.Caret.PositionChanged += HandleEditorCaretPositionChanged;
			editor.Document.FoldTreeUpdated += HandleEditorDocumentFoldTreeUpdated;
		}

		void HandleEditorDocumentFoldTreeUpdated (object sender, EventArgs e)
		{
			editor.RedrawMargin (this);
		}

		void HandleEditorCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!IsInCodeFocusMode) 
				return;
			DocumentLine lineSegment = editor.Document.GetLine (editor.Caret.Line);
			if (lineSegment == null) {
				RemoveBackgroundRenderer ();
				return;
			}
			
			IEnumerable<FoldSegment> newFoldings = editor.Document.GetFoldingContaining (lineSegment);
			if (newFoldings == null) {
				RemoveBackgroundRenderer ();
				return;
			}
			
			bool areEqual = foldings != null;
			
			if (areEqual && foldings.Count () != newFoldings.Count ())
				areEqual = false;
			if (areEqual) {
				List<FoldSegment> list1 = new List<FoldSegment> (foldings);
				List<FoldSegment> list2 = new List<FoldSegment> (newFoldings);
				for (int i = 0; i < list1.Count; i++) {
					if (list1[i] != list2[i]) {
						areEqual = false;
						break;
					}
				}
			}
			
			if (!areEqual) {
				foldings = newFoldings;
				StopTimer ();
			}
		}

		FoldSegment GetSelectedSegment (int lineNumber)
		{
			FoldSegment selectedSegment = null;
			foreach (var segment in editor.Document.GetFoldingContaining (lineNumber)) {
				if (selectedSegment == null || selectedSegment.Contains (segment.Offset))
					selectedSegment = segment;
			}
			return selectedSegment;
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			if (args.LineSegment == null)
				return;

			var selectedSegment = GetSelectedSegment (args.LineNumber);

			if (selectedSegment != null) {
				selectedSegment.IsFolded = !selectedSegment.IsFolded; 
				editor.SetAdjustments ();
				editor.Caret.MoveCaretBeforeFoldings ();
			}
		}

		FoldSegment hoverSegment;
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			
			DocumentLine lineSegment = null;
			if (args.LineSegment != null) {
				lineSegment = args.LineSegment;
				if (lineHover != lineSegment) {
					lineHover = lineSegment;
					editor.RedrawMargin (this);
				}
			} 
			lineHover = lineSegment;

			hoverSegment = GetSelectedSegment (args.LineNumber);
			bool found = hoverSegment != null;

			StopTimer ();
			if (found) {
				var list = new List<FoldSegment>(editor.Document.GetFoldingContaining (lineSegment));
				list.Sort ((x, y) => x.Offset.CompareTo (y.Offset));
				foldings = list;
				if (editor.TextViewMargin.BackgroundRenderer == null) {
					timerId = GLib.Timeout.Add (150, SetBackgroundRenderer);
				} else {
					SetBackgroundRenderer ();
				}
			} else {
				RemoveBackgroundRenderer ();
			}
		}
		
		bool SetBackgroundRenderer ()
		{
			editor.TextViewMargin.DisposeLayoutDict ();

			var disposable = editor.TextViewMargin.BackgroundRenderer as IDisposable;
			if (disposable != null)
				disposable.Dispose ();
			
			editor.TextViewMargin.BackgroundRenderer = new FoldingScreenbackgroundRenderer (editor, foldings);
			editor.QueueDraw ();
			timerId = 0;
			return false;
		}
		
		void StopTimer ()
		{
			if (timerId != 0) {
				GLib.Source.Remove (timerId);
				timerId = 0;
			}
		}
		
		uint timerId;
		IEnumerable<FoldSegment> foldings;
		void RemoveBackgroundRenderer ()
		{
			if (editor.TextViewMargin.BackgroundRenderer != null) {
				editor.TextViewMargin.BackgroundRenderer = null;
				editor.QueueDraw ();
			}
		}
		
		internal protected override void MouseLeft ()
		{
			base.MouseLeft ();
			hoverSegment = null;
			if (lineHover != null) {
				lineHover = null;
				editor.RedrawMargin (this);
			}
			StopTimer ();
			RemoveBackgroundRenderer ();
		}
		
		internal protected override void OptionsChanged ()
		{
			foldBgGC = editor.ColorStyle.FoldLine.CairoBackgroundColor;

			foldLineHighlightedGCBg = editor.ColorStyle.FoldMargin.CairoBackgroundColor;
			foldLineHighlightedGC = editor.ColorStyle.FoldMargin.CairoColor;

			lineStateChangedGC = editor.ColorStyle.LineChangedBg;
			lineStateDirtyGC = editor.ColorStyle.LineDirtyBg;
			
			marginWidth = (int)(10 * editor.Options.Zoom);
		}
		
		Cairo.Color foldBgGC, foldLineHighlightedGC, foldLineHighlightedGCBg;
		Cairo.Color lineStateChangedGC, lineStateDirtyGC;
		
		public override void Dispose ()
		{
			base.Dispose ();
			StopTimer ();
			editor.Document.FoldTreeUpdated -= HandleEditorDocumentFoldTreeUpdated;
			layout = layout.Kill ();
		}

		static HslColor GetColor (Cairo.Color col)
		{
			HslColor hsl = col;
			if (HslColor.Brightness (hsl) < 0.5) {
				hsl.L = System.Math.Min (1.0, hsl.L + 0.5);
			} else {
				hsl.L = System.Math.Max (0.0, hsl.L - 0.5);
			}
			return hsl;
		}

		void DrawClosedFolding (Cairo.Context cr, Cairo.Color col, double x, double y)
		{
			var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Width - foldSegmentSize) / 2) + 0.5, 
			                                    System.Math.Floor (y + (editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);
			cr.MoveTo (new Cairo.PointD (drawArea.X, drawArea.Y));
			cr.LineTo (new Cairo.PointD (drawArea.X, drawArea.Y + drawArea.Height));
			cr.LineTo (new Cairo.PointD (drawArea.X + drawArea.Width, drawArea.Y + drawArea.Height / 2));
			cr.ClosePath ();
			cr.Color = GetColor (col);
			cr.Fill ();
		}

		void DrawUpFolding (Cairo.Context cr, Cairo.Color col, double x, double y)
		{
			var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Width - foldSegmentSize) / 2) + 0.5, 
			                                    System.Math.Floor (y + (editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);
			cr.MoveTo (new Cairo.PointD (drawArea.X, drawArea.Y));
			cr.LineTo (new Cairo.PointD (drawArea.X + drawArea.Width, drawArea.Y));
			cr.LineTo (new Cairo.PointD (drawArea.X + drawArea.Width / 2, drawArea.Y + drawArea.Height));
			cr.ClosePath ();
			
			cr.Color = GetColor (col);
			cr.Fill ();
		}

		void DrawDownFolding (Cairo.Context cr, Cairo.Color col, double x, double y)
		{
			var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Width - foldSegmentSize) / 2) + 0.5, 
			                                    System.Math.Floor (y + (editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);

			cr.MoveTo (new Cairo.PointD (drawArea.X, drawArea.Y + drawArea.Height));
			cr.LineTo (new Cairo.PointD (drawArea.X + drawArea.Width / 2, drawArea.Y));
			cr.LineTo (new Cairo.PointD (drawArea.X + drawArea.Width, drawArea.Y + drawArea.Height));

			cr.ClosePath ();
			cr.Color = GetColor (col);
			cr.Fill ();
		}

		bool IsMouseHover (IEnumerable<FoldSegment> foldings)
		{
			return foldings.Any (s => this.lineHover == s.StartLine);
		}
		
		List<FoldSegment> startFoldings      = new List<FoldSegment> ();
		List<FoldSegment> containingFoldings = new List<FoldSegment> ();

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			foldSegmentSize = marginWidth * 4 / 6;
			foldSegmentSize -= (foldSegmentSize) % 2;
			
			Cairo.Rectangle drawArea = new Cairo.Rectangle (x, y, marginWidth, lineHeight);
			var state = editor.Document.GetLineState (lineSegment);
			
			bool isSelected = false;
			int nextDepth = 0;
			if (line <= editor.Document.LineCount) {
				containingFoldings.Clear ();
				startFoldings.Clear ();
				foreach (FoldSegment segment in editor.Document.GetFoldingContaining (lineSegment)) {
					if (segment.StartLine == lineSegment)
						startFoldings.Add (segment);
					containingFoldings.Add (segment);
				}

				nextDepth = containingFoldings.Count;
				if (lineSegment.NextLine != null)
					nextDepth = editor.Document.GetFoldingContaining (lineSegment.NextLine).Count ();

				
				isSelected = containingFoldings.Contains (hoverSegment);
			}
			
			var bgGC = foldLineHighlightedGC;
			if (editor.TextViewMargin.BackgroundRenderer != null) {
				if (isSelected) {
					bgGC = foldLineHighlightedGCBg;
				} else {
					bgGC = foldBgGC;
				}
			} else {
				HslColor col = foldLineHighlightedGCBg;
				if (col.L < 0.5) {
					col.L = System.Math.Min (1.0, col.L + containingFoldings.Count / 15.0);
				} else {
					col.L = System.Math.Max (0.0, col.L - containingFoldings.Count / 15.0);
				}
				bgGC = col;
			}
			
			cr.Rectangle (drawArea);
			cr.Color = bgGC;
			cr.Fill ();

			if (editor.TextViewMargin.BackgroundRenderer == null) {
				int delta = nextDepth - containingFoldings.Count ();
				if (delta != 0) {
					HslColor col = foldLineHighlightedGCBg;
					if (col.L < 0.5) {
						col.L = System.Math.Min (1.0, col.L + (nextDepth - delta * 1.5) / 15.0);
					} else {
						col.L = System.Math.Max (0.0, col.L - (nextDepth - delta * 1.5) / 15.0);
					}
					cr.Color = col;
					cr.MoveTo (x, y + lineHeight - 0.5);
					cr.LineTo (x + marginWidth, y + lineHeight - 0.5);
					cr.Stroke ();
				}
			}

			if (state == TextDocument.LineState.Changed) {
				cr.Color = lineStateChangedGC;
				cr.Rectangle (x + 1, y, marginWidth / 3, lineHeight);
				cr.Fill ();
			} else if (state == TextDocument.LineState.Dirty) {
				cr.Color = lineStateDirtyGC;
				cr.Rectangle (x + 1, y, marginWidth / 3, lineHeight);
				cr.Fill ();
			}
			
			if (line < editor.Document.LineCount) {
				bool isVisible = true;
				foreach (FoldSegment foldSegment in startFoldings) {
					if (foldSegment.IsFolded) {
						isVisible = false;
					}
				}
				if (!isVisible) {
					DrawClosedFolding (cr, bgGC, x, y);
				} else {
					if (hoverSegment != null && editor.TextViewMargin.BackgroundRenderer != null) {
						if (hoverSegment.StartLine == lineSegment) {
							DrawUpFolding (cr, bgGC, x, y);
						} else if (hoverSegment.EndLine == lineSegment) {
							DrawDownFolding (cr, bgGC, x, y);
						}
					}
				}
			}
		}
	}
}