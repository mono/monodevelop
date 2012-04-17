/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using NGit;
using NGit.Internal;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Creates a table of contents to support random access by
	/// <see cref="PackFile">PackFile</see>
	/// .
	/// <p>
	/// Pack index files (the <code>.idx</code> suffix in a pack file pair)
	/// provides random access to any object in the pack by associating an ObjectId
	/// to the byte offset within the pack where the object's data can be read.
	/// </summary>
	public abstract class PackIndexWriter
	{
		/// <summary>Magic constant indicating post-version 1 format.</summary>
		/// <remarks>Magic constant indicating post-version 1 format.</remarks>
		protected internal static readonly byte[] TOC = new byte[] { unchecked((byte)(-1)
			), (byte)('t'), (byte)('O'), (byte)('c') };

		/// <summary>Create a new writer for the oldest (most widely understood) format.</summary>
		/// <remarks>
		/// Create a new writer for the oldest (most widely understood) format.
		/// <p>
		/// This method selects an index format that can accurate describe the
		/// supplied objects and that will be the most compatible format with older
		/// Git implementations.
		/// <p>
		/// Index version 1 is widely recognized by all Git implementations, but
		/// index version 2 (and later) is not as well recognized as it was
		/// introduced more than a year later. Index version 1 can only be used if
		/// the resulting pack file is under 4 gigabytes in size; packs larger than
		/// that limit must use index version 2.
		/// </remarks>
		/// <param name="dst">
		/// the stream the index data will be written to. If not already
		/// buffered it will be automatically wrapped in a buffered
		/// stream. Callers are always responsible for closing the stream.
		/// </param>
		/// <param name="objs">
		/// the objects the caller needs to store in the index. Entries
		/// will be examined until a format can be conclusively selected.
		/// </param>
		/// <returns>
		/// a new writer to output an index file of the requested format to
		/// the supplied stream.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// no recognized pack index version can support the supplied
		/// objects. This is likely a bug in the implementation.
		/// </exception>
		public static NGit.Storage.File.PackIndexWriter CreateOldestPossible<_T0>(OutputStream
			 dst, IList<_T0> objs) where _T0:PackedObjectInfo
		{
			int version = 1;
			foreach (PackedObjectInfo oe in objs)
			{
				switch (version)
				{
					case 1:
					{
						if (PackIndexWriterV1.CanStore(oe))
						{
							continue;
						}
						version = 2;
						goto case 2;
					}

					case 2:
					{
						goto LOOP_break;
					}
				}
			}
LOOP_break: ;
			return CreateVersion(dst, version);
		}

		/// <summary>Create a new writer instance for a specific index format version.</summary>
		/// <remarks>Create a new writer instance for a specific index format version.</remarks>
		/// <param name="dst">
		/// the stream the index data will be written to. If not already
		/// buffered it will be automatically wrapped in a buffered
		/// stream. Callers are always responsible for closing the stream.
		/// </param>
		/// <param name="version">
		/// index format version number required by the caller. Exactly
		/// this formatted version will be written.
		/// </param>
		/// <returns>
		/// a new writer to output an index file of the requested format to
		/// the supplied stream.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// the version requested is not supported by this
		/// implementation.
		/// </exception>
		public static NGit.Storage.File.PackIndexWriter CreateVersion(OutputStream dst, int
			 version)
		{
			switch (version)
			{
				case 1:
				{
					return new PackIndexWriterV1(dst);
				}

				case 2:
				{
					return new PackIndexWriterV2(dst);
				}

				default:
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().unsupportedPackIndexVersion
						, version));
				}
			}
		}

		/// <summary>The index data stream we are responsible for creating.</summary>
		/// <remarks>The index data stream we are responsible for creating.</remarks>
		protected internal readonly DigestOutputStream @out;

		/// <summary>A temporary buffer for use during IO to {link #out}.</summary>
		/// <remarks>A temporary buffer for use during IO to {link #out}.</remarks>
		protected internal readonly byte[] tmp;

		/// <summary>The entries this writer must pack.</summary>
		/// <remarks>The entries this writer must pack.</remarks>
		protected internal IList<PackedObjectInfo> entries;

		/// <summary>SHA-1 checksum for the entire pack data.</summary>
		/// <remarks>SHA-1 checksum for the entire pack data.</remarks>
		protected internal byte[] packChecksum;

		/// <summary>Create a new writer instance.</summary>
		/// <remarks>Create a new writer instance.</remarks>
		/// <param name="dst">
		/// the stream this instance outputs to. If not already buffered
		/// it will be automatically wrapped in a buffered stream.
		/// </param>
		protected internal PackIndexWriter(OutputStream dst)
		{
			@out = new DigestOutputStream(dst is BufferedOutputStream ? dst : new SafeBufferedOutputStream
				(dst), Constants.NewMessageDigest());
			tmp = new byte[4 + Constants.OBJECT_ID_LENGTH];
		}

		/// <summary>Write all object entries to the index stream.</summary>
		/// <remarks>
		/// Write all object entries to the index stream.
		/// <p>
		/// After writing the stream passed to the factory is flushed but remains
		/// open. Callers are always responsible for closing the output stream.
		/// </remarks>
		/// <param name="toStore">
		/// sorted list of objects to store in the index. The caller must
		/// have previously sorted the list using
		/// <see cref="NGit.Transport.PackedObjectInfo">NGit.Transport.PackedObjectInfo</see>
		/// 's
		/// native
		/// <see cref="System.IComparable{T}">System.IComparable&lt;T&gt;</see>
		/// implementation.
		/// </param>
		/// <param name="packDataChecksum">
		/// checksum signature of the entire pack data content. This is
		/// traditionally the last 20 bytes of the pack file's own stream.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// an error occurred while writing to the output stream, or this
		/// index format cannot store the object data supplied.
		/// </exception>
		public virtual void Write<_T0>(IList<_T0> toStore, byte[] packDataChecksum) where 
			_T0:PackedObjectInfo
		{
			entries = toStore.UpcastTo<_T0, PackedObjectInfo>();
			packChecksum = packDataChecksum;
			WriteImpl();
			@out.Flush();
		}

		/// <summary>
		/// Writes the index file to
		/// <see cref="@out">@out</see>
		/// .
		/// <p>
		/// Implementations should go something like:
		/// <pre>
		/// writeFanOutTable();
		/// for (final PackedObjectInfo po : entries)
		/// writeOneEntry(po);
		/// writeChecksumFooter();
		/// </pre>
		/// <p>
		/// Where the logic for <code>writeOneEntry</code> is specific to the index
		/// format in use. Additional headers/footers may be used if necessary and
		/// the
		/// <see cref="entries">entries</see>
		/// collection may be iterated over more than once if
		/// necessary. Implementors therefore have complete control over the data.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// an error occurred while writing to the output stream, or this
		/// index format cannot store the object data supplied.
		/// </exception>
		protected internal abstract void WriteImpl();

		/// <summary>Output the version 2 (and later) TOC header, with version number.</summary>
		/// <remarks>
		/// Output the version 2 (and later) TOC header, with version number.
		/// <p>
		/// Post version 1 all index files start with a TOC header that makes the
		/// file an invalid version 1 file, and then includes the version number.
		/// This header is necessary to recognize a version 1 from a version 2
		/// formatted index.
		/// </remarks>
		/// <param name="version">version number of this index format being written.</param>
		/// <exception cref="System.IO.IOException">an error occurred while writing to the output stream.
		/// 	</exception>
		protected internal virtual void WriteTOC(int version)
		{
			@out.Write(TOC);
			NB.EncodeInt32(tmp, 0, version);
			@out.Write(tmp, 0, 4);
		}

		/// <summary>Output the standard 256 entry first-level fan-out table.</summary>
		/// <remarks>
		/// Output the standard 256 entry first-level fan-out table.
		/// <p>
		/// The fan-out table is 4 KB in size, holding 256 32-bit unsigned integer
		/// counts. Each count represents the number of objects within this index
		/// whose
		/// <see cref="NGit.AnyObjectId.FirstByte()">NGit.AnyObjectId.FirstByte()</see>
		/// matches the count's position in the
		/// fan-out table.
		/// </remarks>
		/// <exception cref="System.IO.IOException">an error occurred while writing to the output stream.
		/// 	</exception>
		protected internal virtual void WriteFanOutTable()
		{
			int[] fanout = new int[256];
			foreach (PackedObjectInfo po in entries)
			{
				fanout[po.FirstByte & unchecked((int)(0xff))]++;
			}
			for (int i = 1; i < 256; i++)
			{
				fanout[i] += fanout[i - 1];
			}
			foreach (int n in fanout)
			{
				NB.EncodeInt32(tmp, 0, n);
				@out.Write(tmp, 0, 4);
			}
		}

		/// <summary>Output the standard two-checksum index footer.</summary>
		/// <remarks>
		/// Output the standard two-checksum index footer.
		/// <p>
		/// The standard footer contains two checksums (20 byte SHA-1 values):
		/// <ol>
		/// <li>Pack data checksum - taken from the last 20 bytes of the pack file.</li>
		/// <li>Index data checksum - checksum of all index bytes written, including
		/// the pack data checksum above.</li>
		/// </ol>
		/// </remarks>
		/// <exception cref="System.IO.IOException">an error occurred while writing to the output stream.
		/// 	</exception>
		protected internal virtual void WriteChecksumFooter()
		{
			@out.Write(packChecksum);
			@out.On(false);
			@out.Write(@out.GetMessageDigest().Digest());
		}
	}
}
