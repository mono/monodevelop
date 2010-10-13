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

using System.Collections.Generic;
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Lists known refs from the remote and sends objects to the remote.</summary>
	/// <remarks>
	/// Lists known refs from the remote and sends objects to the remote.
	/// <p>
	/// A push connection typically connects to the <code>git-receive-pack</code>
	/// service running where the remote repository is stored. This provides a
	/// one-way object transfer service to copy objects from the local repository
	/// into the remote repository, as well as a way to modify the refs stored by the
	/// remote repository.
	/// <p>
	/// Instances of a PushConnection must be created by a
	/// <see cref="Transport">Transport</see>
	/// that
	/// implements a specific object transfer protocol that both sides of the
	/// connection understand.
	/// <p>
	/// PushConnection instances are not thread safe and may be accessed by only one
	/// thread at a time.
	/// </remarks>
	/// <seealso cref="Transport">Transport</seealso>
	public interface PushConnection : Connection
	{
		/// <summary>Pushes to the remote repository basing on provided specification.</summary>
		/// <remarks>
		/// Pushes to the remote repository basing on provided specification. This
		/// possibly result in update/creation/deletion of refs on remote repository
		/// and sending objects that remote repository need to have a consistent
		/// objects graph from new refs.
		/// <p>
		/// <p>
		/// Only one call per connection is allowed. Subsequent calls will result in
		/// <see cref="NGit.Errors.TransportException">NGit.Errors.TransportException</see>
		/// .
		/// </p>
		/// <p>
		/// Implementation may use local repository to send a minimum set of objects
		/// needed by remote repository in efficient way.
		/// <see cref="Transport.IsPushThin()">Transport.IsPushThin()</see>
		/// should be honored if applicable.
		/// refUpdates should be filled with information about status of each update.
		/// </p>
		/// </remarks>
		/// <param name="monitor">
		/// progress monitor to update the end-user about the amount of
		/// work completed, or to indicate cancellation. Implementors
		/// should poll the monitor at regular intervals to look for
		/// cancellation requests from the user.
		/// </param>
		/// <param name="refUpdates">
		/// map of remote refnames to remote refs update
		/// specifications/statuses. Can't be empty. This indicate what
		/// refs caller want to update on remote side. Only refs updates
		/// with
		/// <see cref="Status.NOT_ATTEMPTED">Status.NOT_ATTEMPTED</see>
		/// should passed.
		/// Implementation must ensure that and appropriate status with
		/// optional message should be set during call. No refUpdate with
		/// <see cref="Status.AWAITING_REPORT">Status.AWAITING_REPORT</see>
		/// or
		/// <see cref="Status.NOT_ATTEMPTED">Status.NOT_ATTEMPTED</see>
		/// can be leaved by implementation after return from this call.
		/// </param>
		/// <exception cref="NGit.Errors.TransportException">
		/// objects could not be copied due to a network failure,
		/// critical protocol error, or error on remote side, or
		/// connection was already used for push - new connection must be
		/// created. Non-critical errors concerning only isolated refs
		/// should be placed in refUpdates.
		/// </exception>
		void Push(ProgressMonitor monitor, IDictionary<string, RemoteRefUpdate> refUpdates
			);
	}
}
