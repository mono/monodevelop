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
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>This class represents an entry in a tree, like a blob or another tree.</summary>
	/// <remarks>This class represents an entry in a tree, like a blob or another tree.</remarks>
	[System.ObsoleteAttribute(@"To look up information about a single path, useNGit.Treewalk.TreeWalk.ForPath(Repository, string, NGit.Revwalk.RevTree) . To lookup information about multiple paths at once, use a and obtain the current entry's information from its getter methods."
		)]
	public abstract class TreeEntry : IComparable
	{
		/// <summary>
		/// a flag for
		/// <see cref="Accept(TreeVisitor, int)">Accept(TreeVisitor, int)</see>
		/// to visit only modified entries
		/// </summary>
		public const int MODIFIED_ONLY = 1 << 0;

		/// <summary>
		/// a flag for
		/// <see cref="Accept(TreeVisitor, int)">Accept(TreeVisitor, int)</see>
		/// to visit only loaded entries
		/// </summary>
		public const int LOADED_ONLY = 1 << 1;

		/// <summary>
		/// a flag for
		/// <see cref="Accept(TreeVisitor, int)">Accept(TreeVisitor, int)</see>
		/// obsolete?
		/// </summary>
		public const int CONCURRENT_MODIFICATION = 1 << 2;

		private byte[] nameUTF8;

		private Tree parent;

		private ObjectId id;

		/// <summary>Construct a named tree entry.</summary>
		/// <remarks>Construct a named tree entry.</remarks>
		/// <param name="myParent"></param>
		/// <param name="myId"></param>
		/// <param name="myNameUTF8"></param>
		protected internal TreeEntry(Tree myParent, ObjectId myId, byte[] myNameUTF8)
		{
			nameUTF8 = myNameUTF8;
			parent = myParent;
			id = myId;
		}

		/// <returns>parent of this tree.</returns>
		public virtual Tree GetParent()
		{
			return parent;
		}

		/// <summary>Delete this entry.</summary>
		/// <remarks>Delete this entry.</remarks>
		public virtual void Delete()
		{
			GetParent().RemoveEntry(this);
			DetachParent();
		}

		/// <summary>Detach this entry from it's parent.</summary>
		/// <remarks>Detach this entry from it's parent.</remarks>
		public virtual void DetachParent()
		{
			parent = null;
		}

		internal virtual void AttachParent(Tree p)
		{
			parent = p;
		}

		/// <returns>the repository owning this entry.</returns>
		public virtual Repository GetRepository()
		{
			return GetParent().GetRepository();
		}

		/// <returns>the raw byte name of this entry.</returns>
		public virtual byte[] GetNameUTF8()
		{
			return nameUTF8;
		}

		/// <returns>the name of this entry.</returns>
		public virtual string GetName()
		{
			if (nameUTF8 != null)
			{
				return RawParseUtils.Decode(nameUTF8);
			}
			return null;
		}

		/// <summary>Rename this entry.</summary>
		/// <remarks>Rename this entry.</remarks>
		/// <param name="n">The new name</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Rename(string n)
		{
			Rename(Constants.Encode(n));
		}

		/// <summary>Rename this entry.</summary>
		/// <remarks>Rename this entry.</remarks>
		/// <param name="n">The new name</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Rename(byte[] n)
		{
			Tree t = GetParent();
			if (t != null)
			{
				Delete();
			}
			nameUTF8 = n;
			if (t != null)
			{
				t.AddEntry(this);
			}
		}

		/// <returns>true if this entry is new or modified since being loaded.</returns>
		public virtual bool IsModified()
		{
			return GetId() == null;
		}

		/// <summary>Mark this entry as modified.</summary>
		/// <remarks>Mark this entry as modified.</remarks>
		public virtual void SetModified()
		{
			SetId(null);
		}

		/// <returns>SHA-1 of this tree entry (null for new unhashed entries)</returns>
		public virtual ObjectId GetId()
		{
			return id;
		}

		/// <summary>Set (update) the SHA-1 of this entry.</summary>
		/// <remarks>
		/// Set (update) the SHA-1 of this entry. Invalidates the id's of all
		/// entries above this entry as they will have to be recomputed.
		/// </remarks>
		/// <param name="n">SHA-1 for this entry.</param>
		public virtual void SetId(ObjectId n)
		{
			// If we have a parent and our id is being cleared or changed then force
			// the parent's id to become unset as it depends on our id.
			//
			Tree p = GetParent();
			if (p != null && id != n)
			{
				if ((id == null && n != null) || (id != null && n == null) || !id.Equals(n))
				{
					p.SetId(null);
				}
			}
			id = n;
		}

		/// <returns>repository relative name of this entry</returns>
		public virtual string GetFullName()
		{
			StringBuilder r = new StringBuilder();
			AppendFullName(r);
			return r.ToString();
		}

		/// <returns>
		/// repository relative name of the entry
		/// FIXME better encoding
		/// </returns>
		public virtual byte[] GetFullNameUTF8()
		{
			return Sharpen.Runtime.GetBytesForString(GetFullName());
		}

		public virtual int CompareTo(object o)
		{
			if (this == o)
			{
				return 0;
			}
			if (o is NGit.TreeEntry)
			{
				return Tree.CompareNames(nameUTF8, ((NGit.TreeEntry)o).nameUTF8, LastChar(this), 
					LastChar((NGit.TreeEntry)o));
			}
			return -1;
		}

		/// <summary>Helper for accessing tree/blob methods.</summary>
		/// <remarks>Helper for accessing tree/blob methods.</remarks>
		/// <param name="treeEntry"></param>
		/// <returns>'/' for Tree entries and NUL for non-treeish objects.</returns>
		public static int LastChar(NGit.TreeEntry treeEntry)
		{
			if (!(treeEntry is Tree))
			{
				return '\0';
			}
			else
			{
				return '/';
			}
		}

		/// <summary>Helper for accessing tree/blob/index methods.</summary>
		/// <remarks>Helper for accessing tree/blob/index methods.</remarks>
		/// <param name="i"></param>
		/// <returns>'/' for Tree entries and NUL for non-treeish objects</returns>
		public static int LastChar(GitIndex.Entry i)
		{
			// FIXME, gitlink etc. Currently Trees cannot appear in the
			// index so '\0' is always returned, except maybe for submodules
			// which we do not support yet.
			return FileMode.TREE.Equals(i.GetModeBits()) ? '/' : '\0';
		}

		/// <summary>
		/// See @{link
		/// <see cref="Accept(TreeVisitor, int)">Accept(TreeVisitor, int)</see>
		/// .
		/// </summary>
		/// <param name="tv"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Accept(TreeVisitor tv)
		{
			Accept(tv, 0);
		}

		/// <summary>Visit the members of this TreeEntry.</summary>
		/// <remarks>Visit the members of this TreeEntry.</remarks>
		/// <param name="tv">A visitor object doing the work</param>
		/// <param name="flags">
		/// Specification for what members to visit. See
		/// <see cref="MODIFIED_ONLY">MODIFIED_ONLY</see>
		/// ,
		/// <see cref="LOADED_ONLY">LOADED_ONLY</see>
		/// ,
		/// <see cref="CONCURRENT_MODIFICATION">CONCURRENT_MODIFICATION</see>
		/// .
		/// </param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public abstract void Accept(TreeVisitor tv, int flags);

		/// <returns>mode (type of object)</returns>
		public abstract FileMode GetMode();

		private void AppendFullName(StringBuilder r)
		{
			NGit.TreeEntry p = GetParent();
			string n = GetName();
			if (p != null)
			{
				p.AppendFullName(r);
				if (r.Length > 0)
				{
					r.Append('/');
				}
			}
			if (n != null)
			{
				r.Append(n);
			}
		}
	}
}
