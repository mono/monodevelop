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
    public class SvnCommand
        : AbstractCommand
    {

        public SvnCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'init' command.
        /// These are passed directly to 'git init'.
        /// 
        /// </summary>
        public string Shared { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'init' command.
        /// These are passed directly to 'git init'.
        /// 
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///    Used with the 'fetch' command.
        /// +
        /// This allows revision ranges for partial/cauterized history
        /// to be supported.  $NUMBER, $NUMBER1:$NUMBER2 (numeric ranges),
        /// $NUMBER:HEAD, and BASE:$NUMBER are all supported.
        /// +
        /// This can allow you to make partial mirrors when running fetch;
        /// but is generally not recommended because history will be skipped
        /// and lost.
        /// 
        /// </summary>
        public string Revision { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'set-tree' command.
        /// +
        /// Read a list of commits from stdin and commit them in reverse
        /// order.  Only the leading sha1 is read from each line, so
        /// 'git rev-list --pretty=oneline' output can be used.
        /// 
        /// </summary>
        public bool Stdin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'dcommit', 'set-tree' and 'commit-diff' commands.
        /// +
        /// Remove directories from the SVN tree if there are no files left
        /// behind.  SVN can version empty directories, and they are not
        /// removed by default if there are no files left in them.  git
        /// cannot version empty directories.  Enabling this flag will make
        /// the commit to SVN act like git.
        /// +
        /// [verse]
        /// config key: svn.rmdir
        /// 
        /// </summary>
        public bool Rmdir { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'dcommit', 'set-tree' and 'commit-diff' commands.
        /// +
        /// Edit the commit message before committing to SVN.  This is off by
        /// default for objects that are commits, and forced on when committing
        /// tree objects.
        /// +
        /// [verse]
        /// config key: svn.edit
        /// 
        /// </summary>
        public bool Edit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only used with the 'dcommit', 'set-tree' and 'commit-diff' commands.
        /// +
        /// They are both passed directly to 'git diff-tree'; see
        /// linkgit:git-diff-tree[1] for more information.
        /// +
        /// [verse]
        /// config key: svn.l
        /// config key: svn.findcopiesharder
        /// 
        /// </summary>
        public string FindCopiesHarder { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// </summary>
        public string AuthorsFile { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
