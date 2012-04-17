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
using NGit.Internal;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>A mutable SHA-1 abstraction.</summary>
	/// <remarks>A mutable SHA-1 abstraction.</remarks>
	public class MutableObjectId : AnyObjectId
	{
		/// <summary>Empty constructor.</summary>
		/// <remarks>Empty constructor. Initialize object with default (zeros) value.</remarks>
		public MutableObjectId() : base()
		{
		}

		/// <summary>Copying constructor.</summary>
		/// <remarks>Copying constructor.</remarks>
		/// <param name="src">original entry, to copy id from</param>
		internal MutableObjectId(NGit.MutableObjectId src)
		{
			FromObjectId(src);
		}

		/// <summary>Set any byte in the id.</summary>
		/// <remarks>Set any byte in the id.</remarks>
		/// <param name="index">
		/// index of the byte to set in the raw form of the ObjectId. Must
		/// be in range [0,
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// ).
		/// </param>
		/// <param name="value">
		/// the value of the specified byte at
		/// <code>index</code>
		/// . Values are
		/// unsigned and thus are in the range [0,255] rather than the
		/// signed byte range of [-128, 127].
		/// </param>
		/// <exception cref="System.IndexOutOfRangeException">
		/// <code>index</code>
		/// is less than 0, equal to
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// , or greater than
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// .
		/// </exception>
		public virtual void SetByte(int index, int value)
		{
			switch (index >> 2)
			{
				case 0:
				{
					w1 = Set(w1, index & 3, value);
					break;
				}

				case 1:
				{
					w2 = Set(w2, index & 3, value);
					break;
				}

				case 2:
				{
					w3 = Set(w3, index & 3, value);
					break;
				}

				case 3:
				{
					w4 = Set(w4, index & 3, value);
					break;
				}

				case 4:
				{
					w5 = Set(w5, index & 3, value);
					break;
				}

				default:
				{
					throw Sharpen.Extensions.CreateIndexOutOfRangeException(index);
				}
			}
		}

		private static int Set(int w, int index, int value)
		{
			value &= unchecked((int)(0xff));
			switch (index)
			{
				case 0:
				{
					return (w & unchecked((int)(0x00ffffff))) | (value << 24);
				}

				case 1:
				{
					return (w & unchecked((int)(0xff00ffff))) | (value << 16);
				}

				case 2:
				{
					return (w & unchecked((int)(0xffff00ff))) | (value << 8);
				}

				case 3:
				{
					return (w & unchecked((int)(0xffffff00))) | value;
				}

				default:
				{
					throw new IndexOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Make this id match
		/// <see cref="ObjectId.ZeroId()">ObjectId.ZeroId()</see>
		/// .
		/// </summary>
		public virtual void Clear()
		{
			w1 = 0;
			w2 = 0;
			w3 = 0;
			w4 = 0;
			w5 = 0;
		}

		/// <summary>Copy an ObjectId into this mutable buffer.</summary>
		/// <remarks>Copy an ObjectId into this mutable buffer.</remarks>
		/// <param name="src">the source id to copy from.</param>
		public virtual void FromObjectId(AnyObjectId src)
		{
			this.w1 = src.w1;
			this.w2 = src.w2;
			this.w3 = src.w3;
			this.w4 = src.w4;
			this.w5 = src.w5;
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="bs">
		/// the raw byte buffer to read from. At least 20 bytes must be
		/// available within this byte array.
		/// </param>
		public virtual void FromRaw(byte[] bs)
		{
			FromRaw(bs, 0);
		}

		/// <summary>Convert an ObjectId from raw binary representation.</summary>
		/// <remarks>Convert an ObjectId from raw binary representation.</remarks>
		/// <param name="bs">
		/// the raw byte buffer to read from. At least 20 bytes after p
		/// must be available within this byte array.
		/// </param>
		/// <param name="p">position to read the first byte of data from.</param>
		public virtual void FromRaw(byte[] bs, int p)
		{
			w1 = NB.DecodeInt32(bs, p);
			w2 = NB.DecodeInt32(bs, p + 4);
			w3 = NB.DecodeInt32(bs, p + 8);
			w4 = NB.DecodeInt32(bs, p + 12);
			w5 = NB.DecodeInt32(bs, p + 16);
		}

		/// <summary>Convert an ObjectId from binary representation expressed in integers.</summary>
		/// <remarks>Convert an ObjectId from binary representation expressed in integers.</remarks>
		/// <param name="ints">
		/// the raw int buffer to read from. At least 5 integers must be
		/// available within this integers array.
		/// </param>
		public virtual void FromRaw(int[] ints)
		{
			FromRaw(ints, 0);
		}

		/// <summary>Convert an ObjectId from binary representation expressed in integers.</summary>
		/// <remarks>Convert an ObjectId from binary representation expressed in integers.</remarks>
		/// <param name="ints">
		/// the raw int buffer to read from. At least 5 integers after p
		/// must be available within this integers array.
		/// </param>
		/// <param name="p">position to read the first integer of data from.</param>
		public virtual void FromRaw(int[] ints, int p)
		{
			w1 = ints[p];
			w2 = ints[p + 1];
			w3 = ints[p + 2];
			w4 = ints[p + 3];
			w5 = ints[p + 4];
		}

		/// <summary>Convert an ObjectId from hex characters (US-ASCII).</summary>
		/// <remarks>Convert an ObjectId from hex characters (US-ASCII).</remarks>
		/// <param name="buf">
		/// the US-ASCII buffer to read from. At least 40 bytes after
		/// offset must be available within this byte array.
		/// </param>
		/// <param name="offset">position to read the first character from.</param>
		public virtual void FromString(byte[] buf, int offset)
		{
			FromHexString(buf, offset);
		}

		/// <summary>Convert an ObjectId from hex characters.</summary>
		/// <remarks>Convert an ObjectId from hex characters.</remarks>
		/// <param name="str">the string to read from. Must be 40 characters long.</param>
		public virtual void FromString(string str)
		{
			if (str.Length != Constants.OBJECT_ID_STRING_LENGTH)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidId, str));
			}
			FromHexString(Constants.EncodeASCII(str), 0);
		}

		private void FromHexString(byte[] bs, int p)
		{
			try
			{
				w1 = RawParseUtils.ParseHexInt32(bs, p);
				w2 = RawParseUtils.ParseHexInt32(bs, p + 8);
				w3 = RawParseUtils.ParseHexInt32(bs, p + 16);
				w4 = RawParseUtils.ParseHexInt32(bs, p + 24);
				w5 = RawParseUtils.ParseHexInt32(bs, p + 32);
			}
			catch (IndexOutOfRangeException)
			{
				throw new InvalidObjectIdException(bs, p, Constants.OBJECT_ID_STRING_LENGTH);
			}
		}

		public override ObjectId ToObjectId()
		{
			return new ObjectId(this);
		}
	}
}
