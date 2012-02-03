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
	/// Wraps a
	/// <see cref="Sequence">Sequence</see>
	/// to have a narrower range of elements.
	/// This sequence acts as a proxy for the real sequence, translating element
	/// indexes on the fly by adding
	/// <code>begin</code>
	/// to them. Sequences of this type
	/// must be used with a
	/// <see cref="SubsequenceComparator{S}">SubsequenceComparator&lt;S&gt;</see>
	/// .
	/// </summary>
	/// <?></?>
	public sealed class Subsequence<S> : Sequence where S:Sequence
	{
		/// <summary>Construct a subsequence around the A region/base sequence.</summary>
		/// <remarks>Construct a subsequence around the A region/base sequence.</remarks>
		/// <?></?>
		/// <param name="a">the A sequence.</param>
		/// <param name="region">
		/// the region of
		/// <code>a</code>
		/// to create a subsequence around.
		/// </param>
		/// <returns>
		/// subsequence of
		/// <code>base</code>
		/// as described by A in
		/// <code>region</code>
		/// .
		/// </returns>
		public static NGit.Diff.Subsequence<S> A<S>(S a, Edit region) where S:Sequence
		{
			return new NGit.Diff.Subsequence<S>(a, region.beginA, region.endA);
		}

		/// <summary>Construct a subsequence around the B region/base sequence.</summary>
		/// <remarks>Construct a subsequence around the B region/base sequence.</remarks>
		/// <?></?>
		/// <param name="b">the B sequence.</param>
		/// <param name="region">
		/// the region of
		/// <code>b</code>
		/// to create a subsequence around.
		/// </param>
		/// <returns>
		/// subsequence of
		/// <code>base</code>
		/// as described by B in
		/// <code>region</code>
		/// .
		/// </returns>
		public static NGit.Diff.Subsequence<S> B<S>(S b, Edit region) where S:Sequence
		{
			return new NGit.Diff.Subsequence<S>(b, region.beginB, region.endB);
		}

		/// <summary>Adjust the Edit to reflect positions in the base sequence.</summary>
		/// <remarks>Adjust the Edit to reflect positions in the base sequence.</remarks>
		/// <?></?>
		/// <param name="e">
		/// edit to adjust in-place. Prior to invocation the indexes are
		/// in terms of the two subsequences; after invocation the indexes
		/// are in terms of the base sequences.
		/// </param>
		/// <param name="a">the A sequence.</param>
		/// <param name="b">the B sequence.</param>
		public static void ToBase<S>(Edit e, NGit.Diff.Subsequence<S> a, NGit.Diff.Subsequence
			<S> b) where S:Sequence
		{
			e.beginA += a.begin;
			e.endA += a.begin;
			e.beginB += b.begin;
			e.endB += b.begin;
		}

		/// <summary>Adjust the Edits to reflect positions in the base sequence.</summary>
		/// <remarks>Adjust the Edits to reflect positions in the base sequence.</remarks>
		/// <?></?>
		/// <param name="edits">
		/// edits to adjust in-place. Prior to invocation the indexes are
		/// in terms of the two subsequences; after invocation the indexes
		/// are in terms of the base sequences.
		/// </param>
		/// <param name="a">the A sequence.</param>
		/// <param name="b">the B sequence.</param>
		/// <returns>
		/// always
		/// <code>edits</code>
		/// (as the list was updated in-place).
		/// </returns>
		public static EditList ToBase<S>(EditList edits, NGit.Diff.Subsequence<S> a, NGit.Diff.Subsequence
			<S> b) where S:Sequence
		{
			foreach (Edit e in edits)
			{
				ToBase(e, a, b);
			}
			return edits;
		}

		internal readonly S @base;

		internal readonly int begin;

		private readonly int size;

		/// <summary>Construct a subset of another sequence.</summary>
		/// <remarks>
		/// Construct a subset of another sequence.
		/// The size of the subsequence will be
		/// <code>end - begin</code>
		/// .
		/// </remarks>
		/// <param name="base">the real sequence.</param>
		/// <param name="begin">
		/// First element index of
		/// <code>base</code>
		/// that will be part of this
		/// new subsequence. The element at
		/// <code>begin</code>
		/// will be this
		/// sequence's element 0.
		/// </param>
		/// <param name="end">
		/// One past the last element index of
		/// <code>base</code>
		/// that will be
		/// part of this new subsequence.
		/// </param>
		public Subsequence(S @base, int begin, int end)
		{
			this.@base = @base;
			this.begin = begin;
			this.size = end - begin;
		}

		public override int Size()
		{
			return size;
		}
	}
}
