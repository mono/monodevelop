/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitSharp.Core
{
    [Complete]
    public class IndexDiff
    {
        private readonly GitIndex _index;
        private readonly Tree _tree;
        private bool _anyChanges;

        /// <summary>
        /// Construct an indexdiff for diffing the workdir against the index.
        /// </summary>
        /// <param name="repository"></param>
        public IndexDiff(Repository repository)
            : this(repository.MapTree("HEAD"), repository.Index)
        {
        }

        /// <summary>
        /// Construct an indexdiff for diffing the workdir against both the index and a tree.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="index"></param>
        public IndexDiff(Tree tree, GitIndex index)
        {
            _anyChanges = false;
            _tree = tree;
            _index = index;

            Added = new HashSet<string>();
            Changed = new HashSet<string>();
            Removed = new HashSet<string>();
            Missing = new HashSet<string>();
            Modified = new HashSet<string>();
            Untracked = new HashSet<string>();
            MergeConflict = new HashSet<string>();
        }

        /// <summary>
        /// Run the diff operation. Until this is called, all lists will be empty
        /// </summary>
        /// <returns>true if anything is different between index, tree, and workdir</returns>
        public bool Diff()
        {
            DirectoryInfo root = _index.Repository.WorkingDirectory;
            var visitor = new AbstractIndexTreeVisitor
                            {
                                VisitEntry = delegate(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file)
                                                {
                                                    if (treeEntry == null)
                                                    {
                                                        Added.Add(indexEntry.Name);
                                                        _anyChanges = true;
                                                    }
                                                    else if (indexEntry == null)
                                                    {
                                                        if (!(treeEntry is Tree))
                                                        {
                                                            Removed.Add(treeEntry.FullName);
                                                        }
                                                        _anyChanges = true;
                                                    }
                                                    else
                                                    {
                                                        if (!treeEntry.Id.Equals(indexEntry.ObjectId))
                                                        {
                                                            Changed.Add(indexEntry.Name);
                                                            _anyChanges = true;
                                                        }
                                                    }

                                                    if (indexEntry != null)
                                                    {
                                                        if (!file.Exists)
                                                        {
                                                            Missing.Add(indexEntry.Name);
                                                            _anyChanges = true;
                                                        }
                                                        else
                                                        {
                                                            if (indexEntry.IsModified(root, true))
                                                            {
                                                                Modified.Add(indexEntry.Name);
                                                                _anyChanges = true;
                                                            }
                                                        }
                                                    }

                                                    if (indexEntry != null)
                                                    {
                                                        if (indexEntry.Stage != 0)
                                                        {
                                                            MergeConflict.Add(indexEntry.Name);
                                                            _anyChanges = true;
                                                        }
                                                    }
                                                }
                            };
            new IndexTreeWalker(_index, _tree, root, visitor).Walk();

            CheckUntrackedDirectory(root.FullName, string.Empty);

            return _anyChanges;
        }


        private void CheckUntrackedDirectory(string path, string relative_path)
        {
            var files = Directory.GetFiles(path);
            foreach (string file in files)
                CheckUntrackedFile(new FileInfo(file), relative_path);

            var dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                var dirname = new DirectoryInfo(dir).Name;
                if (dirname.StartsWith(Constants.DOT_GIT_EXT))
                    continue;

                CheckUntrackedDirectory(dir, (relative_path.Length == 0 ? dirname : relative_path + "/" + dirname));
            }
        }

        private void CheckUntrackedFile(FileInfo f, string relative_path)
        {
            var relative_name =  (relative_path.Length == 0 ? f.Name : relative_path + "/" + f.Name);
            if (!_index.Members.Any(e => e.Name == relative_name))
            {
                Untracked.Add(relative_name);
            }
        }

        /// <summary>
        /// List of files added to the index, not in the tree
        /// </summary>
        public HashSet<string> Added { get; private set; }

        /// <summary>
        /// List of files changed from tree to index
        /// </summary>
        public HashSet<string> Changed { get; private set; }

        /// <summary>
        /// List of files removed from index, but in tree
        /// </summary>
        public HashSet<string> Removed { get; private set; }

        /// <summary>
        /// List of files in index, but not filesystem
        /// </summary>
        public HashSet<string> Missing { get; private set; }

        /// <summary>
        /// List of files modified on disk relative to the index
        /// </summary>
        public HashSet<string> Modified { get; private set; }


        public HashSet<string> Untracked { get; private set; }

        /// <summary>
        /// List of files in index and have a merge conflict
        /// </summary>
        public HashSet<string> MergeConflict { get; private set; }

        /// <summary>
        /// Returns the number of files checked into the git repository
        /// </summary>
        public int IndexSize { get { return _index.Members.Count; } }
    }
}