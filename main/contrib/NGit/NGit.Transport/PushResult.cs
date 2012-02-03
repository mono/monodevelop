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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Result of push operation to the remote repository.</summary>
	/// <remarks>
	/// Result of push operation to the remote repository. Holding information of
	/// <see cref="OperationResult">OperationResult</see>
	/// and remote refs updates status.
	/// </remarks>
	/// <seealso cref="Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E})
	/// 	">Transport.Push(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;)
	/// 	</seealso>
	public class PushResult : OperationResult
	{
		private IDictionary<string, RemoteRefUpdate> remoteUpdates = Sharpen.Collections.
			EmptyMap<string, RemoteRefUpdate>();

		/// <summary>Get status of remote refs updates.</summary>
		/// <remarks>
		/// Get status of remote refs updates. Together with
		/// <see cref="OperationResult.GetAdvertisedRefs()">OperationResult.GetAdvertisedRefs()
		/// 	</see>
		/// it provides full description/status of each
		/// ref update.
		/// <p>
		/// Returned collection is not sorted in any order.
		/// </p>
		/// </remarks>
		/// <returns>collection of remote refs updates</returns>
		public virtual ICollection<RemoteRefUpdate> GetRemoteUpdates()
		{
			return Sharpen.Collections.UnmodifiableCollection(remoteUpdates.Values);
		}

		/// <summary>Get status of specific remote ref update by remote ref name.</summary>
		/// <remarks>
		/// Get status of specific remote ref update by remote ref name. Together
		/// with
		/// <see cref="OperationResult.GetAdvertisedRef(string)">OperationResult.GetAdvertisedRef(string)
		/// 	</see>
		/// it provide full description/status
		/// of this ref update.
		/// </remarks>
		/// <param name="refName">remote ref name</param>
		/// <returns>status of remote ref update</returns>
		public virtual RemoteRefUpdate GetRemoteUpdate(string refName)
		{
			return remoteUpdates.Get(refName);
		}

		internal virtual void SetRemoteUpdates(IDictionary<string, RemoteRefUpdate> remoteUpdates
			)
		{
			this.remoteUpdates = remoteUpdates;
		}
	}
}
