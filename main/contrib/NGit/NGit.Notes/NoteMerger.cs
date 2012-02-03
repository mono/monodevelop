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
using Sharpen;

namespace NGit.Notes
{
	/// <summary>Three-way note merge operation.</summary>
	/// <remarks>
	/// Three-way note merge operation.
	/// <p>
	/// This operation takes three versions of a note: base, ours and theirs,
	/// performs the three-way merge and returns the merge result.
	/// </remarks>
	public interface NoteMerger
	{
		/// <summary>Merges the conflicting note changes.</summary>
		/// <remarks>
		/// Merges the conflicting note changes.
		/// <p>
		/// base, ours and their are all notes on the same object.
		/// </remarks>
		/// <param name="base">version of the Note</param>
		/// <param name="ours">version of the Note</param>
		/// <param name="their">version of the Note</param>
		/// <param name="reader">the object reader that must be used to read Git objects</param>
		/// <param name="inserter">the object inserter that must be used to insert Git objects
		/// 	</param>
		/// <returns>the merge result</returns>
		/// <exception cref="NotesMergeConflictException">
		/// in case there was a merge conflict which this note merger
		/// couldn't resolve
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// in case the reader or the inserter would throw an IOException
		/// the implementor will most likely want to propagate it as it
		/// can't do much to recover from it
		/// </exception>
		/// <exception cref="NGit.Notes.NotesMergeConflictException"></exception>
		Note Merge(Note @base, Note ours, Note their, ObjectReader reader, ObjectInserter
			 inserter);
	}
}
