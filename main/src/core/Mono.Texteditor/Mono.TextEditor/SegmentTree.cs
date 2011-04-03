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
using Mono.TextEditor.Utils;

namespace Mono.TextEditor
{
	/// <summary>
	/// A segment tree contains overlapping segments and get all segments overlapping a segment. It's implemented as a augmented interval tree
	/// described in Cormen et al. (2001, Section 14.3: Interval trees, pp. 311–317).
	/// </summary>
	public class SegmentTree<T> : ISegmentTree where T : TreeSegment
	{
		internal readonly RedBlackTree<T> tree = new RedBlackTree<T> ();
		
		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public IEnumerable<T> Segments {
			get {
				var root = tree.Root;
				if (root == null)
					yield break;
				var node = root.GetOuterLeft ();
				while (node != null) {
					yield return node;
					node = node.GetNextNode ();
				}
			}
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
					segment.UpdateAugmentedData ();
				}
				var node = SearchFirstSegmentWithStartAfter (e.Offset);
				if (node != null) {
					node.DistanceToPrevNode += length;
					node.UpdateAugmentedData ();
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
					segment.UpdateAugmentedData ();
					continue;
				}
				int remainingLength = segment.EndOffset - (e.Offset + e.Count);
				Remove (segment);
				if (remainingLength > 0) {
					segment.Offset = e.Offset + e.Count;
					segment.Length = remainingLength;
					Add (segment);
				}
			}

			var next = SearchFirstSegmentWithStartAfter (e.Offset + 1);

			if (next != null) {
				next.DistanceToPrevNode += delta;
				next.UpdateAugmentedData ();
			}
		}
		
		public void Add (TreeSegment node)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			if (node.segmentTree != null)
				throw new InvalidOperationException ("Node already attached.");
			
			node.segmentTree = this;
			
		
			int insertionOffset = node.Offset;
			node.DistanceToMaxEnd = node.Length;
			
			if (tree.Root == null) {
				tree.Count = 1;
				tree.Root = (T)node;
				node.TotalLength = node.DistanceToPrevNode;
				return;
			}
			
			if (insertionOffset < tree.Root.TotalLength) {
				var n = SearchNode (ref insertionOffset);
				node.TotalLength = node.DistanceToPrevNode = insertionOffset;
				n.DistanceToPrevNode -= insertionOffset;
				tree.InsertBefore (n, node);
				return;
			}
			
			node.DistanceToPrevNode = node.TotalLength = insertionOffset - tree.Root.TotalLength;
			tree.InsertRight (tree.Root.GetOuterRight (), node);
		}
		
		public void Remove (TreeSegment node)
		{
			var calculatedOffset = node.Offset;
			var next = node.GetNextNode ();
			if (next != null)
				next.DistanceToPrevNode += node.DistanceToPrevNode;
			tree.Remove (node);
			if (next != null)
				next.UpdateAugmentedData ();
			node.segmentTree = null;
			node.parent = node.left = node.right = null;
			node.DistanceToPrevNode = calculatedOffset;
		}
		
		TreeSegment SearchFirstSegmentWithStartAfter (int startOffset)
		{
			if (tree.Root == null)
				return null;
			if (startOffset <= 0)
				return tree.Root.GetOuterLeft ();
			var result = SearchNode (ref startOffset);
			while (startOffset == 0) {
				var pre = result == null ? tree.Root.GetOuterRight () : result.GetPrevNode ();
				startOffset += pre.DistanceToPrevNode;
				result = pre;
			}
			return result;
		}
		
		TreeSegment SearchNode (ref int offset)
		{
			TreeSegment n = tree.Root;
			while (true) {
				if (n.left != null) {
					if (offset < n.left.TotalLength) {
						n = n.left;
						continue;
					}
					offset -= n.left.TotalLength;
				}
				if (offset < n.DistanceToPrevNode) 
					return n; 
				offset -= n.DistanceToPrevNode; 
				if (n.right == null) 
					return null;
				n = n.right;
			}
		}
		
		public IEnumerable<T> GetSegmentsAt (int offset)
		{
			return GetSegmentsOverlapping (offset, 0);
		}
		
		public IEnumerable<T> GetSegmentsOverlapping (ISegment segment)
		{
			if (segment == null)
				return Enumerable.Empty<T> ();
			return GetSegmentsOverlapping (segment.Offset, segment.Length);
		}
		
		struct Interval 
		{
			internal TreeSegment node;
			internal int start, end;
			
			public Interval (TreeSegment node,int start,int end)
			{
				this.node = node;
				this.start = start;
				this.end = end;
			}
		}
		
		public IEnumerable<T> GetSegmentsOverlapping (int offset, int length)
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
				int nodeStart = interval.start - node.DistanceToPrevNode;
				int nodeEnd = interval.end - node.DistanceToPrevNode;
				if (node.left != null) {
					nodeStart -= node.left.TotalLength;
					nodeEnd -= node.left.TotalLength;
				}
			
				if (node.DistanceToMaxEnd < nodeStart) 
					continue;
			
				if (node.left != null)
					intervalStack.Push (new Interval (node.left, interval.start, interval.end));
				
				if (nodeEnd < 0) 
					continue;
				
				if (nodeStart <= node.Length) 
					yield return (T)node;
			
				if (node.right != null) 
					intervalStack.Push (new Interval (node.right, nodeStart, nodeEnd));
			}
		}
	}
	
	interface ISegmentTree
	{
		void Add (TreeSegment segment);
		void Remove (TreeSegment segment);
	}
	
	public class TreeSegment : Segment, IRedBlackTreeNode
	{
		internal ISegmentTree segmentTree;

		public override int Offset {
			get {
				if (segmentTree == null)
					return DistanceToPrevNode;
				
				var curNode = this;
				int offset = curNode.DistanceToPrevNode;
				if (curNode.left != null)
					offset += curNode.left.TotalLength;
				while (curNode.parent != null) {
					if (curNode == curNode.parent.right) {
						if (curNode.parent.left != null)
							offset += curNode.parent.left.TotalLength;
						offset += curNode.parent.DistanceToPrevNode;
					}
					curNode = curNode.parent;
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
		
		// TotalLength = DistanceToPrevNode + Left.DistanceToPrevNode + Right.DistanceToPrevNode
		internal int TotalLength;
		
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

		#region IRedBlackTreeNode implementation
		public void UpdateAugmentedData ()
		{
			int totalLength = DistanceToPrevNode;
			int distanceToMaxEnd = Length;
			
			if (left != null) {
				totalLength += left.TotalLength;
				int leftdistance = left.DistanceToMaxEnd - DistanceToPrevNode;
				if (left.right != null)
					leftdistance -= left.right.TotalLength;
				if (leftdistance > distanceToMaxEnd)
					distanceToMaxEnd = leftdistance;
			}
			
			if (right != null) {
				totalLength += right.TotalLength;
				int rightdistance = right.DistanceToMaxEnd + right.DistanceToPrevNode;
				if (right.left != null)
					rightdistance += right.left.TotalLength;
				if (rightdistance > distanceToMaxEnd)
					distanceToMaxEnd = rightdistance;
			}
			
			if (TotalLength != totalLength || DistanceToMaxEnd != distanceToMaxEnd) {
				TotalLength = totalLength;
				DistanceToMaxEnd = distanceToMaxEnd;
				if (parent != null)
					parent.UpdateAugmentedData ();
			}
		}

		internal TreeSegment parent, left, right;

		Mono.TextEditor.Utils.IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Parent {
			get {
				return parent;
			}
			set {
				parent = (TreeSegment)value;
			}
		}

		Mono.TextEditor.Utils.IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Left {
			get {
				return left;
			}
			set {
				left = (TreeSegment)value;
			}
		}

		
		IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Right {
			get {
				return right;
			}
			set {
				right = (TreeSegment)value;
			}
		}

		RedBlackColor Mono.TextEditor.Utils.IRedBlackTreeNode.Color {
			get;
			set;
		}
		#endregion
		
	}
}
