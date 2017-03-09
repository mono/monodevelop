//
// ITextSource.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Text;

namespace MonoDevelop.Core.Text
{
	/// <summary>
	/// A read-only view on a (potentially mutable) text source.
	/// The IDocument interface derives from this interface.
	/// </summary>
	public interface ITextSource
	{
		/// <summary>
		/// Gets a version identifier for this text source.
		/// Returns null for unversioned text sources.
		/// </summary>
		ITextSourceVersion Version { get; }

		/// <summary>
		/// Encoding of the text that was read from or is going to be saved to.
		/// </summary>
		Encoding Encoding { get; }

		/// <summary>
		/// Gets the total text length.
		/// </summary>
		/// <returns>The length of the text, in characters.</returns>
		/// <remarks>This is the same as Text.Length, but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		int Length { get; }

		/// <summary>
		/// Gets the whole text as string.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		string Text { get; }

		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		char this [int offset] { get; }

		/// <summary>
		/// Gets a character at the specified position in the document.
		/// </summary>
		/// <paramref name="offset">The index of the character to get.</paramref>
		/// <exception cref="ArgumentOutOfRangeException">Offset is outside the valid range (0 to TextLength-1).</exception>
		/// <returns>The character at the specified position.</returns>
		/// <remarks>This is the same as Text[offset], but is more efficient because
		///  it doesn't require creating a String object.</remarks>
		char GetCharAt (int offset);

		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		/// <remarks>This is the same as Text.Substring, but is more efficient because
		///  it doesn't require creating a String object for the whole document.</remarks>
		string GetTextAt (int offset, int length);

		/// <summary>
		/// Creates a new TextReader to read from this text source.
		/// </summary>
		TextReader CreateReader ();

		/// <summary>
		/// Creates a new TextReader to read from this text source.
		/// </summary>
		TextReader CreateReader (int offset, int length);		

		/// <summary>
		/// Writes the text from this document into the TextWriter.
		/// </summary>
		void WriteTextTo (TextWriter writer);

		/// <summary>
		/// Writes the text from this document into the TextWriter.
		/// </summary>
		void WriteTextTo (TextWriter writer, int offset, int length);

		/// <summary>
		/// Copies text from the source index to a destination array at destinationIndex.
		/// </summary>
		/// <param name="sourceIndex">The start offset copied from.</param>
		/// <param name="destination">The destination array copied to.</param>
		/// <param name="destinationIndex">The destination index copied to.</param>
		/// <param name="count">The number of characters to be copied.</param>
		void CopyTo (int sourceIndex, char [] destination, int destinationIndex, int count);

		/// <summary>
		/// Creates an immutable snapshot of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextSource CreateSnapshot ();

		/// <summary>
		/// Creates an immutable snapshot of a part of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		ITextSource CreateSnapshot (int offset, int length);
	}

	public static class TextSourceExtension
	{
		/// <summary>
		/// Retrieves the text for a portion of the document.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">offset or length is outside the valid range.</exception>
		public static string GetTextAt (this ITextSource source, ISegment segment)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			return source.GetTextAt (segment.Offset, segment.Length);
		}


		public static string GetTextBetween (this ITextSource source, int startOffset, int endOffset)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (startOffset < 0 || startOffset > source.Length)
				throw new ArgumentNullException ("startOffset");
			if (endOffset < 0 || endOffset > source.Length)
				throw new ArgumentNullException ("endOffset");
			if (startOffset > endOffset)
				throw new InvalidOperationException ();
			return source.GetTextAt (startOffset, endOffset - startOffset);
		}


		/// <summary>
		/// Writes the text from this document into a file.
		/// </summary>
		public static void WriteTextTo (this ITextSource source, string fileName)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			TextFileUtility.WriteText (fileName, source);
		}

		/// <summary>
		/// Writes the text from this document into the TextWriter.
		/// </summary>
		public static void WriteTextTo (this ITextSource source, TextWriter writer, ISegment segment)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (writer == null)
				throw new ArgumentNullException ("writer");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			source.WriteTextTo (writer, segment.Offset, segment.Length);
		}

		/// <summary>
		/// Creates a new TextReader to read from this text source.
		/// </summary>
		public static TextReader CreateReader (this ITextSource source, ISegment segment)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return source.CreateReader (segment.Offset, segment.Length);
		}

		/// <summary>
		/// Creates an immutable snapshot of a part of this text source.
		/// Unlike all other methods in this interface, this method is thread-safe.
		/// </summary>
		public static ITextSource CreateSnapshot (this ITextSource source, ISegment segment)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			return source.CreateSnapshot (segment.Offset, segment.Length);
		}
	}
}