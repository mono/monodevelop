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
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Push</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command.
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-push.html"
	/// *      >Git documentation about Push</a></seealso>
	public class PushCommand : GitCommand<Iterable<PushResult>>
	{
		private string remote = Constants.DEFAULT_REMOTE_NAME;

		private IList<RefSpec> refSpecs;

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		private string receivePack = RemoteConfig.DEFAULT_RECEIVE_PACK;

		private bool dryRun;

		private bool force;

		private bool thin = NGit.Transport.Transport.DEFAULT_PUSH_THIN;

		private int timeout;

		private CredentialsProvider credentialsProvider;

		/// <param name="repo"></param>
		protected internal PushCommand(Repository repo) : base(repo)
		{
			refSpecs = new AList<RefSpec>(3);
		}

		/// <summary>
		/// Executes the
		/// <code>push</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command (means: one
		/// call to
		/// <see cref="Call()">Call()</see>
		/// )
		/// </summary>
		/// <returns>
		/// an iteration over
		/// <see cref="NGit.Transport.PushResult">NGit.Transport.PushResult</see>
		/// objects
		/// </returns>
		/// <exception cref="NGit.Api.Errors.InvalidRemoteException">when called with an invalid remote uri
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// .
		/// </exception>
		public override Iterable<PushResult> Call()
		{
			CheckCallable();
			AList<PushResult> pushResults = new AList<PushResult>(3);
			try
			{
				if (force)
				{
					IList<RefSpec> orig = new AList<RefSpec>(refSpecs);
					refSpecs.Clear();
					foreach (RefSpec spec in orig)
					{
						refSpecs.AddItem(spec.SetForceUpdate(true));
					}
				}
				IList<NGit.Transport.Transport> transports;
				transports = NGit.Transport.Transport.OpenAll(repo, remote, NGit.Transport.Transport.Operation.PUSH
					);
				foreach (NGit.Transport.Transport transport in transports)
				{
					if (0 <= timeout)
					{
						transport.SetTimeout(timeout);
					}
					transport.SetPushThin(thin);
					if (receivePack != null)
					{
						transport.SetOptionReceivePack(receivePack);
					}
					transport.SetDryRun(dryRun);
					if (credentialsProvider != null)
					{
						transport.SetCredentialsProvider(credentialsProvider);
					}
					ICollection<RemoteRefUpdate> toPush = transport.FindRemoteRefUpdatesFor(refSpecs);
					try
					{
						PushResult result = transport.Push(monitor, toPush);
						pushResults.AddItem(result);
					}
					catch (TransportException e)
					{
						throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfPushCommand
							, e);
					}
					finally
					{
						transport.Close();
					}
				}
			}
			catch (URISyntaxException)
			{
				throw new InvalidRemoteException(MessageFormat.Format(JGitText.Get().invalidRemote
					, remote));
			}
			catch (NotSupportedException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfPushCommand
					, e);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfPushCommand
					, e);
			}
			return pushResults.AsIterable ();
		}

		/// <summary>The remote (uri or name) used for the push operation.</summary>
		/// <remarks>
		/// The remote (uri or name) used for the push operation. If no remote is
		/// set, the default value of <code>Constants.DEFAULT_REMOTE_NAME</code> will
		/// be used.
		/// </remarks>
		/// <seealso cref="NGit.Constants.DEFAULT_REMOTE_NAME">NGit.Constants.DEFAULT_REMOTE_NAME
		/// 	</seealso>
		/// <param name="remote"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetRemote(string remote)
		{
			CheckCallable();
			this.remote = remote;
			return this;
		}

		/// <returns>the remote used for the remote operation</returns>
		public virtual string GetRemote()
		{
			return remote;
		}

		/// <summary>The remote executable providing receive-pack service for pack transports.
		/// 	</summary>
		/// <remarks>
		/// The remote executable providing receive-pack service for pack transports.
		/// If no receive-pack is set, the default value of
		/// <code>RemoteConfig.DEFAULT_RECEIVE_PACK</code> will be used.
		/// </remarks>
		/// <seealso cref="NGit.Transport.RemoteConfig.DEFAULT_RECEIVE_PACK">NGit.Transport.RemoteConfig.DEFAULT_RECEIVE_PACK
		/// 	</seealso>
		/// <param name="receivePack"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetReceivePack(string receivePack)
		{
			CheckCallable();
			this.receivePack = receivePack;
			return this;
		}

		/// <returns>the receive-pack used for the remote operation</returns>
		public virtual string GetReceivePack()
		{
			return receivePack;
		}

		/// <param name="timeout">the timeout used for the push operation</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetTimeout(int timeout)
		{
			CheckCallable();
			this.timeout = timeout;
			return this;
		}

		/// <returns>the timeout used for the push operation</returns>
		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <returns>the progress monitor for the push operation</returns>
		public virtual ProgressMonitor GetProgressMonitor()
		{
			return monitor;
		}

		/// <summary>The progress monitor associated with the push operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the push operation. By default, this
		/// is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetProgressMonitor(ProgressMonitor monitor)
		{
			CheckCallable();
			this.monitor = monitor;
			return this;
		}

		/// <returns>the ref specs</returns>
		public virtual IList<RefSpec> GetRefSpecs()
		{
			return refSpecs;
		}

		/// <summary>The ref specs to be used in the push operation</summary>
		/// <param name="specs"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetRefSpecs(params RefSpec[] specs)
		{
			CheckCallable();
			this.refSpecs.Clear();
			Sharpen.Collections.AddAll(refSpecs, specs);
			return this;
		}

		/// <summary>The ref specs to be used in the push operation</summary>
		/// <param name="specs"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetRefSpecs(IList<RefSpec> specs)
		{
			CheckCallable();
			this.refSpecs.Clear();
			Sharpen.Collections.AddAll(this.refSpecs, specs);
			return this;
		}

		/// <returns>the dry run preference for the push operation</returns>
		public virtual bool IsDryRun()
		{
			return dryRun;
		}

		/// <summary>Sets whether the push operation should be a dry run</summary>
		/// <param name="dryRun"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetDryRun(bool dryRun)
		{
			CheckCallable();
			this.dryRun = dryRun;
			return this;
		}

		/// <returns>the thin-pack preference for push operation</returns>
		public virtual bool IsThin()
		{
			return thin;
		}

		/// <summary>Sets the thin-pack preference for push operation.</summary>
		/// <remarks>
		/// Sets the thin-pack preference for push operation.
		/// Default setting is Transport.DEFAULT_PUSH_THIN
		/// </remarks>
		/// <param name="thin"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetThin(bool thin)
		{
			CheckCallable();
			this.thin = thin;
			return this;
		}

		/// <returns>the force preference for push operation</returns>
		public virtual bool IsForce()
		{
			return force;
		}

		/// <summary>Sets the force preference for push operation.</summary>
		/// <remarks>Sets the force preference for push operation.</remarks>
		/// <param name="force"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetForce(bool force)
		{
			CheckCallable();
			this.force = force;
			return this;
		}

		/// <param name="credentialsProvider">
		/// the
		/// <see cref="NGit.Transport.CredentialsProvider">NGit.Transport.CredentialsProvider
		/// 	</see>
		/// to use
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.PushCommand SetCredentialsProvider(CredentialsProvider credentialsProvider
			)
		{
			CheckCallable();
			this.credentialsProvider = credentialsProvider;
			return this;
		}
	}
}
