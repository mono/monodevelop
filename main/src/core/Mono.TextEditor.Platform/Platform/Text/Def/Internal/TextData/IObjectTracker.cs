//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
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
