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
using NGit.Errors;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>Parses raw Git trees from the canonical semi-text/semi-binary format.</summary>
	/// <remarks>Parses raw Git trees from the canonical semi-text/semi-binary format.</remarks>
	public class CanonicalTreeParser : AbstractTreeIterator
	{
		private static readonly byte[] EMPTY = new byte[] {  };

		private byte[] raw;

		/// <summary>
		/// First offset within
		/// <see cref="raw">raw</see>
		/// of the prior entry.
		/// </summary>
		private int prevPtr;

		/// <summary>
		/// First offset within
		/// <see cref="raw">raw</see>
		/// of the current entry's data.
		/// </summary>
		private int currPtr;

		/// <summary>Offset one past the current entry (first byte of next entry).</summary>
		/// <remarks>Offset one past the current entry (first byte of next entry).</remarks>
		private int nextPtr;

		/// <summary>Create a new parser.</summary>
		/// <remarks>Create a new parser.</remarks>
		public CanonicalTreeParser()
		{
			Reset(EMPTY);
		}

		/// <summary>Create a new parser for a tree appearing in a subset of a repository.</summary>
		/// <remarks>Create a new parser for a tree appearing in a subset of a repository.</remarks>
		/// <param name="prefix">
		/// position of this iterator in the repository tree. The value
		/// may be null or the empty array to indicate the prefix is the
		/// root of the repository. A trailing slash ('/') is
		/// automatically appended if the prefix does not end in '/'.
		/// </param>
		/// <param name="reader">reader to load the tree data from.</param>
		/// <param name="treeId">
		/// identity of the tree being parsed; used only in exception
		/// messages if data corruption is found.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">the object supplied is not available from the repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object supplied as an argument is not actually a tree and
		/// cannot be parsed as though it were a tree.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public CanonicalTreeParser(byte[] prefix, ObjectReader reader, AnyObjectId treeId
			) : base(prefix)
		{
			Reset(reader, treeId);
		}

		private CanonicalTreeParser(NGit.Treewalk.CanonicalTreeParser p) : base(p)
		{
		}

		/// <summary>Reset this parser to walk through the given tree data.</summary>
		/// <remarks>Reset this parser to walk through the given tree data.</remarks>
		/// <param name="treeData">the raw tree content.</param>
		public virtual void Reset(byte[] treeData)
		{
			raw = treeData;
			prevPtr = -1;
			currPtr = 0;
			if (Eof())
			{
				nextPtr = 0;
			}
			else
			{
				ParseEntry();
			}
		}

		/// <summary>Reset this parser to walk through the given tree.</summary>
		/// <remarks>Reset this parser to walk through the given tree.</remarks>
		/// <param name="reader">reader to use during repository access.</param>
		/// <param name="id">
		/// identity of the tree being parsed; used only in exception
		/// messages if data corruption is found.
		/// </param>
		/// <returns>the root level parser.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the object supplied is not available from the repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object supplied as an argument is not actually a tree and
		/// cannot be parsed as though it were a tree.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual NGit.Treewalk.CanonicalTreeParser ResetRoot(ObjectReader reader, AnyObjectId
			 id)
		{
			NGit.Treewalk.CanonicalTreeParser p = this;
			while (p.parent != null)
			{
				p = (NGit.Treewalk.CanonicalTreeParser)p.parent;
			}
			p.Reset(reader, id);
			return p;
		}

		/// <returns>this iterator, or its parent, if the tree is at eof.</returns>
		public virtual NGit.Treewalk.CanonicalTreeParser Next()
		{
			NGit.Treewalk.CanonicalTreeParser p = this;
			for (; ; )
			{
				if (p.nextPtr == p.raw.Length)
				{
					// This parser has reached EOF, return to the parent.
					if (p.parent == null)
					{
						p.currPtr = p.nextPtr;
						return p;
					}
					p = (NGit.Treewalk.CanonicalTreeParser)p.parent;
					continue;
				}
				p.prevPtr = p.currPtr;
				p.currPtr = p.nextPtr;
				p.ParseEntry();
				return p;
			}
		}

		/// <summary>Reset this parser to walk through the given tree.</summary>
		/// <remarks>Reset this parser to walk through the given tree.</remarks>
		/// <param name="reader">reader to use during repository access.</param>
		/// <param name="id">
		/// identity of the tree being parsed; used only in exception
		/// messages if data corruption is found.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">the object supplied is not available from the repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object supplied as an argument is not actually a tree and
		/// cannot be parsed as though it were a tree.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual void Reset(ObjectReader reader, AnyObjectId id)
		{
			Reset(reader.Open(id, Constants.OBJ_TREE).GetCachedBytes());
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override AbstractTreeIterator CreateSubtreeIterator(ObjectReader reader, MutableObjectId
			 idBuffer)
		{
			idBuffer.FromRaw(IdBuffer(), IdOffset());
			if (!FileMode.TREE.Equals(mode))
			{
				ObjectId me = idBuffer.ToObjectId();
				throw new IncorrectObjectTypeException(me, Constants.TYPE_TREE);
			}
			return CreateSubtreeIterator0(reader, idBuffer);
		}

		/// <summary>Back door to quickly create a subtree iterator for any subtree.</summary>
		/// <remarks>
		/// Back door to quickly create a subtree iterator for any subtree.
		/// <p>
		/// Don't use this unless you are ObjectWalk. The method is meant to be
		/// called only once the current entry has been identified as a tree and its
		/// identity has been converted into an ObjectId.
		/// </remarks>
		/// <param name="reader">reader to load the tree data from.</param>
		/// <param name="id">ObjectId of the tree to open.</param>
		/// <returns>a new parser that walks over the current subtree.</returns>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public NGit.Treewalk.CanonicalTreeParser CreateSubtreeIterator0(ObjectReader reader
			, AnyObjectId id)
		{
			NGit.Treewalk.CanonicalTreeParser p = new NGit.Treewalk.CanonicalTreeParser(this);
			p.Reset(reader, id);
			return p;
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override AbstractTreeIterator CreateSubtreeIterator(ObjectReader reader)
		{
			return ((NGit.Treewalk.CanonicalTreeParser)CreateSubtreeIterator(reader, new MutableObjectId
				()));
		}

		public override bool HasId()
		{
			return true;
		}

		public override byte[] IdBuffer()
		{
			return raw;
		}

		public override int IdOffset()
		{
			return nextPtr - Constants.OBJECT_ID_LENGTH;
		}

		public override void Reset()
		{
			if (!First())
			{
				Reset(raw);
			}
		}

		public override bool First()
		{
			return currPtr == 0;
		}

		public override bool Eof()
		{
			return currPtr == raw.Length;
		}

		public override void Next(int delta)
		{
			if (delta == 1)
			{
				// Moving forward one is the most common case.
				//
				prevPtr = currPtr;
				currPtr = nextPtr;
				if (!Eof())
				{
					ParseEntry();
				}
				return;
			}
			// Fast skip over records, then parse the last one.
			//
			int end = raw.Length;
			int ptr = nextPtr;
			while (--delta > 0 && ptr != end)
			{
				prevPtr = ptr;
				while (raw[ptr] != 0)
				{
					ptr++;
				}
				ptr += Constants.OBJECT_ID_LENGTH + 1;
			}
			if (delta != 0)
			{
				throw Sharpen.Extensions.CreateIndexOutOfRangeException(delta);
			}
			currPtr = ptr;
			if (!Eof())
			{
				ParseEntry();
			}
		}

		public override void Back(int delta)
		{
			if (delta == 1 && 0 <= prevPtr)
			{
				// Moving back one is common in NameTreeWalk, as the average tree
				// won't have D/F type conflicts to study.
				//
				currPtr = prevPtr;
				prevPtr = -1;
				if (!Eof())
				{
					ParseEntry();
				}
				return;
			}
			else
			{
				if (delta <= 0)
				{
					throw Sharpen.Extensions.CreateIndexOutOfRangeException(delta);
				}
			}
			// Fast skip through the records, from the beginning of the tree.
			// There is no reliable way to read the tree backwards, so we must
			// parse all over again from the beginning. We hold the last "delta"
			// positions in a buffer, so we can find the correct position later.
			//
			int[] trace = new int[delta + 1];
			Arrays.Fill(trace, -1);
			int ptr = 0;
			while (ptr != currPtr)
			{
				System.Array.Copy(trace, 1, trace, 0, delta);
				trace[delta] = ptr;
				while (raw[ptr] != 0)
				{
					ptr++;
				}
				ptr += Constants.OBJECT_ID_LENGTH + 1;
			}
			if (trace[1] == -1)
			{
				throw Sharpen.Extensions.CreateIndexOutOfRangeException(delta);
			}
			prevPtr = trace[0];
			currPtr = trace[1];
			ParseEntry();
		}

		private void ParseEntry()
		{
			int ptr = currPtr;
			byte c = raw[ptr++];
			int tmp = c - '0';
			for (; ; )
			{
				c = raw[ptr++];
				if (' ' == c)
				{
					break;
				}
				tmp <<= 3;
				tmp += c - '0';
			}
			mode = tmp;
			tmp = pathOffset;
			for (; ; tmp++)
			{
				c = raw[ptr++];
				if (c == 0)
				{
					break;
				}
				try
				{
					path[tmp] = c;
				}
				catch (IndexOutOfRangeException)
				{
					GrowPath(tmp);
					path[tmp] = c;
				}
			}
			pathLen = tmp;
			nextPtr = ptr + Constants.OBJECT_ID_LENGTH;
		}
	}
}
