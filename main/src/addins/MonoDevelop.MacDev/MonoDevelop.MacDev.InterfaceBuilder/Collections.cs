
// 
// Collections.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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


namespace MonoDevelop.MacDev.InterfaceBuilder
{
	
	public class NSArray : IBObject
	{
		List<object> values;
		
		public List<object> Values {
			get {
				if (values == null)
					values = new List<object> ();
				return values;
			}
		}
		
		protected override void OnPropertyDeserialized (string name, object value, IReferenceResolver resolver)
		{
			if (name == null)
				Values.Add (value);
			else
				base.OnPropertyDeserialized (name, value, resolver);
		}
	}
	
	public class NSMutableArray : NSArray
	{
	}
	
	public class NSMutableDictionaryDirect : NSMutableDictionary
	{
		protected override void OnPropertyDeserialized (string name, object value, IReferenceResolver resolver)
		{
			Values[name] = value;
		}
	}
	
	public class NSMutableDictionary : IBObject
	{
		Unref<NSArray> sortedKeys, values;
		Dictionary<object, object> dict;
		List<object> nonCoderKeys;
		
		public Dictionary<object, object> Values {
			get {
				if (dict == null) {
					List<object> k = sortedKeys.Value.Values;
					List<object> v = values.Value.Values;
					dict = new Dictionary<object, object> (k.Count);
					for (int i = 0; i < k.Count; i++) {
						dict[k[i]] = v[i];
					}
				}
				return dict;
			}
		}
		
		protected override void OnPropertyDeserialized (string name, object value, IReferenceResolver resolver)
		{
			if (EncodedWithXMLCoder) {
				if (name == "dict.sortedKeys") {
					sortedKeys = new Unref<NSArray> (value);
				} else if (name == "dict.values") {
					values = new Unref<NSArray> (value);
				} else {
					base.OnPropertyDeserialized (name, value, resolver);
				}
			} else {
				if (nonCoderKeys == null) {
					nonCoderKeys = new List<object> ();
					dict = new Dictionary<object, object> ();
				}
				if (name.StartsWith ("NS.key.")) {
					int idx = Int32.Parse (name.Substring ("NS.key.".Length));
					while (nonCoderKeys.Count <= idx)
						nonCoderKeys.Add (null);
					nonCoderKeys[idx] = value;
				} else if (name.StartsWith ("NS.object.")) {
					int idx = Int32.Parse (name.Substring ("NS.object.".Length));
					dict[nonCoderKeys[idx]] = value;
				} else {
					base.OnPropertyDeserialized (name, value, resolver);
				}
			}
		}
	}
	
	public class IBMutableOrderedSet : IBObject
	{
		List<object> orderedObjects;
		
		public List<object> OrderedObjects {
			get {
				if (orderedObjects == null)
					orderedObjects = new List<object> ();
				return orderedObjects;
			}
		}
		
		protected override void OnPropertyDeserialized (string name, object value, IReferenceResolver resolver)
		{
			if (name == "orderedObjects" && value is NSArray)
				orderedObjects = ((NSArray)value).Values;
			else
				base.OnPropertyDeserialized (name, value, resolver);
		}
	}
}