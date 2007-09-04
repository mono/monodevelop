//
// Properties.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using MonoDevelop.Core;

namespace MonoDevelop.Core
{
	public class Properties
	{
		Dictionary<string, object> properties    = new Dictionary<string, object> ();
		Dictionary<string, object> defaultValues = new Dictionary<string, object> ();
		
		public ICollection<string> Keys {
			get {
				return properties.Keys;
			}
		}

		public Properties ()
		{
		}
		
		T Convert<T> (object o)
		{
			if (o is T) {
				return (T)o;
			}
			if (o is LazyXmlDeserializer) {
				return (T)((LazyXmlDeserializer)o).Deserialize ();
			}
			TypeConverter converter = TypeDescriptor.GetConverter (typeof(T));
			if (o is string) {
				try {
					return (T)converter.ConvertFromInvariantString (o.ToString ());
				} catch (Exception) {
					return default(T);
				}
			}
			
			try {
				return (T)converter.ConvertFrom (o);
			} catch (Exception) {
				return default(T);
			}
		}
		
		string ConvertToString (object o)
		{
			if (o == null)
				return null;
			TypeConverter converter = TypeDescriptor.GetConverter (o.GetType ());
			return converter.ConvertToInvariantString (o);
		}
		
		public T Get<T> (string property, T defaultValue)
		{
			defaultValues[property] = defaultValue;
			if (properties.ContainsKey (property))
				return Convert<T> (properties[property]);
			properties[property] = defaultValue;
			return defaultValue;
		}
		
		public T Get<T> (string property)
		{
			if (properties.ContainsKey (property))
				return Convert<T> (properties[property]);
			if (defaultValues.ContainsKey (property))
				return Convert<T> (defaultValues[property]);
			return default(T);
		}
		
		public void Set (string key, object val)
		{
			object old = Get<object> (key);
			properties[key] = val;
			if (!val.GetType ().IsClass || (val is string)) {
				if (defaultValues.ContainsKey (key)) {
					if (defaultValues[key] == val)
						properties.Remove (key);
				}
			}
			OnPropertyChanged (new PropertyChangedEventArgs (key, old, val));
		}

#region I/O
		public const string Node           = "Properties";
		public const string PropertyNode   = "Property";
		public const string KeyAttribute   = "key";
		public const string ValueAttribute = "value";
		public const string XmlSerialized  = "XmlSerialized";
		public const string TypeAttribute  = "type";
		
		public const string PropertiesRootNode = "MonoDevelopProperties";
		public const string PropertiesVersionAttribute  = "version";
		public const string PropertiesVersion  = "2.0";
		
		public void Write (XmlWriter writer)
		{
				Write (writer, true);
		}
		
		public void Write (XmlWriter writer, bool createPropertyParent)
		{
			if (createPropertyParent)
					writer.WriteStartElement (Node);

			foreach (KeyValuePair<string, object> property in this.properties) {
				writer.WriteStartElement (PropertyNode);
				writer.WriteAttributeString (KeyAttribute, property.Key);
				
				if (property.Value is LazyXmlDeserializer) {
					writer.WriteStartElement (XmlSerialized);
					writer.WriteAttributeString (TypeAttribute, ((LazyXmlDeserializer)property.Value).Type);
					writer.WriteRaw (((LazyXmlDeserializer)property.Value).Xml);
					writer.WriteEndElement (); // XmlSerialized
				} else if (property.Value is Properties) {
					((Properties)property.Value).Write (writer);
				} else {
					if (property.Value.GetType () != typeof(string) && property.Value.GetType ().IsClass) {
						writer.WriteStartElement (XmlSerialized);
						writer.WriteAttributeString (TypeAttribute, property.Value.GetType ().FullName);
						XmlSerializer serializer = new XmlSerializer (property.Value.GetType ());
						serializer.Serialize (writer, property.Value);
						writer.WriteEndElement (); // XmlSerialized
					} else {
						writer.WriteAttributeString (ValueAttribute, ConvertToString (property.Value));
					}
				}
				writer.WriteEndElement (); // PropertyNode
			}
				
			if (createPropertyParent)
					writer.WriteEndElement (); // Node
		}
		
		public void Save (string fileName)
		{
			XmlTextWriter writer = new XmlTextWriter (fileName, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			try {
				writer.WriteStartElement (PropertiesRootNode);
				writer.WriteAttributeString (PropertiesVersionAttribute, PropertiesVersion);
				Write (writer, false);
				writer.WriteEndElement (); // PropertiesRootNode
			} finally {
				writer.Close ();
			}
		}
		
		class LazyXmlDeserializer
		{
			string type;
			string xml;
			
			public string Type {
				get {
					return type;
				}
			}
				
			public string Xml {
				get {
					return xml;
				}
			}
			
			public LazyXmlDeserializer (string type, string xml)
			{
				this.type = type;
				this.xml  = xml;
			}
			
			static System.Type Lookup (string type)
			{
				System.Type result = System.Type.GetType (type);
				if (result == null) {
					foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
						result = asm.GetType (type);
						if (result != null)
							break;
					}
				}
				return result;
			}
			
			public object Deserialize ()
			{
				XmlSerializer serializer = new XmlSerializer (Lookup (type));
				return serializer.Deserialize (new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes (xml))));
			}
		}
		
		public static Properties Read (XmlReader reader)
		{
			Properties result = new Properties ();
			XmlReadHelper.ReadList (reader, Node, delegate() {
				switch (reader.LocalName) {
				case PropertyNode:
					string key = reader.GetAttribute (KeyAttribute);
					if (!reader.IsEmptyElement) {
						while (reader.Read () && reader.NodeType != XmlNodeType.Element)
							;  
						switch (reader.LocalName) {
						case Node:
							result.Set (key, Read (reader));
							break;
						case XmlSerialized:
							result.Set (key, new LazyXmlDeserializer(reader.GetAttribute (TypeAttribute), reader.ReadInnerXml ()));
							break;
						}
					} else {
						result.Set (key, reader.GetAttribute (ValueAttribute));
					}
					return true;
				}			
				return false;
			});
			return result;
		}
		
		public static Properties Load (string fileName)
		{
			if (!File.Exists (fileName))
				return null;
			XmlReader reader = XmlTextReader.Create (fileName);
			try {	
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case PropertiesRootNode:
							if (reader.GetAttribute (PropertiesVersionAttribute) == PropertiesVersion)   
								return Read (reader);
							break;
						}
					}
				}
				
			} finally {
				reader.Close ();
			}
			return null;
		}
#endregion

		public override string ToString ()
		{
			StringBuilder result = new StringBuilder ();
			result.Append ("[Properties:");
			foreach (KeyValuePair<string, object> property in this.properties) {
				result.Append (property.Key);
				result.Append ("=");
				result.Append (property.Value);
				result.Append (",");
			}
			result.Append ("]");
			return result.ToString ();
		}
			
		public Properties Clone ()
		{
			Properties result = new Properties ();
			result.properties = new Dictionary<string, object> (properties);
			return result;
		}
		
		protected virtual void OnPropertyChanged (PropertyChangedEventArgs args)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, args);
		}
		
		public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
	}
}
