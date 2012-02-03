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
	/// <summary>Compares two sequences primarily based upon hash codes.</summary>
	/// <remarks>Compares two sequences primarily based upon hash codes.</remarks>
	public abstract class LowLevelDiffAlgorithm : DiffAlgorithm
	{
		public override EditList DiffNonCommon<S>(SequenceComparator<S> cmp, S a, 
			S b)
		{
			HashedSequencePair<S> p = new HashedSequencePair<S>(cmp, a, b);
			HashedSequenceComparator<S> hc = p.GetComparator();
			HashedSequence<S> ha = p.GetA();
			HashedSequence<S> hb = p.GetB();
			p = null;
			EditList res = new EditList();
			Edit region = new Edit(0, a.Size(), 0, b.Size());
			DiffNonCommon(res, hc, ha, hb, region);
			return res;
		}

		/// <summary>Compare two sequences and identify a list of edits between them.</summary>
		/// <remarks>
		/// Compare two sequences and identify a list of edits between them.
		/// This method should be invoked only after the two sequences have been
		/// proven to have no common starting or ending elements. The expected
		/// elimination of common starting and ending elements is automatically
		/// performed by the
		/// <see cref="DiffAlgorithm.Diff{S}(SequenceComparator{S}, Sequence, Sequence)">DiffAlgorithm.Diff&lt;S&gt;(SequenceComparator&lt;S&gt;, Sequence, Sequence)
		/// 	</see>
		/// method, which invokes this method using
		/// <see cref="Subsequence{S}">Subsequence&lt;S&gt;</see>
		/// s.
		/// </remarks>
		/// <?></?>
		/// <param name="edits">result list to append the region's edits onto.</param>
		/// <param name="cmp">the comparator supplying the element equivalence function.</param>
		/// <param name="a">
		/// the first (also known as old or pre-image) sequence. Edits
		/// returned by this algorithm will reference indexes using the
		/// 'A' side:
		/// <see cref="Edit.GetBeginA()">Edit.GetBeginA()</see>
		/// ,
		/// <see cref="Edit.GetEndA()">Edit.GetEndA()</see>
		/// .
		/// </param>
		/// <param name="b">
		/// the second (also known as new or post-image) sequence. Edits
		/// returned by this algorithm will reference indexes using the
		/// 'B' side:
		/// <see cref="Edit.GetBeginB()">Edit.GetBeginB()</see>
		/// ,
		/// <see cref="Edit.GetEndB()">Edit.GetEndB()</see>
		/// .
		/// </param>
		/// <param name="region">the region being compared within the two sequences.</param>
		public abstract void DiffNonCommon<S>(EditList edits, HashedSequenceComparator<S>
			 cmp, HashedSequence<S> a, HashedSequence<S> b, Edit region) where S:Sequence;
	}
}
