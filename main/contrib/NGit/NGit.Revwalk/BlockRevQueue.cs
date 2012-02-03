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
	public abstract class BlockRevQueue : AbstractRevQueue
	{
		internal BlockRevQueue.BlockFreeList free;

		/// <summary>Create an empty revision queue.</summary>
		/// <remarks>Create an empty revision queue.</remarks>
		public BlockRevQueue()
		{
			free = new BlockRevQueue.BlockFreeList();
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal BlockRevQueue(Generator s)
		{
			free = new BlockRevQueue.BlockFreeList();
			outputType = s.OutputType();
			s.ShareFreeList(this);
			for (; ; )
			{
				RevCommit c = s.Next();
				if (c == null)
				{
					break;
				}
				Add(c);
			}
		}

		/// <summary>Reconfigure this queue to share the same free list as another.</summary>
		/// <remarks>
		/// Reconfigure this queue to share the same free list as another.
		/// <p>
		/// Multiple revision queues can be connected to the same free list, making
		/// it less expensive for applications to shuttle commits between them. This
		/// method arranges for the receiver to take from / return to the same free
		/// list as the supplied queue.
		/// <p>
		/// Free lists are not thread-safe. Applications must ensure that all queues
		/// sharing the same free list are doing so from only a single thread.
		/// </remarks>
		/// <param name="q">the other queue we will steal entries from.</param>
		internal override void ShareFreeList(NGit.Revwalk.BlockRevQueue q)
		{
			free = q.free;
		}

		internal sealed class BlockFreeList
		{
			private BlockRevQueue.Block next;

			internal BlockRevQueue.Block NewBlock()
			{
				BlockRevQueue.Block b = next;
				if (b == null)
				{
					return new BlockRevQueue.Block();
				}
				next = b.next;
				b.Clear();
				return b;
			}

			internal void FreeBlock(BlockRevQueue.Block b)
			{
				b.next = next;
				next = b;
			}

			internal void Clear()
			{
				next = null;
			}
		}

		internal sealed class Block
		{
			internal const int BLOCK_SIZE = 256;

			/// <summary>Next block in our chain of blocks; null if we are the last.</summary>
			/// <remarks>Next block in our chain of blocks; null if we are the last.</remarks>
			internal BlockRevQueue.Block next;

			/// <summary>Our table of queued commits.</summary>
			/// <remarks>Our table of queued commits.</remarks>
			internal readonly RevCommit[] commits = new RevCommit[BLOCK_SIZE];

			/// <summary>
			/// Next valid entry in
			/// <see cref="commits">commits</see>
			/// .
			/// </summary>
			internal int headIndex;

			/// <summary>
			/// Next free entry in
			/// <see cref="commits">commits</see>
			/// for addition at.
			/// </summary>
			internal int tailIndex;

			internal bool IsFull()
			{
				return tailIndex == BLOCK_SIZE;
			}

			internal bool IsEmpty()
			{
				return headIndex == tailIndex;
			}

			internal bool CanUnpop()
			{
				return headIndex > 0;
			}

			internal void Add(RevCommit c)
			{
				commits[tailIndex++] = c;
			}

			internal void Unpop(RevCommit c)
			{
				commits[--headIndex] = c;
			}

			internal RevCommit Pop()
			{
				return commits[headIndex++];
			}

			internal RevCommit Peek()
			{
				return commits[headIndex];
			}

			internal void Clear()
			{
				next = null;
				headIndex = 0;
				tailIndex = 0;
			}

			internal void ResetToMiddle()
			{
				headIndex = tailIndex = BLOCK_SIZE / 2;
			}

			internal void ResetToEnd()
			{
				headIndex = tailIndex = BLOCK_SIZE;
			}
		}
	}
}
