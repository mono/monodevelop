/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using GitSharp.Core.Exceptions;

namespace GitSharp.Core
{
	/// <summary>
	/// Reverse index for forward pack index. Provides operations based on offset
	/// instead of object id. Such offset-based reverse lookups are performed in
	/// O(log n) time.
	/// </summary>
	/// <seealso cref="PackIndex"/>
	/// /// <seealso cref="PackFile"/>
	public class PackReverseIndex
	{
		// Index we were created from, and that has our ObjectId data.
		private readonly PackIndex _index;

		// (offset31, truly) Offsets accommodating in 31 bits.
		private readonly int[] _offsets32;

		// Offsets not accommodating in 31 bits.
		private readonly long[] _offsets64;

		// Position of the corresponding offsets32 in index.
		private readonly int[] _nth32;

		// Position of the corresponding offsets64 in index.
		private readonly int[] _nth64;

		/// <summary>
		/// Create reverse index from straight/forward pack index, by indexing all
		/// its entries.
		/// </summary>
		/// <param name="packIndex">
		/// Forward index - entries to (reverse) index.
		/// </param>
		public PackReverseIndex(PackIndex packIndex)
		{
			_index = packIndex;

			long cnt = _index.ObjectCount;
			long n64 = _index.Offset64Count;
			long n32 = cnt - n64;
			if (n32 > int.MaxValue || n64 > int.MaxValue || cnt > 0xffffffffL)
			{
				throw new ArgumentException("Huge indexes are not supported, yet");
			}

			_offsets32 = new int[(int)n32];
			_offsets64 = new long[(int)n64];
			_nth32 = new int[_offsets32.Length];
			_nth64 = new int[_offsets64.Length];

			int i32 = 0;
			int i64 = 0;

			foreach (PackIndex.MutableEntry me in _index)
			{
				long o = me.Offset;
				if (o < int.MaxValue)
				{
					_offsets32[i32++] = (int)o;
				}
				else
				{
					_offsets64[i64++] = o;
				}
			}

			Array.Sort(_offsets32);
			Array.Sort(_offsets64);

			int nth = 0;
			foreach (PackIndex.MutableEntry me in _index)
			{
				long o = me.Offset;
				if (o < int.MaxValue)
				{
					_nth32[Array.BinarySearch(_offsets32, (int)o)] = nth++;
				}
				else
				{
					_nth64[Array.BinarySearch(_offsets64, o)] = nth++;
				}
			}
		}

		/// <summary>
		/// Search for object id with the specified start offset in this pack
		/// (reverse) index.
		/// </summary>
		/// <param name="offset">start offset of object to find.</param>
		/// <returns>
		/// <see cref="ObjectId"/> for this offset, or null if no object was found.
		/// </returns>
		public ObjectId FindObject(long offset)
		{
			if (offset <= int.MaxValue)
			{
				int i32 = Array.BinarySearch(_offsets32, (int)offset);
				if (i32 < 0) return null;
				return _index.GetObjectId(_nth32[i32]);
			}

			int i64 = Array.BinarySearch(_offsets64, offset);
			if (i64 < 0) return null;
			return _index.GetObjectId(_nth64[i64]);
		}

		/// <summary>
		/// Search for the next offset to the specified offset in this pack (reverse)
		/// index.
		/// </summary>
		/// <param name="offset">
		/// start offset of previous object (must be valid-existing offset).
		/// </param>
		/// <param name="maxOffset">
		/// maximum offset in a pack (returned when there is no next offset).
		/// </param>
		/// <returns>
		/// offset of the next object in a pack or maxOffset if provided
		/// offset was the last one.
		/// </returns>
		/// <exception cref="CorruptObjectException">
		/// When there is no object with the provided offset.
		/// </exception>
		public long FindNextOffset(long offset, long maxOffset)
		{
			if (offset <= int.MaxValue)
			{
				int i32 = Array.BinarySearch(_offsets32, (int)offset);
				if (i32 < 0)
				{
					throw new CorruptObjectException("Can't find object in (reverse) pack index for the specified offset " + offset);
				}

				if (i32 + 1 == _offsets32.Length)
				{
					if (_offsets64.Length > 0)
					{
						return _offsets64[0];
					}
					return maxOffset;
				}
				return _offsets32[i32 + 1];
			}

			int i64 = Array.BinarySearch(_offsets64, offset);
			if (i64 < 0)
			{
				throw new CorruptObjectException("Can't find object in (reverse) pack index for the specified offset " + offset);
			}

			if (i64 + 1 == _offsets64.Length)
			{
				return maxOffset;
			}

			return _offsets64[i64 + 1];
		}
	}
}
