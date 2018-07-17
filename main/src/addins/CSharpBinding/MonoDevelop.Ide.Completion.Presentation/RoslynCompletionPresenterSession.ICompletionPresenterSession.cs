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

namespace MonoDevelop.Ide.Completion.Presentation
{
	partial class RoslynCompletionPresenterSession : ICompletionPresenterSession
	{
		bool opened = false;

		public event EventHandler<CompletionItemEventArgs> ItemSelected;
		public event EventHandler<CompletionItemEventArgs> ItemCommitted;

		#pragma warning disable 67 // FIXME: implement intellisense filtering
		public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
		#pragma warning restore 67

		public static RoslynCompletionPresenterSession Instance { get; private set; }


		void ICompletionPresenterSession.PresentItems (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected, ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText)
		{
			if (!opened) {
				Open (triggerSpan, items, selectedItem, suggestionModeItem, suggestionMode, isSoftSelected);
				opened = true;
			} else {
				Update (triggerSpan, items, selectedItem, suggestionModeItem, suggestionMode, isSoftSelected);
			}
		}

		void ICompletionPresenterSession.SelectNextItem ()
		{
			if (filteredItems.Count == 0)
				return;
			if (SelectedItemIndex == filteredItems.Count - 1)
				SelectedItemIndex = 0;
			else
				SelectedItemIndex++;
			ItemSelected?.Invoke (this, new CompletionItemEventArgs (SelectedItem));
		}

		void ICompletionPresenterSession.SelectNextPageItem ()
		{
			if (filteredItems.Count == 0)
				return;
			if (SelectedItemIndex == filteredItems.Count - 1)
				SelectedItemIndex = 0;
			else if (SelectedItemIndex + rows < filteredItems.Count)
				SelectedItemIndex += rows;
			else
				SelectedItemIndex = filteredItems.Count - 1;
			ItemSelected?.Invoke (this, new CompletionItemEventArgs (SelectedItem));
		}

		void ICompletionPresenterSession.SelectPreviousItem ()
		{
			if (filteredItems.Count == 0)
				return;
			if (SelectedItemIndex == 0)
				SelectedItemIndex = filteredItems.Count - 1;
			else
				SelectedItemIndex--;
			ItemSelected?.Invoke (this, new CompletionItemEventArgs (SelectedItem));
		}

		void ICompletionPresenterSession.SelectPreviousPageItem ()
		{
			if (filteredItems.Count == 0)
				return;
			if (SelectedItemIndex == 0)
				SelectedItemIndex = filteredItems.Count - 1;
			else if (SelectedItemIndex - rows > 0)
				SelectedItemIndex -= rows;
			else
				SelectedItemIndex = 0;
			ItemSelected?.Invoke (this, new CompletionItemEventArgs (SelectedItem));
		}
	}
}