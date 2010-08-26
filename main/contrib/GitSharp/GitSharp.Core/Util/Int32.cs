/*
 * Copyrigth (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GitSharp.Core.Util
{
    public static class Int32Extensions
    {
        /// <summary>
        /// computes the number of 1 bits in the two's complement binary representation of the integer
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int BitCount(this int n)
        {
            int i = n;
            int count = 0;
            while (i != 0)
            {
                count++;
                i &= (i - 1);
            }
            return count;
        }

        /// <summary>
        /// computes the number of 0 bits to the right of the first 1
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int NumberOfTrailingZeros(this int n)
        {
            Debug.Assert(n != 0);
            uint i = (uint)n;
            int zeros = 0;
            while ((i & 1) == 0)
            {
                zeros++;
                i >>= 1;
            }
            return zeros;
        }

        /// <summary>
        /// Returns the number of zero bits preceding the highest-order ("leftmost") one-bit in the two's complement 
        /// binary representation of the specified int value. Returns 32 if the specified value has no one-bits in its two's 
        /// complement representation, in other words if it is equal to zero.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int LowestOneBit(this int n)
        {
            if (n == 0)
                return 0;
            return 1 << NumberOfTrailingZeros(n);
        }
    }
}
