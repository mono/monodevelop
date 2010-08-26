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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Core.Transport
{
	/// <summary>
	/// Indexes Git pack files for local use.
	/// </summary>
	public class IndexPack : IDisposable
	{
		/// <summary>
		/// Progress message when reading raw data from the pack.
		/// </summary>
		public const string PROGRESS_DOWNLOAD = "Receiving objects";

		/// <summary>
		/// Progress message when computing names of delta compressed objects.
		/// </summary>
		public const string PROGRESS_RESOLVE_DELTA = "Resolving deltas";
		public const string PackSuffix = ".pack";
		public const string IndexSuffix = ".idx";

		/// <summary>
		/// Size of the internal stream buffer.
		/// <para/>
		/// If callers are going to be supplying IndexPack a BufferedInputStream they
		/// should use this buffer size as the size of the buffer for that
		/// BufferedInputStream, and any other its may be wrapping. This way the
		/// buffers will cascade efficiently and only the IndexPack buffer will be
		/// receiving the bulk of the data stream.
		/// </summary>
		public const int BUFFER_SIZE = 8192;


		/// <summary>
		/// Create an index pack instance to load a new pack into a repository.
		/// <para/>
		/// The received pack data and generated index will be saved to temporary
		/// files within the repository's <code>objects</code> directory. To use the
		/// data contained within them call <see cref="renameAndOpenPack()"/> once the
		/// indexing is complete.
		/// </summary>
		/// <param name="db">the repository that will receive the new pack.</param>
		/// <param name="stream">
		/// stream to read the pack data from. If the stream is buffered
		/// use <see cref="BUFFER_SIZE"/> as the buffer size for the stream.
		/// </param>
		/// <returns>a new index pack instance.</returns>
		internal static IndexPack Create(Repository db, Stream stream)
		{
			DirectoryInfo objdir = db.ObjectsDirectory;
			FileInfo tmp = CreateTempFile("incoming_", PackSuffix, objdir);
			string n = tmp.Name;

			var basef = PathUtil.CombineFilePath(objdir, n.Slice(0, n.Length - PackSuffix.Length));
			var ip = new IndexPack(db, stream, basef);
			ip.setIndexVersion(db.Config.getCore().getPackIndexVersion());
			return ip;
		}

		private readonly Repository _repo;
		private readonly FileStream _packOut;
		private Stream _stream;
		private readonly byte[] _buffer;
		private readonly MessageDigest _objectDigest;
		private readonly MutableObjectId _tempObjectId;
		private readonly Crc32 _crc = new Crc32();

		/// <summary>
		/// Object database used for loading existing objects
		/// </summary>
		private readonly ObjectDatabase _objectDatabase;

		private Inflater _inflater;
		private long _bBase;
		private int _bOffset;
		private int _bAvail;
		private ObjectChecker _objCheck;
		private bool _fixThin;
		private bool _keepEmpty;
		private bool _needBaseObjectIds;
		private int _outputVersion;
		private readonly FileInfo _dstPack;
		private readonly FileInfo _dstIdx;
		private long _objectCount;
		private PackedObjectInfo[] _entries;
		private HashSet<ObjectId> _newObjectIds;
		private int _deltaCount;
		private int _entryCount;
		private ObjectIdSubclassMap<DeltaChain> _baseById;
		private HashSet<ObjectId> _baseIds;
		private LongMap<UnresolvedDelta> _baseByPos;
		private byte[] _objectData;
		private MessageDigest _packDigest;
		private byte[] _packcsum;

		/// <summary>
		/// If <see cref="_fixThin"/> this is the last byte of the original checksum.
		/// </summary>
		private long _originalEof;
		private WindowCursor _windowCursor;

		/// <summary>
		/// Create a new pack indexer utility.
		/// </summary>
		/// <param name="db"></param>
		/// <param name="src">
		/// stream to read the pack data from. If the stream is buffered
		/// use <see cref="BUFFER_SIZE"/> as the buffer size for the stream.
		/// </param>
		/// <param name="dstBase"></param>
		public IndexPack(Repository db, Stream src, FileInfo dstBase)
		{
			_repo = db;
			_objectDatabase = db.ObjectDatabase.newCachedDatabase();
			_stream = src;
			_inflater = InflaterCache.Instance.get();
			_windowCursor = new WindowCursor();
			_buffer = new byte[BUFFER_SIZE];
			_objectData = new byte[BUFFER_SIZE];
			_objectDigest = Constants.newMessageDigest();
			_tempObjectId = new MutableObjectId();
			_packDigest = Constants.newMessageDigest();

			if (dstBase != null)
			{
				DirectoryInfo dir = dstBase.Directory;
				string nam = dstBase.Name;
				_dstPack = PathUtil.CombineFilePath(dir, GetPackFileName(nam));
				_dstIdx = PathUtil.CombineFilePath(dir, GetIndexFileName(nam));
				_packOut = _dstPack.Create();
			}
			else
			{
				_dstPack = null;
				_dstIdx = null;
			}
		}

		/// <summary>
		/// Set the pack index file format version this instance will create.
		/// </summary>
		/// <param name="version">
		/// the version to write. The special version 0 designates the
		/// oldest (most compatible) format available for the objects.
		/// </param>
		public void setIndexVersion(int version)
		{
			_outputVersion = version;
		}

		/// <summary>
		/// Configure this index pack instance to make a thin pack complete.
		/// <para/>
		/// Thin packs are sometimes used during network transfers to allow a delta
		/// to be sent without a base object. Such packs are not permitted on disk.
		/// They can be fixed by copying the base object onto the end of the pack.
		/// </summary>
		/// <param name="fix">true to enable fixing a thin pack.</param>
		public void setFixThin(bool fix)
		{
			_fixThin = fix;
		}

		/// <summary>
		/// Configure this index pack instance to keep an empty pack.
		/// <para/>
		/// By default an empty pack (a pack with no objects) is not kept, as doing
		/// so is completely pointless. With no objects in the pack there is no data
		/// stored by it, so the pack is unnecessary.
		/// </summary>
		/// <param name="empty">true to enable keeping an empty pack.</param>
		public void setKeepEmpty(bool empty)
		{
			_keepEmpty = empty;
		}

		/// <summary>
		/// Configure this index pack instance to keep track of new objects.
		/// <para/>
		/// By default an index pack doesn't save the new objects that were created
		/// when it was instantiated. Setting this flag to {@code true} allows the
		/// caller to use {@link #getNewObjectIds()} to retrieve that list.
		/// </summary>
		/// <param name="b"> True to enable keeping track of new objects.</param>
		public void setNeedNewObjectIds(bool b)
		{
			if (b)
				_newObjectIds = new HashSet<ObjectId>();
			else
				_newObjectIds = null;
		}

		private bool needNewObjectIds()
		{
			return _newObjectIds != null;
		}

		/// <summary>
		/// Configure this index pack instance to keep track of the objects assumed
		/// for delta bases.
		/// <para/>
		/// By default an index pack doesn't save the objects that were used as delta
		/// bases. Setting this flag to {@code true} will allow the caller to
		/// use <see>getBaseObjectIds()</see> to retrieve that list.
		/// </summary>
		///<param name="b"> True to enable keeping track of delta bases.</param>
		public void setNeedBaseObjectIds(bool b)
		{
			this._needBaseObjectIds = b;
		}

		/// <returns> the new objects that were sent by the user</returns>	  
		public HashSet<ObjectId> getNewObjectIds()
		{
			return _newObjectIds ?? new HashSet<ObjectId>();
		}

		/// <returns>the set of objects the incoming pack assumed for delta purposes</returns>	  
		public HashSet<ObjectId> getBaseObjectIds()
		{
			return _baseIds ?? new HashSet<ObjectId>();
		}

		/// <summary>
		/// Configure the checker used to validate received objects.
		/// <para/>
		/// Usually object checking isn't necessary, as Git implementations only
		/// create valid objects in pack files. However, additional checking may be
		/// useful if processing data from an untrusted source.
		/// </summary>
		/// <param name="oc">the checker instance; null to disable object checking.</param>
		public void setObjectChecker(ObjectChecker oc)
		{
			_objCheck = oc;
		}

		/// <summary>
		/// Configure the checker used to validate received objects.
		/// <para/>
		/// Usually object checking isn't necessary, as Git implementations only
		/// create valid objects in pack files. However, additional checking may be
		/// useful if processing data from an untrusted source.
		/// <para/>
		/// This is shorthand for:
		/// 
		/// <pre>
		/// setObjectChecker(on ? new ObjectChecker() : null);
		/// </pre>
		/// </summary>
		/// <param name="on">true to enable the default checker; false to disable it.</param>
		public void setObjectChecking(bool on)
		{
			setObjectChecker(on ? new ObjectChecker() : null);
		}

		/// <summary>
		/// Consume data from the input stream until the packfile is indexed.
		/// </summary>
		/// <param name="progress">progress feedback</param>
		public void index(ProgressMonitor progress)
		{
			progress.Start(2 /* tasks */);
			try
			{
				try
				{
					ReadPackHeader();

					_entries = new PackedObjectInfo[(int)_objectCount];
					_baseById = new ObjectIdSubclassMap<DeltaChain>();
					_baseByPos = new LongMap<UnresolvedDelta>();

					progress.BeginTask(PROGRESS_DOWNLOAD, (int)_objectCount);
					for (int done = 0; done < _objectCount; done++)
					{
						IndexOneObject();
						progress.Update(1);
						if (progress.IsCancelled)
						{
							throw new IOException("Download cancelled");
						}
					}

					ReadPackFooter();
					EndInput();
					progress.EndTask();

					if (_deltaCount > 0)
					{
						if (_packOut == null)
							throw new IOException("need packOut");
						ResolveDeltas(progress);
						if (_needBaseObjectIds)
						{
							_baseIds = new HashSet<ObjectId>();
							foreach (var c in _baseById)
								_baseIds.Add(c);
						}
						if (_entryCount < _objectCount)
						{
							if (!_fixThin)
							{
								throw new IOException("pack has " + (_objectCount - _entryCount) + " unresolved deltas");
							}

							FixThinPack(progress);
						}
					}

					if (_packOut != null && (_keepEmpty || _entryCount > 0))
					{
						_packOut.Flush();
					}

					_packDigest = null;
					_baseById = null;
					_baseByPos = null;

					if (_dstIdx != null && (_keepEmpty || _entryCount > 0))
					{
						WriteIdx();
					}
				}
				finally
				{
					try
					{
						InflaterCache.Instance.release(_inflater);
					}
					finally
					{
						_inflater = null;
						_objectDatabase.close();
					}
					_windowCursor = WindowCursor.Release(_windowCursor);

					progress.EndTask();
					if (_packOut != null)
					{
						_packOut.Dispose();
					}
				}

				if (_keepEmpty || _entryCount > 0)
				{
					if (_dstPack != null)
					{
						_dstPack.IsReadOnly = true;
					}
					if (_dstIdx != null)
					{
						_dstIdx.IsReadOnly = true;
					}
				}
			}
			catch (IOException)
			{
				if (_dstPack != null) _dstPack.DeleteFile();
				if (_dstIdx != null) _dstIdx.DeleteFile();
				throw;
			}
		}

		private void ResolveDeltas(ProgressMonitor progress)
		{
			progress.BeginTask(PROGRESS_RESOLVE_DELTA, _deltaCount);
			int last = _entryCount;
			for (int i = 0; i < last; i++)
			{
				int before = _entryCount;
				ResolveDeltas(_entries[i]);
				progress.Update(_entryCount - before);
				if (progress.IsCancelled)
				{
					throw new IOException("Download cancelled during indexing");
				}
			}
			progress.EndTask();
		}

		private void ResolveDeltas(PackedObjectInfo objectInfo)
		{
			if (_baseById.Get(objectInfo) != null || _baseByPos.containsKey(objectInfo.Offset))
			{
				int oldCrc = objectInfo.CRC;
				ResolveDeltas(objectInfo.Offset, oldCrc, Constants.OBJ_BAD, null, objectInfo);
			}
		}

		private void ResolveDeltas(long pos, int oldCrc, int type, byte[] data, PackedObjectInfo oe)
		{
			_crc.Reset();
			Position(pos);
			int c = ReadFromFile();
			int typecode = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & 0x80) != 0)
			{
				c = ReadFromFile();
				sz += (c & 0x7f) << shift;
				shift += 7;
			}

			switch (typecode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
					type = typecode;
					data = InflateFromFile((int)sz);
					break;

				case Constants.OBJ_OFS_DELTA:
					c = ReadFromFile() & 0xff;
					while ((c & 128) != 0)
					{
						c = ReadFromFile() & 0xff;
					}
					data = BinaryDelta.Apply(data, InflateFromFile((int)sz));
					break;

				case Constants.OBJ_REF_DELTA:
					_crc.Update(_buffer, FillFromFile(20), 20);
					Use(20);
					data = BinaryDelta.Apply(data, InflateFromFile((int)sz));
					break;

				default:
					throw new IOException("Unknown object type " + typecode + ".");
			}

			var crc32 = (int)_crc.Value;
			if (oldCrc != crc32)
			{
				throw new IOException("Corruption detected re-reading at " + pos);
			}

			if (oe == null)
			{
				_objectDigest.Update(Constants.encodedTypeString(type));
				_objectDigest.Update((byte)' ');
				_objectDigest.Update(Constants.encodeASCII(data.Length));
				_objectDigest.Update(0);
				_objectDigest.Update(data);
				_tempObjectId.FromRaw(_objectDigest.Digest(), 0);

				VerifySafeObject(_tempObjectId, type, data);
				oe = new PackedObjectInfo(pos, crc32, _tempObjectId);
				addObjectAndTrack(oe);
			}

			ResolveChildDeltas(pos, type, data, oe);
		}

		private UnresolvedDelta RemoveBaseById(AnyObjectId id)
		{
			DeltaChain d = _baseById.Get(id);
			return d != null ? d.Remove() : null;
		}

		private static UnresolvedDelta Reverse(UnresolvedDelta c)
		{
			UnresolvedDelta tail = null;
			while (c != null)
			{
				UnresolvedDelta n = c.Next;
				c.Next = tail;
				tail = c;
				c = n;
			}
			return tail;
		}

		private void ResolveChildDeltas(long pos, int type, byte[] data, AnyObjectId objectId)
		{
			UnresolvedDelta a = Reverse(RemoveBaseById(objectId));
			UnresolvedDelta b = Reverse(_baseByPos.remove(pos));

			while (a != null && b != null)
			{
				if (a.Position < b.Position)
				{
					ResolveDeltas(a.Position, a.Crc, type, data, null);
					a = a.Next;
				}
				else
				{
					ResolveDeltas(b.Position, b.Crc, type, data, null);
					b = b.Next;
				}
			}

			ResolveChildDeltaChain(type, data, a);
			ResolveChildDeltaChain(type, data, b);
		}

		private void ResolveChildDeltaChain(int type, byte[] data, UnresolvedDelta a)
		{
			while (a != null)
			{
				ResolveDeltas(a.Position, a.Crc, type, data, null);
				a = a.Next;
			}
		}

		private void FixThinPack(ProgressMonitor progress)
		{
			GrowEntries();

			_packDigest.Reset();
			_originalEof = _packOut.Length - 20;
			var def = new Deflater(Deflater.DEFAULT_COMPRESSION, false);
			var missing = new List<DeltaChain>(64);
			long end = _originalEof;

			foreach (DeltaChain baseId in _baseById)
			{
				if (baseId.Head == null)
				{
					continue;
				}

				ObjectLoader ldr = _repo.OpenObject(_windowCursor, baseId);
				if (ldr == null)
				{
					missing.Add(baseId);
					continue;
				}

				byte[] data = ldr.CachedBytes;
				int typeCode = ldr.Type;

				_crc.Reset();
				_packOut.Seek(end, SeekOrigin.Begin);
				WriteWhole(def, typeCode, data);
				var oe = new PackedObjectInfo(end, (int)_crc.Value, baseId);
				_entries[_entryCount++] = oe;
				end = _packOut.Position;

				ResolveChildDeltas(oe.Offset, typeCode, data, oe);
				if (progress.IsCancelled)
				{
					throw new IOException("Download cancelled during indexing");
				}
			}

			def.Finish();

			foreach (DeltaChain baseDeltaChain in missing)
			{
				if (baseDeltaChain.Head != null)
				{
					throw new MissingObjectException(baseDeltaChain, "delta base");
				}
			}

			FixHeaderFooter(_packcsum, _packDigest.Digest());
		}

		private void WriteWhole(Deflater def, int typeCode, byte[] data)
		{
			int sz = data.Length;
			int hdrlen = 0;
			_buffer[hdrlen++] = (byte)((typeCode << 4) | sz & 15);
			sz = (int)(((uint)sz) >> 4);

			while (sz > 0)
			{
				_buffer[hdrlen - 1] |= 0x80;
				_buffer[hdrlen++] = (byte)(sz & 0x7f);
				sz = (int)(((uint)sz) >> 7);
			}

			_packDigest.Update(_buffer, 0, hdrlen);
			_crc.Update(_buffer, 0, hdrlen);
			_packOut.Write(_buffer, 0, hdrlen);
			def.Reset();
			def.SetInput(data);
			def.Finish();

			while (!def.IsFinished)
			{
				int datlen = def.Deflate(_buffer);
				_packDigest.Update(_buffer, 0, datlen);
				_crc.Update(_buffer, 0, datlen);
				_packOut.Write(_buffer, 0, datlen);
			}
		}

		private void FixHeaderFooter(IEnumerable<byte> origcsum, IEnumerable<byte> tailcsum)
		{
			MessageDigest origDigest = Constants.newMessageDigest();
			MessageDigest tailDigest = Constants.newMessageDigest();
			long origRemaining = _originalEof;

			_packOut.Seek(0, SeekOrigin.Begin);
			_bAvail = 0;
			_bOffset = 0;
			FillFromFile(12);

			{
				var origCnt = (int)Math.Min(_bAvail, origRemaining);
				origDigest.Update(_buffer, 0, origCnt);
				origRemaining -= origCnt;
				if (origRemaining == 0)
				{
					tailDigest.Update(_buffer, origCnt, _bAvail - origCnt);
				}
			}

			NB.encodeInt32(_buffer, 8, _entryCount);
			_packOut.Seek(0, SeekOrigin.Begin);
			_packOut.Write(_buffer, 0, 12);
			_packOut.Seek(_bAvail, SeekOrigin.Begin);

			_packDigest.Reset();
			_packDigest.Update(_buffer, 0, _bAvail);

			while (true)
			{
				int n = _packOut.Read(_buffer, 0, _buffer.Length);
				if (n <= 0) break;

				if (origRemaining != 0)
				{
					var origCnt = (int)Math.Min(n, origRemaining);
					origDigest.Update(_buffer, 0, origCnt);
					origRemaining -= origCnt;
					if (origRemaining == 0)
					{
						tailDigest.Update(_buffer, origCnt, n - origCnt);
					}
				}
				else
				{
					tailDigest.Update(_buffer, 0, n);
				}

				_packDigest.Update(_buffer, 0, n);
			}

			if (!origDigest.Digest().SequenceEqual(origcsum) || !tailDigest.Digest().SequenceEqual(tailcsum))
			{
				throw new IOException("Pack corrupted while writing to filesystem");
			}

			_packcsum = _packDigest.Digest();
			_packOut.Write(_packcsum, 0, _packcsum.Length);
		}

		private void GrowEntries()
		{
			var newEntries = new PackedObjectInfo[(int)_objectCount + _baseById.Count];
			Array.Copy(_entries, 0, newEntries, 0, _entryCount);
			_entries = newEntries;
		}

		private void WriteIdx()
		{
			Array.Sort(_entries, 0, _entryCount);
			var list = new List<PackedObjectInfo>(_entries);
			if (_entryCount < _entries.Length)
			{
				list.RemoveRange(_entryCount, _entries.Length - _entryCount);
			}

			using (FileStream os = _dstIdx.Create())
			{
				PackIndexWriter iw = _outputVersion <= 0 ? PackIndexWriter.CreateOldestPossible(os, list) : PackIndexWriter.CreateVersion(os, _outputVersion);

				iw.Write(list, _packcsum);
				os.Flush();
			}
		}

		private void ReadPackHeader()
		{
			int hdrln = Constants.PACK_SIGNATURE.Length + 4 + 4;
			int p = FillFromInput(hdrln);
			for (int k = 0; k < Constants.PACK_SIGNATURE.Length; k++)
			{
				if (_buffer[p + k] != Constants.PACK_SIGNATURE[k])
				{
					throw new IOException("Not a PACK file.");
				}
			}

			long vers = NB.DecodeUInt32(_buffer, p + 4);
			if (vers != 2 && vers != 3)
			{
				throw new IOException("Unsupported pack version " + vers + ".");
			}

			_objectCount = NB.decodeUInt32(_buffer, p + 8);
			Use(hdrln);
		}

		private void ReadPackFooter()
		{
			Sync();
			byte[] cmpcsum = _packDigest.Digest();
			int c = FillFromInput(20);
			_packcsum = new byte[20];
			Array.Copy(_buffer, c, _packcsum, 0, 20);

			Use(20);

			if (_packOut != null)
			{
				_packOut.Write(_packcsum, 0, _packcsum.Length);
			}

			if (!cmpcsum.SequenceEqual(_packcsum))
			{
				throw new CorruptObjectException("Packfile checksum incorrect.");
			}
		}

		/// <summary>
		/// Cleanup all resources associated with our input parsing.
		/// </summary>
		private void EndInput()
		{
			_stream = null;
			_objectData = null;
		}

		/// <summary>
		/// Read one entire object or delta from the input.
		/// </summary>
		private void IndexOneObject()
		{
			long pos = Position();
			_crc.Reset();
			int c = ReadFromInput();
			int typeCode = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & 0x80) != 0)
			{
				c = ReadFromInput();
				sz += (c & 0x7f) << shift;
				shift += 7;
			}

			switch (typeCode)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
					Whole(typeCode, pos, sz);
					break;

				case Constants.OBJ_OFS_DELTA:
					c = ReadFromInput();
					long ofs = c & 127;
					while ((c & 128) != 0)
					{
						ofs += 1;
						c = ReadFromInput();
						ofs <<= 7;
						ofs += (c & 127);
					}
					long pbase = pos - ofs;
					SkipInflateFromInput(sz);
					var n = new UnresolvedDelta(pos, (int)_crc.Value);
					n.Next = _baseByPos.put(pbase, n);
					_deltaCount++;
					break;

				case Constants.OBJ_REF_DELTA:
					c = FillFromInput(20);
					_crc.Update(_buffer, c, 20);
					ObjectId baseId = ObjectId.FromRaw(_buffer, c);
					Use(20);
					DeltaChain r = _baseById.Get(baseId);
					if (r == null)
					{
						r = new DeltaChain(baseId);
						_baseById.Add(r);
					}
					SkipInflateFromInput(sz);
					r.Add(new UnresolvedDelta(pos, (int)_crc.Value));
					_deltaCount++;
					break;

				default:
					throw new IOException("Unknown object type " + typeCode + ".");
			}
		}

		private void Whole(int type, long pos, long sz)
		{
			byte[] data = InflateFromInput((int)sz);
			_objectDigest.Update(Constants.encodedTypeString(type));
			_objectDigest.Update((byte)' ');
			_objectDigest.Update(Constants.encodeASCII(sz));
			_objectDigest.Update(0);
			_objectDigest.Update(data);
			_tempObjectId.FromRaw(_objectDigest.Digest(), 0);

			VerifySafeObject(_tempObjectId, type, data);
			var crc32 = (int)_crc.Value;
			addObjectAndTrack(new PackedObjectInfo(pos, crc32, _tempObjectId));
		}

		private void VerifySafeObject(AnyObjectId id, int type, byte[] data)
		{
			if (_objCheck != null)
			{
				try
				{
					_objCheck.check(type, data);
				}
				catch (CorruptObjectException e)
				{
					throw new IOException("Invalid " + Constants.typeString(type) + " " + id.Name + ": " + e.Message, e);
				}
			}

			ObjectLoader ldr = _objectDatabase.openObject(_windowCursor, id);
			if (ldr != null)
			{
				byte[] existingData = ldr.CachedBytes;
				if (ldr.Type != type || !data.SequenceEqual(existingData))
				{
					throw new IOException("Collision on " + id.Name);
				}
			}
		}

		/// <returns>Current position of <see cref="_bOffset"/> within the entire file.</returns>
		private long Position()
		{
			return _bBase + _bOffset;
		}

		private void Position(long pos)
		{
			_packOut.Seek(pos, SeekOrigin.Begin);
			_bBase = pos;
			_bOffset = 0;
			_bAvail = 0;
		}

		/// <summary>
		/// Consume exactly one byte from the buffer and return it.
		/// </summary>
		private int ReadFromInput()
		{
			if (_bAvail == 0)
			{
				FillFromInput(1);
			}

			_bAvail--;
			int b = _buffer[_bOffset++] & 0xff;
			_crc.Update((uint)b);
			return b;
		}

		/// <summary>
		/// Consume exactly one byte from the buffer and return it.
		/// </summary>
		private int ReadFromFile()
		{
			if (_bAvail == 0)
			{
				FillFromFile(1);
			}

			_bAvail--;
			int b = _buffer[_bOffset++] & 0xff;
			_crc.Update((uint)b);
			return b;
		}

		/// <summary>
		/// Consume cnt byte from the buffer.
		/// </summary>
		private void Use(int cnt)
		{
			_bOffset += cnt;
			_bAvail -= cnt;
		}

		/// <summary>
		/// Ensure at least need bytes are available in in <see cref="_buffer"/>.
		/// </summary>
		private int FillFromInput(int need)
		{
			while (_bAvail < need)
			{
				int next = _bOffset + _bAvail;
				int free = _buffer.Length - next;
				if (free + _bAvail < need)
				{
					Sync();
					next = _bAvail;
					free = _buffer.Length - next;
				}

				next = _stream.Read(_buffer, next, free);
				if (next <= 0)
				{
					throw new EndOfStreamException("Packfile is truncated,");
				}

				_bAvail += next;
			}
			return _bOffset;
		}

		/// <summary>
		/// Ensure at least need bytes are available in in <see cref="_buffer"/>.
		/// </summary>
		private int FillFromFile(int need)
		{
			if (_bAvail < need)
			{
				int next = _bOffset + _bAvail;
				int free = _buffer.Length - next;
				if (free + _bAvail < need)
				{
					if (_bAvail > 0)
					{
						Array.Copy(_buffer, _bOffset, _buffer, 0, _bAvail);
					}

					_bOffset = 0;
					next = _bAvail;
					free = _buffer.Length - next;
				}

				next = _packOut.Read(_buffer, next, free);
				if (next <= 0)
				{
					throw new EndOfStreamException("Packfile is truncated.");
				}

				_bAvail += next;
			}

			return _bOffset;
		}

		/// <summary>
		/// Store consumed bytes in <see cref="_buffer"/> up to <see cref="_bOffset"/>.
		/// </summary>
		private void Sync()
		{
			_packDigest.Update(_buffer, 0, _bOffset);
			if (_packOut != null)
			{
				_packOut.Write(_buffer, 0, _bOffset);
			}

			if (_bAvail > 0)
			{
				Array.Copy(_buffer, _bOffset, _buffer, 0, _bAvail);
			}

			_bBase += _bOffset;
			_bOffset = 0;
		}

		private void SkipInflateFromInput(long sz)
		{
			Inflater inf = _inflater;
			try
			{
				byte[] dst = _objectData;
				int n = 0;
				int p = -1;
				while (!inf.IsFinished)
				{
					if (inf.IsNeedingInput)
					{
						if (p >= 0)
						{
							_crc.Update(_buffer, p, _bAvail);
							Use(_bAvail);
						}
						p = FillFromInput(1);
						inf.SetInput(_buffer, p, _bAvail);
					}

					int free = dst.Length - n;
					if (free < 8)
					{
						sz -= n;
						n = 0;
						free = dst.Length;
					}
					n += inf.Inflate(dst, n, free);
				}

				if (n != sz)
				{
					throw new IOException("wrong decompressed length");
				}

				n = _bAvail - inf.RemainingInput;
				if (n > 0)
				{
					_crc.Update(_buffer, p, n);
					Use(n);
				}
			}
			catch (IOException e)
			{
				throw Corrupt(e);
			}
			finally
			{
				inf.Reset();
			}
		}

		private byte[] InflateFromInput(int size)
		{
			var dst = new byte[size];
			Inflater inf = _inflater;
			try
			{
				int n = 0;
				int p = -1;
				while (!inf.IsFinished)
				{
					if (inf.IsNeedingInput)
					{
						if (p >= 0)
						{
							_crc.Update(_buffer, p, _bAvail);
							Use(_bAvail);
						}
						p = FillFromInput(1);
						inf.SetInput(_buffer, p, _bAvail);
					}

					n += inf.Inflate(dst, n, dst.Length - n);
				}
				if (n != size)
					throw new IOException("Wrong decrompressed length");
				n = _bAvail - inf.RemainingInput;
				if (n > 0)
				{
					_crc.Update(_buffer, p, n);
					Use(n);
				}
				return dst;
			}
			catch (IOException e)
			{
				throw Corrupt(e);
			}
			finally
			{
				inf.Reset();
			}
		}

		private byte[] InflateFromFile(int size)
		{
			var dst = new byte[(int)size];
			Inflater inf = _inflater;
			try
			{
				int n = 0;
				int p = -1;
				while (!inf.IsFinished)
				{
					if (inf.IsNeedingInput)
					{
						if (p >= 0)
						{
							_crc.Update(_buffer, p, _bAvail);
							Use(_bAvail);
						}
						p = FillFromFile(1);
						inf.SetInput(_buffer, p, _bAvail);
					}

					n += inf.Inflate(dst, n, size - n);
				}

				n = _bAvail - inf.RemainingInput;
				if (n > 0)
				{
					_crc.Update(_buffer, p, n);
					Use(n);
				}
				return dst;
			}
			catch (IOException e)
			{
				throw Corrupt(e);
			}
			finally
			{
				inf.Reset();
			}
		}

		private static CorruptObjectException Corrupt(IOException e)
		{
			return new CorruptObjectException("Packfile corruption detected: " + e.Message);
		}

		/// <summary>
		/// Rename the pack to it's final name and location and open it.
		/// <para/>
		/// If the call completes successfully the repository this IndexPack instance
		/// was created with will have the objects in the pack available for reading
		/// and use, without needing to scan for packs.
		/// </summary>
		public void renameAndOpenPack()
		{
			renameAndOpenPack(null);
		}

		/// <summary>
		/// Rename the pack to it's final name and location and open it.
		/// <para/>
		/// If the call completes successfully the repository this IndexPack instance
		/// was created with will have the objects in the pack available for reading
		/// and use, without needing to scan for packs.
		/// </summary>
		/// <param name="lockMessage">
		/// message to place in the pack-*.keep file. If null, no lock
		/// will be created, and this method returns null.
		/// </param>
		/// <returns>the pack lock object, if lockMessage is not null.</returns>
		public PackLock renameAndOpenPack(string lockMessage)
		{
			if (!_keepEmpty && _entryCount == 0)
			{
				CleanupTemporaryFiles();
				return null;
			}

			MessageDigest d = Constants.newMessageDigest();
			var oeBytes = new byte[Constants.OBJECT_ID_LENGTH];
			for (int i = 0; i < _entryCount; i++)
			{
				PackedObjectInfo oe = _entries[i];
				oe.copyRawTo(oeBytes, 0);
				d.Update(oeBytes);
			}

			string name = ObjectId.FromRaw(d.Digest()).Name;
			var packDir = PathUtil.CombineDirectoryPath(_repo.ObjectsDirectory, "pack");
			var finalPack = PathUtil.CombineFilePath(packDir, "pack-" + GetPackFileName(name));
			var finalIdx = PathUtil.CombineFilePath(packDir, "pack-" + GetIndexFileName(name));
			var keep = new PackLock(finalPack);

			if (!packDir.Exists && !packDir.Mkdirs() && !packDir.Exists)
			{
				// The objects/pack directory isn't present, and we are unable
				// to create it. There is no way to move this pack in.
				//
				CleanupTemporaryFiles();
				throw new IOException("Cannot Create " + packDir);
			}

			if (finalPack.Exists)
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
						throw new IOException("Cannot lock pack in " + finalPack);
					}
				}
				catch (IOException)
				{
					CleanupTemporaryFiles();
					throw;
				}
			}

			if (!_dstPack.RenameTo(finalPack.ToString()))
			{
				CleanupTemporaryFiles();
				keep.Unlock();
				throw new IOException("Cannot move pack to " + finalPack);
			}

			if (!_dstIdx.RenameTo(finalIdx.ToString()))
			{
				CleanupTemporaryFiles();
				keep.Unlock();
				finalPack.DeleteFile();
				//if (finalPack.Exists)
				// TODO: [caytchen]  finalPack.deleteOnExit();
				throw new IOException("Cannot move index to " + finalIdx);
			}

			try
			{
				_repo.OpenPack(finalPack, finalIdx);
			}
			catch (IOException)
			{
				keep.Unlock();
				finalPack.DeleteFile();
				finalIdx.DeleteFile();
				throw;
			}

			return lockMessage != null ? keep : null;
		}

		private void CleanupTemporaryFiles()
		{
			_dstIdx.DeleteFile();
			//if (_dstIdx.Exists)
			//  TODO: [caytchen] _dstIdx.deleteOnExit();
			_dstPack.DeleteFile();
			//if (_dstPack.Exists)
			//  TODO: [caytchen] _dstPack.deleteOnExit();
		}

		private void addObjectAndTrack(PackedObjectInfo oe)
		{
			_entries[_entryCount++] = oe;
			if (needNewObjectIds())
				_newObjectIds.Add(oe);
		}

		private static FileInfo CreateTempFile(string pre, string suf, DirectoryInfo dir)
		{
			string p = Path.Combine(dir.FullName, pre + Path.GetRandomFileName() + suf);

			using (var f = File.Create(p))
			{ }
			return new FileInfo(p);
		}


		internal static string GetPackFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullException("fileName");
			}
			return fileName + PackSuffix;
		}

		internal static string GetIndexFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				throw new ArgumentNullException("fileName");
			}
			return fileName + IndexSuffix;
		}

		public void Dispose()
		{
			_packOut.Dispose();
			_stream.Dispose();
			_objectDigest.Dispose();
			_packDigest.Dispose();
		}


		#region Nested Types

		private class DeltaChain : ObjectId
		{
			public UnresolvedDelta Head { get; private set; }

			public DeltaChain(AnyObjectId id)
				: base(id)
			{
			}

			public UnresolvedDelta Remove()
			{
				UnresolvedDelta r = Head;
				if (r != null)
				{
					Head = null;
				}

				return r;
			}

			public void Add(UnresolvedDelta d)
			{
				d.Next = Head;
				Head = d;
			}
		}

		private class UnresolvedDelta
		{
			private readonly long _position;
			private readonly int _crc;

			public UnresolvedDelta(long headerOffset, int crc32)
			{
				_position = headerOffset;
				_crc = crc32;
			}

			public long Position
			{
				get { return _position; }
			}

			public int Crc
			{
				get { return _crc; }
			}

			public UnresolvedDelta Next { get; set; }
		}

		#endregion
	}
}