/*
 * Copyright (C) 2009, Christian Halstrick <christian.halstrick@sap.com>
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

namespace GitSharp.Core.Merge
{
    /// <summary>
    /// One chunk from a merge result. Each chunk contains a range from a
    /// single sequence. In case of conflicts multiple chunks are reported for one
    /// conflict. The conflictState tells when conflicts start and end.
    /// </summary>
    public class MergeChunk
    {

        /// <summary>
        /// A state telling whether a MergeChunk belongs to a conflict or not. The
        /// first chunk of a conflict is reported with a special state to be able to
        /// distinguish the border between two consecutive conflicts
        /// </summary>
        public enum ConflictState
        {
            /// <summary>
            /// This chunk does not belong to a conflict
            /// </summary>
            NO_CONFLICT = 0,

            /// <summary>
            /// This chunk does belong to a conflict and is the first one of the conflicting chunks
            /// </summary>
            FIRST_CONFLICTING_RANGE = 1,

            /// <summary>
            /// This chunk does belong to a conflict but is not the first one of the conflicting chunks. It's a subsequent one.
            /// </summary>
            NEXT_CONFLICTING_RANGE = 2
        }

        private int sequenceIndex;

        private int begin;

        private int end;

        private ConflictState conflictState;

        /// <summary>
        /// Creates a new empty MergeChunk
        /// </summary>
        /// <param name="sequenceIndex">determines to which sequence this chunks belongs to. Same as in <see cref="MergeResult.add"/>
        /// </param>
        /// <param name="begin">the first element from the specified sequence which should be included in the merge result. Indexes start with 0.</param>
        /// <param name="end">
        /// specifies the end of the range to be added. The element this index points to is the first element which not added to the
        /// merge result. All elements between begin (including begin) and this element are added.
        /// </param>
        /// <param name="conflictState">the state of this chunk. See <see cref="ConflictState"/></param>
        public MergeChunk(int sequenceIndex, int begin, int end,
            ConflictState conflictState)
        {
            this.sequenceIndex = sequenceIndex;
            this.begin = begin;
            this.end = end;
            this.conflictState = conflictState;
        }

        /// <returns>the index of the sequence to which sequence this chunks belongs to. Same as in <see cref="MergeResult.add"/></returns>
        public int getSequenceIndex()
        {
            return sequenceIndex;
        }

        /// <returns>the first element from the specified sequence which should be included in the merge result. Indexes start with 0.</returns>
        public int getBegin()
        {
            return begin;
        }

        /// <returns>
        /// the end of the range of this chunk. The element this index points to is the first element which not added to the merge
        /// result. All elements between begin (including begin) and this element are added.
        /// </returns>
        public int getEnd()
        {
            return end;
        }

        /// <returns>the state of this chunk. See <see cref="ConflictState"/></returns>
        public ConflictState getConflictState()
        {
            return conflictState;
        }
    }
}