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
using NGit.Dircache;
using NGit.Errors;
using NGit.Internal;
using NGit.Storage.File;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Submodule
{
	/// <summary>Walker that visits all submodule entries found in a tree</summary>
	public class SubmoduleWalk
	{
		/// <summary>
		/// Create a generator to walk over the submodule entries currently in the
		/// index
		/// </summary>
		/// <param name="repository"></param>
		/// <returns>generator over submodule index entries</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static NGit.Submodule.SubmoduleWalk ForIndex(Repository repository)
		{
			NGit.Submodule.SubmoduleWalk generator = new NGit.Submodule.SubmoduleWalk(repository
				);
			generator.SetTree(new DirCacheIterator(repository.ReadDirCache()));
			return generator;
		}

		/// <summary>
		/// Create a generator and advance it to the submodule entry at the given
		/// path
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="treeId"></param>
		/// <param name="path"></param>
		/// <returns>generator at given path, null if no submodule at given path</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static NGit.Submodule.SubmoduleWalk ForPath(Repository repository, AnyObjectId
			 treeId, string path)
		{
			NGit.Submodule.SubmoduleWalk generator = new NGit.Submodule.SubmoduleWalk(repository
				);
			generator.SetTree(treeId);
			PathFilter filter = PathFilter.Create(path);
			generator.SetFilter(filter);
			while (generator.Next())
			{
				if (filter.IsDone(generator.walk))
				{
					return generator;
				}
			}
			return null;
		}

		/// <summary>
		/// Create a generator and advance it to the submodule entry at the given
		/// path
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="iterator"></param>
		/// <param name="path"></param>
		/// <returns>generator at given path, null if no submodule at given path</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static NGit.Submodule.SubmoduleWalk ForPath(Repository repository, AbstractTreeIterator
			 iterator, string path)
		{
			NGit.Submodule.SubmoduleWalk generator = new NGit.Submodule.SubmoduleWalk(repository
				);
			generator.SetTree(iterator);
			PathFilter filter = PathFilter.Create(path);
			generator.SetFilter(filter);
			while (generator.Next())
			{
				if (filter.IsDone(generator.walk))
				{
					return generator;
				}
			}
			return null;
		}

		/// <summary>Get submodule directory</summary>
		/// <param name="parent"></param>
		/// <param name="path"></param>
		/// <returns>directory</returns>
		public static FilePath GetSubmoduleDirectory(Repository parent, string path)
		{
			return new FilePath(parent.WorkTree, path);
		}

		/// <summary>Get submodule repository</summary>
		/// <param name="parent"></param>
		/// <param name="path"></param>
		/// <returns>repository or null if repository doesn't exist</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static Repository GetSubmoduleRepository(Repository parent, string path)
		{
			return GetSubmoduleRepository(parent.WorkTree, path);
		}

		/// <summary>Get submodule repository at path</summary>
		/// <param name="parent"></param>
		/// <param name="path"></param>
		/// <returns>repository or null if repository doesn't exist</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static Repository GetSubmoduleRepository(FilePath parent, string path)
		{
			FilePath subWorkTree = new FilePath(parent, path);
			if (!subWorkTree.IsDirectory())
			{
				return null;
			}
			FilePath workTree = new FilePath(parent, path);
			try
			{
				return new RepositoryBuilder().SetMustExist(true).SetFS(FS.DETECTED).SetWorkTree(
					workTree).Build();
			}
			catch (RepositoryNotFoundException)
			{
				//
				//
				//
				//
				return null;
			}
		}

		/// <summary>Resolve submodule repository URL.</summary>
		/// <remarks>
		/// Resolve submodule repository URL.
		/// <p>
		/// This handles relative URLs that are typically specified in the
		/// '.gitmodules' file by resolving them against the remote URL of the parent
		/// repository.
		/// <p>
		/// Relative URLs will be resolved against the parent repository's working
		/// directory if the parent repository has no configured remote URL.
		/// </remarks>
		/// <param name="parent">parent repository</param>
		/// <param name="url">absolute or relative URL of the submodule repository</param>
		/// <returns>resolved URL</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static string GetSubmoduleRemoteUrl(Repository parent, string url)
		{
			if (!url.StartsWith("./") && !url.StartsWith("../"))
			{
				return url;
			}
			string remoteName = null;
			// Look up remote URL associated wit HEAD ref
			Ref @ref = parent.GetRef(Constants.HEAD);
			if (@ref != null)
			{
				if (@ref.IsSymbolic())
				{
					@ref = @ref.GetLeaf();
				}
				remoteName = parent.GetConfig().GetString(ConfigConstants.CONFIG_BRANCH_SECTION, 
					Repository.ShortenRefName(@ref.GetName()), ConfigConstants.CONFIG_KEY_REMOTE);
			}
			// Fall back to 'origin' if current HEAD ref has no remote URL
			if (remoteName == null)
			{
				remoteName = Constants.DEFAULT_REMOTE_NAME;
			}
			string remoteUrl = parent.GetConfig().GetString(ConfigConstants.CONFIG_REMOTE_SECTION
				, remoteName, ConfigConstants.CONFIG_KEY_URL);
			// Fall back to parent repository's working directory if no remote URL
			if (remoteUrl == null)
			{
				remoteUrl = parent.WorkTree.GetAbsolutePath();
				// Normalize slashes to '/'
				if ('\\' == FilePath.separatorChar)
				{
					remoteUrl = remoteUrl.Replace('\\', '/');
				}
			}
			// Remove trailing '/'
			if (remoteUrl[remoteUrl.Length - 1] == '/')
			{
				remoteUrl = Sharpen.Runtime.Substring(remoteUrl, 0, remoteUrl.Length - 1);
			}
			char separator = '/';
			string submoduleUrl = url;
			while (submoduleUrl.Length > 0)
			{
				if (submoduleUrl.StartsWith("./"))
				{
					submoduleUrl = Sharpen.Runtime.Substring(submoduleUrl, 2);
				}
				else
				{
					if (submoduleUrl.StartsWith("../"))
					{
						int lastSeparator = remoteUrl.LastIndexOf('/');
						if (lastSeparator < 1)
						{
							lastSeparator = remoteUrl.LastIndexOf(':');
							separator = ':';
						}
						if (lastSeparator < 1)
						{
							throw new IOException(MessageFormat.Format(JGitText.Get().submoduleParentRemoteUrlInvalid
								, remoteUrl));
						}
						remoteUrl = Sharpen.Runtime.Substring(remoteUrl, 0, lastSeparator);
						submoduleUrl = Sharpen.Runtime.Substring(submoduleUrl, 3);
					}
					else
					{
						break;
					}
				}
			}
			return remoteUrl + separator + submoduleUrl;
		}

		private readonly Repository repository;

		private readonly TreeWalk walk;

		private StoredConfig repoConfig;

		private FileBasedConfig modulesConfig;

		private string path;

		/// <summary>Create submodule generator</summary>
		/// <param name="repository"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public SubmoduleWalk(Repository repository)
		{
			this.repository = repository;
			repoConfig = repository.GetConfig();
			walk = new TreeWalk(repository);
			walk.Recursive = true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException"></exception>
		private void LoadModulesConfig()
		{
			if (modulesConfig == null)
			{
				FilePath modulesFile = new FilePath(repository.WorkTree, Constants.DOT_GIT_MODULES
					);
				FileBasedConfig config = new FileBasedConfig(modulesFile, repository.FileSystem);
				config.Load();
				modulesConfig = config;
			}
		}

		/// <summary>Set tree filter</summary>
		/// <param name="filter"></param>
		/// <returns>this generator</returns>
		public virtual NGit.Submodule.SubmoduleWalk SetFilter(TreeFilter filter)
		{
			walk.Filter = filter;
			return this;
		}

		/// <summary>Set the tree iterator used for finding submodule entries</summary>
		/// <param name="iterator"></param>
		/// <returns>this generator</returns>
		/// <exception cref="NGit.Errors.CorruptObjectException">NGit.Errors.CorruptObjectException
		/// 	</exception>
		public virtual NGit.Submodule.SubmoduleWalk SetTree(AbstractTreeIterator iterator
			)
		{
			walk.AddTree(iterator);
			return this;
		}

		/// <summary>Set the tree used for finding submodule entries</summary>
		/// <param name="treeId"></param>
		/// <returns>this generator</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		public virtual NGit.Submodule.SubmoduleWalk SetTree(AnyObjectId treeId)
		{
			walk.AddTree(treeId);
			return this;
		}

		/// <summary>Reset generator and start new submodule walk</summary>
		/// <returns>this generator</returns>
		public virtual NGit.Submodule.SubmoduleWalk Reset()
		{
			repoConfig = repository.GetConfig();
			modulesConfig = null;
			walk.Reset();
			return this;
		}

		/// <summary>Get directory that will be the root of the submodule's local repository</summary>
		/// <returns>submodule repository directory</returns>
		public virtual FilePath GetDirectory()
		{
			return GetSubmoduleDirectory(repository, path);
		}

		/// <summary>Advance to next submodule in the index tree.</summary>
		/// <remarks>
		/// Advance to next submodule in the index tree.
		/// The object id and path of the next entry can be obtained by calling
		/// <see cref="GetObjectId()">GetObjectId()</see>
		/// and
		/// <see cref="GetPath()">GetPath()</see>
		/// .
		/// </remarks>
		/// <returns>true if entry found, false otherwise</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool Next()
		{
			while (walk.Next())
			{
				if (FileMode.GITLINK != walk.GetFileMode(0))
				{
					continue;
				}
				path = walk.PathString;
				return true;
			}
			path = null;
			return false;
		}

		/// <summary>Get path of current submodule entry</summary>
		/// <returns>path</returns>
		public virtual string GetPath()
		{
			return path;
		}

		/// <summary>Get object id of current submodule entry</summary>
		/// <returns>object id</returns>
		public virtual ObjectId GetObjectId()
		{
			return walk.GetObjectId(0);
		}

		/// <summary>Get the configured path for current entry.</summary>
		/// <remarks>
		/// Get the configured path for current entry. This will be the value from
		/// the .gitmodules file in the current repository's working tree.
		/// </remarks>
		/// <returns>configured path</returns>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetModulesPath()
		{
			LoadModulesConfig();
			return modulesConfig.GetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_PATH);
		}

		/// <summary>Get the configured remote URL for current entry.</summary>
		/// <remarks>
		/// Get the configured remote URL for current entry. This will be the value
		/// from the repository's config.
		/// </remarks>
		/// <returns>configured URL</returns>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetConfigUrl()
		{
			return repoConfig.GetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_URL);
		}

		/// <summary>Get the configured remote URL for current entry.</summary>
		/// <remarks>
		/// Get the configured remote URL for current entry. This will be the value
		/// from the .gitmodules file in the current repository's working tree.
		/// </remarks>
		/// <returns>configured URL</returns>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetModulesUrl()
		{
			LoadModulesConfig();
			return modulesConfig.GetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_URL);
		}

		/// <summary>Get the configured update field for current entry.</summary>
		/// <remarks>
		/// Get the configured update field for current entry. This will be the value
		/// from the repository's config.
		/// </remarks>
		/// <returns>update value</returns>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetConfigUpdate()
		{
			return repoConfig.GetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_UPDATE);
		}

		/// <summary>Get the configured update field for current entry.</summary>
		/// <remarks>
		/// Get the configured update field for current entry. This will be the value
		/// from the .gitmodules file in the current repository's working tree.
		/// </remarks>
		/// <returns>update value</returns>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetModulesUpdate()
		{
			LoadModulesConfig();
			return modulesConfig.GetString(ConfigConstants.CONFIG_SUBMODULE_SECTION, path, ConfigConstants
				.CONFIG_KEY_UPDATE);
		}

		/// <summary>Get repository for current submodule entry</summary>
		/// <returns>repository or null if non-existent</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual Repository GetRepository()
		{
			return GetSubmoduleRepository(repository, path);
		}

		/// <summary>Get commit id that HEAD points to in the current submodule's repository</summary>
		/// <returns>object id of HEAD reference</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual ObjectId GetHead()
		{
			Repository subRepo = GetRepository();
			return subRepo != null ? subRepo.Resolve(Constants.HEAD) : null;
		}

		/// <summary>Get ref that HEAD points to in the current submodule's repository</summary>
		/// <returns>ref name, null on failures</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual string GetHeadRef()
		{
			Repository subRepo = GetRepository();
			if (subRepo == null)
			{
				return null;
			}
			Ref head = subRepo.GetRef(Constants.HEAD);
			return head != null ? head.GetLeaf().GetName() : null;
		}

		/// <summary>Get the resolved remote URL for the current submodule.</summary>
		/// <remarks>
		/// Get the resolved remote URL for the current submodule.
		/// <p>
		/// This method resolves the value of
		/// <see cref="GetModulesUrl()">GetModulesUrl()</see>
		/// to an absolute
		/// URL
		/// </remarks>
		/// <returns>resolved remote URL</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException">NGit.Errors.ConfigInvalidException
		/// 	</exception>
		public virtual string GetRemoteUrl()
		{
			string url = GetModulesUrl();
			return url != null ? GetSubmoduleRemoteUrl(repository, url) : null;
		}
	}
}
