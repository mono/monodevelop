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
using NGit.Dircache;
using NGit.Errors;
using NGit.Internal;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using NGit.Util.IO;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>This class handles checking out one or two trees merging with the index.
	/// 	</summary>
	/// <remarks>This class handles checking out one or two trees merging with the index.
	/// 	</remarks>
	public class DirCacheCheckout
	{
		private Repository repo;

		private Dictionary<string, ObjectId> updated = new Dictionary<string, ObjectId>();

		private AList<string> conflicts = new AList<string>();

		private AList<string> removed = new AList<string>();

		private ObjectId mergeCommitTree;

		private DirCache dc;

		private DirCacheBuilder builder;

		private NameConflictTreeWalk walk;

		private ObjectId headCommitTree;

		private WorkingTreeIterator workingTree;

		private bool failOnConflict = true;

		private AList<string> toBeDeleted = new AList<string>();

		/// <returns>a list of updated paths and objectIds</returns>
		public virtual IDictionary<string, ObjectId> GetUpdated()
		{
			return updated;
		}

		/// <returns>a list of conflicts created by this checkout</returns>
		public virtual IList<string> GetConflicts()
		{
			return conflicts;
		}

		/// <returns>
		/// a list of paths (relative to the start of the working tree) of
		/// files which couldn't be deleted during last call to
		/// <see cref="Checkout()">Checkout()</see>
		/// .
		/// <see cref="Checkout()">Checkout()</see>
		/// detected that these
		/// files should be deleted but the deletion in the filesystem failed
		/// (e.g. because a file was locked). To have a consistent state of
		/// the working tree these files have to be deleted by the callers of
		/// <see cref="DirCacheCheckout">DirCacheCheckout</see>
		/// .
		/// </returns>
		public virtual IList<string> GetToBeDeleted()
		{
			return toBeDeleted;
		}

		/// <returns>a list of all files removed by this checkout</returns>
		public virtual IList<string> GetRemoved()
		{
			return removed;
		}

		/// <summary>
		/// Constructs a DirCacheCeckout for merging and checking out two trees (HEAD
		/// and mergeCommitTree) and the index.
		/// </summary>
		/// <remarks>
		/// Constructs a DirCacheCeckout for merging and checking out two trees (HEAD
		/// and mergeCommitTree) and the index.
		/// </remarks>
		/// <param name="repo">the repository in which we do the checkout</param>
		/// <param name="headCommitTree">the id of the tree of the head commit</param>
		/// <param name="dc">the (already locked) Dircache for this repo</param>
		/// <param name="mergeCommitTree">the id of the tree we want to fast-forward to</param>
		/// <param name="workingTree">an iterator over the repositories Working Tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public DirCacheCheckout(Repository repo, ObjectId headCommitTree, DirCache dc, ObjectId
			 mergeCommitTree, WorkingTreeIterator workingTree)
		{
			this.repo = repo;
			this.dc = dc;
			this.headCommitTree = headCommitTree;
			this.mergeCommitTree = mergeCommitTree;
			this.workingTree = workingTree;
		}

		/// <summary>
		/// Constructs a DirCacheCeckout for merging and checking out two trees (HEAD
		/// and mergeCommitTree) and the index.
		/// </summary>
		/// <remarks>
		/// Constructs a DirCacheCeckout for merging and checking out two trees (HEAD
		/// and mergeCommitTree) and the index. As iterator over the working tree
		/// this constructor creates a standard
		/// <see cref="NGit.Treewalk.FileTreeIterator">NGit.Treewalk.FileTreeIterator</see>
		/// </remarks>
		/// <param name="repo">the repository in which we do the checkout</param>
		/// <param name="headCommitTree">the id of the tree of the head commit</param>
		/// <param name="dc">the (already locked) Dircache for this repo</param>
		/// <param name="mergeCommitTree">the id of the tree we want to fast-forward to</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public DirCacheCheckout(Repository repo, ObjectId headCommitTree, DirCache dc, ObjectId
			 mergeCommitTree) : this(repo, headCommitTree, dc, mergeCommitTree, new FileTreeIterator
			(repo))
		{
		}

		/// <summary>
		/// Constructs a DirCacheCeckout for checking out one tree, merging with the
		/// index.
		/// </summary>
		/// <remarks>
		/// Constructs a DirCacheCeckout for checking out one tree, merging with the
		/// index.
		/// </remarks>
		/// <param name="repo">the repository in which we do the checkout</param>
		/// <param name="dc">the (already locked) Dircache for this repo</param>
		/// <param name="mergeCommitTree">the id of the tree we want to fast-forward to</param>
		/// <param name="workingTree">an iterator over the repositories Working Tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public DirCacheCheckout(Repository repo, DirCache dc, ObjectId mergeCommitTree, WorkingTreeIterator
			 workingTree) : this(repo, null, dc, mergeCommitTree, workingTree)
		{
		}

		/// <summary>
		/// Constructs a DirCacheCeckout for checking out one tree, merging with the
		/// index.
		/// </summary>
		/// <remarks>
		/// Constructs a DirCacheCeckout for checking out one tree, merging with the
		/// index. As iterator over the working tree this constructor creates a
		/// standard
		/// <see cref="NGit.Treewalk.FileTreeIterator">NGit.Treewalk.FileTreeIterator</see>
		/// </remarks>
		/// <param name="repo">the repository in which we do the checkout</param>
		/// <param name="dc">the (already locked) Dircache for this repo</param>
		/// <param name="mergeCommitTree">the id of the tree of the</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public DirCacheCheckout(Repository repo, DirCache dc, ObjectId mergeCommitTree) : 
			this(repo, null, dc, mergeCommitTree, new FileTreeIterator(repo))
		{
		}

		/// <summary>Scan head, index and merge tree.</summary>
		/// <remarks>
		/// Scan head, index and merge tree. Used during normal checkout or merge
		/// operations.
		/// </remarks>
		/// <exception cref="NGit.Errors.CorruptObjectException">NGit.Errors.CorruptObjectException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void PreScanTwoTrees()
		{
			removed.Clear();
			updated.Clear();
			conflicts.Clear();
			walk = new NameConflictTreeWalk(repo);
			builder = dc.Builder();
			AddTree(walk, headCommitTree);
			AddTree(walk, mergeCommitTree);
			walk.AddTree(new DirCacheBuildIterator(builder));
			walk.AddTree(workingTree);
			while (walk.Next())
			{
				ProcessEntry(walk.GetTree<CanonicalTreeParser>(0), walk.GetTree<CanonicalTreeParser
					>(1), walk.GetTree<DirCacheBuildIterator>(2), walk.GetTree<WorkingTreeIterator>(
					3));
				if (walk.IsSubtree)
				{
					walk.EnterSubtree();
				}
			}
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void AddTree(TreeWalk tw, ObjectId id)
		{
			if (id == null)
			{
				tw.AddTree(new EmptyTreeIterator());
			}
			else
			{
				tw.AddTree(id);
			}
		}

		/// <summary>Scan index and merge tree (no HEAD).</summary>
		/// <remarks>
		/// Scan index and merge tree (no HEAD). Used e.g. for initial checkout when
		/// there is no head yet.
		/// </remarks>
		/// <exception cref="NGit.Errors.MissingObjectException">NGit.Errors.MissingObjectException
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">NGit.Errors.IncorrectObjectTypeException
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">NGit.Errors.CorruptObjectException
		/// 	</exception>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void PrescanOneTree()
		{
			removed.Clear();
			updated.Clear();
			conflicts.Clear();
			builder = dc.Builder();
			walk = new NameConflictTreeWalk(repo);
			walk.AddTree(mergeCommitTree);
			walk.AddTree(new DirCacheBuildIterator(builder));
			walk.AddTree(workingTree);
			while (walk.Next())
			{
				ProcessEntry(walk.GetTree<CanonicalTreeParser>(0), walk.GetTree<DirCacheBuildIterator
					>(1), walk.GetTree<WorkingTreeIterator>(2));
				if (walk.IsSubtree)
				{
					walk.EnterSubtree();
				}
			}
			conflicts.RemoveAll(removed);
		}

		/// <summary>
		/// Processing an entry in the context of
		/// <see cref="PrescanOneTree()">PrescanOneTree()</see>
		/// when only
		/// one tree is given
		/// </summary>
		/// <param name="m">the tree to merge</param>
		/// <param name="i">the index</param>
		/// <param name="f">the working tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal virtual void ProcessEntry(CanonicalTreeParser m, DirCacheBuildIterator i
			, WorkingTreeIterator f)
		{
			if (m != null)
			{
				// There is an entry in the merge commit. Means: we want to update
				// what's currently in the index and working-tree to that one
				if (i == null)
				{
					// The index entry is missing
					if (f != null && !FileMode.TREE.Equals(f.EntryFileMode) && !f.IsEntryIgnored())
					{
						// don't overwrite an untracked and not ignored file
						conflicts.AddItem(walk.PathString);
					}
					else
					{
						Update(m.EntryPathString, m.EntryObjectId, m.EntryFileMode);
					}
				}
				else
				{
					if (f == null || !m.IdEqual(i))
					{
						// The working tree file is missing or the merge content differs
						// from index content
						Update(m.EntryPathString, m.EntryObjectId, m.EntryFileMode);
					}
					else
					{
						if (i.GetDirCacheEntry() != null)
						{
							// The index contains a file (and not a folder)
							if (f.IsModified(i.GetDirCacheEntry(), true) || i.GetDirCacheEntry().Stage != 0)
							{
								// The working tree file is dirty or the index contains a
								// conflict
								Update(m.EntryPathString, m.EntryObjectId, m.EntryFileMode);
							}
							else
							{
								Keep(i.GetDirCacheEntry());
							}
						}
						else
						{
							// The index contains a folder
							Keep(i.GetDirCacheEntry());
						}
					}
				}
			}
			else
			{
				// There is no entry in the merge commit. Means: we want to delete
				// what's currently in the index and working tree
				if (f != null)
				{
					// There is a file/folder for that path in the working tree
					if (walk.IsDirectoryFileConflict())
					{
						conflicts.AddItem(walk.PathString);
					}
					else
					{
						// No file/folder conflict exists. All entries are files or
						// all entries are folders
						if (i != null)
						{
							// ... and the working tree contained a file or folder
							// -> add it to the removed set and remove it from
							// conflicts set
							Remove(i.EntryPathString);
							conflicts.Remove(i.EntryPathString);
						}
					}
				}
			}
		}

		// untracked file, neither contained in tree to merge
		// nor in index
		// There is no file/folder for that path in the working tree,
		// nor in the merge head.
		// The only entry we have is the index entry. Like the case
		// where there is a file with the same name, remove it,
		/// <summary>Execute this checkout</summary>
		/// <returns>
		/// <code>false</code> if this method could not delete all the files
		/// which should be deleted (e.g. because of of the files was
		/// locked). In this case
		/// <see cref="GetToBeDeleted()">GetToBeDeleted()</see>
		/// lists the files
		/// which should be tried to be deleted outside of this method.
		/// Although <code>false</code> is returned the checkout was
		/// successful and the working tree was updated for all other files.
		/// <code>true</code> is returned when no such problem occurred
		/// </returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool Checkout()
		{
			try
			{
				return DoCheckout();
			}
			finally
			{
				dc.Unlock();
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="NGit.Errors.CheckoutConflictException"></exception>
		/// <exception cref="NGit.Errors.IndexWriteException"></exception>
		private bool DoCheckout()
		{
			toBeDeleted.Clear();
			ObjectReader objectReader = repo.ObjectDatabase.NewReader();
			try
			{
				if (headCommitTree != null)
				{
					PreScanTwoTrees();
				}
				else
				{
					PrescanOneTree();
				}
				if (!conflicts.IsEmpty())
				{
					if (failOnConflict)
					{
						throw new NGit.Errors.CheckoutConflictException(Sharpen.Collections.ToArray(conflicts
							, new string[conflicts.Count]));
					}
					else
					{
						CleanUpConflicts();
					}
				}
				// update our index
				builder.Finish();
				FilePath file = null;
				string last = string.Empty;
				// when deleting files process them in the opposite order as they have
				// been reported. This ensures the files are deleted before we delete
				// their parent folders
				for (int i = removed.Count - 1; i >= 0; i--)
				{
					string r = removed[i];
					file = new FilePath(repo.WorkTree, r);
					if (!file.Delete() && file.Exists())
					{
						// The list of stuff to delete comes from the index
						// which will only contain a directory if it is
						// a submodule, in which case we shall not attempt
						// to delete it. A submodule is not empty, so it
						// is safe to check this after a failed delete.
						if (!file.IsDirectory())
						{
							toBeDeleted.AddItem(r);
						}
					}
					else
					{
						if (!IsSamePrefix(r, last))
						{
							RemoveEmptyParents(new FilePath(repo.WorkTree, last));
						}
						last = r;
					}
				}
				if (file != null)
				{
					RemoveEmptyParents(file);
				}
				foreach (string path in updated.Keys)
				{
					// ... create/overwrite this file ...
					file = new FilePath(repo.WorkTree, path);
					if (!file.GetParentFile().Mkdirs())
					{
					}
					// ignore
					DirCacheEntry entry = dc.GetEntry(path);
					// submodules are handled with separate operations
					if (FileMode.GITLINK.Equals(entry.RawMode))
					{
						continue;
					}
					CheckoutEntry(repo, file, entry, objectReader);
				}
				// commit the index builder - a new index is persisted
				if (!builder.Commit())
				{
					throw new IndexWriteException();
				}
			}
			finally
			{
				objectReader.Release();
			}
			return toBeDeleted.Count == 0;
		}

		private static bool IsSamePrefix(string a, string b)
		{
			int @as = a.LastIndexOf('/');
			int bs = b.LastIndexOf('/');
			return Sharpen.Runtime.Substring(a, 0, @as + 1).Equals(Sharpen.Runtime.Substring(
				b, 0, bs + 1));
		}

		private void RemoveEmptyParents(FilePath f)
		{
			FilePath parentFile = f.GetParentFile();
			while (!parentFile.Equals(repo.WorkTree))
			{
				if (!parentFile.Delete())
				{
					break;
				}
				parentFile = parentFile.GetParentFile();
			}
		}

		/// <summary>Compares whether two pairs of ObjectId and FileMode are equal.</summary>
		/// <remarks>Compares whether two pairs of ObjectId and FileMode are equal.</remarks>
		/// <param name="id1"></param>
		/// <param name="mode1"></param>
		/// <param name="id2"></param>
		/// <param name="mode2"></param>
		/// <returns>
		/// <code>true</code> if FileModes and ObjectIds are equal.
		/// <code>false</code> otherwise
		/// </returns>
		private bool EqualIdAndMode(ObjectId id1, FileMode mode1, ObjectId id2, FileMode 
			mode2)
		{
			if (!mode1.Equals(mode2))
			{
				return false;
			}
			return id1 != null ? id1.Equals(id2) : id2 == null;
		}

		/// <summary>Here the main work is done.</summary>
		/// <remarks>
		/// Here the main work is done. This method is called for each existing path
		/// in head, index and merge. This method decides what to do with the
		/// corresponding index entry: keep it, update it, remove it or mark a
		/// conflict.
		/// </remarks>
		/// <param name="h">the entry for the head</param>
		/// <param name="m">the entry for the merge</param>
		/// <param name="i">the entry for the index</param>
		/// <param name="f">the file in the working tree</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal virtual void ProcessEntry(AbstractTreeIterator h, AbstractTreeIterator m
			, DirCacheBuildIterator i, WorkingTreeIterator f)
		{
			DirCacheEntry dce = i != null ? i.GetDirCacheEntry() : null;
			string name = walk.PathString;
			if (i == null && m == null && h == null)
			{
				// File/Directory conflict case #20
				if (walk.IsDirectoryFileConflict())
				{
					// TODO: check whether it is always correct to report a conflict here
					Conflict(name, null, null, null);
				}
				// file only exists in working tree -> ignore it
				return;
			}
			ObjectId iId = (i == null ? null : i.EntryObjectId);
			ObjectId mId = (m == null ? null : m.EntryObjectId);
			ObjectId hId = (h == null ? null : h.EntryObjectId);
			FileMode iMode = (i == null ? null : i.EntryFileMode);
			FileMode mMode = (m == null ? null : m.EntryFileMode);
			FileMode hMode = (h == null ? null : h.EntryFileMode);
			// The information whether head,index,merge iterators are currently
			// pointing to file/folder/non-existing is encoded into this variable.
			//
			// To decode write down ffMask in hexadecimal form. The last digit
			// represents the state for the merge iterator, the second last the
			// state for the index iterator and the third last represents the state
			// for the head iterator. The hexadecimal constant "F" stands for
			// "file",
			// an "D" stands for "directory" (tree), and a "0" stands for
			// non-existing
			//
			// Examples:
			// ffMask == 0xFFD -> Head=File, Index=File, Merge=Tree
			// ffMask == 0xDD0 -> Head=Tree, Index=Tree, Merge=Non-Existing
			int ffMask = 0;
			if (h != null)
			{
				ffMask = FileMode.TREE.Equals(hMode) ? unchecked((int)(0xD00)) : unchecked((int)(
					0xF00));
			}
			if (i != null)
			{
				ffMask |= FileMode.TREE.Equals(iMode) ? unchecked((int)(0x0D0)) : unchecked((int)
					(0x0F0));
			}
			if (m != null)
			{
				ffMask |= FileMode.TREE.Equals(mMode) ? unchecked((int)(0x00D)) : unchecked((int)
					(0x00F));
			}
			// Check whether we have a possible file/folder conflict. Therefore we
			// need a least one file and one folder.
			if (((ffMask & unchecked((int)(0x222))) != unchecked((int)(0x000))) && (((ffMask 
				& unchecked((int)(0x00F))) == unchecked((int)(0x00D))) || ((ffMask & unchecked((
				int)(0x0F0))) == unchecked((int)(0x0D0))) || ((ffMask & unchecked((int)(0xF00)))
				 == unchecked((int)(0xD00)))))
			{
				switch (ffMask)
				{
					case unchecked((int)(0xDDF)):
					{
						// There are 3*3*3=27 possible combinations of file/folder
						// conflicts. Some of them are not-relevant because
						// they represent no conflict, e.g. 0xFFF, 0xDDD, ... The following
						// switch processes all relevant cases.
						// 1 2
						if (IsModified(name))
						{
							Conflict(name, dce, h, m);
						}
						else
						{
							// 1
							Update(name, mId, mMode);
						}
						// 2
						break;
					}

					case unchecked((int)(0xDFD)):
					{
						// 3 4
						// CAUTION: I put it into removed instead of updated, because
						// that's what our tests expect
						// updated.put(name, mId);
						Remove(name);
						break;
					}

					case unchecked((int)(0xF0D)):
					{
						// 18
						Remove(name);
						break;
					}

					case unchecked((int)(0xDFF)):
					case unchecked((int)(0xFDD)):
					{
						// 5 6
						// 10 11
						// TODO: make use of tree extension as soon as available in jgit
						// we would like to do something like
						// if (!equalIdAndMode(iId, iMode, mId, mMode)
						//   conflict(name, i.getDirCacheEntry(), h, m);
						// But since we don't know the id of a tree in the index we do
						// nothing here and wait that conflicts between index and merge
						// are found later
						break;
					}

					case unchecked((int)(0xD0F)):
					{
						// 19
						Update(name, mId, mMode);
						break;
					}

					case unchecked((int)(0xDF0)):
					case unchecked((int)(0x0FD)):
					{
						// conflict without a rule
						// 15
						Conflict(name, dce, h, m);
						break;
					}

					case unchecked((int)(0xFDF)):
					{
						// 7 8 9
						if (EqualIdAndMode(hId, hMode, mId, mMode))
						{
							if (IsModified(name))
							{
								Conflict(name, dce, h, m);
							}
							else
							{
								// 8
								Update(name, mId, mMode);
							}
						}
						else
						{
							// 7
							if (!IsModified(name))
							{
								Update(name, mId, mMode);
							}
							else
							{
								// 9
								// To be confirmed - this case is not in the table.
								Conflict(name, dce, h, m);
							}
						}
						break;
					}

					case unchecked((int)(0xFD0)):
					{
						// keep without a rule
						Keep(dce);
						break;
					}

					case unchecked((int)(0xFFD)):
					{
						// 12 13 14
						if (EqualIdAndMode(hId, hMode, iId, iMode))
						{
							if (f == null || f.IsModified(dce, true))
							{
								Conflict(name, dce, h, m);
							}
							else
							{
								Remove(name);
							}
						}
						else
						{
							Conflict(name, dce, h, m);
						}
						break;
					}

					case unchecked((int)(0x0DF)):
					{
						// 16 17
						if (!IsModified(name))
						{
							Update(name, mId, mMode);
						}
						else
						{
							Conflict(name, dce, h, m);
						}
						break;
					}

					default:
					{
						Keep(dce);
						break;
					}
				}
				return;
			}
			// if we have no file at all then there is nothing to do
			if ((ffMask & unchecked((int)(0x222))) == 0)
			{
				return;
			}
			if ((ffMask == unchecked((int)(0x00F))) && f != null && FileMode.TREE.Equals(f.EntryFileMode
				))
			{
				// File/Directory conflict case #20
				Conflict(name, null, h, m);
			}
			if (i == null)
			{
				// make sure not to overwrite untracked files
				if (f != null)
				{
					// A submodule is not a file. We should ignore it
					if (!FileMode.GITLINK.Equals(mMode))
					{
						// a dirty worktree: the index is empty but we have a
						// workingtree-file
						if (mId == null || !EqualIdAndMode(mId, mMode, f.EntryObjectId, f.EntryFileMode))
						{
							Conflict(name, null, h, m);
							return;
						}
					}
				}
				if (h == null)
				{
					Update(name, mId, mMode);
				}
				else
				{
					// 1
					if (m == null)
					{
						Remove(name);
					}
					else
					{
						// 2
						Update(name, mId, mMode);
					}
				}
			}
			else
			{
				// 3
				if (h == null)
				{
					if (m == null || EqualIdAndMode(mId, mMode, iId, iMode))
					{
						if (m == null && walk.IsDirectoryFileConflict())
						{
							if (dce != null && (f == null || f.IsModified(dce, true)))
							{
								Conflict(name, dce, h, m);
							}
							else
							{
								Remove(name);
							}
						}
						else
						{
							Keep(dce);
						}
					}
					else
					{
						Conflict(name, dce, h, m);
					}
				}
				else
				{
					if (m == null)
					{
						if (iMode == FileMode.GITLINK)
						{
							// Submodules that disappear from the checkout must
							// be removed from the index, but not deleted from disk.
							Remove(name);
						}
						else
						{
							if (EqualIdAndMode(hId, hMode, iId, iMode))
							{
								if (f == null || f.IsModified(dce, true))
								{
									Conflict(name, dce, h, m);
								}
								else
								{
									Remove(name);
								}
							}
							else
							{
								Conflict(name, dce, h, m);
							}
						}
					}
					else
					{
						if (!EqualIdAndMode(hId, hMode, mId, mMode) && !EqualIdAndMode(hId, hMode, iId, iMode
							) && !EqualIdAndMode(mId, mMode, iId, iMode))
						{
							Conflict(name, dce, h, m);
						}
						else
						{
							if (EqualIdAndMode(hId, hMode, iId, iMode) && !EqualIdAndMode(mId, mMode, iId, iMode
								))
							{
								// For submodules just update the index with the new SHA-1
								if (dce != null && FileMode.GITLINK.Equals(dce.FileMode))
								{
									Update(name, mId, mMode);
								}
								else
								{
									if (dce != null && (f == null || f.IsModified(dce, true)))
									{
										Conflict(name, dce, h, m);
									}
									else
									{
										Update(name, mId, mMode);
									}
								}
							}
							else
							{
								Keep(dce);
							}
						}
					}
				}
			}
		}

		/// <summary>A conflict is detected - add the three different stages to the index</summary>
		/// <param name="path">the path of the conflicting entry</param>
		/// <param name="e">the previous index entry</param>
		/// <param name="h">the first tree you want to merge (the HEAD)</param>
		/// <param name="m">the second tree you want to merge</param>
		private void Conflict(string path, DirCacheEntry e, AbstractTreeIterator h, AbstractTreeIterator
			 m)
		{
			conflicts.AddItem(path);
			DirCacheEntry entry;
			if (e != null)
			{
				entry = new DirCacheEntry(e.PathString, DirCacheEntry.STAGE_1);
				entry.CopyMetaData(e, true);
				builder.Add(entry);
			}
			if (h != null && !FileMode.TREE.Equals(h.EntryFileMode))
			{
				entry = new DirCacheEntry(h.EntryPathString, DirCacheEntry.STAGE_2);
				entry.FileMode = h.EntryFileMode;
				entry.SetObjectId(h.EntryObjectId);
				builder.Add(entry);
			}
			if (m != null && !FileMode.TREE.Equals(m.EntryFileMode))
			{
				entry = new DirCacheEntry(m.EntryPathString, DirCacheEntry.STAGE_3);
				entry.FileMode = m.EntryFileMode;
				entry.SetObjectId(m.EntryObjectId);
				builder.Add(entry);
			}
		}

		private void Keep(DirCacheEntry e)
		{
			if (e != null && !FileMode.TREE.Equals(e.FileMode))
			{
				builder.Add(e);
			}
		}

		private void Remove(string path)
		{
			removed.AddItem(path);
		}

		private void Update(string path, ObjectId mId, FileMode mode)
		{
			if (!FileMode.TREE.Equals(mode))
			{
				updated.Put(path, mId);
				DirCacheEntry entry = new DirCacheEntry(path, DirCacheEntry.STAGE_0);
				entry.SetObjectId(mId);
				entry.FileMode = mode;
				builder.Add(entry);
			}
		}

		/// <summary>
		/// If <code>true</code>, will scan first to see if it's possible to check
		/// out, otherwise throw
		/// <see cref="NGit.Errors.CheckoutConflictException">NGit.Errors.CheckoutConflictException
		/// 	</see>
		/// . If
		/// <code>false</code>, it will silently deal with the problem.
		/// </summary>
		/// <param name="failOnConflict"></param>
		public virtual void SetFailOnConflict(bool failOnConflict)
		{
			this.failOnConflict = failOnConflict;
		}

		/// <summary>
		/// This method implements how to handle conflicts when
		/// <see cref="failOnConflict">failOnConflict</see>
		/// is false
		/// </summary>
		/// <exception cref="NGit.Errors.CheckoutConflictException">NGit.Errors.CheckoutConflictException
		/// 	</exception>
		private void CleanUpConflicts()
		{
			// TODO: couldn't we delete unsaved worktree content here?
			foreach (string c in conflicts)
			{
				FilePath conflict = new FilePath(repo.WorkTree, c);
				if (!conflict.Delete())
				{
					throw new NGit.Errors.CheckoutConflictException(MessageFormat.Format(JGitText.Get
						().cannotDeleteFile, c));
				}
				RemoveEmptyParents(conflict);
			}
			foreach (string r in removed)
			{
				FilePath file = new FilePath(repo.WorkTree, r);
				if (!file.Delete())
				{
					throw new NGit.Errors.CheckoutConflictException(MessageFormat.Format(JGitText.Get
						().cannotDeleteFile, file.GetAbsolutePath()));
				}
				RemoveEmptyParents(file);
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private bool IsModified(string path)
		{
			NameConflictTreeWalk tw = new NameConflictTreeWalk(repo);
			tw.AddTree(new DirCacheIterator(dc));
			tw.AddTree(new FileTreeIterator(repo));
			tw.Recursive = true;
			tw.Filter = PathFilter.Create(path);
			DirCacheIterator dcIt;
			WorkingTreeIterator wtIt;
			while (tw.Next())
			{
				dcIt = tw.GetTree<DirCacheIterator>(0);
				wtIt = tw.GetTree<WorkingTreeIterator>(1);
				if (dcIt == null || wtIt == null)
				{
					return true;
				}
				if (wtIt.IsModified(dcIt.GetDirCacheEntry(), true))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Updates the file in the working tree with content and mode from an entry
		/// in the index.
		/// </summary>
		/// <remarks>
		/// Updates the file in the working tree with content and mode from an entry
		/// in the index. The new content is first written to a new temporary file in
		/// the same directory as the real file. Then that new file is renamed to the
		/// final filename. Use this method only for checkout of a single entry.
		/// Otherwise use
		/// <code>checkoutEntry(Repository, File f, DirCacheEntry, ObjectReader)</code>
		/// instead which allows to reuse one
		/// <code>ObjectReader</code>
		/// for multiple
		/// entries.
		/// <p>
		/// TODO: this method works directly on File IO, we may need another
		/// abstraction (like WorkingTreeIterator). This way we could tell e.g.
		/// Eclipse that Files in the workspace got changed
		/// </p>
		/// </remarks>
		/// <param name="repository"></param>
		/// <param name="f">
		/// the file to be modified. The parent directory for this file
		/// has to exist already
		/// </param>
		/// <param name="entry">the entry containing new mode and content</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static void CheckoutEntry(Repository repository, FilePath f, DirCacheEntry
			 entry)
		{
			ObjectReader or = repository.NewObjectReader();
			try
			{
				CheckoutEntry(repository, f, entry, repository.NewObjectReader());
			}
			finally
			{
				or.Release();
			}
		}

		/// <summary>
		/// Updates the file in the working tree with content and mode from an entry
		/// in the index.
		/// </summary>
		/// <remarks>
		/// Updates the file in the working tree with content and mode from an entry
		/// in the index. The new content is first written to a new temporary file in
		/// the same directory as the real file. Then that new file is renamed to the
		/// final filename.
		/// <p>
		/// TODO: this method works directly on File IO, we may need another
		/// abstraction (like WorkingTreeIterator). This way we could tell e.g.
		/// Eclipse that Files in the workspace got changed
		/// </p>
		/// </remarks>
		/// <param name="repo"></param>
		/// <param name="f">
		/// the file to be modified. The parent directory for this file
		/// has to exist already
		/// </param>
		/// <param name="entry">the entry containing new mode and content</param>
		/// <param name="or">object reader to use for checkout</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static void CheckoutEntry(Repository repo, FilePath f, DirCacheEntry entry
			, ObjectReader or)
		{
			ObjectLoader ol = or.Open(entry.GetObjectId());
			FilePath parentDir = f.GetParentFile();
			FilePath tmpFile = FilePath.CreateTempFile("._" + f.GetName(), null, parentDir);
			WorkingTreeOptions opt = repo.GetConfig().Get(WorkingTreeOptions.KEY);
			FileOutputStream rawChannel = new FileOutputStream(tmpFile);
			OutputStream channel;
			if (opt.GetAutoCRLF() == CoreConfig.AutoCRLF.TRUE)
			{
				channel = new AutoCRLFOutputStream(rawChannel);
			}
			else
			{
				channel = rawChannel;
			}
			try
			{
				ol.CopyTo(channel);
			}
			finally
			{
				channel.Close();
			}
			FS fs = repo.FileSystem;
			if (opt.IsFileMode() && fs.SupportsExecute())
			{
				if (FileMode.EXECUTABLE_FILE.Equals(entry.RawMode))
				{
					if (!fs.CanExecute(tmpFile))
					{
						fs.SetExecute(tmpFile, true);
					}
				}
				else
				{
					if (fs.CanExecute(tmpFile))
					{
						fs.SetExecute(tmpFile, false);
					}
				}
			}
			if (!tmpFile.RenameTo(f))
			{
				// tried to rename which failed. Let' delete the target file and try
				// again
				FileUtils.Delete(f);
				if (!tmpFile.RenameTo(f))
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().couldNotWriteFile, tmpFile
						.GetPath(), f.GetPath()));
				}
			}
			entry.LastModified = f.LastModified();
			if (opt.GetAutoCRLF() != CoreConfig.AutoCRLF.FALSE)
			{
				entry.SetLength(f.Length());
			}
			else
			{
				// AutoCRLF wants on-disk-size
				entry.SetLength((int)ol.GetSize());
			}
		}
	}
}
