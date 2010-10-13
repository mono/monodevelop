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
using System.IO;
using NGit.Transport;
using NGit.Util;
using NSch;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// The base session factory that loads known hosts and private keys from
	/// <code>$HOME/.ssh</code>.
	/// </summary>
	/// <remarks>
	/// The base session factory that loads known hosts and private keys from
	/// <code>$HOME/.ssh</code>.
	/// <p>
	/// This is the default implementation used by JGit and provides most of the
	/// compatibility necessary to match OpenSSH, a popular implementation of SSH
	/// used by C Git.
	/// <p>
	/// The factory does not provide UI behavior. Override the method
	/// <see cref="Configure(Host, NSch.Session)">Configure(Host, NSch.Session)</see>
	/// to supply appropriate
	/// <see cref="NSch.UserInfo">NSch.UserInfo</see>
	/// to the session.
	/// </remarks>
	public abstract class SshConfigSessionFactory : SshSessionFactory
	{
		private readonly IDictionary<string, JSch> byIdentityFile = new Dictionary<string
			, JSch>();

		private JSch defaultJSch;

		private OpenSshConfig config;

		/// <exception cref="NSch.JSchException"></exception>
		public override Session GetSession(string user, string pass, string host, int port
			, FS fs)
		{
			lock (this)
			{
				if (config == null)
				{
					config = OpenSshConfig.Get(fs);
				}
				OpenSshConfig.Host hc = config.Lookup(host);
				host = hc.GetHostName();
				if (port <= 0)
				{
					port = hc.GetPort();
				}
				if (user == null)
				{
					user = hc.GetUser();
				}
				Session session = CreateSession(hc, user, host, port, fs);
				if (pass != null)
				{
					session.SetPassword(pass);
				}
				string strictHostKeyCheckingPolicy = hc.GetStrictHostKeyChecking();
				if (strictHostKeyCheckingPolicy != null)
				{
					session.SetConfig("StrictHostKeyChecking", strictHostKeyCheckingPolicy);
				}
				string pauth = hc.GetPreferredAuthentications();
				if (pauth != null)
				{
					session.SetConfig("PreferredAuthentications", pauth);
				}
				Configure(hc, session);
				return session;
			}
		}

		/// <summary>Create a new JSch session for the requested address.</summary>
		/// <remarks>Create a new JSch session for the requested address.</remarks>
		/// <param name="hc">host configuration</param>
		/// <param name="user">login to authenticate as.</param>
		/// <param name="host">server name to connect to.</param>
		/// <param name="port">port number of the SSH daemon (typically 22).</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to
		/// perform certain file system operations.
		/// </param>
		/// <returns>new session instance, but otherwise unconfigured.</returns>
		/// <exception cref="NSch.JSchException">the session could not be created.</exception>
		protected internal virtual Session CreateSession(OpenSshConfig.Host hc, string user
			, string host, int port, FS fs)
		{
			return GetJSch(hc, fs).GetSession(user, host, port);
		}

		/// <summary>
		/// Provide additional configuration for the session based on the host
		/// information.
		/// </summary>
		/// <remarks>
		/// Provide additional configuration for the session based on the host
		/// information. This method could be used to supply
		/// <see cref="NSch.UserInfo">NSch.UserInfo</see>
		/// .
		/// </remarks>
		/// <param name="hc">host configuration</param>
		/// <param name="session">session to configure</param>
		protected internal abstract void Configure(OpenSshConfig.Host hc, Session session
			);

		/// <summary>Obtain the JSch used to create new sessions.</summary>
		/// <remarks>Obtain the JSch used to create new sessions.</remarks>
		/// <param name="hc">host configuration</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to
		/// perform certain file system operations.
		/// </param>
		/// <returns>the JSch instance to use.</returns>
		/// <exception cref="NSch.JSchException">the user configuration could not be created.
		/// 	</exception>
		protected internal virtual JSch GetJSch(OpenSshConfig.Host hc, FS fs)
		{
			if (defaultJSch == null)
			{
				defaultJSch = CreateDefaultJSch(fs);
				foreach (object name in defaultJSch.GetIdentityNames())
				{
					byIdentityFile.Put((string)name, defaultJSch);
				}
			}
			FilePath identityFile = hc.GetIdentityFile();
			if (identityFile == null)
			{
				return defaultJSch;
			}
			string identityKey = identityFile.GetAbsolutePath();
			JSch jsch = byIdentityFile.Get(identityKey);
			if (jsch == null)
			{
				jsch = new JSch();
				jsch.SetHostKeyRepository(defaultJSch.GetHostKeyRepository());
				jsch.AddIdentity(identityKey);
				byIdentityFile.Put(identityKey, jsch);
			}
			return jsch;
		}

		/// <param name="fs">
		/// the file system abstraction which will be necessary to
		/// perform certain file system operations.
		/// </param>
		/// <returns>the new default JSch implementation.</returns>
		/// <exception cref="NSch.JSchException">known host keys cannot be loaded.</exception>
		protected internal virtual JSch CreateDefaultJSch(FS fs)
		{
			JSch jsch = new JSch();
			KnownHosts(jsch, fs);
			Identities(jsch, fs);
			return jsch;
		}

		/// <exception cref="NSch.JSchException"></exception>
		private static void KnownHosts(JSch sch, FS fs)
		{
			FilePath home = fs.UserHome();
			if (home == null)
			{
				return;
			}
			FilePath known_hosts = new FilePath(new FilePath(home, ".ssh"), "known_hosts");
			try
			{
				FileInputStream @in = new FileInputStream(known_hosts);
				try
				{
					sch.SetKnownHosts(@in);
				}
				finally
				{
					@in.Close();
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (IOException)
			{
			}
		}

		// Oh well. They don't have a known hosts in home.
		// Oh well. They don't have a known hosts in home.
		private static void Identities(JSch sch, FS fs)
		{
			FilePath home = fs.UserHome();
			if (home == null)
			{
				return;
			}
			FilePath sshdir = new FilePath(home, ".ssh");
			if (sshdir.IsDirectory())
			{
				LoadIdentity(sch, new FilePath(sshdir, "identity"));
				LoadIdentity(sch, new FilePath(sshdir, "id_rsa"));
				LoadIdentity(sch, new FilePath(sshdir, "id_dsa"));
			}
		}

		private static void LoadIdentity(JSch sch, FilePath priv)
		{
			if (priv.IsFile())
			{
				try
				{
					sch.AddIdentity(priv.GetAbsolutePath());
				}
				catch (JSchException)
				{
				}
			}
		}
		// Instead, pretend the key doesn't exist.
	}
}
