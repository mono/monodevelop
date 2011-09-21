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
	/// <summary>
	/// Compares two
	/// <see cref="Sequence">Sequence</see>
	/// s to create an
	/// <see cref="EditList">EditList</see>
	/// of changes.
	/// An algorithm's
	/// <code>diff</code>
	/// method must be callable from concurrent threads
	/// without data collisions. This permits some algorithms to use a singleton
	/// pattern, with concurrent invocations using the same singleton. Other
	/// algorithms may support parameterization, in which case the caller can create
	/// a unique instance per thread.
	/// </summary>
	public abstract class DiffAlgorithm
	{
		/// <summary>Supported diff algorithm</summary>
		public enum SupportedAlgorithm
		{
			MYERS,
			HISTOGRAM
		}

		/// <param name="alg">
		/// the diff algorithm for which an implementation should be
		/// returned
		/// </param>
		/// <returns>an implementation of the specified diff algorithm</returns>
		public static DiffAlgorithm GetAlgorithm(DiffAlgorithm.SupportedAlgorithm alg)
		{
			switch (alg)
			{
				case DiffAlgorithm.SupportedAlgorithm.MYERS:
				{
					return MyersDiff<RawText>.INSTANCE;
				}

				case DiffAlgorithm.SupportedAlgorithm.HISTOGRAM:
				{
					return new HistogramDiff();
				}

				default:
				{
					throw new ArgumentException();
				}
			}
		}

		/// <summary>Compare two sequences and identify a list of edits between them.</summary>
		/// <remarks>Compare two sequences and identify a list of edits between them.</remarks>
		/// <?></?>
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
		/// <returns>
		/// a modifiable edit list comparing the two sequences. If empty, the
		/// sequences are identical according to
		/// <code>cmp</code>
		/// 's rules. The
		/// result list is never null.
		/// </returns>
		public virtual EditList Diff<S>(SequenceComparator<S> cmp, S a, S b) where 
			S:Sequence
		{
			Edit region = cmp.ReduceCommonStartEnd(a, b, CoverEdit(a, b));
			switch (region.GetType())
			{
				case Edit.Type.INSERT:
				case Edit.Type.DELETE:
				{
					return EditList.Singleton(region);
				}

				case Edit.Type.REPLACE:
				{
					SubsequenceComparator<S> cs = new SubsequenceComparator<S>(cmp);
					Subsequence<S> @as = Subsequence<S>.A(a, region);
					Subsequence<S> bs = Subsequence<S>.B(b, region);
					EditList e = Subsequence<S>.ToBase(DiffNonCommon(cs, @as, bs), @as, bs);
					// The last insertion may need to be shifted later if it
					// inserts elements that were previously reduced out as
					// common at the end.
					//
					Edit last = e[e.Count - 1];
					if (last.GetType() == Edit.Type.INSERT)
					{
						while (last.endB < b.Size() && cmp.Equals(b, last.beginB, b, last.endB))
						{
							last.beginA++;
							last.endA++;
							last.beginB++;
							last.endB++;
						}
					}
					return e;
				}

				case Edit.Type.EMPTY:
				{
					return new EditList(0);
				}

				default:
				{
					throw new InvalidOperationException();
				}
			}
		}

		private static Edit CoverEdit<S>(S a, S b) where S:Sequence
		{
			return new Edit(0, a.Size(), 0, b.Size());
		}

		/// <summary>Compare two sequences and identify a list of edits between them.</summary>
		/// <remarks>
		/// Compare two sequences and identify a list of edits between them.
		/// This method should be invoked only after the two sequences have been
		/// proven to have no common starting or ending elements. The expected
		/// elimination of common starting and ending elements is automatically
		/// performed by the
		/// <see cref="Diff{S}(SequenceComparator{S}, Sequence, Sequence)">Diff&lt;S&gt;(SequenceComparator&lt;S&gt;, Sequence, Sequence)
		/// 	</see>
		/// method, which invokes this method using
		/// <see cref="Subsequence{S}">Subsequence&lt;S&gt;</see>
		/// s.
		/// </remarks>
		/// <?></?>
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
		/// <returns>a modifiable edit list comparing the two sequences.</returns>
		public abstract EditList DiffNonCommon<S>(SequenceComparator<S> cmp, S a, 
			S b) where S:Sequence;
	}
}
