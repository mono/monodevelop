// 
// PListScheme.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using System.Xml;
using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.PlistEditor
{
	public class PListScheme
	{
		List<Key> keys = new List<Key> ();
		
		
		public IEnumerable<Key> Keys {
			get {
				return keys;
			}
		}
		
		public class Value {
			public string Identifier { get; set; }
			public string Description { get; set; }
		}
		
		public class Key {
			public string Identifier { get; set; }
			public string Description { get; set; }
			public string Type { get; set; }
			public string ArrayType { get; set; }
			
			public readonly List<Value> Values = new List<Value> ();
		}
		
		public Key GetKey (string id)
		{
			return keys.FirstOrDefault (k => k.Identifier == id);
		}
		
		public static PListScheme Load (XmlReader reader)
		{
			var result = new PListScheme ();
			
			XmlReadHelper.ReadList (reader, "PListScheme", delegate () {
				switch (reader.LocalName) {
				case "Key":
					var key = new Key () {
						Identifier = reader.GetAttribute ("name"),
						Description = reader.GetAttribute ("_description"),
						Type = reader.GetAttribute ("type"),
						ArrayType = reader.GetAttribute ("name")
					};
					XmlReadHelper.ReadList (reader, "Key", delegate () {
						if (reader.LocalName == "Value") {
							key.Values.Add (new Value () {
								Identifier = reader.GetAttribute ("name"),
								Description = reader.GetAttribute ("_description")
							});
							return true;
						}
						return false;
					});
					result.keys.Add (key);
					return true;
				}
				return false;
			});
			return result;
		}
		
		public static readonly PListScheme Empty = new PListScheme ();
	}
}

