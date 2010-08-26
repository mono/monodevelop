/*
 * Copyright (C) 2008, Marek Zawirski <marek.zawirski@gmail.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Stefan Schake <caytchen@gmail.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Push implementation using the native Git pack transfer service.
    /// <para/>
    /// This is the canonical implementation for transferring objects to the remote
    /// repository from the local repository by talking to the 'git-receive-pack'
    /// service. Objects are packed on the local side into a pack file and then sent
    /// to the remote repository.
    /// <para/>
    /// This connection requires only a bi-directional pipe or socket, and thus is
    /// easily wrapped up into a local process pipe, anonymous TCP socket, or a
    /// command executed through an SSH tunnel.
    /// <para/>
    /// This implementation honors <see cref="Transport.get_PushThin"/> option.
    /// <para/>
    /// Concrete implementations should just call
    /// <see cref="BasePackConnection.init(System.IO.Stream,System.IO.Stream)"/> and
    /// <see cref="BasePackConnection.readAdvertisedRefs"/> methods in constructor or before any use. They
    /// should also handle resources releasing in <see cref="BasePackConnection.Close"/> method if needed.
    /// </summary>
    public abstract class BasePackPushConnection : BasePackConnection, IPushConnection
    {
        public const string CAPABILITY_REPORT_STATUS = "report-status";
        public const string CAPABILITY_DELETE_REFS = "delete-refs";
        public const string CAPABILITY_OFS_DELTA = "ofs-delta";
		  public const string CAPABILITY_SIDE_BAND_64K = "side-band-64k";

        private readonly bool _thinPack;
        private bool _capableDeleteRefs;
        private bool _capableReport;
        private bool _capableOfsDelta;
        private bool _sentCommand;
        private bool _shouldWritePack;

        /// <summary>
        /// Time in milliseconds spent transferring the pack data.
        /// </summary>
        private long packTransferTime;

        protected BasePackPushConnection(IPackTransport packTransport)
            : base(packTransport)
        {
            _thinPack = transport.PushThin;
        }

        public void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
        {
            markStartedOperation();
            doPush(monitor, refUpdates);
        }

        protected override TransportException noRepository()
        {
            // Sadly we cannot tell the "invalid URI" case from "push not allowed".
            // Opening a fetch connection can help us tell the difference, as any
            // useful repository is going to support fetch if it also would allow
            // push. So if fetch throws NoRemoteRepositoryException we know the
            // URI is wrong. Otherwise we can correctly state push isn't allowed
            // as the fetch connection opened successfully.
            //
            try
            {
                transport.openFetch().Close();
            }
            catch (NotSupportedException)
            {
                // Fall through.
            }
            catch (NoRemoteRepositoryException e)
            {
                // Fetch concluded the repository doesn't exist.
                //
                return e;
            }
            catch (TransportException)
            {
                // Fall through.
            }

            return new TransportException(uri, "push not permitted");
        }

        protected void doPush(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates)
        {
            try
            {
                WriteCommands(refUpdates.Values, monitor);
                if (_shouldWritePack)
                    writePack(refUpdates, monitor);
                if (_sentCommand && _capableReport)
                    readStatusReport(refUpdates);
            }
            catch (TransportException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new TransportException(uri, e.Message, e);
            }
            finally
            {
                Dispose();
            }
        }

        private void WriteCommands(IEnumerable<RemoteRefUpdate> refUpdates, ProgressMonitor monitor)
        {
            string capabilities = EnableCapabilities();
            foreach (RemoteRefUpdate rru in refUpdates)
            {
                if (!_capableDeleteRefs && rru.IsDelete)
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_NODELETE;
                    continue;
                }

                var sb = new StringBuilder();
                Ref advertisedRef = GetRef(rru.RemoteName);
                ObjectId oldId = (advertisedRef == null ? ObjectId.ZeroId : advertisedRef.ObjectId);
                sb.Append(oldId.Name);
                sb.Append(' ');
                sb.Append(rru.NewObjectId.Name);
                sb.Append(' ');
                sb.Append(rru.RemoteName);
                if (!_sentCommand)
                {
                    _sentCommand = true;
                    sb.Append(capabilities);
                }

                pckOut.WriteString(sb.ToString());
                rru.Status = RemoteRefUpdate.UpdateStatus.AWAITING_REPORT;
                if (!rru.IsDelete)
                    _shouldWritePack = true;
            }

            if (monitor.IsCancelled)
                throw new TransportException(uri, "push cancelled");
            pckOut.End();
            outNeedsEnd = false;
        }

        private string EnableCapabilities()
        {
            var line = new StringBuilder();
            _capableReport = wantCapability(line, CAPABILITY_REPORT_STATUS);
            _capableDeleteRefs = wantCapability(line, CAPABILITY_DELETE_REFS);
            _capableOfsDelta = wantCapability(line, CAPABILITY_OFS_DELTA);
            if (line.Length > 0)
                line[0] = '\0';
            return line.ToString();
        }

        private void writePack(IDictionary<string, RemoteRefUpdate> refUpdates, ProgressMonitor monitor)
        {
            PackWriter writer = new PackWriter(local, monitor);
            List<ObjectId> remoteObjects = new List<ObjectId>(Refs.Count);
            List<ObjectId> newObjects = new List<ObjectId>(refUpdates.Count);

            foreach (Ref r in Refs)
                remoteObjects.Add(r.ObjectId);

            remoteObjects.AddRange(additionalHaves);
            foreach (RemoteRefUpdate r in refUpdates.Values)
            {
                if (!ObjectId.ZeroId.Equals(r.NewObjectId))
                    newObjects.Add(r.NewObjectId);
            }

            writer.Thin = _thinPack;
            writer.DeltaBaseAsOffset = _capableOfsDelta;
            writer.preparePack(newObjects, remoteObjects);
            long start = SystemReader.getInstance().getCurrentTime();
            writer.writePack(outStream);
            packTransferTime = SystemReader.getInstance().getCurrentTime() - start;
        }

        private void readStatusReport(IDictionary<string, RemoteRefUpdate> refUpdates)
        {
            string unpackLine = readStringLongTimeout();
            if (!unpackLine.StartsWith("unpack "))
            {
                throw new PackProtocolException(uri, "unexpected report line: " + unpackLine);
            }
            string unpackStatus = unpackLine.Substring("unpack ".Length);
            if (!unpackStatus.Equals("ok"))
            {
                throw new TransportException(uri, "error occoured during unpacking on the remote end: " + unpackStatus);
            }

            String refLine;
            while ((refLine = pckIn.ReadString()) != PacketLineIn.END)
            {
                bool ok = false;
                int refNameEnd = -1;
                if (refLine.StartsWith("ok "))
                {
                    ok = true;
                    refNameEnd = refLine.Length;
                }
                else if (refLine.StartsWith("ng "))
                {
                    refNameEnd = refLine.IndexOf(' ', 3);
                }
                if (refNameEnd == -1)
                {
                    throw new PackProtocolException(uri + ": unexpected report line: " + refLine);
                }
                string refName = refLine.Slice(3, refNameEnd);
                string message = (ok ? null : refLine.Substring(refNameEnd + 1));
                RemoteRefUpdate rru = refUpdates.get(refName);
                if (rru == null)
                    throw new PackProtocolException(uri
                            + ": unexpected ref report: " + refName);
                if (ok)
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.OK;
                }
                else
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_OTHER_REASON;
                    rru.Message = message;
                }
            }

            foreach (RemoteRefUpdate rru in refUpdates.Values)
            {
                if (rru.Status == RemoteRefUpdate.UpdateStatus.AWAITING_REPORT)
                {
                    throw new PackProtocolException(uri + ": expected report for ref " + rru.RemoteName +
                                                    " not received");
                }
            }
        }

        private String readStringLongTimeout()
        {
            return pckIn.ReadString();
        }
    }
}