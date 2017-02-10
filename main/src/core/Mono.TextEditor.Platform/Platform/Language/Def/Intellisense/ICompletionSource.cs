////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides completions for a given content type.
    /// </summary>
    public interface ICompletionSource : IDisposable
    {
        /// <summary>
        /// Determines which <see cref="CompletionSet"/>s should be part of the specified <see cref="ICompletionSession"/>.
        /// </summary>
        /// <param name="session">The session for which completions are to be computed.</param>
        /// <param name="completionSets">The set of the completionSets to be added to the session.</param>
        /// <remarks>
        /// Each applicable <see cref="ICompletionSource.AugmentCompletionSession"/> instance will be called in-order to
        /// (re)calculate a <see cref="ICompletionSession"/>.  <see cref="CompletionSet"/>s can be added to the session by adding
        /// them to the completionSets collection passed-in as a parameter.  In addition, by removing items from the collection, a
        /// source may filter <see cref="CompletionSet"/>s provided by <see cref="ICompletionSource"/>s earlier in the calculation
        /// chain.
        /// </remarks>
        void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets);
    }
}
