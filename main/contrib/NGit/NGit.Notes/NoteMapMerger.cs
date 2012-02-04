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

using NGit;
using NGit.Merge;
using NGit.Notes;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>Three-way note tree merge.</summary>
	/// <remarks>
	/// Three-way note tree merge.
	/// <p>
	/// Direct implementation of NoteMap merger without using
	/// <see cref="NGit.Treewalk.TreeWalk">NGit.Treewalk.TreeWalk</see>
	/// and
	/// <see cref="NGit.Treewalk.AbstractTreeIterator">NGit.Treewalk.AbstractTreeIterator
	/// 	</see>
	/// </remarks>
	public class NoteMapMerger
	{
		private static readonly FanoutBucket EMPTY_FANOUT = new FanoutBucket(0);

		private static readonly LeafBucket EMPTY_LEAF = new LeafBucket(0);

		private readonly Repository db;

		private readonly NoteMerger noteMerger;

		private readonly MergeStrategy nonNotesMergeStrategy;

		private readonly ObjectReader reader;

		private readonly ObjectInserter inserter;

		private readonly MutableObjectId objectIdPrefix;

		/// <summary>
		/// Constructs a NoteMapMerger with custom
		/// <see cref="NoteMerger">NoteMerger</see>
		/// and custom
		/// <see cref="NGit.Merge.MergeStrategy">NGit.Merge.MergeStrategy</see>
		/// .
		/// </summary>
		/// <param name="db">Git repository</param>
		/// <param name="noteMerger">note merger for merging conflicting changes on a note</param>
		/// <param name="nonNotesMergeStrategy">merge strategy for merging non-note entries</param>
		public NoteMapMerger(Repository db, NoteMerger noteMerger, MergeStrategy nonNotesMergeStrategy
			)
		{
			this.db = db;
			this.reader = db.NewObjectReader();
			this.inserter = db.NewObjectInserter();
			this.noteMerger = noteMerger;
			this.nonNotesMergeStrategy = nonNotesMergeStrategy;
			this.objectIdPrefix = new MutableObjectId();
		}

		/// <summary>
		/// Constructs a NoteMapMerger with
		/// <see cref="DefaultNoteMerger">DefaultNoteMerger</see>
		/// as the merger
		/// for notes and the
		/// <see cref="NGit.Merge.MergeStrategy.RESOLVE">NGit.Merge.MergeStrategy.RESOLVE</see>
		/// as the strategy for
		/// resolving conflicts on non-notes
		/// </summary>
		/// <param name="db">Git repository</param>
		public NoteMapMerger(Repository db) : this(db, new DefaultNoteMerger(), MergeStrategy
			.RESOLVE)
		{
		}

		/// <summary>Performs the merge.</summary>
		/// <remarks>Performs the merge.</remarks>
		/// <param name="base">base version of the note tree</param>
		/// <param name="ours">ours version of the note tree</param>
		/// <param name="theirs">theirs version of the note tree</param>
		/// <returns>merge result as a new NoteMap</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual NoteMap Merge(NoteMap @base, NoteMap ours, NoteMap theirs)
		{
			try
			{
				InMemoryNoteBucket mergedBucket = Merge(0, @base.GetRoot(), ours.GetRoot(), theirs
					.GetRoot());
				inserter.Flush();
				return NoteMap.NewMap(mergedBucket, reader);
			}
			finally
			{
				reader.Release();
				inserter.Release();
			}
		}

		/// <summary>
		/// This method is called only when it is known that there is some difference
		/// between base, ours and theirs.
		/// </summary>
		/// <remarks>
		/// This method is called only when it is known that there is some difference
		/// between base, ours and theirs.
		/// </remarks>
		/// <param name="treeDepth"></param>
		/// <param name="base"></param>
		/// <param name="ours"></param>
		/// <param name="theirs"></param>
		/// <returns>merge result as an InMemoryBucket</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		private InMemoryNoteBucket Merge(int treeDepth, InMemoryNoteBucket @base, InMemoryNoteBucket
			 ours, InMemoryNoteBucket theirs)
		{
			InMemoryNoteBucket result;
			if (@base is FanoutBucket || ours is FanoutBucket || theirs is FanoutBucket)
			{
				result = MergeFanoutBucket(treeDepth, AsFanout(@base), AsFanout(ours), AsFanout(theirs
					));
			}
			else
			{
				result = MergeLeafBucket(treeDepth, (LeafBucket)@base, (LeafBucket)ours, (LeafBucket
					)theirs);
			}
			result.nonNotes = MergeNonNotes(NonNotes(@base), NonNotes(ours), NonNotes(theirs)
				);
			return result;
		}

		private FanoutBucket AsFanout(InMemoryNoteBucket bucket)
		{
			if (bucket == null)
			{
				return EMPTY_FANOUT;
			}
			if (bucket is FanoutBucket)
			{
				return (FanoutBucket)bucket;
			}
			return ((LeafBucket)bucket).Split();
		}

		private static NonNoteEntry NonNotes(InMemoryNoteBucket b)
		{
			return b == null ? null : b.nonNotes;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private InMemoryNoteBucket MergeFanoutBucket(int treeDepth, FanoutBucket @base, FanoutBucket
			 ours, FanoutBucket theirs)
		{
			FanoutBucket result = new FanoutBucket(treeDepth * 2);
			// walking through entries of base, ours, theirs
			for (int i = 0; i < 256; i++)
			{
				NoteBucket b = @base.GetBucket(i);
				NoteBucket o = ours.GetBucket(i);
				NoteBucket t = theirs.GetBucket(i);
				if (Equals(o, t))
				{
					AddIfNotNull(result, i, o);
				}
				else
				{
					if (Equals(b, o))
					{
						AddIfNotNull(result, i, t);
					}
					else
					{
						if (Equals(b, t))
						{
							AddIfNotNull(result, i, o);
						}
						else
						{
							objectIdPrefix.SetByte(treeDepth, i);
							InMemoryNoteBucket mergedBucket = Merge(treeDepth + 1, FanoutBucket.LoadIfLazy(b, 
								objectIdPrefix, reader), FanoutBucket.LoadIfLazy(o, objectIdPrefix, reader), FanoutBucket
								.LoadIfLazy(t, objectIdPrefix, reader));
							result.SetBucket(i, mergedBucket);
						}
					}
				}
			}
			return result.ContractIfTooSmall(objectIdPrefix, reader);
		}

		private static bool Equals(NoteBucket a, NoteBucket b)
		{
			if (a == null && b == null)
			{
				return true;
			}
			return a != null && b != null && a.GetTreeId().Equals(b.GetTreeId());
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddIfNotNull(FanoutBucket b, int cell, NoteBucket child)
		{
			if (child == null)
			{
				return;
			}
			if (child is InMemoryNoteBucket)
			{
				b.SetBucket(cell, ((InMemoryNoteBucket)child).WriteTree(inserter));
			}
			else
			{
				b.SetBucket(cell, child.GetTreeId());
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private InMemoryNoteBucket MergeLeafBucket(int treeDepth, LeafBucket bb, LeafBucket
			 ob, LeafBucket tb)
		{
			bb = NotNullOrEmpty(bb);
			ob = NotNullOrEmpty(ob);
			tb = NotNullOrEmpty(tb);
			InMemoryNoteBucket result = new LeafBucket(treeDepth * 2);
			int bi = 0;
			int oi = 0;
			int ti = 0;
			while (bi < bb.Size() || oi < ob.Size() || ti < tb.Size())
			{
				Note b = Get(bb, bi);
				Note o = Get(ob, oi);
				Note t = Get(tb, ti);
				Note min = Min(b, o, t);
				b = SameNoteOrNull(min, b);
				o = SameNoteOrNull(min, o);
				t = SameNoteOrNull(min, t);
				if (SameContent(o, t))
				{
					result = AddIfNotNull(result, o);
				}
				else
				{
					if (SameContent(b, o))
					{
						result = AddIfNotNull(result, t);
					}
					else
					{
						if (SameContent(b, t))
						{
							result = AddIfNotNull(result, o);
						}
						else
						{
							result = AddIfNotNull(result, noteMerger.Merge(b, o, t, reader, inserter));
						}
					}
				}
				if (b != null)
				{
					bi++;
				}
				if (o != null)
				{
					oi++;
				}
				if (t != null)
				{
					ti++;
				}
			}
			return result;
		}

		private static LeafBucket NotNullOrEmpty(LeafBucket b)
		{
			return b != null ? b : EMPTY_LEAF;
		}

		private static Note Get(LeafBucket b, int i)
		{
			return i < b.Size() ? b.Get(i) : null;
		}

		private static Note Min(Note b, Note o, Note t)
		{
			Note min = b;
			if (min == null || (o != null && o.CompareTo(min) < 0))
			{
				min = o;
			}
			if (min == null || (t != null && t.CompareTo(min) < 0))
			{
				min = t;
			}
			return min;
		}

		private static Note SameNoteOrNull(Note min, Note other)
		{
			return SameNote(min, other) ? other : null;
		}

		private static bool SameNote(Note a, Note b)
		{
			if (a == null && b == null)
			{
				return true;
			}
			return a != null && b != null && AnyObjectId.Equals(a, b);
		}

		private static bool SameContent(Note a, Note b)
		{
			if (a == null && b == null)
			{
				return true;
			}
			return a != null && b != null && AnyObjectId.Equals(a.GetData(), b.GetData());
		}

		private static InMemoryNoteBucket AddIfNotNull(InMemoryNoteBucket result, Note note
			)
		{
			if (note != null)
			{
				return result.Append(note);
			}
			else
			{
				return result;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private NonNoteEntry MergeNonNotes(NonNoteEntry baseList, NonNoteEntry oursList, 
			NonNoteEntry theirsList)
		{
			if (baseList == null && oursList == null && theirsList == null)
			{
				return null;
			}
			ObjectId baseId = Write(baseList);
			ObjectId oursId = Write(oursList);
			ObjectId theirsId = Write(theirsList);
			inserter.Flush();
			Merger m = nonNotesMergeStrategy.NewMerger(db, true);
			if (m is ThreeWayMerger)
			{
				((ThreeWayMerger)m).SetBase(baseId);
			}
			if (!m.Merge(oursId, theirsId))
			{
				throw new NotesMergeConflictException(baseList, oursList, theirsList);
			}
			ObjectId resultTreeId = m.GetResultTreeId();
			AbbreviatedObjectId none = AbbreviatedObjectId.FromString(string.Empty);
			return NoteParser.Parse(none, resultTreeId, reader).nonNotes;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId Write(NonNoteEntry list)
		{
			LeafBucket b = new LeafBucket(0);
			b.nonNotes = list;
			return b.WriteTree(inserter);
		}
	}
}
