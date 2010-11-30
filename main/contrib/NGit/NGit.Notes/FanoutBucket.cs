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
using System.IO;
using NGit;
using NGit.Notes;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>A note tree holding only note subtrees, each named using a 2 digit hex name.
	/// 	</summary>
	/// <remarks>
	/// A note tree holding only note subtrees, each named using a 2 digit hex name.
	/// The fanout buckets/trees contain on average 256 subtrees, naming the subtrees
	/// by a slice of the ObjectId contained within them, from "00" through "ff".
	/// Each fanout bucket has a
	/// <see cref="InMemoryNoteBucket.prefixLen">InMemoryNoteBucket.prefixLen</see>
	/// that defines how many digits it
	/// skips in an ObjectId before it gets to the digits matching
	/// <see cref="table">table</see>
	/// .
	/// The root tree has
	/// <code>prefixLen == 0</code>
	/// , and thus does not skip any digits.
	/// For ObjectId "c0ffee...", the note (if it exists) will be stored within the
	/// bucket
	/// <code>table[0xc0]</code>
	/// .
	/// The first level tree has
	/// <code>prefixLen == 2</code>
	/// , and thus skips the first two
	/// digits. For the same example "c0ffee..." object, its note would be found
	/// within the
	/// <code>table[0xff]</code>
	/// bucket (as first 2 digits "c0" are skipped).
	/// Each subtree is loaded on-demand, reducing startup latency for reads that
	/// only need to examine a few objects. However, due to the rather uniform
	/// distribution of the SHA-1 hash that is used for ObjectIds, accessing 256
	/// objects is very likely to load all of the subtrees into memory.
	/// A FanoutBucket must be parsed from a tree object by
	/// <see cref="NoteParser">NoteParser</see>
	/// .
	/// </remarks>
	internal class FanoutBucket : InMemoryNoteBucket
	{
		/// <summary>Fan-out table similar to the PackIndex structure.</summary>
		/// <remarks>
		/// Fan-out table similar to the PackIndex structure.
		/// Notes for an object are stored within the sub-bucket that is held here as
		/// <code>table[ objectId.getByte( prefixLen / 2 ) ]</code>
		/// . If the slot is null
		/// there are no notes with that prefix.
		/// </remarks>
		private readonly NoteBucket[] table;

		/// <summary>
		/// Number of non-null slots in
		/// <see cref="table">table</see>
		/// .
		/// </summary>
		private int cnt;

		internal FanoutBucket(int prefixLen) : base(prefixLen)
		{
			table = new NoteBucket[256];
		}

		internal virtual void ParseOneEntry(int cell, ObjectId id)
		{
			table[cell] = new FanoutBucket.LazyNoteBucket(this, id);
			cnt++;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectId Get(AnyObjectId objId, ObjectReader or)
		{
			NoteBucket b = table[Cell(objId)];
			return b != null ? b.Get(objId, or) : null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override Sharpen.Iterator<Note> Iterator(AnyObjectId objId, ObjectReader
			 reader)
		{
			MutableObjectId id = new MutableObjectId();
			id.FromObjectId(objId);
			return new _Iterator_119(this, id, reader);
		}

		private sealed class _Iterator_119 : Sharpen.Iterator<Note>
		{
			public _Iterator_119(FanoutBucket _enclosing, MutableObjectId id, ObjectReader reader
				)
			{
				this._enclosing = _enclosing;
				this.id = id;
				this.reader = reader;
			}

			private int cell;

			private Sharpen.Iterator<Note> itr;

			public override bool HasNext()
			{
				if (this.itr != null && this.itr.HasNext())
				{
					return true;
				}
				for (; this.cell < this._enclosing.table.Length; this.cell++)
				{
					NoteBucket b = this._enclosing.table[this.cell];
					if (b == null)
					{
						continue;
					}
					try
					{
						id.SetByte(this._enclosing.prefixLen >> 1, this.cell);
						this.itr = b.Iterator(id, reader);
					}
					catch (IOException err)
					{
						throw new RuntimeException(err);
					}
					if (this.itr.HasNext())
					{
						this.cell++;
						return true;
					}
				}
				return false;
			}

			public override Note Next()
			{
				if (this.HasNext())
				{
					return this.itr.Next();
				}
				else
				{
					throw new NoSuchElementException();
				}
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly FanoutBucket _enclosing;

			private readonly MutableObjectId id;

			private readonly ObjectReader reader;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override int EstimateSize(AnyObjectId noteOn, ObjectReader or)
		{
			// If most of this fan-out is full, estimate it should still be split.
			if (LeafBucket.MAX_SIZE * 3 / 4 <= cnt)
			{
				return 1 + LeafBucket.MAX_SIZE;
			}
			// Due to the uniform distribution of ObjectIds, having less nodes full
			// indicates a good chance the total number of children below here
			// is less than the MAX_SIZE split point. Get a more accurate count.
			MutableObjectId id = new MutableObjectId();
			id.FromObjectId(noteOn);
			int sz = 0;
			for (int cell = 0; cell < 256; cell++)
			{
				NoteBucket b = table[cell];
				if (b == null)
				{
					continue;
				}
				id.SetByte(prefixLen >> 1, cell);
				sz += b.EstimateSize(id, or);
				if (LeafBucket.MAX_SIZE < sz)
				{
					break;
				}
			}
			return sz;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override InMemoryNoteBucket Set(AnyObjectId noteOn, AnyObjectId noteData
			, ObjectReader or)
		{
			int cell = Cell(noteOn);
			NoteBucket b = table[cell];
			if (b == null)
			{
				if (noteData == null)
				{
					return this;
				}
				LeafBucket n = new LeafBucket(prefixLen + 2);
				table[cell] = n.Set(noteOn, noteData, or);
				cnt++;
				return this;
			}
			else
			{
				NoteBucket n = b.Set(noteOn, noteData, or);
				if (n == null)
				{
					table[cell] = null;
					cnt--;
					if (cnt == 0)
					{
						return null;
					}
					if (EstimateSize(noteOn, or) < LeafBucket.MAX_SIZE)
					{
						// We are small enough to just contract to a single leaf.
						InMemoryNoteBucket r = new LeafBucket(prefixLen);
						for (Sharpen.Iterator<Note> i = Iterator(noteOn, or); i.HasNext(); )
						{
							r = r.Append(i.Next());
						}
						r.nonNotes = nonNotes;
						return r;
					}
					return this;
				}
				else
				{
					if (n != b)
					{
						table[cell] = n;
					}
				}
				return this;
			}
		}

		private static readonly byte[] hexchar = new byte[] { (byte)('0'), (byte)('1'), (
			byte)('2'), (byte)('3'), (byte)('4'), (byte)('5'), (byte)('6'), (byte)('7'), (byte
			)('8'), (byte)('9'), (byte)('a'), (byte)('b'), (byte)('c'), (byte)('d'), (byte)(
			'e'), (byte)('f') };

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectId WriteTree(ObjectInserter inserter)
		{
			byte[] nameBuf = new byte[2];
			TreeFormatter fmt = new TreeFormatter(TreeSize());
			NonNoteEntry e = nonNotes;
			for (int cell = 0; cell < 256; cell++)
			{
				NoteBucket b = table[cell];
				if (b == null)
				{
					continue;
				}
				nameBuf[0] = hexchar[(int)(((uint)cell) >> 4)];
				nameBuf[1] = hexchar[cell & unchecked((int)(0x0f))];
				while (e != null && e.PathCompare(nameBuf, 0, 2, FileMode.TREE) < 0)
				{
					e.Format(fmt);
					e = e.next;
				}
				fmt.Append(nameBuf, 0, 2, FileMode.TREE, b.WriteTree(inserter));
			}
			for (; e != null; e = e.next)
			{
				e.Format(fmt);
			}
			return fmt.Insert(inserter);
		}

		private int TreeSize()
		{
			int sz = cnt * TreeFormatter.EntrySize(FileMode.TREE, 2);
			for (NonNoteEntry e = nonNotes; e != null; e = e.next)
			{
				sz += e.TreeEntrySize();
			}
			return sz;
		}

		internal override InMemoryNoteBucket Append(Note note)
		{
			int cell = Cell(note);
			InMemoryNoteBucket b = (InMemoryNoteBucket)table[cell];
			if (b == null)
			{
				LeafBucket n = new LeafBucket(prefixLen + 2);
				table[cell] = n.Append(note);
				cnt++;
			}
			else
			{
				InMemoryNoteBucket n = b.Append(note);
				if (n != b)
				{
					table[cell] = n;
				}
			}
			return this;
		}

		private int Cell(AnyObjectId id)
		{
			return id.GetByte(prefixLen >> 1);
		}

		private class LazyNoteBucket : NoteBucket
		{
			private readonly ObjectId treeId;

			internal LazyNoteBucket(FanoutBucket _enclosing, ObjectId treeId)
			{
				this._enclosing = _enclosing;
				this.treeId = treeId;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override ObjectId Get(AnyObjectId objId, ObjectReader or)
			{
				return this.Load(objId, or).Get(objId, or);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override Sharpen.Iterator<Note> Iterator(AnyObjectId objId, ObjectReader
				 reader)
			{
				return this.Load(objId, reader).Iterator(objId, reader);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override int EstimateSize(AnyObjectId objId, ObjectReader or)
			{
				return this.Load(objId, or).EstimateSize(objId, or);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override InMemoryNoteBucket Set(AnyObjectId noteOn, AnyObjectId noteData
				, ObjectReader or)
			{
				return this.Load(noteOn, or).Set(noteOn, noteData, or);
			}

			internal override ObjectId WriteTree(ObjectInserter inserter)
			{
				return this.treeId;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private NoteBucket Load(AnyObjectId objId, ObjectReader or)
			{
				AbbreviatedObjectId p = objId.Abbreviate(this._enclosing.prefixLen + 2);
				NoteBucket self = NoteParser.Parse(p, this.treeId, or);
				this._enclosing.table[this._enclosing.Cell(objId)] = self;
				return self;
			}

			private readonly FanoutBucket _enclosing;
		}
	}
}
