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
using ICSharpCode.SharpZipLib;
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
	/// <summary>
	/// Parses a pack stream and imports it for an
	/// <see cref="NGit.ObjectInserter">NGit.ObjectInserter</see>
	/// .
	/// <p>
	/// Applications can acquire an instance of a parser from ObjectInserter's
	/// <see cref="NGit.ObjectInserter.NewPackParser(Sharpen.InputStream)">NGit.ObjectInserter.NewPackParser(Sharpen.InputStream)
	/// 	</see>
	/// method.
	/// <p>
	/// Implementations of
	/// <see cref="NGit.ObjectInserter">NGit.ObjectInserter</see>
	/// should subclass this type and
	/// provide their own logic for the various
	/// <code>on*()</code>
	/// event methods declared
	/// to be abstract.
	/// </summary>
	public abstract class PackParser
	{
		/// <summary>Size of the internal stream buffer.</summary>
		/// <remarks>Size of the internal stream buffer.</remarks>
		private const int BUFFER_SIZE = 8192;

		/// <summary>Location data is being obtained from.</summary>
		/// <remarks>Location data is being obtained from.</remarks>
		public enum Source
		{
			INPUT,
			DATABASE
		}

		/// <summary>Object database used for loading existing objects.</summary>
		/// <remarks>Object database used for loading existing objects.</remarks>
		private readonly ObjectDatabase objectDatabase;

		private PackParser.InflaterStream inflater;

		private byte[] tempBuffer;

		private byte[] hdrBuf;

		private readonly MessageDigest objectDigest;

		private readonly MutableObjectId tempObjectId;

		private InputStream @in;

		private byte[] buf;

		/// <summary>
		/// Position in the input stream of
		/// <code>buf[0]</code>
		/// .
		/// </summary>
		private long bBase;

		private int bOffset;

		private int bAvail;

		private ObjectChecker objCheck;

		private bool allowThin;

		private bool checkObjectCollisions;

		private bool needBaseObjectIds;

		private bool checkEofAfterPackFooter;

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

		private ObjectIdOwnerMap<PackParser.DeltaChain> baseById;

		/// <summary>Objects referenced by their name from deltas, that aren't in this pack.</summary>
		/// <remarks>
		/// Objects referenced by their name from deltas, that aren't in this pack.
		/// <p>
		/// This is the set of objects that were copied onto the end of this pack to
		/// make it complete. These objects were not transmitted by the remote peer,
		/// but instead were assumed to already exist in the local repository.
		/// </remarks>
		private ObjectIdSubclassMap<ObjectId> baseObjectIds;

		private LongMap<PackParser.UnresolvedDelta> baseByPos;

		/// <summary>Blobs whose contents need to be double-checked after indexing.</summary>
		/// <remarks>Blobs whose contents need to be double-checked after indexing.</remarks>
		private BlockList<PackedObjectInfo> deferredCheckBlobs;

		private MessageDigest packDigest;

		private ObjectReader readCurs;

		/// <summary>Message to protect the pack data from garbage collection.</summary>
		/// <remarks>Message to protect the pack data from garbage collection.</remarks>
		private string lockMessage;

		/// <summary>Initialize a pack parser.</summary>
		/// <remarks>Initialize a pack parser.</remarks>
		/// <param name="odb">database the parser will write its objects into.</param>
		/// <param name="src">the stream the parser will read.</param>
		protected internal PackParser(ObjectDatabase odb, InputStream src)
		{
			objectDatabase = odb.NewCachedDatabase();
			@in = src;
			inflater = new PackParser.InflaterStream(this);
			readCurs = objectDatabase.NewReader();
			buf = new byte[BUFFER_SIZE];
			tempBuffer = new byte[BUFFER_SIZE];
			hdrBuf = new byte[64];
			objectDigest = Constants.NewMessageDigest();
			tempObjectId = new MutableObjectId();
			packDigest = Constants.NewMessageDigest();
			checkObjectCollisions = true;
		}

		/// <returns>true if a thin pack (missing base objects) is permitted.</returns>
		public virtual bool IsAllowThin()
		{
			return allowThin;
		}

		/// <summary>Configure this index pack instance to allow a thin pack.</summary>
		/// <remarks>
		/// Configure this index pack instance to allow a thin pack.
		/// <p>
		/// Thin packs are sometimes used during network transfers to allow a delta
		/// to be sent without a base object. Such packs are not permitted on disk.
		/// </remarks>
		/// <param name="allow">true to enable a thin pack.</param>
		public virtual void SetAllowThin(bool allow)
		{
			allowThin = allow;
		}

		/// <returns>if true received objects are verified to prevent collisions.</returns>
		public virtual bool IsCheckObjectCollisions()
		{
			return checkObjectCollisions;
		}

		/// <summary>Enable checking for collisions with existing objects.</summary>
		/// <remarks>
		/// Enable checking for collisions with existing objects.
		/// <p>
		/// By default PackParser looks for each received object in the repository.
		/// If the object already exists, the existing object is compared
		/// byte-for-byte with the newly received copy to ensure they are identical.
		/// The receive is aborted with an exception if any byte differs. This check
		/// is necessary to prevent an evil attacker from supplying a replacement
		/// object into this repository in the event that a discovery enabling SHA-1
		/// collisions is made.
		/// <p>
		/// This check may be very costly to perform, and some repositories may have
		/// other ways to segregate newly received object data. The check is enabled
		/// by default, but can be explicitly disabled if the implementation can
		/// provide the same guarantee, or is willing to accept the risks associated
		/// with bypassing the check.
		/// </remarks>
		/// <param name="check">true to enable collision checking (strongly encouraged).</param>
		public virtual void SetCheckObjectCollisions(bool check)
		{
			checkObjectCollisions = check;
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
		/// will allow the caller to use
		/// <see cref="GetBaseObjectIds()">GetBaseObjectIds()</see>
		/// to retrieve that list.
		/// </remarks>
		/// <param name="b">
		/// <code>true</code>
		/// to enable keeping track of delta bases.
		/// </param>
		public virtual void SetNeedBaseObjectIds(bool b)
		{
			this.needBaseObjectIds = b;
		}

		/// <returns>true if the EOF should be read from the input after the footer.</returns>
		public virtual bool IsCheckEofAfterPackFooter()
		{
			return checkEofAfterPackFooter;
		}

		/// <summary>Ensure EOF is read from the input stream after the footer.</summary>
		/// <remarks>Ensure EOF is read from the input stream after the footer.</remarks>
		/// <param name="b">true if the EOF should be read; false if it is not checked.</param>
		public virtual void SetCheckEofAfterPackFooter(bool b)
		{
			checkEofAfterPackFooter = b;
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

		/// <returns>the message to record with the pack lock.</returns>
		public virtual string GetLockMessage()
		{
			return lockMessage;
		}

		/// <summary>Set the lock message for the incoming pack data.</summary>
		/// <remarks>Set the lock message for the incoming pack data.</remarks>
		/// <param name="msg">
		/// if not null, the message to associate with the incoming data
		/// while it is locked to prevent garbage collection.
		/// </param>
		public virtual void SetLockMessage(string msg)
		{
			lockMessage = msg;
		}

		/// <summary>Get the number of objects in the stream.</summary>
		/// <remarks>
		/// Get the number of objects in the stream.
		/// <p>
		/// The object count is only available after
		/// <see cref="Parse(NGit.ProgressMonitor)">Parse(NGit.ProgressMonitor)</see>
		/// has returned. The count may have been increased if the stream was a thin
		/// pack, and missing bases objects were appending onto it by the subclass.
		/// </remarks>
		/// <returns>number of objects parsed out of the stream.</returns>
		public virtual int GetObjectCount()
		{
			return entryCount;
		}

		/// <summary>Get the information about the requested object.</summary>
		/// <remarks>
		/// Get the information about the requested object.
		/// <p>
		/// The object information is only available after
		/// <see cref="Parse(NGit.ProgressMonitor)">Parse(NGit.ProgressMonitor)</see>
		/// has returned.
		/// </remarks>
		/// <param name="nth">
		/// index of the object in the stream. Must be between 0 and
		/// <see cref="GetObjectCount()">GetObjectCount()</see>
		/// -1.
		/// </param>
		/// <returns>the object information.</returns>
		public virtual PackedObjectInfo GetObject(int nth)
		{
			return entries[nth];
		}

		/// <summary>Get all of the objects, sorted by their name.</summary>
		/// <remarks>
		/// Get all of the objects, sorted by their name.
		/// <p>
		/// The object information is only available after
		/// <see cref="Parse(NGit.ProgressMonitor)">Parse(NGit.ProgressMonitor)</see>
		/// has returned.
		/// <p>
		/// To maintain lower memory usage and good runtime performance, this method
		/// sorts the objects in-place and therefore impacts the ordering presented
		/// by
		/// <see cref="GetObject(int)">GetObject(int)</see>
		/// .
		/// </remarks>
		/// <param name="cmp">comparison function, if null objects are stored by ObjectId.</param>
		/// <returns>sorted list of objects in this pack stream.</returns>
		public virtual IList<PackedObjectInfo> GetSortedObjectList(IComparer<PackedObjectInfo
			> cmp)
		{
			Arrays.Sort(entries, 0, entryCount, cmp);
			IList<PackedObjectInfo> list = Arrays.AsList(entries);
			if (entryCount < entries.Length)
			{
				list = list.SubList(0, entryCount);
			}
			return list;
		}

		/// <summary>Parse the pack stream.</summary>
		/// <remarks>Parse the pack stream.</remarks>
		/// <param name="progress">
		/// callback to provide progress feedback during parsing. If null,
		/// <see cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</see>
		/// will be used.
		/// </param>
		/// <returns>
		/// the pack lock, if one was requested by setting
		/// <see cref="SetLockMessage(string)">SetLockMessage(string)</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">the stream is malformed, or contains corrupt objects.
		/// 	</exception>
		public PackLock Parse(ProgressMonitor progress)
		{
			return Parse(progress, progress);
		}

		/// <summary>Parse the pack stream.</summary>
		/// <remarks>Parse the pack stream.</remarks>
		/// <param name="receiving">
		/// receives progress feedback during the initial receiving
		/// objects phase. If null,
		/// <see cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</see>
		/// will be
		/// used.
		/// </param>
		/// <param name="resolving">receives progress feedback during the resolving objects phase.
		/// 	</param>
		/// <returns>
		/// the pack lock, if one was requested by setting
		/// <see cref="SetLockMessage(string)">SetLockMessage(string)</see>
		/// .
		/// </returns>
		/// <exception cref="System.IO.IOException">the stream is malformed, or contains corrupt objects.
		/// 	</exception>
		public virtual PackLock Parse(ProgressMonitor receiving, ProgressMonitor resolving
			)
		{
			if (receiving == null)
			{
				receiving = NullProgressMonitor.INSTANCE;
			}
			if (resolving == null)
			{
				resolving = NullProgressMonitor.INSTANCE;
			}
			if (receiving == resolving)
			{
				receiving.Start(2);
			}
			try
			{
				ReadPackHeader();
				entries = new PackedObjectInfo[(int)objectCount];
				baseById = new ObjectIdOwnerMap<PackParser.DeltaChain>();
				baseByPos = new LongMap<PackParser.UnresolvedDelta>();
				deferredCheckBlobs = new BlockList<PackedObjectInfo>();
				receiving.BeginTask(JGitText.Get().receivingObjects, (int)objectCount);
				try
				{
					for (int done = 0; done < objectCount; done++)
					{
						IndexOneObject();
						receiving.Update(1);
						if (receiving.IsCancelled())
						{
							throw new IOException(JGitText.Get().downloadCancelled);
						}
					}
					ReadPackFooter();
					EndInput();
				}
				finally
				{
					receiving.EndTask();
				}
				if (!deferredCheckBlobs.IsEmpty())
				{
					DoDeferredCheckBlobs();
				}
				if (deltaCount > 0)
				{
					ResolveDeltas(resolving);
					if (entryCount < objectCount)
					{
						if (!IsAllowThin())
						{
							throw new IOException(MessageFormat.Format(JGitText.Get().packHasUnresolvedDeltas
								, (objectCount - entryCount)));
						}
						ResolveDeltasWithExternalBases(resolving);
						if (entryCount < objectCount)
						{
							throw new IOException(MessageFormat.Format(JGitText.Get().packHasUnresolvedDeltas
								, (objectCount - entryCount)));
						}
					}
				}
				packDigest = null;
				baseById = null;
				baseByPos = null;
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
					inflater.Release();
				}
				finally
				{
					inflater = null;
					objectDatabase.Close();
				}
			}
			return null;
		}

		// By default there is no locking.
		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltas(ProgressMonitor progress)
		{
			progress.BeginTask(JGitText.Get().resolvingDeltas, deltaCount);
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
			PackParser.UnresolvedDelta children = FirstChildOf(oe);
			if (children == null)
			{
				return;
			}
			PackParser.DeltaVisit visit = new PackParser.DeltaVisit();
			visit.nextChild = children;
			PackParser.ObjectTypeAndSize info = OpenDatabase(oe, new PackParser.ObjectTypeAndSize
				());
			switch (info.type)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					visit.data = InflateAndReturn(PackParser.Source.DATABASE, info.size);
					visit.id = oe;
					break;
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, info
						.type));
				}
			}
			if (!CheckCRC(oe.GetCRC()))
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().corruptionDetectedReReadingAt
					, oe.GetOffset()));
			}
			ResolveDeltas(visit.Next(), info.type, info);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltas(PackParser.DeltaVisit visit, int type, PackParser.ObjectTypeAndSize
			 info)
		{
			do
			{
				info = OpenDatabase(visit.delta, info);
				switch (info.type)
				{
					case Constants.OBJ_OFS_DELTA:
					case Constants.OBJ_REF_DELTA:
					{
						break;
					}

					default:
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, info
							.type));
					}
				}
				visit.data = BinaryDelta.Apply(visit.parent.data, InflateAndReturn(PackParser.Source
					.DATABASE, info.size));
				//
				if (!CheckCRC(visit.delta.crc))
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().corruptionDetectedReReadingAt
						, visit.delta.position));
				}
				objectDigest.Update(Constants.EncodedTypeString(type));
				objectDigest.Update(unchecked((byte)' '));
				objectDigest.Update(Constants.EncodeASCII(visit.data.Length));
				objectDigest.Update(unchecked((byte)0));
				objectDigest.Update(visit.data);
				tempObjectId.FromRaw(objectDigest.Digest(), 0);
				VerifySafeObject(tempObjectId, type, visit.data);
				PackedObjectInfo oe;
				oe = NewInfo(tempObjectId, visit.delta, visit.parent.id);
				oe.SetOffset(visit.delta.position);
				OnInflatedObjectData(oe, type, visit.data);
				AddObjectAndTrack(oe);
				visit.id = oe;
				visit.nextChild = FirstChildOf(oe);
				visit = visit.Next();
			}
			while (visit != null);
		}

		/// <summary>Read the header of the current object.</summary>
		/// <remarks>
		/// Read the header of the current object.
		/// <p>
		/// After the header has been parsed, this method automatically invokes
		/// <see cref="OnObjectHeader(Source, byte[], int, int)">OnObjectHeader(Source, byte[], int, int)
		/// 	</see>
		/// to allow the
		/// implementation to update its internal checksums for the bytes read.
		/// <p>
		/// When this method returns the database will be positioned on the first
		/// byte of the deflated data stream.
		/// </remarks>
		/// <param name="info">the info object to populate.</param>
		/// <returns>
		/// 
		/// <code>info</code>
		/// , after populating.
		/// </returns>
		/// <exception cref="System.IO.IOException">the size cannot be read.</exception>
		protected internal virtual PackParser.ObjectTypeAndSize ReadObjectHeader(PackParser.ObjectTypeAndSize
			 info)
		{
			int hdrPtr = 0;
			int c = ReadFrom(PackParser.Source.DATABASE);
			hdrBuf[hdrPtr++] = unchecked((byte)c);
			info.type = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ReadFrom(PackParser.Source.DATABASE);
				hdrBuf[hdrPtr++] = unchecked((byte)c);
				sz += (c & unchecked((int)(0x7f))) << shift;
				shift += 7;
			}
			info.size = sz;
			switch (info.type)
			{
				case Constants.OBJ_COMMIT:
				case Constants.OBJ_TREE:
				case Constants.OBJ_BLOB:
				case Constants.OBJ_TAG:
				{
					OnObjectHeader(PackParser.Source.DATABASE, hdrBuf, 0, hdrPtr);
					break;
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ReadFrom(PackParser.Source.DATABASE);
					hdrBuf[hdrPtr++] = unchecked((byte)c);
					while ((c & 128) != 0)
					{
						c = ReadFrom(PackParser.Source.DATABASE);
						hdrBuf[hdrPtr++] = unchecked((byte)c);
					}
					OnObjectHeader(PackParser.Source.DATABASE, hdrBuf, 0, hdrPtr);
					break;
				}

				case Constants.OBJ_REF_DELTA:
				{
					System.Array.Copy(buf, Fill(PackParser.Source.DATABASE, 20), hdrBuf, hdrPtr, 20);
					hdrPtr += 20;
					Use(20);
					OnObjectHeader(PackParser.Source.DATABASE, hdrBuf, 0, hdrPtr);
					break;
				}

				default:
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, info
						.type));
				}
			}
			return info;
		}

		private PackParser.UnresolvedDelta RemoveBaseById(AnyObjectId id)
		{
			PackParser.DeltaChain d = baseById.Get(id);
			return d != null ? d.Remove() : null;
		}

		private static PackParser.UnresolvedDelta Reverse(PackParser.UnresolvedDelta c)
		{
			PackParser.UnresolvedDelta tail = null;
			while (c != null)
			{
				PackParser.UnresolvedDelta n = c.next;
				c.next = tail;
				tail = c;
				c = n;
			}
			return tail;
		}

		private PackParser.UnresolvedDelta FirstChildOf(PackedObjectInfo oe)
		{
			PackParser.UnresolvedDelta a = Reverse(RemoveBaseById(oe));
			PackParser.UnresolvedDelta b = Reverse(baseByPos.Remove(oe.GetOffset()));
			if (a == null)
			{
				return b;
			}
			if (b == null)
			{
				return a;
			}
			PackParser.UnresolvedDelta first = null;
			PackParser.UnresolvedDelta last = null;
			while (a != null || b != null)
			{
				PackParser.UnresolvedDelta curr;
				if (b == null || (a != null && a.position < b.position))
				{
					curr = a;
					a = a.next;
				}
				else
				{
					curr = b;
					b = b.next;
				}
				if (last != null)
				{
					last.next = curr;
				}
				else
				{
					first = curr;
				}
				last = curr;
				curr.next = null;
			}
			return first;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResolveDeltasWithExternalBases(ProgressMonitor progress)
		{
			GrowEntries(baseById.Size());
			if (needBaseObjectIds)
			{
				baseObjectIds = new ObjectIdSubclassMap<ObjectId>();
			}
			IList<PackParser.DeltaChain> missing = new AList<PackParser.DeltaChain>(64);
			foreach (PackParser.DeltaChain baseId in baseById)
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
				PackParser.DeltaVisit visit = new PackParser.DeltaVisit();
				visit.data = ldr.GetCachedBytes(int.MaxValue);
				visit.id = baseId;
				int typeCode = ldr.GetType();
				PackedObjectInfo oe = NewInfo(baseId, null, null);
				if (OnAppendBase(typeCode, visit.data, oe))
				{
					entries[entryCount++] = oe;
				}
				visit.nextChild = FirstChildOf(oe);
				ResolveDeltas(visit.Next(), typeCode, new PackParser.ObjectTypeAndSize());
				if (progress.IsCancelled())
				{
					throw new IOException(JGitText.Get().downloadCancelledDuringIndexing);
				}
			}
			foreach (PackParser.DeltaChain @base in missing)
			{
				if (@base.head != null)
				{
					throw new MissingObjectException(@base, "delta base");
				}
			}
			OnEndThinPack();
		}

		private void GrowEntries(int extraObjects)
		{
			PackedObjectInfo[] ne;
			ne = new PackedObjectInfo[(int)objectCount + extraObjects];
			System.Array.Copy(entries, 0, ne, 0, entryCount);
			entries = ne;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadPackHeader()
		{
			int hdrln = Constants.PACK_SIGNATURE.Length + 4 + 4;
			int p = Fill(PackParser.Source.INPUT, hdrln);
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
			OnPackHeader(objectCount);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadPackFooter()
		{
			Sync();
			byte[] actHash = packDigest.Digest();
			int c = Fill(PackParser.Source.INPUT, 20);
			byte[] srcHash = new byte[20];
			System.Array.Copy(buf, c, srcHash, 0, 20);
			Use(20);
			// The input stream should be at EOF at this point. We do not support
			// yielding back any remaining buffered data after the pack footer, so
			// protocols that embed a pack stream are required to either end their
			// stream with the pack, or embed the pack with a framing system like
			// the SideBandInputStream does.
			if (bAvail != 0)
			{
				throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().expectedEOFReceived
					, "\\x" + Sharpen.Extensions.ToHexString(buf[bOffset] & unchecked((int)(0xff))))
					);
			}
			if (IsCheckEofAfterPackFooter())
			{
				int eof = @in.Read();
				if (0 <= eof)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().expectedEOFReceived
						, "\\x" + Sharpen.Extensions.ToHexString(eof)));
				}
			}
			if (!Arrays.Equals(actHash, srcHash))
			{
				throw new CorruptObjectException(JGitText.Get().corruptObjectPackfileChecksumIncorrect
					);
			}
			OnPackFooter(srcHash);
		}

		// Cleanup all resources associated with our input parsing.
		private void EndInput()
		{
			@in = null;
		}

		// Read one entire object or delta from the input.
		/// <exception cref="System.IO.IOException"></exception>
		private void IndexOneObject()
		{
			long streamPosition = StreamPosition();
			int hdrPtr = 0;
			int c = ReadFrom(PackParser.Source.INPUT);
			hdrBuf[hdrPtr++] = unchecked((byte)c);
			int typeCode = (c >> 4) & 7;
			long sz = c & 15;
			int shift = 4;
			while ((c & unchecked((int)(0x80))) != 0)
			{
				c = ReadFrom(PackParser.Source.INPUT);
				hdrBuf[hdrPtr++] = unchecked((byte)c);
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
					OnBeginWholeObject(streamPosition, typeCode, sz);
					OnObjectHeader(PackParser.Source.INPUT, hdrBuf, 0, hdrPtr);
					Whole(streamPosition, typeCode, sz);
					break;
				}

				case Constants.OBJ_OFS_DELTA:
				{
					c = ReadFrom(PackParser.Source.INPUT);
					hdrBuf[hdrPtr++] = unchecked((byte)c);
					long ofs = c & 127;
					while ((c & 128) != 0)
					{
						ofs += 1;
						c = ReadFrom(PackParser.Source.INPUT);
						hdrBuf[hdrPtr++] = unchecked((byte)c);
						ofs <<= 7;
						ofs += (c & 127);
					}
					long @base = streamPosition - ofs;
					OnBeginOfsDelta(streamPosition, @base, sz);
					OnObjectHeader(PackParser.Source.INPUT, hdrBuf, 0, hdrPtr);
					InflateAndSkip(PackParser.Source.INPUT, sz);
					PackParser.UnresolvedDelta n = OnEndDelta();
					n.position = streamPosition;
					n.next = baseByPos.Put(@base, n);
					deltaCount++;
					break;
				}

				case Constants.OBJ_REF_DELTA:
				{
					c = Fill(PackParser.Source.INPUT, 20);
					ObjectId @base = ObjectId.FromRaw(buf, c);
					System.Array.Copy(buf, c, hdrBuf, hdrPtr, 20);
					hdrPtr += 20;
					Use(20);
					PackParser.DeltaChain r = baseById.Get(@base);
					if (r == null)
					{
						r = new PackParser.DeltaChain(@base);
						baseById.Add(r);
					}
					OnBeginRefDelta(streamPosition, @base, sz);
					OnObjectHeader(PackParser.Source.INPUT, hdrBuf, 0, hdrPtr);
					InflateAndSkip(PackParser.Source.INPUT, sz);
					PackParser.UnresolvedDelta n = OnEndDelta();
					n.position = streamPosition;
					r.Add(n);
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
		private void Whole(long pos, int type, long sz)
		{
			objectDigest.Update(Constants.EncodedTypeString(type));
			objectDigest.Update(unchecked((byte)' '));
			objectDigest.Update(Constants.EncodeASCII(sz));
			objectDigest.Update(unchecked((byte)0));
			byte[] data;
			bool checkContentLater = false;
			if (type == Constants.OBJ_BLOB)
			{
				byte[] readBuffer = Buffer();
				InputStream inf = Inflate(PackParser.Source.INPUT, sz);
				long cnt = 0;
				while (cnt < sz)
				{
					int r = inf.Read(readBuffer);
					if (r <= 0)
					{
						break;
					}
					objectDigest.Update(readBuffer, 0, r);
					cnt += r;
				}
				inf.Close();
				tempObjectId.FromRaw(objectDigest.Digest(), 0);
				checkContentLater = IsCheckObjectCollisions() && readCurs.Has(tempObjectId);
				data = null;
			}
			else
			{
				data = InflateAndReturn(PackParser.Source.INPUT, sz);
				objectDigest.Update(data);
				tempObjectId.FromRaw(objectDigest.Digest(), 0);
				VerifySafeObject(tempObjectId, type, data);
			}
			PackedObjectInfo obj = NewInfo(tempObjectId, null, null);
			obj.SetOffset(pos);
			OnEndWholeObject(obj);
			if (data != null)
			{
				OnInflatedObjectData(obj, type, data);
			}
			AddObjectAndTrack(obj);
			if (checkContentLater)
			{
				deferredCheckBlobs.AddItem(obj);
			}
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
			if (IsCheckObjectCollisions())
			{
				try
				{
					ObjectLoader ldr = readCurs.Open(id, type);
					byte[] existingData = ldr.GetCachedBytes(data.Length);
					if (!Arrays.Equals(data, existingData))
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().collisionOn, id.Name));
					}
				}
				catch (MissingObjectException)
				{
				}
			}
		}

		// This is OK, we don't have a copy of the object locally
		// but the API throws when we try to read it as usually its
		// an error to read something that doesn't exist.
		/// <exception cref="System.IO.IOException"></exception>
		private void DoDeferredCheckBlobs()
		{
			byte[] readBuffer = Buffer();
			byte[] curBuffer = new byte[readBuffer.Length];
			PackParser.ObjectTypeAndSize info = new PackParser.ObjectTypeAndSize();
			foreach (PackedObjectInfo obj in deferredCheckBlobs)
			{
				info = OpenDatabase(obj, info);
				if (info.type != Constants.OBJ_BLOB)
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownObjectType, info
						.type));
				}
				ObjectStream cur = readCurs.Open(obj, info.type).OpenStream();
				try
				{
					long sz = info.size;
					if (cur.GetSize() != sz)
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().collisionOn, obj.Name));
					}
					InputStream pck = Inflate(PackParser.Source.DATABASE, sz);
					while (0 < sz)
					{
						int n = (int)Math.Min(readBuffer.Length, sz);
						IOUtil.ReadFully(cur, curBuffer, 0, n);
						IOUtil.ReadFully(pck, readBuffer, 0, n);
						for (int i = 0; i < n; i++)
						{
							if (curBuffer[i] != readBuffer[i])
							{
								throw new IOException(MessageFormat.Format(JGitText.Get().collisionOn, obj.Name));
							}
						}
						sz -= n;
					}
					pck.Close();
				}
				finally
				{
					cur.Close();
				}
			}
		}

		/// <returns>current position of the input stream being parsed.</returns>
		private long StreamPosition()
		{
			return bBase + bOffset;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PackParser.ObjectTypeAndSize OpenDatabase(PackedObjectInfo obj, PackParser.ObjectTypeAndSize
			 info)
		{
			bOffset = 0;
			bAvail = 0;
			return SeekDatabase(obj, info);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PackParser.ObjectTypeAndSize OpenDatabase(PackParser.UnresolvedDelta delta
			, PackParser.ObjectTypeAndSize info)
		{
			bOffset = 0;
			bAvail = 0;
			return SeekDatabase(delta, info);
		}

		// Consume exactly one byte from the buffer and return it.
		/// <exception cref="System.IO.IOException"></exception>
		private int ReadFrom(PackParser.Source src)
		{
			if (bAvail == 0)
			{
				Fill(src, 1);
			}
			bAvail--;
			return buf[bOffset++] & unchecked((int)(0xff));
		}

		// Consume cnt bytes from the buffer.
		private void Use(int cnt)
		{
			bOffset += cnt;
			bAvail -= cnt;
		}

		// Ensure at least need bytes are available in in {@link #buf}.
		/// <exception cref="System.IO.IOException"></exception>
		private int Fill(PackParser.Source src, int need)
		{
			while (bAvail < need)
			{
				int next = bOffset + bAvail;
				int free = buf.Length - next;
				if (free + bAvail < need)
				{
					switch (src)
					{
						case PackParser.Source.INPUT:
						{
							Sync();
							break;
						}

						case PackParser.Source.DATABASE:
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
					case PackParser.Source.INPUT:
					{
						next = @in.Read(buf, next, free);
						break;
					}

					case PackParser.Source.DATABASE:
					{
						next = ReadDatabase(buf, next, free);
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
			OnStoreStream(buf, 0, bOffset);
			if (bAvail > 0)
			{
				System.Array.Copy(buf, bOffset, buf, 0, bAvail);
			}
			bBase += bOffset;
			bOffset = 0;
		}

		/// <returns>a temporary byte array for use by the caller.</returns>
		protected internal virtual byte[] Buffer()
		{
			return tempBuffer;
		}

		/// <summary>Construct a PackedObjectInfo instance for this parser.</summary>
		/// <remarks>Construct a PackedObjectInfo instance for this parser.</remarks>
		/// <param name="id">identity of the object to be tracked.</param>
		/// <param name="delta">
		/// if the object was previously an unresolved delta, this is the
		/// delta object that was tracking it. Otherwise null.
		/// </param>
		/// <param name="deltaBase">
		/// if the object was previously an unresolved delta, this is the
		/// ObjectId of the base of the delta. The base may be outside of
		/// the pack stream if the stream was a thin-pack.
		/// </param>
		/// <returns>info object containing this object's data.</returns>
		protected internal virtual PackedObjectInfo NewInfo(AnyObjectId id, PackParser.UnresolvedDelta
			 delta, ObjectId deltaBase)
		{
			PackedObjectInfo oe = new PackedObjectInfo(id);
			if (delta != null)
			{
				oe.SetCRC(delta.crc);
			}
			return oe;
		}

		/// <summary>Store bytes received from the raw stream.</summary>
		/// <remarks>
		/// Store bytes received from the raw stream.
		/// <p>
		/// This method is invoked during
		/// <see cref="Parse(NGit.ProgressMonitor)">Parse(NGit.ProgressMonitor)</see>
		/// as data is
		/// consumed from the incoming stream. Implementors may use this event to
		/// archive the raw incoming stream to the destination repository in large
		/// chunks, without paying attention to object boundaries.
		/// <p>
		/// The only component of the pack not supplied to this method is the last 20
		/// bytes of the pack that comprise the trailing SHA-1 checksum. Those are
		/// passed to
		/// <see cref="OnPackFooter(byte[])">OnPackFooter(byte[])</see>
		/// .
		/// </remarks>
		/// <param name="raw">buffer to copy data out of.</param>
		/// <param name="pos">first offset within the buffer that is valid.</param>
		/// <param name="len">number of bytes in the buffer that are valid.</param>
		/// <exception cref="System.IO.IOException">the stream cannot be archived.</exception>
		protected internal abstract void OnStoreStream(byte[] raw, int pos, int len);

		/// <summary>Store (and/or checksum) an object header.</summary>
		/// <remarks>
		/// Store (and/or checksum) an object header.
		/// <p>
		/// Invoked after any of the
		/// <code>onBegin()</code>
		/// events. The entire header is
		/// supplied in a single invocation, before any object data is supplied.
		/// </remarks>
		/// <param name="src">where the data came from</param>
		/// <param name="raw">buffer to read data from.</param>
		/// <param name="pos">first offset within buffer that is valid.</param>
		/// <param name="len">number of bytes in buffer that are valid.</param>
		/// <exception cref="System.IO.IOException">the stream cannot be archived.</exception>
		protected internal abstract void OnObjectHeader(PackParser.Source src, byte[] raw
			, int pos, int len);

		/// <summary>Store (and/or checksum) a portion of an object's data.</summary>
		/// <remarks>
		/// Store (and/or checksum) a portion of an object's data.
		/// <p>
		/// This method may be invoked multiple times per object, depending on the
		/// size of the object, the size of the parser's internal read buffer, and
		/// the alignment of the object relative to the read buffer.
		/// <p>
		/// Invoked after
		/// <see cref="OnObjectHeader(Source, byte[], int, int)">OnObjectHeader(Source, byte[], int, int)
		/// 	</see>
		/// .
		/// </remarks>
		/// <param name="src">where the data came from</param>
		/// <param name="raw">buffer to read data from.</param>
		/// <param name="pos">first offset within buffer that is valid.</param>
		/// <param name="len">number of bytes in buffer that are valid.</param>
		/// <exception cref="System.IO.IOException">the stream cannot be archived.</exception>
		protected internal abstract void OnObjectData(PackParser.Source src, byte[] raw, 
			int pos, int len);

		/// <summary>Invoked for commits, trees, tags, and small blobs.</summary>
		/// <remarks>Invoked for commits, trees, tags, and small blobs.</remarks>
		/// <param name="obj">the object info, populated.</param>
		/// <param name="typeCode">the type of the object.</param>
		/// <param name="data">inflated data for the object.</param>
		/// <exception cref="System.IO.IOException">the object cannot be archived.</exception>
		protected internal abstract void OnInflatedObjectData(PackedObjectInfo obj, int typeCode
			, byte[] data);

		/// <summary>Provide the implementation with the original stream's pack header.</summary>
		/// <remarks>Provide the implementation with the original stream's pack header.</remarks>
		/// <param name="objCnt">number of objects expected in the stream.</param>
		/// <exception cref="System.IO.IOException">the implementation refuses to work with this many objects.
		/// 	</exception>
		protected internal abstract void OnPackHeader(long objCnt);

		/// <summary>Provide the implementation with the original stream's pack footer.</summary>
		/// <remarks>Provide the implementation with the original stream's pack footer.</remarks>
		/// <param name="hash">
		/// the trailing 20 bytes of the pack, this is a SHA-1 checksum of
		/// all of the pack data.
		/// </param>
		/// <exception cref="System.IO.IOException">the stream cannot be archived.</exception>
		protected internal abstract void OnPackFooter(byte[] hash);

		/// <summary>Provide the implementation with a base that was outside of the pack.</summary>
		/// <remarks>
		/// Provide the implementation with a base that was outside of the pack.
		/// <p>
		/// This event only occurs on a thin pack for base objects that were outside
		/// of the pack and came from the local repository. Usually an implementation
		/// uses this event to compress the base and append it onto the end of the
		/// pack, so the pack stays self-contained.
		/// </remarks>
		/// <param name="typeCode">type of the base object.</param>
		/// <param name="data">complete content of the base object.</param>
		/// <param name="info">
		/// packed object information for this base. Implementors must
		/// populate the CRC and offset members if returning true.
		/// </param>
		/// <returns>
		/// true if the
		/// <code>info</code>
		/// should be included in the object list
		/// returned by
		/// <see cref="GetSortedObjectList(System.Collections.Generic.IComparer{T})">GetSortedObjectList(System.Collections.Generic.IComparer&lt;T&gt;)
		/// 	</see>
		/// , false if it
		/// should not be included.
		/// </returns>
		/// <exception cref="System.IO.IOException">the base could not be included into the pack.
		/// 	</exception>
		protected internal abstract bool OnAppendBase(int typeCode, byte[] data, PackedObjectInfo
			 info);

		/// <summary>Event indicating a thin pack has been completely processed.</summary>
		/// <remarks>
		/// Event indicating a thin pack has been completely processed.
		/// <p>
		/// This event is invoked only if a thin pack has delta references to objects
		/// external from the pack. The event is called after all of those deltas
		/// have been resolved.
		/// </remarks>
		/// <exception cref="System.IO.IOException">the pack cannot be archived.</exception>
		protected internal abstract void OnEndThinPack();

		/// <summary>Reposition the database to re-read a previously stored object.</summary>
		/// <remarks>
		/// Reposition the database to re-read a previously stored object.
		/// <p>
		/// If the database is computing CRC-32 checksums for object data, it should
		/// reset its internal CRC instance during this method call.
		/// </remarks>
		/// <param name="obj">
		/// the object position to begin reading from. This is from
		/// <see cref="NewInfo(NGit.AnyObjectId, UnresolvedDelta, NGit.ObjectId)">NewInfo(NGit.AnyObjectId, UnresolvedDelta, NGit.ObjectId)
		/// 	</see>
		/// .
		/// </param>
		/// <param name="info">object to populate with type and size.</param>
		/// <returns>
		/// the
		/// <code>info</code>
		/// object.
		/// </returns>
		/// <exception cref="System.IO.IOException">the database cannot reposition to this location.
		/// 	</exception>
		protected internal abstract PackParser.ObjectTypeAndSize SeekDatabase(PackedObjectInfo
			 obj, PackParser.ObjectTypeAndSize info);

		/// <summary>Reposition the database to re-read a previously stored object.</summary>
		/// <remarks>
		/// Reposition the database to re-read a previously stored object.
		/// <p>
		/// If the database is computing CRC-32 checksums for object data, it should
		/// reset its internal CRC instance during this method call.
		/// </remarks>
		/// <param name="delta">
		/// the object position to begin reading from. This is an instance
		/// previously returned by
		/// <see cref="OnEndDelta()">OnEndDelta()</see>
		/// .
		/// </param>
		/// <param name="info">object to populate with type and size.</param>
		/// <returns>
		/// the
		/// <code>info</code>
		/// object.
		/// </returns>
		/// <exception cref="System.IO.IOException">the database cannot reposition to this location.
		/// 	</exception>
		protected internal abstract PackParser.ObjectTypeAndSize SeekDatabase(PackParser.UnresolvedDelta
			 delta, PackParser.ObjectTypeAndSize info);

		/// <summary>Read from the database's current position into the buffer.</summary>
		/// <remarks>Read from the database's current position into the buffer.</remarks>
		/// <param name="dst">the buffer to copy read data into.</param>
		/// <param name="pos">
		/// position within
		/// <code>dst</code>
		/// to start copying data into.
		/// </param>
		/// <param name="cnt">
		/// ideal target number of bytes to read. Actual read length may
		/// be shorter.
		/// </param>
		/// <returns>number of bytes stored.</returns>
		/// <exception cref="System.IO.IOException">the database cannot be accessed.</exception>
		protected internal abstract int ReadDatabase(byte[] dst, int pos, int cnt);

		/// <summary>Check the current CRC matches the expected value.</summary>
		/// <remarks>
		/// Check the current CRC matches the expected value.
		/// <p>
		/// This method is invoked when an object is read back in from the database
		/// and its data is used during delta resolution. The CRC is validated after
		/// the object has been fully read, allowing the parser to verify there was
		/// no silent data corruption.
		/// <p>
		/// Implementations are free to ignore this check by always returning true if
		/// they are performing other data integrity validations at a lower level.
		/// </remarks>
		/// <param name="oldCRC">
		/// the prior CRC that was recorded during the first scan of the
		/// object from the pack stream.
		/// </param>
		/// <returns>true if the CRC matches; false if it does not.</returns>
		protected internal abstract bool CheckCRC(int oldCRC);

		/// <summary>Event notifying the start of an object stored whole (not as a delta).</summary>
		/// <remarks>Event notifying the start of an object stored whole (not as a delta).</remarks>
		/// <param name="streamPosition">position of this object in the incoming stream.</param>
		/// <param name="type">
		/// type of the object; one of
		/// <see cref="NGit.Constants.OBJ_COMMIT">NGit.Constants.OBJ_COMMIT</see>
		/// ,
		/// <see cref="NGit.Constants.OBJ_TREE">NGit.Constants.OBJ_TREE</see>
		/// ,
		/// <see cref="NGit.Constants.OBJ_BLOB">NGit.Constants.OBJ_BLOB</see>
		/// , or
		/// <see cref="NGit.Constants.OBJ_TAG">NGit.Constants.OBJ_TAG</see>
		/// .
		/// </param>
		/// <param name="inflatedSize">
		/// size of the object when fully inflated. The size stored within
		/// the pack may be larger or smaller, and is not yet known.
		/// </param>
		/// <exception cref="System.IO.IOException">the object cannot be recorded.</exception>
		protected internal abstract void OnBeginWholeObject(long streamPosition, int type
			, long inflatedSize);

		/// <summary>Event notifying the the current object.</summary>
		/// <remarks>Event notifying the the current object.</remarks>
		/// <param name="info">object information.</param>
		/// <exception cref="System.IO.IOException">the object cannot be recorded.</exception>
		protected internal abstract void OnEndWholeObject(PackedObjectInfo info);

		/// <summary>Event notifying start of a delta referencing its base by offset.</summary>
		/// <remarks>Event notifying start of a delta referencing its base by offset.</remarks>
		/// <param name="deltaStreamPosition">position of this object in the incoming stream.
		/// 	</param>
		/// <param name="baseStreamPosition">
		/// position of the base object in the incoming stream. The base
		/// must be before the delta, therefore
		/// <code>
		/// baseStreamPosition
		/// &lt; deltaStreamPosition
		/// </code>
		/// . This is <b>not</b> the position
		/// returned by a prior end object event.
		/// </param>
		/// <param name="inflatedSize">
		/// size of the delta when fully inflated. The size stored within
		/// the pack may be larger or smaller, and is not yet known.
		/// </param>
		/// <exception cref="System.IO.IOException">the object cannot be recorded.</exception>
		protected internal abstract void OnBeginOfsDelta(long deltaStreamPosition, long baseStreamPosition
			, long inflatedSize);

		/// <summary>Event notifying start of a delta referencing its base by ObjectId.</summary>
		/// <remarks>Event notifying start of a delta referencing its base by ObjectId.</remarks>
		/// <param name="deltaStreamPosition">position of this object in the incoming stream.
		/// 	</param>
		/// <param name="baseId">
		/// name of the base object. This object may be later in the
		/// stream, or might not appear at all in the stream (in the case
		/// of a thin-pack).
		/// </param>
		/// <param name="inflatedSize">
		/// size of the delta when fully inflated. The size stored within
		/// the pack may be larger or smaller, and is not yet known.
		/// </param>
		/// <exception cref="System.IO.IOException">the object cannot be recorded.</exception>
		protected internal abstract void OnBeginRefDelta(long deltaStreamPosition, AnyObjectId
			 baseId, long inflatedSize);

		/// <summary>Event notifying the the current object.</summary>
		/// <remarks>Event notifying the the current object.</remarks>
		/// <returns>
		/// object information that must be populated with at least the
		/// offset.
		/// </returns>
		/// <exception cref="System.IO.IOException">the object cannot be recorded.</exception>
		protected internal virtual PackParser.UnresolvedDelta OnEndDelta()
		{
			return new PackParser.UnresolvedDelta();
		}

		/// <summary>Type and size information about an object in the database buffer.</summary>
		/// <remarks>Type and size information about an object in the database buffer.</remarks>
		public class ObjectTypeAndSize
		{
			/// <summary>The type of the object.</summary>
			/// <remarks>The type of the object.</remarks>
			public int type;

			/// <summary>The inflated size of the object.</summary>
			/// <remarks>The inflated size of the object.</remarks>
			public long size;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void InflateAndSkip(PackParser.Source src, long inflatedSize)
		{
			InputStream inf = Inflate(src, inflatedSize);
			IOUtil.SkipFully(inf, inflatedSize);
			inf.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private byte[] InflateAndReturn(PackParser.Source src, long inflatedSize)
		{
			byte[] dst = new byte[(int)inflatedSize];
			InputStream inf = Inflate(src, inflatedSize);
			IOUtil.ReadFully(inf, dst, 0, dst.Length);
			inf.Close();
			return dst;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private InputStream Inflate(PackParser.Source src, long inflatedSize)
		{
			inflater.Open(src, inflatedSize);
			return inflater;
		}

		[System.Serializable]
		private class DeltaChain : ObjectIdOwnerMap.Entry
		{
			internal PackParser.UnresolvedDelta head;

			protected internal DeltaChain(AnyObjectId id) : base(id)
			{
			}

			internal virtual PackParser.UnresolvedDelta Remove()
			{
				PackParser.UnresolvedDelta r = head;
				if (r != null)
				{
					head = null;
				}
				return r;
			}

			internal virtual void Add(PackParser.UnresolvedDelta d)
			{
				d.next = head;
				head = d;
			}
		}

		/// <summary>Information about an unresolved delta in this pack stream.</summary>
		/// <remarks>Information about an unresolved delta in this pack stream.</remarks>
		public class UnresolvedDelta
		{
			internal long position;

			internal int crc;

			internal PackParser.UnresolvedDelta next;

			/// <returns>offset within the input stream.</returns>
			public virtual long GetOffset()
			{
				return position;
			}

			/// <returns>the CRC-32 checksum of the stored delta data.</returns>
			public virtual int GetCRC()
			{
				return crc;
			}

			/// <param name="crc32">the CRC-32 checksum of the stored delta data.</param>
			public virtual void SetCRC(int crc32)
			{
				crc = crc32;
			}
		}

		private class DeltaVisit
		{
			internal readonly PackParser.UnresolvedDelta delta;

			internal ObjectId id;

			internal byte[] data;

			internal PackParser.DeltaVisit parent;

			internal PackParser.UnresolvedDelta nextChild;

			public DeltaVisit()
			{
				this.delta = null;
			}

			internal DeltaVisit(PackParser.DeltaVisit parent)
			{
				// At the root of the stack we have a base.
				this.parent = parent;
				this.delta = parent.nextChild;
				parent.nextChild = delta.next;
			}

			internal virtual PackParser.DeltaVisit Next()
			{
				// If our parent has no more children, discard it.
				if (parent != null && parent.nextChild == null)
				{
					parent.data = null;
					parent = parent.parent;
				}
				if (nextChild != null)
				{
					return new PackParser.DeltaVisit(this);
				}
				// If we have no child ourselves, our parent must (if it exists),
				// due to the discard rule above. With no parent, we are done.
				if (parent != null)
				{
					return new PackParser.DeltaVisit(parent);
				}
				return null;
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

		private class InflaterStream : InputStream
		{
			private readonly Inflater inf;

			private readonly byte[] skipBuffer;

			private PackParser.Source src;

			private long expectedSize;

			private long actualSize;

			private int p;

			public InflaterStream(PackParser _enclosing)
			{
				this._enclosing = _enclosing;
				this.inf = InflaterCache.Get();
				this.skipBuffer = new byte[512];
			}

			internal virtual void Release()
			{
				this.inf.Reset();
				InflaterCache.Release(this.inf);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal virtual void Open(PackParser.Source source, long inflatedSize)
			{
				this.src = source;
				this.expectedSize = inflatedSize;
				this.actualSize = 0;
				this.p = this._enclosing.Fill(this.src, 1);
				this.inf.SetInput(this._enclosing.buf, this.p, this._enclosing.bAvail);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Skip(long toSkip)
			{
				long n = 0;
				while (n < toSkip)
				{
					int cnt = (int)Math.Min(this.skipBuffer.Length, toSkip - n);
					int r = this.Read(this.skipBuffer, 0, cnt);
					if (r <= 0)
					{
						break;
					}
					n += r;
				}
				return n;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read()
			{
				int n = this.Read(this.skipBuffer, 0, 1);
				return n == 1 ? this.skipBuffer[0] & unchecked((int)(0xff)) : -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Read(byte[] dst, int pos, int cnt)
			{
				try
				{
					int n = 0;
					while (n < cnt)
					{
						int r = this.inf.Inflate(dst, pos + n, cnt - n);
						if (r == 0)
						{
							if (this.inf.IsFinished)
							{
								break;
							}
							if (this.inf.IsNeedingInput)
							{
								this._enclosing.OnObjectData(this.src, this._enclosing.buf, this.p, this._enclosing
									.bAvail);
								this._enclosing.Use(this._enclosing.bAvail);
								this.p = this._enclosing.Fill(this.src, 1);
								this.inf.SetInput(this._enclosing.buf, this.p, this._enclosing.bAvail);
							}
							else
							{
								throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
									, JGitText.Get().unknownZlibError));
							}
						}
						else
						{
							n += r;
						}
					}
					this.actualSize += n;
					return 0 < n ? n : -1;
				}
				catch (SharpZipBaseException dfe)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
						, dfe.Message));
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				// We need to read here to enter the loop above and pump the
				// trailing checksum into the Inflater. It should return -1 as the
				// caller was supposed to consume all content.
				//
				if (this.Read(this.skipBuffer) != -1 || this.actualSize != this.expectedSize)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().packfileCorruptionDetected
						, JGitText.Get().wrongDecompressedLength));
				}
				int used = this._enclosing.bAvail - this.inf.RemainingInput;
				if (0 < used)
				{
					this._enclosing.OnObjectData(this.src, this._enclosing.buf, this.p, used);
					this._enclosing.Use(used);
				}
				this.inf.Reset();
			}

			private readonly PackParser _enclosing;
		}
	}
}
