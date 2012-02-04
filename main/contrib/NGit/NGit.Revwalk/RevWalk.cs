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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Revwalk
{
	/// <summary>Walks a commit graph and produces the matching commits in order.</summary>
	/// <remarks>
	/// Walks a commit graph and produces the matching commits in order.
	/// <p>
	/// A RevWalk instance can only be used once to generate results. Running a
	/// second time requires creating a new RevWalk instance, or invoking
	/// <see cref="Reset()">Reset()</see>
	/// before starting again. Resetting an existing instance may be
	/// faster for some applications as commit body parsing can be avoided on the
	/// later invocations.
	/// <p>
	/// RevWalk instances are not thread-safe. Applications must either restrict
	/// usage of a RevWalk instance to a single thread, or implement their own
	/// synchronization at a higher level.
	/// <p>
	/// Multiple simultaneous RevWalk instances per
	/// <see cref="NGit.Repository">NGit.Repository</see>
	/// are permitted,
	/// even from concurrent threads. Equality of
	/// <see cref="RevCommit">RevCommit</see>
	/// s from two
	/// different RevWalk instances is never true, even if their
	/// <see cref="NGit.ObjectId">NGit.ObjectId</see>
	/// s
	/// are equal (and thus they describe the same commit).
	/// <p>
	/// The offered iterator is over the list of RevCommits described by the
	/// configuration of this instance. Applications should restrict themselves to
	/// using either the provided Iterator or
	/// <see cref="Next()">Next()</see>
	/// , but never use both on
	/// the same RevWalk at the same time. The Iterator may buffer RevCommits, while
	/// <see cref="Next()">Next()</see>
	/// does not.
	/// </remarks>
	public class RevWalk : Iterable<RevCommit>
	{
		private const int MB = 1 << 20;

		/// <summary>Set on objects whose important header data has been loaded.</summary>
		/// <remarks>
		/// Set on objects whose important header data has been loaded.
		/// <p>
		/// For a RevCommit this indicates we have pulled apart the tree and parent
		/// references from the raw bytes available in the repository and translated
		/// those to our own local RevTree and RevCommit instances. The raw buffer is
		/// also available for message and other header filtering.
		/// <p>
		/// For a RevTag this indicates we have pulled part the tag references to
		/// find out who the tag refers to, and what that object's type is.
		/// </remarks>
		internal const int PARSED = 1 << 0;

		/// <summary>
		/// Set on RevCommit instances added to our
		/// <see cref="pending">pending</see>
		/// queue.
		/// <p>
		/// We use this flag to avoid adding the same commit instance twice to our
		/// queue, especially if we reached it by more than one path.
		/// </summary>
		internal const int SEEN = 1 << 1;

		/// <summary>Set on RevCommit instances the caller does not want output.</summary>
		/// <remarks>
		/// Set on RevCommit instances the caller does not want output.
		/// <p>
		/// We flag commits as uninteresting if the caller does not want commits
		/// reachable from a commit given to
		/// <see cref="MarkUninteresting(RevCommit)">MarkUninteresting(RevCommit)</see>
		/// .
		/// This flag is always carried into the commit's parents and is a key part
		/// of the "rev-list B --not A" feature; A is marked UNINTERESTING.
		/// </remarks>
		internal const int UNINTERESTING = 1 << 2;

		/// <summary>Set on a RevCommit that can collapse out of the history.</summary>
		/// <remarks>
		/// Set on a RevCommit that can collapse out of the history.
		/// <p>
		/// If the
		/// <see cref="treeFilter">treeFilter</see>
		/// concluded that this commit matches his
		/// parents' for all of the paths that the filter is interested in then we
		/// mark the commit REWRITE. Later we can rewrite the parents of a REWRITE
		/// child to remove chains of REWRITE commits before we produce the child to
		/// the application.
		/// </remarks>
		/// <seealso cref="RewriteGenerator">RewriteGenerator</seealso>
		internal const int REWRITE = 1 << 3;

		/// <summary>Temporary mark for use within generators or filters.</summary>
		/// <remarks>
		/// Temporary mark for use within generators or filters.
		/// <p>
		/// This mark is only for local use within a single scope. If someone sets
		/// the mark they must unset it before any other code can see the mark.
		/// </remarks>
		internal const int TEMP_MARK = 1 << 4;

		/// <summary>
		/// Temporary mark for use within
		/// <see cref="TopoSortGenerator">TopoSortGenerator</see>
		/// .
		/// <p>
		/// This mark indicates the commit could not produce when it wanted to, as at
		/// least one child was behind it. Commits with this flag are delayed until
		/// all children have been output first.
		/// </summary>
		internal const int TOPO_DELAY = 1 << 5;

		/// <summary>Number of flag bits we keep internal for our own use.</summary>
		/// <remarks>Number of flag bits we keep internal for our own use. See above flags.</remarks>
		internal const int RESERVED_FLAGS = 6;

		private const int APP_FLAGS = -1 & ~((1 << RESERVED_FLAGS) - 1);

		/// <summary>Exists <b>ONLY</b> to support legacy Tag and Commit objects.</summary>
		/// <remarks>Exists <b>ONLY</b> to support legacy Tag and Commit objects.</remarks>
		internal readonly Repository repository;

		internal readonly ObjectReader reader;

		internal readonly MutableObjectId idBuffer;

		internal ObjectIdOwnerMap<RevObject> objects;

		private int freeFlags = APP_FLAGS;

		private int delayFreeFlags;

		internal int carryFlags = UNINTERESTING;

		internal readonly AList<RevCommit> roots;

		internal AbstractRevQueue queue;

		internal Generator pending;

		private readonly EnumSet<RevSort> sorting;

		private RevFilter filter;

		private TreeFilter treeFilter;

		private bool retainBody;

		/// <summary>Create a new revision walker for a given repository.</summary>
		/// <remarks>Create a new revision walker for a given repository.</remarks>
		/// <param name="repo">
		/// the repository the walker will obtain data from. An
		/// ObjectReader will be created by the walker, and must be
		/// released by the caller.
		/// </param>
		public RevWalk(Repository repo) : this(repo, repo.NewObjectReader())
		{
		}

		/// <summary>Create a new revision walker for a given repository.</summary>
		/// <remarks>Create a new revision walker for a given repository.</remarks>
		/// <param name="or">
		/// the reader the walker will obtain data from. The reader should
		/// be released by the caller when the walker is no longer
		/// required.
		/// </param>
		public RevWalk(ObjectReader or) : this(null, or)
		{
		}

		private RevWalk(Repository repo, ObjectReader or)
		{
			repository = repo;
			reader = or;
			idBuffer = new MutableObjectId();
			objects = new ObjectIdOwnerMap<RevObject>();
			roots = new AList<RevCommit>();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
			sorting = EnumSet.Of(RevSort.NONE);
			filter = RevFilter.ALL;
			treeFilter = TreeFilter.ALL;
			retainBody = true;
		}

		/// <returns>the reader this walker is using to load objects.</returns>
		public virtual ObjectReader GetObjectReader()
		{
			return reader;
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

		/// <summary>Mark a commit to start graph traversal from.</summary>
		/// <remarks>
		/// Mark a commit to start graph traversal from.
		/// <p>
		/// Callers are encouraged to use
		/// <see cref="ParseCommit(NGit.AnyObjectId)">ParseCommit(NGit.AnyObjectId)</see>
		/// to obtain
		/// the commit reference, rather than
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// , as
		/// this method requires the commit to be parsed before it can be added as a
		/// root for the traversal.
		/// <p>
		/// The method will automatically parse an unparsed commit, but error
		/// handling may be more difficult for the application to explain why a
		/// RevCommit is not actually a commit. The object pool of this walker would
		/// also be 'poisoned' by the non-commit RevCommit.
		/// </remarks>
		/// <param name="c">
		/// the commit to start traversing from. The commit passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void MarkStart(RevCommit c)
		{
			if ((c.flags & SEEN) != 0)
			{
				return;
			}
			if ((c.flags & PARSED) == 0)
			{
				c.ParseHeaders(this);
			}
			c.flags |= SEEN;
			roots.AddItem(c);
			queue.Add(c);
		}

		/// <summary>Mark commits to start graph traversal from.</summary>
		/// <remarks>Mark commits to start graph traversal from.</remarks>
		/// <param name="list">
		/// commits to start traversing from. The commits passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// one of the commits supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void MarkStart(ICollection<RevCommit> list)
		{
			foreach (RevCommit c in list)
			{
				MarkStart(c);
			}
		}

		/// <summary>Mark a commit to not produce in the output.</summary>
		/// <remarks>
		/// Mark a commit to not produce in the output.
		/// <p>
		/// Uninteresting commits denote not just themselves but also their entire
		/// ancestry chain, back until the merge base of an uninteresting commit and
		/// an otherwise interesting commit.
		/// <p>
		/// Callers are encouraged to use
		/// <see cref="ParseCommit(NGit.AnyObjectId)">ParseCommit(NGit.AnyObjectId)</see>
		/// to obtain
		/// the commit reference, rather than
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// , as
		/// this method requires the commit to be parsed before it can be added as a
		/// root for the traversal.
		/// <p>
		/// The method will automatically parse an unparsed commit, but error
		/// handling may be more difficult for the application to explain why a
		/// RevCommit is not actually a commit. The object pool of this walker would
		/// also be 'poisoned' by the non-commit RevCommit.
		/// </remarks>
		/// <param name="c">
		/// the commit to start traversing from. The commit passed must be
		/// from this same revision walker.
		/// </param>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// the commit supplied is not available from the object
		/// database. This usually indicates the supplied commit is
		/// invalid, but the reference was constructed during an earlier
		/// invocation to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// the object was not parsed yet and it was discovered during
		/// parsing that it is not actually a commit. This usually
		/// indicates the caller supplied a non-commit SHA-1 to
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// .
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void MarkUninteresting(RevCommit c)
		{
			c.flags |= UNINTERESTING;
			CarryFlagsImpl(c);
			MarkStart(c);
		}

		/// <summary>Determine if a commit is reachable from another commit.</summary>
		/// <remarks>
		/// Determine if a commit is reachable from another commit.
		/// <p>
		/// A commit <code>base</code> is an ancestor of <code>tip</code> if we
		/// can find a path of commits that leads from <code>tip</code> and ends at
		/// <code>base</code>.
		/// <p>
		/// This utility function resets the walker, inserts the two supplied
		/// commits, and then executes a walk until an answer can be obtained.
		/// Currently allocated RevFlags that have been added to RevCommit instances
		/// will be retained through the reset.
		/// </remarks>
		/// <param name="base">commit the caller thinks is reachable from <code>tip</code>.</param>
		/// <param name="tip">
		/// commit to start iteration from, and which is most likely a
		/// descendant (child) of <code>base</code>.
		/// </param>
		/// <returns>
		/// true if there is a path directly from <code>tip</code> to
		/// <code>base</code> (and thus <code>base</code> is fully merged
		/// into <code>tip</code>); false otherwise.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// one or or more of the next commit's parents are not available
		/// from the object database, but were thought to be candidates
		/// for traversal. This usually indicates a broken link.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one or or more of the next commit's parents are not actually
		/// commit objects.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual bool IsMergedInto(RevCommit @base, RevCommit tip)
		{
			RevFilter oldRF = filter;
			TreeFilter oldTF = treeFilter;
			try
			{
				FinishDelayedFreeFlags();
				Reset(~freeFlags & APP_FLAGS);
				filter = RevFilter.MERGE_BASE;
				treeFilter = TreeFilter.ALL;
				MarkStart(tip);
				MarkStart(@base);
				return Next() == @base;
			}
			finally
			{
				filter = oldRF;
				treeFilter = oldTF;
			}
		}

		/// <summary>Pop the next most recent commit.</summary>
		/// <remarks>Pop the next most recent commit.</remarks>
		/// <returns>next most recent commit; null if traversal is over.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">
		/// one or or more of the next commit's parents are not available
		/// from the object database, but were thought to be candidates
		/// for traversal. This usually indicates a broken link.
		/// </exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one or or more of the next commit's parents are not actually
		/// commit objects.
		/// </exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevCommit Next()
		{
			return pending.Next();
		}

		/// <summary>Obtain the sort types applied to the commits returned.</summary>
		/// <remarks>Obtain the sort types applied to the commits returned.</remarks>
		/// <returns>
		/// the sorting strategies employed. At least one strategy is always
		/// used, but that strategy may be
		/// <see cref="RevSort.NONE">RevSort.NONE</see>
		/// .
		/// </returns>
		public virtual EnumSet<RevSort> GetRevSort()
		{
			return sorting.Clone();
		}

		/// <summary>Check whether the provided sorting strategy is enabled.</summary>
		/// <remarks>Check whether the provided sorting strategy is enabled.</remarks>
		/// <param name="sort">a sorting strategy to look for.</param>
		/// <returns>true if this strategy is enabled, false otherwise</returns>
		public virtual bool HasRevSort(RevSort sort)
		{
			return sorting.Contains(sort);
		}

		/// <summary>Select a single sorting strategy for the returned commits.</summary>
		/// <remarks>
		/// Select a single sorting strategy for the returned commits.
		/// <p>
		/// Disables all sorting strategies, then enables only the single strategy
		/// supplied by the caller.
		/// </remarks>
		/// <param name="s">a sorting strategy to enable.</param>
		public virtual void Sort(RevSort s)
		{
			AssertNotStarted();
			sorting.Clear();
			sorting.AddItem(s);
		}

		/// <summary>Add or remove a sorting strategy for the returned commits.</summary>
		/// <remarks>
		/// Add or remove a sorting strategy for the returned commits.
		/// <p>
		/// Multiple strategies can be applied at once, in which case some strategies
		/// may take precedence over others. As an example,
		/// <see cref="RevSort.TOPO">RevSort.TOPO</see>
		/// must
		/// take precedence over
		/// <see cref="RevSort.COMMIT_TIME_DESC">RevSort.COMMIT_TIME_DESC</see>
		/// , otherwise it
		/// cannot enforce its ordering.
		/// </remarks>
		/// <param name="s">a sorting strategy to enable or disable.</param>
		/// <param name="use">
		/// true if this strategy should be used, false if it should be
		/// removed.
		/// </param>
		public virtual void Sort(RevSort s, bool use)
		{
			AssertNotStarted();
			if (use)
			{
				sorting.AddItem(s);
			}
			else
			{
				sorting.Remove(s);
			}
			if (sorting.Count > 1)
			{
				sorting.Remove(RevSort.NONE);
			}
			else
			{
				if (sorting.Count == 0)
				{
					sorting.AddItem(RevSort.NONE);
				}
			}
		}

		/// <summary>Get the currently configured commit filter.</summary>
		/// <remarks>Get the currently configured commit filter.</remarks>
		/// <returns>the current filter. Never null as a filter is always needed.</returns>
		public virtual RevFilter GetRevFilter()
		{
			return filter;
		}

		/// <summary>Set the commit filter for this walker.</summary>
		/// <remarks>
		/// Set the commit filter for this walker.
		/// <p>
		/// Multiple filters may be combined by constructing an arbitrary tree of
		/// <code>AndRevFilter</code> or <code>OrRevFilter</code> instances to
		/// describe the boolean expression required by the application. Custom
		/// filter implementations may also be constructed by applications.
		/// <p>
		/// Note that filters are not thread-safe and may not be shared by concurrent
		/// RevWalk instances. Every RevWalk must be supplied its own unique filter,
		/// unless the filter implementation specifically states it is (and always
		/// will be) thread-safe. Callers may use
		/// <see cref="NGit.Revwalk.Filter.RevFilter.Clone()">NGit.Revwalk.Filter.RevFilter.Clone()
		/// 	</see>
		/// to create
		/// a unique filter tree for this RevWalk instance.
		/// </remarks>
		/// <param name="newFilter">
		/// the new filter. If null the special
		/// <see cref="NGit.Revwalk.Filter.RevFilter.ALL">NGit.Revwalk.Filter.RevFilter.ALL</see>
		/// filter will be used instead, as it matches every commit.
		/// </param>
		/// <seealso cref="NGit.Revwalk.Filter.AndRevFilter">NGit.Revwalk.Filter.AndRevFilter
		/// 	</seealso>
		/// <seealso cref="NGit.Revwalk.Filter.OrRevFilter">NGit.Revwalk.Filter.OrRevFilter</seealso>
		public virtual void SetRevFilter(RevFilter newFilter)
		{
			AssertNotStarted();
			filter = newFilter != null ? newFilter : RevFilter.ALL;
		}

		/// <summary>Get the tree filter used to simplify commits by modified paths.</summary>
		/// <remarks>Get the tree filter used to simplify commits by modified paths.</remarks>
		/// <returns>
		/// the current filter. Never null as a filter is always needed. If
		/// no filter is being applied
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ALL">NGit.Treewalk.Filter.TreeFilter.ALL
		/// 	</see>
		/// is returned.
		/// </returns>
		public virtual TreeFilter GetTreeFilter()
		{
			return treeFilter;
		}

		/// <summary>Set the tree filter used to simplify commits by modified paths.</summary>
		/// <remarks>
		/// Set the tree filter used to simplify commits by modified paths.
		/// <p>
		/// If null or
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ALL">NGit.Treewalk.Filter.TreeFilter.ALL
		/// 	</see>
		/// the path limiter is removed. Commits
		/// will not be simplified.
		/// <p>
		/// If non-null and not
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ALL">NGit.Treewalk.Filter.TreeFilter.ALL
		/// 	</see>
		/// then the tree filter will be
		/// installed and commits will have their ancestry simplified to hide commits
		/// that do not contain tree entries matched by the filter.
		/// <p>
		/// Usually callers should be inserting a filter graph including
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ANY_DIFF">NGit.Treewalk.Filter.TreeFilter.ANY_DIFF
		/// 	</see>
		/// along with one or more
		/// <see cref="NGit.Treewalk.Filter.PathFilter">NGit.Treewalk.Filter.PathFilter</see>
		/// instances.
		/// </remarks>
		/// <param name="newFilter">
		/// new filter. If null the special
		/// <see cref="NGit.Treewalk.Filter.TreeFilter.ALL">NGit.Treewalk.Filter.TreeFilter.ALL
		/// 	</see>
		/// filter
		/// will be used instead, as it matches everything.
		/// </param>
		/// <seealso cref="NGit.Treewalk.Filter.PathFilter">NGit.Treewalk.Filter.PathFilter</seealso>
		public virtual void SetTreeFilter(TreeFilter newFilter)
		{
			AssertNotStarted();
			treeFilter = newFilter != null ? newFilter : TreeFilter.ALL;
		}

		/// <summary>
		/// Should the body of a commit or tag be retained after parsing its headers?
		/// <p>
		/// Usually the body is always retained, but some application code might not
		/// care and would prefer to discard the body of a commit as early as
		/// possible, to reduce memory usage.
		/// </summary>
		/// <remarks>
		/// Should the body of a commit or tag be retained after parsing its headers?
		/// <p>
		/// Usually the body is always retained, but some application code might not
		/// care and would prefer to discard the body of a commit as early as
		/// possible, to reduce memory usage.
		/// </remarks>
		/// <returns>true if the body should be retained; false it is discarded.</returns>
		public virtual bool IsRetainBody()
		{
			return retainBody;
		}

		/// <summary>Set whether or not the body of a commit or tag is retained.</summary>
		/// <remarks>
		/// Set whether or not the body of a commit or tag is retained.
		/// <p>
		/// If a body of a commit or tag is not retained, the application must
		/// call
		/// <see cref="ParseBody(RevObject)">ParseBody(RevObject)</see>
		/// before the body can be safely
		/// accessed through the type specific access methods.
		/// </remarks>
		/// <param name="retain">true to retain bodies; false to discard them early.</param>
		public virtual void SetRetainBody(bool retain)
		{
			retainBody = retain;
		}

		/// <summary>Locate a reference to a blob without loading it.</summary>
		/// <remarks>
		/// Locate a reference to a blob without loading it.
		/// <p>
		/// The blob may or may not exist in the repository. It is impossible to tell
		/// from this method's return value.
		/// </remarks>
		/// <param name="id">name of the blob object.</param>
		/// <returns>reference to the blob object. Never null.</returns>
		public virtual RevBlob LookupBlob(AnyObjectId id)
		{
			RevBlob c = (RevBlob)objects.Get(id);
			if (c == null)
			{
				c = new RevBlob(id);
				objects.Add(c);
			}
			return c;
		}

		/// <summary>Locate a reference to a tree without loading it.</summary>
		/// <remarks>
		/// Locate a reference to a tree without loading it.
		/// <p>
		/// The tree may or may not exist in the repository. It is impossible to tell
		/// from this method's return value.
		/// </remarks>
		/// <param name="id">name of the tree object.</param>
		/// <returns>reference to the tree object. Never null.</returns>
		public virtual RevTree LookupTree(AnyObjectId id)
		{
			RevTree c = (RevTree)objects.Get(id);
			if (c == null)
			{
				c = new RevTree(id);
				objects.Add(c);
			}
			return c;
		}

		/// <summary>Locate a reference to a commit without loading it.</summary>
		/// <remarks>
		/// Locate a reference to a commit without loading it.
		/// <p>
		/// The commit may or may not exist in the repository. It is impossible to
		/// tell from this method's return value.
		/// </remarks>
		/// <param name="id">name of the commit object.</param>
		/// <returns>reference to the commit object. Never null.</returns>
		public virtual RevCommit LookupCommit(AnyObjectId id)
		{
			RevCommit c = (RevCommit)objects.Get(id);
			if (c == null)
			{
				c = CreateCommit(id);
				objects.Add(c);
			}
			return c;
		}

		/// <summary>Locate a reference to a tag without loading it.</summary>
		/// <remarks>
		/// Locate a reference to a tag without loading it.
		/// <p>
		/// The tag may or may not exist in the repository. It is impossible to tell
		/// from this method's return value.
		/// </remarks>
		/// <param name="id">name of the tag object.</param>
		/// <returns>reference to the tag object. Never null.</returns>
		public virtual RevTag LookupTag(AnyObjectId id)
		{
			RevTag c = (RevTag)objects.Get(id);
			if (c == null)
			{
				c = new RevTag(id);
				objects.Add(c);
			}
			return c;
		}

		/// <summary>Locate a reference to any object without loading it.</summary>
		/// <remarks>
		/// Locate a reference to any object without loading it.
		/// <p>
		/// The object may or may not exist in the repository. It is impossible to
		/// tell from this method's return value.
		/// </remarks>
		/// <param name="id">name of the object.</param>
		/// <param name="type">type of the object. Must be a valid Git object type.</param>
		/// <returns>reference to the object. Never null.</returns>
		public virtual RevObject LookupAny(AnyObjectId id, int type)
		{
			RevObject r = objects.Get(id);
			if (r == null)
			{
				switch (type)
				{
					case Constants.OBJ_COMMIT:
					{
						r = CreateCommit(id);
						break;
					}

					case Constants.OBJ_TREE:
					{
						r = new RevTree(id);
						break;
					}

					case Constants.OBJ_BLOB:
					{
						r = new RevBlob(id);
						break;
					}

					case Constants.OBJ_TAG:
					{
						r = new RevTag(id);
						break;
					}

					default:
					{
						throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidGitType, type
							));
					}
				}
				objects.Add(r);
			}
			return r;
		}

		/// <summary>Locate an object that was previously allocated in this walk.</summary>
		/// <remarks>Locate an object that was previously allocated in this walk.</remarks>
		/// <param name="id">name of the object.</param>
		/// <returns>
		/// reference to the object if it has been previously located;
		/// otherwise null.
		/// </returns>
		public virtual RevObject LookupOrNull(AnyObjectId id)
		{
			return objects.Get(id);
		}

		/// <summary>Locate a reference to a commit and immediately parse its content.</summary>
		/// <remarks>
		/// Locate a reference to a commit and immediately parse its content.
		/// <p>
		/// Unlike
		/// <see cref="LookupCommit(NGit.AnyObjectId)">LookupCommit(NGit.AnyObjectId)</see>
		/// this method only returns
		/// successfully if the commit object exists, is verified to be a commit, and
		/// was parsed without error.
		/// </remarks>
		/// <param name="id">name of the commit object.</param>
		/// <returns>reference to the commit object. Never null.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied commit does not exist.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the supplied id is not a commit or an annotated tag.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevCommit ParseCommit(AnyObjectId id)
		{
			RevObject c = Peel(ParseAny(id));
			if (!(c is RevCommit))
			{
				throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_COMMIT);
			}
			return (RevCommit)c;
		}

		/// <summary>Locate a reference to a tree.</summary>
		/// <remarks>
		/// Locate a reference to a tree.
		/// <p>
		/// This method only returns successfully if the tree object exists, is
		/// verified to be a tree.
		/// </remarks>
		/// <param name="id">
		/// name of the tree object, or a commit or annotated tag that may
		/// reference a tree.
		/// </param>
		/// <returns>reference to the tree object. Never null.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied tree does not exist.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the supplied id is not a tree, a commit or an annotated tag.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevTree ParseTree(AnyObjectId id)
		{
			RevObject c = Peel(ParseAny(id));
			RevTree t;
			if (c is RevCommit)
			{
				t = ((RevCommit)c).Tree;
			}
			else
			{
				if (!(c is RevTree))
				{
					throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_TREE);
				}
				else
				{
					t = (RevTree)c;
				}
			}
			ParseHeaders(t);
			return t;
		}

		/// <summary>Locate a reference to an annotated tag and immediately parse its content.
		/// 	</summary>
		/// <remarks>
		/// Locate a reference to an annotated tag and immediately parse its content.
		/// <p>
		/// Unlike
		/// <see cref="LookupTag(NGit.AnyObjectId)">LookupTag(NGit.AnyObjectId)</see>
		/// this method only returns
		/// successfully if the tag object exists, is verified to be a tag, and was
		/// parsed without error.
		/// </remarks>
		/// <param name="id">name of the tag object.</param>
		/// <returns>reference to the tag object. Never null.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied tag does not exist.
		/// 	</exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the supplied id is not a tag or an annotated tag.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevTag ParseTag(AnyObjectId id)
		{
			RevObject c = ParseAny(id);
			if (!(c is RevTag))
			{
				throw new IncorrectObjectTypeException(id.ToObjectId(), Constants.TYPE_TAG);
			}
			return (RevTag)c;
		}

		/// <summary>Locate a reference to any object and immediately parse its headers.</summary>
		/// <remarks>
		/// Locate a reference to any object and immediately parse its headers.
		/// <p>
		/// This method only returns successfully if the object exists and was parsed
		/// without error. Parsing an object can be expensive as the type must be
		/// determined. For blobs this may mean the blob content was unpacked
		/// unnecessarily, and thrown away.
		/// </remarks>
		/// <param name="id">name of the object.</param>
		/// <returns>reference to the object. Never null.</returns>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied does not exist.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevObject ParseAny(AnyObjectId id)
		{
			RevObject r = objects.Get(id);
			if (r == null)
			{
				r = ParseNew(id, reader.Open(id));
			}
			else
			{
				ParseHeaders(r);
			}
			return r;
		}

		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		/// <exception cref="NGit.Errors.CorruptObjectException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private RevObject ParseNew(AnyObjectId id, ObjectLoader ldr)
		{
			RevObject r;
			int type = ldr.GetType();
			switch (type)
			{
				case Constants.OBJ_COMMIT:
				{
					RevCommit c = CreateCommit(id);
					c.ParseCanonical(this, GetCachedBytes(c, ldr));
					r = c;
					break;
				}

				case Constants.OBJ_TREE:
				{
					r = new RevTree(id);
					r.flags |= PARSED;
					break;
				}

				case Constants.OBJ_BLOB:
				{
					r = new RevBlob(id);
					r.flags |= PARSED;
					break;
				}

				case Constants.OBJ_TAG:
				{
					RevTag t = new RevTag(id);
					t.ParseCanonical(this, GetCachedBytes(t, ldr));
					r = t;
					break;
				}

				default:
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().badObjectType, type
						));
				}
			}
			objects.Add(r);
			return r;
		}

		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual byte[] GetCachedBytes(RevObject obj)
		{
			return GetCachedBytes(obj, reader.Open(obj, obj.Type));
		}

		/// <exception cref="NGit.Errors.LargeObjectException"></exception>
		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual byte[] GetCachedBytes(RevObject obj, ObjectLoader ldr)
		{
			try
			{
				return ldr.GetCachedBytes(5 * MB);
			}
			catch (LargeObjectException tooBig)
			{
				tooBig.SetObjectId(obj);
				throw;
			}
		}

		/// <summary>Asynchronous object parsing.</summary>
		/// <remarks>Asynchronous object parsing.</remarks>
		/// <?></?>
		/// <param name="objectIds">
		/// objects to open from the object store. The supplied collection
		/// must not be modified until the queue has finished.
		/// </param>
		/// <param name="reportMissing">
		/// if true missing objects are reported by calling failure with a
		/// MissingObjectException. This may be more expensive for the
		/// implementation to guarantee. If false the implementation may
		/// choose to report MissingObjectException, or silently skip over
		/// the object with no warning.
		/// </param>
		/// <returns>queue to read the objects from.</returns>
		public virtual AsyncRevObjectQueue ParseAny<T>(Iterable<T> objectIds, bool reportMissing
			) where T:ObjectId
		{
			IList<T> need = new AList<T>();
			IList<RevObject> have = new AList<RevObject>();
			foreach (T id in objectIds)
			{
				RevObject r = objects.Get(id);
				if (r != null && (r.flags & PARSED) != 0)
				{
					have.AddItem(r);
				}
				else
				{
					need.AddItem(id);
				}
			}
			Sharpen.Iterator<RevObject> objItr = have.Iterator();
			if (need.IsEmpty())
			{
				return new _AsyncRevObjectQueue_898<T>(objItr);
			}
			// In-memory only, no action required.
			AsyncObjectLoaderQueue<T> lItr = reader.Open(need.AsIterable(), reportMissing);
			return new _AsyncRevObjectQueue_914<T>(this, objItr, lItr);
		}

		private sealed class _AsyncRevObjectQueue_898<T> : AsyncRevObjectQueue where T:ObjectId
		{
			public _AsyncRevObjectQueue_898(Sharpen.Iterator<RevObject> objItr)
			{
				this.objItr = objItr;
			}

			public RevObject Next()
			{
				return objItr.HasNext() ? objItr.Next() : null;
			}

			public bool Cancel(bool mayInterruptIfRunning)
			{
				return true;
			}

			public void Release()
			{
			}

			private readonly Sharpen.Iterator<RevObject> objItr;
		}

		private sealed class _AsyncRevObjectQueue_914<T> : AsyncRevObjectQueue where T:ObjectId
		{
			public _AsyncRevObjectQueue_914(RevWalk _enclosing, Sharpen.Iterator<RevObject> objItr
				, AsyncObjectLoaderQueue<T> lItr)
			{
				this._enclosing = _enclosing;
				this.objItr = objItr;
				this.lItr = lItr;
			}

			/// <exception cref="NGit.Errors.MissingObjectException"></exception>
			/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
			/// <exception cref="System.IO.IOException"></exception>
			public RevObject Next()
			{
				if (objItr.HasNext())
				{
					return objItr.Next();
				}
				if (!lItr.Next())
				{
					return null;
				}
				ObjectId id = lItr.GetObjectId();
				ObjectLoader ldr = lItr.Open();
				RevObject r = this._enclosing.objects.Get(id);
				if (r == null)
				{
					r = this._enclosing.ParseNew(id, ldr);
				}
				else
				{
					if (r is RevCommit)
					{
						byte[] raw = ldr.GetCachedBytes();
						((RevCommit)r).ParseCanonical(this._enclosing, raw);
					}
					else
					{
						if (r is RevTag)
						{
							byte[] raw = ldr.GetCachedBytes();
							((RevTag)r).ParseCanonical(this._enclosing, raw);
						}
						else
						{
							r.flags |= NGit.Revwalk.RevWalk.PARSED;
						}
					}
				}
				return r;
			}

			public bool Cancel(bool mayInterruptIfRunning)
			{
				return lItr.Cancel(mayInterruptIfRunning);
			}

			public void Release()
			{
				lItr.Release();
			}

			private readonly RevWalk _enclosing;

			private readonly Sharpen.Iterator<RevObject> objItr;

			private readonly AsyncObjectLoaderQueue<T> lItr;
		}

		/// <summary>Ensure the object's critical headers have been parsed.</summary>
		/// <remarks>
		/// Ensure the object's critical headers have been parsed.
		/// <p>
		/// This method only returns successfully if the object exists and was parsed
		/// without error.
		/// </remarks>
		/// <param name="obj">the object the caller needs to be parsed.</param>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied does not exist.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void ParseHeaders(RevObject obj)
		{
			if ((obj.flags & PARSED) == 0)
			{
				obj.ParseHeaders(this);
			}
		}

		/// <summary>Ensure the object's full body content is available.</summary>
		/// <remarks>
		/// Ensure the object's full body content is available.
		/// <p>
		/// This method only returns successfully if the object exists and was parsed
		/// without error.
		/// </remarks>
		/// <param name="obj">the object the caller needs to be parsed.</param>
		/// <exception cref="NGit.Errors.MissingObjectException">the supplied does not exist.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual void ParseBody(RevObject obj)
		{
			obj.ParseBody(this);
		}

		/// <summary>Peel back annotated tags until a non-tag object is found.</summary>
		/// <remarks>Peel back annotated tags until a non-tag object is found.</remarks>
		/// <param name="obj">the starting object.</param>
		/// <returns>
		/// If
		/// <code>obj</code>
		/// is not an annotated tag,
		/// <code>obj</code>
		/// . Otherwise
		/// the first non-tag object that
		/// <code>obj</code>
		/// references. The
		/// returned object's headers have been parsed.
		/// </returns>
		/// <exception cref="NGit.Errors.MissingObjectException">a referenced object cannot be found.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">a pack file or loose object could not be read.
		/// 	</exception>
		public virtual RevObject Peel(RevObject obj)
		{
			while (obj is RevTag)
			{
				ParseHeaders(obj);
				obj = ((RevTag)obj).GetObject();
			}
			ParseHeaders(obj);
			return obj;
		}

		/// <summary>Create a new flag for application use during walking.</summary>
		/// <remarks>
		/// Create a new flag for application use during walking.
		/// <p>
		/// Applications are only assured to be able to create 24 unique flags on any
		/// given revision walker instance. Any flags beyond 24 are offered only if
		/// the implementation has extra free space within its internal storage.
		/// </remarks>
		/// <param name="name">description of the flag, primarily useful for debugging.</param>
		/// <returns>newly constructed flag instance.</returns>
		/// <exception cref="System.ArgumentException">too many flags have been reserved on this revision walker.
		/// 	</exception>
		public virtual RevFlag NewFlag(string name)
		{
			int m = AllocFlag();
			return new RevFlag(this, name, m);
		}

		internal virtual int AllocFlag()
		{
			if (freeFlags == 0)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().flagsAlreadyCreated
					, 32 - RESERVED_FLAGS));
			}
			int m = Sharpen.Extensions.LowestOneBit(freeFlags);
			freeFlags &= ~m;
			return m;
		}

		/// <summary>Automatically carry a flag from a child commit to its parents.</summary>
		/// <remarks>
		/// Automatically carry a flag from a child commit to its parents.
		/// <p>
		/// A carried flag is copied from the child commit onto its parents when the
		/// child commit is popped from the lowest level of walk's internal graph.
		/// </remarks>
		/// <param name="flag">the flag to carry onto parents, if set on a descendant.</param>
		public virtual void Carry(RevFlag flag)
		{
			if ((freeFlags & flag.mask) != 0)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().flagIsDisposed, flag
					.name));
			}
			if (flag.walker != this)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().flagNotFromThis, 
					flag.name));
			}
			carryFlags |= flag.mask;
		}

		/// <summary>Automatically carry flags from a child commit to its parents.</summary>
		/// <remarks>
		/// Automatically carry flags from a child commit to its parents.
		/// <p>
		/// A carried flag is copied from the child commit onto its parents when the
		/// child commit is popped from the lowest level of walk's internal graph.
		/// </remarks>
		/// <param name="set">the flags to carry onto parents, if set on a descendant.</param>
		public virtual void Carry(ICollection<RevFlag> set)
		{
			foreach (RevFlag flag in set)
			{
				Carry(flag);
			}
		}

		/// <summary>Allow a flag to be recycled for a different use.</summary>
		/// <remarks>
		/// Allow a flag to be recycled for a different use.
		/// <p>
		/// Recycled flags always come back as a different Java object instance when
		/// assigned again by
		/// <see cref="NewFlag(string)">NewFlag(string)</see>
		/// .
		/// <p>
		/// If the flag was previously being carried, the carrying request is
		/// removed. Disposing of a carried flag while a traversal is in progress has
		/// an undefined behavior.
		/// </remarks>
		/// <param name="flag">the to recycle.</param>
		public virtual void DisposeFlag(RevFlag flag)
		{
			FreeFlag(flag.mask);
		}

		internal virtual void FreeFlag(int mask)
		{
			if (IsNotStarted())
			{
				freeFlags |= mask;
				carryFlags &= ~mask;
			}
			else
			{
				delayFreeFlags |= mask;
			}
		}

		private void FinishDelayedFreeFlags()
		{
			if (delayFreeFlags != 0)
			{
				freeFlags |= delayFreeFlags;
				carryFlags &= ~delayFreeFlags;
				delayFreeFlags = 0;
			}
		}

		/// <summary>Resets internal state and allows this instance to be used again.</summary>
		/// <remarks>
		/// Resets internal state and allows this instance to be used again.
		/// <p>
		/// Unlike
		/// <see cref="Dispose()">Dispose()</see>
		/// previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </remarks>
		public void Reset()
		{
			Reset(0);
		}

		/// <summary>Resets internal state and allows this instance to be used again.</summary>
		/// <remarks>
		/// Resets internal state and allows this instance to be used again.
		/// <p>
		/// Unlike
		/// <see cref="Dispose()">Dispose()</see>
		/// previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </remarks>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		public void ResetRetain(RevFlagSet retainFlags)
		{
			Reset(retainFlags.mask);
		}

		/// <summary>Resets internal state and allows this instance to be used again.</summary>
		/// <remarks>
		/// Resets internal state and allows this instance to be used again.
		/// <p>
		/// Unlike
		/// <see cref="Dispose()">Dispose()</see>
		/// previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </remarks>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		public void ResetRetain(params RevFlag[] retainFlags)
		{
			int mask = 0;
			foreach (RevFlag flag in retainFlags)
			{
				mask |= flag.mask;
			}
			Reset(mask);
		}

		/// <summary>Resets internal state and allows this instance to be used again.</summary>
		/// <remarks>
		/// Resets internal state and allows this instance to be used again.
		/// <p>
		/// Unlike
		/// <see cref="Dispose()">Dispose()</see>
		/// previously acquired RevObject (and RevCommit)
		/// instances are not invalidated. RevFlag instances are not invalidated, but
		/// are removed from all RevObjects.
		/// </remarks>
		/// <param name="retainFlags">
		/// application flags that should <b>not</b> be cleared from
		/// existing commit objects.
		/// </param>
		protected internal virtual void Reset(int retainFlags)
		{
			FinishDelayedFreeFlags();
			retainFlags |= PARSED;
			int clearFlags = ~retainFlags;
			FIFORevQueue q = new FIFORevQueue();
			foreach (RevCommit c in roots)
			{
				if ((c.flags & clearFlags) == 0)
				{
					continue;
				}
				c.flags &= retainFlags;
				c.Reset();
				q.Add(c);
			}
			for (; ; )
			{
				RevCommit c_1 = q.Next();
				if (c_1 == null)
				{
					break;
				}
				if (c_1.parents == null)
				{
					continue;
				}
				foreach (RevCommit p in c_1.parents)
				{
					if ((p.flags & clearFlags) == 0)
					{
						continue;
					}
					p.flags &= retainFlags;
					p.Reset();
					q.Add(p);
				}
			}
			roots.Clear();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
		}

		/// <summary>Dispose all internal state and invalidate all RevObject instances.</summary>
		/// <remarks>
		/// Dispose all internal state and invalidate all RevObject instances.
		/// <p>
		/// All RevObject (and thus RevCommit, etc.) instances previously acquired
		/// from this RevWalk are invalidated by a dispose call. Applications must
		/// not retain or use RevObject instances obtained prior to the dispose call.
		/// All RevFlag instances are also invalidated, and must not be reused.
		/// </remarks>
		public virtual void Dispose()
		{
			reader.Release();
			freeFlags = APP_FLAGS;
			delayFreeFlags = 0;
			carryFlags = UNINTERESTING;
			objects.Clear();
			reader.Release();
			roots.Clear();
			queue = new DateRevQueue();
			pending = new StartGenerator(this);
		}

		/// <summary>Returns an Iterator over the commits of this walker.</summary>
		/// <remarks>
		/// Returns an Iterator over the commits of this walker.
		/// <p>
		/// The returned iterator is only useful for one walk. If this RevWalk gets
		/// reset a new iterator must be obtained to walk over the new results.
		/// <p>
		/// Applications must not use both the Iterator and the
		/// <see cref="Next()">Next()</see>
		/// API
		/// at the same time. Pick one API and use that for the entire walk.
		/// <p>
		/// If a checked exception is thrown during the walk (see
		/// <see cref="Next()">Next()</see>
		/// )
		/// it is rethrown from the Iterator as a
		/// <see cref="NGit.Errors.RevWalkException">NGit.Errors.RevWalkException</see>
		/// .
		/// </remarks>
		/// <returns>an iterator over this walker's commits.</returns>
		/// <seealso cref="NGit.Errors.RevWalkException">NGit.Errors.RevWalkException</seealso>
		public override Sharpen.Iterator<RevCommit> Iterator()
		{
			RevCommit first;
			try
			{
				first = this.Next();
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
			return new _Iterator_1236(this, first);
		}

		private sealed class _Iterator_1236 : Sharpen.Iterator<RevCommit>
		{
			public _Iterator_1236(RevWalk _enclosing, RevCommit first)
			{
				this._enclosing = _enclosing;
				this.first = first;
				this.next = first;
			}

			internal RevCommit next;

			public override bool HasNext()
			{
				return this.next != null;
			}

			public override RevCommit Next()
			{
				try
				{
					RevCommit r = this.next;
					this.next = this._enclosing.Next();
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

			public override void Remove()
			{
				throw new NGit.Errors.NotSupportedException();
			}

			private readonly RevWalk _enclosing;

			private readonly RevCommit first;
		}

		/// <summary>Throws an exception if we have started producing output.</summary>
		/// <remarks>Throws an exception if we have started producing output.</remarks>
		protected internal virtual void AssertNotStarted()
		{
			if (IsNotStarted())
			{
				return;
			}
			throw new InvalidOperationException(JGitText.Get().outputHasAlreadyBeenStarted);
		}

		private bool IsNotStarted()
		{
			return pending is StartGenerator;
		}

		/// <summary>
		/// Create and return an
		/// <see cref="ObjectWalk">ObjectWalk</see>
		/// using the same objects.
		/// <p>
		/// Prior to using this method, the caller must reset this RevWalk to clean
		/// any flags that were used during the last traversal.
		/// <p>
		/// The returned ObjectWalk uses the same ObjectReader, internal object pool,
		/// and free RevFlags. Once the ObjectWalk is created, this RevWalk should
		/// not be used anymore.
		/// </summary>
		/// <returns>a new walk, using the exact same object pool.</returns>
		public virtual ObjectWalk ToObjectWalkWithSameObjects()
		{
			ObjectWalk ow = new ObjectWalk(reader);
			NGit.Revwalk.RevWalk rw = ow;
			rw.objects = objects;
			rw.freeFlags = freeFlags;
			return ow;
		}

		/// <summary>Construct a new unparsed commit for the given object.</summary>
		/// <remarks>Construct a new unparsed commit for the given object.</remarks>
		/// <param name="id">the object this walker requires a commit reference for.</param>
		/// <returns>a new unparsed reference for the object.</returns>
		protected internal virtual RevCommit CreateCommit(AnyObjectId id)
		{
			return new RevCommit(id);
		}

		internal virtual void CarryFlagsImpl(RevCommit c)
		{
			int carry = c.flags & carryFlags;
			if (carry != 0)
			{
				RevCommit.CarryFlags(c, carry);
			}
		}
	}
}
