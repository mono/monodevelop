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
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Storage.Pack;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>A Git version 2 pack file representation.</summary>
	/// <remarks>
	/// A Git version 2 pack file representation. A pack file contains Git objects in
	/// delta packed format yielding high compression of lots of object where some
	/// objects are similar.
	/// </remarks>
	public class PackFile : Iterable<PackIndex.MutableEntry>
	{
		private sealed class _IComparer_90 : IComparer<NGit.Storage.File.PackFile>
		{
			public _IComparer_90()
			{
			}

			public int Compare(NGit.Storage.File.PackFile a, NGit.Storage.File.PackFile b)
			{
				return b.packLastModified - a.packLastModified;
			}
		}

		/// <summary>Sorts PackFiles to be most recently created to least recently created.</summary>
		/// <remarks>Sorts PackFiles to be most recently created to least recently created.</remarks>
		public static readonly IComparer<NGit.Storage.File.PackFile> SORT = new _IComparer_90
			();

		private readonly FilePath idxFile;

		private readonly FilePath packFile;

		internal readonly int hash;

		private RandomAccessFile fd;

		/// <summary>
		/// Serializes reads performed against
		/// <see cref="fd">fd</see>
		/// .
		/// </summary>
		private readonly object readLock = new object();

		internal long length;

		private int activeWindows;

		private int activeCopyRawData;

		private int packLastModified;

		private volatile bool invalid;

		private byte[] packChecksum;

		private PackIndex loadedIdx;

		private PackReverseIndex reverseIdx;

		/// <summary>Objects we have tried to read, and discovered to be corrupt.</summary>
		/// <remarks>
		/// Objects we have tried to read, and discovered to be corrupt.
		/// <p>
		/// The list is allocated after the first corruption is found, and filled in
		/// as more entries are discovered. Typically this list is never used, as
		/// pack files do not usually contain corrupt objects.
		/// </remarks>
		private volatile LongList corruptObjects;

		/// <summary>Construct a reader for an existing, pre-indexed packfile.</summary>
		/// <remarks>Construct a reader for an existing, pre-indexed packfile.</remarks>
		/// <param name="idxFile">path of the <code>.idx</code> file listing the contents.</param>
		/// <param name="packFile">path of the <code>.pack</code> file holding the data.</param>
		public PackFile(FilePath idxFile, FilePath packFile)
		{
			this.idxFile = idxFile;
			this.packFile = packFile;
			this.packLastModified = (int)(packFile.LastModified() >> 10);
			// Multiply by 31 here so we can more directly combine with another
			// value in WindowCache.hash(), without doing the multiply there.
			//
			hash = Runtime.IdentityHashCode(this) * 31;
			length = long.MaxValue;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PackIndex Idx()
		{
			lock (this)
			{
				if (loadedIdx == null)
				{
					if (invalid)
					{
						throw new PackInvalidException(packFile);
					}
					try
					{
						PackIndex idx = PackIndex.Open(idxFile);
						if (packChecksum == null)
						{
							packChecksum = idx.packChecksum;
						}
						else
						{
							if (!Arrays.Equals(packChecksum, idx.packChecksum))
							{
								throw new PackMismatchException(JGitText.Get().packChecksumMismatch);
							}
						}
						loadedIdx = idx;
					}
					catch (IOException e)
					{
						invalid = true;
						throw;
					}
				}
				return loadedIdx;
			}
		}

		/// <returns>the File object which locates this pack on disk.</returns>
		public virtual FilePath GetPackFile()
		{
			return packFile;
		}

		/// <summary>Determine if an object is contained within the pack file.</summary>
		/// <remarks>
		/// Determine if an object is contained within the pack file.
		/// <p>
		/// For performance reasons only the index file is searched; the main pack
		/// content is ignored entirely.
		/// </p>
		/// </remarks>
		/// <param name="id">the object to look for. Must not be null.</param>
		/// <returns>true if the object is in this pack; false otherwise.</returns>
		/// <exception cref="System.IO.IOException">the index file cannot be loaded into memory.
		/// 	</exception>
		public virtual bool HasObject(AnyObjectId id)
		{
			long offset = Idx().FindOffset(id);
			return 0 < offset && !IsCorrupt(offset);
		}

		/// <summary>Get an object from this pack.</summary>
		/// <remarks>Get an object from this pack.</remarks>
		/// <param name="curs">temporary working space associated with the calling thread.</param>
		/// <param name="id">the object to obtain from the pack. Must not be null.</param>
		/// <returns>
		/// the object loader for the requested object if it is contained in
		/// this pack; null if the object was not found.
		/// </returns>
		/// <exception cref="System.IO.IOException">the pack file or the index could not be read.
		/// 	</exception>
		internal virtual ObjectLoader Get(WindowCursor curs, AnyObjectId id)
		{
			long offset = Idx().FindOffset(id);
			return 0 < offset && !IsCorrupt(offset) ? Load(curs, offset) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Resolve(ICollection<ObjectId> matches, AbbreviatedObjectId 
			id, int matchLimit)
		{
			Idx().Resolve(matches, id, matchLimit);
		}

		/// <summary>Close the resources utilized by this repository</summary>
		public virtual void Close()
		{
			DeltaBaseCache.Purge(this);
			WindowCache.Purge(this);
			lock (this)
			{
				loadedIdx = null;
				reverseIdx = null;
			}
		}

		/// <summary>
		/// Provide iterator over entries in associated pack index, that should also
		/// exist in this pack file.
		/// </summary>
		/// <remarks>
		/// Provide iterator over entries in associated pack index, that should also
		/// exist in this pack file. Objects returned by such iterator are mutable
		/// during iteration.
		/// <p>
		/// Iterator returns objects in SHA-1 lexicographical order.
		/// </p>
		/// </remarks>
		/// <returns>iterator over entries of associated pack index</returns>
		/// <seealso cref="PackIndex.Iterator()">PackIndex.Iterator()</seealso>
		public override Sharpen.Iterator<PackIndex.MutableEntry> Iterator()
		{
			try
			{
				return Idx().Iterator();
			}
			catch (IOException)
			{
				return Sharpen.Collections.EmptyList<PackIndex.MutableEntry>().Iterator();
			}
		}

		/// <summary>Obtain the total number of objects available in this pack.</summary>
		/// <remarks>
		/// Obtain the total number of objects available in this pack. This method
		/// relies on pack index, giving number of effectively available objects.
		/// </remarks>
		/// <returns>number of objects in index of this pack, likewise in this pack</returns>
		/// <exception cref="System.IO.IOException">the index file cannot be loaded into memory.
		/// 	</exception>
		internal virtual long GetObjectCount()
		{
			return Idx().GetObjectCount();
		}

		/// <summary>
		/// Search for object id with the specified start offset in associated pack
		/// (reverse) index.
		/// </summary>
		/// <remarks>
		/// Search for object id with the specified start offset in associated pack
		/// (reverse) index.
		/// </remarks>
		/// <param name="offset">start offset of object to find</param>
		/// <returns>object id for this offset, or null if no object was found</returns>
		/// <exception cref="System.IO.IOException">the index file cannot be loaded into memory.
		/// 	</exception>
		internal virtual ObjectId FindObjectForOffset(long offset)
		{
			return GetReverseIdx().FindObject(offset);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.DataFormatException"></exception>
		private void Decompress(long position, WindowCursor curs, byte[] dstbuf, int dstoff
			, int dstsz)
		{
			if (curs.Inflate(this, position, dstbuf, dstoff) != dstsz)
			{
				throw new EOFException(MessageFormat.Format(JGitText.Get().shortCompressedStreamAt
					, position));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.StoredObjectRepresentationNotAvailableException"></exception>
		internal void CopyAsIs(PackOutputStream @out, LocalObjectToPack src, WindowCursor
			 curs)
		{
			BeginCopyAsIs(src);
			try
			{
				CopyAsIs2(@out, src, curs);
			}
			finally
			{
				EndCopyAsIs();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.StoredObjectRepresentationNotAvailableException"></exception>
		private void CopyAsIs2(PackOutputStream @out, LocalObjectToPack src, WindowCursor
			 curs)
		{
			CRC32 crc1 = new CRC32();
			CRC32 crc2 = new CRC32();
			byte[] buf = @out.GetCopyBuffer();
			// Rip apart the header so we can discover the size.
			//
			ReadFully(src.offset, buf, 0, 20, curs);
			int c = buf[0] & unchecked((int)(0xff));
			int typeCode = (c >> 4) & 7;
			long inflatedLength = c & 15;
			int shift = 4;
			int headerCnt = 1;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = buf[headerCnt++] & unchecked((int)(0xff));
				inflatedLength += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			if (typeCode == Constants.OBJ_OFS_DELTA)
			{
				do
				{
					c = buf[headerCnt++] & unchecked((int)(0xff));
				}
				while ((c & 128) != 0);
				crc1.Update(buf, 0, headerCnt);
				crc2.Update(buf, 0, headerCnt);
			}
			else
			{
				if (typeCode == Constants.OBJ_REF_DELTA)
				{
					crc1.Update(buf, 0, headerCnt);
					crc2.Update(buf, 0, headerCnt);
					ReadFully(src.offset + headerCnt, buf, 0, 20, curs);
					crc1.Update(buf, 0, 20);
					crc2.Update(buf, 0, 20);
					headerCnt += 20;
				}
				else
				{
					crc1.Update(buf, 0, headerCnt);
					crc2.Update(buf, 0, headerCnt);
				}
			}
			long dataOffset = src.offset + headerCnt;
			long dataLength = src.length;
			long expectedCRC;
			ByteArrayWindow quickCopy;
			// Verify the object isn't corrupt before sending. If it is,
			// we report it missing instead.
			//
			try
			{
				quickCopy = curs.QuickCopy(this, dataOffset, dataLength);
				if (Idx().HasCRC32Support())
				{
					// Index has the CRC32 code cached, validate the object.
					//
					expectedCRC = Idx().FindCRC32(src);
					if (quickCopy != null)
					{
						quickCopy.Crc32(crc1, dataOffset, (int)dataLength);
					}
					else
					{
						long pos = dataOffset;
						long cnt = dataLength;
						while (cnt > 0)
						{
							int n = (int)Math.Min(cnt, buf.Length);
							ReadFully(pos, buf, 0, n, curs);
							crc1.Update(buf, 0, n);
							pos += n;
							cnt -= n;
						}
					}
					if (crc1.GetValue() != expectedCRC)
					{
						SetCorrupt(src.offset);
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().objectAtHasBadZlibStream
							, src.offset, GetPackFile()));
					}
				}
				else
				{
					// We don't have a CRC32 code in the index, so compute it
					// now while inflating the raw data to get zlib to tell us
					// whether or not the data is safe.
					//
					Inflater inf = curs.Inflater();
					byte[] tmp = new byte[1024];
					if (quickCopy != null)
					{
						quickCopy.Check(inf, tmp, dataOffset, (int)dataLength);
					}
					else
					{
						long pos = dataOffset;
						long cnt = dataLength;
						while (cnt > 0)
						{
							int n = (int)Math.Min(cnt, buf.Length);
							ReadFully(pos, buf, 0, n, curs);
							crc1.Update(buf, 0, n);
							inf.SetInput(buf, 0, n);
							while (inf.Inflate(tmp, 0, tmp.Length) > 0)
							{
								continue;
							}
							pos += n;
							cnt -= n;
						}
					}
					if (!inf.IsFinished || inf.TotalIn != dataLength)
					{
						SetCorrupt(src.offset);
						throw new EOFException(MessageFormat.Format(JGitText.Get().shortCompressedStreamAt
							, src.offset));
					}
					expectedCRC = crc1.GetValue();
				}
			}
			catch (DataFormatException dataFormat)
			{
				SetCorrupt(src.offset);
				CorruptObjectException corruptObject = new CorruptObjectException(MessageFormat.Format
					(JGitText.Get().objectAtHasBadZlibStream, src.offset, GetPackFile()));
				Sharpen.Extensions.InitCause(corruptObject, dataFormat);
				StoredObjectRepresentationNotAvailableException gone;
				gone = new StoredObjectRepresentationNotAvailableException(src);
				Sharpen.Extensions.InitCause(gone, corruptObject);
				throw gone;
			}
			catch (IOException ioError)
			{
				StoredObjectRepresentationNotAvailableException gone;
				gone = new StoredObjectRepresentationNotAvailableException(src);
				Sharpen.Extensions.InitCause(gone, ioError);
				throw gone;
			}
			if (quickCopy != null)
			{
				// The entire object fits into a single byte array window slice,
				// and we have it pinned.  Write this out without copying.
				//
				@out.WriteHeader(src, inflatedLength);
				quickCopy.Write(@out, dataOffset, (int)dataLength);
			}
			else
			{
				if (dataLength <= buf.Length)
				{
					// Tiny optimization: Lots of objects are very small deltas or
					// deflated commits that are likely to fit in the copy buffer.
					//
					@out.WriteHeader(src, inflatedLength);
					@out.Write(buf, 0, (int)dataLength);
				}
				else
				{
					// Now we are committed to sending the object. As we spool it out,
					// check its CRC32 code to make sure there wasn't corruption between
					// the verification we did above, and us actually outputting it.
					//
					@out.WriteHeader(src, inflatedLength);
					long pos = dataOffset;
					long cnt = dataLength;
					while (cnt > 0)
					{
						int n = (int)Math.Min(cnt, buf.Length);
						ReadFully(pos, buf, 0, n, curs);
						crc2.Update(buf, 0, n);
						@out.Write(buf, 0, n);
						pos += n;
						cnt -= n;
					}
					if (crc2.GetValue() != expectedCRC)
					{
						throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().objectAtHasBadZlibStream
							, src.offset, GetPackFile()));
					}
				}
			}
		}

		internal virtual bool Invalid()
		{
			return invalid;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadFully(long position, byte[] dstbuf, int dstoff, int cnt, WindowCursor
			 curs)
		{
			if (curs.Copy(this, position, dstbuf, dstoff, cnt) != cnt)
			{
				throw new EOFException();
			}
		}

		/// <exception cref="NGit.Errors.StoredObjectRepresentationNotAvailableException"></exception>
		private void BeginCopyAsIs(ObjectToPack otp)
		{
			lock (this)
			{
				if (++activeCopyRawData == 1 && activeWindows == 0)
				{
					try
					{
						DoOpen();
					}
					catch (IOException thisPackNotValid)
					{
						StoredObjectRepresentationNotAvailableException gone;
						gone = new StoredObjectRepresentationNotAvailableException(otp);
						Sharpen.Extensions.InitCause(gone, thisPackNotValid);
						throw gone;
					}
				}
			}
		}

		private void EndCopyAsIs()
		{
			lock (this)
			{
				if (--activeCopyRawData == 0 && activeWindows == 0)
				{
					DoClose();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual bool BeginWindowCache()
		{
			lock (this)
			{
				if (++activeWindows == 1)
				{
					if (activeCopyRawData == 0)
					{
						DoOpen();
					}
					return true;
				}
				return false;
			}
		}

		internal virtual bool EndWindowCache()
		{
			lock (this)
			{
				bool r = --activeWindows == 0;
				if (r && activeCopyRawData == 0)
				{
					DoClose();
				}
				return r;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoOpen()
		{
			try
			{
				if (invalid)
				{
					throw new PackInvalidException(packFile);
				}
				lock (readLock)
				{
					fd = new RandomAccessFile(packFile, "r");
					length = fd.Length();
					OnOpenPack();
				}
			}
			catch (IOException ioe)
			{
				OpenFail();
				throw;
			}
			catch (RuntimeException re)
			{
				OpenFail();
				throw;
			}
			catch (Error re)
			{
				OpenFail();
				throw;
			}
		}

		private void OpenFail()
		{
			activeWindows = 0;
			activeCopyRawData = 0;
			invalid = true;
			DoClose();
		}

		private void DoClose()
		{
			lock (readLock)
			{
				if (fd != null)
				{
					try
					{
						fd.Close();
					}
					catch (IOException)
					{
					}
					// Ignore a close event. We had it open only for reading.
					// There should not be errors related to network buffers
					// not flushed, etc.
					fd = null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual ByteArrayWindow Read(long pos, int size)
		{
			lock (readLock)
			{
				if (length < pos + size)
				{
					size = (int)(length - pos);
				}
				byte[] buf = new byte[size];
				fd.Seek(pos);
				fd.ReadFully(buf, 0, size);
				return new ByteArrayWindow(this, pos, buf);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual ByteWindow Mmap(long pos, int size)
		{
			lock (readLock)
			{
				if (length < pos + size)
				{
					size = (int)(length - pos);
				}
				MappedByteBuffer map;
				try
				{
					map = fd.GetChannel().Map(FileChannel.MapMode.READ_ONLY, pos, size);
				}
				catch (IOException)
				{
					// The most likely reason this failed is the JVM has run out
					// of virtual memory. We need to discard quickly, and try to
					// force the GC to finalize and release any existing mappings.
					//
					System.GC.Collect();
					System.GC.WaitForPendingFinalizers();
					map = fd.GetChannel().Map(FileChannel.MapMode.READ_ONLY, pos, size);
				}
				if (map.HasArray())
				{
					return new ByteArrayWindow(this, pos, ((byte[])map.Array()));
				}
				return new ByteBufferWindow(this, pos, map);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void OnOpenPack()
		{
			PackIndex idx = Idx();
			byte[] buf = new byte[20];
			fd.Seek(0);
			fd.ReadFully(buf, 0, 12);
			if (RawParseUtils.Match(buf, 0, Constants.PACK_SIGNATURE) != 4)
			{
				throw new IOException(JGitText.Get().notAPACKFile);
			}
			long vers = NB.DecodeUInt32(buf, 4);
			long packCnt = NB.DecodeUInt32(buf, 8);
			if (vers != 2 && vers != 3)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().unsupportedPackVersion, 
					vers));
			}
			if (packCnt != idx.GetObjectCount())
			{
				throw new PackMismatchException(MessageFormat.Format(JGitText.Get().packObjectCountMismatch
					, packCnt, idx.GetObjectCount(), GetPackFile()));
			}
			fd.Seek(length - 20);
			fd.ReadFully(buf, 0, 20);
			if (!Arrays.Equals(buf, packChecksum))
			{
				throw new PackMismatchException(MessageFormat.Format(JGitText.Get().packObjectCountMismatch
					, ObjectId.FromRaw(buf).Name, ObjectId.FromRaw(idx.packChecksum).Name, GetPackFile
					()));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual ObjectLoader Load(WindowCursor curs, long pos)
		{
			byte[] ib = curs.tempId;
			ReadFully(pos, ib, 0, 20, curs);
			int c = ib[0] & unchecked((int)(0xff));
			int type = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			int p = 1;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ib[p++] & unchecked((int)(0xff));
				sz += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			try
			{
				switch (type)
				{
					case Constants.OBJ_COMMIT:
					case Constants.OBJ_TREE:
					case Constants.OBJ_BLOB:
					case Constants.OBJ_TAG:
					{
						if (sz < curs.GetStreamFileThreshold())
						{
							byte[] data;
							try
							{
								data = new byte[(int)sz];
							}
							catch (OutOfMemoryException)
							{
								return LargeWhole(curs, pos, type, sz, p);
							}
							Decompress(pos + p, curs, data, 0, data.Length);
							return new ObjectLoader.SmallObject(type, data);
						}
						return LargeWhole(curs, pos, type, sz, p);
					}

					case Constants.OBJ_OFS_DELTA:
					{
						c = ib[p++] & unchecked((int)(0xff));
						long ofs = c & 127;
						while ((c & 128) != 0)
						{
							ofs += 1;
							c = ib[p++] & unchecked((int)(0xff));
							ofs <<= 7;
							ofs += (c & 127);
						}
						return LoadDelta(pos, p, sz, pos - ofs, curs);
					}

					case Constants.OBJ_REF_DELTA:
					{
						ReadFully(pos + p, ib, 0, 20, curs);
						long ofs = FindDeltaBase(ObjectId.FromRaw(ib));
						return LoadDelta(pos, p + 20, sz, ofs, curs);
					}

					default:
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, type
							));
					}
				}
			}
			catch (DataFormatException dfe)
			{
				CorruptObjectException coe = new CorruptObjectException(MessageFormat.Format(JGitText
					.Get().objectAtHasBadZlibStream, pos, GetPackFile()));
				Sharpen.Extensions.InitCause(coe, dfe);
				throw coe;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		private long FindDeltaBase(ObjectId baseId)
		{
			long ofs = Idx().FindOffset(baseId);
			if (ofs < 0)
			{
				throw new MissingObjectException(baseId, JGitText.Get().missingDeltaBase);
			}
			return ofs;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.DataFormatException"></exception>
		private ObjectLoader LoadDelta(long posSelf, int hdrLen, long sz, long posBase, WindowCursor
			 curs)
		{
			if (int.MaxValue <= sz)
			{
				return LargeDelta(posSelf, hdrLen, posBase, curs);
			}
			byte[] @base;
			int type;
			DeltaBaseCache.Entry e = DeltaBaseCache.Get(this, posBase);
			if (e != null)
			{
				@base = e.data;
				type = e.type;
			}
			else
			{
				ObjectLoader p = Load(curs, posBase);
				try
				{
					@base = p.GetCachedBytes(curs.GetStreamFileThreshold());
				}
				catch (LargeObjectException)
				{
					return LargeDelta(posSelf, hdrLen, posBase, curs);
				}
				type = p.GetType();
				DeltaBaseCache.Store(this, posBase, @base, type);
			}
			byte[] delta;
			try
			{
				delta = new byte[(int)sz];
			}
			catch (OutOfMemoryException)
			{
				return LargeDelta(posSelf, hdrLen, posBase, curs);
			}
			Decompress(posSelf + hdrLen, curs, delta, 0, delta.Length);
			sz = BinaryDelta.GetResultSize(delta);
			if (int.MaxValue <= sz)
			{
				return LargeDelta(posSelf, hdrLen, posBase, curs);
			}
			byte[] result;
			try
			{
				result = new byte[(int)sz];
			}
			catch (OutOfMemoryException)
			{
				return LargeDelta(posSelf, hdrLen, posBase, curs);
			}
			BinaryDelta.Apply(@base, delta, result);
			return new ObjectLoader.SmallObject(type, result);
		}

		private LargePackedWholeObject LargeWhole(WindowCursor curs, long pos, int type, 
			long sz, int p)
		{
			return new LargePackedWholeObject(type, sz, pos, p, this, curs.db);
		}

		private LargePackedDeltaObject LargeDelta(long posObj, int hdrLen, long posBase, 
			WindowCursor wc)
		{
			return new LargePackedDeltaObject(posObj, posBase, hdrLen, this, wc.db);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Sharpen.DataFormatException"></exception>
		internal virtual byte[] GetDeltaHeader(WindowCursor wc, long pos)
		{
			// The delta stream starts as two variable length integers. If we
			// assume they are 64 bits each, we need 16 bytes to encode them,
			// plus 2 extra bytes for the variable length overhead. So 18 is
			// the longest delta instruction header.
			//
			byte[] hdr = new byte[18];
			wc.Inflate(this, pos, hdr, 0);
			return hdr;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual int GetObjectType(WindowCursor curs, long pos)
		{
			byte[] ib = curs.tempId;
			for (; ; )
			{
				ReadFully(pos, ib, 0, 20, curs);
				int c = ib[0] & unchecked((int)(0xff));
				int type = (c >> 4) & 7;
				switch (type)
				{
					case Constants.OBJ_COMMIT:
					case Constants.OBJ_TREE:
					case Constants.OBJ_BLOB:
					case Constants.OBJ_TAG:
					{
						return type;
					}

					case Constants.OBJ_OFS_DELTA:
					{
						int p = 1;
						while ((c & unchecked((int)(0x80))) != 0)
						{
							c = ib[p++] & unchecked((int)(0xff));
						}
						c = ib[p++] & unchecked((int)(0xff));
						long ofs = c & 127;
						while ((c & 128) != 0)
						{
							ofs += 1;
							c = ib[p++] & unchecked((int)(0xff));
							ofs <<= 7;
							ofs += (c & 127);
						}
						pos = pos - ofs;
						continue;
						goto case Constants.OBJ_REF_DELTA;
					}

					case Constants.OBJ_REF_DELTA:
					{
						int p = 1;
						while ((c & unchecked((int)(0x80))) != 0)
						{
							c = ib[p++] & unchecked((int)(0xff));
						}
						ReadFully(pos + p, ib, 0, 20, curs);
						pos = FindDeltaBase(ObjectId.FromRaw(ib));
						continue;
						goto default;
					}

					default:
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, type
							));
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual long GetObjectSize(WindowCursor curs, AnyObjectId id)
		{
			long offset = Idx().FindOffset(id);
			return 0 < offset ? GetObjectSize(curs, offset) : -1;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual long GetObjectSize(WindowCursor curs, long pos)
		{
			byte[] ib = curs.tempId;
			ReadFully(pos, ib, 0, 20, curs);
			int c = ib[0] & unchecked((int)(0xff));
			int type = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			int p = 1;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ib[p++] & unchecked((int)(0xff));
				sz += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			long deltaAt;
			switch (type)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					return sz;
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ib[p++] & unchecked((int)(0xff));
					while ((c & 128) != 0)
					{
						c = ib[p++] & unchecked((int)(0xff));
					}
					deltaAt = pos + p;
					break;
				}

				case Constants.OBJ_REF_DELTA:
				{
					deltaAt = pos + p + 20;
					break;
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, type
						));
				}
			}
			try
			{
				return BinaryDelta.GetResultSize(GetDeltaHeader(curs, deltaAt));
			}
			catch (DataFormatException)
			{
				throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().objectAtHasBadZlibStream
					, pos, GetPackFile()));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual LocalObjectRepresentation Representation(WindowCursor curs, AnyObjectId
			 objectId)
		{
			long pos = Idx().FindOffset(objectId);
			if (pos < 0)
			{
				return null;
			}
			byte[] ib = curs.tempId;
			ReadFully(pos, ib, 0, 20, curs);
			int c = ib[0] & unchecked((int)(0xff));
			int p = 1;
			int typeCode = (c >> 4) & 7;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ib[p++] & unchecked((int)(0xff));
			}
			long len = (FindEndOffset(pos) - pos);
			switch (typeCode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					return LocalObjectRepresentation.NewWhole(this, pos, len - p);
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ib[p++] & unchecked((int)(0xff));
					long ofs = c & 127;
					while ((c & 128) != 0)
					{
						ofs += 1;
						c = ib[p++] & unchecked((int)(0xff));
						ofs <<= 7;
						ofs += (c & 127);
					}
					ofs = pos - ofs;
					return LocalObjectRepresentation.NewDelta(this, pos, len - p, ofs);
				}

				case Constants.OBJ_REF_DELTA:
				{
					len -= p;
					len -= Constants.OBJECT_ID_LENGTH;
					ReadFully(pos + p, ib, 0, 20, curs);
					ObjectId id = ObjectId.FromRaw(ib);
					return LocalObjectRepresentation.NewDelta(this, pos, len, id);
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, typeCode
						));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		private long FindEndOffset(long startOffset)
		{
			long maxOffset = length - 20;
			return GetReverseIdx().FindNextOffset(startOffset, maxOffset);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PackReverseIndex GetReverseIdx()
		{
			lock (this)
			{
				if (reverseIdx == null)
				{
					reverseIdx = new PackReverseIndex(Idx());
				}
				return reverseIdx;
			}
		}

		private bool IsCorrupt(long offset)
		{
			LongList list = corruptObjects;
			if (list == null)
			{
				return false;
			}
			lock (list)
			{
				return list.Contains(offset);
			}
		}

		private void SetCorrupt(long offset)
		{
			LongList list = corruptObjects;
			if (list == null)
			{
				lock (readLock)
				{
					list = corruptObjects;
					if (list == null)
					{
						list = new LongList();
						corruptObjects = list;
					}
				}
			}
			lock (list)
			{
				list.Add(offset);
			}
		}
	}
}
