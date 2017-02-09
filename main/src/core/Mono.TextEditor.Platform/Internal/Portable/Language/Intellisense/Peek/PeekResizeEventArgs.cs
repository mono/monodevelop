// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text.Editor;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Contains information describing how a user has resized the Peek control.
    /// </summary>
    public class PeekResizeEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new instance of <see cref="PeekResizeEventArgs"/> with the current height of the Peek control
        /// both in pixels and as a percentage of the containing <see cref="ITextView"/>."
        /// </summary>
        /// <param name="newHeightAbsolute">The height of the Peek control in pixels.</param>
        /// <param name="newHeightProportion">The height of the Peek control as a proportion of the containing
        /// <see cref="ITextView"/>. Valid values are between 0 and 1.</param>
        public PeekResizeEventArgs(double newHeightAbsolute, double newHeightProportion) : base()
        {
            NewHeightAbsolute = newHeightAbsolute;
            NewHeightProportion = newHeightProportion;
        }

        /// <summary>
        /// Gets the height of the Peek control in pixels.
        /// </summary>
        public double NewHeightAbsolute { get; private set; }

        /// <summary>
        /// Gets the height of the Peek control as a proportion of the containing <see cref="ITextView"/>.
        /// Values returned should fall into the range between 0 and 1.
        /// </summary>
        public double NewHeightProportion { get; private set; }
    }
}