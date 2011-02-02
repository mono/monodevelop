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

using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Default (and first pass) RevCommit Generator implementation for RevWalk.
	/// 	</summary>
	/// <remarks>
	/// Default (and first pass) RevCommit Generator implementation for RevWalk.
	/// <p>
	/// This generator starts from a set of one or more commits and process them in
	/// descending (newest to oldest) commit time order. Commits automatically cause
	/// their parents to be enqueued for further processing, allowing the entire
	/// commit graph to be walked. A
	/// <see cref="NGit.Revwalk.Filter.RevFilter">NGit.Revwalk.Filter.RevFilter</see>
	/// may be used to select a subset
	/// of the commits and return them to the caller.
	/// </remarks>
	internal class PendingGenerator : Generator
	{
		private const int PARSED = RevWalk.PARSED;

		private const int SEEN = RevWalk.SEEN;

		private const int UNINTERESTING = RevWalk.UNINTERESTING;

		/// <summary>Number of additional commits to scan after we think we are done.</summary>
		/// <remarks>
		/// Number of additional commits to scan after we think we are done.
		/// <p>
		/// This small buffer of commits is scanned to ensure we didn't miss anything
		/// as a result of clock skew when the commits were made. We need to set our
		/// constant to 1 additional commit due to the use of a pre-increment
		/// operator when accessing the value.
		/// </remarks>
		internal const int OVER_SCAN = 5 + 1;

		/// <summary>
		/// A commit near the end of time, to initialize
		/// <see cref="last">last</see>
		/// with.
		/// </summary>
		private static readonly RevCommit INIT_LAST;

		static PendingGenerator()
		{
			INIT_LAST = new RevCommit(ObjectId.ZeroId);
			INIT_LAST.commitTime = int.MaxValue;
		}

		private readonly RevWalk walker;

		private readonly DateRevQueue pending;

		private readonly RevFilter filter;

		private readonly int output;

		/// <summary>
		/// Last commit produced to the caller from
		/// <see cref="Next()">Next()</see>
		/// .
		/// </summary>
		private RevCommit last = INIT_LAST;

		/// <summary>Number of commits we have remaining in our over-scan allotment.</summary>
		/// <remarks>
		/// Number of commits we have remaining in our over-scan allotment.
		/// <p>
		/// Only relevant if there are
		/// <see cref="UNINTERESTING">UNINTERESTING</see>
		/// commits in the
		/// <see cref="pending">pending</see>
		/// queue.
		/// </remarks>
		private int overScan = OVER_SCAN;

		internal bool canDispose;

		internal PendingGenerator(RevWalk w, DateRevQueue p, RevFilter f, int @out)
		{
			walker = w;
			pending = p;
			filter = f;
			output = @out;
			canDispose = true;
		}

		internal override int OutputType()
		{
			return output | SORT_COMMIT_TIME_DESC;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal override RevCommit Next()
		{
			try
			{
				for (; ; )
				{
					RevCommit c = pending.Next();
					if (c == null)
					{
						walker.reader.WalkAdviceEnd();
						if (!(walker is ObjectWalk))
						{
							walker.reader.Release();
						}
						return null;
					}
					bool produce;
					if ((c.flags & UNINTERESTING) != 0)
					{
						produce = false;
					}
					else
					{
						c.ParseBody(walker);
						produce = filter.Include(walker, c);
					}
					foreach (RevCommit p in c.parents)
					{
						if ((p.flags & SEEN) != 0)
						{
							continue;
						}
						if ((p.flags & PARSED) == 0)
						{
							p.ParseHeaders(walker);
						}
						p.flags |= SEEN;
						pending.Add(p);
					}
					walker.CarryFlagsImpl(c);
					if ((c.flags & UNINTERESTING) != 0)
					{
						if (pending.EverbodyHasFlag(UNINTERESTING))
						{
							RevCommit n = pending.Peek();
							if (n != null && n.commitTime >= last.commitTime)
							{
								// This is too close to call. The next commit we
								// would pop is dated after the last one produced.
								// We have to keep going to ensure that we carry
								// flags as much as necessary.
								//
								overScan = OVER_SCAN;
							}
							else
							{
								if (--overScan == 0)
								{
									throw StopWalkException.INSTANCE;
								}
							}
						}
						else
						{
							overScan = OVER_SCAN;
						}
						if (canDispose)
						{
							c.DisposeBody();
						}
						continue;
					}
					if (produce)
					{
						return last = c;
					}
					else
					{
						if (canDispose)
						{
							c.DisposeBody();
						}
					}
				}
			}
			catch (StopWalkException)
			{
				walker.reader.WalkAdviceEnd();
				walker.reader.Release();
				pending.Clear();
				return null;
			}
		}
	}
}
