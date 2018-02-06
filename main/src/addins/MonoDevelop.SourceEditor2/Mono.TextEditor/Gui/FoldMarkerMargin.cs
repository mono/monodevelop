// FoldMarkerMargin.cs
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
using System.Collections.Generic;
using System.Linq;
using Gtk;
using System.Timers;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core;

namespace Mono.TextEditor
{
	class FoldMarkerMargin : Margin
	{
		MonoTextEditor editor;
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
		
		public FoldMarkerMargin (MonoTextEditor editor)
		{
			this.editor = editor;
			layout = PangoUtil.CreateLayout (editor);
			editor.Caret.PositionChanged += HandleEditorCaretPositionChanged;
			editor.Document.FoldTreeUpdated += HandleEditorDocumentFoldTreeUpdated;
			editor.Caret.PositionChanged += EditorCarethandlePositionChanged;

			UpdateAccessibility ();
		}

		void EditorCarethandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!editor.GetTextEditorData ().HighlightCaretLine || e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}

		void HandleEditorDocumentFoldTreeUpdated (object sender, EventArgs e)
		{
			UpdateAccessibility ();
			editor.RedrawMargin (this);
		}

		Dictionary<FoldSegment, FoldingAccessible> accessibles = null;
		void UpdateAccessibility ()
		{
			if (!IdeTheme.AccessibilityEnabled) {
				return;
			}

			if (accessibles == null) {
				accessibles = new Dictionary<FoldSegment, FoldingAccessible> ();
			}
			foreach (var a in accessibles.Values) {
				Accessible.RemoveAccessibleChild (a.Accessible);
				a.Dispose ();
			}
			accessibles.Clear ();

			// Add any folds
			var segments = editor.Document.FoldSegments;
			foreach (var f in segments) {
				var accessible = new FoldingAccessible (f, this, editor);
				accessibles [f] = accessible;

				Accessible.AddAccessibleChild (accessible.Accessible);
			}
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
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			if (args.LineSegment == null || !editor.Options.ShowFoldMargin)
				return;

			foreach (FoldSegment segment in editor.Document.GetStartFoldings (args.LineSegment)) {
				segment.IsCollapsed = !segment.IsCollapsed;
                editor.Document.InformFoldChanged (new FoldSegmentEventArgs (segment));
            }
            editor.SetAdjustments ();
			editor.Caret.MoveCaretBeforeFoldings ();
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			if (!editor.Options.ShowFoldMargin)
				return;

			DocumentLine lineSegment = null;
			if (args.LineSegment != null) {
				lineSegment = args.LineSegment;
				if (lineHover != lineSegment) {
					lineHover = lineSegment;
					editor.RedrawMargin (this);
				}
			} 
			lineHover = lineSegment;
			bool found = false;
			foreach (FoldSegment segment in editor.Document.GetFoldingContaining (lineSegment)) {
				if (segment.GetStartLine (editor.Document).Offset == lineSegment.Offset) {
					found = true;
					break;
				}
			}
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
		List<FoldSegment> oldFolds;
		bool SetBackgroundRenderer ()
		{
			List<FoldSegment> curFolds = new List<FoldSegment> (foldings);
			if (oldFolds != null && oldFolds.Count == curFolds.Count) {
				bool same = true;
				for (int i = 0; i < curFolds.Count; i++) {
					if (oldFolds[i] != curFolds [i]) {
						same = false;
						break;
					}
				}

				if (same) {
					timerId = 0;
					return false;
				}
			}

			oldFolds = curFolds;
			editor.TextViewMargin.DisposeLayoutDict ();
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
			oldFolds = null;
			if (editor.TextViewMargin.BackgroundRenderer != null) {
				editor.TextViewMargin.BackgroundRenderer = null;
				editor.QueueDraw ();
			}
		}
		
		internal protected override void MouseLeft ()
		{
			base.MouseLeft ();
			
			if (lineHover != null) {
				lineHover = null;
				editor.RedrawMargin (this);
			}
			StopTimer ();
			RemoveBackgroundRenderer ();
		}
		
		internal protected override void OptionsChanged ()
		{
			foldBgGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Background);
			foldLineGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.FoldLine);
			foldLineHighlightedGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Foreground);
			
			HslColor hslColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.Background);
			double brightness = HslColor.Brightness (hslColor);
			if (brightness < 0.5) {
				hslColor.L = hslColor.L * 0.85 + hslColor.L * 0.25;
			} else {
				hslColor.L = hslColor.L * 0.9;
			}
			
			foldLineHighlightedGCBg = hslColor;
			foldToggleMarkerGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.FoldCross);
			foldToggleMarkerBackground = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.FoldCrossBackground);
			lineStateChangedGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.QuickDiffChanged);
			lineStateDirtyGC = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.QuickDiffDirty);
			
			marginWidth = editor.LineHeight  * 3 / 4;
		}
		
		Cairo.Color foldBgGC, foldLineGC, foldLineHighlightedGC, foldLineHighlightedGCBg, foldToggleMarkerGC, foldToggleMarkerBackground;
		Cairo.Color lineStateChangedGC, lineStateDirtyGC;
		
		public override void Dispose ()
		{
			base.Dispose ();
			StopTimer ();
			editor.Caret.PositionChanged -= HandleEditorCaretPositionChanged;
			editor.Caret.PositionChanged -= EditorCarethandlePositionChanged;
			editor.Document.FoldTreeUpdated -= HandleEditorDocumentFoldTreeUpdated;
			layout = layout.Kill ();
			foldings = null;
			startFoldings = containingFoldings = endFoldings = null;

			if (accessibles != null) {
				foreach (var a in accessibles.Values) {
					Accessible.RemoveAccessibleChild (a.Accessible);
					a.Dispose ();
				}
				accessibles.Clear ();
			}
		}
		
		void DrawFoldSegment (Cairo.Context ctx, double x, double y, bool isOpen, bool isSelected)
		{
			var drawArea = new Cairo.Rectangle (System.Math.Floor (x + (Width - foldSegmentSize) / 2) + 0.5, 
			                                    System.Math.Floor (y + (editor.LineHeight - foldSegmentSize) / 2) + 0.5, foldSegmentSize, foldSegmentSize);
			ctx.Rectangle (drawArea);
			ctx.SetSourceColor (isOpen ? foldBgGC : foldToggleMarkerBackground);
			ctx.FillPreserve ();
			ctx.SetSourceColor (isSelected ? foldLineHighlightedGC  : foldLineGC);
			ctx.Stroke ();
			
			ctx.DrawLine (isSelected ? foldLineHighlightedGC  : foldToggleMarkerGC,
			              drawArea.X  + drawArea.Width * 2 / 10,
			              drawArea.Y + drawArea.Height / 2,
			              drawArea.X + drawArea.Width - drawArea.Width * 2 / 10,
			              drawArea.Y + drawArea.Height / 2);
			
			if (!isOpen)
				ctx.DrawLine (isSelected ? foldLineHighlightedGC  : foldToggleMarkerGC,
				              drawArea.X + drawArea.Width / 2,
				              drawArea.Y + drawArea.Height * 2 / 10,
				              drawArea.X  + drawArea.Width / 2,
				              drawArea.Y + drawArea.Height - drawArea.Height * 2 / 10);
		}
		
		bool IsMouseHover (IEnumerable<FoldSegment> foldings)
		{
			return foldings.Any (s => this.lineHover == s.GetStartLine  (editor.Document));
		}
		
		List<FoldSegment> startFoldings      = new List<FoldSegment> ();
		List<FoldSegment> containingFoldings = new List<FoldSegment> ();
		List<FoldSegment> endFoldings        = new List<FoldSegment> ();
		
		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine lineSegment, int line, double x, double y, double lineHeight)
		{
			MarginMarker marker = null;
			if (lineSegment != null) {
				foreach (var m in editor.Document.GetMarkers (lineSegment)) {
					var mm = m as MarginMarker;
					if (mm != null && mm.CanDraw (this)) {
						marker = mm;
						break;
					}
				}
			}

			if (marker != null) {
				bool hasDrawn = marker.DrawBackground (editor, cr, new MarginDrawMetrics (this, area, lineSegment, line, x, y, lineHeight));
				if (!hasDrawn)
					marker = null;
			}

			foldSegmentSize = marginWidth * 4 / 6;
			foldSegmentSize -= (foldSegmentSize) % 2;
			
			Cairo.Rectangle drawArea = new Cairo.Rectangle (x, y, marginWidth, lineHeight);
			var state = editor.Document.GetLineState (lineSegment);

			bool isFoldStart = false;
			bool isContaining = false;
			bool isFoldEnd = false;
			
			bool isStartSelected = false;
			bool isContainingSelected = false;
			bool isEndSelected = false;
			
			if (editor.Options.ShowFoldMargin && line <= editor.Document.LineCount) {
				startFoldings.Clear ();
				containingFoldings.Clear ();
				endFoldings.Clear ();
				if (lineSegment != null) {
					foreach (FoldSegment segment in editor.Document.GetFoldingContaining (lineSegment)) {
						if (segment.GetStartLine (editor.Document)?.Offset == lineSegment.Offset) {
							startFoldings.Add (segment);
						} else if (segment.GetEndLine (editor.Document)?.Offset == lineSegment.Offset) {
							endFoldings.Add (segment);
						} else {
							containingFoldings.Add (segment);
						}
					}
				}
				
				isFoldStart = startFoldings.Count > 0;
				isContaining = containingFoldings.Count > 0;
				isFoldEnd = endFoldings.Count > 0;
				
				isStartSelected = this.lineHover != null && IsMouseHover (startFoldings);
				isContainingSelected = this.lineHover != null && IsMouseHover (containingFoldings);
				isEndSelected = this.lineHover != null && IsMouseHover (endFoldings);
			}

			if (marker == null) {
				if (editor.GetTextEditorData ().HighlightCaretLine && editor.Caret.Line == line) {
					editor.TextViewMargin.DrawCaretLineMarker (cr, x, y, Width, lineHeight);
				} else {
					var bgGC = foldBgGC;
					if (editor.TextViewMargin.BackgroundRenderer != null) {
						if (isContainingSelected || isStartSelected || isEndSelected) {
							bgGC = foldBgGC;
						} else {
							bgGC = foldLineHighlightedGCBg;
						}
					}
					
					cr.Rectangle (drawArea);
					cr.SetSourceColor (bgGC);
					cr.Fill ();
				}
			}

			if (editor.Options.EnableQuickDiff && state != TextDocument.LineState.Unchanged) {
				var prevState = lineSegment?.PreviousLine != null ? editor.Document.GetLineState (lineSegment.PreviousLine) : TextDocument.LineState.Unchanged;
				var nextState = lineSegment?.NextLine != null ? editor.Document.GetLineState (lineSegment.NextLine) : TextDocument.LineState.Unchanged;

				if (state == TextDocument.LineState.Changed) {
					cr.SetSourceColor (lineStateChangedGC);
				} else if (state == TextDocument.LineState.Dirty) {
					cr.SetSourceColor (lineStateDirtyGC);
				}

				if ((prevState == TextDocument.LineState.Unchanged  && prevState != state ||
				     nextState == TextDocument.LineState.Unchanged  && nextState != state)) {
					FoldingScreenbackgroundRenderer.DrawRoundRectangle (
						cr, 
						prevState == TextDocument.LineState.Unchanged, 
						nextState == TextDocument.LineState.Unchanged,
						x + 1, 
						y, 
						lineHeight / 4,
						marginWidth / 4, 
						lineHeight
					);
				} else {
					cr.Rectangle (x + 1, y, marginWidth / 4, lineHeight);
				}
				cr.Fill ();
			}

			if (editor.Options.ShowFoldMargin && line <= editor.Document.LineCount) {
				double foldSegmentYPos = y + System.Math.Floor (editor.LineHeight - foldSegmentSize) / 2;
				double xPos = x + System.Math.Floor (marginWidth / 2) + 0.5;
				
				if (isFoldStart) {
					bool isVisible         = true;
					bool moreLinedOpenFold = false;
					foreach (FoldSegment foldSegment in startFoldings) {
						if (foldSegment.IsCollapsed) {
							isVisible = false;
						} else {
							moreLinedOpenFold = foldSegment.GetEndLine (editor.Document).Offset > foldSegment.GetStartLine (editor.Document).Offset;
						}
					}
					bool isFoldEndFromUpperFold = false;
					foreach (FoldSegment foldSegment in endFoldings) {
						if (foldSegment.GetEndLine (editor.Document).Offset > foldSegment.GetStartLine (editor.Document).Offset && !foldSegment.IsCollapsed) 
							isFoldEndFromUpperFold = true;
					}
					DrawFoldSegment (cr, x, y, isVisible, isStartSelected);
					
					if (isContaining || isFoldEndFromUpperFold)
						cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, foldSegmentYPos - 2);
					if (isContaining || moreLinedOpenFold) 
						cr.DrawLine (isEndSelected || (isStartSelected && isVisible) || isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, foldSegmentYPos + foldSegmentSize + 2, xPos, drawArea.Y + drawArea.Height);
				} else {
					if (isFoldEnd) {
						double yMid = System.Math.Floor (drawArea.Y + drawArea.Height / 2) + 0.5;
						cr.DrawLine (isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid, x + marginWidth - 2, yMid);
						cr.DrawLine (isContainingSelected || isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, yMid);
						
						if (isContaining) 
							cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid, xPos, drawArea.Y + drawArea.Height);
					} else if (isContaining) {
						cr.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Y, xPos, drawArea.Y + drawArea.Height);
					}
				}
			}
		}
	}

	class FoldingAccessible : IDisposable
	{
		public AccessibilityElementProxy Accessible { get; private set; }

		FoldSegment segment;
		FoldMarkerMargin margin;
		MonoTextEditor editor;

		double startY;

		public FoldingAccessible (FoldSegment segment, FoldMarkerMargin margin, MonoTextEditor editor)
		{
			Accessible = AccessibilityElementProxy.ButtonElementProxy ();
			Accessible.PerformPress += PerformPress;
			Accessible.GtkParent = margin.Accessible.GtkParent;

			this.segment = segment;
			this.margin = margin;
			this.editor = editor;

			UpdateAccessibility ();
		}

		void UpdateAccessibility ()
		{
			var startLine = segment.GetStartLine (editor.Document).LineNumber;
			var endLine = segment.GetEndLine (editor.Document).LineNumber;

			Accessible.Label = GettextCatalog.GetString ("Fold Region: Line {0} to line {1} - {2}", startLine, endLine,
														 segment.isFolded ? GettextCatalog.GetString ("Folded") : GettextCatalog.GetString ("Expanded"));
			if (segment.isFolded) {
				Accessible.Help = GettextCatalog.GetString ("Activate to expand the region");
			} else {
				Accessible.Help = GettextCatalog.GetString ("Activate to fold the region");
			}

			startY = editor.LineToY (startLine);
			double endY;

			if (segment.isFolded) {
				endY = startY;
			} else {
				endY = editor.LineToY (endLine);
			}

			var rect = new Gdk.Rectangle (0, (int)startY, (int)margin.Width, (int)(endY - startY));
			Accessible.FrameInGtkParent = rect;

			var halfParentHeight = margin.RectInParent.Height / 2;
			var dEndY = endY - halfParentHeight;
			var cocoaEndY = halfParentHeight - dEndY;

			var dStartY = startY - halfParentHeight;
			var cocoaStartY = halfParentHeight - dStartY;

			var minY = Math.Min (cocoaStartY, cocoaEndY);
			var maxY = Math.Max (cocoaStartY, cocoaEndY);

			Accessible.FrameInParent = new Gdk.Rectangle (0, (int)(minY - editor.LineHeight), (int)margin.Width, (int)((maxY - minY) + editor.LineHeight));
		}

		public void Dispose ()
		{
			margin = null;
			editor = null;
			segment = null;

			Accessible.PerformPress -= PerformPress;
			Accessible = null;
		}

		void PerformPress (object sender, EventArgs args)
		{
			var fakeEvent = new MarginMouseEventArgs (editor, Gdk.EventType.ButtonPress, 0, 0, startY, Gdk.ModifierType.None);
			margin.MousePressed (fakeEvent);

			UpdateAccessibility ();
		}
	}
}
