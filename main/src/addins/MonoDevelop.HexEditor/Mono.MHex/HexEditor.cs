// 
// HexEditor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

using Xwt;

using Mono.MHex.Data;
using Mono.MHex.Rendering;
using Xwt.Drawing;

namespace Mono.MHex
{
	[System.ComponentModel.Category ("Mono.HexEditor")]
	[System.ComponentModel.ToolboxItem (true)]
	class HexEditor : Canvas
	{
		public HexEditorData HexEditorData {
			get;
			set;
		}
		
		public IHexEditorOptions Options {
			get;
			set;
		}
		
		IconMargin iconMargin;
		internal HexEditorMargin hexEditorMargin;
		GutterMargin gutterMargin;
		internal TextEditorMargin textEditorMargin;
		
		List<Margin> margins = new List<Margin> ();
		public List<Margin> Margins {
			get { return this.margins; }
		}
		
		public int LineHeight {
			get {
				return (int)HexEditorData.LineHeight;
			}
			set {
				HexEditorData.LineHeight = value;
			}
		}
		
		public int BytesInRow {
			get {
				return HexEditorData.BytesInRow;
			}
			set {
				HexEditorData.BytesInRow = value;
			}
		}
		
		HexEditorStyle style;
		public HexEditorStyle HexEditorStyle {
			get {
				return style;
			}
			set {
				style = value;
				Repaint ();
			}
		}

		protected override bool SupportsCustomScrolling {
			get {
				return true;
			}
		}

		public HexEditor ()
		{
			CanGetFocus = true;
			HexEditorData = new HexEditorData ();
			HexEditorData.EditMode = new SimpleEditMode ();
			HexEditorData.Caret.Changed += delegate {
				if (!HexEditorData.Caret.PreserveSelection)
					HexEditorData.ClearSelection ();
				RequestResetCaretBlink ();
				if (HexEditorData.Caret.AutoScrollToCaret)
					ScrollToCaret ();
				RepaintLine (HexEditorData.Caret.Line);
			};
			HexEditorData.Caret.OffsetChanged += delegate(object sender, CaretLocationEventArgs e) {
				if (!HexEditorData.Caret.PreserveSelection)
					HexEditorData.ClearSelection ();
				RequestResetCaretBlink ();
				if (HexEditorData.Caret.AutoScrollToCaret)
					ScrollToCaret ();
				RepaintLine (e.OldOffset / BytesInRow);
				RepaintLine (HexEditorData.Caret.Line);
			};
			HexEditorData.Undone += delegate {
				PurgeLayoutCaches ();
				Repaint ();
			};
			HexEditorData.Redone += delegate {
				PurgeLayoutCaches ();
				Repaint ();
			};
			HexEditorData.SelectionChanged += HexEditorDataSelectionChanged;
			HexEditorData.Replaced += delegate(object sender, ReplaceEventArgs e) {
				if (e.Count > 0) {
					PurgeLayoutCaches ();
					Repaint ();
				}
			};
			style = new HexEditorStyle ();
			
			iconMargin = new IconMargin (this);
			margins.Add (iconMargin);
			
			gutterMargin = new GutterMargin (this);
			margins.Add (gutterMargin);

			hexEditorMargin = new HexEditorMargin (this);
			margins.Add (hexEditorMargin);
			
			textEditorMargin = new TextEditorMargin (this);
			margins.Add (textEditorMargin);
			
			margins.Add (new EmptySpaceMargin (this));

			HexEditorData.UpdateRequested += delegate {
				HexEditorData.Updates.ForEach (update => update.AddRedraw (this));
				HexEditorData.Updates.Clear ();
			};
			
			Options = HexEditorOptions.DefaultOptions;
			Options.Changed += OptionsChanged;
		}

		protected override void Dispose (bool disposing)
		{
			Options.Changed -= OptionsChanged;
			if (caretTimer != null) { 
				caretTimer.Elapsed -= UpdateCaret;
				caretTimer.Dispose ();
				caretTimer = null;
			}
			base.Dispose (disposing);
		}
		
		public void PurgeLayoutCaches ()
		{
			margins.ForEach (margin => margin.PurgeLayoutCache ());
		}
		
		ISegment oldSelection = null;
		void HexEditorDataSelectionChanged (object sender, EventArgs e)
		{
			ISegment selection = HexEditorData.IsSomethingSelected ? HexEditorData.MainSelection.Segment : null;
			
			long startLine    = selection != null ? selection.Offset / BytesInRow : -1;
			long endLine      = selection != null ? selection.EndOffset / BytesInRow : -1;
			long oldStartLine = oldSelection != null ? oldSelection.Offset / BytesInRow : -1;
			long oldEndLine   = oldSelection != null ? oldSelection.EndOffset / BytesInRow : -1;
			
			
			if (endLine < 0 && startLine >=0)
				endLine = HexEditorData.Length / BytesInRow;
			if (oldEndLine < 0 && oldStartLine >=0)
				oldEndLine = HexEditorData.Length / BytesInRow;
		
			long from = oldEndLine, to = endLine;
			if (selection != null && oldSelection != null) {
				
				if (startLine != oldStartLine && endLine != oldEndLine) {
					from = System.Math.Min (startLine, oldStartLine);
					to   = System.Math.Max (endLine, oldEndLine);
				} else if (startLine != oldStartLine) {
					from = startLine;
					to   = oldStartLine;
				} else if (endLine != oldEndLine) {
					from = endLine;
					to   = oldEndLine;
				} else if (startLine == oldStartLine && endLine == oldEndLine)  {
					if (selection.Offset == oldSelection.Offset) {
						this.RepaintLine (endLine);
					} else if (selection.EndOffset == oldSelection.EndOffset) {
						this.RepaintLine (startLine);
					} else { // 3rd case - may happen when changed programmatically
						this.RepaintLine (endLine);
						this.RepaintLine (startLine);
					}
					from = to = -1;
				}
			} else {
				if (selection == null) {
					from = oldStartLine;
					to = oldEndLine;
				} else if (oldSelection == null) {
					from = startLine;
					to = endLine;
				} 
			}
			
			oldSelection = selection;
			if (from >= 0 && to >= 0) {
				long start = System.Math.Max (0, System.Math.Min (from, to)) - 1;
				long end = System.Math.Max (from, to) + 1;
				RepaintLines (start, end);
			} 
		}

		void OptionsChanged (object sender, EventArgs e)
		{
			gutterMargin.IsVisible = Options.ShowLineNumberMargin;
			iconMargin.IsVisible = iconMargin.IsVisible;
			
			
			Margins.ForEach (margin => { 
				margin.PurgeLayoutCache (); 
				margin.OptionsChanged (); 
			});
			
			this.CalculateBytesInRow ();
			SetAdjustments (Bounds);
			OnBytesInRowChanged (EventArgs.Empty);
		}
		
		protected virtual void OnBytesInRowChanged (EventArgs e)
		{
			EventHandler handler = this.BytesInRowChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler BytesInRowChanged;
		
		#region Scrolling
		int oldHAdjustment = -1;
		void HAdjustmentValueChanged (object sender, EventArgs args)
		{
			if (HexEditorData.HAdjustment.Value != System.Math.Ceiling (HexEditorData.HAdjustment.Value)) {
				HexEditorData.HAdjustment.Value = System.Math.Ceiling (HexEditorData.HAdjustment.Value);
				return;
			}
			
			int curHAdjustment = (int)HexEditorData.HAdjustment.Value;
			if (oldHAdjustment == curHAdjustment)
				return;
			
//			this.RepaintArea (this.textViewMargin.XOffset, 0, this.Bounds.Width - this.textViewMargin.XOffset, this.Bounds.Height);
			oldHAdjustment = curHAdjustment;
		}
		
//		double oldVadjustment = -1;
		void VAdjustmentValueChanged (object sender, EventArgs args)
		{
			if (HexEditorData.VAdjustment.Value != System.Math.Ceiling (HexEditorData.VAdjustment.Value)) {
				HexEditorData.VAdjustment.Value = System.Math.Ceiling (HexEditorData.VAdjustment.Value);
				return;
			}
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine  = (long)((HexEditorData.VAdjustment.Value + Bounds.Height) / LineHeight);
			margins.ForEach (margin => margin.SetVisibleWindow (firstVisibleLine, lastVisibleLine));
			
//			int delta = (int)(HexEditorData.VAdjustment.Value - this.oldVadjustment);
//			oldVadjustment = HexEditorData.VAdjustment.Value;
			
			// update pending redraws

//			if (System.Math.Abs (delta) >= Bounds.Height - this.LineHeight * 2) {
//				this.Repaint ();
//				return;
//			}
//			
//			int from, to;
//			if (delta > 0) {
//				from = delta;
//				to   = 0;
//			} else {
//				from = 0;
//				to   = -delta;
//			} 
//
//		
//			if (delta > 0) {
//				delta += LineHeight;
//				QueueDraw (new Rectangle (0, Bounds.Height - delta, Bounds.Width, delta));
//			} else {
//				delta -= LineHeight;
//				QueueDraw (new Rectangle (0, 0, Bounds.Width, -delta));
//			}
			QueueDraw ();
			ResetCaretBlink ();
		}

		protected override void SetScrollAdjustments (ScrollAdjustment hAdjustement, ScrollAdjustment vAdjustement)
		{
			if (HexEditorData.HAdjustment != null)
				HexEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (HexEditorData.VAdjustment != null)
				HexEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
			
			HexEditorData.HAdjustment = hAdjustement;
			HexEditorData.VAdjustment = vAdjustement;
			
			if (hAdjustement == null || vAdjustement == null)
				return;

			HexEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;
			HexEditorData.VAdjustment.ValueChanged += VAdjustmentValueChanged;
		}

/*		void UpdateAdjustments ()
		{
			SetAdjustments (this.Bounds);
		}
		*/
		
		internal void SetAdjustments ()
		{
			SetAdjustments (Bounds);
		}
		
		void SetHAdjustment ()
		{
			if (HexEditorData.HAdjustment == null)
				return;
/*			textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (longestLine != null && this.textEditorData.HAdjustment != null) {
				int maxX = longestLineWidth + 2 * this.textViewMargin.CharWidth;
				int width = Bounds.Width - this.TextViewMargin.XOffset;
				
				this.textEditorData.HAdjustment.SetBounds (0, maxX, this.textViewMargin.CharWidth, width, width);
				if (maxX < width)
					this.textEditorData.HAdjustment.Value = 0;
			}
			textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;*/
		}
		
		
		internal void SetAdjustments (Rectangle allocation)
		{
			if (HexEditorData.VAdjustment == null)
				return;
			long maxY = (HexEditorData.Length / BytesInRow + 1) * LineHeight;
			var vAdj = HexEditorData.VAdjustment;
			vAdj.LowerValue = 0;
			vAdj.UpperValue = maxY;
			vAdj.StepIncrement = LineHeight;
			vAdj.PageIncrement = allocation.Height;
			vAdj.PageSize = allocation.Height;

			if (maxY < allocation.Height)
				vAdj.Value = 0;
			if (vAdj.Value > maxY - allocation.Height) {
				vAdj.Value = maxY - allocation.Height;
			}
			SetHAdjustment ();
		}
		#endregion
		
		#region Drawing

		protected override void OnDraw (Context ctx, Rectangle area)
		{
			int reminder  = (int)HexEditorData.VAdjustment.Value % (int)LineHeight;
			long firstLine = (long)(HexEditorData.VAdjustment.Value / (long)LineHeight);
			long startLine = (long)(area.Top + reminder) / (int)this.LineHeight;
			long endLine   = (long)(area.Bottom + reminder) / (int)this.LineHeight - 1;
			if ((area.Bottom + reminder) % (int)this.LineHeight != 0)
				endLine++;
			// Initialize the rendering of the margins. Determine wether each margin has to be
			// rendered or not and calculate the X offset.
			List<Margin> marginsToRender = new List<Margin> ();
			double curX = 0;
			foreach (Margin margin in this.margins) {
				if (margin.IsVisible) {
					margin.XOffset = curX;
					if (curX >= area.X || margin.Width < 0)
						marginsToRender.Add (margin);
					curX += margin.Width;
				}
			}

			int curY = (int)(startLine * this.LineHeight - reminder);

			for (long visualLineNumber = startLine; visualLineNumber <= endLine; visualLineNumber++) {
				long logicalLineNumber = visualLineNumber + firstLine;
				foreach (Margin margin in marginsToRender) {
					try {
						margin.Draw (ctx, area, logicalLineNumber, margin.XOffset, curY);
					} catch (Exception e) {
						System.Console.WriteLine (e);
					}
				}
				curY += LineHeight;
				if (curY > area.Bottom)
					break;
			}
			if (requestResetCaretBlink) {
				ResetCaretBlink ();
				requestResetCaretBlink = false;
			}
			DrawCaret (ctx, area);
		}

		public void RepaintLine (long line)
		{
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine =  (long)(HexEditorData.VAdjustment.Value + Bounds.Height) / LineHeight;
			margins.ForEach (margin => margin.PurgeLayoutCache (line));
			if (firstVisibleLine <= line && line <= lastVisibleLine)
				QueueDraw (new Rectangle (0, (int)(line * LineHeight - HexEditorData.VAdjustment.Value), Bounds.Width, LineHeight));
		}
		
		public void RepaintLines (long start, long end)
		{
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine =  (long)(HexEditorData.VAdjustment.Value + Bounds.Height) / LineHeight;
			
			start = System.Math.Max (start, firstVisibleLine);
			end = System.Math.Min (end, lastVisibleLine);
			
			for (long line = start; line <= end; line++)
				margins.ForEach (margin => margin.PurgeLayoutCache (line));
			RepaintArea (0, (int)(start * LineHeight - HexEditorData.VAdjustment.Value), Bounds.Width, (int)((end - start) * LineHeight));
		}
		
		public void RepaintArea (double x, double y, double width, double height)
		{
			RepaintMarginArea (null, x, y, width, height);
		}
		
		public void RepaintMarginArea (Margin margin, double x, double y, double width, double height)
		{
			QueueDraw (new Rectangle (x, y, width, height));
		}
		
	
		public void Repaint ()
		{
			QueueDraw ();
		}		
		
		Timer caretTimer = null;
		object lockObject = new object ();
		
		public void ResetCaretBlink ()
		{
			lock (lockObject) {
				if (caretTimer != null)
					StopCaretThread ();
				
				if (caretTimer == null) {
					caretTimer = new Timer (800);
					caretTimer.Elapsed += UpdateCaret;
				}
				caretBlink = true; 
				caretTimer.Start ();
			}
		}

		internal void StopCaretThread ()
		{
			lock (lockObject) {
				if (caretTimer != null)
					caretTimer.Stop ();
				caretBlink = false; 
			}
		}
		
		void UpdateCaret (object sender, EventArgs args)
		{
			lock (lockObject) {
				caretBlink = !caretBlink;
				Application.Invoke (delegate {
					try {
						RepaintLine (HexEditorData.Caret.Line);
					} catch (Exception) {
						
					}
				});
			}
		}
		#endregion
		
		#region Caret
		bool requestResetCaretBlink = false;
		bool caretBlink = true;
		public void RequestResetCaretBlink ()
		{
			requestResetCaretBlink = true;
		}
		
		public void DrawCaret (Context ctx, Rectangle area)
		{
			if (!caretBlink || HexEditorData.IsSomethingSelected) 
				return;
			long caretY = HexEditorData.Caret.Line * LineHeight - (long)HexEditorData.VAdjustment.Value;
			double caretX;
			char ch;
			if (HexEditorData.Caret.InTextEditor) {
				caretX = textEditorMargin.CalculateCaretXPos (out ch);
			} else {
				caretX = hexEditorMargin.CalculateCaretXPos (out ch);
			}

			if (!area.Contains (caretX, (int)caretY))
				return;
			
			if (HexEditorData.Caret.IsInsertMode) {
				ctx.Rectangle (caretX, (int)caretY, 2, LineHeight);
			} else {
				ctx.Rectangle (caretX, (int)caretY, textEditorMargin.charWidth, LineHeight);
			}

			ctx.SetColor (HexEditorStyle.HexDigit); 
			ctx.Fill ();

			if (!HexEditorData.Caret.IsInsertMode) {
				using (var layout = new TextLayout (this)) {
					layout.Font = Options.Font;
					layout.Text = ch.ToString ();
					ctx.SetColor (HexEditorStyle.HexDigitBg);
					ctx.DrawTextLayout (layout, caretX, caretY); 
				}
			}
		}
		
		public void ScrollToCaret ()
		{
			double caretY = HexEditorData.Caret.Offset / BytesInRow * LineHeight;
			HexEditorData.VAdjustment.Value = System.Math.Max (caretY - HexEditorData.VAdjustment.PageSize + LineHeight, System.Math.Min (caretY, HexEditorData.VAdjustment.Value));
		}
		
		#endregion
		
		#region Events
		protected override void OnBoundsChanged ()
		{
			base.OnBoundsChanged ();
			this.CalculateBytesInRow ();
			OptionsChanged (this, EventArgs.Empty);
			SetAdjustments (Bounds);
			OnBytesInRowChanged (EventArgs.Empty);
			Repaint ();
		}

		protected override void OnKeyPressed (KeyEventArgs args)
		{
			uint unicodeChar = (uint)args.Key;

			var filteredModifiers = args.Modifiers & (ModifierKeys.Shift | ModifierKeys.Command | ModifierKeys.Control);
			
			HexEditorData.EditMode.InternalHandleKeypress (this, args.Key, unicodeChar, filteredModifiers);
			args.Handled = true;

		}
		
		void CalculateBytesInRow ()
		{
			int oldBytes = BytesInRow;
			var maxWidth = Bounds.Width;
			int start = Options.GroupBytes * 2;
			for (int i = start; i < 100; i += Options.GroupBytes) {
				double width = margins.Sum (margin => margin.CalculateWidth (i));
				if (width > maxWidth) {
					BytesInRow = i - Options.GroupBytes;
					if (i == start) {
						WidthRequest = margins.Sum (margin => margin.CalculateWidth (BytesInRow + BytesInRow - 1));
					} else {
						WidthRequest = 10;
					}
					break;
				}
			}
			if (Options.StringRepresentationType == StringRepresentationTypes.UTF16 && BytesInRow % 2 == 1) {
				if (BytesInRow == 1) {
					BytesInRow = 2;
				} else {
					BytesInRow--;
				}
			}
			if (oldBytes != BytesInRow) {
				margins.ForEach (margin => margin.PurgeLayoutCache ());
			}
		}

		/*
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if ((evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask) {
				if (evnt.Direction == ScrollDirection.Down)
					Options.ZoomIn ();
				else 
					Options.ZoomOut ();
				this.Repaint ();
				return true;
			}
			return base.OnScrollEvent (evnt); 
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (caretGc != null) {
				caretGc.Dispose ();
				caretGc = null;
			}
		}
		*/

		protected override void OnGotFocus (EventArgs args)
		{
			base.OnGotFocus (args);
			RequestResetCaretBlink ();
		}

		protected override void OnLostFocus (EventArgs args)
		{
			base.OnLostFocus (args);
			StopCaretThread ();
		}

/*
		protected override void OnRealized ()
		{
			base.OnRealized ();

			AllocateWindowBuffer (Bounds);
		}*/
		
		internal int pressedButton = -1;
		protected override void OnButtonPressed (ButtonEventArgs e)
		{
			base.OnButtonPressed (e);
			this.SetFocus ();
			if (e.Button != PointerButton.Left)
				return;
			pressedButton = (int)e.Button;
			Margin margin = GetMarginAtX ((int)e.X);
			if (margin != null) 
				margin.MousePressed (new MarginMouseEventArgs (this, margin, e));
		}

		protected override void OnButtonReleased (ButtonEventArgs e)
		{
			base.OnButtonReleased (e);

			if (e.Button != PointerButton.Left)
				return;
			pressedButton = -1;
			Margin margin = GetMarginAtX ((int)e.X);
			
			if (margin != null)
				margin.MouseReleased (new MarginMouseEventArgs (this, margin, e));
		}

		protected override void OnMouseMoved (MouseMovedEventArgs e)
		{
			base.OnMouseMoved (e);

			Margin margin = GetMarginAtX ((int)e.X);
			
			if (margin != null)
				margin.MouseHover (new MarginMouseMovedEventArgs (this, margin, e));
		}
		
		Margin GetMarginAtX (int x)
		{
			return this.margins.FirstOrDefault (margin => margin.XOffset <= x && x < margin.XOffset + margin.Width);
		}
		#endregion
	}
}
