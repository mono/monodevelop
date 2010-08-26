/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

namespace GitSharp.Core
{
	[Complete]
	public class PackIndexV1 : PackIndex
	{
		private const int IdxHdrLen = 256 * 4;
		private readonly long[] _idxHeader;
		private readonly byte[][] _idxdata;

		public PackIndexV1(Stream fd, byte[] hdr)
		{
			byte[] fanoutTable = new byte[IdxHdrLen];
			Array.Copy(hdr, 0, fanoutTable, 0, hdr.Length);
			IO.ReadFully(fd, fanoutTable, hdr.Length, IdxHdrLen - hdr.Length);

			_idxHeader = new long[256];
			for (int k = 0; k < _idxHeader.Length; k++)
				_idxHeader[k] = NB.decodeUInt32(fanoutTable, k * 4);

			_idxdata = new byte[_idxHeader.Length][];
			for (int k = 0; k < _idxHeader.Length; k++)
			{
				uint n;
				if (k == 0)
					n = (uint)(_idxHeader[k]);
				else
					n = (uint)(_idxHeader[k] - _idxHeader[k - 1]);
				if (n > 0)
				{
					_idxdata[k] = new byte[n * (Constants.OBJECT_ID_LENGTH + 4)];
					IO.ReadFully(fd, _idxdata[k], 0, _idxdata[k].Length);
				}
			}

			ObjectCount = _idxHeader[255];
			PackChecksum = new byte[20];
			IO.ReadFully(fd, PackChecksum, 0, PackChecksum.Length);



			/*var fanoutTable = new byte[IDX_HDR_LEN];
			Array.Copy(hdr, 0, fanoutTable, 0, hdr.Length);
			NB.ReadFully(fd, fanoutTable, hdr.Length, IDX_HDR_LEN - hdr.Length);

			idxHeader = new long[256]; // really unsigned 32-bit...
			for (int k = 0; k < idxHeader.Length; k++)
				idxHeader[k] = NB.DecodeUInt32(fanoutTable, k * 4);
			idxdata = new byte[idxHeader.Length][];
			for (int k = 0; k < idxHeader.Length; k++)
			{
				_idxHeader[k] = NB.DecodeUInt32(fanoutTable, k * 4);
			}

			_idxdata = new byte[_idxHeader.Length][];
			for (int k = 0; k < _idxHeader.Length; k++)
			{
				int n;
				if (k == 0)
				{
					n = (int)(_idxHeader[k]);
				}
				else
				{
					n = (int)(_idxHeader[k] - _idxHeader[k - 1]);
				}

				if (n <= 0) continue;

				_idxdata[k] = new byte[n * (AnyObjectId.ObjectIdLength + 4)];
				NB.ReadFully(fd, _idxdata[k], 0, _idxdata[k].Length);
			}

			ObjectCount = _idxHeader[255];

			_packChecksum = new byte[20];
			NB.ReadFully(fd, _packChecksum, 0, _packChecksum.Length);
			 * */
		}

		public override IEnumerator<MutableEntry> GetEnumerator()
		{
			return new IndexV1Enumerator(this);
		}

		public override long ObjectCount { get; internal set; }

		public override long Offset64Count
		{
			get
			{
				long n64 = 0;
				foreach (MutableEntry e in this)
				{
					if (e.Offset >= int.MaxValue)
						n64++;
				}
				return n64;
			}
		}

		public override ObjectId GetObjectId(long nthPosition)
		{
			int levelOne = Array.BinarySearch(_idxHeader, nthPosition + 1);
			long lbase;
			if (levelOne >= 0)
			{
				// If we hit the bucket exactly the item is in the bucket, or
				// any bucket before it which has the same object count.
				//
				lbase = _idxHeader[levelOne];
				while (levelOne > 0 && lbase == _idxHeader[levelOne - 1])
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

			lbase = levelOne > 0 ? _idxHeader[levelOne - 1] : 0;
			var p = (int)(nthPosition - lbase);
            int dataIdx = ((4 + Constants.OBJECT_ID_LENGTH) * p) + 4;
			return ObjectId.FromRaw(_idxdata[levelOne], dataIdx);
		}

		public override long FindOffset(AnyObjectId objId)
		{
			int levelOne = objId.GetFirstByte();
			byte[] data = _idxdata[levelOne];
			if (data == null)
			{
				return -1;
			}

            int high = data.Length / (4 + Constants.OBJECT_ID_LENGTH);
			int low = 0;

			do
			{
				int mid = (low + high) / 2;
                int pos = ((4 + Constants.OBJECT_ID_LENGTH) * mid) + 4;
				int cmp = objId.CompareTo(data, pos);
				if (cmp < 0)
				{
					high = mid;
				}
				else if (cmp == 0)
				{
					uint b0 = data[pos - 4] & (uint)0xff;
					uint b1 = data[pos - 3] & (uint)0xff;
					uint b2 = data[pos - 2] & (uint)0xff;
					uint b3 = data[pos - 1] & (uint)0xff;
					return (((long)b0) << 24) | (b1 << 16) | (b2 << 8) | (b3);
				}
				else
					low = mid + 1;
			} while (low < high);
			return -1;
		}

		public override long FindCRC32(AnyObjectId objId)
		{
			throw new NotSupportedException();
		}

		public override bool HasCRC32Support
		{
			get
			{
				return false;
			}
		}

		#region Nested Types

        private class IndexV1Enumerator : EntriesIterator
        {
            private readonly PackIndexV1 _index;
            private int _levelOne;
            private int _levelTwo;

            public IndexV1Enumerator(PackIndexV1 index)
                : base(index)
            {
                _index = index;
            }

            protected override MutableObjectId IdBufferBuilder(MutableObjectId idBuffer)
            {
                idBuffer.FromRaw(_index._idxdata[_levelOne], _levelTwo - Constants.OBJECT_ID_LENGTH);
                return idBuffer;
            }

            protected override MutableEntry InnerNext(MutableEntry entry)
            {
                for (; _levelOne < _index._idxdata.Length; _levelOne++)
                {
                    if (_index._idxdata[_levelOne] == null)
                    {
                        continue;
                    }

                    if (_levelTwo < _index._idxdata[_levelOne].Length)
                    {
                        entry.Offset = NB.DecodeUInt32(_index._idxdata[_levelOne], _levelTwo);
                        _levelTwo += Constants.OBJECT_ID_LENGTH + 4;
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