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
using GitSharp.Core.Util;

namespace GitSharp.Core.TreeWalk.Filter
{


    /**
     * Includes tree entries only if they match the configured path.
     * <para />
     * Applications should use {@link PathFilterGroup} to connect these into a tree
     * filter graph, as the group supports breaking out of traversal once it is
     * known the path can never match.
     */
    public class PathFilter : TreeFilter
    {
        /**
         * Create a new tree filter for a user supplied path.
         * <para />
         * Path strings are relative to the root of the repository. If the user's
         * input should be assumed relative to a subdirectory of the repository the
         * caller must prepend the subdirectory's path prior to creating the filter.
         * <para />
         * Path strings use '/' to delimit directories on all platforms.
         * 
         * @param path
         *            the path to filter on. Must not be the empty string. All
         *            trailing '/' characters will be trimmed before string's Length
         *            is checked or is used as part of the constructed filter.
         * @return a new filter for the requested path.
         * @throws ArgumentException
         *             the path supplied was the empty string.
         */
        public static PathFilter create(string path)
        {
			if (path == null)
				throw new ArgumentNullException ("path");
            while (path.EndsWith("/"))
                path = path.Slice(0, path.Length - 1);
            if (path.Length == 0)
                throw new ArgumentException("Empty path not permitted.");
            return new PathFilter(path);
        }

        public string pathStr;

        public byte[] pathRaw;

        private PathFilter(string s)
        {
            pathStr = s;
            pathRaw = Constants.encode(pathStr);
        }

        public override bool include(TreeWalk walker)
        {
			if (walker == null)
				throw new ArgumentNullException ("walker");
            return walker.isPathPrefix(pathRaw, pathRaw.Length) == 0;
        }

        public override bool shouldBeRecursive()
        {
            foreach (byte b in pathRaw)
                if (b == '/')
                    return true;
            return false;
        }

        public override TreeFilter Clone()
        {
            return this;
        }

        public override string ToString()
        {
            return "PATH(\"" + pathStr + "\")";
        }
    }
}