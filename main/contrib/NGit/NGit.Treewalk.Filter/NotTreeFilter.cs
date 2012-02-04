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

using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Treewalk.Filter
{
	/// <summary>Includes an entry only if the subfilter does not include the entry.</summary>
	/// <remarks>Includes an entry only if the subfilter does not include the entry.</remarks>
	public class NotTreeFilter : TreeFilter
	{
		/// <summary>Create a filter that negates the result of another filter.</summary>
		/// <remarks>Create a filter that negates the result of another filter.</remarks>
		/// <param name="a">filter to negate.</param>
		/// <returns>a filter that does the reverse of <code>a</code>.</returns>
		public static TreeFilter Create(TreeFilter a)
		{
			return new NGit.Treewalk.Filter.NotTreeFilter(a);
		}

		private readonly TreeFilter a;

		private NotTreeFilter(TreeFilter one)
		{
			a = one;
		}

		public override TreeFilter Negate()
		{
			return a;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(TreeWalk walker)
		{
			return !a.Include(walker);
		}

		public override bool ShouldBeRecursive()
		{
			return a.ShouldBeRecursive();
		}

		public override TreeFilter Clone()
		{
			TreeFilter n = a.Clone();
			return n == a ? this : new NGit.Treewalk.Filter.NotTreeFilter(n);
		}

		public override string ToString()
		{
			return "NOT " + a.ToString();
		}
	}
}
