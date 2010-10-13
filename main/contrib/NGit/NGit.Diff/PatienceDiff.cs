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
using NGit.Diff;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>An implementation of the patience difference algorithm.</summary>
	/// <remarks>
	/// An implementation of the patience difference algorithm.
	/// This implementation was derived by using the 4 rules that are outlined in
	/// Bram Cohen's <a href="http://bramcohen.livejournal.com/73318.html">blog</a>.
	/// Because this algorithm requires finding a unique common point to center the
	/// longest common subsequence around, input sequences which have no unique
	/// elements create a degenerate Edit that simply replaces all of one sequence
	/// with all of the other sequence. For many source code files and other human
	/// maintained text, this isn't likely to occur. When it does occur, it can be
	/// easier to read the resulting large-scale replace than to navigate through a
	/// lot of slices of common-but-not-unique lines, like curly braces on lone
	/// lines, or XML close tags. Consequently this algorithm is willing to create a
	/// degenerate Edit in the worst case, in exchange for what may still be
	/// perceived to be an easier to read patch script.
	/// In a nutshell, the implementation defines an Edit that replaces all of
	/// sequence
	/// <code>a</code>
	/// with all of
	/// <code>b</code>
	/// . This Edit is reduced and/or split
	/// to remove common elements, until only Edits spanning non-common elements
	/// remain. Those
	/// <see cref="Edit">Edit</see>
	/// s are the differences.
	/// A slightly more detailed description of the implementation is:
	/// <ol>
	/// <li>Define an Edit that spans the entire two sequences. This edit replaces
	/// all of
	/// <code>a</code>
	/// with all of
	/// <code>b</code>
	/// .</li>
	/// <li>Shrink the Edit by shifting the starting points later in the sequence to
	/// skip over any elements that are common between
	/// <code>a</code>
	/// and
	/// <code>b</code>
	/// .
	/// Likewise shift the ending points earlier in the sequence to skip any trailing
	/// elements that are common. The first and last element of the edit are now not
	/// common, however there may be common content within the interior of the Edit
	/// that hasn't been discovered yet.</li>
	/// <li>Find unique elements within the Edit region that are in both sequences.
	/// This is currently accomplished by hashing the elements and merging them
	/// through a custom hash table in
	/// <see cref="PatienceDiffIndex{S}">PatienceDiffIndex&lt;S&gt;</see>
	/// .</li>
	/// <li>Order the common unique elements by their position within
	/// <code>b</code>
	/// .</li>
	/// <li>For each unique element, stretch an Edit around it in both directions,
	/// consuming neighboring elements that are common to both sequences. Select the
	/// longest such Edit out of the unique element list. During this stretching,
	/// some subsequent unique elements may be consumed into an earlier's common
	/// Edit. This means not all unique elements are evaluated.</li>
	/// <li>Split the Edit region at the longest common edit. Because step 2 shrank
	/// the initial region, there must be at least one element before, and at least
	/// one element after the split.</li>
	/// <li>Recurse on the before and after split points, starting from step 3. Step
	/// 2 doesn't need to be done again because any common part was already removed
	/// by the prior step 2 or 5.</li>
	/// </ol>
	/// </remarks>
	public class PatienceDiff : DiffAlgorithm
	{
		/// <summary>Algorithm we use when there are no common unique lines in a region.</summary>
		/// <remarks>Algorithm we use when there are no common unique lines in a region.</remarks>
		private DiffAlgorithm fallback;

		/// <summary>Set the algorithm used when there are no common unique lines remaining.</summary>
		/// <remarks>Set the algorithm used when there are no common unique lines remaining.</remarks>
		/// <param name="alg">
		/// the secondary algorithm. If null the region will be denoted as
		/// a single REPLACE block.
		/// </param>
		public virtual void SetFallbackAlgorithm(DiffAlgorithm alg)
		{
			fallback = alg;
		}

		public override EditList DiffNonCommon<S>(SequenceComparator<S> cmp, S a, 
			S b)
		{
			PatienceDiff.State<S> s = new PatienceDiff.State<S>(this, new HashedSequencePair<
				S>(cmp, a, b));
			s.DiffReplace(new Edit(0, s.a.Size(), 0, s.b.Size()), null, 0, 0);
			return s.edits;
		}

		private class State<S> where S:Sequence
		{
			private readonly HashedSequenceComparator<S> cmp;

			internal readonly HashedSequence<S> a;

			internal readonly HashedSequence<S> b;

			/// <summary>Result edits we have determined that must be made to convert a to b.</summary>
			/// <remarks>Result edits we have determined that must be made to convert a to b.</remarks>
			internal readonly EditList edits;

			internal State(PatienceDiff _enclosing, HashedSequencePair<S> p)
			{
				this._enclosing = _enclosing;
				this.cmp = p.GetComparator();
				this.a = p.GetA();
				this.b = p.GetB();
				this.edits = new EditList();
			}

			internal virtual void DiffReplace(Edit r, long[] pCommon, int pIdx, int pEnd)
			{
				PatienceDiffIndex<S> p;
				Edit lcs;
				p = new PatienceDiffIndex<S>(this.cmp, this.a, this.b, r, pCommon, pIdx, pEnd);
				lcs = p.FindLongestCommonSequence();
				if (lcs != null)
				{
					pCommon = p.nCommon;
					pIdx = p.cIdx;
					pEnd = p.nCnt;
					p = null;
					this.Diff(r.Before(lcs), pCommon, 0, pIdx);
					this.Diff(r.After(lcs), pCommon, pIdx + 1, pEnd);
				}
				else
				{
					if (this._enclosing.fallback != null)
					{
						pCommon = null;
						p = null;
						SubsequenceComparator<HashedSequence<S>> cs = this.Subcmp();
						Subsequence<HashedSequence<S>> @as = Subsequence<S>.A(this.a, r);
						Subsequence<HashedSequence<S>> bs = Subsequence<S>.B(this.b, r);
						EditList res = this._enclosing.fallback.DiffNonCommon(cs, @as, bs);
						Sharpen.Collections.AddAll(this.edits, Subsequence<S>.ToBase(res, @as, bs));
					}
					else
					{
						this.edits.AddItem(r);
					}
				}
			}

			private void Diff(Edit r, long[] pCommon, int pIdx, int pEnd)
			{
				switch (r.GetType())
				{
					case Edit.Type.INSERT:
					case Edit.Type.DELETE:
					{
						this.edits.AddItem(r);
						break;
					}

					case Edit.Type.REPLACE:
					{
						this.DiffReplace(r, pCommon, pIdx, pEnd);
						break;
					}

					case Edit.Type.EMPTY:
					{
						break;
					}

					default:
					{
						throw new InvalidOperationException();
					}
				}
			}

			private SubsequenceComparator<HashedSequence<S>> Subcmp()
			{
				return new SubsequenceComparator<HashedSequence<S>>(this.cmp);
			}

			private readonly PatienceDiff _enclosing;
		}
	}
}
