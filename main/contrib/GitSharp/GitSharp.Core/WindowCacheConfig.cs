/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

namespace GitSharp.Core
{
    /// <summary>
    /// Configuration parameters for <see cref="WindowCache" />.
    /// </summary>
    public class WindowCacheConfig
    {
        /// <summary>
        /// 1024 (number of bytes in one kibibyte/kilobyte)
        /// </summary>
        public const int Kb = 1024;

        /// <summary>
        /// 1024 <see cref="Kb" /> (number of bytes in one mebibyte/megabyte)
        /// </summary>
        public const int Mb = 1024 * 1024;

        /// <summary>
        /// Create a default configuration.
        /// </summary>
        public WindowCacheConfig()
        {
            PackedGitOpenFiles = 128;
            PackedGitLimit = 10 * Mb;
            PackedGitWindowSize = 8 * Kb;
            PackedGitMMAP = false;
            DeltaBaseCacheLimit = 10 * Mb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns> 
        /// The maximum number of streams to open at a time. Open packs count
        /// against the process limits. <b>Default is 128.</b>
        /// </returns>
        public int PackedGitOpenFiles { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns> maximum number bytes of heap memory to dedicate to caching pack
        /// file data. <b>Default is 10 MB.</b></returns>
        public long PackedGitLimit { get; set; }

        /// <summary>
        /// Gets/Sets the size in bytes of a single window read in from the pack file.
        /// </summary>
        public int PackedGitWindowSize { set; get; }

        /// <summary>
        /// Gets/sets the use of Java NIO virtual memory mapping for
        /// windows; false reads entire window into a byte[] with standard
        /// read calls.
        /// </summary>
        public bool PackedGitMMAP { set; get; }

        /// <summary>
        /// Gets/Sets the maximum number of bytes to cache in <see cref="UnpackedObjectCache" />
        /// for inflated, recently accessed objects, without delta chains.
        /// <para><b>Default 10 MB.</b></para>
        /// </summary>
        public int DeltaBaseCacheLimit { set; get; }

        /// <summary>
        /// Update properties by setting fields from the configuration.
		/// <para />
        /// If a property is not defined in the configuration, then it is left
        /// unmodified.
        /// </summary>
        /// <param name="rc">Configuration to read properties from.</param>
        public void FromConfig(RepositoryConfig rc)
        {
            PackedGitOpenFiles = rc.getInt("core", null, "packedgitopenfiles", PackedGitOpenFiles);
            PackedGitLimit = rc.getLong("core", null, "packedgitlimit", PackedGitLimit);
            PackedGitWindowSize = rc.getInt("core", null, "packedgitwindowsize", PackedGitWindowSize);
            PackedGitMMAP = rc.getBoolean("core", null, "packedgitmmap", PackedGitMMAP);
            DeltaBaseCacheLimit = rc.getInt("core", null, "deltabasecachelimit", DeltaBaseCacheLimit);
        }
    }
}