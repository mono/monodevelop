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
    public class ReadTreeCommand
        : AbstractCommand
    {

        public ReadTreeCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Perform a merge, not just a read.  The command will
        /// refuse to run if your index file has unmerged entries,
        /// indicating that you have not finished previous merge you
        /// started.
        /// 
        /// </summary>
        public bool M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Same as -m, except that unmerged entries are discarded
        ///         instead of failing.
        /// 
        /// </summary>
        public bool Reset { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// After a successful merge, update the files in the work
        /// tree with the result of the merge.
        /// 
        /// </summary>
        public bool U { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually a merge requires the index file as well as the
        /// files in the working tree are up to date with the
        /// current head commit, in order not to lose local
        /// changes.  This flag disables the check with the working
        /// tree and is meant to be used when creating a merge of
        /// trees that are not directly related to the current
        /// working tree status into a temporary index file.
        /// 
        /// </summary>
        public bool I { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the progress of checking files out.
        /// 
        /// </summary>
        public bool V { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Restrict three-way merge by 'git-read-tree' to happen
        /// only if there is no file-level merging required, instead
        /// of resolving merge for trivial cases and leaving
        /// conflicting files unresolved in the index.
        /// 
        /// </summary>
        public bool Trivial { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually a three-way merge by 'git-read-tree' resolves
        /// the merge for really trivial cases and leaves other
        /// cases unresolved in the index, so that Porcelains can
        /// implement different merge policies.  This flag makes the
        /// command to resolve a few more cases internally:
        /// +
        /// * when one side removes a path and the other side leaves the path
        ///   unmodified.  The resolution is to remove that path.
        /// * when both sides remove a path.  The resolution is to remove that path.
        /// * when both sides adds a path identically.  The resolution
        ///   is to add that path.
        /// 
        /// </summary>
        public bool Aggressive { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Keep the current index contents, and read the contents
        /// of named tree-ish under directory at `&lt;prefix&gt;`.  The
        /// original index file cannot have anything at the path
        /// `&lt;prefix&gt;` itself, and have nothing in `&lt;prefix&gt;/`
        /// directory.  Note that the `&lt;prefix&gt;/` value must end
        /// with a slash.
        /// 
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When running the command with `-u` and `-m` options, the
        /// merge result may need to overwrite paths that are not
        /// tracked in the current branch.  The command usually
        /// refuses to proceed with the merge to avoid losing such a
        /// path.  However this safety valve sometimes gets in the
        /// way.  For example, it often happens that the other
        /// branch added a file that used to be a generated file in
        /// your branch, and the safety valve triggers when you try
        /// to switch to that branch after you ran `make` but before
        /// running `make clean` to remove the generated file.  This
        /// option tells the command to read per-directory exclude
        /// file (usually '.gitignore') and allows such an untracked
        /// but explicitly ignored file to be overwritten.
        /// 
        /// </summary>
        public string ExcludePerDirectory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of writing the results out to `$GIT_INDEX_FILE`,
        /// write the resulting index in the named file.  While the
        /// command is operating, the original index file is locked
        /// with the same mechanism as usual.  The file must allow
        /// to be rename(2)ed into from a temporary file that is
        /// created next to the usual index file; typically this
        /// means it needs to be on the same filesystem as the index
        /// file itself, and you need write permission to the
        /// directories the index file and index output file are
        /// located in.
        /// 
        /// </summary>
        public string IndexOutput { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
