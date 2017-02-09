// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text.Editor;
namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Looks up a classification format map for a given view.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IClassificationFormatMapService formatMap = null;
    /// </remarks>
    public interface IClassificationFormatMapService
    {
        /// <summary>
        /// Gets an <see cref="IClassificationFormatMap"/> appropriate for the specified text view. This object is
        /// likely to be shared among multiple text views.
        /// </summary>
        /// <param name="textView">The view.</param>
        /// <returns>An <see cref="IClassificationFormatMap"/> for the view.</returns>
        IClassificationFormatMap GetClassificationFormatMap(ITextView textView);

        /// <summary>
        /// Gets a <see cref="IClassificationFormatMap"/> for the specified appearance category.
        /// </summary>
        /// <param name="category">The appearance category.</param>
        /// <returns>An <see cref="IClassificationFormatMap"/> for the category.</returns>
        IClassificationFormatMap GetClassificationFormatMap(string category);
    }
}
