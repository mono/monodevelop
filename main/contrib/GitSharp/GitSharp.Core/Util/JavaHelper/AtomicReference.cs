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

namespace GitSharp.Core.Util.JavaHelper
{
    public class AtomicReference<T>
    {
        private T _reference;
        private readonly Object _locker = new Object();

        public AtomicReference() : this(default(T))
        {
        }

        public AtomicReference(T reference)
        {
            _reference = reference;
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value == the expected value. the expected value
        /// </summary>
        /// <param name="expected">the expected value</param>
        /// <param name="update">the new value</param>
        /// <returns>true if successful. False return indicates that the actual value was not equal to the expected value.</returns>
        public bool compareAndSet(T expected, T update)
        {
            lock (_locker)
            {
                if ((Equals(_reference, default(T)) && Equals(expected, default(T))) || (!Equals(_reference, default(T)) && _reference.Equals(expected)))
                {
                    _reference = update;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Set to the given value.
        /// </summary>
        /// <param name="update">the new value</param>
        public void set(T update)
        {
            lock (_locker)
            {
                _reference = update;
            }
        }

        /// <summary>
        /// Get the current value.
        /// </summary>
        /// <returns>the current value</returns>
        public T get()
        {
            lock (_locker)
            {
                return _reference;
            }
        }
    }
}