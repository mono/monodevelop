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
	internal class BoundaryGenerator : Generator
	{
		internal const int UNINTERESTING = RevWalk.UNINTERESTING;

		internal Generator g;

		internal BoundaryGenerator(RevWalk w, Generator s)
		{
			g = new BoundaryGenerator.InitialGenerator(this, w, s);
		}

		internal override int OutputType()
		{
			return g.OutputType() | HAS_UNINTERESTING;
		}

		internal override void ShareFreeList(BlockRevQueue q)
		{
			g.ShareFreeList(q);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			return g.Next();
		}

		private class InitialGenerator : Generator
		{
			private const int PARSED = RevWalk.PARSED;

			private const int DUPLICATE = RevWalk.TEMP_MARK;

			private readonly RevWalk walk;

			private readonly FIFORevQueue held;

			private readonly Generator source;

			internal InitialGenerator(BoundaryGenerator _enclosing, RevWalk w, Generator s)
			{
				this._enclosing = _enclosing;
				this.walk = w;
				this.held = new FIFORevQueue();
				this.source = s;
				this.source.ShareFreeList(this.held);
			}

			internal override int OutputType()
			{
				return this.source.OutputType();
			}

			internal override void ShareFreeList(BlockRevQueue q)
			{
				q.ShareFreeList(this.held);
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			internal override RevCommit Next()
			{
				RevCommit c = this.source.Next();
				if (c != null)
				{
					foreach (RevCommit p in c.parents)
					{
						if ((p.flags & BoundaryGenerator.UNINTERESTING) != 0)
						{
							this.held.Add(p);
						}
					}
					return c;
				}
				FIFORevQueue boundary = new FIFORevQueue();
				boundary.ShareFreeList(this.held);
				for (; ; )
				{
					c = this.held.Next();
					if (c == null)
					{
						break;
					}
					if ((c.flags & BoundaryGenerator.InitialGenerator.DUPLICATE) != 0)
					{
						continue;
					}
					if ((c.flags & BoundaryGenerator.InitialGenerator.PARSED) == 0)
					{
						c.ParseHeaders(this.walk);
					}
					c.flags |= BoundaryGenerator.InitialGenerator.DUPLICATE;
					boundary.Add(c);
				}
				boundary.RemoveFlag(BoundaryGenerator.InitialGenerator.DUPLICATE);
				this._enclosing.g = boundary;
				return boundary.Next();
			}

			private readonly BoundaryGenerator _enclosing;
		}
	}
}
