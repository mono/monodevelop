// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification.Implementation
{
    using System;
    using System.ComponentModel.Composition;

	//HACK using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Exports the <see cref="IClassifierAggregatorService"/> component.
    /// </summary>
    [Export(typeof(IClassifierAggregatorService))]
	//HACK [Export(typeof(IViewClassifierAggregatorService))]
	internal sealed class ClassifierAggregatorService : IClassifierAggregatorService//HACK , IViewClassifierAggregatorService
	{
        [Import]
        internal IBufferTagAggregatorFactoryService _bufferTagAggregatorFactory { get; set; }

		//HACK [Import]
		//HACK internal IViewTagAggregatorFactoryService _viewTagAggregatorFactory { get; set; }

		[Import]
        internal IClassificationTypeRegistryService _classificationTypeRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new ClassifierAggregator(textBuffer, _bufferTagAggregatorFactory, _classificationTypeRegistry);
        }

		//HACK public IClassifier GetClassifier(ITextView textView)
		//HACK {
		//HACK     return new ClassifierAggregator(textView, _viewTagAggregatorFactory, _classificationTypeRegistry);
		//HACK }
	}
}
