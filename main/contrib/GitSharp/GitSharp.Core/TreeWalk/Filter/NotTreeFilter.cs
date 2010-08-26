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

namespace GitSharp.Core.TreeWalk.Filter
{


    /** Includes an entry only if the subfilter does not include the entry. */
    public class NotTreeFilter : TreeFilter
    {
        /**
         * Create a filter that negates the result of another filter.
         * 
         * @param a
         *            filter to negate.
         * @return a filter that does the reverse of <code>a</code>.
         */
        public static TreeFilter create(TreeFilter a)
        {
            return new NotTreeFilter(a);
        }

        private TreeFilter a;

        private NotTreeFilter(TreeFilter one)
        {
            a = one;
        }

        public override TreeFilter negate()
        {
            return a;
        }

        public override bool include(TreeWalk walker)
        {
            return !a.include(walker);
        }

        public override bool shouldBeRecursive()
        {
            return a.shouldBeRecursive();
        }

        public override TreeFilter Clone()
        {
            TreeFilter n = a.Clone();
            return n == a ? this : new NotTreeFilter(n);
        }

        public override string ToString()
        {
            return "NOT " + a.ToString();
        }
    }
}