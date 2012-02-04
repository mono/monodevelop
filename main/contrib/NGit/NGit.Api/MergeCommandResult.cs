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

using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Api;
using NGit.Diff;
using NGit.Merge;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Encapsulates the result of a
	/// <see cref="MergeCommand">MergeCommand</see>
	/// .
	/// </summary>
	public class MergeCommandResult
	{
		private ObjectId[] mergedCommits;

		private ObjectId @base;

		private ObjectId newHead;

		private IDictionary<string, int[][]> conflicts;

		private MergeStatus mergeStatus;

		private string description;

		private MergeStrategy mergeStrategy;

		private IDictionary<string, ResolveMerger.MergeFailureReason> failingPaths;

		/// <param name="newHead">the object the head points at after the merge</param>
		/// <param name="base">
		/// the common base which was used to produce a content-merge. May
		/// be <code>null</code> if the merge-result was produced without
		/// computing a common base
		/// </param>
		/// <param name="mergedCommits">all the commits which have been merged together</param>
		/// <param name="mergeStatus">the status the merge resulted in</param>
		/// <param name="lowLevelResults">
		/// merge results as returned by
		/// <see cref="NGit.Merge.ResolveMerger.GetMergeResults()">NGit.Merge.ResolveMerger.GetMergeResults()
		/// 	</see>
		/// </param>
		/// <param name="mergeStrategy">
		/// the used
		/// <see cref="NGit.Merge.MergeStrategy">NGit.Merge.MergeStrategy</see>
		/// </param>
		public MergeCommandResult(ObjectId newHead, ObjectId @base, ObjectId[] mergedCommits
			, MergeStatus mergeStatus, IDictionary<string, MergeResult<Sequence>> lowLevelResults
			, MergeStrategy mergeStrategy) : this(newHead, @base, mergedCommits, mergeStatus
			, mergeStrategy, lowLevelResults, null)
		{
		}

		/// <param name="newHead">the object the head points at after the merge</param>
		/// <param name="base">
		/// the common base which was used to produce a content-merge. May
		/// be <code>null</code> if the merge-result was produced without
		/// computing a common base
		/// </param>
		/// <param name="mergedCommits">all the commits which have been merged together</param>
		/// <param name="mergeStatus">the status the merge resulted in</param>
		/// <param name="mergeStrategy">
		/// the used
		/// <see cref="NGit.Merge.MergeStrategy">NGit.Merge.MergeStrategy</see>
		/// </param>
		/// <param name="lowLevelResults">
		/// merge results as returned by
		/// <see cref="NGit.Merge.ResolveMerger.GetMergeResults()">NGit.Merge.ResolveMerger.GetMergeResults()
		/// 	</see>
		/// </param>
		/// <param name="description">a user friendly description of the merge result</param>
		public MergeCommandResult(ObjectId newHead, ObjectId @base, ObjectId[] mergedCommits
			, MergeStatus mergeStatus, MergeStrategy mergeStrategy, IDictionary<string, MergeResult
			<Sequence>> lowLevelResults, string description) : this(newHead, @base, mergedCommits
			, mergeStatus, mergeStrategy, lowLevelResults, null, null)
		{
		}

		/// <param name="newHead">the object the head points at after the merge</param>
		/// <param name="base">
		/// the common base which was used to produce a content-merge. May
		/// be <code>null</code> if the merge-result was produced without
		/// computing a common base
		/// </param>
		/// <param name="mergedCommits">all the commits which have been merged together</param>
		/// <param name="mergeStatus">the status the merge resulted in</param>
		/// <param name="mergeStrategy">
		/// the used
		/// <see cref="NGit.Merge.MergeStrategy">NGit.Merge.MergeStrategy</see>
		/// </param>
		/// <param name="lowLevelResults">
		/// merge results as returned by
		/// <see cref="NGit.Merge.ResolveMerger.GetMergeResults()">NGit.Merge.ResolveMerger.GetMergeResults()
		/// 	</see>
		/// </param>
		/// <param name="failingPaths">
		/// list of paths causing this merge to fail as returned by
		/// <see cref="NGit.Merge.ResolveMerger.GetFailingPaths()">NGit.Merge.ResolveMerger.GetFailingPaths()
		/// 	</see>
		/// </param>
		/// <param name="description">a user friendly description of the merge result</param>
		public MergeCommandResult(ObjectId newHead, ObjectId @base, ObjectId[] mergedCommits
			, MergeStatus mergeStatus, MergeStrategy mergeStrategy, IDictionary<string, MergeResult
			<Sequence>> lowLevelResults, IDictionary<string, ResolveMerger.MergeFailureReason>
			 failingPaths, string description)
		{
			this.newHead = newHead;
			this.mergedCommits = mergedCommits;
			this.@base = @base;
			this.mergeStatus = mergeStatus;
			this.mergeStrategy = mergeStrategy;
			this.description = description;
			this.failingPaths = failingPaths;
			if (lowLevelResults != null)
			{
				foreach (KeyValuePair<string, MergeResult<Sequence>> result in lowLevelResults.EntrySet
					())
				{
					AddConflict(result.Key, result.Value);
				}
			}
		}

		/// <returns>the object the head points at after the merge</returns>
		public virtual ObjectId GetNewHead()
		{
			return newHead;
		}

		/// <returns>the status the merge resulted in</returns>
		public virtual MergeStatus GetMergeStatus()
		{
			return mergeStatus;
		}

		/// <returns>all the commits which have been merged together</returns>
		public virtual ObjectId[] GetMergedCommits()
		{
			return mergedCommits;
		}

		/// <returns>
		/// base the common base which was used to produce a content-merge.
		/// May be <code>null</code> if the merge-result was produced without
		/// computing a common base
		/// </returns>
		public virtual ObjectId GetBase()
		{
			return @base;
		}

		public override string ToString()
		{
			bool first = true;
			StringBuilder commits = new StringBuilder();
			foreach (ObjectId commit in mergedCommits)
			{
				if (!first)
				{
					commits.Append(", ");
				}
				else
				{
					first = false;
				}
				commits.Append(ObjectId.ToString(commit));
			}
			return MessageFormat.Format(JGitText.Get().mergeUsingStrategyResultedInDescription
				, commits, ObjectId.ToString(@base), mergeStrategy.GetName(), mergeStatus, (description
				 == null ? string.Empty : ", " + description));
		}

		/// <param name="conflicts">the conflicts to set</param>
		public virtual void SetConflicts(IDictionary<string, int[][]> conflicts)
		{
			this.conflicts = conflicts;
		}

		/// <param name="path"></param>
		/// <param name="conflictingRanges">the conflicts to set</param>
		public virtual void AddConflict(string path, int[][] conflictingRanges)
		{
			if (conflicts == null)
			{
				conflicts = new Dictionary<string, int[][]>();
			}
			conflicts.Put(path, conflictingRanges);
		}

		/// <param name="path"></param>
		/// <param name="lowLevelResult"></param>
		public virtual void AddConflict<_T0>(string path, MergeResult<_T0> lowLevelResult
			) where _T0:Sequence
		{
			if (!lowLevelResult.ContainsConflicts())
			{
				return;
			}
			if (conflicts == null)
			{
				conflicts = new Dictionary<string, int[][]>();
			}
			int nrOfConflicts = 0;
			// just counting
			foreach (MergeChunk mergeChunk in lowLevelResult)
			{
				if (mergeChunk.GetConflictState().Equals(MergeChunk.ConflictState.FIRST_CONFLICTING_RANGE
					))
				{
					nrOfConflicts++;
				}
			}
			int currentConflict = -1;
			int[][] ret = new int[nrOfConflicts][];
			for (int n = 0; n < nrOfConflicts; n++)
			{
				ret[n] = new int[mergedCommits.Length + 1];
			}
			foreach (MergeChunk mergeChunk_1 in lowLevelResult)
			{
				// to store the end of this chunk (end of the last conflicting range)
				int endOfChunk = 0;
				if (mergeChunk_1.GetConflictState().Equals(MergeChunk.ConflictState.FIRST_CONFLICTING_RANGE
					))
				{
					if (currentConflict > -1)
					{
						// there was a previous conflicting range for which the end
						// is not set yet - set it!
						ret[currentConflict][mergedCommits.Length] = endOfChunk;
					}
					currentConflict++;
					endOfChunk = mergeChunk_1.GetEnd();
					ret[currentConflict][mergeChunk_1.GetSequenceIndex()] = mergeChunk_1.GetBegin();
				}
				if (mergeChunk_1.GetConflictState().Equals(MergeChunk.ConflictState.NEXT_CONFLICTING_RANGE
					))
				{
					if (mergeChunk_1.GetEnd() > endOfChunk)
					{
						endOfChunk = mergeChunk_1.GetEnd();
					}
					ret[currentConflict][mergeChunk_1.GetSequenceIndex()] = mergeChunk_1.GetBegin();
				}
			}
			conflicts.Put(path, ret);
		}

		/// <summary>
		/// Returns information about the conflicts which occurred during a
		/// <see cref="MergeCommand">MergeCommand</see>
		/// . The returned value maps the path of a conflicting
		/// file to a two-dimensional int-array of line-numbers telling where in the
		/// file conflict markers for which merged commit can be found.
		/// <p>
		/// If the returned value contains a mapping "path"-&gt;[x][y]=z then this means
		/// <ul>
		/// <li>the file with path "path" contains conflicts</li>
		/// <li>if y &lt; "number of merged commits": for conflict number x in this file
		/// the chunk which was copied from commit number y starts on line number z.
		/// All numberings and line numbers start with 0.</li>
		/// <li>if y == "number of merged commits": the first non-conflicting line
		/// after conflict number x starts at line number z</li>
		/// </ul>
		/// <p>
		/// Example code how to parse this data:
		/// <pre> MergeResult m=...;
		/// Map<String, int[][]> allConflicts = m.getConflicts();
		/// for (String path : allConflicts.keySet()) {
		/// int[][] c = allConflicts.get(path);
		/// System.out.println("Conflicts in file " + path);
		/// for (int i = 0; i &lt; c.length; ++i) {
		/// System.out.println("  Conflict #" + i);
		/// for (int j = 0; j &lt; (c[i].length) - 1; ++j) {
		/// if (c[i][j] &gt;= 0)
		/// System.out.println("    Chunk for "
		/// + m.getMergedCommits()[j] + " starts on line #"
		/// + c[i][j]);
		/// }
		/// }
		/// }</pre>
		/// </summary>
		/// <returns>the conflicts or <code>null</code> if no conflict occurred</returns>
		public virtual IDictionary<string, int[][]> GetConflicts()
		{
			return conflicts;
		}

		/// <summary>
		/// Returns a list of paths causing this merge to fail as returned by
		/// <see cref="NGit.Merge.ResolveMerger.GetFailingPaths()">NGit.Merge.ResolveMerger.GetFailingPaths()
		/// 	</see>
		/// </summary>
		/// <returns>
		/// the list of paths causing this merge to fail or <code>null</code>
		/// if no failure occurred
		/// </returns>
		public virtual IDictionary<string, ResolveMerger.MergeFailureReason> GetFailingPaths
			()
		{
			return failingPaths;
		}
	}

	public abstract class MergeStatus
	{
		public static MergeStatus FAST_FORWARD = new MergeStatus.FAST_FORWARD_Class
			();

		public static MergeStatus ALREADY_UP_TO_DATE = new MergeStatus.ALREADY_UP_TO_DATE_Class
			();

		public static MergeStatus FAILED = new MergeStatus.FAILED_Class();

		public static MergeStatus MERGED = new MergeStatus.MERGED_Class();

		public static MergeStatus CONFLICTING = new MergeStatus.CONFLICTING_Class
			();

		public static MergeStatus NOT_SUPPORTED = new MergeStatus.NOT_SUPPORTED_Class
			();

		internal class FAST_FORWARD_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Fast-forward";
			}

			public override bool IsSuccessful()
			{
				return true;
			}
		}

		internal class ALREADY_UP_TO_DATE_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Already-up-to-date";
			}

			public override bool IsSuccessful()
			{
				return true;
			}
		}

		internal class FAILED_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Failed";
			}

			public override bool IsSuccessful()
			{
				return false;
			}
		}

		internal class MERGED_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Merged";
			}

			public override bool IsSuccessful()
			{
				return true;
			}
		}

		internal class CONFLICTING_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Conflicting";
			}

			public override bool IsSuccessful()
			{
				return false;
			}
		}

		internal class NOT_SUPPORTED_Class : MergeStatus
		{
			public override string ToString()
			{
				return "Not-yet-supported";
			}

			public override bool IsSuccessful()
			{
				return false;
			}
		}

		/// <returns>whether the status indicates a successful result</returns>
		public abstract bool IsSuccessful();
	}
}
