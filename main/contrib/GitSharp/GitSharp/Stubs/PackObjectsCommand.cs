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
    public class PackObjectsCommand
        : AbstractCommand
    {

        public PackObjectsCommand() {
        }

        // note: the naming of command parameters is not following .NET conventions in favour of git command line parameter naming conventions.

        #region Properties / Options
        public List<string> Arguments { get; set; }
        /// <summary>
        /// Not implemented
        /// 
        /// Write the pack contents (what would have been written to
        /// .pack file) out to the standard output.
        /// 
        /// </summary>
        public bool Stdout { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Read the revision arguments from the standard input, instead of
        /// individual object names.  The revision arguments are processed
        /// the same way as 'git-rev-list' with the `--objects` flag
        /// uses its `commit` arguments to build the list of objects it
        /// outputs.  The objects on the resulting list are packed.
        /// 
        /// </summary>
        public bool Revs { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This implies `--revs`.  When processing the list of
        /// revision arguments read from the standard input, limit
        /// the objects packed to those that are not already packed.
        /// 
        /// </summary>
        public bool Unpacked { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This implies `--revs`.  In addition to the list of
        /// revision arguments read from the standard input, pretend
        /// as if all refs under `$GIT_DIR/refs` are specified to be
        /// included.
        /// 
        /// </summary>
        public bool All { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Include unasked-for annotated tags if the object they
        /// reference was included in the resulting packfile.  This
        /// can be useful to send new tags to native git clients.
        /// 
        /// </summary>
        public bool IncludeTag { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These two options affect how the objects contained in
        /// the pack are stored using delta compression.  The
        /// objects are first internally sorted by type, size and
        /// optionally names and compared against the other objects
        /// within --window to see if using delta compression saves
        /// space.  --depth limits the maximum delta depth; making
        /// it too deep affects the performance on the unpacker
        /// side, because delta data needs to be applied that many
        /// times to get to the necessary object.
        /// The default value for --window is 10 and --depth is 50.
        /// 
        /// </summary>
        public string Window { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// These two options affect how the objects contained in
        /// the pack are stored using delta compression.  The
        /// objects are first internally sorted by type, size and
        /// optionally names and compared against the other objects
        /// within --window to see if using delta compression saves
        /// space.  --depth limits the maximum delta depth; making
        /// it too deep affects the performance on the unpacker
        /// side, because delta data needs to be applied that many
        /// times to get to the necessary object.
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
        /// The default is unlimited, unless the config variable
        /// `pack.packSizeLimit` is set.
        /// 
        /// </summary>
        public string MaxPackSize { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This flag causes an object already in a local pack that
        /// has a .keep file to be ignored, even if it appears in the
        /// standard input.
        /// 
        /// </summary>
        public bool HonorPackKeep { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This flag causes an object already in a pack ignored
        /// even if it appears in the standard input.
        /// 
        /// </summary>
        public bool Incremental { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This flag is similar to `--incremental`; instead of
        /// ignoring all packed objects, it only ignores objects
        /// that are packed and/or not in the local object store
        /// (i.e. borrowed from an alternate).
        /// 
        /// </summary>
        public bool Local { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        ///         Only create a packed archive if it would contain at
        ///         least one object.
        /// 
        /// </summary>
        public bool NonEmpty { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Progress status is reported on the standard error stream
        /// by default when it is attached to a terminal, unless -q
        /// is specified. This flag forces progress status even if
        /// the standard error stream is not directed to a terminal.
        /// 
        /// </summary>
        public bool Progress { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When --stdout is specified then progress report is
        /// displayed during the object count and compression phases
        /// but inhibited during the write-out phase. The reason is
        /// that in some cases the output stream is directly linked
        /// to another command which may wish to display progress
        /// status of its own as it processes incoming pack data.
        /// This flag is like --progress except that it forces progress
        /// report for the write-out phase as well even if --stdout is
        /// used.
        /// 
        /// </summary>
        public bool AllProgress { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This is used to imply --all-progress whenever progress display
        /// is activated.  Unlike --all-progress this flag doesn't actually
        /// force any progress display by itself.
        /// 
        /// </summary>
        public bool AllProgressImplied { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This flag makes the command not to report its progress
        /// on the standard error stream.
        /// 
        /// </summary>
        public bool Q { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// When creating a packed archive in a repository that
        /// has existing packs, the command reuses existing deltas.
        /// This sometimes results in a slightly suboptimal pack.
        /// This flag tells the command not to reuse existing deltas
        /// but compute them from scratch.
        /// 
        /// </summary>
        public bool NoReuseDelta { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// This flag tells the command not to reuse existing object data at all,
        /// including non deltified object, forcing recompression of everything.
        /// This implies --no-reuse-delta. Useful only in the obscure case where
        /// wholesale enforcement of a different compression level on the
        /// packed data is desired.
        /// 
        /// </summary>
        public bool NoReuseObject { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Specifies compression level for newly-compressed data in the
        /// generated pack.  If not specified,  pack compression level is
        /// determined first by pack.compression,  then by core.compression,
        /// and defaults to -1,  the zlib default,  if neither is set.
        /// Add --no-reuse-object if you want to force a uniform compression
        /// level on all data no matter the source.
        /// 
        /// </summary>
        public string Compression { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// A packed archive can express base object of a delta as
        /// either 20-byte object name or as an offset in the
        /// stream, but older version of git does not understand the
        /// latter.  By default, 'git-pack-objects' only uses the
        /// former format for better compatibility.  This option
        /// allows the command to use the latter format for
        /// compactness.  Depending on the average delta chain
        /// length, this option typically shrinks the resulting
        /// packfile by 3-5 per-cent.
        /// 
        /// </summary>
        public bool DeltaBaseOffset { get; set; }

        /// <summary>
        /// Not implemented
        /// 
        /// Specifies the number of threads to spawn when searching for best
        /// delta matches.  This requires that pack-objects be compiled with
        /// pthreads otherwise this option is ignored with a warning.
        /// This is meant to reduce packing time on multiprocessor machines.
        /// The required amount of memory for the delta search window is
        /// however multiplied by the number of threads.
        /// Specifying 0 will cause git to auto-detect the number of CPU's
        /// and set the number of threads accordingly.
        /// 
        /// </summary>
        public string Threads { get; set; }

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
        /// With this option, parents that are hidden by grafts are packed
        /// nevertheless.
        /// 
        /// </summary>
        public bool KeepTrueParents { get; set; }

        #endregion

        public override void Execute()
        {
            throw new NotImplementedException();
        }
    }
}
