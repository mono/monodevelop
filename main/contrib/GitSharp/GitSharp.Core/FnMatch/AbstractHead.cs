/*
 * Copyright (C) 2008, Florian Köberle <florianskarten@web.de>
 * Copyright (C) 2009, Adriano Machado <adriano.m.machado@hotmail.com>
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

namespace GitSharp.Core.FnMatch
{
    internal abstract class AbstractHead : IHead
    {
        private IList<IHead> _newHeads;

        private readonly bool _star;

        protected internal abstract bool matches(char c);

        protected AbstractHead(bool star)
        {
            _star = star;
        }

        /// <summary>
        /// </summary>
        /// <param name="newHeads">A list of <seealso cref="IHead"/>s which will not be modified.</param>
        public void setNewHeads(IList<IHead> newHeads)
        {
            if (_newHeads != null)
            {
                throw new InvalidOperationException("Property is already non null");
            }
            
            _newHeads = newHeads;
        }

        public virtual IList<IHead> GetNextHeads(char c)
        {
            if (matches(c))
            {
                return _newHeads;
            }
            
            return FileNameMatcher.EmptyHeadList;
        }

        internal virtual bool IsStar
        {
            get { return _star; }
        }
    }
}
