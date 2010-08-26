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
    public class SendPackCommand
        : AbstractCommand
    {

        public SendPackCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Path to the 'git-receive-pack' program on the remote
        /// end.  Sometimes useful when pushing to a remote
        /// repository over ssh, and you do not have the program in
        /// a directory on the default $PATH.
        /// 
        /// </summary>
        public string ReceivePack { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Same as \--receive-pack=&lt;git-receive-pack&gt;.
        /// 
        /// </summary>
        public string Exec { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Instead of explicitly specifying which refs to update,
        /// update all heads that locally exist.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do everything except actually send the updates.
        /// 
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Usually, the command refuses to update a remote ref that
        /// is not an ancestor of the local ref used to overwrite it.
        /// This flag disables the check.  What this means is that
        /// the remote repository can lose commits; use it with
        /// care.
        /// 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Run verbosely.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Spend extra cycles to minimize the number of objects to be sent.
        /// Use it on slower connection.
        /// 
        /// </summary>
        public bool Thin { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
