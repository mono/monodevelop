//// 
//// AbstractPartitioner.cs
////  
//// Author:
////       Mike Krüger <mkrueger@novell.com>
//// 
//// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using System.Collections.Generic;
//
//namespace Mono.TextEditor
//{
//	public abstract class AbstractPartitioner : IDocumentPartitioner
//	{
//		// HACK: most logic taken from the LineSegmentTree with cut & paste
//		// TODO: clean up the tree system - consider using AVL or B* trees (need some performance tests for them)
//		public class TreeNode : TypedSegment
//		{
//			public int count       = 1;
//			public int totalLength = 0;
//			
//			public TreeNode (int length, string type) : base (length, type)
//			{
//			}
//			
//			public override string ToString ()
//			{
//				return String.Format ("[TreeNode: Line={0}, Count={1}, TotalLength={2}]",
//				                      base.ToString (),
//				                      count,
//				                      totalLength);
//			}
//		}
//		
//		internal RedBlackTree<TreeNode> tree = new RedBlackTree<TreeNode> ();
//		
//		public Document Document {
//			get;
//			set;
//		}
//		
//		public AbstractPartitioner ()
//		{
//			tree.ChildrenChanged += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
//				UpdateNode (args.Node);
//			};
//			tree.NodeRotateLeft += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
//				UpdateNode (args.Node);
//				UpdateNode (args.Node.Parent);
//			};
//			tree.NodeRotateRight += delegate (object sender, RedBlackTree<TreeNode>.RedBlackTreeNodeEventArgs args) {
//				UpdateNode (args.Node);
//				UpdateNode (args.Node.Parent);
//			};
//		}
//		
//		static void UpdateNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
//		{
//			if (node == null)
//				return;
//			
//			int count       = 1;
//			int totalLength = node.Value.Length;
//			
//			if (node.Left != null) {
//				count       += node.Left.Value.count;
//				totalLength += node.Left.Value.totalLength;
//			}
//			
//			if (node.Right != null) {
//				count       += node.Right.Value.count;
//				totalLength += node.Right.Value.totalLength;
//			}
//			if (count != node.Value.count || totalLength != node.Value.totalLength) {
//				node.Value.count       = count;
//				node.Value.totalLength = totalLength;
//				UpdateNode (node.Parent);
//			}
//		}
//		
//		public void Clear ()
//		{
//			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (new TreeNode (0, ""));
//			tree.Root.Value.treeNode = tree.Root;
//			tree.Count = 1;
//		}
//		
//		protected RedBlackTree<TreeNode>.RedBlackTreeNode GetTreeNodeAtOffset (int offset)
//		{
//			if (offset == tree.Root.Value.totalLength) 
//				return tree.Root.OuterRight;
//			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
//			int i = offset;
//			while (true) {
//				if (node == null)
//					return null;
//				if (node.Left != null && i < node.Left.Value.totalLength) {
//					node = node.Left;
//				} else {
//					if (node.Left != null) 
//						i -= node.Left.Value.totalLength;
//					i -= node.Value.Length;
//					if (i < 0) 
//						return node;
//					node = node.Right;
//				} 
//			}
//		}
//		
//		public void ChangeLength (TypedSegment segment, int newLength)
//		{
//			segment.Length = newLength;
//			UpdateNode (segment.treeNode);
//		}
//		
//		public void InsertAfter (TypedSegment segment, TreeNode newSegment)
//		{
//			var newNode = new RedBlackTree<TreeNode>.RedBlackTreeNode (newSegment);
//			RedBlackTree<TreeNode>.RedBlackTreeIterator iter = segment != null ? segment.Iter : null;
//			if (iter == null) {
//				tree.Root = newNode;
//				newSegment.treeNode = tree.Root;
//				tree.Count = 1;
//				return;
//			}
//			
//			if (iter.Node.Right == null) {
//				tree.Insert (iter.Node, newNode, false);
//			} else {
//				tree.Insert (iter.Node.Right.OuterLeft, newNode, true);
//			}
//			newSegment.treeNode = newNode;
//			UpdateNode (newNode);
//		}
//		
//		public TreeNode GetNodeAtOffset (int offset)
//		{
//			RedBlackTree<TreeNode>.RedBlackTreeNode node = GetTreeNodeAtOffset (offset);
//			return node != null ? node.Value : null;
//		}
//		
//		public static int GetOffsetFromNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
//		{
//			int offset = node.Left != null ? node.Left.Value.totalLength : 0;
//			while (node.Parent != null) {
//				if (node == node.Parent.Right) {
//					if (node.Parent.Left != null && node.Parent.Left.Value != null)
//						offset += node.Parent.Left.Value.totalLength;
//					if (node.Parent.Value != null)
//						offset += node.Parent.Value.Length;
//				}
//				node = node.Parent;
//			}
//			return offset;
//		}
//		
//		public virtual void TextReplacing (ReplaceEventArgs args)
//		{
//		}
//		
//		public abstract void TextReplaced (ReplaceEventArgs args);
//		
//		public IEnumerable<TypedSegment> GetPartitions (int offset, int length)
//		{
//			var node = GetTreeNodeAtOffset (offset);
//			if (node == null)
//				yield break;
//			int endOffset = offset + length;
//			yield return node.Value;
//			var iter = node.Value.Iter;
//			while (iter.MoveNext ()) {
//				if (iter.Current.Offset > endOffset)
//					break;
//				yield return iter.Current;
//			}
//		}
//		
//		public IEnumerable<TypedSegment> GetPartitions (ISegment segment)
//		{
//			return GetPartitions (segment.Offset, segment.Length);
//		}
//		
//		public TypedSegment GetPartition (int offset)
//		{
//			return GetNodeAtOffset (offset);
//		}
//		
//		public TypedSegment GetPartition (int line, int column)
//		{
//			return GetPartition (Document.LocationToOffset (line, column));
//		}
//		
//		public TypedSegment GetPartition (DocumentLocation location)
//		{
//			return GetPartition (Document.LocationToOffset (location));
//		}
//	}
//}
//
