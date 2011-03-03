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
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Replaces a RevCommit's parents until not colored with REWRITE.</summary>
	/// <remarks>
	/// Replaces a RevCommit's parents until not colored with REWRITE.
	/// <p>
	/// Before a RevCommit is returned to the caller its parents are updated to
	/// create a dense DAG. Instead of reporting the actual parents as recorded when
	/// the commit was created the returned commit will reflect the next closest
	/// commit that matched the revision walker's filters.
	/// <p>
	/// This generator is the second phase of a path limited revision walk and
	/// assumes it is receiving RevCommits from
	/// <see cref="RewriteTreeFilter">RewriteTreeFilter</see>
	/// ,
	/// after they have been fully buffered by
	/// <see cref="AbstractRevQueue">AbstractRevQueue</see>
	/// . The full
	/// buffering is necessary to allow the simple loop used within our own
	/// <see cref="Rewrite(RevCommit)">Rewrite(RevCommit)</see>
	/// to pull completely through a strand of
	/// <see cref="RevWalk.REWRITE">RevWalk.REWRITE</see>
	/// colored commits and come up with a simplification
	/// that makes the DAG dense. Not fully buffering the commits first would cause
	/// this loop to abort early, due to commits not being parsed and colored
	/// correctly.
	/// </remarks>
	/// <seealso cref="RewriteTreeFilter">RewriteTreeFilter</seealso>
	internal class RewriteGenerator : Generator
	{
		private const int REWRITE = RevWalk.REWRITE;

		/// <summary>
		/// For
		/// <see cref="Cleanup(RevCommit[])">Cleanup(RevCommit[])</see>
		/// to remove duplicate parents.
		/// </summary>
		private const int DUPLICATE = RevWalk.TEMP_MARK;

		private readonly Generator source;

		internal RewriteGenerator(Generator s)
		{
			source = s;
		}

		internal override void ShareFreeList(BlockRevQueue q)
		{
			source.ShareFreeList(q);
		}

		internal override int OutputType()
		{
			return source.OutputType() & ~NEEDS_REWRITE;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			for (; ; )
			{
				RevCommit c = source.Next();
				if (c == null)
				{
					return null;
				}
				bool rewrote = false;
				RevCommit[] pList = c.parents;
				int nParents = pList.Length;
				for (int i = 0; i < nParents; i++)
				{
					RevCommit oldp = pList[i];
					RevCommit newp = Rewrite(oldp);
					if (oldp != newp)
					{
						pList[i] = newp;
						rewrote = true;
					}
				}
				if (rewrote)
				{
					c.parents = Cleanup(pList);
				}
				return c;
			}
		}

		private RevCommit Rewrite(RevCommit p)
		{
			for (; ; )
			{
				RevCommit[] pList = p.parents;
				if (pList.Length > 1)
				{
					// This parent is a merge, so keep it.
					//
					return p;
				}
				if ((p.flags & RevWalk.UNINTERESTING) != 0)
				{
					// Retain uninteresting parents. They show where the
					// DAG was cut off because it wasn't interesting.
					//
					return p;
				}
				if ((p.flags & REWRITE) == 0)
				{
					// This parent was not eligible for rewriting. We
					// need to keep it in the DAG.
					//
					return p;
				}
				if (pList.Length == 0)
				{
					// We can't go back any further, other than to
					// just delete the parent entirely.
					//
					return null;
				}
				p = pList[0];
			}
		}

		private RevCommit[] Cleanup(RevCommit[] oldList)
		{
			// Remove any duplicate parents caused due to rewrites (e.g. a merge
			// with two sides that both simplified back into the merge base).
			// We also may have deleted a parent by marking it null.
			//
			int newCnt = 0;
			for (int o = 0; o < oldList.Length; o++)
			{
				RevCommit p = oldList[o];
				if (p == null)
				{
					continue;
				}
				if ((p.flags & DUPLICATE) != 0)
				{
					oldList[o] = null;
					continue;
				}
				p.flags |= DUPLICATE;
				newCnt++;
			}
			if (newCnt == oldList.Length)
			{
				foreach (RevCommit p in oldList)
				{
					p.flags &= ~DUPLICATE;
				}
				return oldList;
			}
			RevCommit[] newList = new RevCommit[newCnt];
			newCnt = 0;
			foreach (RevCommit p_1 in oldList)
			{
				if (p_1 != null)
				{
					newList[newCnt++] = p_1;
					p_1.flags &= ~DUPLICATE;
				}
			}
			return newList;
		}
	}
}
