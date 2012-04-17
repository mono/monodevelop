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
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// A service exposed by
	/// <see cref="Daemon">Daemon</see>
	/// over anonymous <code>git://</code>.
	/// </summary>
	public abstract class DaemonService
	{
		private readonly string command;

		private readonly Config.SectionParser<DaemonService.ServiceConfig> configKey;

		private bool enabled;

		private bool overridable;

		internal DaemonService(string cmdName, string cfgName)
		{
			command = cmdName.StartsWith("git-") ? cmdName : "git-" + cmdName;
			configKey = new _SectionParser_67(this, cfgName);
			overridable = true;
		}

		private sealed class _SectionParser_67 : Config.SectionParser<DaemonService.ServiceConfig
			>
		{
			public _SectionParser_67(DaemonService _enclosing, string cfgName)
			{
				this._enclosing = _enclosing;
				this.cfgName = cfgName;
			}

			public DaemonService.ServiceConfig Parse(Config cfg)
			{
				return new DaemonService.ServiceConfig(this._enclosing, cfg, cfgName);
			}

			private readonly DaemonService _enclosing;

			private readonly string cfgName;
		}

		private class ServiceConfig
		{
			internal readonly bool enabled;

			internal ServiceConfig(DaemonService service, Config cfg, string name)
			{
				enabled = cfg.GetBoolean("daemon", name, service.IsEnabled());
			}
		}

		/// <returns>is this service enabled for invocation?</returns>
		public virtual bool IsEnabled()
		{
			return enabled;
		}

		/// <param name="on">true to allow this service to be used; false to deny it.</param>
		public virtual void SetEnabled(bool on)
		{
			enabled = on;
		}

		/// <returns>can this service be configured in the repository config file?</returns>
		public virtual bool IsOverridable()
		{
			return overridable;
		}

		/// <param name="on">
		/// true to permit repositories to override this service's enabled
		/// state with the <code>daemon.servicename</code> config setting.
		/// </param>
		public virtual void SetOverridable(bool on)
		{
			overridable = on;
		}

		/// <returns>name of the command requested by clients.</returns>
		public virtual string GetCommandName()
		{
			return command;
		}

		/// <summary>Determine if this service can handle the requested command.</summary>
		/// <remarks>Determine if this service can handle the requested command.</remarks>
		/// <param name="commandLine">input line from the client.</param>
		/// <returns>true if this command can accept the given command line.</returns>
		public virtual bool Handles(string commandLine)
		{
			return command.Length + 1 < commandLine.Length && commandLine[command.Length] == 
				' ' && commandLine.StartsWith(command);
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
		internal virtual void Execute(DaemonClient client, string commandLine)
		{
			string name = Sharpen.Runtime.Substring(commandLine, command.Length + 1);
			Repository db;
			try
			{
				db = client.GetDaemon().OpenRepository(client, name);
			}
			catch (ServiceMayNotContinueException e)
			{
				// An error when opening the repo means the client is expecting a ref
				// advertisement, so use that style of error.
				PacketLineOut pktOut = new PacketLineOut(client.GetOutputStream());
				pktOut.WriteString("ERR " + e.Message + "\n");
				db = null;
			}
			if (db == null)
			{
				return;
			}
			try
			{
				if (IsEnabledFor(db))
				{
					Execute(client, db);
				}
			}
			finally
			{
				db.Close();
			}
		}

		private bool IsEnabledFor(Repository db)
		{
			if (IsOverridable())
			{
				return db.GetConfig().Get(configKey).enabled;
			}
			return IsEnabled();
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotAuthorizedException"></exception>
		internal abstract void Execute(DaemonClient client, Repository db);
	}
}
