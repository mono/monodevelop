// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Windows;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents margins that are attached to an edge of an <see cref="IWpfTextView"/>.
    /// </summary>
    public interface IWpfTextViewMargin : ITextViewMargin
    {
        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> that renders the margin.
        /// </summary>
        /// <exception cref="ObjectDisposedException"> if the margin is disposed.</exception>
        FrameworkElement VisualElement
        {
            get;
        }
    }
}
