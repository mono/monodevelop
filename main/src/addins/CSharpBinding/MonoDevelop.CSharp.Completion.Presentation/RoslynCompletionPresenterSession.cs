//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//


using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using System.Linq;
using MonoDevelop.Core;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.CSharp.Completion.Presentation
{
	internal class RoslynCompletionPresenterSession : ICompletionPresenterSession
	{
		readonly ICompletionPresenterView view;

		public event EventHandler<CompletionItemEventArgs> ItemSelected { add => view.ItemSelected += value; remove => view.ItemSelected -= value; }
		public event EventHandler<CompletionItemEventArgs> ItemCommitted { add => view.ItemCommitted += value; remove => view.ItemCommitted -= value; }

		public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
		public event EventHandler<EventArgs> Dismissed;


		public RoslynCompletionPresenterSession (IMdTextView textView, ITextBuffer subjectBuffer, CompletionService completionService)
		{
			view = new GtkCompletionPresenterView (textView, subjectBuffer, completionService);
		}

		public void Dismiss ()
		{
			view.Close ();
		}

		bool opened = false;
		public void PresentItems (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected, ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText)
		{
			if (!opened) {
				view.Open (triggerSpan, items, selectedItem, suggestionModeItem, suggestionMode, isSoftSelected);
				opened = true;
			}
			else {
				view.Update (triggerSpan, items, selectedItem, suggestionModeItem, suggestionMode, isSoftSelected);
			}
		}

		public void SelectNextItem ()
		{
			if (view.FilteredItems.Count == 0)
				return;
			if (view.SelectedItemIndex == view.FilteredItems.Count - 1)
				view.SelectedItemIndex = 0;
			else
				view.SelectedItemIndex++;
		}

		public void SelectNextPageItem ()
		{
			if (view.FilteredItems.Count == 0)
				return;
			if (view.SelectedItemIndex == view.FilteredItems.Count - 1)
				view.SelectedItemIndex = 0;
			else if (view.SelectedItemIndex + view.Rows < view.FilteredItems.Count)
				view.SelectedItemIndex += view.Rows;
			else
				view.SelectedItemIndex = view.FilteredItems.Count - 1;
		}

		public void SelectPreviousItem ()
		{
			if (view.FilteredItems.Count == 0)
				return;
			if (view.SelectedItemIndex == 0)
				view.SelectedItemIndex = view.FilteredItems.Count - 1;
			else
				view.SelectedItemIndex--;
		}

		public void SelectPreviousPageItem ()
		{
			if (view.FilteredItems.Count == 0)
				return;
			if (view.SelectedItemIndex == 0)
				view.SelectedItemIndex = view.FilteredItems.Count - 1;
			else if (view.SelectedItemIndex - view.Rows > 0)
				view.SelectedItemIndex -= view.Rows;
			else
				view.SelectedItemIndex = 0;
		}
	}
}