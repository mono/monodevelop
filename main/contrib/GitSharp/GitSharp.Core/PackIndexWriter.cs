/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Security.Cryptography;
using GitSharp.Core.Transport;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    public abstract class PackIndexWriter
    {
        internal static byte[] TOC = { 255, (byte)'t', (byte)'O', (byte)'c' };

		/// <summary>
		/// Create a new writer for the oldest (most widely understood) format.
		/// <para />
		/// This method selects an index format that can accurate describe the
		/// supplied objects and that will be the most compatible format with older
		/// Git implementations.
		/// <para />
		/// Index version 1 is widely recognized by all Git implementations, but
		/// index version 2 (and later) is not as well recognized as it was
		/// introduced more than a year later. Index version 1 can only be used if
		/// the resulting pack file is under 4 gigabytes in size; packs larger than
		/// that limit must use index version 2.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dst">
		/// The stream the index data will be written to. If not already
		/// buffered it will be automatically wrapped in a buffered
		/// stream. Callers are always responsible for closing the stream.
		/// </param>
		/// <param name="objs">
		/// The objects the caller needs to store in the index. Entries
		/// will be examined until a format can be conclusively selected.
		/// </param>
		/// <returns>
		/// A new writer to output an index file of the requested format to
		/// the supplied stream.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// No recognized pack index version can support the supplied
		/// objects. This is likely a bug in the implementation.
		/// </exception>
        public static PackIndexWriter CreateOldestPossible<T>(Stream dst, List<T> objs) 
			where T : PackedObjectInfo
        {
            int version = 1;
        	bool breakLoop = false;

            foreach (T oe in objs)
            {
                switch (version)
                {
                    case 1:
                        if (PackIndexWriterV1.CanStore(oe)) continue;
                        version = 2;
                		break;

                    case 2:
                		breakLoop = true;
                		break;
				}

				if (breakLoop)
				{
					break;
				}
            }

            return CreateVersion(dst, version);
        }

		/// <summary>
		/// Create a new writer instance for a specific index format version.
		/// </summary>
		/// <param name="dst">
		/// The stream the index data will be written to. If not already
		/// buffered it will be automatically wrapped in a buffered
		/// stream. Callers are always responsible for closing the stream.
		/// </param>
		/// <param name="version">
		/// Index format version number required by the caller. Exactly
		/// this formatted version will be written.
		/// </param>
		/// <returns>
		/// A new writer to output an index file of the requested format to
		/// the supplied stream.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// The version requested is not supported by this
		/// implementation.
		/// </exception>
        public static PackIndexWriter CreateVersion(Stream dst, int version)
        {
            switch (version)
            {
                case 1:
                    return new PackIndexWriterV1(dst);
                case 2:
                    return new PackIndexWriterV2(dst);
                default:
                    throw new ArgumentException("Unsupported pack index version " + version);
            }
        }

		// The index data stream we are responsible for creating.
        internal readonly BinaryWriter _stream;

		// A temporary buffer for use during IO to out.
        internal byte[] tmp = new byte[4 + Constants.OBJECT_ID_LENGTH];

		// The entries this writer must pack.
        internal List<PackedObjectInfo> entries;

		// SHA-1 checksum for the entire pack data.
        internal byte[] packChecksum;

		/// <summary>
		/// Create a new writer instance.
		/// </summary>
		/// <param name="stream">
		/// The stream this instance outputs to. If not already buffered
		/// it will be automatically wrapped in a buffered stream.
		/// </param>
        internal PackIndexWriter(Stream stream)
        {
            _stream = new BinaryWriter(stream);
        }
        
		/// <summary>
		///  Write all object entries to the index stream.
		///  <para />
		///  After writing the stream passed to the factory is flushed but remains
		///  open. Callers are always responsible for closing the output stream.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="toStore">
		/// Sorted list of objects to store in the index. The caller must
		/// have previously sorted the list using <see cref="PackedObjectInfo"/>'s
		/// native {@link Comparable} implementation.
		/// </param>
		/// <param name="packDataChecksum">
		/// Checksum signature of the entire pack data content. This is
		/// traditionally the last 20 bytes of the pack file's own stream.
		/// </param>
		public void Write<T>(List<T> toStore, byte[] packDataChecksum) 
			where T : PackedObjectInfo
        {
            entries = new List<PackedObjectInfo>();
            foreach (T e in toStore) entries.Add(e);
            packChecksum = packDataChecksum;
            WriteInternal();
            _stream.Flush();
        }

		/// <summary>
		///  Writes the index file to out.
		///  <para />
		///  Implementations should go something like:
		/// <example>
		/// WriteFanOutTable();
		/// foreach (PackedObjectInfo po in entries)
		/// {
		///		WriteOneEntry(po);
		/// }
		/// WriteChecksumFooter();
		/// </example>
		/// <para />
		/// Where the logic for <code>writeOneEntry</code> is specific to the index
		/// format in use. Additional headers/footers may be used if necessary and
		/// the entries collection may be iterated over more than once if
		/// necessary. Implementors therefore have complete control over the data.
		/// </summary>
        internal abstract void WriteInternal();

		/// <summary>
		/// Output the version 2 (and later) TOC header, with version number.
		/// <para />
		/// Post version 1 all index files start with a TOC header that makes the
		/// file an invalid version 1 file, and then includes the version number.
		/// This header is necessary to recognize a version 1 from a version 2
		/// formatted index.
		/// </summary>
		/// <param name="version">Version number of this index format being written.</param>
        internal void WriteTOC(int version)
        {
            _stream.Write(TOC);
            NB.encodeInt32(tmp, 0, version);
            _stream.Write(tmp, 0 , 4);
        }

		/// <summary>
		/// utput the standard 256 entry first-level fan-out table.
		/// <para />
		/// The fan-out table is 4 KB in size, holding 256 32-bit unsigned integer
		/// counts. Each count represents the number of objects within this index
		/// whose <see cref="AnyObjectId.GetFirstByte()"/> matches the count's position in the
		/// fan-out table.
		/// </summary>
	    internal void WriteFanOutTable() 
        {
		    int[] fanout = new int[256];
		    foreach (PackedObjectInfo po in entries)
			    fanout[po.GetFirstByte() & 0xff]++;
		    
            for (int i = 1; i < 256; i++)
			    fanout[i] += fanout[i - 1];

            foreach (int n in fanout)
            {
                NB.encodeInt32(tmp, 0, n);
                _stream.Write(tmp, 0, 4);
            }

        }

		/// <summary>
		/// Output the standard two-checksum index footer.
		/// <para />
		/// The standard footer contains two checksums (20 byte SHA-1 values):
		/// <ol>
		/// <li>Pack data checksum - taken from the last 20 bytes of the pack file.</li>
		/// <li>Index data checksum - checksum of all index bytes written, including
		/// the pack data checksum above.</li>
		/// </ol>
		/// </summary>
	    internal void WriteChecksumFooter() {
		    _stream.Write(packChecksum);
            using (var sha = new SHA1CryptoServiceProvider())
			{
			var hash = sha.ComputeHash(_stream.BaseStream);
#warning this should be tested better
				_stream.Write(hash);
			}
	    }
    }
}
