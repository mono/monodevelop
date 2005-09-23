/*
 * Merges, a supporting class for Diffs
 */

using System;
using System.Collections;
using System.Text;

namespace Algorithm.Diff {
	public class Merge : IEnumerable {
		IDiff[] diffs;
		ArrayList hunks = new ArrayList();
		
		public Merge(IList original, IList[] changed, IComparer comparer, IHashCodeProvider hashcoder)
		: this(makediffs(original, changed, comparer, hashcoder))
		{
		}
		
		public Merge(string original, string[] changed, IComparer comparer)
		: this(makediffs(original, changed, comparer))
		{
		}

		private static IDiff[] makediffs(IList original, IList[] changed, IComparer comparer, IHashCodeProvider hashcoder) {
			IDiff[] diffs = new IDiff[changed.Length];
			for (int i = 0; i < changed.Length; i++)
				diffs[i] = new Diff(original, changed[i], comparer, hashcoder);
			return diffs;
		}
		
		private static IDiff[] makediffs(string original, string[] changed, IComparer comparer) {
			IDiff[] diffs = new IDiff[changed.Length];
			for (int i = 0; i < changed.Length; i++)
				diffs[i] = new TextDiff(original, changed[i], comparer);
			return diffs;
		}

		public Merge(IDiff[] diffs) {
			this.diffs = diffs;
			
			// initialize data structures
			
			IEnumerator[] enumerators = new IEnumerator[diffs.Length];
			ArrayList[] hunks = new ArrayList[diffs.Length];

			for (int i = 0; i < hunks.Length; i++) {
				enumerators[i] = ((IEnumerable)diffs[i]).GetEnumerator();
				hunks[i] = new ArrayList();
			}
			
			int startline = 0;
			
			while (true) {
				int endline = -1;
				bool hasmore = false;

				// Get the next hunk for each diff, and find the longest
				// hunk for which there are changes.
				
				for (int i = 0; i < hunks.Length; i++) {
					if (hunks[i].Count > 0) continue;
					if (!enumerators[i].MoveNext()) return;
					hasmore = true;
					Diff.Hunk hunk = (Diff.Hunk)enumerators[i].Current;
					hunks[i].Add(hunk);
					if (!hunk.Same && hunk.Left.End > endline)
						endline = hunk.Left.End;
				}
				
				if (!hasmore) return;
				
				if (endline == -1) {
					// All of the hunks represented no change. Find the shortest hunk,
					// create a hunk from the current start line to the end of the
					// shortest hunk, and retain all of the hunks that overlap into that
					// hunk's next region.  (Clear the rest.)
					int start = int.MaxValue;
					for (int i = 0; i < hunks.Length; i++) {
						Diff.Hunk h = (Diff.Hunk)hunks[i][0];
						if (h.Left.End < start) start = h.Left.End;
					}
					
					// Crop all of the hunks to the shortest region.
					Diff.Hunk[][] h2 = new Diff.Hunk[hunks.Length][];
					for (int i = 0; i < hunks.Length; i++) {
						h2[i] = new Diff.Hunk[1];
						h2[i][0] = (Diff.Hunk)hunks[i][0];
						h2[i][0] = h2[i][0].Crop(startline - h2[i][0].Left.Start, h2[i][0].Left.End - start);
					}
					this.hunks.Add( new Hunk(this, h2, startline, start - startline + 1, true) );
					
					for (int i = 0; i < hunks.Length; i++) {
						Diff.Hunk h = (Diff.Hunk)hunks[i][0];
						if (h.Left.End == start) hunks[i].Clear();
					}
					startline = start+1;
					continue;
				}
				
				// For each diff, add in all of the non-same hunks that fall
				// at least partially within the largest hunk region.  If
				// a hunk crosses the edge, push the edge further and then
				// add more hunks again.
				bool moreToAdd = true;
				while (moreToAdd) {
					moreToAdd = false;
					
					for (int i = 0; i < hunks.Length; i++) {
						Diff.Hunk last = (Diff.Hunk)hunks[i][hunks[i].Count-1];
						while (last.Left.End < endline) {
							if (!enumerators[i].MoveNext()) continue;
							last = (Diff.Hunk)enumerators[i].Current;
							hunks[i].Add(last);
							if (last.Same) continue;
							if (last.Left.End > endline) {
								endline = last.Left.End;
								moreToAdd = true;
							}
						}
					}
				}
				
				Diff.Hunk[][] hunks2 = new Diff.Hunk[hunks.Length][];
				for (int i = 0; i < hunks.Length; i++) {
					// any same hunks that overlap the start or end need to be replaced
					ArrayList hunks3 = new ArrayList();
					foreach (Diff.Hunk h in hunks[i]) {
						Diff.Hunk h2 = h;
						int shiftstart = 0, shiftend = 0;
						if (h2.Same && h2.Left.Start < startline)
							shiftstart = startline - h2.Left.Start;
						if (h2.Same && h2.Left.End > endline)
							shiftend = h2.Left.End - endline;
						if (shiftstart != 0 || shiftend != 0)
							h2 = h2.Crop(shiftstart, shiftend);
						hunks3.Add(h2);
					}
					hunks2[i] = (Diff.Hunk[])hunks3.ToArray(typeof(Diff.Hunk));
				}
				this.hunks.Add( new Hunk(this, hunks2, startline, endline - startline + 1, false) );
				
				// In each hunk list, retain only the last hunk if it
				// overlaps into the next region.
				startline = endline+1;
				for (int i = 0; i < hunks.Length; i++) {
					if (hunks[i].Count == 0) continue;
					Diff.Hunk h = (Diff.Hunk)hunks[i][hunks[i].Count-1];
					hunks[i].Clear();
					if (h.Left.End >= startline)
						hunks[i].Add(h);
				}
				
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator() {
			return hunks.GetEnumerator();
		}
		
		public static IList MergeLists(IList original, IList[] changed, IComparer comparer, IHashCodeProvider hashcoder) {
			Merge m = new Merge(original, changed, comparer, hashcoder);
			ArrayList ret = new ArrayList();
			ArrayList newlines = new ArrayList();
			foreach (Hunk h in m) {
				newlines.Clear();
				
				for (int i = 0; i < changed.Length; i++)
					if (!h.IsSame(i))
						newlines.Add(h.Changes(i));
				
				// If there were no differences in this region, take the original.
				if (newlines.Count == 0)
					ret.AddRange(h.Original());
				
				// If one list has changes, take them
				else if (newlines.Count == 1)
					ret.AddRange((Range)newlines[0]);
				
				// Indicate conflict
				else
					ret.Add(new Conflict((Range[])newlines.ToArray(typeof(Range))));
			}
			return ret;
		}
		
		public class Conflict : IEnumerable {
			Range[] ranges;
			
			internal Conflict(Range[] ranges) {
				this.ranges = ranges;
			}
			
			public Range[] Ranges { get { return ranges; } }
			
			IEnumerator IEnumerable.GetEnumerator() { return ranges.GetEnumerator(); }
			
			public override string ToString() {
				StringBuilder b = new StringBuilder();
				b.Append("<<<<<<<<<<\n");
				for (int i = 0; i < ranges.Length; i++) {
					if (i > 0) b.Append("----------\n");
					foreach (object item in ranges[i]) { 
						b.Append(item);
						b.Append("\n");
					}
				}
				b.Append(">>>>>>>>>>\n");
				return b.ToString();
			}
		}
		
		public class Hunk : Algorithm.Diff.Hunk {
			Merge merge;
			Diff.Hunk[][] hunks;
			int start, count;
			bool same, conflict;
			
			internal Hunk(Merge merge, Diff.Hunk[][] hunks, int start, int count, bool same) {
				this.merge = merge;
				this.hunks = hunks;
				this.start = start;
				this.count = count;
				this.same = same;
				
				int ct = 0;
				foreach (Diff.Hunk[] hh in hunks) {
					foreach (Diff.Hunk h in hh) {
						if (!h.Same) {
							ct++;
							break;
						}
					}
				}
				conflict = (ct > 1);
			}
			
			public override int ChangedLists { get { return merge.diffs.Length; } }
			
			// Returns the set of changes within this hunk's range for the
			// diff of the given index.
			public Diff.Hunk[] ChangesHunks(int index) {
				return hunks[index];
			}
			
			public int ChangedIndex() {
				if (Conflict) throw new InvalidOperationException("ChangedIndex cannot be called if there is a conflict.");
				for (int i = 0; i < hunks.Length; i++) {
					foreach (Diff.Hunk h in hunks[i])
						if (!h.Same) return i;
				}
				return -1;
			}
			
			// Returns the range of elements corresponding to this hunk's range, in the original.
			public override Range Original() {
				return new Range(merge.diffs[0].Left, start, count);
			}
			
			// Returns the range of elements corresponding to this hunk's range, in the diff of the given index.
			public override Range Changes(int index) {
				return new Range(merge.diffs[index].Right, hunks[index][0].Right.Start, hunks[index][hunks[index].Length-1].Right.End - hunks[index][0].Right.Start + 1);
			}
			
			public override bool Same { get { return same; } }
			
			public override bool Conflict { get { return conflict; } }
			
			public override bool IsSame(int index) {
				foreach (Diff.Hunk h in hunks[index])
					if (!h.Same) return false;
				return true;
			}
			
			public override string ToString() {
				StringBuilder b = new StringBuilder();
				if (Same) {
					foreach (object item in Original()) {
						b.Append(" ");
						b.Append(item);
						b.Append("\n");
					}
					return b.ToString();
				}
				
				b.Append("==========\n");
				for (int i = 0; i < hunks.Length; i++) {
					if (i > 0)
						b.Append("----------\n");
					foreach (Diff.Hunk h in hunks[i])
						b.Append(h);
				}
				b.Append("==========\n");				
				return b.ToString();
			}

		}
	}
}

