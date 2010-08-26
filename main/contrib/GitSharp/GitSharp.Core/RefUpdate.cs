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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;

namespace GitSharp.Core
{
    public static class RefUpdateExtensions
    {
        public static ObjectId getNewObjectId(this RefUpdate refUpdate)
        {
            return refUpdate.NewObjectId;
        }

        public static void setNewObjectId(this RefUpdate refUpdate, AnyObjectId id)
        {
            refUpdate.NewObjectId = id.Copy();
        }

        public static bool isForceUpdate(this RefUpdate refUpdate)
        {
            return refUpdate.IsForceUpdate;
        }

        public static void setForceUpdate(this RefUpdate refUpdate, bool b)
        {
            refUpdate.IsForceUpdate = b;
        }
        public static string getName(this RefUpdate refUpdate)
        {
            return refUpdate.Name;
        }
        public static Ref getRef(this RefUpdate refUpdate)
        {
            return refUpdate.Ref;
        }

        public static ObjectId getExpectedOldObjectId(this RefUpdate refUpdate)
        {
            return refUpdate.ExpectedOldObjectId;
        }

        public static void setExpectedOldObjectId(this RefUpdate refUpdate, AnyObjectId id)
        {
            refUpdate.ExpectedOldObjectId = id != null ? id.ToObjectId() : null; ;
        }

        public static ObjectId getOldObjectId(this RefUpdate refUpdate)
        {
            return refUpdate.OldObjectId;
        }

        public static void setOldObjectId(this RefUpdate refUpdate, AnyObjectId id)
        {
            refUpdate.OldObjectId = id != null ? id.ToObjectId() : null; ;
        }

        public static RefUpdate.RefUpdateResult getResult(this RefUpdate refUpdate)
        {
            return refUpdate.Result;
        }

    }

    /// <summary>
    /// Creates, updates or deletes any reference.
    /// </summary>
    public abstract class RefUpdate
    {
        /// <summary>
        /// Status of an update request.
        /// </summary>
        public enum RefUpdateResult
        {
            /// <summary>
            /// The ref update/delete has not been attempted by the caller.
            /// </summary>
            NOT_ATTEMPTED,

            /// <summary>
            /// The ref could not be locked for update/delete.
            /// <para/>
            /// This is generally a transient failure and is usually caused by
            /// another process trying to access the ref at the same time as this
            /// process was trying to update it. It is possible a future operation
            /// will be successful.
            /// </summary>
            LOCK_FAILURE,

            /// <summary>
            /// Same value already stored.
            /// <para/>
            /// Both the old value and the new value are identical. No change was
            /// necessary for an update. For delete the branch is removed.
            /// </summary>
            NO_CHANGE,

            /// <summary>
            /// The ref was created locally for an update, but ignored for delete.
            /// <para/>
            /// The ref did not exist when the update started, but it was created
            /// successfully with the new value.
            /// </summary>
            NEW,

            /// <summary>
            /// The ref had to be forcefully updated/deleted.
            /// <para/>
            /// The ref already existed but its old value was not fully merged into
            /// the new value. The configuration permitted a forced update to take
            /// place, so ref now contains the new value. History associated with the
            /// objects not merged may no longer be reachable.
            /// </summary>
            FORCED,

            /// <summary>
            /// The ref was updated/deleted in a fast-forward way.
            /// <para/>
            /// The tracking ref already existed and its old value was fully merged
            /// into the new value. No history was made unreachable.
            /// </summary>
            FAST_FORWARD,

            /// <summary>
            /// Not a fast-forward and not stored.
            /// <para/>
            /// The tracking ref already existed but its old value was not fully
            /// merged into the new value. The configuration did not allow a forced
            /// update/delete to take place, so ref still contains the old value. No
            /// previous history was lost.
            /// </summary>
            REJECTED,

            /// <summary>
            /// Rejected because trying to delete the current branch.
            /// <para/>
            /// Has no meaning for update.
            /// </summary>
            REJECTED_CURRENT_BRANCH,

            /// <summary>
            /// The ref was probably not updated/deleted because of I/O error.
            /// <para/>
            /// Unexpected I/O error occurred when writing new ref. Such error may
            /// result in uncertain state, but most probably ref was not updated.
            /// <para/>
            /// This kind of error doesn't include {@link #LOCK_FAILURE}, which is a
            /// different case.
            /// </summary>
            IO_FAILURE,

            /// <summary>
            /// The ref was renamed from another name
            /// </summary>
            RENAMED
        }

        /// <summary>
        /// New value the caller wants this ref to have.
        /// </summary>
        private ObjectId newValue;

        /// <summary>
        /// Does this specification ask for forced updated (rewind/reset)?
        /// </summary>
        private bool force;

        /// <summary>
        /// Identity to record action as within the reflog.
        /// </summary>
        private PersonIdent refLogIdent;

        /// <summary>
        /// Message the caller wants included in the reflog.
        /// </summary>
        private string refLogMessage;


        /// <summary>
        /// Should the Result value be appended to <see cref="refLogMessage"/>.
        /// </summary>
        private bool refLogIncludeResult;

        /// <summary>
        /// Old value of the ref, obtained after we lock it.
        /// </summary>
        private ObjectId oldValue;

        /// <summary>
        /// If non-null, the value {@link #oldValue} must have to continue.
        /// </summary>
        private ObjectId expValue;

        /// <summary>
        /// Result of the update operation.
        /// </summary>
        private RefUpdateResult result = RefUpdateResult.NOT_ATTEMPTED;

        private Ref _ref;

        protected RefUpdate(Ref @ref)
        {
            _ref = @ref;
            oldValue = @ref.getObjectId();
            refLogMessage = "";
        }

        /// <returns>the reference database this update modifies.</returns>
        public abstract RefDatabase getRefDatabase();

        /// <returns>the repository storing the database's objects.</returns>
        public abstract Repository getRepository();

        /// <summary>
        /// Try to acquire the lock on the reference.
        /// <para/>
        /// If the locking was successful the implementor must set the current
        /// identity value by calling <see cref="set_OldObjectId"/>.
        /// </summary>
        /// <param name="deref">
        /// true if the lock should be taken against the leaf level
        /// reference; false if it should be taken exactly against the
        /// current reference.
        /// </param>
        /// <returns>
        /// true if the lock was acquired and the reference is likely
        /// protected from concurrent modification; false if it failed.
        /// </returns>
        protected abstract bool tryLock(bool deref);

        /// <summary>
        /// Releases the lock taken by {@link #tryLock} if it succeeded.
        /// </summary>
        public abstract void unlock();

        protected abstract RefUpdateResult doUpdate(RefUpdateResult desiredResult);

        protected abstract RefUpdateResult doDelete(RefUpdateResult desiredResult);

        protected abstract RefUpdateResult doLink(string target);

        /// <summary>name of the underlying ref this update will operate on.</summary>
        public string Name
        {
            get { return Ref.getName(); }
        }

        /// <summary>
        /// the reference this update will create or modify.
        /// </summary>
        public Ref Ref
        {
            get { return _ref; }
        }

        /// <summary>new value the ref will be (or was) updated to.</summary>
        public ObjectId NewObjectId
        {
            get { return newValue; }
            set { newValue = value.Copy(); }
        }

        /// <summary>
        /// the expected value of the ref after the lock is taken, but
        /// before update occurs. Null to avoid the compare and swap test.
        /// Use <see cref="ObjectId.ZeroId"/> to indicate expectation of a
        /// non-existant ref.
        /// </summary>
        public ObjectId ExpectedOldObjectId
        {
            get { return expValue; }
            set { expValue = value != null ? value.ToObjectId() : null; }
        }

        /// <summary>
        /// Will this update want to forcefully change the ref, this ignoring merge results ?
        /// </summary>
        public bool IsForceUpdate
        {
            get { return force; }
            set { force = value; }
        }

        /// <returns>identity of the user making the change in the reflog.</returns>
        public PersonIdent getRefLogIdent()
        {
            return refLogIdent;
        }

        /// <summary>
        /// Set the identity of the user appearing in the reflog.
        /// <para/>
        /// The timestamp portion of the identity is ignored. A new identity with the
        /// current timestamp will be created automatically when the update occurs
        /// and the log record is written.
        /// </summary>
        /// <param name="pi">
        /// identity of the user. If null the identity will be
        /// automatically determined based on the repository
        /// configuration.
        /// </param>
        public void setRefLogIdent(PersonIdent pi)
        {
            refLogIdent = pi;
        }

        /// <summary>
        /// Get the message to include in the reflog.
        /// </summary>
        /// <returns>
        /// message the caller wants to include in the reflog; null if the
        /// update should not be logged.
        /// </returns>
        public string getRefLogMessage()
        {
            return refLogMessage;
        }

        /// <returns>{@code true} if the ref log message should show the result.</returns>
        protected bool isRefLogIncludingResult()
        {
            return refLogIncludeResult;
        }

        /// <summary>
        /// Set the message to include in the reflog.
        /// </summary>
        /// <param name="msg">
        /// the message to describe this change. It may be null if
        /// appendStatus is null in order not to append to the reflog
        /// </param>
        /// <param name="appendStatus">
        /// true if the status of the ref change (fast-forward or
        /// forced-update) should be appended to the user supplied
        /// message.
        /// </param>
        public void setRefLogMessage(string msg, bool appendStatus)
        {
            if (msg == null && !appendStatus)
                disableRefLog();
            else if (msg == null && appendStatus)
            {
                refLogMessage = "";
                refLogIncludeResult = true;
            }
            else
            {
                refLogMessage = msg;
                refLogIncludeResult = appendStatus;
            }
        }

        /// <summary>
        /// Don't record this update in the ref's associated reflog.
        /// </summary>
        public void disableRefLog()
        {
            refLogMessage = null;
            refLogIncludeResult = false;
        }

        ///<summary>
        /// The old value of the ref, prior to the update being attempted.
        /// <para/>
        /// This value may differ before and after the update method. Initially it is
        /// populated with the value of the ref before the lock is taken, but the old
        /// value may change if someone else modified the ref between the time we
        /// last read it and when the ref was locked for update.
        /// </summary>
        public ObjectId OldObjectId
        {
            get { return oldValue; }
            set { oldValue = value; }
        }


        /// <summary>
        /// Get the status of this update.
        /// <para/>
        /// The same value that was previously returned from an update method.
        /// </summary>
        /// <returns></returns>
        public RefUpdateResult Result
        {
            get { return result; }
        }

        private void requireCanDoUpdate()
        {
            if (newValue == null)
                throw new InvalidOperationException("A NewObjectId is required.");
        }

        /// <summary>
        /// Force the ref to take the new value.
        /// <para/>
        /// This is just a convenient helper for setting the force flag, and as such
        /// the merge test is performed.
        /// </summary>
        /// <returns>the result status of the update.</returns>
        public RefUpdateResult forceUpdate()
        {
            force = true;
            return update();
        }

        /// <summary>
        /// Gracefully update the ref to the new value.
        /// <para/>
        /// Merge test will be performed according to <see cref="IsForceUpdate"/>.
        /// <para/>
        /// This is the same as:
        /// 
        /// <pre>
        /// return update(new RevWalk(getRepository()));
        /// </pre>
        /// </summary>
        /// <returns>the result status of the update.</returns>
        public RefUpdateResult update()
        {
            return update(new RevWalk.RevWalk(getRepository()));
        }

        /// <summary>
        /// Gracefully update the ref to the new value.
        /// <para/>
        /// Merge test will be performed according to <see cref="IsForceUpdate"/>.
        /// </summary>
        /// <param name="walk">
        /// a RevWalk instance this update command can borrow to perform
        /// the merge test. The walk will be reset to perform the test.
        /// </param>
        /// <returns>
        /// the result status of the update.
        /// </returns>
        public RefUpdateResult update(RevWalk.RevWalk walk)
        {
            requireCanDoUpdate();
            try
            {
                return result = updateImpl(walk, new UpdateStore(this));
            }
            catch (IOException x)
            {
                result = RefUpdateResult.IO_FAILURE;
                throw x;
            }
        }

        /// <summary>
        /// Delete the ref.
        /// <para/>
        /// This is the same as:
        /// 
        /// <pre>
        /// return delete(new RevWalk(getRepository()));
        /// </pre>
        /// </summary>
        /// <returns>the result status of the delete.</returns>
        public RefUpdateResult delete()
        {
            return delete(new RevWalk.RevWalk(getRepository()));
        }

        /// <summary>
        /// Delete the ref.
        /// </summary>
        /// <param name="walk">
        /// a RevWalk instance this delete command can borrow to perform
        /// the merge test. The walk will be reset to perform the test.
        /// </param>
        /// <returns>the result status of the delete.</returns>
        public RefUpdateResult delete(RevWalk.RevWalk walk)
        {
            string myName = Ref.getLeaf().getName();
            if (myName.StartsWith(Constants.R_HEADS))
            {
                Ref head = getRefDatabase().getRef(Constants.HEAD);
                while (head.isSymbolic())
                {
                    head = head.getTarget();
                    if (myName.Equals(head.getName()))
                        return result = RefUpdateResult.REJECTED_CURRENT_BRANCH;
                }
            }

            try
            {
                return result = updateImpl(walk, new DeleteStore(this));
            }
            catch (IOException)
            {
                result = RefUpdateResult.IO_FAILURE;
                throw;
            }
        }

        /// <summary>
        /// Replace this reference with a symbolic reference to another reference.
        /// <para/>
        /// This exact reference (not its traversed leaf) is replaced with a symbolic
        /// reference to the requested name.
        /// </summary>
        /// <param name="target">
        /// name of the new target for this reference. The new target name
        /// must be absolute, so it must begin with {@code refs/}.
        /// </param>
        /// <returns><see cref="RefUpdateResult.NEW"/> or <see cref="RefUpdateResult.FORCED"/> on success.</returns>
        public RefUpdateResult link(string target)
        {
            if (!target.StartsWith(Constants.R_REFS))
                throw new ArgumentException("Not " + Constants.R_REFS);
            if (getRefDatabase().isNameConflicting(Name))
                return RefUpdateResult.LOCK_FAILURE;
            try
            {
                if (!tryLock(false))
                    return RefUpdateResult.LOCK_FAILURE;

                Ref old = getRefDatabase().getRef(Name);
                if (old != null && old.isSymbolic())
                {
                    Ref dst = old.getTarget();
                    if (target.Equals(dst.getName()))
                        return result = RefUpdateResult.NO_CHANGE;
                }

                if (old != null && old.getObjectId() != null)
                    OldObjectId = (old.getObjectId());

                Ref dst2 = getRefDatabase().getRef(target);
                if (dst2 != null && dst2.getObjectId() != null)
                    NewObjectId = (dst2.getObjectId());

                return result = doLink(target);
            }
            catch (IOException)
            {
                result = RefUpdateResult.IO_FAILURE;
                throw;
            }
            finally
            {
                unlock();
            }
        }


        private RefUpdateResult updateImpl(RevWalk.RevWalk walk, Store store)
        {
            RevObject newObj;
            RevObject oldObj;

            if (getRefDatabase().isNameConflicting(Name))
                return RefUpdateResult.LOCK_FAILURE;
            try
            {
                if (!tryLock(true))
                    return RefUpdateResult.LOCK_FAILURE;
                if (expValue != null)
                {
                    ObjectId o;
                    o = oldValue != null ? oldValue : ObjectId.ZeroId;
                    if (!AnyObjectId.equals(expValue, o))
                        return RefUpdateResult.LOCK_FAILURE;
                }
                if (oldValue == null)
                    return store.execute(RefUpdateResult.NEW);

                newObj = safeParse(walk, newValue);
                oldObj = safeParse(walk, oldValue);
                if (newObj == oldObj)
                    return store.execute(RefUpdateResult.NO_CHANGE);

                if (newObj is RevCommit && oldObj is RevCommit)
                {
                    if (walk.isMergedInto((RevCommit)oldObj, (RevCommit)newObj))
                        return store.execute(RefUpdateResult.FAST_FORWARD);
                }

                if (IsForceUpdate)
                    return store.execute(RefUpdateResult.FORCED);
                return RefUpdateResult.REJECTED;
            }
            finally
            {
                unlock();
            }
        }

        private static RevObject safeParse(RevWalk.RevWalk rw, AnyObjectId id)
        {
            try
            {
                return id != null ? rw.parseAny(id) : null;
            }
            catch (MissingObjectException)
            {
                // We can expect some objects to be missing, like if we are
                // trying to force a deletion of a branch and the object it
                // points to has been pruned from the database due to freak
                // corruption accidents (it happens with 'git new-work-dir').
                //
                return null;
            }
        }

        /// <summary>
        /// Handle the abstraction of storing a ref update. This is because both
        /// updating and deleting of a ref have merge testing in common.
        /// </summary>
        private abstract class Store
        {

            public abstract RefUpdateResult execute(RefUpdateResult status);
        }

        private class UpdateStore : Store
        {
            private readonly RefUpdate _refUpdate;

            public UpdateStore(RefUpdate refUpdate)
            {
                _refUpdate = refUpdate;
            }

            public override RefUpdateResult execute(RefUpdateResult status)
            {
                if (status == RefUpdateResult.NO_CHANGE)
                    return status;
                return _refUpdate.doUpdate(status);
            }
        }

        private class DeleteStore : Store
        {
            private readonly RefUpdate _refUpdate;

            public DeleteStore(RefUpdate refUpdate)
            {
                _refUpdate = refUpdate;
            }

            public override RefUpdateResult execute(RefUpdateResult status)
            {
                return _refUpdate.doDelete(status);
            }
        }
    }
}


