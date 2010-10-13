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
		/// The
		/// <see cref="idHash">idHash</see>
		/// table stops growing at
		/// <code>1 &lt;&lt; MAX_HASH_BITS</code>
		/// .
		/// </summary>
		private const int MAX_HASH_BITS = 17;

		/// <summary>Shift to apply before storing a key.</summary>
		/// <remarks>
		/// Shift to apply before storing a key.
		/// <p>
		/// Within the 64 bit table record space, we leave the highest bit unset so
		/// all values are positive. The lower 32 bits to count bytes.
		/// </remarks>
		private const int KEY_SHIFT = 32;

		/// <summary>Total size of the file we hashed into the structure.</summary>
		/// <remarks>Total size of the file we hashed into the structure.</remarks>
		private long fileSize;

		/// <summary>
		/// Number of non-zero entries in
		/// <see cref="idHash">idHash</see>
		/// .
		/// </summary>
		private int idSize;

		/// <summary>Pairings of content keys and counters.</summary>
		/// <remarks>
		/// Pairings of content keys and counters.
		/// <p>
		/// Slots in the table are actually two ints wedged into a single long. The
		/// upper
		/// <see cref="MAX_HASH_BITS">MAX_HASH_BITS</see>
		/// bits stores the content key, and the
		/// remaining lower bits stores the number of bytes associated with that key.
		/// Empty slots are denoted by 0, which cannot occur because the count cannot
		/// be 0. Values can only be positive, which we enforce during key addition.
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

		internal virtual int Common(NGit.Diff.SimilarityIndex dst)
		{
			return Common(this, dst);
		}

		private static int Common(NGit.Diff.SimilarityIndex src, NGit.Diff.SimilarityIndex
			 dst)
		{
			int srcIdx = src.PackedIndex(0);
			int dstIdx = dst.PackedIndex(0);
			long[] srcHash = src.idHash;
			long[] dstHash = dst.idHash;
			return Common(srcHash, srcIdx, dstHash, dstIdx);
		}

		private static int Common(long[] srcHash, int srcIdx, long[] dstHash, int dstIdx)
		{
			//
			if (srcIdx == srcHash.Length || dstIdx == dstHash.Length)
			{
				return 0;
			}
			int common = 0;
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
						// Regions of dst which do not appear in dst.
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
					if (ShouldGrow())
					{
						Grow();
						j = Slot(key);
						continue;
					}
					idHash[j] = (((long)key) << KEY_SHIFT) | cnt;
					idSize++;
					return;
				}
				else
				{
					if (KeyOf(v) == key)
					{
						// Same key, increment the counter.
						idHash[j] = v + cnt;
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

		private int Slot(int key)
		{
			// We use 31 - idHashBits because the upper bit was already forced
			// to be 0 and we want the remaining high bits to be used as the
			// table slot.
			//
			return (int)(((uint)key) >> (31 - idHashBits));
		}

		private bool ShouldGrow()
		{
			return idHashBits < MAX_HASH_BITS && idHash.Length <= idSize * 2;
		}

		private void Grow()
		{
			long[] oldHash = idHash;
			int oldSize = idHash.Length;
			idHashBits++;
			idHash = new long[1 << idHashBits];
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

		private static int CountOf(long v)
		{
			return (int)v;
		}
	}
}
