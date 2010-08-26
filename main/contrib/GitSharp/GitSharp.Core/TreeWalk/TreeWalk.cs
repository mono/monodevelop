/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.TreeWalk.Filter;
using GitSharp.Core.Util;

namespace GitSharp.Core.TreeWalk
{
	/**
	 * Walks one or more {@link AbstractTreeIterator}s in parallel.
	 * <para />
	 * This class can perform n-way differences across as many trees as necessary.
	 * <para />
	 * Each tree added must have the same root as existing trees in the walk.
	 * <para />
	 * A TreeWalk instance can only be used once to generate results. Running a
	 * second time requires creating a new TreeWalk instance, or invoking
	 * {@link #reset()} and adding new trees before starting again. Resetting an
	 * existing instance may be faster for some applications as some internal
	 * buffers may be recycled.
	 * <para />
	 * TreeWalk instances are not thread-safe. Applications must either restrict
	 * usage of a TreeWalk instance to a single thread, or implement their own
	 * synchronization at a higher level.
	 * <para />
	 * Multiple simultaneous TreeWalk instances per {@link Repository} are
	 * permitted, even from concurrent threads.
	 */
	public class TreeWalk
	{
		/**
		 * Open a tree walk and filter to exactly one path.
		 * <para />
		 * The returned tree walk is already positioned on the requested path, so
		 * the caller should not need to invoke {@link #next()} unless they are
		 * looking for a possible directory/file name conflict.
		 * 
		 * @param db
		 *            repository to Read tree object data from.
		 * @param path
		 *            single path to advance the tree walk instance into.
		 * @param trees
		 *            one or more trees to walk through, all with the same root.
		 * @return a new tree walk configured for exactly this one path; null if no
		 *         path was found in any of the trees.
		 * @throws IOException
		 *             reading a pack file or loose object failed.
		 * @throws CorruptObjectException
		 *             an tree object could not be Read as its data stream did not
		 *             appear to be a tree, or could not be inflated.
		 * @throws IncorrectObjectTypeException
		 *             an object we expected to be a tree was not a tree.
		 * @throws MissingObjectException
		 *             a tree object was not found.
		 */

		public static TreeWalk ForPath(Repository db, string path, params AnyObjectId[] trees)
		{
			var r = new TreeWalk(db);
			r.Recursive = r.getFilter().shouldBeRecursive();

			r.setFilter(PathFilterGroup.createFromStrings(new HashSet<string> { path }));
			r.reset(trees);
			return r.next() ? r : null;
		}

		/**
		 * Open a tree walk and filter to exactly one path.
		 * <para />
		 * The returned tree walk is already positioned on the requested path, so
		 * the caller should not need to invoke {@link #next()} unless they are
		 * looking for a possible directory/file name conflict.
		 * 
		 * @param db
		 *            repository to Read tree object data from.
		 * @param path
		 *            single path to advance the tree walk instance into.
		 * @param tree
		 *            the single tree to walk through.
		 * @return a new tree walk configured for exactly this one path; null if no
		 *         path was found in any of the trees.
		 * @throws IOException
		 *             reading a pack file or loose object failed.
		 * @throws CorruptObjectException
		 *             an tree object could not be Read as its data stream did not
		 *             appear to be a tree, or could not be inflated.
		 * @throws IncorrectObjectTypeException
		 *             an object we expected to be a tree was not a tree.
		 * @throws MissingObjectException
		 *             a tree object was not found.
		 */
		public static TreeWalk ForPath(Repository db, string path, RevTree tree)
		{
			return ForPath(db, path, new ObjectId[] { tree });
		}

		private readonly Repository _db;
		private readonly MutableObjectId _idBuffer;
		private readonly WindowCursor _cursor;

		private TreeFilter _filter;
		private AbstractTreeIterator[] _trees;
		private int _depth;
		private bool _advance;
		private bool _postChildren;
		private AbstractTreeIterator _currentHead;

		/// <summary>
		/// Create a new tree walker for a given repository.
		/// </summary>
		/// <param name="repo">
		/// The repository the walker will obtain data from.
		/// </param>
		public TreeWalk(Repository repo)
		{
			_idBuffer = new MutableObjectId();
			_cursor = new WindowCursor();

			_db = repo;
			_filter = TreeFilter.ALL;
			_trees = new AbstractTreeIterator[] { new EmptyTreeIterator() };
		}

		/// <summary>
		/// Gets the repository this tree walker is reading from.
		/// </summary>
		public Repository Repository
		{
			get { return _db; }
		}

		/**
		 * Get the currently configured filter.
		 * 
		 * @return the current filter. Never null as a filter is always needed.
		 */
		public TreeFilter getFilter()
		{
			return _filter;
		}

		/**
		 * Set the tree entry filter for this walker.
		 * <para />
		 * Multiple filters may be combined by constructing an arbitrary tree of
		 * <code>AndTreeFilter</code> or <code>OrTreeFilter</code> instances to
		 * describe the bool expression required by the application. Custom
		 * filter implementations may also be constructed by applications.
		 * <para />
		 * Note that filters are not thread-safe and may not be shared by concurrent
		 * TreeWalk instances. Every TreeWalk must be supplied its own unique
		 * filter, unless the filter implementation specifically states it is (and
		 * always will be) thread-safe. Callers may use {@link TreeFilter#Clone()}
		 * to Create a unique filter tree for this TreeWalk instance.
		 * 
		 * @param newFilter
		 *            the new filter. If null the special {@link TreeFilter#ALL}
		 *            filter will be used instead, as it matches every entry.
		 * @see org.spearce.jgit.treewalk.filter.AndTreeFilter
		 * @see org.spearce.jgit.treewalk.filter.OrTreeFilter
		 */
		public void setFilter(TreeFilter newFilter)
		{
			_filter = newFilter ?? TreeFilter.ALL;
		}

		/// <summary>
		/// Is this walker automatically entering into subtrees?
		/// <para />
		/// If recursive mode is enabled the walker will hide subtree nodes from the
		/// calling application and will produce only file level nodes. If a tree
		/// (directory) is deleted then all of the file level nodes will appear to be
		/// deleted, recursively, through as many levels as necessary to account for
		/// all entries.
		/// </summary>
		public bool Recursive { get; set; }

		/**
		 * Does this walker return a tree entry After it exits the subtree?
		 * <para />
		 * If post order traversal is enabled then the walker will return a subtree
		 * After it has returned the last entry within that subtree. This may cause
		 * a subtree to be seen by the application twice if {@link #isRecursive()}
		 * is false, as the application will see it once, call
		 * {@link #enterSubtree()}, and then see it again as it leaves the subtree.
		 * <para />
		 * If an application does not enable {@link #isRecursive()} and it does not
		 * call {@link #enterSubtree()} then the tree is returned only once as none
		 * of the children were processed.
		 *
		 * @return true if subtrees are returned After entries within the subtree.
		 */
		public bool PostOrderTraversal { get; set; }

		/// <summary>
		/// Reset this walker so new tree iterators can be added to it.
		/// </summary>
		public void reset()
		{
			_trees = new AbstractTreeIterator[0];
			_advance = false;
			_depth = 0;
		}

		/**
		 * Reset this walker to run over a single existing tree.
		 *
		 * @param id
		 *            the tree we need to parse. The walker will execute over this
		 *            single tree if the reset is successful.
		 * @throws MissingObjectException
		 *             the given tree object does not exist in this repository.
		 * @throws IncorrectObjectTypeException
		 *             the given object id does not denote a tree, but instead names
		 *             some other non-tree type of object. Note that commits are not
		 *             trees, even if they are sometimes called a "tree-ish".
		 * @throws CorruptObjectException
		 *             the object claimed to be a tree, but its contents did not
		 *             appear to be a tree. The repository may have data corruption.
		 * @throws IOException
		 *             a loose object or pack file could not be Read.
		 */
		public void reset(AnyObjectId id)
		{
			if (_trees.Length == 1)
			{
				AbstractTreeIterator iterator = _trees[0];
				while (iterator.Parent != null)
				{
					iterator = iterator.Parent;
				}
				
				CanonicalTreeParser oParse = (iterator as CanonicalTreeParser);
				if (oParse != null)
				{
					iterator.Matches = null;
					iterator.MatchShift = 0;

					oParse.reset(_db, id, _cursor);
					_trees[0] = iterator;
				}
				else
				{
					_trees[0] = ParserFor(id);
				}
			}
			else
			{
				_trees = new AbstractTreeIterator[] { ParserFor(id) };
			}

			_advance = false;
			_depth = 0;
		}

		/**
		 * Reset this walker to run over a set of existing trees.
		 * 
		 * @param ids
		 *            the trees we need to parse. The walker will execute over this
		 *            many parallel trees if the reset is successful.
		 * @throws MissingObjectException
		 *             the given tree object does not exist in this repository.
		 * @throws IncorrectObjectTypeException
		 *             the given object id does not denote a tree, but instead names
		 *             some other non-tree type of object. Note that commits are not
		 *             trees, even if they are sometimes called a "tree-ish".
		 * @throws CorruptObjectException
		 *             the object claimed to be a tree, but its contents did not
		 *             appear to be a tree. The repository may have data corruption.
		 * @throws IOException
		 *             a loose object or pack file could not be Read.
		 */
		public void reset(AnyObjectId[] ids)
		{
			if (ids==null)
				throw new ArgumentNullException("ids");
			int oldLen = _trees.Length;
			int newLen = ids.Length;
			AbstractTreeIterator[] r = newLen == oldLen ? _trees : new AbstractTreeIterator[newLen];
			for (int i = 0; i < newLen; i++)
			{
				AbstractTreeIterator iterator;

				if (i < oldLen)
				{
					iterator = _trees[i];
					while (iterator.Parent != null)
					{
						iterator = iterator.Parent;
					}

					CanonicalTreeParser oParse = (iterator as CanonicalTreeParser);
					if (oParse != null && iterator.PathOffset == 0)
					{
						iterator.Matches = null;
						iterator.MatchShift = 0;
						oParse.reset(_db, ids[i], _cursor);
						r[i] = iterator;
						continue;
					}
				}

				iterator = ParserFor(ids[i]);
				r[i] = iterator;
			}

			_trees = r;
			_advance = false;
			_depth = 0;
		}

		/**
		 * Add an already existing tree object for walking.
		 * <para />
		 * The position of this tree is returned to the caller, in case the caller
		 * has lost track of the order they added the trees into the walker.
		 * <para />
		 * The tree must have the same root as existing trees in the walk.
		 * 
		 * @param id
		 *            identity of the tree object the caller wants walked.
		 * @return position of this tree within the walker.
		 * @throws MissingObjectException
		 *             the given tree object does not exist in this repository.
		 * @throws IncorrectObjectTypeException
		 *             the given object id does not denote a tree, but instead names
		 *             some other non-tree type of object. Note that commits are not
		 *             trees, even if they are sometimes called a "tree-ish".
		 * @throws CorruptObjectException
		 *             the object claimed to be a tree, but its contents did not
		 *             appear to be a tree. The repository may have data corruption.
		 * @throws IOException
		 *             a loose object or pack file could not be Read.
		 */
		public int addTree(ObjectId id)
		{
			return addTree(ParserFor(id));
		}

		/**
		 * Add an already created tree iterator for walking.
		 * <para />
		 * The position of this tree is returned to the caller, in case the caller
		 * has lost track of the order they added the trees into the walker.
		 * <para />
		 * The tree which the iterator operates on must have the same root as
		 * existing trees in the walk.
		 * 
		 * @param parentIterator
		 *            an iterator to walk over. The iterator should be new, with no
		 *            parent, and should still be positioned before the first entry.
		 *            The tree which the iterator operates on must have the same root
		 *            as other trees in the walk.
		 *
		 * @return position of this tree within the walker.
		 * @throws CorruptObjectException
		 *             the iterator was unable to obtain its first entry, due to
		 *             possible data corruption within the backing data store.
		 */
		public int addTree(AbstractTreeIterator parentIterator)
		{
			if (parentIterator == null)
				throw new ArgumentNullException ("parentIterator");
			int n = _trees.Length;
			var newTrees = new AbstractTreeIterator[n + 1];

			Array.Copy(_trees, 0, newTrees, 0, n);
			newTrees[n] = parentIterator;
			parentIterator.Matches = null;
			parentIterator.MatchShift = 0;

			_trees = newTrees;
			return n;
		}

		/**
		 * Get the number of trees known to this walker.
		 * 
		 * @return the total number of trees this walker is iterating over.
		 */
		public int getTreeCount()
		{
			return _trees.Length;
		}

		/**
		 * Advance this walker to the next relevant entry.
		 * 
		 * @return true if there is an entry available; false if all entries have
		 *         been walked and the walk of this set of tree iterators is over.
		 * @throws MissingObjectException
		 *             {@link #isRecursive()} was enabled, a subtree was found, but
		 *             the subtree object does not exist in this repository. The
		 *             repository may be missing objects.
		 * @throws IncorrectObjectTypeException
		 *             {@link #isRecursive()} was enabled, a subtree was found, and
		 *             the subtree id does not denote a tree, but instead names some
		 *             other non-tree type of object. The repository may have data
		 *             corruption.
		 * @throws CorruptObjectException
		 *             the contents of a tree did not appear to be a tree. The
		 *             repository may have data corruption.
		 * @throws IOException
		 *             a loose object or pack file could not be Read.
		 */
		public bool next()
		{
			try
			{
				if (_advance)
				{
					_advance = false;
					_postChildren = false;
					popEntriesEqual();
				}

				while (true)
				{
					AbstractTreeIterator t = min();
					if (t.eof())
					{
						if (_depth > 0)
						{
							ExitSubtree();
							if (PostOrderTraversal)
							{
								_advance = true;
								_postChildren = true;
								return true;
							}
							popEntriesEqual();
							continue;
						}
						return false;
					}

					_currentHead = t;
					if (!_filter.include(this))
					{
						skipEntriesEqual();
						continue;
					}

					if (Recursive && FileMode.Tree == t.EntryFileMode)
					{
						enterSubtree();
						continue;
					}

					_advance = true;
					return true;
				}
			}
			catch (StopWalkException)
			{
				foreach (AbstractTreeIterator t in _trees)
					t.stopWalk();
				return false;
			}
		}

		/// <summary>
		/// Obtain the tree iterator for the current entry.
		/// <para />
		/// Entering into (or exiting out of) a subtree causes the current tree
		/// iterator instance to be changed for the nth tree. This allows the tree
		/// iterators to manage only one list of items, with the diving handled by
		/// recursive trees.
		/// </summary>
		/// <typeparam name="T">type of the tree iterator expected by the caller.</typeparam>
		/// <param name="nth">tree to obtain the current iterator of.</param>
		/// <param name="clazz">type of the tree iterator expected by the caller.</param>
		/// <returns>
		/// The current iterator of the requested type; null if the tree
		/// has no entry to match the current path.
		/// </returns>
		public T getTree<T>(int nth, Type clazz) // [henon] was Class<T> clazz
			where T : AbstractTreeIterator
		{
			AbstractTreeIterator t = _trees[nth];
			return t.Matches == _currentHead ? (T)t : null;
		}

		/**
		 * Obtain the raw {@link FileMode} bits for the current entry.
		 * <para />
		 * Every added tree supplies mode bits, even if the tree does not contain
		 * the current entry. In the latter case {@link FileMode#MISSING}'s mode
		 * bits (0) are returned.
		 * 
		 * @param nth
		 *            tree to obtain the mode bits from.
		 * @return mode bits for the current entry of the nth tree.
		 * @see FileMode#FromBits(int)
		 */
		public int getRawMode(int nth)
		{
			AbstractTreeIterator t = _trees[nth];
			return t.Matches == _currentHead ? t.Mode : 0;
		}

		/**
		 * Obtain the {@link FileMode} for the current entry.
		 * <para />
		 * Every added tree supplies a mode, even if the tree does not contain the
		 * current entry. In the latter case {@link FileMode#MISSING} is returned.
		 * 
		 * @param nth
		 *            tree to obtain the mode from.
		 * @return mode for the current entry of the nth tree.
		 */
		public FileMode getFileMode(int nth)
		{
			return FileMode.FromBits(getRawMode(nth));
		}

		/**
		 * Obtain the ObjectId for the current entry.
		 * <para />
		 * Using this method to compare ObjectId values between trees of this walker
		 * is very inefficient. Applications should try to use
		 * {@link #idEqual(int, int)} or {@link #getObjectId(MutableObjectId, int)}
		 * whenever possible.
		 * <para />
		 * Every tree supplies an object id, even if the tree does not contain the
		 * current entry. In the latter case {@link ObjectId#zeroId()} is returned.
		 * 
		 * @param nth
		 *            tree to obtain the object identifier from.
		 * @return object identifier for the current tree entry.
		 * @see #getObjectId(MutableObjectId, int)
		 * @see #idEqual(int, int)
		 */
		public ObjectId getObjectId(int nth)
		{
			AbstractTreeIterator t = _trees[nth];
			return t.Matches == _currentHead ? t.getEntryObjectId() : ObjectId.ZeroId;
		}

		/**
		 * Obtain the ObjectId for the current entry.
		 * <para />
		 * Every tree supplies an object id, even if the tree does not contain the
		 * current entry. In the latter case {@link ObjectId#zeroId()} is supplied.
		 * <para />
		 * Applications should try to use {@link #idEqual(int, int)} when possible
		 * as it avoids conversion overheads.
		 *
		 * @param out
		 *            buffer to copy the object id into.
		 * @param nth
		 *            tree to obtain the object identifier from.
		 * @see #idEqual(int, int)
		 */
		public void getObjectId(MutableObjectId @out, int nth)
		{
			AbstractTreeIterator t = _trees[nth];
			if (t.Matches == _currentHead)
				t.getEntryObjectId(@out);
			else
				@out.Clear();
		}

		/**
		 * Compare two tree's current ObjectId values for equality.
		 * 
		 * @param nthA
		 *            first tree to compare the object id from.
		 * @param nthB
		 *            second tree to compare the object id from.
		 * @return result of
		 *         <code>getObjectId(nthA).Equals(getObjectId(nthB))</code>.
		 * @see #getObjectId(int)
		 */
		public bool idEqual(int nthA, int nthB)
		{
			AbstractTreeIterator ch = _currentHead;
			AbstractTreeIterator a = _trees[nthA];
			AbstractTreeIterator b = _trees[nthB];
			if (a.Matches == ch && b.Matches == ch)
			{
				return a.idEqual(b);
			}

			if (a.Matches != ch && b.Matches != ch)
			{
				// If neither tree matches the current path node then neither
				// tree has this entry. In such case the ObjectId is zero(),
				// and zero() is always equal to zero().
				//
				return true;
			}

			return false;
		}

		/**
		 * Get the current entry's name within its parent tree.
		 * <para />
		 * This method is not very efficient and is primarily meant for debugging
		 * and  output generation. Applications should try to avoid calling it,
		 * and if invoked do so only once per interesting entry, where the name is
		 * absolutely required for correct function.
		 *
		 * @return name of the current entry within the parent tree (or directory).
		 *         The name never includes a '/'.
		 */
		public string getNameString()
		{
			AbstractTreeIterator t = _currentHead;
			int off = t.PathOffset;
			int end = t.PathLen;
			return RawParseUtils.decode(Constants.CHARSET, t.Path, off, end);
		}

		/**
		 * Get the current entry's complete path.
		 * <para />
		 * This method is not very efficient and is primarily meant for debugging
		 * and  output generation. Applications should try to avoid calling it,
		 * and if invoked do so only once per interesting entry, where the name is
		 * absolutely required for correct function.
		 * 
		 * @return complete path of the current entry, from the root of the
		 *         repository. If the current entry is in a subtree there will be at
		 *         least one '/' in the returned string.
		 */
		public string getPathString()
		{
			return pathOf(_currentHead);
		}

		/**
		 * Get the current entry's complete path as a UTF-8 byte array.
		 *
		 * @return complete path of the current entry, from the root of the
		 *         repository. If the current entry is in a subtree there will be at
		 *         least one '/' in the returned string.
		 */
		public byte[] getRawPath()
		{
			AbstractTreeIterator treeIterator = CurrentHead;
			int newPathLen = treeIterator.PathLen;
			var rawPath = new byte[newPathLen];
			Array.Copy(treeIterator.Path, 0, rawPath, 0, newPathLen);
			return rawPath;
		}

		/**
		 * Test if the supplied path matches the current entry's path.
		 * <para />
		 * This method tests that the supplied path is exactly equal to the current
		 * entry, or is one of its parent directories. It is faster to use this
		 * method then to use {@link #getPathString()} to first Create a string
		 * object, then test <code>startsWith</code> or some other type of string
		 * match function.
		 * 
		 * @param p
		 *            path buffer to test. Callers should ensure the path does not
		 *            end with '/' prior to invocation.
		 * @param pLen
		 *            number of bytes from <code>buf</code> to test.
		 * @return &lt; 0 if p is before the current path; 0 if p matches the current
		 *         path; 1 if the current path is past p and p will never match
		 *         again on this tree walk.
		 */
		public int isPathPrefix(byte[] p, int pLen)
		{
			if (p==null)
				throw new ArgumentNullException("p");
			AbstractTreeIterator t = _currentHead;
			byte[] c = t.Path;
			int cLen = t.PathLen;
			int ci;

			for (ci = 0; ci < cLen && ci < pLen; ci++)
			{
				int cValue = (c[ci] & 0xff) - (p[ci] & 0xff);
				if (cValue != 0)
				{
					return cValue;
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

		/**
		 * Test if the supplied path matches (being suffix of) the current entry's
		 * path.
		 * <para />
		 * This method tests that the supplied path is exactly equal to the current
		 * entry, or is relative to one of entry's parent directories. It is faster
		 * to use this method then to use {@link #getPathString()} to first Create
		 * a String object, then test <code>endsWith</code> or some other type of
		 * string match function.
		 *
		 * @param p
		 *            path buffer to test.
		 * @param pLen
		 *            number of bytes from <code>buf</code> to test.
		 * @return true if p is suffix of the current path;
		 *         false if otherwise
		 */
		public bool isPathSuffix(byte[] p, int pLen)
		{
			if (p==null)
				throw new ArgumentNullException("p");
			AbstractTreeIterator t = _currentHead;
			byte[] c = t.Path;
			int cLen = t.PathLen;
			int ci;

			for (ci = 1; ci < cLen && ci < pLen; ci++)
			{
				if (c[cLen - ci] != p[pLen - ci]) return false;
			}

			return true;
		}

		/**
		 * Get the current subtree depth of this walker.
		 *
		 * @return the current subtree depth of this walker.
		 */

		public int Depth
		{
			get { return _depth; }
		}

		/**
		 * Is the current entry a subtree?
		 * <para />
		 * This method is faster then testing the raw mode bits of all trees to see
		 * if any of them are a subtree. If at least one is a subtree then this
		 * method will return true.
		 * 
		 * @return true if {@link #enterSubtree()} will work on the current node.
		 */
		public bool isSubtree()
		{
			return FileMode.Tree == CurrentHead.EntryFileMode;
		}

		/**
		 * Is the current entry a subtree returned After its children?
		 *
		 * @return true if the current node is a tree that has been returned After
		 *         its children were already processed.
		 * @see #isPostOrderTraversal()
		 */
		public bool isPostChildren()
		{
			return _postChildren && isSubtree();
		}

		/**
		 * Enter into the current subtree.
		 * <para />
		 * If the current entry is a subtree this method arranges for its children
		 * to be returned before the next sibling following the subtree is returned.
		 * 
		 * @throws MissingObjectException
		 *             a subtree was found, but the subtree object does not exist in
		 *             this repository. The repository may be missing objects.
		 * @throws IncorrectObjectTypeException
		 *             a subtree was found, and the subtree id does not denote a
		 *             tree, but instead names some other non-tree type of object.
		 *             The repository may have data corruption.
		 * @throws CorruptObjectException
		 *             the contents of a tree did not appear to be a tree. The
		 *             repository may have data corruption.
		 * @throws IOException
		 *             a loose object or pack file could not be Read.
		 */
		public void enterSubtree()
		{
			AbstractTreeIterator ch = CurrentHead;
			var tmp = new AbstractTreeIterator[_trees.Length];
			for (int i = 0; i < _trees.Length; i++)
			{
				AbstractTreeIterator treeIterator = _trees[i];
				AbstractTreeIterator newIterator;

				if (treeIterator.Matches == ch && !treeIterator.eof() && FileMode.Tree == treeIterator.EntryFileMode)
				{
					newIterator = treeIterator.createSubtreeIterator(_db, _idBuffer, _cursor);
				}
				else
				{
					newIterator = treeIterator.createEmptyTreeIterator();
				}
				
				tmp[i] = newIterator;
			}

			_depth++;
			_advance = false;

			Array.Copy(tmp, 0, _trees, 0, _trees.Length);
		}

		public virtual AbstractTreeIterator min()
		{
			int i = 0;
			AbstractTreeIterator minRef = _trees[i];
			while (minRef.eof() && ++i < _trees.Length)
			{
				minRef = _trees[i];
			}

			if (minRef.eof())
			{
				return minRef;
			}

			minRef.Matches = minRef;
			while (++i < _trees.Length)
			{
				AbstractTreeIterator t = _trees[i];
				if (t.eof()) continue;

				int cmp = t.pathCompare(minRef);
				if (cmp < 0)
				{
					t.Matches = t;
					minRef = t;
				}
				else if (cmp == 0)
				{
					t.Matches = minRef;
				}
			}

			return minRef;
		}

		public virtual void popEntriesEqual()
		{
			AbstractTreeIterator ch = _currentHead;
			for (int i = 0; i < _trees.Length; i++)
			{
				AbstractTreeIterator t = _trees[i];
				if (t.Matches == ch)
				{
					t.next(1);
					t.Matches = null;
				}
			}
		}

		public virtual void skipEntriesEqual()
		{
			AbstractTreeIterator ch = _currentHead;
			for (int i = 0; i < _trees.Length; i++)
			{
				AbstractTreeIterator t = _trees[i];
				if (t.Matches != ch) continue;

				t.skip();
				t.Matches = null;
			}
		}

		private void ExitSubtree()
		{
			_depth--;
			for (int i = 0; i < _trees.Length; i++)
			{
				_trees[i] = _trees[i].Parent;
			}

			AbstractTreeIterator minRef = null;
			foreach (AbstractTreeIterator t in _trees)
			{
				if (t.Matches != t) continue;

				if (minRef == null || t.pathCompare(minRef) < 0)
				{
					minRef = t;
				}
			}
			_currentHead = minRef;
		}

		private CanonicalTreeParser ParserFor(AnyObjectId id)
		{
			var p = new CanonicalTreeParser();
			p.reset(_db, id, _cursor);
			return p;
		}

		public AbstractTreeIterator CurrentHead
		{
			get { return _currentHead; }
		}

		public AbstractTreeIterator[] Trees
		{
			get { return _trees; }
		}

		public static string pathOf(AbstractTreeIterator t)
		{
			if (t == null)
				throw new ArgumentNullException ("t");
			return RawParseUtils.decode(Constants.CHARSET, t.Path, 0, t.PathLen);
		}
	}
}