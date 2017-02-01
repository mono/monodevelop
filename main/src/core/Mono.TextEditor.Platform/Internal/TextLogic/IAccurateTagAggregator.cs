using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Aggregates all the tag providers in a buffer graph for the specified type of tag.
    /// </summary>
    /// <typeparam name="T">The type of tag returned by the aggregator.</typeparam>
    /// <remarks>
    /// The default tag aggregator implementation also does the following:
    /// for each <see cref="ITagger&lt;T&gt;"/>  over which it aggregates tags, if the tagger is
    /// <see cref="IDisposable"/>, call Dispose() on it when the aggregator is disposed
    /// or when the taggers are dropped. For example, you should call Dispose() when 
    /// the content type of a text buffer changes or when a buffer is removed from the buffer graph.
    /// </remarks>
    public interface IAccurateTagAggregator<out T> : ITagAggregator<T> where T : ITag
    {
        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="span"/> of the same type as the aggregator.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>This method is used when final results are needed (when, for example, when doing color printing) and is expected
        /// to return final results (however long it takes to compute) instead of quick but tentative results.</para>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// <para>If the underlying tagger does not support <see cref="IAccurateTagger&lt;T&gt;"/>, then <see cref="ITagger&lt;T&gt;"/>.GetTags(...) is used instead.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetAllTags(SnapshotSpan span, CancellationToken cancel);

        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="span"/> of the type of the aggregator.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>This method is used when final results are needed (when, for example, when doing color printing) and is expected
        /// to return final results (however long it takes to compute) instead of quick but tentative results.</para>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// <para>If the underlying tagger does not support <see cref="IAccurateTagger&lt;T&gt;"/>, then <see cref="ITagger&lt;T&gt;"/>.GetTags(...) is used instead.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetAllTags(IMappingSpan span, CancellationToken cancel);

        /// <summary>
        /// Gets all the tags that intersect the specified <paramref name="snapshotSpans"/> of the type of the aggregator.
        /// </summary>
        /// <param name="snapshotSpans">The spans to search.</param>
        /// <returns>All the tags that intersect the region.</returns>
        /// <remarks>
        /// <para>This method is used when final results are needed (when, for example, when doing color printing) and is expected
        /// to return final results (however long it takes to compute) instead of quick but tentative results.</para>
        /// <para>The default tag aggregator lazily enumerates the tags of its <see cref="ITagger&lt;T&gt;"/> objects.
        /// Because of this, the ordering of the returned mapping spans cannot be predicted.
        /// If you need an ordered set of spans, you should collect the returned tag spans, after being mapped
        /// to the buffer of interest, into a sortable collection.</para>
        /// <para>If the underlying tagger does not support <see cref="IAccurateTagger&lt;T&gt;"/>, then <see cref="ITagger&lt;T&gt;"/>.GetTags(...) is used instead.</para>
        /// </remarks>
        IEnumerable<IMappingTagSpan<T>> GetAllTags(NormalizedSnapshotSpanCollection snapshotSpans, CancellationToken cancel);

    }
}
