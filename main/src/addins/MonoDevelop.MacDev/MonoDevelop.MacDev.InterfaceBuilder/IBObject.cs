// 
// XibObject.cs
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
using System.Xml.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace MonoDevelop.MacDev.InterfaceBuilder
{
	public abstract class IBObject
	{
		public bool EncodedWithXMLCoder { get; set; }
		
		public int? Id { get; private set; }
		
		protected void DeserializeContents (IEnumerable<XElement> children,
			Dictionary<string, Func<IBObject>> constructors, IReferenceResolver resolver)
		{
			foreach (XElement child in children) {
				XAttribute keyAtt = child.Attribute ("key");
				string keyStr = keyAtt == null? null : keyAtt.Value;
				if (child.Name == "bool" && keyStr == "EncodedWithXMLCoder") {
					EncodedWithXMLCoder = child.Value == "YES";
				} else {
					object val = Deserialize (child, constructors, resolver);
					try {
						OnPropertyDeserialized (keyStr, val);
					} catch (Exception ex) {
						MonoDevelop.Core.LoggingService.LogWarning (
							"IB Parser: Error assigning {0}={1} to {2} in id {3}:\n{4}",
							keyStr, val, GetType (), Id, ex);
					}
				}
			}
		}
		
		protected virtual void OnPropertyDeserialized (string name, object value)
		{
			throw new InvalidOperationException (String.Format ("Unexpected property '{0}' of type '{1}' in type '{2}'",
			                                                    name, value.GetType (), GetType ()));
		}
		
		public static object Deserialize (XElement element, Dictionary<string, Func<IBObject>> constructors, IReferenceResolver resolver)
		{
			var idAtt = element.Attribute ("id");
			object val = DeserializeInner (element, constructors, resolver);
			var ib = val as IBObject;
			
			if (idAtt != null) {
				int id = Int32.Parse (idAtt.Value, CultureInfo.InvariantCulture);
				if (ib != null) {
					ib.Id = id;
					resolver.Add (ib);
				} else {
					resolver.Add (id, val);
				}
			}
			
			if (ib != null)
				ib.DeserializeContents (element.Elements (), constructors, resolver);
			
			return val;
		}
		
		static object DeserializeInner (XElement element, Dictionary<string, Func<IBObject>> constructors, IReferenceResolver resolver)
		{
			switch (element.Name.ToString ()) {
			case "int":
				return Int32.Parse (element.Value, CultureInfo.InvariantCulture);
			case "integer":
				return Int32.Parse (element.Attribute ("value").Value, CultureInfo.InvariantCulture);
			case "nil":
				return null;
			case "string":
				XAttribute typeAtt = element.Attribute ("type");
				if (typeAtt != null) {
					switch (typeAtt.Value) {
					case "base64-UTF8":
						//FIXME: figure out the encoding they're using. why do we have to remove the last char to make it decode?
						string s = element.Value.Replace ("\n", "").Replace ("\r", "");
						int last = (s.Length / 4 ) * 4;
						return Encoding.UTF8.GetString (Convert.FromBase64String (s.Substring (0, last)));
					default:
						throw new Exception (String.Format ("Unknown string encoding type {0}", typeAtt.Value));
					}
				}
				return element.Value;
			case "characters":
				return element.Value;
			case "bool":
				return element.Value == "YES";
			case "boolean":
				return element.Attribute ("value").Value == "YES";
			case "double":
				return Double.Parse (element.Value, CultureInfo.InvariantCulture);
			case "float":
				return float.Parse (element.Value, CultureInfo.InvariantCulture);
			case "real":
				return float.Parse (element.Attribute ("value").Value, CultureInfo.InvariantCulture);
			case "bytes":
				//FIXME: figure out the encoding they're using. it's not straight base 64
				return new AppleEvilByteArrayEncoding (element.Value);
			case "reference":
				var refAtt = element.Attribute ("ref");
				IBReference xibRef;
				if (refAtt != null) {
					xibRef = new IBReference (Int32.Parse (refAtt.Value, CultureInfo.InvariantCulture));
					resolver.Add (xibRef);
				} else {
					//FIXME: handle null references more robustly
					xibRef = new IBReference (Int32.MinValue);
				}
				return xibRef;
			case "object": {
				var className = (string) element.Attribute ("class");
				Func<IBObject> constructor;
				IBObject obj;
				if (constructors.TryGetValue (className, out constructor))
					obj = constructor ();
				else
					obj = new UnknownIBObject (className);
				return obj;
			}
			case "array": {
				var className = (string) element.Attribute ("class");
				if (className == null)
					return new NSArray ();
				else if (className == "NSMutableArray")
					return new NSMutableArray ();
				throw new InvalidOperationException ("Unknown array class '" + className + "'");
			}
			case "dictionary": {
				var className = (string) element.Attribute ("class");
				if (className == "NSMutableDictionary")
					return new NSMutableDictionaryDirect ();
				throw new InvalidOperationException ("Unknown dictionary class '" + className + "'");
			}
			default:
				throw new Exception (String.Format ("Cannot handle primitive type {0}", element.Name));
			}
		}
	}
	
	struct AppleEvilByteArrayEncoding
	{
		string text;
		public AppleEvilByteArrayEncoding (string text)
		{
			this.text = text;
		}
		
		public string Text {
			get { return text; }
		}
	}
	
	public class IBProxyObject : IBObject
	{
		public string IBProxiedObjectIdentifier { get; set; }
		public string TargetRuntimeIdentifier { get; set; }
		
		protected override void OnPropertyDeserialized (string name, object value)
		{
			if (name == "IBProxiedObjectIdentifier")
				IBProxiedObjectIdentifier = (string) value;
			else if (name == "targetRuntimeIdentifier")
				TargetRuntimeIdentifier = (string) value;
			else
				base.OnPropertyDeserialized (name, value);
		}
	}
}
