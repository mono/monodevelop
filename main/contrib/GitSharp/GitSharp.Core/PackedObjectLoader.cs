/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System;
using System.IO;

namespace GitSharp.Core
{
	/// <summary>
	/// Base class for a set of object loader classes for packed objects.
	/// </summary>
	public abstract class PackedObjectLoader : ObjectLoader, IDisposable
	{
		private readonly PackFile _packFile;
		private readonly long _dataOffset;
		private readonly long _objectOffset;

		protected PackedObjectLoader(PackFile packFile, long dataOffset, long objectOffset)
		{
			_packFile = packFile;
			_dataOffset = dataOffset;
			_objectOffset = objectOffset;
		}

		/// <summary>
		/// Force this object to be loaded into memory and pinned in this loader.
		/// <para />
		/// Once materialized, subsequent get operations for the following methods
		/// will always succeed without raising an exception, as all information is
		/// pinned in memory by this loader instance.
		/// <ul>
		/// <li>{@link Type}</li>
		/// <li>{@link Size}</li>
		/// <li>{@link #getBytes()}, {@link #getCachedBytes}</li>
		/// <li>{@link #getRawSize()}</li>
		/// <li>{@link #getRawType()}</li>
		/// </ul>
		/// </summary>
		/// <param name="curs">temporary thread storage during data access.</param>
		public abstract void Materialize(WindowCursor curs);

		public override int Type { get; protected set; }

		public override long Size { get; protected set; }

		public override byte[] CachedBytes { get; protected set; }

		/// <summary>
		/// Gets the offset of object header within pack file
		/// </summary>
		/// <returns></returns>
		public long ObjectOffset
		{
			get { return _objectOffset; }
		}

		/// <summary>
		/// Gets the offset of object data within pack file
		/// </summary>
		/// <returns></returns>
		public long DataOffset
		{
			get { return _dataOffset; }
		}

		/// <summary>
		/// Gets if this loader is capable of fast raw-data copying basing on
		/// compressed data checksum; false if raw-data copying needs
		/// uncompressing and compressing data
		/// </summary>
		/// <returns></returns>
		public bool SupportsFastCopyRawData
		{
			get { return _packFile.SupportsFastCopyRawData; }
		}

		/// <summary>
		/// Gets the id of delta base object for this object representation. 
		/// It returns null if object is not stored as delta.
		/// </summary>
		public abstract ObjectId DeltaBase { get; }

		protected PackFile PackFile
		{
			get { return _packFile; }
		}

		/// <summary>
		/// Peg the pack file open to support data copying.
		/// <para />
		/// Applications trying to copy raw pack data should ensure the pack stays
		/// open and available throughout the entire copy. To do that use:
		/// <example>
		/// loader.beginCopyRawData();
		/// try 
		/// {
		///		loader.CopyRawData(out, tmpbuf, curs);
		///	}
		/// finally
		/// {
		///		loader.endCopyRawData();
		///	}
		///	</example>
		/// </summary>
		/// <exception cref="Exception">
		/// This loader contains stale information and cannot be used.
		/// The most likely cause is the underlying pack file has been
		/// deleted, and the object has moved to another pack file.
		/// </exception>
		public void beginCopyRawData()
		{
			_packFile.beginCopyRawData();
		}

		/// <summary>
		/// Release resources after <see cref="beginCopyRawData"/>.
		/// </summary>
		public void endCopyRawData()
		{
			_packFile.endCopyRawData();
		}

		/// <summary>
		/// Copy raw object representation from storage to provided output stream.
		/// <para />
		/// Copied data doesn't include object header. User must provide temporary
		/// buffer used during copying by underlying I/O layer.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="out">
		/// Output stream when data is copied. No buffering is guaranteed.
		/// </param>
		/// <param name="buf">
		/// Temporary buffer used during copying. Recommended size is at
		/// least few kB.
		/// </param>
		/// <param name="curs">temporary thread storage during data access.</param>
		/// <exception cref="Exception">
		/// When the object cannot be read.
		/// </exception>
		/// <seealso cref="beginCopyRawData"/>
		public void CopyRawData<T>(T @out, byte[] buf, WindowCursor curs)
			where T : Stream
		{
			_packFile.CopyRawData(this, @out, buf, curs);
		}
		
		public void Dispose ()
		{
			_packFile.Dispose();
		}
		
	}
}