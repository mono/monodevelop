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
using GitSharp.Core.Util;

namespace GitSharp.Core.DirectoryCache
{
    /// <summary>
    /// A single file (or stage of a file) in a <seealso cref="DirCache"/>.
    /// <para />
    /// An entry represents exactly one stage of a file. If a file path is unmerged
    /// then multiple DirCacheEntry instances may appear for the same path name.
    /// </summary>
    public class DirCacheEntry
    {
        private static readonly byte[] NullPad = new byte[8];

        /** The standard (fully merged) stage for an entry. */
        public const int STAGE_0 = 0;

        /** The base tree revision for an entry. */
        public const int STAGE_1 = 1;

        /** The first tree revision (usually called "ours"). */
        public const int STAGE_2 = 2;

        /** The second tree revision (usually called "theirs"). */
        public const int STAGE_3 = 3;

        // private const  int P_CTIME = 0;
        // private const  int P_CTIME_NSEC = 4;
        private const int PMtime = 8;
        // private const  int P_MTIME_NSEC = 12;
        // private const  int P_DEV = 16;
        // private const  int P_INO = 20;
        private const int PMode = 24;
        // private const  int P_UID = 28;
        // private const  int P_GID = 32;
        private const int PSize = 36;
        private const int PObjectid = 40;
        private const int PFlags = 60;

        public const int INFO_LEN = 62;

        /// <summary>
        /// Mask applied to data in <see cref="PFlags"/> to get the name Length.
        /// </summary>
        private const int NameMask = 0xfff;
        private const int AssumeValid = 0x80;

        /// <summary>
        /// (Possibly shared) header information storage.
        /// </summary>
        private readonly byte[] _info;

        /// <summary>
        /// First location within <see cref="_info"/> where our header starts.
        /// </summary>
        private readonly int _infoOffset;

        /// <summary>
        /// Our encoded path name, from the root of the repository.
        /// </summary>
        private readonly byte[] _path;

        public DirCacheEntry(byte[] sharedInfo, int infoAt, Stream @in, MessageDigest md)
        {
            _info = sharedInfo;
            _infoOffset = infoAt;

            IO.ReadFully(@in, _info, _infoOffset, INFO_LEN);
            md.Update(_info, _infoOffset, INFO_LEN);

            int pathLen = NB.decodeUInt16(_info, _infoOffset + PFlags) & NameMask;
            int skipped = 0;
            if (pathLen < NameMask)
            {
                _path = new byte[pathLen];
                IO.ReadFully(@in, _path, 0, pathLen);
                md.Update(_path, 0, pathLen);
            }
            else
            {
                var tmp = new MemoryStream();
                {
                    var buf = new byte[NameMask];
                    IO.ReadFully(@in, buf, 0, NameMask);
                    tmp.Write(buf, 0, buf.Length);
                }

                while (true)
                {
                    int c = @in.ReadByte();
                    if (c < 0)
                    {
                        throw new EndOfStreamException("Short Read of block.");
                    }

                    if (c == 0) break;

                    tmp.Write(new[] { (byte)c }, 0, 1);
                }

                _path = tmp.ToArray();
                pathLen = _path.Length;
                skipped = 1; // we already skipped 1 '\0' above to break the loop.
                md.Update(_path, 0, pathLen);
                md.Update(0);
            }

            // Index records are padded out to the next 8 byte alignment
            // for historical reasons related to how C Git Read the files.
            //
            int actLen = INFO_LEN + pathLen;
            int expLen = (actLen + 8) & ~7;
            int padLen = expLen - actLen - skipped;
            if (padLen > 0)
            {
                IO.skipFully(@in, padLen);
                md.Update(NullPad, 0, padLen);
            }
        }

        ///	<summary>
        /// Create an empty entry at stage 0.
        /// </summary>
        /// <param name="newPath">Name of the cache entry.</param>
        public DirCacheEntry(string newPath)
            : this(Constants.encode(newPath))
        {
        }

        ///	<summary>
        /// Create an empty entry at the specified stage.
        /// </summary>
        /// <param name="newPath">name of the cache entry.</param>
        /// <param name="stage">the stage index of the new entry.</param>
        public DirCacheEntry(string newPath, int stage)
            : this(Constants.encode(newPath), stage)
        {
        }

        /// <summary>
        /// Create an empty entry at stage 0.
        /// </summary>
        /// <param name="newPath">
        /// name of the cache entry, in the standard encoding.
        /// </param>
        public DirCacheEntry(byte[] newPath)
            : this(newPath, STAGE_0)
        {
        }

        /// <summary>
        /// Create an empty entry at the specified stage.
        /// </summary>
        /// <param name="newPath">
        /// Name of the cache entry, in the standard encoding.
        /// </param>
        /// <param name="stage">The stage index of the new entry.</param>
        public DirCacheEntry(byte[] newPath, int stage)
        {
            if (!isValidPath(newPath))
                throw new ArgumentException("Invalid path: "
                        + toString(newPath));
            if (stage < 0 || 3 < stage)
                throw new ArgumentException("Invalid stage " + stage
                    + " for path " + toString(newPath));

            _info = new byte[INFO_LEN];
            _infoOffset = 0;
            _path = newPath;

            int flags = ((stage & 0x3) << 12);
            if (_path.Length < NameMask)
            {
                flags |= _path.Length;
            }
            else
            {
                flags |= NameMask;
            }

            NB.encodeInt16(_info, _infoOffset + PFlags, flags);
        }

        public void write(Stream os)
        {
            int pathLen = _path.Length;
            os.Write(_info, _infoOffset, INFO_LEN);
            os.Write(_path, 0, pathLen);

            // Index records are padded out to the next 8 byte alignment
            // for historical reasons related to how C Git Read the files.
            //
            int actLen = INFO_LEN + pathLen;
            int expLen = (actLen + 8) & ~7;
            if (actLen != expLen)
            {
                os.Write(NullPad, 0, expLen - actLen);
            }
        }

        ///	<summary>
        /// Is it possible for this entry to be accidentally assumed clean?
        /// <para />
        /// The "racy git" problem happens when a work file can be updated faster
        /// than the filesystem records file modification timestamps. It is possible
        /// for an application to edit a work file, update the index, then edit it
        /// again before the filesystem will give the work file a new modification
        /// timestamp. This method tests to see if file was written out at the same
        /// time as the index.
        /// </summary>
        /// <param name="smudge_s">
        /// Seconds component of the index's last modified time.
        /// </param>
        /// <param name="smudge_ns">
        /// Nanoseconds component of the index's last modified time.
        /// </param>
        /// <returns>true if extra careful checks should be used.</returns>
        public bool mightBeRacilyClean(int smudge_s, int smudge_ns)
        {
            // If the index has a modification time then it came from disk
            // and was not generated from scratch in memory. In such cases
            // the entry is 'racily clean' if the entry's cached modification
            // time is equal to or later than the index modification time. In
            // such cases the work file is too close to the index to tell if
            // it is clean or not based on the modification time alone.
            //
            int @base = _infoOffset + PMtime;
            int mtime = NB.DecodeInt32(_info, @base);
            if (smudge_s < mtime) return true;

            if (smudge_s == mtime)
                return smudge_ns <= NB.DecodeInt32(_info, @base + 4) / 1000000;

            return false;
        }

        /// <summary>
        /// Force this entry to no longer match its working tree file.
        /// <para />
        /// This avoids the "racy git" problem by making this index entry no longer
        /// match the file in the working directory. Later git will be forced to
        /// compare the file content to ensure the file matches the working tree.
        /// </summary>
        public void smudgeRacilyClean()
        {
            // We don't use the same approach as C Git to smudge the entry,
            // as we cannot compare the working tree file to our SHA-1 and
            // thus cannot use the "size to 0" trick without accidentally
            // thinking a zero Length file is clean.
            //
            // Instead we force the mtime to the largest possible value, so
            // it is certainly After the index's own modification time and
            // on a future Read will cause mightBeRacilyClean to say "yes!".
            // It is also unlikely to match with the working tree file.
            //
            // I'll see you again before Jan 19, 2038, 03:14:07 AM GMT.
            //
            int @base = _infoOffset + PMtime;
            _info.Fill(@base, @base + 8, (byte)127);
        }

        public byte[] idBuffer()
        {
            return _info;
        }

        public int idOffset()
        {
            return _infoOffset + PObjectid;
        }

        ///	<summary>
        /// Is this entry always thought to be unmodified?
        /// <para />
        /// Most entries in the index do not have this flag set. Users may however
        /// set them on if the file system stat() costs are too high on this working
        /// directory, such as on NFS or SMB volumes.
        /// </summary>
        /// <returns> true if we must assume the entry is unmodified. </returns>
        public bool isAssumeValid()
        {
            return (_info[_infoOffset + PFlags] & AssumeValid) != 0;
        }

        /// <summary>
        /// Set the assume valid flag for this entry,
        /// </summary>
        /// <param name="assume">
        /// True to ignore apparent modifications; false to look at last
        /// modified to detect file modifications. 
        /// </param>
        public void setAssumeValid(bool assume)
        {
            unchecked
            {
                if (assume)
                    _info[_infoOffset + PFlags] |= (byte)AssumeValid;
                else
                    _info[_infoOffset + PFlags] &= (byte)~AssumeValid; // [henon] (byte)-129 results in an overflow
            }
        }

        /// <summary>
        /// Get the stage of this entry.
        /// <para />
        /// Entries have one of 4 possible stages: 0-3.
        /// </summary>
        /// <returns> the stage of this entry. </returns>
        public int getStage()
        {
            return (int)((uint)(_info[_infoOffset + PFlags]) >> 4) & 0x3;
        }

        ///	<summary>
        /// Obtain the raw <seealso cref="FileMode"/> bits for this entry.
        ///	</summary>
        ///	<returns> mode bits for the entry. </returns>
        ///	<seealso cref="FileMode.FromBits(int)"/>
        public int getRawMode()
        {
            return NB.DecodeInt32(_info, _infoOffset + PMode);
        }

        /// <summary>
        /// Obtain the <seealso cref="FileMode"/> for this entry.
        /// </summary>
        /// <returns>The file mode singleton for this entry.</returns>
        public FileMode getFileMode()
        {
            return FileMode.FromBits(getRawMode());
        }

        ///	<summary>
        /// Set the file mode for this entry.
        ///	</summary>
        ///	<param name="mode"> The new mode constant. </param>
        public void setFileMode(FileMode mode)
        {
            switch (mode.Bits & FileMode.TYPE_MASK)
            {
                case FileMode.TYPE_MISSING:
                case FileMode.TYPE_TREE:
                    throw new ArgumentException("Invalid mode " + mode.Bits
                        + " for path " + getPathString());
            }

            NB.encodeInt32(_info, _infoOffset + PMode, mode.Bits);
        }

        ///	<summary>
        /// Get the cached last modification date of this file, in milliseconds.
        ///	<para />
        /// One of the indicators that the file has been modified by an application
        /// changing the working tree is if the last modification time for the file
        /// differs from the time stored in this entry.
        /// </summary>
        /// <returns> last modification time of this file, in milliseconds since the
        /// Java epoch (midnight Jan 1, 1970 UTC).
        /// </returns>
        public long getLastModified()
        {
            return DecodeTimestamp(PMtime);
        }

        /// <summary>
        /// Set the cached last modification date of this file, using milliseconds.
        /// </summary>
        /// <param name="when">
        /// new cached modification date of the file, in milliseconds.
        /// </param>
        public void setLastModified(long when)
        {
            EncodeTimestamp(PMtime, when);
        }

        /// <summary>
        /// Get the cached size (in bytes) of this file.
        /// <para />
        /// One of the indicators that the file has been modified by an application
        /// changing the working tree is if the size of the file (in bytes) differs
        /// from the size stored in this entry.
        /// <para />
        /// Note that this is the length of the file in the working directory, which
        /// may differ from the size of the decompressed blob if work tree filters
        /// are being used, such as LF&lt;-&gt;CRLF conversion.
        /// </summary>
        /// <returns> cached size of the working directory file, in bytes. </returns>
        public int getLength()
        {
            return NB.DecodeInt32(_info, _infoOffset + PSize);
        }

        /// <summary>
        /// Set the cached size (in bytes) of this file.
        /// </summary>
        /// <param name="sz">new cached size of the file, as bytes.</param>
        public void setLength(int sz)
        {
            NB.encodeInt32(_info, _infoOffset + PSize, sz);
        }

        /// <summary>
        /// Obtain the ObjectId for the entry.
        /// <para />
        /// Using this method to compare ObjectId values between entries is
        /// inefficient as it causes memory allocation.
        /// </summary>
        /// <returns> object identifier for the entry. </returns>
        public ObjectId getObjectId()
        {
            return ObjectId.FromRaw(idBuffer(), idOffset());
        }

        ///	<summary>
        /// Set the ObjectId for the entry.
        ///	</summary>
        ///	<param name="id">
        /// New object identifier for the entry. May be
        /// <seealso cref="ObjectId.ZeroId"/> to remove the current identifier.
        /// </param>
        public void setObjectId(AnyObjectId id)
        {
            id.copyRawTo(idBuffer(), idOffset());
        }

        /// <summary>
        /// Set the ObjectId for the entry from the raw binary representation.
        /// </summary>
        /// <param name="bs">
        /// The raw byte buffer to read from. At least 20 bytes after <paramref name="p"/>
        /// must be available within this byte array. 
        /// </param>
        /// <param name="p">position to read the first byte of data from. </param>
        public void setObjectIdFromRaw(byte[] bs, int p)
        {
            Array.Copy(bs, p, idBuffer(), idOffset(), Constants.OBJECT_ID_LENGTH);
        }

        ///	<summary>
        /// Get the entry's complete path.
        /// <para />
        /// This method is not very efficient and is primarily meant for debugging
        /// and final output generation. Applications should try to avoid calling it,
        /// and if invoked do so only once per interesting entry, where the name is
        /// absolutely required for correct function.
        /// </summary>
        /// <returns>
        /// Complete path of the entry, from the root of the repository. If
        /// the entry is in a subtree there will be at least one '/' in the
        /// returned string. 
        /// </returns>
        public string getPathString()
        {
            return toString(_path);
        }

        ///	<summary>
        /// Copy the ObjectId and other meta fields from an existing entry.
        ///	<para />
        ///	This method copies everything except the path from one entry to another,
        ///	supporting renaming.
        ///	</summary>
        ///	<param name="src">
        /// The entry to copy ObjectId and meta fields from.
        /// </param>
        public void copyMetaData(DirCacheEntry src)
        {
            int pLen = NB.decodeUInt16(_info, _infoOffset + PFlags) & NameMask;
            Array.Copy(src._info, src._infoOffset, _info, _infoOffset, INFO_LEN);

            NB.encodeInt16(_info, _infoOffset + PFlags, pLen
                    | NB.decodeUInt16(_info, _infoOffset + PFlags) & ~NameMask);
        }

        private long DecodeTimestamp(int pIdx)
        {
            int @base = _infoOffset + pIdx;
            int sec = NB.DecodeInt32(_info, @base);
            int ms = NB.DecodeInt32(_info, @base + 4) / 1000000;
            return 1000L * sec + ms;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pIdx"></param>
        /// <param name="when">
        /// New cached modification date of the file, in milliseconds.
        /// </param>
        private void EncodeTimestamp(int pIdx, long when)
        {
            int @base = _infoOffset + pIdx;
            NB.encodeInt32(_info, @base, (int)(when / 1000));
            NB.encodeInt32(_info, @base + 4, ((int)(when % 1000)) * 1000000);
        }

        public byte[] Path
        {
            get { return _path; }
        }

        private static String toString(byte[] path)
        {
            return Constants.CHARSET.GetString(path);
        }

        public static bool isValidPath(byte[] path)
        {
            if (path.Length == 0)
                return false; // empty path is not permitted.

            bool componentHasChars = false;
            foreach (byte c in path)
            {
                switch (c)
                {
                    case 0:
                        return false; // NUL is never allowed within the path.

                    case (byte)'/':
                        if (componentHasChars)
                            componentHasChars = false;
                        else
                            return false;
                        break;

                    default:
                        componentHasChars = true;
                        break;
                }
            }
            return componentHasChars;
        }
    }
}