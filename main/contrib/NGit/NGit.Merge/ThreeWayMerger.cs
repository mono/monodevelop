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
using NGit.Revwalk;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>A merge of 2 trees, using a common base ancestor tree.</summary>
	/// <remarks>A merge of 2 trees, using a common base ancestor tree.</remarks>
	public abstract class ThreeWayMerger : Merger
	{
		private RevTree baseTree;

		/// <summary>Create a new merge instance for a repository.</summary>
		/// <remarks>Create a new merge instance for a repository.</remarks>
		/// <param name="local">the repository this merger will read and write data on.</param>
		protected internal ThreeWayMerger(Repository local) : base(local)
		{
		}

		/// <summary>Create a new merge instance for a repository.</summary>
		/// <remarks>Create a new merge instance for a repository.</remarks>
		/// <param name="local">the repository this merger will read and write data on.</param>
		/// <param name="inCore">perform the merge in core with no working folder involved</param>
		protected internal ThreeWayMerger(Repository local, bool inCore) : this(local)
		{
		}

		/// <summary>Set the common ancestor tree.</summary>
		/// <remarks>Set the common ancestor tree.</remarks>
		/// <param name="id">
		/// common base treeish; null to automatically compute the common
		/// base from the input commits during
		/// <see cref="Merge(NGit.AnyObjectId[])">Merge(NGit.AnyObjectId[])</see>
		/// .
		/// </param>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the object is not a treeish.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">the object does not exist.</exception>
		/// <exception cref="System.IO.IOException">the object could not be read.</exception>
		public virtual void SetBase(AnyObjectId id)
		{
			if (id != null)
			{
				baseTree = walk.ParseTree(id);
			}
			else
			{
				baseTree = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Merge(params AnyObjectId[] tips)
		{
			if (tips.Length != 2)
			{
				return false;
			}
			return base.Merge(tips);
		}

		/// <summary>Create an iterator to walk the merge base.</summary>
		/// <remarks>Create an iterator to walk the merge base.</remarks>
		/// <returns>
		/// an iterator over the caller-specified merge base, or the natural
		/// merge base of the two input commits.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual AbstractTreeIterator MergeBase()
		{
			if (baseTree != null)
			{
				return OpenTree(baseTree);
			}
			return MergeBase(0, 1);
		}
	}
}
