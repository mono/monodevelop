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
using MonoDevelop.Components;
using Mono.TextEditor.Theatrics;
using Xwt.Drawing;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	public class QuickTaskOverviewMode : DrawingArea
	{
		static Xwt.Drawing.Image searchImage = Xwt.Drawing.Image.FromResource ("issues-busy-16.png");
		static Xwt.Drawing.Image okImage = Xwt.Drawing.Image.FromResource ("issues-ok-16.png");
		static Xwt.Drawing.Image warningImage = Xwt.Drawing.Image.FromResource ("issues-warning-16.png");
		static Xwt.Drawing.Image errorImage = Xwt.Drawing.Image.FromResource ("issues-error-16.png");
		static Xwt.Drawing.Image suggestionImage = Xwt.Drawing.Image.FromResource ("issues-suggestion-16.png");

		public static Xwt.Drawing.Image SuggestionImage {
			get {
				return suggestionImage;
			}
		}

		public static Xwt.Drawing.Image ErrorImage {
			get {
				return errorImage;
			}
		}

		public static Xwt.Drawing.Image WarningImage {
			get {
				return warningImage;
			}
		}

		public static Xwt.Drawing.Image OkImage {
			get {
				return okImage;
			}
		}

		//TODO: find a way to look these up from the theme
		static readonly Cairo.Color win81Background = new Cairo.Color (240/255d, 240/255d, 240/255d);
		static readonly Cairo.Color win81Slider = new Cairo.Color (205/255d, 205/255d, 205/255d);
		static readonly Cairo.Color win81SliderPrelight = new Cairo.Color (166/255d, 166/255d, 166/255d);
		static readonly Cairo.Color win81SliderActive = new Cairo.Color (96/255d, 96/255d, 96/255d);

		readonly int barPadding = Platform.IsWindows? 1 : 3;

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

		public IEnumerable<Usage> AllUsages {
			get {
				return parentStrip.AllUsages;
			}
		}
		
		public QuickTaskOverviewMode (QuickTaskStrip parent)
		{
			this.parentStrip = parent;
			Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask |
				EventMask.PointerMotionMask | EventMask.LeaveNotifyMask | EventMask.EnterNotifyMask;
			vadjustment = this.parentStrip.VAdjustment;

			vadjustment.ValueChanged += RedrawOnVAdjustmentChange;
			vadjustment.Changed += RedrawOnVAdjustmentChange;
			parentStrip.TaskProviderUpdated += RedrawOnUpdate;
			TextEditor = parent.TextEditor;
//			TextEditor.Caret.PositionChanged += CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged += RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated += RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged += RedrawOnUpdate;
			TextEditor.GetTextEditorData ().HeightTree.LineUpdateFrom += HandleLineUpdateFrom;
			TextEditor.HighlightSearchPatternChanged += HandleHighlightSearchPatternChanged;
			HasTooltip = true;

			fadeInStage.ActorStep += delegate(Actor<QuickTaskOverviewMode> actor) {
				barColorValue = actor.Percent;
				return true;
			};
			fadeInStage.Iteration += (sender, e) => QueueDraw ();

			fadeOutStage.ActorStep += delegate(Actor<QuickTaskOverviewMode> actor) {
				barColorValue = 1 - actor.Percent;
				return true;
			};
			fadeOutStage.Iteration += (sender, e) => QueueDraw ();

			fadeInStage.UpdateFrequency = fadeOutStage.UpdateFrequency = 10;
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
			CancelFadeInTimeout ();
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
			TextEditor.Caret.PositionChanged -= CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged -= HandleHighlightSearchPatternChanged;
			TextEditor.GetTextEditorData ().HeightTree.LineUpdateFrom -= HandleLineUpdateFrom;
			TextEditor.HighlightSearchPatternChanged -= RedrawOnUpdate;
			TextEditor.TextViewMargin.SearchRegionsUpdated -= RedrawOnUpdate;
			TextEditor.TextViewMargin.MainSearchResultChanged -= RedrawOnUpdate;
			
			parentStrip.TaskProviderUpdated -= RedrawOnUpdate;
			
			vadjustment.ValueChanged -= RedrawOnVAdjustmentChange;
			vadjustment.Changed -= RedrawOnVAdjustmentChange;
		}
		
		void RedrawOnUpdate (object sender, EventArgs e)
		{
			QueueDraw ();
		}

		void RedrawOnVAdjustmentChange (object sender, EventArgs e)
		{
			if (!QuickTaskStrip.MergeScrollBarAndQuickTasks)
				return;
			QueueDraw ();
		}

		bool IsOverIndicator (double y)
		{
			return y < IndicatorHeight;
		}
		
		internal CodeSegmentPreviewWindow previewWindow;

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			RemovePreviewPopupTimeout ();

			if (IsInGrab ()) {
				var yDelta = evnt.Y - grabY;
				MovePosition (grabCenter + yDelta);
			} else {
				UpdatePrelightState (evnt.X, evnt.Y);
			}

			const ModifierType buttonMask = ModifierType.Button1Mask | ModifierType.Button2Mask |
				ModifierType.Button3Mask | ModifierType.Button4Mask | ModifierType.Button5Mask;

			if ((evnt.State & buttonMask & ModifierType.ShiftMask) == ModifierType.ShiftMask) {
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

		bool IsInGrab ()
		{
			return grabY >= 0;
		}
		const uint FadeDuration = 90;
		Stage<QuickTaskOverviewMode> fadeInStage = new Stage<QuickTaskOverviewMode> ();
		Stage<QuickTaskOverviewMode> fadeOutStage = new Stage<QuickTaskOverviewMode> ();

		void UpdatePrelightState (double x, double y)
		{
			var newState = StateType.Normal;
			if (IsInsideBar (x, y))
				newState = StateType.Prelight;
			UpdateState (newState);
		}

		bool IsInsideBar (double x, double y)
		{
			double barX, barY, barW, barH;
			GetBarDimensions (out barX, out barY, out barW, out barH);
			var isInsideBar = x >= barX && x <= barX + barW && y >= barY && y <= barY + barH;
			return isInsideBar;
		}

		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			if (TextEditor.HighlightSearchPattern) {
				if (IsOverIndicator (y)) {
					var matches = TextEditor.TextViewMargin.SearchResultMatchCount;
					tooltip.Text = GettextCatalog.GetPluralString ("{0} match", "{0} matches", matches, matches);
					return true;
				}
				return false;
			}

			if (IsOverIndicator (y)) {
				int errors, warnings, hints, suggestions;
				CountTasks (out errors, out warnings, out hints, out suggestions);
				string text = null;
				if (errors == 0 && warnings == 0) {
					text = GettextCatalog.GetString ("No errors or warnings");
				} else if (errors == 0) {
					text = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", warnings, warnings);
				} else if (warnings == 0) {
					text = GettextCatalog.GetPluralString ("{0} error", "{0} errors", errors, errors);
				} else {
					text = GettextCatalog.GetString ("{0} errors and {1} warnings", errors, warnings);
				}

				if (errors > 0) {
					text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next error");
				} else if (warnings > 0) {
					text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next warning");
				} else if (warnings + hints > 0) {
					text += Environment.NewLine + GettextCatalog.GetString ("Click to navigate to the next message");
				}

				tooltip.Text = text;
				return true;
			}

			var hoverTask = GetHoverTask (y);
			if (hoverTask != null) {
				tooltip.Text = hoverTask.Description;
				return true;
			}

			return false;
		}

		void CountTasks (out int errors, out int warnings, out int hints, out int suggestions)
		{
			errors = warnings = hints = suggestions = 0;
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
		}

		QuickTask GetHoverTask (double y)
		{
			QuickTask hoverTask = null;
			foreach (var task in AllTasks) {
				double ty = GetYPosition (task.Location.Line);
				if (Math.Abs (ty - y) < 3) {
					hoverTask = task;
				}
			}
			return hoverTask;
		}

		void UpdateState (StateType state)
		{
			if (State != state) {
				State = state;
				QueueDraw ();
			}
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

		bool isPointerInside;
		uint fadeTimeOutHandler;

		void CancelFadeInTimeout ()
		{
			if (fadeTimeOutHandler == 0)
				return;
			GLib.Source.Remove (fadeTimeOutHandler); 
			fadeTimeOutHandler = 0;
		}

		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			isPointerInside = true;
			if (!IsInGrab ()) {
				CancelFadeInTimeout ();
				fadeTimeOutHandler = GLib.Timeout.Add (250, delegate {
					StartFadeInAnimation ();
					fadeTimeOutHandler = 0;
					return false;
				});
			}
			return base.OnEnterNotifyEvent (evnt);
		}

		void StartFadeInAnimation ()
		{
			fadeOutStage.Pause ();
			fadeInStage.AddOrReset (this, FadeDuration);
			fadeInStage.Play ();
		}

		void StartFadeOutAnimation ()
		{
			CancelFadeInTimeout ();
			UpdateState (StateType.Normal);
			if (this.barColorValue == 0.0)
				return;
			fadeInStage.Pause ();
			fadeOutStage.AddOrReset (this, FadeDuration);
			fadeOutStage.Play ();
		}

		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			isPointerInside = false;
			if (!IsInGrab ()) 
				StartFadeOutAnimation ();
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

		protected virtual double IndicatorHeight  {
			get {
				return Platform.IsWindows ? Allocation.Width : 3 + 8 + 3;
			}
		}
		
		protected virtual void MovePosition (double y)
		{
			double position = ((y - IndicatorHeight) / (Allocation.Height - IndicatorHeight)) * vadjustment.Upper - vadjustment.PageSize / 2;
			position = Math.Max (vadjustment.Lower, Math.Min (position, vadjustment.Upper - vadjustment.PageSize));
			vadjustment.Value = position;
		}

		double GetSliderCenter ()
		{
			var height = Allocation.Height - IndicatorHeight;
			var fraction = (vadjustment.Value + vadjustment.PageSize / 2) / (vadjustment.Upper - vadjustment.Lower);
			return IndicatorHeight + height * fraction;
		}

		double grabY = -1, grabCenter;

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			if (evnt.Button != 1 || evnt.IsContextMenuButton ())
				return base.OnButtonPressEvent (evnt);

			if (IsOverIndicator (evnt.Y)) {
				parentStrip.GotoTask (parentStrip.SearchNextTask (GetHoverMode ()));
				return base.OnButtonPressEvent (evnt);
			}

			var hoverTask = GetHoverTask (evnt.Y);
			if (hoverTask != null)
				MoveToTask (hoverTask);

			if (IsInsideBar (evnt.X, evnt.Y)) {
				Grab.Add (this);
				grabCenter = GetSliderCenter ();
				grabY = evnt.Y;
			} else {
				MovePosition (evnt.Y);
			}

			return base.OnButtonPressEvent (evnt);
		}

		void ClearGrab ()
		{
			if (IsInGrab ()) {
				Grab.Remove (this);
				grabY = -1;
			}
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			ClearGrab ();
			if (!isPointerInside)
				StartFadeOutAnimation ();
			return base.OnButtonReleaseEvent (evnt);
		}

		protected override bool OnGrabBrokenEvent (EventGrabBroken evnt)
		{
			ClearGrab ();
			return base.OnGrabBrokenEvent (evnt);
		}

		void MoveToTask (QuickTask task)
		{
			if (task.Location.IsEmpty) {
				Console.WriteLine ("empty:" + task.Description);
			}
			var loc = new DocumentLocation (
				Math.Max (DocumentLocation.MinLine, task.Location.Line),
				Math.Max (DocumentLocation.MinColumn, task.Location.Column)
			);
			TextEditor.Caret.Location = loc;
			TextEditor.CenterToCaret ();
			TextEditor.StartCaretPulseAnimation ();
			TextEditor.GrabFocus ();
		}

		QuickTaskStrip.HoverMode GetHoverMode ()
		{
			int errors, warnings, hints, suggestions;
			CountTasks (out errors, out warnings, out hints, out suggestions);
			if (errors > 0)
				return QuickTaskStrip.HoverMode.NextError;
			if (warnings > 0)
				return QuickTaskStrip.HoverMode.NextWarning;
			return QuickTaskStrip.HoverMode.NextMessage;
		}

		protected void DrawIndicator (Cairo.Context cr, Severity severity)
		{
			Xwt.Drawing.Image image;
			switch (severity) {
			case Severity.Error:
				image = errorImage;
				break;
			case Severity.Warning:
				image = warningImage;
				break;
			case Severity.Suggestion:
			case Severity.Hint:
				image = suggestionImage;
				break;
			default:
				image = okImage;
				break;
			}

			DrawIndicator (cr, image);
		}

		protected void DrawSearchIndicator (Cairo.Context cr)
		{
			DrawIndicator (cr, searchImage);
		}

		void DrawIndicator (Cairo.Context cr, Xwt.Drawing.Image img)
		{
			cr.DrawImage (this, img, Math.Round ((Allocation.Width - img.Width) / 2), -1);
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = Platform.IsWindows? 17 : 15;
		}
		
		double LineToY (int logicalLine)
		{
			var h = Allocation.Height - IndicatorHeight;
			var p = TextEditor.LocationToPoint (logicalLine, 1, true).Y;
			var q = Math.Max (TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height) 
				+ TextEditor.Allocation.Height
				- TextEditor.LineHeight;

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

			foreach (var usage in AllUsages) {
				double y = GetYPosition (usage.Location.Line);
				var usageColor = TextEditor.ColorStyle.PlainText.Foreground;
				usageColor.A = 0.4;
				HslColor color;
				if ((usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Write) != 0) {
					color = TextEditor.ColorStyle.ChangingUsagesRectangle.Color;
				} else if ((usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Read) != 0) {
					color = TextEditor.ColorStyle.UsagesRectangle.Color;
				} else {
					color = usageColor;
				}
				color.L = 0.5;
				cr.Color = color;
				cr.MoveTo (0, y - 3);
				cr.LineTo (5, y);
				cr.LineTo (0, y + 3);
				cr.LineTo (0, y - 3);
				cr.ClosePath ();
				cr.Fill ();
			}

			foreach (var task in AllTasks) {
				double y = GetYPosition (task.Location.Line);

				cr.SetSourceColor (GetBarColor (task.Severity));
				cr.Rectangle (0, Math.Round (y) - 1, Allocation.Width, 2);
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
				var col = TextEditor.ColorStyle.PlainText.Background.ToXwtColor ();
				if (!Platform.IsWindows) {
					col.Light *= 0.88;
				}
				cr.SetSourceColor (col.ToCairoColor ());
			}
			cr.Stroke ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			yPositionCache.Clear ();
			base.OnSizeAllocated (allocation);
		}

		void GetBarDimensions (out double x, out double y, out double w, out double h)
		{
			var alloc = Allocation;

			x = Platform.IsWindows ? 0 : 1 + barPadding;
			var adjUpper = vadjustment.Upper;
			var allocH = alloc.Height - (int) IndicatorHeight;
			y = IndicatorHeight + Math.Round (allocH * vadjustment.Value / adjUpper);
			w = Platform.IsWindows ? alloc.Width : 8;
			const int minBarHeight = 16;
			h = Math.Max (minBarHeight, Math.Round (allocH * (vadjustment.PageSize / adjUpper)) - barPadding - barPadding);
		}
		double barColorValue = 0.0;
		const double barAlphaMax = 0.5;
		const double barAlphaMin = 0.22;


		protected virtual void DrawBar (Cairo.Context cr)
		{
			if (vadjustment == null || vadjustment.Upper <= vadjustment.PageSize) 
				return;

			double x, y, w, h;
			GetBarDimensions (out x, out y, out w, out h);

			if (Platform.IsWindows) {
				cr.Rectangle (x, y, w, h);
			} else {
				MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr, x, y, w, h, 4);
			}

			bool prelight = State == StateType.Prelight;

			Cairo.Color c;
			if (Platform.IsWindows) {
				c = prelight ? win81SliderPrelight : win81Slider;
				//compute new color such that it will produce same color when blended with bg
				c = AddAlpha (win81Background, c, 0.5d);
			} else {
				var brightness = HslColor.Brightness (TextEditor.ColorStyle.PlainText.Background); 
				c = new Cairo.Color (1 - brightness, 1 - brightness, 1 - brightness, barColorValue * (barAlphaMax - barAlphaMin) + barAlphaMin);
			}
			cr.SetSourceColor (c);
			cr.Fill ();
		}

		static Cairo.Color AddAlpha (Cairo.Color bg, Cairo.Color final, double alpha)
		{
			return new Cairo.Color (
				ReverseAlpha (final.R, final.A, bg.R, bg.A, alpha),
				ReverseAlpha (final.G, final.A, bg.G, bg.A, alpha),
				ReverseAlpha (final.B, final.A, bg.B, bg.A, alpha),
				alpha
			);
		}

		static double ReverseAlpha (double c0, double a0, double cb, double ab, double aa)
		{
			return (c0 * a0 - cb * ab * (1 - aa)) / aa;
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
					if (Platform.IsWindows) {
						using (var pattern = new Cairo.SolidPattern (win81Background)) {
							cr.SetSource (pattern);
						}
					} else {
						var col = TextEditor.ColorStyle.PlainText.Background.ToXwtColor();
						col.Light *= 0.948;
						using (var grad = new Cairo.LinearGradient (0, 0, Allocation.Width, 0)) {
							grad.AddColorStop (0, col.ToCairoColor ());
							grad.AddColorStop (0.7, TextEditor.ColorStyle.PlainText.Background);
							grad.AddColorStop (1, col.ToCairoColor ());
							cr.SetSource (grad);
						}
						/*
						var col = new Cairo.Color (229 / 255.0, 229 / 255.0, 229 / 255.0);
						using (var grad = new Cairo.LinearGradient (0, 0, Allocation.Width, 0)) {
							grad.AddColorStop (0, col);
							grad.AddColorStop (0.5, new Cairo.Color (1, 1, 1));
							grad.AddColorStop (1, col);
							cr.SetSource (grad);
						}*/
					}
				}
				cr.Fill ();

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

				if (QuickTaskStrip.MergeScrollBarAndQuickTasks)
					DrawBar (cr);
				DrawLeftBorder (cr);
			}
			
			return true;
		}
	}
	
}
