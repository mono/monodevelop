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
	/// <summary>A queue of commits sorted by commit time order.</summary>
	/// <remarks>A queue of commits sorted by commit time order.</remarks>
	public class DateRevQueue : AbstractRevQueue
	{
		private DateRevQueue.Entry head;

		private DateRevQueue.Entry free;

		/// <summary>Create an empty date queue.</summary>
		/// <remarks>Create an empty date queue.</remarks>
		public DateRevQueue() : base()
		{
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal DateRevQueue(Generator s)
		{
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

		public override void Add(RevCommit c)
		{
			DateRevQueue.Entry q = head;
			long when = c.commitTime;
			DateRevQueue.Entry n = NewEntry(c);
			if (q == null || when > q.commit.commitTime)
			{
				n.next = q;
				head = n;
			}
			else
			{
				DateRevQueue.Entry p = q.next;
				while (p != null && p.commit.commitTime > when)
				{
					q = p;
					p = q.next;
				}
				n.next = q.next;
				q.next = n;
			}
		}

		internal override RevCommit Next()
		{
			DateRevQueue.Entry q = head;
			if (q == null)
			{
				return null;
			}
			head = q.next;
			FreeEntry(q);
			return q.commit;
		}

		/// <summary>Peek at the next commit, without removing it.</summary>
		/// <remarks>Peek at the next commit, without removing it.</remarks>
		/// <returns>the next available commit; null if there are no commits left.</returns>
		public virtual RevCommit Peek()
		{
			return head != null ? head.commit : null;
		}

		public override void Clear()
		{
			head = null;
			free = null;
		}

		internal override bool EverbodyHasFlag(int f)
		{
			for (DateRevQueue.Entry q = head; q != null; q = q.next)
			{
				if ((q.commit.flags & f) == 0)
				{
					return false;
				}
			}
			return true;
		}

		internal override bool AnybodyHasFlag(int f)
		{
			for (DateRevQueue.Entry q = head; q != null; q = q.next)
			{
				if ((q.commit.flags & f) != 0)
				{
					return true;
				}
			}
			return false;
		}

		internal override int OutputType()
		{
			return outputType | SORT_COMMIT_TIME_DESC;
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			for (DateRevQueue.Entry q = head; q != null; q = q.next)
			{
				Describe(s, q.commit);
			}
			return s.ToString();
		}

		private DateRevQueue.Entry NewEntry(RevCommit c)
		{
			DateRevQueue.Entry r = free;
			if (r == null)
			{
				r = new DateRevQueue.Entry();
			}
			else
			{
				free = r.next;
			}
			r.commit = c;
			return r;
		}

		private void FreeEntry(DateRevQueue.Entry e)
		{
			e.next = free;
			free = e;
		}

		internal class Entry
		{
			internal DateRevQueue.Entry next;

			internal RevCommit commit;
		}
	}
}
