/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Description of an object stored in a pack file, including offset.
    /// <para>
    /// When objects are stored in packs Git needs the ObjectId and the offset
    /// (starting position of the object data) to perform random-access reads of
    /// objects from the pack. This extension of ObjectId includes the offset.
    /// </para>
    /// </summary>
    public class PackedObjectInfo : ObjectId
    {
        public PackedObjectInfo(long headerOffset, int packedCRC, AnyObjectId id)
            : base(id)
        {
            Offset = headerOffset;
            CRC = packedCRC;
        }

        /// <summary>
        /// Create a new structure to remember information about an object.
        /// </summary>
        /// <param name="id">
        /// The identity of the object the new instance tracks.
        /// </param>
        public PackedObjectInfo(AnyObjectId id)
            : base(id)
        {
        }

        /// <summary>
        /// offset in pack when object has been already written, or 0 if it
        /// has not been written yet
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// the 32 bit CRC checksum for the packed data.
        /// <para/>
        /// checksum of all packed data (including object type code,
        /// inflated length and delta base reference) as computed by
        /// <see cref="Crc32"/>
        /// </summary>
        public int CRC { get; set; }
    }
}