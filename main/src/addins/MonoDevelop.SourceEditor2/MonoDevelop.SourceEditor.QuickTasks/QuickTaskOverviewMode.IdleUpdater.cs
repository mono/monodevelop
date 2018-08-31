//
// QuickTaskOverviewMode.IdleUpdater.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.SourceEditor.QuickTasks
{
	partial class QuickTaskOverviewMode
	{
		class IdleUpdater
		{
			readonly QuickTaskOverviewMode mode;
			readonly CancellationToken token;
			SurfaceWrapper surface;
			Cairo.Context cr;
			Gdk.Rectangle allocation;
			IndicatorDrawingState state;
			public bool ForceUpdate { get; set; }

			public IdleUpdater (QuickTaskOverviewMode mode, System.Threading.CancellationToken token)
			{
				this.mode = mode;
				this.token = token;
			}

			public void Start ()
			{
				allocation = mode.Allocation;
				var swapSurface = mode.swapIndicatorSurface;
				if (swapSurface != null) {
					if (swapSurface.Width == allocation.Width && swapSurface.Height == allocation.Height) {
						surface = swapSurface;
					} else {
						mode.DestroyIndicatorSwapSurface ();
					}
				}

				var displayScale = Core.Platform.IsMac ? GtkWorkarounds.GetScaleFactor (mode) : 1.0;
				if (surface == null) {
					using (var similiar = CairoHelper.Create (IdeApp.Workbench.RootWindow.GdkWindow))
						surface = new SurfaceWrapper (similiar, (int)(allocation.Width * displayScale), (int)(allocation.Height * displayScale));
				}
				state = IndicatorDrawingState.Create ();
				state.Width = allocation.Width;
				state.Height = allocation.Height;
				state.SearchResults.AddRange (mode.TextEditor.TextViewMargin.SearchResults);
				state.Usages.AddRange (mode.AllUsages);
				state.Tasks.AddRange (mode.AllTasks);
				state.ColorCache [IndicatorDrawingState.UsageColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.Foreground);
				state.ColorCache [IndicatorDrawingState.UsageColor].Alpha = 0.4;

				state.ColorCache [IndicatorDrawingState.FocusColor] = Styles.FocusColor.ToCairoColor ();

				state.ColorCache [IndicatorDrawingState.ChangingUsagesColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.ChangingUsagesRectangle);
				if (state.ColorCache [IndicatorDrawingState.ChangingUsagesColor].Alpha == 0.0)
					state.ColorCache [IndicatorDrawingState.ChangingUsagesColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.UsagesRectangle);

				state.ColorCache [IndicatorDrawingState.UsagesRectangleColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.UsagesRectangle);
				for (int i = 0; i < 4; i++) {
					state.ColorCache [i].L = 0.4;
				}

				state.ColorCache [IndicatorDrawingState.UnderlineErrorColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.UnderlineError);
				state.ColorCache [IndicatorDrawingState.UnderlineWarningColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.UnderlineWarning);
				state.ColorCache [IndicatorDrawingState.UnderlineSuggestionColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.UnderlineSuggestion);
				state.ColorCache [IndicatorDrawingState.BackgroundColor] = SyntaxHighlightingService.GetColor (mode.TextEditor.EditorTheme, EditorThemeColors.Background);

				ResetEnumerators ();
				cr = new Cairo.Context (surface.Surface);
				cr.Scale (displayScale, displayScale);
				GLib.Idle.Add (RunHandler);
			}

			void ResetEnumerators ()
			{
				searchResults = state.SearchResults.GetEnumerator ();
				allUsages = state.Usages.GetEnumerator ();
				allTasks = state.Tasks.GetEnumerator ();
			}

			int drawingStep;
			int curIndex = 0;
			IEnumerator<ISegment> searchResults;
			IEnumerator<Usage> allUsages;
			IEnumerator<QuickTask> allTasks;

			bool RunHandler ()
			{
			tokenExit:
				if (token.IsCancellationRequested || mode.TextEditor.GetTextEditorData () == null) {
					cr.Dispose ();
					// if the surface was newly created dispose it otherwise it'll leak.
					if (surface != mode.swapIndicatorSurface)
						surface.Dispose ();
					return false;
				}
				bool nextStep = false;
				switch (drawingStep) {
				case 0:
					for (int i = 0; i < 10 && !nextStep; i++) {
						if (token.IsCancellationRequested)
							goto tokenExit;
						if (mode.TextEditor.HighlightSearchPattern) {
							mode.GetSearchResultIndicator (state, searchResults, ref nextStep);
						} else {
							if (!Debugger.DebuggingService.IsDebugging) {
								mode.GetQuickTasks (state, allUsages, allTasks, ref nextStep);
							} else {
								nextStep = true;
							}
						}
					}
					if (nextStep) {
						drawingStep++;
						nextStep = false;
						if (!ForceUpdate && state.Equals(mode.currentDrawingState)) {
							cr.Dispose ();
							// if the surface was newly created dispose it otherwise it'll leak.
							if (surface != mode.swapIndicatorSurface)
								surface.Dispose ();
							IndicatorDrawingState.Dispose (state);
							return false;
						}
					}
					return true;
				case 1:
					var displayScale = Core.Platform.IsMac ? GtkWorkarounds.GetScaleFactor (mode) : 1.0;
					mode.DrawBackground (cr, allocation);
					drawingStep++;

					state.taskIterator = 0;
					state.usageIterator = 0;
					curIndex = 0;
					return true;
				case 2:
					for (int i = 0; i < 10 && !nextStep; i++) {
						if (token.IsCancellationRequested)
							goto tokenExit;
						if (mode.TextEditor.HighlightSearchPattern) {
							if (curIndex < state.SearchResultIndicators.Count) {
								mode.DrawSearchResults (cr, state, curIndex++);
							} else {
								nextStep = true;
							}
						} else {
							if (!Debugger.DebuggingService.IsDebugging) {
								mode.DrawQuickTasks (cr, state, curIndex++, ref nextStep);
							} else {
								nextStep = true;
							}
						}
					}
					if (nextStep) {
						drawingStep++;
					}
					return true;
				case 3:
					if (mode.TextEditor.HighlightSearchPattern) {
						mode.DrawSearchIndicator (cr);
					} else {
						if (!Debugger.DebuggingService.IsDebugging) {
							mode.DrawIndicator (cr, state.Severity);
						}
					}
					drawingStep++;
					return true;
				default:
					mode.DrawBreakpoints (cr);
					cr.Dispose ();
					var tmp = mode.indicatorSurface;
					mode.indicatorSurface = surface;
					mode.swapIndicatorSurface = tmp;
					IndicatorDrawingState.Dispose (mode.currentDrawingState);
					mode.currentDrawingState = state;
					mode.QueueDraw ();

					return false;
				}
			}
		}
	}
}
