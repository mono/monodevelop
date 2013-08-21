// 
// PieceTable.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace Mono.MHex.Data
{
	class PieceTable
	{
		public abstract class TreeNode
		{
			public long TotalLength {
				get;
				set;
			}
			
			public long CalcOffset (RedBlackTree<TreeNode>.RedBlackTreeNode Node) 
			{
				RedBlackTree<TreeNode>.RedBlackTreeNode cur = Node;
				long offset = cur.left != null ? cur.left.value.TotalLength : 0;
				while (cur.parent != null) {
					if (cur == cur.parent.right) {
						if (cur.parent.left != null && cur.parent.left.value != null)
							offset += cur.parent.left.value.TotalLength;
						if (Node.parent.value != null)
							offset += cur.parent.value.Length;
					}
					cur = cur.parent;
				}
				return offset;
			}
			
			public long Length {
				get;
				set;
			}
			
			public TreeNode (long length)
			{
				this.Length = length;
			}
			
			public abstract byte[] GetBytes (HexEditorData hexEditorData, long myOffset, long offset, int count);
			
			public TreeNode SplitRight (long leftLength)
			{
				return InternalSplitRight (System.Math.Min (Length, System.Math.Max (0, leftLength)));
			}
			
			protected abstract TreeNode InternalSplitRight (long leftLength);
		}
		
		public class OriginalTreeNode : TreeNode, ICloneable
		{
			long BufferOffset {
				get;
				set;
			}
			
			public OriginalTreeNode (long bufferOffset, long length) : base (length)
			{
				this.BufferOffset = bufferOffset;
			}
			
			public override byte[] GetBytes (HexEditorData hexEditorData, long myOffset, long offset, int count)
			{
				return hexEditorData.buffer.GetBytes (BufferOffset + offset - myOffset, count);
			}
			
			protected override TreeNode InternalSplitRight (long leftLength)
			{
				return new OriginalTreeNode (BufferOffset + leftLength, Length - leftLength);
			}
			
			public override string ToString ()
			{
				return string.Format ("[OriginalTreeNode: Length={0}, bufferOffset={1}, TotalLength={2}]", Length, BufferOffset, TotalLength);
			}
			
			public object Clone ()
			{
				OriginalTreeNode result = new OriginalTreeNode (BufferOffset, Length);
				result.TotalLength = TotalLength;
				return result;
			}
		}
		
		public class DataTreeNode : TreeNode, ICloneable
		{
			int AddBufferOffset {
				get;
				set;
			}
			
			public DataTreeNode (int addBufferOffset, long length) : base (length)
			{
				this.AddBufferOffset = addBufferOffset;
			}
			
			public override byte[] GetBytes (HexEditorData hexEditorData, long myOffset, long offset, int count)
			{
				byte[] result = new byte[count];
				for (int i = 0, j = (int)(AddBufferOffset + offset - myOffset); i < result.Length; i++, j++) {
					result[i] = hexEditorData.addBuffer [j];
				}
				return result;
			}
			
			protected override TreeNode InternalSplitRight (long leftLength)
			{
				return new DataTreeNode (AddBufferOffset + (int)leftLength, Length - leftLength);
			}
			
			public override string ToString ()
			{
				return string.Format ("[DataTreeNode: Length={0}, addBufferOffset={1}, TotalLength={2}]", Length, AddBufferOffset, TotalLength);
			}
			public object Clone ()
			{
				DataTreeNode result = new DataTreeNode (AddBufferOffset, Length);
				result.TotalLength = TotalLength;
				return result;
			}
		}
		
		internal RedBlackTree<TreeNode> tree = new RedBlackTree<TreeNode> ();
		public int Count {
			get {
				return tree.Count;
			}
		}
		
		public long Length {
			get {
				return tree.Root.value.TotalLength;
			}
		}
		
		public PieceTable ()
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
			Clear ();
		}
		
		void UpdateNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			if (node == null)
				return;
			long currentTotalLength = node.value.Length;
			
			if (node.left != null) 
				currentTotalLength += node.left.value.TotalLength;
			
			if (node.right != null)
				currentTotalLength += node.right.value.TotalLength;
			
			if (currentTotalLength != node.value.TotalLength) {
				node.value.TotalLength = currentTotalLength;
				UpdateNode (node.parent);
			}
		}
		
		
		void ChangeLength (RedBlackTree<TreeNode>.RedBlackTreeNode node, long newLength)
		{
			node.value.Length = newLength;
			UpdateNode (node);
		}
		
		void RemoveNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			RedBlackTree<TreeNode>.RedBlackTreeNode parent = node.parent; 
			tree.RemoveNode (node);
			UpdateNode (parent); 
			if (tree.Root == null)
				Clear ();
		}
		
		RedBlackTree<TreeNode>.RedBlackTreeNode InsertAfter (RedBlackTree<TreeNode>.RedBlackTreeNode node, TreeNode nodeToInsert)
		{
			RedBlackTree<TreeNode>.RedBlackTreeNode newNode = new RedBlackTree<TreeNode>.RedBlackTreeNode (nodeToInsert);
			
			RedBlackTree<TreeNode>.RedBlackTreeIterator iter = new RedBlackTree<TreeNode>.RedBlackTreeIterator (node);
			
			if (iter.node.right == null) {
				tree.Insert (iter.node, newNode, false);
			} else {
				tree.Insert (iter.node.right.OuterLeft, newNode, true);
			}
			
			UpdateNode (newNode);
			return newNode;
		}
		
		public void Clear ()
		{
			PieceTable.TreeNode node = new OriginalTreeNode (0, 0);
			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (node);
			tree.Count = 1;
		}
		
		public RedBlackTree<TreeNode>.RedBlackTreeNode GetTreeNodeAtOffset (long offset)
		{
			if (offset == tree.Root.value.TotalLength) 
				return tree.Root.OuterRight;
			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
			long i = offset;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.value.TotalLength) {
					node = node.left;
				} else {
					if (node.left != null) 
						i -= node.left.value.TotalLength;
					i -= node.value.Length;
					if (i < 0) 
						return node;
					node = node.right;
				} 
			}
		}
		
		public void Remove (long offset, long length)
		{
			if (length == 0 || Length == 0) 
				return; 
			
			RedBlackTree<TreeNode>.RedBlackTreeNode startNode = GetTreeNodeAtOffset (offset);
			RedBlackTree<TreeNode>.RedBlackTreeNode endNode = GetTreeNodeAtOffset (offset + length);
			
			long newLength = offset - startNode.value.CalcOffset (startNode);
			if (startNode == endNode) {
				TreeNode splittedNode = startNode.value.SplitRight (newLength + length);
				ChangeLength (startNode, newLength);
				
				if (splittedNode.Length > 0)
					InsertAfter (startNode, splittedNode);
				
				return;
			}
			long endSegmentLength = endNode != null ? offset + length - endNode.value.CalcOffset (endNode) : 0;
			RedBlackTree<TreeNode>.RedBlackTreeIterator iter = new RedBlackTree<TreeNode>.RedBlackTreeIterator (startNode);
			RedBlackTree<TreeNode>.RedBlackTreeNode node;
			do {
				node = iter.CurrentNode;
				iter.MoveNext ();
				if (node == null)
					break;
				if (node == startNode) {
					// has no right side, otherwise it would be startNode == endNode
					length -= node.value.Length;
					ChangeLength (node, newLength);
				} else if (node == endNode) {
					// has no left side, otherwise it would be startNode == endNode
					TreeNode rightSide = node.value.SplitRight (endSegmentLength);
					if (rightSide.Length > 0)
						InsertAfter (node, rightSide);
					RemoveNode (node);
				} else { // nodes in between 
					length -= node.value.Length; 
					RemoveNode (node);
				}
			} while (node != endNode);
		}
		
		public void Insert (long offset, int addBufferOffset, long addLength)
		{
			RedBlackTree<TreeNode>.RedBlackTreeNode node = GetTreeNodeAtOffset (offset);
			long oldNodeOffset = node.value.CalcOffset (node);
			long newLength = offset - oldNodeOffset;
			
			TreeNode splittedNode = node.value.SplitRight (newLength);
			ChangeLength (node, newLength);
			
			RedBlackTree<TreeNode>.RedBlackTreeNode newNode = InsertAfter (node, new DataTreeNode (addBufferOffset, addLength));
			
			if (splittedNode.Length > 0)
				InsertAfter (newNode, splittedNode);
			
			if (newLength == 0)
				RemoveNode (node);
		}

		public void SetBuffer (IBuffer buffer)
		{
			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (new OriginalTreeNode (0, buffer.Length));
			tree.Root.value.TotalLength = buffer.Length;
			tree.Count = 1;
		}
	}
}
