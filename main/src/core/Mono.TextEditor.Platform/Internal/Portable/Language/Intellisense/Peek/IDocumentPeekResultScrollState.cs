// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Windows;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Extends the capability of the <see cref="IPeekResultScrollState" /> to be
    /// able to scroll and zoom any text view, rather than only the currently visible
    /// presentation.
    /// </summary>
    /// <remarks>This interface is used in keeping the scroll state and zoom level consistent when a document
    /// presented in Peek is being promoted to a full frame.</remarks>
    public interface IDocumentPeekResultScrollState : IPeekResultScrollState
    {
        /// <summary>
        /// Scrolls any text view to the the <see cref="IPeekResultScrollState"/> that
        /// this inherits from.
        /// </summary>
        /// <param name="presentation">TextView to scroll.</param>
        void RestoreScrollState(ITextView presentation);

        /// <summary>
        /// Restores zoom level in given text view.
        /// </summary>
        /// <param name="textView">TextView to restore zoom level in.</param>
        void RestoreZoomState(ITextView textView);
    }
}