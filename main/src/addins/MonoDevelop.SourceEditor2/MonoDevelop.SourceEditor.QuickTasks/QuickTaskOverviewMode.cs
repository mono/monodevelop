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
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Components;
using Mono.TextEditor.Theatrics;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using Mono.TextEditor;
using System.Threading;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui;
using System.Reflection.Metadata.Ecma335;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	partial class QuickTaskOverviewMode : DrawingArea, IMapMode
	{
		static Xwt.Drawing.Image searchImage = Xwt.Drawing.Image.FromResource ("issues-busy-16.png");
		static Xwt.Drawing.Image okImage = Xwt.Drawing.Image.FromResource ("issues-ok-16.png");
		static Xwt.Drawing.Image warningImage = Xwt.Drawing.Image.FromResource ("issues-warning-16.png");
		static Xwt.Drawing.Image errorImage = Xwt.Drawing.Image.FromResource ("issues-error-16.png");
		static Xwt.Drawing.Image suggestionImage = Xwt.Drawing.Image.FromResource ("issues-suggestion-16.png");
		static Xwt.Drawing.Image hideImage = Xwt.Drawing.Image.FromResource ("issues-hide-16.png");

		public static Xwt.Drawing.Image HideImage
		{
			get
			{
				return hideImage;
			}
		}

		public static Xwt.Drawing.Image SuggestionImage
		{
			get
			{
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

		Cairo.Color win81Slider;
		Cairo.Color win81SliderPrelight;
		int win81ScrollbarWidth;

		protected override void OnStyleSet (Style previous_style)
		{
			base.OnStyleSet (previous_style);
			if (Core.Platform.IsWindows) {
				using (var scrollstyle = Rc.GetStyleByPaths (Settings, null, null, VScrollbar.GType)) {
					var scrl = new VScrollbar (null);
					scrl.Style = scrollstyle;
					win81Slider = scrollstyle.Background (StateType.Normal).ToCairoColor ();
					win81SliderPrelight = scrollstyle.Background (StateType.Prelight).ToCairoColor ();
					win81ScrollbarWidth = (int)scrl.StyleGetProperty ("slider-width");
					scrl.Destroy ();
				}
			}
		}

		readonly int barPadding = MonoDevelop.Core.Platform.IsWindows ? 1 : 3;

		readonly QuickTaskStrip parentStrip;
		protected readonly Adjustment vadjustment;
		TextViewMargin textViewMargin;
		int caretLine = -1;

		public Mono.TextEditor.MonoTextEditor TextEditor {
			get;
			private set;
		}

		// These caches are updated when the bar is redrawn
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
			caret = TextEditor.Caret;
			caret.PositionChanged += CaretPositionChanged;
			TextEditor.HighlightSearchPatternChanged += RedrawOnUpdate;
			textViewMargin = TextEditor.TextViewMargin;
			textViewMargin.SearchRegionsUpdated += RedrawOnUpdate;
			textViewMargin.MainSearchResultChanged += RedrawOnUpdate;
			heightTree = TextEditor.GetTextEditorData ().HeightTree;
			heightTree.LineUpdateFrom += HandleLineUpdateFrom;
			TextEditor.HighlightSearchPatternChanged += HandleHighlightSearchPatternChanged;
			HasTooltip = true;
			parentStrip.SourceEditorView.Breakpoints.BreakEventAdded += BreakpointsChanged;
			parentStrip.SourceEditorView.Breakpoints.BreakEventRemoved += BreakpointsChanged;
			fadeInStage.ActorStep += delegate (Actor<QuickTaskOverviewMode> actor) {
				barColorValue = actor.Percent;
				return true;
			};
			fadeInStage.Iteration += (sender, e) => QueueDraw ();

			fadeOutStage.ActorStep += delegate (Actor<QuickTaskOverviewMode> actor) {
				barColorValue = 1 - actor.Percent;
				return true;
			};
			fadeOutStage.Iteration += (sender, e) => QueueDraw ();

			fadeInStage.UpdateFrequency = fadeOutStage.UpdateFrequency = 10;

			CanFocus = true;
		}

		void BreakpointsChanged (object sender, Mono.Debugging.Client.BreakEventArgs e)
		{
			DrawIndicatorSurface (0, true);
		}

		void HandleHighlightSearchPatternChanged (object sender, EventArgs e)
		{
			yPositionCache.Clear ();
		}

		void HandleLineUpdateFrom (object sender, Mono.TextEditor.HeightTree.HeightChangedEventArgs e)
		{
			yPositionCache.Clear ();
		}

		void CaretPositionChanged (object sender, EventArgs e)
		{
			if (drawnCaretLine != TextEditor.Caret.Line) {
				QueueDraw ();
			}
		}

		protected override void OnDestroyed ()
		{
			parentStrip.SourceEditorView.Breakpoints.BreakEventAdded -= BreakpointsChanged;
			parentStrip.SourceEditorView.Breakpoints.BreakEventRemoved -= BreakpointsChanged;

			DisposeProxies ();
			DestroyBackgroundSurface ();
			RemoveIndicatorIdleHandler ();
			DestroyIndicatorSwapSurface ();
			DestroyIndicatorSurface ();
			CancelFadeInTimeout ();
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
			if (caret != null) {
				caret.PositionChanged -= CaretPositionChanged;
				caret = null;
			}
			TextEditor.HighlightSearchPatternChanged -= HandleHighlightSearchPatternChanged;
			if (heightTree != null) {
				heightTree.LineUpdateFrom -= HandleLineUpdateFrom;
				heightTree = null;
			}

			TextEditor.HighlightSearchPatternChanged -= RedrawOnUpdate;
			textViewMargin.SearchRegionsUpdated -= RedrawOnUpdate;
			textViewMargin.MainSearchResultChanged -= RedrawOnUpdate;
			textViewMargin = null;
			parentStrip.TaskProviderUpdated -= RedrawOnUpdate;

			vadjustment.ValueChanged -= RedrawOnVAdjustmentChange;
			vadjustment.Changed -= RedrawOnVAdjustmentChange;
			base.OnDestroyed ();
		}

		void DestroyBackgroundSurface ()
		{
			if (backgroundSurface != null) {
				backgroundSurface.Dispose ();
				backgroundSurface = null;
			}
		}

		void DestroyIndicatorSurface ()
		{
			if (indicatorSurface != null) {
				indicatorSurface.Dispose ();
				indicatorSurface = null;
			}
		}

		void DestroyIndicatorSwapSurface ()
		{
			if (swapIndicatorSurface != null) {
				swapIndicatorSurface.Dispose ();
				swapIndicatorSurface = null;
			}
		}

		void RedrawOnUpdate (object sender, EventArgs e)
		{
			DrawIndicatorSurface ();
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

		internal Mono.TextEditor.CodeSegmentPreviewWindow previewWindow;

		bool ShowPreview (double y)
		{
			int line = YToLine (y);

			line = Math.Max (1, line - 2);
			int lastLine = Math.Min (TextEditor.LineCount, line + 5);
			var start = TextEditor.GetLine (line);
			var end = TextEditor.GetLine (lastLine);
			if (start == null || end == null) {
				return false;
			}
			var showSegment = new TextSegment (start.Offset, end.Offset + end.Length - start.Offset);

			if (previewWindow != null) {
				previewWindow.SetSegment (showSegment, false);
				PositionPreviewWindow ((int)y);
			} else {
				var popup = new PreviewPopup (this, showSegment, TextEditor.Allocation.Width * 4 / 7, (int)y);
				previewPopupTimeout = GLib.Timeout.Add (450, new GLib.TimeoutHandler (popup.Run));
			}
			return true;
		}

		void HidePreview ()
		{
			RemovePreviewPopupTimeout ();
			DestroyPreviewWindow ();
		}

		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			RemovePreviewPopupTimeout ();

			if (IsInGrab ()) {
				var yDelta = evnt.Y - grabY;
				MovePosition (grabCenter + yDelta);
			} else {
				UpdatePrelightState (evnt.X, evnt.Y);
			}

			if ((evnt.State & ModifierType.ShiftMask) == ModifierType.ShiftMask) {
				if (!ShowPreview (evnt.Y)) {
					return base.OnMotionNotifyEvent (evnt);
				}
			} else {
				HidePreview ();
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

		internal void GetIndicatorStrings (out string label, out string description)
		{
			if (TextEditor.HighlightSearchPattern) {
				var matches = TextEditor.TextViewMargin.SearchResultMatchCount;
				label = GettextCatalog.GetPluralString ("{0} match", "{0} matches", matches, matches);
				description = null;
				return;
			}

			int errors, warnings, hints, suggestions;
			CountTasks (out errors, out warnings, out hints, out suggestions);

			if (errors == 0 && warnings == 0) {
				label = GettextCatalog.GetString ("No errors or warnings");
			} else if (errors == 0) {
				label = GettextCatalog.GetPluralString ("{0} warning", "{0} warnings", warnings, warnings);
			} else if (warnings == 0) {
				label = GettextCatalog.GetPluralString ("{0} error", "{0} errors", errors, errors);
			} else {
				label = GettextCatalog.GetString ("{0} errors and {1} warnings", errors, warnings);
			}

			if (errors > 0) {
				description = GettextCatalog.GetString ("Click to navigate to the next error");
			} else if (warnings > 0) {
				description = GettextCatalog.GetString ("Click to navigate to the next warning");
			} else if (warnings + hints > 0) {
				description = GettextCatalog.GetString ("Click to navigate to the next message");
			} else {
				description = null;
			}
		}

		protected override bool OnQueryTooltip (int x, int y, bool keyboard_tooltip, Tooltip tooltip)
		{
			if (IsOverIndicator (y)) {
				string label, description, text;
				GetIndicatorStrings (out label, out description);
				if (!string.IsNullOrEmpty (description)) {
					text = label + Environment.NewLine + description;
				} else {
					text = label;
				}
				tooltip.Text = text;
				return true;
			}

			if (TextEditor.HighlightSearchPattern) {
				return false;
			}

			var hoverTask = GetHoverTask (y);
			if (hoverTask != null) {
				tooltip.Text = hoverTask.Description;
				return true;
			}

			return false;
		}

		void CountTasks (out int errors, out int warnings, out int infos, out int hidden)
		{
			errors = warnings = infos = hidden = 0;
			foreach (var task in AllTasks) {
				switch (task.Severity) {
				case DiagnosticSeverity.Error:
					errors++;
					break;
				case DiagnosticSeverity.Warning:
					warnings++;
					break;
				case DiagnosticSeverity.Info:
					infos++;
					break;
				case DiagnosticSeverity.Hidden:
					hidden++;
					break;
				}
			}
		}

		QuickTask GetHoverTask (double y)
		{
			QuickTask hoverTask = null;
			foreach (var task in AllTasks) {
				double ty = GetYPosition (TextEditor.OffsetToLineNumber (task.Location));
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

		class PreviewPopup
		{

			QuickTaskOverviewMode strip;
			ISegment segment;
			int w, y;

			public PreviewPopup (QuickTaskOverviewMode strip, ISegment segment, int w, int y)
			{
				this.strip = strip;
				this.segment = segment;
				this.w = w;
				this.y = y;
			}

			public bool Run ()
			{
				strip.previewPopupTimeout = 0;
				strip.previewWindow = new CodeSegmentPreviewWindow (strip.TextEditor, true, segment, false);
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


		internal virtual double IndicatorHeight {
			get {
				return errorImage.Height;
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
			//			var loc = new DocumentLocation (
			//				Math.Max (DocumentLocation.MinLine, task.Location.Line),
			//				Math.Max (DocumentLocation.MinColumn, task.Location.Column)
			//			);
			caret.Offset = task.Location;
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

		protected void DrawIndicator (Cairo.Context cr, DiagnosticSeverity severity)
		{
			Xwt.Drawing.Image image;
			switch (severity) {
			case DiagnosticSeverity.Error:
				image = errorImage;
				break;
			case DiagnosticSeverity.Warning:
				image = warningImage;
				break;
			case DiagnosticSeverity.Info:
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
			requisition.Width = MonoDevelop.Core.Platform.IsWindows ? win81ScrollbarWidth : 15;
		}

		double LineToY (int logicalLine)
		{
			var h = Allocation.Height - IndicatorHeight;
			var p = TextEditor.LineToY (logicalLine);
			var q = Math.Max (TextEditor.GetTextEditorData ().TotalHeight, TextEditor.Allocation.Height)
				+ TextEditor.Allocation.Height
				- TextEditor.LineHeight;
			return IndicatorHeight + h * p / q;
		}

		int YToLine (double y)
		{
			var line = 0.5 + (y - IndicatorHeight) / (Allocation.Height - IndicatorHeight) * (double)(TextEditor.GetTextEditorData ().VisibleLineCount);
			return TextEditor.GetTextEditorData ().VisualToLogicalLine ((int)line);
		}

		int drawnCaretLine, drawnCaretY;
		protected void DrawCaret (Cairo.Context cr, double displayScale)
		{
			if (TextEditor.EditorTheme == null)
				return;
			drawnCaretLine = TextEditor.Caret.Line;
			drawnCaretY = (int)((GetYPosition (drawnCaretLine) - 1) * displayScale);

			cr.SetSourceColor (SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Foreground));
			var w = Math.Floor (Allocation.Width * 0.618);
			cr.Rectangle (Allocation.Width - w, drawnCaretY, w, 2);
			cr.Fill ();
		}

		void DrawBreakpoints (Cairo.Context cr)
		{
			var breakPoints = parentStrip.SourceEditorView.Breakpoints.GetBreakpointsAtFile (TextEditor.FileName);
			if (breakPoints == null)
				return;
			foreach (var point in breakPoints) {
				int y = (int)GetYPosition (point.Line);

				cr.SetSourceColor (SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.BreakpointMarker));
				int r = 4;
				cr.Rectangle (0, y  - r / 2, r, r);
				cr.Fill ();
			}
		}

		Dictionary<int, double> yPositionCache = new Dictionary<int, double> ();

		internal double GetYPosition (int logicalLine)
		{
			double y;
			if (!yPositionCache.TryGetValue (logicalLine, out y))
				yPositionCache [logicalLine] = y = LineToY (logicalLine);
			return y;
		}

		void GetQuickTasks (IndicatorDrawingState state, IEnumerator<Usage> allUsages, IEnumerator<QuickTask> allTasks, ref bool nextStep)
		{
			if (allUsages.MoveNext ()) {
				var usage = allUsages.Current;
				int y = (int)GetYPosition (TextEditor.OffsetToLineNumber (usage.Offset));
				bool isFocusedUsage = (HasFocus && currentFocus == FocusWidget.Usages && state.usageIterator == focusedUsageIndex);
				if (state.lineCache [0].Contains (y) && !isFocusedUsage)
					return;
				state.lineCache [0].Add (y);
				int color;
				if (isFocusedUsage) {
					color = IndicatorDrawingState.FocusColor;
				} else if ((usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Declaration) != 0) {
					color = IndicatorDrawingState.ChangingUsagesColor;
				} else if ((usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Write) != 0) {
					color = IndicatorDrawingState.ChangingUsagesColor;
				} else if ((usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Read) != 0 || (usage.UsageType & MonoDevelop.Ide.FindInFiles.ReferenceUsageType.Keyword) != 0) {
					color = IndicatorDrawingState.UsagesRectangleColor;
				} else {
					color = IndicatorDrawingState.UsageColor;
				}
				state.UsageRectangles.Add ((y, color));
				currentDrawingState.usageIterator++;
			} else if (allTasks.MoveNext ()) {
				var task = allTasks.Current;
				int y = (int)GetYPosition (TextEditor.OffsetToLineNumber (task.Location));
				bool isFocusedTask = (HasFocus && currentFocus == FocusWidget.Tasks && state.taskIterator == focusedTaskIndex);
				if ((!state.lineCache [1].Contains (y) && task.Severity != DiagnosticSeverity.Hidden) || isFocusedTask) {
					state.lineCache [1].Add (y);
					int color;
					if (HasFocus && currentFocus == FocusWidget.Tasks && state.taskIterator == focusedTaskIndex) {
						color = IndicatorDrawingState.FocusColor;
					} else {
						color = IndicatorDrawingState.GetBarColor (task.Severity);
					}
					state.TaskRectangles.Add ((y, color));
				}

				if (task.Severity == DiagnosticSeverity.Error)
					state.Severity = DiagnosticSeverity.Error;
				else if (task.Severity == DiagnosticSeverity.Warning && state.Severity != DiagnosticSeverity.Error)
					state.Severity = DiagnosticSeverity.Warning;

				currentDrawingState.taskIterator++;
			} else {
				nextStep = true;
			}
		}

		void DrawQuickTasks (Cairo.Context cr, IndicatorDrawingState state, int i, ref bool nextStep)
		{
			if (i < state.UsageRectangles.Count) {
				var (y, color) = state.UsageRectangles [i];
				cr.SetSourceColor (state.ColorCache [color]);
				cr.MoveTo (0, y - 3);
				cr.LineTo (5, y);
				cr.LineTo (0, y + 3);
				cr.LineTo (0, y - 3);
				cr.ClosePath ();
				cr.Fill ();
				return;
			}
			var ti = i - state.UsageRectangles.Count;
			if (ti < state.TaskRectangles.Count) {
				var (y, color) = state.TaskRectangles [ti];
				cr.SetSourceColor (state.ColorCache [color]);
				cr.Rectangle (1, y - 1, Allocation.Width - 1, 2);
				cr.Fill ();
				return;
			} 
			nextStep = true;
		}

		protected void DrawLeftBorder (Cairo.Context cr)
		{
			cr.MoveTo (0.5, 0);
			cr.LineTo (0.5, Allocation.Height);
			if (TextEditor.EditorTheme != null) {
				var col = (Xwt.Drawing.Color)SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Background);
				if (!MonoDevelop.Core.Platform.IsWindows) {
					col.Light *= 0.95;
				}
				cr.SetSourceColor (col.ToCairoColor ());
			}
			cr.Stroke ();
		}

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			yPositionCache.Clear ();
			base.OnSizeAllocated (allocation);
			DrawIndicatorSurface (0);
		}

		void GetBarDimensions (out double x, out double y, out double w, out double h)
		{
			var alloc = Allocation;

			x = MonoDevelop.Core.Platform.IsWindows ? 0 : 1 + barPadding;
			var adjUpper = vadjustment.Upper;
			var allocH = alloc.Height - (int)IndicatorHeight;
			y = IndicatorHeight + Math.Round (allocH * vadjustment.Value / adjUpper);
			w = MonoDevelop.Core.Platform.IsWindows ? alloc.Width : 8;
			const int minBarHeight = 16;
			h = Math.Max (minBarHeight, Math.Round (allocH * (vadjustment.PageSize / adjUpper)) - barPadding - barPadding);
		}
		double barColorValue = 0.0;
		const double barAlphaMax = 0.5;
		const double barAlphaMin = 0.22;
		Caret caret;
		HeightTree heightTree;

		protected virtual void DrawBar (Cairo.Context cr)
		{
			if (vadjustment == null || vadjustment.Upper <= vadjustment.PageSize)
				return;

			double x, y, w, h;
			GetBarDimensions (out x, out y, out w, out h);

			if (MonoDevelop.Core.Platform.IsWindows) {
				cr.Rectangle (x, y, w, h);
			} else {
				MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr, x, y, w, h, 4);
			}

			bool prelight = State == StateType.Prelight;

			Cairo.Color c;
			if (MonoDevelop.Core.Platform.IsWindows) {
				c = prelight ? win81SliderPrelight : win81Slider;
				//compute new color such that it will produce same color when blended with bg
				c = AddAlpha (SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Background), c, 0.5d);
			} else {
				var brightness = HslColor.Brightness (SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Background));
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

		void GetSearchResultIndicator (IndicatorDrawingState state, IEnumerator<ISegment> searchResults, ref bool nextStep)
		{
			if (!searchResults.MoveNext ()) {
				nextStep = true;
				return;
			}
			var region = searchResults.Current;
			int line = TextEditor.OffsetToLineNumber (region.Offset);
			double y = GetYPosition (line);
			bool isMainSelection = false;
			if (!TextEditor.TextViewMargin.MainSearchResult.IsInvalid ()) {
				isMainSelection = region.Offset == TextEditor.TextViewMargin.MainSearchResult.Offset;
				if (isMainSelection)
					state.MainSelection = state.SearchResultIndicators.Count;
			}
			state.SearchResultIndicators.Add ((int)Math.Round (y) - 1);
		}

		void DrawSearchResults (Cairo.Context cr, IndicatorDrawingState state, int i)
		{
			bool isSelected = i == state.MainSelection;
			var color = SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, isSelected ? EditorThemeColors.Selection : EditorThemeColors.FindHighlight);
			if (Math.Abs (HslColor.Brightness (color) - HslColor.Brightness (SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Background))) < 0.1)
				color = isSelected ? Styles.Editor.SearchMarkerSelectedFallbackColor : Styles.Editor.SearchMarkerFallbackColor;
			
			cr.SetSourceColor (color);
			cr.Rectangle (barPadding, state.SearchResultIndicators[i], Allocation.Width - barPadding * 2, 2);
			cr.Fill ();
		}
		IndicatorDrawingState currentDrawingState = IndicatorDrawingState.Create ();
		SurfaceWrapper backgroundSurface, indicatorSurface, swapIndicatorSurface;
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (TextEditor == null)
				return true;

			using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {

				cr.Save ();
				var allocation = Allocation;
				var displayScale = Core.Platform.IsMac ? GtkWorkarounds.GetScaleFactor (this) : 1.0;
				cr.Scale (1 / displayScale, 1 / displayScale);
				if (indicatorSurface != null) {
					cr.SetSourceSurface (indicatorSurface.Surface, 0, 0);
					cr.Paint ();
				} else {
					CachedDraw (cr,
					            ref backgroundSurface,
					            e.Area,
					            draw: (c, o) => DrawBackground (c, allocation), forceScale: displayScale);
				}
				if (TextEditor == null)
					return true;

				DrawCaret (cr, displayScale);
				if (QuickTaskStrip.MergeScrollBarAndQuickTasks)
					DrawBar (cr);

				cr.Restore ();

				if (HasFocus) {
					switch (currentFocus) {
					case FocusWidget.Indicator:
						cr.LineWidth = 1.0;

						cr.SetSourceColor (Styles.FocusColor.ToCairoColor ());
						cr.Rectangle (1, 1, Allocation.Width - 2, Allocation.Width - 2);
						cr.SetDash (new double [] { 1, 1 }, 0.5);
						cr.Stroke ();
						break;

					case FocusWidget.Tasks:
						break;

					case FocusWidget.Usages:
						break;
					}
				}
			}

			return false;
		}

		CancellationTokenSource src;

		uint indicatorIdleTimout;
		void DrawIndicatorSurface (uint timeout = 250, bool forceUpdate = false)
		{
			RemoveIndicatorIdleHandler ();
			if (timeout == 0) {
				IndicatorSurfaceTimeoutHandler (forceUpdate);
			} else {
				indicatorIdleTimout = GLib.Timeout.Add (timeout, delegate {
					IndicatorSurfaceTimeoutHandler (forceUpdate);
					return false;
				});
			}
		}

		void IndicatorSurfaceTimeoutHandler (bool forceUpdate)
		{
			indicatorIdleTimout = 0;
			if (!IsRealized)
				return;
			var allocation = Allocation;
			src?.Cancel ();
			src = new CancellationTokenSource ();
			new IdleUpdater (this, src.Token) { ForceUpdate = forceUpdate }.Start ();
		}

		void RemoveIndicatorIdleHandler ()
		{
			if (indicatorIdleTimout > 0) {
				src?.Cancel ();
				GLib.Source.Remove (indicatorIdleTimout);
				indicatorIdleTimout = 0;
			}
		}

		/// TODO: CairoExtensions.CachedDraw seems not to work correctly for me.
		public static void CachedDraw (Cairo.Context self, ref SurfaceWrapper surface, Gdk.Rectangle region, object parameters = null, float opacity = 1.0f, Action<Cairo.Context, float> draw = null, double? forceScale = null)
		{
			double displayScale = forceScale.HasValue ? forceScale.Value : QuartzSurface.GetRetinaScale (self);
			int targetWidth = (int)(region.Width * displayScale);
			int targetHeight = (int)(region.Height * displayScale);

			bool redraw = false;
			if (surface == null || surface.Width != targetWidth || surface.Height != targetHeight) {
				if (surface != null)
					surface.Dispose ();
				surface = new SurfaceWrapper (self, targetWidth, targetHeight);
				redraw = true;
			} else if ((surface.Data == null && parameters != null) || (surface.Data != null && !surface.Data.Equals (parameters))) {
				redraw = true;
			}


			if (redraw) {
				surface.Data = parameters;
				using (var context = new Cairo.Context (surface.Surface)) {
					context.Operator = Cairo.Operator.Clear;
					context.Paint ();
					context.Operator = Cairo.Operator.Over;
					context.Save ();
					context.Scale (displayScale, displayScale);
					draw (context, 1.0f);
					context.Restore ();
				}
			}

			self.Save ();
			self.Translate (region.X, region.Y);
			self.Scale (1 / displayScale, 1 / displayScale);
			self.SetSourceSurface (surface.Surface, 0, 0);
			self.PaintWithAlpha (opacity);
			self.Restore ();
		}

		void DrawBackground (Cairo.Context cr, Gdk.Rectangle allocation)
		{
			cr.LineWidth = 1;
			cr.Rectangle (0, 0, allocation.Width, allocation.Height);

			if (TextEditor.EditorTheme != null) {
				var bgColor = SyntaxHighlightingService.GetColor (TextEditor.EditorTheme, EditorThemeColors.Background);
				if (MonoDevelop.Core.Platform.IsWindows) {
					using (var pattern = new Cairo.SolidPattern (bgColor)) {
						cr.SetSource (pattern);
						cr.Fill ();
					}
				} else {
					cr.SetSourceColor (bgColor);
					cr.Fill ();
				}
			}
			DrawLeftBorder (cr);
		}

		public void ForceDraw ()
		{
			DestroyBackgroundSurface ();
			DestroyIndicatorSwapSurface ();
			DestroyIndicatorSurface ();

			DrawIndicatorSurface (0);
		}

		protected override bool OnFocusInEvent (EventFocus evnt)
		{
			QueueDraw ();
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (EventFocus evnt)
		{
			QueueDraw ();
			currentFocus = FocusWidget.None;
			return base.OnFocusOutEvent (evnt);
		}

		enum FocusWidget
		{
			None,
			Indicator,
			Tasks,
			Usages,
		};
		FocusWidget currentFocus = FocusWidget.None;
		int focusedUsageIndex = -1;
		int focusedTaskIndex = -1;

		bool FocusNextTaskOrUsage (DirectionType direction)
		{
			if (direction == DirectionType.Down) {
				if (currentFocus == FocusWidget.Tasks) {
					focusedTaskIndex++;
					if (focusedTaskIndex >= currentDrawingState.Tasks.Count) {
						focusedTaskIndex = currentDrawingState.Tasks.Count - 1;
					}
				} else if (currentFocus == FocusWidget.Usages) {
					focusedUsageIndex++;
					if (focusedUsageIndex >= currentDrawingState.Usages.Count) {
						focusedUsageIndex = currentDrawingState.Usages.Count - 1;
					}
				}
			} else if (direction == DirectionType.Up) {
				if (currentFocus == FocusWidget.Tasks) {
					focusedTaskIndex--;
					if (focusedTaskIndex < 0) {
						focusedTaskIndex = 0;
					}
				} else if (currentFocus == FocusWidget.Usages) {
					focusedUsageIndex--;
					if (focusedUsageIndex < 0) {
						focusedUsageIndex = 0;
					}
				}
			}

			return true;
		}

		bool FocusNextArea (DirectionType direction)
		{
			var hasUsages = currentDrawingState.Usages == null ? false : currentDrawingState.Usages.Count > 0;
			var hasTasks = currentDrawingState.Tasks == null ? false : currentDrawingState.Tasks.Count > 0;

			switch (currentFocus) {
			case FocusWidget.None:
				if (direction == DirectionType.TabForward) {
					currentFocus = FocusWidget.Indicator;
				} else if (direction == DirectionType.TabBackward) {
					if (hasUsages) {
						focusedUsageIndex = currentDrawingState.Usages.Count - 1;
						currentFocus = FocusWidget.Usages;
					} else if (hasTasks) {
						focusedTaskIndex = currentDrawingState.Tasks.Count - 1;
						currentFocus = FocusWidget.Tasks;
					} else {
						currentFocus = FocusWidget.Indicator;
					}
				}
				break;

			case FocusWidget.Indicator:
				if (direction == DirectionType.TabForward) {
					if (hasTasks) {
						focusedTaskIndex = 0;
						currentFocus = FocusWidget.Tasks;
					} else if (hasUsages) {
						focusedUsageIndex = 0;
						currentFocus = FocusWidget.Usages;
					} else {
						currentFocus = FocusWidget.None;
					}
				} else if (direction == DirectionType.TabBackward) {
					currentFocus = FocusWidget.None;
				}
				break;

			case FocusWidget.Tasks:
				if (direction == DirectionType.TabForward) {
					if (hasUsages) {
						focusedUsageIndex = 0;
						currentFocus = FocusWidget.Usages;
					} else {
						currentFocus = FocusWidget.None;
					}
				} else if (direction == DirectionType.TabBackward) {
					currentFocus = FocusWidget.Indicator;
				}
				break;

			case FocusWidget.Usages:
				if (direction == DirectionType.TabForward) {
					currentFocus = FocusWidget.None;
				} else if (direction == DirectionType.TabBackward) {
					if (hasTasks) {
						focusedTaskIndex = 0;
						currentFocus = FocusWidget.Tasks;
					} else {
						currentFocus = FocusWidget.Indicator;
					}
				}

				break;
			}

			return (currentFocus != FocusWidget.None);
		}

		protected override bool OnFocused (DirectionType direction)
		{
			bool ret = true;

			switch (direction) {
			case DirectionType.TabForward:
			case DirectionType.TabBackward:
				ret = FocusNextArea (direction);
				break;
			case DirectionType.Left:
			case DirectionType.Right:
				ret = false;
				break;

			case DirectionType.Up:
			case DirectionType.Down:
				if (currentFocus == FocusWidget.None) {
					ret = false;
					break;
				} else if (currentFocus == FocusWidget.Indicator) {
					// Up from the indicator moves the focus out of the widget
					if (direction == DirectionType.Up) {
						ret = false;
						break;
					} else {
						// Focus either the tasks or usages if they are available
						ret = FocusNextArea (DirectionType.TabForward);
						break;
					}
				}

				ret = FocusNextTaskOrUsage (direction);
				break;
			}

			if (ret) {
				GrabFocus ();

				double y = -1;
				if (currentFocus == FocusWidget.Tasks) {
					var t = currentDrawingState.Tasks [focusedTaskIndex];
					parentStrip.GotoTask (t, false);
					y = GetYPosition (TextEditor.OffsetToLineNumber (t.Location));
				} else if (currentFocus == FocusWidget.Usages) {
					var u = currentDrawingState.Usages [focusedUsageIndex];
					parentStrip.GotoUsage (u, false);
					y = (int)GetYPosition (TextEditor.OffsetToLineNumber (u.Offset));
				}

				if (y > -1) {
					ShowPreview (y);
				}
			} else {
				currentFocus = FocusWidget.None;
				HidePreview ();
			}

			if (currentFocus == FocusWidget.None || currentFocus == FocusWidget.Indicator) {
				HidePreview ();
				QueueDraw ();
			} else {
				DrawIndicatorSurface (10);
			}
			return ret;
		}

		protected override void OnActivate ()
		{
			HidePreview ();

			if (currentFocus == FocusWidget.Tasks) {
				var t = currentDrawingState.Tasks [focusedTaskIndex];
				parentStrip.GotoTask (t, true);
			} else if (currentFocus == FocusWidget.Usages) {
				var u = currentDrawingState.Usages [focusedUsageIndex];
				parentStrip.GotoUsage (u, false);
			} else if (currentFocus == FocusWidget.Indicator) {
				parentStrip.GotoTask (parentStrip.SearchNextTask (GetHoverMode ()));
			}
			base.OnActivate ();
		}

		protected override bool OnKeyPressEvent (EventKey evnt)
		{
			if (evnt.Key == Gdk.Key.Escape) {
				HidePreview ();
				return true;
			}
			return base.OnKeyPressEvent (evnt);
		}

#region Accessibility
		List<IQuickTaskAccessible> allyChildren;
		void DisposeProxies ()
		{
			if (allyChildren != null) {
				foreach (var child in allyChildren)
					child.Dispose ();
			}
			allyChildren = null;
		}

		public AccessibilityElementProxy[] UpdateAccessibility ()
		{
			if (!AccessibilityElementProxy.Enabled) {
				return null;
			}

			DisposeProxies ();
			allyChildren = new List<IQuickTaskAccessible>();
			allyChildren.Add (new QuickTaskOverviewAccessible (parentStrip, this));

			foreach (var t in AllTasks) {
				allyChildren.Add (new QuickTaskAccessible (parentStrip, this, t));
			}

			foreach (var u in AllUsages) {
				allyChildren.Add (new QuickTaskAccessible (parentStrip, this, u));
			}

			return allyChildren.Select (x => x.Accessible).ToArray ();
		}

		interface IQuickTaskAccessible : IDisposable
		{
			AccessibilityElementProxy Accessible { get; }
		}

		class QuickTaskOverviewAccessible : IQuickTaskAccessible
		{
			public AccessibilityElementProxy Accessible { get; private set; }

			QuickTaskStrip strip;
			QuickTaskOverviewMode mode;

			public QuickTaskOverviewAccessible (QuickTaskStrip parentStrip, QuickTaskOverviewMode parentMode)
			{
				Accessible = AccessibilityElementProxy.ButtonElementProxy ();

				// Set the accessibility parent as the strip to make the A11y tree easier.
				strip = parentStrip;
				Accessible.GtkParent = parentStrip;

				mode = parentMode;

				var frameInParent = new Gdk.Rectangle (0, 0, strip.Allocation.Width, (int)mode.IndicatorHeight);
				Accessible.FrameInGtkParent = frameInParent;
				Accessible.FrameInParent = new Gdk.Rectangle (0, strip.Allocation.Height - (int)mode.IndicatorHeight, strip.Allocation.Width, (int)mode.IndicatorHeight);

				Accessible.Identifier = "MainWindow.QuickTaskStrip.Indicator";
				UpdateAccessibilityDetails ();

				Accessible.PerformPress += PerformPress;
			}

			public void PerformPress (object sender, EventArgs args)
			{
				strip.GotoTask (strip.SearchNextTask (mode.GetHoverMode ()));
			}

			internal void UpdateAccessibilityDetails ()
			{
				string label, description;

				mode.GetIndicatorStrings (out label, out description);
				if (!string.IsNullOrEmpty (label)) {
					Accessible.Label = label;
				}

				if (!string.IsNullOrEmpty (description)) {
					Accessible.Help = description;
				}
			}

			public void Dispose ()
			{
				Accessible.PerformPress -= PerformPress;
			}
		}

		class QuickTaskAccessible : IQuickTaskAccessible
		{
			public AccessibilityElementProxy Accessible { get; private set; }
			QuickTaskStrip strip;
			QuickTaskOverviewMode mode;
			QuickTask task;
			Usage usage;

			QuickTaskAccessible (QuickTaskStrip parent, QuickTaskOverviewMode parentMode)
			{
				Accessible = AccessibilityElementProxy.ButtonElementProxy ();
				strip = parent;
				Accessible.GtkParent = parent;

				mode = parentMode;

				Accessible.PerformPress += PerformPress;
			}

			public QuickTaskAccessible (QuickTaskStrip parent, QuickTaskOverviewMode parentMode, QuickTask t) : this (parent, parentMode)
			{
				task = t;
				usage = null;

				var line = mode.TextEditor.OffsetToLineNumber (t.Location);
				Accessible.Title = t.Description;
				Accessible.Help = string.Format (GettextCatalog.GetString ("Jump to line {0}"), line.ToString ());

				var y = mode.GetYPosition (line);
				var frameInParent = new Gdk.Rectangle (0, (int)y, mode.Allocation.Width, 2);
				Accessible.FrameInGtkParent = frameInParent;

				int halfParentHeight = strip.Allocation.Height / 2;
				float dy = (float)y - halfParentHeight;

				y = (int)(halfParentHeight - dy);
				Accessible.FrameInParent = new Gdk.Rectangle (0, (int)y, mode.Allocation.Width, 2);
			}

			public QuickTaskAccessible (QuickTaskStrip parent, QuickTaskOverviewMode parentMode, Usage u) : this (parent, parentMode)
			{
				usage = u;
				task = null;

				var line = strip.TextEditor.OffsetToLineNumber (u.Offset);
				Accessible.Title = u.UsageType.ToString ();
				Accessible.Help = string.Format (GettextCatalog.GetString ("Jump to line {0}"), line.ToString ());
				
				var y = mode.GetYPosition (line) - 3.0;
				var frameInParent = new Gdk.Rectangle (0, (int)y, 5, 6);
				Accessible.FrameInGtkParent = frameInParent;

				int halfParentHeight = strip.Allocation.Height / 2;
				float dy = (float)y - halfParentHeight;

				y = (int)(halfParentHeight - dy);
				Accessible.FrameInParent = new Gdk.Rectangle (0, (int)y, mode.Allocation.Width, 6);

			}

			public void PerformPress (object sender, EventArgs args)
			{
				if (task != null) {
					strip.GotoTask (task);
				} else {
					strip.GotoUsage (usage);
				}
			}

			public void Dispose ()
			{
				Accessible.PerformPress -= PerformPress;
			}
		}
#endregion
	}
}
