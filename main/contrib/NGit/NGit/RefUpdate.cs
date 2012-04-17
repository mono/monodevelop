/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Revwalk;
using Sharpen;

namespace NGit
{
	/// <summary>Creates, updates or deletes any reference.</summary>
	/// <remarks>Creates, updates or deletes any reference.</remarks>
	public abstract class RefUpdate
	{
		/// <summary>Status of an update request.</summary>
		/// <remarks>Status of an update request.</remarks>
		public enum Result
		{
			NOT_ATTEMPTED,
			LOCK_FAILURE,
			NO_CHANGE,
			NEW,
			FORCED,
			FAST_FORWARD,
			REJECTED,
			REJECTED_CURRENT_BRANCH,
			IO_FAILURE,
			RENAMED
		}

		/// <summary>New value the caller wants this ref to have.</summary>
		/// <remarks>New value the caller wants this ref to have.</remarks>
		private ObjectId newValue;

		/// <summary>Does this specification ask for forced updated (rewind/reset)?</summary>
		private bool force;

		/// <summary>Identity to record action as within the reflog.</summary>
		/// <remarks>Identity to record action as within the reflog.</remarks>
		private PersonIdent refLogIdent;

		/// <summary>Message the caller wants included in the reflog.</summary>
		/// <remarks>Message the caller wants included in the reflog.</remarks>
		private string refLogMessage;

		/// <summary>
		/// Should the Result value be appended to
		/// <see cref="refLogMessage">refLogMessage</see>
		/// .
		/// </summary>
		private bool refLogIncludeResult;

		/// <summary>Old value of the ref, obtained after we lock it.</summary>
		/// <remarks>Old value of the ref, obtained after we lock it.</remarks>
		private ObjectId oldValue;

		/// <summary>
		/// If non-null, the value
		/// <see cref="oldValue">oldValue</see>
		/// must have to continue.
		/// </summary>
		private ObjectId expValue;

		/// <summary>Result of the update operation.</summary>
		/// <remarks>Result of the update operation.</remarks>
		private RefUpdate.Result result = RefUpdate.Result.NOT_ATTEMPTED;

		private readonly Ref @ref;

		/// <summary>
		/// Is this RefUpdate detaching a symbolic ref?
		/// We need this info since this.ref will normally be peeled of in case of
		/// detaching a symbolic ref (HEAD for example).
		/// </summary>
		/// <remarks>
		/// Is this RefUpdate detaching a symbolic ref?
		/// We need this info since this.ref will normally be peeled of in case of
		/// detaching a symbolic ref (HEAD for example).
		/// Without this flag we cannot decide whether the ref has to be updated or
		/// not in case when it was a symbolic ref and the newValue == oldValue.
		/// </remarks>
		private bool detachingSymbolicRef;

		/// <summary>Construct a new update operation for the reference.</summary>
		/// <remarks>
		/// Construct a new update operation for the reference.
		/// <p>
		/// <code>ref.getObjectId()</code>
		/// will be used to seed
		/// <see cref="GetOldObjectId()">GetOldObjectId()</see>
		/// ,
		/// which callers can use as part of their own update logic.
		/// </remarks>
		/// <param name="ref">the reference that will be updated by this operation.</param>
		protected internal RefUpdate(Ref @ref)
		{
			this.@ref = @ref;
			oldValue = @ref.GetObjectId();
			refLogMessage = string.Empty;
		}

		/// <returns>the reference database this update modifies.</returns>
		protected internal abstract RefDatabase GetRefDatabase();

		/// <returns>the repository storing the database's objects.</returns>
		protected internal abstract Repository GetRepository();

		/// <summary>Try to acquire the lock on the reference.</summary>
		/// <remarks>
		/// Try to acquire the lock on the reference.
		/// <p>
		/// If the locking was successful the implementor must set the current
		/// identity value by calling
		/// <see cref="SetOldObjectId(ObjectId)">SetOldObjectId(ObjectId)</see>
		/// .
		/// </remarks>
		/// <param name="deref">
		/// true if the lock should be taken against the leaf level
		/// reference; false if it should be taken exactly against the
		/// current reference.
		/// </param>
		/// <returns>
		/// true if the lock was acquired and the reference is likely
		/// protected from concurrent modification; false if it failed.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// the lock couldn't be taken due to an unexpected storage
		/// failure, and not because of a concurrent update.
		/// </exception>
		protected internal abstract bool TryLock(bool deref);

		/// <summary>
		/// Releases the lock taken by
		/// <see cref="TryLock(bool)">TryLock(bool)</see>
		/// if it succeeded.
		/// </summary>
		protected internal abstract void Unlock();

		/// <param name="desiredResult"></param>
		/// <returns>
		/// 
		/// <code>result</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal abstract RefUpdate.Result DoUpdate(RefUpdate.Result desiredResult
			);

		/// <param name="desiredResult"></param>
		/// <returns>
		/// 
		/// <code>result</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal abstract RefUpdate.Result DoDelete(RefUpdate.Result desiredResult
			);

		/// <param name="target"></param>
		/// <returns>
		/// 
		/// <see cref="Result.NEW">Result.NEW</see>
		/// on success.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal abstract RefUpdate.Result DoLink(string target);

		/// <summary>Get the name of the ref this update will operate on.</summary>
		/// <remarks>Get the name of the ref this update will operate on.</remarks>
		/// <returns>name of underlying ref.</returns>
		public virtual string GetName()
		{
			return GetRef().GetName();
		}

		/// <returns>the reference this update will create or modify.</returns>
		public virtual Ref GetRef()
		{
			return @ref;
		}

		/// <summary>Get the new value the ref will be (or was) updated to.</summary>
		/// <remarks>Get the new value the ref will be (or was) updated to.</remarks>
		/// <returns>new value. Null if the caller has not configured it.</returns>
		public virtual ObjectId GetNewObjectId()
		{
			return newValue;
		}

		/// <summary>Tells this RefUpdate that it is actually detaching a symbolic ref.</summary>
		/// <remarks>Tells this RefUpdate that it is actually detaching a symbolic ref.</remarks>
		public virtual void SetDetachingSymbolicRef()
		{
			detachingSymbolicRef = true;
		}

		/// <summary>Set the new value the ref will update to.</summary>
		/// <remarks>Set the new value the ref will update to.</remarks>
		/// <param name="id">the new value.</param>
		public virtual void SetNewObjectId(AnyObjectId id)
		{
			newValue = id.Copy();
		}

		/// <returns>
		/// the expected value of the ref after the lock is taken, but before
		/// update occurs. Null to avoid the compare and swap test. Use
		/// <see cref="ObjectId.ZeroId()">ObjectId.ZeroId()</see>
		/// to indicate expectation of a
		/// non-existant ref.
		/// </returns>
		public virtual ObjectId GetExpectedOldObjectId()
		{
			return expValue;
		}

		/// <param name="id">
		/// the expected value of the ref after the lock is taken, but
		/// before update occurs. Null to avoid the compare and swap test.
		/// Use
		/// <see cref="ObjectId.ZeroId()">ObjectId.ZeroId()</see>
		/// to indicate expectation of a
		/// non-existant ref.
		/// </param>
		public virtual void SetExpectedOldObjectId(AnyObjectId id)
		{
			expValue = id != null ? id.ToObjectId() : null;
		}

		/// <summary>Check if this update wants to forcefully change the ref.</summary>
		/// <remarks>Check if this update wants to forcefully change the ref.</remarks>
		/// <returns>true if this update should ignore merge tests.</returns>
		public virtual bool IsForceUpdate()
		{
			return force;
		}

		/// <summary>Set if this update wants to forcefully change the ref.</summary>
		/// <remarks>Set if this update wants to forcefully change the ref.</remarks>
		/// <param name="b">true if this update should ignore merge tests.</param>
		public virtual void SetForceUpdate(bool b)
		{
			force = b;
		}

		/// <returns>identity of the user making the change in the reflog.</returns>
		public virtual PersonIdent GetRefLogIdent()
		{
			return refLogIdent;
		}

		/// <summary>Set the identity of the user appearing in the reflog.</summary>
		/// <remarks>
		/// Set the identity of the user appearing in the reflog.
		/// <p>
		/// The timestamp portion of the identity is ignored. A new identity with the
		/// current timestamp will be created automatically when the update occurs
		/// and the log record is written.
		/// </remarks>
		/// <param name="pi">
		/// identity of the user. If null the identity will be
		/// automatically determined based on the repository
		/// configuration.
		/// </param>
		public virtual void SetRefLogIdent(PersonIdent pi)
		{
			refLogIdent = pi;
		}

		/// <summary>Get the message to include in the reflog.</summary>
		/// <remarks>Get the message to include in the reflog.</remarks>
		/// <returns>
		/// message the caller wants to include in the reflog; null if the
		/// update should not be logged.
		/// </returns>
		public virtual string GetRefLogMessage()
		{
			return refLogMessage;
		}

		/// <returns>
		/// 
		/// <code>true</code>
		/// if the ref log message should show the result.
		/// </returns>
		protected internal virtual bool IsRefLogIncludingResult()
		{
			return refLogIncludeResult;
		}

		/// <summary>Set the message to include in the reflog.</summary>
		/// <remarks>Set the message to include in the reflog.</remarks>
		/// <param name="msg">
		/// the message to describe this change. It may be null if
		/// appendStatus is null in order not to append to the reflog
		/// </param>
		/// <param name="appendStatus">
		/// true if the status of the ref change (fast-forward or
		/// forced-update) should be appended to the user supplied
		/// message.
		/// </param>
		public virtual void SetRefLogMessage(string msg, bool appendStatus)
		{
			if (msg == null && !appendStatus)
			{
				DisableRefLog();
			}
			else
			{
				if (msg == null && appendStatus)
				{
					refLogMessage = string.Empty;
					refLogIncludeResult = true;
				}
				else
				{
					refLogMessage = msg;
					refLogIncludeResult = appendStatus;
				}
			}
		}

		/// <summary>Don't record this update in the ref's associated reflog.</summary>
		/// <remarks>Don't record this update in the ref's associated reflog.</remarks>
		public virtual void DisableRefLog()
		{
			refLogMessage = null;
			refLogIncludeResult = false;
		}

		/// <summary>The old value of the ref, prior to the update being attempted.</summary>
		/// <remarks>
		/// The old value of the ref, prior to the update being attempted.
		/// <p>
		/// This value may differ before and after the update method. Initially it is
		/// populated with the value of the ref before the lock is taken, but the old
		/// value may change if someone else modified the ref between the time we
		/// last read it and when the ref was locked for update.
		/// </remarks>
		/// <returns>
		/// the value of the ref prior to the update being attempted; null if
		/// the updated has not been attempted yet.
		/// </returns>
		public virtual ObjectId GetOldObjectId()
		{
			return oldValue;
		}

		/// <summary>Set the old value of the ref.</summary>
		/// <remarks>Set the old value of the ref.</remarks>
		/// <param name="old">the old value.</param>
		protected internal virtual void SetOldObjectId(ObjectId old)
		{
			oldValue = old;
		}

		/// <summary>Get the status of this update.</summary>
		/// <remarks>
		/// Get the status of this update.
		/// <p>
		/// The same value that was previously returned from an update method.
		/// </remarks>
		/// <returns>the status of the update.</returns>
		public virtual RefUpdate.Result GetResult()
		{
			return result;
		}

		private void RequireCanDoUpdate()
		{
			if (newValue == null)
			{
				throw new InvalidOperationException(JGitText.Get().aNewObjectIdIsRequired);
			}
		}

		/// <summary>Force the ref to take the new value.</summary>
		/// <remarks>
		/// Force the ref to take the new value.
		/// <p>
		/// This is just a convenient helper for setting the force flag, and as such
		/// the merge test is performed.
		/// </remarks>
		/// <returns>the result status of the update.</returns>
		/// <exception cref="System.IO.IOException">an unexpected IO error occurred while writing changes.
		/// 	</exception>
		public virtual RefUpdate.Result ForceUpdate()
		{
			force = true;
			return Update();
		}

		/// <summary>Gracefully update the ref to the new value.</summary>
		/// <remarks>
		/// Gracefully update the ref to the new value.
		/// <p>
		/// Merge test will be performed according to
		/// <see cref="IsForceUpdate()">IsForceUpdate()</see>
		/// .
		/// <p>
		/// This is the same as:
		/// <pre>
		/// return update(new RevWalk(getRepository()));
		/// </pre>
		/// </remarks>
		/// <returns>the result status of the update.</returns>
		/// <exception cref="System.IO.IOException">an unexpected IO error occurred while writing changes.
		/// 	</exception>
		public virtual RefUpdate.Result Update()
		{
			RevWalk rw = new RevWalk(GetRepository());
			try
			{
				return Update(rw);
			}
			finally
			{
				rw.Release();
			}
		}

		/// <summary>Gracefully update the ref to the new value.</summary>
		/// <remarks>
		/// Gracefully update the ref to the new value.
		/// <p>
		/// Merge test will be performed according to
		/// <see cref="IsForceUpdate()">IsForceUpdate()</see>
		/// .
		/// </remarks>
		/// <param name="walk">
		/// a RevWalk instance this update command can borrow to perform
		/// the merge test. The walk will be reset to perform the test.
		/// </param>
		/// <returns>the result status of the update.</returns>
		/// <exception cref="System.IO.IOException">an unexpected IO error occurred while writing changes.
		/// 	</exception>
		public virtual RefUpdate.Result Update(RevWalk walk)
		{
			RequireCanDoUpdate();
			try
			{
				return result = UpdateImpl(walk, new _Store_484(this));
			}
			catch (IOException x)
			{
				result = RefUpdate.Result.IO_FAILURE;
				throw;
			}
		}

		private sealed class _Store_484 : RefUpdate.Store
		{
			public _Store_484(RefUpdate _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override RefUpdate.Result Execute(RefUpdate.Result status)
			{
				if (status == RefUpdate.Result.NO_CHANGE)
				{
					return status;
				}
				return this._enclosing.DoUpdate(status);
			}

			private readonly RefUpdate _enclosing;
		}

		/// <summary>Delete the ref.</summary>
		/// <remarks>
		/// Delete the ref.
		/// <p>
		/// This is the same as:
		/// <pre>
		/// return delete(new RevWalk(getRepository()));
		/// </pre>
		/// </remarks>
		/// <returns>the result status of the delete.</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual RefUpdate.Result Delete()
		{
			RevWalk rw = new RevWalk(GetRepository());
			try
			{
				return Delete(rw);
			}
			finally
			{
				rw.Release();
			}
		}

		/// <summary>Delete the ref.</summary>
		/// <remarks>Delete the ref.</remarks>
		/// <param name="walk">
		/// a RevWalk instance this delete command can borrow to perform
		/// the merge test. The walk will be reset to perform the test.
		/// </param>
		/// <returns>the result status of the delete.</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual RefUpdate.Result Delete(RevWalk walk)
		{
			string myName = GetRef().GetLeaf().GetName();
			if (myName.StartsWith(Constants.R_HEADS))
			{
				Ref head = GetRefDatabase().GetRef(Constants.HEAD);
				while (head.IsSymbolic())
				{
					head = head.GetTarget();
					if (myName.Equals(head.GetName()))
					{
						return result = RefUpdate.Result.REJECTED_CURRENT_BRANCH;
					}
				}
			}
			try
			{
				return result = UpdateImpl(walk, new _Store_540(this));
			}
			catch (IOException x)
			{
				result = RefUpdate.Result.IO_FAILURE;
				throw;
			}
		}

		private sealed class _Store_540 : RefUpdate.Store
		{
			public _Store_540(RefUpdate _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override RefUpdate.Result Execute(RefUpdate.Result status)
			{
				return this._enclosing.DoDelete(status);
			}

			private readonly RefUpdate _enclosing;
		}

		/// <summary>Replace this reference with a symbolic reference to another reference.</summary>
		/// <remarks>
		/// Replace this reference with a symbolic reference to another reference.
		/// <p>
		/// This exact reference (not its traversed leaf) is replaced with a symbolic
		/// reference to the requested name.
		/// </remarks>
		/// <param name="target">
		/// name of the new target for this reference. The new target name
		/// must be absolute, so it must begin with
		/// <code>refs/</code>
		/// .
		/// </param>
		/// <returns>
		/// 
		/// <see cref="Result.NEW">Result.NEW</see>
		/// or
		/// <see cref="Result.FORCED">Result.FORCED</see>
		/// on success.
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual RefUpdate.Result Link(string target)
		{
			if (!target.StartsWith(Constants.R_REFS))
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().illegalArgumentNotA
					, Constants.R_REFS));
			}
			if (GetRefDatabase().IsNameConflicting(GetName()))
			{
				return RefUpdate.Result.LOCK_FAILURE;
			}
			try
			{
				if (!TryLock(false))
				{
					return RefUpdate.Result.LOCK_FAILURE;
				}
				Ref old = GetRefDatabase().GetRef(GetName());
				if (old != null && old.IsSymbolic())
				{
					Ref dst = old.GetTarget();
					if (target.Equals(dst.GetName()))
					{
						return result = RefUpdate.Result.NO_CHANGE;
					}
				}
				if (old != null && old.GetObjectId() != null)
				{
					SetOldObjectId(old.GetObjectId());
				}
				Ref dst_1 = GetRefDatabase().GetRef(target);
				if (dst_1 != null && dst_1.GetObjectId() != null)
				{
					SetNewObjectId(dst_1.GetObjectId());
				}
				return result = DoLink(target);
			}
			catch (IOException x)
			{
				result = RefUpdate.Result.IO_FAILURE;
				throw;
			}
			finally
			{
				Unlock();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RefUpdate.Result UpdateImpl(RevWalk walk, RefUpdate.Store store)
		{
			RevObject newObj;
			RevObject oldObj;
			if (GetRefDatabase().IsNameConflicting(GetName()))
			{
				return RefUpdate.Result.LOCK_FAILURE;
			}
			try
			{
				if (!TryLock(true))
				{
					return RefUpdate.Result.LOCK_FAILURE;
				}
				if (expValue != null)
				{
					ObjectId o;
					o = oldValue != null ? oldValue : ObjectId.ZeroId;
					if (!AnyObjectId.Equals(expValue, o))
					{
						return RefUpdate.Result.LOCK_FAILURE;
					}
				}
				if (oldValue == null)
				{
					return store.Execute(RefUpdate.Result.NEW);
				}
				newObj = SafeParse(walk, newValue);
				oldObj = SafeParse(walk, oldValue);
				if (newObj == oldObj && !detachingSymbolicRef)
				{
					return store.Execute(RefUpdate.Result.NO_CHANGE);
				}
				if (newObj is RevCommit && oldObj is RevCommit)
				{
					if (walk.IsMergedInto((RevCommit)oldObj, (RevCommit)newObj))
					{
						return store.Execute(RefUpdate.Result.FAST_FORWARD);
					}
				}
				if (IsForceUpdate())
				{
					return store.Execute(RefUpdate.Result.FORCED);
				}
				return RefUpdate.Result.REJECTED;
			}
			finally
			{
				Unlock();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static RevObject SafeParse(RevWalk rw, AnyObjectId id)
		{
			try
			{
				return id != null ? rw.ParseAny(id) : null;
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

		/// <summary>Handle the abstraction of storing a ref update.</summary>
		/// <remarks>
		/// Handle the abstraction of storing a ref update. This is because both
		/// updating and deleting of a ref have merge testing in common.
		/// </remarks>
		private abstract class Store
		{
			/// <exception cref="System.IO.IOException"></exception>
			internal abstract RefUpdate.Result Execute(RefUpdate.Result status);

			internal Store(RefUpdate _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly RefUpdate _enclosing;
		}
	}
}
