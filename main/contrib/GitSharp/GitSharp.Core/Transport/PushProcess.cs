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
using System.Collections.Generic;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Class performing push operation on remote repository.
    /// </summary>
    /// <seealso cref="Transport.push(ProgressMonitor, ICollection{RemoteRefUpdate})"/>
    public class PushProcess : IDisposable
    {
        /// <summary> Task name for <seealso cref="ProgressMonitor"/> used during opening connection.  </summary>
        internal const string PROGRESS_OPENING_CONNECTION = "Opening connection";

        /// <summary> Transport used to perform this operation.  </summary>
        private readonly Transport _transport;

        /// <summary> Push operation connection created to perform this operation  </summary>
        private IPushConnection _connection;

        /// <summary> Refs to update on remote side.  </summary>
        private readonly IDictionary<string, RemoteRefUpdate> _toPush;

        /// <summary> Revision walker for checking some updates properties.  </summary>
        private readonly RevWalk.RevWalk _walker;

        /// <summary>
        /// Create process for specified transport and refs updates specification.
        /// </summary>
        /// <param name="transport">
        /// transport between remote and local repository, used to Create
        /// connection. </param>
        /// <param name="toPush">
        /// specification of refs updates (and local tracking branches).
        /// </param>
        /// <exception cref="TransportException"> </exception>
        public PushProcess(Transport transport, IEnumerable<RemoteRefUpdate> toPush)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");
            if (toPush == null)
                throw new ArgumentNullException("toPush");

            _walker = new RevWalk.RevWalk(transport.Local);
            _transport = transport;
            _toPush = new Dictionary<string, RemoteRefUpdate>();
            foreach (RemoteRefUpdate rru in toPush)
            {
                if (_toPush.put(rru.RemoteName, rru) != null)
                {
                    throw new TransportException("Duplicate remote ref update is illegal. Affected remote name: " + rru.RemoteName);
                }
            }
        }

        ///	<summary>
        /// Perform push operation between local and remote repository - set remote
        /// refs appropriately, send needed objects and update local tracking refs.
        /// <para />
        /// When <seealso cref="Transport.DryRun"/> is true, result of this operation is
        /// just estimation of real operation result, no real action is performed.
        /// </summary>
        /// <param name="monitor">
        /// Progress monitor used for feedback about operation.
        /// </param>
        /// <returns> result of push operation with complete status description. </returns>
        /// <exception cref="NotSupportedException">
        /// When push operation is not supported by provided transport.
        /// </exception>
        /// <exception cref="TransportException">
        /// When some error occurred during operation, like I/O, protocol
        /// error, or local database consistency error.
        /// </exception>
        public PushResult execute(ProgressMonitor monitor)
        {
            if (monitor == null)
                throw new ArgumentNullException("monitor");

            monitor.BeginTask(PROGRESS_OPENING_CONNECTION, ProgressMonitor.UNKNOWN);
            _connection = _transport.openPush();

            try
            {
                monitor.EndTask();

                IDictionary<string, RemoteRefUpdate> preprocessed = PrepareRemoteUpdates();
                if (_transport.DryRun)
                {
                    ModifyUpdatesForDryRun();
                }
                else if (preprocessed.Count != 0)
                {
                    _connection.Push(monitor, preprocessed);
                }
            }
            finally
            {
                _connection.Close();
            }

            if (!_transport.DryRun)
            {
                UpdateTrackingRefs();
            }

            return PrepareOperationResult();
        }

        private IDictionary<string, RemoteRefUpdate> PrepareRemoteUpdates()
        {
            IDictionary<string, RemoteRefUpdate> result = new Dictionary<string, RemoteRefUpdate>();
            foreach (RemoteRefUpdate rru in _toPush.Values)
            {
                Ref advertisedRef = _connection.GetRef(rru.RemoteName);
                ObjectId advertisedOld = (advertisedRef == null ? ObjectId.ZeroId : advertisedRef.ObjectId);

                if (rru.NewObjectId.Equals(advertisedOld))
                {
                    if (rru.IsDelete)
                    {
                        // ref does exist neither locally nor remotely
                        rru.Status = RemoteRefUpdate.UpdateStatus.NON_EXISTING;
                    }
                    else
                    {
                        // same object - nothing to do
                        rru.Status = RemoteRefUpdate.UpdateStatus.UP_TO_DATE;
                    }
                    continue;
                }

                // caller has explicitly specified expected old object id, while it
                // has been changed in the mean time - reject
                if (rru.IsExpectingOldObjectId && !rru.ExpectedOldObjectId.Equals(advertisedOld))
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_REMOTE_CHANGED;
                    continue;
                }

                // Create ref (hasn't existed on remote side) and delete ref
                // are always fast-forward commands, feasible at this level
                if (advertisedOld.Equals(ObjectId.ZeroId) || rru.IsDelete)
                {
                    rru.FastForward = true;
                    result.put(rru.RemoteName, rru);
                    continue;
                }

                // check for fast-forward:
                // - both old and new ref must point to commits, AND
                // - both of them must be known for us, exist in repository, AND
                // - old commit must be ancestor of new commit
                bool fastForward = true;
                try
                {
                    RevCommit oldRev = (_walker.parseAny(advertisedOld) as RevCommit);
                    RevCommit newRev = (_walker.parseAny(rru.NewObjectId) as RevCommit);
                    if (oldRev == null || newRev == null || !_walker.isMergedInto(oldRev, newRev))
                        fastForward = false;
                }
                catch (MissingObjectException)
                {
                    fastForward = false;
                }
                catch (Exception x)
                {
                    throw new TransportException(_transport.Uri, "reading objects from local repository failed: " + x.Message, x);
                }
                rru.FastForward = fastForward;
                if (!fastForward && !rru.ForceUpdate)
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.REJECTED_NONFASTFORWARD;
                }
                else
                {
                    result.put(rru.RemoteName, rru);
                }
            }
            return result;
        }

        private void ModifyUpdatesForDryRun()
        {
            foreach (RemoteRefUpdate rru in _toPush.Values)
            {
                if (rru.Status == RemoteRefUpdate.UpdateStatus.NOT_ATTEMPTED)
                {
                    rru.Status = RemoteRefUpdate.UpdateStatus.OK;
                }
            }
        }

        private void UpdateTrackingRefs()
        {
            foreach (RemoteRefUpdate rru in _toPush.Values)
            {
                RemoteRefUpdate.UpdateStatus status = rru.Status;
                if (rru.HasTrackingRefUpdate && (status == RemoteRefUpdate.UpdateStatus.UP_TO_DATE || status == RemoteRefUpdate.UpdateStatus.OK))
                {
                    // update local tracking branch only when there is a chance that
                    // it has changed; this is possible for:
                    // -updated (OK) status,
                    // -up to date (UP_TO_DATE) status
                    try
                    {
                        rru.updateTrackingRef(_walker);
                    }
                    catch (System.IO.IOException)
                    {
                        // ignore as RefUpdate has stored I/O error status
                    }
                }
            }
        }

        private PushResult PrepareOperationResult()
        {
            var result = new PushResult();
            result.SetAdvertisedRefs(_transport.Uri, _connection.RefsMap);
            result.SetRemoteUpdates(_toPush);

            foreach (RemoteRefUpdate rru in _toPush.Values)
            {
                TrackingRefUpdate tru = rru.TrackingRefUpdate;
                if (tru != null)
                {
                    result.Add(tru);
                }
            }
            return result;
        }

        public void Dispose()
        {
            _walker.Dispose();
            _transport.Dispose();
        }

    }
}