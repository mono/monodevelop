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
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Indexes Git pack files for local use.</summary>
	/// <remarks>Indexes Git pack files for local use.</remarks>
	public class IndexPack
	{
		/// <summary>Progress message when reading raw data from the pack.</summary>
		/// <remarks>Progress message when reading raw data from the pack.</remarks>
		public static readonly string PROGRESS_DOWNLOAD = JGitText.Get().receivingObjects;

		/// <summary>Progress message when computing names of delta compressed objects.</summary>
		/// <remarks>Progress message when computing names of delta compressed objects.</remarks>
		public static readonly string PROGRESS_RESOLVE_DELTA = JGitText.Get().resolvingDeltas;

		/// <summary>Size of the internal stream buffer.</summary>
		/// <remarks>
		/// Size of the internal stream buffer.
		/// <p>
		/// If callers are going to be supplying IndexPack a BufferedInputStream they
		/// should use this buffer size as the size of the buffer for that
		/// BufferedInputStream, and any other its may be wrapping. This way the
		/// buffers will cascade efficiently and only the IndexPack buffer will be
		/// receiving the bulk of the data stream.
		/// </remarks>
		public const int BUFFER_SIZE = 8192;

		/// <summary>Create an index pack instance to load a new pack into a repository.</summary>
		/// <remarks>
		/// Create an index pack instance to load a new pack into a repository.
		/// <p>
		/// The received pack data and generated index will be saved to temporary
		/// files within the repository's <code>objects</code> directory. To use the
		/// data contained within them call
		/// <see cref="RenameAndOpenPack()">RenameAndOpenPack()</see>
		/// once the
		/// indexing is complete.
		/// </remarks>
		/// <param name="db">the repository that will receive the new pack.</param>
		/// <param name="is">
		/// stream to read the pack data from. If the stream is buffered
		/// use
		/// <see cref="BUFFER_SIZE">BUFFER_SIZE</see>
		/// as the buffer size for the stream.
		/// </param>
		/// <returns>a new index pack instance.</returns>
		/// <exception cref="System.IO.IOException">a temporary file could not be created.</exception>
		public static NGit.Transport.IndexPack Create(Repository db, InputStream @is)
		{
			string suffix = ".pack";
			FilePath objdir = db.ObjectsDirectory;
			FilePath tmp = FilePath.CreateTempFile("incoming_", suffix, objdir);
			string n = tmp.GetName();
			FilePath @base;
			@base = new FilePath(objdir, Sharpen.Runtime.Substring(n, 0, n.Length - suffix.Length
				));
			NGit.Transport.IndexPack ip = new NGit.Transport.IndexPack(db, @is, @base);
			ip.SetIndexVersion(db.GetConfig().Get(CoreConfig.KEY).GetPackIndexVersion());
			return ip;
		}

		private enum Source
		{
			INPUT,
			FILE
		}

		private readonly Repository repo;

		/// <summary>Object database used for loading existing objects</summary>
		private readonly ObjectDatabase objectDatabase;

		private Inflater inflater;

		private readonly MessageDigest objectDigest;

		private readonly MutableObjectId tempObjectId;

		private InputStream @in;

		private byte[] buf;

		private long bBase;

		private int bOffset;

		private int bAvail;

		private ObjectChecker objCheck;

		private bool fixThin;

		private bool keepEmpty;

		private bool needBaseObjectIds;

		private int outputVersion;

		private readonly FilePath dstPack;

		private readonly FilePath dstIdx;

		private long objectCount;

		private PackedObjectInfo[] entries;

		/// <summary>Every object contained within the incoming pack.</summary>
		/// <remarks>
		/// Every object contained within the incoming pack.
		/// <p>
		/// This is a subset of
		/// <see cref="entries">entries</see>
		/// , as thin packs can add additional
		/// objects to
		/// <code>entries</code>
		/// by copying already existing objects from the
		/// repository onto the end of the thin pack to make it self-contained.
		/// </remarks>
		private ObjectIdSubclassMap<ObjectId> newObjectIds;

		private int deltaCount;

		private int entryCount;

		private readonly CRC32 crc = new CRC32();

		private ObjectIdSubclassMap<IndexPack.DeltaChain> baseById;

		/// <summary>Objects referenced by their name from deltas, that aren't in this pack.</summary>
		/// <remarks>
		/// Objects referenced by their name from deltas, that aren't in this pack.
		/// <p>
		/// This is the set of objects that were copied onto the end of this pack to
		/// make it complete. These objects were not transmitted by the remote peer,
		/// but instead were assumed to already exist in the local repository.
		/// </remarks>
		private ObjectIdSubclassMap<ObjectId> baseObjectIds;

		private LongMap<IndexPack.UnresolvedDelta> baseByPos;

		private byte[] skipBuffer;

		private MessageDigest packDigest;

		private RandomAccessFile packOut;

		private byte[] packcsum;

		/// <summary>
		/// If
		/// <see cref="fixThin">fixThin</see>
		/// this is the last byte of the original checksum.
		/// </summary>
		private long originalEOF;

		private ObjectReader readCurs;

		/// <summary>Create a new pack indexer utility.</summary>
		/// <remarks>Create a new pack indexer utility.</remarks>
		/// <param name="db"></param>
		/// <param name="src">
		/// stream to read the pack data from. If the stream is buffered
		/// use
		/// <see cref="BUFFER_SIZE">BUFFER_SIZE</see>
		/// as the buffer size for the stream.
		/// </param>
		/// <param name="dstBase"></param>
		/// <exception cref="System.IO.IOException">the output packfile could not be created.
		/// 	</exception>
		public IndexPack(Repository db, InputStream src, FilePath dstBase)
		{
			repo = db;
			objectDatabase = db.ObjectDatabase.NewCachedDatabase();
			@in = src;
			inflater = InflaterCache.Get();
			readCurs = objectDatabase.NewReader();
			buf = new byte[BUFFER_SIZE];
			skipBuffer = new byte[512];
			objectDigest = Constants.NewMessageDigest();
			tempObjectId = new MutableObjectId();
			packDigest = Constants.NewMessageDigest();
			if (dstBase != null)
			{
				FilePath dir = dstBase.GetParentFile();
				string nam = dstBase.GetName();
				dstPack = new FilePath(dir, nam + ".pack");
				dstIdx = new FilePath(dir, nam + ".idx");
				packOut = new RandomAccessFile(dstPack, "rw");
				packOut.SetLength(0);
			}
			else
			{
				dstPack = null;
				dstIdx = null;
			}
		}

		/// <summary>Set the pack index file format version this instance will create.</summary>
		/// <remarks>Set the pack index file format version this instance will create.</remarks>
		/// <param name="version">
		/// the version to write. The special version 0 designates the
		/// oldest (most compatible) format available for the objects.
		/// </param>
		/// <seealso cref="NGit.Storage.File.PackIndexWriter">NGit.Storage.File.PackIndexWriter
		/// 	</seealso>
		public virtual void SetIndexVersion(int version)
		{
			outputVersion = version;
		}

		/// <summary>Configure this index pack instance to make a thin pack complete.</summary>
		/// <remarks>
		/// Configure this index pack instance to make a thin pack complete.
		/// <p>
		/// Thin packs are sometimes used during network transfers to allow a delta
		/// to be sent without a base object. Such packs are not permitted on disk.
		/// They can be fixed by copying the base object onto the end of the pack.
		/// </remarks>
		/// <param name="fix">true to enable fixing a thin pack.</param>
		public virtual void SetFixThin(bool fix)
		{
			fixThin = fix;
		}

		/// <summary>Configure this index pack instance to keep an empty pack.</summary>
		/// <remarks>
		/// Configure this index pack instance to keep an empty pack.
		/// <p>
		/// By default an empty pack (a pack with no objects) is not kept, as doing
		/// so is completely pointless. With no objects in the pack there is no data
		/// stored by it, so the pack is unnecessary.
		/// </remarks>
		/// <param name="empty">true to enable keeping an empty pack.</param>
		public virtual void SetKeepEmpty(bool empty)
		{
			keepEmpty = empty;
		}

		/// <summary>Configure this index pack instance to keep track of new objects.</summary>
		/// <remarks>
		/// Configure this index pack instance to keep track of new objects.
		/// <p>
		/// By default an index pack doesn't save the new objects that were created
		/// when it was instantiated. Setting this flag to
		/// <code>true</code>
		/// allows the
		/// caller to use
		/// <see cref="GetNewObjectIds()">GetNewObjectIds()</see>
		/// to retrieve that list.
		/// </remarks>
		/// <param name="b">
		/// 
		/// <code>true</code>
		/// to enable keeping track of new objects.
		/// </param>
		public virtual void SetNeedNewObjectIds(bool b)
		{
			if (b)
			{
				newObjectIds = new ObjectIdSubclassMap<ObjectId>();
			}
			else
			{
				newObjectIds = null;
			}
		}

		private bool NeedNewObjectIds()
		{
			return newObjectIds != null;
		}

		/// <summary>
		/// Configure this index pack instance to keep track of the objects assumed
		/// for delta bases.
		/// </summary>
		/// <remarks>
		/// Configure this index pack instance to keep track of the objects assumed
		/// for delta bases.
		/// <p>
		/// By default an index pack doesn't save the objects that were used as delta
		/// bases. Setting this flag to
		/// <code>true</code>
		/// will allow the caller to
		/// use
		/// <see cref="GetBaseObjectIds()">GetBaseObjectIds()</see>
		/// to retrieve that list.
		/// </remarks>
		/// <param name="b">
		/// 
		/// <code>true</code>
		/// to enable keeping track of delta bases.
		/// </param>
		public virtual void SetNeedBaseObjectIds(bool b)
		{
			this.needBaseObjectIds = b;
		}

		/// <returns>the new objects that were sent by the user</returns>
		public virtual ObjectIdSubclassMap<ObjectId> GetNewObjectIds()
		{
			if (newObjectIds != null)
			{
				return newObjectIds;
			}
			return new ObjectIdSubclassMap<ObjectId>();
		}

		/// <returns>set of objects the incoming pack assumed for delta purposes</returns>
		public virtual ObjectIdSubclassMap<ObjectId> GetBaseObjectIds()
		{
			if (baseObjectIds != null)
			{
				return baseObjectIds;
			}
			return new ObjectIdSubclassMap<ObjectId>();
		}

		/// <summary>Configure the checker used to validate received objects.</summary>
		/// <remarks>
		/// Configure the checker used to validate received objects.
		/// <p>
		/// Usually object checking isn't necessary, as Git implementations only
		/// create valid objects in pack files. However, additional checking may be
		/// useful if processing data from an untrusted source.
		/// </remarks>
		/// <param name="oc">the checker instance; null to disable object checking.</param>
		public virtual void SetObjectChecker(ObjectChecker oc)
		{
			objCheck = oc;
		}

		/// <summary>Configure the checker used to validate received objects.</summary>
		/// <remarks>
		/// Configure the checker used to validate received objects.
		/// <p>
		/// Usually object checking isn't necessary, as Git implementations only
		/// create valid objects in pack files. However, additional checking may be
		/// useful if processing data from an untrusted source.
		/// <p>
		/// This is shorthand for:
		/// <pre>
		/// setObjectChecker(on ? new ObjectChecker() : null);
		/// </pre>
		/// </remarks>
		/// <param name="on">true to enable the default checker; false to disable it.</param>
		public virtual void SetObjectChecking(bool on)
		{
			SetObjectChecker(on ? new ObjectChecker() : null);
		}

		/// <summary>Consume data from the input stream until the packfile is indexed.</summary>
		/// <remarks>Consume data from the input stream until the packfile is indexed.</remarks>
		/// <param name="progress">progress feedback</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Index(ProgressMonitor progress)
		{
			progress.Start(2);
			try
			{
				try
				{
					ReadPackHeader();
					entries = new PackedObjectInfo[(int)objectCount];
					baseById = new ObjectIdSubclassMap<IndexPack.DeltaChain>();
					baseByPos = new LongMap<IndexPack.UnresolvedDelta>();
					progress.BeginTask(PROGRESS_DOWNLOAD, (int)objectCount);
					for (int done = 0; done < objectCount; done++)
					{
						IndexOneObject();
						progress.Update(1);
						if (progress.IsCancelled())
						{
							throw new IOException(JGitText.Get().downloadCancelled);
						}
					}
					ReadPackFooter();
					EndInput();
					progress.EndTask();
					if (deltaCount > 0)
					{
						if (packOut == null)
						{
							throw new IOException(JGitText.Get().needPackOut);
						}
						ResolveDeltas(progress);
						if (entryCount < objectCount)
						{
							if (!fixThin)
							{
								throw new IOException(MessageFormat.Format(JGitText.Get().packHasUnresolvedDeltas
									, (objectCount - entryCount)));
							}
							FixThinPack(progress);
						}
					}
					if (packOut != null && (keepEmpty || entryCount > 0))
					{
						packOut.GetChannel().Force(true);
					}
					packDigest = null;
					baseById = null;
					baseByPos = null;
					if (dstIdx != null && (keepEmpty || entryCount > 0))
					{
						WriteIdx();
					}
				}
				finally
				{
					try
					{
						if (readCurs != null)
						{
							readCurs.Release();
						}
					}
					finally
					{
						readCurs = null;
					}
					try
					{
						InflaterCache.Release(inflater);
					}
					finally
					{
						inflater = null;
						objectDatabase.Close();
					}
					progress.EndTask();
					if (packOut != null)
					{
						packOut.Close();
					}
				}
				if (keepEmpty || entryCount > 0)
				{
					if (dstPack != null)
					{
						dstPack.SetReadOnly();
					}
					if (dstIdx != null)
					{
						dstIdx.SetReadOnly();
					}
				}
			}
			catch (IOException err)
			{
				if (dstPack != null)
				{
					dstPack.Delete();
				}
				if (dstIdx != null)
				{
					dstIdx.Delete();
				}
				throw;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltas(ProgressMonitor progress)
		{
			progress.BeginTask(PROGRESS_RESOLVE_DELTA, deltaCount);
			int last = entryCount;
			for (int i = 0; i < last; i++)
			{
				int before = entryCount;
				ResolveDeltas(entries[i]);
				progress.Update(entryCount - before);
				if (progress.IsCancelled())
				{
					throw new IOException(JGitText.Get().downloadCancelledDuringIndexing);
				}
			}
			progress.EndTask();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltas(PackedObjectInfo oe)
		{
			int oldCRC = oe.GetCRC();
			if (baseById.Get(oe) != null || baseByPos.ContainsKey(oe.GetOffset()))
			{
				ResolveDeltas(oe.GetOffset(), oldCRC, Constants.OBJ_BAD, null, oe);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltas(long pos, int oldCRC, int type, byte[] data, PackedObjectInfo
			 oe)
		{
			crc.Reset();
			Position(pos);
			int c = ReadFrom(IndexPack.Source.FILE);
			int typeCode = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ReadFrom(IndexPack.Source.FILE);
				sz += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			switch (typeCode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					type = typeCode;
					data = InflateAndReturn(IndexPack.Source.FILE, sz);
					break;
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ReadFrom(IndexPack.Source.FILE) & unchecked((int)(0xff));
					while ((c & 128) != 0)
					{
						c = ReadFrom(IndexPack.Source.FILE) & unchecked((int)(0xff));
					}
					data = BinaryDelta.Apply(data, InflateAndReturn(IndexPack.Source.FILE, sz));
					break;
				}

				case Constants.OBJ_REF_DELTA:
				{
					crc.Update(buf, Fill(IndexPack.Source.FILE, 20), 20);
					Use(20);
					data = BinaryDelta.Apply(data, InflateAndReturn(IndexPack.Source.FILE, sz));
					break;
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, typeCode
						));
				}
			}
			int crc32 = (int)crc.GetValue();
			if (oldCRC != crc32)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().corruptionDetectedReReadingAt
					, pos));
			}
			if (oe == null)
			{
				objectDigest.Update(Constants.EncodedTypeString(type));
				objectDigest.Update(unchecked((byte)' '));
				objectDigest.Update(Constants.EncodeASCII(data.Length));
				objectDigest.Update(unchecked((byte)0));
				objectDigest.Update(data);
				tempObjectId.FromRaw(objectDigest.Digest(), 0);
				VerifySafeObject(tempObjectId, type, data);
				oe = new PackedObjectInfo(pos, crc32, tempObjectId);
				AddObjectAndTrack(oe);
			}
			ResolveChildDeltas(pos, type, data, oe);
		}

		private IndexPack.UnresolvedDelta RemoveBaseById(AnyObjectId id)
		{
			IndexPack.DeltaChain d = baseById.Get(id);
			return d != null ? d.Remove() : null;
		}

		private static IndexPack.UnresolvedDelta Reverse(IndexPack.UnresolvedDelta c)
		{
			IndexPack.UnresolvedDelta tail = null;
			while (c != null)
			{
				IndexPack.UnresolvedDelta n = c.next;
				c.next = tail;
				tail = c;
				c = n;
			}
			return tail;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveChildDeltas(long pos, int type, byte[] data, PackedObjectInfo
			 oe)
		{
			IndexPack.UnresolvedDelta a = Reverse(RemoveBaseById(oe));
			IndexPack.UnresolvedDelta b = Reverse(baseByPos.Remove(pos));
			while (a != null && b != null)
			{
				if (a.position < b.position)
				{
					ResolveDeltas(a.position, a.crc, type, data, null);
					a = a.next;
				}
				else
				{
					ResolveDeltas(b.position, b.crc, type, data, null);
					b = b.next;
				}
			}
			ResolveChildDeltaChain(type, data, a);
			ResolveChildDeltaChain(type, data, b);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveChildDeltaChain(int type, byte[] data, IndexPack.UnresolvedDelta
			 a)
		{
			while (a != null)
			{
				ResolveDeltas(a.position, a.crc, type, data, null);
				a = a.next;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FixThinPack(ProgressMonitor progress)
		{
			GrowEntries();
			if (needBaseObjectIds)
			{
				baseObjectIds = new ObjectIdSubclassMap<ObjectId>();
			}
			packDigest.Reset();
			originalEOF = packOut.Length() - 20;
			Deflater def = new Deflater(Deflater.DEFAULT_COMPRESSION, false);
			IList<IndexPack.DeltaChain> missing = new AList<IndexPack.DeltaChain>(64);
			long end = originalEOF;
			foreach (IndexPack.DeltaChain baseId in baseById)
			{
				if (baseId.head == null)
				{
					continue;
				}
				if (needBaseObjectIds)
				{
					baseObjectIds.Add(baseId);
				}
				ObjectLoader ldr;
				try
				{
					ldr = readCurs.Open(baseId);
				}
				catch (MissingObjectException)
				{
					missing.AddItem(baseId);
					continue;
				}
				byte[] data = ldr.GetCachedBytes(int.MaxValue);
				int typeCode = ldr.GetType();
				PackedObjectInfo oe;
				crc.Reset();
				packOut.Seek(end);
				WriteWhole(def, typeCode, data);
				oe = new PackedObjectInfo(end, (int)crc.GetValue(), baseId);
				entries[entryCount++] = oe;
				end = packOut.GetFilePointer();
				ResolveChildDeltas(oe.GetOffset(), typeCode, data, oe);
				if (progress.IsCancelled())
				{
					throw new IOException(JGitText.Get().downloadCancelledDuringIndexing);
				}
			}
			def.Finish();
			foreach (IndexPack.DeltaChain @base in missing)
			{
				if (@base.head != null)
				{
					throw new MissingObjectException(@base, "delta base");
				}
			}
			if (end - originalEOF < 20)
			{
				// Ugly corner case; if what we appended on to complete deltas
				// doesn't completely cover the SHA-1 we have to truncate off
				// we need to shorten the file, otherwise we will include part
				// of the old footer as object content.
				packOut.SetLength(end);
			}
			FixHeaderFooter(packcsum, packDigest.Digest());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteWhole(Deflater def, int typeCode, byte[] data)
		{
			int sz = data.Length;
			int hdrlen = 0;
			buf[hdrlen++] = unchecked((byte)((typeCode << 4) | sz & 15));
			sz = (int)(((uint)sz) >> 4);
			while (sz > 0)
			{
				buf[hdrlen - 1] |= unchecked((int)(0x80));
				buf[hdrlen++] = unchecked((byte)(sz & unchecked((int)(0x7f))));
				sz = (int)(((uint)sz) >> 7);
			}
			packDigest.Update(buf, 0, hdrlen);
			crc.Update(buf, 0, hdrlen);
			packOut.Write(buf, 0, hdrlen);
			def.Reset();
			def.SetInput(data);
			def.Finish();
			while (!def.IsFinished)
			{
				int datlen = def.Deflate(buf);
				packDigest.Update(buf, 0, datlen);
				crc.Update(buf, 0, datlen);
				packOut.Write(buf, 0, datlen);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FixHeaderFooter(byte[] origcsum, byte[] tailcsum)
		{
			MessageDigest origDigest = Constants.NewMessageDigest();
			MessageDigest tailDigest = Constants.NewMessageDigest();
			long origRemaining = originalEOF;
			packOut.Seek(0);
			bAvail = 0;
			bOffset = 0;
			Fill(IndexPack.Source.FILE, 12);
			{
				int origCnt = (int)Math.Min(bAvail, origRemaining);
				origDigest.Update(buf, 0, origCnt);
				origRemaining -= origCnt;
				if (origRemaining == 0)
				{
					tailDigest.Update(buf, origCnt, bAvail - origCnt);
				}
			}
			NB.EncodeInt32(buf, 8, entryCount);
			packOut.Seek(0);
			packOut.Write(buf, 0, 12);
			packOut.Seek(bAvail);
			packDigest.Reset();
			packDigest.Update(buf, 0, bAvail);
			for (; ; )
			{
				int n = packOut.Read(buf);
				if (n < 0)
				{
					break;
				}
				if (origRemaining != 0)
				{
					int origCnt2 = (int)Math.Min(n, origRemaining);
					origDigest.Update(buf, 0, origCnt2);
					origRemaining -= origCnt2;
					if (origRemaining == 0)
					{
						tailDigest.Update(buf, origCnt2, n - origCnt2);
					}
				}
				else
				{
					tailDigest.Update(buf, 0, n);
				}
				packDigest.Update(buf, 0, n);
			}
			if (!Arrays.Equals(origDigest.Digest(), origcsum) || !Arrays.Equals(tailDigest.Digest
				(), tailcsum))
			{
				throw new IOException(JGitText.Get().packCorruptedWhileWritingToFilesystem);
			}
			packcsum = packDigest.Digest();
			packOut.Write(packcsum);
		}

		private void GrowEntries()
		{
			PackedObjectInfo[] ne;
			ne = new PackedObjectInfo[(int)objectCount + baseById.Size()];
			System.Array.Copy(entries, 0, ne, 0, entryCount);
			entries = ne;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteIdx()
		{
			Arrays.Sort(entries, 0, entryCount);
			IList<PackedObjectInfo> list = Arrays.AsList(entries);
			if (entryCount < entries.Length)
			{
				list = list.SubList(0, entryCount);
			}
			FileOutputStream os = new FileOutputStream(dstIdx);
			try
			{
				PackIndexWriter iw;
				if (outputVersion <= 0)
				{
					iw = PackIndexWriter.CreateOldestPossible(os, list);
				}
				else
				{
					iw = PackIndexWriter.CreateVersion(os, outputVersion);
				}
				iw.Write(list, packcsum);
				os.GetChannel().Force(true);
			}
			finally
			{
				os.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadPackHeader()
		{
			int hdrln = Constants.PACK_SIGNATURE.Length + 4 + 4;
			int p = Fill(IndexPack.Source.INPUT, hdrln);
			for (int k = 0; k < Constants.PACK_SIGNATURE.Length; k++)
			{
				if (buf[p + k] != Constants.PACK_SIGNATURE[k])
				{
					throw new IOException(JGitText.Get().notAPACKFile);
				}
			}
			long vers = NB.DecodeUInt32(buf, p + 4);
			if (vers != 2 && vers != 3)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().unsupportedPackVersion, 
					vers));
			}
			objectCount = NB.DecodeUInt32(buf, p + 8);
			Use(hdrln);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadPackFooter()
		{
			Sync();
			byte[] cmpcsum = packDigest.Digest();
			int c = Fill(IndexPack.Source.INPUT, 20);
			packcsum = new byte[20];
			System.Array.Copy(buf, c, packcsum, 0, 20);
			Use(20);
			if (packOut != null)
			{
				packOut.Write(packcsum);
			}
			if (!Arrays.Equals(cmpcsum, packcsum))
			{
				throw new CorruptObjectException(JGitText.Get().corruptObjectPackfileChecksumIncorrect
					);
			}
		}

		// Cleanup all resources associated with our input parsing.
		private void EndInput()
		{
			@in = null;
			skipBuffer = null;
		}

		// Read one entire object or delta from the input.
		/// <exception cref="System.IO.IOException"></exception>
		private void IndexOneObject()
		{
			long pos = Position();
			crc.Reset();
			int c = ReadFrom(IndexPack.Source.INPUT);
			int typeCode = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ReadFrom(IndexPack.Source.INPUT);
				sz += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			switch (typeCode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					Whole(typeCode, pos, sz);
					break;
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ReadFrom(IndexPack.Source.INPUT);
					long ofs = c & 127;
					while ((c & 128) != 0)
					{
						ofs += 1;
						c = ReadFrom(IndexPack.Source.INPUT);
						ofs <<= 7;
						ofs += (c & 127);
					}
					long @base = pos - ofs;
					IndexPack.UnresolvedDelta n;
					InflateAndSkip(IndexPack.Source.INPUT, sz);
					n = new IndexPack.UnresolvedDelta(pos, (int)crc.GetValue());
					n.next = baseByPos.Put(@base, n);
					deltaCount++;
					break;
				}

				case Constants.OBJ_REF_DELTA:
				{
					c = Fill(IndexPack.Source.INPUT, 20);
					crc.Update(buf, c, 20);
					ObjectId @base = ObjectId.FromRaw(buf, c);
					Use(20);
					IndexPack.DeltaChain r = baseById.Get(@base);
					if (r == null)
					{
						r = new IndexPack.DeltaChain(@base);
						baseById.Add(r);
					}
					InflateAndSkip(IndexPack.Source.INPUT, sz);
					r.Add(new IndexPack.UnresolvedDelta(pos, (int)crc.GetValue()));
					deltaCount++;
					break;
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, typeCode
						));
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Whole(int type, long pos, long sz)
		{
			byte[] data = InflateAndReturn(IndexPack.Source.INPUT, sz);
			objectDigest.Update(Constants.EncodedTypeString(type));
			objectDigest.Update(unchecked((byte)' '));
			objectDigest.Update(Constants.EncodeASCII(sz));
			objectDigest.Update(unchecked((byte)0));
			objectDigest.Update(data);
			tempObjectId.FromRaw(objectDigest.Digest(), 0);
			VerifySafeObject(tempObjectId, type, data);
			int crc32 = (int)crc.GetValue();
			AddObjectAndTrack(new PackedObjectInfo(pos, crc32, tempObjectId));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VerifySafeObject(AnyObjectId id, int type, byte[] data)
		{
			if (objCheck != null)
			{
				try
				{
					objCheck.Check(type, data);
				}
				catch (CorruptObjectException e)
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().invalidObject, Constants
						.TypeString(type), id.Name, e.Message));
				}
			}
			try
			{
				ObjectLoader ldr = readCurs.Open(id, type);
				byte[] existingData = ldr.GetCachedBytes(int.MaxValue);
				if (!Arrays.Equals(data, existingData))
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().collisionOn, id.Name));
				}
			}
			catch (MissingObjectException)
			{
			}
		}

		// This is OK, we don't have a copy of the object locally
		// but the API throws when we try to read it as usually its
		// an error to read something that doesn't exist.
		// Current position of {@link #bOffset} within the entire file.
		private long Position()
		{
			return bBase + bOffset;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Position(long pos)
		{
			packOut.Seek(pos);
			bBase = pos;
			bOffset = 0;
			bAvail = 0;
		}

		// Consume exactly one byte from the buffer and return it.
		/// <exception cref="System.IO.IOException"></exception>
		private int ReadFrom(IndexPack.Source src)
		{
			if (bAvail == 0)
			{
				Fill(src, 1);
			}
			bAvail--;
			int b = buf[bOffset++] & unchecked((int)(0xff));
			crc.Update(b);
			return b;
		}

		// Consume cnt bytes from the buffer.
		private void Use(int cnt)
		{
			bOffset += cnt;
			bAvail -= cnt;
		}

		// Ensure at least need bytes are available in in {@link #buf}.
		/// <exception cref="System.IO.IOException"></exception>
		private int Fill(IndexPack.Source src, int need)
		{
			while (bAvail < need)
			{
				int next = bOffset + bAvail;
				int free = buf.Length - next;
				if (free + bAvail < need)
				{
					switch (src)
					{
						case IndexPack.Source.INPUT:
						{
							Sync();
							break;
						}

						case IndexPack.Source.FILE:
						{
							if (bAvail > 0)
							{
								System.Array.Copy(buf, bOffset, buf, 0, bAvail);
							}
							bOffset = 0;
							break;
						}
					}
					next = bAvail;
					free = buf.Length - next;
				}
				switch (src)
				{
					case IndexPack.Source.INPUT:
					{
						next = @in.Read(buf, next, free);
						break;
					}

					case IndexPack.Source.FILE:
					{
						next = packOut.Read(buf, next, free);
						break;
					}
				}
				if (next <= 0)
				{
					throw new EOFException(JGitText.Get().packfileIsTruncated);
				}
				bAvail += next;
			}
			return bOffset;
		}

		// Store consumed bytes in {@link #buf} up to {@link #bOffset}.
		/// <exception cref="System.IO.IOException"></exception>
		private void Sync()
		{
			packDigest.Update(buf, 0, bOffset);
			if (packOut != null)
			{
				packOut.Write(buf, 0, bOffset);
			}
			if (bAvail > 0)
			{
				System.Array.Copy(buf, bOffset, buf, 0, bAvail);
			}
			bBase += bOffset;
			bOffset = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void InflateAndSkip(IndexPack.Source src, long inflatedSize)
		{
			Inflate(src, inflatedSize, skipBuffer, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private byte[] InflateAndReturn(IndexPack.Source src, long inflatedSize)
		{
			byte[] dst = new byte[(int)inflatedSize];
			Inflate(src, inflatedSize, dst, true);
			return dst;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Inflate(IndexPack.Source src, long inflatedSize, byte[] dst, bool keep
			)
		{
			Inflater inf = inflater;
			try
			{
				int off = 0;
				long cnt = 0;
				int p = Fill(src, 24);
				inf.SetInput(buf, p, bAvail);
				for (; ; )
				{
					// The +1 is a workaround to a bug in SharpZipLib: RemainingInput won't be properly updated if the inflate
					// length is 0.
					int r = inf.Inflate(dst, off, dst.Length - off + 1);
					if (r == 0)
					{
						if (inf.IsFinished)
						{
							break;
						}
						if (inf.IsNeedingInput)
						{
							if (p >= 0)
							{
								crc.Update(buf, p, bAvail);
								Use(bAvail);
							}
							p = Fill(src, 24);
							inf.SetInput(buf, p, bAvail);
						}
						else
						{
							break;
//							throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
//								, JGitText.Get().unknownZlibError));
						}
					}
					cnt += r;
					if (keep)
					{
						off += r;
					}
				}
				if (cnt != inflatedSize)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
						, JGitText.Get().wrongDecompressedLength));
				}
				int left = bAvail - inf.RemainingInput;
				if (left > 0)
				{
					crc.Update(buf, p, left);
					Use(left);
				}
			}
			catch (DataFormatException dfe)
			{
				throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
					, dfe.Message));
			}
			finally
			{
				inf.Reset();
			}
		}

		[System.Serializable]
		private class DeltaChain : ObjectId
		{
			internal IndexPack.UnresolvedDelta head;

			protected internal DeltaChain(AnyObjectId id) : base(id)
			{
			}

			internal virtual IndexPack.UnresolvedDelta Remove()
			{
				IndexPack.UnresolvedDelta r = head;
				if (r != null)
				{
					head = null;
				}
				return r;
			}

			internal virtual void Add(IndexPack.UnresolvedDelta d)
			{
				d.next = head;
				head = d;
			}
		}

		private class UnresolvedDelta
		{
			internal readonly long position;

			internal readonly int crc;

			internal IndexPack.UnresolvedDelta next;

			internal UnresolvedDelta(long headerOffset, int crc32)
			{
				position = headerOffset;
				crc = crc32;
			}
		}

		/// <summary>Rename the pack to it's final name and location and open it.</summary>
		/// <remarks>
		/// Rename the pack to it's final name and location and open it.
		/// <p>
		/// If the call completes successfully the repository this IndexPack instance
		/// was created with will have the objects in the pack available for reading
		/// and use, without needing to scan for packs.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// The pack could not be inserted into the repository's objects
		/// directory. The pack no longer exists on disk, as it was
		/// removed prior to throwing the exception to the caller.
		/// </exception>
		public virtual void RenameAndOpenPack()
		{
			RenameAndOpenPack(null);
		}

		/// <summary>Rename the pack to it's final name and location and open it.</summary>
		/// <remarks>
		/// Rename the pack to it's final name and location and open it.
		/// <p>
		/// If the call completes successfully the repository this IndexPack instance
		/// was created with will have the objects in the pack available for reading
		/// and use, without needing to scan for packs.
		/// </remarks>
		/// <param name="lockMessage">
		/// message to place in the pack-*.keep file. If null, no lock
		/// will be created, and this method returns null.
		/// </param>
		/// <returns>the pack lock object, if lockMessage is not null.</returns>
		/// <exception cref="System.IO.IOException">
		/// The pack could not be inserted into the repository's objects
		/// directory. The pack no longer exists on disk, as it was
		/// removed prior to throwing the exception to the caller.
		/// </exception>
		public virtual PackLock RenameAndOpenPack(string lockMessage)
		{
			if (!keepEmpty && entryCount == 0)
			{
				CleanupTemporaryFiles();
				return null;
			}
			MessageDigest d = Constants.NewMessageDigest();
			byte[] oeBytes = new byte[Constants.OBJECT_ID_LENGTH];
			for (int i = 0; i < entryCount; i++)
			{
				PackedObjectInfo oe = entries[i];
				oe.CopyRawTo(oeBytes, 0);
				d.Update(oeBytes);
			}
			string name = ObjectId.FromRaw(d.Digest()).Name;
			FilePath packDir = new FilePath(repo.ObjectsDirectory, "pack");
			FilePath finalPack = new FilePath(packDir, "pack-" + name + ".pack");
			FilePath finalIdx = new FilePath(packDir, "pack-" + name + ".idx");
			PackLock keep = new PackLock(finalPack, repo.FileSystem);
			if (!packDir.Exists() && !packDir.Mkdir() && !packDir.Exists())
			{
				// The objects/pack directory isn't present, and we are unable
				// to create it. There is no way to move this pack in.
				//
				CleanupTemporaryFiles();
				throw new IOException(MessageFormat.Format(JGitText.Get().cannotCreateDirectory, 
					packDir.GetAbsolutePath()));
			}
			if (finalPack.Exists())
			{
				// If the pack is already present we should never replace it.
				//
				CleanupTemporaryFiles();
				return null;
			}
			if (lockMessage != null)
			{
				// If we have a reason to create a keep file for this pack, do
				// so, or fail fast and don't put the pack in place.
				//
				try
				{
					if (!keep.Lock(lockMessage))
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().cannotLockPackIn, finalPack
							));
					}
				}
				catch (IOException e)
				{
					CleanupTemporaryFiles();
					throw;
				}
			}
			if (!dstPack.RenameTo(finalPack))
			{
				CleanupTemporaryFiles();
				keep.Unlock();
				throw new IOException(MessageFormat.Format(JGitText.Get().cannotMovePackTo, finalPack
					));
			}
			if (!dstIdx.RenameTo(finalIdx))
			{
				CleanupTemporaryFiles();
				keep.Unlock();
				if (!finalPack.Delete())
				{
					finalPack.DeleteOnExit();
				}
				throw new IOException(MessageFormat.Format(JGitText.Get().cannotMoveIndexTo, finalIdx
					));
			}
			try
			{
				repo.OpenPack(finalPack, finalIdx);
			}
			catch (IOException err)
			{
				keep.Unlock();
				finalPack.Delete();
				finalIdx.Delete();
				throw;
			}
			return lockMessage != null ? keep : null;
		}

		private void CleanupTemporaryFiles()
		{
			if (!dstIdx.Delete())
			{
				dstIdx.DeleteOnExit();
			}
			if (!dstPack.Delete())
			{
				dstPack.DeleteOnExit();
			}
		}

		private void AddObjectAndTrack(PackedObjectInfo oe)
		{
			entries[entryCount++] = oe;
			if (NeedNewObjectIds())
			{
				newObjectIds.Add(oe);
			}
		}
	}
}
