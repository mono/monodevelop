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

using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Errors;
using NGit.Submodule;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>A class used to execute a submodule sync command.</summary>
	/// <remarks>
	/// A class used to execute a submodule sync command.
	/// This will set the remote URL in a submodule's repository to the current value
	/// in the .gitmodules file.
	/// </remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-submodule.html"
	/// *      >Git documentation about submodules</a></seealso>
	public class SubmoduleSyncCommand : GitCommand<IDictionary<string, string>>
	{
		private readonly ICollection<string> paths;

		/// <param name="repo"></param>
		protected internal SubmoduleSyncCommand(Repository repo) : base(repo)
		{
			paths = new AList<string>();
		}

		/// <summary>Add repository-relative submodule path to synchronize</summary>
		/// <param name="path"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleSyncCommand AddPath(string path)
		{
			paths.AddItem(path);
			return this;
		}

		/// <summary>Get branch that HEAD currently points to</summary>
		/// <param name="subRepo"></param>
		/// <returns>shortened branch name, null on failures</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual string GetHeadBranch(Repository subRepo)
		{
			Ref head = subRepo.GetRef(Constants.HEAD);
			if (head != null && head.IsSymbolic())
			{
				return Repository.ShortenRefName(head.GetLeaf().GetName());
			}
			else
			{
				return null;
			}
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override IDictionary<string, string> Call()
		{
			CheckCallable();
			try
			{
				SubmoduleWalk generator = SubmoduleWalk.ForIndex(repo);
				if (!paths.IsEmpty())
				{
					generator.SetFilter(PathFilterGroup.CreateFromStrings(paths));
				}
				IDictionary<string, string> synced = new Dictionary<string, string>();
				StoredConfig config = repo.GetConfig();
				while (generator.Next())
				{
					string remoteUrl = generator.GetRemoteUrl();
					if (remoteUrl == null)
					{
						continue;
					}
					string path = generator.GetPath();
					config.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants.
						CONFIG_KEY_URL, remoteUrl);
					synced.Put(path, remoteUrl);
					Repository subRepo = generator.GetRepository();
					if (subRepo == null)
					{
						continue;
					}
					StoredConfig subConfig = subRepo.GetConfig();
					// Get name of remote associated with current branch and
					// fall back to default remote name as last resort
					string branch = GetHeadBranch(subRepo);
					string remote = null;
					if (branch != null)
					{
						remote = subConfig.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, branch, ConfigConstants
							.CONFIG_KEY_REMOTE);
					}
					if (remote == null)
					{
						remote = Constants.DEFAULT_REMOTE_NAME;
					}
					subConfig.SetString(ConfigConstants.CONFIG_REMOTE_SECTION, remote, ConfigConstants
						.CONFIG_KEY_URL, remoteUrl);
					subConfig.Save();
				}
				if (!synced.IsEmpty())
				{
					config.Save();
				}
				return synced;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (ConfigInvalidException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}
	}
}
