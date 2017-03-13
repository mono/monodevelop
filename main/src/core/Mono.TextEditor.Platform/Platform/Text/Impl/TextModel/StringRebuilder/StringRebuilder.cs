//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Collections;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// An immutable variation on the StringBuilder class.
    /// </summary>
    internal abstract class StringRebuilder
    {
        protected StringRebuilder(int length, int lineBreakCount, int depth)
        {
            this.Length = length;
            this.LineBreakCount = lineBreakCount;
            this.Depth = depth;
        }

        /// <summary>
        /// Number of characters in this <see cref="StringRebuilder"/>.
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Number of line breaks in this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <remarks>Line breaks consist of any of '\r', '\n', 0x85,
        /// or a "\r\n" pair (which is treated as a single line break).</remarks>
        public readonly int LineBreakCount;

        public readonly int Depth;

        /// <summary>
        /// Get the zero-based line number that contains <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position of the character for which to get the line number.</param>
        /// <returns>Number of the line that contains <paramref name="position"/>.</returns>
        /// <remarks>
        /// Lines are bounded by line breaks and the start and end of this <see cref="StringRebuilder"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        public abstract int GetLineNumberFromPosition(int position);

        /// <summary>
        /// Get the LineSpan associated with a zero-based line number.
        /// </summary>
        /// <param name="lineNumber">Line number for which to get the LineSpan.</param>
        /// <returns>LineSpan that define the given line number.</returns>
        /// <remarks>
        /// <para>The last "line" in the StringRebuilder has an implicit line break length of zero.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero or greater than <see cref="LineBreakCount"/>.</exception>
        public abstract LineSpan GetLineFromLineNumber(int lineNumber);

        /// <summary>
        /// Get the "leaf" node of the string rebuilder that contains position.
        /// </summary>
        /// <param name="position">position for which to get the leaf.</param>
        /// <param name="offset">number of characters to the left of the leaf.</param>
        /// <returns>leaf node from the string rebuilder.</returns>
        public abstract StringRebuilder GetLeaf(int position, out int offset);

        /// <summary>
        /// Character at the given index.
        /// </summary>
        /// <param name="index">Index to get the character for.</param>
        /// <returns>Character at position <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.</exception>
        public abstract char this[int index] { get; }

        /// <summary>
        /// Get the string that contains all of the characters in the specified span.
        /// </summary>
        /// <param name="span">Span for which to get the text.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> can contain millions of characters. Be careful what you
        /// ask for: you might get it.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract string GetText(Span span);

        /// <summary>
        /// Convert a range of text to a character array.
        /// </summary>
        /// <param name="startIndex">
        /// The starting index of the range of text.
        /// </param>
        /// <param name="length">
        /// The length of the text.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than zero or <paramref name="startIndex"/> + <paramref name="length"/> is greater than <see cref="Length"/>.</exception>
        public char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");

            if ((length < 0) || (startIndex + length > this.Length) || (startIndex + length < 0))
                throw new ArgumentOutOfRangeException("length");

            char[] copy = new char[length];
            this.CopyTo(startIndex, copy, 0, length);

            return copy;
        }

        /// <summary>
        /// Copy a range of text to a destination character array.
        /// </summary>
        /// <param name="sourceIndex">
        /// The starting index to copy from.
        /// </param>
        /// <param name="destination">
        /// The destination array.
        /// </param>
        /// <param name="destinationIndex">
        /// The index in the destination of the first position to be copied to.
        /// </param>
        /// <param name="count">
        /// The number of characters to copy.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero or <paramref name="sourceIndex"/> + <paramref name="count"/> is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="destinationIndex"/> is less than zero or <paramref name="destinationIndex"/> + <paramref name="count"/> is greater than the length of <paramref name="destination"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Write a substring of the contents of this <see cref="StringRebuilder"/> to a TextWriter.
        /// </summary>
        /// <param name="writer">TextWriter to use.</param>
        /// <param name="span">Span to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract void Write(TextWriter writer, Span span);

        /// <summary>
        /// Create a new StringRebuilder that corresponds to a substring of this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="span">span that defines the desired substring.</param>
        /// <returns>A new StringRebuilder containing the substring.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public abstract StringRebuilder Substring(Span span);

        /// <summary>
        /// Create a new StringRebuilder equivalent to appending text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Append(string text)
        {
            return this.Insert(this.Length, text);
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to appending text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Append(StringRebuilder text)
        {
            return this.Insert(this.Length, text);
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to inserting text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Insert(int position, string text)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(text));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to inserting text into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Insert(int position, StringRebuilder text)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException("position");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, position), text, Span.FromBounds(position, this.Length));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to inserting storage into this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="storage">Storage containing text to insert.</param>
        /// <returns>A new StringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        public StringRebuilder Insert(int position, ITextStorage storage)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(storage));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to deleting text from this <see cref="StringRebuilder"/>.
        /// </summary>
        /// <param name="span">Span of text to delete.</param>
        /// <returns>A new StringRebuilder containing the deletion.</returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        public StringRebuilder Delete(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            return this.Assemble(Span.FromBounds(0, span.Start), Span.FromBounds(span.End, this.Length));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="StringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Replace(Span span, string text)
        {
            return this.Replace(span, SimpleStringRebuilder.Create(text));
        }

        /// <summary>
        /// Create a new StringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="StringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="StringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        public StringRebuilder Replace(Span span, StringRebuilder text)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, span.Start), text, Span.FromBounds(span.End, this.Length));
        }

        public abstract StringRebuilder Child(bool rightSide);

        /// <summary>
        /// Whether this piece ends with a return character. If this is true and the succeeding
        /// piece StartsWithNewline, then a line break crosses a piece boundary.
        /// </summary>
        public abstract bool EndsWithReturn { get; }

        /// <summary>
        /// Whether this piece starts with a newline character. If this is true and the preceeding
        /// piece EndsWithReturn, then a line break crosses a piece boundary.
        /// </summary>
        public abstract bool StartsWithNewLine { get; }

        #region Private
        private StringRebuilder Assemble(Span left, Span right)
        {
            if (left.Length == 0)
                return this.Substring(right);
            else if (right.Length == 0)
                return this.Substring(left);
            else if (left.Length + right.Length == this.Length)
                return this;
            else
                return BinaryStringRebuilder.Create(this.Substring(left), this.Substring(right));
        }

        private StringRebuilder Assemble(Span left, StringRebuilder text, Span right)
        {
            if (text.Length == 0)
                return Assemble(left, right);
            else if (left.Length == 0)
                return (right.Length == 0) ? text : BinaryStringRebuilder.Create(text, this.Substring(right));
            else if (right.Length == 0)
                return BinaryStringRebuilder.Create(this.Substring(left), text);
            else if (left.Length < right.Length)
                return BinaryStringRebuilder.Create(BinaryStringRebuilder.Create(this.Substring(left), text),
                                                    this.Substring(right));
            else
                return BinaryStringRebuilder.Create(this.Substring(left),
                                                    BinaryStringRebuilder.Create(text, this.Substring(right)));
        }
        #endregion
    }
}
