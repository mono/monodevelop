/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit.Diff;
using NGit.Util;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>A Sequence supporting UNIX formatted text in byte[] format.</summary>
	/// <remarks>
	/// A Sequence supporting UNIX formatted text in byte[] format.
	/// <p>
	/// Elements of the sequence are the lines of the file, as delimited by the UNIX
	/// newline character ('\n'). The file content is treated as 8 bit binary text,
	/// with no assumptions or requirements on character encoding.
	/// <p>
	/// Note that the first line of the file is element 0, as defined by the Sequence
	/// interface API. Traditionally in a text editor a patch file the first line is
	/// line number 1. Callers may need to subtract 1 prior to invoking methods if
	/// they are converting from "line number" to "element index".
	/// </remarks>
	public class RawText : Sequence
	{
		/// <summary>A Rawtext of length 0</summary>
		public static readonly NGit.Diff.RawText EMPTY_TEXT = new NGit.Diff.RawText(new byte
			[0]);

		/// <summary>
		/// Number of bytes to check for heuristics in
		/// <see cref="IsBinary(byte[])">IsBinary(byte[])</see>
		/// 
		/// </summary>
		private const int FIRST_FEW_BYTES = 8000;

		/// <summary>The file content for this sequence.</summary>
		/// <remarks>The file content for this sequence.</remarks>
		protected internal readonly byte[] content;

		/// <summary>
		/// Map of line number to starting position within
		/// <see cref="content">content</see>
		/// .
		/// </summary>
		protected internal readonly IntList lines;

		/// <summary>Create a new sequence from an existing content byte array.</summary>
		/// <remarks>
		/// Create a new sequence from an existing content byte array.
		/// <p>
		/// The entire array (indexes 0 through length-1) is used as the content.
		/// </remarks>
		/// <param name="input">
		/// the content array. The array is never modified, so passing
		/// through cached arrays is safe.
		/// </param>
		public RawText(byte[] input)
		{
			content = input;
			lines = RawParseUtils.LineMap(content, 0, content.Length);
		}

		/// <summary>Create a new sequence from a file.</summary>
		/// <remarks>
		/// Create a new sequence from a file.
		/// <p>
		/// The entire file contents are used.
		/// </remarks>
		/// <param name="file">the text file.</param>
		/// <exception cref="System.IO.IOException">if Exceptions occur while reading the file
		/// 	</exception>
		public RawText(FilePath file) : this(IOUtil.ReadFully(file))
		{
		}

		/// <returns>total number of items in the sequence.</returns>
		public override int Size()
		{
			// The line map is always 2 entries larger than the number of lines in
			// the file. Index 0 is padded out/unused. The last index is the total
			// length of the buffer, and acts as a sentinel.
			//
			return lines.Size() - 2;
		}

		/// <summary>Write a specific line to the output stream, without its trailing LF.</summary>
		/// <remarks>
		/// Write a specific line to the output stream, without its trailing LF.
		/// <p>
		/// The specified line is copied as-is, with no character encoding
		/// translation performed.
		/// <p>
		/// If the specified line ends with an LF ('\n'), the LF is <b>not</b>
		/// copied. It is up to the caller to write the LF, if desired, between
		/// output lines.
		/// </remarks>
		/// <param name="out">stream to copy the line data onto.</param>
		/// <param name="i">
		/// index of the line to extract. Note this is 0-based, so line
		/// number 1 is actually index 0.
		/// </param>
		/// <exception cref="System.IO.IOException">the stream write operation failed.</exception>
		public virtual void WriteLine(OutputStream @out, int i)
		{
			int start = GetStart(i);
			int end = GetEnd(i);
			if (content[end - 1] == '\n')
			{
				end--;
			}
			@out.Write(content, start, end - start);
		}

		/// <summary>Determine if the file ends with a LF ('\n').</summary>
		/// <remarks>Determine if the file ends with a LF ('\n').</remarks>
		/// <returns>true if the last line has an LF; false otherwise.</returns>
		public virtual bool IsMissingNewlineAtEnd()
		{
			int end = lines.Get(lines.Size() - 1);
			if (end == 0)
			{
				return true;
			}
			return content[end - 1] != '\n';
		}

		/// <summary>Get the text for a single line.</summary>
		/// <remarks>Get the text for a single line.</remarks>
		/// <param name="i">
		/// index of the line to extract. Note this is 0-based, so line
		/// number 1 is actually index 0.
		/// </param>
		/// <returns>the text for the line, without a trailing LF.</returns>
		public virtual string GetString(int i)
		{
			return GetString(i, i + 1, true);
		}

		/// <summary>Get the text for a region of lines.</summary>
		/// <remarks>Get the text for a region of lines.</remarks>
		/// <param name="begin">
		/// index of the first line to extract. Note this is 0-based, so
		/// line number 1 is actually index 0.
		/// </param>
		/// <param name="end">index of one past the last line to extract.</param>
		/// <param name="dropLF">
		/// if true the trailing LF ('\n') of the last returned line is
		/// dropped, if present.
		/// </param>
		/// <returns>
		/// the text for lines
		/// <code>[begin, end)</code>
		/// .
		/// </returns>
		public virtual string GetString(int begin, int end, bool dropLF)
		{
			if (begin == end)
			{
				return string.Empty;
			}
			int s = GetStart(begin);
			int e = GetEnd(end - 1);
			if (dropLF && content[e - 1] == '\n')
			{
				e--;
			}
			return Decode(s, e);
		}

		/// <summary>Decode a region of the text into a String.</summary>
		/// <remarks>
		/// Decode a region of the text into a String.
		/// The default implementation of this method tries to guess the character
		/// set by considering UTF-8, the platform default, and falling back on
		/// ISO-8859-1 if neither of those can correctly decode the region given.
		/// </remarks>
		/// <param name="start">first byte of the content to decode.</param>
		/// <param name="end">one past the last byte of the content to decode.</param>
		/// <returns>
		/// the region
		/// <code>[start, end)</code>
		/// decoded as a String.
		/// </returns>
		protected internal virtual string Decode(int start, int end)
		{
			return RawParseUtils.Decode(content, start, end);
		}

		private int GetStart(int i)
		{
			return lines.Get(i + 1);
		}

		private int GetEnd(int i)
		{
			return lines.Get(i + 2);
		}

		/// <summary>
		/// Determine heuristically whether a byte array represents binary (as
		/// opposed to text) content.
		/// </summary>
		/// <remarks>
		/// Determine heuristically whether a byte array represents binary (as
		/// opposed to text) content.
		/// </remarks>
		/// <param name="raw">the raw file content.</param>
		/// <returns>true if raw is likely to be a binary file, false otherwise</returns>
		public static bool IsBinary(byte[] raw)
		{
			return IsBinary(raw, raw.Length);
		}

		/// <summary>
		/// Determine heuristically whether the bytes contained in a stream
		/// represents binary (as opposed to text) content.
		/// </summary>
		/// <remarks>
		/// Determine heuristically whether the bytes contained in a stream
		/// represents binary (as opposed to text) content.
		/// Note: Do not further use this stream after having called this method! The
		/// stream may not be fully read and will be left at an unknown position
		/// after consuming an unknown number of bytes. The caller is responsible for
		/// closing the stream.
		/// </remarks>
		/// <param name="raw">input stream containing the raw file content.</param>
		/// <returns>true if raw is likely to be a binary file, false otherwise</returns>
		/// <exception cref="System.IO.IOException">if input stream could not be read</exception>
		public static bool IsBinary(InputStream raw)
		{
			byte[] buffer = new byte[FIRST_FEW_BYTES];
			int cnt = 0;
			while (cnt < buffer.Length)
			{
				int n = raw.Read(buffer, cnt, buffer.Length - cnt);
				if (n == -1)
				{
					break;
				}
				cnt += n;
			}
			return IsBinary(buffer, cnt);
		}

		/// <summary>
		/// Determine heuristically whether a byte array represents binary (as
		/// opposed to text) content.
		/// </summary>
		/// <remarks>
		/// Determine heuristically whether a byte array represents binary (as
		/// opposed to text) content.
		/// </remarks>
		/// <param name="raw">the raw file content.</param>
		/// <param name="length">
		/// number of bytes in
		/// <code>raw</code>
		/// to evaluate. This should be
		/// <code>raw.length</code>
		/// unless
		/// <code>raw</code>
		/// was over-allocated by
		/// the caller.
		/// </param>
		/// <returns>true if raw is likely to be a binary file, false otherwise</returns>
		public static bool IsBinary(byte[] raw, int length)
		{
			// Same heuristic as C Git
			if (length > FIRST_FEW_BYTES)
			{
				length = FIRST_FEW_BYTES;
			}
			for (int ptr = 0; ptr < length; ptr++)
			{
				if (raw[ptr] == '\0')
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Get the line delimiter for the first line.</summary>
		/// <remarks>Get the line delimiter for the first line.</remarks>
		/// <since>2.0</since>
		/// <returns>the line delimiter or <code>null</code></returns>
		public virtual string GetLineDelimiter()
		{
			if (Size() == 0)
			{
				return null;
			}
			int e = GetEnd(0);
			if (content[e - 1] != '\n')
			{
				return null;
			}
			if (content.Length > 1 && content[e - 2] == '\r')
			{
				return "\r\n";
			}
			else
			{
				return "\n";
			}
		}
	}
}
