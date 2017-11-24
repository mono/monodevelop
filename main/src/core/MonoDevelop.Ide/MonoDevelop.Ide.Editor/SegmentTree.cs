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
using MonoDevelop.Core.Text;
using System.Text;
using System.Web.UI.WebControls;
using System.Diagnostics;

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// A segment tree contains overlapping segments and get all segments overlapping a segment. It's implemented as a augmented interval tree
	/// described in Cormen et al. (2001, Section 14.3: Interval trees, pp. 311–317).
	/// </summary>
	public class SegmentTree<T> : TextSegmentTree, ICollection<T> where T : TreeSegment
	{
		readonly RedBlackTree tree = new RedBlackTree ();
		
		ITextDocument ownerDocument;

		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public IEnumerator<T> GetEnumerator ()
		{
			var root = tree.Root;
			if (root == null)
				yield break;
			var node = root.OuterLeft;
			while (node != null) {
				yield return (T)node;
				node = node.NextNode;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public bool Contains (T item)
		{
			return this.Any (item.Equals);
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			Debug.Assert (array != null);
			Debug.Assert (0 <= arrayIndex && arrayIndex < array.Length);
			int i = arrayIndex;
			foreach (T value in this)
				array[i++] = value;
		}

		bool ICollection<T>.IsReadOnly {
			get {
				return false;
			}
		}

		public void Add (T item)
		{
			InternalAdd (item); 
		}

		public bool Remove (T item)
		{
			return InternalRemove (item);
		}
		
		public void Clear ()
		{
			tree.Clear ();
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
				var leftNode = node.Left;
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

				var rightNode = node.Right;
				if (rightNode != null)
					intervalStack = new Interval (intervalStack, rightNode, nodeStart, nodeEnd);
			}
		}

		public void InstallListener (ITextDocument doc)
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

		internal void UpdateOnTextReplace (object sender, TextChangeEventArgs e)
		{
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
					InternalRemove (segment);
					if (remainingLength > 0) {
						segment.Offset = change.Offset + change.RemovalLength;
						segment.Length = remainingLength;
						InternalAdd (segment);
					}
				}
				var next = SearchFirstSegmentWithStartAfter (change.Offset + 1);

				if (next != null) {
					next.DistanceToPrevNode += delta;
					next.UpdateAugmentedData ();
				}
			}
		}

		void InternalAdd (TreeSegment node)
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
			tree.InsertRight (tree.Root.OuterRight, node);
		}

		bool InternalRemove (TreeSegment node)
		{
			if (node.segmentTree == null)
				return false;
			if (node.segmentTree != this)
				throw new InvalidOperationException ("Tried to remove tree segment from wrong tree.");
			var calculatedOffset = node.Offset;
			var next = node.NextNode;
			if (next != null)
				next.DistanceToPrevNode += node.DistanceToPrevNode;
			tree.Remove (node);
			if (next != null)
				next.UpdateAugmentedData ();
			node.segmentTree = null;
			node.Parent = node.Left = node.Right = null;
			node.DistanceToPrevNode = calculatedOffset;
			return true;
		}

		TreeSegment SearchFirstSegmentWithStartAfter (int startOffset)
		{
			if (tree.Root == null)
				return null;
			if (startOffset <= 0)
				return tree.Root.OuterLeft;
			var result = SearchNode (ref startOffset);
			while (startOffset == 0) {
				var pre = result == null ? tree.Root.OuterRight : result.PrevNode;
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
				if (n.Left != null) {
					if (offset < n.Left.TotalLength) {
						n = n.Left;
						continue;
					}
					offset -= n.Left.TotalLength;
				}
				if (offset < n.DistanceToPrevNode) 
					return n; 
				offset -= n.DistanceToPrevNode; 
				if (n.Right == null) 
					return null;
				n = n.Right;
			}
		}

		#region TextSegmentTree implementation

		void TextSegmentTree.Add (TreeSegment segment)
		{
			InternalAdd (segment);
		}

		bool TextSegmentTree.Remove (TreeSegment segment)
		{
			return InternalRemove (segment);
		}

		#endregion
		
		const bool Black = false;
		const bool Red = true;

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
		
		sealed class RedBlackTree
		{
			public T Root { get; set; }

			public void InsertBefore (TreeSegment node, TreeSegment newNode)
			{
				if (node.Left == null) {
					InsertLeft (node, newNode);
				} else {
					InsertRight (node.Left.OuterRight, newNode);
				}
			}

			public void InsertLeft (TreeSegment parentNode, TreeSegment newNode)
			{
				parentNode.Left = newNode;
				newNode.Parent = parentNode;
				newNode.Color = Red;
				parentNode.UpdateAugmentedData ();
				FixTreeOnInsert (newNode);
				Count++;
			}

			public void InsertRight (TreeSegment parentNode, TreeSegment newNode)
			{
				parentNode.Right = newNode;
				newNode.Parent = parentNode;
				newNode.Color = Red;
				parentNode.UpdateAugmentedData ();
				FixTreeOnInsert (newNode);
				Count++;
			}

			void FixTreeOnInsert (TreeSegment node)
			{
				var parent = node.Parent;
				if (parent == null) {
					node.Color = Black;
					return;
				}

				if (parent.Color == Black)
					return;
				var uncle = node.Uncle;
				TreeSegment grandParent = parent.Parent;

				if (uncle != null && uncle.Color == Red) {
					parent.Color = Black;
					uncle.Color = Black;
					grandParent.Color = Red;
					FixTreeOnInsert (grandParent);
					return;
				}

				if (node == parent.Right && parent == grandParent.Left) {
					RotateLeft (parent);
					node = node.Left;
				} else if (node == parent.Left && parent == grandParent.Right) {
					RotateRight (parent);
					node = node.Right;
				}

				parent = node.Parent;
				grandParent = parent.Parent;

				parent.Color = Black;
				grandParent.Color = Red;
				if (node == parent.Left && parent == grandParent.Left) {
					RotateRight (grandParent);
				} else {
					RotateLeft (grandParent);
				}
			}

			void RotateLeft (TreeSegment node)
			{
				TreeSegment right = node.Right;
				Replace (node, right);
				node.Right = right.Left;
				if (node.Right != null)
					node.Right.Parent = node;
				right.Left = node;
				node.Parent = right;
				node.UpdateAugmentedData ();
				node.Parent.UpdateAugmentedData ();
			}

			void RotateRight (TreeSegment node)
			{
				TreeSegment left = node.Left;
				Replace (node, left);
				node.Left = left.Right;
				if (node.Left != null)
					node.Left.Parent = node;
				left.Right = node;
				node.Parent = left;
				node.UpdateAugmentedData ();
				node.Parent.UpdateAugmentedData ();
			}

			void Replace (TreeSegment oldNode, TreeSegment newNode)
			{
				if (newNode != null)
					newNode.Parent = oldNode.Parent;
				if (oldNode.Parent == null) {
					Root = (T)newNode;
				} else {
					if (oldNode.Parent.Left == oldNode)
						oldNode.Parent.Left = newNode;
					else
						oldNode.Parent.Right = newNode;
					oldNode.Parent.UpdateAugmentedData ();
				}
			}

			public void Remove (TreeSegment node)
			{
				if (node.Left != null && node.Right != null) {
					var outerLeft = node.Right.OuterLeft;
					InternalRemove (outerLeft);
					Replace (node, outerLeft);

					outerLeft.Color = node.Color;
					outerLeft.Left = node.Left;
					if (outerLeft.Left != null)
						outerLeft.Left.Parent = outerLeft;

					outerLeft.Right = node.Right;
					if (outerLeft.Right != null)
						outerLeft.Right.Parent = outerLeft;
					outerLeft.UpdateAugmentedData ();
					return;
				}
				InternalRemove (node);
			}

			void InternalRemove (TreeSegment node)
			{
				if (node.Left != null && node.Right != null) {
					var outerLeft = node.Right.OuterLeft;
					InternalRemove (outerLeft);
					Replace (node, outerLeft);

					outerLeft.Color = node.Color;
					outerLeft.Left = node.Left;
					if (outerLeft.Left != null)
						outerLeft.Left.Parent = outerLeft;

					outerLeft.Right = node.Right;
					if (outerLeft.Right != null)
						outerLeft.Right.Parent = outerLeft;
					outerLeft.UpdateAugmentedData ();
					return;
				}
				Count--;
				// node has only one child
				TreeSegment child = node.Left ?? node.Right;

				Replace (node, child);

				if (node.Color == Black && child != null) {
					if (child.Color == Red) {
						child.Color = Black;
					} else {
						DeleteOneChild (child);
					}
				}
			}

			static bool GetColorSafe (TreeSegment node)
			{
				return node != null ? node.Color : Black;
			}

			void DeleteOneChild (TreeSegment node)
			{
				// case 1
				if (node == null || node.Parent == null)
					return;

				var parent = node.Parent;
				var sibling = node.Sibling;
				if (sibling == null)
					return;

				// case 2
				if (sibling.Color == Red) {
					parent.Color = Red;
					sibling.Color = Black;
					if (node == parent.Left) {
						RotateLeft (parent);
					} else {
						RotateRight (parent);
					}
					sibling = node.Sibling;
					if (sibling == null)
						return;
				}

				// case 3
				if (parent.Color == Black && sibling.Color == Black && GetColorSafe (sibling.Left) == Black && GetColorSafe (sibling.Right) == Black) {
					sibling.Color = Red;
					DeleteOneChild (parent);
					return;
				}

				// case 4
				if (parent.Color == Red && sibling.Color == Black && GetColorSafe (sibling.Left) == Black && GetColorSafe (sibling.Right) == Black) {
					sibling.Color = Red;
					parent.Color = Black;
					return;
				}

				// case 5
				if (node == parent.Left && sibling.Color == Black && GetColorSafe (sibling.Left) == Red && GetColorSafe (sibling.Right) == Black) {
					sibling.Color = Red;
					if (sibling.Left != null)
						sibling.Left.Color = Black;
					RotateRight (sibling);
				} else if (node == parent.Right && sibling.Color == Black && GetColorSafe (sibling.Right) == Red && GetColorSafe (sibling.Left) == Black) {
					sibling.Color = Red;
					if (sibling.Right != null)
						sibling.Right.Color = Black;
					RotateLeft (sibling);
				}

				// case 6
				sibling = node.Sibling;
				if (sibling == null)
					return;
				sibling.Color = parent.Color;
				parent.Color = Black;
				if (node == parent.Left) {
					if (sibling.Right != null)
						sibling.Right.Color = Black;
					RotateLeft (parent);
				} else {
					if (sibling.Left != null)
						sibling.Left.Color = Black;
					RotateRight (parent);
				}
			}

			public int Count { get; set; }

			public void Clear ()
			{
				Root = null;
				Count = 0;
			}

			static string GetIndent (int level)
			{
				return new String ('\t', level);
			}

			static void AppendNode (StringBuilder builder, TreeSegment node, int indent)
			{
				builder.Append (GetIndent (indent)).Append ("Node (").Append ((node.Color == Red ? "r" : "b")).Append ("):").AppendLine (node.ToString ());
				builder.Append (GetIndent (indent)).Append ("Left: ");
				if (node.Left != null) {
					builder.Append (Environment.NewLine);
					AppendNode (builder, node.Left, indent + 1);
				} else {
					builder.Append ("null");
				}

				builder.Append (Environment.NewLine);
				builder.Append (GetIndent (indent)).Append ("Right: ");
				if (node.Right != null) {
					builder.Append (Environment.NewLine);
					AppendNode (builder, node.Right, indent + 1);
				} else {
					builder.Append ("null");
				}
			}

			public override string ToString ()
			{
				if (Root == null)
					return "<null>";
				var result = new StringBuilder ();
				AppendNode (result, Root, 0);
				return result.ToString ();
			}
		}
	}

	interface TextSegmentTree
	{
		void Add (TreeSegment segment);
		bool Remove (TreeSegment segment);
	}

	public class TreeSegment : ISegment
	{
		public int Offset {
			get {
				if (segmentTree == null)
					return DistanceToPrevNode;

				var curNode = this;
				int offset = curNode.DistanceToPrevNode;
				if (curNode.Left != null)
					offset += curNode.Left.TotalLength;
				while (curNode.Parent != null) {
					if (curNode == curNode.Parent.Right) {
						if (curNode.Parent.Left != null)
							offset += curNode.Parent.Left.TotalLength;
						offset += curNode.Parent.DistanceToPrevNode;
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

		public int Length {
			get;
			set;
		}

		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

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

		#region Internal API
		internal TextSegmentTree segmentTree;
		internal TreeSegment Parent, Left, Right;
		internal bool Color;
		
		// TotalLength = DistanceToPrevNode + Left.DistanceToPrevNode + Right.DistanceToPrevNode
		internal int TotalLength;

		internal int DistanceToPrevNode;

		// DistanceToMaxEnd = Max (Length, left.DistanceToMaxEnd + Max (left.Offset, right.Offset) - Offset)
		internal int DistanceToMaxEnd;
		
		internal void UpdateAugmentedData ()
		{
			int totalLength = DistanceToPrevNode;
			int distanceToMaxEnd = Length;

			var left = Left;
			if (left != null) {
				totalLength += left.TotalLength;
				int leftdistance = left.DistanceToMaxEnd - DistanceToPrevNode;
				var leftRight = left.Right;
				if (leftRight != null)
					leftdistance -= leftRight.TotalLength;
				if (leftdistance > distanceToMaxEnd)
					distanceToMaxEnd = leftdistance;
			}

			var right = Right;
			if (right != null) {
				totalLength += right.TotalLength;
				int rightdistance = right.DistanceToMaxEnd + right.DistanceToPrevNode;
				var rightLeft = right.Left;
				if (rightLeft != null)
					rightdistance += rightLeft.TotalLength;
				if (rightdistance > distanceToMaxEnd)
					distanceToMaxEnd = rightdistance;
			}

			if (TotalLength != totalLength || DistanceToMaxEnd != distanceToMaxEnd) {
				TotalLength = totalLength;
				DistanceToMaxEnd = distanceToMaxEnd;
				Parent?.UpdateAugmentedData ();
			}
		}

		internal TreeSegment Sibling {
			get {
				if (Parent == null)
					return null;
				return this == Parent.Left ? Parent.Right : Parent.Left;
			}
		}

		internal TreeSegment OuterLeft {
			get {
				TreeSegment result = this;
				while (result.Left != null)
					result = result.Left;
				return result;
			}
		}

		internal TreeSegment OuterRight {
			get {
				TreeSegment result = this;
				while (result.Right != null) {
					result = result.Right;
				}
				return result;
			}
		}

		internal TreeSegment Grandparent {
			get {
				return Parent != null ? Parent.Parent : null;
			}
		}

		internal TreeSegment Uncle {
			get {
				TreeSegment grandparent = Grandparent;
				if (grandparent == null)
					return null;
				return Parent == grandparent.Left ? grandparent.Right : grandparent.Left;
			}
		}

		internal TreeSegment NextNode {
			get {
				if (Right == null) {
					TreeSegment curNode = this;
					TreeSegment oldNode;
					do {
						oldNode = curNode;
						curNode = curNode.Parent;
					} while (curNode != null && curNode.Right == oldNode);
					return curNode;
				}
				return Right.OuterLeft;
			}
		}

		internal TreeSegment PrevNode {
			get {
				if (Left == null) {
					TreeSegment curNode = this;
					TreeSegment oldNode;
					do {
						oldNode = curNode;
						curNode = curNode.Parent;
					} while (curNode != null && curNode.Left == oldNode);
					return curNode;
				}
				return Left.OuterRight;
			}
		}
		#endregion
	}
}
