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
using System.Collections.Immutable;

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

		public Properties ()
		{
		}
		
		T Convert<T> (object o)
		{
			if (o is T) 
				return (T)o;
			
			if ((object)o == null)
				return (T) o;
			
			TypeConverter converter = GetConverter (typeof(T));
			
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
		
		public T Get<T> (string property, T defaultValue)
		{
			if (!defaultValues.Contains (new KeyValuePair<string, object> (property, defaultValue)))
				defaultValues = defaultValues.SetItem (property, defaultValue);
			object val;
			if (GetPropertyValue<T> (property, out val))
				return Convert<T> (val);
			properties = properties.SetItem (property, defaultValue);
			return defaultValue;
		}
		
		public T Get<T> (string property)
		{
			object val;
			if (GetPropertyValue<T> (property, out val))
				return Convert<T> (val);
			if (defaultValues.TryGetValue (property, out val))
				return Convert<T> (val);
			return default(T);
		}
		
		bool GetPropertyValue<T> (string property, out object val)
		{
			if (properties.TryGetValue (property, out val)) {
				if (val is LazyXmlDeserializer) {
					// Deserialize the data and store it in the dictionary, so
					// following calls return the same object
					val = ((LazyXmlDeserializer)val).Deserialize<T> ();
					properties = properties.SetItem (property, val);
				}
				return true;
			} else {
				val = null;
				return false;
			}
		}
		
		public bool HasValue (string key)
		{
			return properties.ContainsKey (key);
		}
		
		public void Set (string key, object val)
		{
			object old = Get<object> (key);
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
		
		public void Write (XmlWriter writer, bool createPropertyParent)
		{
			if (createPropertyParent)
				writer.WriteStartElement (Node);

			foreach (KeyValuePair<string, object> property in this.properties) {
				//don't know how the value could be null but at least we can skip it to avoid breaking completely
				if (property.Value == null)
					continue;
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
			
			public T Deserialize<T> ()
			{
				try {
					if (typeof(ICustomXmlSerializer).IsAssignableFrom (typeof(T))) {
						using (XmlReader reader = new XmlTextReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes ("<" + Properties.SerializedNode + ">" + xml + "</" + Properties.SerializedNode + ">" )))) {
							return (T)((ICustomXmlSerializer)typeof(T).Assembly.CreateInstance (typeof(T).FullName)).ReadFrom (reader);
						}
					}
					
					XmlSerializer serializer = new XmlSerializer (typeof(T));
					using (StreamReader sr = new StreamReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes (xml)))) {
						return (T)serializer.Deserialize (sr);
					}
					
				} catch (Exception e) {
					LoggingService.LogWarning ("Caught exception while deserializing:" + typeof(T), e);
					return default(T);
				}
			}
		}
		
		public static Properties Read (XmlReader reader)
		{
			Properties result = new Properties ();
			XmlReadHelper.ReadList (reader, new string [] { Node, SerializedNode, PropertiesRootNode }, delegate() {
				switch (reader.LocalName) {
				case PropertyNode:
					string key = reader.GetAttribute (KeyAttribute);
					if (!reader.IsEmptyElement) {
						result.Set (key, new LazyXmlDeserializer (reader.ReadInnerXml ()));
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
			result.properties = properties;
			return result;
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
			if (PropertyChanged != null)
				PropertyChanged (this, args);
			
			if (propertyListeners != null) {
				EventHandler<PropertyChangedEventArgs> handlers = null;
				propertyListeners.TryGetValue (args.Key, out handlers);
				if (handlers != null)
					handlers (this, args);
			}
		}
		
		public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
	}
}
