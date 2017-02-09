// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a single result of querying an <see cref="IPeekableItem"/> for a particular relationship.
    /// </summary>
    /// <remarks>In a typical scenario Peek providers create <see cref="IPeekResult"/> instances 
    /// using <see cref="IPeekResultFactory"/> to populate <see cref="IPeekResultCollection"/>.
    /// </remarks>
    public interface IPeekResult : IDisposable
    {
        /// <summary>
        /// Determines properties used for displaying this result to the user.
        /// </summary>
        IPeekResultDisplayInfo DisplayInfo { get; }

        /// <summary>
        /// Determines whether this result has a place to navigate to.
        /// </summary>
        /// <returns>true if can navigate, false otherwise.</returns>
        bool CanNavigateTo { get; }

        /// <summary>
        /// This function will be called directly after navigation completes (if navigation was successful).
        /// </summary>
        /// <remarks>
        /// Argument 1: this <see cref="IPeekResult"/>.
        /// Argument 2: data that this <see cref="IPeekResult"/> decides to pass in from
        /// the results of its navigation (e.g. a handle to a newly opened text view).
        /// Note: Argument 2 can be null if this <see cref="IPeekResult"/> has nothing to pass in.
        /// Argument 3: data that is set by the caller of <see cref="NavigateTo(object)"/>.
        /// </remarks>
        Action<IPeekResult, object, object> PostNavigationCallback { get; }

        /// <summary>
        /// Navigate to the location of this result. If the navigation is succesful, then the PostNavigationCallback
        /// will be called.
        /// </summary>
        /// <param name="data">
        /// The data that is to be passed directly into the third argument of <see cref="PostNavigationCallback"/>, 
        /// if navigation is successful.
        /// </param>
        void NavigateTo(object data);

        /// <summary>
        /// Occurs when an <see cref="IPeekResult"/> is disposed.
        /// </summary>
        event EventHandler Disposed;
    }
}
