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

using NGit.Diff;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>
	/// Supports
	/// <see cref="PatienceDiff">PatienceDiff</see>
	/// by finding unique but common elements.
	/// This index object is constructed once for each region being considered by the
	/// main
	/// <see cref="PatienceDiff">PatienceDiff</see>
	/// algorithm, which really means its once for each
	/// recursive step. Each index instance processes a fixed sized region from the
	/// sequences, and during recursion the region is split into two smaller segments
	/// and processed again.
	/// Index instances from a higher level invocation message some state into a
	/// lower level invocation by passing the
	/// <see cref="PatienceDiffIndex{S}.nCommon">PatienceDiffIndex&lt;S&gt;.nCommon</see>
	/// array from the higher
	/// invocation into the two sub-steps as
	/// <see cref="PatienceDiffIndex{S}.pCommon">PatienceDiffIndex&lt;S&gt;.pCommon</see>
	/// . This permits some
	/// matching work that was already done in the higher invocation to be reused in
	/// the sub-step and can save a lot of time when element equality is expensive.
	/// </summary>
	/// <?></?>
	internal sealed class PatienceDiffIndex<S> where S:Sequence
	{
		private const int A_DUPLICATE = 1;

		private const int B_DUPLICATE = 2;

		private const int DUPLICATE_MASK = B_DUPLICATE | A_DUPLICATE;

		private const int A_SHIFT = 2;

		private const int B_SHIFT = 31 + 2;

		private const int PTR_MASK = unchecked((int)(0x7fffffff));

		private readonly HashedSequenceComparator<S> cmp;

		private readonly HashedSequence<S> a;

		private readonly HashedSequence<S> b;

		private readonly Edit region;

		/// <summary>Pairs of beginB, endB indices previously found to be common and unique.</summary>
		/// <remarks>Pairs of beginB, endB indices previously found to be common and unique.</remarks>
		private readonly long[] pCommon;

		/// <summary>
		/// First valid index in
		/// <see cref="PatienceDiffIndex{S}.pCommon">PatienceDiffIndex&lt;S&gt;.pCommon</see>
		/// .
		/// </summary>
		private readonly int pBegin;

		/// <summary>
		/// 1 past the last valid entry in
		/// <see cref="PatienceDiffIndex{S}.pCommon">PatienceDiffIndex&lt;S&gt;.pCommon</see>
		/// .
		/// </summary>
		private readonly int pEnd;

		/// <summary>
		/// Keyed by
		/// <see cref="PatienceDiffIndex{S}.Hash(HashedSequence{S}, int)">PatienceDiffIndex&lt;S&gt;.Hash(HashedSequence&lt;S&gt;, int)
		/// 	</see>
		/// to get an entry offset.
		/// </summary>
		private readonly int[] table;

		/// <summary>
		/// Number of low bits to discard from a key to index
		/// <see cref="PatienceDiffIndex{S}.table">PatienceDiffIndex&lt;S&gt;.table</see>
		/// .
		/// </summary>
		private readonly int keyShift;

		/// <summary>A matched (or partially examined) element from the two sequences.</summary>
		/// <remarks>
		/// A matched (or partially examined) element from the two sequences.
		/// This is actually a 4-tuple: (bPtr, aPtrP1, bDuplicate, aDuplicate).
		/// bPtr and aPtr are each 31 bits. bPtr is exactly the position in the b
		/// sequence, while aPtrP1 is
		/// <code>aPtr + 1</code>
		/// . This permits us to determine
		/// if there is corresponding element in a by testing for aPtrP1 != 0. If it
		/// equals 0, there is no element in a. If it equals 1, element 0 of a
		/// matches with element bPtr of b.
		/// bDuplicate is 1 if this element occurs more than once in b; likewise
		/// aDuplicate is 1 if this element occurs more than once in a. These flags
		/// permit each element to only be added to the index once. As the duplicates
		/// are the low 2 bits a unique record meets (@code (rec & 2) == 0}.
		/// </remarks>
		private readonly long[] ptrs;

		/// <summary>Array index of the next entry in the table; 0 if at end of chain.</summary>
		/// <remarks>Array index of the next entry in the table; 0 if at end of chain.</remarks>
		private readonly int[] next;

		/// <summary>
		/// Total number of entries that exist in
		/// <see cref="PatienceDiffIndex{S}.ptrs">PatienceDiffIndex&lt;S&gt;.ptrs</see>
		/// .
		/// </summary>
		private int entryCnt;

		/// <summary>
		/// Number of entries in
		/// <see cref="PatienceDiffIndex{S}.ptrs">PatienceDiffIndex&lt;S&gt;.ptrs</see>
		/// that are actually unique.
		/// </summary>
		private int uniqueCommonCnt;

		/// <summary>Pairs of beginB, endB indices found to be common and unique.</summary>
		/// <remarks>
		/// Pairs of beginB, endB indices found to be common and unique.
		/// In order to find the longest common (but unique) sequence within a
		/// region, we also found all of the other common but unique sequences in
		/// that same region. This array stores all of those results, allowing them
		/// to be passed into the subsequent recursive passes so we can later reuse
		/// these matches and avoid recomputing the same points again.
		/// </remarks>
		internal long[] nCommon;

		/// <summary>
		/// Number of items in
		/// <see cref="PatienceDiffIndex{S}.nCommon">PatienceDiffIndex&lt;S&gt;.nCommon</see>
		/// .
		/// </summary>
		internal int nCnt;

		/// <summary>
		/// Index of the longest common subsequence in
		/// <see cref="PatienceDiffIndex{S}.nCommon">PatienceDiffIndex&lt;S&gt;.nCommon</see>
		/// .
		/// </summary>
		internal int cIdx;

		internal PatienceDiffIndex(HashedSequenceComparator<S> cmp, HashedSequence<S> a, 
			HashedSequence<S> b, Edit region, long[] pCommon, int pIdx, int pCnt)
		{
			// To save memory the buckets for hash chains are stored in correlated
			// arrays. This permits us to get 3 values per entry, without paying
			// the penalty for an object header on each entry.
			//
			//
			//
			//
			this.cmp = cmp;
			this.a = a;
			this.b = b;
			this.region = region;
			this.pCommon = pCommon;
			this.pBegin = pIdx;
			this.pEnd = pCnt;
			int sz = region.GetLengthB();
			int tableBits = TableBits(sz);
			table = new int[1 << tableBits];
			keyShift = 32 - tableBits;
			// As we insert elements we preincrement so that 0 is never a
			// valid entry. Therefore we have to allocate one extra space.
			//
			ptrs = new long[1 + sz];
			next = new int[ptrs.Length];
		}

		/// <summary>Index elements in sequence B for later matching with sequence A.</summary>
		/// <remarks>
		/// Index elements in sequence B for later matching with sequence A.
		/// This is the first stage of preparing an index to find the longest common
		/// sequence. Elements of sequence B in the range [ptr, end) are scanned in
		/// order and added to the internal hashtable.
		/// If prior matches were given in the constructor, these may be used to
		/// fast-forward through sections of B to avoid unnecessary recomputation.
		/// </remarks>
		private void ScanB()
		{
			// We insert in ascending order so that a later scan of the table
			// from 0 through entryCnt will iterate through B in order. This
			// is the desired result ordering from match().
			//
			int ptr = region.beginB;
			int end = region.endB;
			int pIdx = pBegin;
			while (ptr < end)
			{
				int tIdx = Hash(b, ptr);
				if (pIdx < pEnd)
				{
					long priorRec = pCommon[pIdx];
					if (ptr == BOf(priorRec))
					{
						// We know this region is unique from a prior pass.
						// Insert the start point, and skip right to the end.
						//
						InsertB(tIdx, ptr);
						pIdx++;
						ptr = AOfRaw(priorRec);
						goto SCAN_continue;
					}
				}
				// We aren't sure what the status of this element is. Add
				// it to our hashtable, and flag it as duplicate if there
				// was already a different entry present.
				//
				for (int eIdx = table[tIdx]; eIdx != 0; eIdx = next[eIdx])
				{
					long rec = ptrs[eIdx];
					if (cmp.Equals(b, ptr, b, BOf(rec)))
					{
						ptrs[eIdx] = rec | B_DUPLICATE;
						ptr++;
						goto SCAN_continue;
					}
				}
				InsertB(tIdx, ptr);
				ptr++;
SCAN_continue: ;
			}
SCAN_break: ;
		}

		private void InsertB(int tIdx, int ptr)
		{
			int eIdx = ++entryCnt;
			ptrs[eIdx] = ((long)ptr) << B_SHIFT;
			next[eIdx] = table[tIdx];
			table[tIdx] = eIdx;
		}

		/// <summary>Index elements in sequence A for later matching.</summary>
		/// <remarks>
		/// Index elements in sequence A for later matching.
		/// This is the second stage of preparing an index to find the longest common
		/// sequence. The state requires
		/// <see cref="PatienceDiffIndex{S}.ScanB()">PatienceDiffIndex&lt;S&gt;.ScanB()</see>
		/// to have been invoked first.
		/// Each element of A in the range [ptr, end) are searched for in the
		/// internal hashtable, to see if B has already registered a location.
		/// If prior matches were given in the constructor, these may be used to
		/// fast-forward through sections of A to avoid unnecessary recomputation.
		/// </remarks>
		private void ScanA()
		{
			int ptr = region.beginA;
			int end = region.endA;
			int pLast = pBegin;
			while (ptr < end)
			{
				int tIdx = Hash(a, ptr);
				for (int eIdx = table[tIdx]; eIdx != 0; eIdx = next[eIdx])
				{
					long rec = ptrs[eIdx];
					int bs = BOf(rec);
					if (IsDuplicate(rec) || !cmp.Equals(a, ptr, b, bs))
					{
						continue;
					}
					int aPtr = AOfRaw(rec);
					if (aPtr != 0 && cmp.Equals(a, ptr, a, aPtr - 1))
					{
						ptrs[eIdx] = rec | A_DUPLICATE;
						uniqueCommonCnt--;
						ptr++;
						goto SCAN_continue;
					}
					// This element is both common and unique. Link the
					// two sequences together at this point.
					//
					ptrs[eIdx] = rec | (((long)(ptr + 1)) << A_SHIFT);
					uniqueCommonCnt++;
					if (pBegin < pEnd)
					{
						// If we have prior match point data, we might be able
						// to locate the length of the match and skip past all
						// of those elements. We try to take advantage of the
						// fact that pCommon is sorted by B, and its likely that
						// matches in A appear in the same order as they do in B.
						//
						for (int pIdx = pLast; ; )
						{
							long priorRec = pCommon[pIdx];
							int priorB = BOf(priorRec);
							if (bs < priorB)
							{
								break;
							}
							if (bs == priorB)
							{
								ptr += AOfRaw(priorRec) - priorB;
								pLast = pIdx;
								goto SCAN_continue;
							}
							pIdx++;
							if (pIdx == pEnd)
							{
								pIdx = pBegin;
							}
							if (pIdx == pLast)
							{
								break;
							}
						}
					}
					ptr++;
					goto SCAN_continue;
				}
				ptr++;
SCAN_continue: ;
			}
SCAN_break: ;
		}

		/// <summary>Scan all potential matches and find the longest common sequence.</summary>
		/// <remarks>
		/// Scan all potential matches and find the longest common sequence.
		/// If this method returns non-null, the caller should copy out the
		/// <see cref="PatienceDiffIndex{S}.nCommon">PatienceDiffIndex&lt;S&gt;.nCommon</see>
		/// array and pass that through to the recursive sub-steps
		/// so that existing common matches can be reused rather than recomputed.
		/// </remarks>
		/// <returns>
		/// an edit covering the longest common sequence. Null if there are
		/// no common unique sequences present.
		/// </returns>
		internal Edit FindLongestCommonSequence()
		{
			ScanB();
			ScanA();
			if (uniqueCommonCnt == 0)
			{
				return null;
			}
			nCommon = new long[uniqueCommonCnt];
			int pIdx = pBegin;
			Edit lcs = new Edit(0, 0);
			for (int eIdx = 1; eIdx <= entryCnt; eIdx++)
			{
				long rec = ptrs[eIdx];
				if (IsDuplicate(rec) || AOfRaw(rec) == 0)
				{
					continue;
				}
				int bs = BOf(rec);
				if (bs < lcs.endB)
				{
					continue;
				}
				int @as = AOf(rec);
				if (pIdx < pEnd)
				{
					long priorRec = pCommon[pIdx];
					if (bs == BOf(priorRec))
					{
						// We had a prior match and we know its unique.
						// Reuse its region rather than computing again.
						//
						int be = AOfRaw(priorRec);
						if (lcs.GetLengthB() < be - bs)
						{
							@as -= BOf(rec) - bs;
							lcs.beginA = @as;
							lcs.beginB = bs;
							lcs.endA = @as + (be - bs);
							lcs.endB = be;
							cIdx = nCnt;
						}
						nCommon[nCnt] = priorRec;
						if (++nCnt == uniqueCommonCnt)
						{
							goto MATCH_break;
						}
						pIdx++;
						goto MATCH_continue;
					}
				}
				// We didn't have prior match data, or this is the first time
				// seeing this particular pair. Extend the region as large as
				// possible and remember it for future use.
				//
				int ae = @as + 1;
				int be_1 = bs + 1;
				while (region.beginA < @as && region.beginB < bs && cmp.Equals(a, @as - 1, b, bs 
					- 1))
				{
					@as--;
					bs--;
				}
				while (ae < region.endA && be_1 < region.endB && cmp.Equals(a, ae, b, be_1))
				{
					ae++;
					be_1++;
				}
				if (lcs.GetLengthB() < be_1 - bs)
				{
					lcs.beginA = @as;
					lcs.beginB = bs;
					lcs.endA = ae;
					lcs.endB = be_1;
					cIdx = nCnt;
				}
				nCommon[nCnt] = (((long)bs) << B_SHIFT) | (((long)be_1) << A_SHIFT);
				if (++nCnt == uniqueCommonCnt)
				{
					goto MATCH_break;
				}
MATCH_continue: ;
			}
MATCH_break: ;
			return lcs;
		}

		private int Hash(HashedSequence<S> s, int idx)
		{
			return (int)(((uint)(cmp.Hash(s, idx) * unchecked((int)(0x9e370001)))) >> keyShift
				);
		}

		private static bool IsDuplicate(long rec)
		{
			return (((int)rec) & DUPLICATE_MASK) != 0;
		}

		private static int AOfRaw(long rec)
		{
			return ((int)((long)(((ulong)rec) >> A_SHIFT))) & PTR_MASK;
		}

		private static int AOf(long rec)
		{
			return AOfRaw(rec) - 1;
		}

		private static int BOf(long rec)
		{
			return (int)((long)(((ulong)rec) >> B_SHIFT));
		}

		private static int TableBits(int sz)
		{
			int bits = 31 - Sharpen.Extensions.NumberOfLeadingZeros(sz);
			if (bits == 0)
			{
				bits = 1;
			}
			if (1 << bits < sz)
			{
				bits++;
			}
			return bits;
		}
	}
}
