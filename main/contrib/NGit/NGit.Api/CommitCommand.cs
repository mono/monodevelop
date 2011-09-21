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
using NGit.Treewalk;
using NGit.Util;
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

		private IList<string> only = new AList<string>();

		private bool[] onlyProcessed;

		private bool amend;

		private bool insertChangeId;

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
		/// <exception cref="NGit.Errors.UnmergedPathException">when the current index contained unmerged paths (conflicts)
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
					if (amend)
					{
						RevCommit previousCommit = new RevWalk(repo).ParseCommit(headId);
						RevCommit[] p = previousCommit.Parents;
						for (int i = 0; i < p.Length; i++)
						{
							parents.Add(0, p[i].Id);
						}
					}
					else
					{
						parents.Add(0, headId);
					}
				}
				// lock the index
				DirCache index = repo.LockDirCache();
				try
				{
					if (!only.IsEmpty())
					{
						index = CreateTemporaryIndex(headId, index);
					}
					ObjectInserter odi = repo.NewObjectInserter();
					try
					{
						// Write the index as tree to the object database. This may
						// fail for example when the index contains unmerged paths
						// (unresolved conflicts)
						ObjectId indexTreeId = index.WriteTree(odi);
						if (insertChangeId)
						{
							InsertChangeId(indexTreeId);
						}
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
							string prefix = amend ? "commit (amend): " : "commit: ";
							ru.SetRefLogMessage(prefix + revCommit.GetShortMessage(), false);
							ru.SetExpectedOldObjectId(headId);
							RefUpdate.Result rc = ru.ForceUpdate();
							switch (rc)
							{
								case RefUpdate.Result.NEW:
								case RefUpdate.Result.FORCED:
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
									else
									{
										if (state == RepositoryState.CHERRY_PICKING_RESOLVED)
										{
											repo.WriteMergeCommitMsg(null);
											repo.WriteCherryPickHead(null);
										}
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

		/// <exception cref="System.IO.IOException"></exception>
		private void InsertChangeId(ObjectId treeId)
		{
			ObjectId firstParentId = null;
			if (!parents.IsEmpty())
			{
				firstParentId = parents[0];
			}
			ObjectId changeId = ChangeIdUtil.ComputeChangeId(treeId, firstParentId, author, committer
				, message);
			message = ChangeIdUtil.InsertId(message, changeId);
			if (changeId != null)
			{
				message = message.ReplaceAll("\nChange-Id: I" + ObjectId.ZeroId.GetName() + "\n", 
					"\nChange-Id: I" + changeId.GetName() + "\n");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DirCache CreateTemporaryIndex(ObjectId headId, DirCache index)
		{
			ObjectInserter inserter = null;
			// get DirCacheEditor to modify the index if required
			DirCacheEditor dcEditor = index.Editor();
			// get DirCacheBuilder for newly created in-core index to build a
			// temporary index for this commit
			DirCache inCoreIndex = DirCache.NewInCore();
			DirCacheBuilder dcBuilder = inCoreIndex.Builder();
			onlyProcessed = new bool[only.Count];
			bool emptyCommit = true;
			TreeWalk treeWalk = new TreeWalk(repo);
			int dcIdx = treeWalk.AddTree(new DirCacheIterator(index));
			int fIdx = treeWalk.AddTree(new FileTreeIterator(repo));
			int hIdx = -1;
			if (headId != null)
			{
				hIdx = treeWalk.AddTree(new RevWalk(repo).ParseTree(headId));
			}
			treeWalk.Recursive = true;
			while (treeWalk.Next())
			{
				string path = treeWalk.PathString;
				// check if current entry's path matches a specified path
				int pos = LookupOnly(path);
				CanonicalTreeParser hTree = null;
				if (hIdx != -1)
				{
					hTree = treeWalk.GetTree<CanonicalTreeParser>(hIdx);
				}
				if (pos >= 0)
				{
					// include entry in commit
					DirCacheIterator dcTree = treeWalk.GetTree<DirCacheIterator>(dcIdx);
					FileTreeIterator fTree = treeWalk.GetTree<FileTreeIterator>(fIdx);
					// check if entry refers to a tracked file
					bool tracked = dcTree != null || hTree != null;
					if (!tracked)
					{
						break;
					}
					if (fTree != null)
					{
						// create a new DirCacheEntry with data retrieved from disk
						DirCacheEntry dcEntry = new DirCacheEntry(path);
						long entryLength = fTree.GetEntryLength();
						dcEntry.SetLength(entryLength);
						dcEntry.LastModified = fTree.GetEntryLastModified();
						dcEntry.FileMode = fTree.EntryFileMode;
						bool objectExists = (dcTree != null && fTree.IdEqual(dcTree)) || (hTree != null &&
							 fTree.IdEqual(hTree));
						if (objectExists)
						{
							dcEntry.SetObjectId(fTree.EntryObjectId);
						}
						else
						{
							// insert object
							if (inserter == null)
							{
								inserter = repo.NewObjectInserter();
							}
							InputStream inputStream = fTree.OpenEntryStream();
							try
							{
								dcEntry.SetObjectId(inserter.Insert(Constants.OBJ_BLOB, entryLength, inputStream)
									);
							}
							finally
							{
								inputStream.Close();
							}
						}
						// update index
						dcEditor.Add(new _PathEdit_359(dcEntry, path));
						// add to temporary in-core index
						dcBuilder.Add(dcEntry);
						if (emptyCommit && (hTree == null || !hTree.IdEqual(fTree)))
						{
							// this is a change
							emptyCommit = false;
						}
					}
					else
					{
						// if no file exists on disk, remove entry from index and
						// don't add it to temporary in-core index
						dcEditor.Add(new DirCacheEditor.DeletePath(path));
						if (emptyCommit && hTree != null)
						{
							// this is a change
							emptyCommit = false;
						}
					}
					// keep track of processed path
					onlyProcessed[pos] = true;
				}
				else
				{
					// add entries from HEAD for all other paths
					if (hTree != null)
					{
						// create a new DirCacheEntry with data retrieved from HEAD
						DirCacheEntry dcEntry = new DirCacheEntry(path);
						dcEntry.SetObjectId(hTree.EntryObjectId);
						dcEntry.FileMode = hTree.EntryFileMode;
						// add to temporary in-core index
						dcBuilder.Add(dcEntry);
					}
				}
			}
			// there must be no unprocessed paths left at this point; otherwise an
			// untracked or unknown path has been specified
			for (int i = 0; i < onlyProcessed.Length; i++)
			{
				if (!onlyProcessed[i])
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().entryNotFoundByPath
						, only[i]));
				}
			}
			// there must be at least one change
			if (emptyCommit)
			{
				throw new JGitInternalException(JGitText.Get().emptyCommit);
			}
			// update index
			dcEditor.Commit();
			// finish temporary in-core index used for this commit
			dcBuilder.Finish();
			return inCoreIndex;
		}

		private sealed class _PathEdit_359 : DirCacheEditor.PathEdit
		{
			public _PathEdit_359(DirCacheEntry dcEntry, string baseArg1) : base(baseArg1)
			{
				this.dcEntry = dcEntry;
			}

			public override void Apply(DirCacheEntry ent)
			{
				ent.CopyMetaData(dcEntry);
			}

			private readonly DirCacheEntry dcEntry;
		}

		/// <summary>
		/// Look an entry's path up in the list of paths specified by the --only/ -o
		/// option
		/// In case the complete (file) path (e.g.
		/// </summary>
		/// <remarks>
		/// Look an entry's path up in the list of paths specified by the --only/ -o
		/// option
		/// In case the complete (file) path (e.g. "d1/d2/f1") cannot be found in
		/// <code>only</code>, lookup is also tried with (parent) directory paths
		/// (e.g. "d1/d2" and "d1").
		/// </remarks>
		/// <param name="pathString">entry's path</param>
		/// <returns>the item's index in <code>only</code>; -1 if no item matches</returns>
		private int LookupOnly(string pathString)
		{
			int i = 0;
			foreach (string o in only)
			{
				string p = pathString;
				while (true)
				{
					if (p.Equals(o))
					{
						return i;
					}
					int l = p.LastIndexOf("/");
					if (l < 1)
					{
						break;
					}
					p = Sharpen.Runtime.Substring(p, 0, l);
				}
				i++;
			}
			return -1;
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
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionOccurredDuringReadingOfGIT_DIR
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
						throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionOccurredDuringReadingOfGIT_DIR
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
		/// been modified and deleted, but new files not known by the repository are
		/// not affected.
		/// </summary>
		/// <remarks>
		/// If set to true the Commit command automatically stages files that have
		/// been modified and deleted, but new files not known by the repository are
		/// not affected. This corresponds to the parameter -a on the command line.
		/// </remarks>
		/// <param name="all"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="NGit.Api.Errors.JGitInternalException">in case of an illegal combination of arguments/ options
		/// 	</exception>
		public virtual NGit.Api.CommitCommand SetAll(bool all)
		{
			CheckCallable();
			if (!only.IsEmpty())
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().illegalCombinationOfArguments
					, "--all", "--only"));
			}
			this.all = all;
			return this;
		}

		/// <summary>Used to amend the tip of the current branch.</summary>
		/// <remarks>
		/// Used to amend the tip of the current branch. If set to true, the previous
		/// commit will be amended. This is equivalent to --amend on the command
		/// line.
		/// </remarks>
		/// <param name="amend"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetAmend(bool amend)
		{
			CheckCallable();
			this.amend = amend;
			return this;
		}

		/// <summary>
		/// Commit dedicated path only
		/// This method can be called several times to add multiple paths.
		/// </summary>
		/// <remarks>
		/// Commit dedicated path only
		/// This method can be called several times to add multiple paths. Full file
		/// paths are supported as well as directory paths; in the latter case this
		/// commits all files/ directories below the specified path.
		/// </remarks>
		/// <param name="only">path to commit</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetOnly(string only)
		{
			CheckCallable();
			if (all)
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().illegalCombinationOfArguments
					, "--only", "--all"));
			}
			string o = only.EndsWith("/") ? Sharpen.Runtime.Substring(only, 0, only.Length - 
				1) : only;
			// ignore duplicates
			if (!this.only.Contains(o))
			{
				this.only.AddItem(o);
			}
			return this;
		}

		/// <summary>
		/// If set to true a change id will be inserted into the commit message
		/// An existing change id is not replaced.
		/// </summary>
		/// <remarks>
		/// If set to true a change id will be inserted into the commit message
		/// An existing change id is not replaced. An initial change id (I000...)
		/// will be replaced by the change id.
		/// </remarks>
		/// <param name="insertChangeId"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CommitCommand SetInsertChangeId(bool insertChangeId)
		{
			CheckCallable();
			this.insertChangeId = insertChangeId;
			return this;
		}
	}
}
