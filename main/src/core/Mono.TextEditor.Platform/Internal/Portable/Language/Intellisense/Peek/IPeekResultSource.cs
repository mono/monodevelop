// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an object instance that can be queried for supported relationships
    /// and results for a particular relationship.
    /// </summary>
    /// <remarks>
    /// Content-type specific Peek providers implement this interface to provide results of
    /// querying <see cref="IPeekableItem"/> instances.
    /// </remarks>
    public interface IPeekResultSource
    {
        /// <summary>
        /// Populates the collection of <see cref="IPeekResult"/>s for the given relationship.
        /// </summary>
        /// <param name="relationshipName">The case insenitive name of the relationship to be queried for results.</param>
        /// <param name="resultCollection">Represents a collection of <see cref="IPeekResult"/>s to be populated.</param>
        /// <param name="cancellationToken">The cancellation token used by the caller to cancel the operation.</param>
        /// <param name="callback">The <see cref="IFindPeekResultsCallback"/> instance used to report progress and failures.</param>
        void FindResults(string relationshipName, IPeekResultCollection resultCollection, CancellationToken cancellationToken, 
            IFindPeekResultsCallback callback);
    }
}
