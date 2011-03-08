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
using NGit.Revwalk;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Represent request and status of a remote ref update.</summary>
	/// <remarks>
	/// Represent request and status of a remote ref update. Specification is
	/// provided by client, while status is handled by
	/// <see cref="PushProcess">PushProcess</see>
	/// class,
	/// being read-only for client.
	/// <p>
	/// Client can create instances of this class directly, basing on user
	/// specification and advertised refs (
	/// <see cref="Connection">Connection</see>
	/// or through
	/// <see cref="Transport">Transport</see>
	/// helper methods. Apply this specification on remote
	/// repository using
	/// <see cref="Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E})
	/// 	">Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;)
	/// 	</see>
	/// method.
	/// </p>
	/// </remarks>
	public class RemoteRefUpdate
	{
		/// <summary>Represent current status of a remote ref update.</summary>
		/// <remarks>Represent current status of a remote ref update.</remarks>
		public enum Status
		{
			NOT_ATTEMPTED,
			UP_TO_DATE,
			REJECTED_NONFASTFORWARD,
			REJECTED_NODELETE,
			REJECTED_REMOTE_CHANGED,
			REJECTED_OTHER_REASON,
			NON_EXISTING,
			AWAITING_REPORT,
			OK
		}

		private readonly ObjectId expectedOldObjectId;

		private readonly ObjectId newObjectId;

		private readonly string remoteName;

		private readonly TrackingRefUpdate trackingRefUpdate;

		private readonly string srcRef;

		private readonly bool forceUpdate;

		private RemoteRefUpdate.Status status;

		private bool fastForward;

		private string message;

		private readonly Repository localDb;

		/// <summary>Construct remote ref update request by providing an update specification.
		/// 	</summary>
		/// <remarks>
		/// Construct remote ref update request by providing an update specification.
		/// Object is created with default
		/// <see cref="Status.NOT_ATTEMPTED">Status.NOT_ATTEMPTED</see>
		/// status and no
		/// message.
		/// </remarks>
		/// <param name="localDb">local repository to push from.</param>
		/// <param name="srcRef">
		/// source revision - any string resolvable by
		/// <see cref="NGit.Repository.Resolve(string)">NGit.Repository.Resolve(string)</see>
		/// . This resolves to the new
		/// object that the caller want remote ref to be after update. Use
		/// null or
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// string for delete request.
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
		/// advertise. Use
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// when expecting no
		/// remote ref with this name.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// when I/O error occurred during creating
		/// <see cref="TrackingRefUpdate">TrackingRefUpdate</see>
		/// for local tracking branch or srcRef
		/// can't be resolved to any object.
		/// </exception>
		/// <exception cref="System.ArgumentException">if some required parameter was null</exception>
		public RemoteRefUpdate(Repository localDb, string srcRef, string remoteName, bool
			 forceUpdate, string localName, ObjectId expectedOldObjectId) : this(localDb, srcRef
			, srcRef != null ? localDb.Resolve(srcRef) : ObjectId.ZeroId, remoteName, forceUpdate
			, localName, expectedOldObjectId)
		{
		}

		/// <summary>Construct remote ref update request by providing an update specification.
		/// 	</summary>
		/// <remarks>
		/// Construct remote ref update request by providing an update specification.
		/// Object is created with default
		/// <see cref="Status.NOT_ATTEMPTED">Status.NOT_ATTEMPTED</see>
		/// status and no
		/// message.
		/// </remarks>
		/// <param name="localDb">local repository to push from.</param>
		/// <param name="srcRef">source revision. Use null to delete.</param>
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
		/// advertise. Use
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// when expecting no
		/// remote ref with this name.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// when I/O error occurred during creating
		/// <see cref="TrackingRefUpdate">TrackingRefUpdate</see>
		/// for local tracking branch or srcRef
		/// can't be resolved to any object.
		/// </exception>
		/// <exception cref="System.ArgumentException">if some required parameter was null</exception>
		public RemoteRefUpdate(Repository localDb, Ref srcRef, string remoteName, bool forceUpdate
			, string localName, ObjectId expectedOldObjectId) : this(localDb, srcRef != null
			 ? srcRef.GetName() : null, srcRef != null ? srcRef.GetObjectId() : null, remoteName
			, forceUpdate, localName, expectedOldObjectId)
		{
		}

		/// <summary>Construct remote ref update request by providing an update specification.
		/// 	</summary>
		/// <remarks>
		/// Construct remote ref update request by providing an update specification.
		/// Object is created with default
		/// <see cref="Status.NOT_ATTEMPTED">Status.NOT_ATTEMPTED</see>
		/// status and no
		/// message.
		/// </remarks>
		/// <param name="localDb">local repository to push from.</param>
		/// <param name="srcRef">
		/// source revision to label srcId with. If null srcId.name() will
		/// be used instead.
		/// </param>
		/// <param name="srcId">
		/// The new object that the caller wants remote ref to be after
		/// update. Use null or
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// for delete
		/// request.
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
		/// advertise. Use
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// when expecting no
		/// remote ref with this name.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// when I/O error occurred during creating
		/// <see cref="TrackingRefUpdate">TrackingRefUpdate</see>
		/// for local tracking branch or srcRef
		/// can't be resolved to any object.
		/// </exception>
		/// <exception cref="System.ArgumentException">if some required parameter was null</exception>
		public RemoteRefUpdate(Repository localDb, string srcRef, ObjectId srcId, string 
			remoteName, bool forceUpdate, string localName, ObjectId expectedOldObjectId)
		{
			if (remoteName == null)
			{
				throw new ArgumentException(JGitText.Get().remoteNameCantBeNull);
			}
			if (srcId == null && srcRef != null)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().sourceRefDoesntResolveToAnyObject
					, srcRef));
			}
			if (srcRef != null)
			{
				this.srcRef = srcRef;
			}
			else
			{
				if (srcId != null && !srcId.Equals(ObjectId.ZeroId))
				{
					this.srcRef = srcId.Name;
				}
				else
				{
					this.srcRef = null;
				}
			}
			if (srcId != null)
			{
				this.newObjectId = srcId;
			}
			else
			{
				this.newObjectId = ObjectId.ZeroId;
			}
			this.remoteName = remoteName;
			this.forceUpdate = forceUpdate;
			if (localName != null && localDb != null)
			{
				trackingRefUpdate = new TrackingRefUpdate(localDb, localName, remoteName, true, newObjectId
					, "push");
			}
			else
			{
				trackingRefUpdate = null;
			}
			this.localDb = localDb;
			this.expectedOldObjectId = expectedOldObjectId;
			this.status = RemoteRefUpdate.Status.NOT_ATTEMPTED;
		}

		/// <summary>
		/// Create a new instance of this object basing on existing instance for
		/// configuration.
		/// </summary>
		/// <remarks>
		/// Create a new instance of this object basing on existing instance for
		/// configuration. State (like
		/// <see cref="GetMessage()">GetMessage()</see>
		/// ,
		/// <see cref="GetStatus()">GetStatus()</see>
		/// )
		/// of base object is not shared. Expected old object id is set up from
		/// scratch, as this constructor may be used for 2-stage push: first one
		/// being dry run, second one being actual push.
		/// </remarks>
		/// <param name="base">configuration base.</param>
		/// <param name="newExpectedOldObjectId">new expected object id value.</param>
		/// <exception cref="System.IO.IOException">
		/// when I/O error occurred during creating
		/// <see cref="TrackingRefUpdate">TrackingRefUpdate</see>
		/// for local tracking branch or srcRef
		/// of base object no longer can be resolved to any object.
		/// </exception>
		public RemoteRefUpdate(NGit.Transport.RemoteRefUpdate @base, ObjectId newExpectedOldObjectId
			) : this(@base.localDb, @base.srcRef, @base.remoteName, @base.forceUpdate, (@base
			.trackingRefUpdate == null ? null : @base.trackingRefUpdate.GetLocalName()), newExpectedOldObjectId
			)
		{
		}

		/// <returns>
		/// expectedOldObjectId required to be advertised by remote side, as
		/// set in constructor; may be null.
		/// </returns>
		public virtual ObjectId GetExpectedOldObjectId()
		{
			return expectedOldObjectId;
		}

		/// <returns>
		/// true if some object is required to be advertised by remote side,
		/// as set in constructor; false otherwise.
		/// </returns>
		public virtual bool IsExpectingOldObjectId()
		{
			return expectedOldObjectId != null;
		}

		/// <returns>newObjectId for remote ref, as set in constructor.</returns>
		public virtual ObjectId GetNewObjectId()
		{
			return newObjectId;
		}

		/// <returns>true if this update is deleting update; false otherwise.</returns>
		public virtual bool IsDelete()
		{
			return ObjectId.ZeroId.Equals(newObjectId);
		}

		/// <returns>name of remote ref to update, as set in constructor.</returns>
		public virtual string GetRemoteName()
		{
			return remoteName;
		}

		/// <returns>local tracking branch update if localName was set in constructor.</returns>
		public virtual TrackingRefUpdate GetTrackingRefUpdate()
		{
			return trackingRefUpdate;
		}

		/// <returns>
		/// source revision as specified by user (in constructor), could be
		/// any string parseable by
		/// <see cref="NGit.Repository.Resolve(string)">NGit.Repository.Resolve(string)</see>
		/// ; can
		/// be null if specified that way in constructor - this stands for
		/// delete request.
		/// </returns>
		public virtual string GetSrcRef()
		{
			return srcRef;
		}

		/// <returns>
		/// true if user specified a local tracking branch for remote update;
		/// false otherwise.
		/// </returns>
		public virtual bool HasTrackingRefUpdate()
		{
			return trackingRefUpdate != null;
		}

		/// <returns>
		/// true if this update is forced regardless of old remote ref
		/// object; false otherwise.
		/// </returns>
		public virtual bool IsForceUpdate()
		{
			return forceUpdate;
		}

		/// <returns>status of remote ref update operation.</returns>
		public virtual RemoteRefUpdate.Status GetStatus()
		{
			return status;
		}

		/// <summary>Check whether update was fast-forward.</summary>
		/// <remarks>
		/// Check whether update was fast-forward. Note that this result is
		/// meaningful only after successful update (when status is
		/// <see cref="Status.OK">Status.OK</see>
		/// ).
		/// </remarks>
		/// <returns>true if update was fast-forward; false otherwise.</returns>
		public virtual bool IsFastForward()
		{
			return fastForward;
		}

		/// <returns>
		/// message describing reasons of status when needed/possible; may be
		/// null.
		/// </returns>
		public virtual string GetMessage()
		{
			return message;
		}

		internal virtual void SetStatus(RemoteRefUpdate.Status status)
		{
			this.status = status;
		}

		internal virtual void SetFastForward(bool fastForward)
		{
			this.fastForward = fastForward;
		}

		internal virtual void SetMessage(string message)
		{
			this.message = message;
		}

		/// <summary>Update locally stored tracking branch with the new object.</summary>
		/// <remarks>Update locally stored tracking branch with the new object.</remarks>
		/// <param name="walk">walker used for checking update properties.</param>
		/// <exception cref="System.IO.IOException">when I/O error occurred during update</exception>
		protected internal virtual void UpdateTrackingRef(RevWalk walk)
		{
			if (IsDelete())
			{
				trackingRefUpdate.Delete(walk);
			}
			else
			{
				trackingRefUpdate.Update(walk);
			}
		}

		public override string ToString()
		{
			return "RemoteRefUpdate[remoteName=" + remoteName + ", " + status + ", " + (expectedOldObjectId
				 != null ? expectedOldObjectId.Name : "(null)") + "..." + (newObjectId != null ? 
				newObjectId.Name : "(null)") + (fastForward ? ", fastForward" : string.Empty) + 
				", srcRef=" + srcRef + (forceUpdate ? ", forceUpdate" : string.Empty) + ", message="
				 + (message != null ? "\"" + message + "\"" : "null") + "]";
		}
	}
}
