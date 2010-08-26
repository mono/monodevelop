/*
 * Copyright (C) 2008, Google Inc.
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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Implements the server side of a fetch connection, transmitting objects.
    /// </summary>
    public class UploadPack : IDisposable
    {
        private const string OptionIncludeTag = BasePackFetchConnection.OPTION_INCLUDE_TAG;
        private const string OptionMultiAck = BasePackFetchConnection.OPTION_MULTI_ACK;
        private const string OPTION_MULTI_ACK_DETAILED = BasePackFetchConnection.OPTION_MULTI_ACK_DETAILED;

        private const string OptionThinPack = BasePackFetchConnection.OPTION_THIN_PACK;
        private const string OptionSideBand = BasePackFetchConnection.OPTION_SIDE_BAND;
        private const string OptionSideBand64K = BasePackFetchConnection.OPTION_SIDE_BAND_64K;
        private const string OptionOfsDelta = BasePackFetchConnection.OPTION_OFS_DELTA;
        private const string OptionNoProgress = BasePackFetchConnection.OPTION_NO_PROGRESS;

        /// <summary>
        /// Database we read the objects from.
        /// </summary>
        private readonly Repository _db;

        /// <summary>
        /// Revision traversal support over <see cref="_db"/>.
        /// </summary>
        private readonly RevWalk.RevWalk _walk;

        /// <summary>
        /// Timeout in seconds to wait for client interaction.
        /// </summary>
        private int _timeout;

        /// <summary>
        /// Is the client connection a bi-directional socket or pipe?
        /// <para/>
        /// If true, this class assumes it can perform multiple read and write cycles
        /// with the client over the input and output streams. This matches the
        /// functionality available with a standard TCP/IP connection, or a local
        /// operating system or in-memory pipe.
        /// <para/>
        /// If false, this class runs in a read everything then output results mode,
        /// making it suitable for single round-trip systems RPCs such as HTTP.
        /// </summary>
        private bool biDirectionalPipe = true;

        private Stream _rawIn;
        private Stream _rawOut;

        private PacketLineIn _pckIn;
        private PacketLineOut _pckOut;

        /// <summary>
        /// The refs we advertised as existing at the start of the connection.
        /// </summary>
        private IDictionary<string, Ref> _refs;

        /// <summary>
        /// Filter used while advertising the refs to the client.
        /// </summary>
        private RefFilter _refFilter;

        /// <summary>
        /// Capabilities requested by the client.
        /// </summary>
        private readonly List<string> _options = new List<string>();

        /// <summary>
        /// Objects the client wants to obtain.
        /// </summary>
        private readonly IList<RevObject> _wantAll = new List<RevObject>();

        /// <summary>
        /// Objects the client wants to obtain.
        /// </summary>
        private readonly List<RevCommit> _wantCommits = new List<RevCommit>();

        /// <summary>
        /// Objects on both sides, these don't have to be sent.
        /// </summary>
        private readonly IList<RevObject> _commonBase = new List<RevObject>();

        /// <summary>
        /// null if <see cref="_commonBase"/> should be examined again.
        /// </summary>
        private bool? okToGiveUp;

        /// <summary>
        /// Marked on objects we sent in our advertisement list.
        /// </summary>
        private readonly RevFlag ADVERTISED;

        /// <summary>
        /// Marked on objects the client has asked us to give them.
        /// </summary>
        private readonly RevFlag WANT;

        /// <summary>
        /// Marked on objects both we and the client have.
        /// </summary>
        private readonly RevFlag PEER_HAS;

        /// <summary>
        /// Marked on objects in <see cref="_commonBase"/>.
        /// </summary>
        private readonly RevFlag COMMON;
        private readonly RevFlagSet SAVE;

        private BasePackFetchConnection.MultiAck _multiAck = BasePackFetchConnection.MultiAck.OFF;

        ///	<summary>
        /// Create a new pack upload for an open repository.
        /// </summary>
        /// <param name="copyFrom">the source repository.</param>
        public UploadPack(Repository copyFrom)
        {
            _db = copyFrom;
            _walk = new RevWalk.RevWalk(_db);
            _walk.setRetainBody(false);

            ADVERTISED = _walk.newFlag("ADVERTISED");
            WANT = _walk.newFlag("WANT");
            PEER_HAS = _walk.newFlag("PEER_HAS");
            COMMON = _walk.newFlag("COMMON");
            _walk.carry(PEER_HAS);

            SAVE = new RevFlagSet { ADVERTISED, WANT, PEER_HAS };
            _refFilter = RefFilterContants.DEFAULT;
        }

        /// <summary>
        /// the repository this upload is reading from.
        /// </summary>
        public Repository Repository
        {
            get { return _db; }
        }

        /// <summary>
        /// the RevWalk instance used by this connection.
        /// </summary>
        public RevWalk.RevWalk RevWalk
        {
            get { return _walk; }
        }

        /// <summary>
        /// number of seconds to wait (with no data transfer occurring)
        /// before aborting an IO read or write operation with the
        /// connected client.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <returns>
        /// true if this class expects a bi-directional pipe opened between
        /// the client and itself. The default is true.
        /// </returns>
        public bool isBiDirectionalPipe()
        {
            return biDirectionalPipe;
        }

        /// <summary>
        /// if true, this class will assume the socket is a fully
        /// bidirectional pipe between the two peers and takes advantage
        /// of that by first transmitting the known refs, then waiting to
        /// read commands. If false, this class assumes it must read the
        /// commands before writing output and does not perform the
        /// initial advertising.
        /// </summary>
        /// <param name="twoWay"></param>
        public void setBiDirectionalPipe(bool twoWay)
        {
            biDirectionalPipe = twoWay;
        }

        /// <returns>the filter used while advertising the refs to the client</returns>
        public RefFilter getRefFilter()
        {
            return _refFilter;
        }

        /// <summary>
        /// Set the filter used while advertising the refs to the client.
        /// <para/>
        /// Only refs allowed by this filter will be sent to the client. This can
        /// be used by a server to restrict the list of references the client can
        /// obtain through clone or fetch, effectively limiting the access to only
        /// certain refs.
        /// </summary>
        /// <param name="refFilter">the filter; may be null to show all refs.</param>
        public void setRefFilter(RefFilter refFilter)
        {
            _refFilter = refFilter ?? RefFilterContants.DEFAULT;
        }

        /// <summary>
        /// Execute the upload task on the socket.
        /// </summary>
        /// <param name="input">
        /// raw input to read client commands from. Caller must ensure the
        /// input is buffered, otherwise read performance may suffer.
        /// </param>
        /// <param name="output">
        /// response back to the Git network client, to write the pack
        /// data onto. Caller must ensure the output is buffered,
        /// otherwise write performance may suffer.
        /// </param>
        /// <param name="messages">
        /// secondary "notice" channel to send additional messages out
        /// through. When run over SSH this should be tied back to the
        /// standard error channel of the command execution. For most
        /// other network connections this should be null.
        /// </param>
        /// <exception cref="IOException"></exception>
        public void Upload(Stream input, Stream output, Stream messages)
        {
            _rawIn = input;
            _rawOut = output;

            if (_timeout > 0)
            {
                var i = new TimeoutStream(_rawIn);
					i.setTimeout(_timeout * 1000);
                var o = new TimeoutStream(_rawOut );
					 o.setTimeout(_timeout * 1000);
                _rawIn = i;
                _rawOut = o;
            }

            _pckIn = new PacketLineIn(_rawIn);
            _pckOut = new PacketLineOut(_rawOut);
            Service();
        }

        private void Service()
        {
            if (biDirectionalPipe)
                sendAdvertisedRefs(new RefAdvertiser.PacketLineOutRefAdvertiser(_pckOut));
            else
            {
                _refs = _refFilter.filter(_db.getAllRefs());
                foreach (Ref r in _refs.Values)
                {
                    try
                    {
                        _walk.parseAny(r.ObjectId).add(ADVERTISED);
                    }
                    catch (IOException)
                    {
                        // Skip missing/corrupt objects
                    }
                }
            }

            RecvWants();
            if (_wantAll.Count == 0) return;
            if (_options.Contains(OPTION_MULTI_ACK_DETAILED))
                _multiAck = BasePackFetchConnection.MultiAck.DETAILED;
            else if (_options.Contains(OptionMultiAck))
                _multiAck = BasePackFetchConnection.MultiAck.CONTINUE;
            else
                _multiAck = BasePackFetchConnection.MultiAck.OFF;

            if (Negotiate())
                SendPack();
        }

        /// <summary>
        /// Generate an advertisement of available refs and capabilities.
        /// </summary>
        /// <param name="adv">the advertisement formatter.</param>
        public void sendAdvertisedRefs(RefAdvertiser adv)
        {
            adv.init(_walk, ADVERTISED);
            adv.advertiseCapability(OptionIncludeTag);
            adv.advertiseCapability(OPTION_MULTI_ACK_DETAILED);
            adv.advertiseCapability(OptionMultiAck);
            adv.advertiseCapability(OptionOfsDelta);
            adv.advertiseCapability(OptionSideBand);
            adv.advertiseCapability(OptionSideBand64K);
            adv.advertiseCapability(OptionThinPack);
            adv.advertiseCapability(OptionNoProgress);
            adv.setDerefTags(true);
            _refs = _refFilter.filter(_db.getAllRefs());
            adv.send(_refs);
            adv.end();
        }

        private void RecvWants()
        {
            bool isFirst = true;
            for (; ; isFirst = false)
            {
                string line;
                try
                {
                    line = _pckIn.ReadString();
                }
                catch (EndOfStreamException)
                {
                    if (isFirst) break;
                    throw;
                }

                if (line == PacketLineIn.END) break;
                if (!line.StartsWith("want ") || line.Length < 45)
                {
                    throw new PackProtocolException("expected want; got " + line);
                }

                if (isFirst && line.Length > 45)
                {
                    string opt = line.Substring(45);
                    if (opt.StartsWith(" "))
                        opt = opt.Substring(1);
                    foreach (string c in opt.Split(' '))
                        _options.Add(c);
                    line = line.Slice(0, 45);
                }

                ObjectId id = ObjectId.FromString(line.Substring(5));
                RevObject o;
                try
                {
                    o = _walk.parseAny(id);
                }
                catch (IOException e)
                {
                    throw new PackProtocolException(id.Name + " not valid", e);
                }
                if (!o.has(ADVERTISED))
                {
                    throw new PackProtocolException(id.Name + " not valid");
                }

                Want(o);
            }
        }

        private void Want(RevObject o)
        {
            if (o.has(WANT)) return;

            o.add(WANT);
            _wantAll.Add(o);

            if (o is RevCommit)
                _wantCommits.Add((RevCommit)o);

            else if (o is RevTag)
            {
                do
                {
                    o = ((RevTag)o).getObject();
                } while (o is RevTag);
                if (o is RevCommit)
                    Want(o);
            }

        }

        private bool Negotiate()
        {
            ObjectId last = ObjectId.ZeroId;

            while (true)
            {
                string line = _pckIn.ReadString();

                if (line == PacketLineIn.END)
                {
                    if (_commonBase.Count == 0 || _multiAck != BasePackFetchConnection.MultiAck.OFF)
                    {
                        _pckOut.WriteString("NAK\n");
                    }
                    _pckOut.Flush();

                    if (!biDirectionalPipe)
                        return false;
                }
                else if (line.StartsWith("have ") && line.Length == 45)
                {
                    ObjectId id = ObjectId.FromString(line.Substring(5));
                    if (MatchHave(id))
                    {
                        // Both sides have the same object; let the client know.
                        //
                        last = id;
                        switch (_multiAck)
                        {
                            case BasePackFetchConnection.MultiAck.OFF:
                                if (_commonBase.Count == 1)
                                    _pckOut.WriteString("ACK " + id.Name + "\n");
                                break;
                            case BasePackFetchConnection.MultiAck.CONTINUE:

                                _pckOut.WriteString("ACK " + id.Name + " continue\n");
                                break;
                            case BasePackFetchConnection.MultiAck.DETAILED:
                                _pckOut.WriteString("ACK " + id.Name + " common\n");
                                break;
                        }
                    }
                    else if (OkToGiveUp())
                    {
                        // They have this object; we don't.
                        //
                        switch (_multiAck)
                        {
                            case BasePackFetchConnection.MultiAck.OFF:
                                break;
                            case BasePackFetchConnection.MultiAck.CONTINUE:
                                _pckOut.WriteString("ACK " + id.Name + " continue\n");
                                break;
                            case BasePackFetchConnection.MultiAck.DETAILED:
                                _pckOut.WriteString("ACK " + id.Name + " ready\n");
                                break;
                        }
                    }
                }
                else if (line.Equals("done"))
                {
                    if (_commonBase.Count == 0)
                    {
                        _pckOut.WriteString("NAK\n");
                    }
                    else if (_multiAck != BasePackFetchConnection.MultiAck.OFF)
                    {
                        _pckOut.WriteString("ACK " + last.Name + "\n");
                    }

                    return true;
                }
                else
                {
                    throw new PackProtocolException("expected have; got " + line);
                }
            }
        }

        private bool MatchHave(AnyObjectId id)
        {
            RevObject o;
            try
            {
                o = _walk.parseAny(id);
            }
            catch (IOException)
            {
                return false;
            }

            if (!o.has(PEER_HAS))
            {
                o.add(PEER_HAS);
                RevCommit oComm = (o as RevCommit);
                if (oComm != null)
                {
                    oComm.carry(PEER_HAS);
                }
                AddCommonBase(o);
            }
            return true;
        }

        private void AddCommonBase(RevObject o)
        {
            if (o.has(COMMON)) return;
            o.add(COMMON);
            _commonBase.Add(o);
            okToGiveUp = null;
        }

        private bool OkToGiveUp()
        {
            if (okToGiveUp == null)
            {
                okToGiveUp = OkToGiveUpImp();
            }
            return okToGiveUp.Value;
        }
        private bool OkToGiveUpImp()
        {
            if (_commonBase.Count == 0) return false;

            try
            {
                _wantCommits.RemoveAll(WantSatisfied);
            }
            catch (IOException e)
            {
                throw new PackProtocolException("internal revision error", e);
            }

            return _wantCommits.Count == 0;
        }

        private bool WantSatisfied(RevCommit want)
        {
            _walk.resetRetain(SAVE);
            _walk.markStart(want);

            while (true)
            {
                RevCommit c = _walk.next();
                if (c == null) break;
                if (c.has(PEER_HAS))
                {
                    AddCommonBase(c);
                    return true;
                }
            }
            return false;
        }

        private void SendPack()
        {
            bool thin = _options.Contains(OptionThinPack);
            bool progress = !_options.Contains(OptionNoProgress);
            bool sideband = _options.Contains(OptionSideBand) || _options.Contains(OptionSideBand64K);

            ProgressMonitor pm = NullProgressMonitor.Instance;
            Stream _packOut = _rawOut;

            if (sideband)
            {
                int bufsz = SideBandOutputStream.SMALL_BUF;
                if (_options.Contains(OptionSideBand64K))
                {
                    bufsz = SideBandOutputStream.MAX_BUF;
                }

                _packOut = new SideBandOutputStream(SideBandOutputStream.CH_DATA, bufsz, _rawOut);

                if (progress)
                    pm = new SideBandProgressMonitor(new SideBandOutputStream(SideBandOutputStream.CH_PROGRESS, bufsz, _rawOut));
            }

            var pw = new PackWriter(_db, pm, NullProgressMonitor.Instance)
                        {
                            DeltaBaseAsOffset = _options.Contains(OptionOfsDelta),
                            Thin = thin
                        };

            pw.preparePack(_wantAll, _commonBase);
            if (_options.Contains(OptionIncludeTag))
            {
                foreach (Ref r in _refs.Values)
                {
                    RevObject o;
                    try
                    {
                        o = _walk.parseAny(r.ObjectId);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    RevTag t = (o as RevTag);
                    if (o.has(WANT) || (t == null)) continue;

                    if (!pw.willInclude(t) && pw.willInclude(t.getObject()))
                        pw.addObject(t);
                }
            }

            pw.writePack(_packOut);

            if (sideband)
            {
                _packOut.Flush();
                _pckOut.End();
            }
            else
            {
                _rawOut.Flush();
            }
        }

        public void Dispose()
        {
            _walk.Dispose();
            ADVERTISED.Dispose();
            WANT.Dispose();
            PEER_HAS.Dispose();
            COMMON.Dispose();
            _rawIn.Dispose();
            _rawOut.Dispose();
        }

    }
}