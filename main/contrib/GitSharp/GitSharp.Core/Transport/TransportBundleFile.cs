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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    public class TransportBundleFile : Transport, ITransportBundle
    {
        private readonly FileInfo _bundle;

        public static bool canHandle(URIish uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");
            if (uri.Host != null || uri.Port > 0 || uri.User != null || uri.Pass != null || uri.Path == null)
                return false;

            if ("file".Equals(uri.Scheme) || uri.Scheme == null)
            {
                FileInfo f = PathUtil.CombineFilePath(new DirectoryInfo("."), uri.Path);
                return f.IsFile() || f.Name.EndsWith(".bundle");
            }

            return false;
        }

        public TransportBundleFile(Repository local, URIish uri)
            : base(local, uri)
        {
            _bundle = PathUtil.CombineFilePath(new DirectoryInfo("."), uri.Path);
        }

        public override IFetchConnection openFetch()
        {
            Stream src;
            try
            {
                src = _bundle.Open(System.IO.FileMode.Open, FileAccess.Read);
            }
            catch (FileNotFoundException)
            {
                throw new TransportException(Uri, "not found");
            }

            return new BundleFetchConnection(this, src);
        }

        public override IPushConnection openPush()
        {
            throw new NotSupportedException("Push is not supported for bundle transport");
        }

        public override void close()
        {
            // Resources must be established per-connection.
        }
    }
}