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

using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	/// <summary>The Pull command</summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-pull.html"
	/// *      >Git documentation about Pull</a></seealso>
	public class PullCommand : GitCommand<PullResult>
	{
		private int timeout = 0;

		private static readonly string DOT = ".";

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		private CredentialsProvider credentialsProvider;

		/// <param name="repo"></param>
		protected internal PullCommand(Repository repo) : base(repo)
		{
		}

		/// <param name="timeout">in seconds</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.PullCommand SetTimeout(int timeout)
		{
			this.timeout = timeout;
			return this;
		}

		/// <param name="monitor">a progress monitor</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.PullCommand SetProgressMonitor(ProgressMonitor monitor)
		{
			this.monitor = monitor;
			return this;
		}

		/// <param name="credentialsProvider">
		/// the
		/// <see cref="NGit.Transport.CredentialsProvider">NGit.Transport.CredentialsProvider
		/// 	</see>
		/// to use
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.PullCommand SetCredentialsProvider(CredentialsProvider credentialsProvider
			)
		{
			CheckCallable();
			this.credentialsProvider = credentialsProvider;
			return this;
		}

		/// <summary>
		/// Executes the
		/// <code>Pull</code>
		/// command with all the options and parameters
		/// collected by the setter methods (e.g.
		/// <see cref="SetProgressMonitor(NGit.ProgressMonitor)">SetProgressMonitor(NGit.ProgressMonitor)
		/// 	</see>
		/// ) of this class. Each
		/// instance of this class should only be used for one invocation of the
		/// command. Don't call this method twice on an instance.
		/// </summary>
		/// <returns>the result of the pull</returns>
		/// <exception cref="NGit.Api.Errors.WrongRepositoryStateException"></exception>
		/// <exception cref="NGit.Api.Errors.InvalidConfigurationException"></exception>
		/// <exception cref="NGit.Api.Errors.DetachedHeadException"></exception>
		/// <exception cref="NGit.Api.Errors.InvalidRemoteException"></exception>
		/// <exception cref="NGit.Api.Errors.CanceledException"></exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException"></exception>
		public override PullResult Call()
		{
			CheckCallable();
			monitor.BeginTask(JGitText.Get().pullTaskName, 2);
			string branchName;
			try
			{
				string fullBranch = repo.GetFullBranch();
				if (!fullBranch.StartsWith(Constants.R_HEADS))
				{
					// we can not pull if HEAD is detached and branch is not
					// specified explicitly
					throw new DetachedHeadException();
				}
				branchName = Sharpen.Runtime.Substring(fullBranch, Constants.R_HEADS.Length);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfPullCommand
					, e);
			}
			if (!repo.GetRepositoryState().Equals(RepositoryState.SAFE))
			{
				throw new WrongRepositoryStateException(MessageFormat.Format(JGitText.Get().cannotPullOnARepoWithState
					, repo.GetRepositoryState().Name()));
			}
			// get the configured remote for the currently checked out branch
			// stored in configuration key branch.<branch name>.remote
			Config repoConfig = repo.GetConfig();
			string remote = repoConfig.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, branchName
				, ConfigConstants.CONFIG_KEY_REMOTE);
			if (remote == null)
			{
				// fall back to default remote
				remote = Constants.DEFAULT_REMOTE_NAME;
			}
			// get the name of the branch in the remote repository
			// stored in configuration key branch.<branch name>.merge
			string remoteBranchName = repoConfig.GetString(ConfigConstants.CONFIG_BRANCH_SECTION
				, branchName, ConfigConstants.CONFIG_KEY_MERGE);
			// check if the branch is configured for pull-rebase
			bool doRebase = repoConfig.GetBoolean(ConfigConstants.CONFIG_BRANCH_SECTION, branchName
				, ConfigConstants.CONFIG_KEY_REBASE, false);
			if (remoteBranchName == null)
			{
				string missingKey = ConfigConstants.CONFIG_BRANCH_SECTION + DOT + branchName + DOT
					 + ConfigConstants.CONFIG_KEY_MERGE;
				throw new InvalidConfigurationException(MessageFormat.Format(JGitText.Get().missingConfigurationForKey
					, missingKey));
			}
			bool isRemote = !remote.Equals(".");
			string remoteUri;
			FetchResult fetchRes;
			if (isRemote)
			{
				remoteUri = repoConfig.GetString("remote", remote, ConfigConstants.CONFIG_KEY_URL
					);
				if (remoteUri == null)
				{
					string missingKey = ConfigConstants.CONFIG_REMOTE_SECTION + DOT + remote + DOT + 
						ConfigConstants.CONFIG_KEY_URL;
					throw new InvalidConfigurationException(MessageFormat.Format(JGitText.Get().missingConfigurationForKey
						, missingKey));
				}
				if (monitor.IsCancelled())
				{
					throw new CanceledException(MessageFormat.Format(JGitText.Get().operationCanceled
						, JGitText.Get().pullTaskName));
				}
				FetchCommand fetch = new FetchCommand(repo);
				fetch.SetRemote(remote);
				fetch.SetProgressMonitor(monitor);
				fetch.SetTimeout(this.timeout);
				fetch.SetCredentialsProvider(credentialsProvider);
				fetchRes = fetch.Call();
			}
			else
			{
				// we can skip the fetch altogether
				remoteUri = "local repository";
				fetchRes = null;
			}
			monitor.Update(1);
			if (monitor.IsCancelled())
			{
				throw new CanceledException(MessageFormat.Format(JGitText.Get().operationCanceled
					, JGitText.Get().pullTaskName));
			}
			// we check the updates to see which of the updated branches
			// corresponds
			// to the remote branch name
			AnyObjectId commitToMerge;
			if (isRemote)
			{
				Ref r = null;
				if (fetchRes != null)
				{
					r = fetchRes.GetAdvertisedRef(remoteBranchName);
					if (r == null)
					{
						r = fetchRes.GetAdvertisedRef(Constants.R_HEADS + remoteBranchName);
					}
				}
				if (r == null)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().couldNotGetAdvertisedRef
						, remoteBranchName));
				}
				else
				{
					commitToMerge = r.GetObjectId();
				}
			}
			else
			{
				try
				{
					commitToMerge = repo.Resolve(remoteBranchName);
					if (commitToMerge == null)
					{
						throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
							, remoteBranchName));
					}
				}
				catch (IOException e)
				{
					throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfPullCommand
						, e);
				}
			}
			PullResult result;
			if (doRebase)
			{
				RebaseCommand rebase = new RebaseCommand(repo);
				try
				{
					RebaseResult rebaseRes = rebase.SetUpstream(commitToMerge).SetProgressMonitor(monitor
						).SetOperation(RebaseCommand.Operation.BEGIN).Call();
					result = new PullResult(fetchRes, remote, rebaseRes);
				}
				catch (NoHeadException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (RefNotFoundException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (JGitInternalException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (GitAPIException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
			}
			else
			{
				MergeCommand merge = new MergeCommand(repo);
				merge.Include("branch \'" + remoteBranchName + "\' of " + remoteUri, commitToMerge
					);
				MergeCommandResult mergeRes;
				try
				{
					mergeRes = merge.Call();
					monitor.Update(1);
					result = new PullResult(fetchRes, remote, mergeRes);
				}
				catch (NoHeadException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (ConcurrentRefUpdateException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (NGit.Api.Errors.CheckoutConflictException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (InvalidMergeHeadsException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (WrongRepositoryStateException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
				catch (NoMessageException e)
				{
					throw new JGitInternalException(e.Message, e);
				}
			}
			monitor.EndTask();
			return result;
		}
	}
}
