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
using Sharpen;

namespace NGit.Diff
{
	/// <summary>Detect and resolve object renames.</summary>
	/// <remarks>Detect and resolve object renames.</remarks>
	public class RenameDetector
	{
		private const int EXACT_RENAME_SCORE = 100;

		private sealed class _IComparer_72 : IComparer<DiffEntry>
		{
			public _IComparer_72()
			{
			}

			public int Compare(DiffEntry a, DiffEntry b)
			{
				int cmp = this.NameOf(a).CompareTo(this.NameOf(b));
				if (cmp == 0)
				{
					cmp = this.SortOf(a.GetChangeType()) - this.SortOf(b.GetChangeType());
				}
				return cmp;
			}

			private string NameOf(DiffEntry ent)
			{
				// Sort by the new name, unless the change is a delete. On
				// deletes the new name is /dev/null, so we sort instead by
				// the old name.
				//
				if (ent.changeType == DiffEntry.ChangeType.DELETE)
				{
					return ent.oldPath;
				}
				return ent.newPath;
			}

			private int SortOf(DiffEntry.ChangeType changeType)
			{
				switch (changeType)
				{
					case DiffEntry.ChangeType.DELETE:
					{
						// Sort deletes before adds so that a major type change for
						// a file path (such as symlink to regular file) will first
						// remove the path, then add it back with the new type.
						//
						return 1;
					}

					case DiffEntry.ChangeType.ADD:
					{
						return 2;
					}

					default:
					{
						return 10;
						break;
					}
				}
			}
		}

		private static readonly IComparer<DiffEntry> DIFF_COMPARATOR = new _IComparer_72(
			);

		private IList<DiffEntry> entries;

		private IList<DiffEntry> deleted;

		private IList<DiffEntry> added;

		private bool done;

		private readonly Repository repo;

		/// <summary>Similarity score required to pair an add/delete as a rename.</summary>
		/// <remarks>Similarity score required to pair an add/delete as a rename.</remarks>
		private int renameScore = 60;

		/// <summary>Similarity score required to keep modified file pairs together.</summary>
		/// <remarks>
		/// Similarity score required to keep modified file pairs together. Any
		/// modified file pairs with a similarity score below this will be broken
		/// apart.
		/// </remarks>
		private int breakScore = -1;

		/// <summary>Limit in the number of files to consider for renames.</summary>
		/// <remarks>Limit in the number of files to consider for renames.</remarks>
		private int renameLimit;

		/// <summary>Set if the number of adds or deletes was over the limit.</summary>
		/// <remarks>Set if the number of adds or deletes was over the limit.</remarks>
		private bool overRenameLimit;

		/// <summary>Create a new rename detector for the given repository</summary>
		/// <param name="repo">the repository to use for rename detection</param>
		public RenameDetector(Repository repo)
		{
			this.repo = repo;
			DiffConfig cfg = repo.GetConfig().Get(DiffConfig.KEY);
			renameLimit = cfg.GetRenameLimit();
			Reset();
		}

		/// <returns>
		/// minimum score required to pair an add/delete as a rename. The
		/// score ranges are within the bounds of (0, 100).
		/// </returns>
		public virtual int GetRenameScore()
		{
			return renameScore;
		}

		/// <summary>Set the minimum score required to pair an add/delete as a rename.</summary>
		/// <remarks>
		/// Set the minimum score required to pair an add/delete as a rename.
		/// <p>
		/// When comparing two files together their score must be greater than or
		/// equal to the rename score for them to be considered a rename match. The
		/// score is computed based on content similarity, so a score of 60 implies
		/// that approximately 60% of the bytes in the files are identical.
		/// </remarks>
		/// <param name="score">new rename score, must be within [0, 100].</param>
		/// <exception cref="System.ArgumentException">the score was not within [0, 100].</exception>
		public virtual void SetRenameScore(int score)
		{
			if (score < 0 || score > 100)
			{
				throw new ArgumentException(JGitText.Get().similarityScoreMustBeWithinBounds);
			}
			renameScore = score;
		}

		/// <returns>
		/// the similarity score required to keep modified file pairs
		/// together. Any modify pairs that score below this will be broken
		/// apart into separate add/deletes. Values less than or equal to
		/// zero indicate that no modifies will be broken apart. Values over
		/// 100 cause all modify pairs to be broken.
		/// </returns>
		public virtual int GetBreakScore()
		{
			return breakScore;
		}

		/// <param name="breakScore">
		/// the similarity score required to keep modified file pairs
		/// together. Any modify pairs that score below this will be
		/// broken apart into separate add/deletes. Values less than or
		/// equal to zero indicate that no modifies will be broken apart.
		/// Values over 100 cause all modify pairs to be broken.
		/// </param>
		public virtual void SetBreakScore(int breakScore)
		{
			this.breakScore = breakScore;
		}

		/// <returns>limit on number of paths to perform inexact rename detection.</returns>
		public virtual int GetRenameLimit()
		{
			return renameLimit;
		}

		/// <summary>Set the limit on the number of files to perform inexact rename detection.
		/// 	</summary>
		/// <remarks>
		/// Set the limit on the number of files to perform inexact rename detection.
		/// <p>
		/// The rename detector has to build a square matrix of the rename limit on
		/// each side, then perform that many file compares to determine similarity.
		/// If 1000 files are added, and 1000 files are deleted, a 1000*1000 matrix
		/// must be allocated, and 1,000,000 file compares may need to be performed.
		/// </remarks>
		/// <param name="limit">new file limit.</param>
		public virtual void SetRenameLimit(int limit)
		{
			renameLimit = limit;
		}

		/// <summary>Check if the detector is over the rename limit.</summary>
		/// <remarks>
		/// Check if the detector is over the rename limit.
		/// <p>
		/// This method can be invoked either before or after
		/// <code>getEntries</code>
		/// has
		/// been used to perform rename detection.
		/// </remarks>
		/// <returns>
		/// true if the detector has more file additions or removals than the
		/// rename limit is currently set to. In such configurations the
		/// detector will skip expensive computation.
		/// </returns>
		public virtual bool IsOverRenameLimit()
		{
			if (done)
			{
				return overRenameLimit;
			}
			int cnt = Math.Max(added.Count, deleted.Count);
			return GetRenameLimit() != 0 && GetRenameLimit() < cnt;
		}

		/// <summary>Add entries to be considered for rename detection.</summary>
		/// <remarks>Add entries to be considered for rename detection.</remarks>
		/// <param name="entriesToAdd">one or more entries to add.</param>
		/// <exception cref="System.InvalidOperationException">
		/// if
		/// <code>getEntries</code>
		/// was already invoked.
		/// </exception>
		public virtual void AddAll(ICollection<DiffEntry> entriesToAdd)
		{
			if (done)
			{
				throw new InvalidOperationException(JGitText.Get().renamesAlreadyFound);
			}
			foreach (DiffEntry entry in entriesToAdd)
			{
				switch (entry.GetChangeType())
				{
					case DiffEntry.ChangeType.ADD:
					{
						added.AddItem(entry);
						break;
					}

					case DiffEntry.ChangeType.DELETE:
					{
						deleted.AddItem(entry);
						break;
					}

					case DiffEntry.ChangeType.MODIFY:
					{
						if (SameType(entry.GetOldMode(), entry.GetNewMode()))
						{
							entries.AddItem(entry);
						}
						else
						{
							IList<DiffEntry> tmp = DiffEntry.BreakModify(entry);
							deleted.AddItem(tmp[0]);
							added.AddItem(tmp[1]);
						}
						break;
					}

					case DiffEntry.ChangeType.COPY:
					case DiffEntry.ChangeType.RENAME:
					default:
					{
						entriesToAdd.AddItem(entry);
						break;
					}
				}
			}
		}

		/// <summary>Add an entry to be considered for rename detection.</summary>
		/// <remarks>Add an entry to be considered for rename detection.</remarks>
		/// <param name="entry">to add.</param>
		/// <exception cref="System.InvalidOperationException">
		/// if
		/// <code>getEntries</code>
		/// was already invoked.
		/// </exception>
		public virtual void Add(DiffEntry entry)
		{
			AddAll(Sharpen.Collections.SingletonList(entry));
		}

		/// <summary>Detect renames in the current file set.</summary>
		/// <remarks>
		/// Detect renames in the current file set.
		/// <p>
		/// This convenience function runs without a progress monitor.
		/// </remarks>
		/// <returns>
		/// an unmodifiable list of
		/// <see cref="DiffEntry">DiffEntry</see>
		/// s representing all files
		/// that have been changed.
		/// </returns>
		/// <exception cref="System.IO.IOException">file contents cannot be read from the repository.
		/// 	</exception>
		public virtual IList<DiffEntry> Compute()
		{
			return Compute(NullProgressMonitor.INSTANCE);
		}

		/// <summary>Detect renames in the current file set.</summary>
		/// <remarks>Detect renames in the current file set.</remarks>
		/// <param name="pm">report progress during the detection phases.</param>
		/// <returns>
		/// an unmodifiable list of
		/// <see cref="DiffEntry">DiffEntry</see>
		/// s representing all files
		/// that have been changed.
		/// </returns>
		/// <exception cref="System.IO.IOException">file contents cannot be read from the repository.
		/// 	</exception>
		public virtual IList<DiffEntry> Compute(ProgressMonitor pm)
		{
			if (!done)
			{
				ObjectReader reader = repo.NewObjectReader();
				try
				{
					return Compute(reader, pm);
				}
				finally
				{
					reader.Release();
				}
			}
			return Sharpen.Collections.UnmodifiableList(entries);
		}

		/// <summary>Detect renames in the current file set.</summary>
		/// <remarks>Detect renames in the current file set.</remarks>
		/// <param name="reader">reader to obtain objects from the repository with.</param>
		/// <param name="pm">report progress during the detection phases.</param>
		/// <returns>
		/// an unmodifiable list of
		/// <see cref="DiffEntry">DiffEntry</see>
		/// s representing all files
		/// that have been changed.
		/// </returns>
		/// <exception cref="System.IO.IOException">file contents cannot be read from the repository.
		/// 	</exception>
		public virtual IList<DiffEntry> Compute(ObjectReader reader, ProgressMonitor pm)
		{
			ContentSource cs = ContentSource.Create(reader);
			return Compute(new ContentSource.Pair(cs, cs), pm);
		}

		/// <summary>Detect renames in the current file set.</summary>
		/// <remarks>Detect renames in the current file set.</remarks>
		/// <param name="reader">reader to obtain objects from the repository with.</param>
		/// <param name="pm">report progress during the detection phases.</param>
		/// <returns>
		/// an unmodifiable list of
		/// <see cref="DiffEntry">DiffEntry</see>
		/// s representing all files
		/// that have been changed.
		/// </returns>
		/// <exception cref="System.IO.IOException">file contents cannot be read from the repository.
		/// 	</exception>
		public virtual IList<DiffEntry> Compute(ContentSource.Pair reader, ProgressMonitor
			 pm)
		{
			if (!done)
			{
				done = true;
				if (pm == null)
				{
					pm = NullProgressMonitor.INSTANCE;
				}
				if (0 < breakScore)
				{
					BreakModifies(reader, pm);
				}
				if (!added.IsEmpty() && !deleted.IsEmpty())
				{
					FindExactRenames(pm);
				}
				if (!added.IsEmpty() && !deleted.IsEmpty())
				{
					FindContentRenames(reader, pm);
				}
				if (0 < breakScore && !added.IsEmpty() && !deleted.IsEmpty())
				{
					RejoinModifies(pm);
				}
				Sharpen.Collections.AddAll(entries, added);
				added = null;
				Sharpen.Collections.AddAll(entries, deleted);
				deleted = null;
				entries.Sort(DIFF_COMPARATOR);
			}
			return Sharpen.Collections.UnmodifiableList(entries);
		}

		/// <summary>Reset this rename detector for another rename detection pass.</summary>
		/// <remarks>Reset this rename detector for another rename detection pass.</remarks>
		public virtual void Reset()
		{
			entries = new AList<DiffEntry>();
			deleted = new AList<DiffEntry>();
			added = new AList<DiffEntry>();
			done = false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void BreakModifies(ContentSource.Pair reader, ProgressMonitor pm)
		{
			AList<DiffEntry> newEntries = new AList<DiffEntry>(entries.Count);
			pm.BeginTask(JGitText.Get().renamesBreakingModifies, entries.Count);
			for (int i = 0; i < entries.Count; i++)
			{
				DiffEntry e = entries[i];
				if (e.GetChangeType() == DiffEntry.ChangeType.MODIFY)
				{
					int score = CalculateModifyScore(reader, e);
					if (score < breakScore)
					{
						IList<DiffEntry> tmp = DiffEntry.BreakModify(e);
						DiffEntry del = tmp[0];
						del.score = score;
						deleted.AddItem(del);
						added.AddItem(tmp[1]);
					}
					else
					{
						newEntries.AddItem(e);
					}
				}
				else
				{
					newEntries.AddItem(e);
				}
				pm.Update(1);
			}
			entries = newEntries;
		}

		private void RejoinModifies(ProgressMonitor pm)
		{
			Dictionary<string, DiffEntry> nameMap = new Dictionary<string, DiffEntry>();
			AList<DiffEntry> newAdded = new AList<DiffEntry>(added.Count);
			pm.BeginTask(JGitText.Get().renamesRejoiningModifies, added.Count + deleted.Count
				);
			foreach (DiffEntry src in deleted)
			{
				nameMap.Put(src.oldPath, src);
				pm.Update(1);
			}
			foreach (DiffEntry dst in added)
			{
				DiffEntry src_1 = Sharpen.Collections.Remove(nameMap, dst.newPath);
				if (src_1 != null)
				{
					if (SameType(src_1.oldMode, dst.newMode))
					{
						entries.AddItem(DiffEntry.Pair(DiffEntry.ChangeType.MODIFY, src_1, dst, src_1.score
							));
					}
					else
					{
						nameMap.Put(src_1.oldPath, src_1);
						newAdded.AddItem(dst);
					}
				}
				else
				{
					newAdded.AddItem(dst);
				}
				pm.Update(1);
			}
			added = newAdded;
			deleted = new AList<DiffEntry>(nameMap.Values);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int CalculateModifyScore(ContentSource.Pair reader, DiffEntry d)
		{
			try
			{
				SimilarityIndex src = new SimilarityIndex();
				src.Hash(reader.Open(DiffEntry.Side.OLD, d));
				src.Sort();
				SimilarityIndex dst = new SimilarityIndex();
				dst.Hash(reader.Open(DiffEntry.Side.NEW, d));
				dst.Sort();
				return src.Score(dst, 100);
			}
			catch (SimilarityIndex.TableFullException)
			{
				// If either table overflowed while being constructed, don't allow
				// the pair to be broken. Returning 1 higher than breakScore will
				// ensure its not similar, but not quite dissimilar enough to break.
				//
				overRenameLimit = true;
				return breakScore + 1;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void FindContentRenames(ContentSource.Pair reader, ProgressMonitor pm)
		{
			int cnt = Math.Max(added.Count, deleted.Count);
			if (GetRenameLimit() == 0 || cnt <= GetRenameLimit())
			{
				SimilarityRenameDetector d;
				d = new SimilarityRenameDetector(reader, deleted, added);
				d.SetRenameScore(GetRenameScore());
				d.Compute(pm);
				overRenameLimit |= d.IsTableOverflow();
				deleted = d.GetLeftOverSources();
				added = d.GetLeftOverDestinations();
				Sharpen.Collections.AddAll(entries, d.GetMatches());
			}
			else
			{
				overRenameLimit = true;
			}
		}

		private void FindExactRenames(ProgressMonitor pm)
		{
			pm.BeginTask(JGitText.Get().renamesFindingExact, added.Count + added.Count + deleted
				.Count + added.Count * deleted.Count);
			//
			Dictionary<AbbreviatedObjectId, object> deletedMap = PopulateMap(deleted, pm);
			Dictionary<AbbreviatedObjectId, object> addedMap = PopulateMap(added, pm);
			AList<DiffEntry> uniqueAdds = new AList<DiffEntry>(added.Count);
			AList<IList<DiffEntry>> nonUniqueAdds = new AList<IList<DiffEntry>>();
			foreach (object o in addedMap.Values)
			{
				if (o is DiffEntry)
				{
					uniqueAdds.AddItem((DiffEntry)o);
				}
				else
				{
					nonUniqueAdds.AddItem((IList<DiffEntry>)o);
				}
			}
			AList<DiffEntry> left = new AList<DiffEntry>(added.Count);
			foreach (DiffEntry a in uniqueAdds)
			{
				object del = deletedMap.Get(a.newId);
				if (del is DiffEntry)
				{
					// We have one add to one delete: pair them if they are the same
					// type
					DiffEntry e = (DiffEntry)del;
					if (SameType(e.oldMode, a.newMode))
					{
						e.changeType = DiffEntry.ChangeType.RENAME;
						entries.AddItem(ExactRename(e, a));
					}
					else
					{
						left.AddItem(a);
					}
				}
				else
				{
					if (del != null)
					{
						// We have one add to many deletes: find the delete with the
						// same type and closest name to the add, then pair them
						IList<DiffEntry> list = (IList<DiffEntry>)del;
						DiffEntry best = BestPathMatch(a, list);
						if (best != null)
						{
							best.changeType = DiffEntry.ChangeType.RENAME;
							entries.AddItem(ExactRename(best, a));
						}
						else
						{
							left.AddItem(a);
						}
					}
					else
					{
						left.AddItem(a);
					}
				}
				pm.Update(1);
			}
			foreach (IList<DiffEntry> adds in nonUniqueAdds)
			{
				object o_1 = deletedMap.Get(adds[0].newId);
				if (o_1 is DiffEntry)
				{
					// We have many adds to one delete: find the add with the same
					// type and closest name to the delete, then pair them. Mark the
					// rest as copies of the delete.
					DiffEntry d = (DiffEntry)o_1;
					DiffEntry best = BestPathMatch(d, adds);
					if (best != null)
					{
						d.changeType = DiffEntry.ChangeType.RENAME;
						entries.AddItem(ExactRename(d, best));
						foreach (DiffEntry a_1 in adds)
						{
							if (a_1 != best)
							{
								if (SameType(d.oldMode, a_1.newMode))
								{
									entries.AddItem(ExactCopy(d, a_1));
								}
								else
								{
									left.AddItem(a_1);
								}
							}
						}
					}
					else
					{
						Sharpen.Collections.AddAll(left, adds);
					}
				}
				else
				{
					if (o_1 != null)
					{
						// We have many adds to many deletes: score all the adds against
						// all the deletes by path name, take the best matches, pair
						// them as renames, then call the rest copies
						IList<DiffEntry> dels = (IList<DiffEntry>)o_1;
						long[] matrix = new long[dels.Count * adds.Count];
						int mNext = 0;
						for (int delIdx = 0; delIdx < dels.Count; delIdx++)
						{
							string deletedName = dels[delIdx].oldPath;
							for (int addIdx = 0; addIdx < adds.Count; addIdx++)
							{
								string addedName = adds[addIdx].newPath;
								int score = SimilarityRenameDetector.NameScore(addedName, deletedName);
								matrix[mNext] = SimilarityRenameDetector.Encode(score, delIdx, addIdx);
								mNext++;
							}
						}
						Arrays.Sort(matrix);
						for (--mNext; mNext >= 0; mNext--)
						{
							long ent = matrix[mNext];
							int delIdx_1 = SimilarityRenameDetector.SrcFile(ent);
							int addIdx = SimilarityRenameDetector.DstFile(ent);
							DiffEntry d = dels[delIdx_1];
							DiffEntry a_1 = adds[addIdx];
							if (a_1 == null)
							{
								pm.Update(1);
								continue;
							}
							// was already matched earlier
							DiffEntry.ChangeType type;
							if (d.changeType == DiffEntry.ChangeType.DELETE)
							{
								// First use of this source file. Tag it as a rename so we
								// later know it is already been used as a rename, other
								// matches (if any) will claim themselves as copies instead.
								//
								d.changeType = DiffEntry.ChangeType.RENAME;
								type = DiffEntry.ChangeType.RENAME;
							}
							else
							{
								type = DiffEntry.ChangeType.COPY;
							}
							entries.AddItem(DiffEntry.Pair(type, d, a_1, 100));
							adds.Set(addIdx, null);
							// Claim the destination was matched.
							pm.Update(1);
						}
					}
					else
					{
						Sharpen.Collections.AddAll(left, adds);
					}
				}
			}
			added = left;
			deleted = new AList<DiffEntry>(deletedMap.Count);
			foreach (object o_2 in deletedMap.Values)
			{
				if (o_2 is DiffEntry)
				{
					DiffEntry e = (DiffEntry)o_2;
					if (e.changeType == DiffEntry.ChangeType.DELETE)
					{
						deleted.AddItem(e);
					}
				}
				else
				{
					IList<DiffEntry> list = (IList<DiffEntry>)o_2;
					foreach (DiffEntry e in list)
					{
						if (e.changeType == DiffEntry.ChangeType.DELETE)
						{
							deleted.AddItem(e);
						}
					}
				}
			}
			pm.EndTask();
		}

		/// <summary>
		/// Find the best match by file path for a given DiffEntry from a list of
		/// DiffEntrys.
		/// </summary>
		/// <remarks>
		/// Find the best match by file path for a given DiffEntry from a list of
		/// DiffEntrys. The returned DiffEntry will be of the same type as <src>. If
		/// no DiffEntry can be found that has the same type, this method will return
		/// null.
		/// </remarks>
		/// <param name="src">the DiffEntry to try to find a match for</param>
		/// <param name="list">a list of DiffEntrys to search through</param>
		/// <returns>the DiffEntry from <list> who's file path best matches <src></returns>
		private static DiffEntry BestPathMatch(DiffEntry src, IList<DiffEntry> list)
		{
			DiffEntry best = null;
			int score = -1;
			foreach (DiffEntry d in list)
			{
				if (SameType(Mode(d), Mode(src)))
				{
					int tmp = SimilarityRenameDetector.NameScore(Path(d), Path(src));
					if (tmp > score)
					{
						best = d;
						score = tmp;
					}
				}
			}
			return best;
		}

		private Dictionary<AbbreviatedObjectId, object> PopulateMap(IList<DiffEntry> diffEntries
			, ProgressMonitor pm)
		{
			Dictionary<AbbreviatedObjectId, object> map = new Dictionary<AbbreviatedObjectId, 
				object>();
			foreach (DiffEntry de in diffEntries)
			{
				object old = map.Put(Id(de), de);
				if (old is DiffEntry)
				{
					AList<DiffEntry> list = new AList<DiffEntry>(2);
					list.AddItem((DiffEntry)old);
					list.AddItem(de);
					map.Put(Id(de), list);
				}
				else
				{
					if (old != null)
					{
						// Must be a list of DiffEntries
						((IList<DiffEntry>)old).AddItem(de);
						map.Put(Id(de), old);
					}
				}
				pm.Update(1);
			}
			return map;
		}

		private static string Path(DiffEntry de)
		{
			return de.changeType == DiffEntry.ChangeType.DELETE ? de.oldPath : de.newPath;
		}

		private static FileMode Mode(DiffEntry de)
		{
			return de.changeType == DiffEntry.ChangeType.DELETE ? de.oldMode : de.newMode;
		}

		private static AbbreviatedObjectId Id(DiffEntry de)
		{
			return de.changeType == DiffEntry.ChangeType.DELETE ? de.oldId : de.newId;
		}

		internal static bool SameType(FileMode a, FileMode b)
		{
			// Files have to be of the same type in order to rename them.
			// We would never want to rename a file to a gitlink, or a
			// symlink to a file.
			//
			int aType = a.GetBits() & FileMode.TYPE_MASK;
			int bType = b.GetBits() & FileMode.TYPE_MASK;
			return aType == bType;
		}

		private static DiffEntry ExactRename(DiffEntry src, DiffEntry dst)
		{
			return DiffEntry.Pair(DiffEntry.ChangeType.RENAME, src, dst, EXACT_RENAME_SCORE);
		}

		private static DiffEntry ExactCopy(DiffEntry src, DiffEntry dst)
		{
			return DiffEntry.Pair(DiffEntry.ChangeType.COPY, src, dst, EXACT_RENAME_SCORE);
		}
	}
}
