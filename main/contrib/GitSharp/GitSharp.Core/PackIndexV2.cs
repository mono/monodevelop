/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core
{
	/// <summary>
	/// Support for the pack index v2 format.
	/// </summary>
	public class PackIndexV2 : PackIndex
	{
		private const long IS_O64 = 1L << 31;
		private const int FANOUT = 256;
		private static readonly int[] NoInts = { };
		private static readonly byte[] NoBytes = { };
		private readonly long[] _fanoutTable;

		/** 256 arrays of contiguous object names. */
		private readonly int[][] _names;

		/** 256 arrays of the 32 bit offset data, matching {@link #names}. */
		private readonly byte[][] _offset32;

		/** 256 arrays of the CRC-32 of objects, matching {@link #names}. */
		private readonly byte[][] _crc32;

		/** 64 bit offset table. */
		private readonly byte[] _offset64;

		public PackIndexV2(Stream fd)
		{
			var fanoutRaw = new byte[4 * FANOUT];
			IO.ReadFully(fd, fanoutRaw, 0, fanoutRaw.Length);
			_fanoutTable = new long[FANOUT];
			for (int k = 0; k < FANOUT; k++)
			{
				_fanoutTable[k] = NB.DecodeUInt32(fanoutRaw, k * 4);
			}
			ObjectCount = _fanoutTable[FANOUT - 1];

			_names = new int[FANOUT][];
			_offset32 = new byte[FANOUT][];
			_crc32 = new byte[FANOUT][];

			// object name table. The size we can permit per fan-out bucket
			// is limited to Java's 2 GB per byte array limitation. That is
			// no more than 107,374,182 objects per fan-out.
			//
			for (int k = 0; k < FANOUT; k++)
			{
				long bucketCnt;
				if (k == 0)
				{
					bucketCnt = _fanoutTable[k];
				}
				else
				{
					bucketCnt = _fanoutTable[k] - _fanoutTable[k - 1];
				}

				if (bucketCnt == 0)
				{
					_names[k] = NoInts;
					_offset32[k] = NoBytes;
					_crc32[k] = NoBytes;
					continue;
				}

                long nameLen = bucketCnt * Constants.OBJECT_ID_LENGTH;
				if (nameLen > int.MaxValue)
				{
					throw new IOException("Index file is too large");
				}

				var intNameLen = (int)nameLen;
				var raw = new byte[intNameLen];
				var bin = new int[intNameLen >> 2];
				IO.ReadFully(fd, raw, 0, raw.Length);
				for (int i = 0; i < bin.Length; i++)
				{
					bin[i] = NB.DecodeInt32(raw, i << 2);
				}

				_names[k] = bin;
				_offset32[k] = new byte[(int)(bucketCnt * 4)];
				_crc32[k] = new byte[(int)(bucketCnt * 4)];
			}

			// CRC32 table.
			for (int k = 0; k < FANOUT; k++)
			{
				IO.ReadFully(fd, _crc32[k], 0, _crc32[k].Length);
			}

			// 32 bit offset table. Any entries with the most significant bit
			// set require a 64 bit offset entry in another table.
			//
			int o64cnt = 0;
			for (int k = 0; k < FANOUT; k++)
			{
				byte[] ofs = _offset32[k];
				IO.ReadFully(fd, ofs, 0, ofs.Length);
				for (int p = 0; p < ofs.Length; p += 4)
				{
                    if (NB.ConvertUnsignedByteToSigned(ofs[p]) < 0)
					{
						o64cnt++;
					}
				}
			}

			// 64 bit offset table. Most objects should not require an entry.
			//
			if (o64cnt > 0)
			{
				_offset64 = new byte[o64cnt * 8];
				IO.ReadFully(fd, _offset64, 0, _offset64.Length);
			}
			else
			{
				_offset64 = NoBytes;
			}

			PackChecksum = new byte[20];
			IO.ReadFully(fd, PackChecksum, 0, PackChecksum.Length);
		}

		public override IEnumerator<MutableEntry> GetEnumerator()
		{
			return new EntriesEnumeratorV2(this);
		}

		public override long ObjectCount { get; internal set; }

		public override long Offset64Count
		{
			get  { return _offset64.Length / 8; }
		}

		public override ObjectId GetObjectId(long nthPosition)
		{
			int levelOne = Array.BinarySearch(_fanoutTable, nthPosition + 1);
			long lbase;
			if (levelOne >= 0)
			{
				// If we hit the bucket exactly the item is in the bucket, or
				// any bucket before it which has the same object count.
				//
				lbase = _fanoutTable[levelOne];
				while (levelOne > 0 && lbase == _fanoutTable[levelOne - 1])
				{
					levelOne--;
				}
			}
			else
			{
				// The item is in the bucket we would insert it into.
				//
				levelOne = -(levelOne + 1);
			}

			lbase = levelOne > 0 ? _fanoutTable[levelOne - 1] : 0;
			var p = (int)(nthPosition - lbase);
			int p4 = p << 2;
			return ObjectId.FromRaw(_names[levelOne], p4 + p); // p * 5
		}

		public override long FindOffset(AnyObjectId objId)
		{
			int levelOne = objId.GetFirstByte();
			int levelTwo = BinarySearchLevelTwo(objId, levelOne);
			if (levelTwo == -1)
			{
				return -1;
			}

			long p = NB.DecodeUInt32(_offset32[levelOne], levelTwo << 2);
			if ((p & IS_O64) != 0)
			{
				return NB.DecodeUInt64(_offset64, (8 * (int)(p & ~IS_O64)));
			}

			return p;
		}

		public override long FindCRC32(AnyObjectId objId)
		{
			int levelOne = objId.GetFirstByte();
			int levelTwo = BinarySearchLevelTwo(objId, levelOne);
			if (levelTwo == -1)
			{
				throw new MissingObjectException(objId.Copy(), ObjectType.Unknown);
			}

			return NB.DecodeUInt32(_crc32[levelOne], levelTwo << 2);
		}

		public override bool HasCRC32Support
		{
			get { return true; }
		}

		private int BinarySearchLevelTwo(AnyObjectId objId, int levelOne)
		{
			int[] data = _names[levelOne];
			var high = (int)((uint)(_offset32[levelOne].Length) >> 2);
			if (high == 0)
			{
				return -1;
			}

			int low = 0;
			do
			{
				var mid = (int)((uint)(low + high) >> 1);
				int mid4 = mid << 2;

				int cmp = objId.CompareTo(data, mid4 + mid);
				if (cmp < 0)
				{
					high = mid;
				}
				else if (cmp == 0)
				{
					return mid;
				}
				else
				{
					low = mid + 1;
				}

			} while (low < high);
			return -1;
		}

		#region Nested Types

        private class EntriesEnumeratorV2 : EntriesIterator
        {
            private readonly PackIndexV2 _index;
            private int _levelOne;
            private int _levelTwo;

            public EntriesEnumeratorV2(PackIndexV2 index)
                : base(index)
            {
                _index = index;
            }

            protected override MutableObjectId IdBufferBuilder(MutableObjectId idBuffer)
            {
                idBuffer.FromRaw(_index._names[_levelOne], _levelTwo - Constants.OBJECT_ID_LENGTH / 4);
                return idBuffer;
            }

            protected override MutableEntry InnerNext(MutableEntry entry)
            {
                for (; _levelOne < _index._names.Length; _levelOne++)
                {
                    if (_levelTwo < _index._names[_levelOne].Length)
                    {
                        int idx = _levelTwo / (Constants.OBJECT_ID_LENGTH / 4) * 4;
                        long offset = NB.DecodeUInt32(_index._offset32[_levelOne], idx);
                        if ((offset & IS_O64) != 0)
                        {
                            idx = (8 * (int)(offset & ~IS_O64));
                            offset = NB.DecodeUInt64(_index._offset64, idx);
                        }
                        entry.Offset = offset;

                        _levelTwo += Constants.OBJECT_ID_LENGTH / 4;
                        ReturnedNumber++;
                        return entry;
                    }
                    _levelTwo = 0;
                }

                throw new IndexOutOfRangeException();
            }
        }

		#endregion
	}
}