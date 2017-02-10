using System;
using System.IO;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Abstraction for an immutable block of text accessible to a text buffer.
    /// </summary>
    public interface ITextStorage
    {
        /// <summary>
        /// The number of characters in the text storage.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Extracts a substring of the storage.
        /// </summary>
        /// <param name="startIndex">Starting index (origin is zero).</param>
        /// <param name="length">Length of text to extract.</param>
        /// <returns>The extracted text.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is less than zero or 
        /// greater than the length of the storage, or <paramref name="length"/> is less than zero, 
        /// or <paramref name="startIndex"/> plus <paramref name="length"/> is greater than the length of the storage.
        /// </exception>
        string GetText(int startIndex, int length);

        /// <summary>
        /// Copies a substring of the storage to an array of characters.
        /// </summary>
        /// <param name="sourceIndex">
        /// The starting index in the text snapshot.
        /// </param>
        /// <param name="destination">
        /// The destination array.
        /// </param>
        /// <param name="destinationIndex">
        /// The index in the destination array at which to start copying the text.
        /// </param>
        /// <param name="count">
        /// The number of characters to copy.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is less than zero or greater than the length of the snapshot, or
        /// <paramref name="count"/> is less than zero, or <paramref name="sourceIndex"/> + <paramref name="count"/> is greater than the length of the snapshot, or
        /// <paramref name="destinationIndex"/> is less than zero, or <paramref name="destinationIndex"/> plus <paramref name="count"/> is greater than the length of <paramref name="destination"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Write a substring of the storage to the provided <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="writer">The writer to which to write.</param>
        /// <param name="startIndex">The starting position in the storage.</param>
        /// <param name="Length">The length of the substring to write.</param>
        void Write(TextWriter writer, int startIndex, int Length);

        /// <summary>
        /// Gets a single character at the specified position.
        /// </summary>
        /// <param name="index">The position of the character.</param>
        /// <returns>The character at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero or greater than or equal to the length of the storage.</exception>
        char this[int index] { get; }

        /// <summary>
        /// Indicates whether the <paramref name="index"/>th character is a newline character.
        /// </summary>
        /// <param name="index">The position of the character of interest.</param>
        /// <returns>Whether said character is a newline.</returns>
        bool IsNewLine(int index);

        /// <summary>
        /// Indicates whether the <paramref name="index"/>th character is a return character.
        /// </summary>
        /// <param name="index">The position of the character of interest.</param>
        /// <returns>Whether said character is a return.</returns>
        bool IsReturn(int index);

        /// <summary>
        /// Positions of the line breaks in this storage. A two-character line break (\r\n) is
        /// represented by the position of its first character.
        /// </summary>
        ILineBreaks LineBreaks { get; }
    }

    /// <summary>
    /// Information about the line breaks contained in an <see cref="ITextStorage"/>.
    /// </summary>
    public interface ILineBreaks
    {
        /// <summary>
        /// The number of line breaks in the <see cref="ITextStorage"/>.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// The starting position in the storage of the <paramref name="index"/>th line break.
        /// </summary>
        /// <param name="index">Selects which line break to consider.</param>
        int StartOfLineBreak(int index);

        /// <summary>
        /// The ending position in the storage of the <paramref name="index"/>th line break. This
        /// value indexes the first character in the following line.
        /// </summary>
        /// <param name="index">Selects which line break to consider.</param>
        int EndOfLineBreak(int index);
    }
}