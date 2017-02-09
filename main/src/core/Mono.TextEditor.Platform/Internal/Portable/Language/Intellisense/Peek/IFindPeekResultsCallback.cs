// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a callback object provided to <see cref="IPeekResultSource"/>s to report
    /// the state of result querying.
    /// </summary>
    public interface IFindPeekResultsCallback
    {
        /// <summary>
        /// Reports the progress of query processing.
        /// </summary>
        /// <param name="percentProgress">The percentage, from 0 to 100, of a work completion.</param>
        void ReportProgress(int percentProgress);

        /// <summary>
        /// Reports a failure of query processing.
        /// </summary>
        /// <param name="failure">The exception representing the deails of the failure.</param>
        void ReportFailure(Exception failure);
    }
}
