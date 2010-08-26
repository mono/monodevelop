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
	/// A queue of commits sorted by commit time order.
	/// </summary>
	public class DateRevQueue : AbstractRevQueue
	{
		private Entry _head;
		private Entry _free;

		/// <summary>
		/// Create an empty date queue.
		/// </summary>
		public DateRevQueue()
		{
		}

		public DateRevQueue(Generator s)
		{
			while (true)
			{
				RevCommit c = s.next();
				if (c == null) break;
				add(c);
			}
		}

		public override void add(RevCommit c)
		{
			Entry q = _head;
			long when = c.CommitTime;
			Entry n = NewEntry(c);
			if (q == null || when > q.Commit.CommitTime)
			{
				n.Next = q;
				_head = n;
			}
			else
			{
				Entry p = q.Next;
				while (p != null && p.Commit.CommitTime > when)
				{
					q = p;
					p = q.Next;
				}
				n.Next = q.Next;
				q.Next = n;
			}
		}

		public override RevCommit next()
		{
			Entry q = _head;
			if (q == null) return null;
			_head = q.Next;
			FreeEntry(q);
			return q.Commit;
		}

		/// <summary>
		/// Peek at the Next commit, without removing it.
		/// </summary>
		/// <returns>
		/// The Next available commit; null if there are no commits left.
		/// </returns>
		public RevCommit peek()
		{
			return _head != null ? _head.Commit : null;
		}

		public override void clear()
		{
			_head = null;
			_free = null;
		}

		internal override bool everbodyHasFlag(int f)
		{
			for (Entry q = _head; q != null; q = q.Next)
			{
				if ((q.Commit.Flags & f) == 0) return false;
			}
			return true;
		}

		internal override bool anybodyHasFlag(int f)
		{
			for (Entry q = _head; q != null; q = q.Next)
			{
				if ((q.Commit.Flags & f) != 0) return true;
			}
			return false;
		}

		public override GeneratorOutputType OutputType
		{
			get { return base.OutputType | GeneratorOutputType.SortCommitTimeDesc; }
		}

		public override string ToString()
		{
			var s = new StringBuilder();
			for (Entry q = _head; q != null; q = q.Next)
			{
				Describe(s, q.Commit);
			}
			return s.ToString();
		}

		private Entry NewEntry(RevCommit c)
		{
			Entry r = _free;
			if (r == null)
			{
				r = new Entry();
			}
			else
			{
				_free = r.Next;
			}
			r.Commit = c;
			return r;
		}

		private void FreeEntry(Entry e)
		{
			e.Next = _free;
			_free = e;
		}

		internal class Entry
		{
			public Entry Next { get; set; }
			public RevCommit Commit { get; set; }
		}
	}
}