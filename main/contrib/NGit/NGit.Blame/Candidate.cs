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

using System.Text;
using NGit;
using NGit.Blame;
using NGit.Diff;
using NGit.Revwalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Blame
{
	/// <summary>A source that may have supplied some (or all) of the result file.</summary>
	/// <remarks>
	/// A source that may have supplied some (or all) of the result file.
	/// <p>
	/// Candidates are kept in a queue by BlameGenerator, allowing the generator to
	/// perform a parallel search down the parents of any merges that are discovered
	/// during the history traversal. Each candidate retains a
	/// <see cref="regionList">regionList</see>
	/// describing sections of the result file the candidate has taken responsibility
	/// for either directly or indirectly through its history. Actual blame from this
	/// region list will be assigned to the candidate when its ancestor commit(s) are
	/// themselves converted into Candidate objects and the ancestor's candidate uses
	/// <see cref="TakeBlame(NGit.Diff.EditList, Candidate)">TakeBlame(NGit.Diff.EditList, Candidate)
	/// 	</see>
	/// to accept responsibility for sections
	/// of the result.
	/// </remarks>
	internal class Candidate
	{
		/// <summary>Next candidate in the candidate queue.</summary>
		/// <remarks>Next candidate in the candidate queue.</remarks>
		internal NGit.Blame.Candidate queueNext;

		/// <summary>Commit being considered (or blamed, depending on state).</summary>
		/// <remarks>Commit being considered (or blamed, depending on state).</remarks>
		internal RevCommit sourceCommit;

		/// <summary>
		/// Path of the candidate file in
		/// <see cref="sourceCommit">sourceCommit</see>
		/// .
		/// </summary>
		internal PathFilter sourcePath;

		/// <summary>
		/// Unique name of the candidate blob in
		/// <see cref="sourceCommit">sourceCommit</see>
		/// .
		/// </summary>
		internal ObjectId sourceBlob;

		/// <summary>
		/// Complete contents of the file in
		/// <see cref="sourceCommit">sourceCommit</see>
		/// .
		/// </summary>
		internal RawText sourceText;

		/// <summary>Chain of regions this candidate may be blamed for.</summary>
		/// <remarks>
		/// Chain of regions this candidate may be blamed for.
		/// <p>
		/// This list is always kept sorted by resultStart order, making it simple to
		/// merge-join with the sorted EditList during blame assignment.
		/// </remarks>
		internal Region regionList;

		/// <summary>Score assigned to the rename to this candidate.</summary>
		/// <remarks>
		/// Score assigned to the rename to this candidate.
		/// <p>
		/// Consider the history "A&lt;-B&lt;-C". If the result file S in C was renamed to
		/// R in B, the rename score for this rename will be held in this field by
		/// the candidate object for B. By storing the score with B, the application
		/// can see what the rename score was as it makes the transition from C/S to
		/// B/R. This may seem backwards since it was C that performed the rename,
		/// but the application doesn't learn about path R until B.
		/// </remarks>
		internal int renameScore;

		internal Candidate(RevCommit commit, PathFilter path)
		{
			sourceCommit = commit;
			sourcePath = path;
		}

		internal virtual int GetParentCount()
		{
			return sourceCommit.ParentCount;
		}

		internal virtual RevCommit GetParent(int idx)
		{
			return sourceCommit.GetParent(idx);
		}

		internal virtual NGit.Blame.Candidate GetNextCandidate(int idx)
		{
			return null;
		}

		internal virtual void Add(RevFlag flag)
		{
			sourceCommit.Add(flag);
		}

		internal virtual int GetTime()
		{
			return sourceCommit.CommitTime;
		}

		internal virtual PersonIdent GetAuthor()
		{
			return sourceCommit.GetAuthorIdent();
		}

		internal virtual NGit.Blame.Candidate Create(RevCommit commit, PathFilter path)
		{
			return new NGit.Blame.Candidate(commit, path);
		}

		internal virtual NGit.Blame.Candidate Copy(RevCommit commit)
		{
			NGit.Blame.Candidate r = Create(commit, sourcePath);
			r.sourceBlob = sourceBlob;
			r.sourceText = sourceText;
			r.regionList = regionList;
			r.renameScore = renameScore;
			return r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void LoadText(ObjectReader reader)
		{
			ObjectLoader ldr = reader.Open(sourceBlob, Constants.OBJ_BLOB);
			sourceText = new RawText(ldr.GetCachedBytes(int.MaxValue));
		}

		internal virtual void TakeBlame(EditList editList, NGit.Blame.Candidate child)
		{
			Blame(editList, this, child);
		}

		private static void Blame(EditList editList, NGit.Blame.Candidate a, NGit.Blame.Candidate
			 b)
		{
			Region r = b.ClearRegionList();
			Region aTail = null;
			Region bTail = null;
			for (int eIdx = 0; eIdx < editList.Count; )
			{
				// If there are no more regions left, neither side has any
				// more responsibility for the result. Remaining edits can
				// be safely ignored.
				if (r == null)
				{
					return;
				}
				Edit e = editList[eIdx];
				// Edit ends before the next candidate region. Skip the edit.
				if (e.GetEndB() <= r.sourceStart)
				{
					eIdx++;
					continue;
				}
				// Next candidate region starts before the edit. Assign some
				// of the blame onto A, but possibly split and also on B.
				if (r.sourceStart < e.GetBeginB())
				{
					int d = e.GetBeginB() - r.sourceStart;
					if (r.length <= d)
					{
						// Pass the blame for this region onto A.
						Region next = r.next;
						r.sourceStart = e.GetBeginA() - d;
						aTail = Add(aTail, a, r);
						r = next;
						continue;
					}
					// Split the region and assign some to A, some to B.
					aTail = Add(aTail, a, r.SplitFirst(e.GetBeginA() - d, d));
					r.SlideAndShrink(d);
				}
				// At this point e.getBeginB() <= r.sourceStart.
				// An empty edit on the B side isn't relevant to this split,
				// as it does not overlap any candidate region.
				if (e.GetLengthB() == 0)
				{
					eIdx++;
					continue;
				}
				// If the region ends before the edit, blame on B.
				int rEnd = r.sourceStart + r.length;
				if (rEnd <= e.GetEndB())
				{
					Region next = r.next;
					bTail = Add(bTail, b, r);
					r = next;
					if (rEnd == e.GetEndB())
					{
						eIdx++;
					}
					continue;
				}
				// This region extends beyond the edit. Blame the first
				// half of the region on B, and process the rest after.
				int len = e.GetEndB() - r.sourceStart;
				bTail = Add(bTail, b, r.SplitFirst(r.sourceStart, len));
				r.SlideAndShrink(len);
				eIdx++;
			}
			if (r == null)
			{
				return;
			}
			// For any remaining region, pass the blame onto A after shifting
			// the source start to account for the difference between the two.
			Edit e_1 = editList[editList.Count - 1];
			int endB = e_1.GetEndB();
			int d_1 = endB - e_1.GetEndA();
			if (aTail == null)
			{
				a.regionList = r;
			}
			else
			{
				aTail.next = r;
			}
			do
			{
				if (endB <= r.sourceStart)
				{
					r.sourceStart -= d_1;
				}
				r = r.next;
			}
			while (r != null);
		}

		private static Region Add(Region aTail, NGit.Blame.Candidate a, Region n)
		{
			// If there is no region on the list, use only this one.
			if (aTail == null)
			{
				a.regionList = n;
				n.next = null;
				return n;
			}
			// If the prior region ends exactly where the new region begins
			// in both the result and the source, combine these together into
			// one contiguous region. This occurs when intermediate commits
			// have inserted and deleted lines in the middle of a region. Try
			// to report this region as a single region to the application,
			// rather than in fragments.
			if (aTail.resultStart + aTail.length == n.resultStart && aTail.sourceStart + aTail
				.length == n.sourceStart)
			{
				aTail.length += n.length;
				return aTail;
			}
			// Append the region onto the end of the list.
			aTail.next = n;
			n.next = null;
			return n;
		}

		private Region ClearRegionList()
		{
			Region r = regionList;
			regionList = null;
			return r;
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("Candidate[");
			r.Append(sourcePath.GetPath());
			if (sourceCommit != null)
			{
				r.Append(" @ ").Append(sourceCommit.Abbreviate(6).Name);
			}
			if (regionList != null)
			{
				r.Append(" regions:").Append(regionList);
			}
			r.Append("]");
			return r.ToString();
		}

		/// <summary>Special candidate type used for reverse blame.</summary>
		/// <remarks>
		/// Special candidate type used for reverse blame.
		/// <p>
		/// Reverse blame inverts the commit history graph to follow from a commit to
		/// its descendant children, rather than the normal history direction of
		/// child to parent. These types require a
		/// <see cref="ReverseCommit">ReverseCommit</see>
		/// which keeps
		/// children pointers, allowing reverse navigation of history.
		/// </remarks>
		internal sealed class ReverseCandidate : Candidate
		{
			internal ReverseCandidate(ReverseWalk.ReverseCommit commit, PathFilter path) : base
				(commit, path)
			{
			}

			internal override int GetParentCount()
			{
				return ((ReverseWalk.ReverseCommit)sourceCommit).GetChildCount();
			}

			internal override RevCommit GetParent(int idx)
			{
				return ((ReverseWalk.ReverseCommit)sourceCommit).GetChild(idx);
			}

			internal override int GetTime()
			{
				// Invert the timestamp so newer dates sort older.
				return -sourceCommit.CommitTime;
			}

			internal override Candidate Create(RevCommit commit, PathFilter path)
			{
				return new Candidate.ReverseCandidate((ReverseWalk.ReverseCommit)commit, path);
			}

			public override string ToString()
			{
				return "Reverse" + base.ToString();
			}
		}

		/// <summary>Candidate loaded from a file source, and not a commit.</summary>
		/// <remarks>
		/// Candidate loaded from a file source, and not a commit.
		/// <p>
		/// The
		/// <see cref="Candidate.sourceCommit">Candidate.sourceCommit</see>
		/// field is always null on this type of
		/// candidate. Instead history traversal follows the single
		/// <see cref="parent">parent</see>
		/// field to discover the next Candidate. Often this is a normal Candidate
		/// type that has a valid sourceCommit.
		/// </remarks>
		internal sealed class BlobCandidate : Candidate
		{
			/// <summary>Next candidate to pass blame onto.</summary>
			/// <remarks>
			/// Next candidate to pass blame onto.
			/// <p>
			/// When computing the differences that this candidate introduced to the
			/// file content, the parent's sourceText is used as the base.
			/// </remarks>
			internal Candidate parent;

			/// <summary>Author name to refer to this blob with.</summary>
			/// <remarks>Author name to refer to this blob with.</remarks>
			internal string description;

			internal BlobCandidate(string name, PathFilter path) : base(null, path)
			{
				description = name;
			}

			internal override int GetParentCount()
			{
				return parent != null ? 1 : 0;
			}

			internal override RevCommit GetParent(int idx)
			{
				return null;
			}

			internal override Candidate GetNextCandidate(int idx)
			{
				return parent;
			}

			internal override void Add(RevFlag flag)
			{
			}

			// Do nothing, sourceCommit is null.
			internal override int GetTime()
			{
				return int.MaxValue;
			}

			internal override PersonIdent GetAuthor()
			{
				return new PersonIdent(description, null);
			}
		}
	}
}
