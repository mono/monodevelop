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
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Commit</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command.
	/// </summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-commit.html"
	/// *      >Git documentation about Commit</a></seealso>
	public class CommitCommand : GitCommand<RevCommit>
	{
		private PersonIdent author;

		private PersonIdent committer;

		private string message;

		private bool all;

		/// <summary>parents this commit should have.</summary>
		/// <remarks>
		/// parents this commit should have. The current HEAD will be in this list
		/// and also all commits mentioned in .git/MERGE_HEAD
		/// </remarks>
		private IList<ObjectId> parents = new List<ObjectId>();

		/// <param name="repo"></param>
		protected internal CommitCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Executes the
		/// <code>commit</code>
		/// command with all the options and parameters
		/// collected by the setter methods of this class. Each instance of this
		/// class should only be used for one invocation of the command (means: one
		/// call to
		/// <see cref="Call()">Call()</see>
		/// )
		/// </summary>
		/// <returns>
		/// a
		/// <see cref="NGit.Revwalk.RevCommit">NGit.Revwalk.RevCommit</see>
		/// object representing the successful commit.
		/// </returns>
		/// <exception cref="NGit.Api.Errors.NoHeadException">when called on a git repo without a HEAD reference
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.NoMessageException">when called without specifying a commit message
		/// 	</exception>
		/// <exception cref="NGit.Errors.UnmergedPathException">when the current index contained unmerged pathes (conflicts)
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.WrongRepositoryStateException">when repository is not in the right state for committing
		/// 	</exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">
		/// a low-level exception of JGit has occurred. The original
		/// exception can be retrieved by calling
		/// <see cref="System.Exception.InnerException()">System.Exception.InnerException()</see>
		/// . Expect only
		/// <code>IOException's</code>
		/// to be wrapped. Subclasses of
		/// <see cref="System.IO.IOException">System.IO.IOException</see>
		/// (e.g.
		/// <see cref="NGit.Errors.UnmergedPathException">NGit.Errors.UnmergedPathException</see>
		/// ) are
		/// typically not wrapped here but thrown as original exception
		/// </exception>
		/// <exception cref="NGit.Api.Errors.ConcurrentRefUpdateException"></exception>
		public override RevCommit Call()
		{
			CheckCallable();
			RepositoryState state = repo.GetRepositoryState();
			if (!state.CanCommit())
			{
				throw new WrongRepositoryStateException(MessageFormat.Format(JGitText.Get().cannotCommitOnARepoWithState
					, state.Name()));
			}
			ProcessOptions(state);
			try
			{
				if (all && !repo.IsBare && repo.WorkTree != null)
				{
					Git git = new Git(repo);
					try
					{
						git.Add().AddFilepattern(".").SetUpdate(true).Call();
					}
					catch (NoFilepatternException e)
					{
						// should really not happen
						throw new JGitInternalException(e.Message, e);
					}
				}
				Ref head = repo.GetRef(Constants.HEAD);
				if (head == null)
				{
					throw new NoHeadException(JGitText.Get().commitOnRepoWithoutHEADCurrentlyNotSupported
						);
				}
				// determine the current HEAD and the commit it is referring to
				ObjectId headId = repo.Resolve(Constants.HEAD + "^{commit}");
				if (headId != null)
				{
					parents.Add(0, headId);
				}
				// lock the index
				DirCache index = repo.LockDirCache();
				try
				{
					ObjectInserter odi = repo.NewObjectInserter();
					try
					{
						// Write the index as tree to the object database. This may
						// fail for example when the index contains unmerged paths
						// (unresolved conflicts)
						ObjectId indexTreeId = index.WriteTree(odi);
						// Create a Commit object, populate it and write it
						NGit.CommitBuilder commit = new NGit.CommitBuilder();
						commit.Committer = committer;
						commit.Author = author;
						commit.Message = message;
						commit.SetParentIds(parents);
						commit.TreeId = indexTreeId;
						ObjectId commitId = odi.Insert(commit);
						odi.Flush();
						RevWalk revWalk = new RevWalk(repo);
						try
						{
							RevCommit revCommit = revWalk.ParseCommit(commitId);
							RefUpdate ru = repo.UpdateRef(Constants.HEAD);
							ru.SetNewObjectId(commitId);
							ru.SetRefLogMessage("commit : " + revCommit.GetShortMessage(), false);
							ru.SetExpectedOldObjectId(headId);
							RefUpdate.Result rc = ru.Update();
							switch (rc)
							{
								case RefUpdate.Result.NEW:
								case RefUpdate.Result.FAST_FORWARD:
								{
									SetCallable(false);
									if (state == RepositoryState.MERGING_RESOLVED)
									{
										// Commit was successful. Now delete the files
										// used for merge commits
										repo.WriteMergeCommitMsg(null);
										repo.WriteMergeHeads(null);
									}
									return revCommit;
								}

								case RefUpdate.Result.REJECTED:
								case RefUpdate.Result.LOCK_FAILURE:
								{
									throw new ConcurrentRefUpdateException(JGitText.Get().couldNotLockHEAD, ru.GetRef
										(), rc);
								}

								default:
								{
									throw new JGitInternalException(MessageFormat.Format(JGitText.Get().updatingRefFailed
										, Constants.HEAD, commitId.ToString(), rc));
								}
							}
						}
						finally
						{
							revWalk.Release();
						}
					}
					finally
					{
						odi.Release();
					}
				}
				finally
				{
					index.Unlock();
				}
			}
			catch (UnmergedPathException e)
			{
				// since UnmergedPathException is a subclass of IOException
				// which should not be wrapped by a JGitInternalException we
				// have to catch and re-throw it here
				throw;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfCommitCommand
					, e);
			}
		}

		/// <summary>Sets default values for not explicitly specified options.</summary>
		/// <remarks>
		/// Sets default values for not explicitly specified options. Then validates
		/// that all required data has been provided.
		/// </remarks>
		/// <param name="state">the state of the repository we are working on</param>
		/// <exception cref="NGit.Api.Errors.NoMessageException">if the commit message has not been specified
		/// 	</exception>
		private void ProcessOptions(RepositoryState state)
		{
			if (committer == null)
			{
				committer = new PersonIdent(repo);
			}
			if (author == null)
			{
				author = committer;
			}
			// when doing a merge commit parse MERGE_HEAD and MERGE_MSG files
			if (state == RepositoryState.MERGING_RESOLVED)
			{
				try
				{
					parents = repo.ReadMergeHeads();
				}
				catch (IOException e)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionOccuredDuringReadingOfGIT_DIR
						, Constants.MERGE_HEAD, e), e);
				}
				if (message == null)
				{
					try
					{
						message = repo.ReadMergeCommitMsg();
					}
					catch (IOException e)
					{
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionOccuredDuringReadingOfGIT_DIR
							, Constants.MERGE_MSG, e), e);
					}
				}
			}
			if (message == null)
			{
				// as long as we don't suppport -C option we have to have
				// an explicit message
				throw new NoMessageException(JGitText.Get().commitMessageNotSpecified);
			}
		}

		/// <param name="message">
		/// the commit message used for the
		/// <code>commit</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetMessage(string message)
		{
			CheckCallable();
			this.message = message;
			return this;
		}

		/// <returns>the commit message used for the <code>commit</code></returns>
		public virtual string GetMessage()
		{
			return message;
		}

		/// <summary>
		/// Sets the committer for this
		/// <code>commit</code>
		/// . If no committer is explicitly
		/// specified because this method is never called or called with
		/// <code>null</code>
		/// value then the committer will be deduced from config info in repository,
		/// with current time.
		/// </summary>
		/// <param name="committer">
		/// the committer used for the
		/// <code>commit</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetCommitter(PersonIdent committer)
		{
			CheckCallable();
			this.committer = committer;
			return this;
		}

		/// <summary>
		/// Sets the committer for this
		/// <code>commit</code>
		/// . If no committer is explicitly
		/// specified because this method is never called or called with
		/// <code>null</code>
		/// value then the committer will be deduced from config info in repository,
		/// with current time.
		/// </summary>
		/// <param name="name">
		/// the name of the committer used for the
		/// <code>commit</code>
		/// </param>
		/// <param name="email">
		/// the email of the committer used for the
		/// <code>commit</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetCommitter(string name, string email)
		{
			CheckCallable();
			return SetCommitter(new PersonIdent(name, email));
		}

		/// <returns>
		/// the committer used for the
		/// <code>commit</code>
		/// . If no committer was
		/// specified
		/// <code>null</code>
		/// is returned and the default
		/// <see cref="NGit.PersonIdent">NGit.PersonIdent</see>
		/// of this repo is used during execution of the
		/// command
		/// </returns>
		public virtual PersonIdent GetCommitter()
		{
			return committer;
		}

		/// <summary>
		/// Sets the author for this
		/// <code>commit</code>
		/// . If no author is explicitly
		/// specified because this method is never called or called with
		/// <code>null</code>
		/// value then the author will be set to the committer.
		/// </summary>
		/// <param name="author">
		/// the author used for the
		/// <code>commit</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetAuthor(PersonIdent author)
		{
			CheckCallable();
			this.author = author;
			return this;
		}

		/// <summary>
		/// Sets the author for this
		/// <code>commit</code>
		/// . If no author is explicitly
		/// specified because this method is never called or called with
		/// <code>null</code>
		/// value then the author will be set to the committer.
		/// </summary>
		/// <param name="name">
		/// the name of the author used for the
		/// <code>commit</code>
		/// </param>
		/// <param name="email">
		/// the email of the author used for the
		/// <code>commit</code>
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetAuthor(string name, string email)
		{
			CheckCallable();
			return SetAuthor(new PersonIdent(name, email));
		}

		/// <returns>
		/// the author used for the
		/// <code>commit</code>
		/// . If no author was
		/// specified
		/// <code>null</code>
		/// is returned and the default
		/// <see cref="NGit.PersonIdent">NGit.PersonIdent</see>
		/// of this repo is used during execution of the
		/// command
		/// </returns>
		public virtual PersonIdent GetAuthor()
		{
			return author;
		}

		/// <summary>
		/// If set to true the Commit command automatically stages files that have
		/// been modified and deleted, but new files you not known by the repository
		/// are not affected.
		/// </summary>
		/// <remarks>
		/// If set to true the Commit command automatically stages files that have
		/// been modified and deleted, but new files you not known by the repository
		/// are not affected. This corresponds to the parameter -a on the command
		/// line.
		/// </remarks>
		/// <param name="all"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetAll(bool all)
		{
			this.all = all;
			return this;
		}
	}
}
