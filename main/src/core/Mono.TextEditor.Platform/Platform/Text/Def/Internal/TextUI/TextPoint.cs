//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents a point in the <see cref="TextBuffer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While this point is immutable, its position may change in response
    /// to edits in the text.
    /// </para>
    /// </remarks>
    public abstract class TextPoint
    {
        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextBuffer"/> of this point.
        /// </summary>
        public abstract TextBuffer TextBuffer { get; }

        /// <summary>
        /// When implemented in a derived class, gets the integer representation of the current position of this text point 
        /// in relation to the start of the buffer.
        /// </summary>
        public abstract int CurrentPosition { get; }

        /// <summary>
        /// When implemented in a derived class, gets the integer representation of the current position of this text point
        /// in relation to the start of the line this point is on.
        /// </summary>
        public abstract int Column { get; }

        /// <summary>
        /// When implemented in a derived class, deletes the character after this text point.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool DeleteNext();

        /// <summary>
        /// When implemented in a derived class, deletes the character before this text point.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool DeletePrevious();

        /// <summary>
        /// When implemented in a derived class, gets a text point for the first non-whitespace
        /// character on the current line.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If a line is all white space, this method returns a <see cref="TextPoint"/> at the end of the line, but before the
        /// line break.
        /// </para>
        /// </remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract TextPoint GetFirstNonWhiteSpaceCharacterOnLine();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> of the current word. The current word may be white space only.
        /// </summary>
        /// <returns>The <see cref="TextRange"/> of the current word.</returns>
        public abstract TextRange GetCurrentWord();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> of the next word that is not white space.
        /// </summary>
        /// <returns>The <see cref="TextRange"/> of the next word.</returns>
        public abstract TextRange GetNextWord();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> of the previous word that is not white space.
        /// </summary>
        /// <returns>The <see cref="TextRange"/> of the previous word.</returns>
        public abstract TextRange GetPreviousWord();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> that has this point and <paramref name="otherPoint"/>
        /// as its start and end points.
        /// </summary>
        /// <returns>The <see cref="TextRange"/> that starts at this point and ends at <paramref name="otherPoint"/>.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="otherPoint"/> does not belong to the same buffer as this point.</exception>
        public abstract TextRange GetTextRange(TextPoint otherPoint);

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextRange"/> that has this point and <paramref name="otherPosition"/>
        /// as its start and end positions.
        /// </summary>
        /// <returns>The <see cref="TextRange"/> that starts at this point and ends at <paramref name="otherPosition"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="otherPosition"/> is negative or past the end of this buffer.</exception>
        public abstract TextRange GetTextRange(int otherPosition);

        /// <summary>
        /// When implemented in a derived class, inserts a new line character at this text point.
        /// </summary>
        /// <returns>
        /// Whether the edit succeeded.
        /// </returns>
        public abstract bool InsertNewLine();

        /// <summary>
        /// When implemented in a derived class, inserts a logical tab at this text point.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool InsertIndent();

        /// <summary>
        /// When implemented in a derived class, inserts <paramref name="text"/> at this text point.
        /// </summary>
        /// <param name="text">The text to insert.</param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool InsertText(string text);

        /// <summary>
        /// When implemented in a derived class, gets the line this text point is on.
        /// </summary>
        public abstract int LineNumber { get; }

        /// <summary>
        /// When implemented in a derived class, gets the position of the start of the line this text point is on.
        /// </summary>
        public abstract int StartOfLine { get; }

        /// <summary>
        /// When implemented in a derived class, gets the position of the end of the line this text point is on.
        /// </summary>
        public abstract int EndOfLine { get; }

        /// <summary>
        /// When implemented in a derived class, removes a logical tab before this text point.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool RemovePreviousIndent();

        /// <summary>
        /// When implemented in a derived class, transposes the two characters on either side of this text point.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool TransposeCharacter();

        /// <summary>
        /// When implemented in a derived class, transposes the line this point is one with the next line. If this point is on the last
        /// line of the file, the line is transposed with the previous one.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool TransposeLine();

        /// <summary>
        /// When implemented in a derived class, transposes the line this point is one with the given line number.
        /// </summary>
        /// <param name="lineNumber">The line number with which to transpose the line this point is on.</param>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool TransposeLine(int lineNumber);

        /// <summary>
        /// When implemented in a derived class, gets the underlying <see cref="SnapshotPoint"/> of this text point.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract SnapshotPoint AdvancedTextPoint
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, gets the next character after this text point.
        /// </summary>
        public abstract string GetNextCharacter();

        /// <summary>
        /// When implemented in a derived class, gets the previous character before this text point.
        /// </summary>
        /// <returns>The previous character.</returns>
        public abstract string GetPreviousCharacter();

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> between this text point and <paramref name="endPoint"/>.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <param name="endPoint">The text point at which to stop searching.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at this text point.</returns>
        public abstract TextRange Find(string pattern, FindOptions findOptions, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> between this text point and <paramref name="endPoint"/>.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="endPoint">The text point at which to stop searching.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at this text point.</returns>
        public abstract TextRange Find(string pattern, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> starting from this text point.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at this text point.</returns>
        public abstract TextRange Find(string pattern, FindOptions findOptions);

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> starting from this text point.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at this text point.</returns>
        public abstract TextRange Find(string pattern);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> between this text point and <paramref name="endPoint"/>.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="endPoint">The text point at which to stop searching.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> between this text point and <paramref name="endPoint"/>.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="endPoint">The text point at which to stop searching.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern, FindOptions findOptions, TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> starting from this text point.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> starting from this text point.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern, FindOptions findOptions);

        /// <summary>
        /// When implemented in a derived class, moves this text point to a specific location.
        /// </summary>
        /// <param name="position">The new position of this text point.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative or past the end of this buffer.</exception>
        public abstract void MoveTo(int position);

        /// <summary>
        /// When implemented in a derived class, moves this point to the next character in the buffer.
        /// </summary>
        public abstract void MoveToNextCharacter();

        /// <summary>
        /// When implemented in a derived class, moves this point to the previous character in the buffer.
        /// </summary>
        /// 
        public abstract void MoveToPreviousCharacter();

        /// <summary>
        /// Creates a new <see cref="TextPoint"/> at this position that can be
        /// moved independently from this one.
        /// </summary>
        /// <returns>The cloned <see cref="TextPoint"/>.</returns>
        public TextPoint Clone()
        {
            return CloneInternal();
        }

        /// <summary>
        /// When implemented in a derived class, clones the <see cref="TextPoint"/>.
        /// </summary>
        /// <returns>The cloned <see cref="TextPoint"/>.</returns>
        protected abstract TextPoint CloneInternal();

        /// <summary>
        /// When implemented in a derived class, moves the text point at the start of the specified line and ensures it is visible.
        /// </summary>
        /// <param name="lineNumber">
        /// The line number on which to position the text point.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero 
        /// or greater than the line number of the last line in the text buffer.</exception>
        public abstract void MoveToLine(int lineNumber);

        /// <summary>
        /// When implemented in a derived class, movea this point to the end of the line that it is currently on.
        /// </summary>
        public abstract void MoveToEndOfLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the start of the line that it is currently on.
        /// </summary>
        public abstract void MoveToStartOfLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the end of the document.
        /// </summary>
        public abstract void MoveToEndOfDocument();

        /// <summary>
        /// When implemented in a derived class, moves this point to the start of the document.
        /// </summary>
        public abstract void MoveToStartOfDocument();

        /// <summary>
        /// When implemented in a derived class, moves this point to the beginning of the next line.
        /// </summary>
        /// <remarks>
        /// This point moves to the end of the line if the point is on the last
        /// line.
        /// </remarks>
        public abstract void MoveToBeginningOfNextLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the beginning of the previous line.
        /// </summary>
        /// <remarks>
        /// This point moves to the start of the line if the point is on the first
        /// line.
        /// </remarks>
        public abstract void MoveToBeginningOfPreviousLine();

        /// <summary>
        /// When implemented in a derived class, moves this point to the end of the current word, or to the beginning of the
        /// next word if the point is already at the end of the current word.
        /// </summary>
        public abstract void MoveToNextWord();

        /// <summary>
        /// When implemented in a derived class, moves this point to the start of the current word, or the end of the
        /// previous word if the point is already at the start of the current word.
        /// </summary>
        public abstract void MoveToPreviousWord();
    }
}
