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

using System.Net;
using System.Net.Sockets;
using NGit.Transport;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Active network client of
	/// <see cref="Daemon">Daemon</see>
	/// .
	/// </summary>
	public class DaemonClient
	{
		private readonly Daemon daemon;

		private IPAddress peer;

		private InputStream rawIn;

		private OutputStream rawOut;

		internal DaemonClient(Daemon d)
		{
			daemon = d;
		}

		internal virtual void SetRemoteAddress(IPAddress ia)
		{
			peer = ia;
		}

		/// <returns>the daemon which spawned this client.</returns>
		public virtual Daemon GetDaemon()
		{
			return daemon;
		}

		/// <returns>Internet address of the remote client.</returns>
		public virtual IPAddress GetRemoteAddress()
		{
			return peer;
		}

		/// <returns>input stream to read from the connected client.</returns>
		public virtual InputStream GetInputStream()
		{
			return rawIn;
		}

		/// <returns>output stream to send data to the connected client.</returns>
		public virtual OutputStream GetOutputStream()
		{
			return rawOut;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
		internal virtual void Execute(Socket sock)
		{
			rawIn = new BufferedInputStream(sock.GetInputStream());
			rawOut = new SafeBufferedOutputStream(sock.GetOutputStream());
			if (0 < daemon.GetTimeout())
			{
				sock.ReceiveTimeout = daemon.GetTimeout() * 1000;
			}
			string cmd = new PacketLineIn(rawIn).ReadStringRaw();
			int nul = cmd.IndexOf('\0');
			if (nul >= 0)
			{
				// Newer clients hide a "host" header behind this byte.
				// Currently we don't use it for anything, so we ignore
				// this portion of the command.
				//
				cmd = Sharpen.Runtime.Substring(cmd, 0, nul);
			}
			DaemonService srv = GetDaemon().MatchService(cmd);
			if (srv == null)
			{
				return;
			}
			sock.ReceiveTimeout = 0;
			srv.Execute(this, cmd);
		}
	}
}
