//
// EnvironmentVariableCollection.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Serialization;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace MonoDevelop.Projects
{
	public class EnvironmentVariableCollection: ICustomDataItem, IDictionary<string,string>
	{
		List<KeyValuePair<string, string>> dict;

		public EnvironmentVariableCollection ()
		{
			dict = new List<KeyValuePair<string, string>> ();
		}

		public EnvironmentVariableCollection (IDictionary<string,string> dictionary)
		{
			dict = dictionary.ToList ();
		}

		public void CopyFrom (IDictionary<string, string> dictionary)
		{
			dict = dictionary.ToList ();
		}

		void ICustomDataItem.Deserialize (ITypeSerializer handler, DataCollection data)
		{
			foreach (var v in data.OfType<DataItem> ()) {
				var name = v.ItemData ["name"] as DataValue;
				var value = v.ItemData ["value"] as DataValue;
				if (name != null && value != null)
					this [name.Value] = value.Value;
			}
		}

		DataCollection ICustomDataItem.Serialize (ITypeSerializer handler)
		{
			// Add known keys first, then new keys
			var col = new DataCollection ();
			foreach (var ev in dict) {
				var vi = new DataItem ();
				vi.Name = "Variable";
				vi.ItemData.Add (new DataValue ("name", ev.Key) { StoreAsAttribute = true });
				vi.ItemData.Add (new DataValue ("value", ev.Value) { StoreAsAttribute = true });
				col.Add (vi);
			}
			return col;
		}

		public string this [string key] {
			get {
				for (int n = 0; n < dict.Count; n++)
					if (dict [n].Key == key)
						return dict [n].Value;
				throw new KeyNotFoundException ();
			}
			set {
				for (int n = 0; n < dict.Count; n++) {
					if (dict [n].Key == key) {
						dict [n] = new KeyValuePair<string, string> (key, value);
						return;
					}
				}
				dict.Add (new KeyValuePair<string, string> (key, value));
			}
		}

		public int Count {
			get {
				return dict.Count;
			}
		}

		bool ICollection<KeyValuePair<string, string>>.IsReadOnly {
			get {
				return false;
			}
		}

		public ICollection<string> Keys {
			get {
				return dict.Select (ev => ev.Key).ToList ();
			}
		}

		public ICollection<string> Values {
			get {
				return dict.Select (ev => ev.Value).ToList ();
			}
		}

		void ICollection<KeyValuePair<string, string>>.Add (KeyValuePair<string, string> item)
		{
			dict.Add (item);
		}

		public void Add (string key, string value)
		{
			dict.Add (new KeyValuePair<string, string> (key, value));
		}

		public void Clear ()
		{
			dict.Clear ();
		}

		bool ICollection<KeyValuePair<string, string>>.Contains (KeyValuePair<string, string> item)
		{
			return dict.Any (ev => ev.Key == item.Key && ev.Value == item.Value);
		}

		public bool ContainsKey (string key)
		{
			return FindKey (key) != -1;
		}

		void ICollection<KeyValuePair<string, string>>.CopyTo (KeyValuePair<string, string> [] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, string>>)dict).CopyTo (array, arrayIndex);
		}

		public List<KeyValuePair<string, string>>.Enumerator GetEnumerator ()
		{
			return dict.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable)dict).GetEnumerator ();
		}

		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator ()
		{
			return ((IEnumerable<KeyValuePair<string, string>>)dict).GetEnumerator ();
		}

		bool ICollection<KeyValuePair<string, string>>.Remove (KeyValuePair<string, string> item)
		{
			return dict.RemoveAll (ev => ev.Key == item.Key && ev.Value == item.Value) > 0;
		}

		public bool Remove (string key)
		{
			var i = FindKey (key);
			if (i != -1) {
				dict.RemoveAt (i);
				return true;
			}
			return false;
		}

		bool IDictionary<string, string>.TryGetValue (string key, out string value)
		{
			var i = FindKey (key);
			if (i != -1) {
				value = dict [i].Value;
				return true;
			}
			value = null;
			return false;
		}

		int FindKey (string key)
		{
			for (int n = 0; n < dict.Count; n++)
				if (dict [n].Key == key)
					return n;
			return -1;
		}
	}
}

