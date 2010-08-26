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

using System;
using GitSharp.Core.RevWalk.Filter;
using GitSharp.Core.TreeWalk.Filter;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Initial RevWalk generator that bootstraps a new walk.
	/// <para />
	/// Initially RevWalk starts with this generator as its chosen implementation.
	/// The first request for a <see cref="RevCommit"/> from the <see cref="RevWalk"/> 
	/// instance calls to our <see cref="next()"/> method, and we replace ourselves with 
	/// the best <see cref="Generator"/> implementation available based upon the 
	/// current configuration.
	/// </summary>
	public class StartGenerator : Generator, IDisposable
	{
		private readonly RevWalk _walker;

		public StartGenerator(RevWalk walker)
		{
			_walker = walker;
		}

		public override GeneratorOutputType OutputType
		{
			get { return 0; }
		}

		public override RevCommit next()
		{
			RevWalk w = _walker;
			RevFilter rf = w.getRevFilter();
			TreeFilter tf = w.getTreeFilter();
			AbstractRevQueue q = _walker.Queue;

			if (rf == RevFilter.MERGE_BASE)
			{
				// Computing for merge bases is a special case and does not
				// use the bulk of the generator pipeline.
				//
				if (tf != TreeFilter.ALL)
				{
					throw new InvalidOperationException("Cannot combine TreeFilter " + tf + " with RevFilter " + rf + ".");
				}

				var mbg = new MergeBaseGenerator(w);
				_walker.Pending = mbg;
				_walker.Queue = AbstractRevQueue.EmptyQueue;
				mbg.init(q);
				return mbg.next();
			}

			bool uninteresting = q.anybodyHasFlag(RevWalk.UNINTERESTING);
			bool boundary = _walker.hasRevSort(RevSort.BOUNDARY);

			if (!boundary && _walker is ObjectWalk)
			{
				// The object walker requires boundary support to color
				// trees and blobs at the boundary uninteresting so it
				// does not produce those in the result.
				//
				boundary = true;
			}

			if (boundary && !uninteresting)
			{
				// If we were not fed uninteresting commits we will never
				// construct a boundary. There is no reason to include the
				// extra overhead associated with that in our pipeline.
				//
				boundary = false;
			}

			DateRevQueue pending = (q as DateRevQueue);
			GeneratorOutputType pendingOutputType = 0;
			if (pending == null)
			{
				pending = new DateRevQueue(q);
			}

			if (tf != TreeFilter.ALL)
			{
				rf = AndRevFilter.create(rf, new RewriteTreeFilter(w, tf));
				pendingOutputType |= GeneratorOutputType.HasRewrite | GeneratorOutputType.NeedsRewrite;
			}

			_walker.Queue = q;
			Generator g = new PendingGenerator(w, pending, rf, pendingOutputType);

			if (boundary)
			{
				// Because the boundary generator may produce uninteresting
				// commits we cannot allow the pending generator to dispose
				// of them early.
				//
				((PendingGenerator)g).CanDispose = false;
			}

			if ((g.OutputType & GeneratorOutputType.NeedsRewrite) != GeneratorOutputType.None)
			{
				// Correction for an upstream NEEDS_REWRITE is to buffer
				// fully and then Apply a rewrite generator that can
				// pull through the rewrite chain and produce a dense
				// output graph.
				//
				g = new FIFORevQueue(g);
				g = new RewriteGenerator(g);
			}

			if (_walker.hasRevSort(RevSort.TOPO) && (g.OutputType & GeneratorOutputType.SortTopo) == 0)
			{
				g = new TopoSortGenerator(g);
			}

			if (_walker.hasRevSort(RevSort.REVERSE))
			{
				g = new LIFORevQueue(g);
			}

			if (boundary)
			{
				g = new BoundaryGenerator(w, g);
			}
			else if (uninteresting)
			{
				// Try to protect ourselves from uninteresting commits producing
				// due to clock skew in the commit time stamps. Delay such that
				// we have a chance at coloring enough of the graph correctly,
				// and then strip any UNINTERESTING nodes that may have leaked
				// through early.
				//
				if (pending.peek() != null)
				{
					g = new DelayRevQueue(g);
				}
				g = new FixUninterestingGenerator(g);
			}

			w.Pending = g;
			return g.next();
		}
		
		public void Dispose ()
		{
			_walker.Dispose();
		}
		
	}
}
