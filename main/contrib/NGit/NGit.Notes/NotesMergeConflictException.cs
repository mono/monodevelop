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
using NGit.Internal;
using NGit.Notes;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>
	/// This exception will be thrown from the
	/// <see cref="NoteMerger">NoteMerger</see>
	/// when a conflict on
	/// Notes content is found during merge.
	/// </summary>
	[System.Serializable]
	public class NotesMergeConflictException : IOException
	{
		private const long serialVersionUID = 1L;

		/// <summary>
		/// Construct a NotesMergeConflictException for the specified base, ours and
		/// theirs note versions.
		/// </summary>
		/// <remarks>
		/// Construct a NotesMergeConflictException for the specified base, ours and
		/// theirs note versions.
		/// </remarks>
		/// <param name="base">note version</param>
		/// <param name="ours">note version</param>
		/// <param name="theirs">note version</param>
		public NotesMergeConflictException(Note @base, Note ours, Note theirs) : base(MessageFormat
			.Format(JGitText.Get().mergeConflictOnNotes, NoteOn(@base, ours, theirs), NoteData
			(@base), NoteData(ours), NoteData(theirs)))
		{
		}

		/// <summary>
		/// Constructs a NotesMergeConflictException for the specified base, ours and
		/// theirs versions of the root note tree.
		/// </summary>
		/// <remarks>
		/// Constructs a NotesMergeConflictException for the specified base, ours and
		/// theirs versions of the root note tree.
		/// </remarks>
		/// <param name="base">version of the root note tree</param>
		/// <param name="ours">version of the root note tree</param>
		/// <param name="theirs">version of the root note tree</param>
		internal NotesMergeConflictException(NonNoteEntry @base, NonNoteEntry ours, NonNoteEntry
			 theirs) : base(MessageFormat.Format(JGitText.Get().mergeConflictOnNonNoteEntries
			, Name(@base), Name(ours), Name(theirs)))
		{
		}

		private static string NoteOn(Note @base, Note ours, Note theirs)
		{
			if (@base != null)
			{
				return @base.Name;
			}
			if (ours != null)
			{
				return ours.Name;
			}
			return theirs.Name;
		}

		private static string NoteData(Note n)
		{
			if (n != null)
			{
				return n.GetData().Name;
			}
			return string.Empty;
		}

		private static string Name(NonNoteEntry e)
		{
			return e != null ? e.Name : string.Empty;
		}
	}
}
