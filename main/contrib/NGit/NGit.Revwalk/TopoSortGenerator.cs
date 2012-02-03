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
	/// <summary>Sorts commits in topological order.</summary>
	/// <remarks>Sorts commits in topological order.</remarks>
	internal class TopoSortGenerator : Generator
	{
		private const int TOPO_DELAY = RevWalk.TOPO_DELAY;

		private readonly FIFORevQueue pending;

		private readonly int outputType;

		/// <summary>Create a new sorter and completely spin the generator.</summary>
		/// <remarks>
		/// Create a new sorter and completely spin the generator.
		/// <p>
		/// When the constructor completes the supplied generator will have no
		/// commits remaining, as all of the commits will be held inside of this
		/// generator's internal buffer.
		/// </remarks>
		/// <param name="s">generator to pull all commits out of, and into this buffer.</param>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal TopoSortGenerator(Generator s)
		{
			pending = new FIFORevQueue();
			outputType = s.OutputType() | SORT_TOPO;
			s.ShareFreeList(pending);
			for (; ; )
			{
				RevCommit c = s.Next();
				if (c == null)
				{
					break;
				}
				foreach (RevCommit p in c.parents)
				{
					p.inDegree++;
				}
				pending.Add(c);
			}
		}

		internal override int OutputType()
		{
			return outputType;
		}

		internal override void ShareFreeList(BlockRevQueue q)
		{
			q.ShareFreeList(pending);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			for (; ; )
			{
				RevCommit c = pending.Next();
				if (c == null)
				{
					return null;
				}
				if (c.inDegree > 0)
				{
					// At least one of our children is missing. We delay
					// production until all of our children are output.
					//
					c.flags |= TOPO_DELAY;
					continue;
				}
				// All of our children have already produced,
				// so it is OK for us to produce now as well.
				//
				foreach (RevCommit p in c.parents)
				{
					if (--p.inDegree == 0 && (p.flags & TOPO_DELAY) != 0)
					{
						// This parent tried to come before us, but we are
						// his last child. unpop the parent so it goes right
						// behind this child.
						//
						p.flags &= ~TOPO_DELAY;
						pending.Unpop(p);
					}
				}
				return c;
			}
		}
	}
}
