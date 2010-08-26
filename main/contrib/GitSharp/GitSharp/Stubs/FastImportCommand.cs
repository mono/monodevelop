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
    public class FastimportCommand
        : AbstractCommand
    {

        public FastimportCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Specify the type of dates the frontend will supply to
        /// fast-import within `author`, `committer` and `tagger` commands.
        /// See ``Date Formats'' below for details about which formats
        /// are supported, and their syntax.
        /// 
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Force updating modified existing branches, even if doing
        /// so would cause commits to be lost (as the new commit does
        /// not contain the old commit).
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Maximum size of each output packfile, expressed in MiB.
        /// The default is 4096 (4 GiB) as that is the maximum allowed
        /// packfile size (due to file format limitations). Some
        /// importers may wish to lower this, such as to ensure the
        /// resulting packfiles fit on CDs.
        /// 
        /// </summary>
        public string MaxPackSize { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Maximum delta depth, for blob and tree deltification.
        /// Default is 10.
        /// 
        /// </summary>
        public string Depth { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Maximum number of branches to maintain active at once.
        /// See ``Memory Utilization'' below for details.  Default is 5.
        /// 
        /// </summary>
        public string ActiveBranches { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Dumps the internal marks table to &lt;file&gt; when complete.
        /// Marks are written one per line as `:markid SHA-1`.
        /// Frontends can use this file to validate imports after they
        /// have been completed, or to save the marks table across
        /// incremental runs.  As &lt;file&gt; is only opened and truncated
        /// at checkpoint (or completion) the same path can also be
        /// safely given to \--import-marks.
        /// 
        /// </summary>
        public string ExportMarks { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Before processing any input, load the marks specified in
        /// &lt;file&gt;.  The input file must exist, must be readable, and
        /// must use the same format as produced by \--export-marks.
        /// Multiple options may be supplied to import more than one
        /// set of marks.  If a mark is defined to different values,
        /// the last file wins.
        /// 
        /// </summary>
        public string ImportMarks { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// After creating a packfile, print a line of data to
        /// &lt;file&gt; listing the filename of the packfile and the last
        /// commit on each branch that was written to that packfile.
        /// This information may be useful after importing projects
        /// whose total object set exceeds the 4 GiB packfile limit,
        /// as these commits can be used as edge points during calls
        /// to 'git-pack-objects'.
        /// 
        /// </summary>
        public string ExportPackEdges { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Disable all non-fatal output, making fast-import silent when it
        /// is successful.  This option disables the output shown by
        /// \--stats.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Display some basic statistics about the objects fast-import has
        /// created, the packfiles they were stored into, and the
        /// memory used by fast-import during this run.  Showing this output
        /// is currently the default, but can be disabled with \--quiet.
        /// 
        /// </summary>
        public bool Stats { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
