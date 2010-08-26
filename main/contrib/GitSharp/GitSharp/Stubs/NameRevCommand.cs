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
    public class NameRevCommand
        : AbstractCommand
    {

        public NameRevCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Do not use branch names, but only tags to name the commits
        /// 
        /// </summary>
        public bool Tags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only use refs whose names match a given shell pattern.
        /// 
        /// </summary>
        public string Refs { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// List all commits reachable from all refs
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Read from stdin, append "(&lt;rev_name&gt;)" to all sha1's of nameable
        /// commits, and pass to stdout
        /// 
        /// </summary>
        public string Stdin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of printing both the SHA-1 and the name, print only
        /// the name.  If given with --tags the usual tag prefix of
        /// "tags/" is also omitted from the name, matching the output
        /// of `git-describe` more closely.
        /// 
        /// </summary>
        public bool NameOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Die with error code != 0 when a reference is undefined,
        /// instead of printing `undefined`.
        /// 
        /// </summary>
        public bool NoUndefined { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show uniquely abbreviated commit object as fallback.
        /// </summary>
        public bool Always { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
