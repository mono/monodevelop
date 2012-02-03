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
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Storage.File;
using NGit.Submodule;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>A class used to execute a submodule add command.</summary>
	/// <remarks>
	/// A class used to execute a submodule add command.
	/// This will clone the configured submodule, register the submodule in the
	/// .gitmodules file and the repository config file, and also add the submodule
	/// and .gitmodules file to the index.
	/// </remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-submodule.html"
	/// *      >Git documentation about submodules</a></seealso>
	public class SubmoduleAddCommand : TransportCommand<NGit.Api.SubmoduleAddCommand, 
		Repository>
	{
		private string path;

		private string uri;

		private ProgressMonitor monitor;

		/// <param name="repo"></param>
		protected internal SubmoduleAddCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>Set repository-relative path of submodule</summary>
		/// <param name="path"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleAddCommand SetPath(string path)
		{
			this.path = path;
			return this;
		}

		/// <summary>Set URI to clone submodule from</summary>
		/// <param name="uri"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleAddCommand SetURI(string uri)
		{
			this.uri = uri;
			return this;
		}

		/// <summary>The progress monitor associated with the clone operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the clone operation. By default,
		/// this is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleAddCommand SetProgressMonitor(ProgressMonitor monitor
			)
		{
			this.monitor = monitor;
			return this;
		}

		/// <summary>Is the configured already a submodule in the index?</summary>
		/// <returns>true if submodule exists in index, false otherwise</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		protected internal virtual bool SubmoduleExists()
		{
			TreeFilter filter = PathFilter.Create(path);
			return SubmoduleWalk.ForIndex(repo).SetFilter(filter).Next();
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override Repository Call()
		{
			CheckCallable();
			if (path == null || path.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().pathNotConfigured);
			}
			if (uri == null || uri.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().uriNotConfigured);
			}
			try
			{
				if (SubmoduleExists())
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().submoduleExists
						, path));
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			string resolvedUri;
			try
			{
				resolvedUri = SubmoduleWalk.GetSubmoduleRemoteUrl(repo, uri);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			// Clone submodule repository
			FilePath moduleDirectory = SubmoduleWalk.GetSubmoduleDirectory(repo, path);
			CloneCommand clone = Git.CloneRepository();
			Configure(clone);
			clone.SetDirectory(moduleDirectory);
			clone.SetURI(resolvedUri);
			if (monitor != null)
			{
				clone.SetProgressMonitor(monitor);
			}
			Repository subRepo = clone.Call().GetRepository();
			// Save submodule URL to parent repository's config
			StoredConfig config = repo.GetConfig();
			config.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants.
				CONFIG_KEY_URL, resolvedUri);
			try
			{
				config.Save();
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			// Save path and URL to parent repository's .gitmodules file
			FileBasedConfig modulesConfig = new FileBasedConfig(new FilePath(repo.WorkTree, Constants
				.DOT_GIT_MODULES), repo.FileSystem);
			modulesConfig.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_PATH, path);
			modulesConfig.SetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_URL, uri);
			try
			{
				modulesConfig.Save();
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			AddCommand add = new AddCommand(repo);
			// Add .gitmodules file to parent repository's index
			add.AddFilepattern(Constants.DOT_GIT_MODULES);
			// Add submodule directory to parent repository's index
			add.AddFilepattern(path);
			try
			{
				add.Call();
			}
			catch (NoFilepatternException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			return subRepo;
		}
	}
}
