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

using System.Text;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// A representation of a file (blob) object in a
	/// <see cref="Tree">Tree</see>
	/// .
	/// </summary>
	[System.ObsoleteAttribute(@"To look up information about a single path, useNGit.Treewalk.TreeWalk.ForPath(Repository, string, NGit.Revwalk.RevTree) . To lookup information about multiple paths at once, use aNGit.Treewalk.TreeWalk and obtain the current entry's information from its getter methods."
		)]
	public class FileTreeEntry : TreeEntry
	{
		private FileMode mode;

		/// <summary>Constructor for a File (blob) object.</summary>
		/// <remarks>Constructor for a File (blob) object.</remarks>
		/// <param name="parent">
		/// The
		/// <see cref="Tree">Tree</see>
		/// holding this object (or null)
		/// </param>
		/// <param name="id">the SHA-1 of the blob (or null for a yet unhashed file)</param>
		/// <param name="nameUTF8">raw object name in the parent tree</param>
		/// <param name="execute">true if the executable flag is set</param>
		public FileTreeEntry(Tree parent, ObjectId id, byte[] nameUTF8, bool execute) : base
			(parent, id, nameUTF8)
		{
			SetExecutable(execute);
		}

		public override FileMode GetMode()
		{
			return mode;
		}

		/// <returns>true if this file is executable</returns>
		public virtual bool IsExecutable()
		{
			return GetMode().Equals(FileMode.EXECUTABLE_FILE);
		}

		/// <param name="execute">set/reset the executable flag</param>
		public virtual void SetExecutable(bool execute)
		{
			mode = execute ? FileMode.EXECUTABLE_FILE : FileMode.REGULAR_FILE;
		}

		/// <returns>
		/// an
		/// <see cref="ObjectLoader">ObjectLoader</see>
		/// that will return the data
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual ObjectLoader OpenReader()
		{
			return GetRepository().Open(GetId(), Constants.OBJ_BLOB);
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append(ObjectId.ToString(GetId()));
			r.Append(' ');
			r.Append(IsExecutable() ? 'X' : 'F');
			r.Append(' ');
			r.Append(GetFullName());
			return r.ToString();
		}
	}
}
