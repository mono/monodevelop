//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Windows.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// View model for a glyph data point
    /// </summary>
    public interface ICodeLensGlyphDataPointViewModel : ICodeLensDataPointViewModel
    {
        /// <summary>
        /// Gets the image source of the glyph.
        /// </summary>
        ImageSource GlyphSource { get; }

        /// <summary>
        /// Gets the opacity of the glyph.
        /// </summary>
        double GlyphOpacity { get; }
    }
}