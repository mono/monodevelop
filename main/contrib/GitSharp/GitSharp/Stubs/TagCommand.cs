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
    public class TagCommand
        : AbstractCommand
    {

        public TagCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Make an unsigned, annotated tag object
        /// 
        /// </summary>
        public bool A { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Make a GPG-signed tag, using the default e-mail address's key
        /// 
        /// </summary>
        public bool S { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Make a GPG-signed tag, using the given key
        /// 
        /// </summary>
        public string U { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Replace an existing tag with the given name (instead of failing)
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Delete existing tags with the given names.
        /// 
        /// </summary>
        public bool D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Verify the gpg signature of the given tag names.
        /// 
        /// </summary>
        public bool V { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// &lt;num&gt; specifies how many lines from the annotation, if any,
        /// are printed when using -l.
        /// The default is not to print any annotation lines.
        /// If no number is given to `-n`, only the first line is printed.
        /// If the tag is not annotated, the commit message is displayed instead.
        /// 
        /// </summary>
        public string N { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// List tags with names that match the given pattern (or all if no pattern is given).
        /// Typing "git tag" without arguments, also lists all tags.
        /// 
        /// </summary>
        public string L { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only list tags which contain the specified commit.
        /// 
        /// </summary>
        public string Contains { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use the given tag message (instead of prompting).
        /// If multiple `-m` options are given, their values are
        /// concatenated as separate paragraphs.
        /// Implies `-a` if none of `-a`, `-s`, or `-u &lt;key-id&gt;`
        /// is given.
        /// 
        /// </summary>
        public string M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Take the tag message from the given file.  Use '-' to
        /// read the message from the standard input.
        /// Implies `-a` if none of `-a`, `-s`, or `-u &lt;key-id&gt;`
        /// is given.
        /// 
        /// </summary>
        public string F { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
