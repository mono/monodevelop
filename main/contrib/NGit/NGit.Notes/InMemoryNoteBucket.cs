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

using NGit.Notes;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>A note bucket that has been loaded into the process.</summary>
	/// <remarks>A note bucket that has been loaded into the process.</remarks>
	internal abstract class InMemoryNoteBucket : NoteBucket
	{
		/// <summary>Number of leading digits that leads to this bucket in the note path.</summary>
		/// <remarks>
		/// Number of leading digits that leads to this bucket in the note path.
		/// This is counted in terms of hex digits, not raw bytes. Each bucket level
		/// is typically 2 higher than its parent, placing about 256 items in each
		/// level of the tree.
		/// </remarks>
		internal readonly int prefixLen;

		/// <summary>Chain of non-note tree entries found at this path in the tree.</summary>
		/// <remarks>
		/// Chain of non-note tree entries found at this path in the tree.
		/// During parsing of a note tree into the in-memory representation,
		/// <see cref="NoteParser">NoteParser</see>
		/// keeps track of all non-note tree entries and stores
		/// them here as a sorted linked list. That list can be merged back with the
		/// note data that is held by the subclass, allowing the tree to be
		/// recreated.
		/// </remarks>
		internal NonNoteEntry nonNotes;

		internal InMemoryNoteBucket(int prefixLen)
		{
			this.prefixLen = prefixLen;
		}

		internal abstract NGit.Notes.InMemoryNoteBucket Append(Note note);
	}
}
