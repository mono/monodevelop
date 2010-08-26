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
    public class DescribeCommand
        : AbstractCommand
    {

        public DescribeCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of using only the annotated tags, use any ref
        /// found in `.git/refs/`.  This option enables matching
        /// any known branch, remote branch, or lightweight tag.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of using only the annotated tags, use any tag
        /// found in `.git/refs/tags`.  This option enables matching
        /// a lightweight (non-annotated) tag.
        /// 
        /// </summary>
        public bool Tags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of finding the tag that predates the commit, find
        /// the tag that comes after the commit, and thus contains it.
        /// Automatically implies --tags.
        /// 
        /// </summary>
        public bool Contains { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of using the default 7 hexadecimal digits as the
        /// abbreviated object name, use &lt;n&gt; digits, or as many digits
        /// as needed to form a unique object name.  An &lt;n&gt; of 0
        /// will suppress long format, only showing the closest tag.
        /// 
        /// </summary>
        public string Abbrev { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of considering only the 10 most recent tags as
        /// candidates to describe the input committish consider
        /// up to &lt;n&gt; candidates.  Increasing &lt;n&gt; above 10 will take
        /// slightly longer but may produce a more accurate result.
        /// An &lt;n&gt; of 0 will cause only exact matches to be output.
        /// 
        /// </summary>
        public string Candidates { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only output exact matches (a tag directly references the
        /// supplied commit).  This is a synonym for --candidates=0.
        /// 
        /// </summary>
        public bool ExactMatch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Verbosely display information about the searching strategy
        /// being employed to standard error.  The tag name will still
        /// be printed to standard out.
        /// 
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Always output the long format (the tag, the number of commits
        /// and the abbreviated commit name) even when it matches a tag.
        /// This is useful when you want to see parts of the commit object name
        /// in "describe" output, even when the commit in question happens to be
        /// a tagged version.  Instead of just emitting the tag name, it will
        /// describe such a commit as v1.2-0-gdeadbee (0th commit since tag v1.2
        /// that points at object deadbee....).
        /// 
        /// </summary>
        public bool Long { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Only consider tags matching the given pattern (can be used to avoid
        /// leaking private tags made from the repository).
        /// 
        /// </summary>
        public string Match { get; set; }

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
