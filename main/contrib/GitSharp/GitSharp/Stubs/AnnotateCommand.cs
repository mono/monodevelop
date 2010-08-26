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
    public class AnnotateCommand
        : AbstractCommand
    {

        public AnnotateCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

      #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Show blank SHA-1 for boundary commits.  This can also
        /// be controlled via the `blame.blankboundary` config option.
        /// 
        /// </summary>
        public bool B { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not treat root commits as boundaries.  This can also be
        /// controlled via the `blame.showroot` config option.
        /// 
        /// </summary>
        public bool Root { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Include additional statistics at the end of blame output.
        /// 
        /// </summary>
        public bool ShowStats { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Annotate only the given line range.  &lt;start&gt; and &lt;end&gt; can take
        /// one of these forms:
        /// 
        /// </summary>
        public string L { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show long rev (Default: off).
        /// 
        /// </summary>
        public bool l { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show raw timestamp (Default: off).
        /// 
        /// </summary>
        public bool T { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use revisions from revs-file instead of calling linkgit:git-rev-list[1].
        /// 
        /// </summary>
        public string S { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Walk history forward instead of backward. Instead of showing
        /// the revision in which a line appeared, this shows the last
        /// revision in which a line has existed. This requires a range of
        /// revision like START..END where the path to blame exists in
        /// START.
        /// 
        /// </summary>
        public bool Reverse { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show in a format designed for machine consumption.
        /// 
        /// </summary>
        public bool Porcelain { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the result incrementally in a format designed for
        /// machine consumption.
        /// 
        /// </summary>
        public bool Incremental { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Specifies the encoding used to output author names
        /// and commit summaries. Setting it to `none` makes blame
        /// output unconverted data. For more information see the
        /// discussion about encoding in the linkgit:git-log[1]
        /// manual page.
        /// 
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When &lt;rev&gt; is not specified, the command annotates the
        /// changes starting backwards from the working tree copy.
        /// This flag makes the command pretend as if the working
        /// tree copy has the contents of the named file (specify
        /// `-` to make the command read from the standard input).
        /// 
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The value is one of the following alternatives:
        /// {relative,local,default,iso,rfc,short}. If --date is not
        /// provided, the value of the blame.date config variable is
        /// used. If the blame.date config variable is also not set, the
        /// iso format is used. For more information, See the discussion
        /// of the --date option at linkgit:git-log[1].
        /// 
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Detect moving lines in the file as well.  When a commit
        /// moves a block of lines in a file (e.g. the original file
        /// has A and then B, and the commit changes it to B and
        /// then A), the traditional 'blame' algorithm typically blames
        /// the lines that were moved up (i.e. B) to the parent and
        /// assigns blame to the lines that were moved down (i.e. A)
        /// to the child commit.  With this option, both groups of lines
        /// are blamed on the parent.
        /// +
        /// alphanumeric characters that git must detect as moving
        /// within a file for it to associate those lines with the parent
        /// commit.
        /// 
        /// </summary>
        public string M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// In addition to `-M`, detect lines copied from other
        /// files that were modified in the same commit.  This is
        /// useful when you reorganize your program and move code
        /// around across files.  When this option is given twice,
        /// the command additionally looks for copies from all other
        /// files in the parent for the commit that creates the file.
        /// +
        /// alphanumeric characters that git must detect as moving
        /// between files for it to associate those lines with the parent
        /// commit.
        /// 
        /// </summary>
        public string C { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show help message.
        /// </summary>
        public bool Help { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
