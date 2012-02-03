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
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Updates the internal path filter to follow copy/renames.</summary>
	/// <remarks>
	/// Updates the internal path filter to follow copy/renames.
	/// <p>
	/// This is a special filter that performs
	/// <code>AND(path, ANY_DIFF)</code>
	/// , but also
	/// triggers rename detection so that the path node is updated to include a prior
	/// file name as the RevWalk traverses history.
	/// The renames found will be reported to a
	/// <see cref="RenameCallback">RenameCallback</see>
	/// if one is set.
	/// <p>
	/// Results with this filter are unpredictable if the path being followed is a
	/// subdirectory.
	/// </remarks>
	public class FollowFilter : TreeFilter
	{
		/// <summary>Create a new tree filter for a user supplied path.</summary>
		/// <remarks>
		/// Create a new tree filter for a user supplied path.
		/// <p>
		/// Path strings are relative to the root of the repository. If the user's
		/// input should be assumed relative to a subdirectory of the repository the
		/// caller must prepend the subdirectory's path prior to creating the filter.
		/// <p>
		/// Path strings use '/' to delimit directories on all platforms.
		/// </remarks>
		/// <param name="path">
		/// the path to filter on. Must not be the empty string. All
		/// trailing '/' characters will be trimmed before string's length
		/// is checked or is used as part of the constructed filter.
		/// </param>
		/// <returns>a new filter for the requested path.</returns>
		/// <exception cref="System.ArgumentException">the path supplied was the empty string.
		/// 	</exception>
		public static NGit.Revwalk.FollowFilter Create(string path)
		{
			return new NGit.Revwalk.FollowFilter(PathFilter.Create(path));
		}

		private readonly PathFilter path;

		private RenameCallback renameCallback;

		internal FollowFilter(PathFilter path)
		{
			this.path = path;
		}

		/// <returns>the path this filter matches.</returns>
		public virtual string GetPath()
		{
			return path.GetPath();
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(TreeWalk walker)
		{
			return path.Include(walker) && ANY_DIFF.Include(walker);
		}

		public override bool ShouldBeRecursive()
		{
			return path.ShouldBeRecursive() || ANY_DIFF.ShouldBeRecursive();
		}

		public override TreeFilter Clone()
		{
			return new NGit.Revwalk.FollowFilter(((PathFilter)path.Clone()));
		}

		public override string ToString()
		{
			return "(FOLLOW(" + path.ToString() + ")" + " AND " + ANY_DIFF.ToString() + ")";
		}

		//
		//
		/// <returns>
		/// the callback to which renames are reported, or <code>null</code>
		/// if none
		/// </returns>
		public virtual RenameCallback GetRenameCallback()
		{
			return renameCallback;
		}

		/// <summary>Sets the callback to which renames shall be reported.</summary>
		/// <remarks>Sets the callback to which renames shall be reported.</remarks>
		/// <param name="callback">the callback to use</param>
		public virtual void SetRenameCallback(RenameCallback callback)
		{
			renameCallback = callback;
		}
	}
}
