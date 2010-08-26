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
    public class UpdateIndexCommand
        : AbstractCommand
    {

        public UpdateIndexCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// If a specified file isn't in the index already then it's
        /// added.
        /// Default behaviour is to ignore new files.
        /// 
        /// </summary>
        public bool Add { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If a specified file is in the index but is missing then it's
        /// removed.
        /// Default behavior is to ignore removed file.
        /// 
        /// </summary>
        public bool Remove { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Looks at the current index and checks to see if merges or
        /// updates are needed by checking stat() information.
        /// 
        /// </summary>
        public bool Refresh { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Quiet.  If --refresh finds that the index needs an update, the
        ///         default behavior is to error out.  This option makes
        /// 'git-update-index' continue anyway.
        /// 
        /// </summary>
        public bool Q { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not try to update submodules.  This option is only respected
        /// when passed before --refresh.
        /// 
        /// </summary>
        public bool IgnoreSubmodules { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         If --refresh finds unmerged changes in the index, the default
        /// behavior is to error out.  This option makes 'git-update-index'
        ///         continue anyway.
        /// 
        /// </summary>
        public bool Unmerged { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignores missing files during a --refresh
        /// 
        /// </summary>
        public bool IgnoreMissing { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Directly insert the specified info into the index.
        /// 
        /// </summary>
        public string Cacheinfo { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Read index information from stdin.
        /// 
        /// </summary>
        public bool IndexInfo { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Set the execute permissions on the updated files.
        /// 
        /// </summary>
        public string Chmod { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When these flags are specified, the object names recorded
        /// for the paths are not updated.  Instead, these options
        /// set and unset the "assume unchanged" bit for the
        /// paths.  When the "assume unchanged" bit is on, git stops
        /// checking the working tree files for possible
        /// modifications, so you need to manually unset the bit to
        /// tell git when you change the working tree file. This is
        /// sometimes helpful when working with a big project on a
        /// filesystem that has very slow lstat(2) system call
        /// (e.g. cifs).
        /// +
        /// This option can be also used as a coarse file-level mechanism
        /// to ignore uncommitted changes in tracked files (akin to what
        /// `.gitignore` does for untracked files).
        /// You should remember that an explicit 'git add' operation will
        /// still cause the file to be refreshed from the working tree.
        /// Git will fail (gracefully) in case it needs to modify this file
        /// in the index e.g. when merging in a commit;
        /// thus, in case the assumed-untracked file is changed upstream,
        /// you will need to handle the situation manually.
        /// 
        /// </summary>
        public bool AssumeUnchanged { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When these flags are specified, the object names recorded
        /// for the paths are not updated.  Instead, these options
        /// set and unset the "assume unchanged" bit for the
        /// paths.  When the "assume unchanged" bit is on, git stops
        /// checking the working tree files for possible
        /// modifications, so you need to manually unset the bit to
        /// tell git when you change the working tree file. This is
        /// sometimes helpful when working with a big project on a
        /// filesystem that has very slow lstat(2) system call
        /// (e.g. cifs).
        /// +
        /// This option can be also used as a coarse file-level mechanism
        /// to ignore uncommitted changes in tracked files (akin to what
        /// `.gitignore` does for untracked files).
        /// You should remember that an explicit 'git add' operation will
        /// still cause the file to be refreshed from the working tree.
        /// Git will fail (gracefully) in case it needs to modify this file
        /// in the index e.g. when merging in a commit;
        /// thus, in case the assumed-untracked file is changed upstream,
        /// you will need to handle the situation manually.
        /// 
        /// </summary>
        public bool NoAssumeUnchanged { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Like '--refresh', but checks stat information unconditionally,
        /// without regard to the "assume unchanged" setting.
        /// 
        /// </summary>
        public bool ReallyRefresh { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Runs 'git-update-index' itself on the paths whose index
        /// entries are different from those from the `HEAD` commit.
        /// 
        /// </summary>
        public bool Again { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Restores the 'unmerged' or 'needs updating' state of a
        /// file during a merge if it was cleared by accident.
        /// 
        /// </summary>
        public bool Unresolve { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not create objects in the object database for all
        /// &lt;file&gt; arguments that follow this flag; just insert
        /// their object IDs into the index.
        /// 
        /// </summary>
        public bool InfoOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Remove the file from the index even when the working directory
        /// still has such a file. (Implies --remove.)
        /// 
        /// </summary>
        public bool ForceRemove { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default, when a file `path` exists in the index,
        /// 'git-update-index' refuses an attempt to add `path/file`.
        /// Similarly if a file `path/file` exists, a file `path`
        /// cannot be added.  With --replace flag, existing entries
        /// that conflict with the entry being added are
        /// automatically removed with warning messages.
        /// 
        /// </summary>
        public bool Replace { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of taking list of paths from the command line,
        /// read list of paths from the standard input.  Paths are
        /// separated by LF (i.e. one path per line) by default.
        /// 
        /// </summary>
        public bool Stdin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Report what is being added and removed from index.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only meaningful with `--stdin`; paths are separated with
        /// NUL character instead of LF.
        /// 
        /// </summary>
        public bool Z { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
