//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor;

namespace MonoDevelop.CSharp.Completion.Presentation
{
	interface ICompletionPresenterView
	{
		int Rows { get; }

		int SelectedItemIndex { get; set; }

		IReadOnlyList<CompletionItem> FilteredItems { get; }

		event EventHandler<CompletionItemEventArgs> ItemSelected;
		event EventHandler<CompletionItemEventArgs> ItemCommitted;

		void Open (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected);
		void Update (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected);
		void Close ();
	}
}