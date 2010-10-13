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
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>This class handles checking out one or two trees merging with the index.
	/// 	</summary>
	/// <remarks>
	/// This class handles checking out one or two trees merging with the index. This
	/// class does similar things as
	/// <code>WorkDirCheckout</code>
	/// but uses
	/// <see cref="DirCache">DirCache</see>
	/// instead of
	/// <code>GitIndex</code>
	/// <p>
	/// The initial implementation of this class was refactored from
	/// WorkDirCheckout}.
	/// </remarks>
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

		/// <returns>a list of updated pathes and objectIds</returns>
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
			return conflicts;
		}

		/// <returns>a list of all files removed by this checkout</returns>
		public virtual IList<string> GetRemoved()
		{
			return removed;
		}

		/// <summary>
		/// Constructs a DirCacheCeckout for fast-forwarding from one tree to
		/// another, merging it with the index
		/// </summary>
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
		/// <param name="headCommitTree">the id of the tree of the head commit</param>
		/// <param name="dc">the (already locked) Dircache for this repo</param>
		/// <param name="mergeCommitTree">the id of the tree of the</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public DirCacheCheckout(Repository repo, ObjectId headCommitTree, DirCache dc, ObjectId
			 mergeCommitTree) : this(repo, headCommitTree, dc, mergeCommitTree, new FileTreeIterator
			(repo.WorkTree, repo.FileSystem, WorkingTreeOptions.CreateDefaultInstance()))
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
			walk.Reset();
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
			walk.Reset();
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
		internal virtual void ProcessEntry(CanonicalTreeParser m, DirCacheBuildIterator i
			, WorkingTreeIterator f)
		{
			if (m != null)
			{
				Update(m.GetEntryPathString(), m.GetEntryObjectId(), m.GetEntryFileMode());
			}
			else
			{
				if (f != null)
				{
					if (walk.IsDirectoryFileConflict())
					{
						conflicts.AddItem(walk.PathString);
					}
					else
					{
						// ... and the working dir contained a file or folder ->
						// add it to the removed set and remove it from conflicts set
						Remove(f.GetEntryPathString());
						conflicts.Remove(f.GetEntryPathString());
					}
				}
				else
				{
					Keep(i.GetDirCacheEntry());
				}
			}
		}

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
			toBeDeleted.Clear();
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
					dc.Unlock();
					throw new CheckoutConflictException(Sharpen.Collections.ToArray(conflicts, new string
						[conflicts.Count]));
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
			foreach (string r in removed)
			{
				file = new FilePath(repo.WorkTree, r);
				if (!file.Delete())
				{
					toBeDeleted.AddItem(r);
				}
				else
				{
					if (!IsSamePrefix(r, last))
					{
						RemoveEmptyParents(file);
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
				file.GetParentFile().Mkdirs();
				file.CreateNewFile();
				DirCacheEntry entry = dc.GetEntry(path);
				CheckoutEntry(repo, file, entry, Config_filemode());
			}
			// commit the index builder - a new index is persisted
			if (!builder.Commit())
			{
				dc.Unlock();
				throw new IndexWriteException();
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
			DirCacheEntry dce;
			string name = walk.PathString;
			if (i == null && m == null && h == null)
			{
				// File/Directory conflict case #20
				if (walk.IsDirectoryFileConflict())
				{
					// TODO: check whether it is always correct to report a conflict here
					Conflict(name, null, h, m);
				}
				// file only exists in working tree -> ignore it
				return;
			}
			ObjectId iId = (i == null ? null : i.GetEntryObjectId());
			ObjectId mId = (m == null ? null : m.GetEntryObjectId());
			ObjectId hId = (h == null ? null : h.GetEntryObjectId());
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
				ffMask = FileMode.TREE.Equals(h.GetEntryFileMode()) ? unchecked((int)(0xD00)) : unchecked(
					(int)(0xF00));
			}
			if (i != null)
			{
				ffMask |= FileMode.TREE.Equals(i.GetEntryFileMode()) ? unchecked((int)(0x0D0)) : 
					unchecked((int)(0x0F0));
			}
			if (m != null)
			{
				ffMask |= FileMode.TREE.Equals(m.GetEntryFileMode()) ? unchecked((int)(0x00D)) : 
					unchecked((int)(0x00F));
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
							Conflict(name, i.GetDirCacheEntry(), h, m);
						}
						else
						{
							// 1
							Update(name, m.GetEntryObjectId(), m.GetEntryFileMode());
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
						// if (!iId.equals(mId))
						//   conflict(name, i.getDirCacheEntry(), h, m);
						// But since we don't know the id of a tree in the index we do
						// nothing here and wait that conflicts between index and merge
						// are found later
						break;
					}

					case unchecked((int)(0xD0F)):
					{
						// 19
						Update(name, mId, m.GetEntryFileMode());
						break;
					}

					case unchecked((int)(0xDF0)):
					case unchecked((int)(0x0FD)):
					{
						// conflict without a rule
						// 15
						Conflict(name, (i != null) ? i.GetDirCacheEntry() : null, h, m);
						break;
					}

					case unchecked((int)(0xFDF)):
					{
						// 7 8 9
						dce = i.GetDirCacheEntry();
						if (hId.Equals(mId))
						{
							if (IsModified(name))
							{
								Conflict(name, i.GetDirCacheEntry(), h, m);
							}
							else
							{
								// 8
								Update(name, mId, m.GetEntryFileMode());
							}
						}
						else
						{
							// 7
							if (!IsModified(name))
							{
								Update(name, mId, m.GetEntryFileMode());
							}
							else
							{
								// 9
								// To be confirmed - this case is not in the table.
								Conflict(name, i.GetDirCacheEntry(), h, m);
							}
						}
						break;
					}

					case unchecked((int)(0xFD0)):
					{
						// keep without a rule
						Keep(i.GetDirCacheEntry());
						break;
					}

					case unchecked((int)(0xFFD)):
					{
						// 12 13 14
						if (hId.Equals(iId))
						{
							dce = i.GetDirCacheEntry();
							if (f == null || f.IsModified(dce, true, Config_filemode(), repo.FileSystem))
							{
								Conflict(name, i.GetDirCacheEntry(), h, m);
							}
							else
							{
								Remove(name);
							}
						}
						else
						{
							Conflict(name, i.GetDirCacheEntry(), h, m);
						}
						break;
					}

					case unchecked((int)(0x0DF)):
					{
						// 16 17
						if (!IsModified(name))
						{
							Update(name, mId, m.GetEntryFileMode());
						}
						else
						{
							Conflict(name, i.GetDirCacheEntry(), h, m);
						}
						break;
					}

					default:
					{
						Keep(i.GetDirCacheEntry());
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
			if ((ffMask == unchecked((int)(0x00F))) && f != null && FileMode.TREE.Equals(f.GetEntryFileMode
				()))
			{
				// File/Directory conflict case #20
				Conflict(name, null, h, m);
			}
			if (i == null)
			{
				if (h == null)
				{
					Update(name, mId, m.GetEntryFileMode());
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
						Update(name, mId, m.GetEntryFileMode());
					}
				}
			}
			else
			{
				// 3
				dce = i.GetDirCacheEntry();
				if (h == null)
				{
					if (m == null || mId.Equals(iId))
					{
						if (m == null && walk.IsDirectoryFileConflict())
						{
							if (dce != null && (f == null || f.IsModified(dce, true, Config_filemode(), repo.
								FileSystem)))
							{
								Conflict(name, i.GetDirCacheEntry(), h, m);
							}
							else
							{
								Remove(name);
							}
						}
						else
						{
							Keep(i.GetDirCacheEntry());
						}
					}
					else
					{
						Conflict(name, i.GetDirCacheEntry(), h, m);
					}
				}
				else
				{
					if (m == null)
					{
						if (hId.Equals(iId))
						{
							if (f == null || f.IsModified(dce, true, Config_filemode(), repo.FileSystem))
							{
								Conflict(name, i.GetDirCacheEntry(), h, m);
							}
							else
							{
								Remove(name);
							}
						}
						else
						{
							Conflict(name, i.GetDirCacheEntry(), h, m);
						}
					}
					else
					{
						if (!hId.Equals(mId) && !hId.Equals(iId) && !mId.Equals(iId))
						{
							Conflict(name, i.GetDirCacheEntry(), h, m);
						}
						else
						{
							if (hId.Equals(iId) && !mId.Equals(iId))
							{
								if (dce != null && (f == null || f.IsModified(dce, true, Config_filemode(), repo.
									FileSystem)))
								{
									Conflict(name, i.GetDirCacheEntry(), h, m);
								}
								else
								{
									Update(name, mId, m.GetEntryFileMode());
								}
							}
							else
							{
								Keep(i.GetDirCacheEntry());
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
				entry = new DirCacheEntry(e.GetPathString(), DirCacheEntry.STAGE_1);
				entry.CopyMetaData(e);
				builder.Add(entry);
			}
			if (h != null && !FileMode.TREE.Equals(h.GetEntryFileMode()))
			{
				entry = new DirCacheEntry(h.GetEntryPathString(), DirCacheEntry.STAGE_2);
				entry.SetFileMode(h.GetEntryFileMode());
				entry.SetObjectId(h.GetEntryObjectId());
				builder.Add(entry);
			}
			if (m != null && !FileMode.TREE.Equals(m.GetEntryFileMode()))
			{
				entry = new DirCacheEntry(m.GetEntryPathString(), DirCacheEntry.STAGE_3);
				entry.SetFileMode(m.GetEntryFileMode());
				entry.SetObjectId(m.GetEntryObjectId());
				builder.Add(entry);
			}
		}

		private void Keep(DirCacheEntry e)
		{
			if (e != null && !FileMode.TREE.Equals(e.GetFileMode()))
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
				entry.SetFileMode(mode);
				builder.Add(entry);
			}
		}

		private bool filemode;

		private bool Config_filemode()
		{
			// TODO: temporary till we can actually set parameters. We need to be
			// able to change this for testing.
			if (filemode == null)
			{
				StoredConfig config = repo.GetConfig();
				filemode = Sharpen.Extensions.ValueOf(config.GetBoolean("core", null, "filemode", 
					true));
			}
			return filemode;
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
					throw new CheckoutConflictException(MessageFormat.Format(JGitText.Get().cannotDeleteFile
						, c));
				}
				RemoveEmptyParents(conflict);
			}
			foreach (string r in removed)
			{
				FilePath file = new FilePath(repo.WorkTree, r);
				file.Delete();
				RemoveEmptyParents(file);
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private bool IsModified(string path)
		{
			NameConflictTreeWalk tw = new NameConflictTreeWalk(repo);
			tw.Reset();
			tw.AddTree(new DirCacheIterator(dc));
			tw.AddTree(new FileTreeIterator(repo.WorkTree, repo.FileSystem, WorkingTreeOptions
				.CreateDefaultInstance()));
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
				if (wtIt.IsModified(dcIt.GetDirCacheEntry(), true, Config_filemode(), repo.FileSystem
					))
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
		/// final filename.
		/// TODO: this method works directly on File IO, we may need another
		/// abstraction (like WorkingTreeIterator). This way we could tell e.g.
		/// Eclipse that Files in the workspace got changed
		/// </remarks>
		/// <param name="repo"></param>
		/// <param name="f">
		/// the file to be modified. The parent directory for this file
		/// has to exist already
		/// </param>
		/// <param name="entry">the entry containing new mode and content</param>
		/// <param name="config_filemode">whether the mode bits should be handled at all.</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public static void CheckoutEntry(Repository repo, FilePath f, DirCacheEntry entry
			, bool config_filemode)
		{
			ObjectLoader ol = repo.Open(entry.GetObjectId());
			if (ol == null)
			{
				throw new MissingObjectException(entry.GetObjectId(), Constants.TYPE_BLOB);
			}
			byte[] bytes = ol.GetCachedBytes();
			FilePath parentDir = f.GetParentFile();
			FilePath tmpFile = FilePath.CreateTempFile("._" + f.GetName(), null, parentDir);
			FileChannel channel = new FileOutputStream(tmpFile).GetChannel();
			ByteBuffer buffer = ByteBuffer.Wrap(bytes);
			try
			{
				int j = channel.Write(buffer);
				if (j != bytes.Length)
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().couldNotWriteFile, tmpFile
						));
				}
			}
			finally
			{
				channel.Close();
			}
			FS fs = repo.FileSystem;
			if (config_filemode && fs.SupportsExecute())
			{
				if (FileMode.EXECUTABLE_FILE.Equals(entry.GetRawMode()))
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
				f.Delete();
				if (!tmpFile.RenameTo(f))
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().couldNotWriteFile, tmpFile
						.GetPath(), f.GetPath()));
				}
			}
			entry.SetLastModified(f.LastModified());
			entry.SetLength((int)ol.GetSize());
		}
	}
}
