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

namespace Mono.MHex.Data
{
	class RedBlackTree<T> : ICollection<T>
	{
		public RedBlackTreeNode Root {
			get;
			set;
		}
		
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
				if (((IComparable)parent.value).CompareTo (node.value) <= 0) {
					if (parent.left == null) {
						Insert (parent, node, true);
						break;
					}
					parent = parent.left;
				} else {
					if (parent.right == null) {
						Insert (parent, node, false);
						break;
					}
					parent = parent.right;
				}
			}
		}
		
		public void Insert (RedBlackTreeNode parent, RedBlackTreeNode node, bool insertLeft)
		{
			if (insertLeft) {
				Debug.Assert (parent.left == null);
				parent.left = node;
			} else {
				Debug.Assert (parent.right == null);
				parent.right = node;
			}
			node.parent = parent;
			node.color = red;
			
			this.OnChildrenChanged (new RedBlackTreeNodeEventArgs (parent));
			FixTreeOnInsert (node);
			Count++;
		}
		
		void FixTreeOnInsert (RedBlackTreeNode node)
		{
			if (node.parent == null) {
				node.color = black;
				return;
			}
			
			if (node.parent.color == black) 
				return;
			
			if (node.Uncle != null && node.Uncle.color == red) {
				node.parent.color = black;
				node.Uncle.color = black;
				node.Grandparent.color = red;
				FixTreeOnInsert (node.Grandparent);
				return;
			}
			
			if (node == node.parent.right && node.parent == node.Grandparent.left) {
				RotateLeft (node.parent);
				node = node.left;
			} else if (node == node.parent.left && node.parent == node.Grandparent.right) {
				RotateRight (node.parent);
				node = node.right;
			}
			
			node.parent.color = black;
			node.Grandparent.color = red;
			if (node == node.parent.left && node.parent == node.Grandparent.left) {
				RotateRight (node.Grandparent);
			} else {
				RotateLeft (node.Grandparent);
			}
		}
		
		void RotateLeft (RedBlackTreeNode node) 
		{
			RedBlackTreeNode right = node.right;
			this.Replace (node, right);
			node.right = right.left;
			if (node.right != null) 
				node.right.parent = node;
			right.left = node;
			node.parent = right;
			this.OnNodeRotateLeft (new RedBlackTreeNodeEventArgs (node));
		}

		void RotateRight (RedBlackTreeNode node) 
		{
			RedBlackTreeNode left = node.left;
			Replace (node, left);
			node.left = left.right;
			if (node.left != null) 
				node.left.parent = node;
			left.right = node;
			node.parent = left;			
			this.OnNodeRotateRight (new RedBlackTreeNodeEventArgs (node));
		}
		
		public void RemoveAt (RedBlackTreeIterator iter)
		{
			try {
				RemoveNode (iter.node);
			} catch (Exception e) {
				string s1 = "remove:" + iter.node.value;
				string s2 = this.ToString ();
				Console.WriteLine (s1);
				Console.WriteLine (s2);
				Console.WriteLine ("----");
				Console.WriteLine (e);
			}
		}
		
		void Replace (RedBlackTreeNode oldNode, RedBlackTreeNode newNode)
		{
			if (newNode != null)
				newNode.parent = oldNode.parent;
			if (oldNode.parent == null) {
				Root = newNode;
			} else {
				if (oldNode.parent.left == oldNode || oldNode == null && oldNode.parent.left == null)
					oldNode.parent.left = newNode;
				else
					oldNode.parent.right = newNode;
				this.OnChildrenChanged (new RedBlackTreeNodeEventArgs (oldNode.parent));
			}
		}
		
		public void RemoveNode (RedBlackTreeNode node)
		{
			if (node.left != null && node.right != null) {
				RedBlackTreeNode outerLeft = node.right.OuterLeft;
				RemoveNode (outerLeft);
				Replace (node, outerLeft);
				
				outerLeft.color = node.color;
				outerLeft.left = node.left;
				if (outerLeft.left != null) 
					outerLeft.left.parent = outerLeft;
				
				outerLeft.right = node.right;
				if (outerLeft.right != null) 
					outerLeft.right.parent = outerLeft;
				this.OnChildrenChanged (new RedBlackTreeNodeEventArgs (outerLeft));
				return;
			}
			Count--;
			// node has only one child
			RedBlackTreeNode child = node.left ?? node.right;
			Replace (node, child);
			
			if (node.color == black && child != null) {
				if (child.color == red) {
					child.color = black;
				} else {
					DeleteOneChild (child);
				}
			}
		}
		
		static bool GetColorSafe (RedBlackTreeNode node)
		{
			return node != null ? node.color : black;
		}
				
		void DeleteOneChild (RedBlackTreeNode node) 
		{
			// case 1
			if (node == null || node.parent == null)
				return;
			
			RedBlackTreeNode parent  = node.parent;
			RedBlackTreeNode sibling = node.Sibling;
			if (sibling == null)
				return;
			
			// case 2
			if (sibling.color == red) {
				parent.color  = red;
				sibling.color = black;
				if (node == parent.left) {
					RotateLeft (parent);
				} else {
					RotateRight (parent);
				}
				sibling = node.Sibling;
				if (sibling == null)
					return;
			}
			
			// case 3
			if (parent.color == black &&
			    sibling.color == black &&
			    GetColorSafe (sibling.left) == black &&
			    GetColorSafe (sibling.right) == black) {
				sibling.color = red;
				DeleteOneChild (parent);
				return;
			}
			
			// case 4
			if (parent.color == red &&
			    sibling.color == black &&
			    GetColorSafe (sibling.left) == black &&
			    GetColorSafe (sibling.right) == black) {
				sibling.color = red;
				parent.color = black;
				return;
			}
			
			// case 5
			if (node == parent.left &&
			    sibling.color == black &&
			    GetColorSafe (sibling.left) == red &&
			    GetColorSafe (sibling.right) == black) {
				sibling.color = red;
				if (sibling.left != null)
					sibling.left.color = black;
				RotateRight (sibling);
			} else if (node == parent.right &&
			           sibling.color == black &&
			           GetColorSafe (sibling.right) == red &&
			           GetColorSafe (sibling.left) == black) {
				sibling.color = red;
				if (sibling.right != null)
					sibling.right.color = black;
				RotateLeft (sibling);
			}
			
			// case 6
			sibling = node.Sibling;
			if (sibling == null)
				return;
			sibling.color = parent.color;
			parent.color = black;
			if (node == parent.left) {
				if (sibling.right != null) 
					sibling.right.color = black;
				RotateLeft (parent);
			} else {
				if (sibling.left != null) 
					sibling.left.color = black;
				RotateRight (parent);
			}
		}
		
#region ICollection<T> implementation
		public int Count {
			get;
			set;
		}
		
		public void Add(T item)
		{
			Add (new RedBlackTreeNode (item));
		}
		
		public void Clear()
		{
			Root = null;
			Count = 0;
		}
		
		public bool Contains(T item)
		{
			RedBlackTreeIterator iter = new RedBlackTreeIterator (Root.OuterLeft);
			while (iter.IsValid) {
				if (iter.Current.Equals (item)) 
					return true;
				iter.MoveNext ();
			}
			return false;
		}
		
		public bool Remove(T item)
		{
			RedBlackTreeIterator iter = new RedBlackTreeIterator (Root.OuterLeft);
			while (iter.IsValid) {
				if (iter.Current.Equals (item)) {
					this.RemoveAt (iter);
					return true;
				}
				iter.MoveNext ();
			}
			return false;
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
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
				array [i++] = value;
		}
		
		public RedBlackTreeIterator GetEnumerator()
		{
			if (Root == null) 
				return null;
			RedBlackTreeNode dummyNode = new RedBlackTreeNode (default(T));
			dummyNode.right = Root;
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
			public RedBlackTreeNode Node {
				get;
				private set;
			}
			
			public RedBlackTreeNodeEventArgs (RedBlackTreeNode node)
			{
				this.Node = node;
			}
		}
		
		string GetIndent (int level)
		{
			return new String ('\t', level);
		}
		
		void AppendNode (StringBuilder builder, RedBlackTreeNode node, int indent)
		{
			builder.Append (GetIndent (indent)).Append ("Node (").Append ((node.color == red ? "r" : "b")).Append ("):").AppendLine (node.value.ToString ());
			builder.Append (GetIndent (indent)).Append ("Left: ");
			if (node.left != null) {
				builder.Append (Environment.NewLine);
				AppendNode (builder, node.left, indent + 1);
			} else { 
				builder.Append ("null");
			}
				
			builder.Append (Environment.NewLine);
			builder.Append (GetIndent (indent)).Append ("Right: ");
			if (node.right != null) {
				builder.Append (Environment.NewLine);
				AppendNode (builder, node.right, indent + 1);
			} else { 
				builder.Append ("null");
			}
		}
		
		public override string ToString ()
		{
			StringBuilder result = new StringBuilder ();
			AppendNode (result, Root, 0);
			return result.ToString ();
		}
		
		static bool red   = true;
		static bool black = false;
		
		public class RedBlackTreeNode 
		{
			public RedBlackTreeNode parent;
			public RedBlackTreeNode left, right;
			public T value;
			public bool color;
			
			public RedBlackTreeNode (T value)
			{
				this.value = value;
			}
			
			public RedBlackTreeNode Clone ()
			{
				RedBlackTreeNode result = new RedBlackTreeNode ((T)(value as ICloneable).Clone ());
				if (left != null) {
					result.left = left.Clone ();
					result.left.parent = result;
				}
				if (right != null) {
					result.right = right.Clone ();
					result.right.parent = result;
				}
				result.color = color;
				return result;
			}
			
			public bool IsLeaf {
				get {
					return left == null && right == null;
				}
			}
			
			public RedBlackTreeNode Sibling {
				get {
					if (parent == null)
						return null;
					return this == parent.left ? parent.right : parent.left;
				}
			}
			
			public RedBlackTreeNode OuterLeft {
				get {
					return left != null ? left.OuterLeft : this;
				}
			}
			
			public RedBlackTreeNode OuterRight {
				get {
					return right != null ? right.OuterRight : this;
				}
			}
			
			public RedBlackTreeNode Grandparent {
				get {
					return parent != null ? parent.parent : null;
				}
			}
			
			public RedBlackTreeNode Uncle {
				get {
					RedBlackTreeNode grandparent = Grandparent;
					if (grandparent == null)
						return null;
					return parent == grandparent.left ? grandparent.right : grandparent.left;
				}
			}
			public override string ToString ()
			{
				return string.Format ("[RedBlackTreeNode: value={0}, color={1}]", value, color);
			}
			
			
		}
		
		public class RedBlackTreeIterator : IEnumerator<T>
		{
			public RedBlackTreeNode startNode;
			public RedBlackTreeNode node;
			
			public RedBlackTreeIterator (RedBlackTreeNode node)
			{
				this.startNode = this.node = node;
			}
			
			public RedBlackTreeIterator Clone ()
			{
				return new RedBlackTreeIterator (this.startNode);
			}
			
			public bool IsValid {
				get { return node != null; }
			}
			
			public T Current {
				get {
					return node != null ? node.value : default(T);
				}
			}
			
			public RedBlackTreeNode CurrentNode {
				get {
					return node;
				}
			}
			
			object System.Collections.IEnumerator.Current {
				get {
					return this.Current;
				}
			}
			
			public void Reset ()
			{
				this.node = this.startNode; 
			}
			
			public void Dispose ()
			{
			}
			
			public bool MoveNext ()
			{
				if (!IsValid)
					return false;
				if (node.right == null) {
					RedBlackTreeNode oldNode;
					do {
						oldNode = node;
						node = node.parent;
					} while (node != null && node.right == oldNode);
				} else {
					node = node.right.OuterLeft;
				}
				return IsValid;
			}
			
			public bool MoveBack ()
			{
				if (!IsValid)
					return false;
				if (node.left == null) {
					RedBlackTreeNode oldNode;
					do {
						oldNode = node;
						node = node.parent;
					} while (node != null && node.left == oldNode);
				} else {
					node = node.left.OuterRight;
				}
				return IsValid;
			}
		}
	}
}