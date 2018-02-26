// 
// RedBlackTree.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using System.Text;
using System.Diagnostics;
using MonoDevelop.Core;

namespace Mono.TextEditor.Utils
{
	enum RedBlackColor : byte
	{
		Black = 0,
		Red = 1
	}
	
	interface IRedBlackTreeNode
	{
		IRedBlackTreeNode Parent { get; set; }
		IRedBlackTreeNode Left { get; set; }
		IRedBlackTreeNode Right { get; set; }
		
		RedBlackColor Color { get; set; }
		
		void UpdateAugmentedData ();
	}
	
	static class RedBlackTreeExtensionMethods
	{
		public static bool IsLeaf (this IRedBlackTreeNode node)
		{
			return node.Left == null && node.Right == null;
		}
		
		public static T GetSibling<T> (this T node) where T : class, IRedBlackTreeNode
		{
			if (node.Parent == null)
				return null;
			return (T)(node == node.Parent.Left ? node.Parent.Right : node.Parent.Left);
		}
		
		public static T GetOuterLeft<T> (this T node) where T : class, IRedBlackTreeNode
		{
			IRedBlackTreeNode result = node;
			while (result.Left != null)
				result = result.Left;
			return (T)result;
		}
		
		public static T GetOuterRight<T> (this T node) where T : class, IRedBlackTreeNode
		{
			IRedBlackTreeNode result = node;
			while (result.Right != null) {
				result = result.Right;
			}
			return (T)result;
		}
		
		public static T GetGrandparent<T> (this T node) where T : class, IRedBlackTreeNode
		{
			return (T)(node.Parent != null ? node.Parent.Parent : null);
		}
		
		public static T GetUncle<T> (this T node) where T : class, IRedBlackTreeNode
		{
			IRedBlackTreeNode grandparent = node.GetGrandparent ();
			if (grandparent == null)
				return null;
			return (T)(node.Parent == grandparent.Left ? grandparent.Right : grandparent.Left);
		}

		public static T GetNextNode<T> (this T node) where T : class, IRedBlackTreeNode
		{
			if (node.Right == null) {
				IRedBlackTreeNode curNode = node;
				IRedBlackTreeNode oldNode;
				do {
					oldNode = curNode;
					curNode = curNode.Parent;
				} while (curNode != null && curNode.Right == oldNode);
				return (T)curNode;
			}
			return (T)node.Right.GetOuterLeft ();
		}

		public static T GetPrevNode<T> (this T node) where T : class, IRedBlackTreeNode
		{
			if (node.Left == null) {
				IRedBlackTreeNode curNode = node;
				IRedBlackTreeNode oldNode;
				do {
					oldNode = curNode;
					curNode = curNode.Parent;
				} while (curNode != null && curNode.Left == oldNode);
				return (T)curNode;
			}
			return (T)node.Left.GetOuterRight ();
		}
	}
	
	class RedBlackTree<T> : ICollection<T> where T : class, IRedBlackTreeNode
	{
		public T Root { get; set; }
		
		bool ICollection<T>.Remove (T node)
		{
			Remove (node);
			return true;
		}
		
		public void Add (T node)
		{
			if (Root == null) {
				Count = 1;
				Root = node;
				FixTreeOnInsert (node);
				return;
			}
			
			IRedBlackTreeNode parent = Root;
			
			while (true) {
				if (((IComparable)parent).CompareTo (node) <= 0) {
					if (parent.Left == null) {
						InsertLeft (parent, node);
						break;
					}
					parent = parent.Left;
				} else {
					if (parent.Right == null) {
						InsertRight (parent, node);
						break;
					}
					parent = parent.Right;
				}
			}
		}
		
		public void InsertBefore (IRedBlackTreeNode node, IRedBlackTreeNode newNode)
		{
			if (node.Left == null) {
				InsertLeft (node, newNode);
			} else {
				InsertRight (node.Left.GetOuterRight (), newNode);
			}
		}
		
		public void InsertAfter (IRedBlackTreeNode node, IRedBlackTreeNode newNode)
		{
			if (node.Right == null) {
				InsertRight (node, newNode);
			} else {
				InsertLeft (node.Right.GetOuterLeft (), newNode);
			}
		}

		public void InsertLeft (IRedBlackTreeNode parentNode, IRedBlackTreeNode newNode)
		{
			parentNode.Left = newNode;
			newNode.Parent = parentNode;
			newNode.Color = RedBlackColor.Red;
			parentNode.UpdateAugmentedData ();
			FixTreeOnInsert (newNode);
			Count++;
		}

		public void InsertRight (IRedBlackTreeNode parentNode, IRedBlackTreeNode newNode)
		{
			parentNode.Right = newNode;
			newNode.Parent = parentNode;
			newNode.Color = RedBlackColor.Red;
			parentNode.UpdateAugmentedData ();
			FixTreeOnInsert (newNode);
			Count++;
		}

		void FixTreeOnInsert (IRedBlackTreeNode node)
		{
			var parent = node.Parent;
			if (parent == null) {
				node.Color = RedBlackColor.Black;
				return;
			}
			
			if (parent.Color == RedBlackColor.Black)
				return;
			var uncle = node.GetUncle ();
			IRedBlackTreeNode grandParent = parent.Parent;
			
			if (uncle != null && uncle.Color == RedBlackColor.Red) {
				parent.Color = RedBlackColor.Black;
				uncle.Color = RedBlackColor.Black;
				grandParent.Color = RedBlackColor.Red;
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
			
			parent.Color = RedBlackColor.Black;
			grandParent.Color = RedBlackColor.Red;
			if (node == parent.Left && parent == grandParent.Left) {
				RotateRight (grandParent);
			} else {
				RotateLeft (grandParent);
			}
		}

		void RotateLeft (IRedBlackTreeNode node)
		{
			IRedBlackTreeNode right = node.Right;
			Replace (node, right);
			node.Right = right.Left;
			if (node.Right != null)
				node.Right.Parent = node;
			right.Left = node;
			node.Parent = right;
			node.UpdateAugmentedData ();
			node.Parent.UpdateAugmentedData ();
		}

		void RotateRight (IRedBlackTreeNode node)
		{
			IRedBlackTreeNode left = node.Left;
			Replace (node, left);
			node.Left = left.Right;
			if (node.Left != null)
				node.Left.Parent = node;
			left.Right = node;
			node.Parent = left;
			node.UpdateAugmentedData ();
			node.Parent.UpdateAugmentedData ();
		}

		void Replace (IRedBlackTreeNode oldNode, IRedBlackTreeNode newNode)
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

		public void Remove (IRedBlackTreeNode node)
		{
			if (node.Left != null && node.Right != null) {
				IRedBlackTreeNode outerLeft = node.Right.GetOuterLeft ();
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
				OnNodeRemoved (new RedBlackTreeNodeEventArgs ((T)node));
				return;
			}
			InternalRemove (node);
			OnNodeRemoved (new RedBlackTreeNodeEventArgs ((T)node));
		}
		
		void InternalRemove (IRedBlackTreeNode node)
		{
			if (node.Left != null && node.Right != null) {
				IRedBlackTreeNode outerLeft = node.Right.GetOuterLeft ();
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
			IRedBlackTreeNode child = node.Left ?? node.Right;
			
			Replace (node, child);
			
			if (node.Color == RedBlackColor.Black && child != null) {
				if (child.Color == RedBlackColor.Red) {
					child.Color = RedBlackColor.Black;
				} else {
					DeleteOneChild (child);
				}
			}
		}

		protected virtual void OnNodeRemoved (RedBlackTreeNodeEventArgs e)
		{
			EventHandler<RedBlackTreeNodeEventArgs> handler = this.NodeRemoved;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<RedBlackTreeNodeEventArgs> NodeRemoved;

		static RedBlackColor GetColorSafe (IRedBlackTreeNode node)
		{
			return node != null ? node.Color : RedBlackColor.Black;
		}

		void DeleteOneChild (IRedBlackTreeNode node)
		{
			// case 1
			if (node == null || node.Parent == null)
				return;
			
			var parent = node.Parent;
			var sibling = node.GetSibling ();
			if (sibling == null)
				return;
			
			// case 2
			if (sibling.Color == RedBlackColor.Red) {
				parent.Color = RedBlackColor.Red;
				sibling.Color = RedBlackColor.Black;
				if (node == parent.Left) {
					RotateLeft (parent);
				} else {
					RotateRight (parent);
				}
				sibling = node.GetSibling ();
				if (sibling == null)
					return;
			}
			
			// case 3
			if (parent.Color == RedBlackColor.Black && sibling.Color == RedBlackColor.Black && GetColorSafe (sibling.Left) == RedBlackColor.Black && GetColorSafe (sibling.Right) == RedBlackColor.Black) {
				sibling.Color = RedBlackColor.Red;
				DeleteOneChild (parent);
				return;
			}
			
			// case 4
			if (parent.Color == RedBlackColor.Red && sibling.Color == RedBlackColor.Black && GetColorSafe (sibling.Left) == RedBlackColor.Black && GetColorSafe (sibling.Right) == RedBlackColor.Black) {
				sibling.Color = RedBlackColor.Red;
				parent.Color = RedBlackColor.Black;
				return;
			}
			
			// case 5
			if (node == parent.Left && sibling.Color == RedBlackColor.Black && GetColorSafe (sibling.Left) == RedBlackColor.Red && GetColorSafe (sibling.Right) == RedBlackColor.Black) {
				sibling.Color = RedBlackColor.Red;
				if (sibling.Left != null)
					sibling.Left.Color = RedBlackColor.Black;
				RotateRight (sibling);
			} else if (node == parent.Right && sibling.Color == RedBlackColor.Black && GetColorSafe (sibling.Right) == RedBlackColor.Red && GetColorSafe (sibling.Left) == RedBlackColor.Black) {
				sibling.Color = RedBlackColor.Red;
				if (sibling.Right != null)
					sibling.Right.Color = RedBlackColor.Black;
				RotateLeft (sibling);
			}
			
			// case 6
			sibling = node.GetSibling ();
			if (sibling == null)
				return;
			sibling.Color = parent.Color;
			parent.Color = RedBlackColor.Black;
			if (node == parent.Left) {
				if (sibling.Right != null)
					sibling.Right.Color = RedBlackColor.Black;
				RotateLeft (parent);
			} else {
				if (sibling.Left != null)
					sibling.Left.Color = RedBlackColor.Black;
				RotateRight (parent);
			}
		}
		
		#region ICollection<T> implementation
		public int Count { get; set; }

		public void Clear ()
		{
			Root = null;
			Count = 0;
		}

		public bool Contains (T item)
		{
			return this.Any (i => item.Equals (i));
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			if (Root == null)
				yield break;
			var node = Root.GetOuterLeft ();
			while (node != null) {
				yield return node;
				node = node.GetNextNode ();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			if (Root == null)
				yield break;
			var node = Root.GetOuterLeft ();
			while (node != null) {
				yield return node;
				node = node.GetNextNode ();
			}
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			Debug.Assert (array != null);
			Debug.Assert (0 <= arrayIndex && arrayIndex < array.Length);
			int i = arrayIndex;
			foreach (T value in this)
				array[i++] = value;
		}

		#endregion

		internal class RedBlackTreeNodeEventArgs : EventArgs
		{
			public T Node { get; private set; }

			public RedBlackTreeNodeEventArgs (T node)
			{
				Node = node;
			}
		}

		static string GetIndent (int level)
		{
			return new String ('\t', level);
		}

		static void AppendNode (StringBuilder builder, IRedBlackTreeNode node, int indent)
		{
			builder.Append (GetIndent (indent)).Append ("Node (").Append ((node.Color == RedBlackColor.Red ? "r" : "b")).Append ("):").AppendLine (node.ToString ());
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
			var result = StringBuilderCache.Allocate ();
			AppendNode (result, Root, 0);
			return StringBuilderCache.ReturnAndFree (result);
		}
	}
}
