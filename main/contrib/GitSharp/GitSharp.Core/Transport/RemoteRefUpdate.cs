/*
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
 * - Neither the remoteName of the Git Development Community nor the
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

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Represent request and status of a remote ref update. Specification is
    /// provided by client, while status is handled by <see cref="PushProcess"/> class,
    /// being read-only for client.
    /// <para/>
    /// Client can create instances of this class directly, basing on user
    /// specification and advertised refs ({@link Connection} or through
    /// <see cref="Transport"/> helper methods. Apply this specification on remote
    /// repository using <see cref="Transport.push"/>
    /// method.</summary>
    public class RemoteRefUpdate
    {
        /// <summary>
        /// Represent current status of a remote ref update.
        /// </summary>
        [Serializable]
        public enum UpdateStatus
        {
            /// <summary>
            /// Push process hasn't yet attempted to update this ref. This is the
            /// default status, prior to push process execution.
            /// </summary>
            NOT_ATTEMPTED,

            /// <summary>
            /// Remote ref was up to date, there was no need to update anything.
            /// </summary>
            UP_TO_DATE,

            /// <summary>
            /// Remote ref update was rejected, as it would cause non fast-forward
            /// update.
            /// </summary>
            REJECTED_NONFASTFORWARD,

            /// <summary>
            /// Remote ref update was rejected, because remote side doesn't
            /// support/allow deleting refs.
            /// </summary>
            REJECTED_NODELETE,

            /// <summary>
            /// Remote ref update was rejected, because old object id on remote
            /// repository wasn't the same as defined expected old object.
            /// </summary>
            REJECTED_REMOTE_CHANGED,

            /// <summary>
            /// Remote ref update was rejected for other reason, possibly described
            /// in <see cref="RemoteRefUpdate.get_Message"/>.
            /// </summary>
            REJECTED_OTHER_REASON,

            /// <summary>
            /// Remote ref didn't exist. Can occur on delete request of a non
            /// existing ref.
            /// </summary>
            NON_EXISTING,

            /// <summary>
            /// Push process is awaiting update report from remote repository. This
            /// is a temporary state or state after critical error in push process.
            /// </summary>
            AWAITING_REPORT,

            /// <summary>
            /// Remote ref was successfully updated.
            /// </summary>
            OK
        }

        private readonly Repository _localDb;

        /// <summary>
        /// Construct remote ref update request by providing an update specification.
        /// Object is created with default {@link Status#NOT_ATTEMPTED} status and no
        /// message.
        /// </summary>
        /// <param name="localDb">local repository to push from.</param>
        /// <param name="srcRef">
        /// source revision - any string resolvable by
        /// <see cref="Repository.Resolve"/>. This resolves to the new
        /// object that the caller want remote ref to be after update. Use
        /// null or <see cref="ObjectId.ZeroId"/> string for delete request.
        /// </param>
        /// <param name="remoteName">
        /// full name of a remote ref to update, e.g. "refs/heads/master"
        /// (no wildcard, no short name).
        /// </param>
        /// <param name="forceUpdate">
        /// true when caller want remote ref to be updated regardless
        /// whether it is fast-forward update (old object is ancestor of
        /// new object).
        /// </param>
        /// <param name="localName">
        /// optional full name of a local stored tracking branch, to
        /// update after push, e.g. "refs/remotes/zawir/dirty" (no
        /// wildcard, no short name); null if no local tracking branch
        /// should be updated.
        /// </param>
        /// <param name="expectedOldObjectId">
        /// optional object id that caller is expecting, requiring to be
        /// advertised by remote side before update; update will take
        /// place ONLY if remote side advertise exactly this expected id;
        /// null if caller doesn't care what object id remote side
        /// advertise. Use {@link ObjectId#zeroId()} when expecting no
        /// remote ref with this name.
        /// </param>
        public RemoteRefUpdate(Repository localDb, string srcRef, string remoteName, bool forceUpdate, string localName, ObjectId expectedOldObjectId)
        {
            if (remoteName == null)
                throw new ArgumentNullException("remoteName", "Remote name can't be null.");

            SourceRef = srcRef;
            NewObjectId = (srcRef == null ? ObjectId.ZeroId : localDb.Resolve(srcRef));
            if (NewObjectId == null)
            {
                throw new IOException("Source ref " + srcRef + " doesn't resolve to any object.");
            }
            RemoteName = remoteName;
            ForceUpdate = forceUpdate;
            if (localName != null && localDb != null)
            {
                TrackingRefUpdate = new TrackingRefUpdate(localDb, localName, remoteName, true, NewObjectId, "push");
            }
            else
            {
                TrackingRefUpdate = null;
            }
            _localDb = localDb;
            ExpectedOldObjectId = expectedOldObjectId;
            Status = UpdateStatus.NOT_ATTEMPTED;
        }

        /// <summary>
        /// Create a new instance of this object basing on existing instance for
        /// configuration. State (like <see cref="get_Message"/>, <see cref="get_Status"/>)
        /// of base object is not shared. Expected old object id is set up from
        /// scratch, as this constructor may be used for 2-stage push: first one
        /// being dry run, second one being actual push.
        /// </summary>
        /// <param name="baseUpdate">configuration base.</param>
        /// <param name="newExpectedOldObjectId">new expected object id value.</param>
        public RemoteRefUpdate(RemoteRefUpdate baseUpdate, ObjectId newExpectedOldObjectId)
            : this(baseUpdate._localDb, baseUpdate.SourceRef, baseUpdate.RemoteName, baseUpdate.ForceUpdate, (baseUpdate.TrackingRefUpdate == null ? null : baseUpdate.TrackingRefUpdate.LocalName), newExpectedOldObjectId)
        {

        }

        /// <summary>
        /// expectedOldObjectId required to be advertised by remote side, as
        /// set in constructor; may be null.
        /// </summary>
        public ObjectId ExpectedOldObjectId { get; private set; }

        /// <summary>
        /// true if some object is required to be advertised by remote side,
        /// as set in constructor; false otherwise.
        /// </summary>
        public bool IsExpectingOldObjectId
        {
            get
            {
                return ExpectedOldObjectId != null;
            }
        }

        /// <summary>
        /// newObjectId for remote ref, as set in constructor.
        /// </summary>
        public ObjectId NewObjectId { get; private set; }

        /// <summary>
        /// true if this update is deleting update; false otherwise.
        /// </summary>
        public bool IsDelete
        {
            get
            {
                return ObjectId.ZeroId.Equals(NewObjectId);
            }
        }

        /// <summary>
        /// name of remote ref to update, as set in constructor.
        /// </summary>
        public string RemoteName { get; private set; }

        /// <summary>
        /// local tracking branch update if localName was set in constructor.
        /// </summary>
        public TrackingRefUpdate TrackingRefUpdate { get; private set; }

        /// <summary>
        /// source revision as specified by user (in constructor), could be
        /// any string parseable by <see cref="Repository.Resolve"/>; can
        /// be null if specified that way in constructor - this stands for
        /// delete request.
        /// </summary>
        public string SourceRef { get; private set; }

        /// <summary>
        /// true if user specified a local tracking branch for remote update;
        /// false otherwise.
        /// </summary>
        public bool HasTrackingRefUpdate
        {
            get
            {
                return TrackingRefUpdate != null;
            }
        }

        /// <summary>
        /// true if user specified a local tracking branch for remote update;
        /// false otherwise.
        /// </summary>
        public bool ForceUpdate { get; private set; }

        /// <summary>
        /// status of remote ref update operation.
        /// </summary>
        public UpdateStatus Status { get; set; }

        /// <summary>
        /// Check whether update was fast-forward. Note that this result is
        /// meaningful only after successful update (when status is <see cref="UpdateStatus.OK"/>.
        /// <para/>
        /// true if update was fast-forward; false otherwise.
        /// </summary>
        public bool FastForward { get; set; }

        /// <summary>
        /// message describing reasons of status when needed/possible; may be null.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Update locally stored tracking branch with the new object.
        /// </summary>
        /// <param name="walk">walker used for checking update properties.</param>
        protected internal void updateTrackingRef(RevWalk.RevWalk walk)
        {
            if (IsDelete)
                TrackingRefUpdate.Delete(walk);
            else
                TrackingRefUpdate.Update(walk);
        }

        public override string ToString()
        {
            return "RemoteRefUpdate[remoteName=" + RemoteName + ", " + Status
                   + ", " + (ExpectedOldObjectId != null ? ExpectedOldObjectId.Abbreviate(_localDb).name() : "(null)")
                   + "..." + (NewObjectId != null ? NewObjectId.Abbreviate(_localDb).name() : "(null)")
                   + (FastForward ? ", fastForward" : string.Empty)
                   + ", srcRef=" + SourceRef + (ForceUpdate ? ", forceUpdate" : string.Empty) + ", message=" +
                   (Message != null
                        ? "\""
                          + Message + "\""
                        : "null") + ", " + _localDb.Directory + "]";
        }
    }
}