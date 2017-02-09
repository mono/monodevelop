// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides information about a request to recreate a content of <see cref="IPeekResultPresentation"/>.
    /// </summary>
    public class RecreateContentEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the Peek result's content presented by <see cref="IPeekResultPresentation"/> was deleted.
        /// </summary>
        public bool IsResultContentDeleted { get; private set; }

        /// <summary>
        /// Creates new instance of the <see cref="RecreateContentEventArgs"/> class.
        /// </summary>
        /// <param name="isResultContentDeleted">Indicates whether the Peek result's content presented by <see cref="IPeekResultPresentation"/> was deleted.</param>
        public RecreateContentEventArgs(bool isResultContentDeleted = false)
        {
            IsResultContentDeleted = isResultContentDeleted;
        }
    }
}
