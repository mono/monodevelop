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

using System.Text;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>A queue of commits in LIFO order.</summary>
	/// <remarks>A queue of commits in LIFO order.</remarks>
	public class LIFORevQueue : BlockRevQueue
	{
		private BlockRevQueue.Block head;

		/// <summary>Create an empty LIFO queue.</summary>
		/// <remarks>Create an empty LIFO queue.</remarks>
		public LIFORevQueue() : base()
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal LIFORevQueue(Generator s) : base(s)
		{
		}

		public override void Add(RevCommit c)
		{
			BlockRevQueue.Block b = head;
			if (b == null || !b.CanUnpop())
			{
				b = free.NewBlock();
				b.ResetToEnd();
				b.next = head;
				head = b;
			}
			b.Unpop(c);
		}

		internal override RevCommit Next()
		{
			BlockRevQueue.Block b = head;
			if (b == null)
			{
				return null;
			}
			RevCommit c = b.Pop();
			if (b.IsEmpty())
			{
				head = b.next;
				free.FreeBlock(b);
			}
			return c;
		}

		public override void Clear()
		{
			head = null;
			free.Clear();
		}

		internal override bool EverbodyHasFlag(int f)
		{
			for (BlockRevQueue.Block b = head; b != null; b = b.next)
			{
				for (int i = b.headIndex; i < b.tailIndex; i++)
				{
					if ((b.commits[i].flags & f) == 0)
					{
						return false;
					}
				}
			}
			return true;
		}

		internal override bool AnybodyHasFlag(int f)
		{
			for (BlockRevQueue.Block b = head; b != null; b = b.next)
			{
				for (int i = b.headIndex; i < b.tailIndex; i++)
				{
					if ((b.commits[i].flags & f) != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			for (BlockRevQueue.Block q = head; q != null; q = q.next)
			{
				for (int i = q.headIndex; i < q.tailIndex; i++)
				{
					Describe(s, q.commits[i]);
				}
			}
			return s.ToString();
		}
	}
}
