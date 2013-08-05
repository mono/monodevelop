//
// AvlTree.cs
//
// Author:
//       Andrea Krüger <andrea.krueger77@googlemail.com>
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

namespace Mono.TextEditor.Utils
{
	interface IAvlNode
	{
		IAvlNode Parent { get; set; }

		IAvlNode Left { get; set; }

		IAvlNode Right { get; set; }

		sbyte Balance { get; set; }

		void UpdateAugmentedData ();
	}

	class AvlTree<T> : ICollection<T> where T : class, IAvlNode
	{
		readonly Func<T, T, int> comparisonFunc;

		public T Root { get; set; }

		public AvlTree () : this (Comparer<T>.Default)
		{
		}

		public AvlTree (IComparer<T> comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException ("comparer");
			this.comparisonFunc = comparer.Compare;
		}

		public AvlTree (Func<T, T, int> comparisonFunc)
		{
			if (comparisonFunc == null)
				throw new ArgumentNullException ("comparisonFunc");
			this.comparisonFunc = comparisonFunc;
		}

		public void InsertLeft (IAvlNode parentNode, IAvlNode newNode)
		{
			if (parentNode == null)
				throw new ArgumentNullException ("parentNode");
			if (newNode == null)
				throw new ArgumentNullException ("newNode");
			parentNode.Left = newNode;
			newNode.Parent = parentNode;
			InsertBalanceTree (parentNode, -1);
			parentNode.UpdateAugmentedData ();
			Count++;
		}

		public void InsertRight (IAvlNode parentNode, IAvlNode newNode)
		{
			if (parentNode == null)
				throw new ArgumentNullException ("parentNode");
			if (newNode == null)
				throw new ArgumentNullException ("newNode");
			parentNode.Right = newNode;
			newNode.Parent = parentNode;
			InsertBalanceTree (parentNode, 1);
			parentNode.UpdateAugmentedData ();
			Count++;
		}

		public void InsertBefore (IAvlNode node, IAvlNode newNode)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			if (newNode == null)
				throw new ArgumentNullException ("newNode");
			if (node.Left == null) {
				InsertLeft (node, newNode);
			} else {
				InsertRight (node.Left.AvlGetOuterRight (), newNode);
			}
		}

		public void InsertAfter (IAvlNode node, T newNode)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			if (newNode == null)
				throw new ArgumentNullException ("newNode");
			if (node.Right == null) {
				InsertRight (node, newNode);
			} else {
				InsertLeft (node.Right.AvlGetOuterLeft (), newNode);
			}
		}

		#region ICollection implementation

		public void Add (T node)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			if (Root == null) {
				Root = node;
				Count = 1;
				return;
			}
			T currentNode = Root;
			while (currentNode != null) {
				if (comparisonFunc (currentNode, node) < 0) {
					if (currentNode.Right == null) {
						InsertRight (currentNode, node);
						break;
					}
					currentNode = (T)currentNode.Right;
				} else {
					if (currentNode.Left == null) {
						InsertLeft (currentNode, node);
						break;
					}
					currentNode = (T)currentNode.Left;
				}
			}
		}

		public void Clear ()
		{
			Root = null;
			Count = 0;
		}

		public bool Contains (T node)
		{
			return this.Any (i => i.Equals (node));
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (array.Length < Count)
				throw new ArgumentException ("The array is too small", "array");
			if (arrayIndex < 0 || arrayIndex + Count > array.Length)
				throw new ArgumentOutOfRangeException ("arrayIndex", arrayIndex, "Value must be between 0 and " + (array.Length - Count));

			int i = arrayIndex;
			foreach (T value in this)
				array [i++] = value;
		}

		bool ICollection<T>.Remove (T node)
		{
			return Remove (node);
		}

		public bool Remove (IAvlNode node)
		{
			if (node == null)
				throw new ArgumentNullException ("node");
			var left = node.Left;
			var right = node.Right;
			var parent = node.Parent;

			if (left == null) {
				if (right == null) {
					if (node == Root) {
						Clear ();
						return true;
					}
					if (parent == null) {
						throw new Exception ();
					}
					if (parent.Left == node) {
						parent.Left = null;
						DeleteBalanceTree (parent, 1);
					} else {
						parent.Right = null;
						DeleteBalanceTree (parent, -1);
					}
				} else {
					if (node == Root) {
						node.Right.Parent = null;
						Root = (T)node.Right;
						return true;
					}
					node.Right.Parent = node.Parent;
					if (parent.Left == node) {
						parent.Left = node.Right;
						DeleteBalanceTree (parent, 1);
					} else {
						parent.Right = node.Right;
						DeleteBalanceTree (parent, -1);
					}
				}
			} else if (right == null) {
				if (node == Root) {
					node.Left.Parent = null;
					Root = (T)node.Left;
					return true;
				}
				node.Left.Parent = node.Parent;
				if (parent.Left == node) {
					parent.Left = node.Left;
					DeleteBalanceTree (parent, 1);
				} else {
					parent.Right = node.Left;
					DeleteBalanceTree (parent, -1);
				}
			} else { // no (half-)leaf
				IAvlNode successor;
				if (node.Balance > -1)
					successor = node.AvlGetNextNode ();
				else
					successor = node.AvlGetPrevNode ();
				SwitchNodes (node, successor);
				successor.UpdateAugmentedData ();
				return Remove (node);
			}
			if (parent != null) {
				parent.UpdateAugmentedData ();
			}
			Count--;
			OnNodeRemoved (new TreeNodeEventArgs ((T)node));
			return true;
		}

		public int Count {
			get;
			internal set;
		}

		bool ICollection<T>.IsReadOnly {
			get {
				return false;
			}
		}

		protected virtual void OnNodeRemoved (TreeNodeEventArgs e)
		{
			var handler = this.NodeRemoved;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<TreeNodeEventArgs> NodeRemoved;

		public class TreeNodeEventArgs : EventArgs
		{
			public T Node { get; private set; }

			public TreeNodeEventArgs (T node)
			{
				Node = node;
			}
		}
		#endregion

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator ()
		{
			if (Root == null)
				yield break;
			var node = Root.AvlGetOuterLeft ();
			while (node != null) {
				yield return node;
				node = node.AvlGetNextNode ();
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion

		public void RotateLeft (IAvlNode node)
		{
			var rightChild = node.Right;

			var rightLeft = rightChild.Left;
			var parent = node.Parent;

			rightChild.Parent = parent;
			rightChild.Left = node;

			if (rightChild.Balance == 0) {
				node.Balance = 1;
			} else if (rightChild.Balance == 1) {
				node.Balance = 0;
			}

			rightChild.Balance--;

			node.Parent = rightChild;
			node.Right = rightLeft;

			if (rightLeft != null)
				rightLeft.Parent = node;
			node.UpdateAugmentedData ();
			rightChild.UpdateAugmentedData ();

			if (node == Root) {
				Root = (T)rightChild;
			} else {
				if (parent.Right == node) {
					parent.Right = rightChild;
				} else {
					parent.Left = rightChild;
				}
				parent.UpdateAugmentedData ();
			}
		}

		public void RotateRight (IAvlNode node)
		{
			var leftChild = node.Left;

			var leftRight = leftChild.Right;
			var parent = node.Parent;

			leftChild.Parent = parent;
			leftChild.Right = node;

			if (leftChild.Balance == 0) {
				node.Balance = -1;
			} else if (leftChild.Balance == -1) {
				node.Balance = 0;
			}

			leftChild.Balance++;

			node.Parent = leftChild;
			node.Left = leftRight;

			if (leftRight != null)
				leftRight.Parent = node;
			node.UpdateAugmentedData ();
			leftChild.UpdateAugmentedData ();

			if (node == Root) {
				Root = (T)leftChild;
			} else {
				if (parent.Left == node) {
					parent.Left = leftChild;
				} else {
					parent.Right = leftChild;
				}
				parent.UpdateAugmentedData ();
			}
		}

		public void RotateRightLeft (IAvlNode node)
		{
			var rightChild = node.Right;
			var rightLeft = rightChild.Left;
			var rightLeftRight = rightLeft.Right;

			rightChild.Left = rightLeftRight;
			if (rightLeftRight != null)
				rightLeftRight.Parent = rightChild;
			rightLeft.Right = rightChild;
			rightChild.Parent = rightLeft;
			node.Right = rightLeft.Left;
			if (rightLeft.Left != null)
				rightLeft.Left.Parent = node;
			rightLeft.Parent = node.Parent;
			if (node == Root) {
				Root = (T)rightLeft;
			} else {
				if (node.Parent.Right == node) {
					node.Parent.Right = rightLeft;
				} else {
					node.Parent.Left = rightLeft;
				}
				node.Parent.UpdateAugmentedData ();
			}
			rightLeft.Left = node;
			node.Parent = rightLeft;
			switch (rightLeft.Balance) {
			case 0:
				rightChild.Balance = 0;
				node.Balance = 0;
				break;
			case 1:
				rightChild.Balance = 0;
				node.Balance = -1;
				break;
			default: // -1
				rightChild.Balance = 1;
				node.Balance = 0;
				break;
			}
			rightLeft.Balance = 0;

			rightChild.UpdateAugmentedData ();
			node.UpdateAugmentedData ();
			rightLeft.UpdateAugmentedData ();
		}

		public void RotateLeftRight (IAvlNode node)
		{
			var leftChild = node.Left;
			var leftRight = leftChild.Right;
			var leftRightLeft = leftRight.Left;

			leftChild.Right = leftRightLeft;
			if (leftRightLeft != null)
				leftRightLeft.Parent = leftChild;
			leftRight.Left = leftChild;
			leftChild.Parent = leftRight;
			node.Left = leftRight.Right;
			if (leftRight.Right != null)
				leftRight.Right.Parent = node;
			leftRight.Parent = node.Parent;
			if (node == Root) {
				Root = (T)leftRight;
			} else {
				if (node.Parent.Right == node) {
					node.Parent.Right = leftRight;
				} else {
					node.Parent.Left = leftRight;
				}
				node.Parent.UpdateAugmentedData (); 
			}
			leftRight.Right = node;
			node.Parent = leftRight;
			switch (leftRight.Balance) {
			case 1:
				leftChild.Balance = -1;
				node.Balance = 0;
				break;
			case 0:
				leftChild.Balance = 0;
				node.Balance = 0;
				break;
			default: // -1
				leftChild.Balance = 0;
				node.Balance = 1;
				break;
			}
			leftRight.Balance = 0;

			leftChild.UpdateAugmentedData ();
			node.UpdateAugmentedData ();
			leftRight.UpdateAugmentedData ();
		}

		void InsertBalanceTree (IAvlNode node, sbyte balance)
		{
			while (node != null) {
				node.Balance += balance;
				balance = node.Balance;

				if (balance == 0)
					return;
				if (balance == -2) {
					if (node.Left.Balance < 1) {
						RotateRight (node);
					} else {
						RotateLeftRight (node);
					}
					return;
				}

				if (balance == 2) {
					if (node.Right.Balance > -1) {
						RotateLeft (node);
					} else {
						RotateRightLeft (node);
					}
					return;
				}

				var parent = node.Parent;
				if (parent != null)
					balance = parent.Left == node ? (sbyte)-1 : (sbyte)1;
				node = parent;
			}
		}

		void DeleteBalanceTree (IAvlNode node, sbyte balance)
		{
			while (node != null) {
				node.Balance += balance;
				balance = node.Balance;
				if (balance == -2) {
					if (node.Left.Balance < 1) {
						RotateRight (node);
						if (node.Balance == 0) {
							node = node.Parent;
						} else if (node.Balance == -1)
							return;
					} else {
						RotateLeftRight (node);
						node = node.Parent;
					}
				} else if (balance == 2) {
					if (node.Right.Balance > -1) {
						RotateLeft (node);
						if (node.Balance == 0) {
							node = node.Parent;
						} else if (node.Balance == 1)
							return;
					} else {
						RotateRightLeft (node);
						node = node.Parent;
					}
				} else if (node.Balance != 0) {
					return;
				}

				var parent = node.Parent;
				if (parent != null)
					balance = parent.Left == node ? (sbyte)1 : (sbyte)-1;
				node = parent;
			}
		}

		public void SwitchNodes (IAvlNode oldNode, IAvlNode newNode)
		{
			if (oldNode == newNode)
				return;

			var oldBalance = oldNode.Balance;
			var newLeft = newNode.Left;
			var newRight = newNode.Right;
			var oldParent = oldNode.Parent;

			oldNode.Balance = newNode.Balance;
			newNode.Balance = oldBalance;

			// oldNode and newNode are Parent and Child
			if (newNode.Parent == oldNode) {
				if (oldNode.Parent != null) {
					if (oldNode.Parent.Right == oldNode) {
						oldNode.Parent.Right = newNode;
					} else {
						oldNode.Parent.Left = newNode;
					}
				} else if (oldNode == Root) {
					Root = (T)newNode;
				}
				newNode.Parent = oldNode.Parent;
				oldNode.Parent = newNode;

				if (oldNode.Left == newNode) {
					// update Children
					newNode.Left = oldNode;
					newNode.Right = oldNode.Right;
					oldNode.Left = newLeft;
					oldNode.Right = newRight;

					// update Parents of Children
					if (newNode.Right != null)
						newNode.Right.Parent = newNode;
					if (oldNode.Right != null)
						oldNode.Right.Parent = oldNode;
					if (oldNode.Left != null)
						oldNode.Left.Parent = oldNode;
				} else { //odlNode.Right == newNode
					// update Children
					newNode.Right = oldNode;
					newNode.Left = oldNode.Left;
					oldNode.Left = newLeft;
					oldNode.Right = newRight;

					// update Parents of Children
					if (newNode.Left != null)
						newNode.Left.Parent = newNode;
					if (oldNode.Right != null)
						oldNode.Right.Parent = oldNode;
					if (oldNode.Left != null)
						oldNode.Left.Parent = oldNode;
				}
				return;
			}
			if (oldNode.Parent == newNode) {
				if (newNode.Parent != null) {
					if (newNode.Parent.Right == newNode) {
						newNode.Parent.Right = oldNode;
					} else {
						newNode.Parent.Left = oldNode;
					}
				} else if (newNode == Root) {
					Root = (T)oldNode;
				}
				oldNode.Parent = newNode.Parent;
				newNode.Parent = oldNode;

				if (newNode.Left == oldNode) {
					// update Children
					newNode.Left = oldNode.Left;
					newNode.Right = oldNode.Right;
					oldNode.Left = newNode;
					oldNode.Right = newRight;

					// update Parents of Children
					if (newNode.Right != null)
						newNode.Right.Parent = newNode;
					if (newNode.Left != null)
						newNode.Left.Parent = newNode;
					if (oldNode.Right != null)
						oldNode.Right.Parent = oldNode;
				} else {
					// update Children
					newNode.Left = oldNode.Left;
					newNode.Right = oldNode.Right;
					oldNode.Left = newLeft;
					oldNode.Right = newNode;

					// update Parents of Children
					if (newNode.Right != null)
						newNode.Right.Parent = newNode;
					if (newNode.Left != null)
						newNode.Left.Parent = newNode;
					if (oldNode.Left != null)
						oldNode.Left.Parent = oldNode;
				}
				return;
			}

			// no node is Parent of the other

			// update Parents
			if (oldNode.AvlGetSibling () == newNode) {
				if (newNode.Parent.Right == newNode) {
					newNode.Parent.Right = oldNode;
					newNode.Parent.Left = newNode;
				} else {
					newNode.Parent.Left = oldNode;
					newNode.Parent.Right = newNode;
				}
			} else {
				oldNode.Parent = newNode.Parent;
				if (newNode.Parent != null) {
					if (newNode.Parent.Right == newNode) {
						newNode.Parent.Right = oldNode;
					} else {
						newNode.Parent.Left = oldNode;
					}
				} else if (newNode == Root) {
					Root = (T)oldNode;
				}
				newNode.Parent = oldParent;
				if (oldParent != null) {
					if (oldParent.Right == oldNode) {
						oldParent.Right = newNode;
					} else {
						oldParent.Left = newNode;
					}
				} else if (oldNode == Root) {
					Root = (T)newNode;
				}
			}
			// assign Children of newNode
			newNode.Left = oldNode.Left;
			if (newNode.Left != null)
				newNode.Left.Parent = newNode;
			newNode.Right = oldNode.Right;
			if (newNode.Right != null)
				newNode.Right.Parent = newNode;

			// assign Children of oldNode
			oldNode.Left = newLeft;
			if (oldNode.Left != null)
				oldNode.Left.Parent = oldNode;
			oldNode.Right = newRight;
			if (oldNode.Right != null)
				oldNode.Right.Parent = oldNode;
		}
	}

	static class AvlExtensions
	{
		public static bool IsLeaf (this IAvlNode node)
		{
			return node.Left == null && node.Right == null;
		}

		public static T AvlGetSibling<T> (this T node) where T : class, IAvlNode
		{
			if (node.Parent == null)
				return null;
			return (T)(node == node.Parent.Left ? node.Parent.Right : node.Parent.Left);
		}

		public static T AvlGetOuterLeft<T> (this T node) where T : class, IAvlNode
		{
			IAvlNode result = node;
			while (result.Left != null)
				result = result.Left;
			return (T)result;
		}

		public static T AvlGetOuterRight<T> (this T node) where T : class, IAvlNode
		{
			IAvlNode result = node;
			while (result.Right != null) {
				result = result.Right;
			}
			return (T)result;
		}

		public static T AvlGetGrandparent<T> (this T node) where T : class, IAvlNode
		{
			return (T)(node.Parent != null ? node.Parent.Parent : null);
		}

		public static T AvlGetUncle<T> (this T node) where T : class, IAvlNode
		{
			return (T)(node.Parent != null ? node.Parent.AvlGetSibling () : null);
			//			var grandparent = node.AvlGetGrandparent ();
			//			if (grandparent == null)
			//				return null;
			//			return (T)(node.Parent == grandparent.Left ? grandparent.Right : grandparent.Left);
		}

		public static T AvlGetNextNode<T> (this T node) where T : class, IAvlNode
		{
			if (node.Right == null) {
				IAvlNode curNode = node;
				IAvlNode oldNode;
				do {
					oldNode = curNode;
					curNode = curNode.Parent;
				} while (curNode != null && curNode.Right == oldNode);
				return (T)curNode;
			}
			return (T)node.Right.AvlGetOuterLeft ();
		}

		public static T AvlGetPrevNode<T> (this T node) where T : class, IAvlNode
		{
			if (node.Left == null) {
				IAvlNode curNode = node;
				IAvlNode oldNode;
				do {
					oldNode = curNode;
					curNode = curNode.Parent;
				} while (curNode != null && curNode.Left == oldNode);
				return (T)curNode;
			}
			return (T)node.Left.AvlGetOuterRight ();
		}
	}
}

