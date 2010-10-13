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
using NGit.Storage.File;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Base helper class for fetch connection implementations.</summary>
	/// <remarks>
	/// Base helper class for fetch connection implementations. Provides some common
	/// typical structures and methods used during fetch connection.
	/// <p>
	/// Implementors of fetch over pack-based protocols should consider using
	/// <see cref="BasePackFetchConnection">BasePackFetchConnection</see>
	/// instead.
	/// </p>
	/// </remarks>
	internal abstract class BaseFetchConnection : BaseConnection, FetchConnection
	{
		/// <exception cref="NGit.Errors.TransportException"></exception>
		public void Fetch(ProgressMonitor monitor, ICollection<Ref> want, ICollection<ObjectId
			> have)
		{
			MarkStartedOperation();
			DoFetch(monitor, want, have);
		}

		/// <summary>
		/// Default implementation of
		/// <see cref="FetchConnection.DidFetchIncludeTags()">FetchConnection.DidFetchIncludeTags()
		/// 	</see>
		/// -
		/// returning false.
		/// </summary>
		public virtual bool DidFetchIncludeTags()
		{
			return false;
		}

		/// <summary>
		/// Implementation of
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// without checking for multiple fetch.
		/// </summary>
		/// <param name="monitor">
		/// as in
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// </param>
		/// <param name="want">
		/// as in
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// </param>
		/// <param name="have">
		/// as in
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// </param>
		/// <exception cref="NGit.Errors.TransportException">
		/// as in
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// , but
		/// implementation doesn't have to care about multiple
		/// <see cref="Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection{E}, System.Collections.Generic.ICollection{E})
		/// 	">Fetch(NGit.ProgressMonitor, System.Collections.Generic.ICollection&lt;E&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// calls, as it
		/// is checked in this class.
		/// </exception>
		protected internal abstract void DoFetch(ProgressMonitor monitor, ICollection<Ref
			> want, ICollection<ObjectId> have);

		public abstract bool DidFetchTestConnectivity();

		public abstract ICollection<PackLock> GetPackLocks();

		public abstract void SetPackLockMessage(string arg1);
	}
}
