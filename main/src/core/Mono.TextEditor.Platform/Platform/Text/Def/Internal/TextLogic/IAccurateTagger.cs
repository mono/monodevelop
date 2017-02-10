using System;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// A provider of tags over a buffer.
    /// </summary>
    /// <typeparam name="T">The type of tags to generate.</typeparam>
    public interface IAccurateTagger<out T> : ITagger<T> where T : ITag 
    {
        /// <summary>
        /// Gets all the tags that intersect the <paramref name="spans"/>.
        /// </summary>
        /// <param name="spans">The spans to visit.</param>
        /// <returns>A <see cref="ITagSpan{T}"/> for each tag.</returns>
        /// <remarks>
        /// <para>This method is used when final results are needed (when, for example, when doing color printing) and is expected
        /// to return final results (however long it takes to compute) instead of quick but tentative results.</para>
        /// <para>Taggers are not required to return their tags in any specific order.</para>
        /// <para>The recommended way to implement this method is by using generators ("yield return"),
        /// which allows lazy evaluation of the entire tagging stack.</para>
        /// </remarks>
        IEnumerable<ITagSpan<T>> GetAllTags(NormalizedSnapshotSpanCollection spans, CancellationToken cancel);
    }
}
