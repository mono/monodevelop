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

using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Matches only commits with some/all RevFlags already set.</summary>
	/// <remarks>Matches only commits with some/all RevFlags already set.</remarks>
	public abstract class RevFlagFilter : RevFilter
	{
		/// <summary>Create a new filter that tests for a single flag.</summary>
		/// <remarks>Create a new filter that tests for a single flag.</remarks>
		/// <param name="a">the flag to test.</param>
		/// <returns>filter that selects only commits with flag <code>a</code>.</returns>
		public static RevFilter Has(RevFlag a)
		{
			RevFlagSet s = new RevFlagSet();
			s.AddItem(a);
			return new RevFlagFilter.HasAll(s);
		}

		/// <summary>Create a new filter that tests all flags in a set.</summary>
		/// <remarks>Create a new filter that tests all flags in a set.</remarks>
		/// <param name="a">set of flags to test.</param>
		/// <returns>filter that selects only commits with all flags in <code>a</code>.</returns>
		public static RevFilter HasAllFilter(params RevFlag[] a)
		{
			RevFlagSet set = new RevFlagSet();
			foreach (RevFlag flag in a)
			{
				set.AddItem(flag);
			}
			return new RevFlagFilter.HasAll(set);
		}

		/// <summary>Create a new filter that tests all flags in a set.</summary>
		/// <remarks>Create a new filter that tests all flags in a set.</remarks>
		/// <param name="a">set of flags to test.</param>
		/// <returns>filter that selects only commits with all flags in <code>a</code>.</returns>
		public static RevFilter HasAllFilter(RevFlagSet a)
		{
			return new RevFlagFilter.HasAll(new RevFlagSet(a));
		}

		/// <summary>Create a new filter that tests for any flag in a set.</summary>
		/// <remarks>Create a new filter that tests for any flag in a set.</remarks>
		/// <param name="a">set of flags to test.</param>
		/// <returns>filter that selects only commits with any flag in <code>a</code>.</returns>
		public static RevFilter HasAnyFilter(params RevFlag[] a)
		{
			RevFlagSet set = new RevFlagSet();
			foreach (RevFlag flag in a)
			{
				set.AddItem(flag);
			}
			return new RevFlagFilter.HasAny(set);
		}

		/// <summary>Create a new filter that tests for any flag in a set.</summary>
		/// <remarks>Create a new filter that tests for any flag in a set.</remarks>
		/// <param name="a">set of flags to test.</param>
		/// <returns>filter that selects only commits with any flag in <code>a</code>.</returns>
		public static RevFilter HasAnyFilter(RevFlagSet a)
		{
			return new RevFlagFilter.HasAny(new RevFlagSet(a));
		}

		internal readonly RevFlagSet flags;

		internal RevFlagFilter(RevFlagSet m)
		{
			flags = m;
		}

		public override RevFilter Clone()
		{
			return this;
		}

		public override string ToString()
		{
			return base.ToString() + flags;
		}

		private class HasAll : RevFlagFilter
		{
			internal HasAll(RevFlagSet m) : base(m)
			{
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override bool Include(RevWalk walker, RevCommit c)
			{
				return c.HasAll(flags);
			}
		}

		private class HasAny : RevFlagFilter
		{
			internal HasAny(RevFlagSet m) : base(m)
			{
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override bool Include(RevWalk walker, RevCommit c)
			{
				return c.HasAny(flags);
			}
		}
	}
}
