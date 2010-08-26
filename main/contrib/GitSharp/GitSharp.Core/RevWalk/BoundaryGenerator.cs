/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace GitSharp.Core.RevWalk
{
	public class BoundaryGenerator : Generator
	{
		private Generator _generator;

		public BoundaryGenerator(RevWalk w, Generator s)
		{
			_generator = new InitialGenerator(w, s, this);
		}

		public override GeneratorOutputType OutputType
		{
			get { return _generator.OutputType | GeneratorOutputType.HasUninteresting; }
		}

		public override void shareFreeList(BlockRevQueue q)
		{
			_generator.shareFreeList(q);
		}

		public override RevCommit next()
		{
			return _generator.next();
		}

		#region Nested Types

		private class InitialGenerator : Generator, IDisposable
		{
			private static readonly int Parsed = RevWalk.PARSED;
			private static readonly int Duplicate = RevWalk.TEMP_MARK;

			private readonly RevWalk _walk;
			private readonly FIFORevQueue _held;
			private readonly Generator _source;
			private readonly BoundaryGenerator _parent;

			public InitialGenerator(RevWalk w, Generator s, BoundaryGenerator parent) // [henon] parent needed because we cannot access outer instances in C#
			{
				_walk = w;
				_held = new FIFORevQueue();
				_source = s;
				_source.shareFreeList(_held);
				_parent = parent;
			}

			public override GeneratorOutputType OutputType
			{
				get { return _source.OutputType; }
			}

			public override void shareFreeList(BlockRevQueue q)
			{
				q.shareFreeList(_held);
			}

			public override RevCommit next()
			{
				RevCommit c = _source.next();
				if (c != null)
				{
					foreach (RevCommit p in c.Parents)
					{
						if ((p.Flags & RevWalk.UNINTERESTING) != 0)
						{
							_held.add(p);
						}
					}
					return c;
				}

				var boundary = new FIFORevQueue();
				boundary.shareFreeList(_held);
				while (true)
				{
					c = _held.next();
					if (c == null)
						break;
					if ((c.Flags & Duplicate) != 0) continue;
					if ((c.Flags & Parsed) == 0)
					{
                        c.parseHeaders(_walk);
					}
					c.Flags |= Duplicate;
					boundary.add(c);
				}
				boundary.removeFlag(Duplicate);
				_parent._generator = boundary;
				return boundary.next();
			}
			
			public void Dispose ()
			{
				_walk.Dispose();
			}
			
		}

		#endregion
	}
}