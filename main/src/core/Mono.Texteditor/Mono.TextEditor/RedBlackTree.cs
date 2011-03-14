// RedBlackTree.cs
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
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class RedBlackTree<T> : ICollection<T>
	{
		public RedBlackTreeNode Root { get; set; }

		public void Add (RedBlackTreeNode node)
		{
			Count++;
			if (Root == null) {
				Root = node;
				FixTreeOnInsert (node);
				return;
			}
			
			RedBlackTreeNode parent = Root;
			
			while (true) {
				if (((IComparable)parent.Value).CompareTo (node.Value) <= 0) {
					if (parent.Left == null) {
						Insert (parent, node, true);
						break;
					}
					parent = parent.Left;
				} else {
					if (parent.Right == null) {
						Insert (parent, node, false);
						break;
					}
					parent = parent.Right;
				}
			}
		}

		public RedBlackTreeIterator Insert (RedBlackTreeNode parent, RedBlackTreeNode node, bool insertLeft)
		{
			if (insertLeft) {
				parent.Left = node;
			} else {
				parent.Right = node;
			}
			node.Parent = parent;
			node.Color = Red;
			
			OnChildrenChanged (new RedBlackTreeNodeEventArgs (parent));
			FixTreeOnInsert (node);
			Count++;
			return new RedBlackTreeIterator (node);
		}
		
		public void InsertBefore (RedBlackTreeNode node, RedBlackTreeNode newNode)
		{
			if (node.Left == null) {
				InsertLeft (node, newNode);
			} else {
				InsertRight (node.Left.OuterRight, newNode);
			}
		}

		public void InsertLeft (RedBlackTreeNode parentNode, RedBlackTreeNode newNode)
		{
			parentNode.Left = newNode;
			newNode.Parent = parentNode;
			newNode.Color = RedBlackTree<TreeSegment>.Red;
			OnChildrenChanged (new RedBlackTreeNodeEventArgs (parentNode));
			FixTreeOnInsert (newNode);
		}

		public void InsertRight (RedBlackTreeNode parentNode, RedBlackTreeNode newNode)
		{
			parentNode.Right = newNode;
			newNode.Parent = parentNode;
			newNode.Color = RedBlackTree<TreeSegment>.Red;
			OnChildrenChanged (new RedBlackTreeNodeEventArgs (parentNode));
			FixTreeOnInsert (newNode);
		}

		void FixTreeOnInsert (RedBlackTreeNode node)
		{
			if (node.Parent == null) {
				node.Color = Black;
				return;
			}
			
			if (node.Parent.Color == Black)
				return;
			
			if (node.Uncle != null && node.Uncle.Color == Red) {
				node.Parent.Color = Black;
				node.Uncle.Color = Black;
				node.Grandparent.Color = Red;
				FixTreeOnInsert (node.Grandparent);
				return;
			}
			
			if (node == node.Parent.Right && node.Parent == node.Grandparent.Left) {
				RotateLeft (node.Parent);
				node = node.Left;
			} else if (node == node.Parent.Left && node.Parent == node.Grandparent.Right) {
				RotateRight (node.Parent);
				node = node.Right;
			}
			
			node.Parent.Color = Black;
			node.Grandparent.Color = Red;
			if (node == node.Parent.Left && node.Parent == node.Grandparent.Left) {
				RotateRight (node.Grandparent);
			} else {
				RotateLeft (node.Grandparent);
			}
		}

		void RotateLeft (RedBlackTreeNode node)
		{
			RedBlackTreeNode right = node.Right;
			Replace (node, right);
			node.Right = right.Left;
			if (node.Right != null)
				node.Right.Parent = node;
			right.Left = node;
			node.Parent = right;
			OnNodeRotateLeft (new RedBlackTreeNodeEventArgs (node));
		}

		void RotateRight (RedBlackTreeNode node)
		{
			RedBlackTreeNode left = node.Left;
			Replace (node, left);
			node.Left = left.Right;
			if (node.Left != null)
				node.Left.Parent = node;
			left.Right = node;
			node.Parent = left;
			OnNodeRotateRight (new RedBlackTreeNodeEventArgs (node));
		}

		public void RemoveAt (RedBlackTreeIterator iter)
		{
			try {
				Remove (iter.Node);
			} catch (Exception e) {
				Console.WriteLine ("remove:" + iter.Node.Value);
				Console.WriteLine (this);
				Console.WriteLine ("----");
				Console.WriteLine (e);
			}
			
		}

		void Replace (RedBlackTreeNode oldNode, RedBlackTreeNode newNode)
		{
			if (newNode != null)
				newNode.Parent = oldNode.Parent;
			if (oldNode.Parent == null) {
				Root = newNode;
			} else {
				if (oldNode.Parent.Left == oldNode)
					oldNode.Parent.Left = newNode;
				else
					oldNode.Parent.Right = newNode;
				OnChildrenChanged (new RedBlackTreeNodeEventArgs (oldNode.Parent));
			}
		}

		public void Remove (RedBlackTreeNode node)
		{
			if (node.Left != null && node.Right != null) {
				RedBlackTreeNode outerLeft = node.Right.OuterLeft;
				Remove (outerLeft);
				Replace (node, outerLeft);
				
				outerLeft.Color = node.Color;
				outerLeft.Left = node.Left;
				if (outerLeft.Left != null)
					outerLeft.Left.Parent = outerLeft;
				
				outerLeft.Right = node.Right;
				if (outerLeft.Right != null)
					outerLeft.Right.Parent = outerLeft;
				OnChildrenChanged (new RedBlackTreeNodeEventArgs (outerLeft));
				return;
			}
			Count--;
			// node has only one child
			RedBlackTreeNode child = node.Left ?? node.Right;
			
			Replace (node, child);
			
			if (node.Color == Black && child != null) {
				if (child.Color == Red) {
					child.Color = Black;
				} else {
					DeleteOneChild (child);
				}
			}
		}

		static bool GetColorSafe (RedBlackTreeNode node)
		{
			return node != null ? node.Color : Black;
		}

		void DeleteOneChild (RedBlackTreeNode node)
		{
			// case 1
			if (node == null || node.Parent == null)
				return;
			
			RedBlackTreeNode parent = node.Parent;
			RedBlackTreeNode sibling = node.Sibling;
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
		
		

		#region ICollection<T> implementation
		public int Count { get; set; }

		public void Add (T item)
		{
			Add (new RedBlackTreeNode (item));
		}

		public void Clear ()
		{
			Root = null;
			Count = 0;
		}

		public bool Contains (T item)
		{
			var iter = new RedBlackTreeIterator (Root.OuterLeft);
			while (iter.IsValid) {
				if (iter.Current.Equals (item))
					return true;
				iter.MoveNext ();
			}
			return false;
		}

		public bool Remove (T item)
		{
			var iter = new RedBlackTreeIterator (Root.OuterLeft);
			while (iter.IsValid) {
				if (iter.Current.Equals (item)) {
					RemoveAt (iter);
					return true;
				}
				iter.MoveNext ();
			}
			return false;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
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

		public RedBlackTreeIterator GetEnumerator ()
		{
			if (Root == null)
				return null;
			var dummyNode = new RedBlackTreeNode (default(T)) { Right = Root };
			return new RedBlackTreeIterator (dummyNode);
		}
		#endregion

		public event EventHandler<RedBlackTreeNodeEventArgs> ChildrenChanged;
		protected virtual void OnChildrenChanged (RedBlackTreeNodeEventArgs args)
		{
			if (ChildrenChanged != null)
				ChildrenChanged (this, args);
		}

		public event EventHandler<RedBlackTreeNodeEventArgs> NodeRotateLeft;
		protected virtual void OnNodeRotateLeft (RedBlackTreeNodeEventArgs args)
		{
			if (NodeRotateLeft != null)
				NodeRotateLeft (this, args);
		}

		public event EventHandler<RedBlackTreeNodeEventArgs> NodeRotateRight;
		protected virtual void OnNodeRotateRight (RedBlackTreeNodeEventArgs args)
		{
			if (NodeRotateRight != null)
				NodeRotateRight (this, args);
		}

		public class RedBlackTreeNodeEventArgs : EventArgs
		{
			public RedBlackTreeNode Node { get; private set; }

			public RedBlackTreeNodeEventArgs (RedBlackTreeNode node)
			{
				Node = node;
			}
		}

		static string GetIndent (int level)
		{
			return new String ('\t', level);
		}

		static void AppendNode (StringBuilder builder, RedBlackTreeNode node, int indent)
		{
			builder.Append (GetIndent (indent) + "Node (" + (node.Color == Red ? "r" : "b") + "):" + node.Value + Environment.NewLine);
			builder.Append (GetIndent (indent) + "Left: ");
			if (node.Left != null) {
				builder.Append (Environment.NewLine);
				AppendNode (builder, node.Left, indent + 1);
			} else {
				builder.Append ("null");
			}
			
			builder.Append (Environment.NewLine);
			builder.Append (GetIndent (indent) + "Right: ");
			if (node.Right != null) {
				builder.Append (Environment.NewLine);
				AppendNode (builder, node.Right, indent + 1);
			} else {
				builder.Append ("null");
			}
		}

		public override string ToString ()
		{
			var result = new StringBuilder ();
			AppendNode (result, Root, 0);
			return result.ToString ();
		}

		internal const bool Red = true;
		internal const bool Black = false;

		public class RedBlackTreeNode
		{
			public RedBlackTreeNode Parent;
			public RedBlackTreeNode Left, Right;
			public T Value;
			public bool Color;

			public RedBlackTreeNode (T value)
			{
				Value = value;
			}

			public bool IsLeaf {
				get { return Left == null && Right == null; }
			}

			public RedBlackTreeNode Sibling {
				get {
					if (Parent == null)
						return null;
					return this == Parent.Left ? Parent.Right : Parent.Left;
				}
			}
			public RedBlackTreeNode OuterLeft {
				get { return Left != null ? Left.OuterLeft : this; }
			}

			public RedBlackTreeNode OuterRight {
				get { return Right != null ? Right.OuterRight : this; }
			}

			public RedBlackTreeNode Grandparent {
				get { return Parent != null ? Parent.Parent : null; }
			}

			public RedBlackTreeNode Uncle {
				get {
					RedBlackTreeNode grandparent = Grandparent;
					if (grandparent == null)
						return null;
					return Parent == grandparent.Left ? grandparent.Right : grandparent.Left;
				}
			}
			
			public RedBlackTreeNode NextNode {
				get {
					if (Right == null) {
						var node = this;
						RedBlackTreeNode oldNode;
						do {
							oldNode = node;
							node = node.Parent;
						} while (node != null && node.Right == oldNode);
						return node;
					}
					return Right.OuterLeft;
				}
			}
			
			public RedBlackTreeNode PrevNode {
				get {
					if (Left == null) {
						var node = this;
						RedBlackTreeNode oldNode;
						do {
							oldNode = node;
							node = node.Parent;
						} while (node != null && node.Left == oldNode);
						return node;
					}
					return Left.OuterRight;
				}
			}
		}

		public class RedBlackTreeIterator : IEnumerator<T>
		{
			public RedBlackTreeNode StartNode;
			public RedBlackTreeNode Node;

			public RedBlackTreeIterator (RedBlackTreeNode node)
			{
				StartNode = Node = node;
			}

			public RedBlackTreeIterator Clone ()
			{
				return new RedBlackTreeIterator (StartNode);
			}

			public bool IsValid {
				get { return Node != null; }
			}

			public T Current {
				get { return Node != null ? Node.Value : default(T); }
			}

			public RedBlackTreeNode CurrentNode {
				get { return Node; }
			}

			object System.Collections.IEnumerator.Current {
				get { return Current; }
			}

			public void Reset ()
			{
				Node = StartNode;
			}

			public void Dispose ()
			{
			}

			public bool MoveNext ()
			{
				if (!IsValid)
					return false;
				Node = Node.NextNode;
				return IsValid;
			}

			public bool MoveBack ()
			{
				if (!IsValid)
					return false;
				Node = Node.PrevNode;
				return IsValid;
			}
		}
	}
}
