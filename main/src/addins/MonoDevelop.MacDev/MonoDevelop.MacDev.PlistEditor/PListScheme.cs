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
	public partial class PListScheme
	{
		public abstract class SchemaItem
		{
			public string ArrayType { get; set; }
			public string Description { get; set; }
			public string Identifier { get; set; }
			public string Type { get; set; }
			public List<Value> Values { get; set; }
			
			public SchemaItem ()
			{
				Values = new List<Value> ();
			}
			
			public PObject Create ()
			{
				if (Type == PDictionary.Type) {
					var dictionary = new PDictionary ();
					foreach (var v in Values) {
						if (v.Required)
							dictionary.Add (v.Identifier, v.Create ());
					}
					return dictionary;
				} else if (Type == PArray.Type) {
					var array = new PArray ();
					foreach (var v in Values) {
						if (v.Required)
							array.Add (v.Create ());
					}
					return array;
				} else if (Values.Any ()){
					return Values.First ().Create ();
				} else {
					var obj = PObject.Create (Type);
					if (!string.IsNullOrEmpty (Identifier) && !(this is Key))
						obj.SetValue (Identifier);
					return obj;
				}
			}
		}
		
		public class Value : SchemaItem {

			public bool Required { get; set; }
			
			public Value ()
			{
				
			}
		}

		public class Key : SchemaItem {
			public static readonly Key Empty = new Key { };
		}
	}
	
	public partial class PListScheme
	{
		public static readonly PListScheme Empty = new PListScheme () { keys = new Key [0] };
		
		IList<Key> keys = new List<Key> ();

		public IList<Key> Keys {
			get {
				return keys;
			}
		}

		public Key GetKey (string id)
		{
			return keys.FirstOrDefault (k => k.Identifier == id);
		}

		public static PListScheme Load (XmlReader reader)
		{
			var result = new PListScheme ();
			var doc = new XmlDocument ();
			doc.Load (reader);
			
			foreach (XmlNode keyNode in doc.SelectNodes ("/PListScheme/*")) {
				var key = new Key {
					Identifier = AttributeToString (keyNode.Attributes ["name"]),
					Description = AttributeToString (keyNode.Attributes ["_description"]),
					Type = AttributeToString (keyNode.Attributes ["type"]),
					ArrayType = AttributeToString (keyNode.Attributes ["arrayType"])
				};
				
				if (keyNode.HasChildNodes) {
					key.Values.AddRange (ParseValues (key.ArrayType ?? key.Type, keyNode.ChildNodes));
				} else if (key.Type == PBoolean.Type) {
					key.Values.Add (new Value { Identifier = "Yes", Description = "Yes" });
					key.Values.Add (new Value { Identifier = "No", Description = "No" });
				}
				result.Keys.Add (key);
			}
			
			return result;
		}
		
		public static Dictionary<PObject, SchemaItem> Match (PDictionary dictionary, PListScheme scheme)
		{
			Dictionary<PObject, SchemaItem> results = new Dictionary<PObject, SchemaItem> ();
			foreach (var kp in dictionary) {
				var key = scheme.GetKey (kp.Key);
				if (key == null) {
					results.Add (kp.Value, key);
					continue;
				}
				
				// Every array element is produced by instantiating a copy of this value
				if (key.Type == PArray.Type && key.Values.Count == 1 && key.Values [0].Identifier == null) {
					results.Add (kp.Value, key);
					foreach (var v in ((PArray) kp.Value)) {
						Match (v, key.Values [0], results);
					}
				} else if (key.Type == PArray.Type) {
					Match (kp.Value, key, results);
				} else if (key.Type == PDictionary.Type) {
					Match (kp.Value, key, results);
				} else {
					results.Add (kp.Value, key);
				}
			}
			return results;
		}
		
		static void Match (PObject o, SchemaItem value, Dictionary<PObject, SchemaItem> results)
		{
			results.Add (o, value);
			if (o is PDictionary) {
				foreach (var kp in ((PDictionary) o)) {
					var subValue = value.Values.Where (k => k.Identifier == kp.Key).FirstOrDefault () ?? value;
					Match (kp.Value, subValue, results);
				}
			} else if (o is PArray) {
				foreach (var v in ((PArray) o)) {
					Match (v, value, results);
				}
			}
		}
		
		
		static IEnumerable<Value> ParseValues (string type, XmlNodeList nodeList)
		{
			List<Value> values = new List<Value> ();
			foreach (XmlNode node in nodeList) {
				if (node.Name != "Value")
					throw new NotSupportedException (string.Format ("Node of type {0} not supported as a Value", node.Name));
				
				Value v = new Value {
					ArrayType = AttributeToString (node.Attributes ["arrayType"]),
					Description = AttributeToString (node.Attributes ["_description"]),
					Identifier = AttributeToString (node.Attributes ["name"]),
					Type = AttributeToString (node.Attributes ["type"]) ?? type
				};
				
				if (node.Attributes ["required"] != null)
					v.Required = bool.Parse (node.Attributes ["required"].Value);
				
				if (node.HasChildNodes)
					v.Values.AddRange (ParseValues (v.ArrayType ?? v.Type, node.ChildNodes));
				
				values.Add (v);
			}
			return values;
		}
		
		static string AttributeToString (XmlAttribute attr)
		{
			if (attr == null || string.IsNullOrEmpty (attr.Value))
				return null;
			return attr.Value;
		}
	}
}

