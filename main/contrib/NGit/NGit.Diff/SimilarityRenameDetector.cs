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
using NGit.Diff;
using NGit.Internal;
using Sharpen;

namespace NGit.Diff
{
	internal class SimilarityRenameDetector
	{
		/// <summary>Number of bits we need to express an index into src or dst list.</summary>
		/// <remarks>
		/// Number of bits we need to express an index into src or dst list.
		/// <p>
		/// This must be 28, giving us a limit of 2^28 entries in either list, which
		/// is an insane limit of 536,870,912 file names being considered in a single
		/// rename pass. The other 8 bits are used to store the score, while staying
		/// under 127 so the long doesn't go negative.
		/// </remarks>
		private const int BITS_PER_INDEX = 28;

		private const int INDEX_MASK = (1 << BITS_PER_INDEX) - 1;

		private const int SCORE_SHIFT = 2 * BITS_PER_INDEX;

		private ContentSource.Pair reader;

		/// <summary>All sources to consider for copies or renames.</summary>
		/// <remarks>
		/// All sources to consider for copies or renames.
		/// <p>
		/// A source is typically a
		/// <see cref="ChangeType.DELETE">ChangeType.DELETE</see>
		/// change, but could be
		/// another type when trying to perform copy detection concurrently with
		/// rename detection.
		/// </remarks>
		private IList<DiffEntry> srcs;

		/// <summary>All destinations to consider looking for a rename.</summary>
		/// <remarks>
		/// All destinations to consider looking for a rename.
		/// <p>
		/// A destination is typically an
		/// <see cref="ChangeType.ADD">ChangeType.ADD</see>
		/// , as the name has
		/// just come into existence, and we want to discover where its initial
		/// content came from.
		/// </remarks>
		private IList<DiffEntry> dsts;

		/// <summary>Matrix of all examined file pairs, and their scores.</summary>
		/// <remarks>
		/// Matrix of all examined file pairs, and their scores.
		/// <p>
		/// The upper 8 bits of each long stores the score, but the score is bounded
		/// to be in the range (0, 128] so that the highest bit is never set, and all
		/// entries are therefore positive.
		/// <p>
		/// List indexes to an element of
		/// <see cref="srcs">srcs</see>
		/// and
		/// <see cref="dsts">dsts</see>
		/// are encoded
		/// as the lower two groups of 28 bits, respectively, but the encoding is
		/// inverted, so that 0 is expressed as
		/// <code>(1 &lt;&lt; 28) - 1</code>
		/// . This sorts
		/// lower list indices later in the matrix, giving precedence to files whose
		/// names sort earlier in the tree.
		/// </remarks>
		private long[] matrix;

		/// <summary>Score a pair must exceed to be considered a rename.</summary>
		/// <remarks>Score a pair must exceed to be considered a rename.</remarks>
		private int renameScore = 60;

		/// <summary>
		/// Set if any
		/// <see cref="TableFullException">TableFullException</see>
		/// occurs.
		/// </summary>
		private bool tableOverflow;

		private IList<DiffEntry> @out;

		internal SimilarityRenameDetector(ContentSource.Pair reader, IList<DiffEntry> srcs
			, IList<DiffEntry> dsts)
		{
			this.reader = reader;
			this.srcs = srcs;
			this.dsts = dsts;
		}

		internal virtual void SetRenameScore(int score)
		{
			renameScore = score;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Compute(ProgressMonitor pm)
		{
			if (pm == null)
			{
				pm = NullProgressMonitor.INSTANCE;
			}
			pm.BeginTask(JGitText.Get().renamesFindingByContent, 2 * srcs.Count * dsts.Count);
			//
			int mNext = BuildMatrix(pm);
			@out = new AList<DiffEntry>(Math.Min(mNext, dsts.Count));
			// Match rename pairs on a first come, first serve basis until
			// we have looked at everything that is above our minimum score.
			//
			for (--mNext; mNext >= 0; mNext--)
			{
				long ent = matrix[mNext];
				int sIdx = SrcFile(ent);
				int dIdx = DstFile(ent);
				DiffEntry s = srcs[sIdx];
				DiffEntry d = dsts[dIdx];
				if (d == null)
				{
					pm.Update(1);
					continue;
				}
				// was already matched earlier
				DiffEntry.ChangeType type;
				if (s.changeType == DiffEntry.ChangeType.DELETE)
				{
					// First use of this source file. Tag it as a rename so we
					// later know it is already been used as a rename, other
					// matches (if any) will claim themselves as copies instead.
					//
					s.changeType = DiffEntry.ChangeType.RENAME;
					type = DiffEntry.ChangeType.RENAME;
				}
				else
				{
					type = DiffEntry.ChangeType.COPY;
				}
				@out.AddItem(DiffEntry.Pair(type, s, d, Score(ent)));
				dsts.Set(dIdx, null);
				// Claim the destination was matched.
				pm.Update(1);
			}
			srcs = CompactSrcList(srcs);
			dsts = CompactDstList(dsts);
			pm.EndTask();
		}

		internal virtual IList<DiffEntry> GetMatches()
		{
			return @out;
		}

		internal virtual IList<DiffEntry> GetLeftOverSources()
		{
			return srcs;
		}

		internal virtual IList<DiffEntry> GetLeftOverDestinations()
		{
			return dsts;
		}

		internal virtual bool IsTableOverflow()
		{
			return tableOverflow;
		}

		private static IList<DiffEntry> CompactSrcList(IList<DiffEntry> @in)
		{
			AList<DiffEntry> r = new AList<DiffEntry>(@in.Count);
			foreach (DiffEntry e in @in)
			{
				if (e.changeType == DiffEntry.ChangeType.DELETE)
				{
					r.AddItem(e);
				}
			}
			return r;
		}

		private static IList<DiffEntry> CompactDstList(IList<DiffEntry> @in)
		{
			AList<DiffEntry> r = new AList<DiffEntry>(@in.Count);
			foreach (DiffEntry e in @in)
			{
				if (e != null)
				{
					r.AddItem(e);
				}
			}
			return r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int BuildMatrix(ProgressMonitor pm)
		{
			// Allocate for the worst-case scenario where every pair has a
			// score that we need to consider. We might not need that many.
			//
			matrix = new long[srcs.Count * dsts.Count];
			long[] srcSizes = new long[srcs.Count];
			long[] dstSizes = new long[dsts.Count];
			BitSet dstTooLarge = null;
			// Consider each pair of files, if the score is above the minimum
			// threshold we need record that scoring in the matrix so we can
			// later find the best matches.
			//
			int mNext = 0;
			for (int srcIdx = 0; srcIdx < srcs.Count; srcIdx++)
			{
				DiffEntry srcEnt = srcs[srcIdx];
				if (!IsFile(srcEnt.oldMode))
				{
					pm.Update(dsts.Count);
					continue;
				}
				SimilarityIndex s = null;
				for (int dstIdx = 0; dstIdx < dsts.Count; dstIdx++)
				{
					DiffEntry dstEnt = dsts[dstIdx];
					if (!IsFile(dstEnt.newMode))
					{
						pm.Update(1);
						continue;
					}
					if (!RenameDetector.SameType(srcEnt.oldMode, dstEnt.newMode))
					{
						pm.Update(1);
						continue;
					}
					if (dstTooLarge != null && dstTooLarge.Get(dstIdx))
					{
						pm.Update(1);
						continue;
					}
					long srcSize = srcSizes[srcIdx];
					if (srcSize == 0)
					{
						srcSize = Size(DiffEntry.Side.OLD, srcEnt) + 1;
						srcSizes[srcIdx] = srcSize;
					}
					long dstSize = dstSizes[dstIdx];
					if (dstSize == 0)
					{
						dstSize = Size(DiffEntry.Side.NEW, dstEnt) + 1;
						dstSizes[dstIdx] = dstSize;
					}
					long max = Math.Max(srcSize, dstSize);
					long min = Math.Min(srcSize, dstSize);
					if (min * 100 / max < renameScore)
					{
						// Cannot possibly match, as the file sizes are so different
						pm.Update(1);
						continue;
					}
					if (s == null)
					{
						try
						{
							s = Hash(DiffEntry.Side.OLD, srcEnt);
						}
						catch (SimilarityIndex.TableFullException)
						{
							tableOverflow = true;
							goto SRC_continue;
						}
					}
					SimilarityIndex d;
					try
					{
						d = Hash(DiffEntry.Side.NEW, dstEnt);
					}
					catch (SimilarityIndex.TableFullException)
					{
						if (dstTooLarge == null)
						{
							dstTooLarge = new BitSet(dsts.Count);
						}
						dstTooLarge.Set(dstIdx);
						tableOverflow = true;
						pm.Update(1);
						continue;
					}
					int contentScore = s.Score(d, 10000);
					// nameScore returns a value between 0 and 100, but we want it
					// to be in the same range as the content score. This allows it
					// to be dropped into the pretty formula for the final score.
					int nameScore = NameScore(srcEnt.oldPath, dstEnt.newPath) * 100;
					int score = (contentScore * 99 + nameScore * 1) / 10000;
					if (score < renameScore)
					{
						pm.Update(1);
						continue;
					}
					matrix[mNext++] = Encode(score, srcIdx, dstIdx);
					pm.Update(1);
				}
SRC_continue: ;
			}
SRC_break: ;
			// Sort everything in the range we populated, which might be the
			// entire matrix, or just a smaller slice if we had some bad low
			// scoring pairs.
			//
			Arrays.Sort(matrix, 0, mNext);
			return mNext;
		}

		internal static int NameScore(string a, string b)
		{
			int aDirLen = a.LastIndexOf("/") + 1;
			int bDirLen = b.LastIndexOf("/") + 1;
			int dirMin = Math.Min(aDirLen, bDirLen);
			int dirMax = Math.Max(aDirLen, bDirLen);
			int dirScoreLtr;
			int dirScoreRtl;
			if (dirMax == 0)
			{
				dirScoreLtr = 100;
				dirScoreRtl = 100;
			}
			else
			{
				int dirSim = 0;
				for (; dirSim < dirMin; dirSim++)
				{
					if (a[dirSim] != b[dirSim])
					{
						break;
					}
				}
				dirScoreLtr = (dirSim * 100) / dirMax;
				if (dirScoreLtr == 100)
				{
					dirScoreRtl = 100;
				}
				else
				{
					for (dirSim = 0; dirSim < dirMin; dirSim++)
					{
						if (a[aDirLen - 1 - dirSim] != b[bDirLen - 1 - dirSim])
						{
							break;
						}
					}
					dirScoreRtl = (dirSim * 100) / dirMax;
				}
			}
			int fileMin = Math.Min(a.Length - aDirLen, b.Length - bDirLen);
			int fileMax = Math.Max(a.Length - aDirLen, b.Length - bDirLen);
			int fileSim = 0;
			for (; fileSim < fileMin; fileSim++)
			{
				if (a[a.Length - 1 - fileSim] != b[b.Length - 1 - fileSim])
				{
					break;
				}
			}
			int fileScore = (fileSim * 100) / fileMax;
			return (((dirScoreLtr + dirScoreRtl) * 25) + (fileScore * 50)) / 100;
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="NGit.Diff.SimilarityIndex.TableFullException"></exception>
		private SimilarityIndex Hash(DiffEntry.Side side, DiffEntry ent)
		{
			SimilarityIndex r = new SimilarityIndex();
			r.Hash(reader.Open(side, ent));
			r.Sort();
			return r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private long Size(DiffEntry.Side side, DiffEntry ent)
		{
			return reader.Size(side, ent);
		}

		private static int Score(long value)
		{
			return (int)((long)(((ulong)value) >> SCORE_SHIFT));
		}

		internal static int SrcFile(long value)
		{
			return DecodeFile(((int)((long)(((ulong)value) >> BITS_PER_INDEX))) & INDEX_MASK);
		}

		internal static int DstFile(long value)
		{
			return DecodeFile(((int)value) & INDEX_MASK);
		}

		internal static long Encode(int score, int srcIdx, int dstIdx)
		{
			return (((long)score) << SCORE_SHIFT) | (EncodeFile(srcIdx) << BITS_PER_INDEX) | 
				EncodeFile(dstIdx);
		}

		//
		//
		private static long EncodeFile(int idx)
		{
			// We invert the index so that the first file in the list sorts
			// later in the table. This permits us to break ties favoring
			// earlier names over later ones.
			//
			return INDEX_MASK - idx;
		}

		private static int DecodeFile(int v)
		{
			return INDEX_MASK - v;
		}

		private static bool IsFile(FileMode mode)
		{
			return (mode.GetBits() & FileMode.TYPE_MASK) == FileMode.TYPE_FILE;
		}
	}
}
