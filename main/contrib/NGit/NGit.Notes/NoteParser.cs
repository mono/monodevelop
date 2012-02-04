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
using NGit.Treewalk;
using NGit.Util;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>Custom tree parser to select note bucket type and load it.</summary>
	/// <remarks>Custom tree parser to select note bucket type and load it.</remarks>
	internal sealed class NoteParser : CanonicalTreeParser
	{
		/// <summary>
		/// Parse a tree object into a
		/// <see cref="NoteBucket">NoteBucket</see>
		/// instance.
		/// The type of note tree is automatically detected by examining the items
		/// within the tree, and allocating the proper storage type based on the
		/// first note-like entry encountered. Since the method parses by guessing
		/// the type on the first element, malformed note trees can be read as the
		/// wrong type of tree.
		/// This method is not recursive, it parses the one tree given to it and
		/// returns the bucket. If there are subtrees for note storage, they are
		/// setup as lazy pointers that will be resolved at a later time.
		/// </summary>
		/// <param name="prefix">
		/// common hex digits that all notes within this tree share. The
		/// root tree has
		/// <code>prefix.length() == 0</code>
		/// , the first-level
		/// subtrees should be
		/// <code>prefix.length()==2</code>
		/// , etc.
		/// </param>
		/// <param name="treeId">the tree to read from the repository.</param>
		/// <param name="reader">reader to access the tree object.</param>
		/// <returns>bucket to holding the notes of the specified tree.</returns>
		/// <exception cref="System.IO.IOException">
		/// <code>treeId</code>
		/// cannot be accessed.
		/// </exception>
		internal static InMemoryNoteBucket Parse(AbbreviatedObjectId prefix, ObjectId treeId
			, ObjectReader reader)
		{
			return new NGit.Notes.NoteParser(prefix, reader, treeId).Parse();
		}

		private readonly int prefixLen;

		private readonly int pathPadding;

		private NonNoteEntry firstNonNote;

		private NonNoteEntry lastNonNote;

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private NoteParser(AbbreviatedObjectId prefix, ObjectReader r, ObjectId t) : base
			(Constants.EncodeASCII(prefix.Name), r, t)
		{
			prefixLen = prefix.Length;
			// Our path buffer has a '/' that we don't want after the prefix.
			// Drop it by shifting the path down one position.
			pathPadding = 0 < prefixLen ? 1 : 0;
			if (0 < pathPadding)
			{
				System.Array.Copy(path, 0, path, pathPadding, prefixLen);
			}
		}

		private InMemoryNoteBucket Parse()
		{
			InMemoryNoteBucket r = ParseTree();
			r.nonNotes = firstNonNote;
			return r;
		}

		private InMemoryNoteBucket ParseTree()
		{
			for (; !Eof; Next(1))
			{
				if (pathLen == pathPadding + Constants.OBJECT_ID_STRING_LENGTH && IsHex())
				{
					return ParseLeafTree();
				}
				else
				{
					if (NameLength == 2 && IsHex() && IsTree())
					{
						return ParseFanoutTree();
					}
					else
					{
						StoreNonNote();
					}
				}
			}
			// If we cannot determine the style used, assume its a leaf.
			return new LeafBucket(prefixLen);
		}

		private LeafBucket ParseLeafTree()
		{
			LeafBucket leaf = new LeafBucket(prefixLen);
			MutableObjectId idBuf = new MutableObjectId();
			for (; !Eof; Next(1))
			{
				if (ParseObjectId(idBuf))
				{
					leaf.ParseOneEntry(idBuf, EntryObjectId);
				}
				else
				{
					StoreNonNote();
				}
			}
			return leaf;
		}

		private bool ParseObjectId(MutableObjectId id)
		{
			if (pathLen == pathPadding + Constants.OBJECT_ID_STRING_LENGTH)
			{
				try
				{
					id.FromString(path, pathPadding);
					return true;
				}
				catch (IndexOutOfRangeException)
				{
					return false;
				}
			}
			return false;
		}

		private FanoutBucket ParseFanoutTree()
		{
			FanoutBucket fanout = new FanoutBucket(prefixLen);
			for (; !Eof; Next(1))
			{
				int cell = ParseFanoutCell();
				if (0 <= cell)
				{
					fanout.SetBucket(cell, EntryObjectId);
				}
				else
				{
					StoreNonNote();
				}
			}
			return fanout;
		}

		private int ParseFanoutCell()
		{
			if (NameLength == 2 && IsTree())
			{
				try
				{
					return (RawParseUtils.ParseHexInt4(path[pathOffset + 0]) << 4) | RawParseUtils.ParseHexInt4
						(path[pathOffset + 1]);
				}
				catch (IndexOutOfRangeException)
				{
					return -1;
				}
			}
			else
			{
				return -1;
			}
		}

		private void StoreNonNote()
		{
			ObjectId id = EntryObjectId;
			FileMode fileMode = EntryFileMode;
			byte[] name = new byte[NameLength];
			GetName(name, 0);
			NonNoteEntry ent = new NonNoteEntry(name, fileMode, id);
			if (firstNonNote == null)
			{
				firstNonNote = ent;
			}
			if (lastNonNote != null)
			{
				lastNonNote.next = ent;
			}
			lastNonNote = ent;
		}

		private bool IsTree()
		{
			return FileMode.TREE.Equals(mode);
		}

		private bool IsHex()
		{
			try
			{
				for (int i = pathOffset; i < pathLen; i++)
				{
					RawParseUtils.ParseHexInt4(path[i]);
				}
				return true;
			}
			catch (IndexOutOfRangeException)
			{
				return false;
			}
		}
	}
}
