// Set.cs
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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Core.Collections
{
	public class Set<T>: ICollection<T>
	{
		Dictionary<T,Set<T>> dict = new Dictionary<T,Set<T>> ();

		public Dictionary<T, Set<T>>.KeyCollection.Enumerator GetEnumerator ()
		{
			return dict.Keys.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable)dict.Keys).GetEnumerator ();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return dict.Keys.GetEnumerator ();
		}

		public bool Add (T item)
		{
			if (!dict.ContainsKey (item)) {
				dict [item] = this;
				return true;
			} else
				return false;
		}

		void ICollection<T>.Add (T item)
		{
			Add (item);
		}
		
		public void Clear ()
		{
			dict.Clear ();
		}
		
		public bool Contains (T item)
		{
			return dict.ContainsKey (item);
		}
		
		public void CopyTo (T[] array, int arrayIndex)
		{
			dict.Keys.CopyTo (array, arrayIndex);
		}
		
		public bool Remove (T item)
		{
			return dict.Remove (item);
		}

		public int Count {
			get {
				return dict.Count;
			}
		}
		
		public bool IsReadOnly {
			get {
				return false;
			}
		}
	}
}
