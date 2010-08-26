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
    public class LsTreeCommand
        : AbstractCommand
    {

        public LsTreeCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Show only the named tree entry itself, not its children.
        /// 
        /// </summary>
        public bool D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Recurse into sub-trees.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show tree entries even when going to recurse them. Has no effect
        /// if '-r' was not passed. '-d' implies '-t'.
        /// 
        /// </summary>
        public bool T { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show object size of blob (file) entries.
        /// 
        /// </summary>
        public bool Long { get; set; }

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
        /// List only filenames (instead of the "long" output), one per line.
        /// 
        /// </summary>
        public bool NameOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// List only filenames (instead of the "long" output), one per line.
        /// 
        /// </summary>
        public bool NameStatus { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the full 40-byte hexadecimal object
        /// lines, show only a partial prefix.
        /// Non default number of digits can be specified with --abbrev=&lt;n&gt;.
        /// 
        /// </summary>
        public string Abbrev { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing the path names relative to the current working
        /// directory, show the full path names.
        /// 
        /// </summary>
        public bool FullName { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not limit the listing to the current working directory.
        /// Implies --full-name.
        /// 
        /// </summary>
        public bool FullTree { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
