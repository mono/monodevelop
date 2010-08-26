/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Matches only commits with some/all RevFlags already set.
	/// </summary>
	public abstract class RevFlagFilter : RevFilter
	{
		private readonly RevFlagSet _flags;

		/// <summary>
		/// Create a new filter that tests for a single flag.
		///	</summary>
		/// <param name="a">The flag to test.</param>
		///	<returns>
		/// Filter that selects only commits with flag <paramref name="a"/>.
		/// </returns>
		public static RevFilter has(RevFlag a)
		{
			var s = new RevFlagSet { a };
			return new HasAll(s);
		}

		///	<summary>
		/// Create a new filter that tests all flags in a set.
		///	</summary>
		///	<param name="a">Set of flags to test.</param>
		///	<returns>
		/// Filter that selects only commits with all flags in <paramref name="a"/>.
		/// </returns>
		public static RevFilter hasAll(params RevFlag[] a)
		{
			var set = new RevFlagSet();
			foreach (RevFlag flag in a)
			{
				set.Add(flag);
			}
			return new HasAll(set);
		}

		///	<summary>
		/// Create a new filter that tests all flags in a set.
		///	</summary>
		///	<param name="a">Set of flags to test.</param>
		///	<returns> filter that selects only commits with all flags in <paramref name="a"/>.
		/// </returns>
		public static RevFilter hasAll(RevFlagSet a)
		{
			return new HasAll(new RevFlagSet(a));
		}

		///	<summary>
		/// Create a new filter that tests for any flag in a set.
		///	</summary>
		///	<param name="a">Set of flags to test. </param>
		///	<returns>
		/// Filter that selects only commits with any flag in <code>a</code>.
		/// </returns>
		public static RevFilter hasAny(params RevFlag[] a)
		{
			var set = new RevFlagSet();
			foreach (RevFlag flag in a)
			{
				set.Add(flag);
			}
			return new HasAny(set);
		}

		///	<summary>
		/// Create a new filter that tests for any flag in a set.
		///	</summary>
		///	<param name="a">Set of flags to test.</param>
		///	<returns>
		/// Filter that selects only commits with any flag in <code>a</code>.
		/// </returns>
		public static RevFilter hasAny(RevFlagSet a)
		{
			return new HasAny(new RevFlagSet(a));
		}

		internal RevFlagFilter(RevFlagSet m)
		{
			_flags = m;
		}

		public override RevFilter Clone()
		{
			return this;
		}

		public override string ToString()
		{
			return base.ToString() + _flags;
		}

		#region Nested Types

		private class HasAll : RevFlagFilter
		{
			public HasAll(RevFlagSet m)
				: base(m)
			{
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return cmit.hasAll(_flags);
			}
		}

		private class HasAny : RevFlagFilter
		{
			internal HasAny(RevFlagSet m)
				: base(m)
			{
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return cmit.hasAny(_flags);
			}
		}

		#endregion
	}
}