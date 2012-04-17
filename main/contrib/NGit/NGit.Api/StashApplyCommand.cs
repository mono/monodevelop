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
using NGit.Internal;
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
	/// <since>2.0</since>
	public class StashApplyCommand : GitCommand<ObjectId>
	{
		private static readonly string DEFAULT_REF = Constants.STASH + "@{0}";

		/// <summary>
		/// Stash diff filter that looks for differences in the first three trees
		/// which must be the stash head tree, stash index tree, and stash working
		/// directory tree in any order.
		/// </summary>
		/// <remarks>
		/// Stash diff filter that looks for differences in the first three trees
		/// which must be the stash head tree, stash index tree, and stash working
		/// directory tree in any order.
		/// </remarks>
		private class StashDiffFilter : TreeFilter
		{
			public override bool Include(TreeWalk walker)
			{
				int m = walker.GetRawMode(0);
				if (walker.GetRawMode(1) != m || !walker.IdEqual(1, 0))
				{
					return true;
				}
				if (walker.GetRawMode(2) != m || !walker.IdEqual(2, 0))
				{
					return true;
				}
				return false;
			}

			public override bool ShouldBeRecursive()
			{
				return false;
			}

			public override TreeFilter Clone()
			{
				return this;
			}

			public override string ToString()
			{
				return "STASH_DIFF";
			}
		}

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
		public virtual StashApplyCommand SetStashRef(string stashRef)
		{
			this.stashRef = stashRef;
			return this;
		}

		private bool IsEqualEntry(AbstractTreeIterator iter1, AbstractTreeIterator iter2)
		{
			if (!iter1.EntryFileMode.Equals(iter2.EntryFileMode))
			{
				return false;
			}
			ObjectId id1 = iter1.EntryObjectId;
			ObjectId id2 = iter2.EntryObjectId;
			return id1 != null ? id1.Equals(id2) : id2 == null;
		}

		/// <summary>Would unstashing overwrite local changes?</summary>
		/// <param name="stashIndexIter"></param>
		/// <param name="stashWorkingTreeIter"></param>
		/// <param name="headIter"></param>
		/// <param name="indexIter"></param>
		/// <param name="workingTreeIter"></param>
		/// <returns>true if unstash conflict, false otherwise</returns>
		private bool IsConflict(AbstractTreeIterator stashIndexIter, AbstractTreeIterator
			 stashWorkingTreeIter, AbstractTreeIterator headIter, AbstractTreeIterator indexIter
			, AbstractTreeIterator workingTreeIter)
		{
			// Is the current index dirty?
			bool indexDirty = indexIter != null && (headIter == null || !IsEqualEntry(indexIter
				, headIter));
			// Is the current working tree dirty?
			bool workingTreeDirty = workingTreeIter != null && (headIter == null || !IsEqualEntry
				(workingTreeIter, headIter));
			// Would unstashing overwrite existing index changes?
			if (indexDirty && stashIndexIter != null && indexIter != null && !IsEqualEntry(stashIndexIter
				, indexIter))
			{
				return true;
			}
			// Would unstashing overwrite existing working tree changes?
			if (workingTreeDirty && stashWorkingTreeIter != null && workingTreeIter != null &&
				 !IsEqualEntry(stashWorkingTreeIter, workingTreeIter))
			{
				return true;
			}
			return false;
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		private ObjectId GetHeadTree()
		{
			ObjectId headTree;
			try
			{
				headTree = repo.Resolve(Constants.HEAD + "^{tree}");
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().cannotReadTree, e);
			}
			if (headTree == null)
			{
				throw new NoHeadException(JGitText.Get().cannotReadTree);
			}
			return headTree;
		}

		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		private ObjectId GetStashId()
		{
			string revision = stashRef != null ? stashRef : DEFAULT_REF;
			ObjectId stashId;
			try
			{
				stashId = repo.Resolve(revision);
			}
			catch (IOException e)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().stashResolveFailed
					, revision), e);
			}
			if (stashId == null)
			{
				throw new InvalidRefNameException(MessageFormat.Format(JGitText.Get().stashResolveFailed
					, revision));
			}
			return stashId;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ScanForConflicts(TreeWalk treeWalk)
		{
			FilePath workingTree = repo.WorkTree;
			while (treeWalk.Next())
			{
				// State of the stashed index and working directory
				AbstractTreeIterator stashIndexIter = treeWalk.GetTree<AbstractTreeIterator>(1);
				AbstractTreeIterator stashWorkingIter = treeWalk.GetTree<AbstractTreeIterator>(2);
				// State of the current HEAD, index, and working directory
				AbstractTreeIterator headIter = treeWalk.GetTree<AbstractTreeIterator>(3);
				AbstractTreeIterator indexIter = treeWalk.GetTree<AbstractTreeIterator>(4);
				AbstractTreeIterator workingIter = treeWalk.GetTree<AbstractTreeIterator>(5);
				if (IsConflict(stashIndexIter, stashWorkingIter, headIter, indexIter, workingIter
					))
				{
					string path = treeWalk.PathString;
					FilePath file = new FilePath(workingTree, path);
					throw new NGit.Errors.CheckoutConflictException(file.GetAbsolutePath());
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ApplyChanges(TreeWalk treeWalk, DirCache cache, DirCacheEditor editor
			)
		{
			FilePath workingTree = repo.WorkTree;
			while (treeWalk.Next())
			{
				string path = treeWalk.PathString;
				FilePath file = new FilePath(workingTree, path);
				// State of the stashed HEAD, index, and working directory
				AbstractTreeIterator stashHeadIter = treeWalk.GetTree<AbstractTreeIterator>(0);
				AbstractTreeIterator stashIndexIter = treeWalk.GetTree<AbstractTreeIterator>(1);
				AbstractTreeIterator stashWorkingIter = treeWalk.GetTree<AbstractTreeIterator>(2);
				if (stashWorkingIter != null && stashIndexIter != null)
				{
					// Checkout index change
					DirCacheEntry entry = cache.GetEntry(path);
					if (entry == null)
					{
						entry = new DirCacheEntry(treeWalk.RawPath);
					}
					entry.FileMode = stashIndexIter.EntryFileMode;
					entry.SetObjectId(stashIndexIter.EntryObjectId);
					DirCacheCheckout.CheckoutEntry(repo, file, entry, treeWalk.ObjectReader);
					DirCacheEntry updatedEntry = entry;
					editor.Add(new _PathEdit_271(updatedEntry, path));
					// Checkout working directory change
					if (!stashWorkingIter.IdEqual(stashIndexIter))
					{
						entry = new DirCacheEntry(treeWalk.RawPath);
						entry.SetObjectId(stashWorkingIter.EntryObjectId);
						DirCacheCheckout.CheckoutEntry(repo, file, entry, treeWalk.ObjectReader);
					}
				}
				else
				{
					if (stashIndexIter == null || (stashHeadIter != null && !stashIndexIter.IdEqual(stashHeadIter
						)))
					{
						editor.Add(new DirCacheEditor.DeletePath(path));
					}
					FileUtils.Delete(file, FileUtils.RETRY | FileUtils.SKIP_MISSING);
				}
			}
		}

		private sealed class _PathEdit_271 : DirCacheEditor.PathEdit
		{
			public _PathEdit_271(DirCacheEntry updatedEntry, string baseArg1) : base(baseArg1
				)
			{
				this.updatedEntry = updatedEntry;
			}

			public override void Apply(DirCacheEntry ent)
			{
				ent.CopyMetaData(updatedEntry);
			}

			private readonly DirCacheEntry updatedEntry;
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
			ObjectId headTree = GetHeadTree();
			ObjectId stashId = GetStashId();
			ObjectReader reader = repo.NewObjectReader();
			try
			{
				RevWalk revWalk = new RevWalk(reader);
				RevCommit stashCommit = revWalk.ParseCommit(stashId);
				if (stashCommit.ParentCount != 2)
				{
					throw new JGitInternalException(MessageFormat.Format(JGitText.Get().stashCommitMissingTwoParents
						, stashId.Name));
				}
				RevTree stashWorkingTree = stashCommit.Tree;
				RevTree stashIndexTree = revWalk.ParseCommit(stashCommit.GetParent(1)).Tree;
				RevTree stashHeadTree = revWalk.ParseCommit(stashCommit.GetParent(0)).Tree;
				CanonicalTreeParser stashWorkingIter = new CanonicalTreeParser();
				stashWorkingIter.Reset(reader, stashWorkingTree);
				CanonicalTreeParser stashIndexIter = new CanonicalTreeParser();
				stashIndexIter.Reset(reader, stashIndexTree);
				CanonicalTreeParser stashHeadIter = new CanonicalTreeParser();
				stashHeadIter.Reset(reader, stashHeadTree);
				CanonicalTreeParser headIter = new CanonicalTreeParser();
				headIter.Reset(reader, headTree);
				DirCache cache = repo.LockDirCache();
				DirCacheEditor editor = cache.Editor();
				try
				{
					DirCacheIterator indexIter = new DirCacheIterator(cache);
					FileTreeIterator workingIter = new FileTreeIterator(repo);
					TreeWalk treeWalk = new TreeWalk(reader);
					treeWalk.Recursive = true;
					treeWalk.Filter = new StashApplyCommand.StashDiffFilter();
					treeWalk.AddTree(stashHeadIter);
					treeWalk.AddTree(stashIndexIter);
					treeWalk.AddTree(stashWorkingIter);
					treeWalk.AddTree(headIter);
					treeWalk.AddTree(indexIter);
					treeWalk.AddTree(workingIter);
					ScanForConflicts(treeWalk);
					// Reset trees and walk
					treeWalk.Reset();
					stashWorkingIter.Reset(reader, stashWorkingTree);
					stashIndexIter.Reset(reader, stashIndexTree);
					stashHeadIter.Reset(reader, stashHeadTree);
					treeWalk.AddTree(stashHeadIter);
					treeWalk.AddTree(stashIndexIter);
					treeWalk.AddTree(stashWorkingIter);
					ApplyChanges(treeWalk, cache, editor);
				}
				finally
				{
					editor.Commit();
					cache.Unlock();
				}
			}
			catch (JGitInternalException e)
			{
				throw;
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
