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
using NGit.Diff;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>Index structure of lines/blocks in one file.</summary>
	/// <remarks>
	/// Index structure of lines/blocks in one file.
	/// <p>
	/// This structure can be used to compute an approximation of the similarity
	/// between two files. The index is used by
	/// <see cref="SimilarityRenameDetector">SimilarityRenameDetector</see>
	/// to
	/// compute scores between files.
	/// <p>
	/// To save space in memory, this index uses a space efficient encoding which
	/// will not exceed 1 MiB per instance. The index starts out at a smaller size
	/// (closer to 2 KiB), but may grow as more distinct blocks within the scanned
	/// file are discovered.
	/// </remarks>
	internal class SimilarityIndex
	{
		/// <summary>
		/// A special
		/// <see cref="TableFullException">TableFullException</see>
		/// used in place of OutOfMemoryError.
		/// </summary>
		private static readonly SimilarityIndex.TableFullException TABLE_FULL_OUT_OF_MEMORY
			 = new SimilarityIndex.TableFullException();

		/// <summary>Shift to apply before storing a key.</summary>
		/// <remarks>
		/// Shift to apply before storing a key.
		/// <p>
		/// Within the 64 bit table record space, we leave the highest bit unset so
		/// all values are positive. The lower 32 bits to count bytes.
		/// </remarks>
		private const int KEY_SHIFT = 32;

		/// <summary>Maximum value of the count field, also mask to extract the count.</summary>
		/// <remarks>Maximum value of the count field, also mask to extract the count.</remarks>
		private const long MAX_COUNT = (1L << KEY_SHIFT) - 1;

		/// <summary>Total size of the file we hashed into the structure.</summary>
		/// <remarks>Total size of the file we hashed into the structure.</remarks>
		private long fileSize;

		/// <summary>
		/// Number of non-zero entries in
		/// <see cref="idHash">idHash</see>
		/// .
		/// </summary>
		private int idSize;

		/// <summary>
		/// <see cref="idSize">idSize</see>
		/// that triggers
		/// <see cref="idHash">idHash</see>
		/// to double in size.
		/// </summary>
		private int idGrowAt;

		/// <summary>Pairings of content keys and counters.</summary>
		/// <remarks>
		/// Pairings of content keys and counters.
		/// <p>
		/// Slots in the table are actually two ints wedged into a single long. The
		/// upper 32 bits stores the content key, and the remaining lower bits stores
		/// the number of bytes associated with that key. Empty slots are denoted by
		/// 0, which cannot occur because the count cannot be 0. Values can only be
		/// positive, which we enforce during key addition.
		/// </remarks>
		private long[] idHash;

		/// <summary>
		/// <code>idHash.length == 1 &lt;&lt; idHashBits</code>
		/// .
		/// </summary>
		private int idHashBits;

		public SimilarityIndex()
		{
			idHashBits = 8;
			idHash = new long[1 << idHashBits];
			idGrowAt = GrowAt(idHashBits);
		}

		internal virtual long GetFileSize()
		{
			return fileSize;
		}

		internal virtual void SetFileSize(long size)
		{
			fileSize = size;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		internal virtual void Hash(ObjectLoader obj)
		{
			if (obj.IsLarge())
			{
				ObjectStream @in = obj.OpenStream();
				try
				{
					SetFileSize(@in.GetSize());
					Hash(@in, fileSize);
				}
				finally
				{
					@in.Close();
				}
			}
			else
			{
				byte[] raw = obj.GetCachedBytes();
				SetFileSize(raw.Length);
				Hash(raw, 0, raw.Length);
			}
		}

		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		internal virtual void Hash(byte[] raw, int ptr, int end)
		{
			while (ptr < end)
			{
				int hash = 5381;
				int start = ptr;
				do
				{
					// Hash one line, or one block, whichever occurs first.
					int c = raw[ptr++] & unchecked((int)(0xff));
					if (c == '\n')
					{
						break;
					}
					hash = (hash << 5) + hash + c;
				}
				while (ptr < end && ptr - start < 64);
				Add(hash, ptr - start);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		internal virtual void Hash(InputStream @in, long remaining)
		{
			byte[] buf = new byte[4096];
			int ptr = 0;
			int cnt = 0;
			while (0 < remaining)
			{
				int hash = 5381;
				// Hash one line, or one block, whichever occurs first.
				int n = 0;
				do
				{
					if (ptr == cnt)
					{
						ptr = 0;
						cnt = @in.Read(buf, 0, buf.Length);
						if (cnt <= 0)
						{
							throw new EOFException();
						}
					}
					n++;
					int c = buf[ptr++] & unchecked((int)(0xff));
					if (c == '\n')
					{
						break;
					}
					hash = (hash << 5) + hash + c;
				}
				while (n < 64 && n < remaining);
				Add(hash, n);
				remaining -= n;
			}
		}

		/// <summary>Sort the internal table so it can be used for efficient scoring.</summary>
		/// <remarks>
		/// Sort the internal table so it can be used for efficient scoring.
		/// <p>
		/// Once sorted, additional lines/blocks cannot be added to the index.
		/// </remarks>
		internal virtual void Sort()
		{
			// Sort the array. All of the empty space will wind up at the front,
			// because we forced all of the keys to always be positive. Later
			// we only work with the back half of the array.
			//
			Arrays.Sort(idHash);
		}

		internal virtual int Score(NGit.Diff.SimilarityIndex dst, int maxScore)
		{
			long max = Math.Max(fileSize, dst.fileSize);
			if (max == 0)
			{
				return maxScore;
			}
			return (int)((Common(dst) * maxScore) / max);
		}

		internal virtual long Common(NGit.Diff.SimilarityIndex dst)
		{
			return Common(this, dst);
		}

		private static long Common(NGit.Diff.SimilarityIndex src, NGit.Diff.SimilarityIndex
			 dst)
		{
			int srcIdx = src.PackedIndex(0);
			int dstIdx = dst.PackedIndex(0);
			long[] srcHash = src.idHash;
			long[] dstHash = dst.idHash;
			return Common(srcHash, srcIdx, dstHash, dstIdx);
		}

		private static long Common(long[] srcHash, int srcIdx, long[] dstHash, int dstIdx
			)
		{
			//
			if (srcIdx == srcHash.Length || dstIdx == dstHash.Length)
			{
				return 0;
			}
			long common = 0;
			int srcKey = KeyOf(srcHash[srcIdx]);
			int dstKey = KeyOf(dstHash[dstIdx]);
			for (; ; )
			{
				if (srcKey == dstKey)
				{
					common += Math.Min(CountOf(srcHash[srcIdx]), CountOf(dstHash[dstIdx]));
					if (++srcIdx == srcHash.Length)
					{
						break;
					}
					srcKey = KeyOf(srcHash[srcIdx]);
					if (++dstIdx == dstHash.Length)
					{
						break;
					}
					dstKey = KeyOf(dstHash[dstIdx]);
				}
				else
				{
					if (srcKey < dstKey)
					{
						// Regions of src which do not appear in dst.
						if (++srcIdx == srcHash.Length)
						{
							break;
						}
						srcKey = KeyOf(srcHash[srcIdx]);
					}
					else
					{
						// Regions of dst which do not appear in src.
						if (++dstIdx == dstHash.Length)
						{
							break;
						}
						dstKey = KeyOf(dstHash[dstIdx]);
					}
				}
			}
			return common;
		}

		// Testing only
		internal virtual int Size()
		{
			return idSize;
		}

		// Testing only
		internal virtual int Key(int idx)
		{
			return KeyOf(idHash[PackedIndex(idx)]);
		}

		// Testing only
		internal virtual long Count(int idx)
		{
			return CountOf(idHash[PackedIndex(idx)]);
		}

		// Brute force approach only for testing.
		internal virtual int FindIndex(int key)
		{
			for (int i = 0; i < idSize; i++)
			{
				if (Key(i) == key)
				{
					return i;
				}
			}
			return -1;
		}

		private int PackedIndex(int idx)
		{
			return (idHash.Length - idSize) + idx;
		}

		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		internal virtual void Add(int key, int cnt)
		{
			key = (int)(((uint)(key * unchecked((int)(0x9e370001)))) >> 1);
			// Mix bits and ensure not negative.
			int j = Slot(key);
			for (; ; )
			{
				long v = idHash[j];
				if (v == 0)
				{
					// Empty slot in the table, store here.
					if (idGrowAt <= idSize)
					{
						Grow();
						j = Slot(key);
						continue;
					}
					idHash[j] = Pair(key, cnt);
					idSize++;
					return;
				}
				else
				{
					if (KeyOf(v) == key)
					{
						// Same key, increment the counter. If it overflows, fail
						// indexing to prevent the key from being impacted.
						//
						idHash[j] = Pair(key, CountOf(v) + cnt);
						return;
					}
					else
					{
						if (++j >= idHash.Length)
						{
							j = 0;
						}
					}
				}
			}
		}

		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		private static long Pair(int key, long cnt)
		{
			if (MAX_COUNT < cnt)
			{
				throw new SimilarityIndex.TableFullException();
			}
			return (((long)key) << KEY_SHIFT) | cnt;
		}

		private int Slot(int key)
		{
			// We use 31 - idHashBits because the upper bit was already forced
			// to be 0 and we want the remaining high bits to be used as the
			// table slot.
			//
			return (int)(((uint)key) >> (31 - idHashBits));
		}

		private static int GrowAt(int idHashBits)
		{
			return (1 << idHashBits) * (idHashBits - 3) / idHashBits;
		}

		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		private void Grow()
		{
			if (idHashBits == 30)
			{
				throw new SimilarityIndex.TableFullException();
			}
			long[] oldHash = idHash;
			int oldSize = idHash.Length;
			idHashBits++;
			idGrowAt = GrowAt(idHashBits);
			try
			{
				idHash = new long[1 << idHashBits];
			}
			catch (OutOfMemoryException)
			{
				throw TABLE_FULL_OUT_OF_MEMORY;
			}
			for (int i = 0; i < oldSize; i++)
			{
				long v = oldHash[i];
				if (v != 0)
				{
					int j = Slot(KeyOf(v));
					while (idHash[j] != 0)
					{
						if (++j >= idHash.Length)
						{
							j = 0;
						}
					}
					idHash[j] = v;
				}
			}
		}

		private static int KeyOf(long v)
		{
			return (int)((long)(((ulong)v) >> KEY_SHIFT));
		}

		private static long CountOf(long v)
		{
			return v & MAX_COUNT;
		}

		[System.Serializable]
		internal class TableFullException : Exception
		{
			private const long serialVersionUID = 1L;
		}
	}
}
