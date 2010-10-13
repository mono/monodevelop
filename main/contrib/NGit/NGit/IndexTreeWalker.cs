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

using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>A class for traversing the index and one or two trees.</summary>
	/// <remarks>
	/// A class for traversing the index and one or two trees.
	/// A visitor is invoked for executing actions, like figuring out how to merge.
	/// </remarks>
	[System.ObsoleteAttribute(@"Use  with multiple iterators, such as , , and a native treeNGit.Treewalk.TreeWalk.AddTree(AnyObjectId) ."
		)]
	public class IndexTreeWalker
	{
		private readonly Tree mainTree;

		private readonly Tree newTree;

		private readonly FilePath root;

		private readonly IndexTreeVisitor visitor;

		private bool threeTrees;

		/// <summary>Construct a walker for the index and one tree.</summary>
		/// <remarks>Construct a walker for the index and one tree.</remarks>
		/// <param name="index"></param>
		/// <param name="tree"></param>
		/// <param name="root"></param>
		/// <param name="visitor"></param>
		public IndexTreeWalker(GitIndex index, Tree tree, FilePath root, IndexTreeVisitor
			 visitor)
		{
			//import org.eclipse.jgit.JGitText;
			this.mainTree = tree;
			this.root = root;
			this.visitor = visitor;
			this.newTree = null;
			threeTrees = false;
			indexMembers = index.GetMembers();
		}

		/// <summary>Construct a walker for the index and two trees.</summary>
		/// <remarks>Construct a walker for the index and two trees.</remarks>
		/// <param name="index"></param>
		/// <param name="mainTree"></param>
		/// <param name="newTree"></param>
		/// <param name="root"></param>
		/// <param name="visitor"></param>
		public IndexTreeWalker(GitIndex index, Tree mainTree, Tree newTree, FilePath root
			, IndexTreeVisitor visitor)
		{
			this.mainTree = mainTree;
			this.newTree = newTree;
			this.root = root;
			this.visitor = visitor;
			threeTrees = true;
			indexMembers = index.GetMembers();
		}

		internal GitIndex.Entry[] indexMembers;

		internal int indexCounter = 0;

		/// <summary>Actually walk the index tree</summary>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Walk()
		{
			Walk(mainTree, newTree);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Walk(Tree tree, Tree auxTree)
		{
			TreeIterator mi = new TreeIterator(tree, TreeIterator.Order.POSTORDER);
			TreeIterator ai = new TreeIterator(auxTree, TreeIterator.Order.POSTORDER);
			TreeEntry m = mi.HasNext() ? mi.Next() : null;
			TreeEntry a = ai.HasNext() ? ai.Next() : null;
			int curIndexPos = indexCounter;
			GitIndex.Entry i = indexCounter < indexMembers.Length ? indexMembers[indexCounter
				++] : null;
			while (m != null || a != null || i != null)
			{
				int cmpma = Compare(m, a);
				int cmpmi = Compare(m, i);
				int cmpai = Compare(a, i);
				TreeEntry pm = cmpma <= 0 && cmpmi <= 0 ? m : null;
				TreeEntry pa = cmpma >= 0 && cmpai <= 0 ? a : null;
				GitIndex.Entry pi = cmpmi >= 0 && cmpai >= 0 ? i : null;
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
					m = mi.HasNext() ? mi.Next() : null;
				}
				if (pa != null)
				{
					a = ai.HasNext() ? ai.Next() : null;
				}
				if (pi != null)
				{
					i = indexCounter < indexMembers.Length ? indexMembers[indexCounter++] : null;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void VisitEntry(TreeEntry t1, TreeEntry t2, GitIndex.Entry i)
		{
			// assert t1 != null || t2 != null || i != null :
			// org.eclipse.jgit.JGitText.get().needsAtLeastOneEntry;
			// assert root != null : JGitText.get().needsWorkdir;
			if (t1 != null && t1.GetParent() == null)
			{
				t1 = null;
			}
			if (t2 != null && t2.GetParent() == null)
			{
				t2 = null;
			}
			FilePath f = null;
			if (i != null)
			{
				f = new FilePath(root, i.GetName());
			}
			else
			{
				if (t1 != null)
				{
					f = new FilePath(root, t1.GetFullName());
				}
				else
				{
					if (t2 != null)
					{
						f = new FilePath(root, t2.GetFullName());
					}
				}
			}
			if (t1 != null || t2 != null || i != null)
			{
				if (threeTrees)
				{
					visitor.VisitEntry(t1, t2, i, f);
				}
				else
				{
					visitor.VisitEntry(t1, i, f);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FinishVisitTree(TreeEntry t1, TreeEntry t2, int curIndexPos)
		{
			if (t1 != null && t1.GetParent() == null)
			{
				t1 = null;
			}
			if (t2 != null && t2.GetParent() == null)
			{
				t2 = null;
			}
			FilePath f = null;
			string c = null;
			if (t1 != null)
			{
				c = t1.GetFullName();
				f = new FilePath(root, c);
			}
			else
			{
				if (t2 != null)
				{
					c = t2.GetFullName();
					f = new FilePath(root, c);
				}
			}
			if (t1 is Tree || t2 is Tree)
			{
				if (threeTrees)
				{
					visitor.FinishVisitTree((Tree)t1, (Tree)t2, c);
				}
				else
				{
					visitor.FinishVisitTree((Tree)t1, indexCounter - curIndexPos, c);
				}
			}
			else
			{
				if (t1 != null || t2 != null)
				{
					if (threeTrees)
					{
						visitor.VisitEntry(t1, t2, null, f);
					}
					else
					{
						visitor.VisitEntry(t1, null, f);
					}
				}
			}
		}

		internal static bool Lt(TreeEntry h, GitIndex.Entry i)
		{
			return Compare(h, i) < 0;
		}

		internal static bool Lt(GitIndex.Entry i, TreeEntry t)
		{
			return Compare(t, i) > 0;
		}

		internal static bool Lt(TreeEntry h, TreeEntry m)
		{
			return Compare(h, m) < 0;
		}

		internal static bool Eq(TreeEntry t1, TreeEntry t2)
		{
			return Compare(t1, t2) == 0;
		}

		internal static bool Eq(TreeEntry t1, GitIndex.Entry e)
		{
			return Compare(t1, e) == 0;
		}

		internal static int Compare(TreeEntry t, GitIndex.Entry i)
		{
			if (t == null && i == null)
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
			return Tree.CompareNames(t.GetFullNameUTF8(), i.GetNameUTF8(), TreeEntry.LastChar
				(t), TreeEntry.LastChar(i));
		}

		internal static int Compare(TreeEntry t1, TreeEntry t2)
		{
			if (t1 != null && t1.GetParent() == null && t2 != null && t2.GetParent() == null)
			{
				return 0;
			}
			if (t1 != null && t1.GetParent() == null)
			{
				return -1;
			}
			if (t2 != null && t2.GetParent() == null)
			{
				return 1;
			}
			if (t1 == null && t2 == null)
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
			return Tree.CompareNames(t1.GetFullNameUTF8(), t2.GetFullNameUTF8(), TreeEntry.LastChar
				(t1), TreeEntry.LastChar(t2));
		}
	}
}
