/*
 * Copyright (C) 2008, Johannes E. Schindelin <johannes.schindelin@gmx.de>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace GitSharp.Core.Diff
{
	/// <summary>
	/// A modified region detected between two versions of roughly the same content.
	/// <para />
	/// Regions should be specified using 0 based notation, so add 1 to the
	/// start and end marks for line numbers in a file.
	/// <para />
	/// An edit where <code>beginA == endA &amp;&amp; beginB &gt; endB</code> is an insert edit,
	/// that is sequence B inserted the elements in region
	/// <code>[beginB, endB)</code> at <code>beginA</code>.
	/// <para />
	/// An edit where <code>beginA &gt; endA &amp;&amp; beginB &gt; endB</code> is a replace edit,
	/// that is sequence B has replaced the range of elements between
	/// <code>[beginA, endA)</code> with those found in <code>[beginB, endB)</code>.
	/// </summary>
	public class Edit
	{
		/// <summary>
		/// Type of edit
		/// </summary>
		[Serializable]
		public enum Type
		{
			/// <summary>
			/// Sequence B has inserted the region.
			/// </summary>
			INSERT,

			/// <summary>
			/// Sequence B has removed the region.
			/// </summary>
			DELETE,

			/// <summary>
			/// Sequence B has replaced the region with different content.
			/// </summary>
			REPLACE,

			/// <summary>
			/// Sequence A and B have zero length, describing nothing.
			/// </summary>
			EMPTY
		}

		/// <summary>
		/// Create a new empty edit.
		/// </summary>
		/// <param name="aStart">beginA: start and end of region in sequence A; 0 based.</param>
		/// <param name="bStart">beginB: start and end of region in sequence B; 0 based.</param>
		public Edit(int aStart, int bStart)
			: this(aStart, aStart, bStart, bStart)
		{
		}

		/// <summary>
		/// Create a new empty edit.
		/// </summary>
		/// <param name="aStart">beginA: start and end of region in sequence A; 0 based.</param>
		/// <param name="aEnd">endA: end of region in sequence A; must be >= as.</param>
		/// <param name="bStart">beginB: start and end of region in sequence B; 0 based.</param>
		/// <param name="bEnd">endB: end of region in sequence B; must be >= bs.</param>
		public Edit(int aStart, int aEnd, int bStart, int bEnd)
		{
			BeginA = aStart;
			EndA = aEnd;

			BeginB = bStart;
			EndB = bEnd;
		}

		/// <summary>
		/// Gets the type of this region.
		/// </summary>
		public Type EditType
		{
			get
			{
				if (BeginA == EndA && BeginB < EndB)
				{
					return Type.INSERT;
				}
				if (BeginA < EndA && BeginB == EndB)
				{
					return Type.DELETE;
				}
				if (BeginA == EndA && BeginB == EndB)
				{
					return Type.EMPTY;
				}

				return Type.REPLACE;
			}
		}

		/// <summary>
		/// Start point in sequence A.
		/// </summary>
		public int BeginA { get; set; }

		/// <summary>
		/// End point in sequence A.
		/// </summary>
		public int EndA { get; private set; }

		/// <summary>
		/// Start point in sequence B.
		/// </summary>
		public int BeginB { get; private set; }

		/// <summary>
		/// End point in sequence B.
		/// </summary>
		public int EndB { get; private set; }

		/// <summary>
		/// Increase <see cref="EndA"/> by 1.
		/// </summary>
		public void ExtendA()
		{
			EndA++;
		}

		/// <summary>
		/// Increase <see cref="EndB"/> by 1.
		/// </summary>
		public void ExtendB()
		{
			EndB++;
		}

		/// <summary>
		/// Swap A and B, so the edit goes the other direction.
		/// </summary>
		public void Swap()
		{
			int sBegin = BeginA;
			int sEnd = EndA;

			BeginA = BeginB;
			EndA = EndB;

			BeginB = sBegin;
			EndB = sEnd;
		}

		public override int GetHashCode()
		{
			return BeginA ^ EndA;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is
		/// equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the
		/// current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with
		/// the current <see cref="T:System.Object"/>.
		/// </param>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		/// <filterpriority>2</filterpriority>
		public override bool Equals(object obj)
		{
			Edit e = (obj as Edit);
			if (e != null)
			{
				return BeginA == e.BeginA && EndA == e.EndA && BeginB == e.BeginB && EndB == e.EndB;
			}

			return false;
		}

		public static bool operator ==(Edit left, Edit right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Edit left, Edit right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			Type t = EditType;
			return t + "(" + BeginA + "-" + EndA + "," + BeginB + "-" + EndB + ")";
		}
	}
}