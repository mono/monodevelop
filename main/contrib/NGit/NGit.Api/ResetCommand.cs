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
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Dircache;
using NGit.Internal;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Reset</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-reset.html"
	/// *      >Git documentation about Reset</a></seealso>
	public class ResetCommand : GitCommand<Ref>
	{
		/// <summary>Kind of reset</summary>
		public enum ResetType
		{
			SOFT,
			MIXED,
			HARD,
			MERGE,
			KEEP
		}

		private string @ref = Constants.HEAD;

		private ResetCommand.ResetType? mode;

		private ICollection<string> filepaths = new List<string>();

		/// <param name="repo"></param>
		protected internal ResetCommand(Repository repo) : base(repo)
		{
		}

		// TODO not implemented yet
		// TODO not implemented yet
		/// <summary>
		/// Executes the
		/// <code>Reset</code>
		/// command. Each instance of this class should
		/// only be used for one invocation of the command. Don't call this method
		/// twice on an instance.
		/// </summary>
		/// <returns>the Ref after reset</returns>
		/// <exception cref="System.IO.IOException"></exception>
		public override Ref Call()
		{
			CheckCallable();
			Ref r;
			RevCommit commit;
			try
			{
				RepositoryState state = repo.GetRepositoryState();
				bool merging = state.Equals(RepositoryState.MERGING) || state.Equals(RepositoryState
					.MERGING_RESOLVED);
				bool cherryPicking = state.Equals(RepositoryState.CHERRY_PICKING) || state.Equals
					(RepositoryState.CHERRY_PICKING_RESOLVED);
				// resolve the ref to a commit
				ObjectId commitId;
				try
				{
					commitId = repo.Resolve(@ref);
					if (commitId == null)
					{
						// @TODO throw an InvalidRefNameException. We can't do that
						// now because this would break the API
						throw new JGitInternalException("Invalid ref " + @ref + " specified");
					}
				}
				catch (IOException e)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().cannotRead, @ref
						), e);
				}
				RevWalk rw = new RevWalk(repo);
				try
				{
					commit = rw.ParseCommit(commitId);
				}
				catch (IOException e)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().cannotReadCommit
						, commitId.ToString()), e);
				}
				finally
				{
					rw.Release();
				}
				if (!filepaths.IsEmpty())
				{
					// reset [commit] -- paths
					ResetIndexForPaths(commit);
					SetCallable(false);
					return repo.GetRef(Constants.HEAD);
				}
				// write the ref
				RefUpdate ru = repo.UpdateRef(Constants.HEAD);
				ru.SetNewObjectId(commitId);
				string refName = Repository.ShortenRefName(@ref);
				string message = refName + ": updating " + Constants.HEAD;
				//$NON-NLS-1$
				ru.SetRefLogMessage(message, false);
				if (ru.ForceUpdate() == RefUpdate.Result.LOCK_FAILURE)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().cannotLock, ru
						.GetName()));
				}
				switch (mode)
				{
					case ResetCommand.ResetType.HARD:
					{
						CheckoutIndex(commit);
						break;
					}

					case ResetCommand.ResetType.MIXED:
					{
						ResetIndex(commit);
						break;
					}

					case ResetCommand.ResetType.SOFT:
					{
						// do nothing, only the ref was changed
						break;
					}

					case ResetCommand.ResetType.KEEP:
					case ResetCommand.ResetType.MERGE:
					{
						// TODO
						// TODO
						throw new NotSupportedException();
					}
				}
				if (mode != ResetCommand.ResetType.SOFT)
				{
					if (merging)
					{
						ResetMerge();
					}
					else
					{
						if (cherryPicking)
						{
							ResetCherryPick();
						}
					}
				}
				SetCallable(false);
				r = ru.GetRef();
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfResetCommand
					, e);
			}
			return r;
		}

		/// <param name="ref">the ref to reset to</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.ResetCommand SetRef(string @ref)
		{
			this.@ref = @ref;
			return this;
		}

		/// <param name="mode">the mode of the reset command</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.ResetCommand SetMode(ResetCommand.ResetType mode)
		{
			if (!filepaths.IsEmpty())
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().illegalCombinationOfArguments
					, "[--mixed | --soft | --hard]", "<paths>..."));
			}
			this.mode = mode;
			return this;
		}

		/// <param name="file">the file to add</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.ResetCommand AddPath(string file)
		{
			if (mode != null)
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().illegalCombinationOfArguments
					, "<paths>...", "[--mixed | --soft | --hard]"));
			}
			filepaths.AddItem(file);
			return this;
		}

		private void ResetIndexForPaths(RevCommit commit)
		{
			DirCache dc = null;
			DirCacheEditor edit;
			try
			{
				dc = repo.LockDirCache();
				edit = dc.Editor();
				TreeWalk tw = new TreeWalk(repo);
				tw.AddTree(new DirCacheIterator(dc));
				tw.AddTree(commit.Tree);
				tw.Filter = PathFilterGroup.CreateFromStrings(filepaths);
				tw.Recursive = true;
				while (tw.Next())
				{
					string path = tw.PathString;
					// DirCacheIterator dci = tw.getTree(0, DirCacheIterator.class);
					CanonicalTreeParser tree = tw.GetTree<CanonicalTreeParser>(1);
					if (tree == null)
					{
						// file is not in the commit, remove from index
						edit.Add(new DirCacheEditor.DeletePath(path));
					}
					else
					{
						// revert index to commit
						// it seams that there is concurrent access to tree
						// variable, therefore we need to keep references to
						// entryFileMode and entryObjectId in local
						// variables
						FileMode entryFileMode = tree.EntryFileMode;
						ObjectId entryObjectId = tree.EntryObjectId;
						edit.Add(new _PathEdit_294(entryFileMode, entryObjectId, path));
					}
				}
				edit.Commit();
			}
			catch (IOException e)
			{
				throw new RuntimeException(e);
			}
			finally
			{
				if (dc != null)
				{
					dc.Unlock();
				}
			}
		}

		private sealed class _PathEdit_294 : DirCacheEditor.PathEdit
		{
			public _PathEdit_294(FileMode entryFileMode, ObjectId entryObjectId, string baseArg1
				) : base(baseArg1)
			{
				this.entryFileMode = entryFileMode;
				this.entryObjectId = entryObjectId;
			}

			public override void Apply(DirCacheEntry ent)
			{
				ent.FileMode = entryFileMode;
				ent.SetObjectId(entryObjectId);
				ent.LastModified = 0;
			}

			private readonly FileMode entryFileMode;

			private readonly ObjectId entryObjectId;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResetIndex(RevCommit commit)
		{
			DirCache dc = null;
			try
			{
				dc = repo.LockDirCache();
				dc.Clear();
				DirCacheBuilder dcb = dc.Builder();
				dcb.AddTree(new byte[0], 0, repo.NewObjectReader(), commit.Tree);
				dcb.Commit();
			}
			catch (IOException e)
			{
				throw;
			}
			finally
			{
				if (dc != null)
				{
					dc.Unlock();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckoutIndex(RevCommit commit)
		{
			DirCache dc = null;
			try
			{
				dc = repo.LockDirCache();
				DirCacheCheckout checkout = new DirCacheCheckout(repo, dc, commit.Tree);
				checkout.SetFailOnConflict(false);
				checkout.Checkout();
			}
			catch (IOException e)
			{
				throw;
			}
			finally
			{
				if (dc != null)
				{
					dc.Unlock();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResetMerge()
		{
			repo.WriteMergeHeads(null);
			repo.WriteMergeCommitMsg(null);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ResetCherryPick()
		{
			repo.WriteCherryPickHead(null);
			repo.WriteMergeCommitMsg(null);
		}
	}
}
