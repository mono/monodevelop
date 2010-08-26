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
    public class SubmoduleCommand
        : AbstractCommand
    {

        public SubmoduleCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Only print error messages.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Branch of repository to add as submodule.
        /// 
        /// </summary>
        public bool Branch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for status and summary commands.  These
        /// commands typically use the commit found in the submodule HEAD, but
        /// with this option, the commit stored in the index is used instead.
        /// 
        /// </summary>
        public bool Cached { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for the summary command. This command
        /// compares the commit in the index with that in the submodule HEAD
        /// when this option is used.
        /// 
        /// </summary>
        public bool Files { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for the summary command.
        /// Limit the summary size (number of commits shown in total).
        /// Giving 0 will disable the summary; a negative number means unlimited
        /// (the default). This limit only applies to modified submodules. The
        /// size is always limited to 1 for added/deleted/typechanged submodules.
        /// 
        /// </summary>
        public bool SummaryLimit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for the update command.
        /// Don't fetch new objects from the remote site.
        /// 
        /// </summary>
        public bool NoFetch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for the update command.
        /// Merge the commit recorded in the superproject into the current branch
        /// of the submodule. If this option is given, the submodule's HEAD will
        /// not be detached. If a merge failure prevents this process, you will
        /// have to resolve the resulting conflicts within the submodule with the
        /// usual conflict resolution tools.
        /// If the key `submodule.$name.update` is set to `merge`, this option is
        /// implicit.
        /// 
        /// </summary>
        public bool Merge { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for the update command.
        /// Rebase the current branch onto the commit recorded in the
        /// superproject. If this option is given, the submodule's HEAD will not
        /// be detached. If a a merge failure prevents this process, you will have
        /// to resolve these failures with linkgit:git-rebase[1].
        /// If the key `submodule.$name.update` is set to `rebase`, this option is
        /// implicit.
        /// 
        /// </summary>
        public bool Rebase { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for add and update commands.  These
        /// commands sometimes need to clone a remote repository. In this case,
        /// this option will be passed to the linkgit:git-clone[1] command.
        /// +
        /// *NOTE*: Do *not* use this option unless you have read the note
        /// for linkgit:git-clone[1]'s --reference and --shared options carefully.
        /// 
        /// </summary>
        public string Reference { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option is only valid for foreach, update and status commands.
        /// Traverse submodules recursively. The operation is performed not
        /// only in the submodules of the current repo, but also
        /// in any nested submodules inside those submodules (and so on).
        /// 
        /// </summary>
        public bool Recursive { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
