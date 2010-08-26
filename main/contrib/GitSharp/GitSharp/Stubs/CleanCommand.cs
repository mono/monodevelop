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
    public class CleanCommand
        : AbstractCommand
    {

        public CleanCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Remove untracked directories in addition to untracked files.
        /// If an untracked directory is managed by a different git
        /// repository, it is not removed by default.  Use -f option twice
        /// if you really want to remove such a directory.
        /// 
        /// </summary>
        public bool D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If the git configuration specifies clean.requireForce as true,
        /// 'git-clean' will refuse to run unless given -f or -n.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't actually remove anything, just show what would be done.
        /// 
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be quiet, only report errors, but not the files that are
        /// successfully removed.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't use the ignore rules.  This allows removing all untracked
        /// files, including build products.  This can be used (possibly in
        /// conjunction with 'git-reset') to create a pristine
        /// working directory to test a clean build.
        /// 
        /// </summary>
        public bool x { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Remove only files ignored by git.  This may be useful to rebuild
        /// everything from scratch, but keep manually created files.
        /// 
        /// </summary>
        public bool X { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
