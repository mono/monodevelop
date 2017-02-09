// Copyright (c) Microsoft Corporation
// All rights reserved

using System.Windows;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides a visual for a specific glyph type.
    /// </summary>
    public interface IGlyphFactory
    {
        /// <summary>
        /// Generates a new glyph visual for the given line.
        /// </summary>
        /// <param name="line">The line that this glyph will be placed on.</param>
        /// <param name="tag">Information about the glyph for which the visual is being generated.</param>
        /// <returns>The visual element for the given tag.</returns>
        UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag);
    }
}
