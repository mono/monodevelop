/* Copyright (C) 2008, Google Inc.
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
using System.Text;
using GitSharp.Core.Transport;
using System.Runtime.Serialization;

namespace GitSharp.Core.Exceptions
{
    /// <summary>
    /// Indicates a base/common object was required, but is not found.
    /// </summary>
	[Serializable]
    public class MissingBundlePrerequisiteException : TransportException
    {
        private const long serialVersionUID = 1L;

        private static string format(IDictionary<ObjectId, string> missingCommits)
        {
            var r = new StringBuilder();
            r.Append("missing prerequisite commits:");
            foreach (KeyValuePair<ObjectId, string> e in missingCommits)
            {
                r.Append("\n  ");
                r.Append(e.Key.Name);
                if (e.Value != null)
                    r.Append(" ").Append(e.Value);
            }
            return r.ToString();
        }

        /// <summary>
        /// Constructs a MissingBundlePrerequisiteException for a set of objects.
        /// </summary>
        /// <param name="uri">URI used for transport</param>
        /// <param name="missingCommits">
        /// the Map of the base/common object(s) we don't have. Keys are
        /// ids of the missing objects and values are short descriptions.
        /// </param>
        public MissingBundlePrerequisiteException(URIish uri, IDictionary<ObjectId, string> missingCommits)
        : base(uri, format(missingCommits))
        {
        }

        public MissingBundlePrerequisiteException(URIish uri, IDictionary<ObjectId, string> missingCommits, Exception inner)
        : base(uri, format(missingCommits), inner)
        {
        }

        protected MissingBundlePrerequisiteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}