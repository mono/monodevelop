/*
 * Copyright (C) 2009, Google Inc.
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

namespace GitSharp.Core.RevWalk
{


    /**
     * Delays commits to be at least {@link PendingGenerator#OVER_SCAN} late.
     * <para />
     * This helps to "fix up" weird corner cases resulting from clock skew, by
     * slowing down what we produce to the caller we get a better chance to ensure
     * PendingGenerator reached back far enough in the graph to correctly mark
     * commits {@link RevWalk#UNINTERESTING} if necessary.
     * <para />
     * This generator should appear before {@link FixUninterestingGenerator} if the
     * lower level {@link #pending} isn't already fully buffered.
     */
    class DelayRevQueue : Generator
    {
        private readonly Generator _pending;
        private readonly FIFORevQueue _delay;
        private int _size;

        public DelayRevQueue(Generator g)
        {
            _pending = g;
            _delay = new FIFORevQueue();
        }

        public override GeneratorOutputType OutputType
        {
            get { return _pending.OutputType; }
        }

        public override RevCommit next()
        {
            while (_size < PendingGenerator.OVER_SCAN)
            {
                RevCommit c = _pending.next();
                if (c == null) break;
                _delay.add(c);
                _size++;
            }

            RevCommit cc = _delay.next();
            if (cc == null) return null;
            _size--;
            return cc;
        }
    }
}