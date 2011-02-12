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
using NGit.Errors;
using NGit.Revwalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Checkout a branch to the working tree</summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-checkout.html"
	/// *      >Git documentation about Checkout</a></seealso>
	public class CheckoutCommand : GitCommand<Ref>
	{
		private string name;

		private bool force = false;

		private bool createBranch = false;

		private CreateBranchCommand.SetupUpstreamMode upstreamMode;

		private string startPoint = Constants.HEAD;

		private RevCommit startCommit;

		private CheckoutResult status;

		/// <param name="repo"></param>
		protected internal CheckoutCommand(Repository repo) : base(repo)
		{
		}

		/// <exception cref="NGit.Api.Errors.RefAlreadyExistsException">
		/// when trying to create (without force) a branch with a name
		/// that already exists
		/// </exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException">if the start point or branch can not be found
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
			try
			{
				if (createBranch)
				{
					Git git = new Git(repo);
					CreateBranchCommand command = git.BranchCreate();
					command.SetName(name);
					command.SetStartPoint(GetStartPoint().Name);
					if (upstreamMode != null)
					{
						command.SetUpstreamMode(upstreamMode);
					}
					command.Call();
				}
				RevWalk revWalk = new RevWalk(repo);
				Ref headRef = repo.GetRef(Constants.HEAD);
				RevCommit headCommit = revWalk.ParseCommit(headRef.GetObjectId());
				string refLogMessage = "checkout: moving from " + headRef.GetTarget().GetName();
				ObjectId branch = repo.Resolve(name);
				if (branch == null)
				{
					throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
						, name));
				}
				RevCommit newCommit = revWalk.ParseCommit(branch);
				DirCacheCheckout dco = new DirCacheCheckout(repo, headCommit.Tree, repo.LockDirCache
					(), newCommit.Tree);
				dco.SetFailOnConflict(true);
				try
				{
					dco.Checkout();
				}
				catch (NGit.Errors.CheckoutConflictException e)
				{
					status = new CheckoutResult(CheckoutResult.Status.CONFLICTS, dco.GetConflicts());
					throw;
				}
				Ref @ref = repo.GetRef(name);
				if (@ref != null && !@ref.GetName().StartsWith(Constants.R_HEADS))
				{
					@ref = null;
				}
				RefUpdate refUpdate = repo.UpdateRef(Constants.HEAD, @ref == null);
				refUpdate.SetForceUpdate(force);
				refUpdate.SetRefLogMessage(refLogMessage + " to " + newCommit.GetName(), false);
				RefUpdate.Result updateResult;
				if (@ref != null)
				{
					updateResult = refUpdate.Link(@ref.GetName());
				}
				else
				{
					refUpdate.SetNewObjectId(newCommit);
					updateResult = refUpdate.ForceUpdate();
				}
				SetCallable(false);
				bool ok = false;
				switch (updateResult)
				{
					case RefUpdate.Result.NEW:
					{
						ok = true;
						break;
					}

					case RefUpdate.Result.NO_CHANGE:
					case RefUpdate.Result.FAST_FORWARD:
					case RefUpdate.Result.FORCED:
					{
						ok = true;
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
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().checkoutUnexpectedResult
						, updateResult.ToString()));
				}
				if (!dco.GetToBeDeleted().IsEmpty())
				{
					status = new CheckoutResult(CheckoutResult.Status.NONDELETED, dco.GetToBeDeleted(
						));
				}
				else
				{
					status = CheckoutResult.OK_RESULT;
				}
				return @ref;
			}
			catch (IOException ioe)
			{
				throw new JGitInternalException(ioe.Message, ioe);
			}
			finally
			{
				if (status == null)
				{
					status = CheckoutResult.ERROR_RESULT;
				}
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
		public virtual NGit.Api.CheckoutCommand SetName(string name)
		{
			CheckCallable();
			this.name = name;
			return this;
		}

		/// <param name="createBranch">
		/// if <code>true</code> a branch will be created as part of the
		/// checkout and set to the specified start point
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CheckoutCommand SetCreateBranch(bool createBranch)
		{
			CheckCallable();
			this.createBranch = createBranch;
			return this;
		}

		/// <param name="force">
		/// if <code>true</code> and the branch with the given name
		/// already exists, the start-point of an existing branch will be
		/// set to a new start-point; if false, the existing branch will
		/// not be changed
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CheckoutCommand SetForce(bool force)
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
		public virtual NGit.Api.CheckoutCommand SetStartPoint(string startPoint)
		{
			CheckCallable();
			this.startPoint = startPoint;
			this.startCommit = null;
			return this;
		}

		/// <param name="startCommit">
		/// corresponds to the start-point option; if <code>null</code>,
		/// the current HEAD will be used
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CheckoutCommand SetStartPoint(RevCommit startCommit)
		{
			CheckCallable();
			this.startCommit = startCommit;
			this.startPoint = null;
			return this;
		}

		/// <param name="mode">
		/// corresponds to the --track/--no-track options; may be
		/// <code>null</code>
		/// </param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.CheckoutCommand SetUpstreamMode(CreateBranchCommand.SetupUpstreamMode
			 mode)
		{
			CheckCallable();
			this.upstreamMode = mode;
			return this;
		}

		/// <returns>the result</returns>
		public virtual CheckoutResult GetResult()
		{
			if (status == null)
			{
				return CheckoutResult.NOT_TRIED_RESULT;
			}
			return status;
		}
	}
}
