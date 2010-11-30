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
using NGit.Notes;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>Index of notes from a note branch.</summary>
	/// <remarks>
	/// Index of notes from a note branch.
	/// This class is not thread-safe, and relies on an
	/// <see cref="NGit.ObjectReader">NGit.ObjectReader</see>
	/// that it
	/// borrows/shares with the caller. The reader can be used during any call, and
	/// is not released by this class. The caller should arrange for releasing the
	/// shared
	/// <code>ObjectReader</code>
	/// at the proper times.
	/// </remarks>
	public class NoteMap
	{
		/// <summary>Construct a new empty note map.</summary>
		/// <remarks>Construct a new empty note map.</remarks>
		/// <returns>an empty note map.</returns>
		public static NGit.Notes.NoteMap NewEmptyMap()
		{
			NGit.Notes.NoteMap r = new NGit.Notes.NoteMap(null);
			r.root = new LeafBucket(0);
			return r;
		}

		/// <summary>Load a collection of notes from a branch.</summary>
		/// <remarks>Load a collection of notes from a branch.</remarks>
		/// <param name="reader">
		/// reader to scan the note branch with. This reader may be
		/// retained by the NoteMap for the life of the map in order to
		/// support lazy loading of entries.
		/// </param>
		/// <param name="commit">the revision of the note branch to read.</param>
		/// <returns>the note map read from the commit.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed through the reader.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">a tree object is corrupt and cannot be read.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">a tree object wasn't actually a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a reference tree object doesn't exist.
		/// 	</exception>
		public static NGit.Notes.NoteMap Read(ObjectReader reader, RevCommit commit)
		{
			return Read(reader, commit.Tree);
		}

		/// <summary>Load a collection of notes from a tree.</summary>
		/// <remarks>Load a collection of notes from a tree.</remarks>
		/// <param name="reader">
		/// reader to scan the note branch with. This reader may be
		/// retained by the NoteMap for the life of the map in order to
		/// support lazy loading of entries.
		/// </param>
		/// <param name="tree">the note tree to read.</param>
		/// <returns>the note map read from the tree.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed through the reader.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">a tree object is corrupt and cannot be read.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">a tree object wasn't actually a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a reference tree object doesn't exist.
		/// 	</exception>
		public static NGit.Notes.NoteMap Read(ObjectReader reader, RevTree tree)
		{
			return ReadTree(reader, tree);
		}

		/// <summary>Load a collection of notes from a tree.</summary>
		/// <remarks>Load a collection of notes from a tree.</remarks>
		/// <param name="reader">
		/// reader to scan the note branch with. This reader may be
		/// retained by the NoteMap for the life of the map in order to
		/// support lazy loading of entries.
		/// </param>
		/// <param name="treeId">the note tree to read.</param>
		/// <returns>the note map read from the tree.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed through the reader.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">a tree object is corrupt and cannot be read.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">a tree object wasn't actually a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a reference tree object doesn't exist.
		/// 	</exception>
		public static NGit.Notes.NoteMap ReadTree(ObjectReader reader, ObjectId treeId)
		{
			NGit.Notes.NoteMap map = new NGit.Notes.NoteMap(reader);
			map.Load(treeId);
			return map;
		}

		/// <summary>Borrowed reader to access the repository.</summary>
		/// <remarks>Borrowed reader to access the repository.</remarks>
		private readonly ObjectReader reader;

		/// <summary>All of the notes that have been loaded.</summary>
		/// <remarks>All of the notes that have been loaded.</remarks>
		private InMemoryNoteBucket root;

		private NoteMap(ObjectReader reader)
		{
			this.reader = reader;
		}

		/// <summary>Lookup a note for a specific ObjectId.</summary>
		/// <remarks>Lookup a note for a specific ObjectId.</remarks>
		/// <param name="id">the object to look for.</param>
		/// <returns>the note's blob ObjectId, or null if no note exists.</returns>
		/// <exception cref="System.IO.IOException">a portion of the note space is not accessible.
		/// 	</exception>
		public virtual ObjectId Get(AnyObjectId id)
		{
			return root.Get(id, reader);
		}

		/// <summary>Determine if a note exists for the specified ObjectId.</summary>
		/// <remarks>Determine if a note exists for the specified ObjectId.</remarks>
		/// <param name="id">the object to look for.</param>
		/// <returns>true if a note exists; false if there is no note.</returns>
		/// <exception cref="System.IO.IOException">a portion of the note space is not accessible.
		/// 	</exception>
		public virtual bool Contains(AnyObjectId id)
		{
			return Get(id) != null;
		}

		/// <summary>Open and return the content of an object's note.</summary>
		/// <remarks>
		/// Open and return the content of an object's note.
		/// This method assumes the note is fairly small and can be accessed
		/// efficiently. Larger notes should be accessed by streaming:
		/// <pre>
		/// ObjectId dataId = thisMap.get(id);
		/// if (dataId != null)
		/// reader.open(dataId).openStream();
		/// </pre>
		/// </remarks>
		/// <param name="id">object to lookup the note of.</param>
		/// <param name="sizeLimit">
		/// maximum number of bytes to return. If the note data size is
		/// larger than this limit, LargeObjectException will be thrown.
		/// </param>
		/// <returns>
		/// if a note is defined for
		/// <code>id</code>
		/// , the note content. If no note
		/// is defined, null.
		/// </returns>
		/// <exception cref="NGit.Errors.LargeObjectException">
		/// the note data is larger than
		/// <code>sizeLimit</code>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.MissingObjectException">the note's blob does not exist in the repository.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the note's blob cannot be read from the repository
		/// 	</exception>
		public virtual byte[] GetCachedBytes(AnyObjectId id, int sizeLimit)
		{
			ObjectId dataId = Get(id);
			if (dataId != null)
			{
				return reader.Open(dataId).GetCachedBytes(sizeLimit);
			}
			else
			{
				return null;
			}
		}

		/// <summary>Attach (or remove) a note on an object.</summary>
		/// <remarks>
		/// Attach (or remove) a note on an object.
		/// If no note exists, a new note is stored. If a note already exists for the
		/// given object, it is replaced (or removed).
		/// This method only updates the map in memory.
		/// If the caller wants to attach a UTF-8 encoded string message to an
		/// object,
		/// <see cref="Set(NGit.AnyObjectId, string, NGit.ObjectInserter)">Set(NGit.AnyObjectId, string, NGit.ObjectInserter)
		/// 	</see>
		/// is a convenient
		/// way to encode and update a note in one step.
		/// </remarks>
		/// <param name="noteOn">
		/// the object to attach the note to. This same ObjectId can later
		/// be used as an argument to
		/// <see cref="Get(NGit.AnyObjectId)">Get(NGit.AnyObjectId)</see>
		/// or
		/// <see cref="GetCachedBytes(NGit.AnyObjectId, int)">GetCachedBytes(NGit.AnyObjectId, int)
		/// 	</see>
		/// to read back the
		/// <code>noteData</code>
		/// .
		/// </param>
		/// <param name="noteData">
		/// data to associate with the note. This must be the ObjectId of
		/// a blob that already exists in the repository. If null the note
		/// will be deleted, if present.
		/// </param>
		/// <exception cref="System.IO.IOException">a portion of the note space is not accessible.
		/// 	</exception>
		public virtual void Set(AnyObjectId noteOn, ObjectId noteData)
		{
			InMemoryNoteBucket newRoot = root.Set(noteOn, noteData, reader);
			if (newRoot == null)
			{
				newRoot = new LeafBucket(0);
				newRoot.nonNotes = root.nonNotes;
			}
			root = newRoot;
		}

		/// <summary>Attach a note to an object.</summary>
		/// <remarks>
		/// Attach a note to an object.
		/// If no note exists, a new note is stored. If a note already exists for the
		/// given object, it is replaced (or removed).
		/// </remarks>
		/// <param name="noteOn">
		/// the object to attach the note to. This same ObjectId can later
		/// be used as an argument to
		/// <see cref="Get(NGit.AnyObjectId)">Get(NGit.AnyObjectId)</see>
		/// or
		/// <see cref="GetCachedBytes(NGit.AnyObjectId, int)">GetCachedBytes(NGit.AnyObjectId, int)
		/// 	</see>
		/// to read back the
		/// <code>noteData</code>
		/// .
		/// </param>
		/// <param name="noteData">
		/// text to store in the note. The text will be UTF-8 encoded when
		/// stored in the repository. If null the note will be deleted, if
		/// the empty string a note with the empty string will be stored.
		/// </param>
		/// <param name="ins">
		/// inserter to write the encoded
		/// <code>noteData</code>
		/// out as a blob.
		/// The caller must ensure the inserter is flushed before the
		/// updated note map is made available for reading.
		/// </param>
		/// <exception cref="System.IO.IOException">the note data could not be stored in the repository.
		/// 	</exception>
		public virtual void Set(AnyObjectId noteOn, string noteData, ObjectInserter ins)
		{
			ObjectId dataId;
			if (noteData != null)
			{
				byte[] dataUTF8 = Constants.Encode(noteData);
				dataId = ins.Insert(Constants.OBJ_BLOB, dataUTF8);
			}
			else
			{
				dataId = null;
			}
			Set(noteOn, dataId);
		}

		/// <summary>Remove a note from an object.</summary>
		/// <remarks>
		/// Remove a note from an object.
		/// If no note exists, no action is performed.
		/// This method only updates the map in memory.
		/// </remarks>
		/// <param name="noteOn">the object to remove the note from.</param>
		/// <exception cref="System.IO.IOException">a portion of the note space is not accessible.
		/// 	</exception>
		public virtual void Remove(AnyObjectId noteOn)
		{
			Set(noteOn, null);
		}

		/// <summary>Write this note map as a tree.</summary>
		/// <remarks>Write this note map as a tree.</remarks>
		/// <param name="inserter">
		/// inserter to use when writing trees to the object database.
		/// Caller is responsible for flushing the inserter before trying
		/// to read the objects, or exposing them through a reference.
		/// </param>
		/// <returns>the top level tree.</returns>
		/// <exception cref="System.IO.IOException">a tree could not be written.</exception>
		public virtual ObjectId WriteTree(ObjectInserter inserter)
		{
			return root.WriteTree(inserter);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void Load(ObjectId rootTree)
		{
			AbbreviatedObjectId none = AbbreviatedObjectId.FromString(string.Empty);
			root = NoteParser.Parse(none, rootTree, reader);
		}
	}
}
