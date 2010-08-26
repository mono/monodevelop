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

namespace GitSharp.Core.RevWalk
{

    /** Sorting strategies supported by {@link RevWalk} and {@link ObjectWalk}. */
    public static class RevSort
    {
		[Serializable]
        public enum Strategy
        {
            /**
             * No specific sorting is requested.
             * <para />
             * Applications should not rely upon the ordering produced by this strategy.
             * Any ordering in the output is caused by low level implementation details
             * and may change without notice.
             */
            NONE,

            /**
             * Sort by commit time, descending (newest first, oldest last).
             * <para />
             * This strategy can be combined with {@link #TOPO}.
             */
            COMMIT_TIME_DESC,

            /**
             * Topological sorting (all children before parents).
             * <para />
             * This strategy can be combined with {@link #COMMIT_TIME_DESC}.
             */
            TOPO,

            /**
             * Flip the output into the reverse ordering.
             * <para />
             * This strategy can be combined with the others described by this type as
             * it is usually performed at the very end.
             */
            REVERSE,

            /**
             * Include {@link RevFlag#UNINTERESTING} boundary commits After all others.
             * In {@link ObjectWalk}, objects associated with such commits (trees,
             * blobs), and all other objects marked explicitly as UNINTERESTING are also
             * included.
             * <para />
             * A boundary commit is a UNINTERESTING parent of an interesting commit that
             * was previously output.
             */
            BOUNDARY,
        }

        public static Strategy NONE = Strategy.NONE;
        public static Strategy COMMIT_TIME_DESC = Strategy.COMMIT_TIME_DESC;
        public static Strategy TOPO = Strategy.TOPO;
        public static Strategy REVERSE = Strategy.REVERSE;
        public static Strategy BOUNDARY = Strategy.BOUNDARY;
    }
}