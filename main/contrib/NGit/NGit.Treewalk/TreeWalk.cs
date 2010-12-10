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
using NGit.Errors;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Treewalk
{
	/// <summary>
	/// Walks one or more
	/// <see cref="AbstractTreeIterator">AbstractTreeIterator</see>
	/// s in parallel.
	/// <p>
	/// This class can perform n-way differences across as many trees as necessary.
	/// <p>
	/// Each tree added must have the same root as existing trees in the walk.
	/// <p>
	/// A TreeWalk instance can only be used once to generate results. Running a
	/// second time requires creating a new TreeWalk instance, or invoking
	/// <see cref="Reset()">Reset()</see>
	/// and adding new trees before starting again. Resetting an
	/// existing instance may be faster for some applications as some internal
	/// buffers may be recycled.
	/// <p>
	/// TreeWalk instances are not thread-safe. Applications must either restrict
	/// usage of a TreeWalk instance to a single thread, or implement their own
	/// synchronization at a higher level.
	/// <p>
	/// Multiple simultaneous TreeWalk instances per
	/// <see cref="NGit.Repository">NGit.Repository</see>
	/// are
	/// permitted, even from concurrent threads.
	/// </summary>
	public class TreeWalk
	{
		private static readonly AbstractTreeIterator[] NO_TREES = new AbstractTreeIterator
			[] {  };

		/// <summary>Open a tree walk and filter to exactly one path.</summary>
		/// <remarks>
		/// Open a tree walk and filter to exactly one path.
		/// <p>
		/// The returned tree walk is already positioned on the requested path, so
		/// the caller should not need to invoke
		/// <see cref="Next()">Next()</see>
		/// unless they are
		/// looking for a possible directory/file name conflict.
		/// </remarks>
		/// <param name="reader">the reader the walker will obtain tree data from.</param>
		/// <param name="path">single path to advance the tree walk instance into.</param>
		/// <param name="trees">one or more trees to walk through, all with the same root.</param>
		/// <returns>
		/// a new tree walk configured for exactly this one path; null if no
		/// path was found in any of the trees.
		/// </returns>
		/// <exception cref="System.IO.IOException">reading a pack file or loose object failed.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// an tree object could not be read as its data stream did not
		/// appear to be a tree, or could not be inflated.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">an object we expected to be a tree was not a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a tree object was not found.
		/// 	</exception>
		public static NGit.Treewalk.TreeWalk ForPath(NGit.ObjectReader reader, string path
			, params AnyObjectId[] trees)
		{
			NGit.Treewalk.TreeWalk r = new NGit.Treewalk.TreeWalk(reader);
			r.Filter = PathFilterGroup.CreateFromStrings(Collections.Singleton(path));
			r.Recursive = r.Filter.ShouldBeRecursive();
			r.Reset(trees);
			return r.Next() ? r : null;
		}

		/// <summary>Open a tree walk and filter to exactly one path.</summary>
		/// <remarks>
		/// Open a tree walk and filter to exactly one path.
		/// <p>
		/// The returned tree walk is already positioned on the requested path, so
		/// the caller should not need to invoke
		/// <see cref="Next()">Next()</see>
		/// unless they are
		/// looking for a possible directory/file name conflict.
		/// </remarks>
		/// <param name="db">repository to read tree object data from.</param>
		/// <param name="path">single path to advance the tree walk instance into.</param>
		/// <param name="trees">one or more trees to walk through, all with the same root.</param>
		/// <returns>
		/// a new tree walk configured for exactly this one path; null if no
		/// path was found in any of the trees.
		/// </returns>
		/// <exception cref="System.IO.IOException">reading a pack file or loose object failed.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// an tree object could not be read as its data stream did not
		/// appear to be a tree, or could not be inflated.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">an object we expected to be a tree was not a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a tree object was not found.
		/// 	</exception>
		public static NGit.Treewalk.TreeWalk ForPath(Repository db, string path, params AnyObjectId
			[] trees)
		{
			NGit.ObjectReader reader = db.NewObjectReader();
			try
			{
				return ForPath(reader, path, trees);
			}
			finally
			{
				reader.Release();
			}
		}

		/// <summary>Open a tree walk and filter to exactly one path.</summary>
		/// <remarks>
		/// Open a tree walk and filter to exactly one path.
		/// <p>
		/// The returned tree walk is already positioned on the requested path, so
		/// the caller should not need to invoke
		/// <see cref="Next()">Next()</see>
		/// unless they are
		/// looking for a possible directory/file name conflict.
		/// </remarks>
		/// <param name="db">repository to read tree object data from.</param>
		/// <param name="path">single path to advance the tree walk instance into.</param>
		/// <param name="tree">the single tree to walk through.</param>
		/// <returns>
		/// a new tree walk configured for exactly this one path; null if no
		/// path was found in any of the trees.
		/// </returns>
		/// <exception cref="System.IO.IOException">reading a pack file or loose object failed.
		/// 	</exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// an tree object could not be read as its data stream did not
		/// appear to be a tree, or could not be inflated.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">an object we expected to be a tree was not a tree.
		/// 	</exception>
		/// <exception cref="NGit.Errors.MissingObjectException">a tree object was not found.
		/// 	</exception>
		public static NGit.Treewalk.TreeWalk ForPath(Repository db, string path, RevTree 
			tree)
		{
			return ForPath(db, path, new ObjectId[] { tree });
		}

		private readonly NGit.ObjectReader reader;

		private readonly MutableObjectId idBuffer = new MutableObjectId();

		private TreeFilter filter;

		internal AbstractTreeIterator[] trees;

		private bool recursive;

		private bool postOrderTraversal;

		private int depth;

		private bool advance;

		private bool postChildren;

		internal AbstractTreeIterator currentHead;

		/// <summary>Create a new tree walker for a given repository.</summary>
		/// <remarks>Create a new tree walker for a given repository.</remarks>
		/// <param name="repo">the repository the walker will obtain data from.</param>
		public TreeWalk(Repository repo) : this(repo.NewObjectReader())
		{
		}

		/// <summary>Create a new tree walker for a given repository.</summary>
		/// <remarks>Create a new tree walker for a given repository.</remarks>
		/// <param name="or">the reader the walker will obtain tree data from.</param>
		public TreeWalk(NGit.ObjectReader or)
		{
			reader = or;
			filter = TreeFilter.ALL;
			trees = NO_TREES;
		}

		/// <returns>the reader this walker is using to load objects.</returns>
		public virtual NGit.ObjectReader ObjectReader
		{
			get
			{
				return reader;
			}
		}

		/// <summary>Release any resources used by this walker's reader.</summary>
		/// <remarks>
		/// Release any resources used by this walker's reader.
		/// <p>
		/// A walker that has been released can be used again, but may need to be
		/// released after the subsequent usage.
		/// </remarks>
		public virtual void Release()
		{
			reader.Release();
		}

		/// <summary>Get the currently configured filter.</summary>
		/// <remarks>Get the currently configured filter.</remarks>
		/// <returns>the current filter. Never null as a filter is always needed.</returns>
		/// <summary>Set the tree entry filter for this walker.</summary>
		/// <remarks>
		/// Set the tree entry filter for this walker.
		/// <p>
		/// Multiple filters may be combined by constructing an arbitrary tree of
		/// <code>AndTreeFilter</code> or <code>OrTreeFilter</code> instances to
		/// describe the boolean expression required by the application. Custom
		/// filter implementations may also be constructed by applications.
		/// <p>
		/// Note that filters are not thread-safe and may not be shared by concurrent
		/// TreeWalk instances. Every TreeWalk must be supplied its own unique
		/// filter, unless the filter implementation specifically states it is (and
		/// always will be) thread-safe. Callers may use
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.Clone()">NGit.Treewalk.Filter.TreeFilter.Clone()
		/// 	</see>
		/// to create a unique filter tree for this TreeWalk instance.
		/// </remarks>
		/// <value>
		/// the new filter. If null the special
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ALL">NGit.Treewalk.Filter.TreeFilter.ALL
		/// 	</see>
		/// filter will be used instead, as it matches every entry.
		/// </value>
		/// <seealso cref="NGit.Treewalk.Filter.AndTreeFilter">NGit.Treewalk.Filter.AndTreeFilter
		/// 	</seealso>
		/// <seealso cref="NGit.Treewalk.Filter.OrTreeFilter">NGit.Treewalk.Filter.OrTreeFilter
		/// 	</seealso>
		public virtual TreeFilter Filter
		{
			get
			{
				return filter;
			}
			set
			{
				TreeFilter newFilter = value;
				filter = newFilter != null ? newFilter : TreeFilter.ALL;
			}
		}

		/// <summary>
		/// Is this walker automatically entering into subtrees?
		/// <p>
		/// If the walker is recursive then the caller will not see a subtree node
		/// and instead will only receive file nodes in all relevant subtrees.
		/// </summary>
		/// <remarks>
		/// Is this walker automatically entering into subtrees?
		/// <p>
		/// If the walker is recursive then the caller will not see a subtree node
		/// and instead will only receive file nodes in all relevant subtrees.
		/// </remarks>
		/// <returns>true if automatically entering subtrees is enabled.</returns>
		/// <summary>Set the walker to enter (or not enter) subtrees automatically.</summary>
		/// <remarks>
		/// Set the walker to enter (or not enter) subtrees automatically.
		/// <p>
		/// If recursive mode is enabled the walker will hide subtree nodes from the
		/// calling application and will produce only file level nodes. If a tree
		/// (directory) is deleted then all of the file level nodes will appear to be
		/// deleted, recursively, through as many levels as necessary to account for
		/// all entries.
		/// </remarks>
		/// <value>true to skip subtree nodes and only obtain files nodes.</value>
		public virtual bool Recursive
		{
			get
			{
				return recursive;
			}
			set
			{
				bool b = value;
				recursive = b;
			}
		}

		/// <summary>
		/// Does this walker return a tree entry after it exits the subtree?
		/// <p>
		/// If post order traversal is enabled then the walker will return a subtree
		/// after it has returned the last entry within that subtree.
		/// </summary>
		/// <remarks>
		/// Does this walker return a tree entry after it exits the subtree?
		/// <p>
		/// If post order traversal is enabled then the walker will return a subtree
		/// after it has returned the last entry within that subtree. This may cause
		/// a subtree to be seen by the application twice if
		/// <see cref="Recursive()">Recursive()</see>
		/// is false, as the application will see it once, call
		/// <see cref="EnterSubtree()">EnterSubtree()</see>
		/// , and then see it again as it leaves the subtree.
		/// <p>
		/// If an application does not enable
		/// <see cref="Recursive()">Recursive()</see>
		/// and it does not
		/// call
		/// <see cref="EnterSubtree()">EnterSubtree()</see>
		/// then the tree is returned only once as none
		/// of the children were processed.
		/// </remarks>
		/// <returns>true if subtrees are returned after entries within the subtree.</returns>
		/// <summary>Set the walker to return trees after their children.</summary>
		/// <remarks>Set the walker to return trees after their children.</remarks>
		/// <value>true to get trees after their children.</value>
		/// <seealso cref="PostOrderTraversal()">PostOrderTraversal()</seealso>
		public virtual bool PostOrderTraversal
		{
			get
			{
				return postOrderTraversal;
			}
			set
			{
				bool b = value;
				postOrderTraversal = b;
			}
		}

		/// <summary>Reset this walker so new tree iterators can be added to it.</summary>
		/// <remarks>Reset this walker so new tree iterators can be added to it.</remarks>
		public virtual void Reset()
		{
			trees = NO_TREES;
			advance = false;
			depth = 0;
		}

		/// <summary>Reset this walker to run over a single existing tree.</summary>
		/// <remarks>Reset this walker to run over a single existing tree.</remarks>
		/// <param name="id">
		/// the tree we need to parse. The walker will execute over this
		/// single tree if the reset is successful.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">the given tree object does not exist in this repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the given object id does not denote a tree, but instead names
		/// some other non-tree type of object. Note that commits are not
		/// trees, even if they are sometimes called a "tree-ish".
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the object claimed to be a tree, but its contents did not
		/// appear to be a tree. The repository may have data corruption.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual void Reset(AnyObjectId id)
		{
			if (trees.Length == 1)
			{
				AbstractTreeIterator o = trees[0];
				while (o.parent != null)
				{
					o = o.parent;
				}
				if (o is CanonicalTreeParser)
				{
					o.matches = null;
					o.matchShift = 0;
					((CanonicalTreeParser)o).Reset(reader, id);
					trees[0] = o;
				}
				else
				{
					trees[0] = ParserFor(id);
				}
			}
			else
			{
				trees = new AbstractTreeIterator[] { ParserFor(id) };
			}
			advance = false;
			depth = 0;
		}

		/// <summary>Reset this walker to run over a set of existing trees.</summary>
		/// <remarks>Reset this walker to run over a set of existing trees.</remarks>
		/// <param name="ids">
		/// the trees we need to parse. The walker will execute over this
		/// many parallel trees if the reset is successful.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">the given tree object does not exist in this repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the given object id does not denote a tree, but instead names
		/// some other non-tree type of object. Note that commits are not
		/// trees, even if they are sometimes called a "tree-ish".
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the object claimed to be a tree, but its contents did not
		/// appear to be a tree. The repository may have data corruption.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual void Reset(params AnyObjectId[] ids)
		{
			int oldLen = trees.Length;
			int newLen = ids.Length;
			AbstractTreeIterator[] r = newLen == oldLen ? trees : new AbstractTreeIterator[newLen
				];
			for (int i = 0; i < newLen; i++)
			{
				AbstractTreeIterator o;
				if (i < oldLen)
				{
					o = trees[i];
					while (o.parent != null)
					{
						o = o.parent;
					}
					if (o is CanonicalTreeParser && o.pathOffset == 0)
					{
						o.matches = null;
						o.matchShift = 0;
						((CanonicalTreeParser)o).Reset(reader, ids[i]);
						r[i] = o;
						continue;
					}
				}
				o = ParserFor(ids[i]);
				r[i] = o;
			}
			trees = r;
			advance = false;
			depth = 0;
		}

		/// <summary>Add an already existing tree object for walking.</summary>
		/// <remarks>
		/// Add an already existing tree object for walking.
		/// <p>
		/// The position of this tree is returned to the caller, in case the caller
		/// has lost track of the order they added the trees into the walker.
		/// <p>
		/// The tree must have the same root as existing trees in the walk.
		/// </remarks>
		/// <param name="id">identity of the tree object the caller wants walked.</param>
		/// <returns>position of this tree within the walker.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the given tree object does not exist in this repository.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the given object id does not denote a tree, but instead names
		/// some other non-tree type of object. Note that commits are not
		/// trees, even if they are sometimes called a "tree-ish".
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the object claimed to be a tree, but its contents did not
		/// appear to be a tree. The repository may have data corruption.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual int AddTree(AnyObjectId id)
		{
			return AddTree(ParserFor(id));
		}

		/// <summary>Add an already created tree iterator for walking.</summary>
		/// <remarks>
		/// Add an already created tree iterator for walking.
		/// <p>
		/// The position of this tree is returned to the caller, in case the caller
		/// has lost track of the order they added the trees into the walker.
		/// <p>
		/// The tree which the iterator operates on must have the same root as
		/// existing trees in the walk.
		/// </remarks>
		/// <param name="p">
		/// an iterator to walk over. The iterator should be new, with no
		/// parent, and should still be positioned before the first entry.
		/// The tree which the iterator operates on must have the same root
		/// as other trees in the walk.
		/// </param>
		/// <returns>position of this tree within the walker.</returns>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the iterator was unable to obtain its first entry, due to
		/// possible data corruption within the backing data store.
		/// </exception>
		public virtual int AddTree(AbstractTreeIterator p)
		{
			int n = trees.Length;
			AbstractTreeIterator[] newTrees = new AbstractTreeIterator[n + 1];
			System.Array.Copy(trees, 0, newTrees, 0, n);
			newTrees[n] = p;
			p.matches = null;
			p.matchShift = 0;
			trees = newTrees;
			return n;
		}

		/// <summary>Get the number of trees known to this walker.</summary>
		/// <remarks>Get the number of trees known to this walker.</remarks>
		/// <returns>the total number of trees this walker is iterating over.</returns>
		public virtual int TreeCount
		{
			get
			{
				return trees.Length;
			}
		}

		/// <summary>Advance this walker to the next relevant entry.</summary>
		/// <remarks>Advance this walker to the next relevant entry.</remarks>
		/// <returns>
		/// true if there is an entry available; false if all entries have
		/// been walked and the walk of this set of tree iterators is over.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// <see cref="Recursive()">Recursive()</see>
		/// was enabled, a subtree was found, but
		/// the subtree object does not exist in this repository. The
		/// repository may be missing objects.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// <see cref="Recursive()">Recursive()</see>
		/// was enabled, a subtree was found, and
		/// the subtree id does not denote a tree, but instead names some
		/// other non-tree type of object. The repository may have data
		/// corruption.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the contents of a tree did not appear to be a tree. The
		/// repository may have data corruption.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual bool Next()
		{
			try
			{
				if (advance)
				{
					advance = false;
					postChildren = false;
					PopEntriesEqual();
				}
				for (; ; )
				{
					AbstractTreeIterator t = Min();
					if (t.Eof)
					{
						if (depth > 0)
						{
							ExitSubtree();
							if (postOrderTraversal)
							{
								advance = true;
								postChildren = true;
								return true;
							}
							PopEntriesEqual();
							continue;
						}
						return false;
					}
					currentHead = t;
					if (!filter.Include(this))
					{
						SkipEntriesEqual();
						continue;
					}
					if (recursive && FileMode.TREE.Equals(t.mode))
					{
						EnterSubtree();
						continue;
					}
					advance = true;
					return true;
				}
			}
			catch (StopWalkException)
			{
				foreach (AbstractTreeIterator t in trees)
				{
					t.StopWalk();
				}
				return false;
			}
		}

		/// <summary>Obtain the tree iterator for the current entry.</summary>
		/// <remarks>
		/// Obtain the tree iterator for the current entry.
		/// <p>
		/// Entering into (or exiting out of) a subtree causes the current tree
		/// iterator instance to be changed for the nth tree. This allows the tree
		/// iterators to manage only one list of items, with the diving handled by
		/// recursive trees.
		/// </remarks>
		/// <?></?>
		/// <param name="nth">tree to obtain the current iterator of.</param>
		/// <param name="clazz">type of the tree iterator expected by the caller.</param>
		/// <returns>
		/// r the current iterator of the requested type; null if the tree
		/// has no entry to match the current path.
		/// </returns>
		public virtual T GetTree<T>(int nth) where T:AbstractTreeIterator
		{
			System.Type clazz = typeof(T);
			AbstractTreeIterator t = trees[nth];
			return t.matches == currentHead ? (T)t : null;
		}

		/// <summary>
		/// Obtain the raw
		/// <see cref="NGit.FileMode">NGit.FileMode</see>
		/// bits for the current entry.
		/// <p>
		/// Every added tree supplies mode bits, even if the tree does not contain
		/// the current entry. In the latter case
		/// <see cref="NGit.FileMode.MISSING">NGit.FileMode.MISSING</see>
		/// 's mode
		/// bits (0) are returned.
		/// </summary>
		/// <param name="nth">tree to obtain the mode bits from.</param>
		/// <returns>mode bits for the current entry of the nth tree.</returns>
		/// <seealso cref="NGit.FileMode.FromBits(int)">NGit.FileMode.FromBits(int)</seealso>
		public virtual int GetRawMode(int nth)
		{
			AbstractTreeIterator t = trees[nth];
			return t.matches == currentHead ? t.mode : 0;
		}

		/// <summary>
		/// Obtain the
		/// <see cref="NGit.FileMode">NGit.FileMode</see>
		/// for the current entry.
		/// <p>
		/// Every added tree supplies a mode, even if the tree does not contain the
		/// current entry. In the latter case
		/// <see cref="NGit.FileMode.MISSING">NGit.FileMode.MISSING</see>
		/// is returned.
		/// </summary>
		/// <param name="nth">tree to obtain the mode from.</param>
		/// <returns>mode for the current entry of the nth tree.</returns>
		public virtual FileMode GetFileMode(int nth)
		{
			return FileMode.FromBits(GetRawMode(nth));
		}

		/// <summary>Obtain the ObjectId for the current entry.</summary>
		/// <remarks>
		/// Obtain the ObjectId for the current entry.
		/// <p>
		/// Using this method to compare ObjectId values between trees of this walker
		/// is very inefficient. Applications should try to use
		/// <see cref="IdEqual(int, int)">IdEqual(int, int)</see>
		/// or
		/// <see cref="GetObjectId(NGit.MutableObjectId, int)">GetObjectId(NGit.MutableObjectId, int)
		/// 	</see>
		/// whenever possible.
		/// <p>
		/// Every tree supplies an object id, even if the tree does not contain the
		/// current entry. In the latter case
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// is returned.
		/// </remarks>
		/// <param name="nth">tree to obtain the object identifier from.</param>
		/// <returns>object identifier for the current tree entry.</returns>
		/// <seealso cref="GetObjectId(NGit.MutableObjectId, int)">GetObjectId(NGit.MutableObjectId, int)
		/// 	</seealso>
		/// <seealso cref="IdEqual(int, int)">IdEqual(int, int)</seealso>
		public virtual ObjectId GetObjectId(int nth)
		{
			AbstractTreeIterator t = trees[nth];
			return t.matches == currentHead ? t.EntryObjectId : ObjectId.ZeroId;
		}

		/// <summary>Obtain the ObjectId for the current entry.</summary>
		/// <remarks>
		/// Obtain the ObjectId for the current entry.
		/// <p>
		/// Every tree supplies an object id, even if the tree does not contain the
		/// current entry. In the latter case
		/// <see cref="NGit.ObjectId.ZeroId()">NGit.ObjectId.ZeroId()</see>
		/// is supplied.
		/// <p>
		/// Applications should try to use
		/// <see cref="IdEqual(int, int)">IdEqual(int, int)</see>
		/// when possible
		/// as it avoids conversion overheads.
		/// </remarks>
		/// <param name="out">buffer to copy the object id into.</param>
		/// <param name="nth">tree to obtain the object identifier from.</param>
		/// <seealso cref="IdEqual(int, int)">IdEqual(int, int)</seealso>
		public virtual void GetObjectId(MutableObjectId @out, int nth)
		{
			AbstractTreeIterator t = trees[nth];
			if (t.matches == currentHead)
			{
				t.GetEntryObjectId(@out);
			}
			else
			{
				@out.Clear();
			}
		}

		/// <summary>Compare two tree's current ObjectId values for equality.</summary>
		/// <remarks>Compare two tree's current ObjectId values for equality.</remarks>
		/// <param name="nthA">first tree to compare the object id from.</param>
		/// <param name="nthB">second tree to compare the object id from.</param>
		/// <returns>
		/// result of
		/// <code>getObjectId(nthA).equals(getObjectId(nthB))</code>.
		/// </returns>
		/// <seealso cref="GetObjectId(int)">GetObjectId(int)</seealso>
		public virtual bool IdEqual(int nthA, int nthB)
		{
			AbstractTreeIterator ch = currentHead;
			AbstractTreeIterator a = trees[nthA];
			AbstractTreeIterator b = trees[nthB];
			if (a.matches != ch && b.matches != ch)
			{
				// If neither tree matches the current path node then neither
				// tree has this entry. In such case the ObjectId is zero(),
				// and zero() is always equal to zero().
				//
				return true;
			}
			if (!a.HasId || !b.HasId)
			{
				return false;
			}
			if (a.matches == ch && b.matches == ch)
			{
				return a.IdEqual(b);
			}
			return false;
		}

		/// <summary>Get the current entry's name within its parent tree.</summary>
		/// <remarks>
		/// Get the current entry's name within its parent tree.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>
		/// name of the current entry within the parent tree (or directory).
		/// The name never includes a '/'.
		/// </returns>
		public virtual string NameString
		{
			get
			{
				AbstractTreeIterator t = currentHead;
				int off = t.pathOffset;
				int end = t.pathLen;
				return RawParseUtils.Decode(Constants.CHARSET, t.path, off, end);
			}
		}

		/// <summary>Get the current entry's complete path.</summary>
		/// <remarks>
		/// Get the current entry's complete path.
		/// <p>
		/// This method is not very efficient and is primarily meant for debugging
		/// and final output generation. Applications should try to avoid calling it,
		/// and if invoked do so only once per interesting entry, where the name is
		/// absolutely required for correct function.
		/// </remarks>
		/// <returns>
		/// complete path of the current entry, from the root of the
		/// repository. If the current entry is in a subtree there will be at
		/// least one '/' in the returned string.
		/// </returns>
		public virtual string PathString
		{
			get
			{
				return PathOf(currentHead);
			}
		}

		/// <summary>Get the current entry's complete path as a UTF-8 byte array.</summary>
		/// <remarks>Get the current entry's complete path as a UTF-8 byte array.</remarks>
		/// <returns>
		/// complete path of the current entry, from the root of the
		/// repository. If the current entry is in a subtree there will be at
		/// least one '/' in the returned string.
		/// </returns>
		public virtual byte[] RawPath
		{
			get
			{
				AbstractTreeIterator t = currentHead;
				int n = t.pathLen;
				byte[] r = new byte[n];
				System.Array.Copy(t.path, 0, r, 0, n);
				return r;
			}
		}

		/// <summary>Test if the supplied path matches the current entry's path.</summary>
		/// <remarks>
		/// Test if the supplied path matches the current entry's path.
		/// <p>
		/// This method tests that the supplied path is exactly equal to the current
		/// entry, or is one of its parent directories. It is faster to use this
		/// method then to use
		/// <see cref="PathString()">PathString()</see>
		/// to first create a String
		/// object, then test <code>startsWith</code> or some other type of string
		/// match function.
		/// </remarks>
		/// <param name="p">
		/// path buffer to test. Callers should ensure the path does not
		/// end with '/' prior to invocation.
		/// </param>
		/// <param name="pLen">number of bytes from <code>buf</code> to test.</param>
		/// <returns>
		/// &lt; 0 if p is before the current path; 0 if p matches the current
		/// path; 1 if the current path is past p and p will never match
		/// again on this tree walk.
		/// </returns>
		public virtual int IsPathPrefix(byte[] p, int pLen)
		{
			AbstractTreeIterator t = currentHead;
			byte[] c = t.path;
			int cLen = t.pathLen;
			int ci;
			for (ci = 0; ci < cLen && ci < pLen; ci++)
			{
				int c_value = (c[ci] & unchecked((int)(0xff))) - (p[ci] & unchecked((int)(0xff)));
				if (c_value != 0)
				{
					return c_value;
				}
			}
			if (ci < cLen)
			{
				// Ran out of pattern but we still had current data.
				// If c[ci] == '/' then pattern matches the subtree.
				// Otherwise we cannot be certain so we return -1.
				//
				return c[ci] == '/' ? 0 : -1;
			}
			if (ci < pLen)
			{
				// Ran out of current, but we still have pattern data.
				// If p[ci] == '/' then pattern matches this subtree,
				// otherwise we cannot be certain so we return -1.
				//
				return p[ci] == '/' ? 0 : -1;
			}
			// Both strings are identical.
			//
			return 0;
		}

		/// <summary>
		/// Test if the supplied path matches (being suffix of) the current entry's
		/// path.
		/// </summary>
		/// <remarks>
		/// Test if the supplied path matches (being suffix of) the current entry's
		/// path.
		/// <p>
		/// This method tests that the supplied path is exactly equal to the current
		/// entry, or is relative to one of entry's parent directories. It is faster
		/// to use this method then to use
		/// <see cref="PathString()">PathString()</see>
		/// to first create
		/// a String object, then test <code>endsWith</code> or some other type of
		/// string match function.
		/// </remarks>
		/// <param name="p">path buffer to test.</param>
		/// <param name="pLen">number of bytes from <code>buf</code> to test.</param>
		/// <returns>
		/// true if p is suffix of the current path;
		/// false if otherwise
		/// </returns>
		public virtual bool IsPathSuffix(byte[] p, int pLen)
		{
			AbstractTreeIterator t = currentHead;
			byte[] c = t.path;
			int cLen = t.pathLen;
			int ci;
			for (ci = 1; ci < cLen && ci < pLen; ci++)
			{
				if (c[cLen - ci] != p[pLen - ci])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Get the current subtree depth of this walker.</summary>
		/// <remarks>Get the current subtree depth of this walker.</remarks>
		/// <returns>the current subtree depth of this walker.</returns>
		public virtual int Depth
		{
			get
			{
				return depth;
			}
		}

		/// <summary>
		/// Is the current entry a subtree?
		/// <p>
		/// This method is faster then testing the raw mode bits of all trees to see
		/// if any of them are a subtree.
		/// </summary>
		/// <remarks>
		/// Is the current entry a subtree?
		/// <p>
		/// This method is faster then testing the raw mode bits of all trees to see
		/// if any of them are a subtree. If at least one is a subtree then this
		/// method will return true.
		/// </remarks>
		/// <returns>
		/// true if
		/// <see cref="EnterSubtree()">EnterSubtree()</see>
		/// will work on the current node.
		/// </returns>
		public virtual bool IsSubtree
		{
			get
			{
				return FileMode.TREE.Equals(currentHead.mode);
			}
		}

		/// <summary>Is the current entry a subtree returned after its children?</summary>
		/// <returns>
		/// true if the current node is a tree that has been returned after
		/// its children were already processed.
		/// </returns>
		/// <seealso cref="PostOrderTraversal()">PostOrderTraversal()</seealso>
		public virtual bool IsPostChildren
		{
			get
			{
				return postChildren && IsSubtree;
			}
		}

		/// <summary>Enter into the current subtree.</summary>
		/// <remarks>
		/// Enter into the current subtree.
		/// <p>
		/// If the current entry is a subtree this method arranges for its children
		/// to be returned before the next sibling following the subtree is returned.
		/// </remarks>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// a subtree was found, but the subtree object does not exist in
		/// this repository. The repository may be missing objects.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// a subtree was found, and the subtree id does not denote a
		/// tree, but instead names some other non-tree type of object.
		/// The repository may have data corruption.
		/// </exception>
		/// <exception cref="NGit.Errors.CorruptObjectException">
		/// the contents of a tree did not appear to be a tree. The
		/// repository may have data corruption.
		/// </exception>
		/// <exception cref="System.IO.IOException">a loose object or pack file could not be read.
		/// 	</exception>
		public virtual void EnterSubtree()
		{
			AbstractTreeIterator ch = currentHead;
			AbstractTreeIterator[] tmp = new AbstractTreeIterator[trees.Length];
			for (int i = 0; i < trees.Length; i++)
			{
				AbstractTreeIterator t = trees[i];
				AbstractTreeIterator n;
				if (t.matches == ch && !t.Eof && FileMode.TREE.Equals(t.mode))
				{
					n = t.CreateSubtreeIterator(reader, idBuffer);
				}
				else
				{
					n = t.CreateEmptyTreeIterator();
				}
				tmp[i] = n;
			}
			depth++;
			advance = false;
			System.Array.Copy(tmp, 0, trees, 0, trees.Length);
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal virtual AbstractTreeIterator Min()
		{
			int i = 0;
			AbstractTreeIterator minRef = trees[i];
			while (minRef.Eof && ++i < trees.Length)
			{
				minRef = trees[i];
			}
			if (minRef.Eof)
			{
				return minRef;
			}
			minRef.matches = minRef;
			while (++i < trees.Length)
			{
				AbstractTreeIterator t = trees[i];
				if (t.Eof)
				{
					continue;
				}
				int cmp = t.PathCompare(minRef);
				if (cmp < 0)
				{
					t.matches = t;
					minRef = t;
				}
				else
				{
					if (cmp == 0)
					{
						t.matches = minRef;
					}
				}
			}
			return minRef;
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal virtual void PopEntriesEqual()
		{
			AbstractTreeIterator ch = currentHead;
			for (int i = 0; i < trees.Length; i++)
			{
				AbstractTreeIterator t = trees[i];
				if (t.matches == ch)
				{
					t.Next(1);
					t.matches = null;
				}
			}
		}

		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		internal virtual void SkipEntriesEqual()
		{
			AbstractTreeIterator ch = currentHead;
			for (int i = 0; i < trees.Length; i++)
			{
				AbstractTreeIterator t = trees[i];
				if (t.matches == ch)
				{
					t.Skip();
					t.matches = null;
				}
			}
		}

		private void ExitSubtree()
		{
			depth--;
			for (int i = 0; i < trees.Length; i++)
			{
				trees[i] = trees[i].parent;
			}
			AbstractTreeIterator minRef = null;
			foreach (AbstractTreeIterator t in trees)
			{
				if (t.matches != t)
				{
					continue;
				}
				if (minRef == null || t.PathCompare(minRef) < 0)
				{
					minRef = t;
				}
			}
			currentHead = minRef;
		}

		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private CanonicalTreeParser ParserFor(AnyObjectId id)
		{
			CanonicalTreeParser p = new CanonicalTreeParser();
			p.Reset(reader, id);
			return p;
		}

		internal static string PathOf(AbstractTreeIterator t)
		{
			return RawParseUtils.Decode(Constants.CHARSET, t.path, 0, t.pathLen);
		}

		internal static string PathOf(byte[] buf, int pos, int end)
		{
			return RawParseUtils.Decode(Constants.CHARSET, buf, pos, end);
		}
	}
}
