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
			return new TransportGitSsh.SshFetchConnection(this);
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			return new TransportGitSsh.SshPushConnection(this);
		}

		private static void SqMinimal(StringBuilder cmd, string val)
		{
			if (val.Matches("^[a-zA-Z0-9._/-]*$"))
			{
				// If the string matches only generally safe characters
				// that the shell is not going to evaluate specially we
				// should leave the string unquoted. Not all systems
				// actually run a shell and over-quoting confuses them
				// when it comes to the command name.
				//
				cmd.Append(val);
			}
			else
			{
				Sq(cmd, val);
			}
		}

		private static void SqAlways(StringBuilder cmd, string val)
		{
			Sq(cmd, val);
		}

		private static void Sq(StringBuilder cmd, string val)
		{
			if (val.Length > 0)
			{
				cmd.Append(QuotedString.BOURNE.Quote(val));
			}
		}

		private string CommandFor(string exe)
		{
			string path = uri.GetPath();
			if (uri.GetScheme() != null && uri.GetPath().StartsWith("/~"))
			{
				path = (Sharpen.Runtime.Substring(uri.GetPath(), 1));
			}
			StringBuilder cmd = new StringBuilder();
			int gitspace = exe.IndexOf("git ");
			if (gitspace >= 0)
			{
				SqMinimal(cmd, Sharpen.Runtime.Substring(exe, 0, gitspace + 3));
				cmd.Append(' ');
				SqMinimal(cmd, Sharpen.Runtime.Substring(exe, gitspace + 4));
			}
			else
			{
				SqMinimal(cmd, exe);
			}
			cmd.Append(' ');
			SqAlways(cmd, path);
			return cmd.ToString();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal virtual ChannelExec Exec(string exe)
		{
			InitSession();
			try
			{
				ChannelExec channel = (ChannelExec)sock.OpenChannel("exec");
				channel.SetCommand(CommandFor(exe));
				return channel;
			}
			catch (JSchException je)
			{
				throw new TransportException(uri, je.Message, je);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void Connect(ChannelExec channel)
		{
			try
			{
				channel.Connect(GetTimeout() > 0 ? GetTimeout() * 1000 : 0);
				if (!channel.IsConnected())
				{
					throw new TransportException(uri, "connection failed");
				}
			}
			catch (JSchException e)
			{
				throw new TransportException(uri, e.Message, e);
			}
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
			SqAlways(pfx, path);
			pfx.Append(": ");
			if (why.StartsWith(pfx.ToString()))
			{
				why = Sharpen.Runtime.Substring(why, pfx.Length);
			}
			return new NoRemoteRepositoryException(uri, why);
		}

		// JSch won't let us interrupt writes when we use our InterruptTimer to
		// break out of a long-running write operation. To work around that we
		// spawn a background thread to shuttle data through a pipe, as we can
		// issue an interrupted write out of that. Its slower, so we only use
		// this route if there is a timeout.
		//
		/// <exception cref="System.IO.IOException"></exception>
		private Sharpen.OutputStream OutputStream(ChannelExec channel)
		{
			Sharpen.OutputStream @out = channel.GetOutputStream();
			if (GetTimeout() <= 0)
			{
				return @out;
			}
			PipedInputStream pipeIn = new PipedInputStream();
			StreamCopyThread copyThread = new StreamCopyThread(pipeIn, @out);
			PipedOutputStream pipeOut = new _PipedOutputStream_213(this, copyThread, pipeIn);
			// Just wake early, the thread will terminate anyway.
			copyThread.Start();
			return pipeOut;
		}

		private sealed class _PipedOutputStream_213 : PipedOutputStream
		{
			public _PipedOutputStream_213(TransportGitSsh _enclosing, StreamCopyThread copyThread
				, PipedInputStream baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.copyThread = copyThread;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				base.Flush();
				copyThread.Flush();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				base.Close();
				try
				{
					copyThread.Join(this._enclosing.GetTimeout() * 1000);
				}
				catch (Exception)
				{
				}
			}

			private readonly TransportGitSsh _enclosing;

			private readonly StreamCopyThread copyThread;
		}

		internal class SshFetchConnection : BasePackFetchConnection
		{
			private ChannelExec channel;

			private StreamCopyThread errorThread;

			private int exitStatus;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public SshFetchConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				try
				{
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					this.channel = this._enclosing.Exec(this._enclosing.GetOptionUploadPack());
					InputStream upErr = this.channel.GetErrStream();
					this.errorThread = new StreamCopyThread(upErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(this.channel.GetInputStream(), this._enclosing.OutputStream(this.channel
						));
					this._enclosing.Connect(this.channel);
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
					this._enclosing.CheckExecFailure(this.exitStatus, this._enclosing.GetOptionUploadPack
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

			private readonly TransportGitSsh _enclosing;
		}

		internal class SshPushConnection : BasePackPushConnection
		{
			private ChannelExec channel;

			private StreamCopyThread errorThread;

			private int exitStatus;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public SshPushConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				try
				{
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					this.channel = this._enclosing.Exec(this._enclosing.GetOptionReceivePack());
					InputStream rpErr = this.channel.GetErrStream();
					this.errorThread = new StreamCopyThread(rpErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(this.channel.GetInputStream(), this._enclosing.OutputStream(this.channel
						));
					this._enclosing.Connect(this.channel);
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
					this._enclosing.CheckExecFailure(this.exitStatus, this._enclosing.GetOptionReceivePack
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

			private readonly TransportGitSsh _enclosing;
		}
	}
}
