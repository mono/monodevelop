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
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Command class to stash changes in the working directory and index in a
	/// commit.
	/// </summary>
	/// <remarks>
	/// Command class to stash changes in the working directory and index in a
	/// commit.
	/// </remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-stash.html"
	/// *      >Git documentation about Stash</a></seealso>
	public class StashCreateCommand : GitCommand<RevCommit>
	{
		private static readonly string MSG_INDEX = "index on {0}: {1} {2}";

		private static readonly string MSG_WORKING_DIR = "WIP on {0}: {1} {2}";

		private string indexMessage = MSG_INDEX;

		private string workingDirectoryMessage = MSG_WORKING_DIR;

		private string @ref = Constants.R_STASH;

		private PersonIdent person;

		/// <summary>Create a command to stash changes in the working directory and index</summary>
		/// <param name="repo"></param>
		protected internal StashCreateCommand(Repository repo) : base(repo)
		{
			person = new PersonIdent(repo);
		}

		/// <summary>
		/// Set the message used when committing index changes
		/// <p>
		/// The message will be formatted with the current branch, abbreviated commit
		/// id, and short commit message when used.
		/// </summary>
		/// <remarks>
		/// Set the message used when committing index changes
		/// <p>
		/// The message will be formatted with the current branch, abbreviated commit
		/// id, and short commit message when used.
		/// </remarks>
		/// <param name="message"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashCreateCommand SetIndexMessage(string message)
		{
			indexMessage = message;
			return this;
		}

		/// <summary>
		/// Set the message used when committing working directory changes
		/// <p>
		/// The message will be formatted with the current branch, abbreviated commit
		/// id, and short commit message when used.
		/// </summary>
		/// <remarks>
		/// Set the message used when committing working directory changes
		/// <p>
		/// The message will be formatted with the current branch, abbreviated commit
		/// id, and short commit message when used.
		/// </remarks>
		/// <param name="message"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashCreateCommand SetWorkingDirectoryMessage(string message
			)
		{
			workingDirectoryMessage = message;
			return this;
		}

		/// <summary>Set the person to use as the author and committer in the commits made</summary>
		/// <param name="person"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashCreateCommand SetPerson(PersonIdent person)
		{
			this.person = person;
			return this;
		}

		/// <summary>
		/// Set the reference to update with the stashed commit id
		/// <p>
		/// This value defaults to
		/// <see cref="NGit.Constants.R_STASH">NGit.Constants.R_STASH</see>
		/// </summary>
		/// <param name="ref"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashCreateCommand SetRef(string @ref)
		{
			this.@ref = @ref;
			return this;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private RevCommit ParseCommit(ObjectReader reader, ObjectId headId)
		{
			RevWalk walk = new RevWalk(reader);
			walk.SetRetainBody(true);
			return walk.ParseCommit(headId);
		}

		private NGit.CommitBuilder CreateBuilder(ObjectId headId)
		{
			NGit.CommitBuilder builder = new NGit.CommitBuilder();
			PersonIdent author = person;
			if (author == null)
			{
				author = new PersonIdent(repo);
			}
			builder.Author = author;
			builder.Committer = author;
			builder.SetParentId(headId);
			return builder;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void UpdateStashRef(ObjectId commitId, PersonIdent refLogIdent, string refLogMessage
			)
		{
			Ref currentRef = repo.GetRef(@ref);
			RefUpdate refUpdate = repo.UpdateRef(@ref);
			refUpdate.SetNewObjectId(commitId);
			refUpdate.SetRefLogIdent(refLogIdent);
			refUpdate.SetRefLogMessage(refLogMessage, false);
			if (currentRef != null)
			{
				refUpdate.SetExpectedOldObjectId(currentRef.GetObjectId());
			}
			else
			{
				refUpdate.SetExpectedOldObjectId(ObjectId.ZeroId);
			}
			refUpdate.ForceUpdate();
		}

		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		private Ref GetHead()
		{
			try
			{
				Ref head = repo.GetRef(Constants.HEAD);
				if (head == null || head.GetObjectId() == null)
				{
					throw new NoHeadException(JGitText.Get().headRequiredToStash);
				}
				return head;
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashFailed, e);
			}
		}

		/// <summary>
		/// Stash the contents on the working directory and index in separate commits
		/// and reset to the current HEAD commit.
		/// </summary>
		/// <remarks>
		/// Stash the contents on the working directory and index in separate commits
		/// and reset to the current HEAD commit.
		/// </remarks>
		/// <returns>stashed commit or null if no changes to stash</returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override RevCommit Call()
		{
			CheckCallable();
			Ref head = GetHead();
			ObjectReader reader = repo.NewObjectReader();
			try
			{
				RevCommit headCommit = ParseCommit(reader, head.GetObjectId());
				DirCache cache = repo.LockDirCache();
				ObjectInserter inserter = repo.NewObjectInserter();
				ObjectId commitId;
				try
				{
					TreeWalk treeWalk = new TreeWalk(reader);
					treeWalk.Recursive = true;
					treeWalk.AddTree(headCommit.Tree);
					treeWalk.AddTree(new DirCacheIterator(cache));
					treeWalk.AddTree(new FileTreeIterator(repo));
					treeWalk.Filter = AndTreeFilter.Create(new SkipWorkTreeFilter(1), new IndexDiffFilter
						(1, 2));
					// Return null if no local changes to stash
					if (!treeWalk.Next())
					{
						return null;
					}
					MutableObjectId id = new MutableObjectId();
					IList<DirCacheEditor.PathEdit> wtEdits = new AList<DirCacheEditor.PathEdit>();
					IList<string> wtDeletes = new AList<string>();
					do
					{
						AbstractTreeIterator headIter = treeWalk.GetTree<AbstractTreeIterator>(0);
						DirCacheIterator indexIter = treeWalk.GetTree<DirCacheIterator>(1);
						WorkingTreeIterator wtIter = treeWalk.GetTree<WorkingTreeIterator>(2);
						if (headIter != null && indexIter != null && wtIter != null)
						{
							if (wtIter.IdEqual(indexIter) || wtIter.IdEqual(headIter))
							{
								continue;
							}
							treeWalk.GetObjectId(id, 0);
							DirCacheEntry entry = new DirCacheEntry(treeWalk.RawPath);
							entry.SetLength(wtIter.GetEntryLength());
							entry.LastModified = wtIter.GetEntryLastModified();
							entry.FileMode = wtIter.EntryFileMode;
							InputStream @in = wtIter.OpenEntryStream();
							try
							{
								entry.SetObjectId(inserter.Insert(Constants.OBJ_BLOB, wtIter.GetEntryLength(), @in
									));
							}
							finally
							{
								@in.Close();
							}
							wtEdits.AddItem(new _PathEdit_265(entry, entry));
						}
						else
						{
							if (indexIter == null)
							{
								wtDeletes.AddItem(treeWalk.PathString);
							}
							else
							{
								if (wtIter == null && headIter != null)
								{
									wtDeletes.AddItem(treeWalk.PathString);
								}
							}
						}
					}
					while (treeWalk.Next());
					string branch = Repository.ShortenRefName(head.GetTarget().GetName());
					// Commit index changes
					NGit.CommitBuilder builder = CreateBuilder(headCommit);
					builder.TreeId = cache.WriteTree(inserter);
					builder.Message = MessageFormat.Format(indexMessage, branch, headCommit.Abbreviate
						(7).Name, headCommit.GetShortMessage());
					ObjectId indexCommit = inserter.Insert(builder);
					// Commit working tree changes
					if (!wtEdits.IsEmpty() || !wtDeletes.IsEmpty())
					{
						DirCacheEditor editor = cache.Editor();
						foreach (DirCacheEditor.PathEdit edit in wtEdits)
						{
							editor.Add(edit);
						}
						foreach (string path in wtDeletes)
						{
							editor.Add(new DirCacheEditor.DeletePath(path));
						}
						editor.Finish();
					}
					builder.AddParentId(indexCommit);
					builder.Message = MessageFormat.Format(workingDirectoryMessage, branch, headCommit
						.Abbreviate(7).Name, headCommit.GetShortMessage());
					builder.TreeId = cache.WriteTree(inserter);
					commitId = inserter.Insert(builder);
					inserter.Flush();
					UpdateStashRef(commitId, builder.Author, builder.Message);
				}
				finally
				{
					inserter.Release();
					cache.Unlock();
				}
				// Hard reset to HEAD
				new ResetCommand(repo).SetMode(ResetCommand.ResetType.HARD).Call();
				// Return stashed commit
				return ParseCommit(reader, commitId);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashFailed, e);
			}
			finally
			{
				reader.Release();
			}
		}

		private sealed class _PathEdit_265 : DirCacheEditor.PathEdit
		{
			public _PathEdit_265(DirCacheEntry entry, DirCacheEntry baseArg1) : base(baseArg1
				)
			{
				this.entry = entry;
			}

			public override void Apply(DirCacheEntry ent)
			{
				ent.CopyMetaData(entry);
			}

			private readonly DirCacheEntry entry;
		}
	}
}
