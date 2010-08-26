/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
    public abstract class BlockRevQueue : AbstractRevQueue
    {
        private BlockFreeList _free;

		/// <summary>
		/// Create an empty revision queue.
		/// </summary>
		protected BlockRevQueue()
			: base(GeneratorOutputType.None)
		{
			_free = new BlockFreeList();
		}

        /// <summary>
		/// Create an empty revision queue.
        /// </summary>
    	protected BlockRevQueue(GeneratorOutputType outputType)
			: base(outputType)
        {
            _free = new BlockFreeList();
        }

    	protected BlockRevQueue(Generator s)
			: this(s.OutputType)
        {
			_free = new BlockFreeList();
            s.shareFreeList(this);
        
			while(true)
            {
                RevCommit c = s.next();
                if (c == null) break;
                add(c);
            }
        }

		public BlockFreeList Free
		{
			get { return _free; }
		}

        /// <summary>
        /// Reconfigure this queue to share the same free list as another.
		/// <para />
		/// Multiple revision queues can be connected to the same free list, making
		/// it less expensive for applications to shuttle commits between them. This
		/// method arranges for the receiver to take from / return to the same free
		/// list as the supplied queue.
		/// <para />
		/// Free lists are not thread-safe. Applications must ensure that all queues
		/// sharing the same free list are doing so from only a single thread.
        /// </summary>
		/// <param name="q">the other queue we will steal entries from.</param>
        public override void shareFreeList(BlockRevQueue q)
        {
            _free = q._free;
        }

    	#region Nested Types

    	public class BlockFreeList
    	{
    		private Block _next;

    		public Block newBlock()
    		{
    			Block b = _next;
    			if (b == null)
    			{
    				return new Block();
    			}
    			_next = b.Next;
    			b.clear();
    			return b;
    		}

    		public void freeBlock(Block b)
    		{
    			b.Next = _next;
    			_next = b;
    		}

    		public void clear()
    		{
    			_next = null;
    		}
    	}

    	public class Block
    	{
    		public static readonly int BLOCK_SIZE = 256;
    		private readonly RevCommit[] _commits;

    		public Block()
			{
				_commits = new RevCommit[BLOCK_SIZE];
			}

    		/// <summary>
    		/// Next free entry in <see cref="Commits"/> for addition at.
    		/// </summary>
    		public int TailIndex { get; set; }

    		/// <summary>
    		/// Next valid entry in <see cref="Commits"/>.
    		/// </summary>
    		public int HeadIndex { get; set; }

    		/// <summary>
			/// Our table of queued commits.
			/// </summary>
    		public RevCommit[] Commits
    		{
    			get { return _commits; }
    		}

    		/// <summary>
    		/// Next block in our chain of blocks; null if we are the last.
    		/// </summary>
    		public Block Next { get; set; }

    		public bool isFull()
    		{
    			return TailIndex == BLOCK_SIZE;
    		}

    		public bool isEmpty()
    		{
    			return HeadIndex == TailIndex;
    		}

    		public bool canUnpop()
    		{
    			return HeadIndex > 0;
    		}

    		public void add(RevCommit c)
    		{
    			_commits[TailIndex++] = c;
    		}

    		public void unpop(RevCommit c)
    		{
    			_commits[--HeadIndex] = c;
    		}

    		public RevCommit pop()
    		{
    			return _commits[HeadIndex++];
    		}

    		public RevCommit peek()
    		{
    			return _commits[HeadIndex];
    		}

    		public void clear()
    		{
    			Next = null;
    			HeadIndex = 0;
    			TailIndex = 0;
    		}

    		public void resetToMiddle()
    		{
    			HeadIndex = TailIndex = BLOCK_SIZE / 2;
    		}

    		public void resetToEnd()
    		{
    			HeadIndex = TailIndex = BLOCK_SIZE;
    		}
    	}

    	#endregion

    }
}