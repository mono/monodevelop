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

using System.IO;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Mutable formatter to construct a single tree object.</summary>
	/// <remarks>
	/// Mutable formatter to construct a single tree object.
	/// This formatter does not process subtrees. Callers must handle creating each
	/// subtree on their own.
	/// To maintain good performance for bulk operations, this formatter does not
	/// validate its input. Callers are responsible for ensuring the resulting tree
	/// object is correctly well formed by writing entries in the correct order.
	/// </remarks>
	public class TreeFormatter
	{
		/// <summary>Compute the size of a tree entry record.</summary>
		/// <remarks>
		/// Compute the size of a tree entry record.
		/// This method can be used to estimate the correct size of a tree prior to
		/// allocating a formatter. Getting the size correct at allocation time
		/// ensures the internal buffer is sized correctly, reducing copying.
		/// </remarks>
		/// <param name="mode">the mode the entry will have.</param>
		/// <param name="nameLen">the length of the name, in bytes.</param>
		/// <returns>the length of the record.</returns>
		public static int EntrySize(FileMode mode, int nameLen)
		{
			return mode.CopyToLength() + nameLen + Constants.OBJECT_ID_LENGTH + 2;
		}

		private byte[] buf;

		private int ptr;

		private TemporaryBuffer.Heap overflowBuffer;

		/// <summary>Create an empty formatter with a default buffer size.</summary>
		/// <remarks>Create an empty formatter with a default buffer size.</remarks>
		public TreeFormatter() : this(8192)
		{
		}

		/// <summary>Create an empty formatter with the specified buffer size.</summary>
		/// <remarks>Create an empty formatter with the specified buffer size.</remarks>
		/// <param name="size">
		/// estimated size of the tree, in bytes. Callers can use
		/// <see cref="EntrySize(FileMode, int)">EntrySize(FileMode, int)</see>
		/// to estimate the size of each
		/// entry in advance of allocating the formatter.
		/// </param>
		public TreeFormatter(int size)
		{
			buf = new byte[size];
		}

		/// <summary>
		/// Add a link to a submodule commit, mode is
		/// <see cref="FileMode.GITLINK">FileMode.GITLINK</see>
		/// .
		/// </summary>
		/// <param name="name">name of the entry.</param>
		/// <param name="commit">the ObjectId to store in this entry.</param>
		public virtual void Append(string name, RevCommit commit)
		{
			Append(name, FileMode.GITLINK, commit);
		}

		/// <summary>
		/// Add a subtree, mode is
		/// <see cref="FileMode.TREE">FileMode.TREE</see>
		/// .
		/// </summary>
		/// <param name="name">name of the entry.</param>
		/// <param name="tree">the ObjectId to store in this entry.</param>
		public virtual void Append(string name, RevTree tree)
		{
			Append(name, FileMode.TREE, tree);
		}

		/// <summary>
		/// Add a regular file, mode is
		/// <see cref="FileMode.REGULAR_FILE">FileMode.REGULAR_FILE</see>
		/// .
		/// </summary>
		/// <param name="name">name of the entry.</param>
		/// <param name="blob">the ObjectId to store in this entry.</param>
		public virtual void Append(string name, RevBlob blob)
		{
			Append(name, FileMode.REGULAR_FILE, blob);
		}

		/// <summary>Append any entry to the tree.</summary>
		/// <remarks>Append any entry to the tree.</remarks>
		/// <param name="name">name of the entry.</param>
		/// <param name="mode">
		/// mode describing the treatment of
		/// <code>id</code>
		/// .
		/// </param>
		/// <param name="id">the ObjectId to store in this entry.</param>
		public virtual void Append(string name, FileMode mode, AnyObjectId id)
		{
			Append(Constants.Encode(name), mode, id);
		}

		/// <summary>Append any entry to the tree.</summary>
		/// <remarks>Append any entry to the tree.</remarks>
		/// <param name="name">
		/// name of the entry. The name should be UTF-8 encoded, but file
		/// name encoding is not a well defined concept in Git.
		/// </param>
		/// <param name="mode">
		/// mode describing the treatment of
		/// <code>id</code>
		/// .
		/// </param>
		/// <param name="id">the ObjectId to store in this entry.</param>
		public virtual void Append(byte[] name, FileMode mode, AnyObjectId id)
		{
			Append(name, 0, name.Length, mode, id);
		}

		/// <summary>Append any entry to the tree.</summary>
		/// <remarks>Append any entry to the tree.</remarks>
		/// <param name="nameBuf">
		/// buffer holding the name of the entry. The name should be UTF-8
		/// encoded, but file name encoding is not a well defined concept
		/// in Git.
		/// </param>
		/// <param name="namePos">
		/// first position within
		/// <code>nameBuf</code>
		/// of the name data.
		/// </param>
		/// <param name="nameLen">
		/// number of bytes from
		/// <code>nameBuf</code>
		/// to use as the name.
		/// </param>
		/// <param name="mode">
		/// mode describing the treatment of
		/// <code>id</code>
		/// .
		/// </param>
		/// <param name="id">the ObjectId to store in this entry.</param>
		public virtual void Append(byte[] nameBuf, int namePos, int nameLen, FileMode mode
			, AnyObjectId id)
		{
			if (FmtBuf(nameBuf, namePos, nameLen, mode))
			{
				id.CopyRawTo(buf, ptr);
				ptr += Constants.OBJECT_ID_LENGTH;
			}
			else
			{
				try
				{
					FmtOverflowBuffer(nameBuf, namePos, nameLen, mode);
					id.CopyRawTo(overflowBuffer);
				}
				catch (IOException badBuffer)
				{
					// This should never occur.
					throw new RuntimeException(badBuffer);
				}
			}
		}

		/// <summary>Append any entry to the tree.</summary>
		/// <remarks>Append any entry to the tree.</remarks>
		/// <param name="nameBuf">
		/// buffer holding the name of the entry. The name should be UTF-8
		/// encoded, but file name encoding is not a well defined concept
		/// in Git.
		/// </param>
		/// <param name="namePos">
		/// first position within
		/// <code>nameBuf</code>
		/// of the name data.
		/// </param>
		/// <param name="nameLen">
		/// number of bytes from
		/// <code>nameBuf</code>
		/// to use as the name.
		/// </param>
		/// <param name="mode">
		/// mode describing the treatment of
		/// <code>id</code>
		/// .
		/// </param>
		/// <param name="idBuf">buffer holding the raw ObjectId of the entry.</param>
		/// <param name="idPos">
		/// first position within
		/// <code>idBuf</code>
		/// to copy the id from.
		/// </param>
		public virtual void Append(byte[] nameBuf, int namePos, int nameLen, FileMode mode
			, byte[] idBuf, int idPos)
		{
			if (FmtBuf(nameBuf, namePos, nameLen, mode))
			{
				System.Array.Copy(idBuf, idPos, buf, ptr, Constants.OBJECT_ID_LENGTH);
				ptr += Constants.OBJECT_ID_LENGTH;
			}
			else
			{
				try
				{
					FmtOverflowBuffer(nameBuf, namePos, nameLen, mode);
					overflowBuffer.Write(idBuf, idPos, Constants.OBJECT_ID_LENGTH);
				}
				catch (IOException badBuffer)
				{
					// This should never occur.
					throw new RuntimeException(badBuffer);
				}
			}
		}

		private bool FmtBuf(byte[] nameBuf, int namePos, int nameLen, FileMode mode)
		{
			if (buf == null || buf.Length < ptr + EntrySize(mode, nameLen))
			{
				return false;
			}
			mode.CopyTo(buf, ptr);
			ptr += mode.CopyToLength();
			buf[ptr++] = (byte)(' ');
			System.Array.Copy(nameBuf, namePos, buf, ptr, nameLen);
			ptr += nameLen;
			buf[ptr++] = 0;
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FmtOverflowBuffer(byte[] nameBuf, int namePos, int nameLen, FileMode
			 mode)
		{
			if (buf != null)
			{
				overflowBuffer = new TemporaryBuffer.Heap(int.MaxValue);
				overflowBuffer.Write(buf, 0, ptr);
				buf = null;
			}
			mode.CopyTo(overflowBuffer);
			overflowBuffer.Write(unchecked((byte)' '));
			overflowBuffer.Write(nameBuf, namePos, nameLen);
			overflowBuffer.Write(unchecked((byte)0));
		}

		/// <summary>Insert this tree and obtain its ObjectId.</summary>
		/// <remarks>Insert this tree and obtain its ObjectId.</remarks>
		/// <param name="ins">the inserter to store the tree.</param>
		/// <returns>computed ObjectId of the tree</returns>
		/// <exception cref="System.IO.IOException">the tree could not be stored.</exception>
		public virtual ObjectId InsertTo(ObjectInserter ins)
		{
			if (buf != null)
			{
				return ins.Insert(Constants.OBJ_TREE, buf, 0, ptr);
			}
			long len = overflowBuffer.Length();
			return ins.Insert(Constants.OBJ_TREE, len, overflowBuffer.OpenInputStream());
		}

		/// <summary>Compute the ObjectId for this tree</summary>
		/// <param name="ins"></param>
		/// <returns>ObjectId for this tree</returns>
		public virtual ObjectId ComputeId(ObjectInserter ins)
		{
			if (buf != null)
			{
				return ins.IdFor(Constants.OBJ_TREE, buf, 0, ptr);
			}
			long len = overflowBuffer.Length();
			try
			{
				return ins.IdFor(Constants.OBJ_TREE, len, overflowBuffer.OpenInputStream());
			}
			catch (IOException e)
			{
				// this should never happen
				throw new RuntimeException(e);
			}
		}

		/// <summary>Copy this formatter's buffer into a byte array.</summary>
		/// <remarks>
		/// Copy this formatter's buffer into a byte array.
		/// This method is not efficient, as it needs to create a copy of the
		/// internal buffer in order to supply an array of the correct size to the
		/// caller. If the buffer is just to pass to an ObjectInserter, consider
		/// using
		/// <see cref="ObjectInserter.Insert(TreeFormatter)">ObjectInserter.Insert(TreeFormatter)
		/// 	</see>
		/// instead.
		/// </remarks>
		/// <returns>a copy of this formatter's buffer.</returns>
		public virtual byte[] ToByteArray()
		{
			if (buf != null)
			{
				byte[] r = new byte[ptr];
				System.Array.Copy(buf, 0, r, 0, ptr);
				return r;
			}
			try
			{
				return overflowBuffer.ToByteArray();
			}
			catch (IOException err)
			{
				// This should never happen, its read failure on a byte array.
				throw new RuntimeException(err);
			}
		}

		public override string ToString()
		{
			byte[] raw = ToByteArray();
			CanonicalTreeParser p = new CanonicalTreeParser();
			p.Reset(raw);
			StringBuilder r = new StringBuilder();
			r.Append("Tree={");
			if (!p.Eof)
			{
				r.Append('\n');
				try
				{
					new ObjectChecker().CheckTree(raw);
				}
				catch (CorruptObjectException error)
				{
					r.Append("*** ERROR: ").Append(error.Message).Append("\n");
					r.Append('\n');
				}
			}
			while (!p.Eof)
			{
				FileMode mode = p.EntryFileMode;
				r.Append(mode);
				r.Append(' ');
				r.Append(Constants.TypeString(mode.GetObjectType()));
				r.Append(' ');
				r.Append(p.EntryObjectId.Name);
				r.Append(' ');
				r.Append(p.EntryPathString);
				r.Append('\n');
				p.Next();
			}
			r.Append("}");
			return r.ToString();
		}
	}
}
