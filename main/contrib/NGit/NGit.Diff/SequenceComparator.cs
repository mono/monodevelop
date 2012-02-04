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
	/// Equivalence function for a
	/// <see cref="Sequence">Sequence</see>
	/// compared by difference algorithm.
	/// Difference algorithms can use a comparator to compare portions of two
	/// sequences and discover the minimal edits required to transform from one
	/// sequence to the other sequence.
	/// Indexes within a sequence are zero-based.
	/// </summary>
	/// <?></?>
	public abstract class SequenceComparator<S> where S:Sequence
	{
		/// <summary>Compare two items to determine if they are equivalent.</summary>
		/// <remarks>
		/// Compare two items to determine if they are equivalent.
		/// It is permissible to compare sequence
		/// <code>a</code>
		/// with itself (by passing
		/// <code>a</code>
		/// again in position
		/// <code>b</code>
		/// ).
		/// </remarks>
		/// <param name="a">the first sequence.</param>
		/// <param name="ai">
		/// item of
		/// <code>ai</code>
		/// to compare.
		/// </param>
		/// <param name="b">the second sequence.</param>
		/// <param name="bi">
		/// item of
		/// <code>bi</code>
		/// to compare.
		/// </param>
		/// <returns>
		/// true if the two items are identical according to this function's
		/// equivalence rule.
		/// </returns>
		public abstract bool Equals(S a, int ai, S b, int bi);

		/// <summary>Get a hash value for an item in a sequence.</summary>
		/// <remarks>
		/// Get a hash value for an item in a sequence.
		/// If two items are equal according to this comparator's
		/// <see cref="SequenceComparator{S}.Equals(Sequence, int, Sequence, int)">SequenceComparator&lt;S&gt;.Equals(Sequence, int, Sequence, int)
		/// 	</see>
		/// method, then this hash
		/// method must produce the same integer result for both items.
		/// It is not required for two items to have different hash values if they
		/// are are unequal according to the
		/// <code>equals()</code>
		/// method.
		/// </remarks>
		/// <param name="seq">the sequence.</param>
		/// <param name="ptr">the item to obtain the hash for.</param>
		/// <returns>hash the hash value.</returns>
		public abstract int Hash(S seq, int ptr);

		/// <summary>Modify the edit to remove common leading and trailing items.</summary>
		/// <remarks>
		/// Modify the edit to remove common leading and trailing items.
		/// The supplied edit
		/// <code>e</code>
		/// is reduced in size by moving the beginning A
		/// and B points so the edit does not cover any items that are in common
		/// between the two sequences. The ending A and B points are also shifted to
		/// remove common items from the end of the region.
		/// </remarks>
		/// <param name="a">the first sequence.</param>
		/// <param name="b">the second sequence.</param>
		/// <param name="e">the edit to start with and update.</param>
		/// <returns>
		/// 
		/// <code>e</code>
		/// if it was updated in-place, otherwise a new edit
		/// containing the reduced region.
		/// </returns>
		public virtual Edit ReduceCommonStartEnd(S a, S b, Edit e)
		{
			// Skip over items that are common at the start.
			//
			while (e.beginA < e.endA && e.beginB < e.endB && Equals(a, e.beginA, b, e.beginB)
				)
			{
				e.beginA++;
				e.beginB++;
			}
			// Skip over items that are common at the end.
			//
			while (e.beginA < e.endA && e.beginB < e.endB && Equals(a, e.endA - 1, b, e.endB 
				- 1))
			{
				e.endA--;
				e.endB--;
			}
			return e;
		}
	}
}
