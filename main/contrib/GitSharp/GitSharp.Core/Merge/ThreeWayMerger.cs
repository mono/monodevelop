/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.TreeWalk;

namespace GitSharp.Core.Merge
{
	/// <summary>
	/// A merge of 2 trees, using a common base ancestor tree.
	/// </summary>
	public abstract class ThreeWayMerger : Merger
	{
		private RevTree _baseTree;

		/// <summary>
		/// Create a new merge instance for a repository.
		/// </summary>
		/// <param name="local">
		/// The repository this merger will Read and write data on. 
		/// </param>
		protected ThreeWayMerger(Repository local)
			: base(local)
		{
		}

		/// <summary>
		/// Set the common ancestor tree.
		/// </summary>
		/// <param name="id">
		/// Common base treeish; null to automatically compute the common
		/// base from the input commits during
		/// <see cref="Merge(AnyObjectId, AnyObjectId)"/>.
		/// </param>
		/// <exception cref="IncorrectObjectTypeException">
		/// The object is not a <see cref="Treeish"/>.
		/// </exception>
		/// <exception cref="MissingObjectException">
		/// The object does not exist.
		/// </exception>
		/// <exception cref="IOException">
		/// The object could not be read.
		/// </exception>
		public void SetBase(AnyObjectId id)
		{
			_baseTree = id != null ? Walk.parseTree(id) : null;
		}

		///	<summary>
		/// Merge together two <see cref="Treeish"/> objects.
		/// <para />
		/// Any tree-ish may be supplied as inputs. Commits and/or tags pointing at
		/// trees or commits may be passed as input objects.
		/// </summary>
		/// <param name="a">source tree to be combined together.</param>
		/// <param name="b">source tree to be combined together.</param>
		/// <returns> 
		/// true if the merge was completed without conflicts; false if the
		/// merge strategy cannot handle this merge or there were conflicts
		/// preventing it from automatically resolving all paths.
		/// </returns>
		/// <exception cref="IncorrectObjectTypeException">
		/// one of the input objects is not a commit, but the strategy
		/// requires it to be a commit.
		/// </exception>
		/// <exception cref="IOException">
		/// one or more sources could not be read, or outputs could not
		/// be written to the Repository.
		/// </exception>
		public bool Merge(AnyObjectId a, AnyObjectId b)
		{
			return Merge(new[] { a, b });
		}

		public override bool Merge(AnyObjectId[] tips)
		{
			if (tips == null)
				throw new System.ArgumentNullException ("tips");
			
			return tips.Length != 2 ? false : base.Merge(tips);
		}

		///	<summary>
		/// Create an iterator to walk the merge base.
		/// </summary>
		/// <returns>
		/// An iterator over the caller-specified merge base, or the natural
		/// merge base of the two input commits.
		/// </returns>
		/// <exception cref="IOException"></exception> 
		protected AbstractTreeIterator MergeBase()
		{
			return _baseTree != null ? OpenTree(_baseTree) : MergeBase(0, 1);
		}
	}
}