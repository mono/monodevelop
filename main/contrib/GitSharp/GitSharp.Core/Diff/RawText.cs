/*
 * Copyright (C) 2008, Johannes E. Schindelin <johannes.schindelin@gmx.de>
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.IO;
using GitSharp.Core.Util;

namespace GitSharp.Core.Diff
{


	/// <summary>
	/// A sequence supporting UNIX formatted text in byte[] format.
	/// <para />
	/// Elements of the sequence are the lines of the file, as delimited by the UNIX
	/// newline character ('\n'). The file content is treated as 8 bit binary text,
	/// with no assumptions or requirements on character encoding.
	/// <para />
	/// Note that the first line of the file is element 0, as defined by the Sequence
	/// interface API. Traditionally in a text editor a patch file the first line is
	/// line number 1. Callers may need to subtract 1 prior to invoking methods if
	/// they are converting from "line number" to "element index".
	/// </summary>
	public class RawText : Sequence
	{
		// The file content for this sequence.
		private readonly byte[] content;

        /// <summary>
        /// The content of the raw text as byte array.
        /// </summary>
        public byte[] Content // <--- [henon] added accessor to be able to reuse the data structure from the api.
	    {
	        get
	        {
	            return content;
	        }
	    }

		// Map of line number to starting position within content.
		private readonly IntList lines;

        /// <summary>
        /// Represents starting points of lines in Content. Note: the line indices are 1-based and 
        /// are mapped to 0-based positions in the Content byte array. As line indices are based on 1 the result of line 0 is undefined.
        /// </summary>
	    public IntList LineStartIndices // <--- [henon] added accessor to be able to reuse the data structure from the api.
	    {
	        get
	        {
	            return lines;
	        }
	    }

		// Hash code for each line, for fast equality elimination.
		private readonly IntList hashes;

		///	<summary>
		/// Create a new sequence from an existing content byte array.
		///	<para />
		///	The entire array (indexes 0 through length-1) is used as the content.
		///	</summary>
		///	<param name="input">
		///	the content array. The array is never modified, so passing
		///	through cached arrays is safe.
		/// </param>
		public RawText(byte[] input)
		{
			content = input;
			lines = RawParseUtils.lineMap(content, 0, content.Length);
			hashes = computeHashes();
		}

        /// <summary>
        /// Create a new sequence from a file.
        /// <para>The entire file contents are used.</para>
        /// </summary>
        /// <param name="file">the text file.</param>
	    public RawText(FileInfo file) : this(IO.ReadFully(file))
	    {}


		public int size()
		{
			// The line map is always 2 entries larger than the number of lines in
			// the file. Index 0 is padded out/unused. The last index is the total
			// length of the buffer, and acts as a sentinel.
			//
			return lines.size() - 2;
		}

		public bool equals(int thisIdx, Sequence other, int otherIdx)
		{
			return equals(this, thisIdx + 1, (RawText) other, otherIdx + 1);
		}

		private static bool equals(RawText a, int ai, RawText b, int bi)
		{
			if (a.hashes.get(ai) != b.hashes.get(bi))
				return false;

			int a_start = a.lines.get(ai);
			int b_start = b.lines.get(bi);
			int a_end = a.lines.get(ai + 1);
			int b_end = b.lines.get(bi + 1);

			if (a_end - a_start != b_end - b_start)
				return false;

			while (a_start < a_end) {
				if (a.content[a_start++] != b.content[b_start++])
					return false;
			}
			return true;
		}

		///	<summary>
		/// Write a specific line to the output stream, without its trailing LF.
		///	<para />
		///	The specified line is copied as-is, with no character encoding
		///	translation performed.
		///	<para />
		///	If the specified line ends with an LF ('\n'), the LF is <b>not</b>
		///	copied. It is up to the caller to write the LF, if desired, between
		///	output lines.
		///	</summary>
		///	<param name="out">
		///	Stream to copy the line data onto. </param>
		///	<param name="i">
		///	Index of the line to extract. Note this is 0-based, so line
		///	number 1 is actually index 0. </param>
		///	<exception cref="IOException">
		///	the stream write operation failed.
		/// </exception>
		public void writeLine(Stream @out, int i)
		{
			int start = lines.get(i + 1);
			int end = lines.get(i + 2);
			if (content[end - 1] == '\n')
			{
				end--;
			}
			@out.Write(content, start, end - start);
		}

		///	<summary>
		/// Determine if the file ends with a LF ('\n').
		///	</summary>
		///	<returns> true if the last line has an LF; false otherwise. </returns>
		public bool isMissingNewlineAtEnd()
		{
			int end = lines.get(lines.size() - 1);
			if (end == 0)
				return true;
			return content[end - 1] != '\n';
		}

		private IntList computeHashes()
		{
			var r = new IntList(lines.size());
			r.add(0);
			for (int lno = 1; lno < lines.size() - 1; lno++)
			{
				int ptr = lines.get(lno);
				int end = lines.get(lno + 1);
				r.add(HashLine(content, ptr, end));
			}
			r.add(0);
			return r;
		}

		///	<summary>
		/// Compute a hash code for a single line.
		///	</summary>
		///	<param name="raw">The raw file content. </param>
		///	<param name="ptr">
		///	First byte of the content line to hash. </param>
		///	<param name="end">
		/// 1 past the last byte of the content line.
		/// </param>
		///	<returns>
		/// Hash code for the region <code>[ptr, end)</code> of raw.
		/// </returns>
		private static int HashLine(byte[] raw, int ptr, int end)
		{
			int hash = 5381;
			for (; ptr < end; ptr++)
			{
				hash = (hash << 5) ^ (raw[ptr] & 0xff);
			}
			return hash;
		}
	}
}