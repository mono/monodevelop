//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents a point in the <see cref="TextBuffer"/> that behaves relative to the view in which it lives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While this point is immutable, its position may change in response
    /// to edits in the text.
    /// </para>
    /// <para>
    /// The start point is always before the end point.
    /// </para>
    /// </remarks>
    public abstract class DisplayTextPoint : TextPoint
    {
        /// <summary>
        ///  When implemented in a derived class, gets the <see cref="TextView"/> of this point.
        /// </summary>
        public abstract TextView TextView { get; }

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="ITextViewLine"/> that contains this point.
        /// </summary>
        public abstract ITextViewLine AdvancedTextViewLine { get; } 

        /// <summary>
        /// When implemented in a derived class, gets the position of the start of the line in the TextView that this DisplayTextPoint is on.
        /// </summary>
        /// <remarks>This value could be affected by whether or not Word Wrap is turned on in the view.</remarks>
        public abstract int StartOfViewLine { get; }

        /// <summary>
        /// When implemented in a derived class, gets the position of the end of the line in the TextView that this DisplayTextPoint is on.
        /// </summary>
        /// <remarks>This value could be affected by whether or not Word Wrap is turned on in the view.</remarks>
        public abstract int EndOfViewLine { get; }

        /// <summary>
        /// When implemented in a derived class, moves this point to the end of the line in the TextView that it is currently on.
        /// </summary>
        /// <remarks>This value could be affected by whether or not Word Wrap is turned on in the view.</remarks>
        public abstract void MoveToEndOfViewLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the start of the line in the TextView that it is currently on.
        /// </summary>
        /// <remarks>This value could be affected by whether or not Word Wrap is turned on in the view.</remarks>
        public abstract void MoveToStartOfViewLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the beginning of the next line in the TextView.
        /// </summary>
        /// <remarks>
        /// <para>This point moves to the end of the line if the point is on the last
        /// line.</para>
        /// <para>This value could be affected by whether or not Word Wrap is turned on in the view.</para>
        /// <para>If the point is on the last line of the file, the caret is moved to the end of the line.</para>
        /// </remarks>
        public abstract void MoveToBeginningOfNextViewLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the beginning of the previous line in the TextView.
        /// </summary>
        /// <remarks>
        /// <para>This point moves to the start of the line if the point is on the first
        /// line.</para>
        /// <para>This value could be affected by whether or not Word Wrap is turned on in the view.</para>
        /// <para>If the point is on the first line of the file, the caret is moved to the beginning of the line.</para>
        /// </remarks>
        public abstract void MoveToBeginningOfPreviousViewLine();

        /// <summary>
        /// When implemented in a derived class, gets a display text point for the first 
        /// non-whitespace character on the current view line.
        /// </summary>
        /// <remarks>
        /// If a line is all white space, this method returns a <see cref="DisplayTextPoint"/> at the start of the line.
        /// </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract DisplayTextPoint GetFirstNonWhiteSpaceCharacterOnViewLine();

        /// <summary>
        ///  When implemented in a derived class, gets the integer representation of the current position of this text point
        /// in relation to the visual start of the line.
        /// </summary>
        public abstract int DisplayColumn { get; }

        /// <summary>
        ///  When implemented in a derived class, determines whether the point is currently visible on the screen.
        /// </summary>
        public abstract bool IsVisible { get; }

        /// <summary>
        /// Creates a new <see cref="DisplayTextPoint"/> at this position that can be
        /// moved independently from this one.
        /// </summary>
        new public DisplayTextPoint Clone()
        {
            return CloneDisplayTextPointInternal();
        }

        /// <summary>
        ///  When implemented in a derived class, gets the <see cref="DisplayTextRange"/> that has this point and <paramref name="otherPoint"/>
        /// as its start and end points.
        /// </summary>
        /// <returns>The <see cref="DisplayTextRange"/> that starts at this point and ends at <paramref name="otherPoint"/>.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="otherPoint"/> does not belong to the same buffer as this point, or
        /// <paramref name="otherPoint"/> does not belong to the same view as this point.</exception>
        public abstract DisplayTextRange GetDisplayTextRange(DisplayTextPoint otherPoint);

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="DisplayTextRange"/> that has this point and <paramref name="otherPosition"/>
        /// as its start and end positions.
        /// </summary>
        /// <returns>The <see cref="DisplayTextRange"/> that starts at this point and ends at <paramref name="otherPosition"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="otherPosition"/> is in negative or past the end of this buffer.</exception>
        public abstract DisplayTextRange GetDisplayTextRange(int otherPosition);

        /// <summary>
        /// Clones this <see cref="DisplayTextPoint"/>.
        /// </summary>
        /// <returns></returns>
        protected sealed override TextPoint CloneInternal()
        {
            return CloneDisplayTextPointInternal();
        }

        /// <summary>
        /// When implemented in a derived class, clones this <see cref="DisplayTextPoint"/>.
        /// </summary>
        protected abstract DisplayTextPoint CloneDisplayTextPointInternal();
    }
}
