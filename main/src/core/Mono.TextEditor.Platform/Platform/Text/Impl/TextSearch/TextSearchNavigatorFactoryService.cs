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
