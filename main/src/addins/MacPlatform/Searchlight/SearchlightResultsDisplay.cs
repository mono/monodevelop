//
// SearchlightResultsDisplay.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) Xamarin, Inc 2016 
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
using System.Linq;

using AppKit;
using CoreGraphics;
using Foundation;
using Xwt;

using MonoDevelop.Components.MainToolbar;
using MacInterop;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.MacIntegration.OverlaySearch
{
	public class SearchlightResultsDisplay : NSStackView, ISearchResultsDisplay
	{
		SearchService searchService;
		NSTrackingArea trackingArea;

		// We maintain a list of ResultCategoryViews and reuse them because if we remove
		List<ResultCategoryView> resultCategories = new List<ResultCategoryView> ();

		public SearchlightResultsDisplay ()
		{
			Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			SetHuggingPriority (1000, NSLayoutConstraintOrientation.Vertical);
			SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);
			TranslatesAutoresizingMaskIntoConstraints = false;
			Identifier = "resultsDisplay";

			searchService = new SearchService ();
			searchService.ResultsUpdated += ShowResults;

			trackingArea = new NSTrackingArea (CGRect.Empty,
											   NSTrackingAreaOptions.InVisibleRect |
											   NSTrackingAreaOptions.MouseEnteredAndExited |
			                                   NSTrackingAreaOptions.MouseMoved |
			                                   NSTrackingAreaOptions.ActiveInKeyWindow,
											   this, null);
			AddTrackingArea (trackingArea);
		}

		public override void ViewDidMoveToSuperview ()
		{
			if (Superview == null) {
				searchService.Dispose ();
				searchService = null;
			} else {
				// Listen for the BoundsChangedNotifications so that we will know when a scroll has completed
				// because NSScrollView lacks a delegate like UIScrollView.
				Superview.PostsBoundsChangedNotifications = true;
				NSNotificationCenter.DefaultCenter.AddObserver (BoundsChangedNotification, CancelIgnoreMouseMove,
				                                                Superview);
			}

			base.ViewDidMoveToSuperview ();
		}

		ResultCategoryView CategoryViewAtPoint (CGPoint point)
		{
			foreach (var cv in resultCategories) {
				if (cv.Frame.Contains (point)) {
					return cv;
				}
			}

			return null;
		}

		void SelectCategoryAtPoint (CGPoint point)
		{
			var pointInView = ConvertPointFromView (point, null);

			var cat = CategoryViewAtPoint (pointInView);
			if (cat == null) {
				return;
			}

			if (highlightedResult != null && highlightedResult != cat) {
				highlightedResult.UnselectResult ();
			}

			var pointInCategory = cat.ConvertPointFromView (pointInView, this);
			cat.SelectResultAtPoint (pointInCategory);
			highlightedResult = cat;
		}

		// When manually scrolling the scrollview during a keypress to keep the currently selected
		// item on screen, a mouse move is registered if the mouse is over the results display.
		// This causes the selected item to jump to the mouse cursor but it is jarring because
		// the user doesn't know why the selection has changed.
		// To fix this we set ignoreMouseMove to true just before we manually cause a scroll to occur.
		// Then we listen for the NSScrollView.ContentView's bounds to change which signals the end of
		// a scroll and stop ignoring mouse moves events again.
		bool ignoreMouseMove;
		void CancelIgnoreMouseMove (NSNotification note)
		{
			ignoreMouseMove = false;
		}

		ResultCategoryView highlightedResult;
		public override void MouseMoved (NSEvent theEvent)
		{
			base.MouseMoved (theEvent);

			if (ignoreMouseMove) {
				return;
			}

			SelectCategoryAtPoint (theEvent.LocationInWindow);
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			base.MouseEntered (theEvent);

			SelectCategoryAtPoint (theEvent.LocationInWindow);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			base.MouseExited (theEvent);
		}

		public bool IsVisible {
			get {
				return !Hidden;
			}
		}

		public bool SearchForMembers { get; set; }

		public event EventHandler Destroyed;

		public void DestroyResultsDisplay ()
		{
			// Just clear the results, don't need to destroy the view
			UpdateResultCategories (0);
		}

		void CloseDisplay ()
		{
			Destroyed?.Invoke (this, EventArgs.Empty);
		}

		public void HideResultsDisplay ()
		{
			// Clear the results
			UpdateResultCategories (0);
		}

		public void OpenFile ()
		{
			if (highlightedResult == null) {
				return;
			}

			var selectedResultView = highlightedResult.SelectedResult;
			if (selectedResultView == null) {
				return;
			}

			ActivateResult (selectedResultView.Result);
		}

		public void PositionResultsDisplay (Gtk.Widget anchor)
		{
			// Not needed for the Mac implementation
		}

		public bool ProcessKey (Key key, ModifierKeys mods)
		{
			// Not really needed for Mac implementation because we go through the NSTextField key handling
			return false;
		}

		public void ShowResultsDisplay ()
		{
			// Not needed for the Mac implementation
		}

		SearchPopupSearchPattern pattern;
		public void UpdateResults (SearchPopupSearchPattern p)
		{
			pattern = p;
			searchService.Update (p);
		}

		void ShowResults (object o, EventArgs args)
		{
			Application.Invoke (delegate {
				var validResults = searchService.Results.Count (r => r.Item2.Count > 0);
				UpdateResultCategories (validResults);

				int idx = 0;
				foreach (var tuple in searchService.Results) {
					var cat = tuple.Item1;
					var results = tuple.Item2;
					if (results.Count == 0) {
						continue;
					}

					var catView = resultCategories [idx];

					catView.Category = cat;
					catView.Results = results;

					idx++;
				}
			});
		}

		void UpdateResultCategories (int validResults)
		{
			var count = resultCategories.Count;
			if (validResults < count) {
				// Need to remove some categories
				var dx = count - validResults;

				for (int d = 1; d <= dx; d++) {
					var cat = resultCategories [count - d];

					cat.ResultActivated -= OnResultActivated;

					RemoveView (cat);
					resultCategories.Remove (cat);
				}
			} else if (validResults > count) {
				// Need to add some categories

				var dx = validResults - count;

				for (int d = 0; d < dx; d++) {
					var cat = new ResultCategoryView ();
					resultCategories.Add (cat);

					cat.ResultActivated += OnResultActivated;

					AddView (cat, NSStackViewGravity.Leading);
				}
			}
		}

		void OnResultActivated (object o, ResultActivatedArgs args)
		{
			ActivateResult (args.Result);
		}

		async void ActivateResult (SearchResult result)
		{
			if (result.CanActivate) {
				result.Activate ();
				CloseDisplay ();
			} else {
				var region = result.Segment;
				var filename = result.File;
				if (string.IsNullOrEmpty (filename)) {
					CloseDisplay ();
					return;
				}

				CloseDisplay ();
				if (region.Length <= 0) {
					if (pattern.LineNumber == 0) {
						await IdeApp.Workbench.OpenDocument (filename, project: null);
					} else {
						await IdeApp.Workbench.OpenDocument (filename, null, pattern.LineNumber, pattern.HasColumn ? pattern.Column : 1);
					}
				} else {
					await IdeApp.Workbench.OpenDocument (new FileOpenInformation (filename, null) {
						Offset = region.Offset
					});
				}
			}
		}

		void ScrollToResult (bool previous)
		{
			if (highlightedResult == null) {
				return;
			}

			var selectedResult = highlightedResult.SelectedResult;
			if (selectedResult == null) {
				return;
			}

			var pointInView = ConvertPointFromView (selectedResult.Frame.Location, highlightedResult);
			var y = Frame.Height - pointInView.Y;

			if ((y > 500 && !previous) || previous) {
				ignoreMouseMove = true;
				var newOrigin = new CGPoint (0.0f, Math.Max (0.0f, (y) - EnclosingScrollView.Frame.Height));
				EnclosingScrollView.ContentView.ScrollToPoint (newOrigin);

				// "you should rarely need to invoke it yourself" say the docs, but if you don't
				// then the scrollbars won't update correctly. *sigh* cocoa.
				EnclosingScrollView.ReflectScrolledClipView (EnclosingScrollView.ContentView);
			}
		}

		void SelectCategory (ResultCategoryView category, bool selectFirstResult)
		{
			if (highlightedResult != null && category != highlightedResult) {
				highlightedResult.UnselectResult ();
			}
			category.SelectResult (selectFirstResult ? category.ResultViews [0] : category.ResultViews.Last ());

			highlightedResult = category;
		}

		internal void HandleKeyPress (Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType type)
		{
			bool moveWasPreviousItem = false;
			switch (type) {
			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.PreviousResult:
				if (highlightedResult != null) {
					if (!highlightedResult.SelectPreviousResult ()) {
						var idx = resultCategories.IndexOf (highlightedResult);
						if (idx == 0) {
							return;
						}

						SelectCategory (resultCategories [idx - 1], false);
					}
				} else {
					SelectCategory (resultCategories.Last (), false);
				}

				moveWasPreviousItem = true;
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.NextResult:
				if (highlightedResult != null) {
					if (!highlightedResult.SelectNextResult ()) {
						var idx = resultCategories.IndexOf (highlightedResult);
						if (idx == resultCategories.Count - 1) {
							return;
						}

						SelectCategory (resultCategories [idx + 1], true);
					}
				} else {
					SelectCategory (resultCategories [0], true);
				}
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.PreviousCategory:
				if (highlightedResult != null) {
					var idx = resultCategories.IndexOf (highlightedResult);
					if (idx == 0) {
						return;
					}

					SelectCategory (resultCategories [idx - 1], true);
				}

				moveWasPreviousItem = true;
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.NextCategory:
				if (highlightedResult != null) {
					var idx = resultCategories.IndexOf (highlightedResult);
					if (idx == resultCategories.Count - 1) {
						return;
					}

					SelectCategory (resultCategories [idx + 1], true);
				}
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.FirstCategory:
				SelectCategory (resultCategories [0], true);
				moveWasPreviousItem = true;
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.LastCategory:
				SelectCategory (resultCategories.Last (), true);
				break;

			case Searchlight.KeyHandlingTextField.MoveResultArgs.MoveType.ActivateResult:
				OpenFile ();
				return;
			}

			ScrollToResult (moveWasPreviousItem);
		}
	}
}

