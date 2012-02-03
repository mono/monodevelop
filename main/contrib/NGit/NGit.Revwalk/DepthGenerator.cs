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
	/// <summary>Only produce commits which are below a specified depth.</summary>
	/// <remarks>Only produce commits which are below a specified depth.</remarks>
	/// <seealso cref="DepthWalk">DepthWalk</seealso>
	internal class DepthGenerator : Generator
	{
		private readonly FIFORevQueue pending;

		private readonly int depth;

		private readonly RevWalk walk;

		/// <summary>
		/// Commits which used to be shallow in the client, but which are
		/// being extended as part of this fetch.
		/// </summary>
		/// <remarks>
		/// Commits which used to be shallow in the client, but which are
		/// being extended as part of this fetch.  These commits should be
		/// returned to the caller as UNINTERESTING so that their blobs/trees
		/// can be marked appropriately in the pack writer.
		/// </remarks>
		private readonly RevFlag UNSHALLOW;

		/// <summary>
		/// Commits which the normal framework has marked as UNINTERESTING,
		/// but which we now care about again.
		/// </summary>
		/// <remarks>
		/// Commits which the normal framework has marked as UNINTERESTING,
		/// but which we now care about again.  This happens if a client is
		/// extending a shallow checkout to become deeper--the new commits at
		/// the bottom of the graph need to be sent, even though they are
		/// below other commits which the client already has.
		/// </remarks>
		private readonly RevFlag REINTERESTING;

		/// <param name="w"></param>
		/// <param name="s">Parent generator</param>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal DepthGenerator(DepthWalk w, Generator s)
		{
			pending = new FIFORevQueue();
			walk = (RevWalk)w;
			this.depth = w.GetDepth();
			this.UNSHALLOW = w.GetUnshallowFlag();
			this.REINTERESTING = w.GetReinterestingFlag();
			s.ShareFreeList(pending);
			// Begin by sucking out all of the source's commits, and
			// adding them to the pending queue
			for (; ; )
			{
				RevCommit c = s.Next();
				if (c == null)
				{
					break;
				}
				if (((NGit.Revwalk.Depthwalk.Commit)c).GetDepth() == 0)
				{
					pending.Add(c);
				}
			}
		}

		internal override int OutputType()
		{
			return pending.OutputType() | HAS_UNINTERESTING;
		}

		internal override void ShareFreeList(BlockRevQueue q)
		{
			pending.ShareFreeList(q);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			// Perform a breadth-first descent into the commit graph,
			// marking depths as we go.  This means that if a commit is
			// reachable by more than one route, we are guaranteed to
			// arrive by the shortest route first.
			for (; ; )
			{
				var c = (NGit.Revwalk.Depthwalk.Commit)pending.Next();
				if (c == null)
				{
					return null;
				}
				if ((c.flags & RevWalk.PARSED) == 0)
				{
					c.ParseHeaders(walk);
				}
				int newDepth = c.depth + 1;
				foreach (RevCommit p in c.parents)
				{
					var dp = (NGit.Revwalk.Depthwalk.Commit)p;
					// If no depth has been assigned to this commit, assign
					// it now.  Since we arrive by the shortest route first,
					// this depth is guaranteed to be the smallest value that
					// any path could produce.
					if (dp.depth == -1)
					{
						dp.depth = newDepth;
						// If the parent is not too deep, add it to the queue
						// so that we can produce it later
						if (newDepth <= depth)
						{
							pending.Add(p);
						}
					}
					// If the current commit has become unshallowed, everything
					// below us is new to the client.  Mark its parent as
					// re-interesting, and carry that flag downward to all
					// of its ancestors.
					if (c.Has(UNSHALLOW) || c.Has(REINTERESTING))
					{
						p.Add(REINTERESTING);
						p.flags &= ~RevWalk.UNINTERESTING;
					}
				}
				// Produce all commits less than the depth cutoff
				bool produce = c.depth <= depth;
				// Unshallow commits are uninteresting, but still need to be sent
				// up to the PackWriter so that it will exclude objects correctly.
				// All other uninteresting commits should be omitted.
				if ((c.flags & RevWalk.UNINTERESTING) != 0 && !c.Has(UNSHALLOW))
				{
					produce = false;
				}
				if (produce)
				{
					return c;
				}
			}
		}
	}
}
