//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class SortedCollectionBase<T> : CollectionBase, IEnumerable<T>, IPropertyComparer<T, string> where T : ISchema
	{
		public SortedCollectionItemEventHandler<T> ItemAdded;
		public SortedCollectionItemEventHandler<T> ItemRemoved;
		
		protected bool sort = false;
		
		protected SortedCollectionBase (bool sort)
		{
			this.sort = sort;
		}
		
		protected SortedCollectionBase (SortedCollectionBase<T> collection, bool sort)
			: this (sort)
		{
			if (collection == null)
				return;
			
			foreach (T item in collection)
				Add ((T)item.Clone ());
		}
		
		public T this [int index] {
			get { return (T)List[index]; }
			set { List[index] = value; }
		}
		
		public bool IsSorted {
			get { return sort; }
			set {
				if (sort != value) {
					sort = value;
					if (value)
						Sort ();
				}
			}
		}

		public int Add (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			int index = -1;
			if (sort)
				index = SortedInsert (item);
			else
				index = List.Add (item);
			
			if (ItemAdded != null)
				ItemAdded (this, new SortedCollectionItemEventArgs<T> (item));
			
			return index;
		}

		public int IndexOf (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			return List.IndexOf (item);
		}

		public void Insert (int index, T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			if (sort)
				throw new InvalidOperationException ("Insert can only be used in an unsorted collection.");
			
			List.Insert (index, item);
			
			if (ItemAdded != null)
				ItemAdded (this, new SortedCollectionItemEventArgs<T> (item));
		}

		public void Remove (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			List.Remove (item);
			
			if (ItemRemoved != null)
				ItemRemoved (this, new SortedCollectionItemEventArgs<T> (item));
		}

		public bool Contains (T item)
		{
			if (item == null)
				throw new ArgumentNullException ("item");
			
			return List.Contains (item);
		}
		
		public bool Contains (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			if (sort)
				return BinarySearchIndex<string> (this, name) >= 0;
			else
				return SearchIndex<string> (this, name) >= 0;
		}
		
		public T Search (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			int index = -1;
			if (sort)
				index = BinarySearchIndex<string> (this, name);
			else
				index = SearchIndex<string> (this, name);
			if (index >= 0)
				return this[index];
			return default (T);
		}
		
		public int SearchIndex (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			if (sort)
				return BinarySearchIndex<string> (this, name);
			else
				return SearchIndex<string> (this, name);
		}
		
		public virtual void Sort ()
		{
			if (List.Count <= 1) return;

			//quicksort
			Sort (0, List.Count - 1);
		}
		
		public virtual void Swap (T x, T y)
		{
			if (x == null)
				throw new ArgumentNullException ("x");
			if (y == null)
				throw new ArgumentNullException ("y");
			
			int indexX = IndexOf (x);
			int indexY = IndexOf (y);
			
			if (indexX < 0 || indexY < 0)
				throw new ArgumentException ("Both items must be present in the collection.");
			
			Swap (indexX, indexY);
		}
		
		public virtual void Swap (int left, int right)
		{
			object swap = List[left];
			List[left] = List[right];
			List[right] = swap;
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			foreach (T item in List)
				yield return item;
		}
		
		protected virtual int SortedInsert (T item)
		{
			if (List.Count == 0) {
				List.Add (item);
				return 0;
			} else {
				int position = 0;
				foreach (T t in List) {
					if (Compare (item, t) < 0)
						break;
					position++;
				}
				if (position == List.Count) {
					List.Add (item);
					return List.Count - 1;
				} else {
					List.Insert (position, item);
					return position;
				}
			}
		}

		protected virtual void Sort (int lower, int upper)
		{
			if (lower < upper) {
				int split = Pivot (lower, upper);
				Sort (lower, split - 1);
				Sort (split + 1, upper);
			}
		}

		protected virtual int Pivot (int lower, int upper)
		{
			int left = lower + 1;
			T pivot = this[lower];
			int right = upper;

			while (left <= right) {

				while ((left <= right) && (Compare (this[left], pivot) <= 0))
					++left;

				while ((left <= right) && (Compare (this[right], pivot) > 0))
					--right;

				if (left < right) {
					Swap (left, right);
					++left;
					--right;
				}
			}

			Swap (lower, right);
			return right;
		}

		protected virtual int BinarySearchIndex<U> (IPropertyComparer<T, U> comparer, U value)
		{
			int min = 0;
			int max = List.Count - 1;
			int cmp = 0;

			while (min <= max) {
				int mid = (min + max) / 2;
				cmp = comparer.Compare (this[mid], value);

				if (cmp == 0)
					return mid;
				else if (cmp > 0)
					max = mid - 1;
				else
					min = mid + 1; // compensate for the rounding down
			}

			return ~min;
		}
		
		protected virtual int SearchIndex<U> (IPropertyComparer<T, U> comparer, U value)
		{
			int max = List.Count - 1;
			for (int i=0; i<max; i++)
				if (comparer.Compare (this[i], value) == 0)
					return i;
			return -1;
		}
		
		protected virtual int Compare (T x, T y)
		{
			return 0;
		}
		
		int IPropertyComparer<T, string>.Compare (T x, string y)
		{
			ISchema schema = (ISchema)x;
			return schema.Name.CompareTo (y);
		}
	}
}