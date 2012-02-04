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
using NGit.Api;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	public interface TransportCommand
	{
		TransportCommand SetCredentialsProvider(CredentialsProvider credentialsProvider);
		TransportCommand SetTimeout(int timeout);
		TransportCommand SetTransportConfigCallback(TransportConfigCallback transportConfigCallback);
	}
	
	/// <summary>
	/// Base class for commands that use a
	/// <see cref="NGit.Transport.Transport">NGit.Transport.Transport</see>
	/// during execution.
	/// <p>
	/// This class provides standard configuration of a transport for options such as
	/// a
	/// <see cref="NGit.Transport.CredentialsProvider">NGit.Transport.CredentialsProvider
	/// 	</see>
	/// , a timeout, and a
	/// <see cref="TransportConfigCallback">TransportConfigCallback</see>
	/// .
	/// </summary>
	/// <?></?>
	/// <?></?>
	public abstract class TransportCommand<C, T> : GitCommand<T>, TransportCommand where C:GitCommand
	{
		/// <summary>Configured credentials provider</summary>
		protected internal CredentialsProvider credentialsProvider;

		/// <summary>Configured transport timeout</summary>
		protected internal int timeout;

		/// <summary>Configured callback for transport configuration</summary>
		protected internal TransportConfigCallback transportConfigCallback;

		/// <param name="repo"></param>
		protected internal TransportCommand(Repository repo) : base(repo)
		{
		}
		
		TransportCommand TransportCommand.SetCredentialsProvider(CredentialsProvider credentialsProvider)
		{
			this.credentialsProvider = credentialsProvider;
			return this;
		}

		TransportCommand TransportCommand.SetTimeout(int timeout)
		{
			this.timeout = timeout;
			return this;
		}

		TransportCommand TransportCommand.SetTransportConfigCallback(TransportConfigCallback transportConfigCallback)
		{
			this.transportConfigCallback = transportConfigCallback;
			return this;
		}
		
		/// <param name="credentialsProvider">
		/// the
		/// <see cref="NGit.Transport.CredentialsProvider">NGit.Transport.CredentialsProvider
		/// 	</see>
		/// to use
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual C SetCredentialsProvider(CredentialsProvider credentialsProvider)
		{
			this.credentialsProvider = credentialsProvider;
			return Self();
		}

		/// <param name="timeout">the timeout used for the transport step</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual C SetTimeout(int timeout)
		{
			this.timeout = timeout;
			return Self();
		}

		/// <param name="transportConfigCallback">
		/// if set, the callback will be invoked after the
		/// <see cref="NGit.Transport.Transport">NGit.Transport.Transport</see>
		/// has created, but before the
		/// <see cref="NGit.Transport.Transport">NGit.Transport.Transport</see>
		/// is used. The callback can use this
		/// opportunity to set additional type-specific configuration on
		/// the
		/// <see cref="NGit.Transport.Transport">NGit.Transport.Transport</see>
		/// instance.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual C SetTransportConfigCallback(TransportConfigCallback transportConfigCallback
			)
		{
			this.transportConfigCallback = transportConfigCallback;
			return Self();
		}

		/// <returns>
		/// 
		/// <code>this</code>
		/// 
		/// </returns>
		protected internal C Self()
		{
			return (C)(object)this;
		}

		/// <summary>
		/// Configure transport with credentials provider, timeout, and config
		/// callback
		/// </summary>
		/// <param name="transport"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		protected internal virtual C Configure(NGit.Transport.Transport transport)
		{
			if (credentialsProvider != null)
			{
				transport.SetCredentialsProvider(credentialsProvider);
			}
			transport.SetTimeout(timeout);
			if (transportConfigCallback != null)
			{
				transportConfigCallback.Configure(transport);
			}
			return Self();
		}

		/// <summary>
		/// Configure a child command with the current configuration set in
		/// <code>this</code>
		/// command
		/// </summary>
		/// <param name="childCommand"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		protected internal virtual C Configure(NGit.Api.TransportCommand childCommand)
		{
			childCommand.SetCredentialsProvider(credentialsProvider);
			childCommand.SetTimeout(timeout);
			childCommand.SetTransportConfigCallback(transportConfigCallback);
			return Self();
		}
	}
}
