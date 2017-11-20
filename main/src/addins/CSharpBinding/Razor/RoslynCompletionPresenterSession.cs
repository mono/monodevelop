//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide.CodeCompletion
{
    internal class RoslynCompletionPresenterSession : ICompletionPresenterSession
    {
        private ITextView _textView;
        private ITextBuffer _subjectBuffer;
        private Document _document;
        private CompletionService _completionService;
        private IMyRoslynCompletionDataProvider _completionDataProvider;
        private bool _isAdvised = false;

        public event EventHandler<CompletionItemEventArgs> ItemSelected;
        public event EventHandler<CompletionItemEventArgs> ItemCommitted;
        public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
        public event EventHandler<EventArgs> Dismissed;


        public RoslynCompletionPresenterSession (ITextView textView, ITextBuffer subjectBuffer, IMyRoslynCompletionDataProvider completionDataProvider, CompletionService completionService)
        {
            _textView = textView;
            _subjectBuffer = subjectBuffer;
            _completionDataProvider = completionDataProvider;
            _completionService = completionService;
        }

        public void Dismiss ()
        {
            CompletionWindowManager.HideWindow();
        }

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
            CompletionTextEditorExtension completionEditorExtension = editor.GetContent<CompletionTextEditorExtension> ();
            completionEditorExtension.ShowCompletion (result);

            if (!_isAdvised)
            {
                CompletionWindowManager.Wnd.SelectionChanged += OnSelectionChanged;
                CompletionWindowManager.WordCompleted += OnWordCompleted;

                CompletionWindowManager.WindowClosed += OnWindowClosed;

                // TODO: Would be nice it we could better detect whether we've already advised on the completion window
                _isAdvised = true;
            }
        }

        private void OnWordCompleted (object sender, CodeCompletionContextEventArgs e)
        {
            MyRoslynCompletionData completionData = (MyRoslynCompletionData) CompletionWindowManager.Wnd.SelectedItem;
            ItemCommitted?.Invoke (this, new CompletionItemEventArgs (completionData.CompletionItem));
        }

        private void OnWindowClosed (object sender, EventArgs e)
        {
            _isAdvised = false;

            CompletionWindowManager.Wnd.SelectionChanged -= OnSelectionChanged;
            CompletionWindowManager.WordCompleted -= OnWordCompleted;

            CompletionWindowManager.WindowClosed -= OnWindowClosed;

            Dismissed?.Invoke (this, EventArgs.Empty);
        }

        private void OnSelectionChanged (object sender, EventArgs e)
        {
            MyRoslynCompletionData completionData = (MyRoslynCompletionData) CompletionWindowManager.Wnd.SelectedItem;
            ItemSelected?.Invoke (this, new CompletionItemEventArgs (completionData.CompletionItem));
        }

        public virtual MyRoslynCompletionData WrapItem (CompletionItem item)
        {
            Document document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            return _completionDataProvider.CreateCompletionData(document, _subjectBuffer.CurrentSnapshot, _completionService, item);
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

        internal void OnCompletionItemCommitted(CompletionItem completionItem)
        {
            ItemCommitted?.Invoke(this, new CompletionItemEventArgs(completionItem));
        }
    }
}