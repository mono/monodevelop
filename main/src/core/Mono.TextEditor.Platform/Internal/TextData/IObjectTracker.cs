////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Describes a tracker of in-memory objects.
    /// </summary>
    public interface IObjectTracker
    {
        /// <summary>
        /// Begins tracking an object.
        /// </summary>
        /// <param name="value">The <see cref="System.Object"/> to track.</param>
        /// <param name="bucketName">The name of the bucket in which to track this <see cref="System.Object"/> reference.</param>
        /// <remarks>Object trackers</remarks>
        void TrackObject(object value, string bucketName);
    }
}
