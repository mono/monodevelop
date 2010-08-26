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
    public class GrepCommand
        : AbstractCommand
    {

        public GrepCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Instead of searching in the working tree files, check
        /// the blobs registered in the index file.
        /// 
        /// </summary>
        public bool Cached { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Process binary files as if they were text.
        /// 
        /// </summary>
        public bool Text { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Ignore case differences between the patterns and the
        /// files.
        /// 
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't match the pattern in binary files.
        /// 
        /// </summary>
        public bool I { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// For each pathspec given on command line, descend at most &lt;depth&gt;
        /// levels of directories. A negative value means no limit.
        /// 
        /// </summary>
        public string MaxDepth { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Match the pattern only at word boundary (either begin at the
        /// beginning of a line, or preceded by a non-word character; end at
        /// the end of a line or followed by a non-word character).
        /// 
        /// </summary>
        public bool WordRegexp { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Select non-matching lines.
        /// 
        /// </summary>
        public bool InvertMatch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// By default, the command shows the filename for each
        /// match.  `-h` option is used to suppress this output.
        /// `-H` is there for completeness and does not do anything
        /// except it overrides `-h` given earlier on the command
        /// line.
        /// 
        /// </summary>
        public bool H { get; set; }

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
        /// Use POSIX extended/basic regexp for patterns.  Default
        /// is to use basic regexp.
        /// 
        /// </summary>
        public bool ExtendedRegexp { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use POSIX extended/basic regexp for patterns.  Default
        /// is to use basic regexp.
        /// 
        /// </summary>
        public bool BasicRegexp { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use fixed strings for patterns (don't interpret pattern
        /// as a regex).
        /// 
        /// </summary>
        public bool FixedStrings { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Prefix the line number to matching lines.
        /// 
        /// </summary>
        public bool N { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing every matched line, show only the
        /// names of files that contain (or do not contain) matches.
        /// For better compatibility with 'git-diff', --name-only is a
        /// synonym for --files-with-matches.
        /// 
        /// </summary>
        public bool FilesWithMatches { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing every matched line, show only the
        /// names of files that contain (or do not contain) matches.
        /// For better compatibility with 'git-diff', --name-only is a
        /// synonym for --files-with-matches.
        /// 
        /// </summary>
        public bool NameOnly { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing every matched line, show only the
        /// names of files that contain (or do not contain) matches.
        /// For better compatibility with 'git-diff', --name-only is a
        /// synonym for --files-with-matches.
        /// 
        /// </summary>
        public bool FilesWithoutMatch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Output \0 instead of the character that normally follows a
        /// file name.
        /// 
        /// </summary>
        public bool Null { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of showing every matched line, show the number of
        /// lines that match.
        /// 
        /// </summary>
        public bool Count { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show colored matches.
        /// 
        /// </summary>
        public bool Color { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Turn off match highlighting, even when the configuration file
        /// gives the default to color output.
        /// 
        /// </summary>
        public bool NoColor { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show `context` trailing (`A` -- after), or leading (`B`
        /// -- before), or both (`C` -- context) lines, and place a
        /// line containing `--` between contiguous groups of
        /// matches.
        /// 
        /// </summary>
        public string A { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show `context` trailing (`A` -- after), or leading (`B`
        /// -- before), or both (`C` -- context) lines, and place a
        /// line containing `--` between contiguous groups of
        /// matches.
        /// 
        /// </summary>
        public string B { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show `context` trailing (`A` -- after), or leading (`B`
        /// -- before), or both (`C` -- context) lines, and place a
        /// line containing `--` between contiguous groups of
        /// matches.
        /// 
        /// </summary>
        public string C { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Show the preceding line that contains the function name of
        /// the match, unless the matching line is a function name itself.
        /// The name is determined in the same way as 'git diff' works out
        /// patch hunk headers (see 'Defining a custom hunk-header' in
        /// linkgit:gitattributes[5]).
        /// 
        /// </summary>
        public bool ShowFunction { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Read patterns from &lt;file&gt;, one per line.
        /// 
        /// </summary>
        public string F { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// The next parameter is the pattern. This option has to be
        /// used for patterns starting with - and should be used in
        /// scripts passing user input to grep.  Multiple patterns are
        /// combined by 'or'.
        /// 
        /// </summary>
        public bool E { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ( ... )::
        /// Specify how multiple patterns are combined using Boolean
        /// expressions.  `--or` is the default operator.  `--and` has
        /// higher precedence than `--or`.  `-e` has to be used for all
        /// patterns.
        /// 
        /// </summary>
        public bool And { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ( ... )::
        /// Specify how multiple patterns are combined using Boolean
        /// expressions.  `--or` is the default operator.  `--and` has
        /// higher precedence than `--or`.  `-e` has to be used for all
        /// patterns.
        /// 
        /// </summary>
        public bool Or { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// ( ... )::
        /// Specify how multiple patterns are combined using Boolean
        /// expressions.  `--or` is the default operator.  `--and` has
        /// higher precedence than `--or`.  `-e` has to be used for all
        /// patterns.
        /// 
        /// </summary>
        public bool Not { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When giving multiple pattern expressions combined with `--or`,
        /// this flag is specified to limit the match to files that
        /// have lines to match all of them.
        /// 
        /// </summary>
        public bool AllMatch { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
