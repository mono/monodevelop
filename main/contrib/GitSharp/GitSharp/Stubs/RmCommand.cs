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
    public class RmCommand
        : AbstractCommand
    {

        public RmCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Override the up-to-date check.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Don't actually remove any file(s).  Instead, just show
        /// if they exist in the index and would otherwise be removed
        /// by the command.
        /// 
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Allow recursive removal when a leading directory name is
        ///         given.
        /// 
        /// </summary>
        public bool R { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use this option to unstage and remove paths only from the index.
        /// Working tree files, whether modified or not, will be
        /// left alone.
        /// 
        /// </summary>
        public bool Cached { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Exit with a zero status even if no files matched.
        /// 
        /// </summary>
        public bool IgnoreUnmatch { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// 'git-rm' normally outputs one line (in the form of an "rm" command)
        /// for each file removed. This option suppresses that output.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
