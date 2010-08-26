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
    public class WhatchangedCommand
        : AbstractCommand
    {

        public WhatchangedCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Show textual diffs, instead of the git internal diff
        /// output format that is useful only to tell the changed
        /// paths and their nature of changes.
        /// 
        /// </summary>
        public bool P { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Limit output to &lt;n&gt; commits.
        /// 
        /// </summary>
        public string n { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show git internal diff output, but for the whole tree,
        /// not just the top level.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default, differences for merge commits are not shown.
        /// With this flag, show differences to that commit from all
        /// of its parents.
        /// +
        /// However, it is not very useful in general, although it
        /// *is* useful on a file-by-file basis.
        /// 
        /// </summary>
        public bool M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// 
        /// </summary>
        public string Pretty { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// 
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the full 40-byte hexadecimal commit object
        /// name, show only a partial prefix.  Non default number of
        /// digits can be specified with "--abbrev=&lt;n&gt;" (which also modifies
        /// diff output, if it is displayed).
        /// +
        /// This should make "--pretty=oneline" a whole lot more readable for
        /// people using 80-column terminals.
        /// 
        /// </summary>
        public bool AbbrevCommit { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is a shorthand for "--pretty=oneline --abbrev-commit"
        /// used together.
        /// 
        /// </summary>
        public bool Oneline { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The commit objects record the encoding used for the log message
        /// in their encoding header; this option can be used to tell the
        /// command to re-code the commit log message in the encoding
        /// preferred by the user.  For non plumbing commands this
        /// defaults to UTF-8.
        /// </summary>
        public string Encoding { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
