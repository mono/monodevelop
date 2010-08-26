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
    public class BranchCommand
        : AbstractCommand
    {

        public BranchCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Delete a branch. The branch must be fully merged in HEAD.
        /// 
        /// </summary>
        public bool d { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Delete a branch irrespective of its merged status.
        /// 
        /// </summary>
        public bool D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Create the branch's reflog.  This activates recording of
        /// all changes made to the branch ref, enabling use of date
        /// based sha1 expressions such as "&lt;branchname&lt;@\{yesterday}".
        /// 
        /// </summary>
        public bool L { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Reset &lt;branchname&gt; to &lt;startpoint&gt; if &lt;branchname&gt; exists
        /// already. Without `-f` 'git-branch' refuses to change an existing branch.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Move/rename a branch and the corresponding reflog.
        /// 
        /// </summary>
        public bool m { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Move/rename a branch even if the new branch name already exists.
        /// 
        /// </summary>
        public bool M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Color branches to highlight current, local, and remote branches.
        /// 
        /// </summary>
        public bool Color { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Turn off branch colors, even when the configuration file gives the
        /// default to color output.
        /// 
        /// </summary>
        public bool NoColor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// List or delete (if used with -d) the remote-tracking branches.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// List both remote-tracking branches and local branches.
        /// 
        /// </summary>
        public bool A { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show sha1 and commit subject line for each head, along with
        /// relationship to upstream branch (if any). If given twice, print
        /// the name of the upstream branch, as well.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Alter the sha1's minimum display length in the output listing.
        /// The default value is 7.
        /// 
        /// </summary>
        public string Abbrev { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Display the full sha1s in the output listing rather than abbreviating them.
        /// 
        /// </summary>
        public bool NoAbbrev { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When creating a new branch, set up configuration to mark the
        /// start-point branch as "upstream" from the new branch. This
        /// configuration will tell git to show the relationship between the
        /// two branches in `git status` and `git branch -v`. Furthermore,
        /// it directs `git pull` without arguments to pull from the
        /// upstream when the new branch is checked out.
        /// +
        /// This behavior is the default when the start point is a remote branch.
        /// Set the branch.autosetupmerge configuration variable to `false` if you
        /// want `git checkout` and `git branch` to always behave as if '--no-track'
        /// were given. Set it to `always` if you want this behavior when the
        /// start-point is either a local or remote branch.
        /// 
        /// </summary>
        public bool Track { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not set up "upstream" configuration, even if the
        /// branch.autosetupmerge configuration variable is true.
        /// 
        /// </summary>
        public bool NoTrack { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only list branches which contain the specified commit.
        /// 
        /// </summary>
        public string Contains { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only list branches whose tips are reachable from the
        /// specified commit (HEAD if not specified).
        /// 
        /// </summary>
        public string Merged { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only list branches whose tips are not reachable from the
        /// specified commit (HEAD if not specified).
        /// 
        /// </summary>
        public string NoMerged { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
