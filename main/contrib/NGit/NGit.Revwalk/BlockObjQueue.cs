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
	internal class BlockObjQueue
	{
		private BlockObjQueue.BlockFreeList free;

		private BlockObjQueue.Block head;

		private BlockObjQueue.Block tail;

		/// <summary>Create an empty queue.</summary>
		/// <remarks>Create an empty queue.</remarks>
		public BlockObjQueue()
		{
			free = new BlockObjQueue.BlockFreeList();
		}

		internal virtual void Add(RevObject c)
		{
			BlockObjQueue.Block b = tail;
			if (b == null)
			{
				b = free.NewBlock();
				b.Add(c);
				head = b;
				tail = b;
				return;
			}
			else
			{
				if (b.IsFull())
				{
					b = free.NewBlock();
					tail.next = b;
					tail = b;
				}
			}
			b.Add(c);
		}

		internal virtual RevObject Next()
		{
			BlockObjQueue.Block b = head;
			if (b == null)
			{
				return null;
			}
			RevObject c = b.Pop();
			if (b.IsEmpty())
			{
				head = b.next;
				if (head == null)
				{
					tail = null;
				}
				free.FreeBlock(b);
			}
			return c;
		}

		internal sealed class BlockFreeList
		{
			private BlockObjQueue.Block next;

			internal BlockObjQueue.Block NewBlock()
			{
				BlockObjQueue.Block b = next;
				if (b == null)
				{
					return new BlockObjQueue.Block();
				}
				next = b.next;
				b.Clear();
				return b;
			}

			internal void FreeBlock(BlockObjQueue.Block b)
			{
				b.next = next;
				next = b;
			}
		}

		internal sealed class Block
		{
			private const int BLOCK_SIZE = 256;

			/// <summary>Next block in our chain of blocks; null if we are the last.</summary>
			/// <remarks>Next block in our chain of blocks; null if we are the last.</remarks>
			internal BlockObjQueue.Block next;

			/// <summary>Our table of queued objects.</summary>
			/// <remarks>Our table of queued objects.</remarks>
			internal readonly RevObject[] objects = new RevObject[BLOCK_SIZE];

			/// <summary>
			/// Next valid entry in
			/// <see cref="objects">objects</see>
			/// .
			/// </summary>
			internal int headIndex;

			/// <summary>
			/// Next free entry in
			/// <see cref="objects">objects</see>
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

			internal void Add(RevObject c)
			{
				objects[tailIndex++] = c;
			}

			internal RevObject Pop()
			{
				return objects[headIndex++];
			}

			internal void Clear()
			{
				next = null;
				headIndex = 0;
				tailIndex = 0;
			}
		}
	}
}
