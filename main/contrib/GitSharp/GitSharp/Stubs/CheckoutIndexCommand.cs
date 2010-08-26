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
    public class CheckoutIndexCommand
        : AbstractCommand
    {

        public CheckoutIndexCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// update stat information for the checked out entries in
        /// the index file.
        /// 
        /// </summary>
        public bool Index { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// be quiet if files exist or are not in the index
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// forces overwrite of existing files
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// checks out all files in the index.  Cannot be used
        /// together with explicit filenames.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't checkout new files, only refresh files already checked
        /// out.
        /// 
        /// </summary>
        public bool NoCreate { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When creating files, prepend &lt;string&gt; (usually a directory
        /// including a trailing /)
        /// 
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of checking out unmerged entries, copy out the
        /// files from named stage. &lt;number&gt; must be between 1 and 3.
        /// Note: --stage=all automatically implies --temp.
        /// 
        /// </summary>
        public string Stage { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of copying the files to the working directory
        /// write the content to temporary files.  The temporary name
        /// associations will be written to stdout.
        /// 
        /// </summary>
        public bool Temp { get; set; }

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
