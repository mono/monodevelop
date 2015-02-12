// ItemCollection.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public class ItemCollection<T>: IEnumerable<T>
	{
		ImmutableList<T> list = ImmutableList<T>.Empty;
		bool controlWrites;
		T[] singleItem = new T[1];

		protected ImmutableList<T> List {
			get {
				return this.list;
			}
			set {
				list = value;
			}
		}

		internal void SetShared ()
		{
			controlWrites = true;
		}

		protected void AssertCanWrite ()
		{
			if (controlWrites)
				Runtime.AssertMainThread ();
		}

		public void Add (T item)
		{
			list = list.Add (item);
			singleItem [0] = item;
			OnItemsAdded (singleItem);
		}

		public void AddRange (IEnumerable<T> items)
		{
			AssertCanWrite ();
			list = list.AddRange (items);
			OnItemsAdded (items);
		}

		public void Insert (int index, T item)
		{
			list = list.Insert (index, item);
			singleItem [0] = item;
			OnItemsAdded (singleItem);
		}

		public void RemoveRange (IEnumerable<T> items)
		{
			AssertCanWrite ();
			list = list.RemoveRange (items);
			OnItemsRemoved (items);
		}

		public bool Remove (T item)
		{
			AssertCanWrite ();

			int i = list.IndexOf (item);
			if (i != -1) {
				RemoveAt (i);
				return true;
			}
			return false;
		}

		public void RemoveAt (int index)
		{
			AssertCanWrite ();
			T it = list [index];
			list = list.RemoveAt (index);
			singleItem [0] = it;
			OnItemsRemoved (singleItem);
		}

		public int IndexOf (T item)
		{
			return list.IndexOf (item);
		}

		public bool Contains (T item)
		{
			return list.Contains (item);
		}

		public T this [int index] {
			get {
				return list [index];
			}
			set {
				AssertCanWrite ();
				T it = list [index];
				list = list.SetItem (index, value);
				singleItem [0] = it;
				OnItemsRemoved (singleItem);
				singleItem [0] = value;
				OnItemsAdded (singleItem);
			}
		}

		public void Clear ()
		{
			AssertCanWrite ();
			var oldList = list;
			list = list.Clear ();
			OnItemsRemoved (oldList);
		}

		public int Count {
			get { return list.Count; }
		}

		protected virtual void OnItemsAdded (IEnumerable<T> items)
		{
		}
		
		protected virtual void OnItemsRemoved (IEnumerable<T> items)
		{
		}

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return ((System.Collections.IEnumerable)list).GetEnumerator ();
		}

		#endregion
	}
}
