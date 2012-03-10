// 
// QuickTaskStrip.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components.Commands;
using ICSharpCode.NRefactory;

namespace MonoDevelop.SourceEditor
{
	public enum ScrollbarCommand
	{
		Top,
		Bottom,
		PgUp,
		PgDown,
		
		ShowScrollBar,
		ShowMap,
		ShowFull
	}
	
	public enum ScrollBarMode {
		Normal,
		Map,
		Full
	}
	
	public enum QuickTaskSeverity
	{
		None,
		Error,
		Warning,
		Hint,
		Suggestion,
		
		Usage
	}
	
	public interface IQuickTaskProvider
	{
		IEnumerable<QuickTask> QuickTasks {
			get;
		}
		
		event EventHandler TasksUpdated;
	}
	
	public class QuickTask
	{
		public string Description {
			get;
			private set;
		}
		
		public TextLocation Location {
			get;
			private set;
		}
		
		public QuickTaskSeverity Severity {
			get;
			private set;
		}
		
		public QuickTask (string description, TextLocation location, QuickTaskSeverity severity)
		{
			this.Description = description;
			this.Location = location;
			this.Severity = severity;
		}
		
		public override string ToString ()
		{
			return string.Format ("[QuickTask: Description={0}, Location={1}, Severity={2}]", Description, Location, Severity);
		}
	}
	
	public class QuickTaskMapMode : DrawingArea
	{
		QuickTaskStrip parentStrip;
		int caretLine = -1;
		
		public Adjustment VAdjustment {
			get {
				return parentStrip.VAdjustment;
			}
		}
		
		public TextEditor TextEditor {
			get;
			private set;
		}
		
		public IEnumerable<QuickTask> AllTasks {
			get {
				return parentStrip.AllTasks;
			}
		}
		
		public QuickTaskMapMode (QuickTaskStrip parent)
		{
			this.parentStrip = parent;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
			
			VAdjustment.ValueChanged += RedrawOnUpdate;
			VAdjustment.Changed += RedrawOnUpdate;
			parentStrip.TaskProviderUpdated += RedrawOnUpdate;
			TextEditor = parent.TextEditor;
			TextEditor.Caret.PositionChanged += CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged += RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated += RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged += RedrawOnUpdate;
		}
		
		void CaretPositionChanged (object sender, EventArgs e)
		{
			var line = TextEditor.Caret.Line;
			if (caretLine != line) {
				caretLine = line;
				QueueDraw ();
			}
		}
		
		protected override void OnDestroyed ()
		{
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
			TextEditor.Caret.PositionChanged -= CaretPositionChanged;
			
			TextEditor.HighlightSearchPatternChanged -= RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated -= RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged -= RedrawOnUpdate;
			
			parentStrip.TaskProviderUpdated -= RedrawOnUpdate;
			
			VAdjustment.ValueChanged -= RedrawOnUpdate;
			VAdjustment.Changed -= RedrawOnUpdate;
			base.OnDestroyed ();
		}
		
		void RedrawOnUpdate (object sender, EventArgs e)
		{
			QueueDraw ();
		}
		
		internal CodeSegmentPreviewWindow previewWindow;
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			RemovePreviewPopupTimeout ();
			
			if (button != 0)
				MouseMove (evnt.Y);
			
			int h = Allocation.Height - Allocation.Width - 3;
			if (TextEditor.HighlightSearchPattern) {
				if (evnt.Y > h)
					this.TooltipText = string.Format (GettextCatalog.GetPluralString ("{0} match", "{0} matches", TextEditor.TextViewMargin.SearchResultMatchCount), TextEditor.TextViewMargin.SearchResultMatchCount);
			} else { 
				if (evnt.Y > h) {
					int errors = 0, warnings = 0;
					foreach (var task in AllTasks) {
						switch (task.Severity) {
						case QuickTaskSeverity.Error:
							errors++;
							break;
						case QuickTaskSeverity.Warning:
							warnings++;
							break;
						}
					}
					string text = null;
					if (errors == 0 && warnings == 0) {
						text = GettextCatalog.GetString ("No errors or warnings");
					} else if (errors == 0) {
						text = string.Format (GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", warnings), warnings);
					} else if (warnings == 0) {
						text = string.Format (GettextCatalog.GetPluralString ("{0} error", "{0} errors", errors), errors);
					} else {
						text = string.Format (GettextCatalog.GetString ("{0} errors and {1} warnings"), errors, warnings);
					}
					this.TooltipText = text;
				} else {
//					TextEditorData editorData = TextEditor.GetTextEditorData ();
					foreach (var task in AllTasks) {
						double y = LineToY (task.Location.Line);
						if (Math.Abs (y - evnt.Y) < 3) {
							hoverTask = task;
						}
					}
					base.TooltipText = hoverTask != null ? hoverTask.Description : null;
				}
			}
			
			if (button == 0 && evnt.State.HasFlag (ModifierType.ShiftMask)) {
				int line = YToLine (evnt.Y);
				
				line = Math.Max (1, line - 2);
				int lastLine = Math.Min (TextEditor.LineCount, line + 5);
				var start = TextEditor.GetLine (line);
				var end = TextEditor.GetLine (lastLine);
				if (start == null || end == null) {
					return base.OnMotionNotifyEvent (evnt);
				}
				var showSegment = new Mono.TextEditor.Segment (start.Offset, end.Offset + end.EditableLength - start.Offset);
				
				if (previewWindow != null) {
					previewWindow.SetSegment (showSegment, false);
					PositionPreviewWindow ((int)evnt.Y);
				} else {
					var popup = new PreviewPopup (this, showSegment, TextEditor.Allocation.Width * 4 / 7, (int)evnt.Y);
					previewPopupTimeout = GLib.Timeout.Add (450, new GLib.TimeoutHandler (popup.Run));
				}
			} else {
				RemovePreviewPopupTimeout ();
				DestroyPreviewWindow ();
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		class PreviewPopup {
			
			QuickTaskMapMode strip;
			Mono.TextEditor.Segment segment;
			int w, y;
			
			public PreviewPopup (QuickTaskMapMode strip, Mono.TextEditor.Segment segment, int w, int y)
			{
				this.strip = strip;
				this.segment = segment;
				this.w = w;
				this.y = y;
			}
			
			public bool Run ()
			{
				strip.previewWindow = new CodeSegmentPreviewWindow (strip.TextEditor, true, segment, w, -1, false);
				strip.previewWindow.WidthRequest = w;
				strip.previewWindow.Show ();
				strip.PositionPreviewWindow (y);
				return false;
			}
			
		}
		
		uint previewPopupTimeout = 0;
		
		void PositionPreviewWindow (int my)
		{
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy);
			
			Gdk.Rectangle geometry = Screen.GetMonitorGeometry (Screen.GetMonitorAtPoint (ox, oy));
			
			var alloc = previewWindow.Allocation;
			int x = ox - 4 - alloc.Width;
			if (x < geometry.Left)
				x = ox + parentStrip.Allocation.Width + 4;
			
			int y = oy + my - alloc.Height / 2;
			y = Math.Max (geometry.Top, Math.Min (y, geometry.Bottom));
			
			previewWindow.Move (x, y);
		}
		
		void RemovePreviewPopupTimeout ()
		{
			if (previewPopupTimeout != 0) {
				GLib.Source.Remove (previewPopupTimeout);
				previewPopupTimeout = 0;
			}
		}
		
		void DestroyPreviewWindow ()
		{
			if (previewWindow != null) {
				previewWindow.Destroy ();
				previewWindow = null;
			}
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		Cairo.Color GetBarColor (QuickTaskSeverity severity)
		{
			var style = this.TextEditor.ColorStyle;
			if (style == null)
				return new Cairo.Color (0, 0, 0);
			switch (severity) {
			case QuickTaskSeverity.Error:
				return style.ErrorUnderline;
			case QuickTaskSeverity.Warning:
				return style.WarningUnderline;
			case QuickTaskSeverity.Suggestion:
				return style.SuggestionUnderline;
			case QuickTaskSeverity.Hint:
				return style.HintUnderline;
			case QuickTaskSeverity.None:
				return style.Default.CairoColor;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}
		
		Cairo.Color GetIndicatorColor (QuickTaskSeverity severity)
		{
			var style = this.TextEditor.ColorStyle;
			if (style == null)
				return new Cairo.Color (0, 0, 0);
			switch (severity) {
			case QuickTaskSeverity.Error:
				return style.ErrorUnderline;
			case QuickTaskSeverity.Warning:
				return style.WarningUnderline;
			default:
				return style.SuggestionUnderline;
			}
		}
		protected virtual double IndicatorHeight  {
			get {
				return Allocation.Width;
			}
		}
		
		void MouseMove (double y)
		{
			if (button != 1)
				return;
			double position = (y / (Allocation.Height - IndicatorHeight)) * VAdjustment.Upper - VAdjustment.PageSize / 2;
			position = Math.Max (VAdjustment.Lower, Math.Min (position, VAdjustment.Upper - VAdjustment.PageSize));
			VAdjustment.Value = position;
		}
		
		QuickTask hoverTask = null;
		
		uint button;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			button |= evnt.Button;
			
			if (!evnt.TriggersContextMenu () && evnt.Button == 1 && hoverTask != null) {
				TextEditor.Caret.Location = new DocumentLocation (hoverTask.Location.Line, Math.Max (DocumentLocation.MinColumn, hoverTask.Location.Column));
				TextEditor.CenterToCaret ();
				TextEditor.StartCaretPulseAnimation ();
				TextEditor.GrabFocus ();
			} 
			
			MouseMove (evnt.Y);
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			button &= ~evnt.Button;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected void DrawIndicator (Cairo.Context cr, QuickTaskSeverity severity)
		{
			cr.Rectangle (3, Allocation.Height - IndicatorHeight + 4, Allocation.Width - 6, IndicatorHeight - 6);
			
			var darkColor = (HslColor)GetIndicatorColor (severity);
			darkColor.L *= 0.5;
			
			using (var pattern = new Cairo.LinearGradient (0, 0, Allocation.Width - 3, IndicatorHeight)) {
				pattern.AddColorStop (0, darkColor);
				pattern.AddColorStop (1, GetIndicatorColor (severity));
				cr.Pattern = pattern;
				cr.FillPreserve ();
			}
			
			cr.Color = darkColor;
			cr.Stroke ();
		}

		protected void DrawSearchIndicator (Cairo.Context cr)
		{
			var x1 = 1 + Allocation.Width / 2;
			var y1 = Allocation.Height - IndicatorHeight / 2;
			cr.Arc (x1, 
				y1, 
				(IndicatorHeight - 1) / 2, 
				0, 
				2 * Math.PI);
			
			var darkColor = (HslColor)TextEditor.ColorStyle.SearchTextBg;
			darkColor.L *= 0.5;
			
			using (var pattern = new Cairo.RadialGradient (x1, y1, Allocation.Width / 2, x1 - Allocation.Width, y1 - Allocation.Width, Allocation.Width)) {
				pattern.AddColorStop (0, darkColor);
				pattern.AddColorStop (1, TextEditor.ColorStyle.SearchTextMainBg);
				cr.Pattern = pattern;
				cr.FillPreserve ();
			}
			
			cr.Color = darkColor;
			cr.Stroke ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 15;
		}
		
		double LineToY (int logicalLine)
		{
			var h = Allocation.Height - IndicatorHeight;
			var p = TextEditor.LocationToPoint (logicalLine, 1, true).Y;
			var q = Math.Max (TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height - IndicatorHeight);

			return h * p / q;
		}
		
		int YToLine (double y)
		{
			var line = 0.5 + y / (Allocation.Height - IndicatorHeight) * (double)(TextEditor.GetTextEditorData ().VisibleLineCount);
			return TextEditor.GetTextEditorData ().VisualToLogicalLine ((int)line);
		}
		
		protected void DrawCaret (Cairo.Context cr)
		{
			if (TextEditor.ColorStyle == null)
				return;
			double y = LineToY (caretLine);
			cr.MoveTo (0, y - 4);
			cr.LineTo (7, y);
			cr.LineTo (0, y + 4);
			cr.ClosePath ();
			cr.Color = TextEditor.ColorStyle.Default.CairoColor;
			cr.Fill ();
		}
		
		protected QuickTaskSeverity DrawQuickTasks (Cairo.Context cr)
		{
			QuickTaskSeverity severity = QuickTaskSeverity.None;
			foreach (var task in AllTasks) {
				double y = LineToY (task.Location.Line);
				
				if (task.Severity == QuickTaskSeverity.Usage) {
					var usageColor = TextEditor.ColorStyle.Default.CairoColor;
					usageColor.A = 0.4;
					cr.Color = usageColor;
					cr.MoveTo (0, y - 3);
					cr.LineTo (5, y);
					cr.LineTo (0, y + 3);
					cr.ClosePath ();
					cr.Fill ();
					continue;
				}
				
				var color = (HslColor)GetBarColor (task.Severity);
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.FillPreserve ();
				
				color.L *= 0.7;
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.Stroke ();
				
				switch (task.Severity) {
				case QuickTaskSeverity.Error:
					severity = QuickTaskSeverity.Error;
					break;
				case QuickTaskSeverity.Warning:
					if (severity == QuickTaskSeverity.None)
						severity = QuickTaskSeverity.Warning;
					break;
				}
			}
			return severity;
		}
		
		protected void DrawLeftBorder (Cairo.Context cr)
		{
			cr.MoveTo (0.5, 1.5);
			cr.LineTo (0.5, Allocation.Height);
			if (TextEditor.ColorStyle != null) {
				var col = (HslColor)TextEditor.ColorStyle.Default.CairoBackgroundColor;
				col.L *= 0.90;
				cr.Color = col;
				
			}
			cr.Stroke ();
			
		}
		
		protected virtual void DrawBar (Cairo.Context cr)
		{
			if (VAdjustment == null || VAdjustment.Upper <= VAdjustment.PageSize) 
				return;
			var h = Allocation.Height - IndicatorHeight;
			
			const int barWidth = 8;
			
			MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr, 
				0.5 +(Allocation.Width - barWidth) / 2,
				h * VAdjustment.Value / VAdjustment.Upper + cr.LineWidth + 0.5,
				barWidth,
				h * (VAdjustment.PageSize / VAdjustment.Upper),
				barWidth / 2);
			
			var color = (HslColor)((TextEditor.ColorStyle != null) ? TextEditor.ColorStyle.Default.CairoColor : new Cairo.Color (0, 0, 0));
			color.L = 0.5;
			var c = (Cairo.Color)color;
			c.A = 0.6;
			cr.Color = c;
			cr.Fill ();
		}
		
		protected void DrawSearchResults (Cairo.Context cr)
		{
			foreach (var region in TextEditor.TextViewMargin.SearchResults) {
				int line = TextEditor.OffsetToLineNumber (region.Offset);
				double y = LineToY (line);
				bool isMainSelection = false;
				if (TextEditor.TextViewMargin.MainSearchResult != null)
					isMainSelection = region.Offset == TextEditor.TextViewMargin.MainSearchResult.Offset;
				var color = (HslColor)(isMainSelection ? TextEditor.ColorStyle.SearchTextMainBg : TextEditor.ColorStyle.SearchTextBg);
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.FillPreserve ();
			
				color.L *= 0.7;
				cr.Color = color;
				cr.Rectangle (3, y - 1, Allocation.Width - 5, 4);
				cr.Stroke ();
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (TextEditor == null)
				return true;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				
				if (TextEditor.ColorStyle != null) {
					var grad = new Cairo.LinearGradient (0, 0, Allocation.Width, 0);
					var col = (HslColor)TextEditor.ColorStyle.Default.CairoBackgroundColor;
					col.L *= 0.95;
					grad.AddColorStop (0, col);
					grad.AddColorStop (0.7, TextEditor.ColorStyle.Default.CairoBackgroundColor);
					grad.AddColorStop (1, col);
					cr.Pattern = grad;
				}
				cr.Fill ();
				
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (-0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);
				cr.Stroke ();
				
				if (TextEditor == null)
					return true;
				
				if (TextEditor.HighlightSearchPattern) {
					DrawSearchResults (cr);
					DrawSearchIndicator (cr);
				} else {
					var severity = DrawQuickTasks (cr);
					DrawIndicator (cr, severity);
				}
				DrawCaret (cr);
				
				DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}
	}
	
	public class QuickTaskFullMode : QuickTaskMapMode
	{
		Pixmap backgroundPixbuf, backgroundBuffer;
		uint redrawTimeout;
		Document doc;
		protected override double IndicatorHeight  {
			get {
				return 16;
			}
		}
		
		public QuickTaskFullMode (QuickTaskStrip parent) : base (parent)
		{
			doc = parent.TextEditor.Document;
			doc.TextReplaced += TextReplaced;
			doc.Folded += HandleFolded;
		}

		void HandleFolded (object sender, FoldSegmentEventArgs e)
		{
			RequestRedraw ();
		}
		
		void TextReplaced (object sender, DocumentChangeEventArgs args)
		{
			RequestRedraw ();
		}

		public void RemoveRedrawTimer ()
		{
			if (redrawTimeout != 0) {
				GLib.Source.Remove (redrawTimeout);
				redrawTimeout = 0;
			}
		}

		void RequestRedraw ()
		{
			RemoveRedrawTimer ();
			redrawTimeout = GLib.Timeout.Add (450, delegate {
				if (curUpdate != null) {
					curUpdate.RemoveHandler ();
					curUpdate = null;
				}
				if (backgroundPixbuf != null)
					curUpdate = new BgBufferUpdate (this);
				redrawTimeout = 0;
				return false;
			});
		}
		
		protected override void DrawBar (Cairo.Context cr)
		{
			if (VAdjustment == null || VAdjustment.Upper <= VAdjustment.PageSize) 
				return;
			var h = Allocation.Height - IndicatorHeight;
			cr.Rectangle (1.5,
				              h * VAdjustment.Value / VAdjustment.Upper + cr.LineWidth + 0.5,
				              Allocation.Width - 2,
				              h * (VAdjustment.PageSize / VAdjustment.Upper));
			Cairo.Color color = (TextEditor.ColorStyle != null) ? TextEditor.ColorStyle.Default.CairoColor : new Cairo.Color (0, 0, 0);
			color.A = 0.5;
			cr.Color = color;
			cr.StrokePreserve ();
			
			color.A = 0.05;
			cr.Color = color;
			cr.Fill ();
		}
	
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 164;
		}
		
		void DestroyBgBuffer ()
		{
			if (curUpdate != null)
				curUpdate.RemoveHandler ();
			if (backgroundPixbuf != null) {
				backgroundPixbuf.Dispose ();
				backgroundBuffer.Dispose ();
				backgroundPixbuf = backgroundBuffer = null;
				curWidth = curHeight = -1;
			}
		}
		
		protected override void OnDestroyed ()
		{
			doc.Folded -= HandleFolded;
			doc.TextReplaced -= TextReplaced;
			RemoveRedrawTimer ();
			DestroyBgBuffer ();
			base.OnDestroyed ();
		}
		
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (allocation.Width > 1 && (allocation.Width != curWidth || allocation.Height != curHeight))
				CreateBgBuffer ();
		}
		
		protected override void OnMapped ()
		{
			if (backgroundPixbuf == null && Allocation.Width > 1)
				CreateBgBuffer ();
			base.OnMapped ();
		}
		
		protected override void OnUnmapped ()
		{
			DestroyBgBuffer ();
			base.OnUnmapped ();
		}
		
		BgBufferUpdate curUpdate = null;
		void SwapBuffer ()
		{
			var tmp = backgroundPixbuf;
			backgroundPixbuf = backgroundBuffer;
			backgroundBuffer = tmp;
		}
		
		int curWidth = -1, curHeight = -1;
		void CreateBgBuffer ()
		{
			DestroyBgBuffer ();
			curWidth = Allocation.Width;
			curHeight = Allocation.Height;
			backgroundPixbuf = new Pixmap (GdkWindow, curWidth, curHeight);
			backgroundBuffer = new Pixmap (GdkWindow, curWidth, curHeight);
			
			if (TextEditor.ColorStyle != null) {
				using (var cr = Gdk.CairoHelper.Create (backgroundPixbuf)) {
					cr.Rectangle (0, 0, curWidth, curHeight);
					cr.Color = TextEditor.ColorStyle.Default.CairoBackgroundColor;
					cr.Fill ();
				}
			}
			curUpdate = new BgBufferUpdate (this);
		}
		
		class BgBufferUpdate {
			int maxLine;
			double sx;
			double sy;
			uint handler;
			
			Cairo.Context cr;
			
			QuickTaskFullMode mode;
			
			int curLine = 1;
			
			public BgBufferUpdate (QuickTaskFullMode mode)
			{
				this.mode = mode;
				
				cr = Gdk.CairoHelper.Create (mode.backgroundBuffer);
				
				cr.LineWidth = 1;
				cr.Rectangle (0, 0, mode.Allocation.Width, mode.Allocation.Height);
				if (mode.TextEditor.ColorStyle != null)
					cr.Color = mode.TextEditor.ColorStyle.Default.CairoBackgroundColor;
				cr.Fill ();
				
				maxLine = mode.TextEditor.GetTextEditorData ().VisibleLineCount;
				sx = mode.Allocation.Width / (double)mode.TextEditor.Allocation.Width;
				sy = Math.Min (1, (mode.Allocation.Height - mode.IndicatorHeight) / (double)mode.TextEditor.GetTextEditorData ().TotalHeight);
				cr.Scale (sx, sy);
				
				handler = GLib.Idle.Add (BgBufferUpdater);
			}
			
			public void RemoveHandler ()
			{
				if (cr == null)
					return;
				GLib.Source.Remove (handler);
				handler = 0;
				((IDisposable)cr).Dispose ();
				cr = null;
				mode.curUpdate = null;
			}
		
			bool BgBufferUpdater ()
			{
				if (mode.TextEditor.Document == null || handler == 0)
					return false;
				try {
					for (int i = 0; i < 25 && curLine < maxLine; i++) {
						var nr = mode.TextEditor.GetTextEditorData ().VisualToLogicalLine (curLine);
						var line = mode.TextEditor.GetLine (nr);
						if (line != null) {
							var layout = mode.TextEditor.TextViewMargin.GetLayout (line);
							cr.MoveTo (0, (curLine - 1) * mode.TextEditor.LineHeight);
							cr.ShowLayout (layout.Layout);
								
							if (layout.IsUncached)
								layout.Dispose ();
						}
						
						curLine++;
					}
					
					if (curLine >= maxLine) {
						mode.SwapBuffer ();
						((IDisposable)cr).Dispose ();
						cr = null;
						mode.curUpdate = null;
						mode.QueueDraw ();
						return false;
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error in background buffer drawer.", e);
					return false;
				}
				return true;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (TextEditor == null)
				return true;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
				cr.LineWidth = 1;
				if (backgroundPixbuf != null) {
					e.Window.DrawDrawable (Style.BlackGC, backgroundPixbuf, 0, 0, 0, 0, Allocation.Width, Allocation.Height);
				} else {
					cr.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					if (TextEditor.ColorStyle != null)
						cr.Color = TextEditor.ColorStyle.Default.CairoBackgroundColor;
					cr.Fill ();
				}
				
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);
				cr.Stroke ();
				
				if (TextEditor.HighlightSearchPattern) {
					DrawSearchResults (cr);
					DrawSearchIndicator (cr);
				} else {
					var severity = DrawQuickTasks (cr);
					DrawIndicator (cr, severity);
				}
				
				DrawCaret (cr);
				DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}
	}
	
	
	public class QuickTaskStrip : VBox
	{
		Adjustment adj;
		
		public Adjustment VAdjustment {
			get {
				return this.adj;
			}
			set {
				adj = value;
				SetupMode ();
			}
		}
		
		Mono.TextEditor.TextEditor textEditor;
		public TextEditor TextEditor {
			get {
				return textEditor;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ();
				textEditor = value;
				SetupMode ();
			}
		}
		
		ScrollBarMode mode;
		public ScrollBarMode ScrollBarMode {
			get {
				return this.mode;
			}
			set {
				mode = value;
				PropertyService.Set ("ScrollBar.Mode", value);
				SetupMode ();
			}
		}
		
		Dictionary<IQuickTaskProvider, List<QuickTask>> providerTasks = new Dictionary<IQuickTaskProvider, List<QuickTask>> ();
		
		public IEnumerable<QuickTask> AllTasks {
			get {
				if (providerTasks == null)
					yield break;
				foreach (var tasks in providerTasks.Values) {
					foreach (var task in tasks) {
						yield return task;
					}
				}
			}
		}
		
		public QuickTaskStrip ()
		{
			ScrollBarMode = ScrollBarMode.Normal; //PropertyService.Get ("ScrollBar.Mode", ScrollBarMode.Map);
//			PropertyService.AddPropertyHandler ("ScrollBar.Mode", ScrollBarModeChanged);
			Events |= EventMask.ButtonPressMask;
		}
		
		VScrollbar vScrollBar;
		QuickTaskMapMode mapMode;
		void SetupMode ()
		{
			if (adj == null || textEditor == null)
				return;
			if (vScrollBar != null) {
				vScrollBar.Destroy ();
				vScrollBar = null;
			}
			
			if (mapMode != null) {
				mapMode.Destroy ();
				mapMode = null;
			}
			switch (ScrollBarMode) {
			case ScrollBarMode.Normal:
				vScrollBar = new VScrollbar (adj);
				PackStart (vScrollBar, true, true, 0);
				break;
			case ScrollBarMode.Map:
				mapMode = new QuickTaskMapMode (this);
				PackStart (mapMode, true, true, 0);
				break;
			case ScrollBarMode.Full:
				mapMode = new QuickTaskFullMode (this);
				PackStart (mapMode, true, true, 0);
				break;
			default:
				throw new ArgumentOutOfRangeException ();
			}
			ShowAll ();
		}
		
		protected override void OnDestroyed ()
		{
			adj = null;
			textEditor = null;
			providerTasks = null;
			PropertyService.RemovePropertyHandler ("ScrollBar.Mode", ScrollBarModeChanged);
			base.OnDestroyed ();
		}
		
		void ScrollBarModeChanged (object sender, PropertyChangedEventArgs args)
		{
			var newMode =  (ScrollBarMode)args.NewValue;
			if (newMode == this.ScrollBarMode)
				return;
			this.ScrollBarMode = newMode;
		}
		
		public void Update (IQuickTaskProvider provider)
		{
			if (providerTasks == null)
				return;
			providerTasks [provider] = new List<QuickTask> (provider.QuickTasks);
			OnTaskProviderUpdated (EventArgs.Empty);
		}

		protected virtual void OnTaskProviderUpdated (EventArgs e)
		{
			var handler = this.TaskProviderUpdated;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler TaskProviderUpdated;
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
		/*	if (evnt.Button == 3) {
				var cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/SourceEditor2/ContextMenu/Scrollbar");
				IdeApp.CommandService.ShowContextMenu (cset, this);
			}*/
			return base.OnButtonPressEvent (evnt);
		}
		
		#region Command handlers
		[CommandHandler (ScrollbarCommand.Top)]
		void GotoTop ()
		{
			VAdjustment.Value = VAdjustment.Lower;
		}
		
		[CommandHandler (ScrollbarCommand.Bottom)]
		void GotoBottom ()
		{
			VAdjustment.Value = Math.Max (VAdjustment.Lower, VAdjustment.Upper - VAdjustment.PageSize / 2);
		}
		
		[CommandHandler (ScrollbarCommand.PgUp)]
		void GotoPgUp ()
		{
			VAdjustment.Value = Math.Max (VAdjustment.Lower, VAdjustment.Value - VAdjustment.PageSize);
		}
		
		[CommandHandler (ScrollbarCommand.PgDown)]
		void GotoPgDown ()
		{
			VAdjustment.Value = Math.Min (VAdjustment.Upper, VAdjustment.Value + VAdjustment.PageSize);
		}
		
		[CommandUpdateHandler (ScrollbarCommand.ShowScrollBar)]
		void UpdateShowScrollBar (CommandInfo info)
		{
			info.Checked = ScrollBarMode == ScrollBarMode.Normal;
		}
		
		[CommandHandler (ScrollbarCommand.ShowScrollBar)]
		void ShowScrollBar ()
		{
			 ScrollBarMode = ScrollBarMode.Normal; 
		}
		
		[CommandUpdateHandler (ScrollbarCommand.ShowMap)]
		void UpdateShowMap (CommandInfo info)
		{
			info.Checked = ScrollBarMode == ScrollBarMode.Map;
		}
		
		[CommandHandler (ScrollbarCommand.ShowMap)]
		void ShowMap ()
		{
			 ScrollBarMode = ScrollBarMode.Map; 
		}
		
		[CommandUpdateHandler (ScrollbarCommand.ShowFull)]
		void UpdateShowFull (CommandInfo info)
		{
			info.Checked = ScrollBarMode == ScrollBarMode.Full;
		}
		
		[CommandHandler (ScrollbarCommand.ShowFull)]
		void ShowFull ()
		{
			 ScrollBarMode = ScrollBarMode.Full; 
		}
		#endregion
	}
}
