// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides the line transform for a line of formatted text.
    /// </summary>
    public interface ILineTransformSource
    {
        /// <summary>
        /// Computes the line transform for a given line of formatted text.
        /// </summary>
        /// <param name="line">The line for which to compute the line transform.</param>
        /// <param name="yPosition">The y-coordinate of the line.</param>
        /// <param name="placement">The placement of the line with respect to <paramref name="yPosition"/>.</param>
        /// <returns>The line transform for that line.</returns>
        /// <remarks>If <paramref name="placement"/> is ViewRelativePosition.Top, then the top of the line
        /// will be located at <paramref name="yPosition"/>. Otherwise the bottom of the line will be located at
        /// <paramref name="yPosition"/>. Also, the line transform only affects the line itself and not any space
        /// allocated above or below the line for adornments.</remarks>
        LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement);
    }
}
