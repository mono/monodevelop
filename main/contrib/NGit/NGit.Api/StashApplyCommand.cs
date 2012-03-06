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
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Command class to apply a stashed commit.</summary>
	/// <remarks>Command class to apply a stashed commit.</remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-stash.html"
	/// *      >Git documentation about Stash</a></seealso>
	public class StashApplyCommand : GitCommand<ObjectId>
	{
		private static readonly string DEFAULT_REF = Constants.STASH + "@{0}";

		private string stashRef;

		/// <summary>Create command to apply the changes of a stashed commit</summary>
		/// <param name="repo"></param>
		protected internal StashApplyCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Set the stash reference to apply
		/// <p>
		/// This will default to apply the latest stashed commit (stash@{0}) if
		/// unspecified
		/// </summary>
		/// <param name="stashRef"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.StashApplyCommand SetStashRef(string stashRef)
		{
			this.stashRef = stashRef;
			return this;
		}

		/// <summary>Apply the changes in a stashed commit to the working directory and index
		/// 	</summary>
		/// <returns>id of stashed commit that was applied</returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override ObjectId Call()
		{
			CheckCallable();
			if (repo.GetRepositoryState() != RepositoryState.SAFE)
			{
				throw new WrongRepositoryStateException(MessageFormat.Format(JGitText.Get().stashApplyOnUnsafeRepository
					, repo.GetRepositoryState()));
			}
			string revision = stashRef != null ? stashRef : DEFAULT_REF;
			ObjectId stashId;
			try
			{
				stashId = repo.Resolve(revision);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashApplyFailed, e);
			}
			if (stashId == null)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().stashResolveFailed
					, revision));
			}
			ObjectReader reader = repo.NewObjectReader();
			try
			{
				RevWalk revWalk = new RevWalk(reader);
				RevCommit wtCommit = revWalk.ParseCommit(stashId);
				if (wtCommit.ParentCount != 2)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().stashCommitMissingTwoParents
						, stashId.Name));
				}
				// Apply index changes
				RevTree indexTree = revWalk.ParseCommit(wtCommit.GetParent(1)).Tree;
				DirCacheCheckout dco = new DirCacheCheckout(repo, repo.LockDirCache(), indexTree, 
					new FileTreeIterator(repo));
				dco.SetFailOnConflict(true);
				dco.Checkout();
				// Apply working directory changes
				RevTree headTree = revWalk.ParseCommit(wtCommit.GetParent(0)).Tree;
				DirCache cache = repo.LockDirCache();
				DirCacheEditor editor = cache.Editor();
				try
				{
					TreeWalk treeWalk = new TreeWalk(reader);
					treeWalk.Recursive = true;
					treeWalk.AddTree(headTree);
					treeWalk.AddTree(indexTree);
					treeWalk.AddTree(wtCommit.Tree);
					treeWalk.Filter = TreeFilter.ANY_DIFF;
					FilePath workingTree = repo.WorkTree;
					while (treeWalk.Next())
					{
						string path = treeWalk.PathString;
						FilePath file = new FilePath(workingTree, path);
						AbstractTreeIterator headIter = treeWalk.GetTree<AbstractTreeIterator>(0);
						AbstractTreeIterator indexIter = treeWalk.GetTree<AbstractTreeIterator>(1);
						AbstractTreeIterator wtIter = treeWalk.GetTree<AbstractTreeIterator>(2);
						if (wtIter != null)
						{
							DirCacheEntry entry = new DirCacheEntry(treeWalk.RawPath);
							entry.SetObjectId(wtIter.EntryObjectId);
							DirCacheCheckout.CheckoutEntry(repo, file, entry);
						}
						else
						{
							if (indexIter != null && headIter != null && !indexIter.IdEqual(headIter))
							{
								editor.Add(new DirCacheEditor.DeletePath(path));
							}
							FileUtils.Delete(file, FileUtils.RETRY | FileUtils.SKIP_MISSING);
						}
					}
				}
				finally
				{
					editor.Commit();
					cache.Unlock();
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().stashApplyFailed, e);
			}
			finally
			{
				reader.Release();
			}
			return stashId;
		}
	}
}
