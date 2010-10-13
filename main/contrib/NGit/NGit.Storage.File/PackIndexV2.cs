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

using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Support for the pack index v2 format.</summary>
	/// <remarks>Support for the pack index v2 format.</remarks>
	internal class PackIndexV2 : PackIndex
	{
		private const long IS_O64 = 1L << 31;

		private const int FANOUT = 256;

		private static readonly int[] NO_INTS = new int[] {  };

		private static readonly byte[] NO_BYTES = new byte[] {  };

		private long objectCnt;

		private readonly long[] fanoutTable;

		/// <summary>256 arrays of contiguous object names.</summary>
		/// <remarks>256 arrays of contiguous object names.</remarks>
		private int[][] names;

		/// <summary>
		/// 256 arrays of the 32 bit offset data, matching
		/// <see cref="names">names</see>
		/// .
		/// </summary>
		private byte[][] offset32;

		/// <summary>
		/// 256 arrays of the CRC-32 of objects, matching
		/// <see cref="names">names</see>
		/// .
		/// </summary>
		private byte[][] crc32;

		/// <summary>64 bit offset table.</summary>
		/// <remarks>64 bit offset table.</remarks>
		private byte[] offset64;

		/// <exception cref="System.IO.IOException"></exception>
		internal PackIndexV2(InputStream fd)
		{
			byte[] fanoutRaw = new byte[4 * FANOUT];
			IOUtil.ReadFully(fd, fanoutRaw, 0, fanoutRaw.Length);
			fanoutTable = new long[FANOUT];
			for (int k = 0; k < FANOUT; k++)
			{
				fanoutTable[k] = NB.DecodeUInt32(fanoutRaw, k * 4);
			}
			objectCnt = fanoutTable[FANOUT - 1];
			names = new int[FANOUT][];
			offset32 = new byte[FANOUT][];
			crc32 = new byte[FANOUT][];
			// Object name table. The size we can permit per fan-out bucket
			// is limited to Java's 2 GB per byte array limitation. That is
			// no more than 107,374,182 objects per fan-out.
			//
			for (int k_1 = 0; k_1 < FANOUT; k_1++)
			{
				long bucketCnt;
				if (k_1 == 0)
				{
					bucketCnt = fanoutTable[k_1];
				}
				else
				{
					bucketCnt = fanoutTable[k_1] - fanoutTable[k_1 - 1];
				}
				if (bucketCnt == 0)
				{
					names[k_1] = NO_INTS;
					offset32[k_1] = NO_BYTES;
					crc32[k_1] = NO_BYTES;
					continue;
				}
				long nameLen = bucketCnt * Constants.OBJECT_ID_LENGTH;
				if (nameLen > int.MaxValue)
				{
					throw new IOException(JGitText.Get().indexFileIsTooLargeForJgit);
				}
				int intNameLen = (int)nameLen;
				byte[] raw = new byte[intNameLen];
				int[] bin = new int[(int)(((uint)intNameLen) >> 2)];
				IOUtil.ReadFully(fd, raw, 0, raw.Length);
				for (int i = 0; i < bin.Length; i++)
				{
					bin[i] = NB.DecodeInt32(raw, i << 2);
				}
				names[k_1] = bin;
				offset32[k_1] = new byte[(int)(bucketCnt * 4)];
				crc32[k_1] = new byte[(int)(bucketCnt * 4)];
			}
			// CRC32 table.
			for (int k_2 = 0; k_2 < FANOUT; k_2++)
			{
				IOUtil.ReadFully(fd, crc32[k_2], 0, crc32[k_2].Length);
			}
			// 32 bit offset table. Any entries with the most significant bit
			// set require a 64 bit offset entry in another table.
			//
			int o64cnt = 0;
			for (int k_3 = 0; k_3 < FANOUT; k_3++)
			{
				byte[] ofs = offset32[k_3];
				IOUtil.ReadFully(fd, ofs, 0, ofs.Length);
				for (int p = 0; p < ofs.Length; p += 4)
				{
					if (((sbyte)ofs[p]) < 0)
					{
						o64cnt++;
					}
				}
			}
			// 64 bit offset table. Most objects should not require an entry.
			//
			if (o64cnt > 0)
			{
				offset64 = new byte[o64cnt * 8];
				IOUtil.ReadFully(fd, offset64, 0, offset64.Length);
			}
			else
			{
				offset64 = NO_BYTES;
			}
			packChecksum = new byte[20];
			IOUtil.ReadFully(fd, packChecksum, 0, packChecksum.Length);
		}

		internal override long GetObjectCount()
		{
			return objectCnt;
		}

		internal override long GetOffset64Count()
		{
			return offset64.Length / 8;
		}

		internal override ObjectId GetObjectId(long nthPosition)
		{
			int levelOne = System.Array.BinarySearch(fanoutTable, nthPosition + 1);
			long @base;
			if (levelOne >= 0)
			{
				// If we hit the bucket exactly the item is in the bucket, or
				// any bucket before it which has the same object count.
				//
				@base = fanoutTable[levelOne];
				while (levelOne > 0 && @base == fanoutTable[levelOne - 1])
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
			@base = levelOne > 0 ? fanoutTable[levelOne - 1] : 0;
			int p = (int)(nthPosition - @base);
			int p4 = p << 2;
			return ObjectId.FromRaw(names[levelOne], p4 + p);
		}

		// p * 5
		internal override long FindOffset(AnyObjectId objId)
		{
			int levelOne = objId.FirstByte;
			int levelTwo = BinarySearchLevelTwo(objId, levelOne);
			if (levelTwo == -1)
			{
				return -1;
			}
			long p = NB.DecodeUInt32(offset32[levelOne], levelTwo << 2);
			if ((p & IS_O64) != 0)
			{
				return NB.DecodeUInt64(offset64, (8 * (int)(p & ~IS_O64)));
			}
			return p;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		internal override long FindCRC32(AnyObjectId objId)
		{
			int levelOne = objId.FirstByte;
			int levelTwo = BinarySearchLevelTwo(objId, levelOne);
			if (levelTwo == -1)
			{
				throw new MissingObjectException(objId.Copy(), "unknown");
			}
			return NB.DecodeUInt32(crc32[levelOne], levelTwo << 2);
		}

		internal override bool HasCRC32Support()
		{
			return true;
		}

		public override Sharpen.Iterator<PackIndex.MutableEntry> Iterator()
		{
			return new PackIndexV2.EntriesIteratorV2(this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId
			 id, int matchLimit)
		{
			int[] data = names[id.FirstByte];
			int max = (int)(((uint)offset32[id.FirstByte].Length) >> 2);
			int high = max;
			if (high == 0)
			{
				return;
			}
			int low = 0;
			do
			{
				int p = (int)(((uint)(low + high)) >> 1);
				int cmp = id.PrefixCompare(data, IdOffset(p));
				if (cmp < 0)
				{
					high = p;
				}
				else
				{
					if (cmp == 0)
					{
						// We may have landed in the middle of the matches.  Move
						// backwards to the start of matches, then walk forwards.
						//
						while (0 < p && id.PrefixCompare(data, IdOffset(p - 1)) == 0)
						{
							p--;
						}
						for (; p < max && id.PrefixCompare(data, IdOffset(p)) == 0; p++)
						{
							matches.AddItem(ObjectId.FromRaw(data, IdOffset(p)));
							if (matches.Count > matchLimit)
							{
								break;
							}
						}
						return;
					}
					else
					{
						low = p + 1;
					}
				}
			}
			while (low < high);
		}

		private static int IdOffset(int p)
		{
			return (p << 2) + p;
		}

		// p * 5
		private int BinarySearchLevelTwo(AnyObjectId objId, int levelOne)
		{
			int[] data = names[levelOne];
			int high = (int)(((uint)offset32[levelOne].Length) >> 2);
			if (high == 0)
			{
				return -1;
			}
			int low = 0;
			do
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				int mid4 = mid << 2;
				int cmp;
				cmp = objId.CompareTo(data, mid4 + mid);
				// mid * 5
				if (cmp < 0)
				{
					high = mid;
				}
				else
				{
					if (cmp == 0)
					{
						return mid;
					}
					else
					{
						low = mid + 1;
					}
				}
			}
			while (low < high);
			return -1;
		}

		private class EntriesIteratorV2 : PackIndex.EntriesIterator
		{
			private int levelOne;

			private int levelTwo;

			protected internal override PackIndex.MutableEntry InitEntry()
			{
				return new _MutableEntry_290(this);
			}

			private sealed class _MutableEntry_290 : PackIndex.MutableEntry
			{
				public _MutableEntry_290(EntriesIteratorV2 _enclosing)
				{
					this._enclosing = _enclosing;
				}

				internal override void EnsureId()
				{
					this.idBuffer.FromRaw(this._enclosing._enclosing.names[this._enclosing.levelOne], 
						this._enclosing.levelTwo - Constants.OBJECT_ID_LENGTH / 4);
				}

				private readonly EntriesIteratorV2 _enclosing;
			}

			public override PackIndex.MutableEntry Next()
			{
				for (; this.levelOne < this._enclosing.names.Length; this.levelOne++)
				{
					if (this.levelTwo < this._enclosing.names[this.levelOne].Length)
					{
						int idx = this.levelTwo / (Constants.OBJECT_ID_LENGTH / 4) * 4;
						long offset = NB.DecodeUInt32(this._enclosing.offset32[this.levelOne], idx);
						if ((offset & PackIndexV2.IS_O64) != 0)
						{
							idx = (8 * (int)(offset & ~PackIndexV2.IS_O64));
							offset = NB.DecodeUInt64(this._enclosing.offset64, idx);
						}
						this.entry.offset = offset;
						this.levelTwo += Constants.OBJECT_ID_LENGTH / 4;
						this.returnedNumber++;
						return this.entry;
					}
					this.levelTwo = 0;
				}
				throw new NoSuchElementException();
			}

			internal EntriesIteratorV2(PackIndexV2 _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PackIndexV2 _enclosing;
		}
	}
}
