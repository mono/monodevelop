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
    internal interface IStringRebuilder
    {
        /// <summary>
        /// Number of characters in this <see cref="IStringRebuilder"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Number of line breaks in this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <remarks>Line breaks consist of any of '\r', '\n', 0x85,
        /// or a "\r\n" pair (which is treated as a single line break).</remarks>
        int LineBreakCount { get; }

        /// <summary>
        /// Get the zero-based line number that contains <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position of the character for which to get the line number.</param>
        /// <returns>Number of the line that contains <paramref name="position"/>.</returns>
        /// <remarks>
        /// Lines are bounded by line breaks and the start and end of this <see cref="IStringRebuilder"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        int GetLineNumberFromPosition(int position);

        /// <summary>
        /// Get the LineSpan associated with a zero-based line number.
        /// </summary>
        /// <param name="lineNumber">Line number for which to get the LineSpan.</param>
        /// <returns>LineSpan that define the given line number.</returns>
        /// <remarks>
        /// <para>The last "line" in the IStringRebuilder has an implicit line break length of zero.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is less than zero or greater than <see cref="LineBreakCount"/>.</exception>
        LineSpan GetLineFromLineNumber(int lineNumber);

        /// <summary>
        /// Get the "leaf" node of the string rebuilder that contains position.
        /// </summary>
        /// <param name="position">position for which to get the leaf.</param>
        /// <param name="offset">number of characters to the left of the leaf.</param>
        /// <returns>leaf node from the string rebuilder.</returns>
        IStringRebuilder GetLeaf(int position, out int offset);

        /// <summary>
        /// Character at the given index.
        /// </summary>
        /// <param name="index">Index to get the character for.</param>
        /// <returns>Character at position <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.</exception>
        char this[int index] { get; }

        /// <summary>
        /// Get the string that contains all of the characters in the specified span.
        /// </summary>
        /// <param name="span">Span for which to get the text.</param>
        /// <returns></returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> can contain millions of characters. Be careful what you
        /// ask for: you might get it.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        string GetText(Span span);

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
        char[] ToCharArray(int startIndex, int length);

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
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Write a substring of the contents of this <see cref="IStringRebuilder"/> to a TextWriter.
        /// </summary>
        /// <param name="writer">TextWriter to use.</param>
        /// <param name="span">Span to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        void Write(TextWriter writer, Span span);

        /// <summary>
        /// Create a new IStringRebuilder that corresponds to a substring of this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="span">span that defines the desired substring.</param>
        /// <returns>A new IStringRebuilder containing the substring.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        IStringRebuilder Substring(Span span);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to appending text into this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="text">Text to append.</param>
        /// <returns>A new IStringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Append(string text);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to appending text into this <see cref="IStringRebuilder"/>.
        /// </summary>
         /// <param name="text">Text to append.</param>
        /// <returns>A new IStringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Append(IStringRebuilder text);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to inserting text into this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new IStringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Insert(int position, string text);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to inserting text into this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="text">Text to insert.</param>
        /// <returns>A new IStringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Insert(int position, IStringRebuilder text);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to inserting storage into this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="position">Position at which to insert.</param>
        /// <param name="storage">Storage containing text to insert.</param>
        /// <returns>A new IStringRebuilder containing the insertion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is less than zero or greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="storage"/> is null.</exception>
        IStringRebuilder Insert(int position, ITextStorage storage);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to deleting text from this <see cref="IStringRebuilder"/>.
        /// </summary>
        /// <param name="span">Span of text to delete.</param>
        /// <returns>A new IStringRebuilder containing the deletion.</returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        IStringRebuilder Delete(Span span);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="IStringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Replace(Span span, string text);

        /// <summary>
        /// Create a new IStringRebuilder equivalent to replacing a contiguous span of characters
        /// with different text.
        /// </summary>
        /// <param name="span">
        /// Span of text in this <see cref="IStringRebuilder"/> to replace.
        /// </param>
        /// <param name="text">
        /// The new text to replace the old.
        /// </param>
        /// <returns>
        /// A new string rebuilder containing the replacement.
        /// </returns>
        /// <remarks>
        /// <para>this <see cref="IStringRebuilder"/> is not modified.</para>
        /// <para>This operation can be performed simultaneously on multiple threads.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="span"/>.End is greater than <see cref="Length"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        IStringRebuilder Replace(Span span, IStringRebuilder text);

        int Depth { get; }

        IStringRebuilder Child(bool rightSide);

        /// <summary>
        /// Whether this piece ends with a return character. If this is true and the succeeding
        /// piece StartsWithNewline, then a line break crosses a piece boundary.
        /// </summary>
        bool EndsWithReturn { get; }

        /// <summary>
        /// Whether this piece starts with a newline character. If this is true and the preceeding
        /// piece EndsWithReturn, then a line break crosses a piece boundary.
        /// </summary>
        bool StartsWithNewLine { get; }
    }
}
