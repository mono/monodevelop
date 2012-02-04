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
using NGit.Notes;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>A note tree holding only notes, with no subtrees.</summary>
	/// <remarks>
	/// A note tree holding only notes, with no subtrees.
	/// The leaf bucket contains on average less than 256 notes, all of whom share
	/// the same leading prefix. If a notes branch has less than 256 notes, the top
	/// level tree of the branch should be a LeafBucket. Once a notes branch has more
	/// than 256 notes, the root should be a
	/// <see cref="FanoutBucket">FanoutBucket</see>
	/// and the LeafBucket
	/// will appear only as a cell of a FanoutBucket.
	/// Entries within the LeafBucket are stored sorted by ObjectId, and lookup is
	/// performed using binary search. As the entry list should contain fewer than
	/// 256 elements, the average number of compares to find an element should be
	/// less than 8 due to the O(log N) lookup behavior.
	/// A LeafBucket must be parsed from a tree object by
	/// <see cref="NoteParser">NoteParser</see>
	/// .
	/// </remarks>
	internal class LeafBucket : InMemoryNoteBucket
	{
		internal const int MAX_SIZE = 256;

		/// <summary>All note blobs in this bucket, sorted sequentially.</summary>
		/// <remarks>All note blobs in this bucket, sorted sequentially.</remarks>
		private Note[] notes;

		/// <summary>
		/// Number of items in
		/// <see cref="notes">notes</see>
		/// .
		/// </summary>
		private int cnt;

		internal LeafBucket(int prefixLen) : base(prefixLen)
		{
			notes = new Note[4];
		}

		private int Search(AnyObjectId objId)
		{
			int low = 0;
			int high = cnt;
			while (low < high)
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				int cmp = objId.CompareTo(notes[mid]);
				if (cmp < 0)
				{
					high = mid;
				}
				else
				{
					if (cmp == 0)
					{
						return mid;
					}
					else
					{
						low = mid + 1;
					}
				}
			}
			return -(low + 1);
		}

		internal override Note GetNote(AnyObjectId objId, ObjectReader or)
		{
			int idx = Search(objId);
			return 0 <= idx ? notes[idx] : null;
		}

		internal virtual Note Get(int index)
		{
			return notes[index];
		}

		internal virtual int Size()
		{
			return cnt;
		}

		internal override Sharpen.Iterator<Note> Iterator(AnyObjectId objId, ObjectReader
			 reader)
		{
			return new _Iterator_121(this);
		}

		private sealed class _Iterator_121 : Sharpen.Iterator<Note>
		{
			public _Iterator_121(LeafBucket _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private int idx;

			public override bool HasNext()
			{
				return this.idx < this._enclosing.cnt;
			}

			public override Note Next()
			{
				if (this.HasNext())
				{
					return this._enclosing.notes[this.idx++];
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

			private readonly LeafBucket _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override int EstimateSize(AnyObjectId noteOn, ObjectReader or)
		{
			return cnt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override InMemoryNoteBucket Set(AnyObjectId noteOn, AnyObjectId noteData
			, ObjectReader or)
		{
			int p = Search(noteOn);
			if (0 <= p)
			{
				if (noteData != null)
				{
					notes[p].SetData(noteData.Copy());
					return this;
				}
				else
				{
					System.Array.Copy(notes, p + 1, notes, p, cnt - p - 1);
					cnt--;
					return 0 < cnt ? this : null;
				}
			}
			else
			{
				if (noteData != null)
				{
					if (ShouldSplit())
					{
						return Split().Set(noteOn, noteData, or);
					}
					else
					{
						GrowIfFull();
						p = -(p + 1);
						if (p < cnt)
						{
							System.Array.Copy(notes, p, notes, p + 1, cnt - p);
						}
						notes[p] = new Note(noteOn, noteData.Copy());
						cnt++;
						return this;
					}
				}
				else
				{
					return this;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override ObjectId WriteTree(ObjectInserter inserter)
		{
			return inserter.Insert(Build());
		}

		internal override ObjectId GetTreeId()
		{
			return new ObjectInserter.Formatter().IdFor(Build());
		}

		private TreeFormatter Build()
		{
			byte[] nameBuf = new byte[Constants.OBJECT_ID_STRING_LENGTH];
			int nameLen = Constants.OBJECT_ID_STRING_LENGTH - prefixLen;
			TreeFormatter fmt = new TreeFormatter(TreeSize(nameLen));
			NonNoteEntry e = nonNotes;
			for (int i = 0; i < cnt; i++)
			{
				Note n = notes[i];
				n.CopyTo(nameBuf, 0);
				while (e != null && e.PathCompare(nameBuf, prefixLen, nameLen, FileMode.REGULAR_FILE
					) < 0)
				{
					e.Format(fmt);
					e = e.next;
				}
				fmt.Append(nameBuf, prefixLen, nameLen, FileMode.REGULAR_FILE, n.GetData());
			}
			for (; e != null; e = e.next)
			{
				e.Format(fmt);
			}
			return fmt;
		}

		private int TreeSize(int nameLen)
		{
			int sz = cnt * TreeFormatter.EntrySize(FileMode.REGULAR_FILE, nameLen);
			for (NonNoteEntry e = nonNotes; e != null; e = e.next)
			{
				sz += e.TreeEntrySize();
			}
			return sz;
		}

		internal virtual void ParseOneEntry(AnyObjectId noteOn, AnyObjectId noteData)
		{
			GrowIfFull();
			notes[cnt++] = new Note(noteOn, noteData.Copy());
		}

		internal override InMemoryNoteBucket Append(Note note)
		{
			if (ShouldSplit())
			{
				return Split().Append(note);
			}
			else
			{
				GrowIfFull();
				notes[cnt++] = note;
				return this;
			}
		}

		private void GrowIfFull()
		{
			if (notes.Length == cnt)
			{
				Note[] n = new Note[notes.Length * 2];
				System.Array.Copy(notes, 0, n, 0, cnt);
				notes = n;
			}
		}

		private bool ShouldSplit()
		{
			return MAX_SIZE <= cnt && prefixLen + 2 < Constants.OBJECT_ID_STRING_LENGTH;
		}

		internal virtual FanoutBucket Split()
		{
			FanoutBucket n = new FanoutBucket(prefixLen);
			for (int i = 0; i < cnt; i++)
			{
				n.Append(notes[i]);
			}
			n.nonNotes = nonNotes;
			return n;
		}
	}
}
