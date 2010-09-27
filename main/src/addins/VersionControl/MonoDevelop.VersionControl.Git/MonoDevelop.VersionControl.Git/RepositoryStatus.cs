/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NGit;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Dircache;
using NGit.Treewalk.Filter;

namespace MonoDevelop.VersionControl.Git
{
	public class RepositoryStatus
	{
		private string _root_path;
		private bool _recursive;
		private string _file_path;
		private bool changesExist;

		internal RepositoryStatus(NGit.Repository repository, string singleFile, string rootDir, bool recursive)
		{
			Repository = repository;
			_root_path = rootDir;
			_recursive = recursive;
			_file_path = singleFile;
			Update();
		}

		public NGit.Repository Repository
		{
			get;
			private set;
		}
		
		public bool ChangesExist {
			get { return changesExist; }
		}
		
		/// <summary>
		/// List of files added to the index, which are not in the current commit
		/// </summary>
		public HashSet<string> Added { get; private set; }

		/// <summary>
		/// List of files added to the index, which are already in the current commit with different content
		/// </summary>
		public HashSet<string> Staged { get; private set; }

		/// <summary>
		/// List of files removed from the index but are existent in the current commit
		/// </summary>
		public HashSet<string> Removed { get; private set; }

		/// <summary>
		/// List of files existent in the index but are missing in the working directory
		/// </summary>
		public HashSet<string> Missing { get; private set; }

		/// <summary>
		/// List of files with unstaged modifications. A file may be modified and staged at the same time if it has been modified after adding.
		/// </summary>
		public HashSet<string> Modified { get; private set; }

		/// <summary>
		/// List of files existing in the working directory but are neither tracked in the index nor in the current commit.
		/// </summary>
		public HashSet<string> Untracked { get; private set; }

		/// <summary>
		/// List of files with staged modifications that conflict.
		/// </summary>
		public HashSet<string> MergeConflict { get; private set; }

		///// <summary>
		///// Returns the number of files checked into the git repository
		///// </summary>
		//public int IndexSize { get { return _index.Members.Count; } }

		public bool AnyDifferences { get; private set; }

		/// <summary>
		/// Recalculates the status
		/// </summary>
		public void Update()
		{
			AnyDifferences = false;
			Added = new HashSet<string>();
			Staged = new HashSet<string>();
			Removed = new HashSet<string>();
			Missing = new HashSet<string>();
			Modified = new HashSet<string>();
			Untracked = new HashSet<string>();
			MergeConflict = new HashSet<string>();
			
			if (_file_path != null)
				UpdateDirectory (_file_path, false);
			else if (_recursive)
				UpdateDirectory (_root_path, true);
			else
				UpdateDirectory (_root_path, false);
		}

		/// <summary>
		/// Run the diff operation. Until this is called, all lists will be empty
		/// </summary>
		/// <returns>true if anything is different between index, tree, and workdir</returns>
		private void UpdateDirectory (string path, bool recursive)
		{
			RevWalk rw = new RevWalk (Repository);
			var commit = rw.ParseCommit (Repository.Resolve (Constants.HEAD));
			
			TreeWalk treeWalk = new TreeWalk (Repository);
			treeWalk.Reset ();
			treeWalk.Recursive = true;

			if (commit != null)
				treeWalk.AddTree (commit.Tree);
			else
				treeWalk.AddTree (new EmptyTreeIterator());
			
			DirCache dc = Repository.ReadDirCache ();
			treeWalk.AddTree (new DirCacheIterator (dc));
			
			FileTreeIterator workTree = new FileTreeIterator (Repository.WorkTree, Repository.FileSystem, WorkingTreeOptions.CreateConfigurationInstance(Repository.GetConfig()));
			treeWalk.AddTree (workTree);

			List<TreeFilter> filters = new List<TreeFilter> ();
			filters.Add (new NotIgnoredFilter(2));
			filters.Add (new SkipWorkTreeFilter(1));
			if (path != ".")
				filters.Add (PathFilter.Create (path));
			filters.Add (TreeFilter.ANY_DIFF);
			treeWalk.Filter = AndTreeFilter.Create(filters);
			
			while (treeWalk.Next())
			{
				AbstractTreeIterator treeIterator = treeWalk.GetTree<AbstractTreeIterator>(0);
				DirCacheIterator dirCacheIterator = treeWalk.GetTree<DirCacheIterator>(1);
				WorkingTreeIterator workingTreeIterator = treeWalk.GetTree<WorkingTreeIterator>(2);
				NGit.FileMode fileModeTree = treeWalk.GetFileMode(0);
				if (treeIterator != null)
				{
					if (dirCacheIterator != null)
					{
						if (!treeIterator.GetEntryObjectId().Equals(dirCacheIterator.GetEntryObjectId()))
						{
							// in repo, in index, content diff => changed
							Modified.Add(dirCacheIterator.GetEntryPathString());
							changesExist = true;
						}
					}
					else
					{
						// in repo, not in index => removed
						if (!fileModeTree.Equals(NGit.FileMode.TYPE_TREE))
						{
							Removed.Add(treeIterator.GetEntryPathString());
							changesExist = true;
						}
					}
				}
				else
				{
					if (dirCacheIterator != null)
					{
						// not in repo, in index => added
						Added.Add(dirCacheIterator.GetEntryPathString());
						changesExist = true;
					}
					else
					{
						// not in repo, not in index => untracked
						if (workingTreeIterator != null && !workingTreeIterator.IsEntryIgnored())
						{
							Untracked.Add(workingTreeIterator.GetEntryPathString());
							changesExist = true;
						}
					}
				}
				if (dirCacheIterator != null)
				{
					if (workingTreeIterator == null)
					{
						// in index, not in workdir => missing
						Missing.Add(dirCacheIterator.GetEntryPathString());
						changesExist = true;
					}
					else
					{
						if (!dirCacheIterator.IdEqual(workingTreeIterator))
						{
							// in index, in workdir, content differs => modified
							Modified.Add(dirCacheIterator.GetEntryPathString());
							changesExist = true;
						}
					}
				}
			}
		}
	}
}
