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
    public class MergetoolCommand
        : AbstractCommand
    {

        public MergetoolCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Use the merge resolution program specified by &lt;tool&gt;.
        /// Valid merge tools are:
        /// kdiff3, tkdiff, meld, xxdiff, emerge, vimdiff, gvimdiff, ecmerge,
        /// diffuse, tortoisemerge, opendiff, p4merge and araxis.
        /// +
        /// If a merge resolution program is not specified, 'git-mergetool'
        /// will use the configuration variable `merge.tool`.  If the
        /// configuration variable `merge.tool` is not set, 'git-mergetool'
        /// will pick a suitable default.
        /// +
        /// You can explicitly provide a full path to the tool by setting the
        /// configuration variable `mergetool.&lt;tool&gt;.path`. For example, you
        /// can configure the absolute path to kdiff3 by setting
        /// `mergetool.kdiff3.path`. Otherwise, 'git-mergetool' assumes the
        /// tool is available in PATH.
        /// +
        /// Instead of running one of the known merge tool programs,
        /// 'git-mergetool' can be customized to run an alternative program
        /// by specifying the command line to invoke in a configuration
        /// variable `mergetool.&lt;tool&gt;.cmd`.
        /// +
        /// When 'git-mergetool' is invoked with this tool (either through the
        /// `-t` or `--tool` option or the `merge.tool` configuration
        /// variable) the configured command line will be invoked with `$BASE`
        /// set to the name of a temporary file containing the common base for
        /// the merge, if available; `$LOCAL` set to the name of a temporary
        /// file containing the contents of the file on the current branch;
        /// `$REMOTE` set to the name of a temporary file containing the
        /// contents of the file to be merged, and `$MERGED` set to the name
        /// of the file to which the merge tool should write the result of the
        /// merge resolution.
        /// +
        /// If the custom merge tool correctly indicates the success of a
        /// merge resolution with its exit code, then the configuration
        /// variable `mergetool.&lt;tool&gt;.trustExitCode` can be set to `true`.
        /// Otherwise, 'git-mergetool' will prompt the user to indicate the
        /// success of the resolution after the custom tool has exited.
        /// 
        /// </summary>
        public string Tool { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't prompt before each invocation of the merge resolution
        /// program.
        /// 
        /// </summary>
        public bool NoPrompt { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Prompt before each invocation of the merge resolution program.
        /// This is the default behaviour; the option is provided to
        /// override any configuration settings.
        /// </summary>
        public bool Prompt { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
