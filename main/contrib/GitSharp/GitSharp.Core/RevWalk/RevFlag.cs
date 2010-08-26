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

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Application level mark bit for <see cref="RevObject"/>s.
	/// </summary>
    public class RevFlag : IDisposable
    {
		/// <summary>
		/// Uninteresting by <see cref="RevWalk.markUninteresting(RevCommit)"/>.
		/// <para />
		/// We flag commits as uninteresting if the caller does not want commits
		/// reachable from a commit to <see cref="RevWalk.markUninteresting(RevCommit)"/>.
		/// This flag is always carried into the commit's parents and is a key part
		/// of the "rev-list B --not A" feature; A is marked UNINTERESTING.
		/// <para />
		/// This is a static flag. Its RevWalk is not available.
		/// </summary>
        public static RevFlag UNINTERESTING = new StaticRevFlag("UNINTERESTING", RevWalk.UNINTERESTING);

		/// <summary>
		/// Get the revision walk instance this flag was created from.
		/// </summary>
        public virtual RevWalk Walker { get; private set; }

        public string Name { get; set; }
        public int Mask { get; set; }

        public RevFlag(RevWalk walker, string name, int mask)
        {
            Walker = walker;
            Name = name;
            Mask = mask;
        }

        public override string ToString()
        {
            return Name;
        }   
		
		public void Dispose ()
		{
			Walker.Dispose();
		}
		
    }

	public class StaticRevFlag : RevFlag
	{
		public StaticRevFlag(string name, int mask)
			: base(null, name, mask)
		{
		}

		public override RevWalk Walker
		{
			get
			{
				throw new InvalidOperationException(ToString()
						+ " is a static flag and has no RevWalk instance");
			}
		}
	}
}