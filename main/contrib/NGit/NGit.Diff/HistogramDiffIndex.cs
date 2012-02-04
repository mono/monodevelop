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
	/// <summary>
	/// Support
	/// <see cref="HistogramDiff">HistogramDiff</see>
	/// by computing occurrence counts of elements.
	/// Each element in the range being considered is put into a hash table, tracking
	/// the number of times that distinct element appears in the sequence. Once all
	/// elements have been inserted from sequence A, each element of sequence B is
	/// probed in the hash table and the longest common subsequence with the lowest
	/// occurrence count in A is used as the result.
	/// </summary>
	/// <?></?>
	internal sealed class HistogramDiffIndex<S> where S:Sequence
	{
		private const int REC_NEXT_SHIFT = 28 + 8;

		private const int REC_PTR_SHIFT = 8;

		private const int REC_PTR_MASK = (1 << 28) - 1;

		private const int REC_CNT_MASK = (1 << 8) - 1;

		private const int MAX_PTR = REC_PTR_MASK;

		private const int MAX_CNT = (1 << 8) - 1;

		private readonly int maxChainLength;

		private readonly HashedSequenceComparator<S> cmp;

		private readonly HashedSequence<S> a;

		private readonly HashedSequence<S> b;

		private readonly Edit region;

		/// <summary>
		/// Keyed by
		/// <see cref="HistogramDiffIndex{S}.Hash(HashedSequence{S}, int)">HistogramDiffIndex&lt;S&gt;.Hash(HashedSequence&lt;S&gt;, int)
		/// 	</see>
		/// for
		/// <see cref="HistogramDiffIndex{S}.recs">HistogramDiffIndex&lt;S&gt;.recs</see>
		/// index.
		/// </summary>
		private readonly int[] table;

		/// <summary>
		/// Number of low bits to discard from a key to index
		/// <see cref="HistogramDiffIndex{S}.table">HistogramDiffIndex&lt;S&gt;.table</see>
		/// .
		/// </summary>
		private readonly int keyShift;

		/// <summary>Describes a unique element in sequence A.</summary>
		/// <remarks>
		/// Describes a unique element in sequence A.
		/// The records in this table are actually 3-tuples of:
		/// <ul>
		/// <li>index of next record in this table that has same hash code</li>
		/// <li>index of first element in this occurrence chain</li>
		/// <li>occurrence count for this element (length of locs list)</li>
		/// </ul>
		/// The occurrence count is capped at
		/// <see cref="HistogramDiffIndex{S}.MAX_CNT">HistogramDiffIndex&lt;S&gt;.MAX_CNT</see>
		/// , as the field is only
		/// a few bits wide. Elements that occur more frequently will have their
		/// count capped.
		/// </remarks>
		private long[] recs;

		/// <summary>
		/// Number of elements in
		/// <see cref="HistogramDiffIndex{S}.recs">HistogramDiffIndex&lt;S&gt;.recs</see>
		/// ; also is the unique element count.
		/// </summary>
		private int recCnt;

		/// <summary>
		/// For
		/// <code>ptr</code>
		/// ,
		/// <code>next[ptr - ptrShift]</code>
		/// has subsequent index.
		/// For the sequence element
		/// <code>ptr</code>
		/// , the value stored at location
		/// <code>next[ptr - ptrShift]</code>
		/// is the next occurrence of the exact same
		/// element in the sequence.
		/// Chains always run from the lowest index to the largest index. Therefore
		/// the array will store
		/// <code>next[1] = 2</code>
		/// , but never
		/// <code>next[2] = 1</code>
		/// .
		/// This allows a chain to terminate with
		/// <code>0</code>
		/// , as
		/// <code>0</code>
		/// would never
		/// be a valid next element.
		/// The array is sized to be
		/// <code>region.getLengthA()</code>
		/// and element indexes
		/// are converted to array indexes by subtracting
		/// <see cref="HistogramDiffIndex{S}.ptrShift">HistogramDiffIndex&lt;S&gt;.ptrShift</see>
		/// , which is
		/// just a cached version of
		/// <code>region.beginA</code>
		/// .
		/// </summary>
		private int[] next;

		/// <summary>
		/// For element
		/// <code>ptr</code>
		/// in A, index of the record in
		/// <see cref="HistogramDiffIndex{S}.recs">HistogramDiffIndex&lt;S&gt;.recs</see>
		/// array.
		/// The record at
		/// <code>recs[recIdx[ptr - ptrShift]]</code>
		/// is the record
		/// describing all occurrences of the element appearing in sequence A at
		/// position
		/// <code>ptr</code>
		/// . The record is needed to get the occurrence count of
		/// the element, or to locate all other occurrences of that element within
		/// sequence A. This index provides constant-time access to the record, and
		/// avoids needing to scan the hash chain.
		/// </summary>
		private int[] recIdx;

		/// <summary>
		/// Value to subtract from element indexes to key
		/// <see cref="HistogramDiffIndex{S}.next">HistogramDiffIndex&lt;S&gt;.next</see>
		/// array.
		/// </summary>
		private int ptrShift;

		private Edit lcs;

		private int cnt;

		private bool hasCommon;

		internal HistogramDiffIndex(int maxChainLength, HashedSequenceComparator<S> cmp, 
			HashedSequence<S> a, HashedSequence<S> b, Edit r)
		{
			this.maxChainLength = maxChainLength;
			this.cmp = cmp;
			this.a = a;
			this.b = b;
			this.region = r;
			if (region.endA >= MAX_PTR)
			{
				throw new ArgumentException(JGitText.Get().sequenceTooLargeForDiffAlgorithm);
			}
			int sz = r.GetLengthA();
			int tableBits = TableBits(sz);
			table = new int[1 << tableBits];
			keyShift = 32 - tableBits;
			ptrShift = r.beginA;
			recs = new long[Math.Max(4, (int)(((uint)sz) >> 3))];
			next = new int[sz];
			recIdx = new int[sz];
		}

		internal Edit FindLongestCommonSequence()
		{
			if (!ScanA())
			{
				return null;
			}
			lcs = new Edit(0, 0);
			cnt = maxChainLength + 1;
			for (int bPtr = region.beginB; bPtr < region.endB; )
			{
				bPtr = TryLongestCommonSequence(bPtr);
			}
			return hasCommon && maxChainLength < cnt ? null : lcs;
		}

		private bool ScanA()
		{
			// Scan the elements backwards, inserting them into the hash table
			// as we go. Going in reverse places the earliest occurrence of any
			// element at the start of the chain, so we consider earlier matches
			// before later matches.
			//
			for (int ptr = region.endA - 1; region.beginA <= ptr; ptr--)
			{
				int tIdx = Hash(a, ptr);
				int chainLen = 0;
				for (int rIdx = table[tIdx]; rIdx != 0; )
				{
					long rec = recs[rIdx];
					if (cmp.Equals(a, RecPtr(rec), a, ptr))
					{
						// ptr is identical to another element. Insert it onto
						// the front of the existing element chain.
						//
						int newCnt = RecCnt(rec) + 1;
						if (MAX_CNT < newCnt)
						{
							newCnt = MAX_CNT;
						}
						recs[rIdx] = RecCreate(RecNext(rec), ptr, newCnt);
						next[ptr - ptrShift] = RecPtr(rec);
						recIdx[ptr - ptrShift] = rIdx;
						goto SCAN_continue;
					}
					rIdx = RecNext(rec);
					chainLen++;
				}
				if (chainLen == maxChainLength)
				{
					return false;
				}
				// This is the first time we have ever seen this particular
				// element in the sequence. Construct a new chain for it.
				//
				int rIdx_1 = ++recCnt;
				if (rIdx_1 == recs.Length)
				{
					int sz = Math.Min(recs.Length << 1, 1 + region.GetLengthA());
					long[] n = new long[sz];
					System.Array.Copy(recs, 0, n, 0, recs.Length);
					recs = n;
				}
				recs[rIdx_1] = RecCreate(table[tIdx], ptr, 1);
				recIdx[ptr - ptrShift] = rIdx_1;
				table[tIdx] = rIdx_1;
SCAN_continue: ;
			}
SCAN_break: ;
			return true;
		}

		private int TryLongestCommonSequence(int bPtr)
		{
			int bNext = bPtr + 1;
			int rIdx = table[Hash(b, bPtr)];
			for (long rec; rIdx != 0; rIdx = RecNext(rec))
			{
				rec = recs[rIdx];
				// If there are more occurrences in A, don't use this chain.
				if (RecCnt(rec) > cnt)
				{
					if (!hasCommon)
					{
						hasCommon = cmp.Equals(a, RecPtr(rec), b, bPtr);
					}
					continue;
				}
				int @as = RecPtr(rec);
				if (!cmp.Equals(a, @as, b, bPtr))
				{
					continue;
				}
				hasCommon = true;
				for (; ; )
				{
					int np = next[@as - ptrShift];
					int bs = bPtr;
					int ae = @as + 1;
					int be = bs + 1;
					int rc = RecCnt(rec);
					while (region.beginA < @as && region.beginB < bs && cmp.Equals(a, @as - 1, b, bs 
						- 1))
					{
						@as--;
						bs--;
						if (1 < rc)
						{
							rc = Math.Min(rc, RecCnt(recs[recIdx[@as - ptrShift]]));
						}
					}
					while (ae < region.endA && be < region.endB && cmp.Equals(a, ae, b, be))
					{
						if (1 < rc)
						{
							rc = Math.Min(rc, RecCnt(recs[recIdx[ae - ptrShift]]));
						}
						ae++;
						be++;
					}
					if (bNext < be)
					{
						bNext = be;
					}
					if (lcs.GetLengthA() < ae - @as || rc < cnt)
					{
						// If this region is the longest, or there are less
						// occurrences of it in A, its now our LCS.
						//
						lcs.beginA = @as;
						lcs.beginB = bs;
						lcs.endA = ae;
						lcs.endB = be;
						cnt = rc;
					}
					// Because we added elements in reverse order index 0
					// cannot possibly be the next position. Its the first
					// element of the sequence and thus would have been the
					// value of as at the start of the TRY_LOCATIONS loop.
					//
					if (np == 0)
					{
						goto TRY_LOCATIONS_break;
					}
					while (np < ae)
					{
						// The next location to consider was actually within
						// the LCS we examined above. Don't reconsider it.
						//
						np = next[np - ptrShift];
						if (np == 0)
						{
							goto TRY_LOCATIONS_break;
						}
					}
					@as = np;
TRY_LOCATIONS_continue: ;
				}
TRY_LOCATIONS_break: ;
			}
			return bNext;
		}

		private int Hash(HashedSequence<S> s, int idx)
		{
			return (int)(((uint)(cmp.Hash(s, idx) * unchecked((int)(0x9e370001)))) >> keyShift
				);
		}

		private static long RecCreate(int next, int ptr, int cnt)
		{
			return ((long)next << REC_NEXT_SHIFT) | ((long)ptr << REC_PTR_SHIFT) | cnt;
		}

		//
		//
		private static int RecNext(long rec)
		{
			return (int)((long)(((ulong)rec) >> REC_NEXT_SHIFT));
		}

		private static int RecPtr(long rec)
		{
			return ((int)((long)(((ulong)rec) >> REC_PTR_SHIFT))) & REC_PTR_MASK;
		}

		private static int RecCnt(long rec)
		{
			return ((int)rec) & REC_CNT_MASK;
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
