//
// Taken from PodSleuth (http://git.gnome.org/cgit/podsleuth)
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2007-2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;

namespace PropertyList
{
	public class PlistDictionary : PlistObjectBase, IEnumerable<KeyValuePair<string, PlistObjectBase>>
	{
		private List<string> keys;
		private Dictionary<string, PlistObjectBase> dict = new Dictionary<string, PlistObjectBase> ();

		public PlistDictionary () : this(false)
		{
		}

		public PlistDictionary (bool keepOrder)
		{
			if (keepOrder) {
				keys = new List<string> ();
			}
		}

		public PlistDictionary (Dictionary<string, PlistObjectBase> value) : this(value, false)
		{
		}

		public PlistDictionary (Dictionary<string, PlistObjectBase> value, bool keepOrder) : this(keepOrder)
		{
			foreach (KeyValuePair<string, PlistObjectBase> item in value) {
				Add (item.Key, item.Value);
			}
		}

		public PlistDictionary (IDictionary value) : this(value, false)
		{
		}

		public PlistDictionary (IDictionary value, bool keepOrder) : this(keepOrder)
		{
			foreach (DictionaryEntry item in value) {
				Add ((string)item.Key, ObjectToPlistObject (item.Value));
			}
		}

		public override void Write (System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement ("dict");
			foreach (KeyValuePair<string, PlistObjectBase> item in this) {
				writer.WriteElementString ("key", item.Key);
				item.Value.Write (writer);
			}
			writer.WriteEndElement ();
		}

		public void Clear ()
		{
			if (keys != null) {
				keys.Clear ();
			}

			dict.Clear ();
		}

		public void Add (string key, PlistObjectBase value)
		{
			if (keys != null) {
				keys.Add (key);
			}

			if (dict.ContainsKey (key)) {
				Console.WriteLine ("Warning: ignoring duplicate key: {0} (null? {1} empty? {2})", key, key == null, key == "");
			} else {
				dict.Add (key, value);
			}
		}

		public bool Remove (string key)
		{
			if (keys != null) {
				keys.Remove (key);
			}

			return dict.Remove (key);
		}

		public bool ContainsKey (string key)
		{
			return dict.ContainsKey (key);
		}

		public PlistObjectBase this[string key] {
			get { return dict[key]; }
			set {
				if (keys != null) {
					if (!dict.ContainsKey (key))
						keys.Add (key);
				}
				dict[key] = value;
			}
		}
		
		public PlistObjectBase TryGetValue (string key)
		{
			PlistObjectBase value;
			if (dict.TryGetValue (key, out value))
				return value;
			return null;
		}

		public int Count {
			get { return dict.Count; }
		}

		private IEnumerator<KeyValuePair<string, PlistObjectBase>> GetEnumeratorFromKeys ()
		{
			foreach (string key in keys) {
				yield return new KeyValuePair<string, PlistObjectBase> (key, dict[key]);
			}
		}

		public IEnumerator<KeyValuePair<string, PlistObjectBase>> GetEnumerator ()
		{
			return keys == null ? dict.GetEnumerator () : GetEnumeratorFromKeys ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}
