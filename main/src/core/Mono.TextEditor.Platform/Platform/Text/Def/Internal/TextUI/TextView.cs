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
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Provides methods for scrolling the editor window up and down.
    /// </summary>
    public abstract class TextView
    {
        /// <summary>
        /// When implemented in a derived class, moves the current line to the top of the view without moving the caret.
        /// </summary>
        /// <param name="lineNumber">The number of lines to move.</param>
        public abstract void MoveLineToTop(int lineNumber);

        /// <summary>
        /// When implemented in a derived class, moves the current line to the bottom of the view without moving the caret.
        /// </summary>
        /// <param name="lineNumber">The number of lines to move.</param>
        public abstract void MoveLineToBottom(int lineNumber);

        /// <summary>
        /// When implemented in a derived class, scrolls the view up by one line.
        /// </summary>
        /// <param name="lines">The number of lines to scroll.</param>
        public abstract void ScrollUp(int lines);

        /// <summary>
        /// When implemented in a derived class, scrolls the view down by one line.
        /// </summary>
        /// <param name="lines">The number of lines to scroll.</param>
        public abstract void ScrollDown(int lines);

        /// <summary>
        /// When implemented in a derived class, scrolls the view down by one page and does not move the caret.
        /// </summary>
        public abstract void ScrollPageDown();

        /// <summary>
        /// When implemented in a derived class, scrolls the view up by one page and does not move the caret.
        /// </summary>
        public abstract void ScrollPageUp();

        /// <summary>
        /// When implemented in a derived class, shows the <paramref name="point"/> in the view.
        /// </summary>
        /// <param name="point">The point to  display.</param>
        /// <param name="howToShow">How the point should be displayed on the screen.</param>
        /// <returns><c>true</c> if the point was actually displayed, otherwise <c>false</c>.</returns>
        public abstract bool Show(DisplayTextPoint point, HowToShow howToShow);

        /// <summary>
        /// When implemented in a derived class, shows the <paramref name="textRange"/> in the view.
        /// </summary>
        /// <param name="textRange">The <see cref="TextRange"/> to  display.</param>
        /// <param name="howToShow">How the point should be displayed on the screen.</param>
        /// <returns>The <see cref="VisibilityState"/> that describes how the range was actually displayed.</returns>
        public abstract VisibilityState Show(DisplayTextRange textRange, HowToShow howToShow);

        /// <summary>
        /// When implemented in a derived class, gets a display text point from a integer position.
        /// </summary>
        /// <param name="position">The position at which to get the text point.</param>
        /// <returns>The <see cref="DisplayTextPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or past the end of the buffer.</exception>
        public abstract DisplayTextPoint GetTextPoint(int position);

        /// <summary>
        /// When implemented in a derived class, gets a display text point from a buffer text point position.
        /// </summary>
        /// <param name="textPoint">The buffer text point to translate into a display text point.</param>
        /// <returns>The <see cref="DisplayTextPoint"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="textPoint"/> does not belong to the same buffer as the view.</exception>
        public abstract DisplayTextPoint GetTextPoint(TextPoint textPoint);

        /// <summary>
        /// When implemented in a derived class, gets a display text point from a line and column.
        /// </summary>
        /// <param name="line">The line on which to get this text point.</param>
        /// <param name="column">The line-relative position at which to get the text point.</param>
        /// <returns>The <see cref="DisplayTextPoint"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> is negative or greater than the number of lines in the buffer, or
        /// <paramref name="column"/> is negative or past the end of the line.</exception>
        public abstract DisplayTextPoint GetTextPoint(int line, int column);

        /// <summary>
        /// When implemented in a derived class, gets a display text range from two display text points.
        /// </summary>
        /// <param name="startPoint">The start point of the range.</param>
        /// <param name="endPoint">The end point of the range.</param>
        /// <returns>The text range that starts and ends at the two points.</returns>
        /// <remarks>The start point of the text range may become the end point if the start point occurs after the end point.</remarks>
        /// <exception cref="ArgumentException"><paramref name="startPoint"/> or <paramref name="endPoint"/> do not belong to this buffer.</exception>
        public abstract DisplayTextRange GetTextRange(TextPoint startPoint, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, gets a display text range from a text range on the buffer.
        /// </summary>
        /// <param name="textRange">The text range on the buffer.</param>
        /// <returns>The <see cref="DisplayTextPoint"/>.</returns>
        /// <remarks>The start point of the text range may become the end point if the start point occurs after the end point.</remarks>
        /// <exception cref="ArgumentException"><paramref name="textRange"/> does not belong to the same buffer as the view.</exception>
        public abstract DisplayTextRange GetTextRange(TextRange textRange);

        /// <summary>
        /// When implemented in a derived class, gets a display text range from two integer positions.
        /// </summary>
        /// <param name="startPosition">The start position of the range.</param>
        /// <param name="endPosition">The end position of the range.</param>
        /// <returns>The text range that starts and ends at the two positions.</returns>
        /// <remarks>The start position of the text range may become the end point if the start position occurs after the end position.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startPosition"/> is negative or past the end of the buffer.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="endPosition"/> is negative or past the end of the buffer.</exception>
        public abstract DisplayTextRange GetTextRange(int startPosition, int endPosition);

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> of text currently visible on screen.
        /// </summary>
        public abstract DisplayTextRange VisibleSpan
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, provides advanced view manipulation functionality.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract ITextView AdvancedTextView
        {
            get;
        }

         /// <summary>
        /// When implemented in a derived class, gets the <see cref="Caret"/> of this view.
        /// </summary>
        public abstract Caret Caret
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="Selection"/>. of this view. 
        /// </summary>
        public abstract Selection Selection
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextBuffer"/>. of this view. 
        /// </summary>
        public abstract TextBuffer TextBuffer
        {
            get;
        }
    }
}
