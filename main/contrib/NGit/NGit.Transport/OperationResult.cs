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
using System.Text;
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Class holding result of operation on remote repository.</summary>
	/// <remarks>
	/// Class holding result of operation on remote repository. This includes refs
	/// advertised by remote repo and local tracking refs updates.
	/// </remarks>
	public abstract class OperationResult
	{
		internal IDictionary<string, Ref> advertisedRefs = Sharpen.Collections.EmptyMap<string, Ref>();

		internal URIish uri;

		internal readonly SortedDictionary<string, TrackingRefUpdate> updates = new SortedDictionary
			<string, TrackingRefUpdate>();

		internal StringBuilder messageBuffer;

		/// <summary>Get the URI this result came from.</summary>
		/// <remarks>
		/// Get the URI this result came from.
		/// <p>
		/// Each transport instance connects to at most one URI at any point in time.
		/// </remarks>
		/// <returns>the URI describing the location of the remote repository.</returns>
		public virtual URIish GetURI()
		{
			return uri;
		}

		/// <summary>Get the complete list of refs advertised by the remote.</summary>
		/// <remarks>
		/// Get the complete list of refs advertised by the remote.
		/// <p>
		/// The returned refs may appear in any order. If the caller needs these to
		/// be sorted, they should be copied into a new array or List and then sorted
		/// by the caller as necessary.
		/// </remarks>
		/// <returns>
		/// available/advertised refs. Never null. Not modifiable. The
		/// collection can be empty if the remote side has no refs (it is an
		/// empty/newly created repository).
		/// </returns>
		public virtual ICollection<Ref> GetAdvertisedRefs()
		{
			return Sharpen.Collections.UnmodifiableCollection(advertisedRefs.Values);
		}

		/// <summary>Get a single advertised ref by name.</summary>
		/// <remarks>
		/// Get a single advertised ref by name.
		/// <p>
		/// The name supplied should be valid ref name. To get a peeled value for a
		/// ref (aka <code>refs/tags/v1.0^{}</code>) use the base name (without
		/// the <code>^{}</code> suffix) and look at the peeled object id.
		/// </remarks>
		/// <param name="name">name of the ref to obtain.</param>
		/// <returns>the requested ref; null if the remote did not advertise this ref.</returns>
		public Ref GetAdvertisedRef(string name)
		{
			return advertisedRefs.Get(name);
		}

		/// <summary>Get the status of all local tracking refs that were updated.</summary>
		/// <remarks>Get the status of all local tracking refs that were updated.</remarks>
		/// <returns>
		/// unmodifiable collection of local updates. Never null. Empty if
		/// there were no local tracking refs updated.
		/// </returns>
		public virtual ICollection<TrackingRefUpdate> GetTrackingRefUpdates()
		{
			return Sharpen.Collections.UnmodifiableCollection(updates.Values);
		}

		/// <summary>Get the status for a specific local tracking ref update.</summary>
		/// <remarks>Get the status for a specific local tracking ref update.</remarks>
		/// <param name="localName">name of the local ref (e.g. "refs/remotes/origin/master").
		/// 	</param>
		/// <returns>
		/// status of the local ref; null if this local ref was not touched
		/// during this operation.
		/// </returns>
		public virtual TrackingRefUpdate GetTrackingRefUpdate(string localName)
		{
			return updates.Get(localName);
		}

		internal virtual void SetAdvertisedRefs(URIish u, IDictionary<string, Ref> ar)
		{
			uri = u;
			advertisedRefs = ar;
		}

		internal virtual void Add(TrackingRefUpdate u)
		{
			updates.Put(u.GetLocalName(), u);
		}

		/// <summary>Get the additional messages, if any, returned by the remote process.</summary>
		/// <remarks>
		/// Get the additional messages, if any, returned by the remote process.
		/// <p>
		/// These messages are most likely informational or error messages, sent by
		/// the remote peer, to help the end-user correct any problems that may have
		/// prevented the operation from completing successfully. Application UIs
		/// should try to show these in an appropriate context.
		/// </remarks>
		/// <returns>
		/// the messages returned by the remote, most likely terminated by a
		/// newline (LF) character. The empty string is returned if the
		/// remote produced no additional messages.
		/// </returns>
		public virtual string GetMessages()
		{
			return messageBuffer != null ? messageBuffer.ToString() : string.Empty;
		}

		internal virtual void AddMessages(string msg)
		{
			if (msg != null && msg.Length > 0)
			{
				if (messageBuffer == null)
				{
					messageBuffer = new StringBuilder();
				}
				messageBuffer.Append(msg);
				if (!msg.EndsWith("\n"))
				{
					messageBuffer.Append('\n');
				}
			}
		}
	}
}
