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
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MonoDevelop.Core
{
	public class Properties : ICustomXmlSerializer
	{
		ImmutableDictionary<string, object> properties    = ImmutableDictionary<string, object>.Empty;
		ImmutableDictionary<string, object> defaultValues = ImmutableDictionary<string, object>.Empty;
		ImmutableDictionary<Type, TypeConverter> cachedConverters = ImmutableDictionary<Type, TypeConverter>.Empty;
		Dictionary<string,EventHandler<PropertyChangedEventArgs>> propertyListeners;
		
		public IEnumerable<string> Keys {
			get {
				return properties.Keys;
			}
		}

		object Convert (object o, Type converterType)
		{
			TypeConverter converter = GetConverter (converterType);

			if (o is string) {
				try {
					return converter.ConvertFromInvariantString (o.ToString ());
				} catch (Exception) {
					return null;
				}
			}

			try {
				return converter.ConvertFrom (o);
			} catch (Exception) {
				return null;
			}
		}
		
		string ConvertToString (object o)
		{
			if (o == null)
				return null;
			TypeConverter converter = GetConverter (o.GetType ());
			return converter.ConvertToInvariantString (o);
		}
		
		TypeConverter GetConverter (Type type)
		{
			TypeConverter converter;
			if (!cachedConverters.TryGetValue (type, out converter)) {
				converter = TypeDescriptor.GetConverter (type);
				cachedConverters = cachedConverters.SetItem (type, converter);
			}
			return converter;
		}

		public object Get (string property, object defaultValue, Type type)
		{
			if (!defaultValues.ContainsKey (property))
				defaultValues = defaultValues.SetItem (property, defaultValue);

			if (GetPropertyValue (property, out object value, type))
				return value;
			properties = properties.SetItem (property, defaultValue);
			return defaultValue;
		}

		
		public T Get<T> (string property, T defaultValue)
		{
			var result = Get (property, defaultValue, typeof (T));
			return result != null ? (T)result : default (T);
		}
		
		public T Get<T> (string property)
		{
			if (GetPropertyValue (property, out object value, typeof(T)))
				return (T)value;
			if (defaultValues.TryGetValue (property, out object defaultValue))
				return (T) defaultValue;
			return default (T);
		}

		object Get (string property, Type type)
		{
			return GetPropertyValue (property, out object value, type) ? value : null;
		}

		bool GetPropertyValue (string property, out object val, Type type)
		{
			if (!properties.TryGetValue (property, out object o)) {
				val = null;
				return false;
			}

			if (o == null) {
				val = null;
				return true;
			}

			if (type.IsInstanceOfType (o)) {
				val = o;
				return true;
			}

			if (o is LazyXmlDeserializer ser) {
				// Deserialize the data and store it in the dictionary, so
				// following calls return the same object
				val = ser.Deserialize (type);
				properties = properties.SetItem (property, val);
				return true;
			}

			val = Convert (o, type);
			properties = properties.SetItem (property, val);
			return true;
		}
		
		public bool HasValue (string key)
		{
			return properties.ContainsKey (key);
		}

		//used for deserialization
		void SetFast (string key, object val)
		{
			if (val != null) {
				properties = properties.SetItem (key, val);
			}
		}

		public void Set (string key, object val)
		{
			object old = Get (key, val?.GetType () ?? typeof(object));
			if (val == null) {
				//avoid emitting the event if not necessary
				if (old == null)
					return;
				if (properties.ContainsKey (key)) 
					properties = properties.Remove (key);
			} else {
				//avoid emitting the event if not necessary
				if (val.Equals (old))
					return;
				properties = properties.SetItem (key, val);
				if (!val.GetType ().IsClass ||(val is string)) {
					if (defaultValues.ContainsKey (key)) {
						if (defaultValues[key] == val)
							properties = properties.Remove (key);
					}
				}
			}
			OnPropertyChanged (new PropertyChangedEventArgs (key, old, val));
		}

#region I/O
		public const string Node           = "Properties";
		public const string SerializedNode = "Serialized";
		public const string PropertyNode   = "Property";
		public const string KeyAttribute   = "key";
		public const string ValueAttribute = "value";
		public const string XmlSerialized  = "XmlSerialized";
		
		public const string PropertiesRootNode = "MonoDevelopProperties";
		public const string PropertiesVersionAttribute  = "version";
		public const string PropertiesVersion  = "2.0";

		void ICustomXmlSerializer.WriteTo (XmlWriter writer)
		{
			Write (writer, false);
		}
			
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (XmlReader reader)
		{
			return Read (reader);
		}
			
		public void Write (XmlWriter writer)
		{
			Write (writer, true);
		}

		class StringKeyComparer : IComparer<KeyValuePair<string,object>>
		{
			public int Compare (KeyValuePair<string, object> a, KeyValuePair<string, object> b)
			{
				return string.CompareOrdinal (a.Key, b.Key);
			}
		}

		public void Write (XmlWriter writer, bool createPropertyParent)
		{
			if (createPropertyParent)
				writer.WriteStartElement (Node);

			var toSerialize = new List<KeyValuePair<string, object>> ();
			toSerialize.AddRange (GetNonDefaultValueProperties ());
			toSerialize.Sort (new StringKeyComparer ());

			foreach (var property in toSerialize) {
				writer.WriteStartElement (PropertyNode);
				writer.WriteAttributeString (KeyAttribute, property.Key);

				if (property.Value is LazyXmlDeserializer deserializer) {
					writer.WriteRaw (deserializer.Xml);
				} else if (property.Value is ICustomXmlSerializer customXmlSerializer) {
					customXmlSerializer.WriteTo (writer);
				} else {
					if (!(property.Value is string) && property.Value.GetType ().IsClass) {
						if (!(property.Value is ICollection<string> collection && collection.Count == 0)) {
							XmlSerializer serializer = new XmlSerializer (property.Value.GetType ());
							serializer.Serialize (writer, property.Value);
						}
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
			string backupFileName = fileName + ".previous";
			string tempFileName = Path.GetDirectoryName (fileName) + 
				Path.DirectorySeparatorChar + ".#" + Path.GetFileName (fileName);
			
			//make a copy of the current file
			try {
				if (File.Exists (fileName)) {
					File.Copy (fileName, backupFileName, true);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error copying properties file '{0}' to backup\n{1}", fileName, ex);
			}
			
			//write out the new state to a temp file
			try {
				using (XmlTextWriter writer = new XmlTextWriter (tempFileName, System.Text.Encoding.UTF8)) {
					writer.Formatting = Formatting.Indented;
					writer.WriteStartElement (PropertiesRootNode);
					writer.WriteAttributeString (PropertiesVersionAttribute, PropertiesVersion);
					Write (writer, false);
					writer.WriteEndElement (); // PropertiesRootNode
				}

				//write was successful (no exception)
				//so move the file to the real location, overwriting the old file
				//(NOTE: File.Move doesn't overwrite existing files, so using Mono.Unix)
				FileService.SystemRename (tempFileName, fileName);
				return;
			}
			catch (Exception ex) {
				LoggingService.LogError ("Error writing properties file '{0}'\n{1}", tempFileName, ex);
			}
		}

		IEnumerable<KeyValuePair<string, object>> GetNonDefaultValueProperties ()
		{
			foreach (KeyValuePair<string, object> property in this.properties) {
				//don't know how the value could be null but at least we can skip it to avoid breaking completely
				if (property.Value == null)
					continue;
				if (property.Value is Properties p) {
					// Do not serialize Properties if it only has default values.
					if (!p.GetNonDefaultValueProperties ().Any ()) {
						continue;
					}
				} else {
					//don't serialize default values
					if (defaultValues.TryGetValue (property.Key, out object defaultValue)) {
						if (property.Value.Equals (defaultValue)) {
							continue;
						}
					}
				}
				yield return property;
			}
		}
		
		class LazyXmlDeserializer
		{
			string xml;
			
			public string Xml {
				get {
					return xml;
				}
			}
			
			public LazyXmlDeserializer (string xml)
			{
				this.xml  = xml;
			}
			
			public object Deserialize (Type type)
			{
				try {
					if (typeof(ICustomXmlSerializer).IsAssignableFrom (type)) {
						using (XmlReader reader = new XmlTextReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes ("<" + Properties.SerializedNode + ">" + xml + "</" + Properties.SerializedNode + ">" )))) {
							return ((ICustomXmlSerializer)type.Assembly.CreateInstance (type.FullName)).ReadFrom (reader);
						}
					}
					
					XmlSerializer serializer = new XmlSerializer (type);
					using (StreamReader sr = new StreamReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes (xml)))) {
						return serializer.Deserialize (sr);
					}

				} catch (Exception e) {
					LoggingService.LogWarning ("Caught exception while deserializing:" + type, e);
					return null;
				}
			}
		}
		
		public static Properties Read (XmlReader reader)
		{
			var result = new Properties ();
			XmlReadHelper.ReadList (reader, new string [] { Node, SerializedNode, PropertiesRootNode }, delegate() {
				switch (reader.LocalName) {
				case PropertyNode:
					string key = reader.GetAttribute (KeyAttribute);
					if (!reader.IsEmptyElement) {
						result.SetFast (key, new LazyXmlDeserializer (reader.ReadInnerXml ()));
					} else {
						result.SetFast (key, reader.GetAttribute (ValueAttribute));
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
			using (var reader = XmlReader.Create (fileName)) {
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
			}
			return null;
		}
#endregion

		public override string ToString ()
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			result.Append ("[Properties:");
			foreach (KeyValuePair<string, object> property in this.properties) {
				result.Append (property.Key);
				result.Append ("=");
				result.Append (property.Value);
				result.Append (",");
			}
			result.Append ("]");
			return StringBuilderCache.ReturnAndFree (result);
		}
			
		public Properties Clone ()
		{
			return new Properties { properties = this.properties };
		}
		
		public void AddPropertyHandler (string propertyName, EventHandler<PropertyChangedEventArgs> handler)
		{
			if (propertyListeners == null)
				propertyListeners = new Dictionary<string,EventHandler<PropertyChangedEventArgs>> ();
			
			EventHandler<PropertyChangedEventArgs> handlers = null;
			propertyListeners.TryGetValue (propertyName, out handlers);
			propertyListeners [propertyName] = handlers + handler;
		}
		
		public void RemovePropertyHandler (string propertyName, EventHandler<PropertyChangedEventArgs> handler)
		{
			if (propertyListeners == null)
				return;
			
			EventHandler<PropertyChangedEventArgs> handlers = null;
			propertyListeners.TryGetValue (propertyName, out handlers);
			handlers -= handler;
			if (handlers != null)
				propertyListeners [propertyName] = handlers;
			else
				propertyListeners.Remove (propertyName);
		}
		
		protected virtual void OnPropertyChanged (PropertyChangedEventArgs args)
		{
			PropertyChanged?.Invoke (this, args);
			
			if (propertyListeners != null) {
				EventHandler<PropertyChangedEventArgs> handlers = null;
				propertyListeners.TryGetValue (args.Key, out handlers);
				handlers?.Invoke (this, args);
			}
		}
		
		public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
	}
}
