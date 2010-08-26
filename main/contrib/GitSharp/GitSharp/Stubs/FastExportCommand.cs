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
    public class FastExportCommand
        : AbstractCommand
    {

        public FastExportCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Insert 'progress' statements every &lt;n&gt; objects, to be shown by
        /// 'git-fast-import' during import.
        /// 
        /// </summary>
        public string Progress { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Specify how to handle signed tags.  Since any transformation
        /// after the export can change the tag names (which can also happen
        /// when excluding revisions) the signatures will not match.
        /// +
        /// When asking to 'abort' (which is the default), this program will die
        /// when encountering a signed tag.  With 'strip', the tags will be made
        /// unsigned, with 'verbatim', they will be silently exported
        /// and with 'warn', they will be exported, but you will see a warning.
        /// 
        /// </summary>
        public string SignedTags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Specify how to handle tags whose tagged objectis filtered out.
        /// Since revisions and files to export can be limited by path,
        /// tagged objects may be filtered completely.
        /// +
        /// When asking to 'abort' (which is the default), this program will die
        /// when encountering such a tag.  With 'drop' it will omit such tags from
        /// the output.  With 'rewrite', if the tagged object is a commit, it will
        /// rewrite the tag to tag an ancestor commit (via parent rewriting; see
        /// linkgit:git-rev-list[1])
        /// 
        /// </summary>
        public string TagOfFilteredObject { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Perform move and/or copy detection, as described in the
        /// linkgit:git-diff[1] manual page, and use it to generate
        /// rename and copy commands in the output dump.
        /// +
        /// Note that earlier versions of this command did not complain and
        /// produced incorrect results if you gave these options.
        /// 
        /// </summary>
        public bool M { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Perform move and/or copy detection, as described in the
        /// linkgit:git-diff[1] manual page, and use it to generate
        /// rename and copy commands in the output dump.
        /// +
        /// Note that earlier versions of this command did not complain and
        /// produced incorrect results if you gave these options.
        /// 
        /// </summary>
        public bool C { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Dumps the internal marks table to &lt;file&gt; when complete.
        /// Marks are written one per line as `:markid SHA-1`. Only marks
        /// for revisions are dumped; marks for blobs are ignored.
        /// Backends can use this file to validate imports after they
        /// have been completed, or to save the marks table across
        /// incremental runs.  As &lt;file&gt; is only opened and truncated
        /// at completion, the same path can also be safely given to
        /// \--import-marks.
        /// 
        /// </summary>
        public string ExportMarks { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Before processing any input, load the marks specified in
        /// &lt;file&gt;.  The input file must exist, must be readable, and
        /// must use the same format as produced by \--export-marks.
        /// +
        /// Any commits that have already been marked will not be exported again.
        /// If the backend uses a similar \--import-marks file, this allows for
        /// incremental bidirectional exporting of the repository by keeping the
        /// marks the same across runs.
        /// 
        /// </summary>
        public string ImportMarks { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Some old repositories have tags without a tagger.  The
        /// fast-import protocol was pretty strict about that, and did not
        /// allow that.  So fake a tagger to be able to fast-import the
        /// output.
        /// 
        /// </summary>
        public bool FakeMissingTagger { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Skip output of blob objects and instead refer to blobs via
        /// their original SHA-1 hash.  This is useful when rewriting the
        /// directory structure or history of a repository without
        /// touching the contents of individual files.  Note that the
        /// resulting stream can only be used by a repository which
        /// already contains the necessary objects.
        /// 
        /// </summary>
        public bool NoData { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
