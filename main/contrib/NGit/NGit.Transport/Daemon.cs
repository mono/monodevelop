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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NGit;
using NGit.Errors;
using NGit.Storage.Pack;
using NGit.Transport;
using NGit.Transport.Resolver;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Basic daemon for the anonymous <code>git://</code> transport protocol.</summary>
	/// <remarks>Basic daemon for the anonymous <code>git://</code> transport protocol.</remarks>
	public class Daemon
	{
		/// <summary>9418: IANA assigned port number for Git.</summary>
		/// <remarks>9418: IANA assigned port number for Git.</remarks>
		public const int DEFAULT_PORT = 9418;

		private const int BACKLOG = 5;

		private IPEndPoint myAddress;

		private readonly DaemonService[] services;

		private readonly ThreadGroup processors;

		private bool run;

		private Sharpen.Thread acceptThread;

		private int timeout;

		private PackConfig packConfig;

		private volatile RepositoryResolver<DaemonClient> repositoryResolver;

		private volatile UploadPackFactory<DaemonClient> uploadPackFactory;

		private volatile ReceivePackFactory<DaemonClient> receivePackFactory;

		/// <summary>Configure a daemon to listen on any available network port.</summary>
		/// <remarks>Configure a daemon to listen on any available network port.</remarks>
		public Daemon() : this(null)
		{
		}

		/// <summary>Configure a new daemon for the specified network address.</summary>
		/// <remarks>Configure a new daemon for the specified network address.</remarks>
		/// <param name="addr">
		/// address to listen for connections on. If null, any available
		/// port will be chosen on all network interfaces.
		/// </param>
		public Daemon(IPEndPoint addr)
		{
			myAddress = addr;
			processors = new ThreadGroup("Git-Daemon");
			repositoryResolver = RepositoryResolver<DaemonClient>.NONE;
			uploadPackFactory = new _UploadPackFactory_112(this);
			receivePackFactory = new _ReceivePackFactory_123(this);
			services = new DaemonService[] { new _DaemonService_143(this, "upload-pack", "uploadpack"
				), new _DaemonService_158(this, "receive-pack", "receivepack") };
		}

		private sealed class _UploadPackFactory_112 : UploadPackFactory<DaemonClient>
		{
			public _UploadPackFactory_112(Daemon _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
			public override UploadPack Create(DaemonClient req, Repository db)
			{
				UploadPack up = new UploadPack(db);
				up.SetTimeout(this._enclosing.GetTimeout());
				up.SetPackConfig(this._enclosing.GetPackConfig());
				return up;
			}

			private readonly Daemon _enclosing;
		}

		private sealed class _ReceivePackFactory_123 : ReceivePackFactory<DaemonClient>
		{
			public _ReceivePackFactory_123(Daemon _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
			public override ReceivePack Create(DaemonClient req, Repository db)
			{
				ReceivePack rp = new ReceivePack(db);
				IPAddress peer = req.GetRemoteAddress();
				string host = peer.ToString();
				if (host == null)
				{
					host = peer.GetHostAddress();
				}
				string name = "anonymous";
				string email = name + "@" + host;
				rp.SetRefLogIdent(new PersonIdent(name, email));
				rp.SetTimeout(this._enclosing.GetTimeout());
				return rp;
			}

			private readonly Daemon _enclosing;
		}

		private sealed class _DaemonService_143 : DaemonService
		{
			public _DaemonService_143(Daemon _enclosing, string baseArg1, string baseArg2) : 
				base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				{
					this.SetEnabled(true);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
			internal override void Execute(DaemonClient dc, Repository db)
			{
				UploadPack up = this._enclosing.uploadPackFactory.Create(dc, db);
				InputStream @in = dc.GetInputStream();
				OutputStream @out = dc.GetOutputStream();
				up.Upload(@in, @out, null);
			}

			private readonly Daemon _enclosing;
		}

		private sealed class _DaemonService_158 : DaemonService
		{
			public _DaemonService_158(Daemon _enclosing, string baseArg1, string baseArg2) : 
				base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				{
					this.SetEnabled(false);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
			/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
			internal override void Execute(DaemonClient dc, Repository db)
			{
				ReceivePack rp = this._enclosing.receivePackFactory.Create(dc, db);
				InputStream @in = dc.GetInputStream();
				OutputStream @out = dc.GetOutputStream();
				rp.Receive(@in, @out, null);
			}

			private readonly Daemon _enclosing;
		}

		/// <returns>the address connections are received on.</returns>
		public virtual IPEndPoint GetAddress()
		{
			lock (this)
			{
				return myAddress;
			}
		}

		/// <summary>Lookup a supported service so it can be reconfigured.</summary>
		/// <remarks>Lookup a supported service so it can be reconfigured.</remarks>
		/// <param name="name">
		/// name of the service; e.g. "receive-pack"/"git-receive-pack" or
		/// "upload-pack"/"git-upload-pack".
		/// </param>
		/// <returns>
		/// the service; null if this daemon implementation doesn't support
		/// the requested service type.
		/// </returns>
		public virtual DaemonService GetService(string name)
		{
			lock (this)
			{
				if (!name.StartsWith("git-"))
				{
					name = "git-" + name;
				}
				foreach (DaemonService s in services)
				{
					if (s.GetCommandName().Equals(name))
					{
						return s;
					}
				}
				return null;
			}
		}

		/// <returns>timeout (in seconds) before aborting an IO operation.</returns>
		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <summary>Set the timeout before willing to abort an IO call.</summary>
		/// <remarks>Set the timeout before willing to abort an IO call.</remarks>
		/// <param name="seconds">
		/// number of seconds to wait (with no data transfer occurring)
		/// before aborting an IO read or write operation with the
		/// connected client.
		/// </param>
		public virtual void SetTimeout(int seconds)
		{
			timeout = seconds;
		}

		/// <returns>configuration controlling packing, may be null.</returns>
		public virtual PackConfig GetPackConfig()
		{
			return packConfig;
		}

		/// <summary>Set the configuration used by the pack generator.</summary>
		/// <remarks>Set the configuration used by the pack generator.</remarks>
		/// <param name="pc">
		/// configuration controlling packing parameters. If null the
		/// source repository's settings will be used.
		/// </param>
		public virtual void SetPackConfig(PackConfig pc)
		{
			this.packConfig = pc;
		}

		/// <summary>Set the resolver used to locate a repository by name.</summary>
		/// <remarks>Set the resolver used to locate a repository by name.</remarks>
		/// <param name="resolver">the resolver instance.</param>
		public virtual void SetRepositoryResolver(RepositoryResolver<DaemonClient> resolver
			)
		{
			repositoryResolver = resolver;
		}

		/// <summary>Set the factory to construct and configure per-request UploadPack.</summary>
		/// <remarks>Set the factory to construct and configure per-request UploadPack.</remarks>
		/// <param name="factory">the factory. If null upload-pack is disabled.</param>
		public virtual void SetUploadPackFactory(UploadPackFactory<DaemonClient> factory)
		{
			if (factory != null)
			{
				uploadPackFactory = factory;
			}
			else
			{
				uploadPackFactory = UploadPackFactory<DaemonClient>.DISABLED;
			}
		}

		/// <summary>Set the factory to construct and configure per-request ReceivePack.</summary>
		/// <remarks>Set the factory to construct and configure per-request ReceivePack.</remarks>
		/// <param name="factory">the factory. If null receive-pack is disabled.</param>
		public virtual void SetReceivePackFactory(ReceivePackFactory<DaemonClient> factory
			)
		{
			if (factory != null)
			{
				receivePackFactory = factory;
			}
			else
			{
				receivePackFactory = ReceivePackFactory<DaemonClient>.DISABLED;
			}
		}

		/// <summary>Start this daemon on a background thread.</summary>
		/// <remarks>Start this daemon on a background thread.</remarks>
		/// <exception cref="System.IO.IOException">the server socket could not be opened.</exception>
		/// <exception cref="System.InvalidOperationException">the daemon is already running.
		/// 	</exception>
		public virtual void Start()
		{
			lock (this)
			{
				if (acceptThread != null)
				{
					throw new InvalidOperationException(JGitText.Get().daemonAlreadyRunning);
				}
				Socket listenSock = Sharpen.Extensions.CreateServerSocket(myAddress != null ? myAddress
					.Port : 0, BACKLOG, myAddress != null ? myAddress.Address : null);
				myAddress = (IPEndPoint)listenSock.LocalEndPoint;
				run = true;
				acceptThread = new _Thread_289(this, listenSock, processors, "Git-Daemon-Accept");
				// Test again to see if we should keep accepting.
				//
				acceptThread.Start();
			}
		}

		private sealed class _Thread_289 : Sharpen.Thread
		{
			public _Thread_289(Daemon _enclosing, Socket listenSock, ThreadGroup baseArg1, string
				 baseArg2) : base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				this.listenSock = listenSock;
			}

			public override void Run()
			{
				while (this._enclosing.IsRunning())
				{
					try
					{
						this._enclosing.StartClient(listenSock.Accept());
					}
					catch (ThreadInterruptedException)
					{
					}
					catch (IOException)
					{
						break;
					}
				}
				try
				{
					listenSock.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					lock (this._enclosing)
					{
						this._enclosing.acceptThread = null;
					}
				}
			}

			private readonly Daemon _enclosing;

			private readonly Socket listenSock;
		}

		/// <returns>true if this daemon is receiving connections.</returns>
		public virtual bool IsRunning()
		{
			lock (this)
			{
				return run;
			}
		}

		/// <summary>Stop this daemon.</summary>
		/// <remarks>Stop this daemon.</remarks>
		public virtual void Stop()
		{
			lock (this)
			{
				if (acceptThread != null)
				{
					run = false;
					acceptThread.Interrupt();
				}
			}
		}

		private void StartClient(Socket s)
		{
			DaemonClient dc = new DaemonClient(this);
			EndPoint peer = s.RemoteEndPoint;
			if (peer is IPEndPoint)
			{
				dc.SetRemoteAddress(((IPEndPoint)peer).Address);
			}
			new _Thread_335(dc, s, processors, "Git-Daemon-Client " + peer.ToString()).Start(
				);
		}

		private sealed class _Thread_335 : Sharpen.Thread
		{
			public _Thread_335(DaemonClient dc, Socket s, ThreadGroup baseArg1, string baseArg2
				) : base(baseArg1, baseArg2)
			{
				this.dc = dc;
				this.s = s;
			}

			public override void Run()
			{
				try
				{
					dc.Execute(s);
				}
				catch (RepositoryNotFoundException)
				{
				}
				catch (ServiceNotEnabledException)
				{
				}
				catch (ServiceNotAuthorizedException)
				{
				}
				catch (IOException e)
				{
					// Ignored. Client cannot use this repository.
					// Ignored. Client cannot use this repository.
					// Ignored. Client cannot use this repository.
					// Ignore unexpected IO exceptions from clients
					Sharpen.Runtime.PrintStackTrace(e);
				}
				finally
				{
					try
					{
						s.GetInputStream().Close();
					}
					catch (IOException)
					{
					}
					// Ignore close exceptions
					try
					{
						s.GetOutputStream().Close();
					}
					catch (IOException)
					{
					}
				}
			}

			private readonly DaemonClient dc;

			private readonly Socket s;
		}

		// Ignore close exceptions
		internal virtual DaemonService MatchService(string cmd)
		{
			lock (this)
			{
				foreach (DaemonService d in services)
				{
					if (d.Handles(cmd))
					{
						return d;
					}
				}
				return null;
			}
		}

		internal virtual Repository OpenRepository(DaemonClient client, string name)
		{
			// Assume any attempt to use \ was by a Windows client
			// and correct to the more typical / used in Git URIs.
			//
			name = name.Replace('\\', '/');
			// git://thishost/path should always be name="/path" here
			//
			if (!name.StartsWith("/"))
			{
				return null;
			}
			try
			{
				return repositoryResolver.Open(client, Sharpen.Runtime.Substring(name, 1));
			}
			catch (RepositoryNotFoundException)
			{
				// null signals it "wasn't found", which is all that is suitable
				// for the remote client to know.
				return null;
			}
			catch (ServiceNotAuthorizedException)
			{
				// null signals it "wasn't found", which is all that is suitable
				// for the remote client to know.
				return null;
			}
			catch (ServiceNotEnabledException)
			{
				// null signals it "wasn't found", which is all that is suitable
				// for the remote client to know.
				return null;
			}
		}
	}
}
