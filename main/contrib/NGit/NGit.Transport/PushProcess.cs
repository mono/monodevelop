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
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Class performing push operation on remote repository.</summary>
	/// <remarks>Class performing push operation on remote repository.</remarks>
	/// <seealso cref="Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E})
	/// 	">Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;)
	/// 	</seealso>
	internal class PushProcess
	{
		/// <summary>
		/// Task name for
		/// <see cref="NGit.ProgressMonitor">NGit.ProgressMonitor</see>
		/// used during opening connection.
		/// </summary>
		internal static readonly string PROGRESS_OPENING_CONNECTION = JGitText.Get().openingConnection;

		/// <summary>Transport used to perform this operation.</summary>
		/// <remarks>Transport used to perform this operation.</remarks>
		private readonly NGit.Transport.Transport transport;

		/// <summary>Push operation connection created to perform this operation</summary>
		private PushConnection connection;

		/// <summary>Refs to update on remote side.</summary>
		/// <remarks>Refs to update on remote side.</remarks>
		private readonly IDictionary<string, RemoteRefUpdate> toPush;

		/// <summary>Revision walker for checking some updates properties.</summary>
		/// <remarks>Revision walker for checking some updates properties.</remarks>
		private readonly RevWalk walker;

		/// <summary>Create process for specified transport and refs updates specification.</summary>
		/// <remarks>Create process for specified transport and refs updates specification.</remarks>
		/// <param name="transport">
		/// transport between remote and local repository, used to create
		/// connection.
		/// </param>
		/// <param name="toPush">specification of refs updates (and local tracking branches).
		/// 	</param>
		/// <exception cref="NGit.Errors.TransportException">NGit.Errors.TransportException</exception>
		internal PushProcess(NGit.Transport.Transport transport, ICollection<RemoteRefUpdate
			> toPush)
		{
			this.walker = new RevWalk(transport.local);
			this.transport = transport;
			this.toPush = new Dictionary<string, RemoteRefUpdate>();
			foreach (RemoteRefUpdate rru in toPush)
			{
				if (this.toPush.Put(rru.GetRemoteName(), rru) != null)
				{
					throw new TransportException(MessageFormat.Format(JGitText.Get().duplicateRemoteRefUpdateIsIllegal
						, rru.GetRemoteName()));
				}
			}
		}

		/// <summary>
		/// Perform push operation between local and remote repository - set remote
		/// refs appropriately, send needed objects and update local tracking refs.
		/// </summary>
		/// <remarks>
		/// Perform push operation between local and remote repository - set remote
		/// refs appropriately, send needed objects and update local tracking refs.
		/// <p>
		/// When
		/// <see cref="Transport.IsDryRun()">Transport.IsDryRun()</see>
		/// is true, result of this operation is
		/// just estimation of real operation result, no real action is performed.
		/// </remarks>
		/// <param name="monitor">progress monitor used for feedback about operation.</param>
		/// <returns>result of push operation with complete status description.</returns>
		/// <exception cref="System.NotSupportedException">when push operation is not supported by provided transport.
		/// 	</exception>
		/// <exception cref="NGit.Errors.TransportException">
		/// when some error occurred during operation, like I/O, protocol
		/// error, or local database consistency error.
		/// </exception>
		internal virtual PushResult Execute(ProgressMonitor monitor)
		{
			try
			{
				monitor.BeginTask(PROGRESS_OPENING_CONNECTION, ProgressMonitor.UNKNOWN);
				PushResult res = new PushResult();
				connection = transport.OpenPush();
				try
				{
					res.SetAdvertisedRefs(transport.GetURI(), connection.GetRefsMap());
					res.SetRemoteUpdates(toPush);
					monitor.EndTask();
					IDictionary<string, RemoteRefUpdate> preprocessed = PrepareRemoteUpdates();
					if (transport.IsDryRun())
					{
						ModifyUpdatesForDryRun();
					}
					else
					{
						if (!preprocessed.IsEmpty())
						{
							connection.Push(monitor, preprocessed);
						}
					}
				}
				finally
				{
					connection.Close();
					res.AddMessages(connection.GetMessages());
				}
				if (!transport.IsDryRun())
				{
					UpdateTrackingRefs();
				}
				foreach (RemoteRefUpdate rru in toPush.Values)
				{
					TrackingRefUpdate tru = rru.GetTrackingRefUpdate();
					if (tru != null)
					{
						res.Add(tru);
					}
				}
				return res;
			}
			finally
			{
				walker.Release();
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private IDictionary<string, RemoteRefUpdate> PrepareRemoteUpdates()
		{
			IDictionary<string, RemoteRefUpdate> result = new Dictionary<string, RemoteRefUpdate
				>();
			foreach (RemoteRefUpdate rru in toPush.Values)
			{
				Ref advertisedRef = connection.GetRef(rru.GetRemoteName());
				ObjectId advertisedOld = (advertisedRef == null ? ObjectId.ZeroId : advertisedRef
					.GetObjectId());
				if (rru.GetNewObjectId().Equals(advertisedOld))
				{
					if (rru.IsDelete())
					{
						// ref does exist neither locally nor remotely
						rru.SetStatus(RemoteRefUpdate.Status.NON_EXISTING);
					}
					else
					{
						// same object - nothing to do
						rru.SetStatus(RemoteRefUpdate.Status.UP_TO_DATE);
					}
					continue;
				}
				// caller has explicitly specified expected old object id, while it
				// has been changed in the mean time - reject
				if (rru.IsExpectingOldObjectId() && !rru.GetExpectedOldObjectId().Equals(advertisedOld
					))
				{
					rru.SetStatus(RemoteRefUpdate.Status.REJECTED_REMOTE_CHANGED);
					continue;
				}
				// create ref (hasn't existed on remote side) and delete ref
				// are always fast-forward commands, feasible at this level
				if (advertisedOld.Equals(ObjectId.ZeroId) || rru.IsDelete())
				{
					rru.SetFastForward(true);
					result.Put(rru.GetRemoteName(), rru);
					continue;
				}
				// check for fast-forward:
				// - both old and new ref must point to commits, AND
				// - both of them must be known for us, exist in repository, AND
				// - old commit must be ancestor of new commit
				bool fastForward = true;
				try
				{
					RevObject oldRev = walker.ParseAny(advertisedOld);
					RevObject newRev = walker.ParseAny(rru.GetNewObjectId());
					if (!(oldRev is RevCommit) || !(newRev is RevCommit) || !walker.IsMergedInto((RevCommit
						)oldRev, (RevCommit)newRev))
					{
						fastForward = false;
					}
				}
				catch (MissingObjectException)
				{
					fastForward = false;
				}
				catch (Exception x)
				{
					throw new TransportException(transport.GetURI(), MessageFormat.Format(JGitText.Get
						().readingObjectsFromLocalRepositoryFailed, x.Message), x);
				}
				rru.SetFastForward(fastForward);
				if (!fastForward && !rru.IsForceUpdate())
				{
					rru.SetStatus(RemoteRefUpdate.Status.REJECTED_NONFASTFORWARD);
				}
				else
				{
					result.Put(rru.GetRemoteName(), rru);
				}
			}
			return result;
		}

		private void ModifyUpdatesForDryRun()
		{
			foreach (RemoteRefUpdate rru in toPush.Values)
			{
				if (rru.GetStatus() == RemoteRefUpdate.Status.NOT_ATTEMPTED)
				{
					rru.SetStatus(RemoteRefUpdate.Status.OK);
				}
			}
		}

		private void UpdateTrackingRefs()
		{
			foreach (RemoteRefUpdate rru in toPush.Values)
			{
				RemoteRefUpdate.Status status = rru.GetStatus();
				if (rru.HasTrackingRefUpdate() && (status == RemoteRefUpdate.Status.UP_TO_DATE ||
					 status == RemoteRefUpdate.Status.OK))
				{
					// update local tracking branch only when there is a chance that
					// it has changed; this is possible for:
					// -updated (OK) status,
					// -up to date (UP_TO_DATE) status
					try
					{
						rru.UpdateTrackingRef(walker);
					}
					catch (IOException)
					{
					}
				}
			}
		}
		// ignore as RefUpdate has stored I/O error status
	}
}
