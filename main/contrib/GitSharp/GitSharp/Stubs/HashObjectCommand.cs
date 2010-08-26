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
    public class HashObjectCommand
        : AbstractCommand
    {

        public HashObjectCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Specify the type (default: "blob").
        /// 
        /// </summary>
        public string T { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Actually write the object into the object database.
        /// 
        /// </summary>
        public bool W { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Read the object from standard input instead of from a file.
        /// 
        /// </summary>
        public bool Stdin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Read file names from stdin instead of from the command-line.
        /// 
        /// </summary>
        public bool StdinPaths { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Hash object as it were located at the given path. The location of
        /// file does not directly influence on the hash value, but path is
        /// used to determine what git filters should be applied to the object
        /// before it can be placed to the object database, and, as result of
        /// applying filters, the actual blob put into the object database may
        /// differ from the given file. This option is mainly useful for hashing
        /// temporary files located outside of the working directory or files
        /// read from stdin.
        /// 
        /// </summary>
        public bool Path { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Hash the contents as is, ignoring any input filter that would
        /// have been chosen by the attributes mechanism, including crlf
        /// conversion. If the file is read from standard input then this
        /// is always implied, unless the --path option is given.
        /// </summary>
        public bool NoFilters { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
