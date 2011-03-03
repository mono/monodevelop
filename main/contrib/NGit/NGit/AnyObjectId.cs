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
using System.IO;
using System.Text;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>A (possibly mutable) SHA-1 abstraction.</summary>
	/// <remarks>
	/// A (possibly mutable) SHA-1 abstraction.
	/// <p>
	/// If this is an instance of
	/// <see cref="MutableObjectId">MutableObjectId</see>
	/// the concept of equality
	/// with this instance can alter at any time, if this instance is modified to
	/// represent a different object name.
	/// </remarks>
	public abstract class AnyObjectId : IComparable
	{
		/// <summary>Compare to object identifier byte sequences for equality.</summary>
		/// <remarks>Compare to object identifier byte sequences for equality.</remarks>
		/// <param name="firstObjectId">the first identifier to compare. Must not be null.</param>
		/// <param name="secondObjectId">the second identifier to compare. Must not be null.</param>
		/// <returns>true if the two identifiers are the same.</returns>
		public static bool Equals(AnyObjectId firstObjectId, AnyObjectId secondObjectId)
		{
			if (firstObjectId == secondObjectId)
			{
				return true;
			}
			// We test word 2 first as odds are someone already used our
			// word 1 as a hash code, and applying that came up with these
			// two instances we are comparing for equality. Therefore the
			// first two words are very likely to be identical. We want to
			// break away from collisions as quickly as possible.
			//
			return firstObjectId.w2 == secondObjectId.w2 && firstObjectId.w3 == secondObjectId
				.w3 && firstObjectId.w4 == secondObjectId.w4 && firstObjectId.w5 == secondObjectId
				.w5 && firstObjectId.w1 == secondObjectId.w1;
		}

		internal int w1;

		internal int w2;

		internal int w3;

		internal int w4;

		internal int w5;

		/// <summary>Get the first 8 bits of the ObjectId.</summary>
		/// <remarks>
		/// Get the first 8 bits of the ObjectId.
		/// This is a faster version of
		/// <code>getByte(0)</code>
		/// .
		/// </remarks>
		/// <returns>
		/// a discriminator usable for a fan-out style map. Returned values
		/// are unsigned and thus are in the range [0,255] rather than the
		/// signed byte range of [-128, 127].
		/// </returns>
		public int FirstByte
		{
			get
			{
				return (int)(((uint)w1) >> 24);
			}
		}

		/// <summary>Get any byte from the ObjectId.</summary>
		/// <remarks>
		/// Get any byte from the ObjectId.
		/// Callers hard-coding
		/// <code>getByte(0)</code>
		/// should instead use the much faster
		/// special case variant
		/// <see cref="FirstByte()">FirstByte()</see>
		/// .
		/// </remarks>
		/// <param name="index">
		/// index of the byte to obtain from the raw form of the ObjectId.
		/// Must be in range [0,
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// ).
		/// </param>
		/// <returns>
		/// the value of the requested byte at
		/// <code>index</code>
		/// . Returned values
		/// are unsigned and thus are in the range [0,255] rather than the
		/// signed byte range of [-128, 127].
		/// </returns>
		/// <exception cref="System.IndexOutOfRangeException">
		/// <code>index</code>
		/// is less than 0, equal to
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// , or greater than
		/// <see cref="Constants.OBJECT_ID_LENGTH">Constants.OBJECT_ID_LENGTH</see>
		/// .
		/// </exception>
		public int GetByte(int index)
		{
			int w;
			switch (index >> 2)
			{
				case 0:
				{
					w = w1;
					break;
				}

				case 1:
				{
					w = w2;
					break;
				}

				case 2:
				{
					w = w3;
					break;
				}

				case 3:
				{
					w = w4;
					break;
				}

				case 4:
				{
					w = w5;
					break;
				}

				default:
				{
					throw Sharpen.Extensions.CreateIndexOutOfRangeException(index);
				}
			}
			return ((int)(((uint)w) >> (8 * (3 - (index & 3))))) & unchecked((int)(0xff));
		}

		/// <summary>Compare this ObjectId to another and obtain a sort ordering.</summary>
		/// <remarks>Compare this ObjectId to another and obtain a sort ordering.</remarks>
		/// <param name="other">the other id to compare to. Must not be null.</param>
		/// <returns>
		/// &lt; 0 if this id comes before other; 0 if this id is equal to
		/// other; &gt; 0 if this id comes after other.
		/// </returns>
		public int CompareTo(AnyObjectId other)
		{
			if (this == other)
			{
				return 0;
			}
			int cmp;
			cmp = NB.CompareUInt32(w1, other.w1);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, other.w2);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, other.w3);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, other.w4);
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, other.w5);
		}

		public int CompareTo(object other)
		{
			return CompareTo(((AnyObjectId)other));
		}

		/// <summary>Compare this ObjectId to a network-byte-order ObjectId.</summary>
		/// <remarks>Compare this ObjectId to a network-byte-order ObjectId.</remarks>
		/// <param name="bs">array containing the other ObjectId in network byte order.</param>
		/// <param name="p">
		/// position within
		/// <code>bs</code>
		/// to start the compare at. At least
		/// 20 bytes, starting at this position are required.
		/// </param>
		/// <returns>
		/// a negative integer, zero, or a positive integer as this object is
		/// less than, equal to, or greater than the specified object.
		/// </returns>
		public int CompareTo(byte[] bs, int p)
		{
			int cmp;
			cmp = NB.CompareUInt32(w1, NB.DecodeInt32(bs, p));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, NB.DecodeInt32(bs, p + 4));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, NB.DecodeInt32(bs, p + 8));
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, NB.DecodeInt32(bs, p + 12));
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, NB.DecodeInt32(bs, p + 16));
		}

		/// <summary>Compare this ObjectId to a network-byte-order ObjectId.</summary>
		/// <remarks>Compare this ObjectId to a network-byte-order ObjectId.</remarks>
		/// <param name="bs">array containing the other ObjectId in network byte order.</param>
		/// <param name="p">
		/// position within
		/// <code>bs</code>
		/// to start the compare at. At least 5
		/// integers, starting at this position are required.
		/// </param>
		/// <returns>
		/// a negative integer, zero, or a positive integer as this object is
		/// less than, equal to, or greater than the specified object.
		/// </returns>
		public int CompareTo(int[] bs, int p)
		{
			int cmp;
			cmp = NB.CompareUInt32(w1, bs[p]);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w2, bs[p + 1]);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w3, bs[p + 2]);
			if (cmp != 0)
			{
				return cmp;
			}
			cmp = NB.CompareUInt32(w4, bs[p + 3]);
			if (cmp != 0)
			{
				return cmp;
			}
			return NB.CompareUInt32(w5, bs[p + 4]);
		}

		/// <summary>Tests if this ObjectId starts with the given abbreviation.</summary>
		/// <remarks>Tests if this ObjectId starts with the given abbreviation.</remarks>
		/// <param name="abbr">the abbreviation.</param>
		/// <returns>true if this ObjectId begins with the abbreviation; else false.</returns>
		public virtual bool StartsWith(AbbreviatedObjectId abbr)
		{
			return abbr.PrefixCompare(this) == 0;
		}

		public sealed override int GetHashCode()
		{
			return w2;
		}

		/// <summary>Determine if this ObjectId has exactly the same value as another.</summary>
		/// <remarks>Determine if this ObjectId has exactly the same value as another.</remarks>
		/// <param name="other">the other id to compare to. May be null.</param>
		/// <returns>true only if both ObjectIds have identical bits.</returns>
		public bool Equals(AnyObjectId other)
		{
			return other != null ? Equals(this, other) : false;
		}

		public sealed override bool Equals(object o)
		{
			if (o is AnyObjectId)
			{
				return Equals((AnyObjectId)o);
			}
			else
			{
				return false;
			}
		}

		/// <summary>Copy this ObjectId to an output writer in raw binary.</summary>
		/// <remarks>Copy this ObjectId to an output writer in raw binary.</remarks>
		/// <param name="w">the buffer to copy to. Must be in big endian order.</param>
		public virtual void CopyRawTo(ByteBuffer w)
		{
			w.PutInt(w1);
			w.PutInt(w2);
			w.PutInt(w3);
			w.PutInt(w4);
			w.PutInt(w5);
		}

		/// <summary>Copy this ObjectId to a byte array.</summary>
		/// <remarks>Copy this ObjectId to a byte array.</remarks>
		/// <param name="b">the buffer to copy to.</param>
		/// <param name="o">the offset within b to write at.</param>
		public virtual void CopyRawTo(byte[] b, int o)
		{
			NB.EncodeInt32(b, o, w1);
			NB.EncodeInt32(b, o + 4, w2);
			NB.EncodeInt32(b, o + 8, w3);
			NB.EncodeInt32(b, o + 12, w4);
			NB.EncodeInt32(b, o + 16, w5);
		}

		/// <summary>Copy this ObjectId to an int array.</summary>
		/// <remarks>Copy this ObjectId to an int array.</remarks>
		/// <param name="b">the buffer to copy to.</param>
		/// <param name="o">the offset within b to write at.</param>
		public virtual void CopyRawTo(int[] b, int o)
		{
			b[o] = w1;
			b[o + 1] = w2;
			b[o + 2] = w3;
			b[o + 3] = w4;
			b[o + 4] = w5;
		}

		/// <summary>Copy this ObjectId to an output writer in raw binary.</summary>
		/// <remarks>Copy this ObjectId to an output writer in raw binary.</remarks>
		/// <param name="w">the stream to write to.</param>
		/// <exception cref="System.IO.IOException">the stream writing failed.</exception>
		public virtual void CopyRawTo(OutputStream w)
		{
			WriteRawInt(w, w1);
			WriteRawInt(w, w2);
			WriteRawInt(w, w3);
			WriteRawInt(w, w4);
			WriteRawInt(w, w5);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void WriteRawInt(OutputStream w, int v)
		{
			w.Write((int)(((uint)v) >> 24));
			w.Write((int)(((uint)v) >> 16));
			w.Write((int)(((uint)v) >> 8));
			w.Write(v);
		}

		/// <summary>Copy this ObjectId to an output writer in hex format.</summary>
		/// <remarks>Copy this ObjectId to an output writer in hex format.</remarks>
		/// <param name="w">the stream to copy to.</param>
		/// <exception cref="System.IO.IOException">the stream writing failed.</exception>
		public virtual void CopyTo(OutputStream w)
		{
			w.Write(ToHexByteArray());
		}

		/// <summary>Copy this ObjectId to a byte array in hex format.</summary>
		/// <remarks>Copy this ObjectId to a byte array in hex format.</remarks>
		/// <param name="b">the buffer to copy to.</param>
		/// <param name="o">the offset within b to write at.</param>
		public virtual void CopyTo(byte[] b, int o)
		{
			FormatHexByte(b, o + 0, w1);
			FormatHexByte(b, o + 8, w2);
			FormatHexByte(b, o + 16, w3);
			FormatHexByte(b, o + 24, w4);
			FormatHexByte(b, o + 32, w5);
		}

		/// <summary>Copy this ObjectId to a ByteBuffer in hex format.</summary>
		/// <remarks>Copy this ObjectId to a ByteBuffer in hex format.</remarks>
		/// <param name="b">the buffer to copy to.</param>
		public virtual void CopyTo(ByteBuffer b)
		{
			b.Put(ToHexByteArray());
		}

		private byte[] ToHexByteArray()
		{
			byte[] dst = new byte[Constants.OBJECT_ID_STRING_LENGTH];
			FormatHexByte(dst, 0, w1);
			FormatHexByte(dst, 8, w2);
			FormatHexByte(dst, 16, w3);
			FormatHexByte(dst, 24, w4);
			FormatHexByte(dst, 32, w5);
			return dst;
		}

		private static readonly byte[] hexbyte = new byte[] { (byte)('0'), (byte)('1'), (
			byte)('2'), (byte)('3'), (byte)('4'), (byte)('5'), (byte)('6'), (byte)('7'), (byte
			)('8'), (byte)('9'), (byte)('a'), (byte)('b'), (byte)('c'), (byte)('d'), (byte)(
			'e'), (byte)('f') };

		private static void FormatHexByte(byte[] dst, int p, int w)
		{
			int o = p + 7;
			while (o >= p && w != 0)
			{
				dst[o--] = hexbyte[w & unchecked((int)(0xf))];
				w = (int)(((uint)w) >> 4);
			}
			while (o >= p)
			{
				dst[o--] = (byte)('0');
			}
		}

		/// <summary>Copy this ObjectId to an output writer in hex format.</summary>
		/// <remarks>Copy this ObjectId to an output writer in hex format.</remarks>
		/// <param name="w">the stream to copy to.</param>
		/// <exception cref="System.IO.IOException">the stream writing failed.</exception>
		public virtual void CopyTo(TextWriter w)
		{
			w.Write(ToHexCharArray());
		}

		/// <summary>Copy this ObjectId to an output writer in hex format.</summary>
		/// <remarks>Copy this ObjectId to an output writer in hex format.</remarks>
		/// <param name="tmp">
		/// temporary char array to buffer construct into before writing.
		/// Must be at least large enough to hold 2 digits for each byte
		/// of object id (40 characters or larger).
		/// </param>
		/// <param name="w">the stream to copy to.</param>
		/// <exception cref="System.IO.IOException">the stream writing failed.</exception>
		public virtual void CopyTo(char[] tmp, TextWriter w)
		{
			ToHexCharArray(tmp);
			w.Write(tmp, 0, Constants.OBJECT_ID_STRING_LENGTH);
		}

		/// <summary>Copy this ObjectId to a StringBuilder in hex format.</summary>
		/// <remarks>Copy this ObjectId to a StringBuilder in hex format.</remarks>
		/// <param name="tmp">
		/// temporary char array to buffer construct into before writing.
		/// Must be at least large enough to hold 2 digits for each byte
		/// of object id (40 characters or larger).
		/// </param>
		/// <param name="w">the string to append onto.</param>
		public virtual void CopyTo(char[] tmp, StringBuilder w)
		{
			ToHexCharArray(tmp);
			w.Append(tmp, 0, Constants.OBJECT_ID_STRING_LENGTH);
		}

		private char[] ToHexCharArray()
		{
			char[] dst = new char[Constants.OBJECT_ID_STRING_LENGTH];
			ToHexCharArray(dst);
			return dst;
		}

		private void ToHexCharArray(char[] dst)
		{
			FormatHexChar(dst, 0, w1);
			FormatHexChar(dst, 8, w2);
			FormatHexChar(dst, 16, w3);
			FormatHexChar(dst, 24, w4);
			FormatHexChar(dst, 32, w5);
		}

		private static readonly char[] hexchar = new char[] { '0', '1', '2', '3', '4', '5'
			, '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

		internal static void FormatHexChar(char[] dst, int p, int w)
		{
			int o = p + 7;
			while (o >= p && w != 0)
			{
				dst[o--] = hexchar[w & unchecked((int)(0xf))];
				w = (int)(((uint)w) >> 4);
			}
			while (o >= p)
			{
				dst[o--] = '0';
			}
		}

		public override string ToString()
		{
			return "AnyObjectId[" + Name + "]";
		}

		/// <returns>string form of the SHA-1, in lower case hexadecimal.</returns>
		public string Name
		{
			get
			{
				return new string(ToHexCharArray());
			}
		}

		/// <returns>string form of the SHA-1, in lower case hexadecimal.</returns>
		internal string GetName()
		{
			return Name;
		}

		/// <summary>Return an abbreviation (prefix) of this object SHA-1.</summary>
		/// <remarks>
		/// Return an abbreviation (prefix) of this object SHA-1.
		/// This implementation does not guaranteeing uniqueness. Callers should
		/// instead use
		/// <see cref="ObjectReader.Abbreviate(AnyObjectId, int)">ObjectReader.Abbreviate(AnyObjectId, int)
		/// 	</see>
		/// to obtain a
		/// unique abbreviation within the scope of a particular object database.
		/// </remarks>
		/// <param name="len">length of the abbreviated string.</param>
		/// <returns>SHA-1 abbreviation.</returns>
		public virtual AbbreviatedObjectId Abbreviate(int len)
		{
			int a = AbbreviatedObjectId.Mask(len, 1, w1);
			int b = AbbreviatedObjectId.Mask(len, 2, w2);
			int c = AbbreviatedObjectId.Mask(len, 3, w3);
			int d = AbbreviatedObjectId.Mask(len, 4, w4);
			int e = AbbreviatedObjectId.Mask(len, 5, w5);
			return new AbbreviatedObjectId(len, a, b, c, d, e);
		}

		/// <summary>Obtain an immutable copy of this current object name value.</summary>
		/// <remarks>
		/// Obtain an immutable copy of this current object name value.
		/// <p>
		/// Only returns <code>this</code> if this instance is an unsubclassed
		/// instance of
		/// <see cref="ObjectId">ObjectId</see>
		/// ; otherwise a new instance is returned
		/// holding the same value.
		/// <p>
		/// This method is useful to shed any additional memory that may be tied to
		/// the subclass, yet retain the unique identity of the object id for future
		/// lookups within maps and repositories.
		/// </remarks>
		/// <returns>an immutable copy, using the smallest memory footprint possible.</returns>
		public ObjectId Copy()
		{
			if (GetType() == typeof(ObjectId))
			{
				return (ObjectId)this;
			}
			return new ObjectId(this);
		}

		/// <summary>Obtain an immutable copy of this current object name value.</summary>
		/// <remarks>
		/// Obtain an immutable copy of this current object name value.
		/// <p>
		/// See
		/// <see cref="Copy()">Copy()</see>
		/// if <code>this</code> is a possibly subclassed (but
		/// immutable) identity and the application needs a lightweight identity
		/// <i>only</i> reference.
		/// </remarks>
		/// <returns>
		/// an immutable copy. May be <code>this</code> if this is already
		/// an immutable instance.
		/// </returns>
		public abstract ObjectId ToObjectId();
	}
}
