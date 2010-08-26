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
    public class RepackCommand
        : AbstractCommand
    {

        public RepackCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Instead of incrementally packing the unpacked objects,
        /// pack everything referenced into a single pack.
        /// Especially useful when packing a repository that is used
        /// for private development. Use
        /// with '-d'.  This will clean up the objects that `git prune`
        /// leaves behind, but `git fsck --full` shows as
        /// dangling.
        /// +
        /// Note that users fetching over dumb protocols will have to fetch the
        /// whole new pack in order to get any contained object, no matter how many
        /// other objects in that pack they already have locally.
        /// 
        /// </summary>
        public bool a { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Same as `-a`, unless '-d' is used.  Then any unreachable
        /// objects in a previous pack become loose, unpacked objects,
        /// instead of being left in the old pack.  Unreachable objects
        /// are never intentionally added to a pack, even when repacking.
        /// This option prevents unreachable objects from being immediately
        /// deleted by way of being left in the old pack and then
        /// removed.  Instead, the loose unreachable objects
        /// will be pruned according to normal expiry rules
        /// with the next 'git-gc' invocation. See linkgit:git-gc[1].
        /// 
        /// </summary>
        public bool A { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// After packing, if the newly created packs make some
        /// existing packs redundant, remove the redundant packs.
        /// Also run  'git-prune-packed' to remove redundant
        /// loose object files.
        /// 
        /// </summary>
        public bool D { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass the `--local` option to 'git-pack-objects'. See
        /// linkgit:git-pack-objects[1].
        /// 
        /// </summary>
        public bool L { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass the `--no-reuse-object` option to `git-pack-objects`, see
        /// linkgit:git-pack-objects[1].
        /// 
        /// </summary>
        public bool F { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Pass the `-q` option to 'git-pack-objects'. See
        /// linkgit:git-pack-objects[1].
        /// 
        /// </summary>
        public bool Q { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Do not update the server information with
        /// 'git-update-server-info'.  This option skips
        /// updating local catalog files needed to publish
        /// this repository (or a direct copy of it)
        /// over HTTP or FTP.  See linkgit:git-update-server-info[1].
        /// 
        /// </summary>
        public bool N { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These two options affect how the objects contained in the pack are
        /// stored using delta compression. The objects are first internally
        /// sorted by type, size and optionally names and compared against the
        /// other objects within `--window` to see if using delta compression saves
        /// space. `--depth` limits the maximum delta depth; making it too deep
        /// affects the performance on the unpacker side, because delta data needs
        /// to be applied that many times to get to the necessary object.
        /// The default value for --window is 10 and --depth is 50.
        /// 
        /// </summary>
        public string Window { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These two options affect how the objects contained in the pack are
        /// stored using delta compression. The objects are first internally
        /// sorted by type, size and optionally names and compared against the
        /// other objects within `--window` to see if using delta compression saves
        /// space. `--depth` limits the maximum delta depth; making it too deep
        /// affects the performance on the unpacker side, because delta data needs
        /// to be applied that many times to get to the necessary object.
        /// The default value for --window is 10 and --depth is 50.
        /// 
        /// </summary>
        public string Depth { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This option provides an additional limit on top of `--window`;
        /// the window size will dynamically scale down so as to not take
        /// up more than N bytes in memory.  This is useful in
        /// repositories with a mix of large and small objects to not run
        /// out of memory with a large window, but still be able to take
        /// advantage of the large window for the smaller objects.  The
        /// size can be suffixed with "k", "m", or "g".
        /// `--window-memory=0` makes memory usage unlimited, which is the
        /// default.
        /// 
        /// </summary>
        public string WindowMemory { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Maximum size of each output packfile, expressed in MiB.
        /// If specified,  multiple packfiles may be created.
        /// The default is unlimited.
        /// 
        /// </summary>
        public string MaxPackSize { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
