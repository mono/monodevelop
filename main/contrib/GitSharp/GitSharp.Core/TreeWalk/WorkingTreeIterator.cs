/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.TreeWalk
{
    /// <summary>
    /// Walks a working directory tree as part of a {@link TreeWalk}.
    /// 
    /// Most applications will want to use the standard implementation of this
    /// iterator, {@link FileTreeIterator}, as that does all IO through the standard
    /// <code>java.io</code> package. Plugins for a Java based IDE may however wish
    /// to Create their own implementations of this class to allow traversal of the
    /// IDE's project space, as well as benefit from any caching the IDE may have.
    /// 
    /// <seealso cref="FileTreeIterator"/>
    /// </summary>
    public abstract class WorkingTreeIterator : AbstractTreeIterator
    {
        private static readonly byte[] Digits = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9' };
        private static readonly byte[] HBlob = Constants.encodedTypeString(Constants.OBJ_BLOB);
        private static readonly Comparison<Entry> EntryComparison = (o1, o2) =>
        {
            byte[] a = o1.EncodedName;
            byte[] b = o2.EncodedName;
            int aLen = o1.EncodedNameLen;
            int bLen = o2.EncodedNameLen;
            int cPos;

            for (cPos = 0; cPos < aLen && cPos < bLen; cPos++)
            {
                int cmp = (a[cPos] & 0xff) - (b[cPos] & 0xff);
                if (cmp != 0) return cmp;
            }

            if (cPos < aLen) return (a[cPos] & 0xff) - LastPathChar(o2);
            if (cPos < bLen) return LastPathChar(o1) - (b[cPos] & 0xff);
            return LastPathChar(o1) - LastPathChar(o2);
        };

        /// <summary>
        /// An empty entry array, suitable for <see cref="Init"/>.
        /// </summary>
        protected static readonly Entry[] Eof = { };

        /// <summary>
        /// Size we perform file IO in if we have to Read and hash a file.
        /// </summary>
        private const int BufferSize = 2048;

        /// <summary>
        /// The <see cref="idBuffer"/> for the current entry.
        /// </summary>
        private byte[] _contentId;

        /// <summary>
        /// Index within <see cref="_entries"/> that <see cref="_contentId"/> came from.
        /// </summary>
        private int _contentIdFromPtr;

        /// <summary>
		/// Buffer used to perform <see cref="_contentId"/> computations.
        /// </summary>
        private byte[] _contentReadBuffer;

        /// <summary>
		/// Digest computer for <see cref="_contentId"/> computations.
        /// </summary>
        private MessageDigest _contentDigest;

        /// <summary>
		/// File name character encoder.
        /// </summary>
        private readonly Encoding _nameEncoder;

        /// <summary>
		/// List of entries obtained from the subclass.
        /// </summary>
        private Entry[] _entries;

        /// <summary>
		/// Total number of _entries in <see cref="_entries"/> that are valid.
        /// </summary>
        private int _entryCount;

        /// <summary>
		/// Current position within <see cref="_entries"/>.
        /// </summary>
        private int ptr;

        /// <summary>
        /// Create a new iterator with no parent.
        /// </summary>
        protected WorkingTreeIterator()
        {
            _nameEncoder = Constants.CHARSET;
        }

        /// <summary>
        /// Create a new iterator with no parent and a prefix.
        /// 
        /// The prefix path supplied is inserted in front of all paths generated by
        /// this iterator. It is intended to be used when an iterator is being
        /// created for a subsection of an overall repository and needs to be
        /// combined with other iterators that are created to run over the entire
        /// repository namespace.
        /// </summary>
        /// <param name="prefix">
        /// Position of this iterator in the repository tree. The value
        /// may be null or the empty string to indicate the prefix is the
        /// root of the repository. A trailing slash ('/') is
        /// automatically appended if the prefix does not end in '/'.
        /// </param>
        protected WorkingTreeIterator(string prefix)
            : base(prefix)
        {
            _nameEncoder = Constants.CHARSET;
        }

        /// <summary>
        /// Create an iterator for a subtree of an existing iterator.
        /// </summary>
        /// <param name="parent">Parent tree iterator.</param>
        protected WorkingTreeIterator(WorkingTreeIterator parent) 
            : base(parent)
        {
            _nameEncoder = parent._nameEncoder;
        }

        public override byte[] idBuffer()
        {
            if (_contentIdFromPtr == ptr)
            {
                return _contentId;
            }

            switch (Mode & FileMode.TYPE_MASK)
            {
                case FileMode.TYPE_FILE:
                    _contentIdFromPtr = ptr;
                    return _contentId = IdBufferBlob(_entries[ptr]);

                case FileMode.TYPE_SYMLINK:
                    // Windows does not support symbolic links, so we should not
                    // have reached this particular part of the walk code.
                    //
                    return ZeroId;

                case FileMode.TYPE_GITLINK:
                    // TODO: Support obtaining current HEAD SHA-1 from nested repository
                    //
                    return ZeroId;
            }

            return ZeroId;
        }

        private void InitializeDigest()
        {
            if (_contentDigest != null) return;

            if (Parent == null)
            {
                _contentReadBuffer = new byte[BufferSize];
                _contentDigest = Constants.newMessageDigest();
            }
            else
            {
                var p = (WorkingTreeIterator)Parent;
                p.InitializeDigest();
                _contentReadBuffer = p._contentReadBuffer;
                _contentDigest = p._contentDigest;
            }
        }

        private byte[] IdBufferBlob(Entry entry)
        {
            try
            {
                FileStream @is = entry.OpenInputStream();
                if (@is == null)
                {
                    return ZeroId;
                }

                try
                {
                    InitializeDigest();

                    _contentDigest.Reset();
                    _contentDigest.Update(HBlob);
                    _contentDigest.Update((byte)' ');

                    long blobLength = entry.Length;
                    
                    long size = blobLength;
                    if (size == 0)
                    {
                        _contentDigest.Update((byte)'0');
                    }
                    else
                    {
                        int bufn = _contentReadBuffer.Length;
                        int p = bufn;
                        do
                        {
                            _contentReadBuffer[--p] = Digits[(int)(size % 10)];
                            size /= 10;
                        } while (size > 0);

                        _contentDigest.Update(_contentReadBuffer, p, bufn - p);
                    }

                    _contentDigest.Update(0);

                    while (true)
                    {
                        int r = @is.Read(_contentReadBuffer, 0, _contentReadBuffer.Length); // was: Read(_contentReadBuffer) in java
                        if (r <= 0) break;
                        _contentDigest.Update(_contentReadBuffer, 0, r);
                        size += r;
                    }

                    if (size != blobLength)
                    {
                        return ZeroId;
                    }

                    return _contentDigest.Digest();
                }
                finally
                {
                    try
                    {
                        @is.Close();
                    }
                    catch (IOException)
                    {
                        // Suppress any error related to closing an input
                        // stream. We don't care, we should not have any
                        // outstanding data to flush or anything like that.
                    }
                }
            }
            catch (IOException)
            {
                // Can't Read the file? Don't report the failure either.
                //
            }

            return ZeroId;
        }

        public override int idOffset()
        {
            return 0;
        }

        public override bool first()
        {
            return ptr == 0;
        }

        public override bool eof()
        {
            return ptr == _entryCount;
        }

        public override void next(int delta)
        {
            ptr += delta;

            if (!eof())
            {
                ParseEntry();
            }
        }

        public override void back(int delta)
        {
            ptr -= delta;
            ParseEntry();
        }

        private void ParseEntry()
        {
            Entry e = _entries[ptr];
            Mode = e.Mode.Bits;

            int nameLen = e.EncodedNameLen;
            ensurePathCapacity(PathOffset + nameLen, PathOffset);
            Array.Copy(e.EncodedName, 0, Path, PathOffset, nameLen);
            PathLen = PathOffset + nameLen;
        }

        /// <summary>
        /// Get the byte Length of this entry.
        /// </summary>
		/// <returns>Size of this file, in bytes.</returns>
        public long getEntryLength()
        {
            return Current.Length;
        }

        /// <summary>
        /// Get the last modified time of this entry.
        /// </summary>
        /// <returns>
        /// Last modified time of this file, in milliseconds since the epoch
		/// (Jan 1, 1970 UTC).
        /// </returns>
        public long getEntryLastModified()
        {
            return Current.LastModified;
        }

        private static int LastPathChar(Entry e)
        {
            return e.Mode == FileMode.Tree ? (byte)'/' : (byte)'\0';
        }

        /// <summary>
        /// Constructor helper.
        /// </summary>
        /// <param name="list">
        /// Files in the subtree of the work tree this iterator operates on.
        /// </param>
        protected void Init(Entry[] list)
        {
            var treeEntries = new[] {".", "..", Constants.DOT_GIT};

            // Filter out nulls, . and .. as these are not valid tree entries,
            // also cache the encoded forms of the path names for efficient use
            // later on during sorting and iteration.
            //
            _entries = list;
            int i, o;

            for (i = 0, o = 0; i < _entries.Length; i++)
            {
                Entry e = _entries[i];
                if (e == null || treeEntries.Contains(e.Name)) continue;

                if (i != o)
                {
                    _entries[o] = e;
                }

                e.EncodeName(_nameEncoder);
                o++;
            }

            _entryCount = o;
            Array.Sort(_entries, EntryComparison); // was Arrays.sort(entries, 0, _entryCnt, EntryComparison) in java

            _contentIdFromPtr = -1;
            ptr = 0;
            if (!eof())
            {
                ParseEntry();
            }
        }

        /// <summary>
        /// Obtain the current entry from this iterator.
        /// </summary>
        internal Entry Current
        {
            get { return _entries[ptr]; }
        }

        #region Nested Types

        /// <summary>
        /// A single entry within a working directory tree.
        /// </summary>
        public abstract class Entry
        {
            public byte[] EncodedName { get; private set; }

            public int EncodedNameLen
            {
                get { return EncodedName.Length; }
            }

            public void EncodeName(Encoding enc)
            {
                try
                {
                    EncodedName = enc.GetBytes(Name);
                }
                catch (EncoderFallbackException)
                {
                    // This should so never happen.
                    throw new Exception("Unencodeable file: " + Name);
                }
            }

            public override string ToString()
            {
                return Mode + " " + Name;
            }

            /// <summary>
            /// Get the type of this entry.
            /// 
            /// <b>Note: Efficient implementation required.</b>
            /// 
            /// The implementation of this method must be efficient. If a subclass
            /// needs to compute the value they should cache the reference within an
            /// instance member instead.
            /// </summary>
            /// <returns>
            /// A file mode constant from <see cref="FileMode"/>.
            /// </returns>
            public abstract FileMode Mode { get; }

            /// <summary>
            /// Get the byte Length of this entry.
            /// 
            /// <b>Note: Efficient implementation required.</b>
            /// 
            /// The implementation of this method must be efficient. If a subclass
            /// needs to compute the value they should cache the reference within an
            /// instance member instead.
            /// </summary>
            public abstract long Length { get; }

            /// <summary>
            /// Get the last modified time of this entry.
            /// 
            /// <b>Note: Efficient implementation required.</b>
            /// 
            /// The implementation of this method must be efficient. If a subclass
            /// needs to compute the value they should cache the reference within an
            /// instance member instead.
            /// </summary>
            public abstract long LastModified { get; }

            /// <summary>
            /// Get the name of this entry within its directory.
            /// 
            /// Efficient implementations are not required. The caller will obtain
            /// the name only once and cache it once obtained.
            /// </summary>
            public abstract string Name { get; }

            /// <summary>
            /// Obtain an input stream to Read the file content.
            /// 
            /// Efficient implementations are not required. The caller will usually
            /// obtain the stream only once per entry, if at all.
            /// 
            /// The input stream should not use buffering if the implementation can
            /// avoid it. The caller will buffer as necessary to perform efficient
            /// block IO operations.
            /// 
            /// The caller will close the stream once complete.
            /// </summary>
            /// <returns>
            /// A stream to Read from the file.
            /// </returns>
            public abstract FileStream OpenInputStream();
        }

        #endregion
    }
}