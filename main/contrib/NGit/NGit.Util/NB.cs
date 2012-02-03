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

using Sharpen;

namespace NGit.Util
{
	/// <summary>Conversion utilities for network byte order handling.</summary>
	/// <remarks>Conversion utilities for network byte order handling.</remarks>
	public sealed class NB
	{
		/// <summary>Compare a 32 bit unsigned integer stored in a 32 bit signed integer.</summary>
		/// <remarks>
		/// Compare a 32 bit unsigned integer stored in a 32 bit signed integer.
		/// <p>
		/// This function performs an unsigned compare operation, even though Java
		/// does not natively support unsigned integer values. Negative numbers are
		/// treated as larger than positive ones.
		/// </remarks>
		/// <param name="a">the first value to compare.</param>
		/// <param name="b">the second value to compare.</param>
		/// <returns>&lt; 0 if a &lt; b; 0 if a == b; &gt; 0 if a &gt; b.</returns>
		public static int CompareUInt32(int a, int b)
		{
			int cmp = ((int)(((uint)a) >> 1)) - ((int)(((uint)b) >> 1));
			if (cmp != 0)
			{
				return cmp;
			}
			return (a & 1) - (b & 1);
		}

		/// <summary>Convert sequence of 2 bytes (network byte order) into unsigned value.</summary>
		/// <remarks>Convert sequence of 2 bytes (network byte order) into unsigned value.</remarks>
		/// <param name="intbuf">buffer to acquire the 2 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next byte after it (for a total of 2 bytes)
		/// will be read.
		/// </param>
		/// <returns>unsigned integer value that matches the 16 bits read.</returns>
		public static int DecodeUInt16(byte[] intbuf, int offset)
		{
			int r = (intbuf[offset] & unchecked((int)(0xff))) << 8;
			return r | (intbuf[offset + 1] & unchecked((int)(0xff)));
		}

		/// <summary>Convert sequence of 4 bytes (network byte order) into signed value.</summary>
		/// <remarks>Convert sequence of 4 bytes (network byte order) into signed value.</remarks>
		/// <param name="intbuf">buffer to acquire the 4 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 3 bytes after it (for a total of 4
		/// bytes) will be read.
		/// </param>
		/// <returns>signed integer value that matches the 32 bits read.</returns>
		public static int DecodeInt32(byte[] intbuf, int offset)
		{
			int r = intbuf[offset] << 8;
			r |= intbuf[offset + 1] & unchecked((int)(0xff));
			r <<= 8;
			r |= intbuf[offset + 2] & unchecked((int)(0xff));
			return (r << 8) | (intbuf[offset + 3] & unchecked((int)(0xff)));
		}

		/// <summary>Convert sequence of 4 bytes (network byte order) into unsigned value.</summary>
		/// <remarks>Convert sequence of 4 bytes (network byte order) into unsigned value.</remarks>
		/// <param name="intbuf">buffer to acquire the 4 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 3 bytes after it (for a total of 4
		/// bytes) will be read.
		/// </param>
		/// <returns>unsigned integer value that matches the 32 bits read.</returns>
		public static long DecodeUInt32(byte[] intbuf, int offset)
		{
			int low = (intbuf[offset + 1] & unchecked((int)(0xff))) << 8;
			low |= (intbuf[offset + 2] & unchecked((int)(0xff)));
			low <<= 8;
			low |= (intbuf[offset + 3] & unchecked((int)(0xff)));
			return ((long)(intbuf[offset] & unchecked((int)(0xff)))) << 24 | low;
		}

		/// <summary>Convert sequence of 8 bytes (network byte order) into unsigned value.</summary>
		/// <remarks>Convert sequence of 8 bytes (network byte order) into unsigned value.</remarks>
		/// <param name="intbuf">buffer to acquire the 8 bytes of data from.</param>
		/// <param name="offset">
		/// position within the buffer to begin reading from. This
		/// position and the next 7 bytes after it (for a total of 8
		/// bytes) will be read.
		/// </param>
		/// <returns>unsigned integer value that matches the 64 bits read.</returns>
		public static long DecodeUInt64(byte[] intbuf, int offset)
		{
			return (DecodeUInt32(intbuf, offset) << 32) | DecodeUInt32(intbuf, offset + 4);
		}

		/// <summary>Write a 16 bit integer as a sequence of 2 bytes (network byte order).</summary>
		/// <remarks>Write a 16 bit integer as a sequence of 2 bytes (network byte order).</remarks>
		/// <param name="intbuf">buffer to write the 2 bytes of data into.</param>
		/// <param name="offset">
		/// position within the buffer to begin writing to. This position
		/// and the next byte after it (for a total of 2 bytes) will be
		/// replaced.
		/// </param>
		/// <param name="v">the value to write.</param>
		public static void EncodeInt16(byte[] intbuf, int offset, int v)
		{
			intbuf[offset + 1] = unchecked((byte)v);
			v = (int)(((uint)v) >> 8);
			intbuf[offset] = unchecked((byte)v);
		}

		/// <summary>Write a 32 bit integer as a sequence of 4 bytes (network byte order).</summary>
		/// <remarks>Write a 32 bit integer as a sequence of 4 bytes (network byte order).</remarks>
		/// <param name="intbuf">buffer to write the 4 bytes of data into.</param>
		/// <param name="offset">
		/// position within the buffer to begin writing to. This position
		/// and the next 3 bytes after it (for a total of 4 bytes) will be
		/// replaced.
		/// </param>
		/// <param name="v">the value to write.</param>
		public static void EncodeInt32(byte[] intbuf, int offset, int v)
		{
			intbuf[offset + 3] = unchecked((byte)v);
			v = (int)(((uint)v) >> 8);
			intbuf[offset + 2] = unchecked((byte)v);
			v = (int)(((uint)v) >> 8);
			intbuf[offset + 1] = unchecked((byte)v);
			v = (int)(((uint)v) >> 8);
			intbuf[offset] = unchecked((byte)v);
		}

		/// <summary>Write a 64 bit integer as a sequence of 8 bytes (network byte order).</summary>
		/// <remarks>Write a 64 bit integer as a sequence of 8 bytes (network byte order).</remarks>
		/// <param name="intbuf">buffer to write the 48bytes of data into.</param>
		/// <param name="offset">
		/// position within the buffer to begin writing to. This position
		/// and the next 7 bytes after it (for a total of 8 bytes) will be
		/// replaced.
		/// </param>
		/// <param name="v">the value to write.</param>
		public static void EncodeInt64(byte[] intbuf, int offset, long v)
		{
			intbuf[offset + 7] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 6] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 5] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 4] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 3] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 2] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset + 1] = unchecked((byte)v);
			v = (long)(((ulong)v) >> 8);
			intbuf[offset] = unchecked((byte)v);
		}

		public NB()
		{
		}
		// Don't create instances of a static only utility.
	}
}
