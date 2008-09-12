// ReadOnlyDictionary.cs
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
	public class ReadOnlyDictionary<TKey,TValue>: IDictionary<TKey,TValue>, IEnumerable
	{
		IDictionary<TKey,TValue> dictionary;
		
		public ReadOnlyDictionary (IDictionary<TKey,TValue> dictionary)
		{
			this.dictionary = dictionary;
		}

		public TValue this [TKey key] {
			get {
				return dictionary [key];
			}
		}

		public ICollection<TValue> Values {
			get { return dictionary.Values; }
		}

		public bool TryGetValue (TKey key, out TValue value)
		{
			return dictionary.TryGetValue (key, out value);
		}

		public bool Contains (KeyValuePair<TKey, TValue> item)
		{
			return dictionary.Contains (item);
		}

		public bool ContainsKey (TKey key)
		{
			return dictionary.ContainsKey (key);
		}

		public void CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			dictionary.CopyTo (array, arrayIndex);
		}

		public int Count {
			get { return dictionary.Count; }
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			return dictionary.GetEnumerator ();
		}

		public ICollection<TKey> Keys {
			get { return dictionary.Keys; }
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly {
			get { return true; }
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add (KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException ("The dictionary is read-only.");
		}

		void IDictionary<TKey,TValue>.Add (TKey key, TValue value)
		{
			throw new NotSupportedException ("The dictionary is read-only.");
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Clear ()
		{
			throw new NotSupportedException ("The dictionary is read-only.");
		}
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove (KeyValuePair<TKey, TValue> item)
		{
			throw new NotSupportedException ("The dictionary is read-only.");
		}

		bool IDictionary<TKey,TValue>.Remove (TKey key)
		{
			throw new NotSupportedException ("The dictionary is read-only.");
		}

		TValue IDictionary<TKey,TValue>.this [TKey key] {
			get {
				return dictionary [key];
			}
			set {
				throw new NotSupportedException ("The dictionary is read-only.");
			}
		}

		// IEnumerable

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable)dictionary).GetEnumerator ();
		}
	}
}
