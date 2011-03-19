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
	/// <code>Fetch</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command.
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-fetch.html"
	/// *      >Git documentation about Fetch</a></seealso>
	public class FetchCommand : GitCommand<FetchResult>
	{
		private string remote = Constants.DEFAULT_REMOTE_NAME;

		private IList<RefSpec> refSpecs;

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		private bool checkFetchedObjects;

		private bool removeDeletedRefs;

		private bool dryRun;

		private bool thin = NGit.Transport.Transport.DEFAULT_FETCH_THIN;

		private int timeout;

		private CredentialsProvider credentialsProvider;

		private TagOpt tagOption;

		/// <param name="repo"></param>
		protected internal FetchCommand(Repository repo) : base(repo)
		{
			refSpecs = new AList<RefSpec>(3);
		}

		/// <summary>
		/// Executes the
		/// <code>fetch</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command (means: one
		/// call to
		/// <see cref="Call()">Call()</see>
		/// )
		/// </summary>
		/// <returns>
		/// a
		/// <see cref="NGit.Transport.FetchResult">NGit.Transport.FetchResult</see>
		/// object representing the successful fetch
		/// result
		/// </returns>
		/// <exception cref="NGit.Api.Errors.InvalidRemoteException">when called with an invalid remote uri
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// .
		/// </exception>
		public override FetchResult Call()
		{
			CheckCallable();
			try
			{
				NGit.Transport.Transport transport = NGit.Transport.Transport.Open(repo, remote);
				try
				{
					transport.SetCheckFetchedObjects(checkFetchedObjects);
					transport.SetRemoveDeletedRefs(removeDeletedRefs);
					transport.SetTimeout(timeout);
					transport.SetDryRun(dryRun);
					if (tagOption != null)
					{
						transport.SetTagOpt(tagOption);
					}
					transport.SetFetchThin(thin);
					if (credentialsProvider != null)
					{
						transport.SetCredentialsProvider(credentialsProvider);
					}
					FetchResult result = transport.Fetch(monitor, refSpecs);
					return result;
				}
				finally
				{
					transport.Close();
				}
			}
			catch (NoRemoteRepositoryException e)
			{
				throw new InvalidRemoteException(MessageFormat.Format(JGitText.Get().invalidRemote
					, remote), e);
			}
			catch (TransportException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfFetchCommand
					, e);
			}
			catch (URISyntaxException)
			{
				throw new InvalidRemoteException(MessageFormat.Format(JGitText.Get().invalidRemote
					, remote));
			}
			catch (NotSupportedException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfFetchCommand
					, e);
			}
		}

		/// <summary>The remote (uri or name) used for the fetch operation.</summary>
		/// <remarks>
		/// The remote (uri or name) used for the fetch operation. If no remote is
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
		public virtual NGit.Api.FetchCommand SetRemote(string remote)
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

		/// <param name="timeout">the timeout used for the fetch operation</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetTimeout(int timeout)
		{
			CheckCallable();
			this.timeout = timeout;
			return this;
		}

		/// <returns>the timeout used for the fetch operation</returns>
		public virtual int GetTimeout()
		{
			return timeout;
		}

		/// <returns>whether to check received objects checked for validity</returns>
		public virtual bool IsCheckFetchedObjects()
		{
			return checkFetchedObjects;
		}

		/// <summary>If set to true, objects received will be checked for validity</summary>
		/// <param name="checkFetchedObjects"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetCheckFetchedObjects(bool checkFetchedObjects
			)
		{
			CheckCallable();
			this.checkFetchedObjects = checkFetchedObjects;
			return this;
		}

		/// <returns>whether or not to remove refs which no longer exist in the source</returns>
		public virtual bool IsRemoveDeletedRefs()
		{
			return removeDeletedRefs;
		}

		/// <summary>If set to true, refs are removed which no longer exist in the source</summary>
		/// <param name="removeDeletedRefs"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetRemoveDeletedRefs(bool removeDeletedRefs)
		{
			CheckCallable();
			this.removeDeletedRefs = removeDeletedRefs;
			return this;
		}

		/// <returns>the progress monitor for the fetch operation</returns>
		public virtual ProgressMonitor GetProgressMonitor()
		{
			return monitor;
		}

		/// <summary>The progress monitor associated with the fetch operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the fetch operation. By default,
		/// this is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetProgressMonitor(ProgressMonitor monitor)
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

		/// <summary>The ref specs to be used in the fetch operation</summary>
		/// <param name="specs"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetRefSpecs(params RefSpec[] specs)
		{
			CheckCallable();
			this.refSpecs.Clear();
			foreach (RefSpec spec in specs)
			{
				refSpecs.AddItem(spec);
			}
			return this;
		}

		/// <summary>The ref specs to be used in the fetch operation</summary>
		/// <param name="specs"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetRefSpecs(IList<RefSpec> specs)
		{
			CheckCallable();
			this.refSpecs.Clear();
			Sharpen.Collections.AddAll(this.refSpecs, specs);
			return this;
		}

		/// <returns>the dry run preference for the fetch operation</returns>
		public virtual bool IsDryRun()
		{
			return dryRun;
		}

		/// <summary>Sets whether the fetch operation should be a dry run</summary>
		/// <param name="dryRun"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetDryRun(bool dryRun)
		{
			CheckCallable();
			this.dryRun = dryRun;
			return this;
		}

		/// <returns>the thin-pack preference for fetch operation</returns>
		public virtual bool IsThin()
		{
			return thin;
		}

		/// <summary>Sets the thin-pack preference for fetch operation.</summary>
		/// <remarks>
		/// Sets the thin-pack preference for fetch operation.
		/// Default setting is Transport.DEFAULT_FETCH_THIN
		/// </remarks>
		/// <param name="thin"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetThin(bool thin)
		{
			CheckCallable();
			this.thin = thin;
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
		public virtual NGit.Api.FetchCommand SetCredentialsProvider(CredentialsProvider credentialsProvider
			)
		{
			CheckCallable();
			this.credentialsProvider = credentialsProvider;
			return this;
		}

		/// <summary>Sets the specification of annotated tag behavior during fetch</summary>
		/// <param name="tagOpt"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.FetchCommand SetTagOpt(TagOpt tagOpt)
		{
			CheckCallable();
			this.tagOption = tagOpt;
			return this;
		}
	}
}
