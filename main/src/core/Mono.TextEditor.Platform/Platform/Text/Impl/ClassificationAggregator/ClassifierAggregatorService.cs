//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Exports the <see cref="IClassifierAggregatorService"/> component.
    /// </summary>
    [Export(typeof(IClassifierAggregatorService))]
    [Export(typeof(IViewClassifierAggregatorService))]
    internal sealed class ClassifierAggregatorService : IClassifierAggregatorService, IViewClassifierAggregatorService
    {
        [Import]
        internal IBufferTagAggregatorFactoryService _bufferTagAggregatorFactory { get; set; }

        [Import]
        internal IViewTagAggregatorFactoryService _viewTagAggregatorFactory { get; set; }

        [Import]
        internal IClassificationTypeRegistryService _classificationTypeRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new ClassifierAggregator(textBuffer, _bufferTagAggregatorFactory, _classificationTypeRegistry);
        }

        public IClassifier GetClassifier(ITextView textView)
        {
            return new ClassifierAggregator(textView, _viewTagAggregatorFactory, _classificationTypeRegistry);
        }
    }
}
