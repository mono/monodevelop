// 
// QuickTaskMapMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	public class QuickTaskOverviewMode : DrawingArea
	{
		const int indicatorPadding = 3;
		bool flatStyle = Platform.IsWindows;
		int barPadding = Platform.IsWindows? 1 : 3;

		readonly QuickTaskStrip parentStrip;
		protected readonly Adjustment vadjustment;
		
		int caretLine = -1;
		
		public TextEditor TextEditor {
			get;
			private set;
		}
		
		public IEnumerable<QuickTask> AllTasks {
			get {
				return parentStrip.AllTasks;
			}
		}

		public IEnumerable<TextLocation> AllUsages {
			get {
				return parentStrip.AllUsages;
			}
		}
		
		public QuickTaskOverviewMode (QuickTaskStrip parent)
		{
			this.parentStrip = parent;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask | EventMask.PointerMotionMask | EventMask.LeaveNotifyMask;
			vadjustment = this.parentStrip.VAdjustment;

			vadjustment.ValueChanged += RedrawOnUpdate;
			vadjustment.Changed += RedrawOnUpdate;
			parentStrip.TaskProviderUpdated += RedrawOnUpdate;
			TextEditor = parent.TextEditor;
//			TextEditor.Caret.PositionChanged += CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged += RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated += RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged += RedrawOnUpdate;
			TextEditor.GetTextEditorData ().HeightTree.LineUpdateFrom += HandleLineUpdateFrom;
			TextEditor.HighlightSearchPatternChanged += HandleHighlightSearchPatternChanged;
		}

		void HandleHighlightSearchPatternChanged (object sender, EventArgs e)
		{
			yPositionCache.Clear ();
		}

		void HandleLineUpdateFrom (object sender, HeightTree.HeightChangedEventArgs e)
		{
			yPositionCache.Clear ();
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
			base.OnDestroyed ();
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
			TextEditor.Caret.PositionChanged -= CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged -= HandleHighlightSearchPatternChanged;
			TextEditor.GetTextEditorData ().HeightTree.LineUpdateFrom -= HandleLineUpdateFrom;
			TextEditor.HighlightSearchPatternChanged -= RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated -= RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged -= RedrawOnUpdate;
			
			parentStrip.TaskProviderUpdated -= RedrawOnUpdate;
			
			vadjustment.ValueChanged -= RedrawOnUpdate;
			vadjustment.Changed -= RedrawOnUpdate;
		}
		
		void RedrawOnUpdate (object sender, EventArgs e)
		{
			QueueDraw ();
		}
		
		internal CodeSegmentPreviewWindow previewWindow;


		bool hoverOverIndicator;
		QuickTaskStrip.HoverMode currentHoverMode;
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			RemovePreviewPopupTimeout ();
			if (button != 0)
				MouseMove (evnt.Y);
			
			var h = IndicatorHeight;
			if (TextEditor.HighlightSearchPattern) {
				hoverOverIndicator = false;
				if (evnt.Y < h)
					this.TooltipText = string.Format (GettextCatalog.GetPluralString ("{0} match", "{0} matches", TextEditor.TextViewMargin.SearchResultMatchCount), TextEditor.TextViewMargin.SearchResultMatchCount);
			} else {
				hoverOverIndicator = evnt.Y < h;
				if (hoverOverIndicator) {
					int errors = 0, warnings = 0, hints = 0, suggestions = 0;
					foreach (var task in AllTasks) {
						switch (task.Severity) {
						case Severity.Error:
							errors++;
							break;
						case Severity.Warning:
							warnings++;
							break;
						case Severity.Hint:
							hints++;
							break;
						case Severity.Suggestion:
							suggestions++;
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

					if (errors > 0) {
						text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next error");
						currentHoverMode = QuickTaskStrip.HoverMode.NextError;
					} else if (warnings > 0) {
						text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next warning");
						currentHoverMode = QuickTaskStrip.HoverMode.NextWarning;
					} else if (warnings + hints > 0) {
						text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next message");
						currentHoverMode = QuickTaskStrip.HoverMode.NextMessage;
					}

					TooltipText = text;
				} else {
//					TextEditorData editorData = TextEditor.GetTextEditorData ();
					foreach (var task in AllTasks) {
						double y = GetYPosition (task.Location.Line);
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
				var showSegment = new TextSegment (start.Offset, end.Offset + end.Length - start.Offset);
				
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
			
			QuickTaskOverviewMode strip;
			TextSegment segment;
			int w, y;
			
			public PreviewPopup (QuickTaskOverviewMode strip, TextSegment segment, int w, int y)
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
		
		Cairo.Color GetBarColor (Severity severity)
		{
			var style = this.TextEditor.ColorStyle;
			if (style == null)
				return new Cairo.Color (0, 0, 0);
			switch (severity) {
			case Severity.Error:
				return style.UnderlineError.Color;
			case Severity.Warning:
				return style.UnderlineWarning.Color;
			case Severity.Suggestion:
				return style.UnderlineSuggestion.Color;
			case Severity.Hint:
				return style.UnderlineHint.Color;
			case Severity.None:
				return style.PlainText.Background;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		Gdk.Pixbuf GetIndicatorIcon (Severity severity)
		{
			switch (severity) {
			case Severity.Error:
				return ImageService.GetPixbuf ("md-issuestatus-error", IconSize.Menu);
			case Severity.Warning:
				return ImageService.GetPixbuf ("md-issuestatus-warning", IconSize.Menu);
			default:
				return ImageService.GetPixbuf ("md-issuestatus-ok", IconSize.Menu);
			}
		}

		protected virtual double IndicatorHeight  {
			get {
				return Allocation.Width;
			}
		}
		
		protected virtual void MouseMove (double y)
		{
			if ((button & 1) == 0)
				return;
			double position = ((y - IndicatorHeight) / (Allocation.Height - IndicatorHeight)) * vadjustment.Upper - vadjustment.PageSize / 2;
			position = Math.Max (vadjustment.Lower, Math.Min (position, vadjustment.Upper - vadjustment.PageSize));
			vadjustment.Value = position;
		}

		QuickTask hoverTask = null;
		
		protected uint button;


		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			button |= evnt.Button;

			if (!evnt.TriggersContextMenu () && evnt.Button == 1 && evnt.Type == EventType.ButtonPress) {
				if (hoverOverIndicator) {
					parentStrip.GotoTask (parentStrip.SearchNextTask (currentHoverMode));
					return base.OnButtonPressEvent (evnt);
				}

				if (hoverTask != null) {
					if (hoverTask.Location.IsEmpty) {
						Console.WriteLine ("empty:"+ hoverTask.Description);
					}
					var loc = new DocumentLocation (Math.Max (DocumentLocation.MinLine, hoverTask.Location.Line), Math.Max (DocumentLocation.MinColumn, hoverTask.Location.Column));
					TextEditor.Caret.Location = loc;
					TextEditor.CenterToCaret ();
					TextEditor.StartCaretPulseAnimation ();
					TextEditor.GrabFocus ();
				} 
			}

			if (evnt.Type == EventType.ButtonPress)
				MouseMove (evnt.Y);
			
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			button &= ~evnt.Button;
			return base.OnButtonReleaseEvent (evnt);
		}

		protected void DrawIndicator (Cairo.Context cr, Severity severity)
		{
			cr.Save ();
			var pixbuf = GetIndicatorIcon (severity);
			cr.Translate (
				1 + (Allocation.Width - pixbuf.Width) / 2,
				1
			);

			CairoHelper.SetSourcePixbuf (
				cr,
				pixbuf,
				0, 0
			);
			cr.Paint ();
			cr.Restore ();
		}

		protected void DrawSearchIndicator (Cairo.Context cr)
		{
			int diameter = Math.Min (Allocation.Width, (int)IndicatorHeight) - indicatorPadding * 2;
			var x1 = Math.Round (Allocation.Width / 2d);
			double y1 = indicatorPadding + diameter / 2;
			if (diameter % 2 == 0) {
				x1 += 0.5;
				y1 += 0.5;
			}

			cr.Arc (x1, y1, diameter / 2d, 0, 2 * Math.PI);
			
			var darkColor = (HslColor)TextEditor.ColorStyle.SearchResult.Color;
			darkColor.L *= 0.5;

			if (flatStyle) {
				using (var pattern = new Cairo.SolidPattern (TextEditor.ColorStyle.SearchResultMain.Color)) {
					cr.SetSource (pattern);
					cr.FillPreserve ();
				}
			} else {
				using (var pattern = new Cairo.RadialGradient (x1, y1, Allocation.Width / 2, x1 - Allocation.Width, y1 - Allocation.Width, Allocation.Width)) {
					pattern.AddColorStop (0, darkColor);
					pattern.AddColorStop (1, TextEditor.ColorStyle.SearchResultMain.Color);
					cr.SetSource (pattern);
					cr.FillPreserve ();
				}
			}
			
			cr.SetSourceColor (darkColor);
			cr.Stroke ();
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 17;
		}
		
		double LineToY (int logicalLine)
		{
			var h = Allocation.Height - IndicatorHeight;
			var p = TextEditor.LocationToPoint (logicalLine, 1, true).Y;
			var q = Math.Max (TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height - IndicatorHeight);

			return IndicatorHeight  + h * p / q;
		}
		
		int YToLine (double y)
		{
			var line = 0.5 + (y - IndicatorHeight) / (Allocation.Height - IndicatorHeight) * (double)(TextEditor.GetTextEditorData ().VisibleLineCount);
			return TextEditor.GetTextEditorData ().VisualToLogicalLine ((int)line);
		}
		
		protected void DrawCaret (Cairo.Context cr)
		{
			if (TextEditor.ColorStyle == null || caretLine < 0)
				return;
			double y = GetYPosition (caretLine);
			cr.MoveTo (0, y - 4);
			cr.LineTo (7, y);
			cr.LineTo (0, y + 4);
			cr.ClosePath ();
			cr.SetSourceColor (TextEditor.ColorStyle.PlainText.Foreground);
			cr.Fill ();
		}

		Dictionary<int, double> yPositionCache = new Dictionary<int, double> ();

		double GetYPosition (int logicalLine)
		{
			double y;
			if (!yPositionCache.TryGetValue (logicalLine, out y))
				yPositionCache [logicalLine] = y = LineToY (logicalLine);
			return y;
		}

		protected Severity DrawQuickTasks (Cairo.Context cr)
		{
			Severity severity = Severity.None;
			/*
			foreach (var usage in AllUsages) {
				double y = GetYPosition (usage.Line);
				var usageColor = TextEditor.ColorStyle.PlainText.Foreground;
				usageColor.A = 0.4;
				cr.Color = usageColor;
				cr.MoveTo (0, y - 3);
				cr.LineTo (5, y);
				cr.LineTo (0, y + 3);
				cr.ClosePath ();
				cr.Fill ();
			}
*/
			foreach (var task in AllTasks) {
				double y = GetYPosition (task.Location.Line);

				cr.SetSourceColor (GetBarColor (task.Severity));
				cr.Rectangle (barPadding, Math.Round (y) - 1, Allocation.Width - barPadding * 2, 2);
				cr.Fill ();

				switch (task.Severity) {
				case Severity.Error:
					severity = Severity.Error;
					break;
				case Severity.Warning:
					if (severity == Severity.None)
						severity = Severity.Warning;
					break;
				}
			}
			return severity;
		}
		
		protected void DrawLeftBorder (Cairo.Context cr)
		{
			cr.MoveTo (0.5, 0);
			cr.LineTo (0.5, Allocation.Height);
			if (TextEditor.ColorStyle != null) {
				var col = (HslColor)TextEditor.ColorStyle.PlainText.Background;
				if (!flatStyle) {
					col.L *= 0.88;
				}
				cr.SetSourceColor (col);
			}
			cr.Stroke ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			yPositionCache.Clear ();
			base.OnSizeAllocated (allocation);
		}

		protected virtual void DrawBar (Cairo.Context cr)
		{
			if (vadjustment == null || vadjustment.Upper <= vadjustment.PageSize) 
				return;

			int barWidth = Allocation.Width - barPadding - barPadding;
			var allocH = Allocation.Height - (int) IndicatorHeight;
			var adjUpper = vadjustment.Upper;
			var barY = IndicatorHeight + Math.Round (allocH * vadjustment.Value / adjUpper) + barPadding;
			const int minBarHeight = 16;
			var barH = Math.Max (minBarHeight, Math.Round (allocH * (vadjustment.PageSize / adjUpper)) - barPadding - barPadding);

			if (flatStyle) {
				cr.Rectangle (barPadding, barY, barWidth, barH);
			} else {
				MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr, barPadding, barY, barWidth, barH, barWidth / 2);
			}
			
			var color = (HslColor)((TextEditor.ColorStyle != null) ? TextEditor.ColorStyle.PlainText.Foreground : new Cairo.Color (0, 0, 0));
			color.L = flatStyle? 0.7 : 0.5;
			var c = (Cairo.Color)color;
			c.A = 0.6;
			cr.SetSourceColor (c);
			cr.Fill ();
		}
		
		protected void DrawSearchResults (Cairo.Context cr)
		{
			foreach (var region in TextEditor.TextViewMargin.SearchResults) {
				int line = TextEditor.OffsetToLineNumber (region.Offset);
				double y = GetYPosition (line);
				bool isMainSelection = false;
				if (!TextEditor.TextViewMargin.MainSearchResult.IsInvalid)
					isMainSelection = region.Offset == TextEditor.TextViewMargin.MainSearchResult.Offset;
				cr.SetSourceColor (isMainSelection ? TextEditor.ColorStyle.SearchResultMain.Color : TextEditor.ColorStyle.SearchResult.Color);
				cr.Rectangle (barPadding, Math.Round (y) - 1, Allocation.Width - barPadding * 2, 2);
				cr.Fill ();
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
					var col = (HslColor)TextEditor.ColorStyle.PlainText.Background;
					col.L *= 0.95;
					if (flatStyle) {
						using (var pattern = new Cairo.SolidPattern (col)) {
							cr.SetSource (pattern);
						}
					} else {
						using (var grad = new Cairo.LinearGradient (0, 0, Allocation.Width, 0)) {
							grad.AddColorStop (0, col);
							grad.AddColorStop (0.7, TextEditor.ColorStyle.PlainText.Background);
							grad.AddColorStop (1, col);
							cr.SetSource (grad);
						}
					}
				}
				cr.Fill ();

				/*
				cr.Color = (HslColor)Style.Dark (State);
				cr.MoveTo (-0.5, 0.5);
				cr.LineTo (Allocation.Width, 0.5);

				cr.MoveTo (-0.5, Allocation.Height - 0.5);
				cr.LineTo (Allocation.Width, Allocation.Height - 0.5);
				cr.Stroke ();*/

				if (TextEditor == null)
					return true;
				
				if (TextEditor.HighlightSearchPattern) {
					DrawSearchResults (cr);
					DrawSearchIndicator (cr);
				} else {
					if (!Debugger.DebuggingService.IsDebugging) {
						var severity = DrawQuickTasks (cr);
						DrawIndicator (cr, severity);
					}
				}
				DrawCaret (cr);
				
				DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}
	}
	
}
