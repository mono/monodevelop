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

using NGit;
using NGit.Revwalk;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Update of a locally stored tracking branch.</summary>
	/// <remarks>Update of a locally stored tracking branch.</remarks>
	public class TrackingRefUpdate
	{
		private readonly string remoteName;

		private readonly RefUpdate update;

		/// <exception cref="System.IO.IOException"></exception>
		internal TrackingRefUpdate(Repository db, RefSpec spec, AnyObjectId nv, string msg
			) : this(db, spec.GetDestination(), spec.GetSource(), spec.IsForceUpdate(), nv, 
			msg)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal TrackingRefUpdate(Repository db, string localName, string remoteName, bool
			 forceUpdate, AnyObjectId nv, string msg)
		{
			this.remoteName = remoteName;
			update = db.UpdateRef(localName);
			update.SetForceUpdate(forceUpdate);
			update.SetNewObjectId(nv);
			update.SetRefLogMessage(msg, true);
		}

		/// <summary>Get the name of the remote ref.</summary>
		/// <remarks>
		/// Get the name of the remote ref.
		/// <p>
		/// Usually this is of the form "refs/heads/master".
		/// </remarks>
		/// <returns>the name used within the remote repository.</returns>
		public virtual string GetRemoteName()
		{
			return remoteName;
		}

		/// <summary>Get the name of the local tracking ref.</summary>
		/// <remarks>
		/// Get the name of the local tracking ref.
		/// <p>
		/// Usually this is of the form "refs/remotes/origin/master".
		/// </remarks>
		/// <returns>the name used within this local repository.</returns>
		public virtual string GetLocalName()
		{
			return update.GetName();
		}

		/// <summary>Get the new value the ref will be (or was) updated to.</summary>
		/// <remarks>Get the new value the ref will be (or was) updated to.</remarks>
		/// <returns>new value. Null if the caller has not configured it.</returns>
		public virtual ObjectId GetNewObjectId()
		{
			return update.GetNewObjectId();
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
			return update.GetOldObjectId();
		}

		/// <summary>Get the status of this update.</summary>
		/// <remarks>Get the status of this update.</remarks>
		/// <returns>the status of the update.</returns>
		public virtual RefUpdate.Result GetResult()
		{
			return update.GetResult();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Update(RevWalk walk)
		{
			update.Update(walk);
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Delete(RevWalk walk)
		{
			update.Delete(walk);
		}
	}
}
