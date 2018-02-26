//
// CompressingTreeList.cs
//
// Based on CompressingTreeList from AvalonEdit by Daniel Grundwald<daniel@danielgrunwald.de>.
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
using System.Diagnostics;

namespace Mono.TextEditor.Utils
{
	sealed class CompressingTreeList<T> : IList<T>
	{
		readonly Func<T, T, bool> comparisonFunc;

		internal class CompressingNode : IRedBlackTreeNode
		{
			internal readonly T value;
			internal int count, totalCount;

			public CompressingNode (T value, int count)
			{
				this.value = value;
				this.count = count;
				this.totalCount = count;
			}

			#region IRedBlackTreeNode implementation

			public void UpdateAugmentedData ()
			{
				int newTotalCount = count;
				if (Left != null)
					newTotalCount += Left.totalCount;
				if (Right != null)
					newTotalCount += Right.totalCount;

				if (totalCount != newTotalCount) {
					totalCount = newTotalCount;
					if (Parent != null)
						Parent.UpdateAugmentedData ();
				}
			}

			public CompressingNode Parent {
				get;
				set;
			}

			IRedBlackTreeNode IRedBlackTreeNode.Parent {
				get {
					return Parent;
				}
				set {
					Parent = (CompressingNode)value;
				}
			}

			public CompressingNode Left {
				get;
				set;
			}

			IRedBlackTreeNode IRedBlackTreeNode.Left {
				get {
					return Left;
				}
				set {
					Left = (CompressingNode)value;
				}
			}

			public CompressingNode Right {
				get;
				set;
			}

			IRedBlackTreeNode IRedBlackTreeNode.Right {
				get {
					return Right;
				}
				set {
					Right = (CompressingNode)value;
				}
			}

			RedBlackColor IRedBlackTreeNode.Color {
				get;
				set;
			}

			#endregion

		}

		internal RedBlackTree<CompressingNode> tree = new RedBlackTree<CompressingNode> ();

		/// <summary>
		/// Creates a new CompressingTreeList instance.
		/// </summary>
		/// <param name="equalityComparer">The equality comparer used for comparing consequtive values.
		/// A single node may be used to store the multiple values that are considered equal.</param>
		public CompressingTreeList (IEqualityComparer<T> equalityComparer)
		{
			if (equalityComparer == null)
				throw new ArgumentNullException ("equalityComparer");
			this.comparisonFunc = equalityComparer.Equals;
		}

		/// <summary>
		/// Creates a new CompressingTreeList instance.
		/// </summary>
		/// <param name="comparisonFunc">A function that checks two values for equality. If this
		/// function returns true, a single node may be used to store the two values.</param>
		public CompressingTreeList (Func<T, T, bool> comparisonFunc)
		{
			if (comparisonFunc == null)
				throw new ArgumentNullException ("comparisonFunc");
			this.comparisonFunc = comparisonFunc;
		}

		/// <summary>
		/// Inserts <paramref name="item"/> <paramref name="count"/> times at position
		/// <paramref name="index"/>.
		/// </summary>
		public void InsertRange (int index, int count, T item)
		{
			if (index < 0 || index > Count)
				throw new ArgumentOutOfRangeException ("index", index, "Value must be between 0 and " + Count);
			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", count, "Value must not be negative");
			if (count == 0)
				return;
			unchecked {
				if (Count + count < 0)
					throw new OverflowException ("Cannot insert elements: total number of elements must not exceed int.MaxValue.");
			}
			if (tree.Root == null) {
				tree.Add (new CompressingNode (item, count));
			} else {
				var n = GetNode (ref index);
				// check if we can put the value into the node n:
				if (comparisonFunc (n.value, item)) {
					n.count += count;
					n.UpdateAugmentedData ();
				} else if (index == n.count) {
					// this can only happen when appending at the end
					tree.InsertRight (n, new CompressingNode (item, count));
				} else if (index == 0) {
					// insert before:
					// maybe we can put the value in the previous node?

					var p = n.GetPrevNode ();
					if (p != null && comparisonFunc (p.value, item)) {
						p.count += count;
						p.UpdateAugmentedData ();
					} else {
						tree.InsertBefore (n, new CompressingNode (item, count));
					}
				} else {
					Debug.Assert (index > 0 && index < n.count);
					// insert in the middle:
					// split n into a new node and n
					n.count -= index;
					tree.InsertBefore (n, new CompressingNode (n.value, index));
					// then insert the new item in between
					tree.InsertBefore (n, new CompressingNode (item, count));
					n.UpdateAugmentedData ();
				}
			}
		}

		/// <summary>
		/// Removes <paramref name="count"/> items starting at position
		/// <paramref name="index"/>.
		/// </summary>
		public void RemoveRange (int index, int count)
		{
			if (index < 0 || index > Count)
				throw new ArgumentOutOfRangeException ("index", index, "Value must be between 0 and " + Count);
			if (count < 0 || index + count > Count)
				throw new ArgumentOutOfRangeException ("count", count, "0 <= length, index(" + index + ")+count <= " + Count);
			if (count == 0)
				return;

			var n = GetNode (ref index);
			if (index + count < n.count) {
				// just remove inside a single node
				n.count -= count;
				n.UpdateAugmentedData ();
			} else {
				// keep only the part of n from 0 to index
				CompressingNode firstNodeBeforeDeletedRange;
				if (index > 0) {
					count -= (n.count - index);
					n.count = index;
					n.UpdateAugmentedData ();
					firstNodeBeforeDeletedRange = n;
					n = n.GetNextNode ();
				} else {
					Debug.Assert (index == 0);
					firstNodeBeforeDeletedRange = n.GetPrevNode ();
				}
				while (n != null && count >= n.count) {
					count -= n.count;
					var s = n.GetNextNode ();
					tree.Remove (n);
					n = s;
				}
				if (count > 0) {
					Debug.Assert (n != null && count < n.count);
					n.count -= count;
					n.UpdateAugmentedData ();
				}
				if (n != null) {
					Debug.Assert (n.GetPrevNode () == firstNodeBeforeDeletedRange);
					if (firstNodeBeforeDeletedRange != null && comparisonFunc (firstNodeBeforeDeletedRange.value, n.value)) {
						firstNodeBeforeDeletedRange.count += n.count;
						tree.Remove (n);
						firstNodeBeforeDeletedRange.UpdateAugmentedData ();
					}
				}		
			}
		}

		#region SetRange

		/// <summary>
		/// Sets <paramref name="count"/> indices starting at <paramref name="index"/> to
		/// <paramref name="item"/>
		/// </summary>
		public void SetRange (int index, int count, T item)
		{
			RemoveRange (index, count);
			InsertRange (index, count, item);
		}

		#endregion

		CompressingNode GetNode (ref int index)
		{
			var node = tree.Root;
			while (true) {
				if (node.Left != null && index < node.Left.totalCount) {
					node = node.Left;
				} else {
					if (node.Left != null) {
						index -= node.Left.totalCount;
					}
					if (index < node.count || node.Right == null)
						return node;
					index -= node.count;
					node = node.Right;
				}
			}
		}

		#region IList implementation

		public int IndexOf (T item)
		{
			int index = 0;
			if (tree.Root != null) {
				var n = tree.Root.GetOuterLeft ();
				while (n != null) {
					if (comparisonFunc (n.value, item))
						return index;
					index += n.count;
					n = n.GetNextNode ();
				}
			}
			return -1;
		}

		public void Insert (int index, T item)
		{
			InsertRange (index, 1, item);
		}

		public void RemoveAt (int index)
		{
			RemoveRange (index, 1);
		}

		public T this [int index] {
			get {
				if (index < 0 || index >= Count)
					throw new ArgumentOutOfRangeException ("index", index, "Value must be between 0 and " + (Count - 1));
				return GetNode (ref index).value;
			}
			set {
				if (index < Count)
					RemoveAt (index);
				Insert (index, value);
			}
		}

		#endregion

		#region ICollection implementation

		public void Add (T item)
		{
			InsertRange (Count, 1, item);
		}

		public void Clear ()
		{
			tree.Clear ();
		}

		public bool Contains (T item)
		{
			return IndexOf (item) >= 0;
		}

		public void CopyTo (T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (array.Length < Count)
				throw new ArgumentException ("The array is too small", "array");
			if (arrayIndex < 0 || arrayIndex + Count > array.Length)
				throw new ArgumentOutOfRangeException ("arrayIndex", arrayIndex, "Value must be between 0 and " + (array.Length - Count));
			foreach (T v in this) {
				array [arrayIndex++] = v;
			}
		}

		public bool Remove (T item)
		{
			int index = IndexOf (item);
			if (index >= 0) {
				RemoveAt (index);
				return true;
			}
			return false;
		}

		public int Count {
			get {
				return tree.Root != null ? tree.Root.totalCount : 0;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		#endregion

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator ()
		{
			foreach (var n in tree) {
				for (int i = 0; i < n.count; i++) {
					yield return n.value;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion
	}
}

