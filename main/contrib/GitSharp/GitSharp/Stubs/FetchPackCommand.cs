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
    public class FetchPackCommand
        : AbstractCommand
    {

        public FetchPackCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Fetch all remote refs.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass '-q' flag to 'git-unpack-objects'; this makes the
        /// cloning process less verbose.
        /// 
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not invoke 'git-unpack-objects' on received data, but
        /// create a single packfile out of it instead, and store it
        /// in the object database. If provided twice then the pack is
        /// locked against repacking.
        /// 
        /// </summary>
        public bool Keep { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Spend extra cycles to minimize the number of objects to be sent.
        /// Use it on slower connection.
        /// 
        /// </summary>
        public bool Thin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// If the remote side supports it, annotated tags objects will
        /// be downloaded on the same connection as the other objects if
        /// the object the tag references is downloaded.  The caller must
        /// otherwise determine the tags this option made available.
        /// 
        /// </summary>
        public bool IncludeTag { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Use this to specify the path to 'git-upload-pack' on the
        /// remote side, if is not found on your $PATH.
        /// Installations of sshd ignores the user's environment
        /// setup scripts for login shells (e.g. .bash_profile) and
        /// your privately installed git may not be found on the system
        /// default $PATH.  Another workaround suggested is to set
        /// up your $PATH in ".bashrc", but this flag is for people
        /// who do not want to pay the overhead for non-interactive
        /// shells by having a lean .bashrc file (they set most of
        /// the things up in .bash_profile).
        /// 
        /// </summary>
        public string UploadPack { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Same as \--upload-pack=&lt;git-upload-pack&gt;.
        /// 
        /// </summary>
        public string Exec { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Limit fetching to ancestor-chains not longer than n.
        /// 
        /// </summary>
        public string Depth { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not show the progress.
        /// 
        /// </summary>
        public bool NoProgress { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Run verbosely.
        /// 
        /// </summary>
        public bool V { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
