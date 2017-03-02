//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the selection on the screen.
    /// </summary>
    public abstract class Selection : DisplayTextRange
    {
        /// <summary>
        /// When implemented in a derived class, selects the given text range.
        /// </summary>
        /// <param name="textRange">The range to select.</param>
        public abstract void SelectRange(TextRange textRange);

        /// <summary>
        /// When implemented in a derived class, selects the given text range, reversing the selection if needed. Ensures
        /// that the end point of the selection is visible on screen.
        /// </summary>
        /// <param name="selectionStart">The start point for the selection.</param>
        /// <param name="selectionEnd">The end point for the selection.</param>
        /// <remarks>If <paramref name="selectionStart"/> is positioned after <paramref name="selectionEnd"/>, then the
        /// selection is reversed.</remarks>
        public abstract void SelectRange(TextPoint selectionStart, TextPoint selectionEnd);

        /// <summary>
        /// When implemented in a derived class, selects all the text in the document. Ensures that the end point
        /// of the selection is visible on screen.
        /// </summary>
        public abstract void SelectAll();

        /// <summary>
        /// When implemented in a derived class, extends the selection from its current start point to the new end point. Ensures
        /// that the end point of the selection is visible on screen.
        /// </summary>
        /// <param name="newEnd">
        /// The text point to which to extend the selection.
        /// </param>
        /// <remarks>
        /// <paramref name="newEnd"/> may become the new start point, if <paramref name="newEnd"/> is before the current start point.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><paramref name="newEnd"/> belongs to a different buffer.</exception>
        public abstract void ExtendSelection(TextPoint newEnd);

        /// <summary>
        /// When implemented in a derived class, resets any selection in the text.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// When implemented in a derived class, provides advanced selection manipulation functionality.
        /// </summary>
        /// <returns>The <see cref="ITextSelection"/>.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract ITextSelection AdvancedSelection
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, determines whether the end point represents the start of the selection.
        /// </summary>
        public abstract bool IsReversed
        {
            get;
            set;
        }
    }
}
