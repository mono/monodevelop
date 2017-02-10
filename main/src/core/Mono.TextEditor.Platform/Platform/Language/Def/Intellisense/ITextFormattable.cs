////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Windows.Media.TextFormatting;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a contract for implementors to override the text formatting properties for an object.
    /// </summary>
    /// <remarks>
    /// This will mainly be implemented by <see cref="Completion"/> instances that wish to override their textual presentation in
    /// the statement completion presenter.
    /// </remarks>
    public interface ITextFormattable
    {
        /// <summary>
        /// Gets a set of <see cref="TextRunProperties"/> that will override the "default" <see cref="TextRunProperties"/> used to
        /// display this object's text.
        /// </summary>
        /// <param name="defaultTextRunProperties">
        /// The set of <see cref="TextRunProperties"/> that would have been used to present this object had no overriding taken
        /// place.
        /// </param>
        /// <returns>A set of <see cref="TextRunProperties"/> that should be used to display this object's text.</returns>
        TextRunProperties GetTextRunProperties(TextRunProperties defaultTextRunProperties);

        /// <summary>
        /// Gets a set of <see cref="TextRunProperties"/> that will override the "default" <see cref="TextRunProperties"/> used to
        /// display this object's text when this object is highlighted.
        /// </summary>
        /// <param name="defaultHighlightedTextRunProperties">The set of <see cref="TextRunProperties"/> that would have been used to present the highlighted object had no
        /// overriding taken place.</param>
        /// <returns>A set of <see cref="TextRunProperties"/> that should be used to display this object's highlighted text.</returns>
        /// An completion item is highlighted in the default statement completion presenter when it is fully-selected.  The
        /// <see cref="TextRunProperties"/> selected to render the highlighted text should be chosen so as to not clash with the
        /// style of the selection rectangle.
        /// </remarks>
        TextRunProperties GetHighlightedTextRunProperties(TextRunProperties defaultHighlightedTextRunProperties);
    }
}
