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

using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Dircache;
using NGit.Errors;
using NGit.Util;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// Single tree record from the 'TREE'
	/// <see cref="DirCache">DirCache</see>
	/// extension.
	/// <p>
	/// A valid cache tree record contains the object id of a tree object and the
	/// total number of
	/// <see cref="DirCacheEntry">DirCacheEntry</see>
	/// instances (counted recursively) from
	/// the DirCache contained within the tree. This information facilitates faster
	/// traversal of the index and quicker generation of tree objects prior to
	/// creating a new commit.
	/// <p>
	/// An invalid cache tree record indicates a known subtree whose file entries
	/// have changed in ways that cause the tree to no longer have a known object id.
	/// Invalid cache tree records must be revalidated prior to use.
	/// </summary>
	public class DirCacheTree
	{
		private static readonly byte[] NO_NAME = new byte[] {  };

		private static readonly NGit.Dircache.DirCacheTree[] NO_CHILDREN = new NGit.Dircache.DirCacheTree
			[] {  };

		private sealed class _IComparer_82 : IComparer<NGit.Dircache.DirCacheTree>
		{
			public _IComparer_82()
			{
			}

			public int Compare(NGit.Dircache.DirCacheTree o1, NGit.Dircache.DirCacheTree o2)
			{
				byte[] a = o1.encodedName;
				byte[] b = o2.encodedName;
				int aLen = a.Length;
				int bLen = b.Length;
				int cPos;
				for (cPos = 0; cPos < aLen && cPos < bLen; cPos++)
				{
					int cmp = (a[cPos] & unchecked((int)(0xff))) - (b[cPos] & unchecked((int)(0xff)));
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
					return '/' - (b[cPos] & unchecked((int)(0xff)));
				}
				return (a[cPos] & unchecked((int)(0xff))) - '/';
			}
		}

		private static readonly IComparer<NGit.Dircache.DirCacheTree> TREE_CMP = new _IComparer_82
			();

		/// <summary>Tree this tree resides in; null if we are the root.</summary>
		/// <remarks>Tree this tree resides in; null if we are the root.</remarks>
		private NGit.Dircache.DirCacheTree parent;

		/// <summary>Name of this tree within its parent.</summary>
		/// <remarks>Name of this tree within its parent.</remarks>
		private byte[] encodedName;

		/// <summary>
		/// Number of
		/// <see cref="DirCacheEntry">DirCacheEntry</see>
		/// records that belong to this tree.
		/// </summary>
		private int entrySpan;

		/// <summary>Unique SHA-1 of this tree; null if invalid.</summary>
		/// <remarks>Unique SHA-1 of this tree; null if invalid.</remarks>
		private ObjectId id;

		/// <summary>
		/// Child trees, if any, sorted by
		/// <see cref="encodedName">encodedName</see>
		/// .
		/// </summary>
		private NGit.Dircache.DirCacheTree[] children;

		/// <summary>
		/// Number of valid children in
		/// <see cref="children">children</see>
		/// .
		/// </summary>
		private int childCnt;

		public DirCacheTree()
		{
			encodedName = NO_NAME;
			children = NO_CHILDREN;
			childCnt = 0;
			entrySpan = -1;
		}

		private DirCacheTree(NGit.Dircache.DirCacheTree myParent, byte[] path, int pathOff
			, int pathLen)
		{
			parent = myParent;
			encodedName = new byte[pathLen];
			System.Array.Copy(path, pathOff, encodedName, 0, pathLen);
			children = NO_CHILDREN;
			childCnt = 0;
			entrySpan = -1;
		}

		internal DirCacheTree(byte[] @in, MutableInteger off, NGit.Dircache.DirCacheTree 
			myParent)
		{
			parent = myParent;
			int ptr = RawParseUtils.Next(@in, off.value, '\0');
			int nameLen = ptr - off.value - 1;
			if (nameLen > 0)
			{
				encodedName = new byte[nameLen];
				System.Array.Copy(@in, off.value, encodedName, 0, nameLen);
			}
			else
			{
				encodedName = NO_NAME;
			}
			entrySpan = RawParseUtils.ParseBase10(@in, ptr, off);
			int subcnt = RawParseUtils.ParseBase10(@in, off.value, off);
			off.value = RawParseUtils.Next(@in, off.value, '\n');
			if (entrySpan >= 0)
			{
				// Valid trees have a positive entry count and an id of a
				// tree object that should exist in the object database.
				//
				id = ObjectId.FromRaw(@in, off.value);
				off.value += Constants.OBJECT_ID_LENGTH;
			}
			if (subcnt > 0)
			{
				bool alreadySorted = true;
				children = new NGit.Dircache.DirCacheTree[subcnt];
				for (int i = 0; i < subcnt; i++)
				{
					children[i] = new NGit.Dircache.DirCacheTree(@in, off, this);
					// C Git's ordering differs from our own; it prefers to
					// sort by length first. This sometimes produces a sort
					// we do not desire. On the other hand it may have been
					// created by us, and be sorted the way we want.
					//
					if (alreadySorted && i > 0 && TREE_CMP.Compare(children[i - 1], children[i]) > 0)
					{
						alreadySorted = false;
					}
				}
				if (!alreadySorted)
				{
					Arrays.Sort(children, 0, subcnt, TREE_CMP);
				}
			}
			else
			{
				// Leaf level trees have no children, only (file) entries.
				//
				children = NO_CHILDREN;
			}
			childCnt = subcnt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Write(byte[] tmp, OutputStream os)
		{
			int ptr = tmp.Length;
			tmp[--ptr] = (byte)('\n');
			ptr = RawParseUtils.FormatBase10(tmp, ptr, childCnt);
			tmp[--ptr] = (byte)(' ');
			ptr = RawParseUtils.FormatBase10(tmp, ptr, IsValid() ? entrySpan : -1);
			tmp[--ptr] = 0;
			os.Write(encodedName);
			os.Write(tmp, ptr, tmp.Length - ptr);
			if (IsValid())
			{
				id.CopyRawTo(tmp, 0);
				os.Write(tmp, 0, Constants.OBJECT_ID_LENGTH);
			}
			for (int i = 0; i < childCnt; i++)
			{
				children[i].Write(tmp, os);
			}
		}

		/// <summary>Determine if this cache is currently valid.</summary>
		/// <remarks>
		/// Determine if this cache is currently valid.
		/// <p>
		/// A valid cache tree knows how many
		/// <see cref="DirCacheEntry">DirCacheEntry</see>
		/// instances from
		/// the parent
		/// <see cref="DirCache">DirCache</see>
		/// reside within this tree (recursively
		/// enumerated). It also knows the object id of the tree, as the tree should
		/// be readily available from the repository's object database.
		/// </remarks>
		/// <returns>
		/// true if this tree is knows key details about itself; false if the
		/// tree needs to be regenerated.
		/// </returns>
		public virtual bool IsValid()
		{
			return id != null;
		}

		/// <summary>Get the number of entries this tree spans within the DirCache.</summary>
		/// <remarks>
		/// Get the number of entries this tree spans within the DirCache.
		/// <p>
		/// If this tree is not valid (see
		/// <see cref="IsValid()">IsValid()</see>
		/// ) this method's return
		/// value is always strictly negative (less than 0) but is otherwise an
		/// undefined result.
		/// </remarks>
		/// <returns>total number of entries (recursively) contained within this tree.</returns>
		public virtual int GetEntrySpan()
		{
			return entrySpan;
		}

		/// <summary>Get the number of cached subtrees contained within this tree.</summary>
		/// <remarks>Get the number of cached subtrees contained within this tree.</remarks>
		/// <returns>number of child trees available through this tree.</returns>
		public virtual int GetChildCount()
		{
			return childCnt;
		}

		/// <summary>Get the i-th child cache tree.</summary>
		/// <remarks>Get the i-th child cache tree.</remarks>
		/// <param name="i">index of the child to obtain.</param>
		/// <returns>the child tree.</returns>
		public virtual NGit.Dircache.DirCacheTree GetChild(int i)
		{
			return children[i];
		}

		internal virtual ObjectId GetObjectId()
		{
			return id;
		}

		/// <summary>Get the tree's name within its parent.</summary>
		/// <remarks>
		/// Get the tree's name within its parent.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>name of the tree. This does not contain any '/' characters.</returns>
		public virtual string GetNameString()
		{
			ByteBuffer bb = ByteBuffer.Wrap(encodedName);
			return Constants.CHARSET.Decode(bb).ToString();
		}

		/// <summary>Get the tree's path within the repository.</summary>
		/// <remarks>
		/// Get the tree's path within the repository.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>
		/// path of the tree, relative to the repository root. If this is not
		/// the root tree the path ends with '/'. The root tree's path string
		/// is the empty string ("").
		/// </returns>
		public virtual string GetPathString()
		{
			StringBuilder r = new StringBuilder();
			AppendName(r);
			return r.ToString();
		}

		/// <summary>Write (if necessary) this tree to the object store.</summary>
		/// <remarks>Write (if necessary) this tree to the object store.</remarks>
		/// <param name="cache">the complete cache from DirCache.</param>
		/// <param name="cIdx">
		/// first position of <code>cache</code> that is a member of this
		/// tree. The path of <code>cache[cacheIdx].path</code> for the
		/// range <code>[0,pathOff-1)</code> matches the complete path of
		/// this tree, from the root of the repository.
		/// </param>
		/// <param name="pathOffset">
		/// number of bytes of <code>cache[cacheIdx].path</code> that
		/// matches this tree's path. The value at array position
		/// <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
		/// <code>pathOff</code> is &gt; 0.
		/// </param>
		/// <param name="ow">the writer to use when serializing to the store.</param>
		/// <returns>identity of this tree.</returns>
		/// <exception cref="NGit.Errors.UnmergedPathException">
		/// one or more paths contain higher-order stages (stage &gt; 0),
		/// which cannot be stored in a tree object.
		/// </exception>
		/// <exception cref="System.IO.IOException">an unexpected error occurred writing to the object store.
		/// 	</exception>
		internal virtual ObjectId WriteTree(DirCacheEntry[] cache, int cIdx, int pathOffset
			, ObjectInserter ow)
		{
			if (id == null)
			{
				int endIdx = cIdx + entrySpan;
				TreeFormatter fmt = new TreeFormatter(ComputeSize(cache, cIdx, pathOffset, ow));
				int childIdx = 0;
				int entryIdx = cIdx;
				while (entryIdx < endIdx)
				{
					DirCacheEntry e = cache[entryIdx];
					byte[] ep = e.path;
					if (childIdx < childCnt)
					{
						NGit.Dircache.DirCacheTree st = children[childIdx];
						if (st.Contains(ep, pathOffset, ep.Length))
						{
							fmt.Append(st.encodedName, FileMode.TREE, st.id);
							entryIdx += st.entrySpan;
							childIdx++;
							continue;
						}
					}
					fmt.Append(ep, pathOffset, ep.Length - pathOffset, e.FileMode, e.IdBuffer, e.IdOffset
						);
					entryIdx++;
				}
				id = ow.Insert(fmt);
			}
			return id;
		}

		/// <exception cref="NGit.Errors.UnmergedPathException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private int ComputeSize(DirCacheEntry[] cache, int cIdx, int pathOffset, ObjectInserter
			 ow)
		{
			int endIdx = cIdx + entrySpan;
			int childIdx = 0;
			int entryIdx = cIdx;
			int size = 0;
			while (entryIdx < endIdx)
			{
				DirCacheEntry e = cache[entryIdx];
				if (e.Stage != 0)
				{
					throw new UnmergedPathException(e);
				}
				byte[] ep = e.path;
				if (childIdx < childCnt)
				{
					NGit.Dircache.DirCacheTree st = children[childIdx];
					if (st.Contains(ep, pathOffset, ep.Length))
					{
						int stOffset = pathOffset + st.NameLength() + 1;
						st.WriteTree(cache, entryIdx, stOffset, ow);
						size += TreeFormatter.EntrySize(FileMode.TREE, st.NameLength());
						entryIdx += st.entrySpan;
						childIdx++;
						continue;
					}
				}
				size += TreeFormatter.EntrySize(e.FileMode, ep.Length - pathOffset);
				entryIdx++;
			}
			return size;
		}

		private void AppendName(StringBuilder r)
		{
			if (parent != null)
			{
				parent.AppendName(r);
				r.Append(GetNameString());
				r.Append('/');
			}
			else
			{
				if (NameLength() > 0)
				{
					r.Append(GetNameString());
					r.Append('/');
				}
			}
		}

		internal int NameLength()
		{
			return encodedName.Length;
		}

		internal bool Contains(byte[] a, int aOff, int aLen)
		{
			byte[] e = encodedName;
			int eLen = e.Length;
			for (int eOff = 0; eOff < eLen && aOff < aLen; eOff++, aOff++)
			{
				if (e[eOff] != a[aOff])
				{
					return false;
				}
			}
			if (aOff == aLen)
			{
				return false;
			}
			return a[aOff] == '/';
		}

		/// <summary>Update (if necessary) this tree's entrySpan.</summary>
		/// <remarks>Update (if necessary) this tree's entrySpan.</remarks>
		/// <param name="cache">the complete cache from DirCache.</param>
		/// <param name="cCnt">
		/// number of entries in <code>cache</code> that are valid for
		/// iteration.
		/// </param>
		/// <param name="cIdx">
		/// first position of <code>cache</code> that is a member of this
		/// tree. The path of <code>cache[cacheIdx].path</code> for the
		/// range <code>[0,pathOff-1)</code> matches the complete path of
		/// this tree, from the root of the repository.
		/// </param>
		/// <param name="pathOff">
		/// number of bytes of <code>cache[cacheIdx].path</code> that
		/// matches this tree's path. The value at array position
		/// <code>cache[cacheIdx].path[pathOff-1]</code> is always '/' if
		/// <code>pathOff</code> is &gt; 0.
		/// </param>
		internal virtual void Validate(DirCacheEntry[] cache, int cCnt, int cIdx, int pathOff
			)
		{
			if (entrySpan >= 0)
			{
				// If we are valid, our children are also valid.
				// We have no need to validate them.
				//
				return;
			}
			entrySpan = 0;
			if (cCnt == 0)
			{
				// Special case of an empty index, and we are the root tree.
				//
				return;
			}
			byte[] firstPath = cache[cIdx].path;
			int stIdx = 0;
			while (cIdx < cCnt)
			{
				byte[] currPath = cache[cIdx].path;
				if (pathOff > 0 && !Peq(firstPath, currPath, pathOff))
				{
					// The current entry is no longer in this tree. Our
					// span is updated and the remainder goes elsewhere.
					//
					break;
				}
				NGit.Dircache.DirCacheTree st = stIdx < childCnt ? children[stIdx] : null;
				int cc = Namecmp(currPath, pathOff, st);
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
						entrySpan++;
						continue;
					}
					// Build a new subtree for this entry.
					//
					st = new NGit.Dircache.DirCacheTree(this, currPath, pathOff, p - pathOff);
					InsertChild(stIdx, st);
				}
				// The entry is contained in this subtree.
				//
				st.Validate(cache, cCnt, cIdx, pathOff + st.NameLength() + 1);
				cIdx += st.entrySpan;
				entrySpan += st.entrySpan;
				stIdx++;
			}
			if (stIdx < childCnt)
			{
				// None of our remaining children can be in this tree
				// as the current cache entry is after our own name.
				//
				NGit.Dircache.DirCacheTree[] dct = new NGit.Dircache.DirCacheTree[stIdx];
				System.Array.Copy(children, 0, dct, 0, stIdx);
				children = dct;
			}
		}

		private void InsertChild(int stIdx, NGit.Dircache.DirCacheTree st)
		{
			NGit.Dircache.DirCacheTree[] c = children;
			if (childCnt + 1 <= c.Length)
			{
				if (stIdx < childCnt)
				{
					System.Array.Copy(c, stIdx, c, stIdx + 1, childCnt - stIdx);
				}
				c[stIdx] = st;
				childCnt++;
				return;
			}
			int n = c.Length;
			NGit.Dircache.DirCacheTree[] a = new NGit.Dircache.DirCacheTree[n + 1];
			if (stIdx > 0)
			{
				System.Array.Copy(c, 0, a, 0, stIdx);
			}
			a[stIdx] = st;
			if (stIdx < n)
			{
				System.Array.Copy(c, stIdx, a, stIdx + 1, n - stIdx);
			}
			children = a;
			childCnt++;
		}

		private void RemoveChild(int stIdx)
		{
			int n = --childCnt;
			if (stIdx < n)
			{
				System.Array.Copy(children, stIdx + 1, children, stIdx, n - stIdx);
			}
			children[n] = null;
		}

		internal static bool Peq(byte[] a, byte[] b, int aLen)
		{
			if (b.Length < aLen)
			{
				return false;
			}
			for (aLen--; aLen >= 0; aLen--)
			{
				if (a[aLen] != b[aLen])
				{
					return false;
				}
			}
			return true;
		}

		private static int Namecmp(byte[] a, int aPos, NGit.Dircache.DirCacheTree ct)
		{
			if (ct == null)
			{
				return -1;
			}
			byte[] b = ct.encodedName;
			int aLen = a.Length;
			int bLen = b.Length;
			int bPos = 0;
			for (; aPos < aLen && bPos < bLen; aPos++, bPos++)
			{
				int cmp = (a[aPos] & unchecked((int)(0xff))) - (b[bPos] & unchecked((int)(0xff)));
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
