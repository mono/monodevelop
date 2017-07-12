//
// CompletionPresenterSession.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Ide.CodeCompletion
{
	abstract class CompletionPresenterSession
	{
		protected abstract RoslynCompletionData WrapItem (CompletionItem item);

		public void PresentItems (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected, ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText)
		{
			var result = new CompletionDataList ();

			foreach (var item in items) {
				if (string.IsNullOrEmpty (item.DisplayText))
					continue;
				result.Add (WrapItem (item));
			}
			if (suggestionMode)
				result.AutoSelect = false;
			if (filterText != null)
				result.DefaultCompletionString = filterText;
			if (suggestionModeItem != null) {
				result.DefaultCompletionString = suggestionModeItem.DisplayText;
				result.AutoSelect = false;
			}

			if (selectedItem != null) {
				result.DefaultCompletionString = selectedItem.DisplayText;
			}

			// TODO: isSoftSelected
			// TODO: completionItemFilters
			var editor = IdeApp.Workbench.ActiveDocument.Editor;
			var widget = IdeApp.Workbench.ActiveDocument.GetContent<ICompletionWidget> ();
			CompletionWindowManager.ShowWindow (null, (char)0, result, widget, widget.CreateCodeCompletionContext (editor.CaretOffset));
		}

		public void SelectPreviousItem ()
		{
			CompletionWindowManager.PreProcessKeyEvent (Editor.Extension.KeyDescriptor.Up);
		}

		public void SelectNextItem ()
		{
			CompletionWindowManager.PreProcessKeyEvent (Editor.Extension.KeyDescriptor.Down);
		}

		public void SelectPreviousPageItem ()
		{
			CompletionWindowManager.PreProcessKeyEvent (Editor.Extension.KeyDescriptor.PageUp);
		}

		public void SelectNextPageItem ()
		{
			CompletionWindowManager.PreProcessKeyEvent (Editor.Extension.KeyDescriptor.PageDown);
		}

		// public event EventHandler<CompletionItemEventArgs> ItemSelected;
		// public event EventHandler<CompletionItemEventArgs> ItemCommitted;
		//public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
	}
}
