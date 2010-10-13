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

using System.IO;
using NGit;
using NGit.Errors;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Merge
{
	/// <summary>
	/// Instance of a specific
	/// <see cref="MergeStrategy">MergeStrategy</see>
	/// for a single
	/// <see cref="NGit.Repository">NGit.Repository</see>
	/// .
	/// </summary>
	public abstract class Merger
	{
		/// <summary>The repository this merger operates on.</summary>
		/// <remarks>The repository this merger operates on.</remarks>
		protected internal readonly Repository db;

		/// <summary>
		/// Reader to support
		/// <see cref="walk">walk</see>
		/// and other object loading.
		/// </summary>
		protected internal readonly ObjectReader reader;

		/// <summary>A RevWalk for computing merge bases, or listing incoming commits.</summary>
		/// <remarks>A RevWalk for computing merge bases, or listing incoming commits.</remarks>
		protected internal readonly RevWalk walk;

		private ObjectInserter inserter;

		/// <summary>The original objects supplied in the merge; this can be any tree-ish.</summary>
		/// <remarks>The original objects supplied in the merge; this can be any tree-ish.</remarks>
		protected internal RevObject[] sourceObjects;

		/// <summary>
		/// If
		/// <see cref="sourceObjects">sourceObjects</see>
		/// [i] is a commit, this is the commit.
		/// </summary>
		protected internal RevCommit[] sourceCommits;

		/// <summary>
		/// The trees matching every entry in
		/// <see cref="sourceObjects">sourceObjects</see>
		/// .
		/// </summary>
		protected internal RevTree[] sourceTrees;

		/// <summary>Create a new merge instance for a repository.</summary>
		/// <remarks>Create a new merge instance for a repository.</remarks>
		/// <param name="local">the repository this merger will read and write data on.</param>
		protected internal Merger(Repository local)
		{
			db = local;
			reader = db.NewObjectReader();
			walk = new RevWalk(reader);
		}

		/// <returns>the repository this merger operates on.</returns>
		public virtual Repository GetRepository()
		{
			return db;
		}

		/// <returns>
		/// an object writer to create objects in
		/// <see cref="GetRepository()">GetRepository()</see>
		/// .
		/// </returns>
		public virtual ObjectInserter GetObjectInserter()
		{
			if (inserter == null)
			{
				inserter = GetRepository().NewObjectInserter();
			}
			return inserter;
		}

		/// <summary>Merge together two or more tree-ish objects.</summary>
		/// <remarks>
		/// Merge together two or more tree-ish objects.
		/// <p>
		/// Any tree-ish may be supplied as inputs. Commits and/or tags pointing at
		/// trees or commits may be passed as input objects.
		/// </remarks>
		/// <param name="tips">
		/// source trees to be combined together. The merge base is not
		/// included in this set.
		/// </param>
		/// <returns>
		/// true if the merge was completed without conflicts; false if the
		/// merge strategy cannot handle this merge or there were conflicts
		/// preventing it from automatically resolving all paths.
		/// </returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one of the input objects is not a commit, but the strategy
		/// requires it to be a commit.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// one or more sources could not be read, or outputs could not
		/// be written to the Repository.
		/// </exception>
		public virtual bool Merge(AnyObjectId[] tips)
		{
			sourceObjects = new RevObject[tips.Length];
			for (int i = 0; i < tips.Length; i++)
			{
				sourceObjects[i] = walk.ParseAny(tips[i]);
			}
			sourceCommits = new RevCommit[sourceObjects.Length];
			for (int i_1 = 0; i_1 < sourceObjects.Length; i_1++)
			{
				try
				{
					sourceCommits[i_1] = walk.ParseCommit(sourceObjects[i_1]);
				}
				catch (IncorrectObjectTypeException)
				{
					sourceCommits[i_1] = null;
				}
			}
			sourceTrees = new RevTree[sourceObjects.Length];
			for (int i_2 = 0; i_2 < sourceObjects.Length; i_2++)
			{
				sourceTrees[i_2] = walk.ParseTree(sourceObjects[i_2]);
			}
			try
			{
				return MergeImpl();
			}
			finally
			{
				if (inserter != null)
				{
					inserter.Release();
				}
				reader.Release();
			}
		}

		/// <summary>Create an iterator to walk the merge base of two commits.</summary>
		/// <remarks>Create an iterator to walk the merge base of two commits.</remarks>
		/// <param name="aIdx">
		/// index of the first commit in
		/// <see cref="sourceObjects">sourceObjects</see>
		/// .
		/// </param>
		/// <param name="bIdx">
		/// index of the second commit in
		/// <see cref="sourceObjects">sourceObjects</see>
		/// .
		/// </param>
		/// <returns>the new iterator</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">one of the input objects is not a commit.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">objects are missing or multiple merge bases were found.
		/// 	</exception>
		protected internal virtual AbstractTreeIterator MergeBase(int aIdx, int bIdx)
		{
			RevCommit @base = GetBaseCommit(aIdx, bIdx);
			return (@base == null) ? new EmptyTreeIterator() : OpenTree(@base.Tree);
		}

		/// <summary>Return the merge base of two commits.</summary>
		/// <remarks>Return the merge base of two commits.</remarks>
		/// <param name="aIdx">
		/// index of the first commit in
		/// <see cref="sourceObjects">sourceObjects</see>
		/// .
		/// </param>
		/// <param name="bIdx">
		/// index of the second commit in
		/// <see cref="sourceObjects">sourceObjects</see>
		/// .
		/// </param>
		/// <returns>the merge base of two commits</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">one of the input objects is not a commit.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">objects are missing or multiple merge bases were found.
		/// 	</exception>
		public virtual RevCommit GetBaseCommit(int aIdx, int bIdx)
		{
			if (sourceCommits[aIdx] == null)
			{
				throw new IncorrectObjectTypeException(sourceObjects[aIdx], Constants.TYPE_COMMIT
					);
			}
			if (sourceCommits[bIdx] == null)
			{
				throw new IncorrectObjectTypeException(sourceObjects[bIdx], Constants.TYPE_COMMIT
					);
			}
			walk.Reset();
			walk.SetRevFilter(RevFilter.MERGE_BASE);
			walk.MarkStart(sourceCommits[aIdx]);
			walk.MarkStart(sourceCommits[bIdx]);
			RevCommit @base = walk.Next();
			if (@base == null)
			{
				return null;
			}
			RevCommit base2 = walk.Next();
			if (base2 != null)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().multipleMergeBasesFor, 
					sourceCommits[aIdx].Name, sourceCommits[bIdx].Name, @base.Name, base2.Name));
			}
			return @base;
		}

		/// <summary>Open an iterator over a tree.</summary>
		/// <remarks>Open an iterator over a tree.</remarks>
		/// <param name="treeId">the tree to scan; must be a tree (not a treeish).</param>
		/// <returns>an iterator for the tree.</returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">the input object is not a tree.
		/// 	</exception>
		/// <exception cref="System.IO.IOException">the tree object is not found or cannot be read.
		/// 	</exception>
		protected internal virtual AbstractTreeIterator OpenTree(AnyObjectId treeId)
		{
			return new CanonicalTreeParser(null, reader, treeId);
		}

		/// <summary>Execute the merge.</summary>
		/// <remarks>
		/// Execute the merge.
		/// <p>
		/// This method is called from
		/// <see cref="Merge(org.eclipse.jgit.lib.AnyObjectId[])">Merge(org.eclipse.jgit.lib.AnyObjectId[])
		/// 	</see>
		/// after the
		/// <see cref="sourceObjects">sourceObjects</see>
		/// ,
		/// <see cref="sourceCommits">sourceCommits</see>
		/// and
		/// <see cref="sourceTrees">sourceTrees</see>
		/// have been populated.
		/// </remarks>
		/// <returns>
		/// true if the merge was completed without conflicts; false if the
		/// merge strategy cannot handle this merge or there were conflicts
		/// preventing it from automatically resolving all paths.
		/// </returns>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException">
		/// one of the input objects is not a commit, but the strategy
		/// requires it to be a commit.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// one or more sources could not be read, or outputs could not
		/// be written to the Repository.
		/// </exception>
		protected internal abstract bool MergeImpl();

		/// <returns>
		/// resulting tree, if
		/// <see cref="Merge(org.eclipse.jgit.lib.AnyObjectId[])">Merge(org.eclipse.jgit.lib.AnyObjectId[])
		/// 	</see>
		/// returned true.
		/// </returns>
		public abstract ObjectId GetResultTreeId();
	}
}
