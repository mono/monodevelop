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
using NGit.Dircache;
using NGit.Revwalk;
using NGit.Transport;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Clone a repository into a new working directory</summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-clone.html"
	/// *      >Git documentation about Clone</a></seealso>
	public class CloneCommand : Callable<Git>
	{
		private string uri;

		private FilePath directory;

		private bool bare;

		private string remote = Constants.DEFAULT_REMOTE_NAME;

		private string branch = Constants.HEAD;

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		private CredentialsProvider credentialsProvider;

		/// <summary>
		/// Executes the
		/// <code>Clone</code>
		/// command.
		/// </summary>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">if the repository can't be created
		/// 	</exception>
		/// <returns>
		/// the newly created
		/// <code>Git</code>
		/// object with associated repository
		/// </returns>
		public virtual Git Call()
		{
			try
			{
				URIish u = new URIish(uri);
				Repository repository = Init(u);
				FetchResult result = Fetch(repository, u);
				Checkout(repository, result);
				return new Git(repository);
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
			catch (InvalidRemoteException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (URISyntaxException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}

		private Repository Init(URIish u)
		{
			InitCommand command = Git.Init();
			command.SetBare(bare);
			if (directory == null)
			{
				directory = new FilePath(u.GetHumanishName(), Constants.DOT_GIT);
			}
			command.SetDirectory(directory);
			return command.Call().GetRepository();
		}

		/// <exception cref="Sharpen.URISyntaxException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		/// <exception cref="NGit.Api.Errors.InvalidRemoteException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private FetchResult Fetch(Repository repo, URIish u)
		{
			// create the remote config and save it
			RemoteConfig config = new RemoteConfig(repo.GetConfig(), remote);
			config.AddURI(u);
			string dst = Constants.R_REMOTES + config.Name;
			RefSpec refSpec = new RefSpec();
			refSpec = refSpec.SetForceUpdate(true);
			refSpec = refSpec.SetSourceDestination(Constants.R_HEADS + "*", dst + "/*");
			//$NON-NLS-1$ //$NON-NLS-2$
			config.AddFetchRefSpec(refSpec);
			config.Update(repo.GetConfig());
			repo.GetConfig().SetString(ConfigConstants.CONFIG_BRANCH_SECTION, branch, ConfigConstants
				.CONFIG_KEY_REMOTE, remote);
			repo.GetConfig().SetString(ConfigConstants.CONFIG_BRANCH_SECTION, branch, ConfigConstants
				.CONFIG_KEY_MERGE, branch);
			repo.GetConfig().Save();
			// run the fetch command
			FetchCommand command = new FetchCommand(repo);
			command.SetRemote(remote);
			command.SetProgressMonitor(monitor);
			command.SetTagOpt(TagOpt.FETCH_TAGS);
			if (credentialsProvider != null)
			{
				command.SetCredentialsProvider(credentialsProvider);
			}
			return command.Call();
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void Checkout(Repository repo, FetchResult result)
		{
			if (branch.StartsWith(Constants.R_HEADS))
			{
				RefUpdate head = repo.UpdateRef(Constants.HEAD);
				head.DisableRefLog();
				head.Link(branch);
			}
			Ref head_1 = result.GetAdvertisedRef(branch);
			if (head_1 == null || head_1.GetObjectId() == null)
			{
				return;
			}
			// throw exception?
			RevCommit commit = ParseCommit(repo, head_1);
			bool detached = !head_1.GetName().StartsWith(Constants.R_HEADS);
			RefUpdate u = repo.UpdateRef(Constants.HEAD, detached);
			u.SetNewObjectId(commit.Id);
			u.ForceUpdate();
			DirCache dc = repo.LockDirCache();
			DirCacheCheckout co = new DirCacheCheckout(repo, dc, commit.Tree);
			co.Checkout();
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private RevCommit ParseCommit(Repository repo, Ref @ref)
		{
			RevWalk rw = new RevWalk(repo);
			RevCommit commit;
			try
			{
				commit = rw.ParseCommit(@ref.GetObjectId());
			}
			finally
			{
				rw.Release();
			}
			return commit;
		}

		/// <param name="uri">the uri to clone from</param>
		/// <returns>this instance</returns>
		public virtual CloneCommand SetURI(string uri)
		{
			this.uri = uri;
			return this;
		}

		/// <summary>The optional directory associated with the clone operation.</summary>
		/// <remarks>
		/// The optional directory associated with the clone operation. If the
		/// directory isn't set, a name associated with the source uri will be used.
		/// </remarks>
		/// <seealso cref="NGit.Transport.URIish.GetHumanishName()">NGit.Transport.URIish.GetHumanishName()
		/// 	</seealso>
		/// <param name="directory">the directory to clone to</param>
		/// <returns>this instance</returns>
		public virtual CloneCommand SetDirectory(FilePath directory)
		{
			this.directory = directory;
			return this;
		}

		/// <param name="bare">whether the cloned repository is bare or not</param>
		/// <returns>this instance</returns>
		public virtual CloneCommand SetBare(bool bare)
		{
			this.bare = bare;
			return this;
		}

		/// <param name="remote">the branch to keep track of in the origin repository</param>
		/// <returns>this instance</returns>
		public virtual CloneCommand SetRemote(string remote)
		{
			this.remote = remote;
			return this;
		}

		/// <param name="branch">the initial branch to check out when cloning the repository</param>
		/// <returns>this instance</returns>
		public virtual CloneCommand SetBranch(string branch)
		{
			this.branch = branch;
			return this;
		}

		/// <summary>The progress monitor associated with the clone operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the clone operation. By default,
		/// this is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual CloneCommand SetProgressMonitor(ProgressMonitor monitor)
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
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual CloneCommand SetCredentialsProvider(CredentialsProvider credentialsProvider
			)
		{
			this.credentialsProvider = credentialsProvider;
			return this;
		}
	}
}
