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
using NGit.Dircache;
using NGit.Errors;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// Iterate and update a
	/// <see cref="DirCache">DirCache</see>
	/// as part of a <code>TreeWalk</code>.
	/// <p>
	/// Like
	/// <see cref="DirCacheIterator">DirCacheIterator</see>
	/// this iterator allows a DirCache to be used in
	/// parallel with other sorts of iterators in a TreeWalk. However any entry which
	/// appears in the source DirCache and which is skipped by the TreeFilter is
	/// automatically copied into
	/// <see cref="DirCacheBuilder">DirCacheBuilder</see>
	/// , thus retaining it in the
	/// newly updated index.
	/// <p>
	/// This iterator is suitable for update processes, or even a simple delete
	/// algorithm. For example deleting a path:
	/// <pre>
	/// final DirCache dirc = db.lockDirCache();
	/// final DirCacheBuilder edit = dirc.builder();
	/// final TreeWalk walk = new TreeWalk(db);
	/// walk.reset();
	/// walk.setRecursive(true);
	/// walk.setFilter(PathFilter.create(&quot;name/to/remove&quot;));
	/// walk.addTree(new DirCacheBuildIterator(edit));
	/// while (walk.next())
	/// ; // do nothing on a match as we want to remove matches
	/// edit.commit();
	/// </pre>
	/// </summary>
	public class DirCacheBuildIterator : DirCacheIterator
	{
		private readonly DirCacheBuilder builder;

		/// <summary>Create a new iterator for an already loaded DirCache instance.</summary>
		/// <remarks>
		/// Create a new iterator for an already loaded DirCache instance.
		/// <p>
		/// The iterator implementation may copy part of the cache's data during
		/// construction, so the cache must be read in prior to creating the
		/// iterator.
		/// </remarks>
		/// <param name="dcb">
		/// the cache builder for the cache to walk. The cache must be
		/// already loaded into memory.
		/// </param>
		public DirCacheBuildIterator(DirCacheBuilder dcb) : base(dcb.GetDirCache())
		{
			builder = dcb;
		}

		internal DirCacheBuildIterator(NGit.Dircache.DirCacheBuildIterator p, DirCacheTree
			 dct) : base(p, dct)
		{
			builder = p.builder;
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override AbstractTreeIterator CreateSubtreeIterator(ObjectReader reader)
		{
			if (currentSubtree == null)
			{
				throw new IncorrectObjectTypeException(EntryObjectId, Constants.TYPE_TREE);
			}
			return new NGit.Dircache.DirCacheBuildIterator(this, currentSubtree);
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		public override void Skip()
		{
			if (currentSubtree != null)
			{
				builder.Keep(ptr, currentSubtree.GetEntrySpan());
			}
			else
			{
				builder.Keep(ptr, 1);
			}
			Next(1);
		}

		public override void StopWalk()
		{
			int cur = ptr;
			int cnt = cache.GetEntryCount();
			if (cur < cnt)
			{
				builder.Keep(cur, cnt - cur);
			}
		}
	}
}
