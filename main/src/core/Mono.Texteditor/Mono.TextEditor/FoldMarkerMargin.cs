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

namespace Mono.TextEditor
{
	public class FoldMarkerMargin : Margin
	{
		TextEditor editor;
		LineSegment lineHover;
		Pango.Layout layout;
		
		int foldSegmentSize = 8;
		int marginWidth;
		public override int Width {
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
		
		public FoldMarkerMargin (TextEditor editor)
		{
			this.editor = editor;
			layout = PangoUtil.CreateLayout (editor);
			delayTimer = new Timer (150);
			delayTimer.AutoReset = false;
			delayTimer.Elapsed += DelayTimerElapsed;
			editor.Caret.PositionChanged += HandleEditorCaretPositionChanged;
		}

		void HandleEditorCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!IsInCodeFocusMode) 
				return;
			LineSegment lineSegment = editor.Document.GetLine (editor.Caret.Line);
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
				DelayTimerElapsed (this, null);
			}
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			
			if (lineHover == null)
				return;
			foreach (FoldSegment segment in editor.Document.GetStartFoldings (lineHover)) {
				segment.IsFolded = !segment.IsFolded; 
			}
			editor.SetAdjustments ();
			editor.Caret.MoveCaretBeforeFoldings ();
			editor.QueueDraw ();
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			
			LineSegment lineSegment = null;
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
				if (segment.StartLine.Offset == lineSegment.Offset) {
					found = true;
					break;
				}
			}
			
			delayTimer.Stop ();
			if (found) {
				foldings = editor.Document.GetFoldingContaining (lineSegment);
				if (editor.TextViewMargin.BackgroundRenderer == null) {
					delayTimer.Start ();
				} else {
					DelayTimerElapsed (this, null);
				}
			} else {
				RemoveBackgroundRenderer ();
			}
		}
		
		Timer delayTimer;
		IEnumerable<FoldSegment> foldings;
		void DelayTimerElapsed (object sender, ElapsedEventArgs e)
		{
			editor.TextViewMargin.BackgroundRenderer = new FoldingScreenbackgroundRenderer (editor, foldings);
			editor.QueueDraw ();
		}
		
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
			
			if (lineHover != null) {
				lineHover = null;
				editor.RedrawMargin (this);
			}
			delayTimer.Stop ();
			RemoveBackgroundRenderer ();
		}
		
		internal protected override void OptionsChanged ()
		{
			DisposeGCs ();
			foldBgGC = new Gdk.GC (editor.GdkWindow);
			foldBgGC.RgbFgColor = editor.ColorStyle.FoldLine.BackgroundColor;
			
			foldLineGC = new Gdk.GC (editor.GdkWindow);
			foldLineGC.RgbFgColor = editor.ColorStyle.FoldLine.Color;
			
			foldLineHighlightedGC = new Gdk.GC (editor.GdkWindow);
			foldLineHighlightedGC.RgbFgColor = editor.ColorStyle.FoldLineHighlighted;
			
			HslColor hslColor = new HslColor (editor.ColorStyle.Default.BackgroundColor);
			double brightness = HslColor.Brightness (editor.ColorStyle.Default.BackgroundColor);
			if (brightness < 0.5) {
				hslColor.L = hslColor.L * 0.85 + hslColor.L * 0.25;
			} else {
				hslColor.L = hslColor.L * 0.9;
			}
			
			foldLineHighlightedGCBg = new Gdk.GC (editor.GdkWindow);
			foldLineHighlightedGCBg.RgbFgColor = hslColor;
			
			foldToggleMarkerGC = new Gdk.GC (editor.GdkWindow);
			foldToggleMarkerGC.RgbFgColor = editor.ColorStyle.FoldToggleMarker;

			lineStateChangedGC = new Gdk.GC (editor.GdkWindow);
			lineStateChangedGC.RgbFgColor = new Gdk.Color (108, 226, 108);
			
			lineStateDirtyGC = new Gdk.GC (editor.GdkWindow);
			lineStateDirtyGC.RgbFgColor = new Gdk.Color (255, 238, 98);

			marginWidth = editor.LineHeight;
			/*
			layout.FontDescription = editor.Options.Font;
			layout.SetText ("!");
			int tmp;
			layout.GetPixelSize (out tmp, out this.marginWidth);
			marginWidth *= 8;
			marginWidth /= 10;*/
		}
		
		Gdk.GC foldBgGC, foldLineGC, foldLineHighlightedGC, foldLineHighlightedGCBg, foldToggleMarkerGC;
		Gdk.GC lineStateChangedGC, lineStateDirtyGC;
		public override void Dispose ()
		{
			base.Dispose ();
			if (delayTimer != null) {
				delayTimer.Stop ();
				delayTimer.Dispose ();
				delayTimer = null;
			}
			layout = layout.Kill ();
			DisposeGCs ();
		}
		
		void DisposeGCs ()
		{
			foldBgGC = foldBgGC.Kill ();
			foldLineGC = foldLineGC.Kill ();
			foldLineHighlightedGC = foldLineHighlightedGC.Kill ();
			foldLineHighlightedGCBg = foldLineHighlightedGCBg.Kill ();
			foldToggleMarkerGC = foldToggleMarkerGC.Kill ();
			lineStateChangedGC = lineStateChangedGC.Kill ();
			lineStateDirtyGC = lineStateDirtyGC.Kill ();
		}
		
		void DrawFoldSegment (Gdk.Drawable win, int x, int y, bool isOpen, bool isSelected)
		{
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x + (Width - foldSegmentSize) / 2, y + (editor.LineHeight - foldSegmentSize) / 2, foldSegmentSize, foldSegmentSize);
			win.DrawRectangle (foldBgGC, true, drawArea);
			win.DrawRectangle (isSelected ? foldLineHighlightedGC  : foldLineGC, false, drawArea);
			
			win.DrawLine (foldToggleMarkerGC, 
			              drawArea.Left  + drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2,
			              drawArea.Right - drawArea.Width * 3 / 10,
			              drawArea.Top + drawArea.Height / 2);
			
			if (!isOpen)
				win.DrawLine (foldToggleMarkerGC, 
				              drawArea.Left + drawArea.Width / 2,
				              drawArea.Top + drawArea.Height * 3 / 10,
				              drawArea.Left  + drawArea.Width / 2,
				              drawArea.Bottom - drawArea.Height * 3 / 10);
		}
		
		
		bool IsMouseHover (IEnumerable<FoldSegment> foldings)
		{
			return foldings.Any (s => this.lineHover == s.StartLine);
		}
		
		List<FoldSegment> startFoldings      = new List<FoldSegment> ();
		List<FoldSegment> containingFoldings = new List<FoldSegment> ();
		List<FoldSegment> endFoldings        = new List<FoldSegment> ();
		
		internal protected override void Draw (Gdk.Drawable win, Gdk.Rectangle area, int line, int x, int y, int lineHeight)
		{
			foldSegmentSize = Width * 4 / 6;
			foldSegmentSize -= (foldSegmentSize) % 2;
			
			Gdk.Rectangle drawArea = new Gdk.Rectangle (x, y, Width, lineHeight);
			Document.LineState state = editor.Document.GetLineState (line);
			
			bool isFoldStart = false;
			bool isContaining = false;
			bool isFoldEnd = false;
			
			bool isStartSelected = false;
			bool isContainingSelected = false;
			bool isEndSelected = false;
			
			if (line < editor.Document.LineCount) {
				LineSegment lineSegment = editor.Document.GetLine (line);
				startFoldings.Clear ();
				containingFoldings.Clear ();
				endFoldings.Clear ();
				foreach (FoldSegment segment in editor.Document.GetFoldingContaining (lineSegment)) {
					if (segment.StartLine.Offset == lineSegment.Offset) {
						startFoldings.Add (segment);
					} else if (segment.EndLine.Offset == lineSegment.Offset) {
						endFoldings.Add (segment);
					} else {
						containingFoldings.Add (segment);
					}
				}
				
				isFoldStart  = startFoldings.Count > 0;
				isContaining = containingFoldings.Count > 0;
				isFoldEnd    = endFoldings.Count > 0;
				
				isStartSelected      = this.lineHover != null && IsMouseHover (startFoldings);
				isContainingSelected = this.lineHover != null && IsMouseHover (containingFoldings);
				isEndSelected        = this.lineHover != null && IsMouseHover (endFoldings);
			}
			
			Gdk.GC bgGC = foldBgGC;
			if (editor.TextViewMargin.BackgroundRenderer != null) {
				if (isContainingSelected || isStartSelected || isEndSelected) {
					bgGC = foldBgGC;
				} else {
					bgGC = foldLineHighlightedGCBg;
				}
			}
			
			win.DrawRectangle (bgGC, true, drawArea);
			if (state == Document.LineState.Changed) {
				win.DrawRectangle (lineStateChangedGC, true, x + 1, y, Width / 3, lineHeight);
		//		win.DrawRectangle (bgGC, true, x + 3 , y, Width  - 3, lineHeight);
			} else if (state == Document.LineState.Dirty) {
				win.DrawRectangle (lineStateDirtyGC, true, x + 1, y, Width / 3, lineHeight);
		//		win.DrawRectangle (bgGC, true, x + 3 , y, Width - 3, lineHeight);
			}/* else {
				win.DrawRectangle (bgGC, true, drawArea);
			}*/
			
			if (line < editor.Document.LineCount) {
			
				int foldSegmentYPos = y + (editor.LineHeight - foldSegmentSize) / 2;
				int xPos = x + Width / 2;
				
				if (isFoldStart) {
					bool isVisible         = true;
					bool moreLinedOpenFold = false;
					foreach (FoldSegment foldSegment in startFoldings) {
						if (foldSegment.IsFolded) {
							isVisible = false;
						} else {
							moreLinedOpenFold = foldSegment.EndLine.Offset > foldSegment.StartLine.Offset;
						}
					}
					bool isFoldEndFromUpperFold = false;
					foreach (FoldSegment foldSegment in endFoldings) {
						if (foldSegment.EndLine.Offset > foldSegment.StartLine.Offset && !foldSegment.IsFolded) 
							isFoldEndFromUpperFold = true;
					}
					DrawFoldSegment (win, x, y, isVisible, isStartSelected);
					if (isContaining || isFoldEndFromUpperFold) 
						win.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Top, xPos, foldSegmentYPos - 2);
					if (isContaining || moreLinedOpenFold) 
						win.DrawLine (isEndSelected || (isStartSelected && isVisible) || isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, foldSegmentYPos + foldSegmentSize + 2, xPos, drawArea.Bottom);
				} else {
					if (isFoldEnd) {
						int yMid = drawArea.Top + drawArea.Height / 2;
						win.DrawLine (isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid, x + Width - 2, yMid);
						win.DrawLine (isContainingSelected || isEndSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Top, xPos, yMid);
						if (isContaining) 
							win.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, yMid + 1, xPos, drawArea.Bottom);
					} else if (isContaining) {
						win.DrawLine (isContainingSelected ? foldLineHighlightedGC : foldLineGC, xPos, drawArea.Top, xPos, drawArea.Bottom);
					}
				}
			}
		}
	}
}
