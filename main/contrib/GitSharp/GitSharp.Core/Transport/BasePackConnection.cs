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
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Base helper class for pack-based operations implementations. Provides partial
    /// implementation of pack-protocol - refs advertising and capabilities support,
    /// and some other helper methods.
    /// </summary>
    public abstract class BasePackConnection : BaseConnection
    {
        /// <summary>
        /// The repository this transport fetches into, or pushes out of.
        /// </summary>
        protected readonly Repository local;

        /// <summary>
        /// Remote repository location.
        /// </summary>
        protected readonly URIish uri;

        /// <summary>
        /// A transport connected to <see cref="uri"/>.
        /// </summary>
        protected readonly Transport transport;

        /// <summary>
        /// Buffered output stream sending to the remote.
        /// </summary>
        protected Stream outStream;

        /// <summary>
        /// Buffered input stream reading from the remote.
        /// </summary>
        protected Stream inStream;

        /// <summary>
        /// Packet line decoder around <see cref="inStream"/>.
        /// </summary>
        protected PacketLineIn pckIn;

        /// <summary>
        /// Packet line encoder around <see cref="outStream"/>.
        /// </summary>
        protected PacketLineOut pckOut;

        /// <summary>
        /// Send <see cref="PacketLineOut.End"/> before closing <see cref="outStream"/>?
        /// </summary>
        protected bool outNeedsEnd;

        /// <summary>
        /// Capability tokens advertised by the remote side.
        /// </summary>
        private readonly List<string> remoteCapabilies = new List<string>();

        /// <summary>
        /// Extra objects the remote has, but which aren't offered as refs.
        /// </summary>
        protected readonly List<ObjectId> additionalHaves = new List<ObjectId>();

        protected BasePackConnection(IPackTransport packTransport)
        {
            transport = (Transport)packTransport;
            local = transport.Local;
            uri = transport.Uri;
        }

        protected void init(Stream myStream)
        {
            init(myStream, myStream);
        }

        protected void init(Stream instream, Stream outstream)
        {
            inStream = instream is BufferedStream ? instream : new BufferedStream(instream, IndexPack.BUFFER_SIZE);
            outStream = outstream is BufferedStream ? outstream : new BufferedStream(outstream, IndexPack.BUFFER_SIZE);

            pckIn = new PacketLineIn(inStream);
            pckOut = new PacketLineOut(outStream);

            outNeedsEnd = true;
        }

        protected void readAdvertisedRefs()
        {
            try
            {
                readAdvertisedRefsImpl();
            }
            catch (TransportException)
            {
                Dispose();
                throw;
            }
            catch (IOException err)
            {
                Dispose();
                throw new TransportException(err.Message, err);
            }
            catch (Exception err)
            {
                Dispose();
                throw new TransportException(err.Message, err);
            }
        }

        private void readAdvertisedRefsImpl()
        {
            var avail = new Dictionary<string, Ref>();
            while (true)
            {
                string line;

                try
                {
                    line = pckIn.ReadString();
                }
                catch (EndOfStreamException)
                {
                    if (avail.Count == 0)
                    {
                        throw noRepository();
                    }

                    throw;
                }

                if (line == PacketLineIn.END)
                    break;

                if (avail.Count == 0)
                {
                    int nul = line.IndexOf('\0');
                    if (nul >= 0)
                    {
                        // The first line (if any) may contain "hidden"
                        // capability values after a NUL byte.
                        foreach (string c in line.Substring(nul + 1).Split(' '))
                            remoteCapabilies.Add(c);
                        line = line.Slice(0, nul);
                    }
                }

                string name = line.Slice(41, line.Length);
                if (avail.Count == 0 && name.Equals("capabilities^{}"))
                {
                    // special line from git-receive-pack to show
                    // capabilities when there are no refs to advertise
                    continue;
                }

                ObjectId id = ObjectId.FromString(line.Slice(0, 40));
                if (name.Equals(".have"))
                    additionalHaves.Add(id);
                else if (name.EndsWith("^{}"))
                {
                    name = name.Slice(0, name.Length - 3);
                    Ref prior = avail.get(name);
                    if (prior == null)
                        throw new PackProtocolException(uri, "advertisement of " + name + "^{} came before " + name);

                    if (prior.PeeledObjectId != null)
                        throw duplicateAdvertisement(name + "^{}");

                    avail.put(name, new PeeledTag(Storage.Network, name, prior.ObjectId, id));
                }
                else
                {
                    Ref prior = avail.put(name, new PeeledNonTag(Storage.Network, name, id));
                    if (prior != null)
                        throw duplicateAdvertisement(name);
                    
                }
            }
            available(avail);
        }

        /// <summary>
        /// Create an exception to indicate problems finding a remote repository. The
        /// caller is expected to throw the returned exception.
        /// <para/>
        /// Subclasses may override this method to provide better diagnostics.
        /// </summary>
        /// <returns>
        /// a TransportException saying a repository cannot be found and
        /// possibly why.
        /// </returns>
        protected virtual TransportException noRepository()
        {
            return new NoRemoteRepositoryException(uri, "not found.");
        }

        protected bool isCapableOf(string option)
        {
            return remoteCapabilies.Contains(option);
        }

        protected bool wantCapability(StringBuilder b, string option)
        {
            if (!isCapableOf(option))
                return false;
            b.Append(' ');
            b.Append(option);
            return true;
        }

        private PackProtocolException duplicateAdvertisement(string name)
        {
            return new PackProtocolException(uri, "duplicate advertisements of " + name);
        }

        public override void Close()
        {
            if (outStream != null)
            {
                try
                {
                    if (outNeedsEnd)
                        pckOut.End();
                    outStream.Close();
                }
                catch (IOException)
                {
                    // Ignore any close errors.
                }
                finally
                {
                    outStream = null;
                    pckOut = null;
                }
            } 
            
            if (inStream != null)
            {
                try
                {
                    inStream.Close();
                }
                catch (IOException)
                {
                    // Ignore any close errors.
                }
                finally
                {
                    inStream = null;
                    pckIn = null;
                }
            }

#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~BasePackConnection()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed: " + this.uri);
        }
#endif
    }

}