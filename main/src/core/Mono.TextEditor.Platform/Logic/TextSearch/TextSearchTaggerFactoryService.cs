// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Find.Implementation
{
    using System;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Tagging;

    [Export(typeof(ITextSearchTaggerFactoryService))]
    class TextSearchTaggerFactoryService : ITextSearchTaggerFactoryService
    {
        [Import]
        private ITextSearchService2 TextSearchService = null;

        #region ITextSearchTaggerFactoryService Members

        public ITextSearchTagger<T> CreateTextSearchTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            // Don't return singleton instances since multiple taggers can exist per buffer
            return new TextSearchTagger<T>(this.TextSearchService, buffer);
        }

        #endregion
    }
}
