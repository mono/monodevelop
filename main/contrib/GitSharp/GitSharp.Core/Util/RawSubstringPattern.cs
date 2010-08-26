/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using GitSharp.Core.Util;

namespace GitSharp.Core.Util
{
    /**
     * Searches text using only substring search.
     * <para />
     * Instances are thread-safe. Multiple concurrent threads may perform matches on
     * different character sequences at the same time.
     */
    public class RawSubStringPattern
    {
        private String needleString;

        private byte[] needle;

        /**
         * Construct a new substring pattern.
         * 
         * @param patternText
         *            text to locate. This should be a literal string, as no
         *            meta-characters are supported by this implementation. The
         *            string may not be the empty string.
         */
        public RawSubStringPattern(string patternText)
        {
			if (patternText == null)
				throw new ArgumentNullException ("patternText");
            if (patternText.Length == 0)
                throw new ArgumentException("Cannot match on empty string.","patternText");
            needleString = patternText;
            byte[] b = Constants.encode(patternText);
            needle = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
                needle[i] = lc(b[i]);
        }

        /**
         * Match a character sequence against this pattern.
         * 
         * @param rcs
         *            the sequence to match. Must not be null but the Length of the
         *            sequence is permitted to be 0.
         * @return offset within <code>rcs</code> of the first occurrence of this
         *         pattern; -1 if this pattern does not appear at any position of
         *         <code>rcs</code>.
         */
        public int match(RawCharSequence rcs)
        {
			if (rcs == null)
				throw new ArgumentNullException ("rcs");
			
            int needleLen = needle.Length;
            byte first = needle[0];

            byte[] text = rcs.buffer;
            int matchPos = rcs.startPtr;
            int maxPos = rcs.endPtr - needleLen;

            for (; matchPos < maxPos; matchPos++)
            {
                if (neq(first, text[matchPos]))
                {
                    while (++matchPos < maxPos && neq(first, text[matchPos]))
                    {
                        /* skip */
                    }
                    if (matchPos == maxPos)
                        return -1;
                }

                int si = ++matchPos;

                bool outer_continue = false;
                for (int j = 1; j < needleLen; j++, si++)
                {
                    if (neq(needle[j], text[si]))
                        outer_continue = true;
                }

                if (outer_continue)
                    continue;

                return matchPos - 1;
            }
            return -1;
        }

        private static bool neq(byte a, byte b)
        {
            return a != b && a != lc(b);
        }

        private static byte lc(byte q)
        {
            return (byte)StringUtils.toLowerCase((char)(q & 0xff));
        }

        /**
         * Get the literal pattern string this instance searches for.
         * 
         * @return the pattern string given to our constructor.
         */
        public string pattern()
        {
            return needleString;
        }

        public override string ToString()
        {
            return pattern();
        }
    }
}
