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
    public class RevertCommand
        : AbstractCommand
    {

        public RevertCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// With this option, 'git-revert' will let you edit the commit
        /// message prior to committing the revert. This is the default if
        /// you run the command from a terminal.
        /// 
        /// </summary>
        public bool Edit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually you cannot revert a merge because you do not know which
        /// side of the merge should be considered the mainline.  This
        /// option specifies the parent number (starting from 1) of
        /// the mainline and allows revert to reverse the change
        /// relative to the specified parent.
        /// +
        /// Reverting a merge commit declares that you will never want the tree changes
        /// brought in by the merge.  As a result, later merges will only bring in tree
        /// changes introduced by commits that are not ancestors of the previously
        /// reverted merge.  This may or may not be what you want.
        /// +
        /// See the link:howto/revert-a-faulty-merge.txt[revert-a-faulty-merge How-To] for
        /// more details.
        /// 
        /// </summary>
        public bool Mainline { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// With this option, 'git-revert' will not start the commit
        /// message editor.
        /// 
        /// </summary>
        public bool NoEdit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually the command automatically creates a commit with
        /// a commit log message stating which commit was
        /// reverted.  This flag applies the change necessary
        /// to revert the named commit to your working tree
        /// and the index, but does not make the commit.  In addition,
        /// when this option is used, your index does not have to match
        /// the HEAD commit.  The revert is done against the
        /// beginning state of your index.
        /// +
        /// This is useful when reverting more than one commits'
        /// effect to your index in a row.
        /// 
        /// </summary>
        public bool NoCommit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Add Signed-off-by line at the end of the commit message.
        /// 
        /// </summary>
        public bool Signoff { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
