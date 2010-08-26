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

using System;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk.Filter;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Default (and first pass) RevCommit Generator implementation for RevWalk.
	/// <para />
	/// This generator starts from a set of one or more commits and process them in
	/// descending (newest to oldest) commit time order. Commits automatically cause
	/// their parents to be enqueued for further processing, allowing the entire
	/// commit graph to be walked. A <see cref="RevFilter"/> may be used to select a subset
	/// of the commits and return them to the caller.
	/// </summary>
	public class PendingGenerator : Generator, IDisposable
	{
		private static readonly RevCommit InitLast;

		/**
		 * Number of additional commits to scan After we think we are done.
		 * <para />
		 * This small buffer of commits is scanned to ensure we didn't miss anything
		 * as a result of clock skew when the commits were made. We need to set our
		 * constant to 1 additional commit due to the use of a pre-increment
		 * operator when accessing the value.
		 */
		public static readonly int OVER_SCAN = 5 + 1;
		public static readonly int PARSED = RevWalk.PARSED;
		public static readonly int SEEN = RevWalk.SEEN;
		public static readonly int UNINTERESTING = RevWalk.UNINTERESTING;

		private readonly RevFilter _filter;
		private readonly GeneratorOutputType _outputType;
		private readonly DateRevQueue _pending;
		private readonly RevWalk _walker;

		public bool CanDispose { get; set; }

		/** Last commit produced to the caller from {@link #Next()}. */
		private RevCommit _last = InitLast;

		/** 
         * Number of commits we have remaining in our over-scan allotment.
         * <para />
         * Only relevant if there are {@link #UNINTERESTING} commits in the
         * {@link #_pending} queue.
         */
		private int _overScan = OVER_SCAN;

		static PendingGenerator()
		{
			InitLast = new RevCommit(ObjectId.ZeroId) { CommitTime = int.MaxValue };
		}

		public PendingGenerator(RevWalk w, DateRevQueue p, RevFilter f, GeneratorOutputType outputType)
		{
			_walker = w;
			_pending = p;
			_filter = f;
			_outputType = outputType;
			CanDispose = true;
		}

		public override GeneratorOutputType OutputType
		{
			get { return _outputType | GeneratorOutputType.SortCommitTimeDesc; }
		}

		public override RevCommit next()
		{
			try
			{
				while (true)
				{
					RevCommit c = _pending.next();
					if (c == null)
					{
						_walker.WindowCursor.Release();
						return null;
					}

					bool produce = !((c.Flags & UNINTERESTING) != 0) && _filter.include(_walker, c);

					foreach (RevCommit p in c.Parents)
					{
						if ((p.Flags & SEEN) != 0) continue;
						if ((p.Flags & PARSED) == 0)
						{
							p.parseHeaders(_walker);
						}
						p.Flags |= SEEN;
						_pending.add(p);
					}
					_walker.carryFlagsImpl(c);

					if ((c.Flags & UNINTERESTING) != 0)
					{
						if (_pending.everbodyHasFlag(UNINTERESTING))
						{
							RevCommit n = _pending.peek();
							if (n != null && n.CommitTime >= _last.CommitTime)
							{
								// This is too close to call. The Next commit we
								// would pop is dated After the last one produced.
								// We have to keep going to ensure that we carry
								// flags as much as necessary.
								//
								_overScan = OVER_SCAN;
							}
							else if (--_overScan == 0)
							{
								throw StopWalkException.INSTANCE;
							}
						}
						else
						{
							_overScan = OVER_SCAN;
						}
						if (CanDispose)
						{
						    c.DisposeBody();
						}
						continue;
					}

					if (produce)
					{
						return _last = c;
					}

					if (CanDispose)
					{
					    c.DisposeBody();
					}
				}
			}
			catch (StopWalkException)
			{
				_walker.WindowCursor.Release();
				_pending.clear();
				return null;
			}
		}
		
		public void Dispose ()
		{
			_walker.Dispose();
		}
		
	}
}