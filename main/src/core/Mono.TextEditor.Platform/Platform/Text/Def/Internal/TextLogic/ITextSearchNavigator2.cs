// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Provides a service to navigate between search results on a <see cref="ITextBuffer"/> and to
    /// perform replacements.
    /// </summary>
    public interface ITextSearchNavigator2 : ITextSearchNavigator
    {
        /// <summary>
        /// Indicates the ranges that should be searched (if any).
        /// </summary>
        /// <remarks>
        /// If this value to a non-null value will effectively override the ITextSearchNavigator.SearchSpan property.
        /// </remarks>
        NormalizedSnapshotSpanCollection SearchSpans { get; set; }
    }
}
