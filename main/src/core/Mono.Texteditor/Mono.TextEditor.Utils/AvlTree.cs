//
// AvlTree.cs
//
// Author:
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
	internal interface IAvlNode
	{
		IAvlNode Parent { get; set; }

		IAvlNode Left { get; set; }

		IAvlNode Right { get; set; }

		int Balance { get; set; }

		void UpdateAugmentedData ();
	}

	internal class AvlTree<T> : ICollection<T> where T : class, IAvlNode
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
			InsertBalanceTree (parentNode, 1);
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
			InsertBalanceTree (parentNode, -1);
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

		public void InsertAfter (IAvlNode node, IAvlNode newNode)
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

		public void Add (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			if (Root == null) {
				Root = item;
				Count = 1;
				return;
			}
			IAvlNode currentNode = Root;
			while (currentNode != null) {
				if (comparisonFunc ((T)currentNode, item) == -1) {
					if (currentNode.Right == null) {
						InsertRight (currentNode, item);
						break;
					}
					currentNode = currentNode.Right;
				} else {
					if (currentNode.Left == null) {
						InsertLeft (currentNode, item);
						break;
					}
					currentNode = currentNode.Left;
				}
			}
		}

		public void Clear ()
		{
			Root = null;
			Count = 0;
		}

		public bool Contains (T item)
		{
			return this.Any (i => i.Equals (item));
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

		public bool Remove (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			IAvlNode left = item.Left;
			IAvlNode right = item.Right;

			if (left == null) {
				if (right == null) {
					if (item == Root) {
						Clear ();
						return true;
					}
					var parent = item.Parent;

					if (parent.Left == item) {
						parent.Left = null;
						DeleteBalanceTree (parent, -1);
					} else {
						parent.Right = null;
						DeleteBalanceTree (parent, 1);
					}
				} else {
					ReplaceNodes (item, right);

					DeleteBalanceTree (item, 0);
				}
			} else if (right == null) {
				ReplaceNodes (item, left);

				DeleteBalanceTree (item, 0);
			} else {
				var successor = right;
				if (successor.Left == null) {
					IAvlNode parent = item.Parent;

					successor.Parent = parent;
					successor.Left = left;
					successor.Balance = item.Balance;

					if (left != null) {
						left.Parent = successor;
					}

					if (item == Root) {
						Root = (T)successor;
					} else {
						if (parent.Left == item) {
							parent.Left = successor;
						} else {
							parent.Right = successor;
						}
					}

					DeleteBalanceTree (successor, 1);
				} else {
					while (successor.Left != null) {
						successor = successor.Left;
					}

					IAvlNode parent = item.Parent;
					IAvlNode successorParent = successor.Parent;
					IAvlNode successorRight = successor.Right;

					if (successorParent.Left == successor) {
						successorParent.Left = successorRight;
					} else {
						successorParent.Right = successorRight;
					}

					if (successorRight != null) {
						successorRight.Parent = successorParent;
					}

					successor.Parent = parent;
					successor.Left = left;
					successor.Balance = item.Balance;
					successor.Right = right;
					right.Parent = successor;

					if (left != null) {
						left.Parent = successor;
					}

					if (item == Root) {
						Root = (T)successor;
					} else {
						if (parent.Left == item) {
							parent.Left = successor;
						} else {
							parent.Right = successor;
						}
					}

					DeleteBalanceTree (successorParent, -1);
				}
			}
			Count--;
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

		void RotateRight (IAvlNode node)
		{
			IAvlNode rightChild = node.Right;
			IAvlNode rightLeft = null;
			IAvlNode parent = node.Parent;

			if (rightChild != null) {
				rightLeft = rightChild.Left;
				rightChild.Parent = parent;
				rightChild.Left = node;
				rightChild.Balance++;
				node.Balance = -rightChild.Balance;
			}

			node.Parent = rightChild;
			node.Right = rightLeft;

			if (rightLeft != null)
				rightLeft.Parent = node;
			node.UpdateAugmentedData ();

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

		void RotateLeft (IAvlNode node)
		{
			IAvlNode leftChild = node.Left;
			IAvlNode leftRight = null;
			IAvlNode parent = node.Parent;

			if (leftChild != null) {
				leftRight = leftChild.Right;
				leftChild.Parent = parent;
				leftChild.Right = node;
				leftChild.Balance--;
				node.Balance = -leftChild.Balance;
			}

			node.Parent = leftChild;
			node.Left = leftRight;

			if (leftRight != null)
				leftRight.Parent = node;
			node.UpdateAugmentedData ();

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

		void RotateRightLeft (IAvlNode node)
		{
			IAvlNode rightChild = node.Right;
			IAvlNode rightLeft = null;
			IAvlNode rightLeftRight = null;

			if (rightChild != null)
				rightLeft = rightChild.Left;
			if (rightLeft != null)
				rightLeftRight = rightLeft.Right;

			node.Right = rightLeft;

			if (rightLeft != null) {
				rightLeft.Parent = node;
				rightLeft.Right = rightChild;
				rightLeft.Balance--;
			}

			if (rightChild != null) {
				rightChild.Parent = rightLeft;
				rightChild.Left = rightLeftRight;
				rightChild.Balance--;
			}

			if (rightLeftRight != null)
				rightLeftRight.Parent = rightChild;

			RotateRight (node);
		}

		void RotateLeftRight (IAvlNode node)
		{
			IAvlNode leftChild = node.Left;
			IAvlNode leftRight = null;
			IAvlNode leftRightLeft = null;

			if (leftChild != null)
				leftRight = leftChild.Right;
			if (leftRight != null)
				leftRightLeft = leftRight.Left;

			node.Left = leftRight;

			if (leftRight != null) {
				leftRight.Parent = node;
				leftRight.Left = leftChild;
				leftRight.Balance++;
			}

			if (leftChild != null) {
				leftChild.Parent = leftRight;
				leftChild.Right = leftRightLeft;
				leftChild.Balance++;
			}

			if (leftRightLeft != null)
				leftRightLeft.Parent = leftChild;

			RotateLeft (node);
		}

		void InsertBalanceTree (IAvlNode node, int balance)
		{
			while (node != null) {
				node.Balance += balance;
				balance = node.Balance;

				if (balance == 0)
					break;
				if (balance == 2) {
					if (node.Left.Balance == 1) {
						RotateLeft (node);
					} else {
						RotateLeftRight (node);
					}
					break;
				}

				if (balance == -2) {
					if (node.Right.Balance == -1) {
						RotateRight (node);
					} else {
						RotateRightLeft (node);
					}
					break;
				}

				var parent = node.Parent;
				if (parent != null)
					balance = parent.Left == node ? 1 : -1;
				node = parent;
			}
		}

		void DeleteBalanceTree (IAvlNode node, int balance)
		{
			while (node != null) {
				node.Balance += balance;
				balance = node.Balance;
				if (balance == 2) {
					if (node.Left != null && node.Left.Balance >= 0) {
						RotateLeft (node);

						if (node.Balance == -1)
							return;
					} else {
						RotateLeftRight (node);
					}
				} else if (balance == -2) {
					if (node.Right != null && node.Right.Balance <= 0) {
						RotateRight (node);

						if (node.Balance == 1)
							return;
					} else {
						RotateRightLeft (node);
					}
				} else if (node.Balance != 0) {
					return;
				}

				var parent = node.Parent;
				if (parent != null)
					balance = parent.Left == node ? -1 : 1;
				node = parent;
			}
		}

		void ReplaceNodes (IAvlNode oldNode, IAvlNode newNode)
		{
			oldNode.Balance = newNode.Balance;

			if (oldNode.Left != null && oldNode.Left != newNode) {
				newNode.Left = oldNode.Left;
				newNode.Left.Parent = newNode;
			}

			if (oldNode.Right != null && oldNode.Right != newNode) {
				newNode.Right = oldNode.Right;
				newNode.Right.Parent = newNode;
			}

			if (oldNode.Parent != null) {
				if (oldNode.Parent.Left == oldNode) {
					oldNode.Parent.Left = newNode;
				} else {
					oldNode.Parent.Right = newNode;
				}
				newNode.Parent = oldNode.Parent;
			} else {
				Root = (T)newNode;
				newNode.Parent = null;
			}
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
			var grandparent = node.AvlGetGrandparent ();
			if (grandparent == null)
				return null;
			return (T)(node.Parent == grandparent.Left ? grandparent.Right : grandparent.Left);
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

