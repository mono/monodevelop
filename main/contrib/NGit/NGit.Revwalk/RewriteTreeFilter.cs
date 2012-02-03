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
using NGit;
using NGit.Diff;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>First phase of a path limited revision walk.</summary>
	/// <remarks>
	/// First phase of a path limited revision walk.
	/// <p>
	/// This filter is ANDed to evaluate after all other filters and ties the
	/// configured
	/// <see cref="NGit.Treewalk.Filter.TreeFilter">NGit.Treewalk.Filter.TreeFilter</see>
	/// into the revision walking process.
	/// <p>
	/// Each commit is differenced concurrently against all of its parents to look
	/// for tree entries that are interesting to the TreeFilter. If none are found
	/// the commit is colored with
	/// <see cref="RevWalk.REWRITE">RevWalk.REWRITE</see>
	/// , allowing a later pass
	/// implemented by
	/// <see cref="RewriteGenerator">RewriteGenerator</see>
	/// to remove those colored commits from
	/// the DAG.
	/// </remarks>
	/// <seealso cref="RewriteGenerator">RewriteGenerator</seealso>
	internal class RewriteTreeFilter : RevFilter
	{
		private const int PARSED = RevWalk.PARSED;

		private const int UNINTERESTING = RevWalk.UNINTERESTING;

		private const int REWRITE = RevWalk.REWRITE;

		private readonly TreeWalk pathFilter;

		private readonly Repository repository;

		internal RewriteTreeFilter(RevWalk walker, TreeFilter t)
		{
			repository = walker.repository;
			pathFilter = new TreeWalk(walker.reader);
			pathFilter.Filter = t;
			pathFilter.Recursive = t.ShouldBeRecursive();
		}

		public override RevFilter Clone()
		{
			throw new NotSupportedException();
		}

		/// <exception cref="NGit.Errors.StopWalkException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(RevWalk walker, RevCommit c)
		{
			// Reset the tree filter to scan this commit and parents.
			//
			RevCommit[] pList = c.parents;
			int nParents = pList.Length;
			TreeWalk tw = pathFilter;
			ObjectId[] trees = new ObjectId[nParents + 1];
			for (int i = 0; i < nParents; i++)
			{
				RevCommit p = c.parents[i];
				if ((p.flags & PARSED) == 0)
				{
					p.ParseHeaders(walker);
				}
				trees[i] = p.Tree;
			}
			trees[nParents] = c.Tree;
			tw.Reset(trees);
			if (nParents == 1)
			{
				// We have exactly one parent. This is a very common case.
				//
				int chgs = 0;
				int adds = 0;
				while (tw.Next())
				{
					chgs++;
					if (tw.GetRawMode(0) == 0 && tw.GetRawMode(1) != 0)
					{
						adds++;
					}
					else
					{
						break;
					}
				}
				// no point in looking at this further.
				if (chgs == 0)
				{
					// No changes, so our tree is effectively the same as
					// our parent tree. We pass the buck to our parent.
					//
					c.flags |= REWRITE;
					return false;
				}
				else
				{
					// We have interesting items, but neither of the special
					// cases denoted above.
					//
					if (adds > 0 && tw.Filter is FollowFilter)
					{
						// One of the paths we care about was added in this
						// commit. We need to update our filter to its older
						// name, if we can discover it. Find out what that is.
						//
						UpdateFollowFilter(trees);
					}
					return true;
				}
			}
			else
			{
				if (nParents == 0)
				{
					// We have no parents to compare against. Consider us to be
					// REWRITE only if we have no paths matching our filter.
					//
					if (tw.Next())
					{
						return true;
					}
					c.flags |= REWRITE;
					return false;
				}
			}
			// We are a merge commit. We can only be REWRITE if we are same
			// to _all_ parents. We may also be able to eliminate a parent if
			// it does not contribute changes to us. Such a parent may be an
			// uninteresting side branch.
			//
			int[] chgs_1 = new int[nParents];
			int[] adds_1 = new int[nParents];
			while (tw.Next())
			{
				int myMode = tw.GetRawMode(nParents);
				for (int i_1 = 0; i_1 < nParents; i_1++)
				{
					int pMode = tw.GetRawMode(i_1);
					if (myMode == pMode && tw.IdEqual(i_1, nParents))
					{
						continue;
					}
					chgs_1[i_1]++;
					if (pMode == 0 && myMode != 0)
					{
						adds_1[i_1]++;
					}
				}
			}
			bool same = false;
			bool diff = false;
			for (int i_2 = 0; i_2 < nParents; i_2++)
			{
				if (chgs_1[i_2] == 0)
				{
					// No changes, so our tree is effectively the same as
					// this parent tree. We pass the buck to only this one
					// parent commit.
					//
					RevCommit p = pList[i_2];
					if ((p.flags & UNINTERESTING) != 0)
					{
						// This parent was marked as not interesting by the
						// application. We should look for another parent
						// that is interesting.
						//
						same = true;
						continue;
					}
					c.flags |= REWRITE;
					c.parents = new RevCommit[] { p };
					return false;
				}
				if (chgs_1[i_2] == adds_1[i_2])
				{
					// All of the differences from this parent were because we
					// added files that they did not have. This parent is our
					// "empty tree root" and thus their history is not relevant.
					// Cut our grandparents to be an empty list.
					//
					pList[i_2].parents = RevCommit.NO_PARENTS;
				}
				// We have an interesting difference relative to this parent.
				//
				diff = true;
			}
			if (diff && !same)
			{
				// We did not abort above, so we are different in at least one
				// way from all of our parents. We have to take the blame for
				// that difference.
				//
				return true;
			}
			// We are the same as all of our parents. We must keep them
			// as they are and allow those parents to flow into pending
			// for further scanning.
			//
			c.flags |= REWRITE;
			return false;
		}

		public override bool RequiresCommitBody()
		{
			return false;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateFollowFilter(ObjectId[] trees)
		{
			TreeWalk tw = pathFilter;
			FollowFilter oldFilter = (FollowFilter)tw.Filter;
			tw.Filter = TreeFilter.ANY_DIFF;
			tw.Reset(trees);
			IList<DiffEntry> files = DiffEntry.Scan(tw);
			RenameDetector rd = new RenameDetector(repository);
			rd.AddAll(files);
			files = rd.Compute();
			TreeFilter newFilter = oldFilter;
			foreach (DiffEntry ent in files)
			{
				if (IsRename(ent) && ent.GetNewPath().Equals(oldFilter.GetPath()))
				{
					newFilter = FollowFilter.Create(ent.GetOldPath());
					RenameCallback callback = oldFilter.GetRenameCallback();
					if (callback != null)
					{
						callback.Renamed(ent);
						// forward the callback to the new follow filter
						((FollowFilter)newFilter).SetRenameCallback(callback);
					}
					break;
				}
			}
			tw.Filter = newFilter;
		}

		private static bool IsRename(DiffEntry ent)
		{
			return ent.GetChangeType() == DiffEntry.ChangeType.RENAME || ent.GetChangeType() 
				== DiffEntry.ChangeType.COPY;
		}
	}
}
