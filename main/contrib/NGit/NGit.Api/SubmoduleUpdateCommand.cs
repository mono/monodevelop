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
using NGit.Dircache;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Submodule;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>A class used to execute a submodule update command.</summary>
	/// <remarks>A class used to execute a submodule update command.</remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-submodule.html"
	/// *      >Git documentation about submodules</a></seealso>
	public class SubmoduleUpdateCommand : TransportCommand<NGit.Api.SubmoduleUpdateCommand
		, ICollection<string>>
	{
		private ProgressMonitor monitor;

		private readonly ICollection<string> paths;

		/// <param name="repo"></param>
		protected internal SubmoduleUpdateCommand(Repository repo) : base(repo)
		{
			paths = new AList<string>();
		}

		/// <summary>The progress monitor associated with the clone operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the clone operation. By default,
		/// this is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleUpdateCommand SetProgressMonitor(ProgressMonitor
			 monitor)
		{
			this.monitor = monitor;
			return this;
		}

		/// <summary>Add repository-relative submodule path to initialize</summary>
		/// <param name="path"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.SubmoduleUpdateCommand AddPath(string path)
		{
			paths.AddItem(path);
			return this;
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override ICollection<string> Call()
		{
			CheckCallable();
			try
			{
				SubmoduleWalk generator = SubmoduleWalk.ForIndex(repo);
				if (!paths.IsEmpty())
				{
					generator.SetFilter(PathFilterGroup.CreateFromStrings(paths));
				}
				IList<string> updated = new AList<string>();
				while (generator.Next())
				{
					// Skip submodules not registered in .gitmodules file
					if (generator.GetModulesPath() == null)
					{
						continue;
					}
					// Skip submodules not registered in parent repository's config
					string url = generator.GetConfigUrl();
					if (url == null)
					{
						continue;
					}
					Repository submoduleRepo = generator.GetRepository();
					// Clone repository is not present
					if (submoduleRepo == null)
					{
						CloneCommand clone = Git.CloneRepository();
						Configure(clone);
						clone.SetURI(url);
						clone.SetDirectory(generator.GetDirectory());
						if (monitor != null)
						{
							clone.SetProgressMonitor(monitor);
						}
						submoduleRepo = clone.Call().GetRepository();
					}
					RevWalk walk = new RevWalk(submoduleRepo);
					RevCommit commit = walk.ParseCommit(generator.GetObjectId());
					string update = generator.GetConfigUpdate();
					if (ConfigConstants.CONFIG_KEY_MERGE.Equals(update))
					{
						MergeCommand merge = new MergeCommand(submoduleRepo);
						merge.Include(commit);
						merge.Call();
					}
					else
					{
						if (ConfigConstants.CONFIG_KEY_REBASE.Equals(update))
						{
							RebaseCommand rebase = new RebaseCommand(submoduleRepo);
							rebase.SetUpstream(commit);
							rebase.Call();
						}
						else
						{
							// Checkout commit referenced in parent repository's index
							// as a detached HEAD
							DirCacheCheckout co = new DirCacheCheckout(submoduleRepo, submoduleRepo.LockDirCache
								(), commit.Tree);
							co.SetFailOnConflict(true);
							co.Checkout();
							RefUpdate refUpdate = submoduleRepo.UpdateRef(Constants.HEAD, true);
							refUpdate.SetNewObjectId(commit);
							refUpdate.ForceUpdate();
						}
					}
					updated.AddItem(generator.GetPath());
				}
				return updated;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (ConfigInvalidException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			catch (GitAPIException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
		}
	}
}
