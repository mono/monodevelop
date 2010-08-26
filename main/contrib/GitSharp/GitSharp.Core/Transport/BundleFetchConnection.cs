/*
 * Copyright (C) 2009, Constantine Plotnikov <constantine.plotnikov@gmail.com>
 * Copyright (C) 2008-2009, Google Inc.
 * Copyright (C) 2009, Matthias Sohn <matthias.sohn@sap.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2009, Sasa Zivkov <sasa.zivkov@sap.com>
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

// [henon] Verified against jgit commit 96690904f53241e825a29be9558be319a0bbfba1

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Fetch connection for bundle based classes. It used by
    /// instances of <see cref="ITransportBundle"/>
    /// </summary>
    public class BundleFetchConnection : BaseFetchConnection
    {
        private readonly Transport _transport;
        private Stream _bin;
        private readonly IDictionary<ObjectId, string> _prereqs = new Dictionary<ObjectId, string>();
        private string _lockMessage;
        private PackLock _packLock;

        public BundleFetchConnection(Transport transportBundle, Stream src)
        {
            _transport = transportBundle;
            _bin = new BufferedStream(src, IndexPack.BUFFER_SIZE);
            try
            {
                switch (readSignature())
                {
                    case 2:
                        readBundleV2();
                        break;

                    default:
                        throw new TransportException(_transport.Uri, "not a bundle");
                }
            }
            catch (TransportException)
            {
                Close();
                throw;
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(_transport.Uri, err.Message, err);
            }
            catch (Exception err)
            {
                Close();
                throw new TransportException(_transport.Uri, err.Message, err);
            }
        }

        private int readSignature()
        {
            string rev = readLine(new byte[1024]);
            if (TransportBundleConstants.V2_BUNDLE_SIGNATURE.Equals(rev))
                return 2;
            throw new TransportException(_transport.Uri, "not a bundle");
        }

        private void readBundleV2()
        {
            byte[] hdrbuf = new byte[1024];
            var avail = new Dictionary<string, Ref>();
            for (; ; )
            {
                string line = readLine(hdrbuf);
                if (line.Length == 0)
                    break;

                if (line[0] == '-')
                {
                    ObjectId id = ObjectId.FromString(line.Slice(1, 41));
                    String shortDesc = null;
                    if (line.Length > 42)
                        shortDesc = line.Substring(42);
                    _prereqs.put(id, shortDesc);
                    continue;
                }

                string name = line.Slice(41, line.Length);
                ObjectId id2 = ObjectId.FromString(line.Slice(0, 40));
                Ref prior = avail.put(name, new Unpeeled(Storage.Network, name, id2));
                if (prior != null)
                {
                    throw duplicateAdvertisement(name);
                }
            }
            available(avail);
        }

        private PackProtocolException duplicateAdvertisement(string name)
        {
            return new PackProtocolException(_transport.Uri, "duplicate advertisement of " + name);
        }

        private string readLine(byte[] hdrbuf)
        {
            long mark = _bin.Position;
            int cnt = _bin.Read(hdrbuf, 0, hdrbuf.Length);
            int lf = 0;
            while (lf < cnt && hdrbuf[lf] != '\n')
                lf++;
            _bin.Position = mark;
            IO.skipFully(_bin, lf);
            if (lf < cnt && hdrbuf[lf] == '\n')
                IO.skipFully(_bin, 1);

            return RawParseUtils.decode(Constants.CHARSET, hdrbuf, 0, lf);
        }

        public override bool DidFetchTestConnectivity
        {
            get { return false; }
        }

        protected override void doFetch(ProgressMonitor monitor, ICollection<Ref> want, IList<ObjectId> have)
        {
            verifyPrerequisites();
            try
            {
                IndexPack ip = newIndexPack();
                ip.index(monitor);
                _packLock = ip.renameAndOpenPack(_lockMessage);
            }
            catch (IOException err)
            {
                Close();
                throw new TransportException(_transport.Uri, err.Message, err);
            }
            catch (Exception err)
            {
                Close();
                throw new TransportException(_transport.Uri, err.Message, err);
            }
        }

        public override void SetPackLockMessage(string message)
        {
            _lockMessage = message;
        }

        public override List<PackLock> PackLocks
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

        private IndexPack newIndexPack()
        {
            IndexPack ip = IndexPack.Create(_transport.Local, _bin);
            ip.setFixThin(true);
            ip.setObjectChecking(_transport.CheckFetchedObjects);
            return ip;
        }

        private void verifyPrerequisites()
        {
            if (_prereqs.isEmpty())
                return;

            using (var rw = new RevWalk.RevWalk(_transport.Local))
            {
                RevFlag PREREQ = rw.newFlag("PREREQ");
                RevFlag SEEN = rw.newFlag("SEEN");

                IDictionary<ObjectId, string> missing = new Dictionary<ObjectId, string>();
                var commits = new List<RevObject>();
                foreach (KeyValuePair<ObjectId, string> e in _prereqs)
                {
                    ObjectId p = e.Key;
                    try
                    {
                        RevCommit c = rw.parseCommit(p);
                        if (!c.has(PREREQ))
                        {
                            c.add(PREREQ);
                            commits.Add(c);
                        }
                    }
                    catch (MissingObjectException)
                    {
                        missing.put(p, e.Value);
                    }
                    catch (IOException err)
                    {
                        throw new TransportException(_transport.Uri, "Cannot Read commit " + p.Name, err);
                    }
                }

                if (!missing.isEmpty())
                    throw new MissingBundlePrerequisiteException(_transport.Uri, missing);

                foreach (Ref r in _transport.Local.getAllRefs().Values)
                {
                    try
                    {
                        rw.markStart(rw.parseCommit(r.ObjectId));
                    }
                    catch (IOException)
                    {
                        // If we cannot read the value of the ref skip it.
                    }
                }

                int remaining = commits.Count;
                try
                {
                    RevCommit c;
                    while ((c = rw.next()) != null)
                    {
                        if (c.has(PREREQ))
                        {
                            c.add(SEEN);
                            if (--remaining == 0)
                                break;
                        }
                    }
                }
                catch (IOException err)
                {
                    throw new TransportException(_transport.Uri, "Cannot Read object", err);
                }

                if (remaining > 0)
                {
                    foreach (RevObject o in commits)
                    {
                        if (!o.has(SEEN))
                            missing.put(o, _prereqs.get(o));
                    }
                    throw new MissingBundlePrerequisiteException(_transport.Uri, missing);
                }
            }
        }

        public override void Close()
        {
            if (_bin != null)
            {
                try
                {
                    _bin.Dispose();
                }
                catch (IOException)
                {
                    // Ignore close failures.
                }
                finally
                {
                    _bin = null;
                }
            }
#if DEBUG
            GC.SuppressFinalize(this); // Disarm lock-release checker
#endif
        }

#if DEBUG
        // A debug mode warning if the type has not been disposed properly
        ~BundleFetchConnection()
        {
            Console.Error.WriteLine(GetType().Name + " has not been properly disposed.");
        }
#endif
    }

}