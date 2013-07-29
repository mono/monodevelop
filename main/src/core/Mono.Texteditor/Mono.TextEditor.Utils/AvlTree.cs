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
using System.Text;
using System.Diagnostics;

namespace Mono.TextEditor.Utils
{
	public interface IAvlNode
	{
		IAvlNode Parent { get; set; }

		IAvlNode Left { get; set; }

		IAvlNode Right { get; set; }

		int Balance { get; set; }

		void UpdateAugmentedData ();
	}

	public class AvlTree<T> : ICollection<T> where T : class, IAvlNode
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
			parentNode.Left = newNode;
			newNode.Parent = parentNode;
			InsertBalanceTree (parentNode, 1);
			parentNode.UpdateAugmentedData ();
			Count++;
		}

		public void InsertRight (IAvlNode parentNode, IAvlNode newNode)
		{
			parentNode.Right = newNode;
			newNode.Parent = parentNode;
			InsertBalanceTree (parentNode, -1);
			parentNode.UpdateAugmentedData ();
			Count++;
		}

		#region ICollection implementation

		public void Add (T item)
		{
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

		public bool Remove (T current)
		{
			if (current.Left == null && current.Right == null) {
				if (current == Root) {
					Root = null;
					Count = 0;
					return true;
				}

				if (current.Parent.Right == current) {
					current.Parent.Right = null;
					DeleteBalanceTree (current.Parent, 1);
				} else {
					current.Parent.Left = null;
					DeleteBalanceTree (current.Parent, -1);
				}
			} else if (current.Left != null) {
				var rightMost = current.AvlGetOuterRight ();
				ReplaceNodes (current, rightMost);
				DeleteBalanceTree (rightMost.Parent, 1);
			} else {
				var leftMost = current.AvlGetOuterLeft ();
				ReplaceNodes (current, leftMost);
				DeleteBalanceTree (leftMost.Parent, -1);
			}

			Count--;
			return true;
		}

		public int Count {
			get;
			private set;
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
			var queue = new Queue<IAvlNode> ();
			queue.Enqueue (Root);
			int x = 0;
			while (queue.Count > 0 && x++ < 10) {
				var tmp = queue.Dequeue ();
				if (tmp.Left != null)
					queue.Enqueue (tmp.Left);
				if (tmp.Right != null)
					queue.Enqueue (tmp.Right);
				yield return (T)tmp;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion

		void RotateRightRight (IAvlNode node)
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

			node.Right = rightLeft;
			node.Parent = rightChild;

			if (rightLeft != null) {
				rightLeft.Parent = node;
			}
			node.UpdateAugmentedData ();
			if (node == this.Root) {
				this.Root = (T)rightChild;
			} else if (parent.Right == node) {
				parent.Right = rightChild;
				parent.UpdateAugmentedData ();
			} else {
				parent.Left = rightChild;
				parent.UpdateAugmentedData ();
			}
		}

		void RotateLeftLeft (IAvlNode node)
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

			if (leftRight != null) {
				leftRight.Parent = node;
			}
			node.UpdateAugmentedData ();

			if (node == this.Root) {
				this.Root = (T)leftChild;
			} else if (parent.Left == node) {
				parent.Left = leftChild;
				parent.UpdateAugmentedData ();
			} else {
				parent.Right = leftChild;
				parent.UpdateAugmentedData ();
			}
		}

		void RotateRightLeft (IAvlNode node)
		{
			IAvlNode rightChild = node.Right;
			IAvlNode rightLeft = null;
			IAvlNode rightLeftRight = null;

			if (rightChild != null) {
				rightLeft = rightChild.Left;
			}
			if (rightLeft != null) {
				rightLeftRight = rightLeft.Right;
			}

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

			if (rightLeftRight != null) {
				rightLeftRight.Parent = rightChild;
			}
			RotateRightRight (node);
		}

		void RotateLeftRight (IAvlNode node)
		{
			IAvlNode leftChild = node.Left;
			IAvlNode leftRight = leftChild.Right;
			IAvlNode leftRightLeft = null;
			if (leftRight != null) {
				leftRightLeft = leftRight.Left;
			}

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

			if (leftRightLeft != null) {
				leftRightLeft.Parent = leftChild;
			}

			RotateLeftLeft (node);
		}

		void InsertBalanceTree (IAvlNode node, int addBalance)
		{
			while (node != null) {
				//Add the new balance value to the current node balance.
				node.Balance += addBalance;

				/*
                 * If the balance was -1 or +1, the tree is still balanced so
                 * we don't have to balanced it further
                */
				if (node.Balance == 0) {
					break;
				}
				//If the height(left-subtree) - height(right-subtree) == 2
				else if (node.Balance == 2) {
					if (node.Left.Balance == 1) {
						RotateLeftLeft (node);
					} else {
						RotateLeftRight (node);
					}
					break;
				}

				//If the height(left-subtree) - height(right-subtree) == -2
				else if (node.Balance == -2) {
					if (node.Right.Balance == -1) {
						RotateRightRight (node);
					} else {
						RotateRightLeft (node);
					}
					break;
				}

				if (node.Parent != null) {
					/*
                     * If the current node is the left child of the parent node
                     * we need to increase the height of the parent node.
                     * */
					if (node.Parent.Left == node) {
						addBalance = 1;
					}
					/*
                     * If it is the right child,
                     * we decrease the height of the parent node
                     * */
					else {
						addBalance = -1;
					}
				}
				node = node.Parent;
			}
		}

		void DeleteBalanceTree (IAvlNode node, int addBalance)
		{
			while (node != null) {
				node.Balance += addBalance;
				addBalance = node.Balance;

				if (node.Balance == 2) {
					if (node.Left != null && node.Left.Balance >= 0) {
						RotateLeftLeft (node);

						if (node.Balance == -1) {
							return;
						}
					} else {
						RotateLeftRight (node);
					}
				} else if (node.Balance == -2) {
					if (node.Right != null && node.Right.Balance <= 0) {
						RotateRightRight (node);

						if (node.Balance == 1) {
							return;
						}
					} else {
						RotateRightLeft (node);
					}
				} else if (node.Balance != 0) {
					return;
				}

				IAvlNode parent = node.Parent;

				if (parent != null) {
					if (parent.Left == node) {
						addBalance = -1;
					} else {
						addBalance = 1;
					}
				}
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

	public static class AvlExtensions
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

