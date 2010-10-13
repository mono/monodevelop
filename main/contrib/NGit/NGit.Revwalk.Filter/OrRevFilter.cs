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
using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Includes a commit if any subfilters include the same commit.</summary>
	/// <remarks>
	/// Includes a commit if any subfilters include the same commit.
	/// <p>
	/// Classic shortcut behavior is used, so evaluation of the
	/// <see cref="RevFilter.Include(NGit.Revwalk.RevWalk, NGit.Revwalk.RevCommit)">RevFilter.Include(NGit.Revwalk.RevWalk, NGit.Revwalk.RevCommit)
	/// 	</see>
	/// method stops as soon as a true
	/// result is obtained. Applications can improve filtering performance by placing
	/// faster filters that are more likely to accept a result earlier in the list.
	/// </remarks>
	public abstract class OrRevFilter : RevFilter
	{
		/// <summary>Create a filter with two filters, one of which must match.</summary>
		/// <remarks>Create a filter with two filters, one of which must match.</remarks>
		/// <param name="a">first filter to test.</param>
		/// <param name="b">second filter to test.</param>
		/// <returns>a filter that must match at least one input filter.</returns>
		public static RevFilter Create(RevFilter a, RevFilter b)
		{
			if (a == ALL || b == ALL)
			{
				return ALL;
			}
			return new OrRevFilter.Binary(a, b);
		}

		/// <summary>Create a filter around many filters, one of which must match.</summary>
		/// <remarks>Create a filter around many filters, one of which must match.</remarks>
		/// <param name="list">
		/// list of filters to match against. Must contain at least 2
		/// filters.
		/// </param>
		/// <returns>a filter that must match at least one input filter.</returns>
		public static RevFilter Create(RevFilter[] list)
		{
			if (list.Length == 2)
			{
				return Create(list[0], list[1]);
			}
			if (list.Length < 2)
			{
				throw new ArgumentException(JGitText.Get().atLeastTwoFiltersNeeded);
			}
			RevFilter[] subfilters = new RevFilter[list.Length];
			System.Array.Copy(list, 0, subfilters, 0, list.Length);
			return new OrRevFilter.List(subfilters);
		}

		/// <summary>Create a filter around many filters, one of which must match.</summary>
		/// <remarks>Create a filter around many filters, one of which must match.</remarks>
		/// <param name="list">
		/// list of filters to match against. Must contain at least 2
		/// filters.
		/// </param>
		/// <returns>a filter that must match at least one input filter.</returns>
		public static RevFilter Create(ICollection<RevFilter> list)
		{
			if (list.Count < 2)
			{
				throw new ArgumentException(JGitText.Get().atLeastTwoFiltersNeeded);
			}
			RevFilter[] subfilters = new RevFilter[list.Count];
			Sharpen.Collections.ToArray(list, subfilters);
			if (subfilters.Length == 2)
			{
				return Create(subfilters[0], subfilters[1]);
			}
			return new OrRevFilter.List(subfilters);
		}

		private class Binary : OrRevFilter
		{
			private readonly RevFilter a;

			private readonly RevFilter b;

			internal Binary(RevFilter one, RevFilter two)
			{
				a = one;
				b = two;
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override bool Include(RevWalk walker, RevCommit c)
			{
				return a.Include(walker, c) || b.Include(walker, c);
			}

			public override RevFilter Clone()
			{
				return new OrRevFilter.Binary(a.Clone(), b.Clone());
			}

			public override string ToString()
			{
				return "(" + a.ToString() + " OR " + b.ToString() + ")";
			}
		}

		private class List : OrRevFilter
		{
			private readonly RevFilter[] subfilters;

			internal List(RevFilter[] list)
			{
				subfilters = list;
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override bool Include(RevWalk walker, RevCommit c)
			{
				foreach (RevFilter f in subfilters)
				{
					if (f.Include(walker, c))
					{
						return true;
					}
				}
				return false;
			}

			public override RevFilter Clone()
			{
				RevFilter[] s = new RevFilter[subfilters.Length];
				for (int i = 0; i < s.Length; i++)
				{
					s[i] = subfilters[i].Clone();
				}
				return new OrRevFilter.List(s);
			}

			public override string ToString()
			{
				StringBuilder r = new StringBuilder();
				r.Append("(");
				for (int i = 0; i < subfilters.Length; i++)
				{
					if (i > 0)
					{
						r.Append(" OR ");
					}
					r.Append(subfilters[i].ToString());
				}
				r.Append(")");
				return r.ToString();
			}
		}
	}
}
