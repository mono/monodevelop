// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Looks up a format map for a given view role.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IEditorFormatMapService formatMap = null;
    /// </remarks>
    public interface IEditorFormatMapService
    {
        /// <summary>
        /// Gets an <see cref="IEditorFormatMap"/> appropriate for a given text view. This object is likely
        /// to be shared among several text views.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>An <see cref="IEditorFormatMap"/> for the text view.</returns>
        IEditorFormatMap GetEditorFormatMap(ITextView view);

        /// <summary>
        /// Get a <see cref="IEditorFormatMap"/> for a given appearance category.
        /// </summary>
        /// <param name="category">The appearance category.</param>
        /// <returns>An <see cref="IEditorFormatMap"/> for the category.</returns>
        IEditorFormatMap GetEditorFormatMap(string category);
    }
}
