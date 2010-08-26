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
using System.Collections.ObjectModel;
using System.Linq;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core.RevPlot
{
    /// <summary>
    /// An ordered list of <see cref="PlotCommit"/> subclasses.
    /// <para>
    /// Commits are allocated into lanes as they enter the list, based upon their
    /// connections between descendant (child) commits and ancestor (parent) commits.
    /// </para>
    /// <para>
    /// The source of the list must be a {@link PlotWalk} and {@link #fillTo(int)}
    /// must be used to populate the list.
    /// </para>
    /// </summary>
    public class PlotCommitList :
        RevCommitList<PlotCommit> {
        private int lanesAllocated;

        private SortedList<int, int> freeLanes = new SortedList<int, int>();

        private HashSet<PlotLane> activeLanes = new HashSet<PlotLane>(); // was new HashSet<PlotLane>(32);

        public override void clear() {
            base.clear();
            lanesAllocated = 0;
            freeLanes.Clear();
            activeLanes.Clear();
        }

        public override void Source(RevWalk.RevWalk walker) {
            if (!(walker is PlotWalk))
                throw new ArgumentException("Not a " + typeof(PlotWalk).FullName);
            base.Source(walker);
        }
        
        /// <summary>
        /// Find the set of lanes passing through a commit's row.
        /// <para>Lanes passing through a commit are lanes that the commit is not directly
        /// on, but that need to travel through this commit to connect a descendant
        /// (child) commit to an ancestor (parent) commit. Typically these lanes will
        /// be drawn as lines in the passed commit's box, and the passed commit won't
        /// appear to be connected to those lines.</para>
        /// <para>This method modifies the passed collection by adding the lanes in any order.</para>
        /// </summary>
        /// <param name="currCommit">the commit the caller needs to get the lanes from.</param>
        /// <param name="result">collection to add the passing lanes into.</param>
        public void findPassingThrough(PlotCommit currCommit,
                                       Collection<PlotLane> result) 
		{
			if (currCommit == null)
				throw new ArgumentNullException ("currCommit");
        	if (result == null)
				throw new ArgumentNullException ("result");
			
            foreach (PlotLane p in currCommit.passingLanes)
                result.Add((PlotLane) p);
        }

        protected override void enter(int index, PlotCommit currCommit) {
            setupChildren(currCommit);

            int nChildren = currCommit.getChildCount();
            if (nChildren == 0)
                return;

            if (nChildren == 1 && currCommit.children[0].ParentCount < 2) {
                // Only one child, child has only us as their parent.
                // Stay in the same lane as the child.
                //
                PlotCommit c = currCommit.children[0];
                if (c.lane == null) {
                    // Hmmph. This child must be the first along this lane.
                    //
                    c.lane = nextFreeLane();
                    activeLanes.Add(c.lane);
                }

                for (int r = index - 1; r >= 0; r--) {
                    PlotCommit rObj = get(r);
                    if (rObj == c)
                        break;
                    rObj.addPassingLane(c.lane);
                }
                currCommit.lane = c.lane;
                currCommit.lane.parent = currCommit;
            } else {
                // More than one child, or our child is a merge.
                // Use a different lane.
                //

                for (int i = 0; i < nChildren; i++) {
                    PlotCommit c = currCommit.children[i];
                    if (activeLanes.Remove(c.lane)) {
                        recycleLane(c.lane);
                        freeLanes.Add(c.lane.getPosition(), c.lane.getPosition());
                    }
                }

                currCommit.lane = nextFreeLane();
                currCommit.lane.parent = currCommit;
                activeLanes.Add(currCommit.lane);

                int remaining = nChildren;
                for (int r = index - 1; r >= 0; r--) {
                    PlotCommit rObj = get(r);
                    if (currCommit.isChild(rObj)) {
                        if (--remaining == 0)
                            break;
                    }
                    rObj.addPassingLane(currCommit.lane);
                }
            }
        }

        private void setupChildren(PlotCommit currCommit) {
            int nParents = currCommit.ParentCount;
            for (int i = 0; i < nParents; i++)
                ((PlotCommit)currCommit.GetParent(i)).addChild(currCommit);
        }

        private PlotLane nextFreeLane() {
            PlotLane p = createLane();
            if (freeLanes.Count == 0) {
                p.position = lanesAllocated++;
            } else {
                int min = freeLanes.First().Key;
                p.position = min;
                freeLanes.Remove(min);
            }
            return p;
        }

        /// <returns>a new Lane appropriate for this particular PlotList.</returns>
        protected PlotLane createLane()
        {
            return new PlotLane();
        }

        /// <summary>
        /// Return colors and other reusable information to the plotter when a lane is no longer needed.
        /// </summary>
        protected void recycleLane(PlotLane lane)
        {
            // Nothing.
        }
    }
}