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

namespace GitSharp.Core.Diff
{
	/// <summary>
	/// Arbitrary sequence of elements with fast comparison support.
	/// <para />
	/// A sequence of elements is defined to contain elements in the index range
	/// <code>[0, <seealso cref="size()"/>)</code>, like a standard Java List implementation.
	/// Unlike a List, the members of the sequence are not directly obtainable, but
	/// element equality can be tested if two Sequences are the same implementation.
	/// <para />
	/// An implementation may chose to implement the equals semantic as necessary,
	/// including fuzzy matching rules such as ignoring insignificant sub-elements,
	/// e.g. ignoring whitespace differences in text.
	/// <para />
	/// Implementations of Sequence are primarily intended for use in content
	/// difference detection algorithms, to produce an <seealso cref="EditList"/> of
	/// <seealso cref="Edit"/> instances describing how two Sequence instances differ. 
	/// </summary>
	public interface Sequence
	{
		/// <returns>
		/// Total number of items in the sequence.
		/// </returns>
		int size();

		///	<summary>
		///  Determine if the i-th member is equal to the j-th member.
		///	<para />
		///	Implementations must ensure <code>equals(thisIdx,other,otherIdx)</code>
		///	returns the same as <code>other.equals(otherIdx,this,thisIdx)</code>.
		///	</summary>
		///	<param name="thisIdx">
		///	Index within <code>this</code> sequence; must be in the range
		///	<code>[ 0, this.size() )</code>.
		/// </param>
		///	<param name="other">
		/// Another sequence; must be the same implementation class, that
		///	is <code>this.getClass() == other.getClass()</code>. </param>
		///	<param name="otherIdx">
		///	Index within <code>other</code> sequence; must be in the range
		///	<code>[ 0, other.size() )</code>. </param>
		///	<returns>
		/// true if the elements are equal; false if they are not equal.
		/// </returns>
		bool equals(int thisIdx, Sequence other, int otherIdx);
	}
}