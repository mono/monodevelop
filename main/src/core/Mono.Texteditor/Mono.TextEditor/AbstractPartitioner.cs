// 
// AbstractPartitioner.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor
{
	public abstract class AbstractPartitioner : IDocumentPartitioner
	{
		// HACK: most logic taken from the LineSegmentTree with cut & paste
		// TODO: clean up the tree system - consider using AVL or B* trees (need some performance tests for them)
		public class TreeNode : TypedSegment
		{
			public int count       = 1;
			public int totalLength = 0;
			
			public TreeNode (int length, string type) : base (length, type)
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
		
		internal RedBlackTree<TreeNode> tree = new RedBlackTree<TreeNode> ();
		
		public Document Document {
			get;
			set;
		}
		
		public AbstractPartitioner ()
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
		
		static void UpdateNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
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
			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (new TreeNode (0, ""));
			tree.Root.value.treeNode = tree.Root;
			tree.Count = 1;
		}
		
		protected RedBlackTree<TreeNode>.RedBlackTreeNode GetTreeNodeAtOffset (int offset)
		{
			if (offset == tree.Root.value.totalLength) 
				return tree.Root.OuterRight;
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
						return node;
					node = node.right;
				} 
			}
		}
		
		public void ChangeLength (TypedSegment segment, int newLength)
		{
			segment.Length = newLength;
			UpdateNode (segment.treeNode);
		}
		
		public void InsertAfter (TypedSegment segment, TreeNode newSegment)
		{
			var newNode = new RedBlackTree<TreeNode>.RedBlackTreeNode (newSegment);
			RedBlackTree<TreeNode>.RedBlackTreeIterator iter = segment != null ? segment.Iter : null;
			if (iter == null) {
				tree.Root = newNode;
				newSegment.treeNode = tree.Root;
				tree.Count = 1;
				return;
			}
			
			if (iter.node.right == null) {
				tree.Insert (iter.node, newNode, false);
			} else {
				tree.Insert (iter.node.right.OuterLeft, newNode, true);
			}
			newSegment.treeNode = newNode;
			UpdateNode (newNode);
		}
		
		public TreeNode GetNodeAtOffset (int offset)
		{
			RedBlackTree<TreeNode>.RedBlackTreeNode node = GetTreeNodeAtOffset (offset);
			return node != null ? node.value : null;
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
		
		public virtual void TextReplacing (ReplaceEventArgs args)
		{
		}
		
		public abstract void TextReplaced (ReplaceEventArgs args);
		
		public IEnumerable<TypedSegment> GetPartitions (int offset, int length)
		{
			var node = GetTreeNodeAtOffset (offset);
			if (node == null)
				yield break;
			int endOffset = offset + length;
			yield return node.value;
			var iter = node.value.Iter;
			while (iter.MoveNext ()) {
				if (iter.Current.Offset > endOffset)
					break;
				yield return iter.Current;
			}
		}
		
		public IEnumerable<TypedSegment> GetPartitions (ISegment segment)
		{
			return GetPartitions (segment.Offset, segment.Length);
		}
		
		public TypedSegment GetPartition (int offset)
		{
			return GetNodeAtOffset (offset);
		}
		
		public TypedSegment GetPartition (int line, int column)
		{
			return GetPartition (Document.LocationToOffset (line, column));
		}
		
		public TypedSegment GetPartition (DocumentLocation location)
		{
			return GetPartition (Document.LocationToOffset (location));
		}
	}
}

