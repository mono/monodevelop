/*
 * Copyright (C) 2008, Charles O'Farrell <charleso@charleso.org>
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
using System.Collections.Generic;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// Util for sorting (or comparing) Ref instances by name.
    /// <para />
    /// Useful for command line tools or writing out refs to file.
    /// </summary>
    public class RefComparator : IComparer<Ref>
    {
        /// <summary>
        /// Singleton instance of RefComparator
        /// </summary>
        public static RefComparator INSTANCE = new RefComparator();

        public int Compare(Ref o1, Ref o2)
        {
            return compareTo(o1, o2);
        }

        /// <summary>
        /// Sorts the collection of refs, returning a new collection.
        /// </summary>
        /// <param name="refs">collection to be sorted</param>
        /// <returns>sorted collection of refs</returns>
        public static IEnumerable<Ref> Sort(IEnumerable<Ref> refs)
        {
            var r = new List<Ref>(refs);
            r.Sort(INSTANCE);
            return r;
        }

        /// <summary>
        /// Compare a reference to a name.
        /// </summary>
        /// <param name="o1">the reference instance.</param>
        /// <param name="o2">the name to compare to.</param>
        /// <returns>standard Comparator result</returns>
        public static int compareTo(Ref o1, String o2)
        {
            return o1.Name.compareTo(o2);
        }

        /// <summary>
        /// Compare two references by name.
        /// </summary>
        /// <param name="o1">the reference instance.</param>
        /// <param name="o2">the other reference instance.</param>
        /// <returns>standard Comparator result</returns>
        public static int compareTo(Ref o1, Ref o2)
        {
            return o1.Name.compareTo(o2.Name);
        }
    }
}
