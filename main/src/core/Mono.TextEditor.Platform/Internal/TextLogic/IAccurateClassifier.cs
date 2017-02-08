// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Assigns <see cref="IClassificationType"/> objects to the text in a <see cref="ITextBuffer"/>.
    /// </summary>
    public interface IAccurateClassifier : IClassifier
    {
        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect the given range of text.
        /// </summary>
        /// <param name="span">
        /// The snapshot span.
        /// </param>
        /// <returns>
        /// A list of <see cref="ClassificationSpan"/> objects that intersect with the given range. 
        /// </returns>
        /// <remarks>
        /// <para>This method is used when final results are needed (when, for example, when doing color printing) and is expected
        /// to return final results (however long it takes to compute) instead of quick but tentative results.</para>
        /// <para>If the underlying tagger does not support <see cref="IClassifier"/>, then <see cref="IClassifier"/>.GetClassificationSpans(...) is used instead.</para>
        /// </remarks>
        IList<ClassificationSpan> GetAllClassificationSpans(SnapshotSpan span, CancellationToken cancel);
    }
}
