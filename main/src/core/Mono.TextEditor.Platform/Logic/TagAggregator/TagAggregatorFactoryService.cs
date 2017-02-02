// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Tagging.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;

    using Microsoft.VisualStudio.Utilities;
	//HACK using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// Exports the TagAggregator provider, both the buffer and view version.
    /// </summary>
    [Export(typeof(IBufferTagAggregatorFactoryService))]
	//HACK [Export(typeof(IViewTagAggregatorFactoryService))]
	internal sealed class TagAggregatorFactoryService : IBufferTagAggregatorFactoryService//HACK, IViewTagAggregatorFactoryService
	{
        [ImportMany(typeof(ITaggerProvider))]
        internal List<Lazy<ITaggerProvider, ITaggerMetadata>> BufferTaggerProviders { get; set; }

		//HACK [ImportMany(typeof(IViewTaggerProvider))]
		//HACK internal List<Lazy<IViewTaggerProvider, IViewTaggerMetadata>> ViewTaggerProviders { get; set; }

		[Import]
        internal IBufferGraphFactoryService bufferGraphFactoryService { get; set; }

        [Import]
        internal GuardedOperations guardedOperations { get; set; }

        #region IBufferTagAggregatorFactoryService Members

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer) where T : ITag
        {
            return CreateTagAggregator<T>(textBuffer, TagAggregatorOptions.None);
        }

        public ITagAggregator<T> CreateTagAggregator<T>(ITextBuffer textBuffer, TagAggregatorOptions options) where T : ITag
        {
            if (textBuffer == null)
                throw new ArgumentNullException("textBuffer");

            return new TagAggregator<T>(this, /*HACK null,*/ bufferGraphFactoryService.CreateBufferGraph(textBuffer), options);

        }

		#endregion

		//HACK #region IViewTagAggregatorFactoryService Members
		//HACK 
		//HACK public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView) where T : ITag
		//HACK {
		//HACK     return CreateTagAggregator<T>(textView, TagAggregatorOptions.None);
		//HACK }
		//HACK 
		//HACK public ITagAggregator<T> CreateTagAggregator<T>(ITextView textView, TagAggregatorOptions options) where T : ITag
		//HACK {
		//HACK     if (textView == null)
		//HACK         throw new ArgumentNullException("textView");
		//HACK 
		//HACK     return new TagAggregator<T>(this, textView, textView.BufferGraph, options);
		//HACK }
		//HACK 
		//HACK #endregion
	}
}
