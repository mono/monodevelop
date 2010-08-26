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
    public class DifftoolCommand
        : AbstractCommand
    {

        public DifftoolCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Do not prompt before launching a diff tool.
        /// 
        /// </summary>
        public bool NoPrompt { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Prompt before each invocation of the diff tool.
        /// This is the default behaviour; the option is provided to
        /// override any configuration settings.
        /// 
        /// </summary>
        public bool Prompt { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use the diff tool specified by &lt;tool&gt;.
        /// Valid merge tools are:
        /// kdiff3, kompare, tkdiff, meld, xxdiff, emerge, vimdiff, gvimdiff,
        /// ecmerge, diffuse, opendiff, p4merge and araxis.
        /// +
        /// If a diff tool is not specified, 'git-difftool'
        /// will use the configuration variable `diff.tool`.  If the
        /// configuration variable `diff.tool` is not set, 'git-difftool'
        /// will pick a suitable default.
        /// +
        /// You can explicitly provide a full path to the tool by setting the
        /// configuration variable `difftool.&lt;tool&gt;.path`. For example, you
        /// can configure the absolute path to kdiff3 by setting
        /// `difftool.kdiff3.path`. Otherwise, 'git-difftool' assumes the
        /// tool is available in PATH.
        /// +
        /// Instead of running one of the known diff tools,
        /// 'git-difftool' can be customized to run an alternative program
        /// by specifying the command line to invoke in a configuration
        /// variable `difftool.&lt;tool&gt;.cmd`.
        /// +
        /// When 'git-difftool' is invoked with this tool (either through the
        /// `-t` or `--tool` option or the `diff.tool` configuration variable)
        /// the configured command line will be invoked with the following
        /// variables available: `$LOCAL` is set to the name of the temporary
        /// file containing the contents of the diff pre-image and `$REMOTE`
        /// is set to the name of the temporary file containing the contents
        /// of the diff post-image.  `$BASE` is provided for compatibility
        /// with custom merge tool commands and has the same value as `$LOCAL`.
        /// 
        /// </summary>
        public string Tool { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
