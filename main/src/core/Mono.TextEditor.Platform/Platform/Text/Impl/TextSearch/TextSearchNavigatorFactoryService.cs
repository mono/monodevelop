// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Find.Implementation
{
    using System;
    using System.ComponentModel.Composition;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;

    [Export(typeof(ITextSearchNavigatorFactoryService))]
    class TextSearchNavigatorFactoryService : ITextSearchNavigatorFactoryService
    {
        [Import]
        ITextSearchService2 TextSearchService = null;

        #region ITextSearchNavigatorFactoryService Members

        public ITextSearchNavigator CreateSearchNavigator(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            // Don't return a singleton since it's allowed to have multiple search navigators on the same buffer
            return new TextSearchNavigator(this.TextSearchService, buffer);
        }

        #endregion
    }
}
