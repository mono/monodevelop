/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
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

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Single shot fetch from a streamed Git bundle.
    /// <para/>
    /// The bundle is Read from an unbuffered input stream, which limits the
    /// transport to opening at most one FetchConnection before needing to recreate
    /// the transport instance.
    /// </summary>
    public class TransportBundleStream : Transport, ITransportBundle
    {
        private Stream _inputStream;

        /// <summary>
        /// Create a new transport to fetch objects from a streamed bundle.
        /// <para/>
        /// The stream can be unbuffered (buffering is automatically provided
        /// internally to smooth out short reads) and unpositionable (the stream is
        /// Read from only once, sequentially).
        /// <para/>
        /// When the FetchConnection or the this instance is closed the supplied
        /// input stream is also automatically closed. This frees callers from
        /// needing to keep track of the supplied stream.
        /// </summary>
        /// <param name="local">repository the fetched objects will be loaded into.</param>
        /// <param name="uri">
        /// symbolic name of the source of the stream. The URI can
        /// reference a non-existent resource. It is used only for
        /// exception reporting.
        /// </param>
        /// <param name="inputStream">the stream to Read the bundle from.</param>
        public TransportBundleStream(Repository local, URIish uri, Stream inputStream)
            : base(local, uri)
        {
            _inputStream = inputStream;
        }

        public override IFetchConnection openFetch()
        {
            if (_inputStream == null)
                throw new TransportException(Uri, "Only one fetch supported");
            try
            {
                return new BundleFetchConnection(this, _inputStream);
            }
            finally
            {
                _inputStream = null;
            }
        }

        public override IPushConnection openPush()
        {
            throw new NotSupportedException("Push is not supported for bundle transport");
        }

        public override void close()
        {
            if (_inputStream != null)
            {
                try
                {
                    _inputStream.Dispose();
                }
                catch (IOException)
                {
                    // Ignore a close error.
                }
                finally
                {
                    _inputStream = null;
                }
            }

#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif

        }
#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~TransportBundleStream()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: {" + Local.Directory + "}/{" + Uri + "}");
        }
#endif
    }
}