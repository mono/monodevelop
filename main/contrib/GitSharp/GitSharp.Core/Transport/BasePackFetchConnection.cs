/*
 * Copyright (C) 2008-2010, Google Inc.
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.RevWalk.Filter;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Fetch implementation using the native Git pack transfer service.
    /// <para/>
    /// This is the canonical implementation for transferring objects from the remote
    /// repository to the local repository by talking to the 'git-upload-pack'
    /// service. Objects are packed on the remote side into a pack file and then sent
    /// down the pipe to us.
    /// <para/>
    /// This connection requires only a bi-directional pipe or socket, and thus is
    /// easily wrapped up into a local process pipe, anonymous TCP socket, or a
    /// command executed through an SSH tunnel.
    /// <para/>
    /// Concrete implementations should just call
    /// <see cref="BasePackConnection.init(System.IO.Stream,System.IO.Stream)"/> and
    /// <see cref="BasePackConnection.readAdvertisedRefs"/> methods in constructor or before any use. They
    /// should also handle resources releasing in <see cref="BasePackConnection.Close"/> method if needed.
    /// </summary>
    public class BasePackFetchConnection : BasePackConnection, IFetchConnection
    {
        /// <summary>
        /// Maximum number of 'have' lines to send before giving up.
        /// <para/>
        /// During <see cref="Negotiate"/> we send at most this many
        /// commits to the remote peer as 'have' lines without an ACK response before
        /// we give up.
        /// </summary>
        private const int MAX_HAVES = 256;

        /// <summary>
        /// Amount of data the client sends before starting to read.
        /// <para/>
        /// Any output stream given to the client must be able to buffer this many
        /// bytes before the client will stop writing and start reading from the
        /// input stream. If the output stream blocks before this many bytes are in
        /// the send queue, the system will deadlock.
        /// </summary>
        protected const int MIN_CLIENT_BUFFER = 2 * 32 * 46 + 8;
        public const string OPTION_INCLUDE_TAG = "include-tag";
        public const string OPTION_MULTI_ACK = "multi_ack";
        public const string OPTION_MULTI_ACK_DETAILED = "multi_ack_detailed";
        public const string OPTION_THIN_PACK = "thin-pack";
        public const string OPTION_SIDE_BAND = "side-band";
        public const string OPTION_SIDE_BAND_64K = "side-band-64k";
        public const string OPTION_OFS_DELTA = "ofs-delta";
        public const string OPTION_SHALLOW = "shallow";
        public const string OPTION_NO_PROGRESS = "no-progress";

        public enum MultiAck
        {
            OFF,
            CONTINUE,
            DETAILED
        }

        private readonly RevWalk.RevWalk _walk;

        /// <summary>
        /// All commits that are immediately reachable by a local ref.
        /// </summary>
        private RevCommitList<RevCommit> _reachableCommits;

        private readonly bool _allowOfsDelta;

        /// <summary>
        /// Marks an object as having all its dependencies.
        /// </summary>
        public readonly RevFlag REACHABLE;

        /// <summary>
        /// Marks a commit known to both sides of the connection.
        /// </summary>
        public readonly RevFlag COMMON;

        /// <summary>
        /// Marks a commit listed in the advertised refs.
        /// </summary>
        public readonly RevFlag ADVERTISED;

        private MultiAck _multiAck = MultiAck.OFF;
        private bool _thinPack;
        private bool _sideband;
        private bool _includeTags;
        private string _lockMessage;
        private PackLock _packLock;

        public BasePackFetchConnection(IPackTransport packTransport)
            : base(packTransport)
        {
            FetchConfig cfg = local.Config.get(FetchConfig.KEY);
            _includeTags = transport.TagOpt != TagOpt.NO_TAGS;
            _thinPack = transport.FetchThin;
            _allowOfsDelta = cfg.AllowOfsDelta;

            _walk = new RevWalk.RevWalk(local);
            _reachableCommits = new RevCommitList<RevCommit>();
            REACHABLE = _walk.newFlag("REACHABLE");
            COMMON = _walk.newFlag("COMMON");
            ADVERTISED = _walk.newFlag("ADVERTISED");

            _walk.carry(COMMON);
            _walk.carry(REACHABLE);
            _walk.carry(ADVERTISED);
        }

        private class FetchConfig
        {
            public static Config.SectionParser<FetchConfig> KEY = new FetchConfigSectionParser();

            readonly bool _allowOfsDelta;

            FetchConfig(Config c)
            {
                _allowOfsDelta = c.getBoolean("repack", "usedeltabaseoffset", true);
            }

            private class FetchConfigSectionParser : Config.SectionParser<FetchConfig>
            {
                public FetchConfig parse(Config cfg)
                {
                    return new FetchConfig(cfg);
                }
            }

            public bool AllowOfsDelta
            {
                get { return _allowOfsDelta; }
            }
        }

        public void Fetch(ProgressMonitor monitor, ICollection<Ref> want, IList<ObjectId> have)
        {
            markStartedOperation();
            doFetch(monitor, want, have);
        }

        public bool DidFetchIncludeTags
        {
            get { return false; }
        }

        public bool DidFetchTestConnectivity
        {
            get { return false; }
        }

        public void SetPackLockMessage(string message)
        {
            _lockMessage = message;
        }

        public List<PackLock> PackLocks
        {
            get
            {
                if (_packLock != null)
                {
                    return new List<PackLock> { _packLock };
                }

                return new List<PackLock>();
            }
        }

        protected void doFetch(ProgressMonitor monitor, ICollection<Ref> want, IList<ObjectId> have)
        {
            try
            {
                MarkRefsAdvertised();
                MarkReachable(have, MaxTimeWanted(want));

                if (SendWants(want))
                {
                    Negotiate(monitor);

                    _walk.Dispose();
                    _reachableCommits = null;

                    ReceivePack(monitor);
                }
            }
            catch (CancelledException)
            {
                Dispose();
                return; // Caller should test (or just know) this themselves.
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

        private int MaxTimeWanted(IEnumerable<Ref> wants)
        {
            int maxTime = 0;
            foreach (Ref r in wants)
            {
                try
                {
                    var obj = (_walk.parseAny(r.ObjectId) as RevCommit);
                    if (obj != null)
                    {
                        int cTime = obj.CommitTime;
                        if (maxTime < cTime)
                            maxTime = cTime;
                    }
                }
                catch (IOException)
                {
                    // We don't have it, but we want to fetch (thus fixing error).
                }
            }

            return maxTime;
        }

        private void MarkReachable(IEnumerable<ObjectId> have, int maxTime)
        {
            foreach (Ref r in local.getAllRefs().Values)
            {
                try
                {
                    RevCommit o = _walk.parseCommit(r.ObjectId);
                    o.add(REACHABLE);
                    _reachableCommits.add(o);
                }
                catch (IOException)
                {
                    // If we cannot read the value of the ref skip it.
                }
            }

            foreach (ObjectId id in have)
            {
                try
                {
                    RevCommit o = _walk.parseCommit(id);
                    o.add(REACHABLE);
                    _reachableCommits.add(o);
                }
                catch (IOException)
                {
                    // If we cannot read the value of the ref skip it.
                }
            }

            if (maxTime > 0)
            {
                // Mark reachable commits until we reach maxTime. These may
                // wind up later matching up against things we want and we
                // can avoid asking for something we already happen to have.
                //
                DateTime maxWhen = (maxTime * 1000L).MillisToUtcDateTime();
                _walk.sort(RevSort.COMMIT_TIME_DESC);
                _walk.markStart(_reachableCommits);
                _walk.setRevFilter(CommitTimeRevFilter.After(maxWhen));
                for (; ; )
                {
                    RevCommit c = _walk.next();
                    if (c == null)
                        break;
                    if (c.has(ADVERTISED) && !c.has(COMMON))
                    {
                        c.add(COMMON);
                        c.carry(COMMON);
                        _reachableCommits.add(c);
                    }
                }
            }
        }

        private bool SendWants(IEnumerable<Ref> want)
        {
            bool first = true;
            foreach (Ref r in want)
            {
                try
                {
                    if (_walk.parseAny(r.ObjectId).has(REACHABLE))
                    {
                        // We already have this object. Asking for it is
                        // not a very good idea.
                        //
                        continue;
                    }
                }
                catch (IOException)
                {
                    // Its OK, we don't have it, but we want to fix that
                    // by fetching the object from the other side.
                }

                var line = new StringBuilder(46);
                line.Append("want ");
                line.Append(r.ObjectId.Name);
                if (first)
                {
                    line.Append(EnableCapabilities());
                    first = false;
                }
                line.Append('\n');
                pckOut.WriteString(line.ToString());
            }
            pckOut.End();
            outNeedsEnd = false;
            return !first;
        }

        private string EnableCapabilities()
        {
            var line = new StringBuilder();
            if (_includeTags)
                _includeTags = wantCapability(line, OPTION_INCLUDE_TAG);
            if (_allowOfsDelta)
                wantCapability(line, OPTION_OFS_DELTA);

            if (wantCapability(line, OPTION_MULTI_ACK_DETAILED))
                _multiAck = MultiAck.DETAILED;
            else if (wantCapability(line, OPTION_MULTI_ACK))
                _multiAck = MultiAck.CONTINUE;
            else
                _multiAck = MultiAck.OFF;

            if (_thinPack)
                _thinPack = wantCapability(line, OPTION_THIN_PACK);
            if (wantCapability(line, OPTION_SIDE_BAND_64K))
                _sideband = true;
            else if (wantCapability(line, OPTION_SIDE_BAND))
                _sideband = true;
            return line.ToString();
        }

        private void Negotiate(ProgressMonitor monitor)
        {
            var ackId = new MutableObjectId();
            int resultsPending = 0;
            int havesSent = 0;
            int havesSinceLastContinue = 0;
            bool receivedContinue = false;
            bool receivedAck = false;

            NegotiateBegin();
            for (; ; )
            {
                RevCommit c = _walk.next();
                if (c == null)
                {
                    goto END_SEND_HAVES;
                }

                pckOut.WriteString("have " + c.getId().Name + "\n");
                havesSent++;
                havesSinceLastContinue++;

                if ((31 & havesSent) != 0)
                {
                    // We group the have lines into blocks of 32, each marked
                    // with a flush (aka end). This one is within a block so
                    // continue with another have line.
                    //
                    continue;
                }

                if (monitor.IsCancelled)
                    throw new CancelledException();

                pckOut.End();
                resultsPending++; // Each end will cause a result to come back.

                if (havesSent == 32)
                {
                    // On the first block we race ahead and try to send
                    // more of the second block while waiting for the
                    // remote to respond to our first block request.
                    // This keeps us one block ahead of the peer.
                    //
                    continue;
                }

                for (; ; )
                {
                    PacketLineIn.AckNackResult anr = pckIn.readACK(ackId);

                    switch (anr)
                    {
                        case PacketLineIn.AckNackResult.NAK:
                            // More have lines are necessary to compute the
                            // pack on the remote side. Keep doing that.

                            resultsPending--;
                            goto END_READ_RESULT;

                        case PacketLineIn.AckNackResult.ACK:
                            // The remote side is happy and knows exactly what
                            // to send us. There is no further negotiation and
                            // we can break out immediately.

                            _multiAck = MultiAck.OFF;
                            resultsPending = 0;
                            receivedAck = true;
                            goto END_SEND_HAVES;

                        case PacketLineIn.AckNackResult.ACK_CONTINUE:
                        case PacketLineIn.AckNackResult.ACK_COMMON:
                        case PacketLineIn.AckNackResult.ACK_READY:
                            // The server knows this commit (ackId). We don't
                            // need to send any further along its ancestry, but
                            // we need to continue to talk about other parts of
                            // our local history.

                            MarkCommon(_walk.parseAny(ackId));
                            receivedAck = true;
                            receivedContinue = true;
                            havesSinceLastContinue = 0;
                            break;
                    }


                    if (monitor.IsCancelled)
                        throw new CancelledException();
                }

            END_READ_RESULT:
                if (receivedContinue && havesSinceLastContinue > MAX_HAVES)
                {
                    // Our history must be really different from the remote's.
                    // We just sent a whole slew of have lines, and it did not
                    // recognize any of them. Avoid sending our entire history
                    // to them by giving up early.
                    //
                    break;
                }
            }

        END_SEND_HAVES:

            // Tell the remote side we have run out of things to talk about.
            //
            if (monitor.IsCancelled)
                throw new CancelledException();
            pckOut.WriteString("done\n");
            pckOut.Flush();

            if (!receivedAck)
            {
                // Apparently if we have never received an ACK earlier
                // there is one more result expected from the done we
                // just sent to the remote.
                //
                _multiAck = MultiAck.OFF;
                resultsPending++;
            }

            while (resultsPending > 0 || _multiAck != MultiAck.OFF)
            {
                PacketLineIn.AckNackResult anr = pckIn.readACK(ackId);
                resultsPending--;

                switch (anr)
                {
                    case PacketLineIn.AckNackResult.NAK:
                        // A NAK is a response to an end we queued earlier
                        // we eat it and look for another ACK/NAK message.
                        //
                        break;

                    case PacketLineIn.AckNackResult.ACK:
                        // A solitary ACK at this point means the remote won't
                        // speak anymore, but is going to send us a pack now.
                        //
                        goto END_READ_RESULT_2;

                    case PacketLineIn.AckNackResult.ACK_CONTINUE:
                    case PacketLineIn.AckNackResult.ACK_COMMON:
                    case PacketLineIn.AckNackResult.ACK_READY:
                        // We will expect a normal ACK to break out of the loop.
                        //
                        _multiAck = MultiAck.CONTINUE;
                        break;
                }

                if (monitor.IsCancelled)
                    throw new CancelledException();
            }

        END_READ_RESULT_2:
            ;
        }

        private class NegotiateBeginRevFilter : RevFilter
        {
            private readonly RevFlag _common;
            private readonly RevFlag _advertised;

            public NegotiateBeginRevFilter(RevFlag c, RevFlag a)
            {
                _common = c;
                _advertised = a;
            }

            public override RevFilter Clone()
            {
                return this;
            }

            public override bool include(RevWalk.RevWalk walker, RevCommit cmit)
            {
                bool remoteKnowsIsCommon = cmit.has(_common);
                if (cmit.has(_advertised))
                {
                    // Remote advertised this, and we have it, hence common.
                    // Whether or not the remote knows that fact is tested
                    // before we added the flag. If the remote doesn't know
                    // we have to still send them this object.
                    //
                    cmit.add(_common);
                }
                return !remoteKnowsIsCommon;
            }
        }

        private void NegotiateBegin()
        {
            _walk.resetRetain(REACHABLE, ADVERTISED);
            _walk.markStart(_reachableCommits);
            _walk.sort(RevSort.COMMIT_TIME_DESC);
            _walk.setRevFilter(new NegotiateBeginRevFilter(COMMON, ADVERTISED));
        }

        private void MarkRefsAdvertised()
        {
            foreach (Ref r in Refs)
            {
                MarkAdvertised(r.ObjectId);
                if (r.PeeledObjectId != null)
                    MarkAdvertised(r.PeeledObjectId);
            }
        }

        private void MarkAdvertised(AnyObjectId id)
        {
            try
            {
                _walk.parseAny(id).add(ADVERTISED);
            }
            catch (IOException)
            {
                // We probably just do not have this object locally.
            }
        }

        private void MarkCommon(RevObject obj)
        {
            obj.add(COMMON);
            var oComm = (obj as RevCommit);
            if (oComm != null)
            {
                oComm.carry(COMMON);
            }
        }

        private void ReceivePack(ProgressMonitor monitor)
        {
			  Stream input = inStream;
			  if (_sideband)
				  input = new SideBandInputStream(input, monitor);
            IndexPack ip = IndexPack.Create(local, input);
            ip.setFixThin(_thinPack);
            ip.setObjectChecking(transport.CheckFetchedObjects);
            ip.index(monitor);
            _packLock = ip.renameAndOpenPack(_lockMessage);
        }

        public override void Dispose()
        {
            _walk.Dispose();
            REACHABLE.Dispose();
            COMMON.Dispose();
            ADVERTISED.Dispose();
            _reachableCommits.Dispose();
            base.Dispose();
        }

    }
}