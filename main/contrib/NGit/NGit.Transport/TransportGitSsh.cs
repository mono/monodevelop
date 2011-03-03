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
using System.Diagnostics;
using System.IO;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Transport;
using NGit.Util;
using NGit.Util.IO;
using NSch;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Transport through an SSH tunnel.</summary>
	/// <remarks>
	/// Transport through an SSH tunnel.
	/// <p>
	/// The SSH transport requires the remote side to have Git installed, as the
	/// transport logs into the remote system and executes a Git helper program on
	/// the remote side to read (or write) the remote repository's files.
	/// <p>
	/// This transport does not support direct SCP style of copying files, as it
	/// assumes there are Git specific smarts on the remote side to perform object
	/// enumeration, save file modification and hook execution.
	/// </remarks>
	public class TransportGitSsh : SshTransport, PackTransport
	{
		internal static bool CanHandle(URIish uri)
		{
			if (!uri.IsRemote())
			{
				return false;
			}
			string scheme = uri.GetScheme();
			if ("ssh".Equals(scheme))
			{
				return true;
			}
			if ("ssh+git".Equals(scheme))
			{
				return true;
			}
			if ("git+ssh".Equals(scheme))
			{
				return true;
			}
			if (scheme == null && uri.GetHost() != null && uri.GetPath() != null)
			{
				return true;
			}
			return false;
		}

		protected internal TransportGitSsh(Repository local, URIish uri) : base(local, uri
			)
		{
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			return new TransportGitSsh.SshFetchConnection(this, NewConnection());
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			return new TransportGitSsh.SshPushConnection(this, NewConnection());
		}

		private TransportGitSsh.Connection NewConnection()
		{
			if (UseExtConnection())
			{
				return new TransportGitSsh.ExtConnection(this);
			}
			return new TransportGitSsh.JschConnection(this);
		}

		internal virtual string CommandFor(string exe)
		{
			string path = uri.GetPath();
			if (uri.GetScheme() != null && uri.GetPath().StartsWith("/~"))
			{
				path = (Sharpen.Runtime.Substring(uri.GetPath(), 1));
			}
			StringBuilder cmd = new StringBuilder();
			cmd.Append(exe);
			cmd.Append(' ');
			cmd.Append(QuotedString.BOURNE.Quote(path));
			return cmd.ToString();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal virtual void CheckExecFailure(int status, string exe, string why)
		{
			if (status == 127)
			{
				IOException cause = null;
				if (why != null && why.Length > 0)
				{
					cause = new IOException(why);
				}
				throw new TransportException(uri, MessageFormat.Format(JGitText.Get().cannotExecute
					, CommandFor(exe)), cause);
			}
		}

		internal virtual NoRemoteRepositoryException CleanNotFound(NoRemoteRepositoryException
			 nf, string why)
		{
			if (why == null || why.Length == 0)
			{
				return nf;
			}
			string path = uri.GetPath();
			if (uri.GetScheme() != null && uri.GetPath().StartsWith("/~"))
			{
				path = Sharpen.Runtime.Substring(uri.GetPath(), 1);
			}
			StringBuilder pfx = new StringBuilder();
			pfx.Append("fatal: ");
			pfx.Append(QuotedString.BOURNE.Quote(path));
			pfx.Append(": ");
			if (why.StartsWith(pfx.ToString()))
			{
				why = Sharpen.Runtime.Substring(why, pfx.Length);
			}
			return new NoRemoteRepositoryException(uri, why);
		}

		private abstract class Connection
		{
			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal abstract void Exec(string commandName);

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal abstract void Connect();

			/// <exception cref="System.IO.IOException"></exception>
			internal abstract InputStream GetInputStream();

			/// <exception cref="System.IO.IOException"></exception>
			internal abstract OutputStream GetOutputStream();

			/// <exception cref="System.IO.IOException"></exception>
			internal abstract InputStream GetErrorStream();

			internal abstract int GetExitStatus();

			internal abstract void Close();

			internal Connection(TransportGitSsh _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TransportGitSsh _enclosing;
		}

		private class JschConnection : TransportGitSsh.Connection
		{
			private ChannelExec channel;

			private int exitStatus;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal override void Exec(string commandName)
			{
				this._enclosing.InitSession();
				try
				{
					this.channel = (ChannelExec)this._enclosing.sock.OpenChannel("exec");
					this.channel.SetCommand(this._enclosing.CommandFor(commandName));
				}
				catch (JSchException je)
				{
					throw new TransportException(this._enclosing.uri, je.Message, je);
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal override void Connect()
			{
				try
				{
					this.channel.Connect(this._enclosing.GetTimeout() > 0 ? this._enclosing.GetTimeout
						() * 1000 : 0);
					if (!this.channel.IsConnected())
					{
						throw new TransportException(this._enclosing.uri, "connection failed");
					}
				}
				catch (JSchException e)
				{
					throw new TransportException(this._enclosing.uri, e.Message, e);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override InputStream GetInputStream()
			{
				return this.channel.GetInputStream();
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override OutputStream GetOutputStream()
			{
				// JSch won't let us interrupt writes when we use our InterruptTimer
				// to break out of a long-running write operation. To work around
				// that we spawn a background thread to shuttle data through a pipe,
				// as we can issue an interrupted write out of that. Its slower, so
				// we only use this route if there is a timeout.
				//
				OutputStream @out = this.channel.GetOutputStream();
				if (this._enclosing.GetTimeout() <= 0)
				{
					return @out;
				}
				PipedInputStream pipeIn = new PipedInputStream();
				StreamCopyThread copier = new StreamCopyThread(pipeIn, @out);
				PipedOutputStream pipeOut = new _PipedOutputStream_221(this, copier, pipeIn);
				// Just wake early, the thread will terminate anyway.
				copier.Start();
				return pipeOut;
			}

			private sealed class _PipedOutputStream_221 : PipedOutputStream
			{
				public _PipedOutputStream_221(JschConnection _enclosing, StreamCopyThread copier, 
					PipedInputStream baseArg1) : base(baseArg1)
				{
					this._enclosing = _enclosing;
					this.copier = copier;
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Flush()
				{
					base.Flush();
					copier.Flush();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void Close()
				{
					base.Close();
					try
					{
						copier.Join(this._enclosing._enclosing.GetTimeout() * 1000);
					}
					catch (Exception)
					{
					}
				}

				private readonly JschConnection _enclosing;

				private readonly StreamCopyThread copier;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override InputStream GetErrorStream()
			{
				return this.channel.GetErrStream();
			}

			internal override int GetExitStatus()
			{
				return this.exitStatus;
			}

			internal override void Close()
			{
				if (this.channel != null)
				{
					try
					{
						this.exitStatus = this.channel.GetExitStatus();
						if (this.channel.IsConnected())
						{
							this.channel.Disconnect();
						}
					}
					finally
					{
						this.channel = null;
					}
				}
			}

			internal JschConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TransportGitSsh _enclosing;
		}

		private static bool UseExtConnection()
		{
			return SystemReader.GetInstance().Getenv("GIT_SSH") != null;
		}

		private class ExtConnection : TransportGitSsh.Connection
		{
			private Process proc;

			private int exitStatus;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal override void Exec(string commandName)
			{
				string ssh = SystemReader.GetInstance().Getenv("GIT_SSH");
				bool putty = ssh.ToLower().Contains("plink");
				IList<string> args = new AList<string>();
				args.AddItem(ssh);
				if (putty && !ssh.ToLower().Contains("tortoiseplink"))
				{
					args.AddItem("-batch");
				}
				if (0 < this._enclosing.GetURI().GetPort())
				{
					args.AddItem(putty ? "-P" : "-p");
					args.AddItem(this._enclosing.GetURI().GetPort().ToString());
				}
				if (this._enclosing.GetURI().GetUser() != null)
				{
					args.AddItem(this._enclosing.GetURI().GetUser() + "@" + this._enclosing.GetURI().
						GetHost());
				}
				else
				{
					args.AddItem(this._enclosing.GetURI().GetHost());
				}
				args.AddItem(this._enclosing.CommandFor(commandName));
				ProcessStartInfo pb = new ProcessStartInfo();
				pb.SetCommand(args);
				if (this._enclosing.local.Directory != null)
				{
					pb.EnvironmentVariables.Put(Constants.GIT_DIR_KEY, this._enclosing.local.Directory
						.GetPath());
				}
				try
				{
					this.proc = pb.Start();
				}
				catch (IOException err)
				{
					throw new TransportException(this._enclosing.uri, err.Message, err);
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal override void Connect()
			{
			}

			// Nothing to do, the process was already opened.
			/// <exception cref="System.IO.IOException"></exception>
			internal override InputStream GetInputStream()
			{
				return this.proc.GetInputStream();
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override OutputStream GetOutputStream()
			{
				return this.proc.GetOutputStream();
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override InputStream GetErrorStream()
			{
				return this.proc.GetErrorStream();
			}

			internal override int GetExitStatus()
			{
				return this.exitStatus;
			}

			internal override void Close()
			{
				if (this.proc != null)
				{
					try
					{
						try
						{
							this.exitStatus = this.proc.WaitFor();
						}
						catch (Exception)
						{
						}
					}
					finally
					{
						// Ignore the interrupt, but return immediately.
						this.proc = null;
					}
				}
			}

			internal ExtConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TransportGitSsh _enclosing;
		}

		private class SshFetchConnection : BasePackFetchConnection
		{
			private TransportGitSsh.Connection conn;

			private StreamCopyThread errorThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal SshFetchConnection(TransportGitSsh _enclosing, TransportGitSsh.Connection
				 conn) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.conn = conn;
				try
				{
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					conn.Exec(this._enclosing.GetOptionUploadPack());
					InputStream upErr = conn.GetErrorStream();
					this.errorThread = new StreamCopyThread(upErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(conn.GetInputStream(), conn.GetOutputStream());
					conn.Connect();
				}
				catch (TransportException err)
				{
					this.Close();
					throw;
				}
				catch (IOException err)
				{
					this.Close();
					throw new TransportException(this.uri, JGitText.Get().remoteHungUpUnexpectedly, err
						);
				}
				try
				{
					this.ReadAdvertisedRefs();
				}
				catch (NoRemoteRepositoryException notFound)
				{
					string msgs = this.GetMessages();
					this._enclosing.CheckExecFailure(conn.GetExitStatus(), this._enclosing.GetOptionUploadPack
						(), msgs);
					throw this._enclosing.CleanNotFound(notFound, msgs);
				}
			}

			public override void Close()
			{
				this.EndOut();
				if (this.errorThread != null)
				{
					try
					{
						this.errorThread.Halt();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.errorThread = null;
					}
				}
				base.Close();
				this.conn.Close();
			}

			private readonly TransportGitSsh _enclosing;
		}

		private class SshPushConnection : BasePackPushConnection
		{
			private TransportGitSsh.Connection conn;

			private StreamCopyThread errorThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal SshPushConnection(TransportGitSsh _enclosing, TransportGitSsh.Connection
				 conn) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.conn = conn;
				try
				{
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					conn.Exec(this._enclosing.GetOptionReceivePack());
					InputStream rpErr = conn.GetErrorStream();
					this.errorThread = new StreamCopyThread(rpErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(conn.GetInputStream(), conn.GetOutputStream());
					conn.Connect();
				}
				catch (TransportException err)
				{
					this.Close();
					throw;
				}
				catch (IOException err)
				{
					this.Close();
					throw new TransportException(this.uri, JGitText.Get().remoteHungUpUnexpectedly, err
						);
				}
				try
				{
					this.ReadAdvertisedRefs();
				}
				catch (NoRemoteRepositoryException notFound)
				{
					string msgs = this.GetMessages();
					this._enclosing.CheckExecFailure(conn.GetExitStatus(), this._enclosing.GetOptionReceivePack
						(), msgs);
					throw this._enclosing.CleanNotFound(notFound, msgs);
				}
			}

			public override void Close()
			{
				this.EndOut();
				if (this.errorThread != null)
				{
					try
					{
						this.errorThread.Halt();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.errorThread = null;
					}
				}
				base.Close();
				this.conn.Close();
			}

			private readonly TransportGitSsh _enclosing;
		}
	}
}
