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
using Sharpen;

namespace NGit.Api
{
	/// <summary>Used to rename branches.</summary>
	/// <remarks>Used to rename branches.</remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-branch.html"
	/// *      >Git documentation about Branch</a></seealso>
	public class RenameBranchCommand : GitCommand<Ref>
	{
		private string oldName;

		private string newName;

		/// <param name="repo"></param>
		protected internal RenameBranchCommand(Repository repo) : base(repo)
		{
		}

		/// <exception cref="NGit.Api.Errors.RefNotFoundException">
		/// if the old branch can not be found (branch with provided old
		/// name does not exist or old name resolves to a tag)
		/// </exception>
		/// <exception cref="NGit.Api.Errors.InvalidRefNameException">
		/// if the provided new name is <code>null</code> or otherwise
		/// invalid
		/// </exception>
		/// <exception cref="NGit.Api.Errors.RefAlreadyExistsException">if a branch with the new name already exists
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.DetachedHeadException">
		/// if rename is tried without specifying the old name and HEAD
		/// is detached
		/// </exception>
		public override Ref Call()
		{
			CheckCallable();
			if (newName == null)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().branchNameInvalid
					, "<null>"));
			}
			try
			{
				string fullOldName;
				string fullNewName;
				if (repo.GetRef(newName) != null)
				{
					throw new RefAlreadyExistsException(MessageFormat.Format(JGitText.Get().refAlreadExists
						, newName));
				}
				if (oldName != null)
				{
					Ref @ref = repo.GetRef(oldName);
					if (@ref == null)
					{
						throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
							, oldName));
					}
					if (@ref.GetName().StartsWith(Constants.R_TAGS))
					{
						throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().renameBranchFailedBecauseTag
							, oldName));
					}
					fullOldName = @ref.GetName();
				}
				else
				{
					fullOldName = repo.GetFullBranch();
					if (ObjectId.IsId(fullOldName))
					{
						throw new DetachedHeadException();
					}
				}
				if (fullOldName.StartsWith(Constants.R_REMOTES))
				{
					fullNewName = Constants.R_REMOTES + newName;
				}
				else
				{
					fullNewName = Constants.R_HEADS + newName;
				}
				if (!Repository.IsValidRefName(fullNewName))
				{
					throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().branchNameInvalid
						, fullNewName));
				}
				RefRename rename = repo.RenameRef(fullOldName, fullNewName);
				RefUpdate.Result renameResult = rename.Rename();
				SetCallable(false);
				bool ok = RefUpdate.Result.RENAMED == renameResult;
				if (ok)
				{
					if (fullNewName.StartsWith(Constants.R_HEADS))
					{
						// move the upstream configuration over to the new branch
						string shortOldName = Sharpen.Runtime.Substring(fullOldName, Constants.R_HEADS.Length
							);
						StoredConfig repoConfig = repo.GetConfig();
						string oldRemote = repoConfig.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, shortOldName
							, ConfigConstants.CONFIG_KEY_REMOTE);
						if (oldRemote != null)
						{
							repoConfig.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, newName, ConfigConstants
								.CONFIG_KEY_REMOTE, oldRemote);
						}
						string oldMerge = repoConfig.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, shortOldName
							, ConfigConstants.CONFIG_KEY_MERGE);
						if (oldMerge != null)
						{
							repoConfig.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, newName, ConfigConstants
								.CONFIG_KEY_MERGE, oldMerge);
						}
						repoConfig.UnsetSection(ConfigConstants.CONFIG_BRANCH_SECTION, shortOldName);
						repoConfig.Save();
					}
				}
				else
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().renameBranchUnexpectedResult
						, renameResult.ToString()));
				}
				Ref resultRef = repo.GetRef(newName);
				if (resultRef == null)
				{
					throw new JGitInternalException(JGitText.Get().renameBranchFailedUnknownReason);
				}
				return resultRef;
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
		}

		/// <param name="newName">the new name</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.RenameBranchCommand SetNewName(string newName)
		{
			CheckCallable();
			this.newName = newName;
			return this;
		}

		/// <param name="oldName">
		/// the name of the branch to rename; if not set, the currently
		/// checked out branch (if any) will be renamed
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.RenameBranchCommand SetOldName(string oldName)
		{
			CheckCallable();
			this.oldName = oldName;
			return this;
		}
	}
}
