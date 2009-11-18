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

using Gdk;
using Gtk;

using Mono.MHex.Data;
using Mono.MHex.Rendering;

namespace Mono.MHex
{
	[System.ComponentModel.Category ("Mono.HexEditor")]
	[System.ComponentModel.ToolboxItem (true)]
	public class HexEditor : Gtk.DrawingArea
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
		DashedLineMargin dashedLineMargin;
		internal TextEditorMargin textEditorMargin;
		
		List<Margin> margins = new List<Margin> ();
		public List<Margin> Margins {
			get { return this.margins; }
		}
		
		public int LineHeight {
			get {
				return HexEditorData.LineHeight;
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
		
		public Pango.FontDescription Font {
			get {
				return Pango.FontDescription.FromString ("Mono 10");
			}
		}
		
		public HexEditor ()
		{
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
			
			dashedLineMargin = new DashedLineMargin (this);
			margins.Add (dashedLineMargin);
			
			hexEditorMargin = new HexEditorMargin (this);
			margins.Add (hexEditorMargin);
			
			textEditorMargin = new TextEditorMargin (this);
			margins.Add (textEditorMargin);
			
			margins.Add (new EmptySpaceMargin (this));
			
			this.Events = EventMask.AllEventsMask;
			this.DoubleBuffered = false;
			this.AppPaintable = true;
			base.CanFocus = true;
			
			HexEditorData.UpdateRequested += delegate {
				HexEditorData.Updates.ForEach (update => update.AddRedraw (this));
				HexEditorData.Updates.Clear ();
			};
			
			Options = HexEditorOptions.DefaultOptions;
			Options.Changed += OptionsChanged;
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
			if (!IsRealized)
				return;
			dashedLineMargin.IsVisible = gutterMargin.IsVisible = Options.ShowLineNumberMargin;
			iconMargin.IsVisible = iconMargin.IsVisible;
			
			
			Margins.ForEach (margin => { 
				margin.PurgeGCs (); 
				margin.PurgeLayoutCache (); 
				margin.OptionsChanged (); 
			});
			
			this.CalculateBytesInRow ();
			SetAdjustments (Allocation);
			OnBytesInRowChanged (EventArgs.Empty);
		}
		
		protected virtual void OnBytesInRowChanged (EventArgs e)
		{
			EventHandler handler = this.BytesInRowChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler BytesInRowChanged;
		
		#region Background buffer
		Gdk.Pixmap buffer = null, flipBuffer = null;
		
		void DoFlipBuffer ()
		{
			Gdk.Pixmap tmp = buffer;
			buffer = flipBuffer;
			flipBuffer = tmp;
		}
		
		void DisposeBgBuffer ()
		{
			if (buffer != null) {
				buffer.Dispose ();
				buffer = null;
			}
			if (flipBuffer !=  null) {
				flipBuffer.Dispose ();
				flipBuffer = null;
			}
		}
		
		void AllocateWindowBuffer (Rectangle allocation)
		{
			DisposeBgBuffer ();
			if (this.IsRealized) {
				buffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
				flipBuffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
			}
		}
		#endregion
		
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
			
//			this.RepaintArea (this.textViewMargin.XOffset, 0, this.Allocation.Width - this.textViewMargin.XOffset, this.Allocation.Height);
			oldHAdjustment = curHAdjustment;
		}
		
		double oldVadjustment = -1;
		void VAdjustmentValueChanged (object sender, EventArgs args)
		{
			if (buffer == null)
				AllocateWindowBuffer (this.Allocation);
			
			if (HexEditorData.VAdjustment.Value != System.Math.Ceiling (HexEditorData.VAdjustment.Value)) {
				HexEditorData.VAdjustment.Value = System.Math.Ceiling (HexEditorData.VAdjustment.Value);
				return;
			}
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine  = (long)((HexEditorData.VAdjustment.Value + Allocation.Height) / LineHeight);
			margins.ForEach (margin => margin.SetVisibleWindow (firstVisibleLine, lastVisibleLine));
			
			int delta = (int)(HexEditorData.VAdjustment.Value - this.oldVadjustment);
			oldVadjustment = HexEditorData.VAdjustment.Value;
			
			// update pending redraws
			if (redrawList.Count > 0)
				redrawList = new List<RedrawRequest> (redrawList.Select (request => { 
					Gdk.Rectangle area = request.Area; 
					area.Y -= delta; 
					request.Area = area;
					return request;
				}));
			
			if (System.Math.Abs (delta) >= Allocation.Height - this.LineHeight * 2) {
				this.Repaint ();
				return;
			}
			
			int from, to;
			if (delta > 0) {
				from = delta;
				to   = 0;
			} else {
				from = 0;
				to   = -delta;
			}
			
			DoFlipBuffer ();
			this.buffer.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
			                          this.flipBuffer,
			                          0, from, 
			                          0, to, 
			                          Allocation.Width, Allocation.Height - from - to);
			
			if (delta > 0) {
				delta += LineHeight;
				RenderMargins (buffer, new Gdk.Rectangle (0, Allocation.Height - delta, Allocation.Width, delta), null);
			} else {
				delta -= LineHeight;
				RenderMargins (buffer, new Gdk.Rectangle (0, 0, Allocation.Width, -delta), null);
			}
			ResetCaretBlink ();
			QueueDraw ();
		}
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
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
			SetAdjustments (this.Allocation);
		}
		*/
		
		internal void SetAdjustments ()
		{
			SetAdjustments (Allocation);
		}
		
		void SetHAdjustment ()
		{
			if (HexEditorData.HAdjustment == null)
				return;
/*			textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged;
			if (longestLine != null && this.textEditorData.HAdjustment != null) {
				int maxX = longestLineWidth + 2 * this.textViewMargin.CharWidth;
				int width = Allocation.Width - this.TextViewMargin.XOffset;
				
				this.textEditorData.HAdjustment.SetBounds (0, maxX, this.textViewMargin.CharWidth, width, width);
				if (maxX < width)
					this.textEditorData.HAdjustment.Value = 0;
			}
			textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged;*/
		}
		
		
		internal void SetAdjustments (Gdk.Rectangle allocation)
		{
			if (HexEditorData.VAdjustment == null)
				return;
			long maxY = (HexEditorData.Length / BytesInRow + 1) * LineHeight;
			HexEditorData.VAdjustment.SetBounds (0, maxY, LineHeight, allocation.Height, allocation.Height);
			if (maxY < allocation.Height)
				HexEditorData.VAdjustment.Value = 0;
			if (HexEditorData.VAdjustment.Value > maxY - allocation.Height) {
				HexEditorData.VAdjustment.Value = maxY - allocation.Height;
			}
			SetHAdjustment ();
		}
		#endregion
		
		#region Drawing
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			RenderPendingUpdates (e.Window);
			e.Window.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
			                       buffer,
			                       e.Area.X, e.Area.Y, e.Area.X, e.Area.Y,
			                       e.Area.Width, e.Area.Height + 1);
			if (requestResetCaretBlink) {
				ResetCaretBlink ();
				requestResetCaretBlink = false;
			}
			DrawCaret (e.Window, e.Area);
			return true;
		}
		
		void RenderMargins (Gdk.Drawable win, Gdk.Rectangle area, Margin marginToRender)
		{
			int reminder  = (int)HexEditorData.VAdjustment.Value % LineHeight;
			long firstLine = (long)(HexEditorData.VAdjustment.Value / (long)LineHeight);
			long startLine = (area.Top + reminder) / this.LineHeight;
			long endLine   = (area.Bottom + reminder) / this.LineHeight - 1;
			if ((area.Bottom + reminder) % this.LineHeight != 0)
				endLine++;
			// Initialize the rendering of the margins. Determine wether each margin has to be
			// rendered or not and calculate the X offset.
			List<Margin> marginsToRender = new List<Margin> ();
			if (marginToRender != null) {
				marginsToRender.Add (marginToRender);
			} else {
				int curX = 0;
				foreach (Margin margin in this.margins) {
					if (margin.IsVisible) {
						margin.XOffset = curX;
						if (curX >= area.X || margin.Width < 0)
							marginsToRender.Add (margin);
						curX += margin.Width;
					}
				}
			}
			
			int curY = (int)(startLine * this.LineHeight - reminder);
			
			for (long visualLineNumber = startLine; visualLineNumber <= endLine; visualLineNumber++) {
				long logicalLineNumber = visualLineNumber + firstLine;
				foreach (Margin margin in marginsToRender) {
					try {
						margin.Draw (win, area, logicalLineNumber, margin.XOffset, curY);
					} catch (Exception e) {
						System.Console.WriteLine (e);
					}
				}
				curY += LineHeight;
				if (curY > area.Bottom)
					break;
			}
		}
		
		List<RedrawRequest> redrawList = new List<RedrawRequest> ();
		void RenderPendingUpdates (Gdk.Window window)
		{
			foreach (RedrawRequest request in redrawList) {
				Rectangle updateRect = request.Area;
				RenderMargins (this.buffer, updateRect, request.Margin);
				window.DrawDrawable (Style.BackgroundGC (StateType.Normal), buffer, updateRect.X, updateRect.Y, updateRect.X, updateRect.Y, updateRect.Width, updateRect.Height);
			}
			redrawList.Clear ();
		}
		
		public void RepaintLine (long line)
		{
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine =  (long)(HexEditorData.VAdjustment.Value + Allocation.Height) / LineHeight;
			margins.ForEach (margin => margin.PurgeLayoutCache (line));
			if (firstVisibleLine <= line && line <= lastVisibleLine)
				RepaintArea (0, (int)(line * LineHeight - HexEditorData.VAdjustment.Value), Allocation.Width, LineHeight);
		}
		
		public void RepaintLines (long start, long end)
		{
			long firstVisibleLine = (long)(HexEditorData.VAdjustment.Value / LineHeight);
			long lastVisibleLine =  (long)(HexEditorData.VAdjustment.Value + Allocation.Height) / LineHeight;
			
			start = System.Math.Max (start, firstVisibleLine);
			end = System.Math.Min (end, lastVisibleLine);
			
			for (long line = start; line <= end; line++)
				margins.ForEach (margin => margin.PurgeLayoutCache (line));
			RepaintArea (0, (int)(start * LineHeight - HexEditorData.VAdjustment.Value), Allocation.Width, (int)((end - start) * LineHeight));
		}
		
		public void RepaintArea (int x, int y, int width, int height)
		{
			RepaintMarginArea (null, x, y, width, height);
		}
		
		public void RepaintMarginArea (Margin margin, int x, int y, int width, int height)
		{
			if (this.buffer == null)
				return;
			y = System.Math.Max (0, y);
			height = System.Math.Min (height, Allocation.Height - y);

			x = System.Math.Max (0, x);
			width = System.Math.Min (width, Allocation.Width - x);
			if (height < 0 || width < 0)
				return;
			
			lock (redrawList) {
				redrawList.Add (new RedrawRequest (margin, new Gdk.Rectangle (x, y, width, height)));
			}
			QueueDrawArea (x, y, width, height);
		}
		
		class RedrawRequest 
		{
			public Margin Margin {
				get;
				set;
			}
			
			public Gdk.Rectangle Area {
				get;
				set;
			}
			
			public RedrawRequest (Gdk.Rectangle area)
			{
				this.Area = area;
			}
			
			public RedrawRequest (Margin margin, Gdk.Rectangle area)
			{
				this.Margin = margin;
				this.Area = area;
			}
		}
		
		public void Repaint ()
		{
			if (buffer == null)
				return;
			lock (redrawList) {
				redrawList.Clear ();
				redrawList.Add (new RedrawRequest (new Gdk.Rectangle (0, 0, this.Allocation.Width, this.Allocation.Height)));
			}
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
					caretTimer = new Timer (Gtk.Settings.Default.CursorBlinkTime / 2);
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
		
		Gdk.GC caretGc;
		public void DrawCaret (Gdk.Drawable win, Gdk.Rectangle area)
		{
			if (Settings.Default.CursorBlink && !caretBlink || HexEditorData.IsSomethingSelected) 
				return;
			if (caretGc == null) {
				caretGc = new Gdk.GC (win);
				caretGc.RgbFgColor = new Color (255, 255, 255);
				caretGc.Function = Gdk.Function.Xor;
			}
			long caretY = HexEditorData.Caret.Line * LineHeight - (long)HexEditorData.VAdjustment.Value;
			int caretX;
			if (HexEditorData.Caret.InTextEditor) {
				caretX = textEditorMargin.CalculateCaretXPos ();
			} else {
				caretX = hexEditorMargin.CalculateCaretXPos ();
			}
			
			if (!area.Contains (caretX, (int)caretY))
				return;
			
			if (HexEditorData.Caret.IsInsertMode) {
				win.DrawRectangle (caretGc, true, new Gdk.Rectangle (caretX, (int)caretY, 2, LineHeight));
			} else {
				win.DrawRectangle (caretGc, true, new Gdk.Rectangle (caretX, (int)caretY, textEditorMargin.charWidth, LineHeight));
			}
		}
		
		public void ScrollToCaret ()
		{
			double caretY = HexEditorData.Caret.Offset / BytesInRow * LineHeight;
			HexEditorData.VAdjustment.Value = System.Math.Max (caretY - HexEditorData.VAdjustment.PageSize + LineHeight, System.Math.Min (caretY, HexEditorData.VAdjustment.Value));
		}
		
		#endregion
		
		#region Events
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (IsRealized) {
				AllocateWindowBuffer (Allocation);
				this.CalculateBytesInRow ();
				SetAdjustments (Allocation);
				OnBytesInRowChanged (EventArgs.Empty);
				Repaint ();
			}
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evt)
		{
			uint unicodeChar = Gdk.Keyval.ToUnicode (evt.KeyValue);
			ModifierType filteredModifiers = evt.State & (ModifierType.ShiftMask | ModifierType.Mod1Mask | ModifierType.ControlMask);
			
			HexEditorData.EditMode.InternalHandleKeypress (this, evt.Key, unicodeChar, filteredModifiers);
			
			
			return true;
		}
		
		void CalculateBytesInRow ()
		{
			int oldBytes = BytesInRow;
			int maxWidth = Allocation.Width;
			int start = Options.GroupBytes * 2;
			for (int i = start; i < 100; i += Options.GroupBytes) {
				int width = margins.Sum (margin => margin.CalculateWidth (i));
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
			if (oldBytes != BytesInRow) {
				margins.ForEach (margin => margin.PurgeLayoutCache ());
			}
		}
		
		protected override void OnMapped ()
		{
			if (buffer == null) {
				AllocateWindowBuffer (this.Allocation);
				if (Allocation.Width != 1 || Allocation.Height != 1)
					Repaint ();
			}
			base.OnMapped (); 
		}

		protected override void OnUnmapped ()
		{
			DisposeBgBuffer ();
			base.OnUnmapped (); 
		}
		
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
		
		protected override bool OnFocusInEvent (EventFocus evnt)
		{
			RequestResetCaretBlink ();
			return base.OnFocusInEvent (evnt);
		}
		
		protected override bool OnFocusOutEvent (EventFocus evnt)
		{
			StopCaretThread ();
			return base.OnFocusOutEvent (evnt);
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			OptionsChanged (this, EventArgs.Empty);
			AllocateWindowBuffer (Allocation);
		}
		
		int pressedButton = -1;
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			pressedButton = (int)e.Button;
			base.IsFocus = true;
			Margin margin = GetMarginAtX ((int)e.X);
			if (margin != null) 
				margin.MousePressed (new MarginMouseEventArgs (this, (int)e.Button, (int)(e.X - margin.XOffset), (int)e.Y, e.Type, e.State));
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			pressedButton = -1;
			Margin margin = GetMarginAtX ((int)e.X);
			
			if (margin != null)
				margin.MouseReleased (new MarginMouseEventArgs (this, (int)e.Button, (int)(e.X - margin.XOffset), (int)e.Y, EventType.ButtonRelease, e.State));
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			Margin margin = GetMarginAtX ((int)e.X);
			
			if (margin != null)
				margin.MouseHover (new MarginMouseEventArgs (this, pressedButton, (int)(e.X - margin.XOffset), (int)e.Y, EventType.MotionNotify, e.State));
			return base.OnMotionNotifyEvent (e);
		}
		
		Margin GetMarginAtX (int x)
		{
			return this.margins.FirstOrDefault (margin => margin.XOffset <= x && x < margin.XOffset + margin.Width);
		}
		#endregion
	}
}
