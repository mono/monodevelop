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
    public abstract class AtomicValue<T> : AtomicReference<T>
    {
        private readonly Object _locker = new Object();

        protected AtomicValue(T init) : base(init)
        {
        }

        protected AtomicValue()
        {
        }

        /// <summary>
        /// Atomically add the given value to current value.
        /// </summary>
        /// <param name="delta">the value to add</param>
        /// <returns>the updated value</returns>
        public T addAndGet(T delta)
        {
            lock (_locker)
            {
                T oldValue = get();
                T newValue = InnerAdd(oldValue, delta);
                set(newValue);
                return newValue;
            }
        }

        /// <summary>
        /// Atomically increment by one the current value.
        /// </summary>
        /// <returns>the updated value</returns>
        public T incrementAndGet()
        {
            lock (_locker)
            {
                return addAndGet(One);
            }
        }

        /// <summary>
        /// Atomically decrement by one the current value.
        /// </summary>
        /// <returns>the updated value</returns>
        public T decrementAndGet()
        {
            lock (_locker)
            {
                return addAndGet(MinusOne);
            }
        }

        protected abstract T InnerAdd(T value, T delta);
        protected abstract T One { get; }
        protected abstract T MinusOne { get; }
    }
}