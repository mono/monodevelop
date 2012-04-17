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
using NGit;
using NGit.Internal;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>The base class for transports that use SSH protocol.</summary>
	/// <remarks>
	/// The base class for transports that use SSH protocol. This class allows
	/// customizing SSH connection settings.
	/// </remarks>
	public abstract class SshTransport : TcpTransport
	{
		private SshSessionFactory sch;

		/// <summary>The open SSH session</summary>
		private RemoteSession sock;

		/// <summary>Create a new transport instance.</summary>
		/// <remarks>Create a new transport instance.</remarks>
		/// <param name="local">
		/// the repository this instance will fetch into, or push out of.
		/// This must be the repository passed to
		/// <see cref="Transport.Open(NGit.Repository, URIish)">Transport.Open(NGit.Repository, URIish)
		/// 	</see>
		/// .
		/// </param>
		/// <param name="uri">
		/// the URI used to access the remote repository. This must be the
		/// URI passed to
		/// <see cref="Transport.Open(NGit.Repository, URIish)">Transport.Open(NGit.Repository, URIish)
		/// 	</see>
		/// .
		/// </param>
		protected internal SshTransport(Repository local, URIish uri) : base(local, uri)
		{
			sch = SshSessionFactory.GetInstance();
		}

		/// <summary>
		/// Set SSH session factory instead of the default one for this instance of
		/// the transport.
		/// </summary>
		/// <remarks>
		/// Set SSH session factory instead of the default one for this instance of
		/// the transport.
		/// </remarks>
		/// <param name="factory">a factory to set, must not be null</param>
		/// <exception cref="System.InvalidOperationException">if session has been already created.
		/// 	</exception>
		public virtual void SetSshSessionFactory(SshSessionFactory factory)
		{
			if (factory == null)
			{
				throw new ArgumentNullException(JGitText.Get().theFactoryMustNotBeNull);
			}
			if (sock != null)
			{
				throw new InvalidOperationException(JGitText.Get().anSSHSessionHasBeenAlreadyCreated
					);
			}
			sch = factory;
		}

		/// <returns>the SSH session factory that will be used for creating SSH sessions</returns>
		public virtual SshSessionFactory GetSshSessionFactory()
		{
			return sch;
		}

		/// <summary>Get the default SSH session</summary>
		/// <returns>a remote session</returns>
		/// <exception cref="NGit.Errors.TransportException">in case of error with opening SSH session
		/// 	</exception>
		protected internal virtual RemoteSession GetSession()
		{
			if (sock != null)
			{
				return sock;
			}
			int tms = GetTimeout() > 0 ? GetTimeout() * 1000 : 0;
			sock = sch.GetSession(uri, GetCredentialsProvider(), local.FileSystem, tms);
			return sock;
		}

		public override void Close()
		{
			if (sock != null)
			{
				try
				{
					sch.ReleaseSession(sock);
				}
				finally
				{
					sock = null;
				}
			}
		}
	}
}
