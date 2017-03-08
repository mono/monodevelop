//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Outlining;
    //using Microsoft.VisualStudio.Language.Intellisense.Utilities;

    [Export(typeof(IEditorOperationsFactoryService))]
    internal sealed class EditorOperationsFactoryService : IEditorOperationsFactoryService
    {
        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorFactory { get; set; }

        // This service should be optional: it is implemented on the VS side and other hosts may not implement it.
        //[Import(AllowDefault = true)]
        //internal IWaitIndicator WaitIndicator { get; set; }

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        [Import]
        internal ITextBufferUndoManagerProvider TextBufferUndoManagerProvider { get; set; }

        [Import]
        internal IEditorPrimitivesFactoryService EditorPrimitivesProvider { get; set; }

        [Import]
        internal IEditorOptionsFactoryService EditorOptionsProvider { get; set; }

#if TARGET_VS
        [Import]
        internal IRtfBuilderService RtfBuilderService { get; set; }
#endif

        [Import]
        internal ISmartIndentationService SmartIndentationService { get; set; }

        [Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import(AllowDefault = true)]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        /// <summary>
        /// Provides a operations implementation for a given text view.
        /// </summary>
        /// <param name="textView">
        /// The text view to which the operations will be bound.
        /// </param> 
        /// <returns>
        /// An implementation of IEditorOperations that can provide operations implementations for the given text view.
        /// </returns>
        public IEditorOperations GetEditorOperations(ITextView textView)
        {
            // Validate
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            // Only one EditorOperations should be created per ITextView
            IEditorOperations editorOperations = null;

            // We create one, only if it doesn't already exist
            if (!textView.Properties.TryGetProperty<IEditorOperations>(typeof(EditorOperationsFactoryService), out editorOperations))
            {
                editorOperations = new EditorOperations(textView, this);
                textView.Properties.AddProperty(typeof(EditorOperationsFactoryService), editorOperations);
            }

            return editorOperations;
        }
    }
}
