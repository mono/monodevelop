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
using NGit;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>Walks a Git tree (directory) in Git sort order.</summary>
	/// <remarks>
	/// Walks a Git tree (directory) in Git sort order.
	/// <p>
	/// A new iterator instance should be positioned on the first entry, or at eof.
	/// Data for the first entry (if not at eof) should be available immediately.
	/// <p>
	/// Implementors must walk a tree in the Git sort order, which has the following
	/// odd sorting:
	/// <ol>
	/// <li>A.c</li>
	/// <li>A/c</li>
	/// <li>A0c</li>
	/// </ol>
	/// <p>
	/// In the second item, <code>A</code> is the name of a subtree and
	/// <code>c</code> is a file within that subtree. The other two items are files
	/// in the root level tree.
	/// </remarks>
	/// <seealso cref="CanonicalTreeParser">CanonicalTreeParser</seealso>
	public abstract class AbstractTreeIterator
	{
		/// <summary>
		/// Default size for the
		/// <see cref="path">path</see>
		/// buffer.
		/// </summary>
		protected internal const int DEFAULT_PATH_SIZE = 128;

		/// <summary>A dummy object id buffer that matches the zero ObjectId.</summary>
		/// <remarks>A dummy object id buffer that matches the zero ObjectId.</remarks>
		protected internal static readonly byte[] zeroid = new byte[Constants.OBJECT_ID_LENGTH
			];

		/// <summary>Iterator for the parent tree; null if we are the root iterator.</summary>
		/// <remarks>Iterator for the parent tree; null if we are the root iterator.</remarks>
		internal readonly NGit.Treewalk.AbstractTreeIterator parent;

		/// <summary>The iterator this current entry is path equal to.</summary>
		/// <remarks>The iterator this current entry is path equal to.</remarks>
		internal NGit.Treewalk.AbstractTreeIterator matches;

		/// <summary>Number of entries we moved forward to force a D/F conflict match.</summary>
		/// <remarks>Number of entries we moved forward to force a D/F conflict match.</remarks>
		/// <seealso cref="NameConflictTreeWalk">NameConflictTreeWalk</seealso>
		internal int matchShift;

		/// <summary>Mode bits for the current entry.</summary>
		/// <remarks>
		/// Mode bits for the current entry.
		/// <p>
		/// A numerical value from FileMode is usually faster for an iterator to
		/// obtain from its data source so this is the preferred representation.
		/// </remarks>
		/// <seealso cref="NGit.FileMode">NGit.FileMode</seealso>
		protected internal int mode;

		/// <summary>Path buffer for the current entry.</summary>
		/// <remarks>
		/// Path buffer for the current entry.
		/// <p>
		/// This buffer is pre-allocated at the start of walking and is shared from
		/// parent iterators down into their subtree iterators. The sharing allows
		/// the current entry to always be a full path from the root, while each
		/// subtree only needs to populate the part that is under their control.
		/// </remarks>
		protected internal byte[] path;

		/// <summary>
		/// Position within
		/// <see cref="path">path</see>
		/// this iterator starts writing at.
		/// <p>
		/// This is the first offset in
		/// <see cref="path">path</see>
		/// that this iterator must
		/// populate during
		/// <see cref="Next(int)">Next(int)</see>
		/// . At the root level (when
		/// <see cref="parent">parent</see>
		/// is null) this is 0. For a subtree iterator the index before this position
		/// should have the value '/'.
		/// </summary>
		protected internal readonly int pathOffset;

		/// <summary>Total length of the current entry's complete path from the root.</summary>
		/// <remarks>
		/// Total length of the current entry's complete path from the root.
		/// <p>
		/// This is the number of bytes within
		/// <see cref="path">path</see>
		/// that pertain to the
		/// current entry. Values at this index through the end of the array are
		/// garbage and may be randomly populated from prior entries.
		/// </remarks>
		protected internal int pathLen;

		/// <summary>Create a new iterator with no parent.</summary>
		/// <remarks>Create a new iterator with no parent.</remarks>
		public AbstractTreeIterator()
		{
			parent = null;
			path = new byte[DEFAULT_PATH_SIZE];
			pathOffset = 0;
		}

		/// <summary>Create a new iterator with no parent and a prefix.</summary>
		/// <remarks>
		/// Create a new iterator with no parent and a prefix.
		/// <p>
		/// The prefix path supplied is inserted in front of all paths generated by
		/// this iterator. It is intended to be used when an iterator is being
		/// created for a subsection of an overall repository and needs to be
		/// combined with other iterators that are created to run over the entire
		/// repository namespace.
		/// </remarks>
		/// <param name="prefix">
		/// position of this iterator in the repository tree. The value
		/// may be null or the empty string to indicate the prefix is the
		/// root of the repository. A trailing slash ('/') is
		/// automatically appended if the prefix does not end in '/'.
		/// </param>
		protected internal AbstractTreeIterator(string prefix)
		{
			parent = null;
			if (prefix != null && prefix.Length > 0)
			{
				ByteBuffer b;
				b = Constants.CHARSET.Encode(CharBuffer.Wrap(prefix));
				pathLen = b.Limit();
				path = new byte[Math.Max(DEFAULT_PATH_SIZE, pathLen + 1)];
				b.Get(path, 0, pathLen);
				if (path[pathLen - 1] != '/')
				{
					path[pathLen++] = (byte)('/');
				}
				pathOffset = pathLen;
			}
			else
			{
				path = new byte[DEFAULT_PATH_SIZE];
				pathOffset = 0;
			}
		}

		/// <summary>Create a new iterator with no parent and a prefix.</summary>
		/// <remarks>
		/// Create a new iterator with no parent and a prefix.
		/// <p>
		/// The prefix path supplied is inserted in front of all paths generated by
		/// this iterator. It is intended to be used when an iterator is being
		/// created for a subsection of an overall repository and needs to be
		/// combined with other iterators that are created to run over the entire
		/// repository namespace.
		/// </remarks>
		/// <param name="prefix">
		/// position of this iterator in the repository tree. The value
		/// may be null or the empty array to indicate the prefix is the
		/// root of the repository. A trailing slash ('/') is
		/// automatically appended if the prefix does not end in '/'.
		/// </param>
		protected internal AbstractTreeIterator(byte[] prefix)
		{
			parent = null;
			if (prefix != null && prefix.Length > 0)
			{
				pathLen = prefix.Length;
				path = new byte[Math.Max(DEFAULT_PATH_SIZE, pathLen + 1)];
				System.Array.Copy(prefix, 0, path, 0, pathLen);
				if (path[pathLen - 1] != '/')
				{
					path[pathLen++] = (byte)('/');
				}
				pathOffset = pathLen;
			}
			else
			{
				path = new byte[DEFAULT_PATH_SIZE];
				pathOffset = 0;
			}
		}

		/// <summary>Create an iterator for a subtree of an existing iterator.</summary>
		/// <remarks>Create an iterator for a subtree of an existing iterator.</remarks>
		/// <param name="p">parent tree iterator.</param>
		protected internal AbstractTreeIterator(NGit.Treewalk.AbstractTreeIterator p)
		{
			parent = p;
			path = p.path;
			pathOffset = p.pathLen + 1;
			try
			{
				path[pathOffset - 1] = (byte)('/');
			}
			catch (IndexOutOfRangeException)
			{
				GrowPath(p.pathLen);
				path[pathOffset - 1] = (byte)('/');
			}
		}

		/// <summary>Create an iterator for a subtree of an existing iterator.</summary>
		/// <remarks>
		/// Create an iterator for a subtree of an existing iterator.
		/// <p>
		/// The caller is responsible for setting up the path of the child iterator.
		/// </remarks>
		/// <param name="p">parent tree iterator.</param>
		/// <param name="childPath">
		/// path array to be used by the child iterator. This path must
		/// contain the path from the top of the walk to the first child
		/// and must end with a '/'.
		/// </param>
		/// <param name="childPathOffset">
		/// position within <code>childPath</code> where the child can
		/// insert its data. The value at
		/// <code>childPath[childPathOffset-1]</code> must be '/'.
		/// </param>
		protected internal AbstractTreeIterator(NGit.Treewalk.AbstractTreeIterator p, byte
			[] childPath, int childPathOffset)
		{
			parent = p;
			path = childPath;
			pathOffset = childPathOffset;
		}

		/// <summary>Grow the path buffer larger.</summary>
		/// <remarks>Grow the path buffer larger.</remarks>
		/// <param name="len">
		/// number of live bytes in the path buffer. This many bytes will
		/// be moved into the larger buffer.
		/// </param>
		protected internal virtual void GrowPath(int len)
		{
			SetPathCapacity(path.Length << 1, len);
		}

		/// <summary>
		/// Ensure that path is capable to hold at least
		/// <code>capacity</code>
		/// bytes
		/// </summary>
		/// <param name="capacity">the amount of bytes to hold</param>
		/// <param name="len">the amount of live bytes in path buffer</param>
		protected internal virtual void EnsurePathCapacity(int capacity, int len)
		{
			if (path.Length >= capacity)
			{
				return;
			}
			byte[] o = path;
			int current = o.Length;
			int newCapacity = current;
			while (newCapacity < capacity && newCapacity > 0)
			{
				newCapacity <<= 1;
			}
			SetPathCapacity(newCapacity, len);
		}

		/// <summary>Set path buffer capacity to the specified size</summary>
		/// <param name="capacity">the new size</param>
		/// <param name="len">the amount of bytes to copy</param>
		private void SetPathCapacity(int capacity, int len)
		{
			byte[] o = path;
			byte[] n = new byte[capacity];
			System.Array.Copy(o, 0, n, 0, len);
			for (NGit.Treewalk.AbstractTreeIterator p = this; p != null && p.path == o; p = p
				.parent)
			{
				p.path = n;
			}
		}

		/// <summary>Compare the path of this current entry to another iterator's entry.</summary>
		/// <remarks>Compare the path of this current entry to another iterator's entry.</remarks>
		/// <param name="p">the other iterator to compare the path against.</param>
		/// <returns>
		/// -1 if this entry sorts first; 0 if the entries are equal; 1 if
		/// p's entry sorts first.
		/// </returns>
		public virtual int PathCompare(NGit.Treewalk.AbstractTreeIterator p)
		{
			return PathCompare(p, p.mode);
		}

		internal virtual int PathCompare(NGit.Treewalk.AbstractTreeIterator p, int pMode)
		{
			// Its common when we are a subtree for both parents to match;
			// when this happens everything in path[0..cPos] is known to
			// be equal and does not require evaluation again.
			//
			int cPos = AlreadyMatch(this, p);
			return PathCompare(p.path, cPos, p.pathLen, pMode, cPos);
		}

		/// <summary>Compare the path of this current entry to a raw buffer.</summary>
		/// <remarks>Compare the path of this current entry to a raw buffer.</remarks>
		/// <param name="buf">the raw path buffer.</param>
		/// <param name="pos">position to start reading the raw buffer.</param>
		/// <param name="end">one past the end of the raw buffer (length is end - pos).</param>
		/// <param name="mode">the mode of the path.</param>
		/// <returns>
		/// -1 if this entry sorts first; 0 if the entries are equal; 1 if
		/// p's entry sorts first.
		/// </returns>
		public virtual int PathCompare(byte[] buf, int pos, int end, int mode)
		{
			return PathCompare(buf, pos, end, mode, 0);
		}

		private int PathCompare(byte[] b, int bPos, int bEnd, int bMode, int aPos)
		{
			byte[] a = path;
			int aEnd = pathLen;
			for (; aPos < aEnd && bPos < bEnd; aPos++, bPos++)
			{
				int cmp = (a[aPos] & unchecked((int)(0xff))) - (b[bPos] & unchecked((int)(0xff)));
				if (cmp != 0)
				{
					return cmp;
				}
			}
			if (aPos < aEnd)
			{
				return (a[aPos] & unchecked((int)(0xff))) - LastPathChar(bMode);
			}
			if (bPos < bEnd)
			{
				return LastPathChar(mode) - (b[bPos] & unchecked((int)(0xff)));
			}
			return LastPathChar(mode) - LastPathChar(bMode);
		}

		private static int AlreadyMatch(NGit.Treewalk.AbstractTreeIterator a, NGit.Treewalk.AbstractTreeIterator
			 b)
		{
			for (; ; )
			{
				NGit.Treewalk.AbstractTreeIterator ap = a.parent;
				NGit.Treewalk.AbstractTreeIterator bp = b.parent;
				if (ap == null || bp == null)
				{
					return 0;
				}
				if (ap.matches == bp.matches)
				{
					return a.pathOffset;
				}
				a = ap;
				b = bp;
			}
		}

		private static int LastPathChar(int mode)
		{
			return FileMode.TREE.Equals(mode) ? '/' : '\0';
		}

		/// <summary>Check if the current entry of both iterators has the same id.</summary>
		/// <remarks>
		/// Check if the current entry of both iterators has the same id.
		/// <p>
		/// This method is faster than
		/// <see cref="EntryObjectId()">EntryObjectId()</see>
		/// as it does not
		/// require copying the bytes out of the buffers. A direct
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// compare operation is performed.
		/// </remarks>
		/// <param name="otherIterator">the other iterator to test against.</param>
		/// <returns>true if both iterators have the same object id; false otherwise.</returns>
		public virtual bool IdEqual(NGit.Treewalk.AbstractTreeIterator otherIterator)
		{
			return ObjectId.Equals(IdBuffer, IdOffset, otherIterator.IdBuffer, otherIterator.
				IdOffset);
		}

		/// <returns>true if the entry has a valid ObjectId.</returns>
		public abstract bool HasId
		{
			get;
		}

		/// <summary>Get the object id of the current entry.</summary>
		/// <remarks>Get the object id of the current entry.</remarks>
		/// <returns>an object id for the current entry.</returns>
		public virtual ObjectId EntryObjectId
		{
			get
			{
				return ObjectId.FromRaw(IdBuffer, IdOffset);
			}
		}

		/// <summary>Obtain the ObjectId for the current entry.</summary>
		/// <remarks>Obtain the ObjectId for the current entry.</remarks>
		/// <param name="out">buffer to copy the object id into.</param>
		public virtual void GetEntryObjectId(MutableObjectId @out)
		{
			@out.FromRaw(IdBuffer, IdOffset);
		}

		/// <returns>the file mode of the current entry.</returns>
		public virtual FileMode EntryFileMode
		{
			get
			{
				return FileMode.FromBits(mode);
			}
		}

		/// <returns>the file mode of the current entry as bits</returns>
		public virtual int EntryRawMode
		{
			get
			{
				return mode;
			}
		}

		/// <returns>path of the current entry, as a string.</returns>
		public virtual string EntryPathString
		{
			get
			{
				return TreeWalk.PathOf(this);
			}
		}

		/// <returns>the internal buffer holding the current path.</returns>
		public virtual byte[] GetEntryPathBuffer()
		{
			return path;
		}

		/// <returns>
		/// length of the path in
		/// <see cref="GetEntryPathBuffer()">GetEntryPathBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetEntryPathLength()
		{
			return pathLen;
		}

		/// <summary>Get the current entry's path hash code.</summary>
		/// <remarks>
		/// Get the current entry's path hash code.
		/// <p>
		/// This method computes a hash code on the fly for this path, the hash is
		/// suitable to cluster objects that may have similar paths together.
		/// </remarks>
		/// <returns>path hash code; any integer may be returned.</returns>
		public virtual int GetEntryPathHashCode()
		{
			int hash = 0;
			for (int i = Math.Max(0, pathLen - 16); i < pathLen; i++)
			{
				byte c = path[i];
				if (c != ' ')
				{
					hash = ((int)(((uint)hash) >> 2)) + (c << 24);
				}
			}
			return hash;
		}

		/// <summary>Get the byte array buffer object IDs must be copied out of.</summary>
		/// <remarks>
		/// Get the byte array buffer object IDs must be copied out of.
		/// <p>
		/// The id buffer contains the bytes necessary to construct an ObjectId for
		/// the current entry of this iterator. The buffer can be the same buffer for
		/// all entries, or it can be a unique buffer per-entry. Implementations are
		/// encouraged to expose their private buffer whenever possible to reduce
		/// garbage generation and copying costs.
		/// </remarks>
		/// <returns>byte array the implementation stores object IDs within.</returns>
		/// <seealso cref="EntryObjectId()">EntryObjectId()</seealso>
		public abstract byte[] IdBuffer
		{
			get;
		}

		/// <summary>
		/// Get the position within
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// of this entry's ObjectId.
		/// </summary>
		/// <returns>
		/// offset into the array returned by
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// where the
		/// ObjectId must be copied out of.
		/// </returns>
		public abstract int IdOffset
		{
			get;
		}

		/// <summary>Create a new iterator for the current entry's subtree.</summary>
		/// <remarks>
		/// Create a new iterator for the current entry's subtree.
		/// <p>
		/// The parent reference of the iterator must be <code>this</code>,
		/// otherwise the caller would not be able to exit out of the subtree
		/// iterator correctly and return to continue walking <code>this</code>.
		/// </remarks>
		/// <param name="reader">reader to load the tree data from.</param>
		/// <returns>a new parser that walks over the current subtree.</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the current entry is not actually a tree and cannot be parsed
		/// as though it were a tree.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public abstract NGit.Treewalk.AbstractTreeIterator CreateSubtreeIterator(ObjectReader
			 reader);

		/// <summary>Create a new iterator as though the current entry were a subtree.</summary>
		/// <remarks>Create a new iterator as though the current entry were a subtree.</remarks>
		/// <returns>a new empty tree iterator.</returns>
		public virtual EmptyTreeIterator CreateEmptyTreeIterator()
		{
			return new EmptyTreeIterator(this);
		}

		/// <summary>Create a new iterator for the current entry's subtree.</summary>
		/// <remarks>
		/// Create a new iterator for the current entry's subtree.
		/// <p>
		/// The parent reference of the iterator must be <code>this</code>, otherwise
		/// the caller would not be able to exit out of the subtree iterator
		/// correctly and return to continue walking <code>this</code>.
		/// </remarks>
		/// <param name="reader">reader to load the tree data from.</param>
		/// <param name="idBuffer">temporary ObjectId buffer for use by this method.</param>
		/// <returns>a new parser that walks over the current subtree.</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the current entry is not actually a tree and cannot be parsed
		/// as though it were a tree.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual NGit.Treewalk.AbstractTreeIterator CreateSubtreeIterator(ObjectReader
			 reader, MutableObjectId idBuffer)
		{
			return CreateSubtreeIterator(reader);
		}

		/// <summary>Position this iterator on the first entry.</summary>
		/// <remarks>
		/// Position this iterator on the first entry.
		/// The default implementation of this method uses
		/// <code>back(1)</code>
		/// until
		/// <code>first()</code>
		/// is true. This is most likely not the most efficient
		/// method of repositioning the iterator to its first entry, so subclasses
		/// are strongly encouraged to override the method.
		/// </remarks>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tree is invalid.</exception>
		public virtual void Reset()
		{
			while (!First)
			{
				Back(1);
			}
		}

		/// <summary>
		/// Is this tree iterator positioned on its first entry?
		/// <p>
		/// An iterator is positioned on the first entry if <code>back(1)</code>
		/// would be an invalid request as there is no entry before the current one.
		/// </summary>
		/// <remarks>
		/// Is this tree iterator positioned on its first entry?
		/// <p>
		/// An iterator is positioned on the first entry if <code>back(1)</code>
		/// would be an invalid request as there is no entry before the current one.
		/// <p>
		/// An empty iterator (one with no entries) will be
		/// <code>first() &amp;&amp; eof()</code>.
		/// </remarks>
		/// <returns>true if the iterator is positioned on the first entry.</returns>
		public abstract bool First
		{
			get;
		}

		/// <summary>
		/// Is this tree iterator at its EOF point (no more entries)?
		/// <p>
		/// An iterator is at EOF if there is no current entry.
		/// </summary>
		/// <remarks>
		/// Is this tree iterator at its EOF point (no more entries)?
		/// <p>
		/// An iterator is at EOF if there is no current entry.
		/// </remarks>
		/// <returns>true if we have walked all entries and have none left.</returns>
		public abstract bool Eof
		{
			get;
		}

		/// <summary>Move to next entry, populating this iterator with the entry data.</summary>
		/// <remarks>
		/// Move to next entry, populating this iterator with the entry data.
		/// <p>
		/// The delta indicates how many moves forward should occur. The most common
		/// delta is 1 to move to the next entry.
		/// <p>
		/// Implementations must populate the following members:
		/// <ul>
		/// <li>
		/// <see cref="mode">mode</see>
		/// </li>
		/// <li>
		/// <see cref="path">path</see>
		/// (from
		/// <see cref="pathOffset">pathOffset</see>
		/// to
		/// <see cref="pathLen">pathLen</see>
		/// )</li>
		/// <li>
		/// <see cref="pathLen">pathLen</see>
		/// </li>
		/// </ul>
		/// as well as any implementation dependent information necessary to
		/// accurately return data from
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// and
		/// <see cref="IdOffset()">IdOffset()</see>
		/// when demanded.
		/// </remarks>
		/// <param name="delta">
		/// number of entries to move the iterator by. Must be a positive,
		/// non-zero integer.
		/// </param>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tree is invalid.</exception>
		public abstract void Next(int delta);

		/// <summary>Move to prior entry, populating this iterator with the entry data.</summary>
		/// <remarks>
		/// Move to prior entry, populating this iterator with the entry data.
		/// <p>
		/// The delta indicates how many moves backward should occur.The most common
		/// delta is 1 to move to the prior entry.
		/// <p>
		/// Implementations must populate the following members:
		/// <ul>
		/// <li>
		/// <see cref="mode">mode</see>
		/// </li>
		/// <li>
		/// <see cref="path">path</see>
		/// (from
		/// <see cref="pathOffset">pathOffset</see>
		/// to
		/// <see cref="pathLen">pathLen</see>
		/// )</li>
		/// <li>
		/// <see cref="pathLen">pathLen</see>
		/// </li>
		/// </ul>
		/// as well as any implementation dependent information necessary to
		/// accurately return data from
		/// <see cref="IdBuffer()">IdBuffer()</see>
		/// and
		/// <see cref="IdOffset()">IdOffset()</see>
		/// when demanded.
		/// </remarks>
		/// <param name="delta">
		/// number of entries to move the iterator by. Must be a positive,
		/// non-zero integer.
		/// </param>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tree is invalid.</exception>
		public abstract void Back(int delta);

		/// <summary>Advance to the next tree entry, populating this iterator with its data.</summary>
		/// <remarks>
		/// Advance to the next tree entry, populating this iterator with its data.
		/// <p>
		/// This method behaves like <code>seek(1)</code> but is called by
		/// <see cref="TreeWalk">TreeWalk</see>
		/// only if a
		/// <see cref="NGit.Treewalk.Filter.TreeFilter">NGit.Treewalk.Filter.TreeFilter</see>
		/// was used and ruled out the
		/// current entry from the results. In such cases this tree iterator may
		/// perform special behavior.
		/// </remarks>
		/// <exception cref="NGit.Errors.CorruptObjectException">the tree is invalid.</exception>
		public virtual void Skip()
		{
			Next(1);
		}

		/// <summary>Indicates to the iterator that no more entries will be read.</summary>
		/// <remarks>
		/// Indicates to the iterator that no more entries will be read.
		/// <p>
		/// This is only invoked by TreeWalk when the iteration is aborted early due
		/// to a
		/// <see cref="NGit.Errors.StopWalkException">NGit.Errors.StopWalkException</see>
		/// being thrown from
		/// within a TreeFilter.
		/// </remarks>
		public virtual void StopWalk()
		{
		}

		/// <returns>the length of the name component of the path for the current entry</returns>
		public virtual int NameLength
		{
			get
			{
				// Do nothing by default.  Most iterators do not care.
				return pathLen - pathOffset;
			}
		}

		/// <summary>Get the name component of the current entry path into the provided buffer.
		/// 	</summary>
		/// <remarks>Get the name component of the current entry path into the provided buffer.
		/// 	</remarks>
		/// <param name="buffer">the buffer to get the name into, it is assumed that buffer can hold the name
		/// 	</param>
		/// <param name="offset">the offset of the name in the buffer</param>
		/// <seealso cref="NameLength()">NameLength()</seealso>
		public virtual void GetName(byte[] buffer, int offset)
		{
			System.Array.Copy(path, pathOffset, buffer, offset, pathLen - pathOffset);
		}
	}
}
