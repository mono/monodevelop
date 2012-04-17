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
using NGit.Errors;
using NGit.Internal;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Treewalk.Filter
{
	/// <summary>Includes tree entries only if they match one or more configured paths.</summary>
	/// <remarks>
	/// Includes tree entries only if they match one or more configured paths.
	/// <p>
	/// Operates like
	/// <see cref="PathFilter">PathFilter</see>
	/// but causes the walk to abort as soon as the
	/// tree can no longer match any of the paths within the group. This may bypass
	/// the boolean logic of a higher level AND or OR group, but does improve
	/// performance for the common case of examining one or more modified paths.
	/// <p>
	/// This filter is effectively an OR group around paths, with the early abort
	/// feature described above.
	/// </remarks>
	public class PathFilterGroup
	{
		/// <summary>Create a collection of path filters from Java strings.</summary>
		/// <remarks>
		/// Create a collection of path filters from Java strings.
		/// <p>
		/// Path strings are relative to the root of the repository. If the user's
		/// input should be assumed relative to a subdirectory of the repository the
		/// caller must prepend the subdirectory's path prior to creating the filter.
		/// <p>
		/// Path strings use '/' to delimit directories on all platforms.
		/// <p>
		/// Paths may appear in any order within the collection. Sorting may be done
		/// internally when the group is constructed if doing so will improve path
		/// matching performance.
		/// </remarks>
		/// <param name="paths">the paths to test against. Must have at least one entry.</param>
		/// <returns>a new filter for the list of paths supplied.</returns>
		public static TreeFilter CreateFromStrings(ICollection<string> paths)
		{
			if (paths.IsEmpty())
			{
				throw new ArgumentException(JGitText.Get().atLeastOnePathIsRequired);
			}
			PathFilter[] p = new PathFilter[paths.Count];
			int i = 0;
			foreach (string s in paths)
			{
				p[i++] = PathFilter.Create(s);
			}
			return Create(p);
		}

		/// <summary>Create a collection of path filters from Java strings.</summary>
		/// <remarks>
		/// Create a collection of path filters from Java strings.
		/// <p>
		/// Path strings are relative to the root of the repository. If the user's
		/// input should be assumed relative to a subdirectory of the repository the
		/// caller must prepend the subdirectory's path prior to creating the filter.
		/// <p>
		/// Path strings use '/' to delimit directories on all platforms.
		/// <p>
		/// Paths may appear in any order. Sorting may be done internally when the
		/// group is constructed if doing so will improve path matching performance.
		/// </remarks>
		/// <param name="paths">the paths to test against. Must have at least one entry.</param>
		/// <returns>a new filter for the paths supplied.</returns>
		public static TreeFilter CreateFromStrings(params string[] paths)
		{
			if (paths.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().atLeastOnePathIsRequired);
			}
			int length = paths.Length;
			PathFilter[] p = new PathFilter[length];
			for (int i = 0; i < length; i++)
			{
				p[i] = PathFilter.Create(paths[i]);
			}
			return Create(p);
		}

		/// <summary>Create a collection of path filters.</summary>
		/// <remarks>
		/// Create a collection of path filters.
		/// <p>
		/// Paths may appear in any order within the collection. Sorting may be done
		/// internally when the group is constructed if doing so will improve path
		/// matching performance.
		/// </remarks>
		/// <param name="paths">the paths to test against. Must have at least one entry.</param>
		/// <returns>a new filter for the list of paths supplied.</returns>
		public static TreeFilter Create(ICollection<PathFilter> paths)
		{
			if (paths.IsEmpty())
			{
				throw new ArgumentException(JGitText.Get().atLeastOnePathIsRequired);
			}
			PathFilter[] p = new PathFilter[paths.Count];
			Sharpen.Collections.ToArray(paths, p);
			return Create(p);
		}

		private static TreeFilter Create(PathFilter[] p)
		{
			if (p.Length == 1)
			{
				return new PathFilterGroup.Single(p[0]);
			}
			return new PathFilterGroup.Group(p);
		}

		internal class Single : TreeFilter
		{
			private readonly PathFilter path;

			private readonly byte[] raw;

			internal Single(PathFilter p)
			{
				path = p;
				raw = path.pathRaw;
			}

			public override bool Include(TreeWalk walker)
			{
				int cmp = walker.IsPathPrefix(raw, raw.Length);
				if (cmp > 0)
				{
					throw StopWalkException.INSTANCE;
				}
				return cmp == 0;
			}

			public override bool ShouldBeRecursive()
			{
				return path.ShouldBeRecursive();
			}

			public override TreeFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "FAST_" + path.ToString();
			}
		}

		internal class Group : TreeFilter
		{
			private sealed class _IComparer_180 : IComparer<PathFilter>
			{
				public _IComparer_180()
				{
				}

				public int Compare(PathFilter o1, PathFilter o2)
				{
					return Sharpen.Runtime.CompareOrdinal(o1.pathStr, o2.pathStr);
				}
			}

			private static readonly IComparer<PathFilter> PATH_SORT = new _IComparer_180();

			private readonly PathFilter[] paths;

			internal Group(PathFilter[] p)
			{
				paths = p;
				Arrays.Sort(paths, PATH_SORT);
			}

			public override bool Include(TreeWalk walker)
			{
				int n = paths.Length;
				for (int i = 0; ; )
				{
					byte[] r = paths[i].pathRaw;
					int cmp = walker.IsPathPrefix(r, r.Length);
					if (cmp == 0)
					{
						return true;
					}
					if (++i < n)
					{
						continue;
					}
					if (cmp > 0)
					{
						throw StopWalkException.INSTANCE;
					}
					return false;
				}
			}

			public override bool ShouldBeRecursive()
			{
				foreach (PathFilter p in paths)
				{
					if (p.ShouldBeRecursive())
					{
						return true;
					}
				}
				return false;
			}

			public override TreeFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				StringBuilder r = new StringBuilder();
				r.Append("FAST(");
				for (int i = 0; i < paths.Length; i++)
				{
					if (i > 0)
					{
						r.Append(" OR ");
					}
					r.Append(paths[i].ToString());
				}
				r.Append(")");
				return r.ToString();
			}
		}
	}
}
