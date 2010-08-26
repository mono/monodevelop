/*
 * Copyright (C) 2007, Robin Rosenberg <me@lathund.dewire.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
	/// <summary>
	/// A representation of a Git tree entry. A Tree is a directory in Git.
	/// </summary>
	public class Tree : TreeEntry, Treeish
	{
		private static readonly TreeEntry[] EmptyTree = new TreeEntry[0];

		private readonly Repository _db;
		private TreeEntry[] _contents;

		///	<summary>
		/// Compare two names represented as bytes. Since git treats names of trees and
		///	blobs differently we have one parameter that represents a '/' for trees. For
		///	other objects the value should be NUL. The names are compare by their positive
		///	byte value (0..255).
		/// <para />
		/// A blob and a tree with the same name will not compare equal.
		/// </summary>
		/// <param name="a"> name </param>
		/// <param name="b"> name </param>
		/// <param name="lastA"> '/' if a is a tree, else NULL.</param>
		/// <param name="lastB"> '/' if b is a tree, else NULL.</param>
		/// <returns> &lt; 0 if a is sorted before b, 0 if they are the same, else b </returns>
		public static int CompareNames(byte[] a, byte[] b, int lastA, int lastB)
		{
			return CompareNames(a, b, 0, b.Length, lastA, lastB);
		}

		/// <summary>
		/// Compare two names represented as bytes. Since git treats names of trees and
		/// blobs differently we have one parameter that represents a '/' for trees. For
		/// other objects the value should be NUL. The names are compare by their positive
		/// byte value (0..255).
		/// <para />
		/// A blob and a tree with the same name will not compare equal.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="nameUTF8"></param>
		/// <param name="nameStart"></param>
		/// <param name="nameEnd"></param>
		/// <param name="lastA"> '/' if a is a tree, else NULL.</param>
		/// <param name="lastB"> '/' if b is a tree, else NULL.</param>
		/// <returns>Return &lt; 0 if a is sorted before b, 0 if they are the same, else b</returns>
		private static int CompareNames(byte[] a, byte[] nameUTF8, int nameStart, int nameEnd, int lastA, int lastB)
		{
			// There must be a .NET way of doing this! I assume there are both UTF8 names,
			// perhaps Constants.CHARSET.GetString on both then .Compare on the strings?
			// I'm pretty sure this is just doing that but the long way round, however
			// I could be wrong so we'll leave it at this for now. - NR
			int j;
			int k;

			for (j = 0, k = nameStart; j < a.Length && k < nameEnd; j++, k++)
			{
				int aj = a[j] & 0xff;
				int bk = nameUTF8[k] & 0xff;

				if (aj < bk) return -1;
				if (aj > bk) return 1;
			}

			if (j < a.Length)
			{
				int aj = a[j] & 0xff;

				if (aj < lastB) return -1;
				if (aj > lastB) return 1;
				if (j == a.Length - 1) return 0;
				return -1;
			}

			if (k < nameEnd)
			{
				int bk = nameUTF8[k] & 0xff;

				if (lastA < bk) return -1;
				if (lastA > bk) return 1;
				if (k == nameEnd - 1) return 0;
				return 1;
			}

			if (lastA < lastB) return -1;
			if (lastA > lastB) return 1;

			int nameLength = nameEnd - nameStart;
			if (a.Length == nameLength) return 0;
			return a.Length < nameLength ? -1 : 1;
		}

		private static byte[] SubString(byte[] s, int nameStart, int nameEnd)
		{
			if (nameStart == 0 && nameStart == s.Length)
			{
				return s;
			}

			var n = new byte[nameEnd - nameStart];
			Array.Copy(s, nameStart, n, 0, n.Length);
			return n;
		}

		private static int BinarySearch(TreeEntry[] entries, byte[] nameUTF8, int nameUTF8Last, int nameStart, int nameEnd)
		{
			if (entries.Length == 0) return -1;

			int high = entries.Length;
			int low = 0;
			do
			{
			    int mid = (int) (((uint) (low + high)) >> 1);
				int cmp = CompareNames(entries[mid].NameUTF8, nameUTF8,
					nameStart, nameEnd, GitSharp.Core.TreeEntry.LastChar(entries[mid]), nameUTF8Last);

				if (cmp < 0)
				{
					low = mid + 1;
				}
				else if (cmp == 0)
				{
					return mid;
				}
				else
				{
					high = mid;
				}

			} while (low < high);
			return -(low + 1);
		}

		public override Repository Repository
		{
			get { return _db; }
		}

		public bool IsRoot
		{
			get { return Parent == null; }
		}

		public override FileMode Mode
		{
			get { return FileMode.Tree; }
		}

		///	<summary>
		/// Constructor for a new Tree
		///	</summary>
		///	<param name="repo">The repository that owns the Tree.</param>
		public Tree(Repository repo)
			: base(null, null, null)
		{
			_db = repo;
			_contents = EmptyTree;
		}

		///	<summary>
		/// Construct a Tree object with known content and hash value
		///	</summary>
		///	<param name="repo"></param>
		///	<param name="id"></param>
		///	<param name="raw"></param>
		///	<exception cref="IOException"></exception>
		public Tree(Repository repo, ObjectId id, byte[] raw)
			: base(null, id, null)
		{
			_db = repo;
			ReadTree(raw);
		}

		///	<summary>
		/// Construct a new Tree under another Tree
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="nameUTF8"></param>
		public Tree(Tree parent, byte[] nameUTF8)
			: base(parent, null, nameUTF8)
		{
			_db = parent.Repository;
			_contents = EmptyTree;
		}

		/// <summary>
		/// Construct a Tree with a known SHA-1 under another tree. Data is not yet
		///	specified and will have to be loaded on demand.
		///	</summary>
		///	<param name="parent"></param>
		///	<param name="id"></param>
		///	<param name="nameUTF8"></param>
		public Tree(Tree parent, ObjectId id, byte[] nameUTF8)
			: base(parent, id, nameUTF8)
		{
			_db = parent.Repository;
		}

		/// <summary>
		/// Returns true of the data of this Tree is loaded.
		/// </summary>
		public bool IsLoaded
		{
			get { return _contents != null; }
		}

		///	<summary>
		/// Forget the in-memory data for this tree.
		/// </summary>
		public void Unload()
		{
			if (IsModified)
			{
				throw new InvalidOperationException("Cannot unload a modified tree.");
			}

			_contents = null;
		}

		///	<summary>
		/// Adds a new or existing file with the specified name to this tree.
		///	Trees are added if necessary as the name may contain '/':s.
		///	</summary>
		///	<param name="name"> Name </param>
		///	<returns>A <seealso cref="FileTreeEntry"/> for the added file.</returns>
		///	<exception cref="IOException"></exception>

		public FileTreeEntry AddFile(string name)
		{
			return AddFile(Repository.GitInternalSlash(Constants.encode(name)), 0);
		}

		/// <summary>
		/// Adds a new or existing file with the specified name to this tree.
		///	Trees are added if necessary as the name may contain '/':s.
		///	</summary>
		///	<param name="s"> an array containing the name </param>
		///	<param name="offset"> when the name starts in the tree.
		///	</param>
		///	<returns>A <seealso cref="FileTreeEntry"/> for the added file.</returns>
		///	<exception cref="IOException"></exception>
		public FileTreeEntry AddFile(byte[] s, int offset)
		{
			int slash;

			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
				// search for path component terminator
				// [henon] body is empty by intention!
			}

			EnsureLoaded();
            byte xlast = slash < s.Length ? (byte)'/' : (byte)0;
			int p = BinarySearch(_contents, s, xlast, offset, slash);
			if (p >= 0 && slash < s.Length && _contents[p] is Tree)
			{
				return ((Tree)_contents[p]).AddFile(s, slash + 1);
			}

			byte[] newName = SubString(s, offset, slash);
			if (p >= 0)
			{
				throw new EntryExistsException(RawParseUtils.decode(newName));
			}

			if (slash < s.Length)
			{
                Tree t = new Tree(this, newName);
				InsertEntry(p, t);
				return t.AddFile(s, slash + 1);
			}

            FileTreeEntry f = new FileTreeEntry(this, null, newName, false);
			InsertEntry(p, f);
			return f;
		}

		///	<summary>
		/// Adds a new or existing Tree with the specified name to this tree.
		///	Trees are added if necessary as the name may contain '/':s.
		///	</summary>
		///	<param name="name"></param>
		///	<returns>A <seealso cref="FileTreeEntry"/> for the added tree.</returns>
		///	<exception cref="IOException"> </exception>
		public Tree AddTree(string name)
		{
			return AddTree(Repository.GitInternalSlash(Constants.encode(name)), 0);
		}

		///	<summary>
		/// Adds a new or existing Tree with the specified name to this tree.
		///	Trees are added if necessary as the name may contain '/':s.
		///	</summary>
		///	<param name="s"> an array containing the name </param>
		///	<param name="offset"> when the name starts in the tree.</param>
		///	<returns>A <seealso cref="FileTreeEntry"/> for the added tree.</returns>
		///	<exception cref="IOException"></exception>
		public Tree AddTree(byte[] s, int offset)
		{
			int slash;

			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
				// search for path component terminator
			}

			EnsureLoaded();
			int p = BinarySearch(_contents, s, (byte)'/', offset, slash);
			if (p >= 0 && slash < s.Length && _contents[p] is Tree)
			{
				return ((Tree)_contents[p]).AddTree(s, slash + 1);
			}

			byte[] newName = SubString(s, offset, slash);
			if (p >= 0)
			{
				throw new EntryExistsException(RawParseUtils.decode(newName));
			}

            Tree t = new Tree(this, newName);
			InsertEntry(p, t);
			return slash == s.Length ? t : t.AddTree(s, slash + 1);
		}

		///	<summary>
		/// Add the specified tree entry to this tree.
		///	</summary>
		///	<param name="e"> </param>
		///	<exception cref="IOException"></exception>
		public void AddEntry(TreeEntry e)
		{
			EnsureLoaded();
			int p = BinarySearch(_contents, e.NameUTF8, GitSharp.Core.TreeEntry.LastChar(e), 0, e.NameUTF8.Length);
			if (p < 0)
			{
				e.AttachParent(this);
				InsertEntry(p, e);
			}
			else
			{
				throw new EntryExistsException(e.Name);
			}
		}

		private void InsertEntry(int p, TreeEntry e)
		{
			TreeEntry[] c = _contents;
			var n = new TreeEntry[c.Length + 1];

			p = -(p + 1);
			for (int k = c.Length - 1; k >= p; k--)
			{
				n[k + 1] = c[k];
			}

			n[p] = e;
			for (int k = p - 1; k >= 0; k--)
			{
				n[k] = c[k];
			}

			_contents = n;

			SetModified();
		}

		internal void RemoveEntry(TreeEntry e)
		{
			TreeEntry[] c = _contents;
			int p = BinarySearch(c, e.NameUTF8, GitSharp.Core.TreeEntry.LastChar(e), 0, e.NameUTF8.Length);
			if (p >= 0)
			{
				var n = new TreeEntry[c.Length - 1];
				for (int k = c.Length - 1; k > p; k--)
				{
					n[k - 1] = c[k];
				}

				for (int k = p - 1; k >= 0; k--)
				{
					n[k] = c[k];
				}

				_contents = n;
				SetModified();
			}
		}

		///	<summary>
		/// Gets the number of members in this tree.
		/// </summary>
		///	<exception cref="IOException"></exception>
		public virtual  int MemberCount
		{
			get
			{
				EnsureLoaded();
				return _contents.Length;
			}
		}

		///	<summary>
		/// Return all members of the tree sorted in Git order.
		///	<para />
		///	Entries are sorted by the numerical unsigned byte
		///	values with (sub)trees having an implicit '/'. An
		///	example of a tree with three entries. a:b is an
		///	actual file name here.
		///	<para />
		///	100644 blob e69de29bb2d1d6434b8b29ae775ad8c2e48c5391    a.b
		///	040000 tree 4277b6e69d25e5efa77c455340557b384a4c018a    a
		///	100644 blob e69de29bb2d1d6434b8b29ae775ad8c2e48c5391    a:b
		///	</summary>
		///	<returns>All entries in this Tree, sorted.</returns>
		///	<exception cref="IOException"></exception>
		public virtual TreeEntry[] Members
		{
			get
			{
				EnsureLoaded();
				TreeEntry[] c = _contents;
				if (c.Length != 0)
				{
					var r = new TreeEntry[c.Length];
					for (int k = c.Length - 1; k >= 0; k--)
					{
						r[k] = c[k];
					}

					return r;
				}

				return c;
			}
		}

		private bool Exists(string s, byte slast)
		{
			return FindMember(s, slast) != null;
		}

		///	<param name="path">Path to the tree.</param>
		///	<returns>
		///	True if a tree with the specified path can be found under this
		///	tree. </returns>
		///	<exception cref="IOException"></exception>
		public bool ExistsTree(string path)
		{
			return Exists(path, (byte)'/');
		}

		/// <param name="path"></param>
		/// <returns>
		/// True if a blob or symlink with the specified name can be found
		/// under this tree.
		/// </returns>
		public bool ExistsBlob(string path)
		{
			return Exists(path, 0);
		}

		private TreeEntry FindMember(string s, byte slast)
		{
			return FindMember(Repository.GitInternalSlash(Constants.encode(s)), slast, 0);
		}

		private TreeEntry FindMember(byte[] s, byte slast, int offset)
		{
			int slash;

			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
				// search for path component terminator
				// [henon] body is intentionally empty!
			}

			EnsureLoaded();
			byte xlast = slash < s.Length ? (byte)'/' : slast;
			int p = BinarySearch(_contents, s, xlast, offset, slash);
			if (p >= 0)
			{
				TreeEntry r = _contents[p];
				if (slash < s.Length - 1)
				{
					Tree oTree = (r as Tree);
					return oTree != null ? oTree.FindMember(s, slast, slash + 1) : null;
				}

				return r;
			}

			return null;
		}

		/// <param name="blobName"></param>
		/// <returns>
		/// a <see cref="TreeEntry"/> representing an object with the specified
		/// relative path.
		/// </returns>
		public TreeEntry FindBlobMember(string blobName)
		{
			return FindMember(blobName, 0);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="treeName">Tree name</param>
		/// <returns>return a <see cref="Tree"/> with the name treeName or null</returns>
		public TreeEntry findTreeMember(string treeName)
		{
			return FindMember(treeName, (byte)'/');
		}

		public override void Accept(TreeVisitor tv, int flags)
		{
			if ((MODIFIED_ONLY & flags) == MODIFIED_ONLY && !IsModified) return;

			if ((LOADED_ONLY & flags) == LOADED_ONLY && !IsLoaded)
			{
				tv.StartVisitTree(this);
				tv.EndVisitTree(this);
				return;
			}

			EnsureLoaded();
			tv.StartVisitTree(this);

			TreeEntry[] c = (CONCURRENT_MODIFICATION & flags) == CONCURRENT_MODIFICATION ? Members : _contents;

			for (int k = 0; k < c.Length; k++)
			{
				c[k].Accept(tv, flags);
			}

			tv.EndVisitTree(this);
		}
        
		private void EnsureLoaded()
		{
			if (IsLoaded) return;

			ObjectLoader or = _db.OpenTree(Id);
			if (or == null)
			{
				throw new MissingObjectException(Id, ObjectType.Tree);
			}

			ReadTree(or.Bytes);
		}

		private void ReadTree(byte[] raw)
		{
			int rawSize = raw.Length;
			int rawPtr = 0;
			int nextIndex = 0;

			while (rawPtr < rawSize)
			{
				while (rawPtr < rawSize && raw[rawPtr] != 0)
				{
					rawPtr++;
				}

				rawPtr++;
				rawPtr += Constants.OBJECT_ID_LENGTH;
				nextIndex++;
			}

			var temp = new TreeEntry[nextIndex];
			rawPtr = 0;
			nextIndex = 0;

			while (rawPtr < rawSize)
			{
				int c = raw[rawPtr++];
				if (c < '0' || c > '7')
				{
					throw new CorruptObjectException(Id, "invalid entry mode");
				}

				int mode = c - '0';

				while (true)
				{
					c = raw[rawPtr++];
					if (' ' == c) break;

					if (c < '0' || c > '7')
					{
						throw new CorruptObjectException(Id, "invalid mode");
					}

					mode <<= 3;
					mode += c - '0';
				}

				int nameLen = 0;
				while (raw[rawPtr + nameLen] != 0)
				{
					nameLen++;
				}

				var name = new byte[nameLen];
				Array.Copy(raw, rawPtr, name, 0, nameLen);
				rawPtr += nameLen + 1;

				ObjectId id = ObjectId.FromRaw(raw, rawPtr);
				rawPtr += Constants.OBJECT_ID_LENGTH;

				TreeEntry ent;
				if (FileMode.RegularFile.Equals(mode))
				{
					ent = new FileTreeEntry(this, id, name, false);
				}
				else if (FileMode.ExecutableFile.Equals(mode))
				{
					ent = new FileTreeEntry(this, id, name, true);
				}
				else if (FileMode.Tree.Equals(mode))
				{
					ent = new Tree(this, id, name);
				}
                else if (FileMode.Symlink.Equals(mode))
                {
                    ent = new SymlinkTreeEntry(this, id, name);
                }
                else if (FileMode.GitLink.Equals(mode))
                {
                    ent = new GitLinkTreeEntry(this, id, name);
                }
				else
				{
					throw new CorruptObjectException(Id, "Invalid mode: " + Convert.ToString(mode, 8));
				}

				temp[nextIndex++] = ent;
			}

			_contents = temp;
		}

		public ObjectId TreeId
		{
			get { return Id; }
		}

		public Tree TreeEntry
		{
			get { return this; }
		}

		public override string ToString()
		{
			var r = new StringBuilder();
			r.Append(ObjectId.ToString(Id));
			r.Append(" T ");
			r.Append(FullName);
			return r.ToString();
		}
	}
}
