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
using GitSharp.Core.RevWalk.Filter;

namespace GitSharp.Core.RevWalk
{
    /// <summary>
    /// Computes the merge base(s) of the starting commits.
	/// <para />
	/// This generator is selected if the RevFilter is only
	/// <see cref="RevFilter.MERGE_BASE"/>.
	/// <para />
	/// To compute the merge base we assign a temporary flag to each of the starting
	/// commits. The maximum number of starting commits is bounded by the number of
	/// free flags available in the RevWalk when the generator is initialized. These
	/// flags will be automatically released on the next reset of the RevWalk, but
	/// not until then, as they are assigned to commits throughout the history.
	/// <para />
	/// Several internal flags are reused here for a different purpose, but this
	/// should not have any impact as this generator should be run alone, and without
	/// any other generators wrapped around it.
	/// </summary>
    public class MergeBaseGenerator : Generator, IDisposable
    {
        private const int Parsed = RevWalk.PARSED;
        private const int InPending = RevWalk.SEEN;
        private const int Popped = RevWalk.TEMP_MARK;
        private const int MergeBase = RevWalk.REWRITE;
        private readonly RevWalk _walker;
        private readonly DateRevQueue _pending;
        private int _branchMask;
        private int _recarryTest;
        private int _recarryMask;

        public MergeBaseGenerator(RevWalk w)
        {
            _walker = w;
            _pending = new DateRevQueue();
        }

        public void init(AbstractRevQueue p)
        {
            try
            {
                while (true)
                {
                    RevCommit c = p.next();
                    if (c == null) break;
                    Add(c);
                }
            }
            finally
            {
                // Always free the flags immediately. This ensures the flags
                // will be available for reuse when the walk resets.
                //
                _walker.freeFlag(_branchMask);

                // Setup the condition used by CarryOntoOne to detect a late
                // merge base and produce it on the next round.
                //
                _recarryTest = _branchMask | Popped;
                _recarryMask = _branchMask | Popped | MergeBase;
            }
        }

        private void Add(RevCommit c)
        {
            int flag = _walker.allocFlag();
            _branchMask |= flag;
            if ((c.Flags & _branchMask) != 0)
            {
                // This should never happen. RevWalk ensures we get a
                // commit admitted to the initial queue only once. If
                // we see this marks aren't correctly erased.
                //
                throw new InvalidOperationException("Stale RevFlags on " + c);
            }
            c.Flags |= flag;
            _pending.add(c);
        }

        public override GeneratorOutputType OutputType
        {
			get { return GeneratorOutputType.None; }
        }

        public override RevCommit next()
        {
            while (true)
            {
                RevCommit c = _pending.next();
                if (c == null)
                {
                    _walker.WindowCursor.Release();
                    return null;
                }

                foreach (RevCommit p in c.Parents)
                {
                    if ((p.Flags & InPending) != 0) continue;
                    if ((p.Flags & Parsed) == 0)
                    {
                    	p.parseHeaders(_walker);
                    }
                    p.Flags |= InPending;
                    _pending.add(p);
                }

                int carry = c.Flags & _branchMask;
                bool mb = carry == _branchMask;
                if (mb)
                {
                    // If we are a merge base make sure our ancestors are
                    // also flagged as being popped, so that they do not
                    // generate to the caller.
                    //
                    carry |= MergeBase;
                }
                CarryOntoHistory(c, carry);

                if ((c.Flags & MergeBase) != 0)
                {
                    // This commit is an ancestor of a merge base we already
                    // popped back to the caller. If everyone in pending is
                    // that way we are done traversing; if not we just need
                    // to move to the next available commit and try again.
                    //
                    if (_pending.everbodyHasFlag(MergeBase)) return null;
                    continue;
                }
                c.Flags |= Popped;

            	if (!mb) continue;
            	c.Flags |= MergeBase;
            	return c;
            }
        }

        private void CarryOntoHistory(RevCommit c, int carry)
        {
            while (true)
            {
                RevCommit[] pList = c.Parents;
                if (pList == null) return;
                int n = pList.Length;
                if (n == 0) return;

                for (int i = 1; i < n; i++)
                {
                    RevCommit p = pList[i];
                    if (!CarryOntoOne(p, carry))
                    {
                    	CarryOntoHistory(p, carry);
                    }
                }

                c = pList[0];
                if (CarryOntoOne(c, carry)) break;
            }
        }

        private bool CarryOntoOne(RevCommit p, int carry)
        {
            bool haveAll = (p.Flags & carry) == carry;
            p.Flags |= carry;

            if ((p.Flags & _recarryMask) == _recarryTest)
            {
                // We were popped without being a merge base, but we just got
                // voted to be one. Inject ourselves back at the front of the
                // pending queue and tell all of our ancestors they are within
                // the merge base now.
                //
                p.Flags &= ~Popped;
                _pending.add(p);
                CarryOntoHistory(p, _branchMask | MergeBase);
                return true;
            }

            // If we already had all carried flags, our parents do too.
            // Return true to stop the caller from running down this leg
            // of the revision graph any further.
            //
            return haveAll;
        }
		
		public void Dispose ()
		{
			_walker.Dispose();
		}
		
    }
}