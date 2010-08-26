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

namespace GitSharp.Core.RevWalk
{

    public class BlockObjQueue
    {
        private BlockFreeList free;

        private Block head;

        private Block tail;

        /** Create an empty queue. */
        public BlockObjQueue()
        {
            free = new BlockFreeList();
        }

        public void add(RevObject c)
        {
            Block b = tail;
            if (b == null)
            {
                b = free.newBlock();
                b.add(c);
                head = b;
                tail = b;
                return;
            }
            else if (b.isFull())
            {
                b = free.newBlock();
                tail.next = b;
                tail = b;
            }
            b.add(c);
        }

        public RevObject next()
        {
            Block b = head;
            if (b == null)
                return null;

            RevObject c = b.pop();
            if (b.isEmpty())
            {
                head = b.next;
                if (head == null)
                    tail = null;
                free.freeBlock(b);
            }
            return c;
        }

        public class BlockFreeList
        {
            private Block next;

            public Block newBlock()
            {
                Block b = next;
                if (b == null)
                    return new Block();
                next = b.next;
                b.clear();
                return b;
            }

            public void freeBlock(Block b)
            {
                b.next = next;
                next = b;
            }
        }

        public class Block
        {
            private static int BLOCK_SIZE = 256;

            /** Next block in our chain of blocks; null if we are the last. */
            public Block next;

            /** Our table of queued objects. */
            public RevObject[] objects = new RevObject[BLOCK_SIZE];

            /** Next valid entry in {@link #objects}. */
            public int headIndex;

            /** Next free entry in {@link #objects} for addition at. */
            public int tailIndex;

            public bool isFull()
            {
                return tailIndex == BLOCK_SIZE;
            }

            public bool isEmpty()
            {
                return headIndex == tailIndex;
            }

            public void add(RevObject c)
            {
                objects[tailIndex++] = c;
            }

            public RevObject pop()
            {
                return objects[headIndex++];
            }

            public void clear()
            {
                next = null;
                headIndex = 0;
                tailIndex = 0;
            }
        }
    }
}
