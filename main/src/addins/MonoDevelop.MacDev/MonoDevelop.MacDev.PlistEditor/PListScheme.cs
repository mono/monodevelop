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
					
					// If nothing was required, create an initial one anyway
					if (dictionary.Count == 0) {
						var first = Values.FirstOrDefault ();
						if (first == null) {
							dictionary.Add ("newNode", PObject.Create (PString.Type));
						} else {
							dictionary.Add (first.Identifier ?? "newNode", first.Create ());
						}
					}
					return dictionary;
				} else if (Type == PArray.Type) {
					var array = new PArray ();
					foreach (var v in Values) {
						if (v.Required)
							array.Add (v.Create ());
					}
					
					// If nothing was required, create an initial one anyway
					if (array.Count == 0) {
						var first = Values.FirstOrDefault ();
						if (first == null) {
							array.Add (PObject.Create (ArrayType));
						} else {
							array.Add (first.Create ());
						}
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
		static readonly Value BooleanYes = new Value { Identifier = "Yes", Description = "Yes", Type = "Boolean" };
		static readonly Value BooleanNo = new Value { Identifier = "No", Description = "No", Type = "Boolean" };
		
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
		
		public static List<SchemaItem> AvailableValues (PObject obj, SchemaItem key, Dictionary<PObject, SchemaItem> tree)
		{
			if (obj is PBoolean)
				return new List<SchemaItem> { BooleanYes, BooleanNo };
			
			if (key == null)
				return null;
			
			var values = key.Values.Cast<PListScheme.SchemaItem> ().ToList ();

			// In this case every element in the array/dictionary is produced from this single Value
			if ((obj is PDictionary || obj is PArray) && values.Count == 1 && values [0].Identifier == null)
				return values;

			// Strip out values which are already used. We can do this trivially as
			// we have already matched every PObject to the SchemaItem which created it
			foreach (var child in PObject.ToEnumerable  (obj))
				values.Remove (tree [child.Value]);

			return values;
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
				
				CreateChildValues (key, keyNode);
				result.Keys.Add (key);
			}
			
			return result;
		}
		
		static void CreateChildValues (SchemaItem key, XmlNode node)
		{
			if (node.HasChildNodes)
				key.Values.AddRange (ParseValues (key.ArrayType ?? key.Type, node.ChildNodes));
			else if (key.Type == "Dictionary")
				key.Values.Add (new Value { Type = "String", Description = "New value" });
			else if (key.Type == "Array")
				key.Values.Add (new Value { Type = key.ArrayType, Description = "New value" });
		}
		
		public static Dictionary<PObject, SchemaItem> Match (PDictionary dictionary, PListScheme scheme)
		{
			Dictionary<PObject, SchemaItem> results = new Dictionary<PObject, SchemaItem> ();
			foreach (var kp in dictionary) {
				var key = scheme.GetKey (kp.Key);
				if (key == null) {
					Match (kp.Value, key, results);
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
			
			foreach (var kp in PObject.ToEnumerable (o)) {
				var subValue = value != null ? value.Values.Where (k => k.Identifier == kp.Key).FirstOrDefault () : null;
				if (subValue == null && value != null && value.Values.Count == 1 && value.Values [0].Identifier == null)
					subValue = value.Values [0];
				Match (kp.Value, subValue, results);
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
				
				CreateChildValues (v, node);
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

