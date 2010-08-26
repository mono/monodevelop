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
    public class GcCommand
        : AbstractCommand
    {

        public GcCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Usually 'git-gc' runs very quickly while providing good disk
        /// space utilization and performance.  This option will cause
        /// 'git-gc' to more aggressively optimize the repository at the expense
        /// of taking much more time.  The effects of this optimization are
        /// persistent, so this option only needs to be used occasionally; every
        /// few hundred changesets or so.
        /// 
        /// </summary>
        public bool Aggressive { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// With this option, 'git-gc' checks whether any housekeeping is
        /// required; if not, it exits without performing any work.
        /// Some git commands run `git gc --auto` after performing
        /// operations that could create many loose objects.
        /// +
        /// Housekeeping is required if there are too many loose objects or
        /// too many packs in the repository. If the number of loose objects
        /// exceeds the value of the `gc.auto` configuration variable, then
        /// all loose objects are combined into a single pack using
        /// 'git-repack -d -l'.  Setting the value of `gc.auto` to 0
        /// disables automatic packing of loose objects.
        /// +
        /// If the number of packs exceeds the value of `gc.autopacklimit`,
        /// then existing packs (except those marked with a `.keep` file)
        /// are consolidated into a single pack by using the `-A` option of
        /// 'git-repack'. Setting `gc.autopacklimit` to 0 disables
        /// automatic consolidation of packs.
        /// 
        /// </summary>
        public bool Auto { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Prune loose objects older than date (default is 2 weeks ago,
        /// overridable by the config variable `gc.pruneExpire`).  This
        /// option is on by default.
        /// 
        /// </summary>
        public string Prune { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not prune any loose objects.
        /// 
        /// </summary>
        public bool NoPrune { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Suppress all progress reports.
        /// </summary>
        public bool Quiet { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
