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
using NGit.Errors;
using NGit.Transport;
using NGit.Util.IO;
using NSch;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Run remote commands using Jsch.</summary>
	/// <remarks>
	/// Run remote commands using Jsch.
	/// <p>
	/// This class is the default session implementation using Jsch. Note that
	/// <see cref="JschConfigSessionFactory">JschConfigSessionFactory</see>
	/// is used to create the actual session passed
	/// to the constructor.
	/// </remarks>
	public class JschSession : RemoteSession
	{
		private readonly Session sock;

		private readonly URIish uri;

		/// <summary>
		/// Create a new session object by passing the real Jsch session and the URI
		/// information.
		/// </summary>
		/// <remarks>
		/// Create a new session object by passing the real Jsch session and the URI
		/// information.
		/// </remarks>
		/// <param name="session">the real Jsch session created elsewhere.</param>
		/// <param name="uri">the URI information for the remote connection</param>
		public JschSession(Session session, URIish uri)
		{
			sock = session;
			this.uri = uri;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual SystemProcess Exec(string command, int timeout)
		{
			return new JschSession.JschProcess(this, command, timeout);
		}

		public virtual void Disconnect()
		{
			if (sock.IsConnected())
			{
				sock.Disconnect();
			}
		}

		/// <summary>
		/// A kludge to allow
		/// <see cref="TransportSftp">TransportSftp</see>
		/// to get an Sftp channel from Jsch.
		/// Ideally, this method would be generic, which would require implementing
		/// generic Sftp channel operations in the RemoteSession class.
		/// </summary>
		/// <returns>a channel suitable for Sftp operations.</returns>
		/// <exception cref="NSch.JSchException">on problems getting the channel.</exception>
		public virtual Channel GetSftpChannel()
		{
			return sock.OpenChannel("sftp");
		}

		/// <summary>Implementation of Process for running a single command using Jsch.</summary>
		/// <remarks>
		/// Implementation of Process for running a single command using Jsch.
		/// <p>
		/// Uses the Jsch session to do actual command execution and manage the
		/// execution.
		/// </remarks>
		internal class JschProcess : SystemProcess
		{
			private ChannelExec channel;

			private readonly int timeout;

			private InputStream inputStream;

			private OutputStream outputStream;

			private InputStream errStream;

			/// <summary>
			/// Opens a channel on the session ("sock") for executing the given
			/// command, opens streams, and starts command execution.
			/// </summary>
			/// <remarks>
			/// Opens a channel on the session ("sock") for executing the given
			/// command, opens streams, and starts command execution.
			/// </remarks>
			/// <param name="commandName">the command to execute</param>
			/// <param name="tms">the timeout value, in seconds, for the command.</param>
			/// <exception cref="NGit.Errors.TransportException">
			/// on problems opening a channel or connecting to the remote
			/// host
			/// </exception>
			/// <exception cref="System.IO.IOException">on problems opening streams</exception>
			public JschProcess(JschSession _enclosing, string commandName, int tms)
			{
				this._enclosing = _enclosing;
				this.timeout = tms;
				try
				{
					this.channel = (ChannelExec)this._enclosing.sock.OpenChannel("exec");
					this.channel.SetCommand(commandName);
					this.SetupStreams();
					this.channel.Connect(this.timeout > 0 ? this.timeout * 1000 : 0);
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
			private void SetupStreams()
			{
				this.inputStream = this.channel.GetInputStream();
				// JSch won't let us interrupt writes when we use our InterruptTimer
				// to break out of a long-running write operation. To work around
				// that we spawn a background thread to shuttle data through a pipe,
				// as we can issue an interrupted write out of that. Its slower, so
				// we only use this route if there is a timeout.
				OutputStream @out = this.channel.GetOutputStream();
				if (this.timeout <= 0)
				{
					this.outputStream = @out;
				}
				else
				{
					PipedInputStream pipeIn = new PipedInputStream();
					StreamCopyThread copier = new StreamCopyThread(pipeIn, @out);
					PipedOutputStream pipeOut = new _PipedOutputStream_173(this, copier, pipeIn);
					// Just wake early, the thread will terminate
					// anyway.
					copier.Start();
					this.outputStream = pipeOut;
				}
				this.errStream = this.channel.GetErrStream();
			}

			private sealed class _PipedOutputStream_173 : PipedOutputStream
			{
				public _PipedOutputStream_173(JschProcess _enclosing, StreamCopyThread copier, PipedInputStream
					 baseArg1) : base(baseArg1)
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
						copier.Join(this._enclosing.timeout * 1000);
					}
					catch (Exception)
					{
					}
				}

				private readonly JschProcess _enclosing;

				private readonly StreamCopyThread copier;
			}

			public override InputStream GetInputStream()
			{
				return this.inputStream;
			}

			public override OutputStream GetOutputStream()
			{
				return this.outputStream;
			}

			public override InputStream GetErrorStream()
			{
				return this.errStream;
			}

			public override int ExitValue()
			{
				if (this.IsRunning())
				{
					throw new InvalidOperationException();
				}
				return this.channel.GetExitStatus();
			}

			private bool IsRunning()
			{
				return this.channel.GetExitStatus() < 0 && this.channel.IsConnected();
			}

			public override void Destroy()
			{
				if (this.channel.IsConnected())
				{
					this.channel.Disconnect();
				}
			}

			/// <exception cref="System.Exception"></exception>
			public override int WaitFor()
			{
				while (this.IsRunning())
				{
					Sharpen.Thread.Sleep(100);
				}
				return this.ExitValue();
			}

			private readonly JschSession _enclosing;
		}
	}
}
