/*
 * Copyright (C) 2008, Google Inc.
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
using System.IO;
using System.Runtime.Serialization;
using GitSharp.Core.DirectoryCache;

namespace GitSharp.Core.Exceptions
{
	/// <summary>
	/// Indicates one or more paths in a DirCache have non-zero stages present.
	/// </summary>
    [Serializable]
	public class UnmergedPathException : IOException
    {
        private readonly DirCacheEntry _entry;

		/// <summary>
		/// Create a new unmerged path exception.
		/// </summary>
		/// <param name="entry">The first non-zero stage of the unmerged path.</param>
        public UnmergedPathException(DirCacheEntry entry) 
            : base("Unmerged path: " + entry.getPathString())
        {
            _entry = entry;
        }
		
		/// <summary>
		/// Create a new unmerged path exception.
		/// </summary>
		/// <param name="entry">The first non-zero stage of the unmerged path.</param>
        /// <param name="inner">Inner Exception.</param>
        public UnmergedPathException(DirCacheEntry entry, Exception inner) 
            : base("Unmerged path: " + entry.getPathString(), inner)
        {
            _entry = entry;
        }

        /// <summary>
		/// Returns the first non-zero stage of the unmerged path.
        /// </summary>
        /// <returns></returns>
        public DirCacheEntry DirCacheEntry
        {
            get { return _entry; }
        }

        protected UnmergedPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
