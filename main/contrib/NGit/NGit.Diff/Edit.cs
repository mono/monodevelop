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
	/// <summary>A modified region detected between two versions of roughly the same content.
	/// 	</summary>
	/// <remarks>
	/// A modified region detected between two versions of roughly the same content.
	/// <p>
	/// An edit covers the modified region only. It does not cover a common region.
	/// <p>
	/// Regions should be specified using 0 based notation, so add 1 to the start and
	/// end marks for line numbers in a file.
	/// <p>
	/// An edit where <code>beginA == endA && beginB &lt; endB</code> is an insert edit,
	/// that is sequence B inserted the elements in region
	/// <code>[beginB, endB)</code> at <code>beginA</code>.
	/// <p>
	/// An edit where <code>beginA &lt; endA && beginB == endB</code> is a delete edit,
	/// that is sequence B has removed the elements between
	/// <code>[beginA, endA)</code>.
	/// <p>
	/// An edit where <code>beginA &lt; endA && beginB &lt; endB</code> is a replace edit,
	/// that is sequence B has replaced the range of elements between
	/// <code>[beginA, endA)</code> with those found in <code>[beginB, endB)</code>.
	/// </remarks>
	public class Edit
	{
		/// <summary>Type of edit</summary>
		public enum Type
		{
			INSERT,
			DELETE,
			REPLACE,
			EMPTY
		}

		internal int beginA;

		internal int endA;

		internal int beginB;

		internal int endB;

		/// <summary>Create a new empty edit.</summary>
		/// <remarks>Create a new empty edit.</remarks>
		/// <param name="as">beginA: start and end of region in sequence A; 0 based.</param>
		/// <param name="bs">beginB: start and end of region in sequence B; 0 based.</param>
		public Edit(int @as, int bs) : this(@as, @as, bs, bs)
		{
		}

		/// <summary>Create a new edit.</summary>
		/// <remarks>Create a new edit.</remarks>
		/// <param name="as">beginA: start of region in sequence A; 0 based.</param>
		/// <param name="ae">endA: end of region in sequence A; must be &gt;= as.</param>
		/// <param name="bs">beginB: start of region in sequence B; 0 based.</param>
		/// <param name="be">endB: end of region in sequence B; must be &gt;= bs.</param>
		public Edit(int @as, int ae, int bs, int be)
		{
			beginA = @as;
			endA = ae;
			beginB = bs;
			endB = be;
		}

		/// <returns>the type of this region</returns>
		public Edit.Type GetType()
		{
			if (beginA < endA)
			{
				if (beginB < endB)
				{
					return Edit.Type.REPLACE;
				}
				else
				{
					return Edit.Type.DELETE;
				}
			}
			else
			{
				if (beginB < endB)
				{
					return Edit.Type.INSERT;
				}
				else
				{
					return Edit.Type.EMPTY;
				}
			}
		}

		/// <returns>true if the edit is empty (lengths of both a and b is zero).</returns>
		public bool IsEmpty()
		{
			return beginA == endA && beginB == endB;
		}

		/// <returns>start point in sequence A.</returns>
		public int GetBeginA()
		{
			return beginA;
		}

		/// <returns>end point in sequence A.</returns>
		public int GetEndA()
		{
			return endA;
		}

		/// <returns>start point in sequence B.</returns>
		public int GetBeginB()
		{
			return beginB;
		}

		/// <returns>end point in sequence B.</returns>
		public int GetEndB()
		{
			return endB;
		}

		/// <returns>length of the region in A.</returns>
		public int GetLengthA()
		{
			return endA - beginA;
		}

		/// <returns>length of the region in B.</returns>
		public int GetLengthB()
		{
			return endB - beginB;
		}

		/// <summary>Construct a new edit representing the region before cut.</summary>
		/// <remarks>Construct a new edit representing the region before cut.</remarks>
		/// <param name="cut">
		/// the cut point. The beginning A and B points are used as the
		/// end points of the returned edit.
		/// </param>
		/// <returns>
		/// an edit representing the slice of
		/// <code>this</code>
		/// edit that occurs
		/// before
		/// <code>cut</code>
		/// starts.
		/// </returns>
		public NGit.Diff.Edit Before(NGit.Diff.Edit cut)
		{
			return new NGit.Diff.Edit(beginA, cut.beginA, beginB, cut.beginB);
		}

		/// <summary>Construct a new edit representing the region after cut.</summary>
		/// <remarks>Construct a new edit representing the region after cut.</remarks>
		/// <param name="cut">
		/// the cut point. The ending A and B points are used as the
		/// starting points of the returned edit.
		/// </param>
		/// <returns>
		/// an edit representing the slice of
		/// <code>this</code>
		/// edit that occurs
		/// after
		/// <code>cut</code>
		/// ends.
		/// </returns>
		public NGit.Diff.Edit After(NGit.Diff.Edit cut)
		{
			return new NGit.Diff.Edit(cut.endA, endA, cut.endB, endB);
		}

		/// <summary>
		/// Increase
		/// <see cref="GetEndA()">GetEndA()</see>
		/// by 1.
		/// </summary>
		public virtual void ExtendA()
		{
			endA++;
		}

		/// <summary>
		/// Increase
		/// <see cref="GetEndB()">GetEndB()</see>
		/// by 1.
		/// </summary>
		public virtual void ExtendB()
		{
			endB++;
		}

		/// <summary>Swap A and B, so the edit goes the other direction.</summary>
		/// <remarks>Swap A and B, so the edit goes the other direction.</remarks>
		public virtual void Swap()
		{
			int sBegin = beginA;
			int sEnd = endA;
			beginA = beginB;
			endA = endB;
			beginB = sBegin;
			endB = sEnd;
		}

		public override int GetHashCode()
		{
			return beginA ^ endA;
		}

		public override bool Equals(object o)
		{
			if (o is NGit.Diff.Edit)
			{
				NGit.Diff.Edit e = (NGit.Diff.Edit)o;
				return this.beginA == e.beginA && this.endA == e.endA && this.beginB == e.beginB 
					&& this.endB == e.endB;
			}
			return false;
		}

		public override string ToString()
		{
			Edit.Type t = GetType();
			return t + "(" + beginA + "-" + endA + "," + beginB + "-" + endB + ")";
		}
	}
}
