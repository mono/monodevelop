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
	/// <summary>A SHA-1 abstraction.</summary>
	/// <remarks>A SHA-1 abstraction.</remarks>
	[System.Serializable]
	public class ObjectId : AnyObjectId
	{
		private const long serialVersionUID = 1L;

		private static readonly NGit.ObjectId ZEROID;

		private static readonly string ZEROID_STR;

		static ObjectId()
		{
			ZEROID = new NGit.ObjectId(0, 0, 0, 0, 0);
			ZEROID_STR = ZEROID.Name;
		}

		/// <summary>Get the special all-null ObjectId.</summary>
		/// <remarks>Get the special all-null ObjectId.</remarks>
		/// <returns>the all-null ObjectId, often used to stand-in for no object.</returns>
		public static NGit.ObjectId ZeroId
		{
			get
			{
				return ZEROID;
			}
		}

		/// <summary>Test a string of characters to verify it is a hex format.</summary>
		/// <remarks>
		/// Test a string of characters to verify it is a hex format.
		/// <p>
		/// If true the string can be parsed with
		/// <see cref="FromString(string)">FromString(string)</see>
		/// .
		/// </remarks>
		/// <param name="id">the string to test.</param>
		/// <returns>true if the string can converted into an ObjectId.</returns>
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
					RawParseUtils.ParseHexInt4(unchecked((byte)id[i]));
				}
				return true;
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}

		/// <summary>Convert an ObjectId into a hex string representation.</summary>
		/// <remarks>Convert an ObjectId into a hex string representation.</remarks>
		/// <param name="i">the id to convert. May be null.</param>
		/// <returns>the hex string conversion of this id's content.</returns>
		public static string ToString(NGit.ObjectId i)
		{
			return i != null ? i.Name : ZEROID_STR;
		}

		/// <summary>Compare to object identifier byte sequences for equality.</summary>
		/// <remarks>Compare to object identifier byte sequences for equality.</remarks>
		/// <param name="firstBuffer">
		/// the first buffer to compare against. Must have at least 20
		/// bytes from position ai through the end of the buffer.
		/// </param>
		/// <param name="fi">first offset within firstBuffer to begin testing.</param>
		/// <param name="secondBuffer">
		/// the second buffer to compare against. Must have at least 2
		/// bytes from position bi through the end of the buffer.
		/// </param>
		/// <param name="si">first offset within secondBuffer to begin testing.</param>
		/// <returns>true if the two identifiers are the same.</returns>
		public static bool Equals(byte[] firstBuffer, int fi, byte[] secondBuffer, int si
			)
		{
			return firstBuffer[fi] == secondBuffer[si] && firstBuffer[fi + 1] == secondBuffer
				[si + 1] && firstBuffer[fi + 2] == secondBuffer[si + 2] && firstBuffer[fi + 3] ==
				 secondBuffer[si + 3] && firstBuffer[fi + 4] == secondBuffer[si + 4] && firstBuffer
				[fi + 5] == secondBuffer[si + 5] && firstBuffer[fi + 6] == secondBuffer[si + 6] 
				&& firstBuffer[fi + 7] == secondBuffer[si + 7] && firstBuffer[fi + 8] == secondBuffer
				[si + 8] && firstBuffer[fi + 9] == secondBuffer[si + 9] && firstBuffer[fi + 10] 
				== secondBuffer[si + 10] && firstBuffer[fi + 11] == secondBuffer[si + 11] && firstBuffer
				[fi + 12] == secondBuffer[si + 12] && firstBuffer[fi + 13] == secondBuffer[si + 
				13] && firstBuffer[fi + 14] == secondBuffer[si + 14] && firstBuffer[fi + 15] == 
				secondBuffer[si + 15] && firstBuffer[fi + 16] == secondBuffer[si + 16] && firstBuffer
				[fi + 17] == secondBuffer[si + 17] && firstBuffer[fi + 18] == secondBuffer[si + 
				18] && firstBuffer[fi + 19] == secondBuffer[si + 19];
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="bs">
		/// the raw byte buffer to read from. At least 20 bytes must be
		/// available within this byte array.
		/// </param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromRaw(byte[] bs)
		{
			return FromRaw(bs, 0);
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="bs">
		/// the raw byte buffer to read from. At least 20 bytes after p
		/// must be available within this byte array.
		/// </param>
		/// <param name="p">position to read the first byte of data from.</param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromRaw(byte[] bs, int p)
		{
			int a = NB.DecodeInt32(bs, p);
			int b = NB.DecodeInt32(bs, p + 4);
			int c = NB.DecodeInt32(bs, p + 8);
			int d = NB.DecodeInt32(bs, p + 12);
			int e = NB.DecodeInt32(bs, p + 16);
			return new NGit.ObjectId(a, b, c, d, e);
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="is">
		/// the raw integers buffer to read from. At least 5 integers must
		/// be available within this int array.
		/// </param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromRaw(int[] @is)
		{
			return FromRaw(@is, 0);
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="is">
		/// the raw integers buffer to read from. At least 5 integers
		/// after p must be available within this int array.
		/// </param>
		/// <param name="p">position to read the first integer of data from.</param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromRaw(int[] @is, int p)
		{
			return new NGit.ObjectId(@is[p], @is[p + 1], @is[p + 2], @is[p + 3], @is[p + 4]);
		}

		/// <summary>Convert an ObjectId from hex characters (US-ASCII).</summary>
		/// <remarks>Convert an ObjectId from hex characters (US-ASCII).</remarks>
		/// <param name="buf">
		/// the US-ASCII buffer to read from. At least 40 bytes after
		/// offset must be available within this byte array.
		/// </param>
		/// <param name="offset">position to read the first character from.</param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromString(byte[] buf, int offset)
		{
			return FromHexString(buf, offset);
		}

		/// <summary>Convert an ObjectId from hex characters.</summary>
		/// <remarks>Convert an ObjectId from hex characters.</remarks>
		/// <param name="str">the string to read from. Must be 40 characters long.</param>
		/// <returns>the converted object id.</returns>
		public static NGit.ObjectId FromString(string str)
		{
			if (str.Length != Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException("Invalid id: " + str);
			}
			return FromHexString(Constants.EncodeASCII(str), 0);
		}

		private static NGit.ObjectId FromHexString(byte[] bs, int p)
		{
			try
			{
				int a = RawParseUtils.ParseHexInt32(bs, p);
				int b = RawParseUtils.ParseHexInt32(bs, p + 8);
				int c = RawParseUtils.ParseHexInt32(bs, p + 16);
				int d = RawParseUtils.ParseHexInt32(bs, p + 24);
				int e = RawParseUtils.ParseHexInt32(bs, p + 32);
				return new NGit.ObjectId(a, b, c, d, e);
			}
			catch (IndexOutOfRangeException)
			{
				throw new InvalidObjectIdException(bs, p, Constants.OBJECT_ID_STRING_LENGTH);
			}
		}

		internal ObjectId(int new_1, int new_2, int new_3, int new_4, int new_5)
		{
			w1 = new_1;
			w2 = new_2;
			w3 = new_3;
			w4 = new_4;
			w5 = new_5;
		}

		/// <summary>Initialize this instance by copying another existing ObjectId.</summary>
		/// <remarks>
		/// Initialize this instance by copying another existing ObjectId.
		/// <p>
		/// This constructor is mostly useful for subclasses who want to extend an
		/// ObjectId with more properties, but initialize from an existing ObjectId
		/// instance acquired by other means.
		/// </remarks>
		/// <param name="src">another already parsed ObjectId to copy the value out of.</param>
		protected internal ObjectId(AnyObjectId src)
		{
			w1 = src.w1;
			w2 = src.w2;
			w3 = src.w3;
			w4 = src.w4;
			w5 = src.w5;
		}

		public override NGit.ObjectId ToObjectId()
		{
			return this;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteObject(ObjectOutputStream os)
		{
			os.WriteInt(w1);
			os.WriteInt(w2);
			os.WriteInt(w3);
			os.WriteInt(w4);
			os.WriteInt(w5);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadObject(ObjectInputStream ois)
		{
			w1 = ois.ReadInt();
			w2 = ois.ReadInt();
			w3 = ois.ReadInt();
			w4 = ois.ReadInt();
			w5 = ois.ReadInt();
		}
	}
}
