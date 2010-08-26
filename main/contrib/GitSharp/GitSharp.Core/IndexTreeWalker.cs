/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2006, Shawn O. Pearce <spearce@spearce.org>
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

using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace GitSharp.Core
{
    public class IndexTreeWalker
    {
        // Fields
        private readonly IList<GitIndex.Entry> _indexMembers;
        private readonly Tree _mainTree;
        private readonly Tree _newTree;
        private readonly FileSystemInfo _root;
        private readonly bool _threeTrees;
        private readonly IndexTreeVisitor _visitor;

        // Methods
        public IndexTreeWalker(GitIndex index, Tree mainTree, FileSystemInfo root, IndexTreeVisitor visitor)
            : this(index, mainTree, null, root, visitor)
        {
        }

        public IndexTreeWalker(GitIndex index, Tree mainTree, Tree newTree, FileSystemInfo root,
                               IndexTreeVisitor visitor)
        {
            _mainTree = mainTree;
            _newTree = newTree;
            _root = root;
            _visitor = visitor;
            _threeTrees = newTree != null;
            _indexMembers = index.Members;
        }

        public int IndexCounter { get; private set; }

        private static int Compare(TreeEntry t, GitIndex.Entry i)
        {
            if ((t == null) && (i == null))
            {
                return 0;
            }
            if (t == null)
            {
                return 1;
            }
            if (i == null)
            {
                return -1;
            }
            return Tree.CompareNames(t.FullNameUTF8, i.NameUTF8, TreeEntry.LastChar(t), TreeEntry.LastChar(i));
        }

        private static int Compare(TreeEntry t1, TreeEntry t2)
        {
            if ((((t1 != null) && (t1.Parent == null)) && (t2 != null)) && (t2.Parent == null))
            {
                return 0;
            }
            if ((t1 != null) && (t1.Parent == null))
            {
                return -1;
            }
            if ((t2 != null) && (t2.Parent == null))
            {
                return 1;
            }
            if ((t1 == null) && (t2 == null))
            {
                return 0;
            }
            if (t1 == null)
            {
                return 1;
            }
            if (t2 == null)
            {
                return -1;
            }
            return Tree.CompareNames(t1.FullNameUTF8, t2.FullNameUTF8, TreeEntry.LastChar(t1), TreeEntry.LastChar(t2));
        }

        private static bool eq(TreeEntry t1, GitIndex.Entry e)
        {
            return (Compare(t1, e) == 0);
        }

        private static bool eq(TreeEntry t1, TreeEntry t2)
        {
            return (Compare(t1, t2) == 0);
        }

        private void FinishVisitTree(TreeEntry t1, TreeEntry t2, int curIndexPos)
        {
            Debug.Assert((t1 != null) || (t2 != null), "Needs at least one entry");
            Debug.Assert(_root != null, "Needs workdir");

            if ((t1 != null) && (t1.Parent == null))
            {
                t1 = null;
            }
            if ((t2 != null) && (t2.Parent == null))
            {
                t2 = null;
            }

            FileInfo file = null;
            string fileName = null;
            if (t1 != null)
            {
                fileName = t1.FullName;
                file = new FileInfo(Path.Combine(_root.FullName, fileName));
            }
            else if (t2 != null)
            {
                fileName = t2.FullName;
                file = new FileInfo(Path.Combine(_root.FullName, fileName));
            }

			Tree tr1 = (t1 as Tree);
			Tree tr2 = (t2 as Tree);
            if (tr1 != null || tr2 != null)
            {
                if (_threeTrees)
                    _visitor.FinishVisitTree(tr1, tr2, fileName);
                else
                    _visitor.FinishVisitTree(tr1, IndexCounter - curIndexPos, fileName);
            }
            else if (t1 != null || t2 != null)
            {
                if (_threeTrees)
                    _visitor.VisitEntry(t1, t2, null, file);
                else
                    _visitor.VisitEntry(t1, null, file);
            }
        }

        private static bool lt(GitIndex.Entry i, TreeEntry t)
        {
            return (Compare(t, i) > 0);
        }

        private static bool lt(TreeEntry h, GitIndex.Entry i)
        {
            return (Compare(h, i) < 0);
        }

        private static bool lt(TreeEntry h, TreeEntry m)
        {
            return (Compare(h, m) < 0);
        }

        private void VisitEntry(TreeEntry t1, TreeEntry t2, GitIndex.Entry i)
        {
            Debug.Assert(((t1 != null) || (t2 != null)) || (i != null), "Needs at least one entry");
            Debug.Assert(_root != null, "Needs workdir");
            if ((t1 != null) && (t1.Parent == null))
            {
                t1 = null;
            }
            if ((t2 != null) && (t2.Parent == null))
            {
                t2 = null;
            }
            FileInfo file = null;
            if (i != null)
            {
                file = new FileInfo(Path.Combine(_root.FullName, i.Name));
            }
            else if (t1 != null)
            {
                file = new FileInfo(Path.Combine(_root.FullName, t1.FullName));
            }
            else if (t2 != null)
            {
                file = new FileInfo(Path.Combine(_root.FullName, t2.FullName));
            }
            if (((t1 != null) || (t2 != null)) || (i != null))
            {
                if (_threeTrees)
                {
                    _visitor.VisitEntry(t1, t2, i, file);
                }
                else
                {
                    _visitor.VisitEntry(t1, i, file);
                }
            }
        }

        public virtual void Walk()
        {
            Walk(_mainTree, _newTree);
        }

		private void Walk(Tree tree, Tree auxTree)
		{
			var mi = new TreeIterator(tree, TreeIterator.Order.POSTORDER);
			var ai = new TreeIterator(auxTree, TreeIterator.Order.POSTORDER);
			TreeEntry m = mi.hasNext() ? mi.next() : null;
			TreeEntry a = ai.hasNext() ? ai.next() : null;
			int curIndexPos = IndexCounter;
			GitIndex.Entry entry = (IndexCounter < _indexMembers.Count) ? _indexMembers[IndexCounter++] : null;
			while (((m != null) || (a != null)) || (entry != null))
			{
				int cmpma = Compare(m, a);
				int cmpmi = Compare(m, entry);
				int cmpai = Compare(a, entry);
				TreeEntry pm = ((cmpma <= 0) && (cmpmi <= 0)) ? m : null;
				TreeEntry pa = ((cmpma >= 0) && (cmpai <= 0)) ? a : null;
				GitIndex.Entry pi = ((cmpmi >= 0) && (cmpai >= 0)) ? entry : null;

				if (pi != null)
				{
					VisitEntry(pm, pa, pi);
				}
				else
				{
					FinishVisitTree(pm, pa, curIndexPos);
				}

				if (pm != null)
				{
					m = mi.hasNext() ? mi.next() : null;
				}

				if (pa != null)
				{
					a = ai.hasNext() ? ai.next() : null;
				}

				if (pi != null)
				{
                    entry = (IndexCounter < _indexMembers.Count) ? _indexMembers[IndexCounter++] : null;
				}
			}
		}
    }
}