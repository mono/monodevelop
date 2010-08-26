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
    public class ResetCommand
        : AbstractCommand
    {

        public ResetCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Resets the index but not the working tree (i.e., the changed files
        /// are preserved but not marked for commit) and reports what has not
        /// been updated. This is the default action.
        /// 
        /// </summary>
        public bool Mixed { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Does not touch the index file nor the working tree at all, but
        /// requires them to be in a good order. This leaves all your changed
        /// files "Changes to be committed", as 'git-status' would
        /// put it.
        /// 
        /// </summary>
        public bool Soft { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Matches the working tree and index to that of the tree being
        /// switched to. Any changes to tracked files in the working tree
        /// since &lt;commit&gt; are lost.
        /// 
        /// </summary>
        public bool Hard { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Resets the index to match the tree recorded by the named commit,
        /// and updates the files that are different between the named commit
        /// and the current commit in the working tree.
        /// 
        /// </summary>
        public bool Merge { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Interactively select hunks in the difference between the index
        /// and &lt;commit&gt; (defaults to HEAD).  The chosen hunks are applied
        /// in reverse to the index.
        /// +
        /// This means that `git reset -p` is the opposite of `git add -p` (see
        /// linkgit:git-add[1]).
        /// 
        /// </summary>
        public bool Patch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be quiet, only report errors.
        /// 
        /// </summary>
        public bool Q { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
