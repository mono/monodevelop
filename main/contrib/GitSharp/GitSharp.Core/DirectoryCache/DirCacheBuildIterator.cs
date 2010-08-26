/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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

using GitSharp.Core.Exceptions;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.TreeWalk.Filter;

namespace GitSharp.Core.DirectoryCache
{
	/// <summary>
	/// Iterate and update a <see cref="DirCache"/> as part of a <see cref="TreeWalk"/>.
	/// <para />
	/// Like <see cref="DirCacheIterator"/> this iterator allows a <see cref="DirCache"/>
	/// to be used in parallel with other sorts of iterators in a <see cref="TreeWalk"/>. 
	/// However any entry which appears in the source <see cref="DirCache"/> and which 
	/// is skipped by the <see cref="TreeFilter"/> is automatically copied into 
	/// <see cref="DirCacheBuilder"/>, thus retaining it in the newly updated index.
	/// <para/>
	/// This iterator is suitable for update processes, or even a simple delete
	/// algorithm. For example deleting a path:
	/// <para/>
	/// <example>
	/// DirCache dirc = DirCache.lock(db);
	/// DirCacheBuilder edit = dirc.builder();
	/// 
	/// TreeWalk walk = new TreeWalk(db);
	/// walk.reset();
	/// walk.setRecursive(true);
	/// walk.setFilter(PathFilter.Create(&quot;name/to/remove&quot;));
	/// walk.addTree(new DirCacheBuildIterator(edit));
	/// 
	/// while (walk.next())
	/// ; // do nothing on a match as we want to remove matches
	/// edit.commit();
	/// </example>
	/// </summary>
    public class DirCacheBuildIterator : DirCacheIterator
    {
        private readonly DirCacheBuilder _builder;

		/// <summary>
		/// Create a new iterator for an already loaded <see cref="DirCache"/> instance.
		/// <para/>
		/// The iterator implementation may copy part of the cache's data during
		/// construction, so the cache must be Read in prior to creating the
		/// iterator.
		/// </summary>
		/// <param name="builder">
		/// The cache builder for the cache to walk. The cache must be
		/// already loaded into memory.
		/// </param>
        public DirCacheBuildIterator(DirCacheBuilder builder)
            : base(builder.getDirCache())
        {
			if ( builder == null)
			{
				throw new System.ArgumentNullException("builder");
			}
            _builder = builder;
        }

		/// <summary>
		/// Create a new iterator for an already loaded <see cref="DirCache"/> instance.
		/// <para/>
		/// The iterator implementation may copy part of the cache's data during
		/// construction, so the cache must be Read in prior to creating the
		/// iterator.
		/// </summary>
		/// <param name="parentIterator">The parent iterator</param>
		/// <param name="cacheTree">The cache tree</param>
        DirCacheBuildIterator(DirCacheBuildIterator parentIterator, DirCacheTree cacheTree)
            : base(parentIterator, cacheTree)
        {
			if (parentIterator == null)
				throw new System.ArgumentNullException ("parentIterator");
			
            _builder = parentIterator._builder;
        }

        public override AbstractTreeIterator createSubtreeIterator(Repository repo)
        {
            if (CurrentSubtree == null)
            {
            	throw new IncorrectObjectTypeException(getEntryObjectId(),
            	                                       Constants.TYPE_TREE);
            }

            return new DirCacheBuildIterator(this, CurrentSubtree);
        }

        public override void skip()
        {
            if (CurrentSubtree != null)
            {
            	_builder.keep(Pointer, CurrentSubtree.getEntrySpan());
            }
            else
            {
            	_builder.keep(Pointer, 1);
            }
            next(1);
        }

        public override void stopWalk()
        {
            int cur = Pointer;
            int cnt = Cache.getEntryCount();
            if (cur < cnt)
            {
            	_builder.keep(cur, cnt - cur);
            }
        }
    }
}
