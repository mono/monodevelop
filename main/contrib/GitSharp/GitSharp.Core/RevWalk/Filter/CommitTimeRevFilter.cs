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
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Selects commits based upon the commit time field.
	/// </summary>
	public abstract class CommitTimeRevFilter : RevFilter
	{
		private readonly int _when;  // seconds since  epoch, will overflow 2038.

		/// <summary>
		/// Create a new filter to select commits before a given date/time.
		/// </summary>
		/// <param name="ts">the point in time to cut on.</param>
		/// <returns>
		/// a new filter to select commits on or before <paramref name="ts"/>.
		/// </returns>
		public static RevFilter Before(DateTime ts)
		{
            return new BeforeCommitTimeRevFilter(ts.ToMillisecondsSinceEpoch());
		}

		/// <summary>
		/// Create a new filter to select commits After a given date/time.
		/// </summary>
		/// <param name="ts">the point in time to cut on.</param>
		/// <returns>
		/// a new filter to select commits on or After <paramref name="ts"/>.
		/// </returns>
		public static RevFilter After(DateTime ts)
		{
			return new AfterCommitTimeRevFilter(ts.ToMillisecondsSinceEpoch());
		}

	    /// <summary>
	    /// Create a new filter to select commits after or equal a given date/time <code>since</code>
	    /// and before or equal a given date/time <code>until</code>.
	    /// </summary>
	    /// <param name="since"> the point in time to cut on.</param>
	    /// <param name="until"> the point in time to cut off.</param>
	    /// <returns>a new filter to select commits between the given date/times.</returns>
	    public static RevFilter Between(DateTime since, DateTime until)
	    {
	        return new BetweenCommitTimeRevFilter(since.ToMillisecondsSinceEpoch(), until.ToMillisecondsSinceEpoch());
	    }

		private CommitTimeRevFilter(long ts)
		{
			_when = (int)(ts / 1000);
		}

		public override RevFilter Clone()
		{
			return this;
		}

		#region Nested Types

		private class BeforeCommitTimeRevFilter : CommitTimeRevFilter
		{
			/// <summary>
			///
			/// </summary>
			/// <param name="ts">git internal time (seconds since epoch)</param>
			public BeforeCommitTimeRevFilter(long ts)
				: base(ts)
			{
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return cmit.CommitTime <= _when;
			}

            public override string ToString()
            {
                return base.ToString() + "(" + ((long)_when * 1000).MillisToUtcDateTime() + ")";
            }
		}

		private class AfterCommitTimeRevFilter : CommitTimeRevFilter
		{
			/// <summary>
			///
			/// </summary>
			/// <param name="ts">git internal time (seconds since epoch)</param>
			public AfterCommitTimeRevFilter(long ts)
				: base(ts)
			{
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				// Since the walker sorts commits by commit time we can be
				// reasonably certain there is nothing remaining worth our
				// scanning if this commit is before the point in question.
				//
				if (cmit.CommitTime < _when)
				{
					throw StopWalkException.INSTANCE;
				}

				return true;
			}

            public override string ToString()
            {
                return base.ToString() + "(" + ((long)_when * 1000).MillisToUtcDateTime() + ")";
            }
		}

		private class BetweenCommitTimeRevFilter : CommitTimeRevFilter
		{
			private readonly int _until;

            internal BetweenCommitTimeRevFilter(long since, long until)
                : base(since)
			{
				_until = (int)(until / 1000);
			}

			public override bool include(RevWalk walker, RevCommit cmit)
			{
				return cmit.CommitTime <= _until && cmit.CommitTime >= _when;
			}

			public override string ToString()
			{
                return base.ToString() + "(" + ((long)_when * 1000).MillisToUtcDateTime() + " - " + ((long)_until * 1000).MillisToUtcDateTime() + ")";
			}
		}

		#endregion
	}
}