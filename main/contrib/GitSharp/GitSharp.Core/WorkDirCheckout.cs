/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Roger C. Soares <rogersoares@intelinet.com.br>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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
using System.Collections.Generic;
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// This class handles checking out one or two trees merging
    /// with the index (actually a tree too).
    /// <para />
    /// Three-way merges are no performed. See <seealso cref="FailOnConflict"/>.
    /// </summary>
    public class WorkDirCheckout
    {
        private readonly Dictionary<string, ObjectId> _updated;
        private readonly Tree _head;
        private readonly GitIndex _index;
        private readonly Tree _merge;
        private readonly DirectoryInfo _root;
        private Repository _repo;

        internal WorkDirCheckout(Repository repo, DirectoryInfo workDir, GitIndex oldIndex, GitIndex newIndex)
            : this()
        {
            _repo = repo;
            _root = workDir;
            _index = oldIndex;
            _merge = repo.MapTree(newIndex.writeTree());
        }

        ///	<summary>
        /// Create a checkout class for checking out one tree, merging with the index
        ///	</summary>
        ///	<param name="repo"> </param>
        ///	<param name="root"> workdir </param>
        ///	<param name="index"> current index </param>
        ///	<param name="merge"> tree to check out </param>
        public WorkDirCheckout(Repository repo, DirectoryInfo root, GitIndex index, Tree merge)
            : this()
        {
            this._repo = repo;
            this._root = root;
            this._index = index;
            this._merge = merge;
        }

        ///	<summary>
        /// Create a checkout class for merging and checking our two trees and the index.
        ///	</summary>
        ///	<param name="repo"> </param>
        ///	<param name="root"> workdir </param>
        ///	<param name="head"> </param>
        ///	<param name="index"> </param>
        ///	<param name="merge"> </param>
        public WorkDirCheckout(Repository repo, DirectoryInfo root, Core.Tree head, GitIndex index, Core.Tree merge)
            : this(repo, root, index, merge)
        {
            this._head = head;
        }

        private WorkDirCheckout()
        {
            Conflicts = new List<string>();
            Removed = new List<string>();
            _updated = new Dictionary<string, ObjectId>();
            FailOnConflict = true;
        }

        ///	<summary>
        /// If <code>true</code>, will scan first to see if it's possible to check out, 
        /// otherwise throw <seealso cref="CheckoutConflictException"/>. If <code>false</code>,
        /// it will silently deal with the problem.
        /// </summary>
        public bool FailOnConflict { get; set; }

        /// <summary>
        /// The list of conflicts created by this checkout
        /// </summary>
        /// <returns></returns>
        public List<string> Conflicts { get; private set; }

        /// <summary>
        /// The list of all files removed by this checkout
        /// </summary>
        /// <returns></returns>
        public List<string> Removed { get; private set; }

        public Dictionary<string, ObjectId> Updated
        {
            get { return _updated; }
        }

        ///	<summary>
        /// Execute this checkout
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void checkout()
        {
            if (_head == null)
            {
                PrescanOneTree();
            }
            else
            {
                PrescanTwoTrees();
            }

            if (Conflicts.Count != 0)
            {
                if (FailOnConflict)
                {
                    string[] entries = Conflicts.ToArray();
                    throw new CheckoutConflictException(entries);
                }
            }

            CleanUpConflicts();
            if (_head == null)
            {
                CheckoutOutIndexNoHead();
            }
            else
            {
                CheckoutTwoTrees();
            }
        }

        private void CheckoutTwoTrees()
        {
            foreach (string path in Removed)
            {
                _index.remove(_root, new FileInfo(Path.Combine(_root.FullName, path)));
            }

            foreach (KeyValuePair<string, ObjectId> entry in _updated)
            {
                GitIndex.Entry newEntry = _index.addEntry(_merge.FindBlobMember(entry.Key));
                _index.checkoutEntry(_root, newEntry);
            }
        }

        private void CheckoutOutIndexNoHead()
        {
            var visitor = new AbstractIndexTreeVisitor
                              {
                                  VisitEntry = (m, i, f) =>
                                                   {
                                                       if (m == null)
                                                       {
                                                           _index.remove(_root, f);
                                                           return;
                                                       }

                                                       bool needsCheckout = false;
                                                       if (i == null)
                                                       {
                                                           needsCheckout = true;
                                                       }
                                                       else if (i.ObjectId.Equals(m.Id))
                                                       {
                                                           if (i.IsModified(_root, true))
                                                           {
                                                               needsCheckout = true;
                                                           }
                                                       }
                                                       else
                                                       {
                                                           needsCheckout = true;
                                                       }

                                                       if (needsCheckout)
                                                       {
                                                           GitIndex.Entry newEntry = _index.addEntry(m);
                                                           _index.checkoutEntry(_root, newEntry);
                                                       }
                                                   }
                              };

            new IndexTreeWalker(_index, _merge, _root, visitor).Walk();
        }

        private void CleanUpConflicts()
        {
            foreach (string conflictFile in Conflicts)
            {
                var conflict = new FileInfo(Path.Combine(_root.DirectoryName(), conflictFile));

                try
                {
                    conflict.Delete();
                }
                catch (IOException)
                {
                    throw new CheckoutConflictException("Cannot delete file: " + conflict);
                }

                RemoveEmptyParents(conflict);
            }

            foreach (string removedFile in Removed)
            {
                var file = new FileInfo(Path.Combine(_root.DirectoryName(), removedFile));
                file.Delete();
                RemoveEmptyParents(file);
            }
        }

        private void RemoveEmptyParents(FileSystemInfo f)
        {
            FileSystemInfo parentFile = Directory.GetParent(f.FullName);
            if (parentFile == null) return;

            while (parentFile.FullName != _root.FullName)
            {
                if (parentFile.IsDirectory() && Directory.GetFiles(parentFile.FullName).Length == 0)
                {
                    parentFile.Delete();
                }
                else
                {
                    break;
                }

                parentFile = Directory.GetParent(parentFile.FullName);
                if (parentFile == null) return;
            }
        }

        internal void PrescanOneTree()
        {
            var visitor = new AbstractIndexTreeVisitor
                              {
                                  VisitEntry = (m, i, f) =>
                                                   {
                                                       if (m != null)
                                                       {
                                                           if (!f.IsFile())
                                                           {
                                                               CheckConflictsWithFile(f);
                                                           }
                                                       }
                                                       else
                                                       {
                                                           if (f.Exists)
                                                           {
                                                               Removed.Add(i.Name);
                                                               Conflicts.Remove(i.Name);
                                                           }
                                                       }
                                                   }
                              };

            new IndexTreeWalker(_index, _merge, _root, visitor).Walk();

            Conflicts.RemoveAll(conflict => Removed.Contains(conflict));
        }


         private List<string> ListFiles(FileSystemInfo file)
        {
            var list = new List<string>();
            ListFiles(file, list);
            return list;
        }

         private void ListFiles(FileSystemInfo dir, ICollection<string> list)
         {
             foreach (FileInfo f in dir.ListFiles())
             {
                 if (f.IsDirectory())
                     ListFiles(f, list);
                 else
                 {
                     list.Add(Repository.StripWorkDir(_root, new FileInfo(f.FullName)));
                 }
             }
         }

        internal void PrescanTwoTrees()
        {
            var visitor = new AbstractIndexTreeVisitor
                              {
                                  VisitEntryAux = (treeEntry, auxEntry, indexEntry, file) =>
                                                   {
                                                       if (treeEntry is Tree || auxEntry is Tree)
                                                       {
                                                           throw new ArgumentException("Can't pass me a tree!");
                                                       }

                                                       ProcessEntry(treeEntry, auxEntry, indexEntry);
                                                   },

                                  FinishVisitTree = (tree, auxTree, currentDirectory) =>
                                                        {
                                                            if (currentDirectory.Length == 0) return;
                                                            if (auxTree == null) return;

                                                            if (_index.GetEntry(currentDirectory) != null)
                                                            {
                                                                Removed.Add(currentDirectory);
                                                            }
                                                        }
                              };

            new IndexTreeWalker(_index, _head, _merge, _root, visitor).Walk();

            // if there's a conflict, don't list it under
            // to-be-removed, since that messed up our next
            // section
            Removed.RemoveAll(removed => Conflicts.Contains(removed));

            foreach (string path in _updated.Keys)
            {
                if (_index.GetEntry(path) == null)
                {
                    FileSystemInfo file = new FileInfo(Path.Combine(_root.DirectoryName(), path));
                    if (file.IsFile())
                    {
                        Conflicts.Add(path);
                    }
                    else if (file.IsDirectory())
                    {
                        CheckConflictsWithFile(file);
                    }
                }
            }

            Conflicts.RemoveAll(conflict => Removed.Contains(conflict));
        }

        private void ProcessEntry(TreeEntry h, TreeEntry m, GitIndex.Entry i)
        {
            ObjectId iId = (i == null ? null : i.ObjectId);
            ObjectId mId = (m == null ? null : m.Id);
            ObjectId hId = (h == null ? null : h.Id);

            string name = (i != null ? i.Name : (h != null ? h.FullName : m.FullName));

            if (i == null)
            {
                //                    
                //				    I (index)                H        M        Result
                //			        -------------------------------------------------------
                //			        0 nothing             nothing  nothing  (does not happen)
                //			        1 nothing             nothing  exists   use M
                //			        2 nothing             exists   nothing  remove path from index
                //			        3 nothing             exists   exists   use M 

                if (h == null)
                {
                    _updated.Add(name, mId);
                }
                else if (m == null)
                {
                    Removed.Add(name);
                }
                else
                {
                    _updated.Add(name, mId);
                }
            }
            else if (h == null)
            {
                //                    
                //					  clean I==H  I==M       H        M        Result
                //			         -----------------------------------------------------
                //			        4 yes   N/A   N/A     nothing  nothing  keep index
                //			        5 no    N/A   N/A     nothing  nothing  keep index
                //			
                //			        6 yes   N/A   yes     nothing  exists   keep index
                //			        7 no    N/A   yes     nothing  exists   keep index
                //			        8 yes   N/A   no      nothing  exists   fail
                //			        9 no    N/A   no      nothing  exists   fail       

                if (m == null || mId.Equals(iId))
                {
                    if (HasParentBlob(_merge, name))
                    {
                        if (i.IsModified(_root, true))
                        {
                            Conflicts.Add(name);
                        }
                        else
                        {
                            Removed.Add(name);
                        }
                    }
                }
                else
                {
                    Conflicts.Add(name);
                }
            }
            else if (m == null)
            {
                //                    
                //					10 yes   yes   N/A     exists   nothing  remove path from index
                //			        11 no    yes   N/A     exists   nothing  fail
                //			        12 yes   no    N/A     exists   nothing  fail
                //			        13 no    no    N/A     exists   nothing  fail
                //					 

                if (hId.Equals(iId))
                {
                    if (i.IsModified(_root, true))
                    {
                        Conflicts.Add(name);
                    }
                    else
                    {
                        Removed.Add(name);
                    }
                }
                else
                {
                    Conflicts.Add(name);
                }
            }
            else
            {
                if (!hId.Equals(mId) && !hId.Equals(iId) && !mId.Equals(iId))
                {
                    Conflicts.Add(name);
                }
                else if (hId.Equals(iId) && !mId.Equals(iId))
                {
                    if (i.IsModified(_root, true))
                    {
                        Conflicts.Add(name);
                    }
                    else
                    {
                        _updated.Add(name, mId);
                    }
                }
            }
        }

        private static bool HasParentBlob(Tree t, string name)
        {
            if (name.IndexOf('/') == -1)
            {
                return false;
            }

            string parent = name.Slice(0, name.LastIndexOf('/'));
            return t.FindBlobMember(parent) != null || HasParentBlob(t, parent);
        }

        private void CheckConflictsWithFile(FileSystemInfo file)
        {
            if (file.IsDirectory())
            {
                List<string> childFiles = ListFiles(file);
                Conflicts.AddRange(childFiles);
            }
            else
            {
                FileSystemInfo parent = Directory.GetParent(file.FullName);
                if (parent == null) return;

                while (!parent.Equals(_root))
                {
                    if (parent.IsDirectory())
                    {
                        break;
                    }

                    if (parent.IsFile())
                    {
                        Conflicts.Add(Repository.StripWorkDir(_root, parent));
                        break;
                    }

                    parent = Directory.GetParent(parent.FullName);
                    if (parent == null) return;
                }
            }
        }
    }
}