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
    public class CherryPickCommand
        : AbstractCommand
    {

        public CherryPickCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// With this option, 'git-cherry-pick' will let you edit the commit
        /// message prior to committing.
        /// 
        /// </summary>
        public bool Edit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When recording the commit, append to the original commit
        /// message a note that indicates which commit this change
        /// was cherry-picked from.  Append the note only for cherry
        /// picks without conflicts.  Do not use this option if
        /// you are cherry-picking from your private branch because
        /// the information is useless to the recipient.  If on the
        /// other hand you are cherry-picking between two publicly
        /// visible branches (e.g. backporting a fix to a
        /// maintenance branch for an older release from a
        /// development branch), adding this information can be
        /// useful.
        /// 
        /// </summary>
        public bool X { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// It used to be that the command defaulted to do `-x`
        /// described above, and `-r` was to disable it.  Now the
        /// default is not to do `-x` so this option is a no-op.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually you cannot cherry-pick a merge because you do not know which
        /// side of the merge should be considered the mainline.  This
        /// option specifies the parent number (starting from 1) of
        /// the mainline and allows cherry-pick to replay the change
        /// relative to the specified parent.
        /// 
        /// </summary>
        public bool Mainline { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually the command automatically creates a commit.
        /// This flag applies the change necessary to cherry-pick
        /// the named commit to your working tree and the index,
        /// but does not make the commit.  In addition, when this
        /// option is used, your index does not have to match the
        /// HEAD commit.  The cherry-pick is done against the
        /// beginning state of your index.
        /// +
        /// This is useful when cherry-picking more than one commits'
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
