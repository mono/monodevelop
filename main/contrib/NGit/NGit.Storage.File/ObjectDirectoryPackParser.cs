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
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>
	/// Consumes a pack stream and stores as a pack file in
	/// <see cref="ObjectDirectory">ObjectDirectory</see>
	/// .
	/// <p>
	/// To obtain an instance of a parser, applications should use
	/// <see cref="NGit.ObjectInserter.NewPackParser(Sharpen.InputStream)">NGit.ObjectInserter.NewPackParser(Sharpen.InputStream)
	/// 	</see>
	/// .
	/// </summary>
	public class ObjectDirectoryPackParser : PackParser
	{
		private readonly FileObjectDatabase db;

		/// <summary>CRC-32 computation for objects that are appended onto the pack.</summary>
		/// <remarks>CRC-32 computation for objects that are appended onto the pack.</remarks>
		private readonly CRC32 crc;

		/// <summary>
		/// Running SHA-1 of any base objects appended after
		/// <see cref="origEnd">origEnd</see>
		/// .
		/// </summary>
		private readonly MessageDigest tailDigest;

		/// <summary>Preferred format version of the pack-*.idx file to generate.</summary>
		/// <remarks>Preferred format version of the pack-*.idx file to generate.</remarks>
		private int indexVersion;

		/// <summary>If true, pack with 0 objects will be stored.</summary>
		/// <remarks>If true, pack with 0 objects will be stored. Usually these are deleted.</remarks>
		private bool keepEmpty;

		/// <summary>Path of the temporary file holding the pack data.</summary>
		/// <remarks>Path of the temporary file holding the pack data.</remarks>
		private FilePath tmpPack;

		/// <summary>
		/// Path of the index created for the pack, to find objects quickly at read
		/// time.
		/// </summary>
		/// <remarks>
		/// Path of the index created for the pack, to find objects quickly at read
		/// time.
		/// </remarks>
		private FilePath tmpIdx;

		/// <summary>
		/// Read/write handle to
		/// <see cref="tmpPack">tmpPack</see>
		/// while it is being parsed.
		/// </summary>
		private RandomAccessFile @out;

		/// <summary>Length of the original pack stream, before missing bases were appended.</summary>
		/// <remarks>Length of the original pack stream, before missing bases were appended.</remarks>
		private long origEnd;

		/// <summary>
		/// The original checksum of data up to
		/// <see cref="origEnd">origEnd</see>
		/// .
		/// </summary>
		private byte[] origHash;

		/// <summary>Current end of the pack file.</summary>
		/// <remarks>Current end of the pack file.</remarks>
		private long packEnd;

		/// <summary>Checksum of the entire pack file.</summary>
		/// <remarks>Checksum of the entire pack file.</remarks>
		private byte[] packHash;

		/// <summary>Compresses delta bases when completing a thin pack.</summary>
		/// <remarks>Compresses delta bases when completing a thin pack.</remarks>
		private Deflater def;

		/// <summary>The pack that was created, if parsing was successful.</summary>
		/// <remarks>The pack that was created, if parsing was successful.</remarks>
		private PackFile newPack;

		internal ObjectDirectoryPackParser(FileObjectDatabase odb, InputStream src) : base
			(odb, src)
		{
			this.db = odb;
			this.crc = new CRC32();
			this.tailDigest = Constants.NewMessageDigest();
			indexVersion = db.GetConfig().Get(CoreConfig.KEY).GetPackIndexVersion();
		}

		/// <summary>Set the pack index file format version this instance will create.</summary>
		/// <remarks>Set the pack index file format version this instance will create.</remarks>
		/// <param name="version">
		/// the version to write. The special version 0 designates the
		/// oldest (most compatible) format available for the objects.
		/// </param>
		/// <seealso cref="PackIndexWriter">PackIndexWriter</seealso>
		public virtual void SetIndexVersion(int version)
		{
			indexVersion = version;
		}

		/// <summary>Configure this index pack instance to keep an empty pack.</summary>
		/// <remarks>
		/// Configure this index pack instance to keep an empty pack.
		/// <p>
		/// By default an empty pack (a pack with no objects) is not kept, as doi so
		/// is completely pointless. With no objects in the pack there is no d stored
		/// by it, so the pack is unnecessary.
		/// </remarks>
		/// <param name="empty">true to enable keeping an empty pack.</param>
		public virtual void SetKeepEmpty(bool empty)
		{
			keepEmpty = empty;
		}

		/// <summary>
		/// Get the imported
		/// <see cref="PackFile">PackFile</see>
		/// .
		/// <p>
		/// This method is supplied only to support testing; applications shouldn't
		/// be using it directly to access the imported data.
		/// </summary>
		/// <returns>the imported PackFile, if parsing was successful.</returns>
		public virtual PackFile GetPackFile()
		{
			return newPack;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override PackLock Parse(ProgressMonitor receiving, ProgressMonitor resolving
			)
		{
			tmpPack = FilePath.CreateTempFile("incoming_", ".pack", db.GetDirectory());
			tmpIdx = new FilePath(db.GetDirectory(), BaseName(tmpPack) + ".idx");
			try
			{
				@out = new RandomAccessFile(tmpPack, "rw");
				base.Parse(receiving, resolving);
				@out.Seek(packEnd);
				@out.Write(packHash);
				@out.GetChannel().Force(true);
				@out.Close();
				WriteIdx();
				tmpPack.SetReadOnly();
				tmpIdx.SetReadOnly();
				return RenameAndOpenPack(GetLockMessage());
			}
			finally
			{
				if (def != null)
				{
					def.Finish();
				}
				try
				{
					if (@out != null && @out.GetChannel().IsOpen())
					{
						@out.Close();
					}
				}
				catch (IOException)
				{
				}
				// Ignored. We want to delete the file.
				CleanupTemporaryFiles();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnPackHeader(long objectCount)
		{
		}

		// Ignored, the count is not required.
		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnBeginWholeObject(long streamPosition, int type
			, long inflatedSize)
		{
			crc.Reset();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnEndWholeObject(PackedObjectInfo info)
		{
			info.SetCRC((int)crc.GetValue());
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnBeginOfsDelta(long streamPosition, long baseStreamPosition
			, long inflatedSize)
		{
			crc.Reset();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnBeginRefDelta(long streamPosition, AnyObjectId
			 baseId, long inflatedSize)
		{
			crc.Reset();
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override PackParser.UnresolvedDelta OnEndDelta()
		{
			PackParser.UnresolvedDelta delta = new PackParser.UnresolvedDelta();
			delta.SetCRC((int)crc.GetValue());
			return delta;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnInflatedObjectData(PackedObjectInfo obj, int typeCode
			, byte[] data)
		{
		}

		// ObjectDirectory ignores this event.
		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnObjectHeader(PackParser.Source src, byte[] raw
			, int pos, int len)
		{
			crc.Update(raw, pos, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnObjectData(PackParser.Source src, byte[] raw, 
			int pos, int len)
		{
			crc.Update(raw, pos, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnStoreStream(byte[] raw, int pos, int len)
		{
			@out.Write(raw, pos, len);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnPackFooter(byte[] hash)
		{
			packEnd = @out.GetFilePointer();
			origEnd = packEnd;
			origHash = hash;
			packHash = hash;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override PackParser.ObjectTypeAndSize SeekDatabase(PackParser.UnresolvedDelta
			 delta, PackParser.ObjectTypeAndSize info)
		{
			@out.Seek(delta.GetOffset());
			crc.Reset();
			return ReadObjectHeader(info);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override PackParser.ObjectTypeAndSize SeekDatabase(PackedObjectInfo
			 obj, PackParser.ObjectTypeAndSize info)
		{
			@out.Seek(obj.GetOffset());
			crc.Reset();
			return ReadObjectHeader(info);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override int ReadDatabase(byte[] dst, int pos, int cnt)
		{
			return @out.Read(dst, pos, cnt);
		}

		protected internal override bool CheckCRC(int oldCRC)
		{
			return oldCRC == (int)crc.GetValue();
		}

		private static string BaseName(FilePath tmpPack)
		{
			string name = tmpPack.GetName();
			return Sharpen.Runtime.Substring(name, 0, name.LastIndexOf('.'));
		}

		private void CleanupTemporaryFiles()
		{
			if (tmpIdx != null && !tmpIdx.Delete() && tmpIdx.Exists())
			{
				tmpIdx.DeleteOnExit();
			}
			if (tmpPack != null && !tmpPack.Delete() && tmpPack.Exists())
			{
				tmpPack.DeleteOnExit();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override bool OnAppendBase(int typeCode, byte[] data, PackedObjectInfo
			 info)
		{
			info.SetOffset(packEnd);
			byte[] buf = Buffer();
			int sz = data.Length;
			int len = 0;
			buf[len++] = unchecked((byte)((typeCode << 4) | sz & 15));
			sz = (int)(((uint)sz) >> 4);
			while (sz > 0)
			{
				buf[len - 1] |= unchecked((int)(0x80));
				buf[len++] = unchecked((byte)(sz & unchecked((int)(0x7f))));
				sz = (int)(((uint)sz) >> 7);
			}
			tailDigest.Update(buf, 0, len);
			crc.Reset();
			crc.Update(buf, 0, len);
			@out.Seek(packEnd);
			@out.Write(buf, 0, len);
			packEnd += len;
			if (def == null)
			{
				def = new Deflater(Deflater.DEFAULT_COMPRESSION, false);
			}
			else
			{
				def.Reset();
			}
			def.SetInput(data);
			def.Finish();
			while (!def.IsFinished)
			{
				len = def.Deflate(buf);
				tailDigest.Update(buf, 0, len);
				crc.Update(buf, 0, len);
				@out.Write(buf, 0, len);
				packEnd += len;
			}
			info.SetCRC((int)crc.GetValue());
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void OnEndThinPack()
		{
			byte[] tailHash = this.tailDigest.Digest();
			byte[] buf = Buffer();
			MessageDigest origDigest = Constants.NewMessageDigest();
			MessageDigest tailDigest = Constants.NewMessageDigest();
			MessageDigest packDigest = Constants.NewMessageDigest();
			long origRemaining = origEnd;
			@out.Seek(0);
			@out.ReadFully(buf, 0, 12);
			origDigest.Update(buf, 0, 12);
			origRemaining -= 12;
			NB.EncodeInt32(buf, 8, GetObjectCount());
			@out.Seek(0);
			@out.Write(buf, 0, 12);
			packDigest.Update(buf, 0, 12);
			for (; ; )
			{
				int n = @out.Read(buf);
				if (n < 0)
				{
					break;
				}
				if (origRemaining != 0)
				{
					int origCnt = (int)Math.Min(n, origRemaining);
					origDigest.Update(buf, 0, origCnt);
					origRemaining -= origCnt;
					if (origRemaining == 0)
					{
						tailDigest.Update(buf, origCnt, n - origCnt);
					}
				}
				else
				{
					tailDigest.Update(buf, 0, n);
				}
				packDigest.Update(buf, 0, n);
			}
			if (!Arrays.Equals(origDigest.Digest(), origHash) || !Arrays.Equals(tailDigest.Digest
				(), tailHash))
			{
				throw new IOException(JGitText.Get().packCorruptedWhileWritingToFilesystem);
			}
			packHash = packDigest.Digest();
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void WriteIdx()
		{
			IList<PackedObjectInfo> list = GetSortedObjectList(null);
			FileOutputStream os = new FileOutputStream(tmpIdx);
			try
			{
				PackIndexWriter iw;
				if (indexVersion <= 0)
				{
					iw = PackIndexWriter.CreateOldestPossible(os, list);
				}
				else
				{
					iw = PackIndexWriter.CreateVersion(os, indexVersion);
				}
				iw.Write(list, packHash);
				os.GetChannel().Force(true);
			}
			finally
			{
				os.Close();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private PackLock RenameAndOpenPack(string lockMessage)
		{
			if (!keepEmpty && GetObjectCount() == 0)
			{
				CleanupTemporaryFiles();
				return null;
			}
			MessageDigest d = Constants.NewMessageDigest();
			byte[] oeBytes = new byte[Constants.OBJECT_ID_LENGTH];
			for (int i = 0; i < GetObjectCount(); i++)
			{
				PackedObjectInfo oe = GetObject(i);
				oe.CopyRawTo(oeBytes, 0);
				d.Update(oeBytes);
			}
			string name = ObjectId.FromRaw(d.Digest()).Name;
			FilePath packDir = new FilePath(db.GetDirectory(), "pack");
			FilePath finalPack = new FilePath(packDir, "pack-" + name + ".pack");
			FilePath finalIdx = new FilePath(packDir, "pack-" + name + ".idx");
			PackLock keep = new PackLock(finalPack, db.GetFS());
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
			if (!tmpPack.RenameTo(finalPack))
			{
				CleanupTemporaryFiles();
				keep.Unlock();
				throw new IOException(MessageFormat.Format(JGitText.Get().cannotMovePackTo, finalPack
					));
			}
			if (!tmpIdx.RenameTo(finalIdx))
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
				newPack = db.OpenPack(finalPack, finalIdx);
			}
			catch (IOException err)
			{
				keep.Unlock();
				if (finalPack.Exists())
				{
					FileUtils.Delete(finalPack);
				}
				if (finalIdx.Exists())
				{
					FileUtils.Delete(finalIdx);
				}
				throw;
			}
			return lockMessage != null ? keep : null;
		}
	}
}
