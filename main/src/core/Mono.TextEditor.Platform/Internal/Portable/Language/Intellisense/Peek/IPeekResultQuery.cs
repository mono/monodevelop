// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an asynchronous query for <see cref="IPeekResult"/>s.
    /// </summary>
    public interface IPeekResultQuery
    {
        /// <summary>
        /// An observable collection of <see cref="IPeekResult"/>s for the given query.
        /// </summary>
        ReadOnlyObservableCollection<IPeekResult> Results { get; }

        /// <summary>
        /// Starts the query.
        /// </summary>
        void Start();

        /// <summary>
        /// Cancels the query.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Raised when the query is successfully completed.
        /// </summary>
        event EventHandler Completed;

        /// <summary>
        /// Raised when the query failed.
        /// </summary>
        event EventHandler<ExceptionEventArgs> Failed;

        /// <summary>
        /// Raised when the query progress has changed.
        /// </summary>
        event ProgressChangedEventHandler ProgressChanged;
    }
}
