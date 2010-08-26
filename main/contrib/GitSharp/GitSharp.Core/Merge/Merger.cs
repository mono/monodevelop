/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Dan Rigby <dan@danrigby.com>
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
using System.IO;
using GitSharp.Core.Exceptions;
using GitSharp.Core.RevWalk;
using GitSharp.Core.RevWalk.Filter;
using GitSharp.Core.TreeWalk;

namespace GitSharp.Core.Merge
{
	/// <summary>
	/// Instance of a specific <seealso cref="MergeStrategy"/> for a single <seealso cref="Repository"/>.
	/// </summary>
	public abstract class Merger : IDisposable
	{
		private readonly Repository _db;
		private readonly RevWalk.RevWalk _walk;
		private ObjectWriter _writer;
		private RevObject[] _sourceObjects;
		private RevCommit[] _sourceCommits;

		/// <summary>
		/// Create a new merge instance for a repository.
		/// </summary>
		/// <param name="local">
		/// the repository this merger will read and write data on. 
		/// </param>
		protected Merger(Repository local)
		{
			_db = local;
			_walk = new RevWalk.RevWalk(_db);
		}

		/// <summary>
		/// The repository this merger operates on.
		/// </summary>
		protected Repository Repository
		{
			get { return _db; }
		}

		/// <summary>
		/// A <see cref="RevWalk"/> for computing merge bases, or listing incoming commits.
		/// </summary>
		protected RevWalk.RevWalk Walk
		{
			get { return _walk; }
		}

		/// <summary>
		/// The original objects supplied in the merge; this can be any <see cref="Treeish"/>.
		/// </summary>
		public RevCommit[] SourceCommits
		{
			get { return _sourceCommits; }
		}

		/// <summary>
		/// If <seealso cref="SourceObjects"/>[i] is a commit, this is the commit.
		/// </summary>
		public RevObject[] SourceObjects
		{
			get { return _sourceObjects; }
		}

		/// <summary>
		/// The trees matching every entry in <seealso cref="SourceObjects"/>.
		/// </summary>
		protected RevTree[] SourceTrees { get; private set; }

		/// <summary>
		/// An object writer to Create objects in <see cref="Repository"/>.
		/// </summary>
		/// <returns></returns>
		protected ObjectWriter GetObjectWriter()
		{
			if (_writer == null)
			{
				_writer = new ObjectWriter(Repository);
			}
			return _writer;
		}

		///	<summary>
		/// Merge together two or more tree-ish objects.
		/// <para />
		/// Any tree-ish may be supplied as inputs. Commits and/or tags pointing at
		/// trees or commits may be passed as input objects.
		/// </summary>
		/// <param name="tips">
		/// source trees to be combined together. The merge base is not
		/// included in this set. </param>
		/// <returns>
		/// True if the merge was completed without conflicts; false if the
		/// merge strategy cannot handle this merge or there were conflicts
		/// preventing it from automatically resolving all paths.
		/// </returns>
		/// <exception cref="IncorrectObjectTypeException">
		/// one of the input objects is not a commit, but the strategy
		/// requires it to be a commit.
		/// </exception>
		/// <exception cref="IOException">
		/// one or more sources could not be read, or outputs could not
		/// be written to the Repository.
		/// </exception>
		public virtual bool Merge(AnyObjectId[] tips)
		{
			if (tips == null)
				throw new ArgumentNullException ("tips");
			
			_sourceObjects = new RevObject[tips.Length];
			for (int i = 0; i < tips.Length; i++)
				_sourceObjects[i] = _walk.parseAny(tips[i]);

			_sourceCommits = new RevCommit[_sourceObjects.Length];
			for (int i = 0; i < _sourceObjects.Length; i++)
			{
				try
				{
					_sourceCommits[i] = _walk.parseCommit(_sourceObjects[i]);
				}
				catch (IncorrectObjectTypeException)
				{
					_sourceCommits[i] = null;
				}
			}

			SourceTrees = new RevTree[_sourceObjects.Length];
			for (int i = 0; i < _sourceObjects.Length; i++)
				SourceTrees[i] = _walk.parseTree(_sourceObjects[i]);

			return MergeImpl();
		}

		///	<summary>
		/// Create an iterator to walk the merge base of two commits.
		/// </summary>
		/// <param name="aIdx">
		/// Index of the first commit in <seealso cref="SourceObjects"/>.
		/// </param>
		/// <param name="bIdx">
		/// Index of the second commit in <seealso cref="SourceObjects"/>.
		/// </param>
		/// <returns> the new iterator </returns>
		/// <exception cref="IncorrectObjectTypeException">
		/// one of the input objects is not a commit.
		/// </exception>
		/// <exception cref="IOException">
		/// objects are missing or multiple merge bases were found.
		/// </exception>
		protected AbstractTreeIterator MergeBase(int aIdx, int bIdx)
		{
			if (_sourceCommits[aIdx] == null)
				throw new IncorrectObjectTypeException(_sourceObjects[aIdx], Constants.TYPE_COMMIT);

			if (_sourceCommits[bIdx] == null)
				throw new IncorrectObjectTypeException(_sourceObjects[bIdx], Constants.TYPE_COMMIT);

			_walk.reset();
			_walk.setRevFilter(RevFilter.MERGE_BASE);
			_walk.markStart(_sourceCommits[aIdx]);
			_walk.markStart(_sourceCommits[bIdx]);
			RevCommit base1 = _walk.next();

			if (base1 == null)
			{
				return new EmptyTreeIterator();
			}

			RevCommit base2 = _walk.next();
			if (base2 != null)
			{
				throw new IOException("Multiple merge bases for:" + "\n  "
						+ _sourceCommits[aIdx].Name + "\n  "
						+ _sourceCommits[bIdx].Name + "found:" + "\n  "
						+ base1.Name + "\n  " + base2.Name);
			}

			return OpenTree(base1.Tree);
		}

		///	<summary>
		/// Open an iterator over a tree.
		/// </summary>
		/// <param name="treeId">
		/// the tree to scan; must be a tree (not a <see cref="Treeish"/>).
		/// </param>
		/// <returns>An iterator for the tree.</returns>
		/// <exception cref="IncorrectObjectTypeException">
		/// the input object is not a tree.
		/// </exception>
		/// <exception cref="IOException">
		/// the tree object is not found or cannot be read.
		/// </exception>
		protected AbstractTreeIterator OpenTree(AnyObjectId treeId)
		{
			var windowCursor = new WindowCursor();
			try
			{
				return new CanonicalTreeParser(null, _db, treeId, windowCursor);
			}
			finally
			{
				windowCursor.Release();
			}
		}

		///	<summary>
		/// Execute the merge.
		/// <para />
		/// This method is called from <seealso cref="Merge(AnyObjectId[])"/> after the
		/// <seealso cref="SourceObjects"/>, <seealso cref="SourceCommits"/> and <seealso cref="SourceTrees"/>
		/// have been populated.
		/// </summary>
		/// <returns> true if the merge was completed without conflicts; false if the
		/// merge strategy cannot handle this merge or there were conflicts
		/// preventing it from automatically resolving all paths. </returns>
		/// <exception cref="IncorrectObjectTypeException">
		/// one of the input objects is not a commit, but the strategy
		/// requires it to be a commit. </exception>
		/// <exception cref="IOException">
		/// one or more sources could not be read, or outputs could not
		/// be written to the Repository.
		/// </exception>
		protected abstract bool MergeImpl();

		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// Resulting tree, if <seealso cref="Merge(AnyObjectId[])"/> returned true. 
		/// </returns>
		public abstract ObjectId GetResultTreeId();
		
		public void Dispose ()
		{
			_walk.Dispose();
		}
		
	}
}