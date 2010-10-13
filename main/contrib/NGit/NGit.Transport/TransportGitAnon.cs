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

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Transport through a git-daemon waiting for anonymous TCP connections.</summary>
	/// <remarks>
	/// Transport through a git-daemon waiting for anonymous TCP connections.
	/// <p>
	/// This transport supports the <code>git://</code> protocol, usually run on
	/// the IANA registered port 9418. It is a popular means for distributing open
	/// source projects, as there are no authentication or authorization overheads.
	/// </remarks>
	internal class TransportGitAnon : TcpTransport, PackTransport
	{
		internal const int GIT_PORT = Daemon.DEFAULT_PORT;

		internal static bool CanHandle(URIish uri)
		{
			return "git".Equals(uri.GetScheme());
		}

		protected internal TransportGitAnon(Repository local, URIish uri) : base(local, uri
			)
		{
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			return new TransportGitAnon.TcpFetchConnection(this);
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			return new TransportGitAnon.TcpPushConnection(this);
		}

		public override void Close()
		{
		}

		// Resources must be established per-connection.
		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal virtual Socket OpenConnection()
		{
			int tms = GetTimeout() > 0 ? GetTimeout() * 1000 : 0;
			int port = uri.GetPort() > 0 ? uri.GetPort() : GIT_PORT;
			Socket s = Sharpen.Extensions.CreateSocket ();
			try
			{
				IPAddress host = Sharpen.Extensions.GetAddressByName(uri.GetHost());
				s.Bind2(null);
				s.Connect(new IPEndPoint(host, port), tms);
			}
			catch (IOException c)
			{
				try
				{
					s.Close();
				}
				catch (IOException)
				{
				}
				// ignore a failure during close, we're already failing
				if (c is UnknownHostException)
				{
					throw new TransportException(uri, JGitText.Get().unknownHost);
				}
				if (c is ConnectException)
				{
					throw new TransportException(uri, c.Message);
				}
				throw new TransportException(uri, c.Message, c);
			}
			return s;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Service(string name, PacketLineOut pckOut)
		{
			StringBuilder cmd = new StringBuilder();
			cmd.Append(name);
			cmd.Append(' ');
			cmd.Append(uri.GetPath());
			cmd.Append('\0');
			cmd.Append("host=");
			cmd.Append(uri.GetHost());
			if (uri.GetPort() > 0 && uri.GetPort() != GIT_PORT)
			{
				cmd.Append(":");
				cmd.Append(uri.GetPort());
			}
			cmd.Append('\0');
			pckOut.WriteString(cmd.ToString());
			pckOut.Flush();
		}

		internal class TcpFetchConnection : BasePackFetchConnection
		{
			private Socket sock;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public TcpFetchConnection(TransportGitAnon _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.sock = this._enclosing.OpenConnection();
				try
				{
					InputStream sIn = this.sock.GetInputStream();
					OutputStream sOut = this.sock.GetOutputStream();
					sIn = new BufferedInputStream(sIn);
					sOut = new BufferedOutputStream(sOut);
					this.Init(sIn, sOut);
					this._enclosing.Service("git-upload-pack", this.pckOut);
				}
				catch (IOException err)
				{
					this.Close();
					throw new TransportException(this.uri, JGitText.Get().remoteHungUpUnexpectedly, err
						);
				}
				this.ReadAdvertisedRefs();
			}

			public override void Close()
			{
				base.Close();
				if (this.sock != null)
				{
					try
					{
						this.sock.Close();
					}
					catch (IOException)
					{
					}
					finally
					{
						// Ignore errors during close.
						this.sock = null;
					}
				}
			}

			private readonly TransportGitAnon _enclosing;
		}

		internal class TcpPushConnection : BasePackPushConnection
		{
			private Socket sock;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public TcpPushConnection(TransportGitAnon _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.sock = this._enclosing.OpenConnection();
				try
				{
					InputStream sIn = this.sock.GetInputStream();
					OutputStream sOut = this.sock.GetOutputStream();
					sIn = new BufferedInputStream(sIn);
					sOut = new BufferedOutputStream(sOut);
					this.Init(sIn, sOut);
					this._enclosing.Service("git-receive-pack", this.pckOut);
				}
				catch (IOException err)
				{
					this.Close();
					throw new TransportException(this.uri, JGitText.Get().remoteHungUpUnexpectedly, err
						);
				}
				this.ReadAdvertisedRefs();
			}

			public override void Close()
			{
				base.Close();
				if (this.sock != null)
				{
					try
					{
						this.sock.Close();
					}
					catch (IOException)
					{
					}
					finally
					{
						// Ignore errors during close.
						this.sock = null;
					}
				}
			}

			private readonly TransportGitAnon _enclosing;
		}
	}
}
