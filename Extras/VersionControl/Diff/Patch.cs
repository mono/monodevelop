/*
 * Patches, a supporting class for Diffs
 */

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace Algorithm.Diff {
	
	public class Patch : IEnumerable {
		Hunk[] hunks;
		
		internal Patch(Hunk[] hunks) {
			this.hunks = hunks;
		}
		
		public class Hunk {
			object[] rightData;
			int leftstart, leftcount, rightstart, rightcount;
			bool same;
			
			internal Hunk(object[] rightData, int st, int c, int rs, int rc, bool s) {
				this.rightData = rightData;
				leftstart = st;
				leftcount = c;
				rightstart = rs;
				rightcount = rc;
				same = s;
			}
			
			public bool Same { get { return same; } }
			
			public int Start { get { return leftstart; } }
			public int Count { get { return leftcount; } }
			public int End { get { return leftstart+leftcount-1; } }
			
			public IList Right { get { if (same) return null; return new Range(rightData, rightstart, rightcount); } }
		}
		
		IEnumerator IEnumerable.GetEnumerator() {
			return hunks.GetEnumerator();
		}
		
		public IList Apply(IList original) {
			ArrayList right = new ArrayList();
			foreach (Hunk hunk in this) {
				if (hunk.Same)
					right.AddRange(new Range(original, hunk.Start, hunk.Count));
				else
					right.AddRange(hunk.Right);
			}
			return right;
		}
		
	}
}
