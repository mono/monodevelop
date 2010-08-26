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

using System.Text;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// A queue of commits in LIFO order.
	/// </summary>
	public class LIFORevQueue : BlockRevQueue
	{
		private Block _head;

		/// <summary>
		/// Create an empty LIFO queue.
		/// </summary>
		internal LIFORevQueue()
		{
		}

		public LIFORevQueue(Generator s)
			: base(s)
		{
		}

		public override void add(RevCommit c)
		{
			Block b = _head;
			if (b == null || !b.canUnpop())
			{
				b = Free.newBlock();
				b.resetToEnd();
				b.Next = _head;
				_head = b;
			}
			b.unpop(c);
		}

		public override RevCommit next()
		{
			Block b = _head;
			if (b == null) return null;

			RevCommit c = b.pop();
			if (b.isEmpty())
			{
				_head = b.Next;
				Free.freeBlock(b);
			}
			return c;
		}

		public override void clear()
		{
			_head = null;
			Free.clear();
		}

		internal override bool everbodyHasFlag(int f)
		{
			for (Block b = _head; b != null; b = b.Next)
			{
				for (int i = b.HeadIndex; i < b.TailIndex; i++)
				{
					if ((b.Commits[i].Flags & f) == 0) return false;
				}
			}

			return true;
		}

		internal override bool anybodyHasFlag(int f)
		{
			for (Block b = _head; b != null; b = b.Next)
			{
				for (int i = b.HeadIndex; i < b.TailIndex; i++)
				{
					if ((b.Commits[i].Flags & f) != 0) return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			var s = new StringBuilder();
			for (Block q = _head; q != null; q = q.Next)
			{
				for (int i = q.HeadIndex; i < q.TailIndex; i++)
				{
					Describe(s, q.Commits[i]);
				}
			}
			return s.ToString();
		}
	}
}