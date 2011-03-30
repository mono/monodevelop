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
		private sealed class _TransportProtocol_84 : TransportProtocol
		{
			public _TransportProtocol_84()
			{
				this.schemeNames = new string[] { "ssh", "ssh+git", "git+ssh" };
				this.schemeSet = Sharpen.Collections.UnmodifiableSet(new LinkedHashSet<string>(Arrays
					.AsList(this.schemeNames)));
			}

			private readonly string[] schemeNames;

			private readonly ICollection<string> schemeSet;

			//$NON-NLS-1$ //$NON-NLS-2$ //$NON-NLS-3$
			public override string GetName()
			{
				return JGitText.Get().transportProtoSSH;
			}

			public override ICollection<string> GetSchemes()
			{
				return this.schemeSet;
			}

			public override ICollection<TransportProtocol.URIishField> GetRequiredFields()
			{
				return Sharpen.Collections.UnmodifiableSet(EnumSet.Of(TransportProtocol.URIishField
					.HOST, TransportProtocol.URIishField.PATH));
			}

			public override ICollection<TransportProtocol.URIishField> GetOptionalFields()
			{
				return Sharpen.Collections.UnmodifiableSet(EnumSet.Of(TransportProtocol.URIishField
					.USER, TransportProtocol.URIishField.PASS, TransportProtocol.URIishField.PORT));
			}

			public override int GetDefaultPort()
			{
				return 22;
			}

			public override bool CanHandle(URIish uri, Repository local, string remoteName)
			{
				if (uri.GetScheme() == null)
				{
					// scp-style URI "host:path" does not have scheme.
					return uri.GetHost() != null && uri.GetPath() != null && uri.GetHost().Length != 
						0 && uri.GetPath().Length != 0;
				}
				return base.CanHandle(uri, local, remoteName);
			}

			/// <exception cref="System.NotSupportedException"></exception>
			public override NGit.Transport.Transport Open(URIish uri, Repository local, string
				 remoteName)
			{
				return new NGit.Transport.TransportGitSsh(local, uri);
			}
		}

		internal static readonly TransportProtocol PROTO_SSH = new _TransportProtocol_84(
			);

		protected internal TransportGitSsh(Repository local, URIish uri) : base(local, uri
			)
		{
			if (UseExtSession())
			{
				SetSshSessionFactory(new _SshSessionFactory_134(this));
			}
		}

		private sealed class _SshSessionFactory_134 : SshSessionFactory
		{
			TransportGitSsh _enclosing;
			
			public _SshSessionFactory_134(TransportGitSsh _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public override RemoteSession GetSession(URIish uri2, CredentialsProvider credentialsProvider
				, FS fs, int tms)
			{
				return new TransportGitSsh.ExtSession(_enclosing);
			}
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

		private static bool UseExtSession()
		{
			return SystemReader.GetInstance().Getenv("GIT_SSH") != null;
		}

		private class ExtSession : RemoteSession
		{
			/// <exception cref="NGit.Errors.TransportException"></exception>
			public virtual SystemProcess Exec(string command, int timeout)
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
				args.AddItem(command);
				ProcessStartInfo pb = new ProcessStartInfo();
				pb.SetCommand(args);
				if (this._enclosing.local.Directory != null)
				{
					pb.EnvironmentVariables.Put(Constants.GIT_DIR_KEY, this._enclosing.local.Directory
						.GetPath());
				}
				try
				{
					return pb.Start();
				}
				catch (IOException err)
				{
					throw new TransportException(err.Message, err);
				}
			}

			public virtual void Disconnect()
			{
			}

			internal ExtSession(TransportGitSsh _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TransportGitSsh _enclosing;
			// Nothing to do
		}

		private class SshFetchConnection : BasePackFetchConnection
		{
			private readonly SystemProcess process;

			private StreamCopyThread errorThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public SshFetchConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				try
				{
					this.process = this._enclosing.GetSession().Exec(this._enclosing.CommandFor(this.
						_enclosing.GetOptionUploadPack()), this._enclosing.GetTimeout());
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					InputStream upErr = this.process.GetErrorStream();
					this.errorThread = new StreamCopyThread(upErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(this.process.GetInputStream(), this.process.GetOutputStream());
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
					this._enclosing.CheckExecFailure(this.process.ExitValue(), this._enclosing.GetOptionUploadPack
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
				if (this.process != null)
				{
					this.process.Destroy();
				}
			}

			private readonly TransportGitSsh _enclosing;
		}

		private class SshPushConnection : BasePackPushConnection
		{
			private readonly SystemProcess process;

			private StreamCopyThread errorThread;

			/// <exception cref="NGit.Errors.TransportException"></exception>
			public SshPushConnection(TransportGitSsh _enclosing) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				try
				{
					this.process = this._enclosing.GetSession().Exec(this._enclosing.CommandFor(this.
						_enclosing.GetOptionReceivePack()), this._enclosing.GetTimeout());
					MessageWriter msg = new MessageWriter();
					this.SetMessageWriter(msg);
					InputStream rpErr = this.process.GetErrorStream();
					this.errorThread = new StreamCopyThread(rpErr, msg.GetRawStream());
					this.errorThread.Start();
					this.Init(this.process.GetInputStream(), this.process.GetOutputStream());
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
					this._enclosing.CheckExecFailure(this.process.ExitValue(), this._enclosing.GetOptionReceivePack
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
				if (this.process != null)
				{
					this.process.Destroy();
				}
			}

			private readonly TransportGitSsh _enclosing;
		}
	}
}
