/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
	public class ObjectId : AnyObjectId
	{
		private static readonly string ZeroIdString;

		static ObjectId()
		{
			ZeroId = new ObjectId(0, 0, 0, 0, 0);
			ZeroIdString = ZeroId.Name;
		}

		internal ObjectId(int w1, int w2, int w3, int w4, int w5)
			: base(w1, w2, w3, w4, w5)
		{
		}

		public ObjectId(AnyObjectId src)
			: base(src)
		{
		}

		public static ObjectId ZeroId { get; private set; }

		///	<summary>
		/// Test a string of characters to verify it is a hex format.
		///	<para />
		///	If true the string can be parsed with <seealso cref="FromString(string)"/>.
		///	</summary>
		///	<param name="id">the string to test.</param>
		///	<returns> true if the string can converted into an <see cref="ObjectId"/>.
		/// </returns>
		public static bool IsId(string id)
		{
            if (id.Length != Constants.OBJECT_ID_STRING_LENGTH)
			{
				return false;
			}

			try
			{
                for (int i = 0; i < Constants.OBJECT_ID_STRING_LENGTH; i++)
                {
                    RawParseUtils.parseHexInt4((byte)id[i]);
                }

				return true;
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}

		///	<summary>
		/// Convert an ObjectId into a hex string representation.
		///	</summary>
		///	<param name="i">The id to convert. May be null.</param>
		///	<returns>The hex string conversion of this id's content.</returns>
		public static string ToString(ObjectId i)
		{
			return i != null ? i.Name : ZeroIdString;
		}

		/// <summary>
		/// Compare to object identifier byte sequences for equality.
		/// </summary>
		/// <param name="firstBuffer">
		/// the first buffer to compare against. Must have at least 20
		/// bytes from position ai through the end of the buffer.
		/// </param>
		/// <param name="fi">
		/// first offset within firstBuffer to begin testing.
		/// </param>
		/// <param name="secondBuffer">
		/// the second buffer to compare against. Must have at least 2
		/// bytes from position bi through the end of the buffer.
		/// </param>
		/// <param name="si">
		/// first offset within secondBuffer to begin testing.
		/// </param>
		/// <returns>
		/// return true if the two identifiers are the same.
		/// </returns>
		public static bool Equals(byte[] firstBuffer, int fi, byte[] secondBuffer, int si)
		{
			return firstBuffer[fi] == secondBuffer[si]
				   && firstBuffer[fi + 1] == secondBuffer[si + 1]
				   && firstBuffer[fi + 2] == secondBuffer[si + 2]
				   && firstBuffer[fi + 3] == secondBuffer[si + 3]
				   && firstBuffer[fi + 4] == secondBuffer[si + 4]
				   && firstBuffer[fi + 5] == secondBuffer[si + 5]
				   && firstBuffer[fi + 6] == secondBuffer[si + 6]
				   && firstBuffer[fi + 7] == secondBuffer[si + 7]
				   && firstBuffer[fi + 8] == secondBuffer[si + 8]
				   && firstBuffer[fi + 9] == secondBuffer[si + 9]
				   && firstBuffer[fi + 10] == secondBuffer[si + 10]
				   && firstBuffer[fi + 11] == secondBuffer[si + 11]
				   && firstBuffer[fi + 12] == secondBuffer[si + 12]
				   && firstBuffer[fi + 13] == secondBuffer[si + 13]
				   && firstBuffer[fi + 14] == secondBuffer[si + 14]
				   && firstBuffer[fi + 15] == secondBuffer[si + 15]
				   && firstBuffer[fi + 16] == secondBuffer[si + 16]
				   && firstBuffer[fi + 17] == secondBuffer[si + 17]
				   && firstBuffer[fi + 18] == secondBuffer[si + 18]
				   && firstBuffer[fi + 19] == secondBuffer[si + 19];
		}

		///	<summary>
		/// Convert an ObjectId from raw binary representation.
		/// </summary>
		/// <param name="bs">
		/// The raw byte buffer to read from. At least 20 bytes after <paramref name="offset"/>
		/// must be available within this byte array.
		/// </param>
		///	<param name="offset">
		/// Position to read the first byte of data from.
		/// </param>
		///	<returns>The converted object id.</returns>
		public static ObjectId FromString(byte[] bs, int offset)
		{
			return FromHexString(bs, offset);
		}

		///	<summary>
		/// Convert an ObjectId from raw binary representation.
		/// </summary>
		/// <param name="str">
		/// The raw byte buffer to read from. At least 20 bytes must be
		/// available within this byte array.
		/// </param>
		/// <returns> the converted object id. </returns>
		public static ObjectId FromString(string str)
		{
            if (str.Length != Constants.OBJECT_ID_STRING_LENGTH)
			{
                throw new ArgumentException("Invalid id: " + str, "str");
			}
			return FromHexString(Constants.encodeASCII(str), 0);
		}

		public static ObjectId FromHexString(byte[] bs, int p)
		{
			try
			{
				int a = RawParseUtils.parseHexInt32(bs, p);
                int b = RawParseUtils.parseHexInt32(bs, p + 8);
                int c = RawParseUtils.parseHexInt32(bs, p + 16);
                int d = RawParseUtils.parseHexInt32(bs, p + 24);
                int e = RawParseUtils.parseHexInt32(bs, p + 32);
				return new ObjectId(a, b, c, d, e);
			}
			catch (IndexOutOfRangeException e)
			{
                throw new InvalidObjectIdException(bs, p, Constants.OBJECT_ID_STRING_LENGTH, e);
			}
		}

		public override ObjectId ToObjectId()
		{
			return this;
		}

		public static ObjectId FromRaw(byte[] buffer)
		{
			return FromRaw(buffer, 0);
		}

		public static ObjectId FromRaw(byte[] buffer, int offset)
		{
			int a = NB.DecodeInt32(buffer, offset);
			int b = NB.DecodeInt32(buffer, offset + 4);
			int c = NB.DecodeInt32(buffer, offset + 8);
			int d = NB.DecodeInt32(buffer, offset + 12);
			int e = NB.DecodeInt32(buffer, offset + 16);
			return new ObjectId(a, b, c, d, e);
		}

		public static ObjectId FromRaw(int[] intbuffer)
		{
			return FromRaw(intbuffer, 0);
		}

		public static ObjectId FromRaw(int[] intbuffer, int offset)
		{
			return new ObjectId(intbuffer[offset], intbuffer[offset + 1], intbuffer[offset + 2], intbuffer[offset + 3],
								intbuffer[offset + 4]);
		}
	}
}