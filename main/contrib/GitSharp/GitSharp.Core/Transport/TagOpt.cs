/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

namespace GitSharp.Core.Transport
{

    /// <summary>
    /// Specification of annotated tag behavior during fetch.
    /// </summary>
    public class TagOpt
    {
        /// <summary>
        /// Automatically follow tags if we fetch the thing they point at.
        /// <para />
        /// This is the default behavior and tries to balance the benefit of having
        /// an annotated tag against the cost of possibly objects that are only on
        /// branches we care nothing about. Annotated tags are fetched only if we can
        /// prove that we already have (or will have when the fetch completes) the
        /// object the annotated tag peels (dereferences) to.
        /// </summary>
        public static readonly TagOpt AUTO_FOLLOW = new TagOpt(string.Empty);

        /// <summary>
        /// Never fetch tags, even if we have the thing it points at.
        /// <para />
        /// This option must be requested by the user and always avoids fetching
        /// annotated tags. It is most useful if the location you are fetching from
        /// publishes annotated tags, but you are not interested in the tags and only
        /// want their branches.
        /// </summary>
        public static readonly TagOpt NO_TAGS = new TagOpt("--no-tags");

        /// <summary>
        /// Always fetch tags, even if we do not have the thing it points at.
        /// <para />
        /// Unlike {@link #AUTO_FOLLOW} the tag is always obtained. This may cause
        /// hundreds of megabytes of objects to be fetched if the receiving
        /// repository does not yet have the necessary dependencies.
        /// 
        /// </summary>
        public static readonly TagOpt FETCH_TAGS = new TagOpt("--tags");

        private static readonly TagOpt[] values = new[] { AUTO_FOLLOW, NO_TAGS, FETCH_TAGS };

        /// <summary>
        /// Command line/configuration file text for this value.
        /// </summary>
        public string Option { get; private set; }

        private TagOpt(string o)
        {
            Option = o;
        }

        /// <summary>
        /// Convert a command line/configuration file text into a value instance.
        /// </summary>
        /// <param name="o">the configuration file text value.</param>
        /// <returns>the option that matches the passed parameter.</returns>
        public static TagOpt fromOption(string o)
        {
            if (string.IsNullOrEmpty(o))
            {
                return AUTO_FOLLOW;
            }

            foreach (TagOpt tagopt in values)
            {
                if (tagopt.Option.Equals(o))
                    return tagopt;
            }
            throw new ArgumentException("Invalid tag option: " + o);
        }
    }
}