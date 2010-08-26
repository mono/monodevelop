/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core.RevWalk.Filter;
using GitSharp.Core.TreeWalk.Filter;
using GitSharp.Core.Util;

namespace GitSharp.Core.RevWalk
{
	/// <summary>
	/// Walks a commit graph and produces the matching commits in order.
	/// <para />
	/// A RevWalk instance can only be used once to generate results. Running a
	/// second time requires creating a new RevWalk instance, or invoking
	/// <seealso cref="reset()"/> before starting again. Resetting an existing instance may be
	/// faster for some applications as commit body parsing can be avoided on the
	/// later invocations.
	/// <para />
	/// RevWalk instances are not thread-safe. Applications must either restrict
	/// usage of a RevWalk instance to a single thread, or implement their own
	/// synchronization at a higher level.
	/// <para />
	/// Multiple simultaneous RevWalk instances per <seealso cref="Repository"/> are permitted,
	/// even from concurrent threads. Equality of <seealso cref="RevCommit"/>s from two
	/// different RevWalk instances is never true, even if their <seealso cref="ObjectId"/>s
	/// are equal (and thus they describe the same commit).
	/// <para />
	/// The offered iterator is over the list of RevCommits described by the
	/// configuration of this instance. Applications should restrict themselves to
	/// using either the provided Iterator or <seealso cref="next()"/>, but never use both on
	/// the same RevWalk at the same time. The Iterator may buffer RevCommits, while
	/// <seealso cref="next()"/> does not.
	/// </summary>
	public class RevWalk : IEnumerable<RevCommit>, IDisposable
	{
		#region Enums

		[Flags]
		[Serializable]
		public enum RevWalkState
		{
			/// <summary>
			/// Set on objects whose important header data has been loaded.
			/// <para />
			/// For a RevCommit this indicates we have pulled apart the tree and parent
			/// references from the raw bytes available in the repository and translated
			/// those to our own local RevTree and RevCommit instances. The raw buffer is
			/// also available for message and other header filtering.
			/// <para />
			/// For a RevTag this indicates we have pulled part the tag references to
			/// find out who the tag refers to, and what that object's type is.
			/// </summary>
			PARSED = 1 << 0,

			/// <summary>
			/// Set on RevCommit instances added to our <see cref="Pending"/> queue.
			/// <para />
			/// We use this flag to avoid adding the same commit instance twice to our
			/// queue, especially if we reached it by more than one path.
			/// </summary>
			SEEN = 1 << 1,

			/// <summary>
			/// Set on RevCommit instances the caller does not want output.
			/// <para />
			/// We flag commits as uninteresting if the caller does not want commits
			/// reachable from a commit given to <see cref="markUninteresting(RevCommit)"/>.
			/// This flag is always carried into the commit's parents and is a key part
			/// of the "rev-list B --not A" feature; A is marked UNINTERESTING.
			/// </summary>
			UNINTERESTING = 1 << 2,

			/// <summary>
			/// Set on a RevCommit that can collapse out of the history.
			/// <para />
			/// If the <see cref="TreeFilter"/> concluded that this commit matches his
			/// parents' for all of the paths that the filter is interested in then we
			/// mark the commit REWRITE. Later we can rewrite the parents of a REWRITE
			/// child to remove chains of REWRITE commits before we produce the child to
			/// the application.
			/// </summary>
			/// <seealso cref="RewriteGenerator"/>
			REWRITE = 1 << 3,

			/// <summary>
			/// Temporary mark for use within generators or filters.
			/// <para />
			/// This mark is only for local use within a single scope. If someone sets
			/// the mark they must unset it before any other code can see the mark.
			/// </summary>
			TEMP_MARK = 1 << 4,

			/// <summary>
			/// Temporary mark for use within {@link TopoSortGenerator}.
			/// <para />
			/// This mark indicates the commit could not produce when it wanted to, as at
			/// least one child was behind it. Commits with this flag are delayed until
			/// all children have been output first.
			/// </summary>
			TOPO_DELAY = 1 << 5,
		}

		#endregion

		///	<summary>
		/// Set on objects whose important header data has been loaded.
		/// <para />
		/// For a RevCommit this indicates we have pulled apart the tree and parent
		/// references from the raw bytes available in the repository and translated
		/// those to our own local RevTree and RevCommit instances. The raw buffer is
		/// also available for message and other header filtering.
		/// <para />
		/// For a RevTag this indicates we have pulled part the tag references to
		/// find out who the tag refers to, and what that object's type is.
		/// </summary>
		internal const int PARSED = 1 << 0;

		///	<summary>
		/// Set on RevCommit instances added to our <seealso cref="Pending"/> queue.
		/// <para />
		/// We use this flag to avoid adding the same commit instance twice to our
		/// queue, especially if we reached it by more than one path.
		/// </summary>
		internal const int SEEN = 1 << 1;

		///	<summary>
		/// Set on RevCommit instances the caller does not want output.
		/// <para />
		/// We flag commits as uninteresting if the caller does not want commits
		/// reachable from a commit given to <seealso cref="markUninteresting(RevCommit)"/>.
		/// This flag is always carried into the commit's parents and is a key part
		/// of the "rev-list B --not A" feature; A is marked UNINTERESTING.
		/// </summary>
		internal const int UNINTERESTING = 1 << 2;

		///	<summary> 
		/// Set on a RevCommit that can collapse out of the history.
		/// <para />
		/// If the <seealso cref="TreeFilter"/> concluded that this commit matches his
		/// parents' for all of the paths that the filter is interested in then we
		/// mark the commit REWRITE. Later we can rewrite the parents of a REWRITE
		/// child to remove chains of REWRITE commits before we produce the child to
		/// the application.
		/// </summary>
		///	<seealso cref="RewriteGenerator" />
		internal const int REWRITE = 1 << 3;

		///	<summary>
		/// Temporary mark for use within generators or filters.
		/// <para />
		/// This mark is only for local use within a single scope. If someone sets
		/// the mark they must unset it before any other code can see the mark.
		/// </summary>
		internal const int TEMP_MARK = 1 << 4;

		///	<summary>
		/// Temporary mark for use within <seealso cref="TopoSortGenerator"/>.
		/// <para />
		/// This mark indicates the commit could not produce when it wanted to, as at
		/// least one child was behind it. Commits with this flag are delayed until
		/// all children have been output first.
		/// </summary>
		internal const int TOPO_DELAY = 1 << 5;

		// Number of flag bits we keep internal for our own use. See above flags.
		private const int ReservedFlags = 6;
		private const int AppFlags = -1 & ~((1 << ReservedFlags) - 1);

		private readonly ObjectIdSubclassMap<RevObject> _objects;
		private readonly List<RevCommit> _roots;
		private readonly HashSet<RevSort.Strategy> _sorting;
		private readonly Repository _db;
		private readonly WindowCursor _curs;
		private readonly MutableObjectId _idBuffer;

		private int _delayFreeFlags;
		private int _freeFlags;
		private int _carryFlags;
		private RevFilter _filter;
		private TreeFilter _treeFilter;
		private bool _retainBody;

		/// <summary>
		/// Create a new revision walker for a given repository.
		/// </summary>
		/// <param name="repo">
		/// The repository the walker will obtain data from.
		/// </param>
		public RevWalk(Repository repo)
		{
			_freeFlags = AppFlags;
			_carryFlags = UNINTERESTING;

			_db = repo;
			_curs = new WindowCursor();
			_idBuffer = new MutableObjectId();
			_objects = new ObjectIdSubclassMap<RevObject>();
			_roots = new List<RevCommit>();
			Queue = new DateRevQueue();
			Pending = new StartGenerator(this);
			_sorting = new HashSet<RevSort.Strategy> { RevSort.NONE };
			_filter = RevFilter.ALL;
			_treeFilter = TreeFilter.ALL;
		    _retainBody = true;
		}

		public MutableObjectId IdBuffer
		{
			get { return _idBuffer; }
		}

		public WindowCursor WindowCursor
		{
			get { return _curs; }
		}

		public Generator Pending { get; set; }

		public AbstractRevQueue Queue { get; set; }

		/// <summary>
		/// Get the repository this walker loads objects from.
		/// </summary>
		public Repository Repository
		{
			get { return _db; }
		}

		/// <summary>
		/// Mark a commit to start graph traversal from.
		/// <para />
		/// Callers are encouraged to use <see cref="parseCommit(AnyObjectId)"/> to obtain
		/// the commit reference, rather than <see cref="lookupCommit(AnyObjectId)"/>, as
		/// this method requires the commit to be parsed before it can be added as a
		/// root for the traversal.
		/// <para />
		/// The method will automatically parse an unparsed commit, but error
		/// handling may be more difficult for the application to explain why a
		/// <see cref="RevCommit"/> is not actually a commit. The object pool of this 
		/// walker would also be 'poisoned' by the non-commit RevCommit.
		/// </summary>
		/// <param name="c">
		/// The commit to start traversing from. The commit passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="MissingObjectException">
		/// The commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to <see cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// The object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		public void markStart(RevCommit c)
		{
			if ((c.Flags & SEEN) != 0) return;
			if ((c.Flags & PARSED) == 0)
			{
				c.parseHeaders(this);
			}
			c.Flags |= SEEN;
			_roots.Add(c);
			Queue.add(c);
		}

		/// <summary>
		/// Mark a commit to start graph traversal from.
		/// <para />
		/// Callers are encouraged to use <see cref="parseCommit(AnyObjectId)"/> to obtain
		/// the commit reference, rather than <see cref="lookupCommit(AnyObjectId)"/>, as
		/// this method requires the commit to be parsed before it can be added as a
		/// root for the traversal.
		/// <para />
		/// The method will automatically parse an unparsed commit, but error
		/// handling may be more difficult for the application to explain why a
		/// <see cref="RevCommit"/> is not actually a commit. The object pool of this 
		/// walker would also be 'poisoned' by the non-commit RevCommit.
		/// </summary>
		/// <param name="list">
		/// Commits to start traversing from. The commits passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="MissingObjectException">
		/// The commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to <see cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// The object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		public void markStart(IEnumerable<RevCommit> list)
		{
			foreach (RevCommit c in list)
			{
				markStart(c);
			}
		}

		///	<summary>
		/// Mark a commit to not produce in the output.
		/// <para />
		/// Uninteresting commits denote not just themselves but also their entire
		/// ancestry chain, back until the merge base of an uninteresting commit and
		/// an otherwise interesting commit.
		/// <para />
		/// Callers are encouraged to use <seealso cref="parseCommit(AnyObjectId)"/> to obtain
		/// the commit reference, rather than <seealso cref="lookupCommit(AnyObjectId)"/>, as
		/// this method requires the commit to be parsed before it can be added as a
		/// root for the traversal.
		/// <para />
		/// The method will automatically parse an unparsed commit, but error
		/// handling may be more difficult for the application to explain why a
		/// RevCommit is not actually a commit. The object pool of this walker would
		/// also be 'poisoned' by the non-commit RevCommit.
		/// </summary>
		/// <param name="c">
		/// The commit to start traversing from. The commit passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="MissingObjectException">
		/// The commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to <seealso cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <seealso cref="lookupCommit(AnyObjectId)"/>.
		/// </exception>
		/// <exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public void markUninteresting(RevCommit c)
		{
			c.Flags |= UNINTERESTING;
			carryFlagsImpl(c);
			markStart(c);
		}

		///	<summary>
		/// Determine if a commit is reachable from another commit.
		/// <para />
		/// A commit <code>base</code> is an ancestor of <code>tip</code> if we
		/// can find a path of commits that leads from <code>tip</code> and ends at
		/// <code>base</code>.
		/// <para />
		/// This utility function resets the walker, inserts the two supplied
		/// commits, and then executes a walk until an answer can be obtained.
		/// Currently allocated RevFlags that have been added to RevCommit instances
		/// will be retained through the reset.
		/// </summary>
		/// <param name="base">
		/// commit the caller thinks is reachable from <code>tip</code>.
		/// </param>
		/// <param name="tip">
		/// commit to start iteration from, and which is most likely a
		/// descendant (child) of <code>base</code>.
		/// </param>
		/// <returns>
		/// true if there is a path directly from <code>tip</code> to
		/// <code>base</code> (and thus <code>base</code> is fully merged
		/// into <code>tip</code>); false otherwise.
		/// </returns>
		/// <exception cref="MissingObjectException">
		/// one or or more of the next commit's parents are not available
		/// from the object database, but were thought to be candidates
		/// for traversal. This usually indicates a broken link.
		/// </exception>
		///	<exception cref="IncorrectObjectTypeException">
		/// one or or more of the next commit's parents are not actually
		/// commit objects.
		/// </exception>
		///	<exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public bool isMergedInto(RevCommit @base, RevCommit tip)
		{
			RevFilter oldRF = _filter;
			TreeFilter oldTF = _treeFilter;
			try
			{
				FinishDelayedFreeFlags();
				reset(~_freeFlags & AppFlags);
				_filter = RevFilter.MERGE_BASE;
				_treeFilter = TreeFilter.ALL;
				markStart(tip);
				markStart(@base);
				return (next() == @base);
			}
			finally
			{
				_filter = oldRF;
				_treeFilter = oldTF;
			}
		}

		///	<summary>
		/// Pop the next most recent commit.
		/// </summary>
		/// <returns>
		/// Next most recent commit; null if traversal is over.
		/// </returns>
		/// <exception cref="MissingObjectException">
		/// one or or more of the next commit's parents are not available
		/// from the object database, but were thought to be candidates
		/// for traversal. This usually indicates a broken link.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// one or or more of the next commit's parents are not actually
		/// commit objects.
		/// </exception>
		/// <exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public virtual RevCommit next()
		{
			return Pending.next();
		}

		///	<summary>
		/// Obtain the sort types applied to the commits returned.
		/// </summary>
		/// <returns>
		/// The sorting strategies employed. At least one strategy is always
		/// used, but that strategy may be <seealso cref="RevSort.NONE"/>.
		/// </returns>
		public HashSet<RevSort.Strategy> RevSortStrategy
		{
			get { return new HashSet<RevSort.Strategy>(_sorting); }
		}

		///	<summary>
		/// Check whether the provided sorting strategy is enabled.
		/// </summary>
		/// <param name="sort">
		/// a sorting strategy to look for.
		/// </param>
		/// <returns>
		/// True if this strategy is enabled, false otherwise
		/// </returns>
		public bool hasRevSort(RevSort.Strategy sort)
		{
			return _sorting.Contains(sort);
		}

		///	<summary>
		/// Select a single sorting strategy for the returned commits.
		/// <para />
		/// Disables all sorting strategies, then enables only the single strategy
		/// supplied by the caller.
		/// </summary>
		/// <param name="s">a sorting strategy to enable.</param>
		public void sort(RevSort.Strategy s)
		{
			assertNotStarted();
			_sorting.Clear();
			_sorting.Add(s);
		}

		///	<summary>
		/// Add or remove a sorting strategy for the returned commits.
		/// <para />
		/// Multiple strategies can be applied at once, in which case some strategies
		/// may take precedence over others. As an example, <seealso cref="RevSort.TOPO"/> must
		/// take precedence over <seealso cref="RevSort.NONE"/>, otherwise it
		/// cannot enforce its ordering.
		/// </summary>
		/// <param name="s">A sorting strategy to enable or disable.</param>
		///	<param name="use">
		/// true if this strategy should be used, false if it should be
		/// removed.
		/// </param>
		public virtual void sort(RevSort.Strategy s, bool use)
		{
			assertNotStarted();
			if (use)
			{
				_sorting.Add(s);
			}
			else
			{
				_sorting.Remove(s);
			}

			if (_sorting.Count > 1)
			{
				_sorting.Remove(RevSort.NONE);
			}
			else if (_sorting.Count == 0)
			{
				_sorting.Add(RevSort.NONE);
			}
		}

		/// <summary>
		/// Get the currently configured commit filter.
		/// </summary>
		/// <returns>
		/// Return the current filter. Never null as a filter is always needed.
		/// </returns>
		public RevFilter getRevFilter()
		{
			return _filter;
		}

		///	<summary>
		/// Set the commit filter for this walker.
		/// <para />
		/// Multiple filters may be combined by constructing an arbitrary tree of
		/// <seealso cref="AndRevFilter"/> or <seealso cref="OrRevFilter"/> instances to
		/// describe the boolean expression required by the application. Custom
		/// filter implementations may also be constructed by applications.
		/// <para />
		/// Note that filters are not thread-safe and may not be shared by concurrent
		/// RevWalk instances. Every RevWalk must be supplied its own unique filter,
		/// unless the filter implementation specifically states it is (and always
		/// will be) thread-safe. Callers may use <seealso cref="RevFilter.Clone()"/> to create
		/// a unique filter tree for this RevWalk instance.
		/// </summary>
		/// <param name="newFilter">
		/// The new filter. If null the special <seealso cref="RevFilter.ALL"/>
		/// filter will be used instead, as it matches every commit.
		/// </param>
		/// <seealso cref="AndRevFilter" />
		/// <seealso cref="OrRevFilter" />
		public void setRevFilter(RevFilter newFilter)
		{
			assertNotStarted();
			_filter = newFilter ?? RevFilter.ALL;
		}

		///	<summary>
		/// Get the tree filter used to simplify commits by modified paths.
		/// </summary>
		/// <returns>
		/// The current filter. Never null as a filter is always needed. If
		/// no filter is being applied <seealso cref="TreeFilter.ALL"/> is returned.
		/// </returns>
		public TreeFilter getTreeFilter()
		{
			return _treeFilter;
		}

		///	<summary>
		/// Set the tree filter used to simplify commits by modified paths.
		/// <para />
		/// If null or <seealso cref="TreeFilter.ALL"/> the path limiter is removed. Commits
		/// will not be simplified.
		/// <para />
		/// If non-null and not <seealso cref="TreeFilter.ALL"/> then the tree filter will be
		/// installed and commits will have their ancestry simplified to hide commits
		/// that do not contain tree entries matched by the filter.
		/// <para />
		/// Usually callers should be inserting a filter graph including
		/// <seealso cref="TreeFilter.ANY_DIFF"/> along with one or more
		/// <seealso cref="PathFilter"/> instances.
		/// </summary>
		/// <param name="newFilter">
		/// New filter. If null the special <seealso cref="TreeFilter.ALL"/> filter
		/// will be used instead, as it matches everything.
		/// </param>
		/// <seealso cref="PathFilter"/>
		public void setTreeFilter(TreeFilter newFilter)
		{
			assertNotStarted();
			_treeFilter = newFilter ?? TreeFilter.ALL;
		}

		///	<summary>
		/// Should the body of a commit or tag be retained after parsing its headers?
		/// <para />
		/// Usually the body is always retained, but some application code might not
		/// care and would prefer to discard the body of a commit as early as
		/// possible, to reduce memory usage.
		/// </summary>
		/// <returns> true if the body should be retained; false it is discarded. </returns>
		public bool isRetainBody()
		{
			return _retainBody;
		}

		///	<summary>
		/// Set whether or not the body of a commit or tag is retained.
		/// <para />
		/// If a body of a commit or tag is not retained, the application must
		/// call <seealso cref="parseBody(RevObject)"/> before the body can be safely
		/// accessed through the type specific access methods.
		/// </summary>
		/// <param name="retain">True to retain bodies; false to discard them early.</param>
		public void setRetainBody(bool retain)
		{
			_retainBody = retain;
		}

		///	<summary>
		/// Locate a reference to a blob without loading it.
		/// <para />
		/// The blob may or may not exist in the repository. It is impossible to tell
		/// from this method's return value.
		/// </summary>
		/// <param name="id">name of the blob object.</param>
		/// <returns>Reference to the blob object. Never null.</returns>
		public RevBlob lookupBlob(AnyObjectId id)
		{
			var c = (RevBlob)_objects.Get(id);
			if (c == null)
			{
				c = new RevBlob(id);
				_objects.Add(c);
			}
			return c;
		}

		///	<summary>
		/// Locate a reference to a tree without loading it.
		/// <para />
		/// The tree may or may not exist in the repository. It is impossible to tell
		/// from this method's return value.
		/// </summary>
		/// <param name="id">Name of the tree object.</param>
		///	<returns>Reference to the tree object. Never null.</returns>
		public RevTree lookupTree(AnyObjectId id)
		{
			var c = (RevTree)_objects.Get(id);
			if (c == null)
			{
				c = new RevTree(id);
				_objects.Add(c);
			}
			return c;
		}

		///	<summary>
		/// Locate a reference to a commit without loading it.
		/// <para />
		/// The commit may or may not exist in the repository. It is impossible to
		/// tell from this method's return value.
		/// </summary>
		/// <param name="id">name of the commit object.</param>
		/// <returns> reference to the commit object. Never null.</returns>
		public RevCommit lookupCommit(AnyObjectId id)
		{
			var c = (RevCommit)_objects.Get(id);
			if (c == null)
			{
				c = createCommit(id);
				_objects.Add(c);
			}
			return c;
		}

		///	<summary>
		/// Locate a reference to any object without loading it.
		/// <para />
		/// The object may or may not exist in the repository. It is impossible to
		/// tell from this method's return value.
		/// </summary>
		/// <param name="id">name of the object.</param>
		/// <param name="type">
		/// type of the object. Must be a valid Git object type.
		/// </param>
		/// <returns>Reference to the object. Never null.
		/// </returns>
		public RevObject lookupAny(AnyObjectId id, int type)
		{
			RevObject r = _objects.Get(id);
			if (r == null)
			{
				switch (type)
				{
					case Constants.OBJ_COMMIT:
						r = createCommit(id);
						break;

					case Constants.OBJ_TREE:
						r = new RevTree(id);
						break;

					case Constants.OBJ_BLOB:
						r = new RevBlob(id);
						break;

					case Constants.OBJ_TAG:
						r = new RevTag(id);
						break;

					default:
						throw new ArgumentException("invalid git type: " + type);
				}

				_objects.Add(r);
			}
			return r;
		}

		///	<summary>
		/// Locate a reference to a commit and immediately parse its content.
		/// <para />
		/// Unlike <seealso cref="lookupCommit(AnyObjectId)"/> this method only returns
		/// successfully if the commit object exists, is verified to be a commit, and
		/// was parsed without error.
		/// </summary>
		/// <param name="id">name of the commit object.</param>
		/// <returns>reference to the commit object. Never null.</returns>
		/// <exception cref="MissingObjectException">
		/// the supplied commit does not exist.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// the supplied id is not a commit or an annotated tag.
		/// </exception>
		///	<exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public RevCommit parseCommit(AnyObjectId id)
		{
			RevObject c = parseAny(id);
			RevTag oTag = (c as RevTag);
			while (oTag != null)
			{
				c = oTag.getObject();
				parseHeaders(c);
			}

			RevCommit oComm = (c as RevCommit);
			if (oComm == null)
			{
				throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_COMMIT);
			}

			return oComm;
		}

		///	<summary>
		/// Locate a reference to a tree.
		/// <para />
		/// This method only returns successfully if the tree object exists, is
		/// verified to be a tree.
		/// </summary>
		/// <param name="id">
		/// Name of the tree object, or a commit or annotated tag that may
		/// reference a tree.
		/// </param>
		/// <returns>Reference to the tree object. Never null.</returns>
		/// <exception cref="MissingObjectException">
		/// The supplied tree does not exist.
		/// </exception>
		/// <exception cref="IncorrectObjectTypeException">
		/// The supplied id is not a tree, a commit or an annotated tag.
		/// </exception>
		/// <exception cref="IOException">
		/// A pack file or loose object could not be read.
		/// </exception>
		public RevTree parseTree(AnyObjectId id)
		{
			RevObject c = parseAny(id);
			RevTag oTag = (c as RevTag);
			while (oTag != null)
			{
				c = oTag.getObject();
				parseHeaders(c);
			}

			RevTree t = (c as RevTree);
			if ( t == null)
			{
				RevCommit oComm = (c as RevCommit);
				if (oComm != null)
				{
					t = oComm.Tree;
				}
				else if (t == null)
				{
					throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_TREE);
				}
			}

			parseHeaders(t);

			return t;
		}

		///	<summary>
		/// Locate a reference to any object and immediately parse its headers.
		/// <para />
		/// This method only returns successfully if the object exists and was parsed
		/// without error. Parsing an object can be expensive as the type must be
		/// determined. For blobs this may mean the blob content was unpacked
		/// unnecessarily, and thrown away.
		/// </summary>
		/// <param name="id">Name of the object.</param>
		/// <returns>Reference to the object. Never null.</returns>
		/// <exception cref="MissingObjectException">the supplied does not exist.</exception>
		/// <exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public RevObject parseAny(AnyObjectId id)
		{
			RevObject r = _objects.Get(id);
			if (r == null)
			{
				ObjectLoader ldr = _db.OpenObject(_curs, id);
				if (ldr == null)
				{
					throw new MissingObjectException(id.ToObjectId(), "unknown");
				}

				byte[] data = ldr.CachedBytes;
				int type = ldr.Type;
				switch (type)
				{
					case Constants.OBJ_COMMIT:
						{
							RevCommit c = createCommit(id);
							c.parseCanonical(this, data);
							r = c;
							break;
						}
					case Constants.OBJ_TREE:
						{
							r = new RevTree(id);
							r.Flags |= PARSED;
							break;
						}
					case Constants.OBJ_BLOB:
						{
							r = new RevBlob(id);
							r.Flags |= PARSED;
							break;
						}
					case Constants.OBJ_TAG:
						{
							var t = new RevTag(id);
							t.parseCanonical(this, data);
							r = t;
							break;
						}
					default:
						throw new ArgumentException("Bad object type: " + type);
				}
				_objects.Add(r);
			}
			else
			{
				parseHeaders(r);
			}
			return r;
		}

		///	<summary>
		/// Ensure the object's critical headers have been parsed.
		///	<para />
		///	This method only returns successfully if the object exists and was parsed
		///	without error.
		///	</summary>
		///	<param name="obj">The object the caller needs to be parsed.</param>
		///	<exception cref="MissingObjectException">
		/// The supplied does not exist.
		/// </exception>
		///	<exception cref="IOException">
		/// A pack file or loose object could not be read.
		/// </exception>
		public void parseHeaders(RevObject obj)
		{
			if ((obj.Flags & PARSED) == 0)
			{
				obj.parseHeaders(this);
			}
		}

		///	<summary> * Ensure the object's fully body content is available.
		///	<para />
		///	This method only returns successfully if the object exists and was parsed
		///	without error.
		///	</summary>
		///	<param name="obj">the object the caller needs to be parsed.</param>
		/// <exception cref="MissingObjectException">
		/// the supplied does not exist.
		/// </exception>
		/// <exception cref="IOException">
		/// a pack file or loose object could not be read.
		/// </exception>
		public void parseBody(RevObject obj)
		{
			obj.parseBody(this);
		}

		///	<summary>
		/// Create a new flag for application use during walking.
		/// <para />
		/// Applications are only assured to be able to create 24 unique flags on any
		/// given revision walker instance. Any flags beyond 24 are offered only if
		/// the implementation has extra free space within its internal storage.
		/// </summary>
		/// <param name="name">
		/// description of the flag, primarily useful for debugging.
		/// </param>
		/// <returns> newly constructed flag instance. </returns>
		/// <exception cref="ArgumentException">
		/// too many flags have been reserved on this revision walker.
		/// </exception>
		public RevFlag newFlag(string name)
		{
			int m = allocFlag();
			return new RevFlag(this, name, m);
		}

		public int allocFlag()
		{
			if (_freeFlags == 0)
			{
				throw new ArgumentException(32 - ReservedFlags + " flags already created.");
			}
			int m = _freeFlags.LowestOneBit();
			_freeFlags &= ~m;
			return m;
		}

		///	<summary>
		/// Automatically carry a flag from a child commit to its parents.
		/// <para />
		/// A carried flag is copied from the child commit onto its parents when the
		/// child commit is popped from the lowest level of walk's internal graph.
		/// </summary>
		/// <param name="flag">
		/// The flag to carry onto parents, if set on a descendant.
		/// </param>
		public void carry(RevFlag flag)
		{
			if ((_freeFlags & flag.Mask) != 0)
				throw new ArgumentException(flag.Name + " is disposed.");
			if (flag.Walker != this)
				throw new ArgumentException(flag.Name + " not from this.");
			_carryFlags |= flag.Mask;
		}

		///	<summary>
		/// Automatically carry flags from a child commit to its parents.
		///	<para />
		///	A carried flag is copied from the child commit onto its parents when the
		///	child commit is popped from the lowest level of walk's internal graph.
		///	</summary>
		///	<param name="set">
		///	The flags to carry onto parents, if set on a descendant.
		/// </param>
		public void carry(IEnumerable<RevFlag> set)
		{
			foreach (RevFlag flag in set)
				carry(flag);
		}

		///	<summary>
		/// Allow a flag to be recycled for a different use.
		/// <para />
		/// Recycled flags always come back as a different Java object instance when
		/// assigned again by <seealso cref="newFlag(string)"/>.
		/// <para />
		/// If the flag was previously being carried, the carrying request is
		/// removed. Disposing of a carried flag while a traversal is in progress has
		/// an undefined behavior.
		/// </summary>
		/// <param name="flag">the to recycle.</param>
		public void disposeFlag(RevFlag flag)
		{
			freeFlag(flag.Mask);
		}

		internal void freeFlag(int mask)
		{
			if (IsNotStarted())
			{
				_freeFlags |= mask;
				_carryFlags &= ~mask;
			}
			else
			{
				_delayFreeFlags |= mask;
			}
		}

		private void FinishDelayedFreeFlags()
		{
			if (_delayFreeFlags == 0) return;
			_freeFlags |= _delayFreeFlags;
			_carryFlags &= ~_delayFreeFlags;
			_delayFreeFlags = 0;
		}

		///	<summary>
		/// Resets internal state and allows this instance to be used again.
		/// <para />
		/// Unlike <seealso cref="Dispose()"/> previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </summary>
		public void reset()
		{
			reset(0);
		}

		///	<summary>
		/// Resets internal state and allows this instance to be used again.
		/// <para />
		/// Unlike <seealso cref="Dispose()"/> previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </summary>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		public void resetRetain(RevFlagSet retainFlags)
		{
			reset(retainFlags.Mask);
		}

		///	<summary>
		/// Resets internal state and allows this instance to be used again.
		/// <para />
		/// Unlike <seealso cref="Dispose()"/> previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </summary>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		public void resetRetain(params RevFlag[] retainFlags)
		{
			int mask = 0;
			foreach (RevFlag flag in retainFlags)
				mask |= flag.Mask;
			reset(mask);
		}

		///	<summary>
		/// Resets internal state and allows this instance to be used again.
		/// <para />
		/// Unlike <seealso cref="Dispose()"/> previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </summary>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		internal virtual void reset(int retainFlags)
		{
			FinishDelayedFreeFlags();
			retainFlags |= PARSED;
			int clearFlags = ~retainFlags;

			var q = new FIFORevQueue();
			foreach (RevCommit c in _roots)
			{
				if ((c.Flags & clearFlags) == 0) continue;
				c.Flags &= retainFlags;
				c.reset();
				q.add(c);
			}

			while (true)
			{
				RevCommit c = q.next();
				if (c == null) break;
				if (c.Parents == null) continue;

				foreach (RevCommit p in c.Parents)
				{
					if ((p.Flags & clearFlags) == 0) continue;
					p.Flags &= retainFlags;
					p.reset();
					q.add(p);
				}
			}

			_curs.Release();
			_roots.Clear();
			Queue = new DateRevQueue();
			Pending = new StartGenerator(this);
		}

		///	<summary>
		/// Returns an Iterator over the commits of this walker.
		/// <para />
		/// The returned iterator is only useful for one walk. If this RevWalk gets
		/// reset a new iterator must be obtained to walk over the new results.
		/// <para />
		/// Applications must not use both the Iterator and the <seealso cref="next()"/> API
		/// at the same time. Pick one API and use that for the entire walk.
		/// <para />
		/// If a checked exception is thrown during the walk (see <seealso cref="next()"/>)
		/// it is rethrown from the Iterator as a <seealso cref="RevWalkException"/>.
		/// </summary>
		/// <returns> an iterator over this walker's commits. </returns>
		/// <seealso cref="RevWalkException"/>
		public Iterator<RevCommit> iterator()
		{
			return new Iterator<RevCommit>(this);
		}

		public class Iterator<T> : IEnumerator<T>
			where T : RevCommit
		{
			private T _first;
			private T _next;
		    private T _current;
			private RevWalk _revwalk;

			public Iterator(RevWalk revwalk)
			{
				_revwalk = revwalk;

				try
				{
					_first = _next = (T)revwalk.next();
                }
                catch (MissingObjectException e)
                {
                    throw new RevWalkException(e);
                }
                catch (IncorrectObjectTypeException e)
                {
                    throw new RevWalkException(e);
				}
				catch (IOException e)
				{
					throw new RevWalkException(e);
				}
			}

			public T Current
			{
				get { return _current; }
			}

			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}

            public bool MoveNext()
            {
                if (!hasNext())
                {
                    _current = null;
                    return false;
                }

                _current = (T)next();
                return true;
            }

            public bool hasNext()
            {
                return _next != default(T);
            }

		    public RevCommit next()
		    {
		        try
		        {
		            RevCommit r = _next;
		            _next = (T)_revwalk.next();
		            return r;
		        }
		        catch (MissingObjectException e)
		        {
		            throw new RevWalkException(e);
		        }
		        catch (IncorrectObjectTypeException e)
		        {
		            throw new RevWalkException(e);
		        }
		        catch (IOException e)
		        {
		            throw new RevWalkException(e);
		        }
		    }

			public void Reset()
			{
				_next = _first;
                _current = null;
			}

			#region IDisposable Members

			public void Dispose()
			{
				_first = null;
				_next = null;
				_revwalk.Dispose();
			    _revwalk = null;
			}

			#endregion
		}

		/// <summary>
		/// Throws an exception if we have started producing output.
		/// </summary>
		internal void assertNotStarted()
		{
			if (IsNotStarted()) return;
			throw new InvalidOperationException("Output has already been started.");
		}

		private bool IsNotStarted()
		{
			return Pending is StartGenerator;
		}

		///	<summary>
		/// Construct a new unparsed commit for the given object.
		/// </summary>
		/// <param name="id">
		/// the object this walker requires a commit reference for.
		/// </param>
		/// <returns> a new unparsed reference for the object.
		/// </returns>
		protected virtual RevCommit createCommit(AnyObjectId id)
		{
			return new RevCommit(id);
		}

		internal void carryFlagsImpl(RevCommit c)
		{
			int carry = c.Flags & _carryFlags;
			if (carry != 0)
			{
				RevCommit.carryFlags(c, carry);
			}
		}

		#region IEnumerable<RevCommit> Members

		public IEnumerator<RevCommit> GetEnumerator()
		{
			return iterator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return iterator();
		}

		#endregion

		#region IDisposable Members

		///	<summary>
		/// Dispose all internal state and invalidate all RevObject instances.
		/// <para />
		/// All RevObject (and thus RevCommit, etc.) instances previously acquired
		/// from this RevWalk are invalidated by a dispose call. Applications must
		/// not retain or use RevObject instances obtained prior to the dispose call.
		/// All RevFlag instances are also invalidated, and must not be reused.
		/// </summary>
		public virtual void Dispose()
		{
			_freeFlags = AppFlags;
			_delayFreeFlags = 0;
			_carryFlags = UNINTERESTING;
			_objects.Clear();
			_curs.Release();
			_roots.Clear();
			Queue = new DateRevQueue();
			Pending = new StartGenerator(this);
		}

		#endregion
	}
}