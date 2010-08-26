/*
 * Copyright (C) 2008, Google Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
	/// <summary>
	/// Basic daemon for the anonymous <code>git://</code> transport protocol.
	/// </summary>
	public class Daemon
	{
		public const int DEFAULT_PORT = 9418;
		private const int BACKLOG = 5;

		public IPEndPoint MyAddress { get; private set; }
		public DaemonService[] Services { get; private set; }
		public Dictionary<string, Thread> Processors { get; private set; }
		public bool ExportAll { get; set; }
		public Dictionary<string, Repository> Exports { get; private set; }
		public ICollection<DirectoryInfo> ExportBase { get; private set; }
		public bool Run { get; private set; }

		private Thread acceptThread;
		
		private Object locker = new Object();

		/// <summary>
		///  Configure a daemon to listen on any available network port.
		/// </summary>
		public Daemon()
			: this(null)
		{
		}

		///	<summary>
		/// Configure a new daemon for the specified network address.
		///	</summary>
		///	<param name="addr">
		/// Address to listen for connections on. If null, any available
		/// port will be chosen on all network interfaces.
		/// </param>
		public Daemon(IPEndPoint addr)
		{
			MyAddress = addr;
			Exports = new Dictionary<string, Repository>();
			ExportBase = new List<DirectoryInfo>();
			Processors = new Dictionary<string, Thread>();
			Services = new DaemonService[] { new UploadPackService(), new ReceivePackService() };
		}

		///	<summary> * Lookup a supported service so it can be reconfigured.
		///	</summary>
		///	<param name="name">
		///	Name of the service; e.g. "receive-pack"/"git-receive-pack" or
		///	"upload-pack"/"git-upload-pack".
		/// </param>
		///	<returns>
		/// The service; null if this daemon implementation doesn't support
		///	the requested service type.
		/// </returns>
		public DaemonService GetService(string name)
		{
			lock(locker)
			{
				if (!name.StartsWith("git-"))
					name = "git-" + name;
				foreach (DaemonService s in Services)
				{
					if (s.Command.Equals(name))
						return s;
				}
				return null;
			}
		}

		///	<summary>
		/// Add a single repository to the set that is exported by this daemon.
		///	<para />
		///	The existence (or lack-thereof) of <code>git-daemon-export-ok</code> is
		///	ignored by this method. The repository is always published.
		///	</summary>
		///	<param name="name">
		/// name the repository will be published under.
		/// </param>
		///	<param name="db">the repository instance. </param>
		public void ExportRepository(string name, Repository db)
		{
            if (!name.EndsWith(Constants.DOT_GIT_EXT))
                name = name + Constants.DOT_GIT_EXT;
			Exports.Add(name, db);
			RepositoryCache.register(db);
		}

		/// <summary>
		/// Recursively export all Git repositories within a directory.
		/// </summary>
		/// <param name="dir">
		/// the directory to export. This directory must not itself be a
		/// git repository, but any directory below it which has a file
		/// named <code>git-daemon-export-ok</code> will be published.
		/// </param>
		public void ExportDirectory(DirectoryInfo dir)
		{
			ExportBase.Add(dir);
		}

		///	<summary>
		/// Start this daemon on a background thread.
		///	</summary>
		///	<exception cref="IOException">
		/// the server socket could not be opened.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// the daemon is already running.
		/// </exception>
		public void Start()
		{
			if (acceptThread != null)
			{
				throw new InvalidOperationException("Daemon already running");
			}

			var listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listenSock.Bind(MyAddress ?? new IPEndPoint(IPAddress.Any, 0));
			listenSock.Listen(BACKLOG);
			MyAddress = (IPEndPoint)listenSock.LocalEndPoint;

			Run = true;
			acceptThread = new Thread(new ThreadStart(delegate
														  {
															  while (Run)
															  {
																  try
																  {
																	  startClient(listenSock.Accept());
																  }
																  catch (ThreadInterruptedException)
																  {

																  }
																  catch (SocketException)
																  {
																	  break;
																  }
															  }

															  try
															  {
																  listenSock.Close();
															  }
															  catch (SocketException)
															  {

															  }
															  finally
															  {
																  lock (this)
																  {
																	  acceptThread = null;
																  }
															  }
														  }));
			acceptThread.Start();
		}

		/// <returns>
		/// true if this daemon is receiving connections.
		/// </returns>
		public virtual bool isRunning()
		{
			lock(locker)
			{
				return Run;
			}
		}

		private void startClient(Socket s)
		{
			var dc = new DaemonClient(this) { Peer = s.RemoteEndPoint };

			// [caytchen] TODO: insanse anonymous methods were ported 1:1 from jgit, do properly sometime
			var t = new Thread(
				new ThreadStart(delegate
									{
										using(NetworkStream stream = new NetworkStream(s))
										{
											try
											{
												dc.Execute(new BufferedStream(stream));
											}
											catch (IOException)
											{
											}
											catch (SocketException)
											{
											}
											finally
											{
												try
												{
													s.Close();
												}
												catch (IOException)
												{
												}
												catch (SocketException)
												{
												}
											}
										}
									}));

			t.Start();
			Processors.Add("Git-Daemon-Client " + s.RemoteEndPoint, t);
		}

		public DaemonService MatchService(string cmd)
		{
			foreach (DaemonService d in Services)
			{
				if (d.Handles(cmd))
					return d;
			}
			return null;
		}

		/// <summary>
		/// Stop this daemon.
		/// </summary>
		public void Stop()
		{
			if (acceptThread != null)
			{
				Run = false;
				// [caytchen] behaviour probably doesn't match
				//acceptThread.Interrupt();
			}
		}

		public Repository OpenRepository(string name)
		{
			// Assume any attempt to use \ was by a Windows client
			// and correct to the more typical / used in Git URIs.
			//
			name = name.Replace('\\', '/');

			// git://thishost/path should always be name="/path" here
			//
			if (!name.StartsWith("/")) return null;

			// Forbid Windows UNC paths as they might escape the base
			//
			if (name.StartsWith("//")) return null;

			// Forbid funny paths which contain an up-reference, they
			// might be trying to escape and read /../etc/password.
			//
			if (name.Contains("/../")) return null;

			name = name.Substring(1);

			Repository db = Exports[name];
			if (db != null) return db;
            db = Exports[name + Constants.DOT_GIT_EXT];
			if (db != null) return db;

			DirectoryInfo[] search = ExportBase.ToArray();
			foreach (DirectoryInfo f in search)
			{
				string p = f.ToString();
				if (!p.EndsWith("/")) p = p + '/';

				db = OpenRepository(new DirectoryInfo(p + name));
				if (db != null) return db;

                db = OpenRepository(new DirectoryInfo(p + name + Constants.DOT_GIT_EXT));
				if (db != null) return db;

                db = OpenRepository(new DirectoryInfo(p + name + "/" + Constants.DOT_GIT));
				if (db != null) return db;
			}
			return null;
		}

		private Repository OpenRepository(DirectoryInfo f)
		{
			if (Directory.Exists(f.ToString()) && CanExport(f))
			{
				try
				{
					return new Repository(f);
				}
				catch (IOException)
				{
				}
			}
			return null;
		}

		private bool CanExport(DirectoryInfo d)
		{
			if (ExportAll) return true;
			string p = d.ToString();
			if (!p.EndsWith("/")) p = p + '/';
			return File.Exists(p + "git-daemon-export-ok");
		}

		#region Nested Types

		// [caytchen] note these two were actually done anonymously in the original jgit
		class UploadPackService : DaemonService
		{
			public UploadPackService()
				: base("upload-pack", "uploadpack")
			{
				Enabled = true;
			}

			public override void Execute(DaemonClient client, Repository db)
			{
				var rp = new UploadPack(db);
				Stream stream = client.Stream;
				rp.Upload(stream, null, null);
			}
		}

		class ReceivePackService : DaemonService
		{
			public ReceivePackService()
				: base("receive-pack", "receivepack")
			{
				Enabled = false;
			}

			public override void Execute(DaemonClient client, Repository db)
			{
				EndPoint peer = client.Peer;

				var ipEndpoint = peer as IPEndPoint;
				if (ipEndpoint == null)
				{
					throw new InvalidOperationException("peer must be a IPEndPoint");
				}

				string host = Dns.GetHostEntry(ipEndpoint.Address).HostName ?? ipEndpoint.Address.ToString();
				var rp = new ReceivePack(db);
				Stream stream = client.Stream;
				const string name = "anonymous";
				string email = name + "@" + host;
				rp.setRefLogIdent(new PersonIdent(name, email));
				rp.receive(stream, stream, null);
			}
		}

		#endregion
	}
}