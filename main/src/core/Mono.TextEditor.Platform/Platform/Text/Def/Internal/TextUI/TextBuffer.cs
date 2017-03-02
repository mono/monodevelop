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
    /// Provides methods for text insertion, deletion, and modification.
    /// </summary>
    public abstract class TextBuffer
    {
        /// <summary>
        /// When implemented in a derived class, gets a text point from a integer position.
        /// </summary>
        /// <param name="position">The position at which to get the text point.</param>
        /// <returns>The <see cref="TextPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or past the end of the buffer.</exception>
        public abstract TextPoint GetTextPoint(int position);

        /// <summary>
        /// When implemented in a derived class, gets a text point from a line and column.
        /// </summary>
        /// <param name="line">The line on which to get this text point.</param>
        /// <param name="column">The line-relative position at which to get the text point.</param>
        /// <returns>The <see cref="TextPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> is negative or greater than the number of lines in the buffer, or 
        /// <paramref name="column"/> is negative or past the end of the line.</exception>
        /// <remarks>
        /// The line and column are zero-based.
        /// </remarks>
        public abstract TextPoint GetTextPoint(int line, int column);

        /// <summary>
        /// When implemented in a derived class, gets a <see cref="TextRange"/> representing a line in the <see cref="TextBuffer"/>.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>A <see cref="TextRange"/> representing a line in the <see cref="TextBuffer"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> is negative or greater than the number of lines in the buffer.</exception>
        /// <remarks>
        /// <para>
        /// The <see cref="TextRange"/> returned does not include the line break characters for the line.
        /// </para>
        /// </remarks>
        public abstract TextRange GetLine(int line);

        /// <summary>
        /// When implemented in a derived class, gets a text range from two text points.
        /// </summary>
        /// <param name="startPoint">The start point of the range.</param>
        /// <param name="endPoint">The end point of the range.</param>
        /// <returns>The text range that starts and ends at the two points.</returns>
        /// <remarks>The start point of the text range may become the end point if the start point is after the end point.</remarks>
        /// <exception cref="InvalidOperationException"><paramref name="startPoint"/> or <paramref name="endPoint"/> do not belong to this buffer.</exception>
        public abstract TextRange GetTextRange(TextPoint startPoint, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, gets a text range from two integer positions.
        /// </summary>
        /// <param name="startPosition">The start position of the range.</param>
        /// <param name="endPosition">The end position of the range.</param>
        /// <returns>The text range that starts and ends at the two positions.</returns>
        /// <remarks>The start position of the text range may become the end point if the start position is after the end position.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPosition"/> is negative or past the end of the buffer.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="endPosition"/> is negative or past the end of the buffer.</exception>
        public abstract TextRange GetTextRange(int startPosition, int endPosition);

        /// <summary>
        /// When implemented in a derived class, provides advanced text manipulation functionality.
        /// </summary>
        /// <returns>The <see cref="ITextBuffer"/>.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract ITextBuffer AdvancedTextBuffer
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, gets the start point of the buffer (always zero).
        /// </summary>
        /// <returns>The starting <see cref="TextPoint"/>.</returns>
        public abstract TextPoint GetStartPoint();

        /// <summary>
        /// When implemented in a derived class, gets the end point of the buffer (always the last position in the buffer.
        /// </summary>
        /// <returns>The end <see cref="TextPoint"/>.</returns>
        public abstract TextPoint GetEndPoint();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> objectss representing lines in the buffer.
        /// </summary>
        public abstract IEnumerable<TextRange> Lines
        {
            get;
        }
    }
}
