/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.TreeWalk.Filter
{


    /**
     * Includes tree entries only if they match one or more configured paths.
     * <para />
     * Operates like {@link PathFilter} but causes the walk to abort as soon as the
     * tree can no longer match any of the paths within the group. This may bypass
     * the bool logic of a higher level AND or OR group, but does improve
     * performance for the common case of examining one or more modified paths.
     * <para />
     * This filter is effectively an OR group around paths, with the early abort
     * feature described above.
     */
    public static class PathFilterGroup
    {
        /**
         * Create a collection of path filters from Java strings.
         * <para />
         * Path strings are relative to the root of the repository. If the user's
         * input should be assumed relative to a subdirectory of the repository the
         * caller must prepend the subdirectory's path prior to creating the filter.
         * <para />
         * Path strings use '/' to delimit directories on all platforms.
         * <para />
         * Paths may appear in any order within the collection. Sorting may be done
         * internally when the group is constructed if doing so will improve path
         * matching performance.
         * 
         * @param paths
         *            the paths to test against. Must have at least one entry.
         * @return a new filter for the list of paths supplied.
         */
        public static TreeFilter createFromStrings(IEnumerable<string> paths)
        {
            if (paths.Count()==0)
                throw new ArgumentException("At least one path is required.");
            PathFilter[] p = new PathFilter[paths.Count()];
            int i = 0;
            foreach (string s in paths)
                p[i++] = PathFilter.create(s);
            return create(p);
        }

        /**
         * Create a collection of path filters.
         * <para />
         * Paths may appear in any order within the collection. Sorting may be done
         * internally when the group is constructed if doing so will improve path
         * matching performance.
         * 
         * @param paths
         *            the paths to test against. Must have at least one entry.
         * @return a new filter for the list of paths supplied.
         */
        public static TreeFilter create(IEnumerable<PathFilter> paths)
        {
            if (paths.Count() == 0)
                throw new ArgumentException("At least one path is required.");
            PathFilter[] p = paths.ToArray();
            return create(p);
        }

        private static TreeFilter create(PathFilter[] p)
        {
            if (p.Length == 1)
                return new Single(p[0]);
            return new Group(p);
        }

        public class Single : TreeFilter
        {
            private PathFilter path;

            private byte[] raw;

            public Single(PathFilter p)
            {
                path = p;
                raw = path.pathRaw;
            }

            public override bool include(TreeWalk walker)
            {
				if (walker == null)
					throw new ArgumentNullException ("walker");
                int cmp = walker.isPathPrefix(raw, raw.Length);
                if (cmp > 0)
                    throw StopWalkException.INSTANCE;
                return cmp == 0;
            }

            public override bool shouldBeRecursive()
            {
                return path.shouldBeRecursive();
            }

            public override TreeFilter Clone()
            {
                return this;
            }

            public override string ToString()
            {
                return "FAST_" + path.ToString();
            }
        }

        public class Group : TreeFilter
        {
            private static Comparison<PathFilter> PATH_SORT = new Comparison<PathFilter>((o1, o2) => o1.pathStr.CompareTo(o2.pathStr));


            private PathFilter[] paths;

            public Group(PathFilter[] p)
            {
                paths = p;
                Array.Sort(paths, PATH_SORT);
            }

            public override bool include(TreeWalk walker)
            {
				if (walker == null)
					throw new ArgumentNullException ("walker");
                int n = paths.Length;
                for (int i = 0; ; )
                {
                    byte[] r = paths[i].pathRaw;
                    int cmp = walker.isPathPrefix(r, r.Length);
                    if (cmp == 0)
                        return true;
                    if (++i < n)
                        continue;
                    if (cmp > 0)
                        throw StopWalkException.INSTANCE;
                    return false;
                }
            }

            public override bool shouldBeRecursive()
            {
                foreach (PathFilter p in paths)
                    if (p.shouldBeRecursive())
                        return true;
                return false;
            }

            public override TreeFilter Clone()
            {
                return this;
            }

            public override string ToString()
            {
                StringBuilder r = new StringBuilder();
                r.Append("FAST(");
                for (int i = 0; i < paths.Length; i++)
                {
                    if (i > 0)
                        r.Append(" OR ");
                    r.Append(paths[i].ToString());
                }
                r.Append(")");
                return r.ToString();
            }
        }
    }
}
