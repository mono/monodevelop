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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Core.Util
{

    /**
     * A rough character sequence around a raw byte buffer.
     * <para />
     * Characters are assumed to be 8-bit US-ASCII.
     */
    public class RawCharSequence : ICharSequence
    {
        /** A zero-Length character sequence. */
        public static RawCharSequence EMPTY = new RawCharSequence(null, 0, 0);

        public byte[] buffer;

        public int startPtr;

        public int endPtr;

        /**
         * Create a rough character sequence around the raw byte buffer.
         *
         * @param buf
         *            buffer to scan.
         * @param start
         *            starting position for the sequence.
         * @param end
         *            ending position for the sequence.
         */
        public RawCharSequence(byte[] buf, int start, int end)
        {
            buffer = buf;
            startPtr = start;
            endPtr = end;
        }

        public char CharAt(int index)
        {
            return (char)(buffer[startPtr + index] & 0xff);
        }

        public int Length()
        {
            return endPtr - startPtr;
        }

        public ICharSequence subSequence(int start, int end)
        {
            return new RawCharSequence(buffer, startPtr + start, startPtr + end);
        }

        public override string ToString()
        {
            int n = Length();
            StringBuilder b = new StringBuilder(n);
            for (int i = 0; i < n; i++)
                b.Append(CharAt(i));
            return b.ToString();
        }
    }
}
