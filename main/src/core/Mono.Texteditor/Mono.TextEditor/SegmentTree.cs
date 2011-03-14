// 
// SegmentTree.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mono.TextEditor
{
	/// <summary>
	/// A segment tree contains overlapping segments and get all segments overlapping a segment. It's implemented as a augmented interval tree
	/// described in Cormen et al. (2001, Section 14.3: Interval trees, pp. 311–317).
	/// </summary>
	public class SegmentTree
	{
		readonly RedBlackTree<TreeSegment> tree = new RedBlackTree<TreeSegment> ();
		
		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public IEnumerable<TreeSegment> Segments {
			get {
				var root = tree.Root;
				if (root == null)
					yield break;
				var node = root.OuterLeft;
				while (node != null) {
					yield return node.Value;
					node = node.NextNode;
				}
			}
		}
		
		public SegmentTree ()
		{
			tree.ChildrenChanged += (sender, args) => UpdateNode (args.Node);
			tree.NodeRotateLeft += (sender, args) => 
			{
				UpdateNode (args.Node);
				UpdateNode (args.Node.Parent);
			};
			tree.NodeRotateRight += (sender, args) => 
			{
				UpdateNode (args.Node);
				UpdateNode (args.Node.Parent);
			};
		}
		
		public void InstallListener (Document doc)
		{
			doc.TextReplaced += UpdateOnTextReplace;
		}

		public void RemoveListener (Document doc)
		{
			doc.TextReplaced -= UpdateOnTextReplace;
		}

		void UpdateOnTextReplace (object sender, ReplaceEventArgs e)
		{
			if (e.Count == 0) {
				var length = (e.Value != null ? e.Value.Length : 0);
				foreach (var segment in GetSegmentsAt (e.Offset).Where (s => s.Offset < e.Offset && e.Offset < s.EndOffset)) {
					segment.Length += length;
				}
				var node = SearchFirstSegmentWithStartAfter (e.Offset);
				if (node != null) {
					node.Value.DistanceToPrevNode += length;
					UpdateNode (node);
				}
				return;
			}
			
			int delta = (e.Value != null ? e.Value.Length : 0) - e.Count;

			foreach (var segment in GetSegmentsOverlapping (e.Offset, e.Count)) {
				if (segment.Offset <= e.Offset) {
					if (segment.EndOffset >= e.Offset + e.Count) {
						segment.Length += delta;
					} else {
						segment.Length = e.Offset - segment.Offset;
					}
					continue;
				}
				int remainingLength = segment.EndOffset - (e.Offset + e.Count);
				Remove (segment);
				segment.Offset = e.Offset + e.Count;
				segment.Length = System.Math.Max (0, remainingLength);
				Add (segment);
			}

			var next = SearchFirstSegmentWithStartAfter (e.Offset + 1);

			if (next != null) {
				next.Value.DistanceToPrevNode += delta;
				UpdateNode (next);
			}
		}
		
		static void UpdateNode (RedBlackTree<TreeSegment>.RedBlackTreeNode node)
		{
			if (node == null)
				return;
			int totalLength = node.Value.DistanceToPrevNode;
			int distanceToMaxEnd = node.Value.Length;
			
			var left = node.Left;
			if (left != null) {
				totalLength += left.Value.TotalSubtreeLength;
				int leftdistance = left.Value.DistanceToMaxEnd - node.Value.DistanceToPrevNode;
				if (left.Right != null)
					leftdistance -= left.Right.Value.TotalSubtreeLength;
				if (leftdistance > distanceToMaxEnd)
					distanceToMaxEnd = leftdistance;
			}
			
			var right = node.Right;
			if (right != null) {
				totalLength += right.Value.TotalSubtreeLength;
				int rightdistance = right.Value.DistanceToMaxEnd + right.Value.DistanceToPrevNode;
				if (right.Left != null)
					rightdistance += right.Left.Value.TotalSubtreeLength;
				if (rightdistance > distanceToMaxEnd)
					distanceToMaxEnd = rightdistance;
			}
			
			if (node.Value.TotalSubtreeLength != totalLength || node.Value.DistanceToMaxEnd != distanceToMaxEnd) {
				node.Value.TotalSubtreeLength = totalLength;
				node.Value.DistanceToMaxEnd = distanceToMaxEnd;
				if (node.Parent != null)
					UpdateNode (node.Parent);
			}
		}
		
		public void Add (TreeSegment segment)
		{
			if (segment == null)
				throw new ArgumentNullException ("segment");
			var node = new RedBlackTree<TreeSegment>.RedBlackTreeNode (segment);
			segment.segmentTree = this;
			segment.treeNode = node;
			Add (node);
		}
		
		void Add (RedBlackTree<TreeSegment>.RedBlackTreeNode node)
		{
			tree.Count++;
			int insertionOffset = node.Value.Offset;
			node.Value.DistanceToMaxEnd = node.Value.Length;
			
			if (tree.Root == null) {
				tree.Root = node;
				node.Value.TotalSubtreeLength = node.Value.DistanceToPrevNode;
				return;
			}
			
			if (insertionOffset < tree.Root.Value.TotalSubtreeLength) {
				var n = SearchNode (ref insertionOffset);
				node.Value.TotalSubtreeLength = node.Value.DistanceToPrevNode = insertionOffset;
				n.Value.DistanceToPrevNode -= insertionOffset;
				tree.InsertBefore (n, node);
				return;
			}
			
			node.Value.DistanceToPrevNode = node.Value.TotalSubtreeLength = insertionOffset - tree.Root.Value.TotalSubtreeLength;
			tree.InsertRight (tree.Root.OuterRight, node);
		}
		
		
		public void Remove (TreeSegment node)
		{
			Remove (node.treeNode);
			node.treeNode = null;
			node.segmentTree = null;
		}

		void Remove (RedBlackTree<TreeSegment>.RedBlackTreeNode node)
		{
			var next = node.NextNode;
			tree.Remove (node);
			if (next != null)
				UpdateNode (next);
		}
		
		RedBlackTree<TreeSegment>.RedBlackTreeNode SearchFirstSegmentWithStartAfter (int startOffset)
		{
			if (tree.Root == null)
				return null;
			if (startOffset <= 0)
				return tree.Root.OuterLeft;
			var result = SearchNode (ref startOffset);
			while (startOffset == 0) {
				var pre = result == null ? tree.Root.OuterRight : result.PrevNode;
				startOffset += pre.Value.DistanceToPrevNode;
				result = pre;
			}
			return result;
		}
		
		RedBlackTree<TreeSegment>.RedBlackTreeNode SearchNode (ref int offset)
		{
			var n = tree.Root;
			while (true) {
				if (n.Left != null) {
					if (offset < n.Left.Value.TotalSubtreeLength) {
						n = n.Left;
						continue;
					}
					offset -= n.Left.Value.TotalSubtreeLength;
				}
				if (offset < n.Value.DistanceToPrevNode) 
					return n; 
				offset -= n.Value.DistanceToPrevNode; 
				if (n.Right == null) 
					return null;
				n = n.Right;
			}
		}
		
		public IEnumerable<TreeSegment> GetSegmentsAt (int offset)
		{
			return GetSegmentsOverlapping (offset, 0);
		}
		
		public IEnumerable<TreeSegment> GetSegmentsOverlapping (ISegment segment)
		{
			if (segment == null)
				return Enumerable.Empty<TreeSegment> ();
			return GetSegmentsOverlapping (segment.Offset, segment.Length);
		}
		
		struct Interval 
		{
			internal RedBlackTree<TreeSegment>.RedBlackTreeNode node;
			internal int start, end;
			
			public Interval (RedBlackTree<TreeSegment>.RedBlackTreeNode node,int start,int end)
			{
				this.node = node;
				this.start = start;
				this.end = end;
			}
		}
		
		public IEnumerable<TreeSegment> GetSegmentsOverlapping (int offset, int length)
		{
			if (tree.Root == null)
				yield break;
			Stack<Interval> intervalStack = new Stack<Interval> ();
			intervalStack.Push (new Interval (tree.Root, offset, offset + length));
			while (intervalStack.Count > 0) {
				var interval = intervalStack.Pop ();
				if (interval.end < 0) 
					continue;
				
				var node = interval.node;
				int nodeStart = interval.start - node.Value.DistanceToPrevNode;
				int nodeEnd = interval.end - node.Value.DistanceToPrevNode;
				if (node.Left != null) {
					nodeStart -= node.Left.Value.TotalSubtreeLength;
					nodeEnd -= node.Left.Value.TotalSubtreeLength;
				}
			
				if (node.Value.DistanceToMaxEnd < nodeStart) 
					yield break;
			
				if (node.Left != null)
					intervalStack.Push (new Interval (node.Left, interval.start, interval.end));
				
				if (nodeEnd < 0) 
					continue;
				
				if (nodeStart <= node.Value.Length) 
					yield return node.Value;
			
				if (node.Right != null) 
					intervalStack.Push (new Interval (node.Right, nodeStart, nodeEnd));
			}
		}
	}
	
	public class TreeSegment : Segment
	{
		internal SegmentTree segmentTree;
		internal RedBlackTree<TreeSegment>.RedBlackTreeNode treeNode;

		public RedBlackTree<TreeSegment>.RedBlackTreeIterator Iter {
			get { return new RedBlackTree<TreeSegment>.RedBlackTreeIterator (treeNode); }
		}

		public override int Offset {
			get {
				var curNode = treeNode;
				if (curNode == null)
					return DistanceToPrevNode;
				int offset = curNode.Value.DistanceToPrevNode;
				if (curNode.Left != null)
					offset += curNode.Left.Value.TotalSubtreeLength;
				while (curNode.Parent != null) {
					if (curNode == curNode.Parent.Right) {
						if (curNode.Parent.Left != null)
							offset += curNode.Parent.Left.Value.TotalSubtreeLength;
						offset += curNode.Parent.Value.DistanceToPrevNode;
					}
					curNode = curNode.Parent;
				}
				return offset;
			}
			set {
				if (segmentTree != null)
					segmentTree.Remove (this);
				DistanceToPrevNode = value;
				if (segmentTree != null)
					segmentTree.Add (this);
			}
		}
		
		// TotalSubtreeLength = DistanceToPrevNode + Left.DistanceToPrevNode + Right.DistanceToPrevNode
		internal int TotalSubtreeLength;
		
		internal int DistanceToPrevNode;
		
		// DistanceToMaxEnd = Max (Length, left.DistanceToMaxEnd + Max (left.Offset, right.Offset) - Offset)
		internal int DistanceToMaxEnd;
		
		protected TreeSegment ()
		{
		}

		public TreeSegment (int offset, int length) : base (offset, length)
		{
		}

		public TreeSegment (ISegment segment) : base (segment)
		{
		}
		
	}
}
