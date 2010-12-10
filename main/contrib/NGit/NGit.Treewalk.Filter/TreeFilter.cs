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
	/// <summary>Selects interesting tree entries during walking.</summary>
	/// <remarks>
	/// Selects interesting tree entries during walking.
	/// <p>
	/// This is an abstract interface. Applications may implement a subclass, or use
	/// one of the predefined implementations already available within this package.
	/// <p>
	/// Unless specifically noted otherwise a TreeFilter implementation is not thread
	/// safe and may not be shared by different TreeWalk instances at the same time.
	/// This restriction allows TreeFilter implementations to cache state within
	/// their instances during
	/// <see cref="Include(NGit.Treewalk.TreeWalk)">Include(NGit.Treewalk.TreeWalk)</see>
	/// if it is beneficial to
	/// their implementation. Deep clones created by
	/// <see cref="Clone()">Clone()</see>
	/// may be used to
	/// construct a thread-safe copy of an existing filter.
	/// <p>
	/// <b>Path filters:</b>
	/// <ul>
	/// <li>Matching pathname:
	/// <see cref="PathFilter">PathFilter</see>
	/// </li>
	/// </ul>
	/// <p>
	/// <b>Difference filters:</b>
	/// <ul>
	/// <li>Only select differences:
	/// <see cref="ANY_DIFF">ANY_DIFF</see>
	/// .</li>
	/// </ul>
	/// <p>
	/// <b>Boolean modifiers:</b>
	/// <ul>
	/// <li>AND:
	/// <see cref="AndTreeFilter">AndTreeFilter</see>
	/// </li>
	/// <li>OR:
	/// <see cref="OrTreeFilter">OrTreeFilter</see>
	/// </li>
	/// <li>NOT:
	/// <see cref="NotTreeFilter">NotTreeFilter</see>
	/// </li>
	/// </ul>
	/// </remarks>
	public abstract class TreeFilter
	{
		/// <summary>Selects all tree entries.</summary>
		/// <remarks>Selects all tree entries.</remarks>
		public static readonly TreeFilter ALL = new TreeFilter.AllFilter();

		private sealed class AllFilter : TreeFilter
		{
			public override bool Include(TreeWalk walker)
			{
				return true;
			}

			public override bool ShouldBeRecursive()
			{
				return false;
			}

			public override TreeFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "ALL";
			}
		}

		/// <summary>Selects only tree entries which differ between at least 2 trees.</summary>
		/// <remarks>
		/// Selects only tree entries which differ between at least 2 trees.
		/// <p>
		/// This filter also prevents a TreeWalk from recursing into a subtree if all
		/// parent trees have the identical subtree at the same path. This
		/// dramatically improves walk performance as only the changed subtrees are
		/// entered into.
		/// <p>
		/// If this filter is applied to a walker with only one tree it behaves like
		/// <see cref="ALL">ALL</see>
		/// , or as though the walker was matching a virtual empty tree
		/// against the single tree it was actually given. Applications may wish to
		/// treat such a difference as "all names added".
		/// <p>
		/// When comparing
		/// <see cref="NGit.Treewalk.WorkingTreeIterator">NGit.Treewalk.WorkingTreeIterator</see>
		/// and
		/// <see cref="NGit.Dircache.DirCacheIterator">NGit.Dircache.DirCacheIterator</see>
		/// applications should use
		/// <see cref="IndexDiffFilter">IndexDiffFilter</see>
		/// .
		/// </remarks>
		public static readonly TreeFilter ANY_DIFF = new TreeFilter.AnyDiffFilter();

		private sealed class AnyDiffFilter : TreeFilter
		{
			public override bool Include(TreeWalk walker)
			{
				int n = walker.TreeCount;
				if (n == 1)
				{
					// Assume they meant difference to empty tree.
					return true;
				}
				int m = walker.GetRawMode(0);
				for (int i = 1; i < n; i++)
				{
					if (walker.GetRawMode(i) != m || !walker.IdEqual(i, 0))
					{
						return true;
					}
				}
				return false;
			}

			public override bool ShouldBeRecursive()
			{
				return false;
			}

			public override TreeFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "ANY_DIFF";
			}
		}

		/// <summary>Create a new filter that does the opposite of this filter.</summary>
		/// <remarks>Create a new filter that does the opposite of this filter.</remarks>
		/// <returns>a new filter that includes tree entries this filter rejects.</returns>
		public virtual TreeFilter Negate()
		{
			return NotTreeFilter.Create(this);
		}

		/// <summary>Determine if the current entry is interesting to report.</summary>
		/// <remarks>
		/// Determine if the current entry is interesting to report.
		/// <p>
		/// This method is consulted for subtree entries even if
		/// <see cref="NGit.Treewalk.TreeWalk.Recursive()">NGit.Treewalk.TreeWalk.Recursive()
		/// 	</see>
		/// is enabled. The consultation allows the
		/// filter to bypass subtree recursion on a case-by-case basis, even when
		/// recursion is enabled at the application level.
		/// </remarks>
		/// <param name="walker">the walker the filter needs to examine.</param>
		/// <returns>
		/// true if the current entry should be seen by the application;
		/// false to hide the entry.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// an object the filter needs to consult to determine its answer
		/// does not exist in the Git repository the walker is operating
		/// on. Filtering this current walker entry is impossible without
		/// the object.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// an object the filter needed to consult was not of the
		/// expected object type. This usually indicates a corrupt
		/// repository, as an object link is referencing the wrong type.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// a loose object or pack file could not be read to obtain data
		/// necessary for the filter to make its decision.
		/// </exception>
		public abstract bool Include(TreeWalk walker);

		/// <summary>
		/// Does this tree filter require a recursive walk to match everything?
		/// <p>
		/// If this tree filter is matching on full entry path names and its pattern
		/// is looking for a '/' then the filter would require a recursive TreeWalk
		/// to accurately make its decisions.
		/// </summary>
		/// <remarks>
		/// Does this tree filter require a recursive walk to match everything?
		/// <p>
		/// If this tree filter is matching on full entry path names and its pattern
		/// is looking for a '/' then the filter would require a recursive TreeWalk
		/// to accurately make its decisions. The walker is not required to enable
		/// recursive behavior for any particular filter, this is only a hint.
		/// </remarks>
		/// <returns>
		/// true if the filter would like to have the walker recurse into
		/// subtrees to make sure it matches everything correctly; false if
		/// the filter does not require entering subtrees.
		/// </returns>
		public abstract bool ShouldBeRecursive();

		/// <summary>Clone this tree filter, including its parameters.</summary>
		/// <remarks>
		/// Clone this tree filter, including its parameters.
		/// <p>
		/// This is a deep clone. If this filter embeds objects or other filters it
		/// must also clone those, to ensure the instances do not share mutable data.
		/// </remarks>
		/// <returns>another copy of this filter, suitable for another thread.</returns>
		public abstract TreeFilter Clone();

		public override string ToString()
		{
			string n = GetType().FullName;
			int lastDot = n.LastIndexOf('.');
			if (lastDot >= 0)
			{
				n = Sharpen.Runtime.Substring(n, lastDot + 1);
			}
			return n.Replace('$', '.');
		}
	}
}
