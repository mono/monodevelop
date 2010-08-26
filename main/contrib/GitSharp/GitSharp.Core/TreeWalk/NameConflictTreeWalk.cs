/*
 * Copyright (C) 2008, Google Inc.
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

namespace GitSharp.Core.TreeWalk
{
    /**
     * Specialized TreeWalk to detect directory-file (D/F) name conflicts.
     * <para />
     * Due to the way a Git tree is organized the standard {@link TreeWalk} won't
     * easily find a D/F conflict when merging two or more trees together. In the
     * standard TreeWalk the file will be returned first, and then much later the
     * directory will be returned. This makes it impossible for the application to
     * efficiently detect and handle the conflict.
     * <para />
     * Using this walk implementation causes the directory to report earlier than
     * usual, at the same time as the non-directory entry. This permits the
     * application to handle the D/F conflict in a single step. The directory is
     * returned only once, so it does not get returned later in the iteration.
     * <para />
     * When a D/F conflict is detected {@link TreeWalk#isSubtree()} will return true
     * and {@link TreeWalk#enterSubtree()} will recurse into the subtree, no matter
     * which iterator originally supplied the subtree.
     * <para />
     * Because conflicted directories report early, using this walk implementation
     * to populate a {@link DirCacheBuilder} may cause the automatic resorting to
     * run and fix the entry ordering.
     * <para />
     * This walk implementation requires more CPU to implement a look-ahead and a
     * look-behind to merge a D/F pair together, or to skip a previously reported
     * directory. In typical Git repositories the look-ahead cost is 0 and the
     * look-behind doesn't trigger, as users tend not to Create trees which contain
     * both "foo" as a directory and "foo.c" as a file.
     * <para />
     * In the worst-case however several thousand look-ahead steps per walk step may
     * be necessary, making the overhead quite significant. Since this worst-case
     * should never happen this walk implementation has made the time/space tradeoff
     * in favor of more-time/less-space, as that better suits the typical case.
     */
    public class NameConflictTreeWalk : TreeWalk
    {
        private static readonly int TreeMode = FileMode.Tree.Bits;
        private bool _fastMinHasMatch;

        /**
         * Create a new tree walker for a given repository.
         *
         * @param repo
         *            the repository the walker will obtain data from.
         */
        public NameConflictTreeWalk(Repository repo)
            : base(repo)
        {
        }

        public override AbstractTreeIterator min()
        {
            while (true)
            {
                AbstractTreeIterator minRef = FastMin();
                if (_fastMinHasMatch)
                {
                	return minRef;
                }

                if (IsTree(minRef))
                {
                    if (SkipEntry(minRef))
                    {
                        foreach (AbstractTreeIterator t in Trees)
                        {
                        	if (t.Matches != minRef) continue;
                        	t.next(1);
                        	t.Matches = null;
                        }

                        continue;
                    }

                    return minRef;
                }

                return CombineDF(minRef);
            }
        }

        private AbstractTreeIterator FastMin()
        {
            _fastMinHasMatch = true;

            int i = 0;
            AbstractTreeIterator minRef = Trees[i];
            while (minRef.eof() && ++i < Trees.Length)
            {
            	minRef = Trees[i];
            }

            if (minRef.eof())
            {
            	return minRef;
            }

            minRef.Matches = minRef;
            while (++i < Trees.Length)
            {
                AbstractTreeIterator t = Trees[i];
                if (t.eof())
                {
                	continue;
                }

                int cmp = t.pathCompare(minRef);
                if (cmp < 0)
                {
                    if (_fastMinHasMatch && IsTree(minRef) && !IsTree(t)
                            && NameEqual(minRef, t))
                    {
                        // We used to be at a tree, but now we are at a file
                        // with the same name. Allow the file to match the
                        // tree anyway.
                        //
                        t.Matches = minRef;
                    }
                    else
                    {
                        _fastMinHasMatch = false;
                        t.Matches = t;
                        minRef = t;
                    }
                }
                else if (cmp == 0)
                {
                    // Exact name/mode match is best.
                    //
                    t.Matches = minRef;
                }
                else if (_fastMinHasMatch && IsTree(t) && !IsTree(minRef)
                      && NameEqual(t, minRef))
                {
                    // The minimum is a file (non-tree) but the next entry
                    // of this iterator is a tree whose name matches our file.
                    // This is a classic D/F conflict and commonly occurs like
                    // this, with no gaps in between the file and directory.
                    //
                    // Use the tree as the minimum instead (see CombineDF).
                    //

                    for (int k = 0; k < i; k++)
                    {
                        AbstractTreeIterator p = Trees[k];
                        if (p.Matches == minRef)
                        {
                        	p.Matches = t;
                        }
                    }

                    t.Matches = t;
                    minRef = t;
                }
                else
                {
                	_fastMinHasMatch = false;
                }
            }

            return minRef;
        }

        private static bool NameEqual(AbstractTreeIterator a,
                 AbstractTreeIterator b)
        {
            return a.pathCompare(b, TreeMode) == 0;
        }

        private static bool IsTree(AbstractTreeIterator p)
        {
            return FileMode.Tree == p.EntryFileMode;
        }

        private bool SkipEntry(AbstractTreeIterator minRef)
        {
            // A tree D/F may have been handled earlier. We need to
            // not report this path if it has already been reported.
            //
            foreach (AbstractTreeIterator t in Trees)
            {
                if (t.Matches == minRef || t.first()) continue;

                int stepsBack = 0;
                while (true)
                {
                    stepsBack++;
                    t.back(1);

                    int cmp = t.pathCompare(minRef, 0);
                    if (cmp == 0)
                    {
                        // We have already seen this "$path" before. Skip it.
                        //
                        t.next(stepsBack);
                        return true;
                    }

                	if (cmp >= 0 && !t.first()) continue;

                	// We cannot find "$path" in t; it will never appear.
                	//
                	t.next(stepsBack);
                	break;
                }
            }

            // We have never seen the current path before.
            //
            return false;
        }

        private AbstractTreeIterator CombineDF(AbstractTreeIterator minRef)
        {
            // Look for a possible D/F conflict forward in the tree(s)
            // as there may be a "$path/" which matches "$path". Make
            // such entries match this entry.
            //
            AbstractTreeIterator treeMatch = null;
            foreach (AbstractTreeIterator t in Trees)
            {
                if (t.Matches == minRef || t.eof()) continue;

                for (; ; )
                {
                    int cmp = t.pathCompare(minRef, TreeMode);
                    if (cmp < 0)
                    {
                        // The "$path/" may still appear later.
                        //
                        t.MatchShift++;
                        t.next(1);
                        if (t.eof())
                        {
                            t.back(t.MatchShift);
                            t.MatchShift = 0;
                            break;
                        }
                    }
                    else if (cmp == 0)
                    {
                        // We have a conflict match here.
                        //
                        t.Matches = minRef;
                        treeMatch = t;
                        break;
                    }
                    else
                    {
                        // A conflict match is not possible.
                        //
                        if (t.MatchShift != 0)
                        {
                            t.back(t.MatchShift);
                            t.MatchShift = 0;
                        }
                        break;
                    }
                }
            }

            if (treeMatch != null)
            {
                // If we do have a conflict use one of the directory
                // matching iterators instead of the file iterator.
                // This way isSubtree is true and isRecursive works.
                //
                foreach (AbstractTreeIterator t in Trees)
                {
                	if (t.Matches == minRef)
                	{
                		t.Matches = treeMatch;
                	}
                }

                return treeMatch;
            }

            return minRef;
        }

        public override void popEntriesEqual()
        {
            AbstractTreeIterator ch = CurrentHead;
            for (int i = 0; i < Trees.Length; i++)
            {
                AbstractTreeIterator t = Trees[i];
                if (t.Matches == ch)
                {
                    if (t.MatchShift == 0)
                    {
                    	t.next(1);
                    }
                    else
                    {
                        t.back(t.MatchShift);
                        t.MatchShift = 0;
                    }
                    t.Matches = null;
                }
            }
        }

        public override void skipEntriesEqual()
        {
            AbstractTreeIterator ch = CurrentHead;
            for (int i = 0; i < Trees.Length; i++)
            {
                AbstractTreeIterator t = Trees[i];
            	if (t.Matches != ch) continue;

            	if (t.MatchShift == 0)
            	{
            		t.skip();
            	}
            	else
            	{
            		t.back(t.MatchShift);
            		t.MatchShift = 0;
            	}

            	t.Matches = null;
            }
        }
    }
}