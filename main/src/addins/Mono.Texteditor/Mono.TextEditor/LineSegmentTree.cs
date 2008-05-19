// LineSegmentTree.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Diagnostics;

namespace Mono.TextEditor
{
	public class LineSegmentTree : IDisposable 
	{
		public class TreeNode : LineSegment
		{
			public int count       = 1;
			public int totalLength = 0;
			
			public TreeNode (int length, int delimiterLength) : base (length, delimiterLength)
			{
			}
			
			public override string ToString ()
			{
				return String.Format ("[TreeNode: Line={0}, Count={1}, TotalLength={2}]",
				                      base.ToString (),
				                      count,
				                      totalLength);
			}
		}
		
		RedBlackTree<TreeNode> tree = new RedBlackTree<TreeNode> ();
		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public int Length {
			get {
				return tree.Root.value.totalLength;
			}
		
		}
		
		public LineSegmentTree()
		{
			tree.ChildrenChanged += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
				UpdateNode (args.Node);
			};
			tree.NodeRotateLeft += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
				UpdateNode (args.Node);
				UpdateNode (args.Node.parent);
			};
			tree.NodeRotateRight += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
				UpdateNode (args.Node);
				UpdateNode (args.Node.parent);
			};
		}
		
		public void Dispose ()
		{
			if (tree != null) {
				tree.Dispose ();
				tree = null;
			}
		}
		
		void UpdateNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			if (node == null)
				return;
			
			int count       = 1;
			int totalLength = node.value.Length;
			
			if (node.left != null) {
				count       += node.left.value.count;
				totalLength += node.left.value.totalLength;
			}
			
			if (node.right != null) {
				count       += node.right.value.count;
				totalLength += node.right.value.totalLength;
			}
			if (count != node.value.count || totalLength != node.value.totalLength) {
				node.value.count       = count;
				node.value.totalLength = totalLength;
				UpdateNode (node.parent);
			}
		}
		
		public void Clear ()
		{
			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (new TreeNode (0, 0));
			tree.Root.value.TreeNode =  tree.Root;
			tree.Count = 1;
		}
		
		public LineSegment InsertAfter (LineSegment segment, int length, int delimiterLength)
		{
			TreeNode result = new TreeNode (length, delimiterLength);
			result.StartSpan = segment.StartSpan;
			RedBlackTree<TreeNode>.RedBlackTreeNode newNode = new RedBlackTree<TreeNode>.RedBlackTreeNode (result);
			RedBlackTree<TreeNode>.RedBlackTreeIterator iter = segment != null ? segment.Iter : null;
			if (iter == null) {
				tree.Root = newNode;
				result.TreeNode = tree.Root;
				tree.Count = 1;
				return result;
			}
			
			if (iter.node.right == null) {
				tree.Insert (iter.node, newNode, false);
			} else {
				tree.Insert (iter.node.right.OuterLeft, newNode, true);
			}
			result.TreeNode = newNode;
			UpdateNode (newNode);
			return result;
		}
		
		public override string ToString ()
		{
			return tree.ToString ();
		}
		
		
		public void ChangeLength (LineSegment line, int newLength)
		{
			Debug.Assert (line != null);
			ChangeLength (line, newLength, line.DelimiterLength);
		}
		
		public void ChangeLength (LineSegment line, int newLength, int delimiterLength)
		{
			Debug.Assert (line != null);
			Debug.Assert (newLength >= 0);
			line.Length = newLength;
			line.DelimiterLength = delimiterLength;
			UpdateNode (line.Iter.CurrentNode);
		}
		
		public static int GetOffsetFromNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			int offset = node.left != null ? node.left.value.totalLength : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null && node.parent.left.value != null)
						offset += node.parent.left.value.totalLength;
					if (node.parent.value != null)
						offset += node.parent.value.Length;
				}
				node = node.parent;
			}
			return offset;
		}
		
		public TreeNode GetNodeAtOffset (int offset)
		{
			Debug.Assert (0 <= offset && offset <= tree.Root.value.totalLength);
			if (offset == tree.Root.value.totalLength) 
				return tree.Root.OuterRight.value;
			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
			int i = offset;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.value.totalLength) {
					node = node.left;
				} else {
					if (node.left != null) 
						i -= node.left.value.totalLength;
					i -= node.value.Length;
					if (i < 0) 
						return node.value;
					node = node.right;
				} 
			}
		}
		
		public void RemoveLine (LineSegment line)
		{
			RedBlackTree<TreeNode>.RedBlackTreeNode parent = line.Iter.CurrentNode.parent; 
			tree.RemoveAt (line.Iter);
			UpdateNode (parent); 
		}
		
		public TreeNode GetNode (int index)
		{
#if DEBUG
			if (index < 0)
				Debug.Assert (false, "index must be >=0 but was " + index + "." + Environment.NewLine + "Stack trace:" + Environment.StackTrace);
#endif
			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
			int i = index;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.value.count) {
					node = node.left;
				} else {
					if (node.left != null) {
						i -= node.left.value.count;
					}
					if (i <= 0)
						return node.value;
					i--;
					node = node.right;
				} 
			}
		}
	}
}
