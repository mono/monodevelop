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

using System.Collections.Generic;
using NGit;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class telling where the working-tree, the index and the current HEAD differ
	/// from each other.
	/// </summary>
	/// <remarks>
	/// A class telling where the working-tree, the index and the current HEAD differ
	/// from each other. Collections are exposed containing the paths of the modified
	/// files. E.g. to find out which files are dirty in the working tree (modified
	/// but not added) you would inspect the collection returned by
	/// <see cref="GetModified()">GetModified()</see>
	/// .
	/// <p>
	/// The same path can be returned by multiple getters. E.g. if a modification has
	/// been added to the index and afterwards the corresponding working tree file is
	/// again modified this path will be returned by
	/// <see cref="GetModified()">GetModified()</see>
	/// and
	/// <see cref="GetChanged()">GetChanged()</see>
	/// </remarks>
	public class Status
	{
		private IndexDiff diff;

		/// <param name="diff"></param>
		public Status(IndexDiff diff) : base()
		{
			this.diff = diff;
		}

		/// <returns>
		/// list of files added to the index, not in HEAD (e.g. what you get
		/// if you call 'git add ...' on a newly created file)
		/// </returns>
		public virtual ICollection<string> GetAdded()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetAdded());
		}

		/// <returns>
		/// list of files changed from HEAD to index (e.g. what you get if
		/// you modify an existing file and call 'git add ...' on it)
		/// </returns>
		public virtual ICollection<string> GetChanged()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetChanged());
		}

		/// <returns>
		/// list of files removed from index, but in HEAD (e.g. what you get
		/// if you call 'git rm ...' on a existing file)
		/// </returns>
		public virtual ICollection<string> GetRemoved()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetRemoved());
		}

		/// <returns>
		/// list of files in index, but not filesystem (e.g. what you get if
		/// you call 'rm ...' on a existing file)
		/// </returns>
		public virtual ICollection<string> GetMissing()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetMissing());
		}

		/// <returns>
		/// list of files modified on disk relative to the index (e.g. what
		/// you get if you modify an existing file without adding it to the
		/// index)
		/// </returns>
		public virtual ICollection<string> GetModified()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetModified());
		}

		/// <returns>
		/// list of files that are not ignored, and not in the index. (e.g.
		/// what you get if you create a new file without adding it to the
		/// index)
		/// </returns>
		public virtual ICollection<string> GetUntracked()
		{
			return Sharpen.Collections.UnmodifiableSet(diff.GetUntracked());
		}
	}
}
