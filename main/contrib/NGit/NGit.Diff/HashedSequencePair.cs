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
	/// Wraps two
	/// <see cref="Sequence">Sequence</see>
	/// instances to cache their element hash codes.
	/// This pair wraps two sequences that contain cached hash codes for the input
	/// sequences.
	/// </summary>
	/// <?></?>
	public class HashedSequencePair<S> where S:Sequence
	{
		private readonly SequenceComparator<S> cmp;

		private readonly S baseA;

		private readonly S baseB;

		private HashedSequence<S> cachedA;

		private HashedSequence<S> cachedB;

		/// <summary>Construct a pair to provide fast hash codes.</summary>
		/// <remarks>Construct a pair to provide fast hash codes.</remarks>
		/// <param name="cmp">the base comparator for the sequence elements.</param>
		/// <param name="a">the A sequence.</param>
		/// <param name="b">the B sequence.</param>
		public HashedSequencePair(SequenceComparator<S> cmp, S a, S b)
		{
			this.cmp = cmp;
			this.baseA = a;
			this.baseB = b;
		}

		/// <returns>obtain a comparator that uses the cached hash codes.</returns>
		public virtual HashedSequenceComparator<S> GetComparator()
		{
			return new HashedSequenceComparator<S>(cmp);
		}

		/// <returns>wrapper around A that includes cached hash codes.</returns>
		public virtual HashedSequence<S> GetA()
		{
			if (cachedA == null)
			{
				cachedA = Wrap(baseA);
			}
			return cachedA;
		}

		/// <returns>wrapper around B that includes cached hash codes.</returns>
		public virtual HashedSequence<S> GetB()
		{
			if (cachedB == null)
			{
				cachedB = Wrap(baseB);
			}
			return cachedB;
		}

		private HashedSequence<S> Wrap(S @base)
		{
			int end = @base.Size();
			int[] hashes = new int[end];
			for (int ptr = 0; ptr < end; ptr++)
			{
				hashes[ptr] = cmp.Hash(@base, ptr);
			}
			return new HashedSequence<S>(@base, hashes);
		}
	}
}
