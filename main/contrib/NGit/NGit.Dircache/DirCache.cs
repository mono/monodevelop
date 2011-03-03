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
using NGit;
using NGit.Dircache;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>Support for the Git dircache (aka index file).</summary>
	/// <remarks>
	/// Support for the Git dircache (aka index file).
	/// <p>
	/// The index file keeps track of which objects are currently checked out in the
	/// working directory, and the last modified time of those working files. Changes
	/// in the working directory can be detected by comparing the modification times
	/// to the cached modification time within the index file.
	/// <p>
	/// Index files are also used during merges, where the merge happens within the
	/// index file first, and the working directory is updated as a post-merge step.
	/// Conflicts are stored in the index file to allow tool (and human) based
	/// resolutions to be easily performed.
	/// </remarks>
	public class DirCache
	{
		private static readonly byte[] SIG_DIRC = new byte[] { (byte)('D'), (byte)('I'), 
			(byte)('R'), (byte)('C') };

		private const int EXT_TREE = unchecked((int)(0x54524545));

		private static readonly DirCacheEntry[] NO_ENTRIES = new DirCacheEntry[] {  };

		private sealed class _IComparer_96 : IComparer<DirCacheEntry>
		{
			public _IComparer_96()
			{
			}

			public int Compare(DirCacheEntry o1, DirCacheEntry o2)
			{
				int cr = NGit.Dircache.DirCache.Cmp(o1, o2);
				if (cr != 0)
				{
					return cr;
				}
				return o1.Stage - o2.Stage;
			}
		}

		internal static readonly IComparer<DirCacheEntry> ENT_CMP = new _IComparer_96();

		internal static int Cmp(DirCacheEntry a, DirCacheEntry b)
		{
			return Cmp(a.path, a.path.Length, b);
		}

		internal static int Cmp(byte[] aPath, int aLen, DirCacheEntry b)
		{
			return Cmp(aPath, aLen, b.path, b.path.Length);
		}

		internal static int Cmp(byte[] aPath, int aLen, byte[] bPath, int bLen)
		{
			for (int cPos = 0; cPos < aLen && cPos < bLen; cPos++)
			{
				int cmp = (aPath[cPos] & unchecked((int)(0xff))) - (bPath[cPos] & unchecked((int)
					(0xff)));
				if (cmp != 0)
				{
					return cmp;
				}
			}
			return aLen - bLen;
		}

		/// <summary>Create a new empty index which is never stored on disk.</summary>
		/// <remarks>Create a new empty index which is never stored on disk.</remarks>
		/// <returns>
		/// an empty cache which has no backing store file. The cache may not
		/// be read or written, but it may be queried and updated (in
		/// memory).
		/// </returns>
		public static NGit.Dircache.DirCache NewInCore()
		{
			return new NGit.Dircache.DirCache(null, null);
		}

		/// <summary>Create a new in-core index representation and read an index from disk.</summary>
		/// <remarks>
		/// Create a new in-core index representation and read an index from disk.
		/// <p>
		/// The new index will be read before it is returned to the caller. Read
		/// failures are reported as exceptions and therefore prevent the method from
		/// returning a partially populated index.
		/// </remarks>
		/// <param name="indexLocation">location of the index file on disk.</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		/// <returns>
		/// a cache representing the contents of the specified index file (if
		/// it exists) or an empty cache if the file does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">the index file is present but could not be read.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the index file is using a format or extension that this
		/// library does not support.
		/// </exception>
		public static NGit.Dircache.DirCache Read(FilePath indexLocation, FS fs)
		{
			NGit.Dircache.DirCache c = new NGit.Dircache.DirCache(indexLocation, fs);
			c.Read();
			return c;
		}

		/// <summary>Create a new in-core index representation, lock it, and read from disk.</summary>
		/// <remarks>
		/// Create a new in-core index representation, lock it, and read from disk.
		/// <p>
		/// The new index will be locked and then read before it is returned to the
		/// caller. Read failures are reported as exceptions and therefore prevent
		/// the method from returning a partially populated index. On read failure,
		/// the lock is released.
		/// </remarks>
		/// <param name="indexLocation">location of the index file on disk.</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		/// <returns>
		/// a cache representing the contents of the specified index file (if
		/// it exists) or an empty cache if the file does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the index file is present but could not be read, or the lock
		/// could not be obtained.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the index file is using a format or extension that this
		/// library does not support.
		/// </exception>
		public static NGit.Dircache.DirCache Lock(FilePath indexLocation, FS fs)
		{
			NGit.Dircache.DirCache c = new NGit.Dircache.DirCache(indexLocation, fs);
			if (!c.Lock())
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().cannotLock, indexLocation
					));
			}
			try
			{
				c.Read();
			}
			catch (IOException e)
			{
				c.Unlock();
				throw;
			}
			catch (RuntimeException e)
			{
				c.Unlock();
				throw;
			}
			catch (Error e)
			{
				c.Unlock();
				throw;
			}
			return c;
		}

		/// <summary>Location of the current version of the index file.</summary>
		/// <remarks>Location of the current version of the index file.</remarks>
		private readonly FilePath liveFile;

		/// <summary>Modification time of the file at the last read/write we did.</summary>
		/// <remarks>Modification time of the file at the last read/write we did.</remarks>
		private long lastModified;

		/// <summary>Individual file index entries, sorted by path name.</summary>
		/// <remarks>Individual file index entries, sorted by path name.</remarks>
		private DirCacheEntry[] sortedEntries;

		/// <summary>
		/// Number of positions within
		/// <see cref="sortedEntries">sortedEntries</see>
		/// that are valid.
		/// </summary>
		private int entryCnt;

		/// <summary>Cache tree for this index; null if the cache tree is not available.</summary>
		/// <remarks>Cache tree for this index; null if the cache tree is not available.</remarks>
		private DirCacheTree tree;

		/// <summary>Our active lock (if we hold it); null if we don't have it locked.</summary>
		/// <remarks>Our active lock (if we hold it); null if we don't have it locked.</remarks>
		private LockFile myLock;

		/// <summary>file system abstraction</summary>
		private readonly FS fs;

		/// <summary>Create a new in-core index representation.</summary>
		/// <remarks>
		/// Create a new in-core index representation.
		/// <p>
		/// The new index will be empty. Callers may wish to read from the on disk
		/// file first with
		/// <see cref="Read()">Read()</see>
		/// .
		/// </remarks>
		/// <param name="indexLocation">location of the index file on disk.</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		public DirCache(FilePath indexLocation, FS fs)
		{
			liveFile = indexLocation;
			this.fs = fs;
			Clear();
		}

		/// <summary>Create a new builder to update this cache.</summary>
		/// <remarks>
		/// Create a new builder to update this cache.
		/// <p>
		/// Callers should add all entries to the builder, then use
		/// <see cref="DirCacheBuilder.Finish()">DirCacheBuilder.Finish()</see>
		/// to update this instance.
		/// </remarks>
		/// <returns>a new builder instance for this cache.</returns>
		public virtual DirCacheBuilder Builder()
		{
			return new DirCacheBuilder(this, entryCnt + 16);
		}

		/// <summary>Create a new editor to recreate this cache.</summary>
		/// <remarks>
		/// Create a new editor to recreate this cache.
		/// <p>
		/// Callers should add commands to the editor, then use
		/// <see cref="DirCacheEditor.Finish()">DirCacheEditor.Finish()</see>
		/// to update this instance.
		/// </remarks>
		/// <returns>a new builder instance for this cache.</returns>
		public virtual DirCacheEditor Editor()
		{
			return new DirCacheEditor(this, entryCnt + 16);
		}

		internal virtual void Replace(DirCacheEntry[] e, int cnt)
		{
			sortedEntries = e;
			entryCnt = cnt;
			tree = null;
		}

		/// <summary>Read the index from disk, if it has changed on disk.</summary>
		/// <remarks>
		/// Read the index from disk, if it has changed on disk.
		/// <p>
		/// This method tries to avoid loading the index if it has not changed since
		/// the last time we consulted it. A missing index file will be treated as
		/// though it were present but had no file entries in it.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// the index file is present but could not be read. This
		/// DirCache instance may not be populated correctly.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the index file is using a format or extension that this
		/// library does not support.
		/// </exception>
		public virtual void Read()
		{
			if (liveFile == null)
			{
				throw new IOException(JGitText.Get().dirCacheDoesNotHaveABackingFile);
			}
			if (!liveFile.Exists())
			{
				Clear();
			}
			else
			{
				if (liveFile.LastModified() != lastModified)
				{
					try
					{
						FileInputStream inStream = new FileInputStream(liveFile);
						try
						{
							Clear();
							ReadFrom(inStream);
						}
						finally
						{
							try
							{
								inStream.Close();
							}
							catch (IOException)
							{
							}
						}
					}
					catch (FileNotFoundException)
					{
						// Ignore any close failures.
						// Someone must have deleted it between our exists test
						// and actually opening the path. That's fine, its empty.
						//
						Clear();
					}
				}
			}
		}

		/// <returns>true if the memory state differs from the index file</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool IsOutdated()
		{
			if (liveFile == null || !liveFile.Exists())
			{
				return false;
			}
			return liveFile.LastModified() != lastModified;
		}

		/// <summary>Empty this index, removing all entries.</summary>
		/// <remarks>Empty this index, removing all entries.</remarks>
		public virtual void Clear()
		{
			lastModified = 0;
			sortedEntries = NO_ENTRIES;
			entryCnt = 0;
			tree = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		private void ReadFrom(InputStream inStream)
		{
			BufferedInputStream @in = new BufferedInputStream(inStream);
			MessageDigest md = Constants.NewMessageDigest();
			// Read the index header and verify we understand it.
			//
			byte[] hdr = new byte[20];
			IOUtil.ReadFully(@in, hdr, 0, 12);
			md.Update(hdr, 0, 12);
			if (!Is_DIRC(hdr))
			{
				throw new CorruptObjectException(JGitText.Get().notADIRCFile);
			}
			int ver = NB.DecodeInt32(hdr, 4);
			bool extended = false;
			if (ver == 3)
			{
				extended = true;
			}
			else
			{
				if (ver != 2)
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().unknownDIRCVersion
						, ver));
				}
			}
			entryCnt = NB.DecodeInt32(hdr, 8);
			if (entryCnt < 0)
			{
				throw new CorruptObjectException(JGitText.Get().DIRCHasTooManyEntries);
			}
			// Load the individual file entries.
			//
			int infoLength = DirCacheEntry.GetMaximumInfoLength(extended);
			byte[] infos = new byte[infoLength * entryCnt];
			sortedEntries = new DirCacheEntry[entryCnt];
			MutableInteger infoAt = new MutableInteger();
			for (int i = 0; i < entryCnt; i++)
			{
				sortedEntries[i] = new DirCacheEntry(infos, infoAt, @in, md);
			}
			lastModified = liveFile.LastModified();
			// After the file entries are index extensions, and then a footer.
			//
			for (; ; )
			{
				@in.Mark(21);
				IOUtil.ReadFully(@in, hdr, 0, 20);
				if (@in.Read() < 0)
				{
					// No extensions present; the file ended where we expected.
					//
					break;
				}
				@in.Reset();
				md.Update(hdr, 0, 8);
				IOUtil.SkipFully(@in, 8);
				long sz = NB.DecodeUInt32(hdr, 4);
				switch (NB.DecodeInt32(hdr, 0))
				{
					case EXT_TREE:
					{
						if (int.MaxValue < sz)
						{
							throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().DIRCExtensionIsTooLargeAt
								, FormatExtensionName(hdr), sz));
						}
						byte[] raw = new byte[(int)sz];
						IOUtil.ReadFully(@in, raw, 0, raw.Length);
						md.Update(raw, 0, raw.Length);
						tree = new DirCacheTree(raw, new MutableInteger(), null);
						break;
					}

					default:
					{
						if (hdr[0] >= 'A' && ((sbyte)hdr[0]) <= 'Z')
						{
							// The extension is optional and is here only as
							// a performance optimization. Since we do not
							// understand it, we can safely skip past it, after
							// we include its data in our checksum.
							//
							SkipOptionalExtension(@in, md, hdr, sz);
						}
						else
						{
							// The extension is not an optimization and is
							// _required_ to understand this index format.
							// Since we did not trap it above we must abort.
							//
							throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().DIRCExtensionNotSupportedByThisVersion
								, FormatExtensionName(hdr)));
						}
						break;
					}
				}
			}
			byte[] exp = md.Digest();
			if (!Arrays.Equals(exp, hdr))
			{
				throw new CorruptObjectException(JGitText.Get().DIRCChecksumMismatch);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void SkipOptionalExtension(InputStream @in, MessageDigest md, byte[] hdr, 
			long sz)
		{
			byte[] b = new byte[4096];
			while (0 < sz)
			{
				int n = @in.Read(b, 0, (int)Math.Min(b.Length, sz));
				if (n < 0)
				{
					throw new EOFException(MessageFormat.Format(JGitText.Get().shortReadOfOptionalDIRCExtensionExpectedAnotherBytes
						, FormatExtensionName(hdr), sz));
				}
				md.Update(b, 0, n);
				sz -= n;
			}
		}

		/// <exception cref="Sharpen.UnsupportedEncodingException"></exception>
		private static string FormatExtensionName(byte[] hdr)
		{
			return "'" + Sharpen.Runtime.GetStringForBytes(hdr, 0, 4, "ISO-8859-1") + "'";
		}

		private static bool Is_DIRC(byte[] hdr)
		{
			if (hdr.Length < SIG_DIRC.Length)
			{
				return false;
			}
			for (int i = 0; i < SIG_DIRC.Length; i++)
			{
				if (hdr[i] != SIG_DIRC[i])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Try to establish an update lock on the cache file.</summary>
		/// <remarks>Try to establish an update lock on the cache file.</remarks>
		/// <returns>
		/// true if the lock is now held by the caller; false if it is held
		/// by someone else.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the output file could not be created. The caller does not
		/// hold the lock.
		/// </exception>
		public virtual bool Lock()
		{
			if (liveFile == null)
			{
				throw new IOException(JGitText.Get().dirCacheDoesNotHaveABackingFile);
			}
			LockFile tmp = new LockFile(liveFile, fs);
			if (tmp.Lock())
			{
				tmp.SetNeedStatInformation(true);
				myLock = tmp;
				return true;
			}
			return false;
		}

		/// <summary>Write the entry records from memory to disk.</summary>
		/// <remarks>
		/// Write the entry records from memory to disk.
		/// <p>
		/// The cache must be locked first by calling
		/// <see cref="Lock()">Lock()</see>
		/// and receiving
		/// true as the return value. Applications are encouraged to lock the index,
		/// then invoke
		/// <see cref="Read()">Read()</see>
		/// to ensure the in-memory data is current,
		/// prior to updating the in-memory entries.
		/// <p>
		/// Once written the lock is closed and must be either committed with
		/// <see cref="Commit()">Commit()</see>
		/// or rolled back with
		/// <see cref="Unlock()">Unlock()</see>
		/// .
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// the output file could not be created. The caller no longer
		/// holds the lock.
		/// </exception>
		public virtual void Write()
		{
			LockFile tmp = myLock;
			RequireLocked(tmp);
			try
			{
				WriteTo(new BufferedOutputStream(tmp.GetOutputStream()));
			}
			catch (IOException err)
			{
				tmp.Unlock();
				throw;
			}
			catch (RuntimeException err)
			{
				tmp.Unlock();
				throw;
			}
			catch (Error err)
			{
				tmp.Unlock();
				throw;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void WriteTo(OutputStream os)
		{
			MessageDigest foot = Constants.NewMessageDigest();
			DigestOutputStream dos = new DigestOutputStream(os, foot);
			bool extended = false;
			for (int i = 0; i < entryCnt; i++)
			{
				extended |= sortedEntries[i].IsExtended;
			}
			// Write the header.
			//
			byte[] tmp = new byte[128];
			System.Array.Copy(SIG_DIRC, 0, tmp, 0, SIG_DIRC.Length);
			NB.EncodeInt32(tmp, 4, extended ? 3 : 2);
			NB.EncodeInt32(tmp, 8, entryCnt);
			dos.Write(tmp, 0, 12);
			// Write the individual file entries.
			//
			if (lastModified <= 0)
			{
				// Write a new index, as no entries require smudging.
				//
				for (int i_1 = 0; i_1 < entryCnt; i_1++)
				{
					sortedEntries[i_1].Write(dos);
				}
			}
			else
			{
				int smudge_s = (int)(lastModified / 1000);
				int smudge_ns = ((int)(lastModified % 1000)) * 1000000;
				for (int i_1 = 0; i_1 < entryCnt; i_1++)
				{
					DirCacheEntry e = sortedEntries[i_1];
					if (e.MightBeRacilyClean(smudge_s, smudge_ns))
					{
						e.SmudgeRacilyClean();
					}
					e.Write(dos);
				}
			}
			if (tree != null)
			{
				TemporaryBuffer bb = new TemporaryBuffer.LocalFile();
				tree.Write(tmp, bb);
				bb.Close();
				NB.EncodeInt32(tmp, 0, EXT_TREE);
				NB.EncodeInt32(tmp, 4, (int)bb.Length());
				dos.Write(tmp, 0, 8);
				bb.WriteTo(dos, null);
			}
			os.Write(foot.Digest());
			os.Close();
		}

		/// <summary>Commit this change and release the lock.</summary>
		/// <remarks>
		/// Commit this change and release the lock.
		/// <p>
		/// If this method fails (returns false) the lock is still released.
		/// </remarks>
		/// <returns>
		/// true if the commit was successful and the file contains the new
		/// data; false if the commit failed and the file remains with the
		/// old data.
		/// </returns>
		/// <exception cref="System.InvalidOperationException">the lock is not held.</exception>
		public virtual bool Commit()
		{
			LockFile tmp = myLock;
			RequireLocked(tmp);
			myLock = null;
			if (!tmp.Commit())
			{
				return false;
			}
			lastModified = tmp.GetCommitLastModified();
			return true;
		}

		private void RequireLocked(LockFile tmp)
		{
			if (liveFile == null)
			{
				throw new InvalidOperationException(JGitText.Get().dirCacheIsNotLocked);
			}
			if (tmp == null)
			{
				throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().dirCacheFileIsNotLocked
					, liveFile.GetAbsolutePath()));
			}
		}

		/// <summary>Unlock this file and abort this change.</summary>
		/// <remarks>
		/// Unlock this file and abort this change.
		/// <p>
		/// The temporary file (if created) is deleted before returning.
		/// </remarks>
		public virtual void Unlock()
		{
			LockFile tmp = myLock;
			if (tmp != null)
			{
				myLock = null;
				tmp.Unlock();
			}
		}

		/// <summary>Locate the position a path's entry is at in the index.</summary>
		/// <remarks>
		/// Locate the position a path's entry is at in the index.
		/// <p>
		/// If there is at least one entry in the index for this path the position of
		/// the lowest stage is returned. Subsequent stages can be identified by
		/// testing consecutive entries until the path differs.
		/// <p>
		/// If no path matches the entry -(position+1) is returned, where position is
		/// the location it would have gone within the index.
		/// </remarks>
		/// <param name="path">the path to search for.</param>
		/// <returns>
		/// if &gt;= 0 then the return value is the position of the entry in the
		/// index; pass to
		/// <see cref="GetEntry(int)">GetEntry(int)</see>
		/// to obtain the entry
		/// information. If &lt; 0 the entry does not exist in the index.
		/// </returns>
		public virtual int FindEntry(string path)
		{
			byte[] p = Constants.Encode(path);
			return FindEntry(p, p.Length);
		}

		internal virtual int FindEntry(byte[] p, int pLen)
		{
			int low = 0;
			int high = entryCnt;
			while (low < high)
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				int cmp = Cmp(p, pLen, sortedEntries[mid]);
				if (cmp < 0)
				{
					high = mid;
				}
				else
				{
					if (cmp == 0)
					{
						while (mid > 0 && Cmp(p, pLen, sortedEntries[mid - 1]) == 0)
						{
							mid--;
						}
						return mid;
					}
					else
					{
						low = mid + 1;
					}
				}
			}
			return -(low + 1);
		}

		/// <summary>Determine the next index position past all entries with the same name.</summary>
		/// <remarks>
		/// Determine the next index position past all entries with the same name.
		/// <p>
		/// As index entries are sorted by path name, then stage number, this method
		/// advances the supplied position to the first position in the index whose
		/// path name does not match the path name of the supplied position's entry.
		/// </remarks>
		/// <param name="position">entry position of the path that should be skipped.</param>
		/// <returns>position of the next entry whose path is after the input.</returns>
		public virtual int NextEntry(int position)
		{
			DirCacheEntry last = sortedEntries[position];
			int nextIdx = position + 1;
			while (nextIdx < entryCnt)
			{
				DirCacheEntry next = sortedEntries[nextIdx];
				if (Cmp(last, next) != 0)
				{
					break;
				}
				last = next;
				nextIdx++;
			}
			return nextIdx;
		}

		internal virtual int NextEntry(byte[] p, int pLen, int nextIdx)
		{
			while (nextIdx < entryCnt)
			{
				DirCacheEntry next = sortedEntries[nextIdx];
				if (!DirCacheTree.Peq(p, next.path, pLen))
				{
					break;
				}
				nextIdx++;
			}
			return nextIdx;
		}

		/// <summary>Total number of file entries stored in the index.</summary>
		/// <remarks>
		/// Total number of file entries stored in the index.
		/// <p>
		/// This count includes unmerged stages for a file entry if the file is
		/// currently conflicted in a merge. This means the total number of entries
		/// in the index may be up to 3 times larger than the number of files in the
		/// working directory.
		/// <p>
		/// Note that this value counts only <i>files</i>.
		/// </remarks>
		/// <returns>number of entries available.</returns>
		/// <seealso cref="GetEntry(int)">GetEntry(int)</seealso>
		public virtual int GetEntryCount()
		{
			return entryCnt;
		}

		/// <summary>Get a specific entry.</summary>
		/// <remarks>Get a specific entry.</remarks>
		/// <param name="i">position of the entry to get.</param>
		/// <returns>the entry at position <code>i</code>.</returns>
		public virtual DirCacheEntry GetEntry(int i)
		{
			return sortedEntries[i];
		}

		/// <summary>Get a specific entry.</summary>
		/// <remarks>Get a specific entry.</remarks>
		/// <param name="path">the path to search for.</param>
		/// <returns>the entry for the given <code>path</code>.</returns>
		public virtual DirCacheEntry GetEntry(string path)
		{
			int i = FindEntry(path);
			return i < 0 ? null : sortedEntries[i];
		}

		/// <summary>Recursively get all entries within a subtree.</summary>
		/// <remarks>Recursively get all entries within a subtree.</remarks>
		/// <param name="path">the subtree path to get all entries within.</param>
		/// <returns>all entries recursively contained within the subtree.</returns>
		public virtual DirCacheEntry[] GetEntriesWithin(string path)
		{
			if (!path.EndsWith("/"))
			{
				path += "/";
			}
			byte[] p = Constants.Encode(path);
			int pLen = p.Length;
			int eIdx = FindEntry(p, pLen);
			if (eIdx < 0)
			{
				eIdx = -(eIdx + 1);
			}
			int lastIdx = NextEntry(p, pLen, eIdx);
			DirCacheEntry[] r = new DirCacheEntry[lastIdx - eIdx];
			System.Array.Copy(sortedEntries, eIdx, r, 0, r.Length);
			return r;
		}

		internal virtual void ToArray(int i, DirCacheEntry[] dst, int off, int cnt)
		{
			System.Array.Copy(sortedEntries, i, dst, off, cnt);
		}

		/// <summary>Obtain (or build) the current cache tree structure.</summary>
		/// <remarks>
		/// Obtain (or build) the current cache tree structure.
		/// <p>
		/// This method can optionally recreate the cache tree, without flushing the
		/// tree objects themselves to disk.
		/// </remarks>
		/// <param name="build">
		/// if true and the cache tree is not present in the index it will
		/// be generated and returned to the caller.
		/// </param>
		/// <returns>
		/// the cache tree; null if there is no current cache tree available
		/// and <code>build</code> was false.
		/// </returns>
		public virtual DirCacheTree GetCacheTree(bool build)
		{
			if (build)
			{
				if (tree == null)
				{
					tree = new DirCacheTree();
				}
				tree.Validate(sortedEntries, entryCnt, 0, 0);
			}
			return tree;
		}

		/// <summary>Write all index trees to the object store, returning the root tree.</summary>
		/// <remarks>Write all index trees to the object store, returning the root tree.</remarks>
		/// <param name="ow">
		/// the writer to use when serializing to the store. The caller is
		/// responsible for flushing the inserter before trying to use the
		/// returned tree identity.
		/// </param>
		/// <returns>identity for the root tree.</returns>
		/// <exception cref="NGit.Errors.UnmergedPathException">
		/// one or more paths contain higher-order stages (stage &gt; 0),
		/// which cannot be stored in a tree object.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// one or more paths contain an invalid mode which should never
		/// appear in a tree object.
		/// </exception>
		/// <exception cref="System.IO.IOException">an unexpected error occurred writing to the object store.
		/// 	</exception>
		public virtual ObjectId WriteTree(ObjectInserter ow)
		{
			return GetCacheTree(true).WriteTree(sortedEntries, 0, 0, ow);
		}

		/// <summary>Tells whether this index contains unmerged paths.</summary>
		/// <remarks>Tells whether this index contains unmerged paths.</remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if this index contains unmerged paths. Means: at
		/// least one entry is of a stage different from 0.
		/// <code>false</code>
		/// will be returned if all entries are of stage 0.
		/// </returns>
		public virtual bool HasUnmergedPaths()
		{
			for (int i = 0; i < entryCnt; i++)
			{
				if (sortedEntries[i].Stage > 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
