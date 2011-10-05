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

using System;
using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Handy utility functions to parse raw object contents.</summary>
	/// <remarks>Handy utility functions to parse raw object contents.</remarks>
	public sealed class RawParseUtils
	{
		private static readonly byte[] digits10;

		private static readonly sbyte[] digits16;

		private static readonly byte[] footerLineKeyChars;

		private static readonly IDictionary<string, System.Text.Encoding> encodingAliases;

		static RawParseUtils()
		{
			encodingAliases = new Dictionary<string, System.Text.Encoding>();
			encodingAliases.Put("latin-1", Sharpen.Extensions.GetEncoding("ISO-8859-1"));
			digits10 = new byte[(byte)('9') + 1];
			Arrays.Fill(digits10, unchecked((byte)-1));
			for (char i = '0'; i <= '9'; i++)
			{
				digits10[i] = unchecked((byte)(i - (byte)('0')));
			}
			digits16 = new sbyte[(byte)('f') + 1];
			Arrays.Fill(digits16, (sbyte)-1);
			for (char i_1 = '0'; i_1 <= '9'; i_1++)
			{
				digits16[i_1] = (sbyte)(i_1 - (sbyte)('0'));
			}
			for (char i_2 = 'a'; i_2 <= 'f'; i_2++)
			{
				digits16[i_2] = (sbyte)((i_2 - (sbyte)('a')) + 10);
			}
			for (char i_3 = 'A'; i_3 <= 'F'; i_3++)
			{
				digits16[i_3] = (sbyte)((i_3 - (sbyte)('A')) + 10);
			}
			footerLineKeyChars = new byte[(byte)('z') + 1];
			footerLineKeyChars[(byte)('-')] = 1;
			for (char i_4 = '0'; i_4 <= '9'; i_4++)
			{
				footerLineKeyChars[i_4] = 1;
			}
			for (char i_5 = 'A'; i_5 <= 'Z'; i_5++)
			{
				footerLineKeyChars[i_5] = 1;
			}
			for (char i_6 = 'a'; i_6 <= 'z'; i_6++)
			{
				footerLineKeyChars[i_6] = 1;
			}
		}

		/// <summary>Determine if b[ptr] matches src.</summary>
		/// <remarks>Determine if b[ptr] matches src.</remarks>
		/// <param name="b">the buffer to scan.</param>
		/// <param name="ptr">first position within b, this should match src[0].</param>
		/// <param name="src">the buffer to test for equality with b.</param>
		/// <returns>ptr + src.length if b[ptr..src.length] == src; else -1.</returns>
		public static int Match(byte[] b, int ptr, byte[] src)
		{
			if (ptr + src.Length > b.Length)
			{
				return -1;
			}
			for (int i = 0; i < src.Length; i++, ptr++)
			{
				if (b[ptr] != src[i])
				{
					return -1;
				}
			}
			return ptr;
		}

		private static readonly byte[] base10byte = new byte[] { (byte)('0'), (byte)('1')
			, (byte)('2'), (byte)('3'), (byte)('4'), (byte)('5'), (byte)('6'), (byte)('7'), 
			(byte)('8'), (byte)('9') };

		/// <summary>Format a base 10 numeric into a temporary buffer.</summary>
		/// <remarks>
		/// Format a base 10 numeric into a temporary buffer.
		/// <p>
		/// Formatting is performed backwards. The method starts at offset
		/// <code>o-1</code> and ends at <code>o-1-digits</code>, where
		/// <code>digits</code> is the number of positions necessary to store the
		/// base 10 value.
		/// <p>
		/// The argument and return values from this method make it easy to chain
		/// writing, for example:
		/// </p>
		/// <pre>
		/// final byte[] tmp = new byte[64];
		/// int ptr = tmp.length;
		/// tmp[--ptr] = '\n';
		/// ptr = RawParseUtils.formatBase10(tmp, ptr, 32);
		/// tmp[--ptr] = ' ';
		/// ptr = RawParseUtils.formatBase10(tmp, ptr, 18);
		/// tmp[--ptr] = 0;
		/// final String str = new String(tmp, ptr, tmp.length - ptr);
		/// </pre>
		/// </remarks>
		/// <param name="b">buffer to write into.</param>
		/// <param name="o">
		/// one offset past the location where writing will begin; writing
		/// proceeds towards lower index values.
		/// </param>
		/// <param name="value">the value to store.</param>
		/// <returns>
		/// the new offset value <code>o</code>. This is the position of
		/// the last byte written. Additional writing should start at one
		/// position earlier.
		/// </returns>
		public static int FormatBase10(byte[] b, int o, int value)
		{
			if (value == 0)
			{
				b[--o] = (byte)('0');
				return o;
			}
			bool isneg = value < 0;
			if (isneg)
			{
				value = -value;
			}
			while (value != 0)
			{
				b[--o] = base10byte[value % 10];
				value /= 10;
			}
			if (isneg)
			{
				b[--o] = (byte)('-');
			}
			return o;
		}

		/// <summary>Parse a base 10 numeric from a sequence of ASCII digits into an int.</summary>
		/// <remarks>
		/// Parse a base 10 numeric from a sequence of ASCII digits into an int.
		/// <p>
		/// Digit sequences can begin with an optional run of spaces before the
		/// sequence, and may start with a '+' or a '-' to indicate sign position.
		/// Any other characters will cause the method to stop and return the current
		/// result to the caller.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start parsing digits at.</param>
		/// <param name="ptrResult">
		/// optional location to return the new ptr value through. If null
		/// the ptr value will be discarded.
		/// </param>
		/// <returns>
		/// the value at this location; 0 if the location is not a valid
		/// numeric.
		/// </returns>
		public static int ParseBase10(byte[] b, int ptr, MutableInteger ptrResult)
		{
			int r = 0;
			int sign = 0;
			try
			{
				int sz = b.Length;
				while (ptr < sz && b[ptr] == ' ')
				{
					ptr++;
				}
				if (ptr >= sz)
				{
					return 0;
				}
				switch (b[ptr])
				{
					case (byte)('-'):
					{
						sign = -1;
						ptr++;
						break;
					}

					case (byte)('+'):
					{
						ptr++;
						break;
					}
				}
				while (ptr < sz)
				{
					byte v = digits10[b[ptr]];
					if (((sbyte)v) < 0)
					{
						break;
					}
					r = (r * 10) + v;
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
			}
			// Not a valid digit.
			if (ptrResult != null)
			{
				ptrResult.value = ptr;
			}
			return sign < 0 ? -r : r;
		}

		/// <summary>Parse a base 10 numeric from a sequence of ASCII digits into a long.</summary>
		/// <remarks>
		/// Parse a base 10 numeric from a sequence of ASCII digits into a long.
		/// <p>
		/// Digit sequences can begin with an optional run of spaces before the
		/// sequence, and may start with a '+' or a '-' to indicate sign position.
		/// Any other characters will cause the method to stop and return the current
		/// result to the caller.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start parsing digits at.</param>
		/// <param name="ptrResult">
		/// optional location to return the new ptr value through. If null
		/// the ptr value will be discarded.
		/// </param>
		/// <returns>
		/// the value at this location; 0 if the location is not a valid
		/// numeric.
		/// </returns>
		public static long ParseLongBase10(byte[] b, int ptr, MutableInteger ptrResult)
		{
			long r = 0;
			int sign = 0;
			try
			{
				int sz = b.Length;
				while (ptr < sz && b[ptr] == ' ')
				{
					ptr++;
				}
				if (ptr >= sz)
				{
					return 0;
				}
				switch (b[ptr])
				{
					case (byte)('-'):
					{
						sign = -1;
						ptr++;
						break;
					}

					case (byte)('+'):
					{
						ptr++;
						break;
					}
				}
				while (ptr < sz)
				{
					byte v = digits10[b[ptr]];
					if (((sbyte)v) < 0)
					{
						break;
					}
					r = (r * 10) + v;
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
			}
			// Not a valid digit.
			if (ptrResult != null)
			{
				ptrResult.value = ptr;
			}
			return sign < 0 ? -r : r;
		}

		/// <summary>Parse 4 character base 16 (hex) formatted string to unsigned integer.</summary>
		/// <remarks>
		/// Parse 4 character base 16 (hex) formatted string to unsigned integer.
		/// <p>
		/// The number is read in network byte order, that is, most significant
		/// nybble first.
		/// </remarks>
		/// <param name="bs">
		/// buffer to parse digits from; positions
		/// <code>[p, p+4)</code>
		/// will
		/// be parsed.
		/// </param>
		/// <param name="p">first position within the buffer to parse.</param>
		/// <returns>the integer value.</returns>
		/// <exception cref="System.IndexOutOfRangeException">if the string is not hex formatted.
		/// 	</exception>
		public static int ParseHexInt16(byte[] bs, int p)
		{
			int r = digits16[bs[p]] << 4;
			r |= digits16[bs[p + 1]];
			r <<= 4;
			r |= digits16[bs[p + 2]];
			r <<= 4;
			r |= digits16[bs[p + 3]];
			if (r < 0)
			{
				throw new IndexOutOfRangeException();
			}
			return r;
		}

		/// <summary>Parse 8 character base 16 (hex) formatted string to unsigned integer.</summary>
		/// <remarks>
		/// Parse 8 character base 16 (hex) formatted string to unsigned integer.
		/// <p>
		/// The number is read in network byte order, that is, most significant
		/// nybble first.
		/// </remarks>
		/// <param name="bs">
		/// buffer to parse digits from; positions
		/// <code>[p, p+8)</code>
		/// will
		/// be parsed.
		/// </param>
		/// <param name="p">first position within the buffer to parse.</param>
		/// <returns>the integer value.</returns>
		/// <exception cref="System.IndexOutOfRangeException">if the string is not hex formatted.
		/// 	</exception>
		public static int ParseHexInt32(byte[] bs, int p)
		{
			int r = digits16[bs[p]] << 4;
			r |= digits16[bs[p + 1]];
			r <<= 4;
			r |= digits16[bs[p + 2]];
			r <<= 4;
			r |= digits16[bs[p + 3]];
			r <<= 4;
			r |= digits16[bs[p + 4]];
			r <<= 4;
			r |= digits16[bs[p + 5]];
			r <<= 4;
			r |= digits16[bs[p + 6]];
			int last = digits16[bs[p + 7]];
			if (r < 0 || last < 0)
			{
				throw new IndexOutOfRangeException();
			}
			return (r << 4) | last;
		}

		/// <summary>Parse a single hex digit to its numeric value (0-15).</summary>
		/// <remarks>Parse a single hex digit to its numeric value (0-15).</remarks>
		/// <param name="digit">hex character to parse.</param>
		/// <returns>numeric value, in the range 0-15.</returns>
		/// <exception cref="System.IndexOutOfRangeException">if the input digit is not a valid hex digit.
		/// 	</exception>
		public static int ParseHexInt4(byte digit)
		{
			sbyte r = digits16[digit];
			if (r < 0)
			{
				throw new IndexOutOfRangeException();
			}
			return r;
		}

		/// <summary>Parse a Git style timezone string.</summary>
		/// <remarks>
		/// Parse a Git style timezone string.
		/// <p>
		/// The sequence "-0315" will be parsed as the numeric value -195, as the
		/// lower two positions count minutes, not 100ths of an hour.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start parsing digits at.</param>
		/// <returns>the timezone at this location, expressed in minutes.</returns>
		public static int ParseTimeZoneOffset(byte[] b, int ptr)
		{
			int v = ParseBase10(b, ptr, null);
			int tzMins = v % 100;
			int tzHours = v / 100;
			return tzHours * 60 + tzMins;
		}

		/// <summary>Locate the first position after a given character.</summary>
		/// <remarks>Locate the first position after a given character.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for chrA at.</param>
		/// <param name="chrA">character to find.</param>
		/// <returns>new position just after chrA.</returns>
		public static int Next(byte[] b, int ptr, char chrA)
		{
			int sz = b.Length;
			while (ptr < sz)
			{
				if (b[ptr++] == chrA)
				{
					return ptr;
				}
			}
			return ptr;
		}

		/// <summary>Locate the first position after the next LF.</summary>
		/// <remarks>
		/// Locate the first position after the next LF.
		/// <p>
		/// This method stops on the first '\n' it finds.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for LF at.</param>
		/// <returns>new position just after the first LF found.</returns>
		public static int NextLF(byte[] b, int ptr)
		{
			return Next(b, ptr, '\n');
		}

		/// <summary>Locate the first position after either the given character or LF.</summary>
		/// <remarks>
		/// Locate the first position after either the given character or LF.
		/// <p>
		/// This method stops on the first match it finds from either chrA or '\n'.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for chrA or LF at.</param>
		/// <param name="chrA">character to find.</param>
		/// <returns>new position just after the first chrA or LF to be found.</returns>
		public static int NextLF(byte[] b, int ptr, char chrA)
		{
			int sz = b.Length;
			while (ptr < sz)
			{
				byte c = b[ptr++];
				if (c == chrA || c == '\n')
				{
					return ptr;
				}
			}
			return ptr;
		}

		/// <summary>Locate the first position before a given character.</summary>
		/// <remarks>Locate the first position before a given character.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for chrA at.</param>
		/// <param name="chrA">character to find.</param>
		/// <returns>new position just before chrA, -1 for not found</returns>
		public static int Prev(byte[] b, int ptr, char chrA)
		{
			if (ptr == b.Length)
			{
				--ptr;
			}
			while (ptr >= 0)
			{
				if (b[ptr--] == chrA)
				{
					return ptr;
				}
			}
			return ptr;
		}

		/// <summary>Locate the first position before the previous LF.</summary>
		/// <remarks>
		/// Locate the first position before the previous LF.
		/// <p>
		/// This method stops on the first '\n' it finds.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for LF at.</param>
		/// <returns>new position just before the first LF found, -1 for not found</returns>
		public static int PrevLF(byte[] b, int ptr)
		{
			return Prev(b, ptr, '\n');
		}

		/// <summary>Locate the previous position before either the given character or LF.</summary>
		/// <remarks>
		/// Locate the previous position before either the given character or LF.
		/// <p>
		/// This method stops on the first match it finds from either chrA or '\n'.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">position within buffer to start looking for chrA or LF at.</param>
		/// <param name="chrA">character to find.</param>
		/// <returns>
		/// new position just before the first chrA or LF to be found, -1 for
		/// not found
		/// </returns>
		public static int PrevLF(byte[] b, int ptr, char chrA)
		{
			if (ptr == b.Length)
			{
				--ptr;
			}
			while (ptr >= 0)
			{
				byte c = b[ptr--];
				if (c == chrA || c == '\n')
				{
					return ptr;
				}
			}
			return ptr;
		}

		/// <summary>Index the region between <code>[ptr, end)</code> to find line starts.</summary>
		/// <remarks>
		/// Index the region between <code>[ptr, end)</code> to find line starts.
		/// <p>
		/// The returned list is 1 indexed. Index 0 contains
		/// <see cref="int.MinValue">int.MinValue</see>
		/// to pad the list out.
		/// <p>
		/// Using a 1 indexed list means that line numbers can be directly accessed
		/// from the list, so <code>list.get(1)</code> (aka get line 1) returns
		/// <code>ptr</code>.
		/// <p>
		/// The last element (index <code>map.size()-1</code>) always contains
		/// <code>end</code>.
		/// </remarks>
		/// <param name="buf">buffer to scan.</param>
		/// <param name="ptr">
		/// position within the buffer corresponding to the first byte of
		/// line 1.
		/// </param>
		/// <param name="end">1 past the end of the content within <code>buf</code>.</param>
		/// <returns>a line map indexing the start position of each line.</returns>
		public static IntList LineMap(byte[] buf, int ptr, int end)
		{
			// Experimentally derived from multiple source repositories
			// the average number of bytes/line is 36. Its a rough guess
			// to initially size our map close to the target.
			//
			IntList map = new IntList((end - ptr) / 36);
			map.FillTo(1, int.MinValue);
			for (; ptr < end; ptr = NextLF(buf, ptr))
			{
				map.Add(ptr);
			}
			map.Add(end);
			return map;
		}

		/// <summary>Locate the "author " header line data.</summary>
		/// <remarks>Locate the "author " header line data.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer and does not accidentally look at message body.
		/// </param>
		/// <returns>
		/// position just after the space in "author ", so the first
		/// character of the author's name. If no author header can be
		/// located -1 is returned.
		/// </returns>
		public static int Author(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46;
			}
			// skip the "tree ..." line.
			while (ptr < sz && b[ptr] == 'p')
			{
				ptr += 48;
			}
			// skip this parent.
			return Match(b, ptr, ObjectChecker.author);
		}

		/// <summary>Locate the "committer " header line data.</summary>
		/// <remarks>Locate the "committer " header line data.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer and does not accidentally look at message body.
		/// </param>
		/// <returns>
		/// position just after the space in "committer ", so the first
		/// character of the committer's name. If no committer header can be
		/// located -1 is returned.
		/// </returns>
		public static int Committer(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46;
			}
			// skip the "tree ..." line.
			while (ptr < sz && b[ptr] == 'p')
			{
				ptr += 48;
			}
			// skip this parent.
			if (ptr < sz && b[ptr] == 'a')
			{
				ptr = NextLF(b, ptr);
			}
			return Match(b, ptr, ObjectChecker.committer);
		}

		/// <summary>Locate the "tagger " header line data.</summary>
		/// <remarks>Locate the "tagger " header line data.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the tag
		/// buffer and does not accidentally look at message body.
		/// </param>
		/// <returns>
		/// position just after the space in "tagger ", so the first
		/// character of the tagger's name. If no tagger header can be
		/// located -1 is returned.
		/// </returns>
		public static int Tagger(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 48;
			}
			// skip the "object ..." line.
			while (ptr < sz)
			{
				if (b[ptr] == '\n')
				{
					return -1;
				}
				int m = Match(b, ptr, ObjectChecker.tagger);
				if (m >= 0)
				{
					return m;
				}
				ptr = NextLF(b, ptr);
			}
			return -1;
		}

		/// <summary>Locate the "encoding " header line.</summary>
		/// <remarks>Locate the "encoding " header line.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// buffer and does not accidentally look at the message body.
		/// </param>
		/// <returns>
		/// position just after the space in "encoding ", so the first
		/// character of the encoding's name. If no encoding header can be
		/// located -1 is returned (and UTF-8 should be assumed).
		/// </returns>
		public static int Encoding(byte[] b, int ptr)
		{
			int sz = b.Length;
			while (ptr < sz)
			{
				if (b[ptr] == '\n')
				{
					return -1;
				}
				if (b[ptr] == 'e')
				{
					break;
				}
				ptr = NextLF(b, ptr);
			}
			return Match(b, ptr, ObjectChecker.encoding);
		}

		/// <summary>Parse the "encoding " header into a character set reference.</summary>
		/// <remarks>
		/// Parse the "encoding " header into a character set reference.
		/// <p>
		/// Locates the "encoding " header (if present) by first calling
		/// <see cref="Encoding(byte[], int)">Encoding(byte[], int)</see>
		/// and then returns the proper character set
		/// to apply to this buffer to evaluate its contents as character data.
		/// <p>
		/// If no encoding header is present,
		/// <see cref="NGit.Constants.CHARSET">NGit.Constants.CHARSET</see>
		/// is assumed.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <returns>the Java character set representation. Never null.</returns>
		public static System.Text.Encoding ParseEncoding(byte[] b)
		{
			int enc = Encoding(b, 0);
			if (enc < 0)
			{
				return Constants.CHARSET;
			}
			int lf = NextLF(b, enc);
			string decoded = Decode(Constants.CHARSET, b, enc, lf - 1);
			try
			{
				return Sharpen.Extensions.GetEncoding(decoded);
			}
			catch (IllegalCharsetNameException badName)
			{
				System.Text.Encoding aliased = CharsetForAlias(decoded);
				if (aliased != null)
				{
					return aliased;
				}
				throw;
			}
			catch (UnsupportedCharsetException badName)
			{
				System.Text.Encoding aliased = CharsetForAlias(decoded);
				if (aliased != null)
				{
					return aliased;
				}
				throw;
			}
		}

		/// <summary>Parse a name string (e.g.</summary>
		/// <remarks>
		/// Parse a name string (e.g. author, committer, tagger) into a PersonIdent.
		/// <p>
		/// Leading spaces won't be trimmed from the string, i.e. will show up in the
		/// parsed name afterwards.
		/// </remarks>
		/// <param name="in">the string to parse a name from.</param>
		/// <returns>
		/// the parsed identity or null in case the identity could not be
		/// parsed.
		/// </returns>
		public static PersonIdent ParsePersonIdent(string @in)
		{
			return ParsePersonIdent(Constants.Encode(@in), 0);
		}

		/// <summary>Parse a name line (e.g.</summary>
		/// <remarks>
		/// Parse a name line (e.g. author, committer, tagger) into a PersonIdent.
		/// <p>
		/// When passing in a value for <code>nameB</code> callers should use the
		/// return value of
		/// <see cref="Author(byte[], int)">Author(byte[], int)</see>
		/// or
		/// <see cref="Committer(byte[], int)">Committer(byte[], int)</see>
		/// , as these methods provide the proper
		/// position within the buffer.
		/// </remarks>
		/// <param name="raw">the buffer to parse character data from.</param>
		/// <param name="nameB">
		/// first position of the identity information. This should be the
		/// first position after the space which delimits the header field
		/// name (e.g. "author" or "committer") from the rest of the
		/// identity line.
		/// </param>
		/// <returns>
		/// the parsed identity or null in case the identity could not be
		/// parsed.
		/// </returns>
		public static PersonIdent ParsePersonIdent(byte[] raw, int nameB)
		{
			System.Text.Encoding cs = ParseEncoding(raw);
			int emailB = NextLF(raw, nameB, '<');
			int emailE = NextLF(raw, emailB, '>');
			if (emailB >= raw.Length || raw[emailB] == '\n' || (emailE >= raw.Length - 1 && raw
				[emailE - 1] != '>'))
			{
				return null;
			}
			int nameEnd = emailB - 2 >= 0 && raw[emailB - 2] == ' ' ? emailB - 2 : emailB - 1;
			string name = Decode(cs, raw, nameB, nameEnd);
			string email = Decode(cs, raw, emailB, emailE - 1);
			// Start searching from end of line, as after first name-email pair,
			// another name-email pair may occur. We will ignore all kinds of
			// "junk" following the first email.
			//
			// We've to use (emailE - 1) for the case that raw[email] is LF,
			// otherwise we would run too far. "-2" is necessary to position
			// before the LF in case of LF termination resp. the penultimate
			// character if there is no trailing LF.
			int tzBegin = LastIndexOfTrim(raw, ' ', NextLF(raw, emailE - 1) - 2) + 1;
			if (tzBegin <= emailE)
			{
				// No time/zone, still valid
				return new PersonIdent(name, email, 0, 0);
			}
			int whenBegin = Math.Max(emailE, LastIndexOfTrim(raw, ' ', tzBegin - 1) + 1);
			if (whenBegin >= tzBegin - 1)
			{
				// No time/zone, still valid
				return new PersonIdent(name, email, 0, 0);
			}
			long when = ParseLongBase10(raw, whenBegin, null);
			int tz = ParseTimeZoneOffset(raw, tzBegin);
			return new PersonIdent(name, email, when * 1000L, tz);
		}

		/// <summary>Parse a name data (e.g.</summary>
		/// <remarks>
		/// Parse a name data (e.g. as within a reflog) into a PersonIdent.
		/// <p>
		/// When passing in a value for <code>nameB</code> callers should use the
		/// return value of
		/// <see cref="Author(byte[], int)">Author(byte[], int)</see>
		/// or
		/// <see cref="Committer(byte[], int)">Committer(byte[], int)</see>
		/// , as these methods provide the proper
		/// position within the buffer.
		/// </remarks>
		/// <param name="raw">the buffer to parse character data from.</param>
		/// <param name="nameB">
		/// first position of the identity information. This should be the
		/// first position after the space which delimits the header field
		/// name (e.g. "author" or "committer") from the rest of the
		/// identity line.
		/// </param>
		/// <returns>the parsed identity. Never null.</returns>
		public static PersonIdent ParsePersonIdentOnly(byte[] raw, int nameB)
		{
			int stop = NextLF(raw, nameB);
			int emailB = NextLF(raw, nameB, '<');
			int emailE = NextLF(raw, emailB, '>');
			string name;
			string email;
			if (emailE < stop)
			{
				email = Decode(raw, emailB, emailE - 1);
			}
			else
			{
				email = "invalid";
			}
			if (emailB < stop)
			{
				name = Decode(raw, nameB, emailB - 2);
			}
			else
			{
				name = Decode(raw, nameB, stop);
			}
			MutableInteger ptrout = new MutableInteger();
			long when;
			int tz;
			if (emailE < stop)
			{
				when = ParseLongBase10(raw, emailE + 1, ptrout);
				tz = ParseTimeZoneOffset(raw, ptrout.value);
			}
			else
			{
				when = 0;
				tz = 0;
			}
			return new PersonIdent(name, email, when * 1000L, tz);
		}

		/// <summary>Locate the end of a footer line key string.</summary>
		/// <remarks>
		/// Locate the end of a footer line key string.
		/// <p>
		/// If the region at
		/// <code>raw[ptr]</code>
		/// matches
		/// <code>^[A-Za-z0-9-]+:</code>
		/// (e.g.
		/// "Signed-off-by: A. U. Thor\n") then this method returns the position of
		/// the first ':'.
		/// <p>
		/// If the region at
		/// <code>raw[ptr]</code>
		/// does not match
		/// <code>^[A-Za-z0-9-]+:</code>
		/// then this method returns -1.
		/// </remarks>
		/// <param name="raw">buffer to scan.</param>
		/// <param name="ptr">first position within raw to consider as a footer line key.</param>
		/// <returns>
		/// position of the ':' which terminates the footer line key if this
		/// is otherwise a valid footer line key; otherwise -1.
		/// </returns>
		public static int EndOfFooterLineKey(byte[] raw, int ptr)
		{
			try
			{
				for (; ; )
				{
					byte c = raw[ptr];
					if (footerLineKeyChars[c] == 0)
					{
						if (c == ':')
						{
							return ptr;
						}
						return -1;
					}
					ptr++;
				}
			}
			catch (IndexOutOfRangeException)
			{
				return -1;
			}
		}

		/// <summary>Decode a buffer under UTF-8, if possible.</summary>
		/// <remarks>
		/// Decode a buffer under UTF-8, if possible.
		/// If the byte stream cannot be decoded that way, the platform default is tried
		/// and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </remarks>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <returns>
		/// a string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string Decode(byte[] buffer)
		{
			return Decode(buffer, 0, buffer.Length);
		}

		/// <summary>Decode a buffer under UTF-8, if possible.</summary>
		/// <remarks>
		/// Decode a buffer under UTF-8, if possible.
		/// If the byte stream cannot be decoded that way, the platform default is
		/// tried and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </remarks>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <param name="start">start position in buffer</param>
		/// <param name="end">
		/// one position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// a string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string Decode(byte[] buffer, int start, int end)
		{
			return Decode(Constants.CHARSET, buffer, start, end);
		}

		/// <summary>Decode a buffer under the specified character set if possible.</summary>
		/// <remarks>
		/// Decode a buffer under the specified character set if possible.
		/// If the byte stream cannot be decoded that way, the platform default is tried
		/// and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </remarks>
		/// <param name="cs">character set to use when decoding the buffer.</param>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <returns>
		/// a string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string Decode(System.Text.Encoding cs, byte[] buffer)
		{
			return Decode(cs, buffer, 0, buffer.Length);
		}

		/// <summary>Decode a region of the buffer under the specified character set if possible.
		/// 	</summary>
		/// <remarks>
		/// Decode a region of the buffer under the specified character set if possible.
		/// If the byte stream cannot be decoded that way, the platform default is tried
		/// and if that too fails, the fail-safe ISO-8859-1 encoding is tried.
		/// </remarks>
		/// <param name="cs">character set to use when decoding the buffer.</param>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <param name="start">first position within the buffer to take data from.</param>
		/// <param name="end">
		/// one position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// a string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		public static string Decode(System.Text.Encoding cs, byte[] buffer, int start, int
			 end)
		{
			try
			{
				return DecodeNoFallback(cs, buffer, start, end);
			}
			catch (CharacterCodingException)
			{
				// Fall back to an ISO-8859-1 style encoding. At least all of
				// the bytes will be present in the output.
				//
				return ExtractBinaryString(buffer, start, end);
			}
		}

		/// <summary>
		/// Decode a region of the buffer under the specified character set if
		/// possible.
		/// </summary>
		/// <remarks>
		/// Decode a region of the buffer under the specified character set if
		/// possible.
		/// If the byte stream cannot be decoded that way, the platform default is
		/// tried and if that too fails, an exception is thrown.
		/// </remarks>
		/// <param name="cs">character set to use when decoding the buffer.</param>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <param name="start">first position within the buffer to take data from.</param>
		/// <param name="end">
		/// one position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>
		/// a string representation of the range <code>[start,end)</code>,
		/// after decoding the region through the specified character set.
		/// </returns>
		/// <exception cref="Sharpen.CharacterCodingException">the input is not in any of the tested character sets.
		/// 	</exception>
		public static string DecodeNoFallback(System.Text.Encoding cs, byte[] buffer, int
			 start, int end)
		{
			ByteBuffer b = ByteBuffer.Wrap(buffer, start, end - start);
			b.Mark();
			// Try our built-in favorite. The assumption here is that
			// decoding will fail if the data is not actually encoded
			// using that encoder.
			//
			try
			{
				return Decode(b, Constants.CHARSET);
			}
			catch (CharacterCodingException)
			{
				b.Reset();
			}
			if (!cs.Equals(Constants.CHARSET))
			{
				// Try the suggested encoding, it might be right since it was
				// provided by the caller.
				//
				try
				{
					return Decode(b, cs);
				}
				catch (CharacterCodingException)
				{
					b.Reset();
				}
			}
			// Try the default character set. A small group of people
			// might actually use the same (or very similar) locale.
			//
			System.Text.Encoding defcs = System.Text.Encoding.Default;
			if (!defcs.Equals(cs) && !defcs.Equals(Constants.CHARSET))
			{
				try
				{
					return Decode(b, defcs);
				}
				catch (CharacterCodingException)
				{
					b.Reset();
				}
			}
			throw new CharacterCodingException();
		}

		/// <summary>Decode a region of the buffer under the ISO-8859-1 encoding.</summary>
		/// <remarks>
		/// Decode a region of the buffer under the ISO-8859-1 encoding.
		/// Each byte is treated as a single character in the 8859-1 character
		/// encoding, performing a raw binary-&gt;char conversion.
		/// </remarks>
		/// <param name="buffer">buffer to pull raw bytes from.</param>
		/// <param name="start">first position within the buffer to take data from.</param>
		/// <param name="end">
		/// one position past the last location within the buffer to take
		/// data from.
		/// </param>
		/// <returns>a string representation of the range <code>[start,end)</code>.</returns>
		public static string ExtractBinaryString(byte[] buffer, int start, int end)
		{
			StringBuilder r = new StringBuilder(end - start);
			for (int i = start; i < end; i++)
			{
				r.Append((char)(buffer[i] & unchecked((int)(0xff))));
			}
			return r.ToString();
		}

		/// <exception cref="Sharpen.CharacterCodingException"></exception>
		private static string Decode(ByteBuffer b, System.Text.Encoding charset)
		{
			CharsetDecoder d = charset.NewDecoder();
			d.OnMalformedInput(CodingErrorAction.REPORT);
			d.OnUnmappableCharacter(CodingErrorAction.REPORT);
			return d.Decode(b).ToString();
		}

		/// <summary>Locate the position of the commit message body.</summary>
		/// <remarks>Locate the position of the commit message body.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the
		/// commit buffer.
		/// </param>
		/// <returns>position of the user's message buffer.</returns>
		public static int CommitMessage(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 46;
			}
			// skip the "tree ..." line.
			while (ptr < sz && b[ptr] == 'p')
			{
				ptr += 48;
			}
			// skip this parent.
			// Skip any remaining header lines, ignoring what their actual
			// header line type is. This is identical to the logic for a tag.
			//
			return TagMessage(b, ptr);
		}

		/// <summary>Locate the position of the tag message body.</summary>
		/// <remarks>Locate the position of the tag message body.</remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="ptr">
		/// position in buffer to start the scan at. Most callers should
		/// pass 0 to ensure the scan starts from the beginning of the tag
		/// buffer.
		/// </param>
		/// <returns>position of the user's message buffer.</returns>
		public static int TagMessage(byte[] b, int ptr)
		{
			int sz = b.Length;
			if (ptr == 0)
			{
				ptr += 48;
			}
			// skip the "object ..." line.
			while (ptr < sz && b[ptr] != '\n')
			{
				ptr = NextLF(b, ptr);
			}
			if (ptr < sz && b[ptr] == '\n')
			{
				return ptr + 1;
			}
			return -1;
		}

		/// <summary>Locate the end of a paragraph.</summary>
		/// <remarks>
		/// Locate the end of a paragraph.
		/// <p>
		/// A paragraph is ended by two consecutive LF bytes.
		/// </remarks>
		/// <param name="b">buffer to scan.</param>
		/// <param name="start">
		/// position in buffer to start the scan at. Most callers will
		/// want to pass the first position of the commit message (as
		/// found by
		/// <see cref="CommitMessage(byte[], int)">CommitMessage(byte[], int)</see>
		/// .
		/// </param>
		/// <returns>
		/// position of the LF at the end of the paragraph;
		/// <code>b.length</code> if no paragraph end could be located.
		/// </returns>
		public static int EndOfParagraph(byte[] b, int start)
		{
			int ptr = start;
			int sz = b.Length;
			while (ptr < sz && b[ptr] != '\n')
			{
				ptr = NextLF(b, ptr);
			}
			while (0 < ptr && start < ptr && b[ptr - 1] == '\n')
			{
				ptr--;
			}
			return ptr;
		}

		private static int LastIndexOfTrim(byte[] raw, char ch, int pos)
		{
			while (pos >= 0 && raw[pos] == ' ')
			{
				pos--;
			}
			while (pos >= 0 && raw[pos] != ch)
			{
				pos--;
			}
			return pos;
		}

		private static System.Text.Encoding CharsetForAlias(string name)
		{
			return encodingAliases.Get(StringUtils.ToLowerCase(name));
		}

		public RawParseUtils()
		{
		}
		// Don't create instances of a static only utility.
	}
}
