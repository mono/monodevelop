/*
 * Copyright (C) 2009-2010, Henon <meinrad.recheis@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Core;

namespace GitSharp
{

	/// <summary>
	/// Represents a <see cref="Tree"/> (directory) or <see cref="Leaf"/> (file) in the <see cref="Repository"/>.
	/// </summary>
	public abstract class AbstractTreeNode : AbstractObject
	{
		internal AbstractTreeNode(Repository repo, ObjectId id) : base(repo, id) { }

		public abstract string Name { get; }
		public abstract string Path { get; }
		public abstract Tree Parent { get; }
		public abstract int Permissions { get; }

		/// <summary>
		/// Returns all commits this file or directory was changed in
		/// </summary>
		/// <returns>commits in the order "most recent first"</returns>
		public IEnumerable<Commit> GetHistory()
		{
			return GetHistoryBefore(_repo.CurrentBranch.CurrentCommit);
		}

		/// <summary>
		/// Returns all commits this file or directory was changed in before the given commit
		/// </summary>
		/// <param name="commit">The commit in whose ancestors should be searched</param>
		/// <returns>commits in the order "most recent first"</returns>
		public IEnumerable<Commit> GetHistoryBefore(Commit commit)
		{
			if (commit == null)
				yield break;
			foreach (var c in new[] { commit }.Concat(commit.Ancestors))
			{
				foreach (var change in c.Changes)
				{
					if ((this is Leaf && change.Path == this.Path) || (this is Tree && change.Path.StartsWith(this.Path))) // <--- [henon] normally this is bad style but here I prefer it over polymorphism for sake of readability
					{
						yield return c;

						if ((change.Path == this.Path) && (change.ChangeType == ChangeType.Added))
							yield break; // creation point, so there is no more history
						else
							break; // <--- we found a change we can exit early
					}
				}
			}
		}

		/// <summary>
		/// Find the commit this file or tree was last changed in
		/// </summary>
		public Commit GetLastCommit()
		{
			return GetLastCommitBefore(_repo.CurrentBranch.CurrentCommit);
		}

		/// <summary>
		/// Find the commit this file or tree was last changed in before the given commit
		/// </summary>
		/// <param name="commit">commit to start at</param>
		public Commit GetLastCommitBefore(Commit commit)
		{
			return GetHistoryBefore(commit).FirstOrDefault();
		}

	}
}
