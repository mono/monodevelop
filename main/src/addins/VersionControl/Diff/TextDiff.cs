using System;
using System.Collections;

namespace Algorithm.Diff {
	
	public class TextDiff : IDiff {
		Node left, right;
		ArrayList hunks = new ArrayList();
		
		public TextDiff(string left, string right) : this (left, right, null) {
		}
		
		public TextDiff(string left, string right, IComparer comparer) {
			this.left = new Node(left, 0, left.Length, 0);
			this.right = new Node(right, 0, right.Length, 0);
			

			TextDiffStructuredDiff diff = new TextDiffStructuredDiff(hunks, left.ToCharArray(), right.ToCharArray());
			diff.AddInterface(typeof(Node), new TextDiffNodeInterface(comparer));
			diff.Compare(this.left, this.right);
		}
		
		public IList Left { get { return left.ToString().ToCharArray(); } }
		public  IList Right { get { return right.ToString().ToCharArray(); } }
		
		IEnumerator IEnumerable.GetEnumerator() {
			return hunks.GetEnumerator();
		}

		private class TextDiffStructuredDiff : StructuredDiff {
			ArrayList hunks;
			IList leftlist, rightlist;
			
			public TextDiffStructuredDiff(ArrayList hunks, IList leftlist, IList rightlist) {
				this.hunks = hunks;
				this.leftlist = leftlist;
				this.rightlist = rightlist;
			}
			
			protected override void WritePushNode(NodeInterface nodeInterface, object left, object right) {
			}
	
			protected override void WritePushSame() {
			}
			
			private void AddHunk(int lcount, int rcount, bool same) {
				if (hunks.Count > 0 && same == ((Diff.Hunk)hunks[hunks.Count-1]).Same) {
					Diff.Hunk prev = (Diff.Hunk)hunks[hunks.Count-1];
					hunks[hunks.Count-1] = new Diff.Hunk(
						leftlist,
						rightlist,
						prev.Left.Start,
						prev.Left.End + lcount,
						prev.Right.Start,
						prev.Right.End + rcount,
						same);
					return;
				}
				
				int le = -1;
				int re = -1;
				if (hunks.Count > 0) {
					Diff.Hunk prev = (Diff.Hunk)hunks[hunks.Count-1];
					le = prev.Left.End;
					re = prev.Right.End;
				}
				
				hunks.Add( new Diff.Hunk(
						leftlist,
						rightlist,
						le + 1,
						le + lcount,
						re + 1,
						re + rcount,
						same) );
			}
		
			protected override void WriteNodeSame(NodeInterface nodeInterface, object left, object right) {
				AddHunk(((Node)left).count, ((Node)right).count, true);
			}

			protected override void WritePopSame() {
			}
	
			protected override void WriteNodeChange(NodeInterface leftInterface, object left, NodeInterface rightInterface, object right) {
				AddHunk(((Node)left).count, ((Node)right).count, false);
			}
	
			protected override void WriteNodesRemoved(IList objects) {
				int start = ((Node)objects[0]).start;
				int end = ((Node)objects[objects.Count-1]).start + ((Node)objects[objects.Count-1]).count - 1;
				
				AddHunk(end - start + 1, 0, false);
			}
	
			protected override void WriteNodesAdded(IList objects) {
				int start = ((Node)objects[0]).start;
				int end = ((Node)objects[objects.Count-1]).start + ((Node)objects[objects.Count-1]).count - 1;
				
				AddHunk(0, end - start + 1, false);
			}
	
			protected override void WritePopNode() {
			}
		}
		
		private class TextDiffNodeInterface : NodeInterface {
			IComparer comparer;
			
			public TextDiffNodeInterface(IComparer comparer) { this.comparer = comparer; }
			
			public override IList GetChildren(object node) {
				if (((Node)node).children.Count == 0) return null;
				return ((Node)node).children;
			}
			
			private bool Equal(string a, string b) {
				if (comparer == null)
					return a == b;
				return comparer.Compare(a, b) == 0;
			}
			
			public override float Compare(object left, object right, StructuredDiff comparer) {
				string l = left.ToString(), r = right.ToString();
				if (Equal(l, r)) return 0;
				if (l.Length == 1 || r.Length == 1) return 1;
				float d = comparer.CompareLists(GetChildren(left), GetChildren(right));
				if (((Node)left).level == 2 && d >= .75) d = 1.1f;
				return d;
			}
			
			public override int GetHashCode(object node) {
				return node.ToString().GetHashCode();
			}
		}	
		
		private class Node {
			public string source;
			public int start, count;
			
			public int level;
			public ArrayList children = new ArrayList();
			
			static char[][] delims = new char[][] {
				new char[] { '\n', '\r' },
				new char[] { ' ', '\t', '.', ',' }
				};
			
			public Node(string source, int start, int count, int level) {
				this.source = source;
				this.start = start;
				this.count = count;
				this.level = level;
				
				if (level <= 1) {
					int pos = start;
					foreach (string child in ToString().Split(delims[level])) {
						if (child.Length >= 1)
							children.Add(new Node(source, pos, child.Length, level+1));
						if (pos + child.Length < count)
							children.Add(new Node(source, pos+child.Length, 1, level+1));
						pos += child.Length + 1;
					}
				} else if (level == 2) {
					for (int i = start; i < start + count; i++)
						children.Add(new Node(source, i, 1, level+1));
				}
				
			}
			
			public override string ToString() {
				return source.Substring(start, count);
			}
		}
	}
	
}
