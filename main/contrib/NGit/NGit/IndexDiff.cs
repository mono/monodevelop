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
using NGit;
using NGit.Dircache;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Compares the index, a tree, and the working directory Ignored files are not
	/// taken into account.
	/// </summary>
	/// <remarks>
	/// Compares the index, a tree, and the working directory Ignored files are not
	/// taken into account. The following information is retrieved:
	/// <ul>
	/// <li>added files</li>
	/// <li>changed files</li>
	/// <li>removed files</li>
	/// <li>missing files</li>
	/// <li>modified files</li>
	/// <li>conflicting files</li>
	/// <li>untracked files</li>
	/// <li>files with assume-unchanged flag</li>
	/// </ul>
	/// </remarks>
	public class IndexDiff
	{
		private sealed class ProgressReportingFilter : TreeFilter
		{
			private readonly ProgressMonitor monitor;

			private int count = 0;

			private int stepSize;

			private readonly int total;

			public ProgressReportingFilter(ProgressMonitor monitor, int total)
			{
				this.monitor = monitor;
				this.total = total;
				stepSize = total / 100;
				if (stepSize == 0)
				{
					stepSize = 1000;
				}
			}

			public override bool ShouldBeRecursive()
			{
				return false;
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public override bool Include(TreeWalk walker)
			{
				count++;
				if (count % stepSize == 0)
				{
					if (count <= total)
					{
						monitor.Update(stepSize);
					}
					if (monitor.IsCancelled())
					{
						throw StopWalkException.INSTANCE;
					}
				}
				return true;
			}

			public override TreeFilter Clone()
			{
				throw new InvalidOperationException("Do not clone this kind of filter: " + GetType
					().FullName);
			}
		}

		private const int TREE = 0;

		private const int INDEX = 1;

		private const int WORKDIR = 2;

		private readonly Repository repository;

		private readonly RevTree tree;

		private TreeFilter filter = null;

		private readonly WorkingTreeIterator initialWorkingTreeIterator;

		private ICollection<string> added = new HashSet<string>();

		private ICollection<string> changed = new HashSet<string>();

		private ICollection<string> removed = new HashSet<string>();

		private ICollection<string> missing = new HashSet<string>();

		private ICollection<string> modified = new HashSet<string>();

		private ICollection<string> untracked = new HashSet<string>();

		private ICollection<string> conflicts = new HashSet<string>();

		private ICollection<string> assumeUnchanged;

		private DirCache dirCache;

		/// <summary>Construct an IndexDiff</summary>
		/// <param name="repository"></param>
		/// <param name="revstr">
		/// symbolic name e.g. HEAD
		/// An EmptyTreeIterator is used if <code>revstr</code> cannot be resolved.
		/// </param>
		/// <param name="workingTreeIterator">iterator for working directory</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public IndexDiff(Repository repository, string revstr, WorkingTreeIterator workingTreeIterator
			)
		{
			this.repository = repository;
			ObjectId objectId = repository.Resolve(revstr);
			if (objectId != null)
			{
				tree = new RevWalk(repository).ParseTree(objectId);
			}
			else
			{
				tree = null;
			}
			this.initialWorkingTreeIterator = workingTreeIterator;
		}

		/// <summary>Construct an Indexdiff</summary>
		/// <param name="repository"></param>
		/// <param name="objectId">tree id. If null, an EmptyTreeIterator is used.</param>
		/// <param name="workingTreeIterator">iterator for working directory</param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public IndexDiff(Repository repository, ObjectId objectId, WorkingTreeIterator workingTreeIterator
			)
		{
			this.repository = repository;
			if (objectId != null)
			{
				tree = new RevWalk(repository).ParseTree(objectId);
			}
			else
			{
				tree = null;
			}
			this.initialWorkingTreeIterator = workingTreeIterator;
		}

		/// <summary>Sets a filter.</summary>
		/// <remarks>
		/// Sets a filter. Can be used e.g. for restricting the tree walk to a set of
		/// files.
		/// </remarks>
		/// <param name="filter"></param>
		public virtual void SetFilter(TreeFilter filter)
		{
			this.filter = filter;
		}

		/// <summary>Run the diff operation.</summary>
		/// <remarks>
		/// Run the diff operation. Until this is called, all lists will be empty.
		/// Use
		/// <see cref="Diff(ProgressMonitor, int, int, string)">Diff(ProgressMonitor, int, int, string)
		/// 	</see>
		/// if a progress
		/// monitor is required.
		/// </remarks>
		/// <returns>if anything is different between index, tree, and workdir</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool Diff()
		{
			return Diff(null, 0, 0, string.Empty);
		}

		/// <summary>Run the diff operation.</summary>
		/// <remarks>
		/// Run the diff operation. Until this is called, all lists will be empty.
		/// <p>
		/// The operation may be aborted by the progress monitor. In that event it
		/// will report what was found before the cancel operation was detected.
		/// Callers should ignore the result if monitor.isCancelled() is true. If a
		/// progress monitor is not needed, callers should use
		/// <see cref="Diff()">Diff()</see>
		/// instead. Progress reporting is crude and approximate and only intended
		/// for informing the user.
		/// </remarks>
		/// <param name="monitor">for reporting progress, may be null</param>
		/// <param name="estWorkTreeSize">number or estimated files in the working tree</param>
		/// <param name="estIndexSize">number of estimated entries in the cache</param>
		/// <param name="title"></param>
		/// <returns>if anything is different between index, tree, and workdir</returns>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual bool Diff(ProgressMonitor monitor, int estWorkTreeSize, int estIndexSize
			, string title)
		{
			dirCache = repository.ReadDirCache();
			TreeWalk treeWalk = new TreeWalk(repository);
			treeWalk.Recursive = true;
			// add the trees (tree, dirchache, workdir)
			if (tree != null)
			{
				treeWalk.AddTree(tree);
			}
			else
			{
				treeWalk.AddTree(new EmptyTreeIterator());
			}
			treeWalk.AddTree(new DirCacheIterator(dirCache));
			treeWalk.AddTree(initialWorkingTreeIterator);
			ICollection<TreeFilter> filters = new AList<TreeFilter>(4);
			if (monitor != null)
			{
				// Get the maximum size of the work tree and index
				// and add some (quite arbitrary)
				if (estIndexSize == 0)
				{
					estIndexSize = dirCache.GetEntryCount();
				}
				int total = Math.Max(estIndexSize * 10 / 9, estWorkTreeSize * 10 / 9);
				monitor.BeginTask(title, total);
				filters.AddItem(new IndexDiff.ProgressReportingFilter(monitor, total));
			}
			if (filter != null)
			{
				filters.AddItem(filter);
			}
			filters.AddItem(new SkipWorkTreeFilter(INDEX));
			filters.AddItem(new IndexDiffFilter(INDEX, WORKDIR));
			treeWalk.Filter = AndTreeFilter.Create(filters);
			while (treeWalk.Next())
			{
				AbstractTreeIterator treeIterator = treeWalk.GetTree<AbstractTreeIterator>(TREE);
				DirCacheIterator dirCacheIterator = treeWalk.GetTree<DirCacheIterator>(INDEX);
				WorkingTreeIterator workingTreeIterator = treeWalk.GetTree<WorkingTreeIterator>(WORKDIR
					);
				if (dirCacheIterator != null)
				{
					DirCacheEntry dirCacheEntry = dirCacheIterator.GetDirCacheEntry();
					if (dirCacheEntry != null && dirCacheEntry.Stage > 0)
					{
						conflicts.AddItem(treeWalk.PathString);
						continue;
					}
				}
				if (treeIterator != null)
				{
					if (dirCacheIterator != null)
					{
						if (!treeIterator.IdEqual(dirCacheIterator) || treeIterator.EntryRawMode != dirCacheIterator
							.EntryRawMode)
						{
							// in repo, in index, content diff => changed
							changed.AddItem(treeWalk.PathString);
						}
					}
					else
					{
						// in repo, not in index => removed
						removed.AddItem(treeWalk.PathString);
						if (workingTreeIterator != null)
						{
							untracked.AddItem(treeWalk.PathString);
						}
					}
				}
				else
				{
					if (dirCacheIterator != null)
					{
						// not in repo, in index => added
						added.AddItem(treeWalk.PathString);
					}
					else
					{
						// not in repo, not in index => untracked
						if (workingTreeIterator != null && !workingTreeIterator.IsEntryIgnored())
						{
							untracked.AddItem(treeWalk.PathString);
						}
					}
				}
				if (dirCacheIterator != null)
				{
					if (workingTreeIterator == null)
					{
						// in index, not in workdir => missing
						missing.AddItem(treeWalk.PathString);
					}
					else
					{
						if (workingTreeIterator.IsModified(dirCacheIterator.GetDirCacheEntry(), true))
						{
							// in index, in workdir, content differs => modified
							modified.AddItem(treeWalk.PathString);
						}
					}
				}
			}
			// consume the remaining work
			if (monitor != null)
			{
				monitor.EndTask();
			}
			if (added.IsEmpty() && changed.IsEmpty() && removed.IsEmpty() && missing.IsEmpty(
				) && modified.IsEmpty() && untracked.IsEmpty())
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <returns>list of files added to the index, not in the tree</returns>
		public virtual ICollection<string> GetAdded()
		{
			return added;
		}

		/// <returns>list of files changed from tree to index</returns>
		public virtual ICollection<string> GetChanged()
		{
			return changed;
		}

		/// <returns>list of files removed from index, but in tree</returns>
		public virtual ICollection<string> GetRemoved()
		{
			return removed;
		}

		/// <returns>list of files in index, but not filesystem</returns>
		public virtual ICollection<string> GetMissing()
		{
			return missing;
		}

		/// <returns>list of files on modified on disk relative to the index</returns>
		public virtual ICollection<string> GetModified()
		{
			return modified;
		}

		/// <returns>list of files that are not ignored, and not in the index.</returns>
		public virtual ICollection<string> GetUntracked()
		{
			return untracked;
		}

		/// <returns>list of files that are in conflict</returns>
		public virtual ICollection<string> GetConflicting()
		{
			return conflicts;
		}

		/// <returns>list of files with the flag assume-unchanged</returns>
		public virtual ICollection<string> GetAssumeUnchanged()
		{
			if (assumeUnchanged == null)
			{
				HashSet<string> unchanged = new HashSet<string>();
				for (int i = 0; i < dirCache.GetEntryCount(); i++)
				{
					if (dirCache.GetEntry(i).IsAssumeValid)
					{
						unchanged.AddItem(dirCache.GetEntry(i).PathString);
					}
				}
				assumeUnchanged = unchanged;
			}
			return assumeUnchanged;
		}
	}
}
