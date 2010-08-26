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

using GitSharp.Core.RevWalk;

namespace GitSharp.Core.RevPlot
{
    /// <summary>
    /// A commit reference to a commit in the DAG.
    /// </summary>
    public class PlotCommit : RevCommit
    {

        /// <summary>
        /// Obtain the lane this commit has been plotted into.
        /// </summary>
        /// <returns>the assigned lane for this commit.</returns>
        public PlotLane getLane()
        {
            return lane;
        }

        static PlotCommit[] NO_CHILDREN = { };

        static PlotLane[] NO_LANES = { };

        public PlotLane[] passingLanes;

        public PlotLane lane;

        public PlotCommit[] children;

        public Ref[] refs;

        /// <summary>
        /// Create a new commit.
        /// </summary>
        /// <param name="id">the identity of this commit.</param>
        /// <param name="tags">the tags associated with this commit, null for no tags</param>
        public PlotCommit(AnyObjectId id, Ref[] tags) : base(id)
        {
            this.refs = tags;
            passingLanes = NO_LANES;
            children = NO_CHILDREN;
        }

        public void addPassingLane(PlotLane c) {
            int cnt = passingLanes.Length;
            if (cnt == 0)
                passingLanes = new PlotLane[] { c };
            else if (cnt == 1)
                passingLanes = new PlotLane[] { passingLanes[0], c };
            else {
                PlotLane[] n = new PlotLane[cnt + 1];
                System.Array.Copy(passingLanes, 0, n, 0, cnt);
                n[cnt] = c;
                passingLanes = n;
            }
        }

        public void addChild(PlotCommit c)
        {
            int cnt = children.Length;
            if (cnt == 0)
                children = new PlotCommit[] { c };
            else if (cnt == 1)
                children = new PlotCommit[] { children[0], c };
            else
            {
                var n = new PlotCommit[cnt + 1];
                System.Array.Copy(children, 0, n, 0, cnt);
                n[cnt] = c;
                children = n;
            }
        }


        /// <summary>
        /// Get the number of child commits listed in this commit.
        /// </summary>
        /// <returns>number of children; always a positive value but can be 0.</returns>
        public int getChildCount()
        {
            return children.Length;
        }

        /// <summary>
        /// Get the nth child from this commit's child list.
        /// </summary>
        /// <param name="nth">child index to obtain. Must be in the range 0 through <see cref="getChildCount"/>() - 1</param>
        /// <returns>the specified child.</returns>
        public PlotCommit getChild(int nth)
        {
            return children[nth];
        }

        /// <summary>
        /// Determine if the given commit is a child (descendant) of this commit.
        /// </summary>
        /// <param name="c">the commit to test.</param>
        /// <returns>true if the given commit built on top of this commit.</returns>
        public bool isChild(PlotCommit c)
        {
            foreach (PlotCommit a in children)
                if (a == c)
                    return true;
            return false;
        }

        public override void reset()
        {
            passingLanes = NO_LANES;
            children = NO_CHILDREN;
            lane = null;
            base.reset();
        }
    }
}