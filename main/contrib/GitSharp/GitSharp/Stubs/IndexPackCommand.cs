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
    public class IndexpackCommand
        : AbstractCommand
    {

        public IndexpackCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Be verbose about what is going on, including progress status.
        /// 
        /// </summary>
        public bool V { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Write the generated pack index into the specified
        /// file.  Without this option the name of pack index
        /// file is constructed from the name of packed archive
        /// file by replacing .pack with .idx (and the program
        /// fails if the name of packed archive does not end
        /// with .pack).
        /// 
        /// </summary>
        public string O { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When this flag is provided, the pack is read from stdin
        /// instead and a copy is then written to &lt;pack-file&gt;. If
        /// &lt;pack-file&gt; is not specified, the pack is written to
        /// objects/pack/ directory of the current git repository with
        /// a default name determined from the pack content.  If
        /// &lt;pack-file&gt; is not specified consider using --keep to
        /// prevent a race condition between this process and
        /// 'git-repack'.
        /// 
        /// </summary>
        public bool Stdin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// It is possible for 'git-pack-objects' to build
        /// "thin" pack, which records objects in deltified form based on
        /// objects not included in the pack to reduce network traffic.
        /// Those objects are expected to be present on the receiving end
        /// and they must be included in the pack for that pack to be self
        /// contained and indexable. Without this option any attempt to
        /// index a thin pack will fail. This option only makes sense in
        /// conjunction with --stdin.
        /// 
        /// </summary>
        public bool FixThin { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Before moving the index into its final destination
        /// create an empty .keep file for the associated pack file.
        /// This option is usually necessary with --stdin to prevent a
        /// simultaneous 'git-repack' process from deleting
        /// the newly constructed pack and index before refs can be
        /// updated to use objects contained in the pack.
        /// 
        /// </summary>
        public bool Keep { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Like --keep create a .keep file before moving the index into
        /// its final destination, but rather than creating an empty file
        /// place 'why' followed by an LF into the .keep file.  The 'why'
        /// message can later be searched for within all .keep files to
        /// locate any which have outlived their usefulness.
        /// 
        /// </summary>
        public string KeepMsg { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is intended to be used by the test suite only. It allows
        /// to force the version for the generated pack index, and to force
        /// 64-bit index entries on objects located above the given offset.
        /// 
        /// </summary>
        public string IndexVersion { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Die, if the pack contains broken objects or links.
        /// 
        /// </summary>
        public bool Strict { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
