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

using System;
using System.IO;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
	/// <summary>
	/// Indicates a local repository does not exist
	/// </summary>
    [Serializable]
	public class RepositoryNotFoundException : TransportException
    {
        //private static  long serialVersionUID = 1L;

		/// <summary>
		/// Constructs an exception indicating a local repository does not exist
		/// </summary>
		/// <param name="location">
		/// Description of the repository not found, usually file path
		/// </param>
        public RepositoryNotFoundException(DirectoryInfo location)
            : this(location.ToString())
        {
        }

        /// <summary>
		/// Constructs an exception indicating a local repository does not exist
		/// </summary>
		/// <param name="location">
		/// Description of the repository not found, usually file path
		/// </param>
        public RepositoryNotFoundException(string location)
            : base("repository not found: " + location)
        {
        }
		
		/// <summary>
		/// Constructs an exception indicating a local repository does not exist
		/// </summary>
		/// <param name="location">Description of the repository not found, usually file path</param>
        /// <param name="inner">Inner Exception.</param>
        public RepositoryNotFoundException(DirectoryInfo location, Exception inner)
            : this(location.ToString(),inner)
        {
        }

        /// <summary>
		/// Constructs an exception indicating a local repository does not exist
		/// </summary>
		/// <param name="location">Description of the repository not found, usually file path</param>
        /// <param name="inner">Inner Exception.</param>
        public RepositoryNotFoundException(string location, Exception inner)
            : base("repository not found: " + location, inner)
        {
        }

        protected RepositoryNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
