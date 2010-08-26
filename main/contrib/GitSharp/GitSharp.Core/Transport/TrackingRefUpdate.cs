/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
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

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Update of a locally stored tracking branch.
    /// </summary>
    public class TrackingRefUpdate
    {
        private readonly RefUpdate update;

        public TrackingRefUpdate(Repository db, RefSpec spec, AnyObjectId nv, string msg)
            : this(db, spec.Destination, spec.Source, spec.Force, nv, msg)
        {
            if (spec == null)
                throw new System.ArgumentNullException("spec");
        }

        public TrackingRefUpdate(Repository db, string localName, string remoteName, bool forceUpdate, AnyObjectId nv, string msg)
        {
            if (db == null)
                throw new System.ArgumentNullException("db");
            if (nv == null)
                throw new System.ArgumentNullException("nv");
            RemoteName = remoteName;
            update = db.UpdateRef(localName);
            update.IsForceUpdate = forceUpdate;
            update.NewObjectId = nv.Copy();
            update.setRefLogMessage(msg, true);
        }

        /// <summary>
        /// the name of the remote ref.
        /// <para/>
        /// Usually this is of the form "refs/heads/master".
        /// </summary>
        public string RemoteName { get; private set; }

        /// <summary>
        /// Get the name of the local tracking ref.
        /// <para/>
        /// Usually this is of the form "refs/remotes/origin/master".
        /// </summary>
        public string LocalName
        {
            get
            {
                return update.Name;
            }
        }

        /// <summary>
        /// Get the new value the ref will be (or was) updated to. Null if the caller has not configured it.
        /// </summary>
        public ObjectId NewObjectId
        {
            get
            {
                return update.NewObjectId;
            }
        }

        /// <summary>
        /// The old value of the ref, prior to the update being attempted.
        /// <para/>
        /// This value may differ before and after the update method. Initially it is
        /// populated with the value of the ref before the lock is taken, but the old
        /// value may change if someone else modified the ref between the time we
        /// last read it and when the ref was locked for update.
        /// <para/>
        /// Returns the value of the ref prior to the update being attempted; null if
        /// the updated has not been attempted yet.
        /// </summary>
        public ObjectId OldObjectId
        {
            get
            {
                return update.OldObjectId;
            }
        }

        /// <summary>
        /// the status of this update.
        /// </summary>
        public RefUpdate.RefUpdateResult Result
        {
            get
            {
                return update.Result;
            }
        }

        public void Update(RevWalk.RevWalk walk)
        {
            update.update(walk);
        }

        public void Delete(RevWalk.RevWalk walk)
        {
            update.delete(walk);
        }
    }
}