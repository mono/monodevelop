/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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
using System.Diagnostics;
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Transport;
using GitSharp.Core.Util;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Core
{
	public class PackWriter : IDisposable
	{
		public const string COUNTING_OBJECTS_PROGRESS = "Counting objects";
		public const string SEARCHING_REUSE_PROGRESS = "Compressing objects";
		public const string WRITING_OBJECTS_PROGRESS = "Writing objects";
		public const bool DEFAULT_REUSE_DELTAS = true;
		public const bool DEFAULT_REUSE_OBJECTS = true;
		public const bool DEFAULT_DELTA_BASE_AS_OFFSET = false;
		public const int DEFAULT_MAX_DELTA_DEPTH = 50;

		private const int PackVersionGenerated = 2;
		
		private static List<ObjectToPack>[] CreateObjectsLists()
		{
			var ret = new List<ObjectToPack>[Constants.OBJ_TAG + 1];
			ret[0] = new List<ObjectToPack>();
			ret[Constants.OBJ_COMMIT] = new List<ObjectToPack>();
			ret[Constants.OBJ_TREE] = new List<ObjectToPack>();
			ret[Constants.OBJ_BLOB] = new List<ObjectToPack>();
			ret[Constants.OBJ_TAG] = new List<ObjectToPack>();
			return ret;
		}

		private readonly List<ObjectToPack>[] _objectsLists;
		private readonly ObjectIdSubclassMap<ObjectToPack> _objectsMap;
		private readonly ObjectIdSubclassMap<ObjectId> _edgeObjects;
		private readonly byte[] _buf;
		private readonly WindowCursor _windowCursor;
		private readonly Repository _db;
		private PackOutputStream _pos;
		private readonly Deflater _deflater;
		private readonly ProgressMonitor _initMonitor;
		private readonly ProgressMonitor _writeMonitor;
		private List<ObjectToPack> _sortedByName;
		private byte[] _packChecksum;
		private int _outputVersion;

		public PackWriter(Repository repo, ProgressMonitor monitor)
			: this(repo, monitor, monitor)
		{
		}

		public PackWriter(Repository repo, ProgressMonitor imonitor, ProgressMonitor wmonitor)
		{
			_objectsLists = CreateObjectsLists();
			_objectsMap = new ObjectIdSubclassMap<ObjectToPack>();
			_edgeObjects = new ObjectIdSubclassMap<ObjectId>();
			_buf = new byte[16384]; // 16 KB
			_windowCursor = new WindowCursor();

			IgnoreMissingUninteresting = true;
			MaxDeltaDepth = DEFAULT_MAX_DELTA_DEPTH;
			DeltaBaseAsOffset = DEFAULT_DELTA_BASE_AS_OFFSET;
			ReuseObjects = DEFAULT_REUSE_OBJECTS;
			ReuseDeltas = DEFAULT_REUSE_DELTAS;
			_db = repo;
			_initMonitor = imonitor;
			_writeMonitor = wmonitor;
			_deflater = new Deflater(_db.Config.getCore().getCompression());
			_outputVersion = repo.Config.getCore().getPackIndexVersion();
		}

		public bool ReuseDeltas { get; set; }
		public bool ReuseObjects { get; set; }
		public bool DeltaBaseAsOffset { get; set; }
		public int MaxDeltaDepth { get; set; }
		public bool Thin { get; set; }
		public bool IgnoreMissingUninteresting { get; set; }

		public void setIndexVersion(int version)
		{
			_outputVersion = version;
		}

		public int getObjectsNumber()
		{
			return _objectsMap.Count;
		}

		public void preparePack(IEnumerable<RevObject> objectsSource)
		{
			foreach (RevObject obj in objectsSource)
			{
				addObject(obj);
			}
		}

		public void preparePack<T>(IEnumerable<T> interestingObjects, IEnumerable<T> uninterestingObjects)
			where T : ObjectId
		{
			using (ObjectWalk walker = SetUpWalker(interestingObjects, uninterestingObjects))
			{
			FindObjectsToPack(walker);
			}
		}

		public bool willInclude(AnyObjectId id)
		{
			return _objectsMap.Get(id) != null;
		}

		public ObjectId computeName()
		{
			MessageDigest md = Constants.newMessageDigest();
			foreach (ObjectToPack otp in sortByName())
			{
				otp.copyRawTo(_buf, 0);
				md.Update(_buf, 0, Constants.OBJECT_ID_LENGTH);
			}
			return ObjectId.FromRaw(md.Digest());
		}

		public void writeIndex(Stream indexStream)
		{
			List<ObjectToPack> list = sortByName();

			PackIndexWriter iw = _outputVersion <= 0 ?
				PackIndexWriter.CreateOldestPossible(indexStream, list) :
				PackIndexWriter.CreateVersion(indexStream, _outputVersion);

			iw.Write(list, _packChecksum);
		}

		private List<ObjectToPack> sortByName()
		{
			if (_sortedByName == null)
			{
				_sortedByName = new List<ObjectToPack>(_objectsMap.Count);

				foreach (List<ObjectToPack> list in _objectsLists)
				{
					foreach (ObjectToPack otp in list)
					{
						_sortedByName.Add(otp);
					}
				}

				_sortedByName.Sort();
			}

			return _sortedByName;
		}

		public void writePack(Stream packStream)
		{
			if (ReuseDeltas || ReuseObjects)
			{
				SearchForReuse();
			}

			if (!(packStream is BufferedStream))
			{
				packStream = new BufferedStream(packStream);
			}

			_pos = new PackOutputStream(packStream);

			_writeMonitor.BeginTask(WRITING_OBJECTS_PROGRESS, getObjectsNumber());
			WriteHeader();
			WriteObjects();
			WriteChecksum();

			_pos.Flush();
			_windowCursor.Release();
			_writeMonitor.EndTask();
		}

		private void SearchForReuse()
		{
			_initMonitor.BeginTask(SEARCHING_REUSE_PROGRESS, getObjectsNumber());
			var reuseLoaders = new List<PackedObjectLoader>();
			foreach (List<ObjectToPack> list in _objectsLists)
			{
				foreach (ObjectToPack otp in list)
				{
					if (_initMonitor.IsCancelled)
					{
						throw new IOException("Packing cancelled during objects writing.");
					}
					reuseLoaders.Clear();
					SearchForReuse(reuseLoaders, otp);
					_initMonitor.Update(1);
				}
			}

			_initMonitor.EndTask();
		}

		private void SearchForReuse(ICollection<PackedObjectLoader> reuseLoaders, ObjectToPack otp)
		{
			_db.OpenObjectInAllPacks(otp, reuseLoaders, _windowCursor);

			if (ReuseDeltas)
			{
				SelectDeltaReuseForObject(otp, reuseLoaders);
			}

			if (ReuseObjects && !otp.HasReuseLoader)
			{
				SelectObjectReuseForObject(otp, reuseLoaders);
			}
		}

		private void SelectDeltaReuseForObject(ObjectToPack otp, IEnumerable<PackedObjectLoader> loaders)
		{
			PackedObjectLoader bestLoader = null;
			ObjectId bestBase = null;

			foreach (PackedObjectLoader loader in loaders)
			{
				ObjectId idBase = loader.DeltaBase;
				if (idBase == null) continue;
				ObjectToPack otpBase = _objectsMap.Get(idBase);

				if ((otpBase != null || (Thin && _edgeObjects.Get(idBase) != null)) && IsBetterDeltaReuseLoader(bestLoader, loader))
				{
					bestLoader = loader;
					bestBase = (otpBase ?? idBase);
				}
			}

			if (bestLoader == null) return;

			otp.SetReuseLoader(bestLoader);
			otp.DeltaBaseId = bestBase;
		}

		private static bool IsBetterDeltaReuseLoader(PackedObjectLoader currentLoader, PackedObjectLoader loader)
		{
			if (currentLoader == null) return true;

			if (loader.RawSize < currentLoader.RawSize) return true;

			return loader.RawSize == currentLoader.RawSize &&
				loader.SupportsFastCopyRawData &&
				!currentLoader.SupportsFastCopyRawData;
		}

		private static void SelectObjectReuseForObject(ObjectToPack otp, IEnumerable<PackedObjectLoader> loaders)
		{
			foreach (PackedObjectLoader loader in loaders)
			{
				if (!(loader is WholePackedObjectLoader)) continue;

				otp.SetReuseLoader(loader);
				return;
			}
		}

		private void WriteHeader()
		{
			Array.Copy(Constants.PACK_SIGNATURE, 0, _buf, 0, 4);
			NB.encodeInt32(_buf, 4, PackVersionGenerated);
			NB.encodeInt32(_buf, 8, getObjectsNumber());
			_pos.Write(_buf, 0, 12);
		}

		private void WriteObjects()
		{
			foreach (List<ObjectToPack> list in _objectsLists)
			{
				foreach (ObjectToPack otp in list)
				{
					if (_writeMonitor.IsCancelled)
					{
						throw new IOException("Packing cancelled during objects writing");
					}

					if (!otp.IsWritten)
					{
						WriteObject(otp);
					}
				}
			}
		}

		private void WriteObject(ObjectToPack otp)
		{
			otp.MarkWantWrite();
			if (otp.IsDeltaRepresentation)
			{
				ObjectToPack deltaBase = otp.DeltaBase;
				Debug.Assert(deltaBase != null || Thin);
				if (deltaBase != null && !deltaBase.IsWritten)
				{
					if (deltaBase.WantWrite)
					{
						otp.ClearDeltaBase();
						otp.DisposeLoader();
					}
					else
					{
						WriteObject(deltaBase);
					}
				}
			}

			Debug.Assert(!otp.IsWritten);

			_pos.resetCRC32();
			otp.Offset = _pos.Length;

			PackedObjectLoader reuse = Open(otp);
			if (reuse != null)
			{
				try
				{
					if (otp.IsDeltaRepresentation)
					{
						WriteDeltaObjectReuse(otp, reuse);
					}
					else
					{
						WriteObjectHeader(otp.Type, reuse.Size);
						reuse.CopyRawData(_pos, _buf, _windowCursor);
					}
				}
				finally
				{
					reuse.endCopyRawData();
				}
			}
			else if (otp.IsDeltaRepresentation)
			{
				throw new IOException("creating deltas is not implemented");
			}
			else
			{
				WriteWholeObjectDeflate(otp);
			}

			otp.CRC = _pos.getCRC32();
			_writeMonitor.Update(1);
		}

		private PackedObjectLoader Open(ObjectToPack otp)
		{
			while (true)
			{
				PackedObjectLoader reuse = otp.UseLoader();
				if (reuse == null) return null;

				try
				{
					reuse.beginCopyRawData();
					return reuse;
				}
				catch (IOException)
				{
					otp.ClearDeltaBase();
					SearchForReuse(new List<PackedObjectLoader>(), otp);
					continue;
				}
			}
		}

		private void WriteWholeObjectDeflate(ObjectToPack otp)
		{
			ObjectLoader loader = _db.OpenObject(_windowCursor, otp);
			byte[] data = loader.CachedBytes;
			WriteObjectHeader(otp.Type, data.Length);
			_deflater.Reset();
			_deflater.SetInput(data, 0, data.Length);
			_deflater.Finish();
			do
			{
				int n = _deflater.Deflate(_buf, 0, _buf.Length);
				if (n > 0)
				{
					_pos.Write(_buf, 0, n);
				}
			} while (!_deflater.IsFinished);
		}

		private void WriteDeltaObjectReuse(ObjectToPack otp, PackedObjectLoader reuse)
		{
			if (DeltaBaseAsOffset && otp.DeltaBase != null)
			{
				WriteObjectHeader(Constants.OBJ_OFS_DELTA, reuse.RawSize);

				ObjectToPack deltaBase = otp.DeltaBase;
				long offsetDiff = otp.Offset - deltaBase.Offset;
				int localPos = _buf.Length - 1;
				_buf[localPos] = (byte)(offsetDiff & 0x7F);
				while ((offsetDiff >>= 7) > 0)
				{
					_buf[--localPos] = (byte)(0x80 | (--offsetDiff & 0x7F));
				}

				_pos.Write(_buf, localPos, _buf.Length - localPos);
			}
			else
			{
				WriteObjectHeader(Constants.OBJ_REF_DELTA, reuse.RawSize);
				otp.DeltaBaseId.copyRawTo(_buf, 0);
				_pos.Write(_buf, 0, Constants.OBJECT_ID_LENGTH);
			}

			reuse.CopyRawData(_pos, _buf, _windowCursor);
		}

		private void WriteObjectHeader(int objectType, long dataLength)
		{
			var nextLength = (long)(((ulong)dataLength) >> 4);
			int size = 0;
			_buf[size++] = (byte)((nextLength > 0 ? (byte)0x80 : (byte)0x00) | (byte)(objectType << 4) | (byte)(dataLength & 0x0F));
			dataLength = nextLength;

			while (dataLength > 0)
			{
				nextLength = (long)(((ulong)nextLength) >> 7);
				_buf[size++] = (byte)((nextLength > 0 ? (byte)0x80 : (byte)0x00) | (byte)(dataLength & 0x7F));
				dataLength = nextLength;
			}
			_pos.Write(_buf, 0, size);
		}

		private void WriteChecksum()
		{
			_packChecksum = _pos.getDigest();
			_pos.Write(_packChecksum, 0, _packChecksum.Length);
		}

		private ObjectWalk SetUpWalker<T>(IEnumerable<T> interestingObjects, IEnumerable<T> uninterestingObjects)
			where T : ObjectId
		{
			var walker = new ObjectWalk(_db);
			walker.sort(RevSort.Strategy.TOPO);
			walker.sort(RevSort.Strategy.COMMIT_TIME_DESC, true);

			if (Thin)
			{
				walker.sort(RevSort.Strategy.BOUNDARY, true);
			}

			foreach (T id in interestingObjects)
			{
				RevObject o = walker.parseAny(id);
				walker.markStart(o);
			}

			if (uninterestingObjects != null)
			{
				foreach (T id in uninterestingObjects)
				{
					RevObject o;

					try
					{
						o = walker.parseAny(id);
					}
					catch (MissingObjectException)
					{
						if (IgnoreMissingUninteresting) continue;
						throw;
					}

					walker.markUninteresting(o);
				}
			}

			return walker;
		}

		private void FindObjectsToPack(ObjectWalk walker)
		{
			_initMonitor.BeginTask(COUNTING_OBJECTS_PROGRESS, ProgressMonitor.UNKNOWN);
			RevObject o;

			while ((o = walker.next()) != null)
			{
				addObject(o);
				_initMonitor.Update(1);
			}

			while ((o = walker.nextObject()) != null)
			{
				addObject(o);
				_initMonitor.Update(1);
			}
			_initMonitor.EndTask();
		}

		public void addObject(RevObject robject)
		{
			if (robject.has(RevFlag.UNINTERESTING))
			{
				_edgeObjects.Add(robject);
				Thin = true;
				return;
			}

			var otp = new ObjectToPack(robject, robject.Type);
			try
			{
				_objectsLists[robject.Type].Add(otp);
			}
			catch (IndexOutOfRangeException)
			{
				throw new IncorrectObjectTypeException(robject, "COMMIT nor TREE nor BLOB nor TAG");
			}
			_objectsMap.Add(otp);
		}
		
		public void Dispose ()
		{
			_pos.Dispose();
		}
		

		#region Nested Types

		class ObjectToPack : PackedObjectInfo
		{
			private PackedObjectLoader _reuseLoader;
			private int _flags;

			public ObjectToPack(AnyObjectId src, int type)
				: base(src)
			{
				_flags |= type << 1;
			}

			public ObjectId DeltaBaseId { get; set; }

			public ObjectToPack DeltaBase
			{
				get
				{
					if (DeltaBaseId is ObjectToPack) return (ObjectToPack)DeltaBaseId;
					return null;
				}
			}

			public bool IsDeltaRepresentation
			{
				get { return DeltaBaseId != null; }
			}

			public bool IsWritten
			{
				get { return Offset != 0; }
			}

			public bool HasReuseLoader
			{
				get { return _reuseLoader != null; }
			}

			public int Type
			{
				get { return (_flags >> 1) & 0x7; }
			}

			public bool WantWrite
			{
				get { return (_flags & 1) == 1; }
			}

			public void DisposeLoader()
			{
				_reuseLoader = null;
			}

			public void ClearDeltaBase()
			{
				DeltaBaseId = null;
			}

			public PackedObjectLoader UseLoader()
			{
				PackedObjectLoader r = _reuseLoader;
				_reuseLoader = null;
				return r;
			}

			public void SetReuseLoader(PackedObjectLoader reuseLoader)
			{
				_reuseLoader = reuseLoader;
			}

			public void MarkWantWrite()
			{
				_flags |= 1;
			}
		}

		#endregion
	}
}