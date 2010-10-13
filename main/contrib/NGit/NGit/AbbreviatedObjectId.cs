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
using NGit;
using NGit.Errors;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// A prefix abbreviation of an
	/// <see cref="ObjectId">ObjectId</see>
	/// .
	/// <p>
	/// Sometimes Git produces abbreviated SHA-1 strings, using sufficient leading
	/// digits from the ObjectId name to still be unique within the repository the
	/// string was generated from. These ids are likely to be unique for a useful
	/// period of time, especially if they contain at least 6-10 hex digits.
	/// <p>
	/// This class converts the hex string into a binary form, to make it more
	/// efficient for matching against an object.
	/// </summary>
	public sealed class AbbreviatedObjectId
	{
		/// <summary>Test a string of characters to verify it is a hex format.</summary>
		/// <remarks>
		/// Test a string of characters to verify it is a hex format.
		/// <p>
		/// If true the string can be parsed with
		/// <see cref="FromString(string)">FromString(string)</see>
		/// .
		/// </remarks>
		/// <param name="id">the string to test.</param>
		/// <returns>true if the string can converted into an AbbreviatedObjectId.</returns>
		public static bool IsId(string id)
		{
			if (id.Length < 2 || Constants.OBJECT_ID_STRING_LENGTH < id.Length)
			{
				return false;
			}
			try
			{
				for (int i = 0; i < id.Length; i++)
				{
					RawParseUtils.ParseHexInt4(unchecked((byte)id[i]));
				}
				return true;
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}

		/// <summary>Convert an AbbreviatedObjectId from hex characters (US-ASCII).</summary>
		/// <remarks>Convert an AbbreviatedObjectId from hex characters (US-ASCII).</remarks>
		/// <param name="buf">the US-ASCII buffer to read from.</param>
		/// <param name="offset">position to read the first character from.</param>
		/// <param name="end">
		/// one past the last position to read (<code>end-offset</code> is
		/// the length of the string).
		/// </param>
		/// <returns>the converted object id.</returns>
		public static NGit.AbbreviatedObjectId FromString(byte[] buf, int offset, int end
			)
		{
			if (end - offset > Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidIdLength, 
					end - offset, Constants.OBJECT_ID_STRING_LENGTH));
			}
			return FromHexString(buf, offset, end);
		}

		/// <summary>
		/// Convert an AbbreviatedObjectId from an
		/// <see cref="AnyObjectId">AnyObjectId</see>
		/// .
		/// <p>
		/// This method copies over all bits of the Id, and is therefore complete
		/// (see
		/// <see cref="IsComplete()">IsComplete()</see>
		/// ).
		/// </summary>
		/// <param name="id">
		/// the
		/// <see cref="ObjectId">ObjectId</see>
		/// to convert from.
		/// </param>
		/// <returns>the converted object id.</returns>
		public static NGit.AbbreviatedObjectId FromObjectId(AnyObjectId id)
		{
			return new NGit.AbbreviatedObjectId(Constants.OBJECT_ID_STRING_LENGTH, id.w1, id.
				w2, id.w3, id.w4, id.w5);
		}

		/// <summary>Convert an AbbreviatedObjectId from hex characters.</summary>
		/// <remarks>Convert an AbbreviatedObjectId from hex characters.</remarks>
		/// <param name="str">the string to read from. Must be &lt;= 40 characters.</param>
		/// <returns>the converted object id.</returns>
		public static NGit.AbbreviatedObjectId FromString(string str)
		{
			if (str.Length > Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidId, str));
			}
			byte[] b = Constants.EncodeASCII(str);
			return FromHexString(b, 0, b.Length);
		}

		private static NGit.AbbreviatedObjectId FromHexString(byte[] bs, int ptr, int end
			)
		{
			try
			{
				int a = HexUInt32(bs, ptr, end);
				int b = HexUInt32(bs, ptr + 8, end);
				int c = HexUInt32(bs, ptr + 16, end);
				int d = HexUInt32(bs, ptr + 24, end);
				int e = HexUInt32(bs, ptr + 32, end);
				return new NGit.AbbreviatedObjectId(end - ptr, a, b, c, d, e);
			}
			catch (IndexOutOfRangeException)
			{
				throw new InvalidObjectIdException(bs, ptr, end - ptr);
			}
		}

		private static int HexUInt32(byte[] bs, int p, int end)
		{
			if (8 <= end - p)
			{
				return RawParseUtils.ParseHexInt32(bs, p);
			}
			int r = 0;
			int n = 0;
			while (n < 8 && p < end)
			{
				r <<= 4;
				r |= RawParseUtils.ParseHexInt4(bs[p++]);
				n++;
			}
			return r << (8 - n) * 4;
		}

		internal static int Mask(int nibbles, int word, int v)
		{
			int b = (word - 1) * 8;
			if (b + 8 <= nibbles)
			{
				// We have all of the bits required for this word.
				//
				return v;
			}
			if (nibbles <= b)
			{
				// We have none of the bits required for this word.
				//
				return 0;
			}
			int s = 32 - (nibbles - b) * 4;
			return ((int)(((uint)v) >> s)) << s;
		}

		/// <summary>Number of half-bytes used by this id.</summary>
		/// <remarks>Number of half-bytes used by this id.</remarks>
		internal readonly int nibbles;

		internal readonly int w1;

		internal readonly int w2;

		internal readonly int w3;

		internal readonly int w4;

		internal readonly int w5;

		internal AbbreviatedObjectId(int n, int new_1, int new_2, int new_3, int new_4, int
			 new_5)
		{
			nibbles = n;
			w1 = new_1;
			w2 = new_2;
			w3 = new_3;
			w4 = new_4;
			w5 = new_5;
		}

		/// <returns>number of hex digits appearing in this id</returns>
		public int Length
		{
			get
			{
				return nibbles;
			}
		}

		/// <returns>true if this ObjectId is actually a complete id.</returns>
		public bool IsComplete
		{
			get
			{
				return Length == Constants.OBJECT_ID_STRING_LENGTH;
			}
		}

		/// <returns>
		/// a complete ObjectId; null if
		/// <see cref="IsComplete()">IsComplete()</see>
		/// is false
		/// </returns>
		public ObjectId ToObjectId()
		{
			return IsComplete ? new ObjectId(w1, w2, w3, w4, w5) : null;
		}

		/// <summary>Compares this abbreviation to a full object id.</summary>
		/// <remarks>Compares this abbreviation to a full object id.</remarks>
		/// <param name="other">the other object id.</param>
		/// <returns>
		/// &lt;0 if this abbreviation names an object that is less than
		/// <code>other</code>; 0 if this abbreviation exactly matches the
		/// first
		/// <see cref="Length()">Length()</see>
		/// digits of <code>other.name()</code>;
		/// &gt;0 if this abbreviation names an object that is after
		/// <code>other</code>.
		/// </returns>
		public int PrefixCompare(AnyObjectId other)
		{
			int cmp;
			cmp = NB.CompareUInt32(w1, Mask(1, other.w1));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, Mask(2, other.w2));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, Mask(3, other.w3));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, Mask(4, other.w4));
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, Mask(5, other.w5));
		}

		/// <summary>Compare this abbreviation to a network-byte-order ObjectId.</summary>
		/// <remarks>Compare this abbreviation to a network-byte-order ObjectId.</remarks>
		/// <param name="bs">array containing the other ObjectId in network byte order.</param>
		/// <param name="p">
		/// position within
		/// <code>bs</code>
		/// to start the compare at. At least
		/// 20 bytes, starting at this position are required.
		/// </param>
		/// <returns>
		/// &lt;0 if this abbreviation names an object that is less than
		/// <code>other</code>; 0 if this abbreviation exactly matches the
		/// first
		/// <see cref="Length()">Length()</see>
		/// digits of <code>other.name()</code>;
		/// &gt;0 if this abbreviation names an object that is after
		/// <code>other</code>.
		/// </returns>
		public int PrefixCompare(byte[] bs, int p)
		{
			int cmp;
			cmp = NB.CompareUInt32(w1, Mask(1, NB.DecodeInt32(bs, p)));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, Mask(2, NB.DecodeInt32(bs, p + 4)));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, Mask(3, NB.DecodeInt32(bs, p + 8)));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, Mask(4, NB.DecodeInt32(bs, p + 12)));
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, Mask(5, NB.DecodeInt32(bs, p + 16)));
		}

		/// <summary>Compare this abbreviation to a network-byte-order ObjectId.</summary>
		/// <remarks>Compare this abbreviation to a network-byte-order ObjectId.</remarks>
		/// <param name="bs">array containing the other ObjectId in network byte order.</param>
		/// <param name="p">
		/// position within
		/// <code>bs</code>
		/// to start the compare at. At least 5
		/// ints, starting at this position are required.
		/// </param>
		/// <returns>
		/// &lt;0 if this abbreviation names an object that is less than
		/// <code>other</code>; 0 if this abbreviation exactly matches the
		/// first
		/// <see cref="Length()">Length()</see>
		/// digits of <code>other.name()</code>;
		/// &gt;0 if this abbreviation names an object that is after
		/// <code>other</code>.
		/// </returns>
		public int PrefixCompare(int[] bs, int p)
		{
			int cmp;
			cmp = NB.CompareUInt32(w1, Mask(1, bs[p]));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, Mask(2, bs[p + 1]));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, Mask(3, bs[p + 2]));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, Mask(4, bs[p + 3]));
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, Mask(5, bs[p + 4]));
		}

		/// <returns>value for a fan-out style map, only valid of length &gt;= 2.</returns>
		public int FirstByte
		{
			get
			{
				return (int)(((uint)w1) >> 24);
			}
		}

		private int Mask(int word, int v)
		{
			return Mask(nibbles, word, v);
		}

		public override int GetHashCode()
		{
			return w2;
		}

		public override bool Equals(object o)
		{
			if (o is NGit.AbbreviatedObjectId)
			{
				NGit.AbbreviatedObjectId b = (NGit.AbbreviatedObjectId)o;
				return nibbles == b.nibbles && w1 == b.w1 && w2 == b.w2 && w3 == b.w3 && w4 == b.
					w4 && w5 == b.w5;
			}
			return false;
		}

		/// <returns>string form of the abbreviation, in lower case hexadecimal.</returns>
		public string Name
		{
			get
			{
				char[] b = new char[Constants.OBJECT_ID_STRING_LENGTH];
				AnyObjectId.FormatHexChar(b, 0, w1);
				if (nibbles <= 8)
				{
					return Sharpen.Extensions.CreateString(b, 0, nibbles);
				}
				AnyObjectId.FormatHexChar(b, 8, w2);
				if (nibbles <= 16)
				{
					return Sharpen.Extensions.CreateString(b, 0, nibbles);
				}
				AnyObjectId.FormatHexChar(b, 16, w3);
				if (nibbles <= 24)
				{
					return Sharpen.Extensions.CreateString(b, 0, nibbles);
				}
				AnyObjectId.FormatHexChar(b, 24, w4);
				if (nibbles <= 32)
				{
					return Sharpen.Extensions.CreateString(b, 0, nibbles);
				}
				AnyObjectId.FormatHexChar(b, 32, w5);
				return Sharpen.Extensions.CreateString(b, 0, nibbles);
			}
		}

		public override string ToString()
		{
			return "AbbreviatedObjectId[" + Name + "]";
		}
	}
}
