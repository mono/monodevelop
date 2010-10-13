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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NGit;
using NGit.Storage.Pack;
using NGit.Transport;
using NGit.Util;
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

		private volatile bool exportAll;

		private IDictionary<string, Repository> exports;

		private ICollection<FilePath> exportBase;

		private bool run;

		private Sharpen.Thread acceptThread;

		private int timeout;

		private PackConfig packConfig;

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
			exports = new ConcurrentHashMap<string, Repository>();
			exportBase = new CopyOnWriteArrayList<FilePath>();
			processors = new ThreadGroup("Git-Daemon");
			services = new DaemonService[] { new _DaemonService_115(this, "upload-pack", "uploadpack"
				), new _DaemonService_129(this, "receive-pack", "receivepack") };
		}

		private sealed class _DaemonService_115 : DaemonService
		{
			public _DaemonService_115(Daemon _enclosing, string baseArg1, string baseArg2) : 
				base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				{
					this.SetEnabled(true);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void Execute(DaemonClient dc, Repository db)
			{
				UploadPack rp = new UploadPack(db);
				InputStream @in = dc.GetInputStream();
				rp.SetTimeout(this._enclosing.GetTimeout());
				rp.SetPackConfig(this._enclosing.packConfig);
				rp.Upload(@in, dc.GetOutputStream(), null);
			}

			private readonly Daemon _enclosing;
		}

		private sealed class _DaemonService_129 : DaemonService
		{
			public _DaemonService_129(Daemon _enclosing, string baseArg1, string baseArg2) : 
				base(baseArg1, baseArg2)
			{
				this._enclosing = _enclosing;
				{
					this.SetEnabled(false);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void Execute(DaemonClient dc, Repository db)
			{
				IPAddress peer = dc.GetRemoteAddress();
				string host = peer.ToString();
				if (host == null)
				{
					host = peer.GetHostAddress();
				}
				ReceivePack rp = new ReceivePack(db);
				InputStream @in = dc.GetInputStream();
				string name = "anonymous";
				string email = name + "@" + host;
				rp.SetRefLogIdent(new PersonIdent(name, email));
				rp.SetTimeout(this._enclosing.GetTimeout());
				rp.Receive(@in, dc.GetOutputStream(), null);
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

		/// <returns>
		/// false if <code>git-daemon-export-ok</code> is required to export
		/// a repository; true if <code>git-daemon-export-ok</code> is
		/// ignored.
		/// </returns>
		/// <seealso cref="SetExportAll(bool)">SetExportAll(bool)</seealso>
		public virtual bool IsExportAll()
		{
			return exportAll;
		}

		/// <summary>Set whether or not to export all repositories.</summary>
		/// <remarks>
		/// Set whether or not to export all repositories.
		/// <p>
		/// If false (the default), repositories must have a
		/// <code>git-daemon-export-ok</code> file to be accessed through this
		/// daemon.
		/// <p>
		/// If true, all repositories are available through the daemon, whether or
		/// not <code>git-daemon-export-ok</code> exists.
		/// </remarks>
		/// <param name="export"></param>
		public virtual void SetExportAll(bool export)
		{
			exportAll = export;
		}

		/// <summary>Add a single repository to the set that is exported by this daemon.</summary>
		/// <remarks>
		/// Add a single repository to the set that is exported by this daemon.
		/// <p>
		/// The existence (or lack-thereof) of <code>git-daemon-export-ok</code> is
		/// ignored by this method. The repository is always published.
		/// </remarks>
		/// <param name="name">name the repository will be published under.</param>
		/// <param name="db">the repository instance.</param>
		public virtual void ExportRepository(string name, Repository db)
		{
			if (!name.EndsWith(Constants.DOT_GIT_EXT))
			{
				name = name + Constants.DOT_GIT_EXT;
			}
			exports.Put(name, db);
			RepositoryCache.Register(db);
		}

		/// <summary>Recursively export all Git repositories within a directory.</summary>
		/// <remarks>Recursively export all Git repositories within a directory.</remarks>
		/// <param name="dir">
		/// the directory to export. This directory must not itself be a
		/// git repository, but any directory below it which has a file
		/// named <code>git-daemon-export-ok</code> will be published.
		/// </param>
		public virtual void ExportDirectory(FilePath dir)
		{
			exportBase.AddItem(dir);
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
				acceptThread = new _Thread_278(this, listenSock, processors, "Git-Daemon-Accept");
				// Test again to see if we should keep accepting.
				//
				acceptThread.Start();
			}
		}

		private sealed class _Thread_278 : Sharpen.Thread
		{
			public _Thread_278(Daemon _enclosing, Socket listenSock, ThreadGroup baseArg1, string
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
			new _Thread_324(dc, s, processors, "Git-Daemon-Client " + peer.ToString()).Start(
				);
		}

		private sealed class _Thread_324 : Sharpen.Thread
		{
			public _Thread_324(DaemonClient dc, Socket s, ThreadGroup baseArg1, string baseArg2
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
				catch (IOException e)
				{
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

		internal virtual Repository OpenRepository(string name)
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
			// Forbid Windows UNC paths as they might escape the base
			//
			if (name.StartsWith("//"))
			{
				return null;
			}
			// Forbid funny paths which contain an up-reference, they
			// might be trying to escape and read /../etc/password.
			//
			if (name.Contains("/../"))
			{
				return null;
			}
			name = Sharpen.Runtime.Substring(name, 1);
			Repository db;
			db = exports.Get(name.EndsWith(Constants.DOT_GIT_EXT) ? name : name + Constants.DOT_GIT_EXT
				);
			if (db != null)
			{
				db.IncrementOpen();
				return db;
			}
			foreach (FilePath baseDir in exportBase)
			{
				FilePath gitdir = RepositoryCache.FileKey.Resolve(new FilePath(baseDir, name), FS
					.DETECTED);
				if (gitdir != null && CanExport(gitdir))
				{
					return OpenRepository(gitdir);
				}
			}
			return null;
		}

		private static Repository OpenRepository(FilePath gitdir)
		{
			try
			{
				return RepositoryCache.Open(RepositoryCache.FileKey.Exact(gitdir, FS.DETECTED));
			}
			catch (IOException)
			{
				// null signals it "wasn't found", which is all that is suitable
				// for the remote client to know.
				return null;
			}
		}

		private bool CanExport(FilePath d)
		{
			if (IsExportAll())
			{
				return true;
			}
			return new FilePath(d, "git-daemon-export-ok").Exists();
		}
	}
}
