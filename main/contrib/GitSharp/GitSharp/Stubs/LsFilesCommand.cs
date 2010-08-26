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
    public class LsFilesCommand
        : AbstractCommand
    {

        public LsFilesCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Show cached files in the output (default)
        /// 
        /// </summary>
        public bool Cached { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show deleted files in the output
        /// 
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show modified files in the output
        /// 
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show other (i.e. untracked) files in the output
        /// 
        /// </summary>
        public bool Others { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show only ignored files in the output. When showing files in the
        /// index, print only those matched by an exclude pattern. When
        /// showing "other" files, show only those matched by an exclude
        /// pattern.
        /// 
        /// </summary>
        public bool Ignored { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show staged contents' object name, mode bits and stage number in the output.
        /// 
        /// </summary>
        public bool Stage { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If a whole directory is classified as "other", show just its
        /// name (with a trailing slash) and not its whole contents.
        /// 
        /// </summary>
        public bool Directory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not list empty directories. Has no effect without --directory.
        /// 
        /// </summary>
        public bool NoEmptyDirectory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show unmerged files in the output (forces --stage)
        /// 
        /// </summary>
        public bool Unmerged { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show files on the filesystem that need to be removed due
        /// to file/directory conflicts for checkout-index to
        /// succeed.
        /// 
        /// </summary>
        public bool Killed { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// \0 line termination on output.
        /// 
        /// </summary>
        public bool Z { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Skips files matching pattern.
        /// Note that pattern is a shell wildcard pattern.
        /// 
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// exclude patterns are read from &lt;file&gt;; 1 per line.
        /// 
        /// </summary>
        public string ExcludeFrom { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// read additional exclude patterns that apply only to the
        /// directory and its subdirectories in &lt;file&gt;.
        /// 
        /// </summary>
        public string ExcludePerDirectory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Add the standard git exclusions: .git/info/exclude, .gitignore
        /// in each directory, and the user's global exclusion file.
        /// 
        /// </summary>
        public bool ExcludeStandard { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If any &lt;file&gt; does not appear in the index, treat this as an
        /// error (return 1).
        /// 
        /// </summary>
        public string ErrorUnmatch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When using --error-unmatch to expand the user supplied
        /// &lt;file&gt; (i.e. path pattern) arguments to paths, pretend
        /// that paths which were removed in the index since the
        /// named &lt;tree-ish&gt; are still present.  Using this option
        /// with `-s` or `-u` options does not make any sense.
        /// 
        /// </summary>
        public string WithTree { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Identify the file status with the following tags (followed by
        /// a space) at the start of each line:
        /// H::cached
        /// M::unmerged
        /// R::removed/deleted
        /// C::modified/changed
        /// K::to be killed
        /// ?::other
        /// 
        /// </summary>
        public bool T { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Similar to `-t`, but use lowercase letters for files
        /// that are marked as 'assume unchanged' (see
        /// linkgit:git-update-index[1]).
        /// 
        /// </summary>
        public bool V { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When run from a subdirectory, the command usually
        /// outputs paths relative to the current directory.  This
        /// option forces paths to be output relative to the project
        /// top directory.
        /// 
        /// </summary>
        public bool FullName { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the full 40-byte hexadecimal object
        /// lines, show only a partial prefix.
        /// Non default number of digits can be specified with --abbrev=&lt;n&gt;.
        /// 
        /// </summary>
        public string Abbrev { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
