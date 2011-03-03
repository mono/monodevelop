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
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Transport to access a local directory as though it were a remote peer.</summary>
	/// <remarks>
	/// Transport to access a local directory as though it were a remote peer.
	/// <p>
	/// This transport is suitable for use on the local system, where the caller has
	/// direct read or write access to the "remote" repository.
	/// <p>
	/// By default this transport works by spawning a helper thread within the same
	/// JVM, and processes the data transfer using a shared memory buffer between the
	/// calling thread and the helper thread. This is a pure-Java implementation
	/// which does not require forking an external process.
	/// <p>
	/// However, during
	/// <see cref="OpenFetch()">OpenFetch()</see>
	/// , if the Transport has configured
	/// <see cref="Transport.GetOptionUploadPack()">Transport.GetOptionUploadPack()</see>
	/// to be anything other than
	/// <code>"git-upload-pack"</code> or <code>"git upload-pack"</code>, this
	/// implementation will fork and execute the external process, using an operating
	/// system pipe to transfer data.
	/// <p>
	/// Similarly, during
	/// <see cref="OpenPush()">OpenPush()</see>
	/// , if the Transport has configured
	/// <see cref="Transport.GetOptionReceivePack()">Transport.GetOptionReceivePack()</see>
	/// to be anything other than
	/// <code>"git-receive-pack"</code> or <code>"git receive-pack"</code>, this
	/// implementation will fork and execute the external process, using an operating
	/// system pipe to transfer data.
	/// </remarks>
	internal class TransportLocal : NGit.Transport.Transport, PackTransport
	{
		private static readonly string PWD = ".";

		internal static bool CanHandle(URIish uri, FS fs)
		{
			if (uri.GetHost() != null || uri.GetPort() > 0 || uri.GetUser() != null || uri.GetPass
				() != null || uri.GetPath() == null)
			{
				return false;
			}
			if ("file".Equals(uri.GetScheme()) || uri.GetScheme() == null)
			{
				return fs.Resolve(new FilePath(PWD), uri.GetPath()).IsDirectory();
			}
			return false;
		}

		private readonly FilePath remoteGitDir;

		protected internal TransportLocal(Repository local, URIish uri) : base(local, uri
			)
		{
			FilePath d = local.FileSystem.Resolve(new FilePath(PWD), uri.GetPath()).GetAbsoluteFile
				();
			if (new FilePath(d, Constants.DOT_GIT).IsDirectory())
			{
				d = new FilePath(d, Constants.DOT_GIT);
			}
			remoteGitDir = d;
		}

		internal virtual UploadPack CreateUploadPack(Repository dst)
		{
			return new UploadPack(dst);
		}

		internal virtual ReceivePack CreateReceivePack(Repository dst)
		{
			return new ReceivePack(dst);
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			string up = GetOptionUploadPack();
			if ("git-upload-pack".Equals(up) || "git upload-pack".Equals(up))
			{
				return new TransportLocal.InternalLocalFetchConnection(this);
			}
			return new TransportLocal.ForkLocalFetchConnection(this);
		}

		/// <exception cref="System.NotSupportedException"></exception>
		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			string rp = GetOptionReceivePack();
			if ("git-receive-pack".Equals(rp) || "git receive-pack".Equals(rp))
			{
				return new TransportLocal.InternalLocalPushConnection(this);
			}
			return new TransportLocal.ForkLocalPushConnection(this);
		}

		public override void Close()
		{
		}

		// Resources must be established per-connection.
		/// <exception cref="NGit.Errors.TransportException"></exception>
		protected internal virtual Process Spawn(string cmd)
		{
			try
			{
				string[] args = new string[] { "." };
				ProcessStartInfo proc = local.FileSystem.RunInShell(cmd, args);
				proc.WorkingDirectory = remoteGitDir;
				// Remove the same variables CGit does.
				var env = proc.EnvironmentVariables;
				env.Remove ("GIT_ALTERNATE_OBJECT_DIRECTORIES");
				env.Remove ("GIT_CONFIG");
				env.Remove ("GIT_CONFIG_PARAMETERS");
				env.Remove ("GIT_DIR");
				env.Remove ("GIT_WORK_TREE");
				env.Remove ("GIT_GRAFT_FILE");
				env.Remove ("GIT_INDEX_FILE");
				env.Remove ("GIT_NO_REPLACE_OBJECTS");
				return proc.Start();
			}
			catch (IOException err)
			{
				throw new TransportException(uri, err.Message, err);
			}
		}

		internal class InternalLocalFetchConnection : BasePackFetchConnection
		{
			private Sharpen.Thread worker;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public InternalLocalFetchConnection(TransportLocal _enclosing) : base(_enclosing
				)
			{
				this._enclosing = _enclosing;
				Repository dst;
				try
				{
					dst = new FileRepository(this._enclosing.remoteGitDir);
				}
				catch (IOException)
				{
					throw new TransportException(this.uri, JGitText.Get().notAGitDirectory);
				}
				PipedInputStream in_r;
				PipedOutputStream in_w;
				PipedInputStream out_r;
				PipedOutputStream out_w;
				try
				{
					in_r = new PipedInputStream();
					in_w = new PipedOutputStream(in_r);
					out_r = new _PipedInputStream_193();
					// The client (BasePackFetchConnection) can write
					// a huge burst before it reads again. We need to
					// force the buffer to be big enough, otherwise it
					// will deadlock both threads.
					out_w = new PipedOutputStream(out_r);
				}
				catch (IOException err)
				{
					dst.Close();
					throw new TransportException(this.uri, JGitText.Get().cannotConnectPipes, err);
				}
				this.worker = new _Thread_208(this, dst, out_r, in_w, "JGit-Upload-Pack");
				// Client side of the pipes should report the problem.
				// Clients side will notice we went away, and report.
				// Ignore close failure, we probably crashed above.
				// Ignore close failure, we probably crashed above.
				this.worker.Start();
				this.Init(in_r, out_w);
				this.ReadAdvertisedRefs();
			}

			private sealed class _PipedInputStream_193 : PipedInputStream
			{
				public _PipedInputStream_193()
				{
					{
						this.buffer = new byte[BasePackFetchConnection.MIN_CLIENT_BUFFER];
					}
				}
			}

			private sealed class _Thread_208 : Sharpen.Thread
			{
				public _Thread_208(InternalLocalFetchConnection _enclosing, Repository dst, PipedInputStream
					 out_r, PipedOutputStream in_w, string baseArg1) : base(baseArg1)
				{
					this._enclosing = _enclosing;
					this.dst = dst;
					this.out_r = out_r;
					this.in_w = in_w;
				}

				public override void Run()
				{
					try
					{
						UploadPack rp = this._enclosing._enclosing.CreateUploadPack(dst);
						rp.Upload(out_r, in_w, null);
					}
					catch (IOException err)
					{
						Sharpen.Runtime.PrintStackTrace(err);
					}
					catch (RuntimeException err)
					{
						Sharpen.Runtime.PrintStackTrace(err);
					}
					finally
					{
						try
						{
							out_r.Close();
						}
						catch (IOException)
						{
						}
						try
						{
							in_w.Close();
						}
						catch (IOException)
						{
						}
						dst.Close();
					}
				}

				private readonly InternalLocalFetchConnection _enclosing;

				private readonly Repository dst;

				private readonly PipedInputStream out_r;

				private readonly PipedOutputStream in_w;
			}

			public override void Close()
			{
				base.Close();
				if (this.worker != null)
				{
					try
					{
						this.worker.Join();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.worker = null;
					}
				}
			}

			private readonly TransportLocal _enclosing;
		}

		internal class ForkLocalFetchConnection : BasePackFetchConnection
		{
			private Process uploadPack;

			private Sharpen.Thread errorReaderThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public ForkLocalFetchConnection(TransportLocal _enclosing) : base(_enclosing
				)
			{
				this._enclosing = _enclosing;
				MessageWriter msg = new MessageWriter();
				this.SetMessageWriter(msg);
				this.uploadPack = this._enclosing.Spawn(this._enclosing.GetOptionUploadPack());
				InputStream upErr = this.uploadPack.GetErrorStream();
				this.errorReaderThread = new StreamCopyThread(upErr, msg.GetRawStream());
				this.errorReaderThread.Start();
				InputStream upIn = this.uploadPack.GetInputStream();
				OutputStream upOut = this.uploadPack.GetOutputStream();
				upIn = new BufferedInputStream(upIn);
				upOut = new BufferedOutputStream(upOut);
				this.Init(upIn, upOut);
				this.ReadAdvertisedRefs();
			}

			public override void Close()
			{
				base.Close();
				if (this.uploadPack != null)
				{
					try
					{
						this.uploadPack.WaitFor();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.uploadPack = null;
					}
				}
				if (this.errorReaderThread != null)
				{
					try
					{
						this.errorReaderThread.Join();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.errorReaderThread = null;
					}
				}
			}

			private readonly TransportLocal _enclosing;
		}

		internal class InternalLocalPushConnection : BasePackPushConnection
		{
			private Sharpen.Thread worker;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public InternalLocalPushConnection(TransportLocal _enclosing) : base(_enclosing
				)
			{
				this._enclosing = _enclosing;
				Repository dst;
				try
				{
					dst = new FileRepository(this._enclosing.remoteGitDir);
				}
				catch (IOException)
				{
					throw new TransportException(this.uri, JGitText.Get().notAGitDirectory);
				}
				PipedInputStream in_r;
				PipedOutputStream in_w;
				PipedInputStream out_r;
				PipedOutputStream out_w;
				try
				{
					in_r = new PipedInputStream();
					in_w = new PipedOutputStream(in_r);
					out_r = new PipedInputStream();
					out_w = new PipedOutputStream(out_r);
				}
				catch (IOException err)
				{
					dst.Close();
					throw new TransportException(this.uri, JGitText.Get().cannotConnectPipes, err);
				}
				this.worker = new _Thread_340(this, dst, out_r, in_w, "JGit-Receive-Pack");
				// Client side of the pipes should report the problem.
				// Clients side will notice we went away, and report.
				// Ignore close failure, we probably crashed above.
				// Ignore close failure, we probably crashed above.
				this.worker.Start();
				this.Init(in_r, out_w);
				this.ReadAdvertisedRefs();
			}

			private sealed class _Thread_340 : Sharpen.Thread
			{
				public _Thread_340(InternalLocalPushConnection _enclosing, Repository dst, PipedInputStream
					 out_r, PipedOutputStream in_w, string baseArg1) : base(baseArg1)
				{
					this._enclosing = _enclosing;
					this.dst = dst;
					this.out_r = out_r;
					this.in_w = in_w;
				}

				public override void Run()
				{
					try
					{
						ReceivePack rp = this._enclosing._enclosing.CreateReceivePack(dst);
						rp.Receive(out_r, in_w, System.Console.OpenStandardError ());
					}
					catch (IOException)
					{
					}
					catch (RuntimeException)
					{
					}
					finally
					{
						try
						{
							out_r.Close();
						}
						catch (IOException)
						{
						}
						try
						{
							in_w.Close();
						}
						catch (IOException)
						{
						}
						dst.Close();
					}
				}

				private readonly InternalLocalPushConnection _enclosing;

				private readonly Repository dst;

				private readonly PipedInputStream out_r;

				private readonly PipedOutputStream in_w;
			}

			public override void Close()
			{
				base.Close();
				if (this.worker != null)
				{
					try
					{
						this.worker.Join();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.worker = null;
					}
				}
			}

			private readonly TransportLocal _enclosing;
		}

		internal class ForkLocalPushConnection : BasePackPushConnection
		{
			private Process receivePack;

			private Sharpen.Thread errorReaderThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public ForkLocalPushConnection(TransportLocal _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				MessageWriter msg = new MessageWriter();
				this.SetMessageWriter(msg);
				this.receivePack = this._enclosing.Spawn(this._enclosing.GetOptionReceivePack());
				InputStream rpErr = this.receivePack.GetErrorStream();
				this.errorReaderThread = new StreamCopyThread(rpErr, msg.GetRawStream());
				this.errorReaderThread.Start();
				InputStream rpIn = this.receivePack.GetInputStream();
				OutputStream rpOut = this.receivePack.GetOutputStream();
				rpIn = new BufferedInputStream(rpIn);
				rpOut = new BufferedOutputStream(rpOut);
				this.Init(rpIn, rpOut);
				this.ReadAdvertisedRefs();
			}

			public override void Close()
			{
				base.Close();
				if (this.receivePack != null)
				{
					try
					{
						this.receivePack.WaitFor();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.receivePack = null;
					}
				}
				if (this.errorReaderThread != null)
				{
					try
					{
						this.errorReaderThread.Join();
					}
					catch (Exception)
					{
					}
					finally
					{
						// Stop waiting and return anyway.
						this.errorReaderThread = null;
					}
				}
			}

			private readonly TransportLocal _enclosing;
		}
	}
}
