/*
 * Copyright (C) 2008, Google Inc.
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

using GitSharp.Core.DirectoryCache;
using GitSharp.Core.Exceptions;
using GitSharp.Core.TreeWalk;

namespace GitSharp.Core.Merge
{
    /// <summary>
    /// Merges two commits together in-memory, ignoring any working directory.
	/// <para />
	/// The strategy chooses a path from one of the two input trees if the path is
	/// unchanged in the other relative to their common merge base tree. This is a
	/// trivial 3-way merge (at the file path level only).
	/// <para />
	/// Modifications of the same file path (content and/or file mode) by both input
	/// trees will cause a merge conflict, as this strategy does not attempt to merge
	/// file contents.
	/// </summary>
    public class StrategySimpleTwoWayInCore : ThreeWayMergeStrategy
    {
        public override string Name
        {
            get {return "simple-two-way-in-core";}
        }

        public override Merger NewMerger(Repository db)
        {
            return new InCoreMerger(db);
        }

    	#region Nested Types

    	private class InCoreMerger : ThreeWayMerger
    	{
    		private const int Base = 0;
    		private const int Ours = 1;
    		private const int Theirs = 2;

    		private readonly NameConflictTreeWalk _tw;
    		private readonly DirCache _cache;
    		private DirCacheBuilder _builder;
    		private ObjectId _resultTree;

    		public InCoreMerger(Repository local)
    			: base(local)
    		{
    			_tw = new NameConflictTreeWalk(Repository);
    			_cache = DirCache.newInCore();
    		}

    		protected override bool MergeImpl()
    		{
    			_tw.reset();
    			_tw.addTree(MergeBase());
    			_tw.addTree(SourceTrees[0]);
    			_tw.addTree(SourceTrees[1]);

    			bool hasConflict = false;

    			_builder = _cache.builder();
    			while (_tw.next())
    			{
    				int modeO = _tw.getRawMode(Ours);
    				int modeT = _tw.getRawMode(Theirs);
    				if (modeO == modeT && _tw.idEqual(Ours, Theirs))
    				{
    					Add(Ours, DirCacheEntry.STAGE_0);
    					continue;
    				}

    				int modeB = _tw.getRawMode(Base);
    				if (modeB == modeO && _tw.idEqual(Base, Ours))
    					Add(Theirs, DirCacheEntry.STAGE_0);
    				else if (modeB == modeT && _tw.idEqual(Base, Theirs))
    					Add(Ours, DirCacheEntry.STAGE_0);
    				else if (_tw.isSubtree())
    				{
    					if (NonTree(modeB))
    					{
    						Add(Base, DirCacheEntry.STAGE_1);
    						hasConflict = true;
    					}
    					if (NonTree(modeO))
    					{
    						Add(Ours, DirCacheEntry.STAGE_2);
    						hasConflict = true;
    					}
    					if (NonTree(modeT))
    					{
    						Add(Theirs, DirCacheEntry.STAGE_3);
    						hasConflict = true;
    					}
    					_tw.enterSubtree();
    				}
    				else
    				{
    					Add(Base, DirCacheEntry.STAGE_1);
    					Add(Ours, DirCacheEntry.STAGE_2);
    					Add(Theirs, DirCacheEntry.STAGE_3);
    					hasConflict = true;
    				}
    			}
    			_builder.finish();
    			_builder = null;

    			if (hasConflict)
    				return false;
    			try
    			{
    				_resultTree = _cache.writeTree(GetObjectWriter());
    				return true;
    			}
    			catch (UnmergedPathException)
    			{
    				_resultTree = null;
    				return false;
    			}
    		}

    		private static bool NonTree(int mode)
    		{
    			return mode != 0 && !FileMode.Tree.Equals(mode);
    		}

    		private void Add(int tree, int stage)
    		{
    			AbstractTreeIterator i = GetTree(tree);
    			if (i == null) return;

    			if (FileMode.Tree.Equals(_tw.getRawMode(tree)))
    			{
    				_builder.addTree(_tw.getRawPath(), stage, Repository, _tw.getObjectId(tree));
    			}
    			else
    			{
    				var e = new DirCacheEntry(_tw.getRawPath(), stage);
    				e.setObjectIdFromRaw(i.idBuffer(), i.idOffset());
    				e.setFileMode(_tw.getFileMode(tree));
    				_builder.add(e);
    			}
    		}

    		private AbstractTreeIterator GetTree(int tree)
    		{
    			return _tw.getTree<AbstractTreeIterator>(tree, typeof(AbstractTreeIterator));
    		}

    		public override ObjectId GetResultTreeId()
    		{
    			return _resultTree;
    		}
    	}

    	#endregion
    }
}