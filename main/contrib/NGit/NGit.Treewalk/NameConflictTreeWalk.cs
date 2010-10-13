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
using NGit.Treewalk;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>Specialized TreeWalk to detect directory-file (D/F) name conflicts.</summary>
	/// <remarks>
	/// Specialized TreeWalk to detect directory-file (D/F) name conflicts.
	/// <p>
	/// Due to the way a Git tree is organized the standard
	/// <see cref="TreeWalk">TreeWalk</see>
	/// won't
	/// easily find a D/F conflict when merging two or more trees together. In the
	/// standard TreeWalk the file will be returned first, and then much later the
	/// directory will be returned. This makes it impossible for the application to
	/// efficiently detect and handle the conflict.
	/// <p>
	/// Using this walk implementation causes the directory to report earlier than
	/// usual, at the same time as the non-directory entry. This permits the
	/// application to handle the D/F conflict in a single step. The directory is
	/// returned only once, so it does not get returned later in the iteration.
	/// <p>
	/// When a D/F conflict is detected
	/// <see cref="TreeWalk.IsSubtree()">TreeWalk.IsSubtree()</see>
	/// will return true
	/// and
	/// <see cref="TreeWalk.EnterSubtree()">TreeWalk.EnterSubtree()</see>
	/// will recurse into the subtree, no matter
	/// which iterator originally supplied the subtree.
	/// <p>
	/// Because conflicted directories report early, using this walk implementation
	/// to populate a
	/// <see cref="NGit.Dircache.DirCacheBuilder">NGit.Dircache.DirCacheBuilder</see>
	/// may cause the automatic resorting to
	/// run and fix the entry ordering.
	/// <p>
	/// This walk implementation requires more CPU to implement a look-ahead and a
	/// look-behind to merge a D/F pair together, or to skip a previously reported
	/// directory. In typical Git repositories the look-ahead cost is 0 and the
	/// look-behind doesn't trigger, as users tend not to create trees which contain
	/// both "foo" as a directory and "foo.c" as a file.
	/// <p>
	/// In the worst-case however several thousand look-ahead steps per walk step may
	/// be necessary, making the overhead quite significant. Since this worst-case
	/// should never happen this walk implementation has made the time/space tradeoff
	/// in favor of more-time/less-space, as that better suits the typical case.
	/// </remarks>
	public class NameConflictTreeWalk : TreeWalk
	{
		private static readonly int TREE_MODE = FileMode.TREE.GetBits();

		private bool fastMinHasMatch;

		private AbstractTreeIterator dfConflict;

		/// <summary>Create a new tree walker for a given repository.</summary>
		/// <remarks>Create a new tree walker for a given repository.</remarks>
		/// <param name="repo">the repository the walker will obtain data from.</param>
		public NameConflictTreeWalk(Repository repo) : this(repo.NewObjectReader())
		{
		}

		/// <summary>Create a new tree walker for a given repository.</summary>
		/// <remarks>Create a new tree walker for a given repository.</remarks>
		/// <param name="or">the reader the walker will obtain tree data from.</param>
		public NameConflictTreeWalk(ObjectReader or) : base(or)
		{
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal override AbstractTreeIterator Min()
		{
			for (; ; )
			{
				AbstractTreeIterator minRef = FastMin();
				if (fastMinHasMatch)
				{
					return minRef;
				}
				if (IsTree(minRef))
				{
					if (SkipEntry(minRef))
					{
						foreach (AbstractTreeIterator t in trees)
						{
							if (t.matches == minRef)
							{
								t.Next(1);
								t.matches = null;
							}
						}
						continue;
					}
					return minRef;
				}
				return CombineDF(minRef);
			}
		}

		private AbstractTreeIterator FastMin()
		{
			fastMinHasMatch = true;
			int i = 0;
			AbstractTreeIterator minRef = trees[i];
			while (minRef.Eof() && ++i < trees.Length)
			{
				minRef = trees[i];
			}
			if (minRef.Eof())
			{
				return minRef;
			}
			bool hasConflict = false;
			minRef.matches = minRef;
			while (++i < trees.Length)
			{
				AbstractTreeIterator t = trees[i];
				if (t.Eof())
				{
					continue;
				}
				int cmp = t.PathCompare(minRef);
				if (cmp < 0)
				{
					if (fastMinHasMatch && IsTree(minRef) && !IsTree(t) && NameEqual(minRef, t))
					{
						// We used to be at a tree, but now we are at a file
						// with the same name. Allow the file to match the
						// tree anyway.
						//
						t.matches = minRef;
						hasConflict = true;
					}
					else
					{
						fastMinHasMatch = false;
						t.matches = t;
						minRef = t;
					}
				}
				else
				{
					if (cmp == 0)
					{
						// Exact name/mode match is best.
						//
						t.matches = minRef;
					}
					else
					{
						if (fastMinHasMatch && IsTree(t) && !IsTree(minRef) && NameEqual(t, minRef))
						{
							// The minimum is a file (non-tree) but the next entry
							// of this iterator is a tree whose name matches our file.
							// This is a classic D/F conflict and commonly occurs like
							// this, with no gaps in between the file and directory.
							//
							// Use the tree as the minimum instead (see combineDF).
							//
							for (int k = 0; k < i; k++)
							{
								AbstractTreeIterator p = trees[k];
								if (p.matches == minRef)
								{
									p.matches = t;
								}
							}
							t.matches = t;
							minRef = t;
							hasConflict = true;
						}
						else
						{
							fastMinHasMatch = false;
						}
					}
				}
			}
			if (hasConflict && fastMinHasMatch && dfConflict == null)
			{
				dfConflict = minRef;
			}
			return minRef;
		}

		private static bool NameEqual(AbstractTreeIterator a, AbstractTreeIterator b)
		{
			return a.PathCompare(b, TREE_MODE) == 0;
		}

		private static bool IsTree(AbstractTreeIterator p)
		{
			return FileMode.TREE.Equals(p.mode);
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		private bool SkipEntry(AbstractTreeIterator minRef)
		{
			// A tree D/F may have been handled earlier. We need to
			// not report this path if it has already been reported.
			//
			foreach (AbstractTreeIterator t in trees)
			{
				if (t.matches == minRef || t.First())
				{
					continue;
				}
				int stepsBack = 0;
				for (; ; )
				{
					stepsBack++;
					t.Back(1);
					int cmp = t.PathCompare(minRef, 0);
					if (cmp == 0)
					{
						// We have already seen this "$path" before. Skip it.
						//
						t.Next(stepsBack);
						return true;
					}
					else
					{
						if (cmp < 0 || t.First())
						{
							// We cannot find "$path" in t; it will never appear.
							//
							t.Next(stepsBack);
							break;
						}
					}
				}
			}
			// We have never seen the current path before.
			//
			return false;
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		private AbstractTreeIterator CombineDF(AbstractTreeIterator minRef)
		{
			// Look for a possible D/F conflict forward in the tree(s)
			// as there may be a "$path/" which matches "$path". Make
			// such entries match this entry.
			//
			AbstractTreeIterator treeMatch = null;
			foreach (AbstractTreeIterator t in trees)
			{
				if (t.matches == minRef || t.Eof())
				{
					continue;
				}
				for (; ; )
				{
					int cmp = t.PathCompare(minRef, TREE_MODE);
					if (cmp < 0)
					{
						// The "$path/" may still appear later.
						//
						t.matchShift++;
						t.Next(1);
						if (t.Eof())
						{
							t.Back(t.matchShift);
							t.matchShift = 0;
							break;
						}
					}
					else
					{
						if (cmp == 0)
						{
							// We have a conflict match here.
							//
							t.matches = minRef;
							treeMatch = t;
							break;
						}
						else
						{
							// A conflict match is not possible.
							//
							if (t.matchShift != 0)
							{
								t.Back(t.matchShift);
								t.matchShift = 0;
							}
							break;
						}
					}
				}
			}
			if (treeMatch != null)
			{
				// If we do have a conflict use one of the directory
				// matching iterators instead of the file iterator.
				// This way isSubtree is true and isRecursive works.
				//
				foreach (AbstractTreeIterator t_1 in trees)
				{
					if (t_1.matches == minRef)
					{
						t_1.matches = treeMatch;
					}
				}
				if (dfConflict == null)
				{
					dfConflict = treeMatch;
				}
				return treeMatch;
			}
			return minRef;
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal override void PopEntriesEqual()
		{
			AbstractTreeIterator ch = currentHead;
			for (int i = 0; i < trees.Length; i++)
			{
				AbstractTreeIterator t = trees[i];
				if (t.matches == ch)
				{
					if (t.matchShift == 0)
					{
						t.Next(1);
					}
					else
					{
						t.Back(t.matchShift);
						t.matchShift = 0;
					}
					t.matches = null;
				}
			}
			if (ch == dfConflict)
			{
				dfConflict = null;
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal override void SkipEntriesEqual()
		{
			AbstractTreeIterator ch = currentHead;
			for (int i = 0; i < trees.Length; i++)
			{
				AbstractTreeIterator t = trees[i];
				if (t.matches == ch)
				{
					if (t.matchShift == 0)
					{
						t.Skip();
					}
					else
					{
						t.Back(t.matchShift);
						t.matchShift = 0;
					}
					t.matches = null;
				}
			}
			if (ch == dfConflict)
			{
				dfConflict = null;
			}
		}

		/// <summary>True if the current entry is covered by a directory/file conflict.</summary>
		/// <remarks>
		/// True if the current entry is covered by a directory/file conflict.
		/// This means that for some prefix of the current entry's path, this walk
		/// has detected a directory/file conflict. Also true if the current entry
		/// itself is a directory/file conflict.
		/// Example: If this TreeWalk points to foo/bar/a.txt and this method returns
		/// true then you know that either for path foo or for path foo/bar files and
		/// folders were detected.
		/// </remarks>
		/// <returns>
		/// <code>true</code> if the current entry is covered by a
		/// directory/file conflict, <code>false</code> otherwise
		/// </returns>
		public virtual bool IsDirectoryFileConflict()
		{
			return dfConflict != null;
		}
	}
}
