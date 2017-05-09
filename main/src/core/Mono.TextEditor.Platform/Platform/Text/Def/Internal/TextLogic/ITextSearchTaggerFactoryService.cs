//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;

    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Provides <see cref="ITextSearchTagger{T}"/> objects.
    /// </summary>
    /// <remarks>
    /// This class is a Managed Extensibility Framework service provided by the editor.
    /// </remarks>
    /// <example>
    /// [Import]
    /// ITextSearchTaggerFactoryService TextSearchTaggerProvider { get; set; }
    /// </example>
    public interface ITextSearchTaggerFactoryService
    {
        /// <summary>
        /// Creates an <see cref="ITextSearchTagger{T}"/> that searches the <paramref name="buffer"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of tags the tagger will produce.
        /// </typeparam>
        /// <param name="buffer">
        /// The <see cref="ITextBuffer"/> the tagger will search.
        /// </param>
        /// <returns>
        /// A <see cref="ITextSearchTagger{T}"/> that searches the contents of <paramref name="buffer"/>.
        /// </returns>
        ITextSearchTagger<T> CreateTextSearchTagger<T>(ITextBuffer buffer) where T : ITag;
    }
}
