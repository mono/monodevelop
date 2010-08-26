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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitSharp.Core.Diff;
using GitSharp.Core.Util;

namespace GitSharp.Core.Merge
{
    /// <summary>
    /// A class to convert merge results into a Git conformant textual presentation
    /// </summary>
    public class MergeFormatter
    {
        /// <summary>
        /// Formats the results of a merge of <see cref="RawText"/> objects in a Git
        /// conformant way. This method also assumes that the <see cref="RawText"/> objects
        /// being merged are line oriented files which use LF as delimiter. This
        /// method will also use LF to separate chunks and conflict metadata,
        /// therefore it fits only to texts that are LF-separated lines.
        /// </summary>
        /// <param name="out">the outputstream where to write the textual presentation</param>
        /// <param name="res">the merge result which should be presented</param>
        /// <param name="seqName">
        /// When a conflict is reported each conflicting range will get a
        /// name. This name is following the "&lt;&lt;&lt;&lt;&lt;&lt;&lt; " or "&gt;&gt;&gt;&gt;&gt;&gt;&gt; "
        /// conflict markers. The names for the sequences are given in
        /// this list
        /// </param>
        /// <param name="charsetName">
        /// the name of the characterSet used when writing conflict
        /// metadata
        /// </param>
        public void formatMerge(BinaryWriter @out, MergeResult res,
                List<String> seqName, string charsetName)
        {
            String lastConflictingName = null; // is set to non-null whenever we are
            // in a conflict
            bool threeWayMerge = (res.getSequences().Count == 3);
            foreach (MergeChunk chunk in res)
            {
                RawText seq = (RawText)res.getSequences()[
                        chunk.getSequenceIndex()];
                if (lastConflictingName != null
                        && chunk.getConflictState() != MergeChunk.ConflictState.NEXT_CONFLICTING_RANGE)
                {
                    // found the end of an conflict
                    @out.Write((">>>>>>> " + lastConflictingName + "\n").getBytes(charsetName));
                    lastConflictingName = null;
                }
                if (chunk.getConflictState() == MergeChunk.ConflictState.FIRST_CONFLICTING_RANGE)
                {
                    // found the start of an conflict
                    @out.Write(("<<<<<<< " + seqName[chunk.getSequenceIndex()] +
                            "\n").getBytes(charsetName));
                    lastConflictingName = seqName[chunk.getSequenceIndex()];
                }
                else if (chunk.getConflictState() == MergeChunk.ConflictState.NEXT_CONFLICTING_RANGE)
                {
                    // found another conflicting chunk

                    /*
                     * In case of a non-three-way merge I'll add the name of the
                     * conflicting chunk behind the equal signs. I also append the
                     * name of the last conflicting chunk after the ending
                     * greater-than signs. If somebody knows a better notation to
                     * present non-three-way merges - feel free to correct here.
                     */
                    lastConflictingName = seqName[chunk.getSequenceIndex()];
                    @out.Write((threeWayMerge ? "=======\n" : "======= "
                            + lastConflictingName + "\n").getBytes(charsetName));
                }
                // the lines with conflict-metadata are written. Now write the chunk
                for (int i = chunk.getBegin(); i < chunk.getEnd(); i++)
                {
                    seq.writeLine(@out.BaseStream, i);
                    @out.Write('\n');

                }
            }
            // one possible leftover: if the merge result ended with a conflict we
            // have to close the last conflict here
            if (lastConflictingName != null)
            {
                @out.Write((">>>>>>> " + lastConflictingName + "\n").getBytes(charsetName));
            }
        }

        /// <summary>
        /// Formats the results of a merge of exactly two <see cref="RawText"/> objects in
        /// a Git conformant way. This convenience method accepts the names for the
        /// three sequences (base and the two merged sequences) as explicit
        /// parameters and doesn't require the caller to specify a List
        /// </summary>
        /// <param name="out">
        /// the <see cref="BinaryWriter"/> where to write the textual
        /// presentation
        /// </param>
        /// <param name="res">the merge result which should be presented</param>
        /// <param name="baseName">the name ranges from the base should get</param>
        /// <param name="oursName">the name ranges from ours should get</param>
        /// <param name="theirsName">the name ranges from theirs should get</param>
        /// <param name="charsetName">
        /// the name of the characterSet used when writing conflict
        /// metadata
        /// </param>
        public void formatMerge(BinaryWriter @out, MergeResult res, String baseName,
                String oursName, String theirsName, string charsetName)
        {
            var names = new List<String>(3);
            names.Add(baseName);
            names.Add(oursName);
            names.Add(theirsName);
            formatMerge(@out, res, names, charsetName);
        }
    }
}