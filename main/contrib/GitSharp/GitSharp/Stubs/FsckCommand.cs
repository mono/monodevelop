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
    public class FsckCommand
        : AbstractCommand
    {

        public FsckCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        
        /// <summary>
        /// Not implemented
        /// 
        /// Print out objects that exist but that aren't readable from any
        /// of the reference nodes.
        /// 
        /// </summary>
        public bool Unreachable { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Report root nodes.
        /// 
        /// </summary>
        public bool Root { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Report tags.
        /// 
        /// </summary>
        public bool Tags { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Consider any object recorded in the index also as a head node for
        /// an unreachability trace.
        /// 
        /// </summary>
        public bool Cache { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not consider commits that are referenced only by an
        /// entry in a reflog to be reachable.  This option is meant
        /// only to search for commits that used to be in a ref, but
        /// now aren't, but are still in that corresponding reflog.
        /// 
        /// </summary>
        public bool NoReflogs { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Check not just objects in GIT_OBJECT_DIRECTORY
        /// ($GIT_DIR/objects), but also the ones found in alternate
        /// object pools listed in GIT_ALTERNATE_OBJECT_DIRECTORIES
        /// or $GIT_DIR/objects/info/alternates,
        /// and in packed git archives found in $GIT_DIR/objects/pack
        /// and corresponding pack subdirectories in alternate
        /// object pools.  This is now default; you can turn it off
        /// with --no-full.
        /// 
        /// </summary>
        public bool Full { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Enable more strict checking, namely to catch a file mode
        /// recorded with g+w bit set, which was created by older
        /// versions of git.  Existing repositories, including the
        /// Linux kernel, git itself, and sparse repository have old
        /// objects that triggers this check, but it is recommended
        /// to check new projects with this flag.
        /// 
        /// </summary>
        public bool Strict { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Be chatty.
        /// 
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Write dangling objects into .git/lost-found/commit/ or
        /// .git/lost-found/other/, depending on type.  If the object is
        /// a blob, the contents are written into the file, rather than
        /// its object name.
        /// 
        /// </summary>
        public bool LostFound { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
