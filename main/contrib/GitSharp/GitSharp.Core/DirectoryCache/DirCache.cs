/*
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
using System.Linq;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core.DirectoryCache
{
    /// <summary>
    /// Support for the Git dircache (aka index file).
    /// <para />
    /// The index file keeps track of which objects are currently checked out in the
    /// working directory, and the last modified time of those working files. Changes
    /// in the working directory can be detected by comparing the modification times
    /// to the cached modification time within the index file.
    /// <para />
    /// Index files are also used during merges, where the merge happens within the
    /// index file first, and the working directory is updated as a post-merge step.
    /// Conflicts are stored in the index file to allow tool (and human) based
    /// resolutions to be easily performed.
    /// </summary>
    public class DirCache
    {
        private static readonly byte[] SigDirc = { (byte)'D', (byte)'I', (byte)'R', (byte)'C' };
        private static readonly DirCacheEntry[] NoEntries = { };
        private const int ExtTree = 0x54524545 /* 'TREE' */;
        private const int InfoLen = DirCacheEntry.INFO_LEN;

        internal static readonly Comparison<DirCacheEntry> EntryComparer = (o1, o2) =>
        {
            int cr = Compare(o1, o2);
            if (cr != 0)
            {
                return cr;
            }
            return o1.getStage() - o2.getStage();
        };

        public static int Compare(DirCacheEntry a, DirCacheEntry b)
        {
            return Compare(a.Path, a.Path.Length, b);
        }

        private static int Compare(byte[] aPath, int aLen, DirCacheEntry b)
        {
            return Compare(aPath, aLen, b.Path, b.Path.Length);
        }

        public static int Compare(byte[] aPath, int aLen, byte[] bPath, int bLen)
        {
            for (int cPos = 0; cPos < aLen && cPos < bLen; cPos++)
            {
                int cmp = (aPath[cPos] & 0xff) - (bPath[cPos] & 0xff);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return aLen - bLen;
        }

        ///	<summary>
        /// Create a new empty index which is never stored on disk.
        ///	</summary>
        ///	<returns>
        /// An empty cache which has no backing store file. The cache may not
        /// be read or written, but it may be queried and updated (in memory).
        /// </returns>
        public static DirCache newInCore()
        {
            return new DirCache(null);
        }

        ///	<summary>
        /// Create a new in-core index representation and read an index from disk.
        ///	<para />
        ///	The new index will be read before it is returned to the caller. Read
        /// failures are reported as exceptions and therefore prevent the method from
        /// returning a partially populated index.
        /// </summary>
        /// <param name="indexLocation">Location of the index file on disk.</param>
        /// <returns> a cache representing the contents of the specified index file (if
        /// it exists) or an empty cache if the file does not exist.
        /// </returns>
        /// <exception cref="IOException">
        /// The index file is present but could not be read.
        /// </exception>
        /// <exception cref="CorruptObjectException">
        /// The index file is using a format or extension that this
        /// library does not support.
        /// </exception>
        public static DirCache read(FileInfo indexLocation)
        {
            var c = new DirCache(indexLocation);
            c.read();
            return c;
        }

        ///	<summary>
        /// Create a new in-core index representation and read an index from disk.
        /// <para />
        /// The new index will be read before it is returned to the caller. Read
        /// failures are reported as exceptions and therefore prevent the method from
        /// returning a partially populated index.
        /// </summary>
        /// <param name="db">
        /// repository the caller wants to read the default index of.
        /// </param>
        /// <returns>
        /// A cache representing the contents of the specified index file (if
        /// it exists) or an empty cache if the file does not exist.
        /// </returns>
        /// <exception cref="IOException">
        /// The index file is present but could not be read.
        /// </exception>
        /// <exception cref="CorruptObjectException">
        /// The index file is using a format or extension that this
        /// library does not support.
        /// </exception>
        public static DirCache read(Repository db)
        {
            return read(new FileInfo(db.Directory + "/index"));
        }

        ///	<summary>
        /// Create a new in-core index representation, lock it, and read from disk.
        /// <para />
        /// The new index will be locked and then read before it is returned to the
        /// caller. Read failures are reported as exceptions and therefore prevent
        /// the method from returning a partially populated index.  On read failure,
        /// the lock is released.
        /// </summary>
        /// <param name="indexLocation">
        /// location of the index file on disk.
        /// </param>
        /// <returns>
        /// A cache representing the contents of the specified index file (if
        /// it exists) or an empty cache if the file does not exist.
        /// </returns>
        /// <exception cref="IOException">
        /// The index file is present but could not be read, or the lock
        /// could not be obtained.
        /// </exception>
        /// <exception cref="CorruptObjectException">
        /// the index file is using a format or extension that this
        /// library does not support.
        /// </exception>
        public static DirCache Lock(FileInfo indexLocation)
        {
            var c = new DirCache(indexLocation);
            if (!c.Lock())
            {
                throw new IOException("Cannot lock " + indexLocation);
            }

            try
            {
                c.read();
            }
            catch (Exception)
            {
                c.unlock();
                throw;
            }

            return c;
        }

        ///	<summary>
        /// Create a new in-core index representation, lock it, and read from disk.
        ///	<para />
        ///	The new index will be locked and then read before it is returned to the
        ///	caller. Read failures are reported as exceptions and therefore prevent
        ///	the method from returning a partially populated index.
        ///	</summary>
        ///	<param name="db">
        /// Repository the caller wants to read the default index of.
        /// </param>
        /// <returns>
        /// A cache representing the contents of the specified index file (if
        /// it exists) or an empty cache if the file does not exist.
        /// </returns>
        /// <exception cref="IOException">
        /// The index file is present but could not be read, or the lock
        /// could not be obtained.
        /// </exception>
        /// <exception cref="CorruptObjectException">
        /// The index file is using a format or extension that this
        /// library does not support.
        /// </exception>
        public static DirCache Lock(Repository db)
        {
            return Lock(new FileInfo(db.Directory + "/index"));
        }

        // Location of the current version of the index file.
        private readonly FileInfo _liveFile;

        // Modification time of the file at the last Read/write we did.
        private long _lastModified;

        // Individual file index entries, sorted by path name.
        private DirCacheEntry[] _sortedEntries;

        // Number of positions within sortedEntries that are valid.
        private int _entryCnt;

        // Cache tree for this index; null if the cache tree is not available.
        private DirCacheTree _cacheTree;

        // Our active lock (if we hold it); null if we don't have it locked.
        private LockFile _myLock;

        ///	<summary>
        /// Create a new in-core index representation.
        ///	<para />
        ///	The new index will be empty. Callers may wish to read from the on disk
        ///	file first with <seealso cref="read()"/>.
        ///	</summary>
        ///	<param name="indexLocation">location of the index file on disk. </param>
        public DirCache(FileInfo indexLocation)
        {
            _liveFile = indexLocation;
            clear();
        }

        ///	<summary>
        /// Create a new builder to update this cache.
        /// <para />
        /// Callers should add all entries to the builder, then use
        /// <seealso cref="DirCacheBuilder.finish()"/> to update this instance.
        /// </summary>
        /// <returns>A new builder instance for this cache.</returns>
        public DirCacheBuilder builder()
        {
            return new DirCacheBuilder(this, _entryCnt + 16);
        }

        /// <summary>
        /// Create a new editor to recreate this cache.
        /// <para />
        /// Callers should add commands to the editor, then use
        /// <seealso cref="DirCacheEditor.finish()"/> to update this instance.
        ///	</summary>
        ///	<returns>A new builder instance for this cache.</returns>
        public DirCacheEditor editor()
        {
            return new DirCacheEditor(this, _entryCnt + 16);
        }

        public void replace(DirCacheEntry[] e, int cnt)
        {
            _sortedEntries = e;
            _entryCnt = cnt;
            _cacheTree = null;
        }

        /// <summary>
        /// Read the index from disk, if it has changed on disk.
        /// <para />
        /// This method tries to avoid loading the index if it has not changed since
        /// the last time we consulted it. A missing index file will be treated as
        /// though it were present but had no file entries in it.
        /// </summary>
        /// <exception cref="IOException">
        /// The index file is present but could not be read. This
        /// <see cref="DirCache"/> instance may not be populated correctly.
        /// </exception>
        /// <exception cref="CorruptObjectException">
        /// The index file is using a format or extension that this
        /// library does not support.
        /// </exception>
        public void read()
        {
            if (_liveFile == null)
            {
                throw new IOException("DirCache does not have a backing file");
            }
            if (!_liveFile.Exists)
            {
                clear();
            }
            else if (_liveFile.lastModified() != _lastModified)
            {
                try
                {
                    var inStream = new FileStream(_liveFile.FullName, System.IO.FileMode.Open, FileAccess.Read);
                    try
                    {
                        clear();
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
                            // Ignore any close failures.
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // Someone must have deleted it between our exists test
                    // and actually opening the path. That's fine, its empty.
                    //
                    clear();
                }
            }
        }

        /// <summary>
        /// Empty this index, removing all entries.
        /// </summary>
        public void clear()
        {
            _lastModified = 0;
            _sortedEntries = NoEntries;
            _entryCnt = 0;
            _cacheTree = null;
        }

        private void ReadFrom(Stream inStream)
        {
            var @in = new StreamReader(inStream);
            MessageDigest md = Constants.newMessageDigest();

            // Read the index header and verify we understand it.
            //
            var hdr = new byte[20];
            IO.ReadFully(inStream, hdr, 0, 12);
            md.Update(hdr, 0, 12);
            if (!IsDIRC(hdr))
            {
                throw new CorruptObjectException("Not a DIRC file.");
            }

            int ver = NB.DecodeInt32(hdr, 4);
            if (ver != 2)
            {
                throw new CorruptObjectException("Unknown DIRC version " + ver);
            }

            _entryCnt = NB.DecodeInt32(hdr, 8);
            if (_entryCnt < 0)
            {
                throw new CorruptObjectException("DIRC has too many entries.");
            }

            // Load the individual file entries.
            //
            var infos = new byte[InfoLen * _entryCnt];
            _sortedEntries = new DirCacheEntry[_entryCnt];
            for (int i = 0; i < _entryCnt; i++)
            {
                _sortedEntries[i] = new DirCacheEntry(infos, i * InfoLen, inStream, md);
            }
            _lastModified = _liveFile.lastModified();

            // After the file entries are index extensions, and then a footer.
            //
            while (true)
            {
                var pos = inStream.Position;
                IO.ReadFully(inStream, hdr, 0, 20);

                if (inStream.ReadByte() < 0)
                {
                    // No extensions present; the file ended where we expected.
                    //
                    break;
                }
                inStream.Seek(pos, SeekOrigin.Begin);
                md.Update(hdr, 0, 8);
                IO.skipFully(inStream, 8);

                long sz = NB.decodeUInt32(hdr, 4);

                switch (NB.DecodeInt32(hdr, 0))
                {
                    case ExtTree:
                        if (int.MaxValue < sz)
                        {
                            throw new CorruptObjectException("DIRC extension "
                                        + formatExtensionName(hdr) + " is too large at "
                                        + sz + " bytes.");
                        }
                        byte[] raw = new byte[(int)sz];
                        IO.ReadFully(inStream, raw, 0, raw.Length);
                        md.Update(raw, 0, raw.Length);
                        _cacheTree = new DirCacheTree(raw, new MutableInteger(), null);
                        break;

                    default:
                        if (hdr[0] >= (byte)'A' && hdr[0] <= (byte)'Z')
                        {
                            // The extension is optional and is here only as
                            // a performance optimization. Since we do not
                            // understand it, we can safely skip past it, after
                            // we include its data in our checksum.
                            //
                            skipOptionalExtension(inStream, md, hdr, sz);
                        }
                        else
                        {
                            // The extension is not an optimization and is
                            // _required_ to understand this index format.
                            // Since we did not trap it above we must abort.
                            //
                            throw new CorruptObjectException("DIRC extension "
                                    + formatExtensionName(hdr)
                                    + " not supported by this version.");
                        }

                        break;
                }
            }

            byte[] exp = md.Digest();
            if (!exp.SequenceEqual(hdr))
            {
                throw new CorruptObjectException("DIRC checksum mismatch");
            }
        }

        private void skipOptionalExtension(Stream inStream, MessageDigest md, byte[] hdr, long sz)
        {
            byte[] b = new byte[4096];
            while (0 < sz)
            {
                int n = inStream.Read(b, 0, (int)Math.Min(b.Length, sz));
                if (n < 0)
                {
                    throw new EndOfStreamException("Short read of optional DIRC extension "
                            + formatExtensionName(hdr) + "; expected another " + sz
                            + " bytes within the section.");
                }
                md.Update(b, 0, n);
                sz -= n;
            }
        }

        private static String formatExtensionName(byte[] hdr)
        {
            return "'" + Charset.forName("ISO-8859-1").GetString(hdr, 0, 4) + "'";
        }

        private static bool IsDIRC(byte[] header)
        {
            if (header.Length < SigDirc.Length)
            {
                return false;
            }

            for (int i = 0; i < SigDirc.Length; i++)
            {
                if (header[i] != SigDirc[i]) return false;
            }

            return true;
        }

        /// <summary>
        /// Try to establish an update lock on the cache file.
        /// </summary>
        /// <returns>
        /// True if the lock is now held by the caller; false if it is held
        /// by someone else.
        /// </returns>
        /// <exception cref="IOException">
        /// The output file could not be created. The caller does not
        /// hold the lock.
        /// </exception>
        public bool Lock()
        {
            if (_liveFile == null)
            {
                throw new IOException("DirCache does not have a backing file");
            }

            _myLock = new LockFile(_liveFile);
            if (_myLock.Lock())
            {
                _myLock.NeedStatInformation = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Write the entry records from memory to disk.
        /// <para />
        /// The cache must be locked first by calling <seealso cref="Lock()"/> and receiving
        /// true as the return value. Applications are encouraged to lock the index,
        /// then invoke <seealso cref="read()"/> to ensure the in-memory data is current,
        /// prior to updating the in-memory entries.
        /// <para />
        /// Once written the lock is closed and must be either committed with
        /// <seealso cref="commit()"/> or rolled back with <seealso cref="unlock()"/>.
        /// </summary>
        /// <exception cref="IOException">
        /// The output file could not be created. The caller no longer
        /// holds the lock.
        /// </exception>
        public void write()
        {
            LockFile tmp = _myLock;
            RequireLocked(tmp);
            try
            {
                WriteTo(tmp.GetOutputStream());
            }
            catch (Exception)
            {
                tmp.Unlock();
                throw;
            }
        }

        private void WriteTo(Stream os)
        {
            MessageDigest foot = Constants.newMessageDigest();
            var dos = new DigestOutputStream(os, foot);

            // Write the header.
            //
            var tmp = new byte[128];
            Array.Copy(SigDirc, 0, tmp, 0, SigDirc.Length);
            NB.encodeInt32(tmp, 4, /* version */2);
            NB.encodeInt32(tmp, 8, _entryCnt);
            dos.Write(tmp, 0, 12);

            // Write the individual file entries.
            //
            if (_lastModified <= 0) 
            {
                // Write a new index, as no entries require smudging.
                //
                for (int i = 0; i < _entryCnt; i++)
                {
                    _sortedEntries[i].write(dos);
                }
            }
            else
            {
                int smudge_s = (int)(_lastModified / 1000);
                int smudge_ns = ((int)(_lastModified % 1000)) * 1000000;
                for (int i = 0; i < _entryCnt; i++)
                {
                    DirCacheEntry e = _sortedEntries[i];
                    if (e.mightBeRacilyClean(smudge_s, smudge_ns))
                        e.smudgeRacilyClean();
                    e.write(dos);
                }
            }

            if (_cacheTree != null)
            {
                var bb = new LocalFileBuffer();
                _cacheTree.write(tmp, bb);
                bb.close();

                NB.encodeInt32(tmp, 0, ExtTree);
                NB.encodeInt32(tmp, 4, (int)bb.Length);
                dos.Write(tmp, 0, 8);
                bb.writeTo(dos, null);
            }
            var hash = foot.Digest();
            os.Write(hash, 0, hash.Length);
            os.Close();
        }

        ///	<summary>
        /// Commit this change and release the lock.
        /// <para />
        /// If this method fails (returns false) the lock is still released.
        /// </summary>
        /// <returns>
        /// True if the commit was successful and the file contains the new
        /// data; false if the commit failed and the file remains with the
        /// old data.
        /// </returns>
        ///	<exception cref="InvalidOperationException">
        /// the lock is not held.
        /// </exception>
        public bool commit()
        {
            LockFile tmp = _myLock;
            RequireLocked(tmp);
            _myLock = null;
            if (!tmp.Commit()) return false;
            _lastModified = tmp.CommitLastModified;
            return true;
        }

        private void RequireLocked(LockFile tmp)
        {
            if (_liveFile == null)
            {
                throw new InvalidOperationException("DirCache is not locked");
            }
            if (tmp == null)
            {
                throw new InvalidOperationException("DirCache " + _liveFile.FullName + " not locked.");
            }
        }

        /// <summary>
        /// Unlock this file and abort this change.
        /// <para />
        /// The temporary file (if created) is deleted before returning.
        /// </summary>
        public void unlock()
        {
            LockFile tmp = _myLock;
            if (tmp != null)
            {
                _myLock = null;
                tmp.Unlock();
            }
        }

        /// <summary>
        /// Locate the position a path's entry is at in the index.
        /// <para />
        /// If there is at least one entry in the index for this path the position of
        /// the lowest stage is returned. Subsequent stages can be identified by
        /// testing consecutive entries until the path differs.
        /// <para />
        /// If no path matches the entry -(position+1) is returned, where position is
        /// the location it would have gone within the index.
        /// </summary>
        /// <param name="path">The path to search for.</param>
        /// <returns>
        /// if >= 0 then the return value is the position of the entry in the
        /// index; pass to <seealso cref="getEntry(int)"/> to obtain the entry
        /// information. If &gt; 0 the entry does not exist in the index.
        /// </returns>
        ///
        public int findEntry(string path)
        {
            if (_entryCnt == 0)
                return -1;
            byte[] p = Constants.encode(path);
            return findEntry(p, p.Length);
        }

        public int findEntry(byte[] p, int pLen)
        {
            int low = 0;
            int high = _entryCnt;

            while (low < high)
            {
                var mid = (int)(((uint)(low + high)) >> 1);
                int cmp = Compare(p, pLen, _sortedEntries[mid]);
                if (cmp < 0)
                {
                    high = mid;
                }
                else if (cmp == 0)
                {
                    while (mid > 0 && Compare(p, pLen, _sortedEntries[mid - 1]) == 0)
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

            return -(low + 1);
        }

        /// <summary>
        /// Determine the next index position past all entries with the same name.
        /// <para />
        /// As index entries are sorted by path name, then stage number, this method
        /// advances the supplied position to the first position in the index whose
        /// path name does not match the path name of the supplied position's entry.
        ///	</summary>
        ///	<param name="position">
        /// entry position of the path that should be skipped.
        /// </param>
        /// <returns>
        /// Position of the next entry whose path is after the input.
        /// </returns>
        public int nextEntry(int position)
        {
            DirCacheEntry last = _sortedEntries[position];
            int nextIdx = position + 1;
            while (nextIdx < _entryCnt)
            {
                DirCacheEntry next = _sortedEntries[nextIdx];
                if (Compare(last, next) != 0) break;
                last = next;
                nextIdx++;
            }
            return nextIdx;
        }

        public int nextEntry(byte[] p, int pLen, int nextIdx)
        {
            while (nextIdx < _entryCnt)
            {
                DirCacheEntry next = _sortedEntries[nextIdx];
                if (!DirCacheTree.peq(p, next.Path, pLen)) break;
                nextIdx++;
            }
            return nextIdx;
        }

        /// <summary>
        /// Total number of file entries stored in the index.
        /// <para />
        /// This count includes unmerged stages for a file entry if the file is
        /// currently conflicted in a merge. This means the total number of entries
        /// in the index may be up to 3 times larger than the number of files in the
        /// working directory.
        /// <para />
        /// Note that this value counts only <i>files</i>.
        /// </summary>
        /// <returns>Number of entries available.</returns>
        /// <seealso cref="getEntry(int)"/>
        public int getEntryCount()
        {
            return _entryCnt;
        }

        /// <summary>
        /// Get a specific entry.
        /// </summary>
        /// <param name="i">
        /// position of the entry to get.
        /// </param>
        ///	<returns> The entry at position <paramref name="i"/>.</returns>
        public DirCacheEntry getEntry(int i)
        {
            return _sortedEntries[i];
        }

        /// <summary>
        /// Get a specific entry.
        /// </summary>
        /// <param name="path">The path to search for.</param>
        ///	<returns>The entry at position <paramref name="i"/>.</returns>
        public DirCacheEntry getEntry(string path)
        {
            int i = findEntry(path);
            return i < 0 ? null : _sortedEntries[i];
        }

        /// <summary>
        /// Recursively get all entries within a subtree.
        /// </summary>
        /// <param name="path">
        /// The subtree path to get all entries within.
        /// </param>
        /// <returns>
        /// All entries recursively contained within the subtree.
        /// </returns>
        public DirCacheEntry[] getEntriesWithin(string path)
        {
            if (!path.EndsWith("/"))
            {
                path += "/";
            }

            byte[] p = Constants.encode(path);
            int pLen = p.Length;

            int eIdx = findEntry(p, pLen);
            if (eIdx < 0)
            {
                eIdx = -(eIdx + 1);
            }
            int lastIdx = nextEntry(p, pLen, eIdx);
            var r = new DirCacheEntry[lastIdx - eIdx];
            Array.Copy(_sortedEntries, eIdx, r, 0, r.Length);
            return r;
        }

        public void toArray(int i, DirCacheEntry[] dst, int off, int cnt)
        {
            Array.Copy(_sortedEntries, i, dst, off, cnt);
        }

        /// <summary>
        /// Obtain (or build) the current cache tree structure.
        /// <para />
        /// This method can optionally recreate the cache tree, without flushing the
        /// tree objects themselves to disk.
        /// </summary>
        ///	<param name="build">
        /// If true and the cache tree is not present in the index it will
        /// be generated and returned to the caller.
        /// </param>
        ///	<returns>
        /// The cache tree; null if there is no current cache tree available
        /// and <paramref name="build"/> was false.
        /// </returns>
        public DirCacheTree getCacheTree(bool build)
        {
            if (build)
            {
                if (_cacheTree == null)
                {
                    _cacheTree = new DirCacheTree();
                }
                _cacheTree.validate(_sortedEntries, _entryCnt, 0, 0);
            }
            return _cacheTree;
        }

        ///	<summary>
        /// Write all index trees to the object store, returning the root tree.
        ///	</summary>
        ///	<param name="ow">
        /// The writer to use when serializing to the store.
        /// </param>
        ///	<returns> identity for the root tree. </returns>
        ///	<exception cref="UnmergedPathException">
        /// One or more paths contain higher-order stages (stage > 0),
        /// which cannot be stored in a tree object.
        /// </exception>
        ///	<exception cref="InvalidOperationException">
        /// One or more paths contain an invalid mode which should never
        /// appear in a tree object.
        /// </exception>
        ///	<exception cref="IOException">
        /// An unexpected error occurred writing to the object store.
        /// </exception>
        public ObjectId writeTree(ObjectWriter ow)
        {
            return getCacheTree(true).writeTree(_sortedEntries, 0, 0, ow);
        }
    }
}