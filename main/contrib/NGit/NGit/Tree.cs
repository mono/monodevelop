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
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>A representation of a Git tree entry.</summary>
	/// <remarks>A representation of a Git tree entry. A Tree is a directory in Git.</remarks>
	[System.ObsoleteAttribute(@"To look up information about a single path, useNGit.Treewalk.TreeWalk.ForPath(Repository, string, NGit.Revwalk.RevTree) . To lookup information about multiple paths at once, use aNGit.Treewalk.TreeWalk and obtain the current entry's information from its getter methods."
		)]
	public class Tree : TreeEntry
	{
		private static readonly TreeEntry[] EMPTY_TREE = new TreeEntry[] {  };

		/// <summary>Compare two names represented as bytes.</summary>
		/// <remarks>
		/// Compare two names represented as bytes. Since git treats names of trees and
		/// blobs differently we have one parameter that represents a '/' for trees. For
		/// other objects the value should be NUL. The names are compare by their positive
		/// byte value (0..255).
		/// A blob and a tree with the same name will not compare equal.
		/// </remarks>
		/// <param name="a">name</param>
		/// <param name="b">name</param>
		/// <param name="lasta">'/' if a is a tree, else NUL</param>
		/// <param name="lastb">'/' if b is a tree, else NUL</param>
		/// <returns>&lt; 0 if a is sorted before b, 0 if they are the same, else b</returns>
		public static int CompareNames(byte[] a, byte[] b, int lasta, int lastb)
		{
			return CompareNames(a, b, 0, b.Length, lasta, lastb);
		}

		private static int CompareNames(byte[] a, byte[] nameUTF8, int nameStart, int nameEnd
			, int lasta, int lastb)
		{
			int j;
			int k;
			for (j = 0, k = nameStart; j < a.Length && k < nameEnd; j++, k++)
			{
				int aj = a[j] & unchecked((int)(0xff));
				int bk = nameUTF8[k] & unchecked((int)(0xff));
				if (aj < bk)
				{
					return -1;
				}
				else
				{
					if (aj > bk)
					{
						return 1;
					}
				}
			}
			if (j < a.Length)
			{
				int aj = a[j] & unchecked((int)(0xff));
				if (aj < lastb)
				{
					return -1;
				}
				else
				{
					if (aj > lastb)
					{
						return 1;
					}
					else
					{
						if (j == a.Length - 1)
						{
							return 0;
						}
						else
						{
							return -1;
						}
					}
				}
			}
			if (k < nameEnd)
			{
				int bk = nameUTF8[k] & unchecked((int)(0xff));
				if (lasta < bk)
				{
					return -1;
				}
				else
				{
					if (lasta > bk)
					{
						return 1;
					}
					else
					{
						if (k == nameEnd - 1)
						{
							return 0;
						}
						else
						{
							return 1;
						}
					}
				}
			}
			if (lasta < lastb)
			{
				return -1;
			}
			else
			{
				if (lasta > lastb)
				{
					return 1;
				}
			}
			int namelength = nameEnd - nameStart;
			if (a.Length == namelength)
			{
				return 0;
			}
			else
			{
				if (a.Length < namelength)
				{
					return -1;
				}
				else
				{
					return 1;
				}
			}
		}

		private static byte[] Substring(byte[] s, int nameStart, int nameEnd)
		{
			if (nameStart == 0 && nameStart == s.Length)
			{
				return s;
			}
			byte[] n = new byte[nameEnd - nameStart];
			System.Array.Copy(s, nameStart, n, 0, n.Length);
			return n;
		}

		private static int BinarySearch(TreeEntry[] entries, byte[] nameUTF8, int nameUTF8last
			, int nameStart, int nameEnd)
		{
			if (entries.Length == 0)
			{
				return -1;
			}
			int high = entries.Length;
			int low = 0;
			do
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				int cmp = CompareNames(entries[mid].GetNameUTF8(), nameUTF8, nameStart, nameEnd, 
					TreeEntry.LastChar(entries[mid]), nameUTF8last);
				if (cmp < 0)
				{
					low = mid + 1;
				}
				else
				{
					if (cmp == 0)
					{
						return mid;
					}
					else
					{
						high = mid;
					}
				}
			}
			while (low < high);
			return -(low + 1);
		}

		private readonly Repository db;

		private TreeEntry[] contents;

		/// <summary>Constructor for a new Tree</summary>
		/// <param name="repo">The repository that owns the Tree.</param>
		public Tree(Repository repo) : base(null, null, null)
		{
			db = repo;
			contents = EMPTY_TREE;
		}

		/// <summary>Construct a Tree object with known content and hash value</summary>
		/// <param name="repo"></param>
		/// <param name="myId"></param>
		/// <param name="raw"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public Tree(Repository repo, ObjectId myId, byte[] raw) : base(null, myId, null)
		{
			db = repo;
			ReadTree(raw);
		}

		/// <summary>Construct a new Tree under another Tree</summary>
		/// <param name="parent"></param>
		/// <param name="nameUTF8"></param>
		public Tree(NGit.Tree parent, byte[] nameUTF8) : base(parent, null, nameUTF8)
		{
			db = parent.GetRepository();
			contents = EMPTY_TREE;
		}

		/// <summary>Construct a Tree with a known SHA-1 under another tree.</summary>
		/// <remarks>
		/// Construct a Tree with a known SHA-1 under another tree. Data is not yet
		/// specified and will have to be loaded on demand.
		/// </remarks>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="nameUTF8"></param>
		protected internal Tree(NGit.Tree parent, ObjectId id, byte[] nameUTF8) : base(parent
			, id, nameUTF8)
		{
			db = parent.GetRepository();
		}

		public override FileMode GetMode()
		{
			return FileMode.TREE;
		}

		/// <returns>true if this Tree is the top level Tree.</returns>
		public virtual bool IsRoot()
		{
			return GetParent() == null;
		}

		public override Repository GetRepository()
		{
			return db;
		}

		/// <returns>true of the data of this Tree is loaded</returns>
		public virtual bool IsLoaded()
		{
			return contents != null;
		}

		/// <summary>Forget the in-memory data for this tree.</summary>
		/// <remarks>Forget the in-memory data for this tree.</remarks>
		public virtual void Unload()
		{
			if (IsModified())
			{
				throw new InvalidOperationException(JGitText.Get().cannotUnloadAModifiedTree);
			}
			contents = null;
		}

		/// <summary>Adds a new or existing file with the specified name to this tree.</summary>
		/// <remarks>
		/// Adds a new or existing file with the specified name to this tree.
		/// Trees are added if necessary as the name may contain '/':s.
		/// </remarks>
		/// <param name="name">Name</param>
		/// <returns>
		/// a
		/// <see cref="FileTreeEntry">FileTreeEntry</see>
		/// for the added file.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual FileTreeEntry AddFile(string name)
		{
			return AddFile(Repository.GitInternalSlash(Constants.Encode(name)), 0);
		}

		/// <summary>Adds a new or existing file with the specified name to this tree.</summary>
		/// <remarks>
		/// Adds a new or existing file with the specified name to this tree.
		/// Trees are added if necessary as the name may contain '/':s.
		/// </remarks>
		/// <param name="s">an array containing the name</param>
		/// <param name="offset">when the name starts in the tree.</param>
		/// <returns>
		/// a
		/// <see cref="FileTreeEntry">FileTreeEntry</see>
		/// for the added file.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual FileTreeEntry AddFile(byte[] s, int offset)
		{
			int slash;
			int p;
			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
			}
			// search for path component terminator
			EnsureLoaded();
			byte xlast = slash < s.Length ? unchecked((byte)(byte)('/')) : (byte)0;
			p = BinarySearch(contents, s, xlast, offset, slash);
			if (p >= 0 && slash < s.Length && contents[p] is NGit.Tree)
			{
				return ((NGit.Tree)contents[p]).AddFile(s, slash + 1);
			}
			byte[] newName = Substring(s, offset, slash);
			if (p >= 0)
			{
				throw new EntryExistsException(RawParseUtils.Decode(newName));
			}
			else
			{
				if (slash < s.Length)
				{
					NGit.Tree t = new NGit.Tree(this, newName);
					InsertEntry(p, t);
					return t.AddFile(s, slash + 1);
				}
				else
				{
					FileTreeEntry f = new FileTreeEntry(this, null, newName, false);
					InsertEntry(p, f);
					return f;
				}
			}
		}

		/// <summary>Adds a new or existing Tree with the specified name to this tree.</summary>
		/// <remarks>
		/// Adds a new or existing Tree with the specified name to this tree.
		/// Trees are added if necessary as the name may contain '/':s.
		/// </remarks>
		/// <param name="name">Name</param>
		/// <returns>
		/// a
		/// <see cref="FileTreeEntry">FileTreeEntry</see>
		/// for the added tree.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NGit.Tree AddTree(string name)
		{
			return AddTree(Repository.GitInternalSlash(Constants.Encode(name)), 0);
		}

		/// <summary>Adds a new or existing Tree with the specified name to this tree.</summary>
		/// <remarks>
		/// Adds a new or existing Tree with the specified name to this tree.
		/// Trees are added if necessary as the name may contain '/':s.
		/// </remarks>
		/// <param name="s">an array containing the name</param>
		/// <param name="offset">when the name starts in the tree.</param>
		/// <returns>
		/// a
		/// <see cref="FileTreeEntry">FileTreeEntry</see>
		/// for the added tree.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NGit.Tree AddTree(byte[] s, int offset)
		{
			int slash;
			int p;
			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
			}
			// search for path component terminator
			EnsureLoaded();
			p = BinarySearch(contents, s, unchecked((byte)'/'), offset, slash);
			if (p >= 0 && slash < s.Length && contents[p] is NGit.Tree)
			{
				return ((NGit.Tree)contents[p]).AddTree(s, slash + 1);
			}
			byte[] newName = Substring(s, offset, slash);
			if (p >= 0)
			{
				throw new EntryExistsException(RawParseUtils.Decode(newName));
			}
			NGit.Tree t = new NGit.Tree(this, newName);
			InsertEntry(p, t);
			return slash == s.Length ? t : t.AddTree(s, slash + 1);
		}

		/// <summary>Add the specified tree entry to this tree.</summary>
		/// <remarks>Add the specified tree entry to this tree.</remarks>
		/// <param name="e"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void AddEntry(TreeEntry e)
		{
			int p;
			EnsureLoaded();
			p = BinarySearch(contents, e.GetNameUTF8(), TreeEntry.LastChar(e), 0, e.GetNameUTF8
				().Length);
			if (p < 0)
			{
				e.AttachParent(this);
				InsertEntry(p, e);
			}
			else
			{
				throw new EntryExistsException(e.GetName());
			}
		}

		private void InsertEntry(int p, TreeEntry e)
		{
			TreeEntry[] c = contents;
			TreeEntry[] n = new TreeEntry[c.Length + 1];
			p = -(p + 1);
			for (int k = c.Length - 1; k >= p; k--)
			{
				n[k + 1] = c[k];
			}
			n[p] = e;
			for (int k_1 = p - 1; k_1 >= 0; k_1--)
			{
				n[k_1] = c[k_1];
			}
			contents = n;
			SetModified();
		}

		internal virtual void RemoveEntry(TreeEntry e)
		{
			TreeEntry[] c = contents;
			int p = BinarySearch(c, e.GetNameUTF8(), TreeEntry.LastChar(e), 0, e.GetNameUTF8(
				).Length);
			if (p >= 0)
			{
				TreeEntry[] n = new TreeEntry[c.Length - 1];
				for (int k = c.Length - 1; k > p; k--)
				{
					n[k - 1] = c[k];
				}
				for (int k_1 = p - 1; k_1 >= 0; k_1--)
				{
					n[k_1] = c[k_1];
				}
				contents = n;
				SetModified();
			}
		}

		/// <returns>number of members in this tree</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual int MemberCount()
		{
			EnsureLoaded();
			return contents.Length;
		}

		/// <summary>Return all members of the tree sorted in Git order.</summary>
		/// <remarks>
		/// Return all members of the tree sorted in Git order.
		/// Entries are sorted by the numerical unsigned byte
		/// values with (sub)trees having an implicit '/'. An
		/// example of a tree with three entries. a:b is an
		/// actual file name here.
		/// <p>
		/// 100644 blob e69de29bb2d1d6434b8b29ae775ad8c2e48c5391    a.b
		/// 040000 tree 4277b6e69d25e5efa77c455340557b384a4c018a    a
		/// 100644 blob e69de29bb2d1d6434b8b29ae775ad8c2e48c5391    a:b
		/// </remarks>
		/// <returns>all entries in this Tree, sorted.</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual TreeEntry[] Members()
		{
			EnsureLoaded();
			TreeEntry[] c = contents;
			if (c.Length != 0)
			{
				TreeEntry[] r = new TreeEntry[c.Length];
				for (int k = c.Length - 1; k >= 0; k--)
				{
					r[k] = c[k];
				}
				return r;
			}
			else
			{
				return c;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Exists(string s, byte slast)
		{
			return FindMember(s, slast) != null;
		}

		/// <param name="path">to the tree.</param>
		/// <returns>
		/// true if a tree with the specified path can be found under this
		/// tree.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool ExistsTree(string path)
		{
			return Exists(path, unchecked((byte)'/'));
		}

		/// <param name="path">of the non-tree entry.</param>
		/// <returns>
		/// true if a blob, symlink, or gitlink with the specified name
		/// can be found under this tree.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool ExistsBlob(string path)
		{
			return Exists(path, unchecked((byte)0));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TreeEntry FindMember(string s, byte slast)
		{
			return FindMember(Repository.GitInternalSlash(Constants.Encode(s)), slast, 0);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private TreeEntry FindMember(byte[] s, byte slast, int offset)
		{
			int slash;
			int p;
			for (slash = offset; slash < s.Length && s[slash] != '/'; slash++)
			{
			}
			// search for path component terminator
			EnsureLoaded();
			byte xlast = slash < s.Length ? unchecked((byte)(byte)('/')) : slast;
			p = BinarySearch(contents, s, xlast, offset, slash);
			if (p >= 0)
			{
				TreeEntry r = contents[p];
				if (slash < s.Length - 1)
				{
					return r is NGit.Tree ? ((NGit.Tree)r).FindMember(s, slast, slash + 1) : null;
				}
				return r;
			}
			return null;
		}

		/// <param name="s">blob name</param>
		/// <returns>
		/// a
		/// <see cref="TreeEntry">TreeEntry</see>
		/// representing an object with the specified
		/// relative path.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual TreeEntry FindBlobMember(string s)
		{
			return FindMember(s, unchecked((byte)0));
		}

		/// <param name="s">Tree Name</param>
		/// <returns>a Tree with the name s or null</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual TreeEntry FindTreeMember(string s)
		{
			return FindMember(s, unchecked((byte)'/'));
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		private void EnsureLoaded()
		{
			if (!IsLoaded())
			{
				ObjectLoader ldr = db.Open(GetId(), Constants.OBJ_TREE);
				ReadTree(ldr.GetCachedBytes());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadTree(byte[] raw)
		{
			int rawSize = raw.Length;
			int rawPtr = 0;
			TreeEntry[] temp;
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
			temp = new TreeEntry[nextIndex];
			rawPtr = 0;
			nextIndex = 0;
			while (rawPtr < rawSize)
			{
				int c = raw[rawPtr++];
				if (c < '0' || c > '7')
				{
					throw new CorruptObjectException(GetId(), JGitText.Get().corruptObjectInvalidEntryMode
						);
				}
				int mode = c - '0';
				for (; ; )
				{
					c = raw[rawPtr++];
					if (' ' == c)
					{
						break;
					}
					else
					{
						if (c < '0' || c > '7')
						{
							throw new CorruptObjectException(GetId(), JGitText.Get().corruptObjectInvalidMode
								);
						}
					}
					mode <<= 3;
					mode += c - '0';
				}
				int nameLen = 0;
				while (raw[rawPtr + nameLen] != 0)
				{
					nameLen++;
				}
				byte[] name = new byte[nameLen];
				System.Array.Copy(raw, rawPtr, name, 0, nameLen);
				rawPtr += nameLen + 1;
				ObjectId id = ObjectId.FromRaw(raw, rawPtr);
				rawPtr += Constants.OBJECT_ID_LENGTH;
				TreeEntry ent;
				if (FileMode.REGULAR_FILE.Equals(mode))
				{
					ent = new FileTreeEntry(this, id, name, false);
				}
				else
				{
					if (FileMode.EXECUTABLE_FILE.Equals(mode))
					{
						ent = new FileTreeEntry(this, id, name, true);
					}
					else
					{
						if (FileMode.TREE.Equals(mode))
						{
							ent = new NGit.Tree(this, id, name);
						}
						else
						{
							if (FileMode.SYMLINK.Equals(mode))
							{
								ent = new SymlinkTreeEntry(this, id, name);
							}
							else
							{
								if (FileMode.GITLINK.Equals(mode))
								{
									ent = new GitlinkTreeEntry(this, id, name);
								}
								else
								{
									throw new CorruptObjectException(GetId(), MessageFormat.Format(JGitText.Get().corruptObjectInvalidMode2
										, Sharpen.Extensions.ToOctalString(mode)));
								}
							}
						}
					}
				}
				temp[nextIndex++] = ent;
			}
			contents = temp;
		}

		/// <summary>Format this Tree in canonical format.</summary>
		/// <remarks>Format this Tree in canonical format.</remarks>
		/// <returns>canonical encoding of the tree object.</returns>
		/// <exception cref="System.IO.IOException">the tree cannot be loaded, or its not in a writable state.
		/// 	</exception>
		public virtual byte[] Format()
		{
			TreeFormatter fmt = new TreeFormatter();
			foreach (TreeEntry e in Members())
			{
				ObjectId id = e.GetId();
				if (id == null)
				{
					throw new ObjectWritingException(MessageFormat.Format(JGitText.Get().objectAtPathDoesNotHaveId
						, e.GetFullName()));
				}
				fmt.Append(e.GetNameUTF8(), e.GetMode(), id);
			}
			return fmt.ToByteArray();
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append(ObjectId.ToString(GetId()));
			r.Append(" T ");
			r.Append(GetFullName());
			return r.ToString();
		}
	}
}
