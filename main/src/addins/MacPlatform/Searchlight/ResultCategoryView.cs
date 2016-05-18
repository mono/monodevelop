//
// ResultCategoryView.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2016 
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
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.MacIntegration.OverlaySearch
{
	public class ResultCategoryView : NSStackView
	{
		public event EventHandler<ResultActivatedArgs> ResultActivated;
		NSTextField categoryTitle;
		NSStackView resultsStack;
		public List<ResultView> ResultViews { get; private set; }

		public ResultView SelectedResult { get; private set; }

		SearchCategory category;
		public SearchCategory Category {
			get {
				return category;
			}

			set {
				category = value;
				categoryTitle.StringValue = category.Name;
			}
		}

		IReadOnlyList<SearchResult> results;
		public IReadOnlyList<SearchResult> Results {
			get {
				return results;
			}

			set {
				results = value;

				// Clear any old views out
				if (ResultViews != null) {
					foreach (var v in ResultViews) {
						v.ResultActivated -= OnResultActivated;
						resultsStack.RemoveView (v);
					}
				}

				ResultViews = new List<ResultView> ();

				foreach (var result in results) {
					var r = new ResultView (result);
					r.ResultActivated += OnResultActivated;

					resultsStack.AddView (r, NSStackViewGravity.Leading);

					ResultViews.Add (r);
				}
			}
		}

		void OnResultActivated (object o, EventArgs args)
		{
			var rv = o as ResultView;
			var newArgs = new ResultActivatedArgs (rv.Result);
			ResultActivated?.Invoke (this, newArgs);
		}

		public ResultCategoryView ()
		{
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
			SetHuggingPriority (1000, NSLayoutConstraintOrientation.Vertical);
			SetHuggingPriority (100, NSLayoutConstraintOrientation.Horizontal);
			SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);
			Spacing = 8f;

			categoryTitle = new NSTextField ();
			categoryTitle.Editable = false;
			categoryTitle.Selectable = false;
			categoryTitle.Bordered = false;
			categoryTitle.DrawsBackground = false;
			categoryTitle.Bezeled = false;
			categoryTitle.TranslatesAutoresizingMaskIntoConstraints = false;

			categoryTitle.SetContentHuggingPriorityForOrientation (100, NSLayoutConstraintOrientation.Vertical);

			var constraint = NSLayoutConstraint.Create (categoryTitle, NSLayoutAttribute.Width, NSLayoutRelation.Equal, 1.0f, 100.0f);
			categoryTitle.AddConstraint (constraint);

			AddView (categoryTitle, NSStackViewGravity.Leading);

			resultsStack = new NSStackView ();
			resultsStack.Spacing = 4f;
			resultsStack.Alignment = NSLayoutAttribute.Leading;
			resultsStack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			resultsStack.SetHuggingPriority (1000, NSLayoutConstraintOrientation.Vertical);
			resultsStack.SetHuggingPriority (100, NSLayoutConstraintOrientation.Horizontal);
			resultsStack.SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);
			resultsStack.TranslatesAutoresizingMaskIntoConstraints = false;

			AddView (resultsStack, NSStackViewGravity.Leading);
		}

		public bool SelectNextResult ()
		{
			if (SelectedResult == null) {
				return false;
			}

			var idx = ResultViews.IndexOf (SelectedResult);
			if (idx == ResultViews.Count - 1) {
				return false;
			}

			SelectResult (ResultViews [idx + 1]);
			return true;
		}

		public bool SelectPreviousResult ()
		{
			if (SelectedResult == null) {
				return false;
			}

			var idx = ResultViews.IndexOf (SelectedResult);
			if (idx == 0) {
				return false;
			}

			SelectResult (ResultViews [idx - 1]);

			return true;
		}

		public void UnselectResult ()
		{
			if (SelectedResult == null) {
				return;
			}
			SelectedResult.Highlighted = false;
		}

		ResultView ResultAtPoint (CGPoint pointInCategory)
		{
			return ResultViews.Where (v => v.Frame.Contains (pointInCategory)).Select (v => v).FirstOrDefault ();
		}

		public void SelectResultAtPoint (CGPoint pointInCategory)
		{
			var newView = ResultAtPoint (pointInCategory);
			if (newView == null) {
				return;
			}

			SelectResult (newView);
		}

		internal void SelectResult (ResultView newView)
		{
			if (SelectedResult != null && newView != SelectedResult) {
				SelectedResult.Highlighted = false;
			}

			SelectedResult = newView;
			SelectedResult.Highlighted = true;
		}
	}

	public class ResultActivatedArgs : EventArgs
	{
		public SearchResult Result { get; private set; }
		public ResultActivatedArgs (SearchResult result)
		{
			Result = result;
		}
	}
}

