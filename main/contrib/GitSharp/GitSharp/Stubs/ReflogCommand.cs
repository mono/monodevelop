/*
 * Copyright (C) 2010, Dominique van de Vorle <dvdvorle@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitSharp.Commands
{
    public class ReflogCommand
        : AbstractCommand
    {

        public ReflogCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// This revamps the logic -- the definition of "broken commit"
        /// becomes: a commit that is not reachable from any of the refs and
        /// there is a missing object among the commit, tree, or blob
        /// objects reachable from it that is not reachable from any of the
        /// refs.
        /// +
        /// This computation involves traversing all the reachable objects, i.e. it
        /// has the same cost as 'git-prune'.  Fortunately, once this is run, we
        /// should not have to ever worry about missing objects, because the current
        /// prune and pack-objects know about reflogs and protect objects referred by
        /// them.
        /// 
        /// </summary>
        public bool StaleFix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Entries older than this time are pruned.  Without the
        /// option it is taken from configuration `gc.reflogExpire`,
        /// which in turn defaults to 90 days.
        /// 
        /// </summary>
        public string Expire { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Entries older than this time and not reachable from
        /// the current tip of the branch are pruned.  Without the
        /// option it is taken from configuration
        /// `gc.reflogExpireUnreachable`, which in turn defaults to
        /// 30 days.
        /// 
        /// </summary>
        public string ExpireUnreachable { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of listing &lt;refs&gt; explicitly, prune all refs.
        /// 
        /// </summary>
        public string All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Update the ref with the sha1 of the top reflog entry (i.e.
        /// &lt;ref&gt;@\{0\}) after expiring or deleting.
        /// 
        /// </summary>
        public bool Updateref { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// While expiring or deleting, adjust each reflog entry to ensure
        /// that the `old` sha1 field points to the `new` sha1 field of the
        /// previous entry.
        /// 
        /// </summary>
        public bool Rewrite { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Print extra information on screen.
        /// </summary>
        public bool Verbose { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
