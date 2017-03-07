//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
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
