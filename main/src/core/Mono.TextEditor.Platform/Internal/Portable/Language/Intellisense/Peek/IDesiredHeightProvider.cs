// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Indicates that an implementing object provides its height to a container.
    /// </summary>
    public interface IDesiredHeightProvider
    {
        /// <summary>
        /// The desired height in pixels.
        /// </summary>
        double DesiredHeight { get; }

        /// <summary>
        /// Raised when the container should requery DesiredHeight.
        /// </summary>
        event EventHandler<EventArgs> DesiredHeightChanged;
    }
}