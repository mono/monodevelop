/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Includes a commit only if all subfilters include the same commit.
	/// <para />
	/// Classic shortcut behavior is used, so evaluation of the
	/// <seealso cref="RevFilter.include(RevWalk, RevCommit)"/> method stops as soon as a false
	/// result is obtained. Applications can improve filtering performance by placing
	/// faster filters that are more likely to reject a result earlier in the list.
	/// </summary>
	public abstract class AndRevFilter : RevFilter
	{
		///	<summary>
		/// Create a filter with two filters, both of which must match.
		///	</summary>
		///	<param name="a">First filter to test.</param>
		///	<param name="b">Second filter to test.</param>
		///	<returns>
		/// A filter that must match both input filters.
		/// </returns>
		public static RevFilter create(RevFilter a, RevFilter b)
		{
			if (a == ALL) return b;
			if (b == ALL) return a;

			return new Binary(a, b);
		}

		///	<summary>
		/// Create a filter around many filters, all of which must match.
		///	</summary>
		///	<param name="list">
		/// List of filters to match against. Must contain at least 2
		/// filters.
		/// </param>
		///	<returns>
		/// A filter that must match all input filters.
		/// </returns>
		public static RevFilter create(RevFilter[] list)
		{
			if (list.Length == 2)
			{
				return create(list[0], list[1]);
			}

			if (list.Length < 2)
			{
				throw new ArgumentException("At least two filters needed.");
			}

			var subfilters = new RevFilter[list.Length];
			Array.Copy(list, 0, subfilters, 0, list.Length);
			return new List(subfilters);
		}

		///	<summary>
		/// Create a filter around many filters, all of which must match.
		///	</summary>
		///	<param name="list">
		/// List of filters to match against. Must contain at least 2
		/// filters.
		/// </param>
		///	<returns>
		/// A filter that must match all input filters.
		/// </returns>
		public static RevFilter create(IEnumerable<RevFilter> list)
		{
			if (list.Count() < 2)
			{
				throw new ArgumentException("At least two filters needed.");
			}

			RevFilter[] subfilters = list.ToArray();
			if (subfilters.Length == 2)
			{
				return create(subfilters[0], subfilters[1]);
			}

			return new List(subfilters);
		}

		#region Nested Types

		private class Binary : AndRevFilter
		{
			private readonly RevFilter _a;
			private readonly RevFilter _b;

			internal Binary(RevFilter one, RevFilter two)
			{
				_a = one;
				_b = two;
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return _a.include(walker, cmit) && _b.include(walker, cmit);
			}

			public override RevFilter Clone()
			{
				return new Binary(_a.Clone(), _b.Clone());
			}

			public override string ToString()
			{
				return "(" + _a + " AND " + _b + ")";
			}
		}

		private class List : AndRevFilter
		{
			private readonly RevFilter[] _subfilters;

			internal List(RevFilter[] list)
			{
				_subfilters = list;
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				foreach (RevFilter f in _subfilters)
				{
					if (!f.include(walker, cmit)) return false;
				}
				return true;
			}

			public override RevFilter Clone()
			{
				var s = new RevFilter[_subfilters.Length];
				for (int i = 0; i < s.Length; i++)
				{
					s[i] = _subfilters[i].Clone();
				}

				return new List(s);
			}

			public override string ToString()
			{
				var r = new StringBuilder();
				r.Append("(");
				for (int i = 0; i < _subfilters.Length; i++)
				{
					if (i > 0)
					{
						r.Append(" AND ");
					}
					r.Append(_subfilters[i].ToString());
				}
				r.Append(")");
				return r.ToString();
			}
		}

		#endregion
	}
}
