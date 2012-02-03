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
using NGit.Errors;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Used to create a local branch.</summary>
	/// <remarks>Used to create a local branch.</remarks>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-branch.html"
	/// *      >Git documentation about Branch</a></seealso>
	public class CreateBranchCommand : GitCommand<Ref>
	{
		private string name;

		private bool force = false;

		private CreateBranchCommand.SetupUpstreamMode upstreamMode;

		private string startPoint = Constants.HEAD;

		private RevCommit startCommit;

		/// <summary>
		/// The modes available for setting up the upstream configuration
		/// (corresponding to the --set-upstream, --track, --no-track options
		/// </summary>
		public enum SetupUpstreamMode
		{
			TRACK,
			NOTRACK,
			SET_UPSTREAM,
			NOT_SET
		}

		/// <param name="repo"></param>
		protected internal CreateBranchCommand(Repository repo) : base(repo)
		{
		}

		/// <exception cref="NGit.Api.Errors.RefAlreadyExistsException">
		/// when trying to create (without force) a branch with a name
		/// that already exists
		/// </exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException">if the start point can not be found
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.InvalidRefNameException">
		/// if the provided name is <code>null</code> or otherwise
		/// invalid
		/// </exception>
		/// <returns>the newly created branch</returns>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override Ref Call()
		{
			CheckCallable();
			ProcessOptions();
			RevWalk revWalk = new RevWalk(repo);
			try
			{
				Ref refToCheck = repo.GetRef(name);
				bool exists = refToCheck != null && refToCheck.GetName().StartsWith(Constants.R_HEADS
					);
				if (!force && exists)
				{
					throw new RefAlreadyExistsException(MessageFormat.Format(JGitText.Get().refAlreadyExists
						, name));
				}
				ObjectId startAt = GetStartPoint();
				string startPointFullName = null;
				if (startPoint != null)
				{
					Ref baseRef = repo.GetRef(startPoint);
					if (baseRef != null)
					{
						startPointFullName = baseRef.GetName();
					}
				}
				// determine whether we are based on a commit,
				// a branch, or a tag and compose the reflog message
				string refLogMessage;
				string baseBranch = string.Empty;
				if (startPointFullName == null)
				{
					string baseCommit;
					if (startCommit != null)
					{
						baseCommit = startCommit.GetShortMessage();
					}
					else
					{
						RevCommit commit = revWalk.ParseCommit(repo.Resolve(startPoint));
						baseCommit = commit.GetShortMessage();
					}
					if (exists)
					{
						refLogMessage = "branch: Reset start-point to commit " + baseCommit;
					}
					else
					{
						refLogMessage = "branch: Created from commit " + baseCommit;
					}
				}
				else
				{
					if (startPointFullName.StartsWith(Constants.R_HEADS) || startPointFullName.StartsWith
						(Constants.R_REMOTES))
					{
						baseBranch = startPointFullName;
						if (exists)
						{
							refLogMessage = "branch: Reset start-point to branch " + startPointFullName;
						}
						else
						{
							// TODO
							refLogMessage = "branch: Created from branch " + baseBranch;
						}
					}
					else
					{
						startAt = revWalk.Peel(revWalk.ParseAny(startAt));
						if (exists)
						{
							refLogMessage = "branch: Reset start-point to tag " + startPointFullName;
						}
						else
						{
							refLogMessage = "branch: Created from tag " + startPointFullName;
						}
					}
				}
				RefUpdate updateRef = repo.UpdateRef(Constants.R_HEADS + name);
				updateRef.SetNewObjectId(startAt);
				updateRef.SetRefLogMessage(refLogMessage, false);
				RefUpdate.Result updateResult;
				if (exists && force)
				{
					updateResult = updateRef.ForceUpdate();
				}
				else
				{
					updateResult = updateRef.Update();
				}
				SetCallable(false);
				bool ok = false;
				switch (updateResult)
				{
					case RefUpdate.Result.NEW:
					{
						ok = !exists;
						break;
					}

					case RefUpdate.Result.NO_CHANGE:
					case RefUpdate.Result.FAST_FORWARD:
					case RefUpdate.Result.FORCED:
					{
						ok = exists;
						break;
					}

					default:
					{
						break;
						break;
					}
				}
				if (!ok)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().createBranchUnexpectedResult
						, updateResult.ToString()));
				}
				Ref result = repo.GetRef(name);
				if (result == null)
				{
					throw new JGitInternalException(JGitText.Get().createBranchFailedUnknownReason);
				}
				if (baseBranch.Length == 0)
				{
					return result;
				}
				// if we are based on another branch, see
				// if we need to configure upstream configuration: first check
				// whether the setting was done explicitly
				bool doConfigure;
				if (upstreamMode == CreateBranchCommand.SetupUpstreamMode.SET_UPSTREAM || upstreamMode
					 == CreateBranchCommand.SetupUpstreamMode.TRACK)
				{
					// explicitly set to configure
					doConfigure = true;
				}
				else
				{
					if (upstreamMode == CreateBranchCommand.SetupUpstreamMode.NOTRACK)
					{
						// explicitly set to not configure
						doConfigure = false;
					}
					else
					{
						// if there was no explicit setting, check the configuration
						string autosetupflag = repo.GetConfig().GetString(ConfigConstants.CONFIG_BRANCH_SECTION
							, null, ConfigConstants.CONFIG_KEY_AUTOSETUPMERGE);
						if ("false".Equals(autosetupflag))
						{
							doConfigure = false;
						}
						else
						{
							if ("always".Equals(autosetupflag))
							{
								doConfigure = true;
							}
							else
							{
								// in this case, the default is to configure
								// only in case the base branch was a remote branch
								doConfigure = baseBranch.StartsWith(Constants.R_REMOTES);
							}
						}
					}
				}
				if (doConfigure)
				{
					StoredConfig config = repo.GetConfig();
					string[] tokens = baseBranch.Split("/", 4);
					bool isRemote = tokens[1].Equals("remotes");
					if (isRemote)
					{
						// refs/remotes/<remote name>/<branch>
						string remoteName = tokens[2];
						string branchName = tokens[3];
						config.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, name, ConfigConstants.CONFIG_KEY_REMOTE
							, remoteName);
						config.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, name, ConfigConstants.CONFIG_KEY_MERGE
							, Constants.R_HEADS + branchName);
					}
					else
					{
						// set "." as remote
						config.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, name, ConfigConstants.CONFIG_KEY_REMOTE
							, ".");
						config.SetString(ConfigConstants.CONFIG_BRANCH_SECTION, name, ConfigConstants.CONFIG_KEY_MERGE
							, baseBranch);
					}
					config.Save();
				}
				return result;
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
			finally
			{
				revWalk.Release();
			}
		}

		/// <exception cref="NGit.Errors.AmbiguousObjectException"></exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private ObjectId GetStartPoint()
		{
			if (startCommit != null)
			{
				return startCommit.Id;
			}
			ObjectId result = null;
			try
			{
				result = repo.Resolve((startPoint == null) ? Constants.HEAD : startPoint);
			}
			catch (AmbiguousObjectException e)
			{
				throw;
			}
			if (result == null)
			{
				throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
					, startPoint != null ? startPoint : Constants.HEAD));
			}
			return result;
		}

		/// <exception cref="NGit.Api.Errors.InvalidRefNameException"></exception>
		private void ProcessOptions()
		{
			if (name == null || !Repository.IsValidRefName(Constants.R_HEADS + name))
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().branchNameInvalid
					, name == null ? "<null>" : name));
			}
		}

		/// <param name="name">the name of the new branch</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CreateBranchCommand SetName(string name)
		{
			CheckCallable();
			this.name = name;
			return this;
		}

		/// <param name="force">
		/// if <code>true</code> and the branch with the given name
		/// already exists, the start-point of an existing branch will be
		/// set to a new start-point; if false, the existing branch will
		/// not be changed
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CreateBranchCommand SetForce(bool force)
		{
			CheckCallable();
			this.force = force;
			return this;
		}

		/// <param name="startPoint">
		/// corresponds to the start-point option; if <code>null</code>,
		/// the current HEAD will be used
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CreateBranchCommand SetStartPoint(string startPoint)
		{
			CheckCallable();
			this.startPoint = startPoint;
			this.startCommit = null;
			return this;
		}

		/// <param name="startPoint">
		/// corresponds to the start-point option; if <code>null</code>,
		/// the current HEAD will be used
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CreateBranchCommand SetStartPoint(RevCommit startPoint)
		{
			CheckCallable();
			this.startCommit = startPoint;
			this.startPoint = null;
			return this;
		}

		/// <param name="mode">
		/// corresponds to the --track/--no-track/--set-upstream options;
		/// may be <code>null</code>
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CreateBranchCommand SetUpstreamMode(CreateBranchCommand.SetupUpstreamMode
			 mode)
		{
			CheckCallable();
			this.upstreamMode = mode;
			return this;
		}
	}
}
