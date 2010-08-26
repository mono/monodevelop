/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
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
using System.Text;
using System.IO;
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.DirectoryCache
{
    /// <summary>
    /// Single tree record from the 'TREE' <seealso cref="DirCache"/> extension.
    /// <para />
    /// A valid cache tree record contains the object id of a tree object and the
    /// total number of <seealso cref="DirCacheEntry"/> instances (counted recursively) from
    /// the DirCache contained within the tree. This information facilitates faster
    /// traversal of the index and quicker generation of tree objects prior to
    /// creating a new commit.
    /// <para />
    /// An invalid cache tree record indicates a known subtree whose file entries
    /// have changed in ways that cause the tree to no longer have a known object id.
    /// Invalid cache tree records must be revalidated prior to use.
    /// </summary>
    public class DirCacheTree
    {
        private static readonly byte[] NoName = { };
        private static readonly DirCacheTree[] NoChildren = { };

        private static readonly Comparison<DirCacheTree> TreeComparison = (o1, o2) =>
        {
            byte[] a = o1._encodedName;
            byte[] b = o2._encodedName;
            int aLen = a.Length;
            int bLen = b.Length;
            int cPos;

            for (cPos = 0; cPos < aLen && cPos < bLen; cPos++)
            {
                int cmp = (a[cPos] & 0xff) - (b[cPos] & (byte)0xff);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            if (aLen == bLen)
            {
                return 0;
            }

            if (aLen < bLen)
            {
                return '/' - (b[cPos] & 0xff);
            }

            return (a[cPos] & 0xff) - '/';
        };

        // Tree this tree resides in; null if we are the root.
        private readonly DirCacheTree _parent;

        // Name of this tree within its parent.
        private readonly byte[] _encodedName;

        // Number of DirCacheEntry records that belong to this tree.
        private int _entrySpan;

        // Unique SHA-1 of this tree; null if invalid.
        private ObjectId _id;

        // Child trees, if any, sorted by EncodedName.
        private DirCacheTree[] _children;

        // Number of valid children.
        private int _childCount;

        public DirCacheTree()
        {
            _encodedName = NoName;
            _children = NoChildren;
            _childCount = 0;
            _entrySpan = -1;
        }

        private DirCacheTree(DirCacheTree myParent, byte[] path, int pathOff, int pathLen)
        {
            _parent = myParent;
            _encodedName = new byte[pathLen];
            Array.Copy(path, pathOff, _encodedName, 0, pathLen);
            _children = NoChildren;
            _childCount = 0;
            _entrySpan = -1;
        }

        public DirCacheTree(byte[] @in, MutableInteger off, DirCacheTree myParent)
        {
            _parent = myParent;

            int ptr = RawParseUtils.next(@in, off.value, (byte)'\0');
            int nameLen = ptr - off.value - 1;
            if (nameLen > 0)
            {
                _encodedName = new byte[nameLen];
                Array.Copy(@in, off.value, _encodedName, 0, nameLen);
            }
            else
            {
                _encodedName = NoName;
            }

            _entrySpan = RawParseUtils.parseBase10(@in, ptr, off);
            int subcnt = RawParseUtils.parseBase10(@in, off.value, off);
            off.value = RawParseUtils.next(@in, off.value, (byte)'\n');

            if (_entrySpan >= 0)
            {
                // Valid trees have a positive entry count and an id of a
                // tree object that should exist in the object database.
                //
                _id = ObjectId.FromRaw(@in, off.value);
                off.value += Constants.OBJECT_ID_LENGTH;
            }

            if (subcnt > 0)
            {
                bool alreadySorted = true;
                _children = new DirCacheTree[subcnt];
                for (int i = 0; i < subcnt; i++)
                {
                    _children[i] = new DirCacheTree(@in, off, this);

                    // C Git's ordering differs from our own; it prefers to
                    // sort by Length first. This sometimes produces a sort
                    // we do not desire. On the other hand it may have been
                    // created by us, and be sorted the way we want.
                    //
                    if (alreadySorted && i > 0
                            && TreeComparison(_children[i - 1], _children[i]) > 0)
                    {
                        alreadySorted = false;
                    }
                }

                if (!alreadySorted)
                {
                    Array.Sort(_children, TreeComparison);
                }
            }
            else
            {
                // Leaf level trees have no children, only (file) entries.
                //
                _children = NoChildren;
            }
            _childCount = subcnt;
        }

        public void write(byte[] tmp, TemporaryBuffer os)
        {
            int ptr = tmp.Length;
            tmp[--ptr] = (byte)'\n';
            ptr = RawParseUtils.formatBase10(tmp, ptr, _childCount);
            tmp[--ptr] = (byte)' ';
            ptr = RawParseUtils.formatBase10(tmp, ptr, isValid() ? _entrySpan : -1);
            tmp[--ptr] = 0;

            os.write(_encodedName, 0, _encodedName.Length);
            os.write(tmp, ptr, tmp.Length - ptr);

            if (isValid())
            {
                _id.copyRawTo(tmp, 0);
                os.write(tmp, 0, Constants.OBJECT_ID_LENGTH);
            }

            for (int i = 0; i < _childCount; i++)
            {
                _children[i].write(tmp, os);
            }
        }

        /// <summary>
        /// Determine if this cache is currently valid.
        /// <para />
        /// A valid cache tree knows how many <seealso cref="DirCacheEntry"/> instances from
        /// the parent <seealso cref="DirCache"/> reside within this tree (recursively
        /// enumerated). It also knows the object id of the tree, as the tree should
        /// be readily available from the repository's object database.
        /// </summary>
        /// <returns>
        /// True if this tree is knows key details about itself; false if the
        /// tree needs to be regenerated.
        /// </returns>
        public bool isValid()
        {
            return _id != null;
        }

        /// <summary>
        /// Get the number of entries this tree spans within the DirCache.
        /// <para />
        /// If this tree is not valid (see <seealso cref="isValid()"/>) this method's return
        /// value is always strictly negative (less than 0) but is otherwise an
        /// undefined result.
        /// </summary>
        /// <returns>
        /// Total number of entries (recursively) contained within this tree.
        /// </returns>
        public int getEntrySpan()
        {
            return _entrySpan;
        }

        /// <summary>
        /// Get the number of cached subtrees contained within this tree.
        /// </summary>
        /// <returns>
        /// Number of child trees available through this tree.
        /// </returns>
        public int getChildCount()
        {
            return _childCount;
        }

        /// <summary>
        /// Get the i-th child cache tree.
        /// </summary>
        /// <param name="i">Index of the child to obtain.</param>
        /// <returns>The child tree.</returns>
        public DirCacheTree getChild(int i)
        {
            return _children[i];
        }

        public ObjectId getObjectId()
        {
            return _id;
        }

        /// <summary>
        /// Get the tree's name within its parent.
        /// <para />
        /// This method is not very efficient and is primarily meant for debugging
        /// and final output generation. Applications should try to avoid calling it,
        /// and if invoked do so only once per interesting entry, where the name is
        /// absolutely required for correct function.
        /// </summary>
        /// <returns>
        /// Name of the tree. This does not contain any '/' characters.
        /// </returns>
        public string getNameString()
        {
            return Constants.CHARSET.GetString(_encodedName);
        }

        /// <summary>
        /// Get the tree's path within the repository.
        /// <para />
        /// This method is not very efficient and is primarily meant for debugging
        /// and final output generation. Applications should try to avoid calling it,
        /// and if invoked do so only once per interesting entry, where the name is
        /// absolutely required for correct function.
        /// </summary>
        /// <returns>
        /// Path of the tree, relative to the repository root. If this is not
        /// the root tree the path ends with '/'. The root tree's path string
        /// is the empty string ("").
        /// </returns>
        public string getPathString()
        {
            var sb = new StringBuilder();
            AppendName(sb);
            return sb.ToString();
        }

        ///	<summary>
        /// Write (if necessary) this tree to the object store.
        ///	</summary>
        ///	<param name="cacheEntry">the complete cache from DirCache.</param>
        ///	<param name="cIdx">
        /// first position of <code>cache</code> that is a member of this
        /// tree. The path of <code>cache[cacheIdx].path</code> for the
        /// range <code>[0,pathOff-1)</code> matches the complete path of
        /// this tree, from the root of the repository. </param>
        ///	<param name="pathOffset">
        /// number of bytes of <code>cache[cacheIdx].path</code> that
        /// matches this tree's path. The value at array position
        /// <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
        /// <code>pathOff</code> is > 0.
        /// </param>
        ///	<param name="ow">
        /// the writer to use when serializing to the store.
        /// </param>
        ///	<returns>identity of this tree.</returns>
        ///	<exception cref="UnmergedPathException">
        /// one or more paths contain higher-order stages (stage > 0),
        /// which cannot be stored in a tree object.
        /// </exception>
        ///	<exception cref="IOException">
        /// an unexpected error occurred writing to the object store.
        /// </exception>
        public ObjectId writeTree(DirCacheEntry[] cacheEntry, int cIdx, int pathOffset, ObjectWriter ow)
        {
            if (_id == null)
            {
                int endIdx = cIdx + _entrySpan;
                int size = ComputeSize(cacheEntry, cIdx, pathOffset, ow);
                var @out = new MemoryStream(size);
                int childIdx = 0;
                int entryIdx = cIdx;

                while (entryIdx < endIdx)
                {
                    DirCacheEntry e = cacheEntry[entryIdx];
                    byte[] ep = e.Path;
                    if (childIdx < _childCount)
                    {
                        DirCacheTree st = _children[childIdx];
                        if (st.contains(ep, pathOffset, ep.Length))
                        {
                            FileMode.Tree.CopyTo(@out);
                            @out.Write(new[] { (byte)' ' }, 0, 1);
                            @out.Write(st._encodedName, 0, st._encodedName.Length);
                            @out.Write(new[] { (byte)0 }, 0, 1);
                            st._id.copyRawTo(@out);

                            entryIdx += st._entrySpan;
                            childIdx++;
                            continue;
                        }
                    }

                    e.getFileMode().CopyTo(@out);
                    @out.Write(new[] { (byte)' ' }, 0, 1);
                    @out.Write(ep, pathOffset, ep.Length - pathOffset);
                    @out.Write(new byte[] { 0 }, 0, 1);
                    @out.Write(e.idBuffer(), e.idOffset(), Constants.OBJECT_ID_LENGTH);
                    entryIdx++;
                }

                _id = ow.WriteCanonicalTree(@out.ToArray());
            }

            return _id;
        }

        private int ComputeSize(DirCacheEntry[] cache, int cIdx, int pathOffset, ObjectWriter ow)
        {
            int endIdx = cIdx + _entrySpan;
            int childIdx = 0;
            int entryIdx = cIdx;
            int size = 0;

            while (entryIdx < endIdx)
            {
                DirCacheEntry e = cache[entryIdx];
                if (e.getStage() != 0)
                {
                    throw new UnmergedPathException(e);
                }

                byte[] ep = e.Path;
                if (childIdx < _childCount)
                {
                    DirCacheTree st = _children[childIdx];
                    if (st.contains(ep, pathOffset, ep.Length))
                    {
                        int stOffset = pathOffset + st.nameLength() + 1;
                        st.writeTree(cache, entryIdx, stOffset, ow);

                        size += FileMode.Tree.copyToLength();
                        size += st.nameLength();
                        size += Constants.OBJECT_ID_LENGTH + 2;

                        entryIdx += st._entrySpan;
                        childIdx++;
                        continue;
                    }
                }

                FileMode mode = e.getFileMode();

                size += mode.copyToLength();
                size += ep.Length - pathOffset;
                size += Constants.OBJECT_ID_LENGTH + 2;
                entryIdx++;
            }

            return size;
        }

        private void AppendName(StringBuilder sb)
        {
            if (_parent != null)
            {
                _parent.AppendName(sb);
                sb.Append(getNameString());
                sb.Append('/');
            }
            else if (nameLength() > 0)
            {
                sb.Append(getNameString());
                sb.Append('/');
            }
        }

        public int nameLength()
        {
            return _encodedName.Length;
        }

        public bool contains(byte[] a, int aOff, int aLen)
        {
            byte[] e = _encodedName;
            int eLen = e.Length;
            for (int eOff = 0; eOff < eLen && aOff < aLen; eOff++, aOff++)
            {
                if (e[eOff] != a[aOff]) return false;
            }

            if (aOff == aLen) return false;
            return a[aOff] == '/';
        }

        ///	<summary>
        /// Update (if necessary) this tree's entrySpan.
        ///	</summary>
        ///	<param name="cache">the complete cache from DirCache. </param>
        ///	<param name="cCnt">
        /// Number of entries in <code>cache</code> that are valid for
        /// iteration.
        /// </param>
        ///	<param name="cIdx">
        /// First position of <code>cache</code> that is a member of this
        /// tree. The path of <code>cache[cacheIdx].path</code> for the
        /// range <code>[0,pathOff-1)</code> matches the complete path of
        /// this tree, from the root of the repository.
        /// </param>
        ///	<param name="pathOff">
        /// number of bytes of <code>cache[cacheIdx].path</code> that
        /// matches this tree's path. The value at array position
        /// <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
        /// <code>pathOff</code> is > 0.
        /// </param>
        public void validate(DirCacheEntry[] cache, int cCnt, int cIdx, int pathOff)
        {
            if (_entrySpan >= 0)
            {
                // If we are valid, our children are also valid.
                // We have no need to validate them.
                //
                return;
            }

            _entrySpan = 0;
            if (cCnt == 0)
            {
                // Special case of an empty index, and we are the root tree.
                //
                return;
            }

            byte[] firstPath = cache[cIdx].Path;
            int stIdx = 0;
            while (cIdx < cCnt)
            {
                byte[] currPath = cache[cIdx].Path;
                if (pathOff > 0 && !peq(firstPath, currPath, pathOff))
                {
                    // The current entry is no longer in this tree. Our
                    // span is updated and the remainder goes elsewhere.
                    //
                    break;
                }

                DirCacheTree st = stIdx < _childCount ? _children[stIdx] : null;
                int cc = NameComparison(currPath, pathOff, st);
                if (cc > 0)
                {
                    // This subtree is now empty.
                    //
                    RemoveChild(stIdx);
                    continue;
                }

                if (cc < 0)
                {
                    int p = Slash(currPath, pathOff);
                    if (p < 0)
                    {
                        // The entry has no '/' and thus is directly in this
                        // tree. Count it as one of our own.
                        //
                        cIdx++;
                        _entrySpan++;
                        continue;
                    }

                    // Build a new subtree for this entry.
                    //
                    st = new DirCacheTree(this, currPath, pathOff, p - pathOff);
                    InsertChild(stIdx, st);
                }

                // The entry is contained in this subtree.
                //
                st.validate(cache, cCnt, cIdx, pathOff + st.nameLength() + 1);
                cIdx += st._entrySpan;
                _entrySpan += st._entrySpan;
                stIdx++;
            }

            if (stIdx < _childCount)
            {
                // None of our remaining children can be in this tree
                // as the current cache entry is After our own name.
                //
                var dct = new DirCacheTree[stIdx];
                Array.Copy(_children, 0, dct, 0, stIdx);
                _children = dct;
            }
        }

        private void InsertChild(int stIdx, DirCacheTree st)
        {
            DirCacheTree[] c = _children;
            if (_childCount + 1 <= c.Length)
            {
                if (stIdx < _childCount)
                {
                    Array.Copy(c, stIdx, c, stIdx + 1, _childCount - stIdx);
                }
                c[stIdx] = st;
                _childCount++;
                return;
            }

            int n = c.Length;
            var a = new DirCacheTree[n + 1];
            if (stIdx > 0)
            {
                Array.Copy(c, 0, a, 0, stIdx);
            }
            a[stIdx] = st;
            if (stIdx < n)
            {
                Array.Copy(c, stIdx, a, stIdx + 1, n - stIdx);
            }
            _children = a;
            _childCount++;
        }

        private void RemoveChild(int stIdx)
        {
            int n = --_childCount;
            if (stIdx < n)
            {
                Array.Copy(_children, stIdx + 1, _children, stIdx, n - stIdx);
            }
            _children[n] = null;
        }

        internal static bool peq(byte[] a, byte[] b, int aLen)
        {
            if (b.Length < aLen) return false;

            for (aLen--; aLen >= 0; aLen--)
            {
                if (a[aLen] != b[aLen]) return false;
            }

            return true;
        }

        private static int NameComparison(byte[] a, int aPos, DirCacheTree ct)
        {
            if (ct == null) return -1;

            byte[] b = ct._encodedName;
            int aLen = a.Length;
            int bLen = b.Length;
            int bPos = 0;
            for (; aPos < aLen && bPos < bLen; aPos++, bPos++)
            {
                int cmp = (a[aPos] & 0xff) - (b[bPos] & 0xff);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            if (bPos == bLen)
            {
                return a[aPos] == '/' ? 0 : -1;
            }

            return aLen - bLen;
        }

        private static int Slash(byte[] a, int aPos)
        {
            int aLen = a.Length;
            for (; aPos < aLen; aPos++)
            {
                if (a[aPos] == '/')
                {
                    return aPos;
                }
            }
            return -1;
        }
    }
}