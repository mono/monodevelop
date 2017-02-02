// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Operations
{
    using System;

    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// A tagger that tags contents of a buffer based on the search terms that are passed to the object. To
    /// obtain an implementation of this interface, import the <see cref="ITextSearchTaggerFactoryService"/>
    /// via the Managed Extensibility Framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All search operations are performed on a low priority background thread and on demand.
    /// </para>
    /// <para>
    /// In order for this tagger to be consumed by the editor, a corresponding <see cref="ITaggerProvider"/>
    /// that provides an instance of this tagger must be exported through the Managed Extensibility Framework.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Export]
    /// [TagType(typeof(T))]
    /// [ContentType("any")]
    /// class TaggerProvider : ITaggerProvider
    /// {
    ///     [Import]
    ///     ITextSearchTaggerFactoryService searchTaggerFactory;
    ///     
    ///     #region ITaggerProvider Members
    ///
    ///     public ITagger&lt;T&gt; CreateTagger&lt;T&gt;(Microsoft.VisualStudio.Text.ITextBuffer buffer) where T : ITag
    ///     {
    ///         ITextSearchTagger&lt;T&gt; tagger = searchTaggerFactory.CreateTextSearchTagger&lt;T&gt;(buffer);
    ///         
    ///         tagger.TagTerm(...);
    ///         
    ///         return tagger as ITagger&lt;T&gt;;
    ///     }
    ///
    ///     #endregion
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="T">
    /// A derivative of <see cref="ITag"/>.
    /// </typeparam>
    /// <remarks>
    /// The <see cref="ITextSearchTagger{T}"/> expects to be queried for monotonically increasing snapshot versions. If a query
    /// is made in the reverse order, the results returned by the tagger for older versions might differ from the results 
    /// obtained originally for those versions.
    /// </remarks>
    public interface ITextSearchTagger<T> : ITagger<T> where T : ITag
    {
        /// <summary>
        /// Limits the scope of the tagger to the provided <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </summary>
        /// <remarks>
        /// If the value is set to <c>null</c> the entire range of the buffer will be searched.
        /// </remarks>
        NormalizedSnapshotSpanCollection SearchSpans { get; set; }

        /// <summary>
        /// Starts tagging occurences of the <paramref name="searchTerm"/>.
        /// </summary>
        /// <param name="searchTerm">
        /// The term to search for.
        /// </param>
        /// <param name="searchOptions">
        /// The options to use for the search.
        /// </param>
        /// <param name="tagFactory">
        /// A factory delegate used to generate tags for matches. The delegate is passed as input
        /// a <see cref="SnapshotSpan"/> corresponding to a match and is expected to return the corresponding tag.
        /// </param>
        /// <exception cref="ArgumentException">If <paramref name="searchOptions"/> requests the search to be 
        /// performed in the reverse direction (see remarks).</exception>
        /// <exception cref="ArgumentException">If <paramref name="searchOptions"/> requests the search to be performed with
        /// wrap (see remarks).</exception>
        /// <remarks>
        /// In order to guarantee that the tagger finds all matches in a given span of text, the searches are always
        /// performed in the forwards direction with no wrap. If the <paramref name="searchOptions"/> passed to the
        /// tagger indicate otherwise, an exception will be thrown.
        /// </remarks>
        void TagTerm(string searchTerm, FindOptions searchOptions, Func<SnapshotSpan, T> tagFactory);

        /// <summary>
        /// Clears any existing tags and all search terms that are being search for. Cancels any
        /// ongoing background searches.
        /// </summary>
        void ClearTags();
    }
}
