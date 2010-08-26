/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using System;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	/// <summary>
	/// A prefix abbreviation of an {@link ObjectId}.
	/// 
	/// Sometimes Git produces abbreviated SHA-1 strings, using sufficient leading
	/// digits from the ObjectId name to still be unique within the repository the
	/// string was generated from. These ids are likely to be unique for a useful
	/// period of time, especially if they contain at least 6-10 hex digits.
	/// 
	/// This class converts the hex string into a binary form, to make it more
	/// efficient for matching against an object.
	/// </summary>
	[Serializable]
	public class AbbreviatedObjectId
	{
		/// Number of half-bytes used by this id.
		private readonly int _nibbles;

		readonly int _w1;
		readonly int _w2;
		readonly int _w3;
		readonly int _w4;
		readonly int _w5;

		/// <summary>
		/// Convert an AbbreviatedObjectId from hex characters (US-ASCII).
		/// </summary>
		/// <param name="buf">the US-ASCII buffer to read from.</param>
		/// <param name="offset">position to read the first character from.</param>
		/// <param name="end">
		/// one past the last position to read (<code>end-offset</code> is
		/// the Length of the string).
		/// </param>
		/// <returns>the converted object id.</returns>
		public static AbbreviatedObjectId FromString(byte[] buf, int offset, int end)
		{
            if (end - offset > Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException("Invalid id");
			}

			return FromHexString(buf, offset, end);
		}

		/// <summary>
		/// Convert an AbbreviatedObjectId from hex characters.
		/// </summary>
		/// <param name="str">
		/// the string to read from. Must be &lt;= 40 characters.
		/// </param>
		/// <returns>the converted object id.</returns>
		public static AbbreviatedObjectId FromString(string str)
		{
			if (str == null)
				throw new ArgumentNullException ("str");
            if (str.Length > Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException("Invalid id: " + str);
			}

			byte[] b = Constants.encodeASCII(str);
			return FromHexString(b, 0, b.Length);
		}

		public static int Mask(int nibbles, int word, int v)
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
			return (int)((uint)v >> s) << s; // [henon] unsigned int needed to get the effect of java's rightshift operator >>>
		}

		public AbbreviatedObjectId(int nibbles, int w1, int w2, int w3, int w4, int w5)
		{
			_nibbles = nibbles;
			_w1 = w1;
			_w2 = w2;
			_w3 = w3;
			_w4 = w4;
			_w5 = w5;
		}

		/// <summary>
		/// Number of hex digits appearing in this id
		/// </summary>
		public int Length
		{
			get { return _nibbles; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// true if this ObjectId is actually a complete id.
		/// </returns>
		public bool isComplete()
		{
            return Length == Constants.OBJECT_ID_STRING_LENGTH;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// Return a complete <see cref="ObjectId"/>; null if <see cref="isComplete"/> is false.
		/// </returns>
		public ObjectId ToObjectId()
		{
			return isComplete() ? new ObjectId(_w1, _w2, _w3, _w4, _w5) : null;
		}

		/// <summary>
		/// Compares this abbreviation to a full object id.
		/// </summary>
		/// <param name="other">the other object id.</param>
		/// <returns>
		/// Return &lt;0 if this abbreviation names an object that is less than
		/// <code>other</code>; 0 if this abbreviation exactly matches the
		/// first <see cref="Length"/> digits of <code>other.name()</code>;
		/// &gt;0 if this abbreviation names an object that is after
		/// <code>other</code>.
		/// </returns>
		public int prefixCompare(AnyObjectId other)
		{
			if (other == null)
			{
				throw new ArgumentNullException("other");
			}
			
			int cmp = NB.CompareUInt32(_w1, mask(1, other.W1));
			if (cmp != 0)
			{
				return cmp;
			}

			cmp = NB.CompareUInt32(_w2, mask(2, other.W2));
			if (cmp != 0)
			{
				return cmp;
			}

			cmp = NB.CompareUInt32(_w3, mask(3, other.W3));
			if (cmp != 0)
			{
				return cmp;
			}

			cmp = NB.CompareUInt32(_w4, mask(4, other.W4));
			if (cmp != 0)
			{
				return cmp;
			}

			return NB.CompareUInt32(_w5, mask(5, other.W5));
		}

		private int mask(int word, int v)
		{
			return Mask(_nibbles, word, v);
		}

		public override int GetHashCode()
		{
			return _w2;
		}

		public override bool Equals(object obj)
		{
			AbbreviatedObjectId b = (obj as AbbreviatedObjectId);
			if (b != null)
			{
				return _nibbles == b._nibbles && _w1 == b._w1 && _w2 == b._w2
						&& _w3 == b._w3 && _w4 == b._w4 && _w5 == b._w5;
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>string form of the abbreviation, in lower case hexadecimal.</returns>
		public string name()
		{
            var b = new char[Constants.OBJECT_ID_STRING_LENGTH];

			Hex.FillHexCharArray(b, 0, _w1);
			if (_nibbles <= 8)
			{
				return new string(b, 0, _nibbles);
			}

			Hex.FillHexCharArray(b, 8, _w2);
			if (_nibbles <= 16)
			{
				return new string(b, 0, _nibbles);
			}

			Hex.FillHexCharArray(b, 16, _w3);
			if (_nibbles <= 24)
			{
				return new string(b, 0, _nibbles);
			}

			Hex.FillHexCharArray(b, 24, _w4);
			if (_nibbles <= 32)
			{
				return new string(b, 0, _nibbles);
			}

			Hex.FillHexCharArray(b, 32, _w5);
			return new string(b, 0, _nibbles);
		}

		public override string ToString()
		{
			return "AbbreviatedObjectId[" + name() + "]";
		}

		private static AbbreviatedObjectId FromHexString(byte[] bs, int ptr, int end)
		{
			try
			{
				int a = Hex.HexUInt32(bs, ptr, end);
				int b = Hex.HexUInt32(bs, ptr + 8, end);
				int c = Hex.HexUInt32(bs, ptr + 16, end);
				int d = Hex.HexUInt32(bs, ptr + 24, end);
				int e = Hex.HexUInt32(bs, ptr + 32, end);
				return new AbbreviatedObjectId(end - ptr, a, b, c, d, e);
			}
			catch (IndexOutOfRangeException e)
			{
                throw new InvalidObjectIdException(bs, ptr, end - ptr, e);
			}
		}
	}
}