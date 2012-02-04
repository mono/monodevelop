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

using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.Pack
{
	/// <summary>Index of blocks in a source file.</summary>
	/// <remarks>
	/// Index of blocks in a source file.
	/// <p>
	/// The index can be passed a result buffer, and output an instruction sequence
	/// that transforms the source buffer used by the index into the result buffer.
	/// The instruction sequence can be executed by
	/// <see cref="BinaryDelta">BinaryDelta</see>
	/// or
	/// <see cref="DeltaStream">DeltaStream</see>
	/// to recreate the result buffer.
	/// <p>
	/// An index stores the entire contents of the source buffer, but also a table of
	/// block identities mapped to locations where the block appears in the source
	/// buffer. The mapping table uses 12 bytes for every 16 bytes of source buffer,
	/// and is therefore ~75% of the source buffer size. The overall index is ~1.75x
	/// the size of the source buffer. This relationship holds for any JVM, as only a
	/// constant number of objects are allocated per index. Callers can use the
	/// method
	/// <see cref="GetIndexSize()">GetIndexSize()</see>
	/// to obtain a reasonably accurate estimate of
	/// the complete heap space used by this index.
	/// <p>
	/// A
	/// <code>DeltaIndex</code>
	/// is thread-safe. Concurrent threads can use the same
	/// index to encode delta instructions for different result buffers.
	/// </remarks>
	public class DeltaIndex
	{
		/// <summary>Number of bytes in a block.</summary>
		/// <remarks>Number of bytes in a block.</remarks>
		internal const int BLKSZ = 16;

		// must be 16, see unrolled loop in hashBlock
		/// <summary>Estimate the size of an index for a given source.</summary>
		/// <remarks>
		/// Estimate the size of an index for a given source.
		/// <p>
		/// This is roughly a worst-case estimate. The actual index may be smaller.
		/// </remarks>
		/// <param name="sourceLength">length of the source, in bytes.</param>
		/// <returns>
		/// estimated size. Approximately
		/// <code>1.75 * sourceLength</code>
		/// .
		/// </returns>
		public static long EstimateIndexSize(int sourceLength)
		{
			return sourceLength + (sourceLength * 3 / 4);
		}

		/// <summary>Maximum number of positions to consider for a given content hash.</summary>
		/// <remarks>
		/// Maximum number of positions to consider for a given content hash.
		/// <p>
		/// All positions with the same content hash are stored into a single chain.
		/// The chain size is capped to ensure delta encoding stays linear time at
		/// O(len_src + len_dst) rather than quadratic at O(len_src * len_dst).
		/// </remarks>
		private const int MAX_CHAIN_LENGTH = 64;

		/// <summary>Original source file that we indexed.</summary>
		/// <remarks>Original source file that we indexed.</remarks>
		private readonly byte[] src;

		/// <summary>
		/// Pointers into the
		/// <see cref="entries">entries</see>
		/// table, indexed by block hash.
		/// <p>
		/// A block hash is masked with
		/// <see cref="tableMask">tableMask</see>
		/// to become the array index
		/// of this table. The value stored here is the first index within
		/// <see cref="entries">entries</see>
		/// that starts the consecutive list of blocks with that
		/// same masked hash. If there are no matching blocks, 0 is stored instead.
		/// <p>
		/// Note that this table is always a power of 2 in size, to support fast
		/// normalization of a block hash into an array index.
		/// </summary>
		private readonly int[] table;

		/// <summary>
		/// Pairs of block hash value and
		/// <see cref="src">src</see>
		/// offsets.
		/// <p>
		/// The very first entry in this table at index 0 is always empty, this is to
		/// allow fast evaluation when
		/// <see cref="table">table</see>
		/// has no values under any given
		/// slot. Remaining entries are pairs of integers, with the upper 32 bits
		/// holding the block hash and the lower 32 bits holding the source offset.
		/// </summary>
		private readonly long[] entries;

		/// <summary>
		/// Mask to make block hashes into an array index for
		/// <see cref="table">table</see>
		/// .
		/// </summary>
		private readonly int tableMask;

		/// <summary>Construct an index from the source file.</summary>
		/// <remarks>Construct an index from the source file.</remarks>
		/// <param name="sourceBuffer">
		/// the source file's raw contents. The buffer will be held by the
		/// index instance to facilitate matching, and therefore must not
		/// be modified by the caller.
		/// </param>
		public DeltaIndex(byte[] sourceBuffer)
		{
			src = sourceBuffer;
			DeltaIndexScanner scan = new DeltaIndexScanner(src, src.Length);
			// Reuse the same table the scanner made. We will replace the
			// values at each position, but we want the same-length array.
			//
			table = scan.table;
			tableMask = scan.tableMask;
			// Because entry index 0 means there are no entries for the
			// slot in the table, we have to allocate one extra position.
			//
			entries = new long[1 + CountEntries(scan)];
			CopyEntries(scan);
		}

		private int CountEntries(DeltaIndexScanner scan)
		{
			// Figure out exactly how many entries we need. As we do the
			// enumeration truncate any delta chains longer than what we
			// are willing to scan during encode. This keeps the encode
			// logic linear in the size of the input rather than quadratic.
			//
			int cnt = 0;
			for (int i = 0; i < table.Length; i++)
			{
				int h = table[i];
				if (h == 0)
				{
					continue;
				}
				int len = 0;
				do
				{
					if (++len == MAX_CHAIN_LENGTH)
					{
						scan.next[h] = 0;
						break;
					}
					h = scan.next[h];
				}
				while (h != 0);
				cnt += len;
			}
			return cnt;
		}

		private void CopyEntries(DeltaIndexScanner scan)
		{
			// Rebuild the entries list from the scanner, positioning all
			// blocks in the same hash chain next to each other. We can
			// then later discard the next list, along with the scanner.
			//
			int next = 1;
			for (int i = 0; i < table.Length; i++)
			{
				int h = table[i];
				if (h == 0)
				{
					continue;
				}
				table[i] = next;
				do
				{
					entries[next++] = scan.entries[h];
					h = scan.next[h];
				}
				while (h != 0);
			}
		}

		/// <returns>size of the source buffer this index has scanned.</returns>
		public virtual long GetSourceSize()
		{
			return src.Length;
		}

		/// <summary>Get an estimate of the memory required by this index.</summary>
		/// <remarks>Get an estimate of the memory required by this index.</remarks>
		/// <returns>
		/// an approximation of the number of bytes used by this index in
		/// memory. The size includes the cached source buffer size from
		/// <see cref="GetSourceSize()">GetSourceSize()</see>
		/// , as well as a rough approximation of JVM
		/// object overheads.
		/// </returns>
		public virtual long GetIndexSize()
		{
			long sz = 8;
			sz += 4 * 4;
			sz += SizeOf(src);
			sz += SizeOf(table);
			sz += SizeOf(entries);
			return sz;
		}

		private static long SizeOf(byte[] b)
		{
			return SizeOfArray(1, b.Length);
		}

		private static long SizeOf(int[] b)
		{
			return SizeOfArray(4, b.Length);
		}

		private static long SizeOf(long[] b)
		{
			return SizeOfArray(8, b.Length);
		}

		private static int SizeOfArray(int entSize, int len)
		{
			return 12 + (len * entSize);
		}

		/// <summary>Generate a delta sequence to recreate the result buffer.</summary>
		/// <remarks>
		/// Generate a delta sequence to recreate the result buffer.
		/// <p>
		/// There is no limit on the size of the delta sequence created. This is the
		/// same as
		/// <code>encode(out, res, 0)</code>
		/// .
		/// </remarks>
		/// <param name="out">
		/// stream to receive the delta instructions that can transform
		/// this index's source buffer into
		/// <code>res</code>
		/// . This stream
		/// should be buffered, as instructions are written directly to it
		/// in small bursts.
		/// </param>
		/// <param name="res">
		/// the desired result buffer. The generated instructions will
		/// recreate this buffer when applied to the source buffer stored
		/// within this index.
		/// </param>
		/// <exception cref="System.IO.IOException">the output stream refused to write the instructions.
		/// 	</exception>
		public virtual void Encode(OutputStream @out, byte[] res)
		{
			Encode(@out, res, 0);
		}

		/// <summary>Generate a delta sequence to recreate the result buffer.</summary>
		/// <remarks>Generate a delta sequence to recreate the result buffer.</remarks>
		/// <param name="out">
		/// stream to receive the delta instructions that can transform
		/// this index's source buffer into
		/// <code>res</code>
		/// . This stream
		/// should be buffered, as instructions are written directly to it
		/// in small bursts. If the caller might need to discard the
		/// instructions (such as when deltaSizeLimit would be exceeded)
		/// the caller is responsible for discarding or rewinding the
		/// stream when this method returns false.
		/// </param>
		/// <param name="res">
		/// the desired result buffer. The generated instructions will
		/// recreate this buffer when applied to the source buffer stored
		/// within this index.
		/// </param>
		/// <param name="deltaSizeLimit">
		/// maximum number of bytes that the delta instructions can
		/// occupy. If the generated instructions would be longer than
		/// this amount, this method returns false. If 0, there is no
		/// limit on the length of delta created.
		/// </param>
		/// <returns>
		/// true if the delta is smaller than deltaSizeLimit; false if the
		/// encoder aborted because the encoded delta instructions would be
		/// longer than deltaSizeLimit bytes.
		/// </returns>
		/// <exception cref="System.IO.IOException">the output stream refused to write the instructions.
		/// 	</exception>
		public virtual bool Encode(OutputStream @out, byte[] res, int deltaSizeLimit)
		{
			int end = res.Length;
			DeltaEncoder enc = NewEncoder(@out, end, deltaSizeLimit);
			// If either input is smaller than one full block, we simply punt
			// and construct a delta as a literal. This implies that any file
			// smaller than our block size is never delta encoded as the delta
			// will always be larger than the file itself would be.
			//
			if (end < BLKSZ || table.Length == 0)
			{
				return enc.Insert(res);
			}
			// Bootstrap the scan by constructing a hash for the first block
			// in the input.
			//
			int blkPtr = 0;
			int blkEnd = BLKSZ;
			int hash = HashBlock(res, 0);
			int resPtr = 0;
			while (blkEnd < end)
			{
				int tableIdx = hash & tableMask;
				int entryIdx = table[tableIdx];
				if (entryIdx == 0)
				{
					// No matching blocks, slide forward one byte.
					//
					hash = Step(hash, res[blkPtr++], res[blkEnd++]);
					continue;
				}
				// For every possible location of the current block, try to
				// extend the match out to the longest common substring.
				//
				int bestLen = -1;
				int bestPtr = -1;
				int bestNeg = 0;
				do
				{
					long ent = entries[entryIdx++];
					if (KeyOf(ent) == hash)
					{
						int neg = 0;
						if (resPtr < blkPtr)
						{
							// If we need to do an insertion, check to see if
							// moving the starting point of the copy backwards
							// will allow us to shorten the insert. Our hash
							// may not have allowed us to identify this area.
							// Since it is quite fast to perform a negative
							// scan, try to stretch backwards too.
							//
							neg = blkPtr - resPtr;
							neg = Negmatch(res, blkPtr, src, ValOf(ent), neg);
						}
						int len = neg + Fwdmatch(res, blkPtr, src, ValOf(ent));
						if (bestLen < len)
						{
							bestLen = len;
							bestPtr = ValOf(ent);
							bestNeg = neg;
						}
					}
					else
					{
						if ((KeyOf(ent) & tableMask) != tableIdx)
						{
							break;
						}
					}
				}
				while (bestLen < 4096 && entryIdx < entries.Length);
				if (bestLen < BLKSZ)
				{
					// All of the locations were false positives, or the copy
					// is shorter than a block. In the latter case this won't
					// give us a very great copy instruction, so delay and try
					// at the next byte.
					//
					hash = Step(hash, res[blkPtr++], res[blkEnd++]);
					continue;
				}
				blkPtr -= bestNeg;
				if (resPtr < blkPtr)
				{
					// There are bytes between the last instruction we made
					// and the current block pointer. None of these matched
					// during the earlier iteration so insert them directly
					// into the instruction stream.
					//
					int cnt = blkPtr - resPtr;
					if (!enc.Insert(res, resPtr, cnt))
					{
						return false;
					}
				}
				if (!enc.Copy(bestPtr - bestNeg, bestLen))
				{
					return false;
				}
				blkPtr += bestLen;
				resPtr = blkPtr;
				blkEnd = blkPtr + BLKSZ;
				// If we don't have a full block available to us, abort now.
				//
				if (end <= blkEnd)
				{
					break;
				}
				// Start a new hash of the block after the copy region.
				//
				hash = HashBlock(res, blkPtr);
			}
			if (resPtr < end)
			{
				// There were bytes at the end which didn't match, or maybe
				// didn't make a full block. Insert whatever is left over.
				//
				int cnt = end - resPtr;
				return enc.Insert(res, resPtr, cnt);
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DeltaEncoder NewEncoder(OutputStream @out, long resSize, int limit)
		{
			return new DeltaEncoder(@out, GetSourceSize(), resSize, limit);
		}

		private static int Fwdmatch(byte[] res, int resPtr, byte[] src, int srcPtr)
		{
			int start = resPtr;
			for (; resPtr < res.Length && srcPtr < src.Length; resPtr++, srcPtr++)
			{
				if (res[resPtr] != src[srcPtr])
				{
					break;
				}
			}
			return resPtr - start;
		}

		private static int Negmatch(byte[] res, int resPtr, byte[] src, int srcPtr, int limit
			)
		{
			if (srcPtr == 0)
			{
				return 0;
			}
			resPtr--;
			srcPtr--;
			int start = resPtr;
			do
			{
				if (res[resPtr] != src[srcPtr])
				{
					break;
				}
				resPtr--;
				srcPtr--;
			}
			while (0 <= srcPtr && 0 < --limit);
			return start - resPtr;
		}

		public override string ToString()
		{
			string[] units = new string[] { "bytes", "KiB", "MiB", "GiB" };
			long sz = GetIndexSize();
			int u = 0;
			while (1024 <= sz && u < units.Length - 1)
			{
				int rem = (int)(sz % 1024);
				sz /= 1024;
				if (rem != 0)
				{
					sz++;
				}
				u++;
			}
			return "DeltaIndex[" + sz + " " + units[u] + "]";
		}

		internal static int HashBlock(byte[] raw, int ptr)
		{
			int hash;
			// The first 4 steps collapse out into a 4 byte big-endian decode,
			// with a larger right shift as we combined shift lefts together.
			//
			hash = ((raw[ptr] & unchecked((int)(0xff))) << 24) | ((raw[ptr + 1] & unchecked((
				int)(0xff))) << 16) | ((raw[ptr + 2] & unchecked((int)(0xff))) << 8) | (raw[ptr 
				+ 3] & unchecked((int)(0xff)));
			//
			//
			//
			hash ^= T[(int)(((uint)hash) >> 31)];
			hash = ((hash << 8) | (raw[ptr + 4] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 5] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 6] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 7] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 8] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 9] & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash
				) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 10] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 11] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 12] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 13] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 14] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			hash = ((hash << 8) | (raw[ptr + 15] & unchecked((int)(0xff)))) ^ T[(int)(((uint)
				hash) >> 23)];
			return hash;
		}

		private static int Step(int hash, byte toRemove, byte toAdd)
		{
			hash ^= U[toRemove & unchecked((int)(0xff))];
			return ((hash << 8) | (toAdd & unchecked((int)(0xff)))) ^ T[(int)(((uint)hash) >>
				 23)];
		}

		private static int KeyOf(long ent)
		{
			return (int)((long)(((ulong)ent) >> 32));
		}

		private static int ValOf(long ent)
		{
			return (int)ent;
		}

		private static readonly int[] T = new int[] { unchecked((int)(0x00000000)), unchecked(
			(int)(0xd4c6b32d)), unchecked((int)(0x7d4bd577)), unchecked((int)(0xa98d665a)), 
			unchecked((int)(0x2e5119c3)), unchecked((int)(0xfa97aaee)), unchecked((int)(0x531accb4
			)), unchecked((int)(0x87dc7f99)), unchecked((int)(0x5ca23386)), unchecked((int)(
			0x886480ab)), unchecked((int)(0x21e9e6f1)), unchecked((int)(0xf52f55dc)), unchecked(
			(int)(0x72f32a45)), unchecked((int)(0xa6359968)), unchecked((int)(0x0fb8ff32)), 
			unchecked((int)(0xdb7e4c1f)), unchecked((int)(0x6d82d421)), unchecked((int)(0xb944670c
			)), unchecked((int)(0x10c90156)), unchecked((int)(0xc40fb27b)), unchecked((int)(
			0x43d3cde2)), unchecked((int)(0x97157ecf)), unchecked((int)(0x3e981895)), unchecked(
			(int)(0xea5eabb8)), unchecked((int)(0x3120e7a7)), unchecked((int)(0xe5e6548a)), 
			unchecked((int)(0x4c6b32d0)), unchecked((int)(0x98ad81fd)), unchecked((int)(0x1f71fe64
			)), unchecked((int)(0xcbb74d49)), unchecked((int)(0x623a2b13)), unchecked((int)(
			0xb6fc983e)), unchecked((int)(0x0fc31b6f)), unchecked((int)(0xdb05a842)), unchecked(
			(int)(0x7288ce18)), unchecked((int)(0xa64e7d35)), unchecked((int)(0x219202ac)), 
			unchecked((int)(0xf554b181)), unchecked((int)(0x5cd9d7db)), unchecked((int)(0x881f64f6
			)), unchecked((int)(0x536128e9)), unchecked((int)(0x87a79bc4)), unchecked((int)(
			0x2e2afd9e)), unchecked((int)(0xfaec4eb3)), unchecked((int)(0x7d30312a)), unchecked(
			(int)(0xa9f68207)), unchecked((int)(0x007be45d)), unchecked((int)(0xd4bd5770)), 
			unchecked((int)(0x6241cf4e)), unchecked((int)(0xb6877c63)), unchecked((int)(0x1f0a1a39
			)), unchecked((int)(0xcbcca914)), unchecked((int)(0x4c10d68d)), unchecked((int)(
			0x98d665a0)), unchecked((int)(0x315b03fa)), unchecked((int)(0xe59db0d7)), unchecked(
			(int)(0x3ee3fcc8)), unchecked((int)(0xea254fe5)), unchecked((int)(0x43a829bf)), 
			unchecked((int)(0x976e9a92)), unchecked((int)(0x10b2e50b)), unchecked((int)(0xc4745626
			)), unchecked((int)(0x6df9307c)), unchecked((int)(0xb93f8351)), unchecked((int)(
			0x1f8636de)), unchecked((int)(0xcb4085f3)), unchecked((int)(0x62cde3a9)), unchecked(
			(int)(0xb60b5084)), unchecked((int)(0x31d72f1d)), unchecked((int)(0xe5119c30)), 
			unchecked((int)(0x4c9cfa6a)), unchecked((int)(0x985a4947)), unchecked((int)(0x43240558
			)), unchecked((int)(0x97e2b675)), unchecked((int)(0x3e6fd02f)), unchecked((int)(
			0xeaa96302)), unchecked((int)(0x6d751c9b)), unchecked((int)(0xb9b3afb6)), unchecked(
			(int)(0x103ec9ec)), unchecked((int)(0xc4f87ac1)), unchecked((int)(0x7204e2ff)), 
			unchecked((int)(0xa6c251d2)), unchecked((int)(0x0f4f3788)), unchecked((int)(0xdb8984a5
			)), unchecked((int)(0x5c55fb3c)), unchecked((int)(0x88934811)), unchecked((int)(
			0x211e2e4b)), unchecked((int)(0xf5d89d66)), unchecked((int)(0x2ea6d179)), unchecked(
			(int)(0xfa606254)), unchecked((int)(0x53ed040e)), unchecked((int)(0x872bb723)), 
			unchecked((int)(0x00f7c8ba)), unchecked((int)(0xd4317b97)), unchecked((int)(0x7dbc1dcd
			)), unchecked((int)(0xa97aaee0)), unchecked((int)(0x10452db1)), unchecked((int)(
			0xc4839e9c)), unchecked((int)(0x6d0ef8c6)), unchecked((int)(0xb9c84beb)), unchecked(
			(int)(0x3e143472)), unchecked((int)(0xead2875f)), unchecked((int)(0x435fe105)), 
			unchecked((int)(0x97995228)), unchecked((int)(0x4ce71e37)), unchecked((int)(0x9821ad1a
			)), unchecked((int)(0x31accb40)), unchecked((int)(0xe56a786d)), unchecked((int)(
			0x62b607f4)), unchecked((int)(0xb670b4d9)), unchecked((int)(0x1ffdd283)), unchecked(
			(int)(0xcb3b61ae)), unchecked((int)(0x7dc7f990)), unchecked((int)(0xa9014abd)), 
			unchecked((int)(0x008c2ce7)), unchecked((int)(0xd44a9fca)), unchecked((int)(0x5396e053
			)), unchecked((int)(0x8750537e)), unchecked((int)(0x2edd3524)), unchecked((int)(
			0xfa1b8609)), unchecked((int)(0x2165ca16)), unchecked((int)(0xf5a3793b)), unchecked(
			(int)(0x5c2e1f61)), unchecked((int)(0x88e8ac4c)), unchecked((int)(0x0f34d3d5)), 
			unchecked((int)(0xdbf260f8)), unchecked((int)(0x727f06a2)), unchecked((int)(0xa6b9b58f
			)), unchecked((int)(0x3f0c6dbc)), unchecked((int)(0xebcade91)), unchecked((int)(
			0x4247b8cb)), unchecked((int)(0x96810be6)), unchecked((int)(0x115d747f)), unchecked(
			(int)(0xc59bc752)), unchecked((int)(0x6c16a108)), unchecked((int)(0xb8d01225)), 
			unchecked((int)(0x63ae5e3a)), unchecked((int)(0xb768ed17)), unchecked((int)(0x1ee58b4d
			)), unchecked((int)(0xca233860)), unchecked((int)(0x4dff47f9)), unchecked((int)(
			0x9939f4d4)), unchecked((int)(0x30b4928e)), unchecked((int)(0xe47221a3)), unchecked(
			(int)(0x528eb99d)), unchecked((int)(0x86480ab0)), unchecked((int)(0x2fc56cea)), 
			unchecked((int)(0xfb03dfc7)), unchecked((int)(0x7cdfa05e)), unchecked((int)(0xa8191373
			)), unchecked((int)(0x01947529)), unchecked((int)(0xd552c604)), unchecked((int)(
			0x0e2c8a1b)), unchecked((int)(0xdaea3936)), unchecked((int)(0x73675f6c)), unchecked(
			(int)(0xa7a1ec41)), unchecked((int)(0x207d93d8)), unchecked((int)(0xf4bb20f5)), 
			unchecked((int)(0x5d3646af)), unchecked((int)(0x89f0f582)), unchecked((int)(0x30cf76d3
			)), unchecked((int)(0xe409c5fe)), unchecked((int)(0x4d84a3a4)), unchecked((int)(
			0x99421089)), unchecked((int)(0x1e9e6f10)), unchecked((int)(0xca58dc3d)), unchecked(
			(int)(0x63d5ba67)), unchecked((int)(0xb713094a)), unchecked((int)(0x6c6d4555)), 
			unchecked((int)(0xb8abf678)), unchecked((int)(0x11269022)), unchecked((int)(0xc5e0230f
			)), unchecked((int)(0x423c5c96)), unchecked((int)(0x96faefbb)), unchecked((int)(
			0x3f7789e1)), unchecked((int)(0xebb13acc)), unchecked((int)(0x5d4da2f2)), unchecked(
			(int)(0x898b11df)), unchecked((int)(0x20067785)), unchecked((int)(0xf4c0c4a8)), 
			unchecked((int)(0x731cbb31)), unchecked((int)(0xa7da081c)), unchecked((int)(0x0e576e46
			)), unchecked((int)(0xda91dd6b)), unchecked((int)(0x01ef9174)), unchecked((int)(
			0xd5292259)), unchecked((int)(0x7ca44403)), unchecked((int)(0xa862f72e)), unchecked(
			(int)(0x2fbe88b7)), unchecked((int)(0xfb783b9a)), unchecked((int)(0x52f55dc0)), 
			unchecked((int)(0x8633eeed)), unchecked((int)(0x208a5b62)), unchecked((int)(0xf44ce84f
			)), unchecked((int)(0x5dc18e15)), unchecked((int)(0x89073d38)), unchecked((int)(
			0x0edb42a1)), unchecked((int)(0xda1df18c)), unchecked((int)(0x739097d6)), unchecked(
			(int)(0xa75624fb)), unchecked((int)(0x7c2868e4)), unchecked((int)(0xa8eedbc9)), 
			unchecked((int)(0x0163bd93)), unchecked((int)(0xd5a50ebe)), unchecked((int)(0x52797127
			)), unchecked((int)(0x86bfc20a)), unchecked((int)(0x2f32a450)), unchecked((int)(
			0xfbf4177d)), unchecked((int)(0x4d088f43)), unchecked((int)(0x99ce3c6e)), unchecked(
			(int)(0x30435a34)), unchecked((int)(0xe485e919)), unchecked((int)(0x63599680)), 
			unchecked((int)(0xb79f25ad)), unchecked((int)(0x1e1243f7)), unchecked((int)(0xcad4f0da
			)), unchecked((int)(0x11aabcc5)), unchecked((int)(0xc56c0fe8)), unchecked((int)(
			0x6ce169b2)), unchecked((int)(0xb827da9f)), unchecked((int)(0x3ffba506)), unchecked(
			(int)(0xeb3d162b)), unchecked((int)(0x42b07071)), unchecked((int)(0x9676c35c)), 
			unchecked((int)(0x2f49400d)), unchecked((int)(0xfb8ff320)), unchecked((int)(0x5202957a
			)), unchecked((int)(0x86c42657)), unchecked((int)(0x011859ce)), unchecked((int)(
			0xd5deeae3)), unchecked((int)(0x7c538cb9)), unchecked((int)(0xa8953f94)), unchecked(
			(int)(0x73eb738b)), unchecked((int)(0xa72dc0a6)), unchecked((int)(0x0ea0a6fc)), 
			unchecked((int)(0xda6615d1)), unchecked((int)(0x5dba6a48)), unchecked((int)(0x897cd965
			)), unchecked((int)(0x20f1bf3f)), unchecked((int)(0xf4370c12)), unchecked((int)(
			0x42cb942c)), unchecked((int)(0x960d2701)), unchecked((int)(0x3f80415b)), unchecked(
			(int)(0xeb46f276)), unchecked((int)(0x6c9a8def)), unchecked((int)(0xb85c3ec2)), 
			unchecked((int)(0x11d15898)), unchecked((int)(0xc517ebb5)), unchecked((int)(0x1e69a7aa
			)), unchecked((int)(0xcaaf1487)), unchecked((int)(0x632272dd)), unchecked((int)(
			0xb7e4c1f0)), unchecked((int)(0x3038be69)), unchecked((int)(0xe4fe0d44)), unchecked(
			(int)(0x4d736b1e)), unchecked((int)(0x99b5d833)) };

		private static readonly int[] U = new int[] { unchecked((int)(0x00000000)), unchecked(
			(int)(0x12c6e90f)), unchecked((int)(0x258dd21e)), unchecked((int)(0x374b3b11)), 
			unchecked((int)(0x4b1ba43c)), unchecked((int)(0x59dd4d33)), unchecked((int)(0x6e967622
			)), unchecked((int)(0x7c509f2d)), unchecked((int)(0x42f1fb55)), unchecked((int)(
			0x5037125a)), unchecked((int)(0x677c294b)), unchecked((int)(0x75bac044)), unchecked(
			(int)(0x09ea5f69)), unchecked((int)(0x1b2cb666)), unchecked((int)(0x2c678d77)), 
			unchecked((int)(0x3ea16478)), unchecked((int)(0x51254587)), unchecked((int)(0x43e3ac88
			)), unchecked((int)(0x74a89799)), unchecked((int)(0x666e7e96)), unchecked((int)(
			0x1a3ee1bb)), unchecked((int)(0x08f808b4)), unchecked((int)(0x3fb333a5)), unchecked(
			(int)(0x2d75daaa)), unchecked((int)(0x13d4bed2)), unchecked((int)(0x011257dd)), 
			unchecked((int)(0x36596ccc)), unchecked((int)(0x249f85c3)), unchecked((int)(0x58cf1aee
			)), unchecked((int)(0x4a09f3e1)), unchecked((int)(0x7d42c8f0)), unchecked((int)(
			0x6f8421ff)), unchecked((int)(0x768c3823)), unchecked((int)(0x644ad12c)), unchecked(
			(int)(0x5301ea3d)), unchecked((int)(0x41c70332)), unchecked((int)(0x3d979c1f)), 
			unchecked((int)(0x2f517510)), unchecked((int)(0x181a4e01)), unchecked((int)(0x0adca70e
			)), unchecked((int)(0x347dc376)), unchecked((int)(0x26bb2a79)), unchecked((int)(
			0x11f01168)), unchecked((int)(0x0336f867)), unchecked((int)(0x7f66674a)), unchecked(
			(int)(0x6da08e45)), unchecked((int)(0x5aebb554)), unchecked((int)(0x482d5c5b)), 
			unchecked((int)(0x27a97da4)), unchecked((int)(0x356f94ab)), unchecked((int)(0x0224afba
			)), unchecked((int)(0x10e246b5)), unchecked((int)(0x6cb2d998)), unchecked((int)(
			0x7e743097)), unchecked((int)(0x493f0b86)), unchecked((int)(0x5bf9e289)), unchecked(
			(int)(0x655886f1)), unchecked((int)(0x779e6ffe)), unchecked((int)(0x40d554ef)), 
			unchecked((int)(0x5213bde0)), unchecked((int)(0x2e4322cd)), unchecked((int)(0x3c85cbc2
			)), unchecked((int)(0x0bcef0d3)), unchecked((int)(0x190819dc)), unchecked((int)(
			0x39dec36b)), unchecked((int)(0x2b182a64)), unchecked((int)(0x1c531175)), unchecked(
			(int)(0x0e95f87a)), unchecked((int)(0x72c56757)), unchecked((int)(0x60038e58)), 
			unchecked((int)(0x5748b549)), unchecked((int)(0x458e5c46)), unchecked((int)(0x7b2f383e
			)), unchecked((int)(0x69e9d131)), unchecked((int)(0x5ea2ea20)), unchecked((int)(
			0x4c64032f)), unchecked((int)(0x30349c02)), unchecked((int)(0x22f2750d)), unchecked(
			(int)(0x15b94e1c)), unchecked((int)(0x077fa713)), unchecked((int)(0x68fb86ec)), 
			unchecked((int)(0x7a3d6fe3)), unchecked((int)(0x4d7654f2)), unchecked((int)(0x5fb0bdfd
			)), unchecked((int)(0x23e022d0)), unchecked((int)(0x3126cbdf)), unchecked((int)(
			0x066df0ce)), unchecked((int)(0x14ab19c1)), unchecked((int)(0x2a0a7db9)), unchecked(
			(int)(0x38cc94b6)), unchecked((int)(0x0f87afa7)), unchecked((int)(0x1d4146a8)), 
			unchecked((int)(0x6111d985)), unchecked((int)(0x73d7308a)), unchecked((int)(0x449c0b9b
			)), unchecked((int)(0x565ae294)), unchecked((int)(0x4f52fb48)), unchecked((int)(
			0x5d941247)), unchecked((int)(0x6adf2956)), unchecked((int)(0x7819c059)), unchecked(
			(int)(0x04495f74)), unchecked((int)(0x168fb67b)), unchecked((int)(0x21c48d6a)), 
			unchecked((int)(0x33026465)), unchecked((int)(0x0da3001d)), unchecked((int)(0x1f65e912
			)), unchecked((int)(0x282ed203)), unchecked((int)(0x3ae83b0c)), unchecked((int)(
			0x46b8a421)), unchecked((int)(0x547e4d2e)), unchecked((int)(0x6335763f)), unchecked(
			(int)(0x71f39f30)), unchecked((int)(0x1e77becf)), unchecked((int)(0x0cb157c0)), 
			unchecked((int)(0x3bfa6cd1)), unchecked((int)(0x293c85de)), unchecked((int)(0x556c1af3
			)), unchecked((int)(0x47aaf3fc)), unchecked((int)(0x70e1c8ed)), unchecked((int)(
			0x622721e2)), unchecked((int)(0x5c86459a)), unchecked((int)(0x4e40ac95)), unchecked(
			(int)(0x790b9784)), unchecked((int)(0x6bcd7e8b)), unchecked((int)(0x179de1a6)), 
			unchecked((int)(0x055b08a9)), unchecked((int)(0x321033b8)), unchecked((int)(0x20d6dab7
			)), unchecked((int)(0x73bd86d6)), unchecked((int)(0x617b6fd9)), unchecked((int)(
			0x563054c8)), unchecked((int)(0x44f6bdc7)), unchecked((int)(0x38a622ea)), unchecked(
			(int)(0x2a60cbe5)), unchecked((int)(0x1d2bf0f4)), unchecked((int)(0x0fed19fb)), 
			unchecked((int)(0x314c7d83)), unchecked((int)(0x238a948c)), unchecked((int)(0x14c1af9d
			)), unchecked((int)(0x06074692)), unchecked((int)(0x7a57d9bf)), unchecked((int)(
			0x689130b0)), unchecked((int)(0x5fda0ba1)), unchecked((int)(0x4d1ce2ae)), unchecked(
			(int)(0x2298c351)), unchecked((int)(0x305e2a5e)), unchecked((int)(0x0715114f)), 
			unchecked((int)(0x15d3f840)), unchecked((int)(0x6983676d)), unchecked((int)(0x7b458e62
			)), unchecked((int)(0x4c0eb573)), unchecked((int)(0x5ec85c7c)), unchecked((int)(
			0x60693804)), unchecked((int)(0x72afd10b)), unchecked((int)(0x45e4ea1a)), unchecked(
			(int)(0x57220315)), unchecked((int)(0x2b729c38)), unchecked((int)(0x39b47537)), 
			unchecked((int)(0x0eff4e26)), unchecked((int)(0x1c39a729)), unchecked((int)(0x0531bef5
			)), unchecked((int)(0x17f757fa)), unchecked((int)(0x20bc6ceb)), unchecked((int)(
			0x327a85e4)), unchecked((int)(0x4e2a1ac9)), unchecked((int)(0x5cecf3c6)), unchecked(
			(int)(0x6ba7c8d7)), unchecked((int)(0x796121d8)), unchecked((int)(0x47c045a0)), 
			unchecked((int)(0x5506acaf)), unchecked((int)(0x624d97be)), unchecked((int)(0x708b7eb1
			)), unchecked((int)(0x0cdbe19c)), unchecked((int)(0x1e1d0893)), unchecked((int)(
			0x29563382)), unchecked((int)(0x3b90da8d)), unchecked((int)(0x5414fb72)), unchecked(
			(int)(0x46d2127d)), unchecked((int)(0x7199296c)), unchecked((int)(0x635fc063)), 
			unchecked((int)(0x1f0f5f4e)), unchecked((int)(0x0dc9b641)), unchecked((int)(0x3a828d50
			)), unchecked((int)(0x2844645f)), unchecked((int)(0x16e50027)), unchecked((int)(
			0x0423e928)), unchecked((int)(0x3368d239)), unchecked((int)(0x21ae3b36)), unchecked(
			(int)(0x5dfea41b)), unchecked((int)(0x4f384d14)), unchecked((int)(0x78737605)), 
			unchecked((int)(0x6ab59f0a)), unchecked((int)(0x4a6345bd)), unchecked((int)(0x58a5acb2
			)), unchecked((int)(0x6fee97a3)), unchecked((int)(0x7d287eac)), unchecked((int)(
			0x0178e181)), unchecked((int)(0x13be088e)), unchecked((int)(0x24f5339f)), unchecked(
			(int)(0x3633da90)), unchecked((int)(0x0892bee8)), unchecked((int)(0x1a5457e7)), 
			unchecked((int)(0x2d1f6cf6)), unchecked((int)(0x3fd985f9)), unchecked((int)(0x43891ad4
			)), unchecked((int)(0x514ff3db)), unchecked((int)(0x6604c8ca)), unchecked((int)(
			0x74c221c5)), unchecked((int)(0x1b46003a)), unchecked((int)(0x0980e935)), unchecked(
			(int)(0x3ecbd224)), unchecked((int)(0x2c0d3b2b)), unchecked((int)(0x505da406)), 
			unchecked((int)(0x429b4d09)), unchecked((int)(0x75d07618)), unchecked((int)(0x67169f17
			)), unchecked((int)(0x59b7fb6f)), unchecked((int)(0x4b711260)), unchecked((int)(
			0x7c3a2971)), unchecked((int)(0x6efcc07e)), unchecked((int)(0x12ac5f53)), unchecked(
			(int)(0x006ab65c)), unchecked((int)(0x37218d4d)), unchecked((int)(0x25e76442)), 
			unchecked((int)(0x3cef7d9e)), unchecked((int)(0x2e299491)), unchecked((int)(0x1962af80
			)), unchecked((int)(0x0ba4468f)), unchecked((int)(0x77f4d9a2)), unchecked((int)(
			0x653230ad)), unchecked((int)(0x52790bbc)), unchecked((int)(0x40bfe2b3)), unchecked(
			(int)(0x7e1e86cb)), unchecked((int)(0x6cd86fc4)), unchecked((int)(0x5b9354d5)), 
			unchecked((int)(0x4955bdda)), unchecked((int)(0x350522f7)), unchecked((int)(0x27c3cbf8
			)), unchecked((int)(0x1088f0e9)), unchecked((int)(0x024e19e6)), unchecked((int)(
			0x6dca3819)), unchecked((int)(0x7f0cd116)), unchecked((int)(0x4847ea07)), unchecked(
			(int)(0x5a810308)), unchecked((int)(0x26d19c25)), unchecked((int)(0x3417752a)), 
			unchecked((int)(0x035c4e3b)), unchecked((int)(0x119aa734)), unchecked((int)(0x2f3bc34c
			)), unchecked((int)(0x3dfd2a43)), unchecked((int)(0x0ab61152)), unchecked((int)(
			0x1870f85d)), unchecked((int)(0x64206770)), unchecked((int)(0x76e68e7f)), unchecked(
			(int)(0x41adb56e)), unchecked((int)(0x536b5c61)) };
	}
}
