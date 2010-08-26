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
 * - Neither the name of the Eclipse Foundation, Inc. nor the
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
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core.RevPlot
{
    /// <summary>
    /// Specialized RevWalk for visualization of a commit graph.
    /// </summary>
    public class PlotWalk : RevWalk.RevWalk {

        private Dictionary<AnyObjectId, List<Ref>> reverseRefMap;

        public override void Dispose() {
            base.Dispose();
            reverseRefMap.Clear();
        }

        /// <summary>
        /// Create a new revision walker for a given repository.
        /// </summary>
        /// <param name="repo">the repository the walker will obtain data from.</param>
        public PlotWalk(Repository repo) : base(repo) {
            base.sort(RevSort.TOPO, true);
            reverseRefMap = repo.getAllRefsByPeeledObjectId();
        }

        public override void sort(RevSort.Strategy s, bool use) {
            if (s == RevSort.TOPO && !use)
                throw new ArgumentException("Topological sort required.");
            base.sort(s, use);
        }

        protected override RevCommit createCommit(AnyObjectId id) {
            return new PlotCommit(id, getTags(id));
        }

        /// <returns>the list of knows tags referring to this commit</returns>
        protected Ref[] getTags(AnyObjectId commitId) {
            if (!reverseRefMap.ContainsKey(commitId))
                return null;
            Ref[] tags = reverseRefMap[commitId].ToArray();
            Array.Sort(tags, new PlotRefComparator(Repository));
            return tags;
        }

        class PlotRefComparator : IComparer<Ref> {
            private readonly Repository _repository;

            public PlotRefComparator(Repository repository)
            {
                _repository = repository;
            }

            public int Compare(Ref o1, Ref o2) {
                try {
                    Object obj1 = _repository.MapObject(o1.ObjectId, o1.Name);
                    Object obj2 = _repository.MapObject(o2.ObjectId, o2.Name);
                    long t1 = timeof(obj1);
                    long t2 = timeof(obj2);
                    if (t1 > t2)
                        return -1;
                    if (t1 < t2)
                        return 1;
                    return 0;
                } catch (IOException) {
                    // ignore
                    return 0;
                }
            }
            long timeof(Object o) {
                if (o is Commit)
                    return ((Commit)o).Committer.When;
                if (o is Tag)
                    return ((Tag)o).Tagger.When;
                return 0;
            }
        }
    }
}