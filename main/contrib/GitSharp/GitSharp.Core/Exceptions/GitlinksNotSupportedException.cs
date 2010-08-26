/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2007, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Jonas Fonseca <fonseca@diku.dk>
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

namespace GitSharp.Core.Exceptions
{
	/// <summary>
	/// An exception thrown when a gitlink entry is found and cannot be
	/// handled.
	/// </summary>
	[Serializable]
    public class GitlinksNotSupportedException : IOException
    {
        //private static  long serialVersionUID = 1L;

        /// <summary>
		/// Construct a GitlinksNotSupportedException for the specified link
		/// </summary>
		/// <param name="s">
		/// Name of link in tree or workdir
		/// </param>
        public GitlinksNotSupportedException(string s)
            : base(s)
        {
        }

		/// <summary>
		/// Construct a GitlinksNotSupportedException for the specified link
		/// </summary>
		/// <param name="s">Name of link in tree or workdir</param>
        /// <param name="inner">Inner Exception</param>
        public GitlinksNotSupportedException(string s, Exception inner)
            : base(s, inner)
        {
        }
		
		protected GitlinksNotSupportedException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
    }

}
