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
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	/// <summary>
	/// A segment tree contains overlapping segments and get all segments overlapping a segment. It's implemented as a augmented interval tree
	/// described in Cormen et al. (2001, Section 14.3: Interval trees, pp. 311–317).
	/// </summary>
	class SegmentTree<T> : TextSegmentTree where T : TreeSegment
	{
		internal readonly RedBlackTree<T> tree = new RedBlackTree<T> ();
		
		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public bool IsDirty {
			get;
			set;
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
		
		TextDocument ownerDocument;
		public void InstallListener (TextDocument doc)
		{
			if (ownerDocument != null)
				throw new InvalidOperationException ("Segment tree already installed");
			ownerDocument = doc;
			doc.TextChanged += UpdateOnTextReplace;
		}

		public void RemoveListener ()
		{
			if (ownerDocument == null)
				throw new InvalidOperationException ("Segment tree is not installed");
			ownerDocument.TextChanged -= UpdateOnTextReplace;
			ownerDocument = null;
		}
		
		public void Clear ()
		{
			IsDirty = false;
			tree.Clear ();
		}
		
		public void UpdateOnTextReplace (object sender, TextChangeEventArgs e)
		{
			IsDirty = true;
			for (int i = 0; i < e.TextChanges.Count; ++i) {
				var change = e.TextChanges[i];
				if (change.RemovalLength == 0) {
					var length = change.InsertionLength;
					foreach (var segment in GetSegmentsAt (change.Offset).Where (s => s.Offset < change.Offset && change.Offset < s.EndOffset)) {
						segment.Length += length;
						segment.UpdateAugmentedData ();
					}
					var node = SearchFirstSegmentWithStartAfter (change.Offset + 1);
					if (node != null) {
						node.DistanceToPrevNode += length;
						node.UpdateAugmentedData ();
					}
					continue;
				}
				int delta = change.ChangeDelta;
				foreach (var segment in new List<T> (GetSegmentsOverlapping (change.Offset, change.RemovalLength))) {
					if (segment.Offset < change.Offset) {
						if (segment.EndOffset >= change.Offset + change.RemovalLength) {
							segment.Length += delta;
						} else {
							segment.Length = change.Offset - segment.Offset;
						}
						segment.UpdateAugmentedData ();
						continue;
					}
					int remainingLength = segment.EndOffset - (change.Offset + change.RemovalLength);
					Remove (segment);
					if (remainingLength > 0) {
						segment.Offset = change.Offset + change.RemovalLength;
						segment.Length = remainingLength;
						Add (segment);
					}
				}
				var next = SearchFirstSegmentWithStartAfter (change.Offset + 1);

				if (next != null) {
					next.DistanceToPrevNode += delta;
					next.UpdateAugmentedData ();
				}
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
		
		public bool Remove (TreeSegment node)
		{
			if (node.segmentTree == null)
				return false;
			if (node.segmentTree != this)
				throw new InvalidOperationException ("Tried to remove tree segment from wrong tree.");
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
			return true;
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
				if (pre == null)
					return null;
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
			if (segment.Offset < 0)
				return Enumerable.Empty<T> ();
			return GetSegmentsOverlapping (segment.Offset, segment.Length);
		}

		class Interval
		{
			internal Interval tail;

			internal TreeSegment node;
			internal int start, end;

			public Interval (Interval tail, TreeSegment node, int start, int end)
			{
				this.tail = tail;
				this.node = node;
				this.start = start;
				this.end = end;
			}

			public override string ToString ()
			{
				return string.Format ("[Interval: start={0},end={1}]", start, end);
			}
		}

		public IEnumerable<T> GetSegmentsOverlapping (int offset, int length)
		{
			if (tree.Root == null)
				yield break;
			var intervalStack = new Interval (null, tree.Root, offset, offset + length);
			while (intervalStack != null) {
				var interval = intervalStack;
				intervalStack = intervalStack.tail;
				if (interval.end < 0)
					continue;

				var node = interval.node;
				int nodeStart = interval.start - node.DistanceToPrevNode;
				int nodeEnd = interval.end - node.DistanceToPrevNode;
				var leftNode = node.left;
				if (leftNode != null) {
					nodeStart -= leftNode.TotalLength;
					nodeEnd -= leftNode.TotalLength;
				}

				if (node.DistanceToMaxEnd < nodeStart)
					continue;

				if (leftNode != null)
					intervalStack = new Interval (intervalStack, leftNode, interval.start, interval.end);

				if (nodeEnd < 0)
					continue;

				if (nodeStart <= node.Length)
					yield return (T)node;

				var rightNode = node.right;
				if (rightNode != null)
					intervalStack = new Interval (intervalStack, rightNode, nodeStart, nodeEnd);
			}
		}
	}
	
	interface TextSegmentTree
	{
		void Add (TreeSegment segment);
		bool Remove (TreeSegment segment);
	}
	
	class TreeSegment : IRedBlackTreeNode
	{
		internal TextSegmentTree segmentTree;

		public int Offset {
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
		
		public int Length {
			get;
			set;
		}

		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

		public ISegment Segment {
			get {
				return new TextSegment (Offset, Length);
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

		public TreeSegment (int offset, int length)
		{
			Offset = offset;
			Length = length;
		}

		public TreeSegment (ISegment segment) : this (segment.Offset, segment.Length)
		{
		}

		public bool Contains (int offset)
		{
			return Offset <= offset && offset < EndOffset;
		}

		public bool Contains (ISegment segment)
		{
			return Offset <= segment.Offset && segment.EndOffset <= EndOffset;
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
