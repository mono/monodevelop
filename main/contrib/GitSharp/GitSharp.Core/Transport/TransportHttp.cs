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
using System.Collections.Generic;
using System.IO;
using System.Net;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Transport over HTTP and FTP protocols.
    /// <para/>
    /// If the transport is using HTTP and the remote HTTP service is Git-aware
    /// (speaks the "smart-http protocol") this client will automatically take
    /// advantage of the additional Git-specific HTTP extensions. If the remote
    /// service does not support these extensions, the client will degrade to direct
    /// file fetching.
    /// <para/>
    /// If the remote (server side) repository does not have the specialized Git
    /// support, object files are retrieved directly through standard HTTP GET (or
    /// binary FTP GET) requests. This make it easy to serve a Git repository through
    /// a standard web host provider that does not offer specific support for Git.
    /// </summary>
    public class TransportHttp : HttpTransport, IWalkTransport
    {
        public static bool canHandle(URIish uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (!uri.IsRemote)
            {
                return false;
            }
            string s = uri.Scheme;
            return "http".Equals(s) || "https".Equals(s) || "ftp".Equals(s);
        }

        private readonly Uri _baseUrl;
        private readonly Uri _objectsUrl;

        public TransportHttp(Repository local, URIish uri)
            : base(local, uri)
        {
            try
            {
                string uriString = uri.ToString();
                if (!uriString.EndsWith("/"))
                {
                    uriString += "/";
                }
                _baseUrl = new Uri(uriString);
                _objectsUrl = new Uri(_baseUrl, "objects/");
            }
            catch (UriFormatException e)
            {
                throw new NotSupportedException("Invalid URL " + uri, e);
            }
        }

        public override IFetchConnection openFetch()
        {
            var c = new HttpObjectDatabase(_objectsUrl);
            var r = new WalkFetchConnection(this, c);
            r.available(c.ReadAdvertisedRefs());
            return r;
        }

        public override IPushConnection openPush()
        {
            string s = Uri.Scheme;
            throw new NotSupportedException("Push not supported over " + s + ".");
        }

        public override void close()
        {
            // No explicit connections are maintained.
        }

        #region Nested Types

        private class HttpObjectDatabase : WalkRemoteObjectDatabase
        {
            private readonly Uri _objectsUrl;

            public HttpObjectDatabase(Uri b)
            {
                _objectsUrl = b;
            }

            public override URIish getURI()
            {
                return new URIish(_objectsUrl);
            }

            public override ICollection<WalkRemoteObjectDatabase> getAlternates()
            {
                try
                {
                    return readAlternates(INFO_HTTP_ALTERNATES);
                }
                catch (FileNotFoundException)
                {
                    // Fall through.
                }

                try
                {
                    return readAlternates(INFO_ALTERNATES);
                }
                catch (FileNotFoundException)
                {
                    // Fall through.
                }

                return null;
            }

            public override WalkRemoteObjectDatabase openAlternate(string location)
            {
                return new HttpObjectDatabase(new Uri(_objectsUrl, location));
            }

            public override ICollection<string> getPackNames()
            {
                var packs = new List<string>();
                try
                {
                    using (StreamReader br = openReader(INFO_PACKS))
                    {
                        while (true)
                        {
                            string s = br.ReadLine();
                            if (string.IsNullOrEmpty(s)) break;

                            if (!s.StartsWith("P pack-") || !s.EndsWith(IndexPack.PackSuffix))
                            {
                                throw InvalidAdvertisement(s);
                            }
                            packs.Add(s.Substring(2));
                        }

                        return packs;
                    }
                }
                catch (FileNotFoundException)
                {
                    return packs;
                }
            }

            public override Stream open(string path)
            {
                Uri @base = _objectsUrl;
                var u = new Uri(@base, path);

                var c = (HttpWebRequest)WebRequest.Create(u);

                try
                {
                    var response = (HttpWebResponse)c.GetResponse();
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return response.GetResponseStream();

                        default:
                            throw new IOException(u + ": " + response.StatusDescription);
                    }
                }
                catch (WebException e)
                {
                    var response2 = ((HttpWebResponse)(e.Response));
                    if (response2.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new FileNotFoundException(u.ToString());
                    }

                    throw new IOException(u + ": " + response2.StatusDescription, e);
                }

            }

            public IDictionary<string, Ref> ReadAdvertisedRefs()
            {
                try
                {
                    using (StreamReader br = openReader(INFO_REFS))
                    {
                        return ReadAdvertisedImpl(br);
                    }
                }
                catch (IOException err)
                {
                    try
                    {
                        throw new TransportException(new Uri(_objectsUrl, INFO_REFS) + ": cannot Read available refs", err);
                    }
                    catch (UriFormatException)
                    {
                        throw new TransportException(_objectsUrl + INFO_REFS + ": cannot Read available refs", err);
                    }
                }
            }

            private static IDictionary<string, Ref> ReadAdvertisedImpl(TextReader br)
            {
                var avail = new SortedDictionary<string, Ref>();

                while (true)
                {
                    string line = br.ReadLine();
                    if (line == null) break;

                    int tab = line.IndexOf('\t');
                    if (tab < 0)
                    {
                        throw InvalidAdvertisement(line);
                    }

                    string name = line.Substring(tab + 1);
                    ObjectId id = ObjectId.FromString(line.Slice(0, tab));
                    if (name.EndsWith("^{}"))
                    {
                        name = name.Slice(0, name.Length - 3);
                        Ref prior = avail.get(name);
                        if (prior == null)
                        {
                            throw OutOfOrderAdvertisement(name);
                        }

                        if (prior.PeeledObjectId != null)
                        {
                            throw DuplicateAdvertisement(name + "^{}");
                        }

                        avail.put(name, new PeeledTag(Storage.Network, name, prior.ObjectId, id));
                    }
                    else
                    {
                        Ref prior = avail.put(name, new PeeledNonTag(Storage.Network, name, id));

                        if (prior != null)
                        {
                            throw DuplicateAdvertisement(name);
                        }
                    }
                }
                return avail;
            }

            private static PackProtocolException OutOfOrderAdvertisement(string n)
            {
                return new PackProtocolException("advertisement of " + n + "^{} came before " + n);
            }

            private static PackProtocolException InvalidAdvertisement(string n)
            {
                return new PackProtocolException("invalid advertisement of " + n);
            }

            private static PackProtocolException DuplicateAdvertisement(string n)
            {
                return new PackProtocolException("duplicate advertisements of " + n);
            }

            public override void close()
            {
                // We do not maintain persistent connections.
            }
        }

        #endregion
    }
}
