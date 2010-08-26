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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    internal class FetchProcess
    {
        /// <summary> Transport we will fetch over.  </summary>
        private readonly Transport _transport;

        /// <summary> List of things we want to fetch from the remote repository.  </summary>
        private readonly ICollection<RefSpec> _toFetch;

        /// <summary> Set of refs we will actually wind up asking to obtain.  </summary>
        private readonly IDictionary<ObjectId, Ref> _askFor = new Dictionary<ObjectId, Ref>();

        /// <summary> Objects we know we have locally.  </summary>
        private readonly HashSet<ObjectId> _have = new HashSet<ObjectId>();

        /// <summary> Updates to local tracking branches (if any).  </summary>
        private readonly List<TrackingRefUpdate> _localUpdates = new List<TrackingRefUpdate>();

        /// <summary> Records to be recorded into FETCH_HEAD.  </summary>
        private readonly List<FetchHeadRecord> _fetchHeadUpdates = new List<FetchHeadRecord>();

        private readonly List<PackLock> _packLocks = new List<PackLock>();

        private IFetchConnection _connection;

        internal FetchProcess(Transport t, ICollection<RefSpec> f)
        {
            _transport = t;
            _toFetch = f;
        }

        internal virtual void execute(ProgressMonitor monitor, FetchResult result)
        {
            _askFor.Clear();
            _localUpdates.Clear();
            _fetchHeadUpdates.Clear();
            _packLocks.Clear();

            try
            {
                executeImp(monitor, result);
            }
            finally
            {
                foreach (PackLock @lock in _packLocks)
                {
                    @lock.Unlock();
                }
            }
        }

        private void executeImp(ProgressMonitor monitor, FetchResult result)
        {
            _connection = _transport.openFetch();
            try
            {
                result.SetAdvertisedRefs(_transport.Uri, _connection.RefsMap);
                HashSet<Ref> matched = new HashSet<Ref>();
                foreach (RefSpec spec in _toFetch)
                {
                    if (spec.Source == null)
                        throw new TransportException("Source ref not specified for refspec: " + spec);

                    if (spec.Wildcard)
                    {
                        expandWildcard(spec, matched);
                    }
                    else
                    {
                        expandSingle(spec, matched);
                    }
                }

                ICollection<Ref> additionalTags = new Collection<Ref>();

                TagOpt tagopt = _transport.TagOpt;
                if (tagopt == TagOpt.AUTO_FOLLOW)
                {
                    additionalTags = expandAutoFollowTags();
                }
                else if (tagopt == TagOpt.FETCH_TAGS)
                {
                    expandFetchTags();
                }

                bool includedTags;
                if (_askFor.Count != 0 && !askForIsComplete())
                {
                    fetchObjects(monitor);
                    includedTags = _connection.DidFetchIncludeTags;

                    // Connection was used for object transfer. If we
                    // do another fetch we must open a new connection.
                    //
                    closeConnection();
                }
                else
                {
                    includedTags = false;
                }

                if (tagopt == TagOpt.AUTO_FOLLOW && additionalTags.Count != 0)
                {
                    // There are more tags that we want to follow, but
                    // not all were asked for on the initial request.
                    foreach (ObjectId key in _askFor.Keys)
                    {
                        _have.Add(key);
                    }

                    _askFor.Clear();
                    foreach (Ref r in additionalTags)
                    {
                        ObjectId id = r.PeeledObjectId;
                        if (id == null || _transport.Local.HasObject(id))
                        {
                            wantTag(r);
                        }
                    }

                    if (_askFor.Count != 0 && (!includedTags || !askForIsComplete()))
                    {
                        reopenConnection();
                        if (_askFor.Count != 0)
                        {
                            fetchObjects(monitor);
                        }
                    }
                }
            }
            finally
            {
                closeConnection();
            }

            using (RevWalk.RevWalk walk = new RevWalk.RevWalk(_transport.Local))
            {
                if (_transport.RemoveDeletedRefs)
                {
                    deleteStaleTrackingRefs(result, walk);
                }

                foreach (TrackingRefUpdate u in _localUpdates)
                {
                    try
                    {
                        u.Update(walk);
                        result.Add(u);
                    }
                    catch (IOException err)
                    {
                        throw new TransportException("Failure updating tracking ref " + u.LocalName + ": " + err.Message, err);
                    }
                }
            }

            if (_fetchHeadUpdates.Count != 0)
            {
                try
                {
                    updateFETCH_HEAD(result);
                }
                catch (IOException err)
                {
                    throw new TransportException("Failure updating FETCH_HEAD: " + err.Message, err);
                }
            }
        }

        private void fetchObjects(ProgressMonitor monitor)
        {
            try
            {
                _connection.SetPackLockMessage("jgit fetch " + _transport.Uri);
                _connection.Fetch(monitor, _askFor.Values, _have.ToList());
            }
            finally
            {
                _packLocks.AddRange(_connection.PackLocks);
            }
            if (_transport.CheckFetchedObjects && !_connection.DidFetchTestConnectivity && !askForIsComplete())
                throw new TransportException(_transport.Uri, "peer did not supply a complete object graph");
        }

        private void closeConnection()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }
        }

        private void reopenConnection()
        {
            if (_connection != null)
            {
                return;
            }

            _connection = _transport.openFetch();

            // Since we opened a new connection we cannot be certain
            // that the system we connected to has the same exact set
            // of objects available (think round-robin DNS and mirrors
            // that aren't updated at the same time).
            //
            // We rebuild our askFor list using only the refs that the
            // new connection has offered to us.
            //
            IDictionary<ObjectId, Ref> avail = new Dictionary<ObjectId, Ref>();
            foreach (Ref r in _connection.Refs)
            {
                avail.put(r.getObjectId(), r);
            }

            ICollection<Ref> wants = new List<Ref>(_askFor.Values);
            _askFor.Clear();
            foreach (Ref want in wants)
            {
                Ref newRef = avail.get(want.ObjectId);
                if (newRef != null)
                {
                    _askFor.put(newRef.ObjectId, newRef);
                }
                else
                {
                    removeFetchHeadRecord(want.ObjectId);
                    removeTrackingRefUpdate(want.ObjectId);
                }
            }
        }

        private void removeTrackingRefUpdate(ObjectId want)
        {
            _localUpdates.RemoveAll(x => x.NewObjectId.Equals(want));
        }

        private void removeFetchHeadRecord(ObjectId want)
        {
            _fetchHeadUpdates.RemoveAll(x => x.NewValue.Equals(want));
        }

        private void updateFETCH_HEAD(FetchResult result)
        {
            using (LockFile @lock = new LockFile(PathUtil.CombineFilePath(_transport.Local.Directory, "FETCH_HEAD")))
            {
                if (@lock.Lock())
                {
                    using (StreamWriter w = new StreamWriter(@lock.GetOutputStream()))
                    {
                        foreach (FetchHeadRecord h in _fetchHeadUpdates)
                        {
                            h.Write(w);
                            result.Add(h);
                        }
                    }

                    @lock.Commit();
                }
            }
        }

        private bool askForIsComplete()
        {
            try
            {
                using (ObjectWalk ow = new ObjectWalk(_transport.Local))
                {
                    foreach (ObjectId want in _askFor.Keys)
                    {
                        ow.markStart(ow.parseAny(want));
                    }
                    foreach (Ref @ref in _transport.Local.getAllRefs().Values)
                    {
                        ow.markUninteresting(ow.parseAny(@ref.ObjectId));
                    }
                    ow.checkConnectivity();
                    return true;
                }
            }
            catch (MissingObjectException)
            {
                return false;
            }
            catch (IOException e)
            {
                throw new TransportException("Unable to check connectivity.", e);
            }
        }

        private void expandWildcard(RefSpec spec, HashSet<Ref> matched)
        {
            foreach (Ref src in _connection.Refs)
            {
                if (spec.MatchSource(src) && matched.Add(src))
                    want(src, spec.ExpandFromSource((src)));
            }
        }

        private void expandSingle(RefSpec spec, HashSet<Ref> matched)
        {
            Ref src = _connection.GetRef(spec.Source);
            if (src == null)
            {
                throw new TransportException("Remote does not have " + spec.Source + " available for fetch.");
            }
            if (matched.Add(src))
            {
                want(src, spec);
            }
        }

        private ICollection<Ref> expandAutoFollowTags()
        {
            ICollection<Ref> additionalTags = new List<Ref>();
            IDictionary<string, Ref> haveRefs = _transport.Local.getAllRefs();
            foreach (Ref r in _connection.Refs)
            {
                if (!isTag(r))
                {
                    continue;
                }

                if (r.PeeledObjectId == null)
                {
                    additionalTags.Add(r);
                    continue;
                }

                Ref local = haveRefs.get(r.Name);
                if (local != null)
                {
                    if (!r.ObjectId.Equals(local.ObjectId))
                    {
                        wantTag(r);
                    }
                }
                else if (_askFor.ContainsKey(r.PeeledObjectId) || _transport.Local.HasObject(r.PeeledObjectId))
                    wantTag(r);
                else
                    additionalTags.Add(r);
            }
            return additionalTags;
        }

        private void expandFetchTags()
        {
            IDictionary<string, Ref> haveRefs = _transport.Local.getAllRefs();
            foreach (Ref r in _connection.Refs)
            {
                if (!isTag(r))
                    continue;
                Ref local = haveRefs.get(r.Name);
                if (local == null || !r.ObjectId.Equals(local.ObjectId))
                    wantTag(r);
            }
        }

        private void wantTag(Ref r)
        {
            want(r, new RefSpec(r.Name, r.Name));
        }

        private void want(Ref src, RefSpec spec)
        {
            ObjectId newId = src.ObjectId;
            if (spec.Destination != null)
            {
                try
                {
                    TrackingRefUpdate tru = createUpdate(spec, newId);
                    if (newId.Equals(tru.OldObjectId))
                    {
                        return;
                    }
                    _localUpdates.Add(tru);
                }
                catch (System.IO.IOException err)
                {
                    // Bad symbolic ref? That is the most likely cause.
                    throw new TransportException("Cannot resolve" + " local tracking ref " + spec.Destination + " for updating.", err);
                }
            }

            _askFor.put(newId, src);

            FetchHeadRecord fhr = new FetchHeadRecord(newId, spec.Destination != null, src.Name, _transport.Uri);
            _fetchHeadUpdates.Add(fhr);
        }

        private TrackingRefUpdate createUpdate(RefSpec spec, ObjectId newId)
        {
            return new TrackingRefUpdate(_transport.Local, spec, newId, "fetch");
        }

        private void deleteStaleTrackingRefs(FetchResult result, RevWalk.RevWalk walk)
        {
            Repository db = _transport.Local;
            foreach (Ref @ref in db.getAllRefs().Values)
            {
                string refname = @ref.Name;
                foreach (RefSpec spec in _toFetch)
                {
                    if (spec.MatchDestination(refname))
                    {
                        RefSpec s = spec.ExpandFromDestination(refname);
                        if (result.GetAdvertisedRef(s.Source) == null)
                        {
                            deleteTrackingRef(result, db, walk, s, @ref);
                        }
                    }
                }
            }
        }

        private void deleteTrackingRef(FetchResult result, Repository db, RevWalk.RevWalk walk, RefSpec spec, Ref localRef)
        {
            string name = localRef.Name;
            try
            {
                TrackingRefUpdate u = new TrackingRefUpdate(db, name, spec.Source, true, ObjectId.ZeroId, "deleted");
                result.Add(u);
                if (_transport.DryRun)
                {
                    return;
                }

                u.Delete(walk);

                switch (u.Result)
                {
                    case RefUpdate.RefUpdateResult.NEW:
                    case RefUpdate.RefUpdateResult.NO_CHANGE:
                    case RefUpdate.RefUpdateResult.FAST_FORWARD:
                    case RefUpdate.RefUpdateResult.FORCED:
                        break;

                    default:
                        throw new TransportException(_transport.Uri, "Cannot delete stale tracking ref " + name + ": " + Enum.GetName(typeof(RefUpdate.RefUpdateResult), u.Result));
                }
            }
            catch (IOException e)
            {
                throw new TransportException(_transport.Uri, "Cannot delete stale tracking ref " + name, e);
            }
        }

        private static bool isTag(Ref r)
        {
            return isTag(r.Name);
        }

        private static bool isTag(string name)
        {
            return name.StartsWith(Constants.R_TAGS);
        }
    }
}