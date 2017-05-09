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
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents a range of text in the buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While this range is immutable, edits to the text will cause
    /// it to adjust its location in response to the edits.
    /// </para>
    /// </remarks>
    public abstract class TextRange : IEnumerable<TextPoint>
    {
        /// <summary>
        /// When implemented in a derived class, gets the start point of this text range.
        /// </summary>
        /// <returns>The starting <see cref="TextPoint"/>.</returns>
        public abstract TextPoint GetStartPoint();

        /// <summary>
        /// When implemented in a derived class, gets the end point of this text range.
        /// </summary>
        /// <returns>The end <see cref="TextPoint"/>.</returns>
        public abstract TextPoint GetEndPoint();

        /// <summary>
        /// When implemented in a derived class, gets the <see cref="TextBuffer"/> of this text range.
        /// </summary>
        /// <returns>The <see cref="TextBuffer"/> of this text range.</returns>
        public abstract TextBuffer TextBuffer
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, gets the underlying <see cref="SnapshotSpan"/> of this <see cref="TextRange"/>.
        /// The <see cref="SnapshotSpan"/> should be used only for advanced functionality.
        /// </summary>
        /// <returns>The <see cref="SnapshotSpan"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public abstract SnapshotSpan AdvancedTextRange
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, makes the text in this range uppercase.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks><para>If the range is empty, will apply to the character next to the range only.</para></remarks>
        public abstract bool MakeUppercase();

        /// <summary>
        /// When implemented in a derived class, makes the text in the this range lowercase.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        /// <remarks><para>If the range is empty, this method applies to the character next to the range only.</para></remarks>
        public abstract bool MakeLowercase();

        /// <summary>
        /// When implemented in a derived class, makes the first character in every word in this range uppercase, and makes the rest of the characters lowercase.
        /// </summary>
        /// <remarks>
        /// If the range is empty, this method applies to the character next to the range only.
        /// If the range starts in the middle of a word, only the part in the range will be made lowercase.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool Capitalize();

        /// <summary>
        /// When implemented in a derived class, switches the case of every character in this range.
        /// </summary>
        /// <remarks><para>If the range is empty, this method applies to the character next to the range only.</para></remarks>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool ToggleCase();

        /// <summary>
        /// When implemented in a derived class, deletes all the text in this range.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool Delete();

        /// <summary>
        /// When implemented in a derived class, indents all the lines in this range.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool Indent();

        /// <summary>
        /// When implemented in a derived class, unindents all the lines in this range.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool Unindent();

        /// <summary>
        /// When implemented in a derived class, determines whether the <see cref="TextRange"/> is zero-length.
        /// </summary>
        /// <returns><c>true</c> if the <see cref="TextRange"/> is zero length, <c>false</c> otherwise.</returns>
        public abstract bool IsEmpty
        {
            get;
        }

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> in this text range.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at the start point of this range.</returns>
        public abstract TextRange Find(string pattern);

        /// <summary>
        /// When implemented in a derived class, finds the start of the first occurrence of <paramref name="pattern"/> in this text range.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <returns>The text range of the first occurrence of the pattern if it was found, otherwise a zero-length text range at the start point of this range.</returns>
        public abstract TextRange Find(string pattern, FindOptions findOptions);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> starting in this text range.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern);

        /// <summary>
        /// When implemented in a derived class, finds all matches of <paramref name="pattern"/> starting in this text range.
        /// </summary>
        /// <param name="pattern">The pattern to find.</param>
        /// <param name="findOptions">The options to use while searching.</param>
        /// <returns>A list of matches in the order they were found.</returns>
        public abstract Collection<TextRange> FindAll(string pattern, FindOptions findOptions);

        /// <summary>
        /// When implemented in a derived class, replaces the text in this range with <paramref name="newText"/>.
        /// </summary>
        /// <param name="newText">The new text.</param>
        /// <remarks>
        /// This <see cref="TextRange"/> spans the new text after it has been replaced.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the edit succeeded, otherwise <c>false</c>.
        /// </returns>
        public abstract bool ReplaceText(string newText);

        /// <summary>
        /// When implemented in a derived class, gets the text in this range.
        /// </summary>
        /// <returns>The text in the range.</returns>
        public abstract string GetText();

        /// <summary>
        /// When implemented in a derived class, creates a clone of this text range than can be moved independently of this one.
        /// </summary>
        /// <returns>The cloned <see cref="TextRange"/>.</returns>
        public TextRange Clone()
        {
            return CloneInternal();
        }

        /// <summary>
        /// When implemented in a derived class, clones the text range.
        /// </summary>
        /// <returns>The cloned <see cref="TextRange"/>.</returns>
        protected abstract TextRange CloneInternal();

        /// <summary>
        /// When implemented in a derived class, sets the start point of this text range.
        /// </summary>
        /// <param name="startPoint">The new start point.</param>
        /// <remarks>
        /// If <paramref name="startPoint"/> occurs after
        /// the current end point in the buffer, <paramref name="startPoint"/> becomes the end point, and the current end point becomes the start point.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><paramref name="startPoint"/> belongs to another buffer.</exception>
        public abstract void SetStart(TextPoint startPoint);

        /// <summary>
        /// When implemented in a derived class, sets the end point of this text range.
        /// </summary>
        /// <param name="endPoint">The new end point.</param>
        /// <remarks>
        /// <para>
        /// If <paramref name="endPoint"/> is before
        /// the current start point in the buffer, <paramref name="endPoint"/> becomes the start point, and the current start point becomes the end point.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><paramref name="endPoint"/> belongs to another buffer.</exception>
        public abstract void SetEnd(TextPoint endPoint);

        /// <summary>
        /// When implemented in a derived class, moves this text range to the range of <paramref name="newRange"/>.
        /// </summary>
        /// <param name="newRange">The new range.</param>
        public abstract void MoveTo(TextRange newRange);

        /// <summary>
        /// When implemented in a derived class, gets the enumerator of type <see cref="TextPoint"/>.
        /// </summary>
        /// <returns>the enumerator of type <see cref="TextPoint"/>.</returns>
        protected abstract IEnumerator<TextPoint> GetEnumeratorInternal();

        #region IEnumerable<TextPoint> Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>An enumerator of type <see cref="TextPoint"/>.</returns>
        public IEnumerator<TextPoint> GetEnumerator()
        {
            return GetEnumeratorInternal();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
