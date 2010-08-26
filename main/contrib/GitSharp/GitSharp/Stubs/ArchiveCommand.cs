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
    public class ArchiveCommand
        : AbstractCommand
    {

        public ArchiveCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Format of the resulting archive: 'tar' or 'zip'. If this option
        /// is not given, and the output file is specified, the format is
        /// inferred from the filename if possible (e.g. writing to "foo.zip"
        /// makes the output to be in the zip format). Otherwise the output
        /// format is `tar`.
        /// 
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show all available formats.
        /// 
        /// </summary>
        public bool List { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Report progress to stderr.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Prepend &lt;prefix&gt;/ to each filename in the archive.
        /// 
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Write the archive to &lt;file&gt; instead of stdout.
        /// 
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Look for attributes in .gitattributes in working directory too.
        /// 
        /// </summary>
        public bool WorktreeAttributes { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of making a tar archive from the local repository,
        /// retrieve a tar archive from a remote repository.
        /// 
        /// </summary>
        public string Remote { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Used with --remote to specify the path to the
        /// 'git-upload-archive' on the remote side.
        /// 
        /// </summary>
        public string Exec { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
