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
using NGit;
using NGit.Blame;
using NGit.Diff;
using NGit.Revwalk;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Blame
{
	/// <summary>Generate author information for lines based on introduction to the file.
	/// 	</summary>
	/// <remarks>
	/// Generate author information for lines based on introduction to the file.
	/// <p>
	/// Applications that want a simple one-shot computation of blame for a file
	/// should use
	/// <see cref="ComputeBlameResult()">ComputeBlameResult()</see>
	/// to prepare the entire result in one
	/// method call. This may block for significant time as the history of the
	/// repository must be traversed until information is gathered for every line.
	/// <p>
	/// Applications that want more incremental update behavior may use either the
	/// raw
	/// <see cref="Next()">Next()</see>
	/// streaming approach supported by this class, or construct
	/// a
	/// <see cref="BlameResult">BlameResult</see>
	/// using
	/// <see cref="BlameResult.Create(BlameGenerator)">BlameResult.Create(BlameGenerator)
	/// 	</see>
	/// and
	/// incrementally construct the result with
	/// <see cref="BlameResult.ComputeNext()">BlameResult.ComputeNext()</see>
	/// .
	/// <p>
	/// This class is not thread-safe.
	/// <p>
	/// An instance of BlameGenerator can only be used once. To blame multiple files
	/// the application must create a new BlameGenerator.
	/// <p>
	/// During blame processing there are two files involved:
	/// <ul>
	/// <li>result - The file whose lines are being examined. This is the revision
	/// the user is trying to view blame/annotation information alongside of.</li>
	/// <li>source - The file that was blamed with supplying one or more lines of
	/// data into result. The source may be a different file path (due to copy or
	/// rename). Source line numbers may differ from result line numbers due to lines
	/// being added/removed in intermediate revisions.</li>
	/// </ul>
	/// <p>
	/// The blame algorithm is implemented by initially assigning responsibility for
	/// all lines of the result to the starting commit. A difference against the
	/// commit's ancestor is computed, and responsibility is passed to the ancestor
	/// commit for any lines that are common. The starting commit is blamed only for
	/// the lines that do not appear in the ancestor, if any. The loop repeats using
	/// the ancestor, until there are no more lines to acquire information on, or the
	/// file's creation point is discovered in history.
	/// </remarks>
	public class BlameGenerator
	{
		private readonly Repository repository;

		private readonly PathFilter resultPath;

		private readonly MutableObjectId idBuf;

		/// <summary>Revision pool used to acquire commits from.</summary>
		/// <remarks>Revision pool used to acquire commits from.</remarks>
		private RevWalk revPool;

		/// <summary>Indicates the commit has already been processed.</summary>
		/// <remarks>Indicates the commit has already been processed.</remarks>
		private RevFlag SEEN;

		private ObjectReader reader;

		private TreeWalk treeWalk;

		private DiffAlgorithm diffAlgorithm = new HistogramDiff();

		private RawTextComparator textComparator = RawTextComparator.DEFAULT;

		private RenameDetector renameDetector;

		/// <summary>Potential candidates, sorted by commit time descending.</summary>
		/// <remarks>Potential candidates, sorted by commit time descending.</remarks>
		private Candidate queue;

		/// <summary>Number of lines that still need to be discovered.</summary>
		/// <remarks>Number of lines that still need to be discovered.</remarks>
		private int remaining;

		/// <summary>Blame is currently assigned to this source.</summary>
		/// <remarks>Blame is currently assigned to this source.</remarks>
		private Candidate currentSource;

		/// <summary>Create a blame generator for the repository and path</summary>
		/// <param name="repository">repository to access revision data from.</param>
		/// <param name="path">initial path of the file to start scanning.</param>
		public BlameGenerator(Repository repository, string path)
		{
			this.repository = repository;
			this.resultPath = PathFilter.Create(path);
			idBuf = new MutableObjectId();
			SetFollowFileRenames(true);
			InitRevPool(false);
			remaining = -1;
		}

		private void InitRevPool(bool reverse)
		{
			if (queue != null)
			{
				throw new InvalidOperationException();
			}
			if (revPool != null)
			{
				revPool.Release();
			}
			if (reverse)
			{
				revPool = new ReverseWalk(GetRepository());
			}
			else
			{
				revPool = new RevWalk(GetRepository());
			}
			revPool.SetRetainBody(true);
			SEEN = revPool.NewFlag("SEEN");
			reader = revPool.GetObjectReader();
			treeWalk = new TreeWalk(reader);
			treeWalk.Recursive = true;
		}

		/// <returns>repository being scanned for revision history.</returns>
		public virtual Repository GetRepository()
		{
			return repository;
		}

		/// <returns>path file path being processed.</returns>
		public virtual string GetResultPath()
		{
			return resultPath.GetPath();
		}

		/// <summary>Difference algorithm to use when comparing revisions.</summary>
		/// <remarks>Difference algorithm to use when comparing revisions.</remarks>
		/// <param name="algorithm"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Blame.BlameGenerator SetDiffAlgorithm(DiffAlgorithm algorithm
			)
		{
			diffAlgorithm = algorithm;
			return this;
		}

		/// <summary>Text comparator to use when comparing revisions.</summary>
		/// <remarks>Text comparator to use when comparing revisions.</remarks>
		/// <param name="comparator"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Blame.BlameGenerator SetTextComparator(RawTextComparator comparator
			)
		{
			textComparator = comparator;
			return this;
		}

		/// <summary>Enable (or disable) following file renames, on by default.</summary>
		/// <remarks>
		/// Enable (or disable) following file renames, on by default.
		/// <p>
		/// If true renames are followed using the standard FollowFilter behavior
		/// used by RevWalk (which matches
		/// <code>git log --follow</code>
		/// in the C
		/// implementation). This is not the same as copy/move detection as
		/// implemented by the C implementation's of
		/// <code>git blame -M -C</code>
		/// .
		/// </remarks>
		/// <param name="follow">enable following.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Blame.BlameGenerator SetFollowFileRenames(bool follow)
		{
			if (follow)
			{
				renameDetector = new RenameDetector(GetRepository());
			}
			else
			{
				renameDetector = null;
			}
			return this;
		}

		/// <summary>
		/// Obtain the RenameDetector if
		/// <code>setFollowFileRenames(true)</code>
		/// .
		/// </summary>
		/// <returns>
		/// the rename detector, allowing the application to configure its
		/// settings for rename score and breaking behavior.
		/// </returns>
		public virtual RenameDetector GetRenameDetector()
		{
			return renameDetector;
		}

		/// <summary>Push a candidate blob onto the generator's traversal stack.</summary>
		/// <remarks>
		/// Push a candidate blob onto the generator's traversal stack.
		/// <p>
		/// Candidates should be pushed in history order from oldest-to-newest.
		/// Applications should push the starting commit first, then the index
		/// revision (if the index is interesting), and finally the working tree
		/// copy (if the working tree is interesting).
		/// </remarks>
		/// <param name="description">description of the blob revision, such as "Working Tree".
		/// 	</param>
		/// <param name="contents">contents of the file.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Blame.BlameGenerator Push(string description, byte[] contents
			)
		{
			return Push(description, new RawText(contents));
		}

		/// <summary>Push a candidate blob onto the generator's traversal stack.</summary>
		/// <remarks>
		/// Push a candidate blob onto the generator's traversal stack.
		/// <p>
		/// Candidates should be pushed in history order from oldest-to-newest.
		/// Applications should push the starting commit first, then the index
		/// revision (if the index is interesting), and finally the working tree copy
		/// (if the working tree is interesting).
		/// </remarks>
		/// <param name="description">description of the blob revision, such as "Working Tree".
		/// 	</param>
		/// <param name="contents">contents of the file.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Blame.BlameGenerator Push(string description, RawText contents
			)
		{
			if (description == null)
			{
				description = JGitText.Get().blameNotCommittedYet;
			}
			Candidate.BlobCandidate c = new Candidate.BlobCandidate(description, resultPath);
			c.sourceText = contents;
			c.regionList = new Region(0, 0, contents.Size());
			remaining = contents.Size();
			Push(c);
			return this;
		}

		/// <summary>Push a candidate object onto the generator's traversal stack.</summary>
		/// <remarks>
		/// Push a candidate object onto the generator's traversal stack.
		/// <p>
		/// Candidates should be pushed in history order from oldest-to-newest.
		/// Applications should push the starting commit first, then the index
		/// revision (if the index is interesting), and finally the working tree copy
		/// (if the working tree is interesting).
		/// </remarks>
		/// <param name="description">description of the blob revision, such as "Working Tree".
		/// 	</param>
		/// <param name="id">may be a commit or a blob.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Blame.BlameGenerator Push(string description, AnyObjectId id)
		{
			ObjectLoader ldr = reader.Open(id);
			if (ldr.GetType() == Constants.OBJ_BLOB)
			{
				if (description == null)
				{
					description = JGitText.Get().blameNotCommittedYet;
				}
				Candidate.BlobCandidate c = new Candidate.BlobCandidate(description, resultPath);
				c.sourceBlob = id.ToObjectId();
				c.sourceText = new RawText(ldr.GetCachedBytes(int.MaxValue));
				c.regionList = new Region(0, 0, c.sourceText.Size());
				remaining = c.sourceText.Size();
				Push(c);
				return this;
			}
			RevCommit commit = revPool.ParseCommit(id);
			if (!Find(commit, resultPath))
			{
				return this;
			}
			Candidate c_1 = new Candidate(commit, resultPath);
			c_1.sourceBlob = idBuf.ToObjectId();
			c_1.LoadText(reader);
			c_1.regionList = new Region(0, 0, c_1.sourceText.Size());
			remaining = c_1.sourceText.Size();
			Push(c_1);
			return this;
		}

		/// <summary>Configure the generator to compute reverse blame (history of deletes).</summary>
		/// <remarks>
		/// Configure the generator to compute reverse blame (history of deletes).
		/// <p>
		/// This method is expensive as it immediately runs a RevWalk over the
		/// history spanning the expression
		/// <code>start..end</code>
		/// (end being more recent
		/// than start) and then performs the equivalent operation as
		/// <see cref="Push(string, NGit.AnyObjectId)">Push(string, NGit.AnyObjectId)</see>
		/// to begin blame traversal from the
		/// commit named by
		/// <code>start</code>
		/// walking forwards through history until
		/// <code>end</code>
		/// blaming line deletions.
		/// <p>
		/// A reverse blame may produce multiple sources for the same result line,
		/// each of these is a descendant commit that removed the line, typically
		/// this occurs when the same deletion appears in multiple side branches such
		/// as due to a cherry-pick. Applications relying on reverse should use
		/// <see cref="BlameResult">BlameResult</see>
		/// as it filters these duplicate sources and only
		/// remembers the first (oldest) deletion.
		/// </remarks>
		/// <param name="start">
		/// oldest commit to traverse from. The result file will be loaded
		/// from this commit's tree.
		/// </param>
		/// <param name="end">
		/// most recent commit to stop traversal at. Usually an active
		/// branch tip, tag, or HEAD.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Blame.BlameGenerator Reverse(AnyObjectId start, AnyObjectId end
			)
		{
			return Reverse(start, Collections.Singleton(end.ToObjectId()));
		}

		/// <summary>Configure the generator to compute reverse blame (history of deletes).</summary>
		/// <remarks>
		/// Configure the generator to compute reverse blame (history of deletes).
		/// <p>
		/// This method is expensive as it immediately runs a RevWalk over the
		/// history spanning the expression
		/// <code>start..end</code>
		/// (end being more recent
		/// than start) and then performs the equivalent operation as
		/// <see cref="Push(string, NGit.AnyObjectId)">Push(string, NGit.AnyObjectId)</see>
		/// to begin blame traversal from the
		/// commit named by
		/// <code>start</code>
		/// walking forwards through history until
		/// <code>end</code>
		/// blaming line deletions.
		/// <p>
		/// A reverse blame may produce multiple sources for the same result line,
		/// each of these is a descendant commit that removed the line, typically
		/// this occurs when the same deletion appears in multiple side branches such
		/// as due to a cherry-pick. Applications relying on reverse should use
		/// <see cref="BlameResult">BlameResult</see>
		/// as it filters these duplicate sources and only
		/// remembers the first (oldest) deletion.
		/// </remarks>
		/// <param name="start">
		/// oldest commit to traverse from. The result file will be loaded
		/// from this commit's tree.
		/// </param>
		/// <param name="end">
		/// most recent commits to stop traversal at. Usually an active
		/// branch tip, tag, or HEAD.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Blame.BlameGenerator Reverse<_T0>(AnyObjectId start, ICollection
			<_T0> end) where _T0:ObjectId
		{
			InitRevPool(true);
			ReverseWalk.ReverseCommit result = (ReverseWalk.ReverseCommit)revPool.ParseCommit
				(start);
			if (!Find(result, resultPath))
			{
				return this;
			}
			revPool.MarkUninteresting(result);
			foreach (ObjectId id in end)
			{
				revPool.MarkStart(revPool.ParseCommit(id));
			}
			while (revPool.Next() != null)
			{
			}
			// just pump the queue
			Candidate.ReverseCandidate c = new Candidate.ReverseCandidate(result, resultPath);
			c.sourceBlob = idBuf.ToObjectId();
			c.LoadText(reader);
			c.regionList = new Region(0, 0, c.sourceText.Size());
			remaining = c.sourceText.Size();
			Push(c);
			return this;
		}

		/// <summary>Execute the generator in a blocking fashion until all data is ready.</summary>
		/// <remarks>Execute the generator in a blocking fashion until all data is ready.</remarks>
		/// <returns>the complete result. Null if no file exists for the given path.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual BlameResult ComputeBlameResult()
		{
			try
			{
				BlameResult r = BlameResult.Create(this);
				if (r != null)
				{
					r.ComputeAll();
				}
				return r;
			}
			finally
			{
				Release();
			}
		}

		/// <summary>Step the blame algorithm one iteration.</summary>
		/// <remarks>Step the blame algorithm one iteration.</remarks>
		/// <returns>
		/// true if the generator has found a region's source. The getSource
		/// and
		/// <see cref="GetResultStart()">GetResultStart()</see>
		/// ,
		/// <see cref="GetResultEnd()">GetResultEnd()</see>
		/// methods
		/// can be used to inspect the region found. False if there are no
		/// more regions to describe.
		/// </returns>
		/// <exception cref="System.IO.IOException">repository cannot be read.</exception>
		public virtual bool Next()
		{
			// If there is a source still pending, produce the next region.
			if (currentSource != null)
			{
				Region r = currentSource.regionList;
				Region n = r.next;
				remaining -= r.length;
				if (n != null)
				{
					currentSource.regionList = n;
					return true;
				}
				if (currentSource.queueNext != null)
				{
					return Result(currentSource.queueNext);
				}
				currentSource = null;
			}
			// If there are no lines remaining, the entire result is done,
			// even if there are revisions still available for the path.
			if (remaining == 0)
			{
				return Done();
			}
			for (; ; )
			{
				Candidate n = Pop();
				if (n == null)
				{
					return Done();
				}
				int pCnt = n.GetParentCount();
				if (pCnt == 1)
				{
					if (ProcessOne(n))
					{
						return true;
					}
				}
				else
				{
					if (1 < pCnt)
					{
						if (ProcessMerge(n))
						{
							return true;
						}
					}
					else
					{
						if (n is Candidate.ReverseCandidate)
						{
						}
						else
						{
							// Do not generate a tip of a reverse. The region
							// survives and should not appear to be deleted.
							// Root commit, with at least one surviving region.
							// Assign the remaining blame here.
							return Result(n);
						}
					}
				}
			}
		}

		private bool Done()
		{
			Release();
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Result(Candidate n)
		{
			if (n.sourceCommit != null)
			{
				revPool.ParseBody(n.sourceCommit);
			}
			currentSource = n;
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ReverseResult(Candidate parent, Candidate source)
		{
			// On a reverse blame present the application the parent
			// (as this is what did the removals), however the region
			// list to enumerate is the source's surviving list.
			Candidate res = parent.Copy(parent.sourceCommit);
			res.regionList = source.regionList;
			return Result(res);
		}

		private Candidate Pop()
		{
			Candidate n = queue;
			if (n != null)
			{
				queue = n.queueNext;
				n.queueNext = null;
			}
			return n;
		}

		private void Push(Candidate.BlobCandidate toInsert)
		{
			Candidate c = queue;
			if (c != null)
			{
				c.regionList = null;
				toInsert.parent = c;
			}
			queue = toInsert;
		}

		private void Push(Candidate toInsert)
		{
			// Mark sources to ensure they get discarded (above) if
			// another path to the same commit.
			toInsert.Add(SEEN);
			// Insert into the queue using descending commit time, so
			// the most recent commit will pop next.
			int time = toInsert.GetTime();
			Candidate n = queue;
			if (n == null || time >= n.GetTime())
			{
				toInsert.queueNext = n;
				queue = toInsert;
				return;
			}
			for (Candidate p = n; ; p = n)
			{
				n = p.queueNext;
				if (n == null || time >= n.GetTime())
				{
					toInsert.queueNext = n;
					p.queueNext = toInsert;
					return;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ProcessOne(Candidate n)
		{
			RevCommit parent = n.GetParent(0);
			if (parent == null)
			{
				return Split(n.GetNextCandidate(0), n);
			}
			if (parent.Has(SEEN))
			{
				return false;
			}
			revPool.ParseHeaders(parent);
			if (Find(parent, n.sourcePath))
			{
				if (idBuf.Equals(n.sourceBlob))
				{
					// The common case of the file not being modified in
					// a simple string-of-pearls history. Blame parent.
					n.sourceCommit = parent;
					Push(n);
					return false;
				}
				Candidate next = n.Create(parent, n.sourcePath);
				next.sourceBlob = idBuf.ToObjectId();
				next.LoadText(reader);
				return Split(next, n);
			}
			if (n.sourceCommit == null)
			{
				return Result(n);
			}
			DiffEntry r = FindRename(parent, n.sourceCommit, n.sourcePath);
			if (r == null)
			{
				return Result(n);
			}
			if (0 == r.GetOldId().PrefixCompare(n.sourceBlob))
			{
				// A 100% rename without any content change can also
				// skip directly to the parent.
				n.sourceCommit = parent;
				n.sourcePath = PathFilter.Create(r.GetOldPath());
				Push(n);
				return false;
			}
			Candidate next_1 = n.Create(parent, PathFilter.Create(r.GetOldPath()));
			next_1.sourceBlob = r.GetOldId().ToObjectId();
			next_1.renameScore = r.GetScore();
			next_1.LoadText(reader);
			return Split(next_1, n);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Split(Candidate parent, Candidate source)
		{
			EditList editList = diffAlgorithm.Diff(textComparator, parent.sourceText, source.
				sourceText);
			if (editList.IsEmpty())
			{
				// Ignoring whitespace (or some other special comparator) can
				// cause non-identical blobs to have an empty edit list. In
				// a case like this push the parent alone.
				parent.regionList = source.regionList;
				Push(parent);
				return false;
			}
			parent.TakeBlame(editList, source);
			if (parent.regionList != null)
			{
				Push(parent);
			}
			if (source.regionList != null)
			{
				if (source is Candidate.ReverseCandidate)
				{
					return ReverseResult(parent, source);
				}
				return Result(source);
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool ProcessMerge(Candidate n)
		{
			int pCnt = n.GetParentCount();
			for (int pIdx = 0; pIdx < pCnt; pIdx++)
			{
				RevCommit parent = n.GetParent(pIdx);
				if (parent.Has(SEEN))
				{
					continue;
				}
				revPool.ParseHeaders(parent);
			}
			// If any single parent exactly matches the merge, follow only
			// that one parent through history.
			ObjectId[] ids = null;
			for (int pIdx_1 = 0; pIdx_1 < pCnt; pIdx_1++)
			{
				RevCommit parent = n.GetParent(pIdx_1);
				if (parent.Has(SEEN))
				{
					continue;
				}
				if (!Find(parent, n.sourcePath))
				{
					continue;
				}
				if (!(n is Candidate.ReverseCandidate) && idBuf.Equals(n.sourceBlob))
				{
					n.sourceCommit = parent;
					Push(n);
					return false;
				}
				if (ids == null)
				{
					ids = new ObjectId[pCnt];
				}
				ids[pIdx_1] = idBuf.ToObjectId();
			}
			// If rename detection is enabled, search for any relevant names.
			DiffEntry[] renames = null;
			if (renameDetector != null)
			{
				renames = new DiffEntry[pCnt];
				for (int pIdx_2 = 0; pIdx_2 < pCnt; pIdx_2++)
				{
					RevCommit parent = n.GetParent(pIdx_2);
					if (parent.Has(SEEN))
					{
						continue;
					}
					if (ids != null && ids[pIdx_2] != null)
					{
						continue;
					}
					DiffEntry r = FindRename(parent, n.sourceCommit, n.sourcePath);
					if (r == null)
					{
						continue;
					}
					if (n is Candidate.ReverseCandidate)
					{
						if (ids == null)
						{
							ids = new ObjectId[pCnt];
						}
						ids[pCnt] = r.GetOldId().ToObjectId();
					}
					else
					{
						if (0 == r.GetOldId().PrefixCompare(n.sourceBlob))
						{
							// A 100% rename without any content change can also
							// skip directly to the parent. Note this bypasses an
							// earlier parent that had the path (above) but did not
							// have an exact content match. For performance reasons
							// we choose to follow the one parent over trying to do
							// possibly both parents.
							n.sourceCommit = parent;
							n.sourcePath = PathFilter.Create(r.GetOldPath());
							Push(n);
							return false;
						}
					}
					renames[pIdx_2] = r;
				}
			}
			// Construct the candidate for each parent.
			Candidate[] parents = new Candidate[pCnt];
			for (int pIdx_3 = 0; pIdx_3 < pCnt; pIdx_3++)
			{
				RevCommit parent = n.GetParent(pIdx_3);
				if (parent.Has(SEEN))
				{
					continue;
				}
				Candidate p;
				if (renames != null && renames[pIdx_3] != null)
				{
					p = n.Create(parent, PathFilter.Create(renames[pIdx_3].GetOldPath()));
					p.renameScore = renames[pIdx_3].GetScore();
					p.sourceBlob = renames[pIdx_3].GetOldId().ToObjectId();
				}
				else
				{
					if (ids != null && ids[pIdx_3] != null)
					{
						p = n.Create(parent, n.sourcePath);
						p.sourceBlob = ids[pIdx_3];
					}
					else
					{
						continue;
					}
				}
				EditList editList;
				if (n is Candidate.ReverseCandidate && p.sourceBlob.Equals(n.sourceBlob))
				{
					// This special case happens on ReverseCandidate forks.
					p.sourceText = n.sourceText;
					editList = new EditList(0);
				}
				else
				{
					p.LoadText(reader);
					editList = diffAlgorithm.Diff(textComparator, p.sourceText, n.sourceText);
				}
				if (editList.IsEmpty())
				{
					// Ignoring whitespace (or some other special comparator) can
					// cause non-identical blobs to have an empty edit list. In
					// a case like this push the parent alone.
					if (n is Candidate.ReverseCandidate)
					{
						parents[pIdx_3] = p;
						continue;
					}
					p.regionList = n.regionList;
					Push(p);
					return false;
				}
				p.TakeBlame(editList, n);
				// Only remember this parent candidate if there is at least
				// one region that was blamed on the parent.
				if (p.regionList != null)
				{
					// Reverse blame requires inverting the regions. This puts
					// the regions the parent deleted from us into the parent,
					// and retains the common regions to look at other parents
					// for deletions.
					if (n is Candidate.ReverseCandidate)
					{
						Region r = p.regionList;
						p.regionList = n.regionList;
						n.regionList = r;
					}
					parents[pIdx_3] = p;
				}
			}
			if (n is Candidate.ReverseCandidate)
			{
				// On a reverse blame report all deletions found in the children,
				// and pass on to them a copy of our region list.
				Candidate resultHead = null;
				Candidate resultTail = null;
				for (int pIdx_2 = 0; pIdx_2 < pCnt; pIdx_2++)
				{
					Candidate p = parents[pIdx_2];
					if (p == null)
					{
						continue;
					}
					if (p.regionList != null)
					{
						Candidate r = p.Copy(p.sourceCommit);
						if (resultTail != null)
						{
							resultTail.queueNext = r;
							resultTail = r;
						}
						else
						{
							resultHead = r;
							resultTail = r;
						}
					}
					if (n.regionList != null)
					{
						p.regionList = n.regionList.DeepCopy();
						Push(p);
					}
				}
				if (resultHead != null)
				{
					return Result(resultHead);
				}
				return false;
			}
			// Push any parents that are still candidates.
			for (int pIdx_4 = 0; pIdx_4 < pCnt; pIdx_4++)
			{
				if (parents[pIdx_4] != null)
				{
					Push(parents[pIdx_4]);
				}
			}
			if (n.regionList != null)
			{
				return Result(n);
			}
			return false;
		}

		/// <summary>Get the revision blamed for the current region.</summary>
		/// <remarks>
		/// Get the revision blamed for the current region.
		/// <p>
		/// The source commit may be null if the line was blamed to an uncommitted
		/// revision, such as the working tree copy, or during a reverse blame if the
		/// line survives to the end revision (e.g. the branch tip).
		/// </remarks>
		/// <returns>current revision being blamed.</returns>
		public virtual RevCommit GetSourceCommit()
		{
			return currentSource.sourceCommit;
		}

		/// <returns>current author being blamed.</returns>
		public virtual PersonIdent GetSourceAuthor()
		{
			return currentSource.GetAuthor();
		}

		/// <returns>current committer being blamed.</returns>
		public virtual PersonIdent GetSourceCommitter()
		{
			RevCommit c = GetSourceCommit();
			return c != null ? c.GetCommitterIdent() : null;
		}

		/// <returns>path of the file being blamed.</returns>
		public virtual string GetSourcePath()
		{
			return currentSource.sourcePath.GetPath();
		}

		/// <returns>
		/// rename score if a rename occurred in
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// .
		/// </returns>
		public virtual int GetRenameScore()
		{
			return currentSource.renameScore;
		}

		/// <returns>
		/// first line of the source data that has been blamed for the
		/// current region. This is line number of where the region was added
		/// during
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// in file
		/// <see cref="GetSourcePath()">GetSourcePath()</see>
		/// .
		/// </returns>
		public virtual int GetSourceStart()
		{
			return currentSource.regionList.sourceStart;
		}

		/// <returns>
		/// one past the range of the source data that has been blamed for
		/// the current region. This is line number of where the region was
		/// added during
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// in file
		/// <see cref="GetSourcePath()">GetSourcePath()</see>
		/// .
		/// </returns>
		public virtual int GetSourceEnd()
		{
			Region r = currentSource.regionList;
			return r.sourceStart + r.length;
		}

		/// <returns>
		/// first line of the result that
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// has been
		/// blamed for providing. Line numbers use 0 based indexing.
		/// </returns>
		public virtual int GetResultStart()
		{
			return currentSource.regionList.resultStart;
		}

		/// <returns>
		/// one past the range of the result that
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// has been blamed for providing. Line numbers use 0 based indexing.
		/// Because a source cannot be blamed for an empty region of the
		/// result,
		/// <see cref="GetResultEnd()">GetResultEnd()</see>
		/// is always at least one larger
		/// than
		/// <see cref="GetResultStart()">GetResultStart()</see>
		/// .
		/// </returns>
		public virtual int GetResultEnd()
		{
			Region r = currentSource.regionList;
			return r.resultStart + r.length;
		}

		/// <returns>
		/// number of lines in the current region being blamed to
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// . This is always the value of the
		/// expression
		/// <code>getResultEnd() - getResultStart()</code>
		/// , but also
		/// <code>getSourceEnd() - getSourceStart()</code>
		/// .
		/// </returns>
		public virtual int GetRegionLength()
		{
			return currentSource.regionList.length;
		}

		/// <returns>
		/// complete contents of the source file blamed for the current
		/// output region. This is the contents of
		/// <see cref="GetSourcePath()">GetSourcePath()</see>
		/// within
		/// <see cref="GetSourceCommit()">GetSourceCommit()</see>
		/// . The source contents is
		/// temporarily available as an artifact of the blame algorithm. Most
		/// applications will want the result contents for display to users.
		/// </returns>
		public virtual RawText GetSourceContents()
		{
			return currentSource.sourceText;
		}

		/// <returns>
		/// complete file contents of the result file blame is annotating.
		/// This value is accessible only after being configured and only
		/// immediately before the first call to
		/// <see cref="Next()">Next()</see>
		/// . Returns
		/// null if the path does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">repository cannot be read.</exception>
		/// <exception cref="System.InvalidOperationException">
		/// <see cref="Next()">Next()</see>
		/// has already been invoked.
		/// </exception>
		public virtual RawText GetResultContents()
		{
			return queue != null ? queue.sourceText : null;
		}

		/// <summary>Release the current blame session.</summary>
		/// <remarks>Release the current blame session.</remarks>
		public virtual void Release()
		{
			revPool.Release();
			queue = null;
			currentSource = null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool Find(RevCommit commit, PathFilter path)
		{
			treeWalk.Filter = path;
			treeWalk.Reset(commit.Tree);
			while (treeWalk.Next())
			{
				if (path.IsDone(treeWalk))
				{
					if (treeWalk.GetFileMode(0).GetObjectType() != Constants.OBJ_BLOB)
					{
						return false;
					}
					treeWalk.GetObjectId(idBuf, 0);
					return true;
				}
				if (treeWalk.IsSubtree)
				{
					treeWalk.EnterSubtree();
				}
			}
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private DiffEntry FindRename(RevCommit parent, RevCommit commit, PathFilter path)
		{
			if (renameDetector == null)
			{
				return null;
			}
			treeWalk.Filter = TreeFilter.ANY_DIFF;
			treeWalk.Reset(parent.Tree, commit.Tree);
			renameDetector.Reset();
			renameDetector.AddAll(DiffEntry.Scan(treeWalk));
			foreach (DiffEntry ent in renameDetector.Compute())
			{
				if (IsRename(ent) && ent.GetNewPath().Equals(path.GetPath()))
				{
					return ent;
				}
			}
			return null;
		}

		private static bool IsRename(DiffEntry ent)
		{
			return ent.GetChangeType() == DiffEntry.ChangeType.RENAME || ent.GetChangeType() 
				== DiffEntry.ChangeType.COPY;
		}
	}
}
