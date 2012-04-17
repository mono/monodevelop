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
using NGit.Internal;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
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

		private string startPoint = null;

		private RevCommit startCommit;

		private CheckoutResult status;

		private IList<string> paths;

		private bool checkoutAllPaths;

		/// <param name="repo"></param>
		protected internal CheckoutCommand(Repository repo) : base(repo)
		{
			this.paths = new List<string>();
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
		/// <exception cref="NGit.Api.Errors.CheckoutConflictException"></exception>
		public override Ref Call()
		{
			CheckCallable();
			ProcessOptions();
			try
			{
				if (checkoutAllPaths || !paths.IsEmpty())
				{
					CheckoutPaths();
					status = CheckoutResult.OK_RESULT;
					SetCallable(false);
					return null;
				}
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
				Ref headRef = repo.GetRef(Constants.HEAD);
				string shortHeadRef = GetShortBranchName(headRef);
				string refLogMessage = "checkout: moving from " + shortHeadRef;
				ObjectId branch = repo.Resolve(name);
				if (branch == null)
				{
					throw new RefNotFoundException(MessageFormat.Format(JGitText.Get().refNotResolved
						, name));
				}
				RevWalk revWalk = new RevWalk(repo);
				AnyObjectId headId = headRef.GetObjectId();
				RevCommit headCommit = headId == null ? null : revWalk.ParseCommit(headId);
				RevCommit newCommit = revWalk.ParseCommit(branch);
				RevTree headTree = headCommit == null ? null : headCommit.Tree;
				DirCacheCheckout dco;
				DirCache dc = repo.LockDirCache();
				try
				{
					dco = new DirCacheCheckout(repo, headTree, dc, newCommit.Tree);
					dco.SetFailOnConflict(true);
					try
					{
						dco.Checkout();
					}
					catch (NGit.Errors.CheckoutConflictException e)
					{
						status = new CheckoutResult(CheckoutResult.Status.CONFLICTS, dco.GetConflicts());
						throw new NGit.Api.Errors.CheckoutConflictException(dco.GetConflicts(), e);
					}
				}
				finally
				{
					dc.Unlock();
				}
				Ref @ref = repo.GetRef(name);
				if (@ref != null && !@ref.GetName().StartsWith(Constants.R_HEADS))
				{
					@ref = null;
				}
				string toName = Repository.ShortenRefName(name);
				RefUpdate refUpdate = repo.UpdateRef(Constants.HEAD, @ref == null);
				refUpdate.SetForceUpdate(force);
				refUpdate.SetRefLogMessage(refLogMessage + " to " + toName, false);
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

		private string GetShortBranchName(Ref headRef)
		{
			if (headRef.GetTarget().GetName().Equals(headRef.GetName()))
			{
				return headRef.GetTarget().GetObjectId().GetName();
			}
			return Repository.ShortenRefName(headRef.GetTarget().GetName());
		}

		/// <param name="path">Path to update in the working tree and index.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CheckoutCommand AddPath(string path)
		{
			CheckCallable();
			this.paths.AddItem(path);
			return this;
		}

		/// <summary>
		/// Set whether to checkout all paths
		/// <p>
		/// This options should be used when you want to do a path checkout on the
		/// entire repository and so calling
		/// <see cref="AddPath(string)">AddPath(string)</see>
		/// is not possible
		/// since empty paths are not allowed.
		/// </summary>
		/// <param name="all">true to checkout all paths, false otherwise</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CheckoutCommand SetAllPaths(bool all)
		{
			checkoutAllPaths = all;
			return this;
		}

		/// <summary>Checkout paths into index and working directory</summary>
		/// <returns>this instance</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		/// <exception cref="NGit.Api.Errors.RefNotFoundException">NGit.Api.Errors.RefNotFoundException
		/// 	</exception>
		protected internal virtual NGit.Api.CheckoutCommand CheckoutPaths()
		{
			RevWalk revWalk = new RevWalk(repo);
			DirCache dc = repo.LockDirCache();
			try
			{
				DirCacheEditor editor = dc.Editor();
				TreeWalk startWalk = new TreeWalk(revWalk.GetObjectReader());
				startWalk.Recursive = true;
				if (!checkoutAllPaths)
				{
					startWalk.Filter = PathFilterGroup.CreateFromStrings(paths);
				}
				bool checkoutIndex = startCommit == null && startPoint == null;
				if (!checkoutIndex)
				{
					startWalk.AddTree(revWalk.ParseCommit(GetStartPoint()).Tree);
				}
				else
				{
					startWalk.AddTree(new DirCacheIterator(dc));
				}
				FilePath workTree = repo.WorkTree;
				ObjectReader r = repo.ObjectDatabase.NewReader();
				try
				{
					while (startWalk.Next())
					{
						ObjectId blobId = startWalk.GetObjectId(0);
						FileMode mode = startWalk.GetFileMode(0);
						editor.Add(new _PathEdit_292(this, blobId, mode, workTree, r, startWalk.PathString
							));
					}
					editor.Commit();
				}
				finally
				{
					startWalk.Release();
					r.Release();
				}
			}
			finally
			{
				dc.Unlock();
				revWalk.Release();
			}
			return this;
		}

		private sealed class _PathEdit_292 : DirCacheEditor.PathEdit
		{
			public _PathEdit_292(CheckoutCommand _enclosing, ObjectId blobId, FileMode mode, 
				FilePath workTree, ObjectReader r, string baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.blobId = blobId;
				this.mode = mode;
				this.workTree = workTree;
				this.r = r;
			}

			public override void Apply(DirCacheEntry ent)
			{
				ent.SetObjectId(blobId);
				ent.FileMode = mode;
				try
				{
					DirCacheCheckout.CheckoutEntry(this._enclosing.repo, new FilePath(workTree, ent.PathString
						), ent, r);
				}
				catch (IOException e)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().checkoutConflictWithFile
						, ent.PathString), e);
				}
			}

			private readonly CheckoutCommand _enclosing;

			private readonly ObjectId blobId;

			private readonly FileMode mode;

			private readonly FilePath workTree;

			private readonly ObjectReader r;
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
			if ((!checkoutAllPaths && paths.IsEmpty()) && (name == null || !Repository.IsValidRefName
				(Constants.R_HEADS + name)))
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
